using UnityEngine;
using System.Collections;

	[ExecuteInEditMode]
	public class GUIMenu : MonoBehaviour
	{
	
		private bool[] toogleBool = {true,false,false,false};
		private int activeToogle = 0;
		private int selGridInt = 0;
		private int selCampaign = 0;
		private string[] campaigns = new string[] {"1939\nGermany launches a series of blizkrieg attacks in Europe, beggining with Poland",
												   "1941 West\nHungry for the oil of the Middle East, the Axis moves through North Africa",
												   "1941 East\nIn the greatest invasion of history, the Axis strikes into the Soviet Union",
												   "1943 West\nThe Axis must defend the soft underbelly of Europe from the Allies",
												   "1943 East\nOverextended Soviet forces, the Axis tries to regain the initiative in the East"};
		private string[] selOption = new string[] {"Campaign", "Scenario", "Load Game", "Replay Game", "Online Game"};
	
		void OnGUI ()
		{
			GUI.Window (0, new Rect ((Screen.width/2)-250, 50, 500, 500), MenuWindow, "");
		}
	
		void DoUpdRButtons ()
		{
			for (int i=0; i<4; i++) {
				toogleBool [i] = GUILayout.Toggle (toogleBool [i], "Mapa " + (i + 1));	
			}

			for (int i=0; i<4; i++) {
				if (activeToogle != i) {
					if (toogleBool [i] == true) {
						toogleBool [activeToogle] = false;
						toogleBool [i] = true;
						activeToogle = i;
					}
				}
			}  
		}
	
		int rButtonWithGrid ()
		{
		
			return GUILayout.SelectionGrid (selGridInt, selOption, 5); 
			//DoUpdRButtons();
			
		}
	
		void MenuWindow (int windowID)
		{
		
			GUILayout.Label ("Rotulo");
			GUILayout.Space (20);
			selGridInt = rButtonWithGrid ();
			GUI.Box (new Rect(10,100,480,325),"");	
			switch(selGridInt)
			{
				case 0:
					selCampaign = GUI.SelectionGrid(new Rect(10,150,480,220),selCampaign,campaigns,1);
					break;
				case 1:
					//TODO_RR insertar codigo para GUI de escenario
					break;
				case 2:
					//TODO_RR insertar codigo para GUI de Load Game
					break;
				case 3:
					//TODO_RR insertar codigo para GUI de replay Game
					break;
				case 4:
					//TODO_RR insertar codigo para GUI de Online Game
					break;
			}
			GUI.BeginGroup(new Rect(190,350,200,300));
			if (GUILayout.Button ("START",GUILayout.Width(100))){
				//TODO_RR insertar codigo para empezar con la aplicacion
			}
			if (GUILayout.Button ("EXIT",GUILayout.Width(100))){
				//TODO_RR insertar codigo para empezar con la aplicacion
			}
			GUI.EndGroup();		
        
		}
	}