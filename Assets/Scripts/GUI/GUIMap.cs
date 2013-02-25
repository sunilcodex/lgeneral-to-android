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
	private static GameObject[,] mapped;
        
	private void MakeMap (Map map)
	{
		mapped = new GameObject[map.map_w,map.map_h];
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
				mapped[j,i] = hex;
				//put hex as child of map
				hex.transform.parent = this.gameObject.transform;
				//TODO_RR AddTextureTerrain (hex, map.map [j, i]);
				SDL_Surface hexTex;
				//Draw Terrain
				hexTex = map.map_draw_terrain (j, i);
				if (map.map [j, i].g_unit != null || map.map [j, i].a_unit != null) {
					hexTex = map.map_draw_units (hexTex, j, i, !Engine.Air_mode, false);                                 
				}
				hex.renderer.material = hexTex.BitmapMaterial;
                                
			}
		}
		Engine.status = STATUS.STATUS_NONE;
	}
    
	public static void Repaint(Map map, bool use_frame){
			for (int i=0; i<map.map_h; i++) {
                for (int j=0; j<map.map_w; j++) {
					SDL_Surface hexTex;
					//Draw Terrain
					hexTex = map.map_draw_terrain (j, i);
					if (map.map [j, i].g_unit != null || map.map [j, i].a_unit != null) {
						 if (Engine.cur_unit != null && Engine.cur_unit.x == j && Engine.cur_unit.y == i
							&& Engine.status != STATUS.STATUS_MOVE && map.mask[j, i].spot){
							hexTex = map.map_draw_units (hexTex, j, i, !Engine.Air_mode, use_frame); 
						}
						else{
							hexTex = map.map_draw_units (hexTex, j, i, !Engine.Air_mode, false);
						}
					}
					if (Engine.showCross != null && Engine.showCross.x==j && Engine.showCross.y==i)
             		{
 						SDL_Surface cross = SDL_Surface.Resize(Engine.terrain.terrainIcons.cross,0.6f);
 						int xdest = (Config.hex_w-cross.w)/2;
 						int ydest = (Config.hex_h-cross.h)/2;
 						SDL_Surface.copy_image_without_key(hexTex,cross,xdest,ydest,Color.black);
						//TODO_RR hexTex.BitmapMaterial.color = Color.red;
					}
					mapped[j,i].renderer.material = hexTex.BitmapMaterial;
				}
			}
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
		if (Engine.Draw_from_state_machine){
			Engine.status = STATUS.STATUS_NONE;
			bool use_frame = (Engine.cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU);
			Repaint(Engine.map,use_frame);
			Engine.Draw_from_state_machine = false;
		}
		/*if (Engine.Draw_map_state_machine){
			if (Engine.map.isLoaded) {
				Engine.status = STATUS.STATUS_NONE;
				bool use_frame = (Engine.cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU);
				GUIMap.Repaint(Engine.map,use_frame);
			}
			Engine.draw_map = false;
			Engine.Draw_map_state_machine = false;
		}*/
	}
}