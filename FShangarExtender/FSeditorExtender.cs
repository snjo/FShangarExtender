using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using System.IO;

namespace FShangarExtender
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	class FSeditorExtender : MonoBehaviour
	{
		private static bool SPHextentsAlwaysMax = true;
		private static bool VABextentsAlwaysMax = true;
		private static bool BuildingStartMaxSize = false;
		private static float configCamMaxDistance = 250f; //outdated
		private static float configMaxWorkArea = 1000f; //outdated

		private static string sceneGeometryNameVAB = "model_vab_parent_prefab";
		private static string sceneGeometryNameSPH = "model_sph_parent_prefab";
		private static string[] sceneGeometryNameVABStatics = new string[] { "model_vab_interior_occluder_v16", "model_vab_interior_props_v16", "model_vab_walls", "model_vab_windows" };
		private static string sceneGeometryNameSPHStatic = "model_sph_interior_main_v16";



		private List<VABCamera> vabCameras = new List<VABCamera>();
		private List<SPHCamera> sphCameras = new List<SPHCamera>();
		private List<Light> sceneLights = new List<Light>();
		private List<Node> scalingNodes = new List<Node>();
		private List<Node> nonScalingNodes = new List<Node>();
		private Transform tempParent;
		private Vector3 originalConstructionBoundExtends;
		private Vector3 originalCameraOffsetBoundExtends;
		private bool sceneScaled = false;
		private static string s_hotKey = Constants.defaultHotKey;
		private static float scalingFactor = Constants.defaultScaleFactor;
		private static bool hideHangars = false;
		private static bool advancedDebug = false;
		private bool hangarExtenderReady = false;



		private class Node
		{
			public Transform transform;
			public Transform originalParent;
			public Vector3 defaultScaling;
		}


		/// <summary>
		/// method to check a NodeList for a certain contained Transform
		/// </summary>
		/// <param name="nodeList"></param>
		/// <param name="controlObject"></param>
		/// <returns></returns>
		private static bool doesListContain(List<Node> nodeList, Transform controlObject)
		{
			if (nodeList.Count > 0)
			{
				foreach (Node n in nodeList)
				{
					if (n.transform == controlObject)
					{
						return true;
					}
				}
			}
			return false;
		}


		/// <summary>
		/// default mono awake method which is called very first of the class
		/// </summary>
		public void Awake()
		{
		}


		/// <summary>
		/// default mono start method which is called each time the thing is started
		/// </summary>
		public void Start()
		{
			if (sceneScaled)
			{
				StartCoroutine(toggleScaling());
			}
			resetMod();
			StartCoroutine(initFSHangarExtender());
		}


		/// <summary>
		/// default mono update method which is called once each frame
		/// </summary>
		public void Update()
		{
			if (hangarExtenderReady)
			{
				bool gotKeyPress = rescaleKeyPressed();
				if (gotKeyPress)
				{
					Debugger.advancedDebug("Scaling Hangar Geometry", advancedDebug);
					StartCoroutine(toggleScaling());
				}
			}
		}


		/// <summary>
		/// default mono OnDestroy when the object gets removed from scene
		/// </summary>
		public void OnDestroy()
		{
			if (sceneScaled)
			{
				StartCoroutine(toggleScaling());
			}
			resetMod();
			Debugger.advancedDebug("cleared the lists", advancedDebug);
		}


		/// <summary>
		/// method to completly reset the whole mod
		/// </summary>
		private void resetMod()
		{
			scalingNodes.Clear();
			nonScalingNodes.Clear();
			sceneLights.Clear();
			vabCameras.Clear();
			sphCameras.Clear();

			if (hideHangars)
			{
				foreach (Node n in scalingNodes)
				{
					List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
					n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
					foreach (SkinnedMeshRenderer r in skinRenderers)
					{
						r.enabled = true;
					}
					List<MeshRenderer> renderers = new List<MeshRenderer>();
					n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
					foreach (MeshRenderer r in renderers)
					{
						r.enabled = true;
					}
				}
			}
			sceneScaled = false;
			hangarExtenderReady = false;
		}


		/// <summary>
		/// method to initalize the whole Extender
		/// </summary>
		/// <returns></returns>
		private IEnumerator<YieldInstruction> initFSHangarExtender()
		{
			Debugger.advancedDebug(Constants.debugVersion+" Starting Up", true);
			getSettings();
			Debugger.advancedDebug("Attempting to init", true);
			while ((object)EditorBounds.Instance == null && HighLogic.LoadedScene == GameScenes.EDITOR)
			{
				hangarExtenderReady = false;
				yield return null;
			}
			while (scalingNodes.Count < 1)
			{
				fetchSceneNodes();
				yield return null;
			}
			originalConstructionBoundExtends = EditorBounds.Instance.constructionBounds.extents * 2;
			originalCameraOffsetBoundExtends = EditorBounds.Instance.cameraOffsetBounds.extents * 2;
			EditorBounds.Instance.cameraMinDistance /= scalingFactor;
			fetchCameras();
			fetchLights();
			listNodes();
			hangarExtenderReady = true;
			sceneScaled = false;
			Debugger.advancedDebug("Attempting to init successful", true);
			Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", advancedDebug);
		}


		/// <summary>
		/// method to update the camera bounds and scale the scene
		/// </summary>
		/// <returns></returns>
		private IEnumerator<YieldInstruction> toggleScaling() // code taken from NathanKell, https://github.com/NathanKell/RealSolarSystem/blob/master/Source/CameraFixer.cs
		{
			Debugger.advancedDebug("Attempting work area scaling", advancedDebug);
			while ((object)EditorBounds.Instance == null)
			{
				yield return null;
			}
			if ((object)(EditorBounds.Instance) != null)
			{
				if (sceneScaled)
				{
					Debugger.advancedDebug("shrink scene", advancedDebug);

					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (originalConstructionBoundExtends));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (originalCameraOffsetBoundExtends));
					EditorBounds.Instance.cameraMaxDistance /= scalingFactor;
					Debugger.advancedDebug("Bounds scaled", advancedDebug);

					foreach (VABCamera c in vabCameras)
					{
						c.maxHeight /= scalingFactor;
						c.maxDistance /= scalingFactor;
						c.camera.farClipPlane /= scalingFactor;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] /= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("vabCameras scaled", advancedDebug);
					foreach (SPHCamera c in sphCameras)
					{
						c.maxHeight /= scalingFactor;
						c.maxDistance /= scalingFactor;
						c.maxDisplaceX /= scalingFactor;
						c.maxDisplaceZ /= scalingFactor;
						c.camera.farClipPlane /= scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] /= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("sphCameras scaled", advancedDebug);
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane /= scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] /= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("CameraManager scaled", advancedDebug);
					RenderSettings.fogStartDistance /= scalingFactor;
					RenderSettings.fogEndDistance /= scalingFactor;

					Debugger.advancedDebug("show Hangars", advancedDebug);
					if (scalingNodes != null && scalingNodes.Count > 0)
					{
						foreach (Node n in scalingNodes)
						{
							n.transform.localScale = n.defaultScaling;
						}
					}
					if (nonScalingNodes != null && nonScalingNodes.Count > 0)
					{
						foreach (Node n in nonScalingNodes)
						{
							n.transform.parent = n.originalParent;
							n.transform.localScale = n.defaultScaling;
						}
					}
					Debugger.advancedDebug("scaling lights", advancedDebug);
					if (sceneLights != null && sceneLights.Count > 0)
					{
						foreach (Light l in sceneLights)
						{
							if (l.type == LightType.Spot)
							{
								l.gameObject.SetActive(true);
								l.range /= scalingFactor;
							}
						}
					}

					if (hideHangars)
					{
						Debugger.advancedDebug("hide Hangars", advancedDebug);
						if (scalingNodes != null && scalingNodes.Count > 0)
						{
							foreach (Node n in scalingNodes)
							{
								List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
								n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
								foreach (SkinnedMeshRenderer r in skinRenderers)
								{
									r.enabled = true;
								}
								List<MeshRenderer> renderers = new List<MeshRenderer>();
								n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
								foreach (MeshRenderer r in renderers)
								{
									r.enabled = true;
								}
							}
						}
						Debugger.advancedDebug("hide Hangars complete", advancedDebug);
					}

					Debugger.advancedDebug("shrink scene complete", advancedDebug);
				}
				else
				{
					Debugger.advancedDebug("rise scene", advancedDebug);

					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (originalConstructionBoundExtends * scalingFactor));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (originalCameraOffsetBoundExtends * scalingFactor));
					EditorBounds.Instance.cameraMaxDistance *= scalingFactor;
					Debugger.advancedDebug("Bounds scaled", advancedDebug);

					foreach (VABCamera c in vabCameras)
					{
						c.maxHeight *= scalingFactor;
						c.maxDistance *= scalingFactor;
						c.camera.farClipPlane *= scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] *= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("vabCameras scaled", advancedDebug);
					foreach (SPHCamera c in sphCameras)
					{
						c.maxHeight *= scalingFactor;
						c.maxDistance *= scalingFactor;
						c.maxDisplaceX *= scalingFactor;
						c.maxDisplaceZ *= scalingFactor;
						c.camera.farClipPlane *= scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] *= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("sphCameras scaled", advancedDebug);
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane *= scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] *= scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("CameraManager scaled", advancedDebug);
					RenderSettings.fogStartDistance *= scalingFactor;
					RenderSettings.fogEndDistance *= scalingFactor;

					if (hideHangars)
					{
						Debugger.advancedDebug("hide Hangars", advancedDebug);
						if (scalingNodes != null && scalingNodes.Count > 0)
						{
							foreach (Node n in scalingNodes)
							{
								List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
								n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
								foreach (SkinnedMeshRenderer r in skinRenderers)
								{
									r.enabled = false;
								}
								List<MeshRenderer> renderers = new List<MeshRenderer>();
								n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
								foreach (MeshRenderer r in renderers)
								{
									r.enabled = false;
								}
							}
						}
						Debugger.advancedDebug("hide Hangars complete", advancedDebug);
					}

					Debugger.advancedDebug("show Hangars", advancedDebug);
					if (nonScalingNodes != null && nonScalingNodes.Count > 0)
					{
						foreach (Node n in nonScalingNodes)
						{
							n.transform.parent = tempParent;
							n.transform.localScale = n.defaultScaling;
						}
					}
					if (scalingNodes != null && scalingNodes.Count > 0)
					{
						foreach (Node n in scalingNodes)
						{
							n.transform.localScale = n.defaultScaling * scalingFactor;
						}
					}
					Debugger.advancedDebug("scaling lights", advancedDebug);
					if (sceneLights != null && sceneLights.Count > 0)
					{
						foreach (Light l in sceneLights)
						{
							if (l.type == LightType.Spot)
							{
								l.gameObject.SetActive(true);
								l.range *= scalingFactor;
							}
						}
					}
					Debugger.advancedDebug("rise scene complete", advancedDebug);
				}
				sceneScaled = !sceneScaled;
			}
			Debugger.advancedDebug("Attempting work area scaling complete", advancedDebug);
			Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", advancedDebug);
		}


		/// <summary>
		/// method to fetch the cameras in the scene and assigns the zoom limits to it
		/// </summary>
		private void fetchCameras()
		{
			vabCameras = ((VABCamera[])Resources.FindObjectsOfTypeAll(typeof(VABCamera))).ToList();
			sphCameras = ((SPHCamera[])Resources.FindObjectsOfTypeAll(typeof(SPHCamera))).ToList();
		}


		/// <summary>
		/// method to return a list of every single child transforms from a wanted transform
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		private static List<Transform> getTransformChildsList(Transform parent)
		{
			if (parent != null)
			{
				List<Transform> newList = new List<Transform>();
				foreach (Transform t in parent)
				{
					newList.Add(t);
					foreach (Transform tx in getTransformChildsList(t))
					{
						newList.Add(tx);
					}
				}
				return newList;
			}
			return null;
		}


		/// <summary>
		/// debug output of the root transforms
		/// </summary>
		private static void listNodes()
		{
			List<Transform> rootNodes = new List<Transform>();
			foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
			{
				Transform newTransform = t.root;
				while (newTransform.parent != null)
				{
					newTransform = newTransform.parent;
				}
				if (!rootNodes.Contains(newTransform))
				{
					rootNodes.Add(newTransform);
				}
			}
			foreach (Transform t in rootNodes)
			{
				if (!(string.Equals(t.name, "_UI") || string.Equals(t.name, "ScreenSafeUI")))
				{
					Debugger.advancedDebug("RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf, advancedDebug);
				}
			}
		}


		/// <summary>
		/// debug output for the root transforms and their childs
		/// </summary>
		private static void listNodesAdvanced()
		{
			{
				List<Transform> rootNodes = new List<Transform>();
				foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
				{
					Transform newTransform = t.root;
					while (newTransform.parent != null)
					{
						newTransform = newTransform.parent;
					}
					if (!rootNodes.Contains(newTransform))
					{
						rootNodes.Add(newTransform);
					}
				}
				foreach (Transform t in rootNodes)
				{
					if (!(string.Equals(t.name, "_UI") || string.Equals(t.name, "ScreenSafeUI")))
					{
						Debugger.advancedDebug("RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf, advancedDebug);

						List<Transform> childs = getTransformChildsList(t);
						foreach (Transform tx in childs)
						{
							Debugger.advancedDebug("ChildTransform = " + tx.name + " | Parent = " + tx.parent.name + " | isActive = " + tx.gameObject.activeSelf, advancedDebug);
						}

					}
				}
			}
		}


		/// <summary>
		/// collects all Lights in the scene.
		/// </summary>
		private void fetchLights()
		{
			sceneLights = ((Light[])FindObjectsOfType(typeof(Light))).ToList();
			foreach (Light l in sceneLights)
			{
				Debugger.advancedDebug("Light = " + l.name + " - Type = " + l.type + " - Intensity = " + l.intensity, advancedDebug);
			}
		}


		/// <summary>
		/// method that will read the transforms from the scene
		/// </summary>
		private void fetchSceneNodes()
		{
			Debugger.advancedDebug("fetchSceneNodes Nodes found = " + UnityEngine.Object.FindObjectsOfType<Transform>().Length, advancedDebug);
			List<Transform> rootNodes = new List<Transform>();
			scalingNodes.Clear();
			nonScalingNodes.Clear();
			GameObject temp = new GameObject();
			tempParent = temp.transform;
			tempParent.position = Vector3.zero;
			tempParent.localScale = Vector3.one;
			tempParent.name = Constants.defaultTempParentName;

			foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
			{
				Transform newTransform = t.root;
				while (newTransform.parent != null)
				{
					newTransform = newTransform.parent;
				}
				if (!rootNodes.Contains(newTransform))
				{
					rootNodes.Add(newTransform);
				}
			}
			Debugger.advancedDebug("root nodes collected", advancedDebug);

			foreach (Transform t in rootNodes)
			{
				foreach (string s in Constants.baseSceneNodeNames)
				{
					if (string.Equals(t.name.ToLower(), s))
					{
						if (!doesListContain(scalingNodes, t))
						{
							Node newNode = new Node();
							newNode.transform = t;
							newNode.originalParent = t.parent;
							newNode.defaultScaling = t.localScale;
							scalingNodes.Add(newNode);
							Debugger.advancedDebug("found new scene node: " + t.name, advancedDebug);
						}
					}
				}
			}
			if (scalingNodes.Count < 1)
			{
				Debugger.advancedDebug("no scalable nodes found", advancedDebug);
				return;
			}
			Debugger.advancedDebug("base scaling nodes collected", advancedDebug);

			foreach (Node n in scalingNodes)
			{
				foreach (Transform t in getTransformChildsList(n.transform))
				{
						foreach (string s in Constants.nonScalingNodeNames)
						{
							if (string.Equals(t.name.ToLower(), s))
							{
								if (!doesListContain(nonScalingNodes, t))
								{
									Node newNode = new Node();
									newNode.transform = t;
									newNode.originalParent = t.parent;
									newNode.defaultScaling = t.localScale;
									nonScalingNodes.Add(newNode);
									Debugger.advancedDebug("found new scen node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z, advancedDebug);
								}
							}
					}
				}
			}
			Debugger.advancedDebug("nonscaling nodes collected", advancedDebug);
		}


		/// <summary>
		/// loads the settings from the settings file
		/// </summary>
		private static void getSettings()
		{
			StreamReader stream;
			try
			{
				Debugger.advancedDebug("Filename and Path: " + Constants.CompletePathAndFileName, true);
				stream = new StreamReader(Constants.CompletePathAndFileName);
			}
			catch
			{
				Debugger.advancedDebug("settings.txt not found in GameData\\FShangarExtender\\, using default settings.", true);
				return;
			}
			string newLine = string.Empty;
			int craftFileFormat = 0;

			// reads the configfile version
			newLine = readSetting(stream);
			int.TryParse(newLine, out craftFileFormat);
			// reads the configfile version

			// reads the used hotkey
			newLine = readSetting(stream);
			s_hotKey = newLine;
			Debugger.advancedDebug("Assigned hotkey: " + newLine, true);
			// reads the used hotkey

			try
			{
				if (craftFileFormat > 1)
				{
					newLine = readSetting(stream);
					SPHextentsAlwaysMax = bool.Parse(newLine);

					newLine = readSetting(stream);
					VABextentsAlwaysMax = bool.Parse(newLine);

					newLine = readSetting(stream);
					BuildingStartMaxSize = bool.Parse(newLine);
				}
				if (craftFileFormat > 2)
				{
					newLine = readSetting(stream);
					configCamMaxDistance = float.Parse(newLine);

					newLine = readSetting(stream);
					configMaxWorkArea = float.Parse(newLine);
				}
				if (craftFileFormat > 3)
				{
					newLine = readSetting(stream);
					scalingFactor = float.Parse(newLine) > Constants.defaultScaleFactor ? (float.Parse(newLine) < 1 ? 1 : float.Parse(newLine)) : float.Parse(newLine);
					Debugger.advancedDebug("Assigned scalingFactor: " + scalingFactor, true);

					newLine = readSetting(stream);
					hideHangars = newLine != "" ? bool.Parse(newLine) : false;
					Debugger.advancedDebug("Assigned hideHangars: " + hideHangars, true);

					newLine = readSetting(stream);
					advancedDebug = newLine != "" ? bool.Parse(newLine) : false;
					Debugger.advancedDebug("Assigned advancedDebug: " + advancedDebug, true);
				}
				else
				{
					Debugger.advancedDebug("settings file format mismatching, using default settings.", true);
				}
			}
			catch
			{
				Debugger.advancedDebug("Error parsing config values", true);
			}
		}


		/// <summary>
		/// reas single lines from the config file and returns them
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		private static string readSetting(StreamReader stream)
		{
			string newLine = string.Empty;
			try
			{
				while (newLine == string.Empty && !stream.EndOfStream)
				{
					newLine = stream.ReadLine();
					newLine = newLine.Trim(' ');
					if (newLine.Length > 1)
					{
						if (newLine.Substring(0, 2) == "//")
						{
							newLine = string.Empty;
						}
					}
				}
			}
			catch (Exception e)
			{
				Debugger.advancedDebug("stream reader error: " + e.ToString(), true);
			}
			return newLine;
		}


		/// <summary>
		/// method to check for pressed key
		/// </summary>
		/// <returns></returns>
		private static bool rescaleKeyPressed()
		{
			bool gotKeyPress = false;
			try
			{
				gotKeyPress = Input.GetKeyUp(s_hotKey);
			}
			catch
			{
				Debugger.advancedDebug("Invalid keycode. Resetting to numpad *", true);
				s_hotKey = Constants.defaultHotKey;
				gotKeyPress = false;
			}
			return gotKeyPress;
		}


	}
}