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



		private static List<VABCamera> vabCameras = new List<VABCamera>();
		private static List<SPHCamera> sphCameras = new List<SPHCamera>();
		private static List<Light> sceneLights = new List<Light>();
		private static List<Node> scalingNodes = new List<Node>();
		private static List<Node> nonScalingNodes = new List<Node>();
		private static Transform tempParent;
		private static bool sceneScaled = false;
		private static string s_hotKey = Constants.defaultHotKey;
		private static float scalingFactor = Constants.defaultScaleFactor;
		private static bool hangarExtenderReady = false;



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
			getSettings();
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
					Debug.Log("[FSHangarExtender] - Scaling Hangar Geometry");
					StartCoroutine(EditorBoundsFixer());
					toggleScaling();
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
				RenderSettings.fogStartDistance /= scalingFactor;
				RenderSettings.fogEndDistance /= scalingFactor;
				Camera.main.farClipPlane /= scalingFactor;
			}
			scalingNodes.Clear();
			nonScalingNodes.Clear();
			sceneLights.Clear();
			vabCameras.Clear();
			sphCameras.Clear();
			sceneScaled = false;
			hangarExtenderReady = false;
			Debug.Log("[FSHangarExtender] - cleared the lists");
		}


		/// <summary>
		/// method to initalize the whole Extender
		/// </summary>
		/// <returns></returns>
		private static IEnumerator<YieldInstruction> initFSHangarExtender()
		{
			Debug.Log("[FSHangarExtender] - Attempting to init");
			while ((object)EditorBounds.Instance == null && HighLogic.LoadedScene == GameScenes.EDITOR)
			{
				yield return null;
			}
			while (scalingNodes.Count < 1)
			{
				fetchSceneNodes();
				yield return null;
			}
			fetchCameras();
			fetchLights();
			listNodes();
			hangarExtenderReady = true;
			Debug.Log("[FSHangarExtender] - Attempting to init successful");
		}


		/// <summary>
		/// method to update the camera bounds on the scene
		/// </summary>
		/// <returns></returns>
		private static IEnumerator<YieldInstruction> EditorBoundsFixer() // code taken from NathanKell, https://github.com/NathanKell/RealSolarSystem/blob/master/Source/CameraFixer.cs
		{
			Debug.Log("[FSHangarExtender] - Attempting work area scaling");
			while ((object)EditorBounds.Instance == null)
			{
				yield return null;
			}
			if ((object)(EditorBounds.Instance) != null)
			{
				if (sceneScaled)
				{
					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (EditorBounds.Instance.constructionBounds.extents / scalingFactor));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (EditorBounds.Instance.cameraOffsetBounds.extents / scalingFactor));
					EditorBounds.Instance.cameraMaxDistance /= scalingFactor;
					//Debug.Log("[FSHangarExtender] - Bounds scaled");
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
					//Debug.Log("[FSHangarExtender] - vabCameras scaled");
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
					//Debug.Log("[FSHangarExtender] - sphCameras scaled");
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane /= scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] /= scalingFactor * 2;
						}
					}
					//Debug.Log("[FSHangarExtender] - CameraManager scaled");
					RenderSettings.fogStartDistance /= scalingFactor;
					RenderSettings.fogEndDistance /= scalingFactor;
				}
				else
				{
					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (EditorBounds.Instance.constructionBounds.extents * scalingFactor));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (EditorBounds.Instance.cameraOffsetBounds.extents * scalingFactor));
					EditorBounds.Instance.cameraMaxDistance *= scalingFactor;
					//Debug.Log("[FSHangarExtender] - Bounds scaled");
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
					//Debug.Log("[FSHangarExtender] - vabCameras scaled");
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
					//Debug.Log("[FSHangarExtender] - sphCameras scaled");
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane *= scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] *= scalingFactor * 2;
						}
					}
					//Debug.Log("[FSHangarExtender] - CameraManager scaled");
					RenderSettings.fogStartDistance *= scalingFactor;
					RenderSettings.fogEndDistance *= scalingFactor;
				}
			}
			Debug.Log("[FSHangarExtender] - Attempting work area scaling complete");
			//print("[FSHangarExtender] - Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance);
			//print("[FSHangarExtender] - EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = " + EditorBounds.Instance.constructionBounds.extents);
			//print("[FSHangarExtender] - EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = " + EditorBounds.Instance.constructionBounds.extents);
		}


		/// <summary>
		/// method to fetch the cameras in the scene and assigns the zoom limits to it
		/// </summary>
		private static void fetchCameras()
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
					Debug.Log("[FSHangarExtender] - RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf);
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
						Debug.Log("[FSHangarExtender] - RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf);

						List<Transform> childs = getTransformChildsList(t);
						foreach (Transform tx in childs)
						{
							Debug.Log("[FSHangarExtender] - ChildTransform = " + tx.name + " | Parent = " + tx.parent.name + " | isActive = " + tx.gameObject.activeSelf);
						}

					}
				}
			}
		}


		/// <summary>
		/// collects all Lights in the scene.
		/// </summary>
		private static void fetchLights()
		{
			sceneLights = ((Light[])FindObjectsOfType(typeof(Light))).ToList();
		}


		/// <summary>
		/// method that will read the transforms from the scene
		/// </summary>
		private static void fetchSceneNodes()
		{
			Debug.Log("[FSHangarExtender] - fetchSceneNodes Nodes found = " + UnityEngine.Object.FindObjectsOfType<Transform>().Length);
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
			Debug.Log("[FSHangarExtender] - root nodes collected");

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
							Debug.Log("[FSHangarExtender] - found new scene node: " + t.name);
						}
					}
				}
			}
			if (scalingNodes.Count < 1)
			{
				Debug.Log("[FSHangarExtender] - no scalable nodes found");
				return;
			}
			Debug.Log("[FSHangarExtender] - base scaling nodes collected");

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
									Debug.Log("[FSHangarExtender] - found new scen node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z);
								}
							}
					}
				}
			}
			Debug.Log("[FSHangarExtender] - nonscaling nodes collected");
		}


		/// <summary>
		/// the actual method where the things are scaled and set into new relations
		/// </summary>
		private void toggleScaling()
		{
			if (sceneScaled)
			{
				Debug.Log("[FSHangarExtender] - shrink scene");
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
				if (sceneLights != null && sceneLights.Count > 0)
				{
					foreach (Light l in sceneLights)
					{
						if (l.type == LightType.Spot)
						{
							l.range /= scalingFactor;
						}
					}
				}
				Debug.Log("[FSHangarExtender] - shrink scene complete");
			}
			else
			{
				Debug.Log("[FSHangarExtender] - rise scene");
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
				if (sceneLights != null && sceneLights.Count > 0)
				{
					foreach (Light l in sceneLights)
					{
						if (l.type == LightType.Spot)
						{
							l.range *= scalingFactor;
						}
					}
				}
				Debug.Log("[FSHangarExtender] - shrink scene complete");
			}
			sceneScaled = !sceneScaled;
		}


		/// <summary>
		/// loads the settings from the settings file
		/// </summary>
		private static void getSettings()
		{
			StreamReader stream;
			try
			{
				Debug.Log("[FSHangarExtender] - Filename and Path: " + Constants.CompletePathAndFileName);
				stream = new StreamReader(Constants.CompletePathAndFileName);
			}
			catch
			{
				Debug.Log("[FSHangarExtender] - settings.txt not found in GameData\\FShangarExtender\\, using default settings.");
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
			Debug.Log("[FSHangarExtender] - Assigned hotkey: " + newLine);
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
					scalingFactor = float.Parse(newLine) > 10 ? float.Parse(newLine) < 1 ? 1 : float.Parse(newLine) : float.Parse(newLine);
				}
				else
				{
					Debug.Log("[FSHangarExtender] - settings file format mismatching, using default settings.");
				}
			}
			catch
			{
				Debug.Log("[FSHangarExtender] - Error parsing config values");
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
				Debug.Log("[FSHangarExtender] - stream reader error: " + e.ToString());
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
				Debug.Log("[FSHangarExtender] - Invalid keycode. Resetting to numpad *");
				s_hotKey = Constants.defaultHotKey;
				gotKeyPress = false;
			}
			return gotKeyPress;
		}


	}
}