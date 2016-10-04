using UnityEngine;
using System.Collections;
using UnityStandardAssets.Vehicles.Car;

/// <summary>
/// Handles collision detection using Kinematic OnTrigger behaviour 
/// of colliders that map onto existing (non-kinematic) box colliders.
/// </summary>
public class VehicleCollisionDetect : MonoBehaviour {
	private Rigidbody vehicleRigidbody;
	private CarController carC;
	private Vehicle parentVehicle;
	private int damageThreshold;

	void Start () {
		parentVehicle = GetComponentInParent<Vehicle> ();
		carC = parentVehicle.GetComponent<CarController> ();
		vehicleRigidbody = parentVehicle.gameObject.GetComponent<Rigidbody> ();
		damageThreshold = parentVehicle.getVehicleToughness(); //Gets vehicle toughness from vehicle (or child) class
	}

	void OnTriggerEnter (Collider other) {
		if (other.GetComponent<Rigidbody> () != null) {
			if (carC.CurrentSpeed > damageThreshold) {
				/*Damages car for each integer above the damage threshold when
				it collides with another object, using CurrentSpeed for comparison*/
				int damage = (int)(carC.CurrentSpeed - damageThreshold);
				parentVehicle.damageObject(damage);
			}
		}
	}

}
