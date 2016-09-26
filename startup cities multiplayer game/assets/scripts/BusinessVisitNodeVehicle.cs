using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class BusinessVisitNodeVehicle : MonoBehaviour
{
	VehicleBusiness veh;
	// Use this for initialization
	void Start ()
	{
		veh = GetComponentInParent<VehicleBusiness> ();
	}

	void OnTriggerEnter (Collider residentCollider)
	{
		if (veh.isServer) {
			Resident resident = residentCollider.GetComponent<Resident> ();
			if (resident != null) {
				veh.visitBusiness (resident);
			}
		}
	}
}
