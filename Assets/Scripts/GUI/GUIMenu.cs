using UnityEngine;
using System.Collections;
using Miscellaneous;
using EngineApp;
using DataFile;

[ExecuteInEditMode]
public class GUIMenu : MonoBehaviour
{
	
	private int selGridInt = 0;
	public GUIStyle myStyle;
	public GUIStyle myStyle2;
	private string[] selOption = new string[] {"Campaign", "Config", "About"};

	void Awake ()
	{
		Misc.set_random_seed ();
	}
	
	void OnGUI ()
	{
		GUI.Window (0, new Rect ((Screen.width / 2) - 200, (Screen.height / 2) - 175, 400, 350), WindowMenu, "");
	}
	
	private int rButtonWithGrid ()
	{
		
		return GUILayout.SelectionGrid (selGridInt, selOption, 3); 
			
	}
	
	void WindowMenu (int windowID)
	{
			GUI.Label (new Rect (70, 40, 80, 25), "Imagen", myStyle2);
			GUI.Label (new Rect (200, 40, 100, 25), "LGeneral", myStyle);
			GUILayout.Space (70);
			selGridInt = rButtonWithGrid ();
			GUI.Box (new Rect (10, 120, 380, 180), "");
			switch (selGridInt) {
			case 0:
				if (GUI.Button (new Rect (50, 140, 100, 20), new GUIContent ("1939", "Germany launches a series of blizkrieg attacks in Europe, beggining with Poland"))) {
					Config.CampaignSelected = "Poland.xml";
					Application.LoadLevel("ScenInfo");
				}
				GUI.Button (new Rect (50, 170, 100, 20), new GUIContent ("1941 West", 
				"Hungry for the oil of the Middle East, the Axis moves through North Africa"));
				GUI.Button (new Rect (50, 200, 100, 20), 
				new GUIContent ("1941 East", "In the greatest invasion of history, the Axis strikes into the Soviet Union"));
				GUI.Button (new Rect (50, 230, 100, 20), 
				new GUIContent ("1943 West", "The Axis must defend the soft underbelly of Europe from the Allies"));
				GUI.Button (new Rect (50, 260, 100, 20), new GUIContent ("1943 East", 
				"Overextended Soviet forces, the Axis tries to regain the initiative in the East"));
				GUI.Label (new Rect (200, 175, 150, 100), GUI.tooltip);
				break;
			case 1:
				Config.supply = GUI.Toggle (new Rect (145, 155, 200, 30), Config.supply, "Units must supply?");
				Config.weather = GUI.Toggle (new Rect (145, 185, 200, 30), Config.weather, "Does weather have?");
				Config.fog_of_war = GUI.Toggle (new Rect (145, 215, 200, 30), Config.fog_of_war, "Fog of war?");
				Config.show_cpu_turn = GUI.Toggle (new Rect (145, 245, 200, 30), Config.show_cpu_turn, "Show CPU turn?");
				break;
			case 2:
				GUI.Label(new Rect(50,130,100,20),"Version: 1.0");
				GUI.Label(new Rect(50,150,200,20),"Autor: Ruben Ramos");
				GUI.Label(new Rect(50,170,300,120),"This code is based on the adaptation of a project " +
					"based on the game LGeneral Copyright 2001-2005 by Michael Speck. This program is free software; " +
					"you can redistribute it and/or modify it under the terms of the GNU General Public License as " +
					"published by the Free Software Foundation.");
				break;
			}
			if (GUI.Button (new Rect (170, 310, 55, 25), "Exit")) {
				Application.Quit ();
			}	
	}
	
}
