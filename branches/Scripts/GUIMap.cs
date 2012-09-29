using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using Engine;
using Miscellaneous;

public class GUIMap : MonoBehaviour
{
	
	public GameObject hexPrefab;
	public string mapname;
	
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
		for (int i=0; i<map.map_h; i++) {
			for (int j=0; j<map.map_w; j++) {
				GameObject hex;
				if (IsEven (j)) {
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * 1.5f, 0, -Mathf.Sqrt (3) * i), Quaternion.identity);
					
				} else {
					hex = (GameObject)Instantiate (hexPrefab, new Vector3 (j * 1.5f, 0,
				-(Mathf.Sqrt (3) * i) - (Mathf.Sqrt (3) / 2)), Quaternion.identity);
				}
				hex.transform.parent = this.gameObject.transform;
				Hexagon hexScript = hex.GetComponent(typeof(Hexagon)) as  Hexagon;
				hexScript.actualTexture = map.map[j,i].strat_image_offset;
				hexScript.maxTextures = TextureTable.GetMaxTextureOf (map.map[j,i].terrain.name.ToLower());
				SDL_Surface sdl = SDL_Surface.LoadSurface("Textures/terrain/"+map.map[j,i].terrain.name.ToLower(),false);
				hex.renderer.sharedMaterial = sdl.bitmapMaterial;
			}
		}
	}

	void Awake ()
	{
		
		XmlSerializer SerializerObj = new XmlSerializer (typeof(Map));
		// Create a new file stream for reading the XML file
		FileStream ReadFileStream = new FileStream ("Assets/Maps/" + mapname + ".xml", 
									FileMode.Open, FileAccess.Read, FileShare.Read);
		// Load the object saved above by using the Deserialize function
		Map LoadedObj = (Map)SerializerObj.Deserialize (ReadFileStream);
		LoadedObj.isLoaded = true;
		// Cleanup
		ReadFileStream.Close ();
		MakeMap(LoadedObj);
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