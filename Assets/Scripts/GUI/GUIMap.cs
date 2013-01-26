using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using EngineApp;
using Miscellaneous;
using DataFile;

public class GUIMap : MonoBehaviour
{
	
	public GameObject hexPrefab;
	public string scen_name;
	
	
	
	
	private void MakeMap (Map map)
	{
		//Create Map
		for (int i=0; i<map.map_h; i++) {
			for (int j=0; j<map.map_w; j++) {
				GameObject hex;
				if (Misc.IsEven (j)) {
					//hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * 1.5f, 0, -Mathf.Sqrt (3) * i), Quaternion.identity);
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * Config.hex_x_offset, 0, 
													-Config.hex_h * i), Quaternion.identity);
				} else {
					//hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * 1.5f, 0,		
					//-(Mathf.Sqrt (3) * i) - (Mathf.Sqrt (3) / 2)), Quaternion.identity);
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * Config.hex_x_offset, 0,
													-(Config.hex_h * i) - Config.hex_y_offset), Quaternion.identity);
				}
				//put hex as child of map
				hex.transform.parent = this.gameObject.transform;
				//TODO_RR AddTextureTerrain (hex, map.map [j, i]);
				SDL_Surface hexTex;
				//Draw Terrain
				hexTex = map.map_draw_terrain(j,i);
				if (map.map [j, i].g_unit!=null || map.map [j, i].a_unit!=null){
					hexTex = map.map_draw_units(hexTex,j,i,false);
				}
				hex.renderer.material = hexTex.BitmapMaterial;
				
			}
		}
	}

	
	private void onLoadScen(){
		Engine.engine_set_status(STATUS.STATUS_NONE);
		Engine.engine_init(scen_name);
		Engine.engine_run();
		Engine.engine_begin_turn(Engine.cur_player, DB.setup.type == SETUP.SETUP_LOAD_GAME);
		//Scenario.scen_load (scen_name);
		MakeMap (Engine.map);
	}
	
	void Awake ()
	{
		if (string.IsNullOrEmpty (scen_name)) {
			throw new Exception ("name of scenario not found");
		}
		//onLoadScen();
		//Scenario.scen_load("Poland.xml");
		MakeMap (Engine.map);
				
	}
	
	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Engine.draw_map){
			print (Engine.status);
			print ("Hay que repintar");
#if TODO_RR
			añadir las nieblas y repintar
#endif
		 	Engine.draw_map = false;
		}
	}
}