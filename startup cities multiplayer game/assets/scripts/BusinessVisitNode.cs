using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class BusinessVisitNode : MonoBehaviour {
	Business biz;
	// Use this for initialization
	void Start ()
	{
		biz = GetComponentInParent<Business> ();
	}

	void OnTriggerEnter (Collider residentCollider)
	{
		if (biz.isServer) {
			if (biz.occupied && !biz.ruin) {
				Resident resident = residentCollider.GetComponent<Resident> ();
				if (resident != null && (resident.jobBuilding == null || biz.id != resident.jobBuilding.id)) {
					biz.visitBusiness (resident);
				}
			}
		}
	}
}
