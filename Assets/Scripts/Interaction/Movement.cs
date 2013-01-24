using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;

public class Movement : MonoBehaviour
{
	
	private Ray ray;
	private RaycastHit hit;
	private RaycastHit hitSelected;
	private int width;
	private int height;
	
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
                            	//TODO_RR Action.action_queue_move(Engine.cur_unit, mapx, mapy);
								print ("Move");
                            }
							else{
								if (Engine.map.mask[mapx, mapy].sea_embark)
                                {
									print ("Embarcar...");
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
		ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100))
		{
			if (hitSelected.transform == null){
				hit.transform.gameObject.renderer.material.color = new Color(0,1,0,0.5F);
				hitSelected = hit;
			}
			else{
				hitSelected.transform.gameObject.renderer.material.color = new Color(1,1,1,0.5F);
				hit.transform.gameObject.renderer.material.color = Color.yellow;//new Color(0,1,0,0.5F);
				hitSelected = hit;
			}
					
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
}
