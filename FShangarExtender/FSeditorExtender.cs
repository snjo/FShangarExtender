using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
class FSeditorExtender : MonoBehaviour
{
    public SPHCamera SPHcam;
    public VABCamera VABcam;
    public GameObject geo;

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
            Debug.Log("Previous hangar Bounds" + elogic[i].editorBounds.extents);
            elogic[i].editorBounds.extents = new Vector3(1000f, 200f, 1000f);
            Debug.Log("New hangar Bounds" + elogic[i].editorBounds.extents);
        }
        
        geo = GameObject.Find("SPH_Interior_Geometry");
        //if (geo == null)
        //{
        //    //geo = GameObject.Find("VAB_Interior_Geometry");            
        //    if (geo != null) Debug.Log("Found VAB geo");
        //    else Debug.Log("did not find VAB geo");
        //}
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            Debug.Log("Scaling Hangar Geometry");
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