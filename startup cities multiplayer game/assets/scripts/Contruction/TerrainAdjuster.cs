using UnityEngine;
using System.Collections;

public class TerrainAdjuster : MonoBehaviour {

	public Player p;
	int count;

	void Update() {
		count++;
		if (count > 50) {
			Destroy (this.gameObject);
		}
	}

	void OnTriggerEnter(Collider c) {
		Debug.Log (c.name);
		Lot l = c.GetComponent<Lot> ();
		if (l != null && l.ownedBy(p.netId)) {
			p.GetComponent<ConstructionController> ().SetTerrainHeight (this.transform.position);
		}
	}
}
