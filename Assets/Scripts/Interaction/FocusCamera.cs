using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;

public class FocusCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (Engine.map.isLoaded){
			int xpos = Engine.map.map_w*Config.hex_x_offset-15;
			int zpos = Engine.map.map_h*-Config.hex_h;
			print (xpos);
			print (zpos);
			print(Camera.mainCamera.transform.position.x);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
