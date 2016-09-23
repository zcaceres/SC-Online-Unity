using UnityEngine;
using System.Collections;

public class FireHydrantController : MonoBehaviour
{

	// Use this for initialization
	void OnTriggerEnter (Collider playerCollider)
	{
		Player p = playerCollider.GetComponent<Player> ();
		if (p != null) {
			GameObject hose = p.gameObject.transform.Find ("MainCamera").gameObject.transform.Find ("Hose").gameObject;
			playerCollider.GetComponent<Hose> ().activated = true;
			hose.SetActive (true); //Turns on the hose child PF in the player object
			p.message = "Fire hydrant activated";
		}
	}

	void OnTriggerExit (Collider playerCollider)
	{
		Player p = playerCollider.GetComponent<Player> ();
		if (p != null) {
			GameObject hose = p.gameObject.transform.Find ("MainCamera").gameObject.transform.Find ("Hose").gameObject;
			playerCollider.GetComponent<Hose> ().activated = false;
			hose.SetActive (false);
			p.message = "Fire hydrant deactivated.";
		}
	}
		
}
