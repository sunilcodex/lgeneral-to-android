using UnityEngine;
using System.Collections;
using Miscellaneous;
using EngineApp;
using DataFile;

[ExecuteInEditMode]
public class GUIScenInfo : MonoBehaviour {
	private string info;

	void OnGUI ()
	{
		GUI.Window (0, new Rect ((Screen.width / 2) - 150, (Screen.height / 2)-150, 300, 300), ShowInfo, "");
	}
	
	void ShowInfo (int windowID)
	{
		GUI.Label(new Rect(20,30,300,400),info);
		if (GUI.Button(new Rect(125,235,50,25),"OK")){
			Application.LoadLevel("Map");
		}
	}

	void Awake(){
		Engine.engine_set_status(STATUS.STATUS_NONE);
		Engine.engine_init(Config.CampaignSelected);
		Engine.engine_run();
		Engine.engine_begin_turn(Engine.cur_player, DB.setup.type == SETUP.SETUP_LOAD_GAME);
		if (Engine.map.isLoaded){
			info = Engine.GuiShowScenInfo();
		}
	}
}
