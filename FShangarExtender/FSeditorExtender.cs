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



		private List<VABCamera> _vabCameras = new List<VABCamera>();
		private List<SPHCamera> _sphCameras = new List<SPHCamera>();
		private List<Light> _sceneLights = new List<Light>();
		private List<Node> _sceneNodes = new List<Node>();
		private List<Node> _hangarNodes = new List<Node>();
		private List<Node> _nonScalingNodes = new List<Node>();
		private Transform _tempParent;
		private Vector3 _originalConstructionBoundExtends;
		private Vector3 _originalCameraOffsetBoundExtends;
		private bool _sceneScaled = false;
		private static string _hotKey = Constants.defaultHotKey;
		private static float _scalingFactor = Constants.defaultScaleFactor;
		private static bool _hideHangars = false;
		private static bool _advancedDebug = false;
		private bool _hangarExtenderReady = false;
		private ApplicationLauncherButton _toolbarButton;
		private static Texture2D _shrinkIcon;
		private static Texture2D _extendIcon;



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
			if (_sceneScaled)
			{
				StartCoroutine(toggleScaling());
			}
			resetMod();
			StartCoroutine(initFSHangarExtender());
			loadToolbarButton();
		}


		/// <summary>
		/// default mono update method which is called once each frame
		/// </summary>
		public void Update()
		{
			if (_hangarExtenderReady)
			{
				bool gotKeyPress = rescaleKeyPressed();
				if (gotKeyPress)
				{
					Debugger.advancedDebug("Scaling Hangar Geometry", _advancedDebug);
					StartCoroutine(toggleScaling());
				}
			}
		}


		/// <summary>
		/// default mono OnDestroy when the object gets removed from scene
		/// </summary>
		public void OnDestroy()
		{
			if (_sceneScaled)
			{
				StartCoroutine(toggleScaling());
			}
			resetMod();
			Debugger.advancedDebug("cleared the lists", _advancedDebug);
		}


		public void loadToolbarButton()
		{
			Debugger.advancedDebug("Applauncher loading up", true);
			if (_extendIcon == null)
			{
				_extendIcon = GameDatabase.Instance.GetTexture(Constants.extentIconFileName, false);
				Debugger.advancedDebug("Applauncher icon 1 found", true);
			}
			if (_shrinkIcon == null)
			{
				_shrinkIcon = GameDatabase.Instance.GetTexture(Constants.shrinkIconFileName, false);
				Debugger.advancedDebug("Applauncher icon 2 found", true);
			}
			if (_toolbarButton == null)
			{
				_toolbarButton = ApplicationLauncher.Instance.AddModApplication(() => StartCoroutine(toggleScaling()), () => StartCoroutine(toggleScaling()), null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, _extendIcon);
				Debugger.advancedDebug("Applauncher loading complete", true);
			}
		}


		/// <summary>
		/// method to completly reset the whole mod
		/// </summary>
		private void resetMod()
		{
			if (_hideHangars)
			{
				foreach (Node n in _hangarNodes)
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

			_sceneNodes.Clear();
			_hangarNodes.Clear();
			_nonScalingNodes.Clear();
			_sceneLights.Clear();
			_vabCameras.Clear();
			_sphCameras.Clear();
			_sceneScaled = false;
			_hangarExtenderReady = false;

			if (_toolbarButton != null)
			{
				ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
			}
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
				_hangarExtenderReady = false;
				yield return null;
			}
			while (_hangarNodes.Count <1 || _sceneNodes.Count < 1)
			{
				fetchSceneNodes();
				yield return null;
			}
			_originalConstructionBoundExtends = EditorBounds.Instance.constructionBounds.extents * 2;
			_originalCameraOffsetBoundExtends = EditorBounds.Instance.cameraOffsetBounds.extents * 2;
			EditorBounds.Instance.cameraMinDistance /= _scalingFactor;
			fetchCameras();
			fetchLights();
			listNodes();
			_hangarExtenderReady = true;
			_sceneScaled = false;
			Debugger.advancedDebug("Attempting to init successful", true);
			Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, _advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", _advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", _advancedDebug);
		}


		/// <summary>
		/// method to update the camera bounds and scale the scene
		/// </summary>
		/// <returns></returns>
		private IEnumerator<YieldInstruction> toggleScaling() // code taken from NathanKell, https://github.com/NathanKell/RealSolarSystem/blob/master/Source/CameraFixer.cs
		{
			Debugger.advancedDebug("Attempting work area scaling", _advancedDebug);
			while ((object)EditorBounds.Instance == null)
			{
				yield return null;
			}
			if ((object)(EditorBounds.Instance) != null)
			{
				if (_sceneScaled)
				{
					Debugger.advancedDebug("shrink scene", _advancedDebug);

					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (_originalConstructionBoundExtends));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (_originalCameraOffsetBoundExtends));
					EditorBounds.Instance.cameraMaxDistance /= _scalingFactor;
					Debugger.advancedDebug("Bounds scaled", _advancedDebug);

					foreach (VABCamera c in _vabCameras)
					{
						c.maxHeight /= _scalingFactor;
						c.maxDistance /= _scalingFactor;
						c.camera.farClipPlane /= _scalingFactor;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] /= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("vabCameras scaled", _advancedDebug);
					foreach (SPHCamera c in _sphCameras)
					{
						c.maxHeight /= _scalingFactor;
						c.maxDistance /= _scalingFactor;
						c.maxDisplaceX /= _scalingFactor;
						c.maxDisplaceZ /= _scalingFactor;
						c.camera.farClipPlane /= _scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] /= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("sphCameras scaled", _advancedDebug);
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane /= _scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] /= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("CameraManager scaled", _advancedDebug);
					RenderSettings.fogStartDistance /= _scalingFactor;
					RenderSettings.fogEndDistance /= _scalingFactor;

					Debugger.advancedDebug("show Hangars", _advancedDebug);
					if (_hangarNodes != null && _hangarNodes.Count > 0)
					{
						foreach (Node n in _hangarNodes)
						{
							n.transform.localScale = n.defaultScaling;
						}
					}
					if (_sceneNodes != null && _sceneNodes.Count > 0)
					{
						foreach (Node n in _sceneNodes)
						{
							n.transform.localScale = n.defaultScaling;
						}
					}

					if (_nonScalingNodes != null && _nonScalingNodes.Count > 0)
					{
						foreach (Node n in _nonScalingNodes)
						{
							n.transform.parent = n.originalParent;
							n.transform.localScale = n.defaultScaling;
						}
					}
					Debugger.advancedDebug("scaling lights", _advancedDebug);
					if (_sceneLights != null && _sceneLights.Count > 0)
					{
						foreach (Light l in _sceneLights)
						{
							if (l.type == LightType.Spot)
							{
								l.range /= _scalingFactor;
							}
						}
					}

					if (_hideHangars)
					{
						Debugger.advancedDebug("hide Hangars", _advancedDebug);
						if (_hangarNodes != null && _hangarNodes.Count > 0)
						{
							foreach (Node n in _hangarNodes)
							{
								List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
								n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
								foreach (SkinnedMeshRenderer r in skinRenderers)
								{
									if (!Constants.baseHangarVisibleNames.Contains(r.gameObject.name))
									{
										r.enabled = true;
									}
								}
								List<MeshRenderer> renderers = new List<MeshRenderer>();
								n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
								foreach (MeshRenderer r in renderers)
								{
									if (!Constants.baseHangarVisibleNames.Contains(r.gameObject.name))
									{
										r.enabled = true;
									}
								}
							}
						}
						Debugger.advancedDebug("hide Hangars complete", _advancedDebug);
					}

					if (_toolbarButton != null && _extendIcon != null)
					{
						_toolbarButton.SetTexture(_extendIcon);
					}

					Debugger.advancedDebug("shrink scene complete", _advancedDebug);
				}
				else
				{
					Debugger.advancedDebug("extend scene", _advancedDebug);

					EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (_originalConstructionBoundExtends * _scalingFactor));
					EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (_originalCameraOffsetBoundExtends * _scalingFactor));
					EditorBounds.Instance.cameraMaxDistance *= _scalingFactor;
					Debugger.advancedDebug("Bounds scaled", _advancedDebug);

					foreach (VABCamera c in _vabCameras)
					{
						c.maxHeight *= _scalingFactor;
						c.maxDistance *= _scalingFactor;
						c.camera.farClipPlane *= _scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] *= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("vabCameras scaled", _advancedDebug);
					foreach (SPHCamera c in _sphCameras)
					{
						c.maxHeight *= _scalingFactor;
						c.maxDistance *= _scalingFactor;
						c.maxDisplaceX *= _scalingFactor;
						c.maxDisplaceZ *= _scalingFactor;
						c.camera.farClipPlane *= _scalingFactor * 2;
						for (int i = 0; i < c.camera.layerCullDistances.Length; i++)
						{
							c.camera.layerCullDistances[i] *= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("sphCameras scaled", _advancedDebug);
					if (CameraManager.Instance != null)
					{
						CameraManager.Instance.camera.farClipPlane *= _scalingFactor * 2;
						for (int i = 0; i < CameraManager.Instance.camera.layerCullDistances.Length; i++)
						{
							CameraManager.Instance.camera.layerCullDistances[i] *= _scalingFactor * 2;
						}
					}
					Debugger.advancedDebug("CameraManager scaled", _advancedDebug);
					RenderSettings.fogStartDistance *= _scalingFactor;
					RenderSettings.fogEndDistance *= _scalingFactor;

					if (_hideHangars)
					{
						Debugger.advancedDebug("hide Hangars", _advancedDebug);
						if (_hangarNodes != null && _hangarNodes.Count > 0)
						{
							foreach (Node n in _hangarNodes)
							{
								List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
								n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
								foreach (SkinnedMeshRenderer r in skinRenderers)
								{
									if (!Constants.baseHangarVisibleNames.Contains(r.gameObject.name))
									{
										r.enabled = false;
									}
								}
								List<MeshRenderer> renderers = new List<MeshRenderer>();
								n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
								foreach (MeshRenderer r in renderers)
								{
									if (!Constants.baseHangarVisibleNames.Contains(r.gameObject.name))
									{
										r.enabled = false;
									}
								}
							}
						}
						Debugger.advancedDebug("hide Hangars complete", _advancedDebug);
					}

					Debugger.advancedDebug("show Hangars", _advancedDebug);
					if (_nonScalingNodes != null && _nonScalingNodes.Count > 0)
					{
						foreach (Node n in _nonScalingNodes)
						{
							n.transform.parent = _tempParent;
							n.transform.localScale = n.defaultScaling;
						}
					}
					if (_hangarNodes != null && _hangarNodes.Count > 0)
					{
						foreach (Node n in _hangarNodes)
						{
							n.transform.localScale = n.defaultScaling * _scalingFactor;
						}
					}
					if (_sceneNodes != null && _sceneNodes.Count > 0)
					{
						foreach (Node n in _sceneNodes)
						{
							n.transform.localScale = n.defaultScaling * _scalingFactor;
						}
					}

					Debugger.advancedDebug("scaling lights", _advancedDebug);
					if (_sceneLights != null && _sceneLights.Count > 0)
					{
						foreach (Light l in _sceneLights)
						{
							if (l.type == LightType.Spot)
							{
								l.range *= _scalingFactor;
							}
						}
					}

					if (_toolbarButton != null && _shrinkIcon != null)
					{
						_toolbarButton.SetTexture(_shrinkIcon);
					}

					Debugger.advancedDebug("extend scene complete", _advancedDebug);
				}
				_sceneScaled = !_sceneScaled;
			}
			Debugger.advancedDebug("Attempting work area scaling complete", _advancedDebug);
			Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, _advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", _advancedDebug);
			Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", _advancedDebug);
		}


		/// <summary>
		/// method to fetch the cameras in the scene and assigns the zoom limits to it
		/// </summary>
		private void fetchCameras()
		{
			_vabCameras = ((VABCamera[])Resources.FindObjectsOfTypeAll(typeof(VABCamera))).ToList();
			_sphCameras = ((SPHCamera[])Resources.FindObjectsOfTypeAll(typeof(SPHCamera))).ToList();
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
					Debugger.advancedDebug("RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf, _advancedDebug);
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
						Debugger.advancedDebug("RootTransform = " + t.name + " | isActive = " + t.gameObject.activeSelf, _advancedDebug);

						List<Transform> childs = getTransformChildsList(t);
						foreach (Transform tx in childs)
						{
							Debugger.advancedDebug("ChildTransform = " + tx.name + " | Parent = " + tx.parent.name + " | isActive = " + tx.gameObject.activeSelf, _advancedDebug);
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
			_sceneLights = ((Light[])FindObjectsOfType(typeof(Light))).ToList();
			foreach (Light l in _sceneLights)
			{
				Debugger.advancedDebug("Light = " + l.name + " - Type = " + l.type + " - Intensity = " + l.intensity, _advancedDebug);
			}
		}


		/// <summary>
		/// method that will read the transforms from the scene
		/// </summary>
		private void fetchSceneNodes()
		{
			Debugger.advancedDebug("fetchSceneNodes Nodes found = " + UnityEngine.Object.FindObjectsOfType<Transform>().Length, _advancedDebug);
			List<Transform> rootNodes = new List<Transform>();
			_hangarNodes.Clear();
			_sceneNodes.Clear();
			_nonScalingNodes.Clear();
			GameObject temp = new GameObject();
			_tempParent = temp.transform;
			_tempParent.position = Vector3.zero;
			_tempParent.localScale = Vector3.one;
			_tempParent.name = Constants.defaultTempParentName;

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
			Debugger.advancedDebug("root nodes collected", _advancedDebug);

			foreach (Transform t in rootNodes)
			{
				foreach (string s in Constants.baseHangarNames)
				{
					if (string.Equals(t.name.ToLower(), s))
					{
						if (!doesListContain(_hangarNodes, t))
						{
							Node newNode = new Node();
							newNode.transform = t;
							newNode.originalParent = t.parent;
							newNode.defaultScaling = t.localScale;
							_hangarNodes.Add(newNode);
							Debugger.advancedDebug("found new scene node: " + t.name, _advancedDebug);
							break;
						}
					}
				}
				foreach (string s in Constants.baseSceneNames)
				{
					if (string.Equals(t.name.ToLower(), s))
					{
						if (!doesListContain(_sceneNodes, t))
						{
							Node newNode = new Node();
							newNode.transform = t;
							newNode.originalParent = t.parent;
							newNode.defaultScaling = t.localScale;
							_sceneNodes.Add(newNode);
							Debugger.advancedDebug("found new scene node: " + t.name, _advancedDebug);
							break;
						}
					}
				}
			}
			if (_hangarNodes.Count < 1 || _sceneNodes.Count < 1)
			{
				Debugger.advancedDebug("no scalable nodes found", _advancedDebug);
				return;
			}
			Debugger.advancedDebug("base scaling nodes collected", _advancedDebug);

			foreach (Node n in _hangarNodes)
			{
				foreach (Transform t in getTransformChildsList(n.transform))
				{
					foreach (string s in Constants.nonScalingNodeNames)
					{
						if (string.Equals(t.name.ToLower(), s))
						{
							if (!doesListContain(_nonScalingNodes, t))
							{
								Node newNode = new Node();
								newNode.transform = t;
								newNode.originalParent = t.parent;
								newNode.defaultScaling = t.localScale;
								_nonScalingNodes.Add(newNode);
								Debugger.advancedDebug("found new scen node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z, _advancedDebug);
								break;
							}
						}
					}
				}
			}
			foreach (Node n in _sceneNodes)
			{
				foreach (Transform t in getTransformChildsList(n.transform))
				{
					foreach (string s in Constants.nonScalingNodeNames)
					{
						if (string.Equals(t.name.ToLower(), s))
						{
							if (!doesListContain(_nonScalingNodes, t))
							{
								Node newNode = new Node();
								newNode.transform = t;
								newNode.originalParent = t.parent;
								newNode.defaultScaling = t.localScale;
								_nonScalingNodes.Add(newNode);
								Debugger.advancedDebug("found new scen node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z, _advancedDebug);
								break;
							}
						}
					}
				}
			}
			Debugger.advancedDebug("nonscaling nodes collected", _advancedDebug);
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
			_hotKey = newLine;
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
					_scalingFactor = float.Parse(newLine) > Constants.defaultScaleFactor ? (float.Parse(newLine) < 1 ? 1 : float.Parse(newLine)) : float.Parse(newLine);
					Debugger.advancedDebug("Assigned scalingFactor: " + _scalingFactor, true);

					newLine = readSetting(stream);
					_hideHangars = newLine != "" ? bool.Parse(newLine) : false;
					Debugger.advancedDebug("Assigned hideHangars: " + _hideHangars, true);

					newLine = readSetting(stream);
					_advancedDebug = newLine != "" ? bool.Parse(newLine) : false;
					Debugger.advancedDebug("Assigned advancedDebug: " + _advancedDebug, true);
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
				gotKeyPress = Input.GetKeyUp(_hotKey);
			}
			catch
			{
				Debugger.advancedDebug("Invalid keycode. Resetting to numpad *", true);
				_hotKey = Constants.defaultHotKey;
				gotKeyPress = false;
			}
			return gotKeyPress;
		}


	}
}