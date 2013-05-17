using UnityEngine;
using System.Collections;
using Miscellaneous;
using EngineApp;

public class Edges : MonoBehaviour {
	
	public GameObject planeH;
	public GameObject planeV;

	void Awake () {
		planeH.renderer.material = new Material(Shader.Find("Diffuse"));
		planeH.renderer.material.mainTexture = Resources.Load("Textures/menu/background") as Texture2D;
		planeV.renderer.material = new Material(Shader.Find("Diffuse"));
		planeV.renderer.material.mainTexture = Resources.Load("Textures/menu/background") as Texture2D;
		GameObject edgetop = (GameObject) Instantiate (planeH, new Vector3 (0, 0, 0), Quaternion.identity);
		edgetop.transform.parent = this.gameObject.transform;
		GameObject edgebottom = (GameObject) Instantiate (planeH, new Vector3 (0, 0, -Config.hex_h * Engine.map.map_h), Quaternion.identity);
		edgebottom.transform.parent = this.gameObject.transform;
		GameObject edgeleft = (GameObject)Instantiate (planeV, new Vector3 (-Config.hex_w/2, 0, Config.hex_h/2), Quaternion.identity);
		edgeleft.transform.parent = this.gameObject.transform;
		GameObject edgeright;
		if (Misc.IsEven(Engine.map.map_w)){
			 edgeright = (GameObject) Instantiate (planeV, new Vector3 (Config.hex_x_offset*Engine.map.map_w-Config.hex_x_offset, 0, 0), Quaternion.identity);
		}
		else{
			edgeright = (GameObject) Instantiate (planeV, new Vector3 (Config.hex_x_offset*Engine.map.map_w-Config.hex_w/2, 0, Config.hex_h/2), Quaternion.identity);
		}
		edgeright.transform.parent = this.gameObject.transform;
	}
	
}
