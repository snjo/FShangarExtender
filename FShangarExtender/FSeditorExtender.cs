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

    public void Start()
    {
        SPHcam = Camera.main.GetComponent<SPHCamera>();
        SPHcam.maxHeight = 300f;
        SPHcam.maxDistance = 1000f;
        SPHcam.maxDisplaceX = 1000f;
        SPHcam.maxDisplaceZ = 1000f;
        SPHcam.scrollHeight = 300f;

        VABcam = Camera.main.GetComponent<VABCamera>();
        VABcam.maxHeight = 300f;
        VABcam.maxDistance = 1000f;
        VABcam.scrollHeight = 300f;

        GameObject gameLogic = GameObject.Find("GameLogic");
        EditorLogic editorLogic = gameLogic.GetComponent<EditorLogic>();
        editorLogic.editorBounds.extents = new Vector3(1000f, 300f, 1000f);        
    }

    public void Update()
    {

        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            //Camera.main.transform.position += Vector3.forward;
            GameObject geo = GameObject.Find("SPH_Interior_Geometry");
            Debug.Log("geo: " + geo.transform.localScale);
            geo.transform.localScale = new Vector3(10f, 10f, 10);
        }
    }
}