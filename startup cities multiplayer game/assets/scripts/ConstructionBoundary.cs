using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent (typeof(BoxCollider))]
public class ConstructionBoundary : MonoBehaviour
{

	//Array that can flexibly adapt to as many 'boundary' vertices/edges as we wish for a building
	protected BoxCollider[] boundaries;
	//Scaffolding is child game object on dummy which includes box colliders to prevent buildings from being too close to each other
	public Scaffolding scaffold;
	public Lot triggerLot;
	//This is the bool checked by the Player class to see whether the building can be constructed
	public bool isConstructable;
	//This int compares the number of collisions that have occurred on the boundary vertices against all possible boundaries for the building
	protected int constructionChecker;
	protected Color color;
	protected Player owner;
	protected MeshRenderer mr;

	// Use this for initialization
	void Start ()
	{
		color = gameObject.GetComponent<MeshRenderer> ().materials [0].color;
		boundaries = GetComponents<BoxCollider> ();
		constructionChecker = 0;
		mr = gameObject.GetComponent<MeshRenderer> ();
		scaffold = gameObject.GetComponentInChildren<Scaffolding> ();
	}


	/// <summary>
	/// Raises the trigger enter event and checks if rigidbody colliding has a lot attached so that it can send
	/// isConstructable bool to player class
	/// </summary>
	/// <param name="other">Kinematic rigidbody on the lot</param>
	void OnTriggerEnter (Collider other)
	{
		if (other.GetComponent<Lot> () != null) {
			constructionChecker += 1;
			triggerLot = other.GetComponent<Lot> ();
		}
	}

	//This is called so that the constructionboundary checker is constantly updated while a dummy object is in play
	void Update ()
	{
		CheckConstructionStatus ();
	}



	/// <summary>
	/// Checks the construction status of the dummy building. Compares corner colliders with quantity of total colliders on the building.
	/// Also uses 'scaffolding' child game object on dummy to make sure that building is not colliding with another building or character
	/// </summary>
	protected virtual void CheckConstructionStatus ()
	{
		if (constructionChecker == boundaries.Length) { //All vertices on lot
			if (!scaffold.colliding) { //Not colliding with anything
				isConstructable = true;
			} else {
				isConstructable = false;
			}
		} else {
			isConstructable = false;
		}
	}



	/// <summary>
	/// Decrements the constructionChecker int when a constructionboundary leaves the limits of the lot
	/// </summary>
	protected virtual void OnTriggerExit (Collider other)
	{
		if (other.GetComponent<Lot> () != null) {
			constructionChecker -= 1;
			triggerLot = null;
		}
		if (constructionChecker < 0) {
			constructionChecker = 0;
		}
		isConstructable = false;
	}


	/// <summary>
	/// Turns the dummy green.
	/// </summary>
	public void turnGreen ()
	{
		if (mr.materials [0].color != Color.green) {
			mr.materials [0].color = Color.green;
		}
	}


	/// <summary>
	/// Turns the dummy red.
	/// </summary>
	public void turnRed ()
	{
		if (mr.materials [0].color != Color.red) {
			mr.materials [0].color = Color.red;
		}
	}


	/// <summary>
	/// Resets the dummy's color.
	/// </summary>
	public void resetColor ()
	{
		mr.materials [0].color = color;
	}
}
