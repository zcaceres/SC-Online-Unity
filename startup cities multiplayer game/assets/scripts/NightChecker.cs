using UnityEngine;
using System.Collections;

public class NightChecker : MonoBehaviour {
	// Use this for initialization
	void Start () {
		MonthManager mm = GameObject.FindObjectOfType<MonthManager> ();
		int weather = mm.weatherType;
		TurnOn (weather);
	}

	void TurnOn (int weather) {
		if (weather <= 1) {
			GetComponent<Light> ().enabled = false;
		} else {
			GetComponent<Light> ().enabled = true;
		}

	}


}
