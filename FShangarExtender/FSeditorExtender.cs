using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP;
using System.IO;

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
class FSeditorExtender : MonoBehaviour
{    
    private bool SPHextentsAlwaysMax = true;
    private bool VABextentsAlwaysMax = true;
    private bool BuildingStartMaxSize = false;

    private bool SPHmaxed = false;
    private bool VABmaxed = false;

    public float configCamMaxDistance = 80f;
    public float configMaxWorkArea = 500f;

    private VABCamera cameraVAB;
    private float cameraVABHeightMin, cameraVABHeightMax = 300f;
    private float cameraVABDistanceMin, cameraVABDistanceMax = 1000f;

    private SPHCamera cameraSPH;
    private float cameraSPHHeightMin, cameraSPHHeightMax = 300f;
    private float cameraSPHDistanceMin, cameraSPHDistanceMax = 1000f;
    private float cameraSPHDisplaceXMin, cameraSPHDisplaceXMax = 1000f;
    private float cameraSPHDisplaceZMin, cameraSPHDisplaceZMax = 1000f;

    private SpaceNavigatorLocalCamera editorSpaceNavigator;
    private Vector3 editorSpaceNavigatorBoundsMin, editorSpaceNavigatorBoundsMax = new Vector3 (1000f, 200f, 1000f);

    private EditorLogic[] editorLogic;
    private Vector3 editorLogicBoundsMin, editorLogicBoundsMax = new Vector3 (1000f, 300f, 1000f);

    private GameObject sceneGeometryVAB;
    private GameObject sceneGeometrySPH;

    private string sceneGeometryNameVAB = "model_vab_parent_prefab";
    private string sceneGeometryNameSPH = "model_sph_parent_prefab";

    private string[] sceneGeometryNameVABStatics = new string[]{
        "model_vab_interior_occluder_v16",
        "model_vab_interior_props_v16",
        "model_vab_walls",
        "model_vab_windows"
    };
    private string sceneGeometryNameSPHStatic = "model_sph_interior_main_v16";

    private string sceneCrewNameVAB = "VABCrew";
    private string sceneCrewNameSPH = "SPHCrew";

    private float sceneScaleMinVAB = 0.9f;
    private float sceneScaleMinSPH = 0.8f;

    private float sceneScaleMaxVAB = 4f;
    private float sceneScaleMaxSPH = 4f;

    private Light[] sceneLights;

    private string hotKey = "[*]";

    public static string appPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");
    public static string pluginDataPath = appPath + "GameData/FShangarExtender/";
    public string settingsFile = pluginDataPath + "settings.txt";

    public void Awake ()
    {

    }

    public void Start ()
    {
        //fetchCameras();
        //fetchEditorLogic();
        //fetchScenes();
        //fetchCrew();
        //fetchLights();
        getSettings();
        applyConfigMaxSize();

        StartCoroutine(EditorBoundsFixer());

        //setEditorBounds();


        //SPHmaxed = BuildingStartMaxSize;
        //VABmaxed = BuildingStartMaxSize;

        //setScaleVAB(VABmaxed);
        //setScaleSPH(SPHmaxed);
        //setCamExtentsSPH(SPHextentsAlwaysMax || SPHmaxed);
        //setCamExtentsVAB(VABextentsAlwaysMax || VABmaxed);
    }

    private void applyConfigMaxSize()
    {
        //cameraVABHeightMin = 300f;
        //cameraVABHeightMax = 300f;
        cameraVABDistanceMin = 5f;
        cameraVABDistanceMax = configCamMaxDistance;
        //cameraSPHHeightMin = 300f;
        //cameraSPHHeightMax = 300f;
        cameraSPHDistanceMin = 5f;
        cameraSPHDistanceMax = configCamMaxDistance;
        cameraSPHDisplaceXMin = configMaxWorkArea;
        cameraSPHDisplaceXMax = configMaxWorkArea;
        cameraSPHDisplaceZMin = configMaxWorkArea;
        cameraSPHDisplaceZMax = configMaxWorkArea;

        editorSpaceNavigatorBoundsMin = new Vector3(configMaxWorkArea, 200f, configMaxWorkArea);
        editorSpaceNavigatorBoundsMax = new Vector3(configMaxWorkArea, 200f, configMaxWorkArea);

        editorLogicBoundsMin = new Vector3(configMaxWorkArea, 300f, configMaxWorkArea);
        editorLogicBoundsMax = new Vector3(configMaxWorkArea, 300f, configMaxWorkArea);
    }

    private IEnumerator<YieldInstruction> EditorBoundsFixer() // code taken from NathanKell, https://github.com/NathanKell/RealSolarSystem/blob/master/Source/CameraFixer.cs
    {
        Debug.Log("FSHangarExtender: Attempting work area scaling");
        while ((object)EditorBounds.Instance == null)
            yield return null;
        if ((object)(EditorBounds.Instance) != null)
        {
            EditorBounds.Instance.constructionBounds.extents = editorLogicBoundsMax;
            EditorBounds.Instance.cameraOffsetBounds.extents = editorLogicBoundsMax;
            EditorBounds.Instance.cameraMaxDistance = cameraSPHDistanceMax;
        }
        foreach (VABCamera c in Resources.FindObjectsOfTypeAll(typeof(VABCamera)))
        {
            c.maxHeight = cameraVABHeightMax;
            c.maxDistance = cameraVABDistanceMax;                
        }

        foreach (SPHCamera c in Resources.FindObjectsOfTypeAll(typeof(SPHCamera)))
        {
            c.maxHeight = cameraSPHHeightMax;
            c.maxDistance = cameraSPHDistanceMax;            
            c.maxDisplaceX = cameraSPHDisplaceXMax;
            c.maxDisplaceZ = cameraSPHDisplaceZMax;                
        }
        print("Editor camera set to " + EditorBounds.Instance.cameraMinDistance + "/" + EditorBounds.Instance.cameraMaxDistance
            + ", bounds " + EditorBounds.Instance.constructionBounds.ToString() + "/" + EditorBounds.Instance.cameraOffsetBounds.ToString());
    }
    
    /*
    private void toggleSize()
    {
        if (sceneGeometryVAB != null)
        {
            //Debug.Log("VAB scaling disabled. Models inaccessible. (This is for info only, known error)");            
            VABmaxed = !VABmaxed;
            setScaleVAB(VABmaxed);
        }
        else if (sceneGeometrySPH != null)
        {
            SPHmaxed = !SPHmaxed;
            setScaleSPH(SPHmaxed);
        }
    }

    private void fetchLights()
    {
        sceneLights = (Light[])FindObjectsOfType(typeof(Light));
    }

    private void fetchCrew()
    {
        GameObject sceneCrewHolder = new GameObject();
        sceneCrewHolder.transform.localScale = Vector3.one;
        sceneCrewHolder.transform.localPosition = Vector3.zero;

        GameObject sceneCrewVAB = GameObject.Find(sceneCrewNameVAB);
        if (sceneCrewVAB != null)
        {
            Debug.Log("FSeditorExtender: VAB crew found.");
            sceneCrewVAB.transform.parent = sceneCrewHolder.transform;
        }

        GameObject sceneCrewSPH = GameObject.Find(sceneCrewNameSPH);
        if (sceneCrewSPH != null)
        {
            Debug.Log("FSeditorExtender: SPH crew found.");
            sceneCrewSPH.transform.parent = sceneCrewHolder.transform;
        }
    }

    private void fetchScenes()
    {
        sceneGeometryVAB = GameObject.Find(sceneGeometryNameVAB);
        if (sceneGeometryVAB != null)
        {
            Debug.Log("FSeditorExtender: VAB geometry found.");
            sceneGeometryVAB.isStatic = false;
            for (int i = 0; i < sceneGeometryNameVABStatics.Length; ++i)
            {
                GameObject sceneGeometryVABBody = GameObject.Find(sceneGeometryNameVABStatics[i]);
                if (sceneGeometryVABBody != null)
                {
                    Debug.Log("FSeditorExtender: VAB geometry body found: " + sceneGeometryVABBody.name + " / " + sceneGeometryVABBody);
                    sceneGeometryVABBody.isStatic = false;

                    sceneGeometryVABBody.transform.parent = sceneGeometryVAB.transform;
                    sceneGeometryVABBody.transform.localScale = Vector3.one;
                }
            }
        }

        sceneGeometrySPH = GameObject.Find(sceneGeometryNameSPH);
        if (sceneGeometrySPH != null)
        {
            Debug.Log("FSeditorExtender: SPH geometry found.");
            sceneGeometrySPH.isStatic = false;
            GameObject sceneGeometrySPHBody = GameObject.Find(sceneGeometryNameSPHStatic);
            if (sceneGeometrySPHBody != null)
            {
                Debug.Log("FSeditorExtender: SPH geometry body found.");
                sceneGeometrySPHBody.isStatic = false;
                sceneGeometrySPHBody.transform.parent = sceneGeometrySPH.transform;
                sceneGeometrySPHBody.transform.localScale = Vector3.one;
            }
        }
    }

    private void fetchEditorLogic()
    {
        editorSpaceNavigator = Camera.main.GetComponent<SpaceNavigatorLocalCamera>();
        if (editorSpaceNavigator != null)
        {
            Debug.Log("FSeditorExtender: Found SpaceNavigator.");
            editorSpaceNavigatorBoundsMin = editorSpaceNavigator.bounds.extents;
        }

        editorLogic = (EditorLogic[])FindObjectsOfType(typeof(EditorLogic));
        if (editorLogic.Length > 0)
        {
            Debug.Log("FSeditorExtender: Found EditorLogic: " + editorLogic.Length);
            for (int i = 0; i < editorLogic.Length; i++)
            {
                editorLogicBoundsMin = editorLogic[i].editorBounds.extents;
            }
        }
    }

    private void fetchCameras()
    {
        cameraVAB = Camera.main.GetComponent<VABCamera>();
        if (cameraVAB != null)
        {
            Debug.Log("FSeditorExtender: Found the SPH camera.");
            cameraVABHeightMin = cameraVAB.maxHeight;
            cameraVABDistanceMin = cameraVAB.maxDistance;
        }

        cameraSPH = Camera.main.GetComponent<SPHCamera>();
        if (cameraSPH != null)
        {
            Debug.Log("FSeditorExtender: Found the SPH camera.");
            cameraSPHHeightMin = cameraSPH.maxHeight;
            cameraSPHDistanceMin = cameraSPH.maxDistance;
            cameraSPHDisplaceXMin = cameraSPH.maxDisplaceX;
            cameraSPHDisplaceZMin = cameraSPH.maxDisplaceZ;
        }
    }*/

    private void getSettings()
    {
        StreamReader stream;
        try
        {
            stream = new StreamReader(settingsFile);
        }
        catch
        {
            Debug.Log("FSeditorExtender: settings.txt not found in GameData\\FShangarExtender\\, using default hotkey numpad *");
            return;
        }
        string newLine = string.Empty;
        int craftFileFormat = 0;

        newLine = readSetting(stream);
        int.TryParse(newLine, out craftFileFormat);

        newLine = readSetting(stream);
        hotKey = newLine;

        Debug.Log("FSeditorExtender: Assigned hangar scale hotkey: " + newLine);

        if (craftFileFormat >= 3)
        {
            try
            {
                newLine = readSetting(stream);
                SPHextentsAlwaysMax = bool.Parse(newLine);
                newLine = readSetting(stream);
                VABextentsAlwaysMax = bool.Parse(newLine);
                newLine = readSetting(stream);
                BuildingStartMaxSize = bool.Parse(newLine);
                newLine = readSetting(stream);
                configCamMaxDistance = float.Parse(newLine);
                newLine = readSetting(stream);
                configMaxWorkArea = float.Parse(newLine);
            }
            catch
            {
                Debug.Log("FSeditorExtender: Error parsing config values");
            }
        }
        else if (craftFileFormat == 2)
        {
            try
            {
                newLine = readSetting(stream);
                SPHextentsAlwaysMax = bool.Parse(newLine);
                newLine = readSetting(stream);
                VABextentsAlwaysMax = bool.Parse(newLine);
                newLine = readSetting(stream);
                BuildingStartMaxSize = bool.Parse(newLine);
            }
            catch
            {
                Debug.Log("FSeditorExtender: Error parsing config bools");
            }
        }
        else
        {
            Debug.Log("FSeditorExtender: Old settings file format, using default Extent and Scale settings.");
        }
    }

    private string readSetting (StreamReader stream)
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
            Debug.Log("FSeditorExtender: stream reader error: " + e.ToString());
        }
        return newLine;
    }
     

    public void Update ()
    {
        bool gotKeyPress = rescaleKeyPressed();
        if (gotKeyPress)
        {
            Debug.Log("FSeditorExtender: Scaling Hangar Geometry");
            //toggleSize();
            StartCoroutine(EditorBoundsFixer());
        }
    }

    /*
    private void toggleEditorExtents(int i)
    {
        if (editorLogic[i].editorBounds.extents == editorLogicBoundsMin) setEditorExtents(editorLogic[i], editorLogicBoundsMax);
        else setEditorExtents(editorLogic[i], editorLogicBoundsMin);
    }

    private void toggleSpaceNavExtents()
    {
        if (editorSpaceNavigator != null)
        {
            if (editorSpaceNavigator.bounds.extents == editorSpaceNavigatorBoundsMin)
            {
                setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMax);
            }
            else
            {
                setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMin);
            }
        }
    }

    private void setEditorExtents(EditorLogic editorLogic, Vector3 newSize)
    {
        EditorBounds.Instance.constructionBounds.SetMinMax(newSize, newSize);
    }

    private void setSpaceNavExtents(SpaceNavigatorLocalCamera cam, Vector3 newSize)
    {
        if (cam != null)
            cam.bounds.extents = newSize;
    }*/

    private bool rescaleKeyPressed()
    {
        bool gotKeyPress = false;
        try
        {
            gotKeyPress = Input.GetKeyDown(hotKey);
        }
        catch
        {
            Debug.Log("FShangarExtender: Invalid keycode. Resetting to numpad *");
            hotKey = "[*]";
            gotKeyPress = false;
        }
        return gotKeyPress;
    }

    /*
    private void setScaleSPH(bool max)
    {
        if (max) //sceneGeometrySPH.transform.localScale.x == sceneScaleMinSPH
        {
            RenderSettings.fogStartDistance *= sceneScaleMaxSPH / sceneScaleMinSPH;
            RenderSettings.fogEndDistance *= sceneScaleMaxSPH / sceneScaleMinSPH;

            //sceneGeometrySPH.transform.localScale = Vector3.one * sceneScaleMaxSPH;
            setCamExtentsSPH(true);
            //if (sceneLights.Length > 0)
            //{
            //    for (int i = 0; i < sceneLights.Length; ++i)
            //    {
            //        if (sceneLights[i].type == LightType.Spot) sceneLights[i].range *= sceneScaleMaxSPH / sceneScaleMinSPH;
            //    }
            //}
        }
        else
        {
            RenderSettings.fogStartDistance /= sceneScaleMaxSPH / sceneScaleMinSPH;
            RenderSettings.fogEndDistance /= sceneScaleMaxSPH / sceneScaleMinSPH;

            //sceneGeometrySPH.transform.localScale = Vector3.one * sceneScaleMinSPH;
            if (!SPHextentsAlwaysMax) setCamExtentsSPH(false);

            //if (sceneLights.Length > 0)
            //{
            //    for (int i = 0; i < sceneLights.Length; ++i)
            //    {
            //        if (sceneLights[i].type == LightType.Spot) sceneLights[i].range /= sceneScaleMaxSPH / sceneScaleMinSPH;
            //    }
            //}
        }
        //sceneGeometrySPH.transform.localPosition = new Vector3(0f, -0.2f * (sceneGeometrySPH.transform.localScale.x - sceneScaleMinSPH), 0f);
    }



    private void setScaleVAB(bool max)
    {
        if (sceneGeometryVAB != null)
        {
            if (max) //sceneGeometryVAB.transform.localScale.x == sceneScaleMinVAB
            {
                sceneGeometryVAB.transform.localScale = Vector3.one * sceneScaleMaxVAB;
                setCamExtentsVAB(true);
                if (sceneLights.Length > 0)
                {
                    for (int i = 0; i < sceneLights.Length; ++i)
                    {
                        if (sceneLights[i].type == LightType.Spot) sceneLights[i].range *= sceneScaleMaxVAB / sceneScaleMinVAB;
                    }
                }
            }
            else
            {
                sceneGeometryVAB.transform.localScale = Vector3.one * sceneScaleMinVAB;
                if (!VABextentsAlwaysMax) setCamExtentsVAB(false);

                if (sceneLights.Length > 0)
                {
                    for (int i = 0; i < sceneLights.Length; ++i)
                    {
                        if (sceneLights[i].type == LightType.Spot) sceneLights[i].range /= sceneScaleMaxVAB / sceneScaleMinVAB;
                    }
                }
            }
        }
    }

    private void setCamExtentsSPH(bool max)
    {
        if (sceneGeometrySPH != null)
        {
            if (max)
            {
                setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMax);
                cameraSPH.maxHeight = cameraSPHHeightMax;
                cameraSPH.maxDistance = cameraSPHDistanceMax;
                cameraSPH.maxDisplaceX = cameraSPHDisplaceXMax;
                cameraSPH.maxDisplaceZ = cameraSPHDisplaceZMax;
            }
            else
            {
                setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMin);
                cameraSPH.maxHeight = cameraSPHHeightMin;
                cameraSPH.maxDistance = cameraSPHDistanceMin;
                cameraSPH.maxDisplaceX = cameraSPHDisplaceXMin;
                cameraSPH.maxDisplaceZ = cameraSPHDisplaceZMin;
            }
            for (int i = 0; i < editorLogic.Length; i++)
            {
                if (max)
                {
                    setEditorExtents(editorLogic[i], editorLogicBoundsMax);
                }
                else
                {
                    setEditorExtents(editorLogic[i], editorLogicBoundsMin);
                }
            }
        }
    }

    private void setCamExtentsVAB(bool max)
    {
        if (max)
        {
            setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMax);
            cameraVAB.maxHeight = cameraVABHeightMax;
            cameraVAB.maxDistance = cameraVABDistanceMax;
        }
        else
        {
            setSpaceNavExtents(editorSpaceNavigator, editorSpaceNavigatorBoundsMin);
            cameraVAB.maxHeight = cameraVABHeightMin;
            cameraVAB.maxDistance = cameraVABDistanceMin;
        }
        for (int i = 0; i < editorLogic.Length; i++)
        {
            if (max)
            {
                setEditorExtents(editorLogic[i], editorLogicBoundsMax);
            }
            else
            {
                setEditorExtents(editorLogic[i], editorLogicBoundsMin);
            }
        }
    }*/
}