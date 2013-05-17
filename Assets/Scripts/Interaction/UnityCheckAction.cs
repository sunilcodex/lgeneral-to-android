using UnityEngine;
using System.Collections;
using EngineApp;

public class UnityCheckAction : MonoBehaviour {
	
	// Update is called once per frame
	void FixedUpdate () {
		Engine.stateMachine.Scheduler.CheckUpdate(Time.deltaTime);	
	}
	
}
