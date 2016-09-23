using UnityEngine;
using System.Collections;


/// <summary>
/// Used for PATHS like sidewalks or roads.
/// Checks to make sure a path is connected to another path so that
/// single chunks of paths, which are used for AI, cannot be built outside
/// of a path network.
/// Overrides typical ConstructionBoundary on dummy objects.
/// </summary>
public class PathBoundary : ConstructionBoundary {

	void Start () {
		color = gameObject.GetComponent<MeshRenderer> ().materials[0].color;
		constructionChecker = 0;
		mr = gameObject.GetComponent<MeshRenderer> ();
		scaffold = gameObject.GetComponentInChildren<Scaffolding> ();
	}
	
	/// <summary>
	/// Raises the trigger enter event and checks if rigidbody colliding has a lot attached so that it can send
	/// isConstrucable bool to player class
	/// </summary>
	/// <param name="other">Kinematic rigidbody on the lot</param>
	void OnTriggerEnter (Collider other) {
		if (other.CompareTag("floor")) {
			constructionChecker += 1;
		} 
	}

	//This is called so that the constructionboundary checker is constantly updated while a dummy object is in play
	void Update() {
		CheckConstructionStatus ();
	}

	/// <summary>
	/// Checks the construction status of the dummy path. Makes sure that at least one of the ends of the path
	/// is colliding with another path (road, sidewalk etc).
	/// Also uses 'scaffolding' child game object on dummy to make sure that path is not colliding with a building or character
	/// </summary>
	protected override void CheckConstructionStatus ()
	{
		if (constructionChecker > 0) { 
			if (!scaffold.colliding) {
				isConstructable = true;
			} else {
				isConstructable = false;
			}
		} else {
			isConstructable = false;
		}
	}

	/// <summary>
	/// Decrements the constructionChecker int when a path boundary
	/// no longer collides with another path object (tagged "floor")
	/// </summary>
	void OnTriggerExit (Collider other) {
		if (other.CompareTag("floor")) {
			constructionChecker -= 1;
		}
		if (constructionChecker < 0) {
			constructionChecker = 0;
		}
	}
}
