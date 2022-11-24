﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using BDArmory.Utils;

namespace BDArmory.Extensions
{
    public static class VesselExtensions
    {
        public static HashSet<Vessel.Situations> InOrbitSituations = new HashSet<Vessel.Situations> { Vessel.Situations.ORBITING, Vessel.Situations.SUB_ORBITAL, Vessel.Situations.ESCAPING };

        public static bool InOrbit(this Vessel v)
        {
            if (v == null) return false;
            return InOrbitSituations.Contains(v.situation);
        }

        public static bool InVacuum(this Vessel v)
        {
            return v.atmDensity <= 0.001f;
        }

        public static bool IsUnderwater(this Vessel v)
        {
            if (!v) return false;
            return v.altitude < -20; //some boats sit slightly underwater, this is only for submersibles
        }

        /// <summary>
        /// Get the vessel's velocity accounting for whether it's in orbit and optionally whether it's above 100km (which is another hard-coded KSP limit).
        /// </summary>
        /// <param name="v"></param>
        /// <param name="altitudeCheck"></param>
        /// <returns></returns>
        public static Vector3d Velocity(this Vessel v, bool altitudeCheck = true)
        {
            try
            {
                if (v == null) return Vector3d.zero;
                if (v.InOrbit() && (!altitudeCheck || v.altitude > 1e5f)) return v.obt_velocity;
                else return v.srf_velocity;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BDArmory.VesselExtensions]: Exception thrown in Velocity: " + e.Message + "\n" + e.StackTrace);
                //return v.srf_velocity;
                return new Vector3d(0, 0, 0);
            }
        }

        public static double GetFutureAltitude(this Vessel vessel, float predictionTime = 10) => GetRadarAltitudeAtPos(AIUtils.PredictPosition(vessel, predictionTime));

        public static Vector3 GetFuturePosition(this Vessel vessel, float predictionTime = 10) => AIUtils.PredictPosition(vessel, predictionTime);

        public static float GetRadarAltitudeAtPos(Vector3 position)
        {
            double latitudeAtPos = FlightGlobals.currentMainBody.GetLatitude(position);
            double longitudeAtPos = FlightGlobals.currentMainBody.GetLongitude(position);

            float radarAlt = Mathf.Clamp(
                (float)(FlightGlobals.currentMainBody.GetAltitude(position) -
                        FlightGlobals.currentMainBody.TerrainAltitude(latitudeAtPos, longitudeAtPos)), 0,
                (float)FlightGlobals.currentMainBody.GetAltitude(position));
            return radarAlt;
        }

        // Get a vessel's "radius".
        public static float GetRadius(this Vessel vessel, Vector3 fireTransform = default(Vector3), Vector3 bounds = default(Vector3))
        {
            if (fireTransform == Vector3.zero || bounds == Vector3.zero)
            {
                // Get vessel size.
                Vector3 size = vessel.vesselSize;

				// Get largest dimension as this is mostly used for terrain/vessel avoidance. More precise "radii" should probably pass the fireTransform and bounds parameters.
				return Mathf.Max(Mathf.Max(size.x, size.y), size.z) / 2f;
            }
            else
            {
                // Check the 4 diagonals of the box and take the max.
                var radius = BDAMath.Sqrt(Mathf.Max(
                    Vector3.ProjectOnPlane(vessel.vesselTransform.up * bounds.y + vessel.vesselTransform.right * bounds.x + vessel.vesselTransform.forward * bounds.z, fireTransform).sqrMagnitude,
                    Vector3.ProjectOnPlane(-vessel.vesselTransform.up * bounds.y + vessel.vesselTransform.right * bounds.x + vessel.vesselTransform.forward * bounds.z, fireTransform).sqrMagnitude,
                    Vector3.ProjectOnPlane(vessel.vesselTransform.up * bounds.y - vessel.vesselTransform.right * bounds.x + vessel.vesselTransform.forward * bounds.z, fireTransform).sqrMagnitude,
                    Vector3.ProjectOnPlane(vessel.vesselTransform.up * bounds.y + vessel.vesselTransform.right * bounds.x - vessel.vesselTransform.forward * bounds.z, fireTransform).sqrMagnitude
                )) / 2f;
#if DEBUG
                if (radius < bounds.x / 2f && radius < bounds.y / 2f && radius < bounds.z / 2f) Debug.LogWarning($"DEBUG Radius {radius} of {vessel.vesselName} is less than half its minimum bounds {bounds}");
#endif
                return Mathf.Min(radius, (Mathf.Max(Mathf.Max(vessel.vesselSize.x, vessel.vesselSize.y), vessel.vesselSize.z) / 2f) * 1.732f); // clamp bounds to vesselsize in case of Bounds erroneously reporting vessel sizes that are impossibly large
            }
        }

        static HashSet<string> badBoundsParts = null;
        static void GetBadBoundsParts()
        {
            badBoundsParts = new HashSet<string>();
            foreach (var part in PartLoader.LoadedPartsList)
            {
                var weapon = part.partPrefab.FindModuleImplementing<Weapons.ModuleWeapon>();
                if (weapon != null)
                {
                    Debug.Log($"[BDArmory.VesselExtensions]: Adding {weapon.name} to the bounds exclusion list.");
                    badBoundsParts.Add(weapon.name); // Exclude all weapons as they can become unreasonably large if they have line renderers attached to them.
                }
            }
        }

        /// <summary>
        /// Get a vessel's bounds.
        /// </summary>
        /// <param name="vessel">The vessel to get the bounds of.</param>
        /// <param name="useBounds">Use the renderer bounds and calculate min/max manually instead of using KSP's internal functions.</param>
        /// <returns></returns>
        public static Vector3 GetBounds(this Vessel vessel, bool useBounds = true)
        {
            if (vessel is null || vessel.packed || !vessel.loaded) return Vector3.zero;
            if (badBoundsParts == null) GetBadBoundsParts();
            var vesselRot = vessel.transform.rotation;
            vessel.SetRotation(Quaternion.identity);

            Vector3 size = Vector3.zero;
            Vector3 min = default, max = default;
            if (!useBounds)
            {
                size = ShipConstruction.CalculateCraftSize(vessel.Parts, vessel.rootPart); //x: Width, y: Length, z: Height
            }
            else
            {
                var rootBound = vessel.rootPart.gameObject.GetRendererBoundsWithoutParticles();
                min = rootBound.min; max = rootBound.max;
                using (var part = vessel.Parts.GetEnumerator())
                    while (part.MoveNext())
                    {
                        if (badBoundsParts.Contains(part.Current.name)) continue; // Skip parts that are known to give bad bounds (e.g., lasers when firing).
                        var partBound = part.Current.gameObject.GetRendererBoundsWithoutParticles();
                        min.x = Mathf.Min(min.x, partBound.min.x);
                        min.y = Mathf.Min(min.y, partBound.min.y);
                        min.z = Mathf.Min(min.z, partBound.min.z);
                        max.x = Mathf.Max(max.x, partBound.max.x);
                        max.y = Mathf.Max(max.y, partBound.max.y);
                        max.z = Mathf.Max(max.z, partBound.max.z);
                    }
                size = max - min; //x: Width, y: Length, z: Height
            }
#if DEBUG
            var GetBoundString = (Part p) => { var bwop = p.gameObject.GetRendererBoundsWithoutParticles(); return $"{bwop.size}@{bwop.center}"; };
            if (size.x > 1000 || size.y > 1000 || size.z > 1000) Debug.LogWarning($"DEBUG Bounds on {vessel.vesselName} are bad: {size} (max: {max}, min: {min}, useBounds: {useBounds}). Parts: {string.Join("; ", vessel.Parts.Select(p => $"{p.name}, collider bounds: {string.Join(", ", p.GetColliderBounds().Select(b => $"{b.size}@{b.center}"))}, bounds w/o particles: {GetBoundString(p)}"))}. Root: {vessel.rootPart.name}, bounds {string.Join(", ", vessel.rootPart.GetColliderBounds().Select(b => $"{b.size}@{b.center}"))}.");
#endif
            vessel.SetRotation(vesselRot);
            return size;
        }
    }
}
