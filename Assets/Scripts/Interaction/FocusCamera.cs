using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;

public class FocusCamera : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
		if (Engine.map.isLoaded){
			float xpos = Engine.map.map_w*Config.hex_x_offset-15;
			float zpos = Engine.map.map_h*-Config.hex_h+Config.hex_y_offset;
			Camera.mainCamera.transform.position = new Vector3((xpos/2),50,(zpos/2));
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
