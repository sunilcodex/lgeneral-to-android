using UnityEngine;
using System.Collections;
using EngineA;
using Miscellaneous;

public class Movement : MonoBehaviour
{
	
	private bool mapLoaded = false;
	private int width;
	private int height;
	
	private void onClick ()
	{
		if (Input.GetMouseButtonDown (0)) {
			Vector3 clickedPosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			int x;
			int y;	
			Engine.REGION region;
			if (Engine.engine_get_map_pos (clickedPosition.x, clickedPosition.z, out x, out y, out region)) {
				if (Engine.cur_unit != null) {
					Unit unit = Engine.engine_get_target (x, y, region);
					if (unit != null) {
#if TODO_RR
						meter accion de atacar
#endif
						print ("ataca!!");
					} else if (Engine.map.mask [x, y].in_range != 0 && !Engine.map.mask [x, y].blocked) {
#if TODO_RR
						falta el codigo de mover la unidad
#endif
						print ("me tengo que mover");
					} else if (Engine.map.mask [x, y].sea_embark) {
						if (Engine.cur_unit.embark == UnitEmbarkTypes.EMBARK_NONE)
							print ("me tengo que embarcar");
						else
							print ("tengo que desembarcarme");
#if TODO_RR
						Engine.engine_backup_move (Engine.cur_unit, x, y);

					falta repintar todo el mapa con la seleccion que hemos hecho
#endif
					}
					
#if TODO_RR
					falta poner codigo y repintarla
#endif
				} else {
					
					Unit unit = Engine.engine_get_select_unit (x, y, region);
					if (unit != null && Engine.cur_unit != unit) {
						Engine.engine_select_unit (unit);
						Engine.engine_update_info (x, y, region);
						print (x + " " + y + " " + region);
					}
#if TODO_RR
					falta repintar todo el mapa con la seleccion que hemos hecho
#endif
				}
				
				
			}	
		}
	}
	// Use this for initialization
	void Start ()
	{
		mapLoaded = Engine.map.isLoaded;
		/*if (mapLoaded){
			Engine.cur_player = Player.players_get_first();
		}*/
	}
	
	// Update is called once per frame
	void Update ()
	{
			
#if TODO_RR
			//falta el codigo de pasar a tactil
#endif
		if (mapLoaded) {
			onClick ();
		}
			
	}
}
