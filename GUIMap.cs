using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using EngineA;
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
				hex.renderer.material.mainTexture = hexTex.bitmap;
				
			}
		}
	}
#if TODO_RR
	private void AddTextureTerrain (GameObject hex, Map_Tile tile)
	{
		int offset;
		string path;
		if (tile.terrain.name.ToLower () == "mountain") {
			print ("entra");
			int numT = TextureTable.elegirImgTex (tile.strat_image_offset);
			path = pathTexTerrain + tile.terrain.name.ToLower () + numT;
			offset = 0;
			if (numT == 1) {
				offset = tile.strat_image_offset * Config.hex_w - Config.hex_w;
			} else {
				offset = (tile.strat_image_offset - 39) * Config.hex_w - Config.hex_w;
			}
			
		} else {
			path = pathTexTerrain + tile.terrain.name.ToLower ();
			offset = (tile.strat_image_offset * Config.hex_w) - Config.hex_w;			
		}
		SDL_Surface terraintex = SDL_Surface.LoadSurface (path, false);
		SDL_Surface hextex = new SDL_Surface ();
		SDL_Surface.copy_image (hextex, Config.hex_w, Config.hex_h, terraintex, offset, 0);
		//Add texture flag
		if (tile.nation != null) {
			Nation.nation_draw_flag(tile.nation,hextex);
		} 
		hex.renderer.material.mainTexture = hextex.bitmap;

		
	}

#endif

	void Awake ()
	{
		if (string.IsNullOrEmpty (scen_name)) {
			throw new Exception ("name of scenario not found");
		}
		Scenario.scen_load (scen_name);
		MakeMap (Engine.map);
	}
	
	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}
}