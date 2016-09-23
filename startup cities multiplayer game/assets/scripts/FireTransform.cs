using UnityEngine;
using System.Collections;

public class FireTransform : MonoBehaviour {
	public bool onFire; //communicates to Building class whether the area of the building represented by this transform is on fire

	void Start () {
		onFire = false; // buildings not on fire by default!
	}

}
