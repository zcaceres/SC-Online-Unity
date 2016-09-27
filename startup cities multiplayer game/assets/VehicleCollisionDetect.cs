using UnityEngine;
using System.Collections;

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
				parentVehicle.damageCar((int)(CarC.CurrentSpeed));
			}

			//Algo for collision damage here!!
		}
	}

}
