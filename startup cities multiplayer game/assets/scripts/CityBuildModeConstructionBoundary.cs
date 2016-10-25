using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent (typeof(BoxCollider))]
public class CityBuildModeConstructionBoundary : ConstructionBoundary
{

	/// <summary>
	/// Raises the trigger enter event and checks if rigidbody colliding has a lot attached so that it can send
	/// isConstructable bool to player class
	/// </summary>
	/// <param name="other">Kinematic rigidbody on the lot</param>
	protected override void OnTriggerEnter (Collider other)
	{
		if (other.CompareTag("floor") || other.CompareTag("terrain")) {
			constructionChecker += 1;
		}
	}
		
	/// <summary>
	/// Decrements the constructionChecker int when a constructionboundary leaves the limits of the lot
	/// </summary>
	protected override void OnTriggerExit (Collider other)
	{
		if (other.CompareTag("floor") || other.CompareTag("terrain")) {
			constructionChecker -= 1;
		}
		if (constructionChecker < 0) {
			constructionChecker = 0;
		}
		isConstructable = false;
	}
}
