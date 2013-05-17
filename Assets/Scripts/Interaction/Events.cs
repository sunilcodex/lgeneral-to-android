using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;
using DataFile;
using AI_Enemy;

public class Events : MonoBehaviour {
	
	private int widthInfo = 250;
	private int windowType = 0;
	private Ray ray;
	private RaycastHit hit;
	private RaycastHit hitSelected;
	private int x, y;
	private Unit unitAtk;
	private int addTurn = 0;
	
	void Awake(){
		if (Engine.map.isLoaded){
			float xpos = Engine.map.map_w*Config.hex_x_offset-15+widthInfo;
			float zpos = Engine.map.map_h*-Config.hex_h+Config.hex_y_offset;
			Camera.mainCamera.transform.position = new Vector3((xpos/2),50,(zpos/2));
		}
	}
	
	void OnGUI ()
	{
		if (Engine.map.isLoaded) {
			GUI.Window (windowType, new Rect (Screen.width - widthInfo, 0, widthInfo, Screen.height), ConfigWindow, "");
		}
	}
	
	void ConfigWindow (int windowID)
	{
		switch (windowID) {
		case 0:
			GeneralConfig ();
			break;
		case 2:
			VictCondGUI ();
			break;
		case 3:
			EndTurnGUI ();
			break;
		case 4:
			ShowCUnitInfoGUI ();
			break;
		}
	}
	
	private bool isVisibleUnit (int x, int y)
	{
		foreach (Unit unit in Scenario.vis_units) {
			if (unit.x == x && unit.y == y)
				return true;
		}
		return false;
	}
	
	private void GeneralConfig ()
	{
		string turns = "Turn " + (Scenario.turn+addTurn) + " of " + Scenario.scen_info.turn_limit;
		string prestige = "Prestige: " + Player.players_get_first ().prestige;
		string weather = "Weather:  " +
                    ((Scenario.turn < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes [Scenario.scen_get_weather ()].name : "");
		string forecast = "Forecast: " +
                    ((Scenario.turn + 1 < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes [Scenario.scen_get_forecast ()].name : "");
		GUI.Label (new Rect (83, 25, 100, 25), turns);
		GUI.Label (new Rect (83, 50, 100, 25), prestige);
		GUI.Label (new Rect (83, 75, 100, 25), weather);
		GUI.Label (new Rect (83, 100, 100, 25), forecast);
		if (GUI.Button (new Rect (15, 125, 90, 25), "Supply Units")) {
			SupplyUnitsGUI ();
		}
		GUI.Button (new Rect (145, 125, 90, 25), "Deploy Units");
		if (GUI.Button (new Rect (15, 155, 90, 25), "Vict. Cond")) {
			windowType = 2;
		}
		if (GUI.Button (new Rect (145, 155, 90, 25), "End Turn")) {
			windowType = 3;
		}
		string current_info = ""; 
		if (Engine.cur_unit != null) {
			current_info = Engine.cur_unit.name + "\nFuel:" +
						   Engine.cur_unit.cur_fuel + " Ammo:" +
						   Engine.cur_unit.cur_ammo + " Ent:" + Engine.cur_unit.entr;
		}
		if (GUI.Button (new Rect (15, 185, 90, 25), "C. Unit Info") && Engine.cur_unit!=null) {
			windowType = 4;
		}
		if (GUI.Button(new Rect(145,185,90,25),"Exit")){
			Application.Quit();
		}
		GUI.Box (new Rect (37.5f, 215, 175, 40), current_info);
		string hover_info = "";
		string terrain_info = "";
		if (hitSelected.transform != null) {
			terrain_info = Engine.map.map [x, y].name + "(" + x + "," + y + ")";
			if (Engine.map.map [x, y].a_unit != null && isVisibleUnit (x, y)) {
				hover_info = Engine.map.map [x, y].a_unit.name + "\nFuel:" +
						   	 Engine.map.map [x, y].a_unit.cur_fuel + " Ammo:" +
						   	 Engine.map.map [x, y].a_unit.cur_ammo + " Ent:" + Engine.map.map [x, y].a_unit.entr;
			}
			if (Engine.map.map [x, y].g_unit != null && isVisibleUnit (x, y)) {
				hover_info = Engine.map.map [x, y].g_unit.name + "\nFuel:" +
						   	 Engine.map.map [x, y].g_unit.cur_fuel + " Ammo:" +
						   	 Engine.map.map [x, y].g_unit.cur_ammo + " Ent:" + Engine.map.map [x, y].g_unit.entr;
			}
		}
		GUI.Box (new Rect (37.5f, 260, 175, 40), hover_info);
		GUI.Label (new Rect (83, 300, 200, 20), terrain_info);
		if (unitAtk != null) {
 			int unit_damage, target_damage;
 			Unit attacker = Engine.cur_unit.unit_backup ();
 			Unit defender = unitAtk.unit_backup ();
 			Unit.GetExpectedLosses (attacker, defender, out unit_damage, out target_damage);
 			GUI.Label (new Rect (50, 320, 200, 20), "Expected losses: " + unit_damage + " vs " + target_damage); 
 		}
		
	}
	
	private void SupplyUnitsGUI ()
	{
		if (Engine.map.isLoaded && Engine.cur_unit != null) {
			int ammo, fuel;
			if (Engine.cur_unit.CheckSupply (Unit.UNIT_SUPPLY.UNIT_SUPPLY_ANYTHING, out ammo, out fuel))
				Engine.cur_unit.Supply (Unit.UNIT_SUPPLY.UNIT_SUPPLY_ALL);
			Engine.engine_select_unit (Engine.cur_unit);
			Engine.draw_map = true;
		}
	}
	
	private void VictCondGUI ()
	{
		string info = Engine.gui_show_conds ();
		GUILayout.Box (info);
		if (GUILayout.Button ("OK"))
			windowType = 0;
	}
	
	private void EndTurnGUI(){
		bool endGame = false;
		GUILayout.Label("Do you really want to end your turn?");
		GUILayout.Label("End Your Turn #" + (Scenario.turn+addTurn), GUILayout.ExpandWidth(true));
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("YES")){
			Engine.engine_end_turn();
			if (!Engine.end_scen){
            	Engine.engine_begin_turn(null, false);
			}
			if (Engine.draw_map)
            {
                    Draw();
            }
			while (Engine.cur_player.ctrl != PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                {
                    Engine.engine_end_turn();
                    if (!Engine.end_scen){
                        Engine.engine_begin_turn(null, false);
					}
                    if (Engine.draw_map)
                    {
                        Draw();
                    }
					if (Engine.cur_player == null){
						endGame = true;
						addTurn = 0;
						break;
					}
                }
			if (endGame){
				Application.LoadLevel("EndGame");
			}
			if (Scenario.turn==0){
				addTurn = 1;
			}
			windowType = 0;
		}
		if (GUILayout.Button ("NO")){
			windowType = 0;
		}	
		GUILayout.EndHorizontal();
	}
	
	private void ShowCUnitInfoGUI ()
	{
		if (Engine.map.isLoaded && Engine.cur_unit != null) {
			Unit unit = Engine.cur_unit;
			string str = "";
			str+= "Unit Name:\t" + unit.name;
			str+="\nClass:\t\t" + DB.UnitLib.unit_classes [unit.prop.unit_class].name;
			str+="\nMovement:\t" + DB.UnitLib.mov_types [unit.prop.mov_type].name;
			str+="\nTarget:\t\t" + DB.UnitLib.trgt_types [unit.prop.trgt_type].name;
			/* ammo, fuel, spot, mov, ini, range */
			if (unit.prop.ammo == 0)
				str+="\nAmmo:\t\tN.A.";
			else if (Engine.cur_player == null || Player.player_is_ally (Engine.cur_player, unit.player))
				str+="\nAmmo:\t\t" + unit.cur_ammo + ", " + unit.prop.ammo;
			else
				str+="\nAmmo:\t\t" + unit.prop.ammo;
			if (unit.prop.fuel == 0)
				str+="\nFuel:\t\tN.A.";
			else if (Engine.cur_player == null || Player.player_is_ally (Engine.cur_player, unit.player))
				str+="\nFuel:\t\t" + unit.cur_fuel + ", " + unit.prop.fuel;
			else
				str+="\nFuel:\t\t" + unit.prop.fuel;
			str+="\nSpotting:\t\t" + unit.prop.spt;
			str+="\nMovement:\t" + unit.prop.mov;
			str+="\nInitiative:\t\t" + unit.prop.ini;
			str+="\nRange:\t\t" + unit.prop.rng;
			str+="\nExperience:\t" + unit.exp;
			str+="\nEntrenchment:\t" + unit.entr;
			/* attack/defense */
			for (int i = 0; i < DB.UnitLib.trgt_type_count; i++) {
				if (unit.prop.atks [i] < 0)
					str+="\n" + DB.UnitLib.trgt_types [i].name + " Attack:\t" + -unit.prop.atks [i];
				else
					str+="\n" + DB.UnitLib.trgt_types [i].name + " Attack:\t" + unit.prop.atks [i];
			}
			str+="\nGround Defense:\t" + unit.prop.def_grnd;
			str+="\nAir Defense:\t" + unit.prop.def_air;
			str+="\nClose Defense:\t" + unit.prop.def_cls;
			str+="\nSuppression:\t" + unit.turn_suppr;

			/* transporter */
			if (unit.trsp_prop != null) {
				/* icon */
				/* name & type */
				str+="\nTransporter Name:\t\t" + unit.trsp_prop.name;
				str+="\nTrans. Class:\t\t" + DB.UnitLib.unit_classes [unit.trsp_prop.unit_class].name;
				str+="\nTrans. Movement:\t" + DB.UnitLib.mov_types [unit.trsp_prop.mov_type].name;
				str+="\nTrans. Target:\t" + DB.UnitLib.trgt_types [unit.trsp_prop.trgt_type].name;
				/* spt, mov, ini, rng */
				str+="\nTrans. Spotting:\t" + unit.trsp_prop.spt;
				str+="\nTrans. Movement:\t" + unit.trsp_prop.mov;
				str+="\nTrans. Initiative:\t" + unit.trsp_prop.ini;
				str+="\nTrans. Range::\t" + unit.trsp_prop.rng;
				/* attack & defense */
				for (int i = 0; i < DB.UnitLib.trgt_type_count; i++) {
					str+="\n " + DB.UnitLib.trgt_types [i].name + " Trans. Attack:\t" + unit.trsp_prop.atks [i];
				}
				str+="\nTrans. Ground Defense:\t" + unit.trsp_prop.def_grnd;
				str+="\nTrans. Air Defense:\t" + unit.trsp_prop.def_air;
				str+="\nTrans. Close Defense:\t" + unit.trsp_prop.def_cls;
			}
			/* show */
			GUILayout.Box (str);
		}
		if (GUILayout.Button ("OK"))
			windowType = 0;
	}
	
	private void OnClick(){
		if (Input.GetMouseButtonDown (0) && Engine.map.isLoaded
			&& Engine.phase==PHASE.PHASE_NONE) {//TODO_RR if (Input.GetTouch(0).phase == TouchPhase.Began)){
			int mapx, mapy;
			Engine.REGION region;
			Vector3 clickedPosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			if (Engine.engine_get_map_pos (hitSelected.transform.position.x, 
										   hitSelected.transform.position.z, out mapx, 
										   out mapy, out region, clickedPosition.z)) {
				if (Engine.cur_unit != null) {
					/* handle current unit */
					if (!(Engine.cur_unit.x == mapx && Engine.cur_unit.y == mapy &&
                            Engine.engine_get_prim_unit (mapx, mapy, region) == Engine.cur_unit)) {
						Unit unit = Engine.engine_get_target (mapx, mapy, region);
						if (unit != null) {
							Action.action_queue_attack(Engine.cur_unit, unit);
						} else {
							if (Engine.map.mask [mapx, mapy].in_range != 0 && !Engine.map.mask [mapx, mapy].blocked) {
								Action.action_queue_move (Engine.cur_unit, mapx, mapy);					
							} else {
								if (Engine.map.mask [mapx, mapy].sea_embark) {
									if (Engine.cur_unit.embark == UnitEmbarkTypes.EMBARK_NONE)
										Action.action_queue_embark_sea (Engine.cur_unit, mapx, mapy);
									else
										Action.action_queue_debark_sea (Engine.cur_unit, mapx, mapy);
									Engine.engine_backup_move (Engine.cur_unit, mapx, mapy);
									Engine.draw_map = true;
									
								} else {
									unit = Engine.engine_get_select_unit (mapx, mapy, region);
									if (unit != null && Engine.cur_unit != unit) {
										if (Engine.cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN) {
											if (Engine.engine_capture_flag (Engine.cur_unit)) { 
#if TODO_RR
		si ha acabado saltar al nuevo menu
#endif
												/* CHECK IF SCENARIO IS FINISHED */
												if (Scenario.scen_check_result (false)) {
													Engine.engine_finish_scenario ();
													return;
												}              
											}
										}
										Engine.engine_select_unit (unit);
										Engine.engine_clear_backup ();
										Engine.engine_update_info (mapx, mapy, region);
										Engine.draw_map = true;
									}
								}
							}
						}
					}
				} else {
					Unit unit = Engine.engine_get_select_unit (mapx, mapy, region);
					if (unit != null && Engine.cur_unit != unit) {
						/* select unit */
						Engine.engine_select_unit (unit);
						Engine.engine_update_info (mapx, mapy, region);
						Engine.draw_map = true;
#if WITH_SOUND
						wav_play( terrain_icons.wav_select );
#endif
					}
				}
				
			}
		}
		else
			return;
		if (Engine.draw_map) {
			Draw ();
		}
	}
	
	private void OnMouseMove(){
		int xS, yS;
		Engine.REGION aux_region;
		ray = Camera.main.ScreenPointToRay (Input.mousePosition); //ray = Camera.main.ScreenPointToRay(touch.position);
		if (Physics.Raycast (ray, out hit, 100) && hit.transform.tag!="Edge") {
			Engine.engine_get_map_pos(hit.transform.position.x,
									  hit.transform.position.z,out xS,out yS,
									  out aux_region,Input.mousePosition.z);
			if (xS == x && yS == y)
				return;
			x = xS;
			y = yS;
			hitSelected = hit;
			unitAtk = Engine.engine_get_target (x, y, aux_region);
 			if (unitAtk != null) {
 				if (Engine.showCross == null || (Engine.showCross.x != x || Engine.showCross.y != y)) {
 					Engine.showCross = new Engine.MapPoint ();
 					Engine.showCross.x = x;
 					Engine.showCross.y = y;
 					Draw ();
 				}
 			} else {
 				if (Engine.showCross != null) {
 					Engine.showCross = null;
 					Draw ();
 				}
 			}
		}
	}
	
	
	public static void Draw ()
	{
		if (Engine.map.isLoaded) {
			Engine.status = STATUS.STATUS_NONE;
			bool use_frame = (Engine.cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU);
			GUIMap.Repaint(Engine.map,use_frame);
		}
		Engine.draw_map = false;
	}
	
	void Update() {
		OnMouseMove ();
		OnClick();
	}
}
