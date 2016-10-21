using UnityEngine;
using System.Collections;

public class ConstructionScalerOneUse : MonoBehaviour {

	BoxCollider[] colliders;
	int hitTerrainCount;
	// Use this for initialization
	void Start () {
		colliders = gameObject.GetComponents<BoxCollider> ();
		foreach (BoxCollider c in colliders) {
			c.gameObject.transform.Rotate (-c.transform.eulerAngles.x, 0, 0);
		}
		hitTerrainCount = 0;
	}

	// Update is called once per frame
	void Update () {
		if (hitTerrainCount < colliders.Length) {
			gameObject.transform.localScale = new Vector3 (gameObject.transform.localScale.x, gameObject.transform.localScale.y + 5f, gameObject.transform.localScale.z);
			gameObject.transform.localPosition = new Vector3 (gameObject.transform.localPosition.x, gameObject.transform.localPosition.y - 2.5f, gameObject.transform.localPosition.z);
		} else {
			Destroy (this);
		}
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.gameObject.CompareTag ("terrain")) {
			hitTerrainCount++;
		}
	}

	void OnTriggerExit(Collider other) {
		if (other.gameObject.CompareTag ("terrain")) {
			hitTerrainCount--;
		}
	}
}
