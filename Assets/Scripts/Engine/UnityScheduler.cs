using UnityEngine;
using System.Collections;
using System;

#region Delegate type: SendTimerDelegate

public delegate void SendTimerDelegate ();

#endregion

public class UnityScheduler /*: MonoBehaviour */
{
	
	private bool isrunning = false;
	private float time;
	private float runningTime;
	private SendTimerDelegate callback;
	private float pollingInterval;

	public void CheckUpdate (float deltaTime)
	{
		if (isrunning && callback != null) { // revisar la lógica
			if (runningTime < time)
				runningTime += deltaTime;
			else { // revisar la lógica
				callback ();
				callback = null;
				runningTime = 0;
				//quitar el callback
				//resetear tiempo y revisar otra logica similar
			}
		}
	}
	
	public bool IsRunning {
		get{ return isrunning;}
	}
	
	public float PollingInterval{
		get{return pollingInterval;}
		set{pollingInterval = value;}
	}
	
	public float RunningTime{
		get{return runningTime;}
	}
	public void Start ()
	{
		if (!isrunning)
			isrunning = true;
	}
	
	public void Stop ()
	{
		isrunning = false;
	}
	
	public void Clear ()
	{
		time = 0;
		runningTime = 0;
		isrunning = false;
		callback = null;
	}
	
	public void Add (int count, float millisecondsTimeout, SendTimerDelegate method)
	{
		time = millisecondsTimeout;
		callback = method;
	}
	
	public void Dispose(){
		this.Clear();
	}
	
}
