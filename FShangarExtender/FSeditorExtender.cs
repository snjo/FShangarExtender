using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;
using System.IO;

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
class FSeditorExtender : MonoBehaviour
{
    public SPHCamera SPHcam;
    public VABCamera VABcam;
    public GameObject geo;
    //private KeyCode hotKey = KeyCode.KeypadMultiply;
    private string hotKey = "[*]";

    public static string AppPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");
    public static string PlugInDataPath = AppPath + "GameData/FShangarExtender/";
    public string settingsFile = PlugInDataPath + "settings.txt";
    public float currentScale = 1f;

    public void Start()
    {
        Debug.Log("FSeditorExtender: altering sph camera extents");
        SPHcam = Camera.main.GetComponent<SPHCamera>();        
        SPHcam.maxHeight = 300f;        
        SPHcam.maxDistance = 1000f;        
        SPHcam.maxDisplaceX = 1000f;        
        SPHcam.maxDisplaceZ = 1000f;        
        //SPHcam.scrollHeight = 300f;

        Debug.Log("FSeditorExtender: altering vab camera extents");

        VABcam = Camera.main.GetComponent<VABCamera>();        
        VABcam.maxHeight = 300f;        
        VABcam.maxDistance = 1000f;        
        //VABcam.scrollHeight = 300f;

        SpaceNavigatorLocalCamera spaceNav = Camera.main.GetComponent<SpaceNavigatorLocalCamera>();
        spaceNav.bounds.extents = new Vector3(1000f, 200f, 1000f);

        Debug.Log("FSeditorExtender: altering editorLogic part extents");

        EditorLogic[] elogic = (EditorLogic[])FindObjectsOfType(typeof(EditorLogic));
        Debug.Log("Found EditorLogic: " + elogic.Length);
        for (int i = 0; i < elogic.Length; i++)
        {
            Debug.Log(elogic[i].gameObject.name);
            Debug.Log("FSeditorExtender: Previous hangar Bounds" + elogic[i].editorBounds.extents);
            elogic[i].editorBounds.extents = new Vector3(1000f, 300f, 1000f);
            Debug.Log("FSeditorExtender: New hangar Bounds" + elogic[i].editorBounds.extents);
        }
        
        geo = GameObject.Find("SPH_Interior_Geometry");
        //if (geo == null)
        //{
        //    //geo = GameObject.Find("VAB_Interior_Geometry");            
        //    if (geo != null) Debug.Log("Found VAB geo");
        //    else Debug.Log("did not find VAB geo");
        //}

        getHotKey();
    }

    private void getHotKey()
    {
        StreamReader stream = new StreamReader(settingsFile);
        string newLine = string.Empty;
        int craftFileFormat = 0;

        newLine = readSetting(stream);        
        int.TryParse(newLine, out craftFileFormat);

        newLine = readSetting(stream);

        hotKey = newLine;
        //if (Enum.TryParse(newLine, out hotKey))
        //{
        Debug.Log("FSeditorExtender: Assigned hangar scale hotkey: " + newLine);
        //}
    }

    private string readSetting(StreamReader stream)
    {
        string newLine = string.Empty;
        try
        {
            while (newLine == string.Empty && !stream.EndOfStream)
            {
                newLine = stream.ReadLine();
                newLine = newLine.TrimStart(' ');
                if (newLine.Length > 1)
                {
                    if (newLine.Substring(0, 2) == "//")
                    {
                        //Debug.Log("FSeditorExtender settings comment Line: " + newLine);
                        newLine = string.Empty;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("FSeditorExtender: stream reader error: " + e.ToString());
        }

        //Debug.Log("FSeditorExtender stream Value: " + newLine);
        return newLine;
    }

    public void Update()
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
        if (gotKeyPress)
        {
            Debug.Log("FSeditorExtender: Scaling Hangar Geometry");
            if (geo != null)
            {
                if (currentScale == 1f) currentScale = 5f;
                else currentScale = 1f;                                   
                geo.transform.localScale = Vector3.one * currentScale;            
                geo.transform.localPosition = new Vector3(0f, -0.25f * (currentScale - 1f), 0f);
            }
        }
        //if (Input.GetKeyDown(KeyCode.Backspace))
        //{
        //    Debug.Log("Listing Objects");
        //    //ListTransforms(gameObject.transform);
        //    GameObject[] objects = (GameObject[])FindObjectsOfType(typeof(GameObject));
        //    for (int i = 0; i < objects.Length; i++)
        //    {
        //        Debug.Log(objects[i].name);
        //    }
        //}
    }
}