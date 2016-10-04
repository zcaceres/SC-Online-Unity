using UnityEngine;
using System.Collections;

/// <summary>
/// Handles collision detection using Kinematic OnTrigger behaviour 
/// of colliders that map onto existing (non-kinematic) box colliders.
/// </summary>
public class VehicleCollisionDetect : MonoBehaviour {
	private Rigidbody vehicleRigidbody;
	private UnityStandardAssets.Vehicles.Car.CarController CarC;
	private Vehicle parentVehicle;
	private float damageThreshold = 1f;

	void Start () {
		parentVehicle = GetComponentInParent<Vehicle> ();
		CarC = parentVehicle.GetComponent<UnityStandardAssets.Vehicles.Car.CarController> ();
		vehicleRigidbody = parentVehicle.gameObject.GetComponent<Rigidbody> ();
	}

	void OnTriggerEnter (Collider other) {
		if (other.GetComponent<Rigidbody> () != null) {
			if (CarC.CurrentSpeed > damageThreshold) {
				/*Damages car for each integer above the damage threshold when
				it collides with another object, using CurrentSpeed for comparison*/
				parentVehicle.damageObject((int)(CarC.CurrentSpeed));
			}
		}
	}

}
