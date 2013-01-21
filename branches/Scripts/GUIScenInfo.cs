using UnityEngine;
using System.Collections;
using Miscellaneous;

[ExecuteInEditMode]
public class GUIScenInfo : MonoBehaviour {
	
	void OnGUI ()
	{
		GUI.Window (0, new Rect ((Screen.width / 2) - 150, (Screen.height / 2)-100, 300, 200), ShowInfo, "");
	}
	
	void ShowInfo (int windowID)
	{
		GUI.Label(new Rect(68,50,200,75),Config.Show_info_scen);
		if (GUI.Button(new Rect(125,125,50,25),"OK")){
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
