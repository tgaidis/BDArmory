//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18449
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDATargetManager : MonoBehaviour
	{
		public static Dictionary<BDArmorySettings.BDATeams, List<TargetInfo>> TargetDatabase;

		public static Dictionary<BDArmorySettings.BDATeams, List<GPSTargetInfo>> GPSTargets;

		public static List<ModuleTargetingCamera> ActiveLasers;

		public static List<MissileLauncher> FiredMissiles;

		public static List<DestructibleBuilding> LoadedBuildings;

		public static List<Vessel> LoadedVessels;

		public static BDATargetManager Instance;



		string debugString = string.Empty;

		public static float heatScore = 0;
		public static float flareScore = 0;

		public static bool hasAddedButton = false;

		void Awake()
		{
			GameEvents.onGameStateLoad.Add(LoadGPSTargets);
			GameEvents.onGameStateSave.Add(SaveGPSTargets);
			LoadedBuildings = new List<DestructibleBuilding>();
			DestructibleBuilding.OnLoaded.Add(AddBuilding);
			LoadedVessels = new List<Vessel>();
			GameEvents.onVesselLoaded.Add(AddVessel);
			GameEvents.onVesselGoOnRails.Add(RemoveVessel);
			GameEvents.onVesselGoOffRails.Add(AddVessel);
			GameEvents.onVesselCreate.Add(AddVessel);
			GameEvents.onVesselDestroy.Add(CleanVesselList);

			Instance = this;
		}

		void OnDestroy()
		{
			if(GameEvents.onGameStateLoad != null && GameEvents.onGameStateSave != null)
			{
				GameEvents.onGameStateLoad.Remove(LoadGPSTargets);
				GameEvents.onGameStateSave.Remove(SaveGPSTargets);
			}

			GPSTargets = new Dictionary<BDArmorySettings.BDATeams, List<GPSTargetInfo>>();
			GPSTargets.Add(BDArmorySettings.BDATeams.A, new List<GPSTargetInfo>());
			GPSTargets.Add(BDArmorySettings.BDATeams.B, new List<GPSTargetInfo>());


			GameEvents.onVesselLoaded.Remove(AddVessel);
			GameEvents.onVesselGoOnRails.Remove(RemoveVessel);
			GameEvents.onVesselGoOffRails.Remove(AddVessel);
			GameEvents.onVesselCreate.Remove(AddVessel);
			GameEvents.onVesselDestroy.Remove(CleanVesselList);
		}

		void Start()
		{
			//legacy targetDatabase
			TargetDatabase = new Dictionary<BDArmorySettings.BDATeams, List<TargetInfo>>();
			TargetDatabase.Add(BDArmorySettings.BDATeams.A, new List<TargetInfo>());
			TargetDatabase.Add(BDArmorySettings.BDATeams.B, new List<TargetInfo>());
			StartCoroutine(CleanDatabaseRoutine());

			if(GPSTargets == null)
			{
				GPSTargets = new Dictionary<BDArmorySettings.BDATeams, List<GPSTargetInfo>>();
				GPSTargets.Add(BDArmorySettings.BDATeams.A, new List<GPSTargetInfo>());
				GPSTargets.Add(BDArmorySettings.BDATeams.B, new List<GPSTargetInfo>());
			}

			//Laser points
			ActiveLasers = new List<ModuleTargetingCamera>();

			FiredMissiles = new List<MissileLauncher>();

			AddToolbarButton();

		}

		void AddBuilding(DestructibleBuilding b)
		{
			if(!LoadedBuildings.Contains(b))
			{
				LoadedBuildings.Add(b);
			}

			LoadedBuildings.RemoveAll(x => x == null);
		}

		void AddVessel(Vessel v)
		{
			if(!LoadedVessels.Contains(v))
			{
				LoadedVessels.Add(v);
			}
			CleanVesselList(v);
		}

		void RemoveVessel(Vessel v)
		{
			if(v != null)
			{
				LoadedVessels.Remove(v);
			}
			CleanVesselList(v);
		}

		void CleanVesselList(Vessel v)
		{
			LoadedVessels.RemoveAll(ves => ves == null);
			LoadedVessels.RemoveAll(ves => ves.loaded == false);
		}

		void AddToolbarButton()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(!hasAddedButton)
				{
					Texture buttonTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "icon", false);
					ApplicationLauncher.Instance.AddModApplication(ShowToolbarGUI, HideToolbarGUI, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
					hasAddedButton = true;
				}
			}
		}
		public void ShowToolbarGUI()
		{
			BDArmorySettings.toolbarGuiEnabled = true;	
		}

		public void HideToolbarGUI()
		{
			BDArmorySettings.toolbarGuiEnabled = false;	
		}
		void Dummy()
		{}

		void Update()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS)
			{
				UpdateDebugLabels();
			}
		}


		//Laser point stuff
		public static void RegisterLaserPoint(ModuleTargetingCamera cam)
		{
			if(ActiveLasers.Contains(cam))
			{
				return;
			}
			else
			{
				ActiveLasers.Add(cam);
			}
		}

		/// <summary>
		/// Gets the laser target painter with the least angle off boresight. Set the missile as the reference transform.
		/// </summary>
		/// <returns>The laser target painter.</returns>
		/// <param name="referenceTransform">Reference transform.</param>
		/// <param name="maxBoreSight">Max bore sight.</param>
		public static ModuleTargetingCamera GetLaserTarget(MissileLauncher ml)
		{
			Transform referenceTransform = ml.transform;
			float maxOffBoresight = ml.maxOffBoresight;
			ModuleTargetingCamera finalCam = null;
			float smallestAngle = 360;
			foreach(var cam in ActiveLasers)
			{
				if(!cam)
				{
					continue;
				}



				if(cam.cameraEnabled && cam.groundStabilized && cam.surfaceDetected && !cam.gimbalLimitReached)
				{
					/*
					if(ml.guidanceMode == MissileLauncher.GuidanceModes.BeamRiding && Vector3.Dot(ml.transform.position - cam.transform.position, ml.transform.forward) < 0)
					{
						continue;
					}
					*/

					float angle = Vector3.Angle(referenceTransform.forward, cam.groundTargetPosition-referenceTransform.position);
					if(angle < maxOffBoresight && angle < smallestAngle && ml.CanSeePosition(cam.groundTargetPosition))
					{
						smallestAngle = angle;
						finalCam = cam;
					}
				}
			}
			return finalCam;
		}

		public static TargetSignatureData GetHeatTarget(Ray ray, float scanRadius, float highpassThreshold, bool allAspect, MissileFire mf = null)
		{
			float minScore = highpassThreshold;
			float minMass = 0.5f;
			TargetSignatureData finalData = TargetSignatureData.noTarget;
			float finalScore = 0;
			foreach(var vessel in BDATargetManager.LoadedVessels)
			{
				if(!vessel || !vessel.loaded)
				{
					continue;
				}

				TargetInfo tInfo = vessel.gameObject.GetComponent<TargetInfo>();
				if(mf == null || 
					!tInfo || 
					!(mf && tInfo.isMissile && tInfo.team != BDATargetManager.BoolToTeam(mf.team) && (tInfo.missileModule.MissileState == MissileLauncher.MissileStates.Boost || tInfo.missileModule.MissileState == MissileLauncher.MissileStates.Cruise)))
				{
					if(vessel.GetTotalMass() < minMass)
					{
						continue;
					}
				}

				if(RadarUtils.TerrainCheck(ray.origin, vessel.transform.position))
				{
					continue;
				}

				float angle = Vector3.Angle(vessel.CoM-ray.origin, ray.direction);
				if(angle < scanRadius)
				{
					float score = 0;
					foreach(var part in vessel.Parts)
					{
						if(!part) continue;
						if(!allAspect)
						{
							if(!Misc.CheckSightLine(ray.origin, part.transform.position, 10000, 5, 5)) continue;
						}

						float thisScore = (float)(part.thermalInternalFluxPrevious+part.skinTemperature) * (15/Mathf.Max(15,angle));
						thisScore *= Mathf.Pow(1400,2)/Mathf.Clamp((vessel.CoM-ray.origin).sqrMagnitude, 90000, 36000000);
						score = Mathf.Max (score, thisScore);
					}

					if(vessel.LandedOrSplashed)
					{
						score /= 4;
					}

					score *= Mathf.Clamp(Vector3.Angle(vessel.transform.position-ray.origin, -VectorUtils.GetUpDirection(ray.origin))/90, 0.5f, 1.5f);

					if(score > finalScore)
					{
						finalScore = score;
						finalData = new TargetSignatureData(vessel, score);
					}
				}
			}

			heatScore = finalScore;//DEBUG
			flareScore = 0; //DEBUG
			foreach(var flare in BDArmorySettings.Flares)
			{
				if(!flare) continue;

				float angle = Vector3.Angle(flare.transform.position-ray.origin, ray.direction);
				if(angle < scanRadius)
				{
					float score = flare.thermal * Mathf.Clamp01(15/angle);
					score *= Mathf.Pow(1400,2)/Mathf.Clamp((flare.transform.position-ray.origin).sqrMagnitude, 90000, 36000000);

					score *= Mathf.Clamp(Vector3.Angle(flare.transform.position-ray.origin, -VectorUtils.GetUpDirection(ray.origin))/90, 0.5f, 1.5f);

					if(score > finalScore)
					{
						flareScore = score;//DEBUG
						finalScore = score;
						finalData = new TargetSignatureData(flare, score);
					}
				}
			}



			if(finalScore < minScore)
			{
				finalData = TargetSignatureData.noTarget;
			}

			return finalData;
		}








		void UpdateDebugLabels()
		{
			debugString = string.Empty;
			debugString+= ("Team A's targets:");
			foreach(var targetInfo in TargetDatabase[BDArmorySettings.BDATeams.A])
			{
				if(targetInfo)
				{
					if(!targetInfo.Vessel)
					{
						debugString+= ("\n - A target with no vessel reference.");
					}
					else
					{
						debugString+= ("\n - "+targetInfo.Vessel.vesselName+", Engaged by "+targetInfo.numFriendliesEngaging);
					}
				}
				else
				{
					debugString+= ("\n - A null target info.");
				}
			}
			debugString+= ("\nTeam B's targets:");
			foreach(var targetInfo in TargetDatabase[BDArmorySettings.BDATeams.B])
			{
				if(targetInfo)
				{
					if(!targetInfo.Vessel)
					{
						debugString+= ("\n - A target with no vessel reference.");
					}
					else
					{
						debugString+= ("\n - "+targetInfo.Vessel.vesselName+", Engaged by "+targetInfo.numFriendliesEngaging);
					}
				}
				else
				{
					debugString+= ("\n - A null target info.");
				}
			}

			debugString += "\n\nHeat score: "+heatScore;
			debugString += "\nFlare score: "+flareScore;

			/*
			debugString += "\n\nLoaded vessels: ";
			foreach(var v in LoadedVessels)
			{
				if(v)
				{
					debugString += "\n" + v.vesselName;
				}
			}
			*/
		}




		//gps stuff
		void SaveGPSTargets(ConfigNode saveNode)
		{
			string saveTitle = HighLogic.CurrentGame.Title;
			Debug.Log("Save title: " + saveTitle);
			ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/gpsTargets.cfg");
			if(fileNode == null)
			{
				fileNode = new ConfigNode();
				fileNode.AddNode("BDARMORY");
				fileNode.Save("GameData/BDArmory/gpsTargets.cfg");

			}
		
			if(fileNode!=null && fileNode.HasNode("BDARMORY"))
			{
				ConfigNode node = fileNode.GetNode("BDARMORY");

				if(GPSTargets == null || !FlightGlobals.ready)
				{
					return;
				}

				ConfigNode gpsNode = null;
				if(node.HasNode("BDAGPSTargets"))
				{
					foreach(var n in node.GetNodes("BDAGPSTargets"))
					{
						if(n.GetValue("SaveGame") == saveTitle)
						{
							gpsNode = n;
							break;
						}
					}

					if(gpsNode == null)
					{
						gpsNode = node.AddNode("BDAGPSTargets");
						gpsNode.AddValue("SaveGame", saveTitle);
					}
				}
				else
				{
					gpsNode = node.AddNode("BDAGPSTargets");
					gpsNode.AddValue("SaveGame", saveTitle);
				}

				if(GPSTargets[BDArmorySettings.BDATeams.A].Count == 0 && GPSTargets[BDArmorySettings.BDATeams.B].Count == 0)
				{
					//gpsNode.SetValue("Targets", string.Empty, true);
					return;
				}

				string targetString = GPSListToString();
				gpsNode.SetValue("Targets", targetString, true);
				fileNode.Save("GameData/BDArmory/gpsTargets.cfg");
				Debug.Log("==== Saved BDA GPS Targets ====");
			}
		}

	

		void LoadGPSTargets(ConfigNode saveNode)
		{
			ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/gpsTargets.cfg");
			string saveTitle = HighLogic.CurrentGame.Title;

			if(fileNode != null && fileNode.HasNode("BDARMORY"))
			{
				ConfigNode node = fileNode.GetNode("BDARMORY");

				foreach(var gpsNode in node.GetNodes("BDAGPSTargets"))
				{
					if(gpsNode.HasValue("SaveGame") && gpsNode.GetValue("SaveGame") == saveTitle)
					{
						if(gpsNode.HasValue("Targets"))
						{
							string targetString = gpsNode.GetValue("Targets");
							if(targetString == string.Empty)
							{
								Debug.Log("==== BDA GPS Target string was empty! ====");
								return;
							}
							else
							{
								StringToGPSList(targetString);
								Debug.Log("==== Loaded BDA GPS Targets ====");
							}
						}
						else
						{
							Debug.Log("==== No BDA GPS Targets value found! ====");
						}
					}
				}
			}
		}

		//format: SAVENAME&name,lat,long,alt;name,lat,long,alt:name,lat,long,alt  (A;A;A:B;B)
		private string GPSListToString()
		{
			string finalString = string.Empty;
			string aString = string.Empty;
			foreach(var gpsInfo in GPSTargets[BDArmorySettings.BDATeams.A])
			{
				aString += gpsInfo.name;
				aString += ",";
				aString += gpsInfo.gpsCoordinates.x;
				aString += ",";
				aString += gpsInfo.gpsCoordinates.y;
				aString += ",";
				aString += gpsInfo.gpsCoordinates.z;
				aString += ";";
			}
			if(aString == string.Empty)
			{
				aString = "null";
			}
			finalString += aString;
			finalString += ":";

			string bString = string.Empty;
			foreach(var gpsInfo in GPSTargets[BDArmorySettings.BDATeams.B])
			{
				bString += gpsInfo.name;
				bString += ",";
				bString += gpsInfo.gpsCoordinates.x;
				bString += ",";
				bString += gpsInfo.gpsCoordinates.y;
				bString += ",";
				bString += gpsInfo.gpsCoordinates.z;
				bString += ";";
			}
			if(bString == string.Empty)
			{
				bString = "null";
			}
			finalString += bString;

			return finalString;
		}

		private void StringToGPSList(string listString)
		{
			if(GPSTargets == null)
			{
				GPSTargets = new Dictionary<BDArmorySettings.BDATeams, List<GPSTargetInfo>>();
			}
			GPSTargets.Clear();
			GPSTargets.Add(BDArmorySettings.BDATeams.A, new List<GPSTargetInfo>());
			GPSTargets.Add(BDArmorySettings.BDATeams.B, new List<GPSTargetInfo>());

			if(listString == null || listString == string.Empty)
			{
				Debug.Log("=== GPS List string was empty or null ===");
				return;
			}

			string[] teams = listString.Split(new char[]{ ':' });

			Debug.Log("==== Loading GPS Targets. Number of teams: " + teams.Length);

			if(teams[0] != null && teams[0].Length > 0 && teams[0] != "null")
			{
				string[] teamACoords = teams[0].Split(new char[]{ ';' });
				for(int i = 0; i < teamACoords.Length; i++)
				{
					if(teamACoords[i] != null && teamACoords[i].Length > 0)
					{
						string[] data = teamACoords[i].Split(new char[]{ ',' });
						string name = data[0];
						double lat = double.Parse(data[1]);
						double longi = double.Parse(data[2]);
						double alt = double.Parse(data[3]);
						GPSTargetInfo newInfo = new GPSTargetInfo(new Vector3d(lat, longi, alt), name);
						GPSTargets[BDArmorySettings.BDATeams.A].Add(newInfo);
					}
				}
			}

			if(teams[1] != null && teams[1].Length > 0 && teams[1] != "null")
			{
				string[] teamBCoords = teams[1].Split(new char[]{ ';' });
				for(int i = 0; i < teamBCoords.Length; i++)
				{
					if(teamBCoords[i] != null && teamBCoords[i].Length > 0)
					{
						string[] data = teamBCoords[i].Split(new char[]{ ',' });
						string name = data[0];
						double lat = double.Parse(data[1]);
						double longi = double.Parse(data[2]);
						double alt = double.Parse(data[3]);
						GPSTargetInfo newInfo = new GPSTargetInfo(new Vector3d(lat, longi, alt), name);
						GPSTargets[BDArmorySettings.BDATeams.B].Add(newInfo);
					}
				}
			}
		}








		//Legacy target managing stuff

		public static BDArmorySettings.BDATeams BoolToTeam(bool team)
		{
			return team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
		}

		public static BDArmorySettings.BDATeams OtherTeam(BDArmorySettings.BDATeams team)
		{
			return team == BDArmorySettings.BDATeams.A ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
		}

		IEnumerator CleanDatabaseRoutine()
		{
			while(enabled)
			{
				yield return new WaitForSeconds(5);
			
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => target == null);
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => target.team == BDArmorySettings.BDATeams.A);
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => !target.isThreat);

				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => target == null);
				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => target.team == BDArmorySettings.BDATeams.B);
				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => !target.isThreat);
			}
		}

		void RemoveTarget(TargetInfo target, BDArmorySettings.BDATeams team)
		{
			TargetDatabase[team].Remove(target);
		}

		public static void ReportVessel(Vessel v, MissileFire reporter)
		{
			if(!v) return;
			if(!reporter) return;

			TargetInfo info = v.gameObject.GetComponent<TargetInfo>();
			if(!info)
			{
				foreach(var mf in v.FindPartModulesImplementing<MissileFire>())
				{
					if(mf.team != reporter.team)
					{
						info = v.gameObject.AddComponent<TargetInfo>();
					}
					return;
				}

				foreach(var ml in v.FindPartModulesImplementing<MissileLauncher>())
				{
					if(ml.hasFired)
					{
						if(ml.team != reporter.team)
						{
							info = v.gameObject.AddComponent<TargetInfo>();
						}
					}

					return;
				}
			}
			else
			{
				info.detectedTime = Time.time;
			}
		}

		public static void ClearDatabase()
		{
			foreach(var t in TargetDatabase.Keys)
			{
				foreach(var target in TargetDatabase[t])
				{
					target.detectedTime = 0;
				}
			}

			TargetDatabase[BDArmorySettings.BDATeams.A].Clear();
			TargetDatabase[BDArmorySettings.BDATeams.B].Clear();
		}

		public static TargetInfo GetAirToAirTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target.numFriendliesEngaging >= 2) continue;
				if(target && target.Vessel && !target.isLanded && !target.isMissile)
				{
					if(finalTarget == null || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging)
					{
						finalTarget = target;
					}
				}
			}

			return finalTarget;
		}
		 

		public static TargetInfo GetClosestTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && !target.isMissile)
				{
					if(finalTarget == null || (target.IsCloser(finalTarget, mf)))
					{
						finalTarget = target;
					}
				}
			}

			return finalTarget;
		}

		public static List<TargetInfo> GetAllTargetsExcluding(List<TargetInfo> excluding, MissileFire mf)
		{
			List<TargetInfo> finalTargets = new List<TargetInfo>();
			BDArmorySettings.BDATeams team = BoolToTeam(mf.team);

			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && !excluding.Contains(target))
				{
					finalTargets.Add(target);
				}
			}

			return finalTargets;
		}

		public static TargetInfo GetLeastEngagedTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;
			
			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && !target.isMissile)
				{
					if(finalTarget == null || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging)
					{
						finalTarget = target;
					}
				}
			}
		
			return finalTarget;
		}

		public static TargetInfo GetMissileTarget(MissileFire mf, bool targetingMeOnly = false)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && target.isMissile && target.isThreat && mf.CanSeeTarget(target.Vessel) )
				{
					if(target.missileModule)
					{
						if(targetingMeOnly)
						{
							if(Vector3.SqrMagnitude(target.missileModule.targetPosition - mf.vessel.CoM) > 60 * 60)
							{
								continue;
							}
						}
					}
					else
					{
						Debug.LogWarning("checking target missile -  doesn't have missile module");
					}


					if(((finalTarget == null && target.numFriendliesEngaging < 2) || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging))
					{
						finalTarget = target;
					}
				}
			}
			
			return finalTarget;
		}

		public static TargetInfo GetUnengagedMissileTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;

			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && target.isMissile && target.isThreat)
				{
					if(target.numFriendliesEngaging == 0)
					{
						return target;
					}
				}
			}
			
			return null;
		}

		public static TargetInfo GetClosestMissileTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = BoolToTeam(mf.team);
			TargetInfo finalTarget = null;
			
			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && target.isMissile)
				{
					bool isHostile = false;
					if(target.isThreat)
					{
						isHostile = true;
					}

					if(isHostile && (finalTarget == null || target.IsCloser(finalTarget, mf)))
					{
						finalTarget = target;
					}
				}
			}
			
			return finalTarget;
		}



		void OnGUI()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS)	
			{
				GUI.Label(new Rect(600,100,600,600), debugString);	
			}

			if(competitionStarting)
			{
				GUIStyle cStyle = new GUIStyle(HighLogic.Skin.label);
				cStyle.fontStyle = FontStyle.Bold;
				cStyle.fontSize = 22;
				cStyle.alignment = TextAnchor.UpperCenter;
				Rect cLabelRect = new Rect(0, Screen.height / 6, Screen.width, 100);


				GUIStyle cShadowStyle = new GUIStyle(cStyle);
				Rect cShadowRect = new Rect(cLabelRect);
				cShadowRect.x += 2;
				cShadowRect.y += 2;
				cShadowStyle.normal.textColor = new Color(0, 0, 0, 0.75f);

				GUI.Label(cShadowRect, competitionStatus, cShadowStyle);
				GUI.Label(cLabelRect, competitionStatus, cStyle);
			}
		}


		//Competition mode
		public static bool competitionStarting = false;
		string competitionStatus = "";
		bool stopCompetition = false;
		public void StartCompetitionMode(float distance)
		{
			if(!competitionStarting)
			{
				stopCompetition = false;
				StartCoroutine(CompetitionModeRoutine(distance/2));
			}
		}

		public void StopCompetition()
		{
			stopCompetition = true;
		}

		IEnumerator CompetitionModeRoutine(float distance)
		{
			competitionStarting = true;
			competitionStatus = "Competition: Pilots are taking off.";
			Dictionary<BDArmorySettings.BDATeams, List<BDModulePilotAI>> pilots = new Dictionary<BDArmorySettings.BDATeams, List<BDModulePilotAI>>();
			pilots.Add(BDArmorySettings.BDATeams.A, new List<BDModulePilotAI>());
			pilots.Add(BDArmorySettings.BDATeams.B, new List<BDModulePilotAI>());
			foreach(var v in LoadedVessels)
			{
				if(!v || !v.loaded) continue;
				BDModulePilotAI pilot = null;
				foreach(var p in v.FindPartModulesImplementing<BDModulePilotAI>())
				{
					pilot = p;
					break;
				}

				if(!pilot || !pilot.weaponManager) continue;

				pilots[BoolToTeam(pilot.weaponManager.team)].Add(pilot);
				pilot.ActivatePilot();
				pilot.standbyMode = false;
				if(pilot.weaponManager.guardMode)
				{
					pilot.weaponManager.ToggleGuardMode();
				}
			}

			//clear target database so pilots don't attack yet
			ClearDatabase();

			if(pilots[BDArmorySettings.BDATeams.A].Count == 0 || pilots[BDArmorySettings.BDATeams.B].Count == 0)
			{
				Debug.Log("Unable to start competition mode - one or more teams is empty");
				competitionStatus = "Competition: Failed!  One or more teams is empty.";
				yield return new WaitForSeconds(2);
				competitionStarting = false;
				yield break;
			}

			BDModulePilotAI aLeader = pilots[BDArmorySettings.BDATeams.A][0];
			BDModulePilotAI bLeader = pilots[BDArmorySettings.BDATeams.B][0];

			aLeader.weaponManager.wingCommander.CommandAllFollow();
			bLeader.weaponManager.wingCommander.CommandAllFollow();



			//wait till the leaders are airborne
			while(aLeader.vessel.LandedOrSplashed || bLeader.vessel.LandedOrSplashed)
			{
				if(stopCompetition)
				{
					foreach(var t in pilots.Keys)
					{
						foreach(var p in pilots[t])
						{
							if(p && p.vessel.LandedOrSplashed)
							{
								p.DeactivatePilot();
							}
						}
					}
					competitionStarting = false;
					stopCompetition = false;
					yield break;
				}

				yield return null;
			}
			competitionStatus = "Competition: Sending pilots to start position.";
			Vector3 aDirection = (aLeader.vessel.CoM - bLeader.vessel.CoM).normalized;
			Vector3 bDirection = (bLeader.vessel.CoM - aLeader.vessel.CoM).normalized;
			Vector3 center = (aLeader.vessel.CoM + bLeader.vessel.CoM) / 2f;
			Vector3 aDestination = center + (aDirection * distance);
			Vector3 bDestination = center + (bDirection * distance);
			aDestination = VectorUtils.WorldPositionToGeoCoords(aDestination, FlightGlobals.currentMainBody);
			bDestination = VectorUtils.WorldPositionToGeoCoords(bDestination, FlightGlobals.currentMainBody);

			Vector3 centerGPS = VectorUtils.WorldPositionToGeoCoords(center, FlightGlobals.currentMainBody);

			aLeader.CommandFlyTo(aDestination);
			bLeader.CommandFlyTo(bDestination);

			//wait till everyone is in position
			bool waiting = true;
			while(waiting)
			{
				waiting = false;

				if(stopCompetition)
				{
					foreach(var t in pilots.Keys)
					{
						foreach(var p in pilots[t])
						{
							if(p && p.vessel.LandedOrSplashed)
							{
								p.DeactivatePilot();
							}
						}
					}
					competitionStarting = false;
					stopCompetition = false;
					yield break;
				}

				if(Vector3.Distance(aLeader.transform.position, bLeader.transform.position) < distance*1.95f)
				{
					waiting = true;
				}
				else
				{
					foreach(var t in pilots.Keys)
					{
						foreach(var p in pilots[t])
						{
							if(p.currentCommand == BDModulePilotAI.PilotCommands.Follow && Vector3.Distance(p.vessel.CoM, p.commandLeader.vessel.CoM) > 1000)
							{
								competitionStatus = "Competition: Waiting for teams to get in position.";
								waiting = true;
							}
						}
					}
				}

				yield return new WaitForSeconds(1);
			}

			//start the match
			foreach(var t in pilots.Keys)
			{
				foreach(var p in pilots[t])
				{
					//enable guard mode
					if(p && !p.weaponManager.guardMode)
					{
						p.weaponManager.ToggleGuardMode();
					}

					//report all vessels
					if(BoolToTeam(p.weaponManager.team) == BDArmorySettings.BDATeams.B)
					{
						ReportVessel(p.vessel, aLeader.weaponManager);
					}
					else
					{
						ReportVessel(p.vessel, bLeader.weaponManager);
					}

					//release command
					p.ReleaseCommand();
				}
			}
			competitionStatus = "Competition starting!  Good luck!";
			yield return new WaitForSeconds(2);
			competitionStarting = false;

		}
	}
}

