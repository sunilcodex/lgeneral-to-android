using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;
using AI_Enemy;
using DataFile;

public class Movement : MonoBehaviour
{
	
	private Ray ray;
	private RaycastHit hit;
	private RaycastHit hitSelected;
	private int width;
	private int height;
	private static int widthInfo = 250;
	private int windowType = 0;
	public static int WidthInfo{
		get{return widthInfo;}
	}
	private int x,y;
	
	private void OnClick ()
	{
		if (Input.GetMouseButtonDown (0)) {//TODO_RR if (Input.GetTouch(0).phase == TouchPhase.Began)){
			int mapx, mapy;
            Engine.REGION region;
			Vector3 clickedPosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			if (Engine.engine_get_map_pos (hitSelected.transform.position.x, 
										   hitSelected.transform.position.z, out mapx, 
										   out mapy, out region, clickedPosition.z)){
				if (Engine.cur_unit != null)
                {
					/* handle current unit */
                    if (!(Engine.cur_unit.x == mapx && Engine.cur_unit.y == mapy &&
                            Engine.engine_get_prim_unit(mapx, mapy, region) == Engine.cur_unit))
					{
						Unit unit = Engine.engine_get_target(mapx, mapy, region);
						if (unit != null)
                        {
                        	//TODO_RR Action.action_queue_attack(Engine.cur_unit, unit);
							print ("Attack");
                        }
						else{
							if (Engine.map.mask[mapx, mapy].in_range != 0 && !Engine.map.mask[mapx, mapy].blocked)
                            {
                            	//Action.action_queue_move(Engine.cur_unit, mapx, mapy);
								print ("Move");
                            }
							else{
								if (Engine.map.mask[mapx, mapy].sea_embark)
                                {
									if (Engine.cur_unit.embark == UnitEmbarkTypes.EMBARK_NONE)
										Action.action_queue_embark_sea(Engine.cur_unit, mapx, mapy);
									else
                                        Action.action_queue_debark_sea(Engine.cur_unit, mapx, mapy);
                                    Engine.engine_backup_move(Engine.cur_unit, mapx, mapy);
									Engine.draw_map = true;
									
								}
								else
                                {
									unit = Engine.engine_get_select_unit(mapx, mapy, region);
									if (unit != null && Engine.cur_unit != unit)
                                    {
										if (Engine.cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                                        {
											if (Engine.engine_capture_flag(Engine.cur_unit))
                                            { 
												 /* CHECK IF SCENARIO IS FINISHED */
                                      			if (Scenario.scen_check_result(false))
                                                {
                                                	Engine.engine_finish_scenario();
                                                    return;
                                                }              
                                            }
										}
										Engine.engine_select_unit(unit);
										Engine.engine_clear_backup();
										Engine.engine_update_info(mapx, mapy, region);
										Engine.draw_map = true;
									}
								}
							}
						}
					}
				}
				else{
					Unit unit = Engine.engine_get_select_unit(mapx, mapy, region);
					if (unit != null && Engine.cur_unit != unit)
                    {
						/* select unit */
                        Engine.engine_select_unit(unit);
						Engine.engine_update_info(mapx, mapy, region);
						Engine.draw_map = true;
#if WITH_SOUND
						wav_play( terrain_icons.wav_select );
#endif
					}
				}
				
			}
		}
	}
	
	private void OnMove(){
		Color hover = Color.yellow;
		Color fogHover = new Color(0.3f,0.3f,0.3f,1);
		int xS,yS;
		ray = Camera.main.ScreenPointToRay(Input.mousePosition); //ray = Camera.main.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out hit, 100))
		{
			Engine.engine_get_screen_pos(hit.transform.position.x,hit.transform.position.z,out x,out y);
			if (!Engine.map.mask[x,y].fog){
				hit.transform.renderer.material.color = hover;
				if (hitSelected.transform!=null){
					Engine.engine_get_screen_pos(hitSelected.transform.position.x,hitSelected.transform.position.z,out xS,out yS);
					if (Engine.cur_unit!=null && Engine.cur_unit.x==xS && Engine.cur_unit.y==yS){
						hitSelected.transform.renderer.material.color = Color.green;
					}
					else if (xS==x && yS==y){
						hitSelected.transform.renderer.material.color = hover;
					}
					else if (!Engine.map.mask[xS,yS].fog){
						hitSelected.transform.renderer.material.color = Color.white;
					}
					else{
						hitSelected.transform.renderer.material.color = new Color(0.7f,0.7f,0.7f,1);;
					}
					
				}
				hitSelected = hit;
			}
			else{
				hit.transform.renderer.material.color = fogHover;
				if (hitSelected.transform!=null){
					Engine.engine_get_screen_pos(hitSelected.transform.position.x,hitSelected.transform.position.z,out xS,out yS);
					if (Engine.cur_unit!=null && Engine.cur_unit.x==xS && Engine.cur_unit.y==yS){
						hitSelected.transform.renderer.material.color = Color.green;
					}
					else if (xS==x && yS==y){
						hitSelected.transform.renderer.material.color = fogHover;
					}
					else if (!Engine.map.mask[xS,yS].fog){
						hitSelected.transform.renderer.material.color = Color.white;
					}
					else{
						hitSelected.transform.renderer.material.color = new Color(0.7f,0.7f,0.7f,1);;
					}
				}
				hitSelected = hit;
			}
			//hitSelected = hit;
			/*if (hitSelected.transform == null){
				Engine.engine_get_screen_pos(hit.transform.position.x,hit.transform.position.z,out x,out y);
				if (Engine.map.mask[x,y].fog){
					hit.transform.gameObject.renderer.material.color = fogHover;
				}
				else{
					hit.transform.gameObject.renderer.material.color = hover;
				}
                hitSelected = hit;
            }
            else{
				Engine.engine_get_screen_pos(hitSelected.transform.position.x,hitSelected.transform.position.z,out x,out y);
				if (Engine.map.mask[x,y].fog){
					hitSelected.transform.gameObject.renderer.material.color = Color.grey;
				}
				else if (Engine.cur_unit!=null && Engine.cur_unit.x==x && Engine.cur_unit.y==y){
					hitSelected.transform.gameObject.renderer.material.color = Color.green;
				}
				else{
					hitSelected.transform.gameObject.renderer.material.color = new Color(1,1,1,0.5F);
				}
                Engine.engine_get_screen_pos(hit.transform.position.x,hit.transform.position.z,out x,out y);
				if (Engine.map.mask[x,y].fog){
					hit.transform.gameObject.renderer.material.color = fogHover;
				}
				else{
					hit.transform.gameObject.renderer.material.color = hover;
				}
            	hitSelected = hit;
            }*/
		}
	}
	// Use this for initialization
	void Start ()
	{
	}
	
	// Update is called once per frame
	void Update ()
	{
		OnMove();
		OnClick();
	}
	
	void OnGUI(){
		if (Engine.map.isLoaded){
			GUI.Window (windowType, new Rect (Screen.width-widthInfo, 0, widthInfo, Screen.height), ConfigWindow, "");
		}
	}
	
	void ConfigWindow(int windowID){
		switch(windowID){
			case 0:
				GeneralConfig();
				break;
			case 1:
				SupplyUnitsGUI();
				break;
			case 2:
				DeployUnitsGUI();
				break;
			case 3:
				VictCondGUI();
				break;
			case 4:
				EndTurnGUI();
				break;
			case 5:
				ShowCUnitInfoGUI();
				break;
		}		
		
	}
	
	private bool isVisibleUnit(int x, int y){
		foreach(Unit unit in Scenario.vis_units){
			if (unit.x==x && unit.y==y)
				return true;
		}
		return false;
	}
	
	private void GeneralConfig(){
		/*string movement = "Movement: ";
		string spotting = "Spotting: ";
		string range = "Range: ";
		string initiative = "Initiative: ";
		string softat = "Soft At.: ";*/
		string turns = "Turn " +Scenario.turn +" of " + Scenario.scen_info.turn_limit;
		string prestige = "Prestige: "+ Player.players_get_first().prestige;
		string weather = "Weather:  " +
                    ((Scenario.turn < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes[Scenario.scen_get_weather()].name:"");
		string forecast = "Forecast: " +
                    ((Scenario.turn + 1 < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes[Scenario.scen_get_forecast()].name : "");
		GUI.Label(new Rect(83,25,75,25),turns);
		GUI.Label(new Rect(83,50,100,25),prestige);
		GUI.Label(new Rect(83,75,100,25),weather);
		GUI.Label(new Rect(83,100,100,25),forecast);
		if (GUI.Button(new Rect(15,125,90,25),"Supply Units")){
			windowType=1;
		}
		if (GUI.Button(new Rect(145,125,90,25),"Deploy Units")){
			windowType=2;
		}
		if(GUI.Button(new Rect(15,155,90,25),"Vict. Cond")){
			windowType = 3;
		}
		if (GUI.Button(new Rect(145,155,90,25),"End Turn")){
			windowType = 4;
		}
		string current_info =""; 
		//Unit_Lib_Entry unit_info = null;
		if (Engine.cur_unit!=null){
			current_info = Engine.cur_unit.name+"\nFuel:"+
						   Engine.cur_unit.cur_fuel+" Ammo:"+
						   Engine.cur_unit.cur_ammo+" Ent:"+Engine.cur_unit.entr;
			/*string name = Unit.DeleteOrdinal(Engine.cur_unit.name);
			unit_info = DB.UnitLib.unit_lib_find_by_name(name);
			movement+=unit_info.mov+"("+DB.UnitLib.mov_types[unit_info.mov_type].name+")";
			spotting+=unit_info.spt;
			range+=unit_info.rng;
			initiative+=unit_info.ini;
			softat+=unit_info.atks[0];*/
		}
		if (GUI.Button (new Rect(62.5f,185,125,25),"Show C. Unit Info")){
			windowType = 5;
		}
		GUI.Box (new Rect(37.5f,215,175,40),current_info);
		string hover_info ="";
		string terrain_info="";
		if (hitSelected.transform!=null){
			terrain_info = Engine.map.map[x,y].name+"("+x+","+y+")";
			if (Engine.map.map[x,y].a_unit!=null && isVisibleUnit(x,y)){
				hover_info = Engine.map.map[x,y].a_unit.name+"\nFuel:"+
						   	 Engine.map.map[x,y].a_unit.cur_fuel+" Ammo:"+
						   	 Engine.map.map[x,y].a_unit.cur_ammo+" Ent:"+Engine.map.map[x,y].a_unit.entr;
				/*Unit_Lib_Entry unit_info_hover = DB.UnitLib.unit_lib_find_by_name(Unit.DeleteOrdinal(Engine.map.map[x,y].a_unit.name));
				movement = getMovOfUnitHover(unit_info_hover,movement);
				spotting = getSpotOfUnitHover(unit_info_hover, spotting);
				range = getRangeOfUnitHover(unit_info_hover, range);
				initiative = getIniOfUnitHover(unit_info_hover, initiative);*/
				
			}
			if (Engine.map.map[x,y].g_unit!=null && isVisibleUnit(x,y)){
				hover_info = Engine.map.map[x,y].g_unit.name+"\nFuel:"+
						   	 Engine.map.map[x,y].g_unit.cur_fuel+" Ammo:"+
						   	 Engine.map.map[x,y].g_unit.cur_ammo+" Ent:"+Engine.map.map[x,y].g_unit.entr;
				/*Unit_Lib_Entry unit_info_hover = DB.UnitLib.unit_lib_find_by_name(Unit.DeleteOrdinal(Engine.map.map[x,y].g_unit.name));
				movement = getMovOfUnitHover(unit_info_hover,movement);
				spotting = getSpotOfUnitHover(unit_info_hover, spotting);
				range = getRangeOfUnitHover(unit_info_hover, range);
				initiative = getIniOfUnitHover(unit_info_hover, initiative);*/
			}
		}
		GUI.Box (new Rect(37.5f,260,175,40),hover_info);
		GUI.Label (new Rect(83,300,200,20),terrain_info);
		GUI.Label(new Rect(15,320,200,20),"Expected losses");
		/*GUI.Label(new Rect(25,290,235,20),movement);
		GUI.Label(new Rect(25,310,235,20),spotting);
		GUI.Label(new Rect(25,330,235,20),range);
		GUI.Label(new Rect(25,350,235,20),initiative);
		GUI.Label(new Rect(25,370,235,20),softat);*/
	}
	
	private void SupplyUnitsGUI(){
		if (GUILayout.Button("OK"))
			windowType = 0;
	}
	
	private void DeployUnitsGUI(){
		if (GUILayout.Button("OK"))
			windowType = 0;
	}
	
	private void VictCondGUI(){
		if (GUILayout.Button("OK"))
			windowType = 0;
	}
	
	private void EndTurnGUI(){
		if (GUILayout.Button("OK"))
			windowType = 0;
	}
	
	private void ShowCUnitInfoGUI(){
		if (GUILayout.Button("OK"))
			windowType = 0;
	}
	/*private string getMovOfUnitHover(Unit_Lib_Entry unit_info_hover, string movement){
		if (movement.Equals("Movement: ")){
			return movement+="\t\t\t\t"+unit_info_hover.mov+"("+DB.UnitLib.mov_types[unit_info_hover.mov_type].name+")";
		}
		else{
			return movement+="\t"+unit_info_hover.mov+"("+DB.UnitLib.mov_types[unit_info_hover.mov_type].name+")";
		}
	}
	private string getSpotOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Spotting: ")){
			return spotting+="\t\t\t\t\t"+unit_info_hover.spt;
		}
		else{
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.spt;
		}
	}
	
	private string getRangeOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Range: ")){
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.rng;
		}
		else{
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.rng;
		}
	}
	
	private string getIniOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Initiative: ")){
			return spotting+="\t\t\t\t\t"+unit_info_hover.rng;
		}
		else{
			return spotting+="\t\t\t\t\t"+unit_info_hover.rng;
		}
	}*/
}
