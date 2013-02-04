using UnityEngine;
using System.Collections;
using Miscellaneous;
using EngineApp;

[ExecuteInEditMode]
public class GUIScenInfo : MonoBehaviour {
	
	void OnGUI ()
	{
		GUI.Window (0, new Rect ((Screen.width / 2) - 150, (Screen.height / 2)-150, 300, 300), ShowInfo, "");
	}
	
	void ShowInfo (int windowID)
	{
		string info = "";
		if (Engine.map.isLoaded){
			info = Engine.GuiShowScenInfo();
		}
		GUI.Label(new Rect(20,30,300,400),info);
		if (GUI.Button(new Rect(125,235,50,25),"OK")){
			Application.LoadLevel("Map");
		}
	}
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
