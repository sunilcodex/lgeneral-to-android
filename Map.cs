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
		for (int i=0; i<width;i++){
			for (int j=0; j<height;j++){
				GameObject hex;
				if(IsEven(i)){
					hex = (GameObject)Instantiate(hexPrefab,new Vector3(i*1.5f,0,-Mathf.Sqrt(3)*j),Quaternion.identity);
					
				}
				else{
					float desp = Mathf.Sqrt(3)/2;
					hex = (GameObject)Instantiate(hexPrefab,new Vector3(i*1.5f,0,(-Mathf.Sqrt(3)*j)+desp),Quaternion.identity);
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
