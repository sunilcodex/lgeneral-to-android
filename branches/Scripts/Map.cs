using UnityEngine;
using System.Collections;

public class Map : MonoBehaviour {
	
	private int tam_x = 10;
	private int tam_y = 10;
	
	private bool EsPar(int num){
		if (num%2==0){
			return true;
		}
		else{
			return false;
		}
	}
	
	void Awake(){
		
		GameObject hexagon = GameObject.Find("Hexagon");
		GameObject map = GameObject.Find ("Map");
		for (int i=0; i<tam_x;i++){
			for (int j=0; j<tam_y;j++){
				if (i==0 && j==0){
					map.transform.Translate(0,0,0);
					hexagon.transform.Translate(0,0,0);
				}
				else if(EsPar(i)){
					GameObject hex = (GameObject)Instantiate(hexagon,new Vector3(i*1.5f,0,-Mathf.Sqrt(3)*j),Quaternion.identity);
					hex.transform.parent = map.transform;
				}
				else{
					float desp = Mathf.Sqrt(3)/2;
					GameObject hex = (GameObject)Instantiate(hexagon,new Vector3(i*1.5f,0,(-Mathf.Sqrt(3)*j)+desp),Quaternion.identity);
					hex.transform.parent = map.transform;
				}
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
