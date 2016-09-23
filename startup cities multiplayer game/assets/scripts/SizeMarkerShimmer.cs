using UnityEngine;
using System.Collections;

public class SizeMarkerShimmer : MonoBehaviour
{
	float alpha;
	Color color;
	bool opaque;
	GameObject parentlot;
	// Use this for initialization
	void Start ()
	{
		//Gets rotation and position from parent lot and sets overlay cube to match
		parentlot = gameObject.transform.parent.gameObject;
		gameObject.transform.position = parentlot.transform.position;
		gameObject.transform.rotation = parentlot.transform.rotation;
		//Scale may need to be changed for lots depending on their size....
		gameObject.transform.localScale = new Vector3 (10, .1f, 10);
		
		alpha = .01f; //controls speed of flashing

		//Resets renderer to zero opacity
		color = gameObject.GetComponent<Renderer> ().material.color;
		color.a = 0;
		gameObject.GetComponent<Renderer> ().material.color = color;

		opaque = true;
	}
	
	// Update is called once per frame
	void Update ()
	{
		//Toggles opacity up and downward
		if (opaque) {
			if (gameObject.GetComponent<MeshRenderer> ().material.color.a < .5f) {
				color.a += alpha;
				gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", color);
			} else {
				opaque = false;
			}
		}

		if (!opaque) {
			if (gameObject.GetComponent<Renderer> ().material.color.a > .01f) {
				color.a -= alpha;
				gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", color);
			} else {
				opaque = true;
			}
		}
	}
}
