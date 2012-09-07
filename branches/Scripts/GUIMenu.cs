using UnityEngine;
using System.Collections;

	public class GUIMenu : MonoBehaviour
	{
	
		private bool[] toogleBool = {true,false,false,false};
		private int activeToogle = 0;
		private int selGridInt = 0;
		private string[] selStrings = new string[] {"Mapa 1", "Mapa 2", "Mapa 3", "Mapa 4"};
	
		void OnGUI ()
		{
			GUILayout.Window (0, new Rect (0, 0, 300, 300), MenuWindow, "");
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
	
		void rButtonWithGrid ()
		{
		
			selGridInt = GUILayout.SelectionGrid (selGridInt, selStrings, 2);
		}
	
		void MenuWindow (int windowID)
		{
		
			GUILayout.Label ("LGeneral");
			GUILayout.Space (20);
			//DoUpdRButtons();
			rButtonWithGrid ();
			GUILayout.Space (20);
			GUILayout.Button ("Empezar");
			GUILayout.Button ("Salir");
		
        
		}
	}