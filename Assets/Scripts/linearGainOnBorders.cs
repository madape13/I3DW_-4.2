using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class linearGainOnBorders : MonoBehaviour {
    [Tooltip("Place the Steam CameraRig here!")]
    public GameObject steamCameraRig;

    [Tooltip("This value sets the gain in the outer rim ;)")]
    [Range(0.5f, 3.0f)]
    public float gain = 2.0f;

    [Tooltip("This value sets the size of the outer rim (in percentage of the game area)")]
    [Range(0.05f, 0.5f)]
    public float percentageOfArea = 0.1f;
    
    protected float playAreaX;
    protected float playAreaY;

    protected float subAreaXmin = 0.0f;  // die max-Werte sind einfach min + grenzWert*2
    protected float subAreaYmin = 0.0f;

    protected float dimX = 2.0f;
    protected float dimY = 2.0f;

    private Material highlight;
    public Color color;

    void Start () {
        highlight = this.GetComponent<Renderer>().material;
		if (steamCameraRig == null) {
            Debug.LogError("Missing Steam CameraRig as Object");            
        } else {
            StartCoroutine(getPlayArea());        
        }
       
	}

    IEnumerator getPlayArea() {
        var rect = new Valve.VR.HmdQuad_t();
        while (!SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size.Calibrated, ref rect))
            yield return new WaitForSeconds(1f);

        Vector3 newScale = new Vector3(Mathf.Abs(rect.vCorners0.v0 - rect.vCorners2.v0), this.transform.localScale.y, Mathf.Abs(rect.vCorners0.v2 - rect.vCorners2.v2));
        if (false) {
            Debug.Log("Quad Ecke0 x:" + rect.vCorners0.v0 + " y: " + rect.vCorners0.v1 + " z: " + rect.vCorners0.v2);
            Debug.Log("Quad Ecke1 x:" + rect.vCorners1.v0 + " y: " + rect.vCorners1.v1 + " z: " + rect.vCorners1.v2);
            Debug.Log("Quad Ecke2 x:" + rect.vCorners2.v0 + " y: " + rect.vCorners2.v1 + " z: " + rect.vCorners2.v2);
            Debug.Log("Quad Ecke3 x:" + rect.vCorners3.v0 + " y: " + rect.vCorners3.v1 + " z: " + rect.vCorners3.v2);
            Debug.Log("New Scale would be x:" + newScale.x + " y: " + newScale.y + " z:" + newScale.z);
        }
        playAreaX = newScale.x;
        playAreaY = newScale.z;
    }

    /// Getrennte Gain-Bestimmung für die einzelnen Achsen:
    /// d.h. x erreicht => GainX an.
    bool isPlayerInGainAreaX(Vector3 playerPos) {
        if (Mathf.Abs(playerPos.x) > (playAreaX/2.0f * (1.0f - percentageOfArea)))
            return true;
        return false;
    }
    bool isPlayerInGainAreaY(Vector3 playerPos) {
        if (Mathf.Abs(playerPos.z) > (playAreaY / 2.0f * (1.0f - percentageOfArea)))
            return true;
        return false;
    }

    void Update () {
        /// Important: This only works if the head is component 3 of SteamCamRig (default!)
        Vector3 playerPos = steamCameraRig.transform.GetChild(2).transform.localPosition;

        setNewPositionBoxedGain(playerPos);
        myMesh(subAreaXmin+steamCameraRig.transform.position.x, subAreaYmin+ steamCameraRig.transform.position.z, dimX, dimY);
        highlight.color = this.color;
    }

    void setSubArea(float grenzWertX, float grenzWertY, Vector3 playerPos) {
        subAreaXmin = playerPos.x - grenzWertX;
        subAreaYmin = playerPos.z - grenzWertY;
    }

    void setNewPositionBoxedGain(Vector3 playerPos) {
        float grenzWertX = (playAreaX / 2.0f) * (1.0f - percentageOfArea);
        float grenzWertY = (playAreaY / 2.0f) * (1.0f - percentageOfArea);
        float shiftX = 0.0f;
        float shiftY = 0.0f;
        bool isInGain = false;

        /// Version 4:
        /// Final Try: there is a Sub-Area within the PlayArea, whenever
        /// this areas-Borders are reached, gain ist activated an this area moves
        /// within the borders of the playArea (shifting it with it).
        /// grenzWertX und Y sind die Größe der SubArea (es empfiehlt sich 50% Einzustellen).

        // Initiales Setzen der SubArea:
        if (Mathf.Abs(subAreaXmin) <= 0.0001f && Mathf.Abs(subAreaYmin) <= 0.0001f) {
            setSubArea(grenzWertX, grenzWertY, playerPos);
            Debug.Log("Setting subArea to (" + playerPos.x + ", " + playerPos.z + ")");
        }

        // haben wir die Area verlassen? (KISS programmiert)
        // In X:
        if ((playerPos.x < subAreaXmin) || (playerPos.x > subAreaXmin + 2 * grenzWertX)) {
            isInGain = true;
            // playArea verschiebung berechnen
            if (playerPos.x < subAreaXmin)
                shiftX = playerPos.x - subAreaXmin;
            else
                shiftX = playerPos.x - (subAreaXmin + 2 * grenzWertX);
        }
        if ((playerPos.z < subAreaYmin) || (playerPos.z > subAreaYmin + 2 * grenzWertY)) {
            isInGain = true;
            // playArea verschiebung berechnen
            if (playerPos.z < subAreaYmin)
                shiftY += playerPos.z - subAreaYmin;
            else
                shiftY += playerPos.z - (subAreaYmin + 2 * grenzWertY);
        }

        // Darstellung und shift Schluss:
        if (isInGain) {
            // subArea verschieben
            subAreaXmin += shiftX;
            subAreaYmin += shiftY;
         // Gain:
            shiftX *= gain;
            shiftY *= gain;
            shiftX += steamCameraRig.transform.position.x;
            shiftY += steamCameraRig.transform.position.z;
            steamCameraRig.transform.position = new Vector3(shiftX, 0f, shiftY);                    
        }
    }

   

 void myMesh(float posX, float posY, float dimX, float dimY) {

        MeshFilter mf = GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mf.mesh = mesh;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(posX, 0, posY);
        vertices[1] = new Vector3(posX+dimX, 0, posY);
        vertices[2] = new Vector3(posX, 0, posY+dimY);
        vertices[3] = new Vector3(posX+dimX, 0, posY+dimY);
        mesh.vertices = vertices;

        int[] tri = new int[6];
        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;
        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;
        mesh.triangles = tri;

        Vector3[] normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;
    }

}
