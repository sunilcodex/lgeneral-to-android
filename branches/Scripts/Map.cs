using UnityEngine;
using System.Collections;

public class Map : MonoBehaviour {
	
	public int width;
	public int height;
	public GameObject hexPrefab;
	public Material defaultTexture;
	
	//public TipoSuelo[,] mapa;
	
	private bool IsEven(int number){
		if (number%2==0){
			return true;
		}
		else{
			return false;
		}
	}
	
	void Awake(){
		
		
		for (int i=0; i<height; i++){
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
		}
		
	}
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

