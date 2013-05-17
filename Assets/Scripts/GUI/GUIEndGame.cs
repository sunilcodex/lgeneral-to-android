using UnityEngine;
using System.Collections;
using EngineApp;

[ExecuteInEditMode]
public class GUIEndGame : MonoBehaviour {

	public GUIStyle lose;
	public GUIStyle win;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI(){
		GUI.Window (0, new Rect ((Screen.width / 2) - 100, (Screen.height / 2) - 105, 200, 210),WindowEndGame,"");
	}
	
	void WindowEndGame(int windowID){
		if (Scenario.scen_get_result()=="defeat")
			GUI.Label(new Rect(55,20,90,90),"",lose);
		else
			GUI.Label(new Rect(57.5f,20,85,110),"",win);
		GUI.Label(new Rect(15,115,170,25),"Result: "+ Scenario.scen_get_result());
		GUI.Label(new Rect(15,140,170,25),"Message: "+Scenario.scen_get_result_message());
		if (GUI.Button(new Rect(25,170,150,25),"Back to Menu"))
			Application.LoadLevel("Menu");
		
	}
}
