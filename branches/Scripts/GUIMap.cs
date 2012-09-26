using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using Engine;

public class GUIMap : MonoBehaviour
{
	
	public GameObject hexPrefab;
	//public Material defaultTexture;
	public string mapname;
	//private string title;
	//private string description;
	
	//public TipoSuelo[,] mapa;
	
	private void ReadXMLMapFile (string name)
	{
		XmlDocument xDoc = new XmlDocument ();
		xDoc.Load ("Assets/Maps/" + name + ".xml");
		//title = xDoc.GetElementsByTagName("title")[0].InnerText;
		//print (title);
		//description = xDoc.GetElementsByTagName("description")[0].InnerText;
		//print (description);
		//width = int.Parse (xDoc.GetElementsByTagName ("map_w") [0].InnerText);
		//height = int.Parse (xDoc.GetElementsByTagName ("map_h") [0].InnerText);
		/*XmlNodeList terrain = xDoc.GetElementsByTagName("terrain");
		XmlNodeList cells = ((XmlElement)terrain[0]).GetElementsByTagName("cell"); 
		foreach (XmlElement node in cells){
			int y = Convert.ToInt32(node.SelectSingleNode("position_h").InnerText);
			int x = Convert.ToInt32(node.SelectSingleNode("position_w").InnerText);
			string terrain_type = node.SelectSingleNode("terrain_type").InnerText;
			int orientation = Convert.ToInt32(node.SelectSingleNode("orientation").InnerText);
			MakeMap(x,y,terrain_type,orientation);
			
		}
		*/
		
		XmlNodeList terrain = xDoc.GetElementsByTagName ("map");
		foreach (XmlElement node in terrain) {
			int y = Convert.ToInt32 (node.SelectSingleNode ("height").InnerText);
			int x = Convert.ToInt32 (node.SelectSingleNode ("width").InnerText);
			string terrain_type = node.SelectSingleNode ("celltype").InnerText.ToLower ();
			int orientation = Convert.ToInt32 (node.SelectSingleNode ("orientation").InnerText);
			MakeMap (x, y, terrain_type, orientation);
			
		}
		
	}
	
	private void MakeMap (int x, int y, string terrain_type, int orientation)
	{
		GameObject hex;
		if (IsEven (x)) {
			hex = (GameObject)Instantiate (hexPrefab, new Vector3 (x * 1.5f, 0, -Mathf.Sqrt (3) * y), Quaternion.identity);
					
		} else {
			hex = (GameObject)Instantiate (hexPrefab, new Vector3 (x * 1.5f, 0,
				-(Mathf.Sqrt (3) * y) - (Mathf.Sqrt (3) / 2)), Quaternion.identity);
		}
		hex.transform.parent = this.gameObject.transform;
		Hexagon hexScript = hex.GetComponent (typeof(Hexagon)) as  Hexagon;
		hexScript.actualTexture = orientation;
		hexScript.maxTextures = TextureTable.GetMaxTextureOf (terrain_type);
		if (hexScript.maxTextures == -1) {
			Debug.Log ("Texture don't exit");
		}
		
		
		//hex.renderer.sharedMaterial = Resources.Load("Textures/Terrain/Materials/"+terrain_type) as Material;
		hex.renderer.material.mainTexture = Resources.Load ("Textures/Terrain/" + terrain_type) as Texture2D;

			
	}
	
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
				SDL_Surface sdl = SDL_Surface.LoadSurface(map.map[j,i].terrain.name.ToLower(),false);
				hex.renderer.material.mainTexture = sdl.bitmap;
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
		//ReadXMLMapFile (mapname);
		/*for (int i=0; i<height; i++){
			for (int j=0; j<width; j++){
				GameObject hex;
				if (IsEven(j)){
					hex = (GameObject)Instantiate(hexPrefab,new Vector3(j*1.5f,0,-Mathf.Sqrt(3)*i),Quaternion.identity);
					
				}
				else{
					hex = (GameObject)Instantiate(hexPrefab,new Vector3(j*1.5f,0,
						-(Mathf.Sqrt(3)*i)-(Mathf.Sqrt(3)/2)),Quaternion.identity);
				}
				hex.transform.parent = this.gameObject.transform;
				Hexagon hexScript = hex.GetComponent(typeof(Hexagon)) as  Hexagon;
				hexScript.actualTexture = 1;
				hexScript.maxTextures = 3;
				//Material hexMaterial  = hex.renderer;
				hex.renderer.sharedMaterial = defaultTexture;
				
			}
		}*/
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