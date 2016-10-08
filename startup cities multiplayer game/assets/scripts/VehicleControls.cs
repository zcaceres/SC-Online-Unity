using System;
using UnityEngine.Networking;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Car;

public class VehicleControls : NetworkBehaviour
{
	private Player driver; //the local player driving the car

	//Toggled from Vehicle Class
	void OnEnable () {
		driver = GetComponent<Player> ();
	}


	private void FixedUpdate ()
	{
		if (!isLocalPlayer) {
			return;
		}
			
		if (gameObject.transform.parent != null) { //means that player has entered a vehicle
			float h = CrossPlatformInputManager.GetAxis("Horizontal");
			float v = CrossPlatformInputManager.GetAxis("Vertical");
			float handbrake = CrossPlatformInputManager.GetAxis ("Jump");
			if (isServer) {
				driver.currentVehicle.Move (h, v, v, handbrake); //Server driving
			} else {
				driver.CmdDrive (h, v, v, handbrake); //Client driving
				}
			}
		}





}
