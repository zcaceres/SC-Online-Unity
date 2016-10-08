using System;
using UnityEngine;
using UnityEngine.Networking;

public class Hose : NetworkBehaviour
{
	public float maxPower = 30;
	public float minPower = 5;
	public float changeSpeed = 5;
	public ParticleSystem[] hoseWaterSystems;
	public Renderer systemRenderer;
	private float localPower;
	private float maxOrMin;
	public bool activated;

	[SyncVar (hook = "SetPower")]
	private float m_Power;

	void Start() 
	{
		if (isLocalPlayer) {
			CmdChangeWaterPressure (minPower);
		}
	}

	// Update is called once per frame
	private void Update ()
	{
		
		if (activated) { //Set by FireHydrant class to enable water stream
			if (isLocalPlayer) { //Prevents server  control of client players
				if (Input.GetMouseButtonDown (0)) {
					CmdChangeWaterPressure (maxPower);
				} else if (Input.GetMouseButtonUp (0)) {
					CmdChangeWaterPressure (minPower);
				}
			}
			if (isServer) {
				m_Power = Mathf.Lerp (m_Power, maxOrMin, Time.deltaTime * changeSpeed); //Lerps between max and min val by time
				foreach (var system in hoseWaterSystems) { //Updates all child object particle systems of hose object (child of player)
					system.startSpeed = m_Power;
					var emission = system.emission;
					emission.enabled = (m_Power > minPower * 1.1f);
				}
			}
		}
	}

	[Command] //Sends new float across network to synchronize whether lerp goes towards max or min value
	void CmdChangeWaterPressure (float power)
	{
		maxOrMin = power;
	}


	/// <summary>
	/// SyncVar hook function that propagates changes in particle system across the server
	/// </summary>
	/// <param name="power">new float value of water pressure power m_Power</param>
	void SetPower (float power)
	{
		m_Power = power;
		foreach (var system in hoseWaterSystems) {
			system.startSpeed = m_Power;
			var emission = system.emission;
			emission.enabled = (m_Power > minPower * 1.1f);
		}
	}

}