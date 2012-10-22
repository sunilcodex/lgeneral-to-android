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
	private string pathTexTerrain = "Textures/terrain/";
	
	private bool IsEven (int number)
	{
		if (number % 2 == 0) {
			return true;
		} else {
			return false;
		}
	}
	
	private void MakeMap (Map map)
	{
		//Create Map
		for (int i=0; i<map.map_h; i++) {
			for (int j=0; j<map.map_w; j++) {
				GameObject hex;
				if (IsEven (j)) {
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * Config.hex_x_offset, 0, -Config.hex_h * i), Quaternion.identity);
				}
				else{
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * Config.hex_x_offset, 0,
				-(Config.hex_h * i) - Config.hex_y_offset), Quaternion.identity);
				}
				//put hex as child of map
				hex.transform.parent = this.gameObject.transform;
				Hexagon hexScript = hex.GetComponent(typeof(Hexagon)) as  Hexagon;
				//Load texture terrain
				hexScript.actualTexture = map.map[j,i].strat_image_offset;
				hexScript.maxTextures = TextureTable.GetMaxTextureOf (map.map[j,i].terrain.name.ToLower());
				if (map.map[j,i].terrain.name.ToLower()=="mountain"){
					int numT = TextureTable.elegirImgTex(map.map[j,i].strat_image_offset,"mountain");
					string path = pathTexTerrain+map.map[j,i].terrain.name.ToLower()+numT;
					SDL_Surface sdl = SDL_Surface.LoadSurface(path,false);
					hex.renderer.material.mainTexture = sdl.bitmap;
				}
				else{
					string path = pathTexTerrain+map.map[j,i].terrain.name.ToLower();
					SDL_Surface sdl = SDL_Surface.LoadSurface(path,false);
					hex.renderer.material.mainTexture = sdl.bitmap;
				}
#if TODO_RR
				//Add texture Unit
				if (map.map[j,i].g_unit!=null){
					AddTextureUnit(map.map[j,i].g_unit.name, hex);
				}
				//Add texture flag
				if (map.map[j,i].nation!=null){
					AddTextureFlag(map.map[j,i].nation,hex);
				}
#endif
				
			}
		}
	}
	
	private void AddTextureUnit(string unit_name,GameObject hex){
		List<Material> materials = new List<Material>();
		Material[] aux = hex.renderer.sharedMaterials;
		for (int i=0; i<aux.Length;i++){
			materials.Add(aux[i]);
		}
		Unit_Lib_Entry unit_lib = DB.UnitLib;
		Unit_Lib_Entry unit_aux = unit_lib.unit_lib_find_by_name(Unit.DeleteOrdinal(unit_name));
	}
	
	private void AddTextureFlag(Nation nation,GameObject hex){
		List<Material> materials = new List<Material>();
		Material[] aux = hex.renderer.sharedMaterials;
		for (int i=0; i<aux.Length;i++){
			materials.Add(aux[i]);
		}
		SDL_Surface dest = new SDL_Surface();
		SDL_Surface.copy_image(dest,Nation.nation_flag_width,
					Nation.nation_flag_height,Nation.nation_flags,0,
					(Nation.nation_flags.h-nation.Flag_offset-Nation.nation_flag_height));
		//materials.Add(dest.bitmapMaterial);
		hex.renderer.sharedMaterials = materials.ToArray();

		
	}
	void Awake ()
	{
		if (string.IsNullOrEmpty(scen_name)){
			throw new Exception("name of scenario not found");
		}
		Scenario.scen_load(scen_name);
		MakeMap(Engine.map);
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