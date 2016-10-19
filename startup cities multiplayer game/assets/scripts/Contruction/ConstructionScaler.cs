using UnityEngine;
using System.Collections;

/// <summary>
/// Scales the object until all box colliders are hitting the terrain
/// </summary>
public class ConstructionScaler : MonoBehaviour {

	BoxCollider[] colliders;
	int hitTerrainCount;
	// Use this for initialization
	void Start () {
		colliders = gameObject.GetComponents<BoxCollider> ();
		hitTerrainCount = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (hitTerrainCount < colliders.Length) {
			gameObject.transform.localScale = new Vector3 (gameObject.transform.localScale.x, gameObject.transform.localScale.y + 5, gameObject.transform.localScale.z);
			gameObject.transform.position = new Vector3 (gameObject.transform.position.x, gameObject.transform.position.y - 2.5f, gameObject.transform.position.z);
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