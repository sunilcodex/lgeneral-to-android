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
					print ("Seleccionado");
				}
				else{
					Unit unit = Engine.engine_get_select_unit(mapx, mapy, region);
					print (unit.name);
				}
				
			}
#if TODO_RR
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
#endif
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
				hit.transform.gameObject.renderer.material.color = new Color(0,1,0,0.5F);
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
