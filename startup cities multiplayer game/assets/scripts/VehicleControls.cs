using System;
using UnityEngine.Networking;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Car;

public class VehicleControls : NetworkBehaviour
{
	CarController m_Car;

	//Toggle from entervehicle class
	void OnEnable () {
		m_Car = transform.parent.GetComponent<CarController> ();
	}
		
	//OnDisable reassign m_Car to null?


	private void FixedUpdate ()
	{
		if (!isLocalPlayer) {
			return;
		}

		if (gameObject.transform.parent != null) { //means that player has entered a vehicle
			//if (m_Car != null) {
			//}
			float h = CrossPlatformInputManager.GetAxis("Horizontal");
			float v = CrossPlatformInputManager.GetAxis("Vertical");
			float handbrake = CrossPlatformInputManager.GetAxis ("Jump");
			if (isServer) {
				m_Car.Move (h, v, v, handbrake);
			} else {
				if (m_Car == null) {
					transform.parent.GetComponent<CarController> ();
				} else {
					CmdDrive (h, v, v, handbrake);
				}
			}
		}
	}


	[Command]
	void CmdDrive (float h, float v, float ve, float hb) {
		Debug.LogError (m_Car);
		m_Car.Move (h, v, ve, hb);
	}



}
