using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for road connections. Put on ROAD prefabs. Handles snapping for road building
/// </summary>
public class RoadConnector : MonoBehaviour {
	private Transform[] roadConnectorTransforms; 

	//Find all RoadConnectors on start of server
	void Start () {
		roadConnectorTransforms = FindAllConnectorTransforms ();
	}

	/// <summary>
	/// Finds all connector transforms. Used to allow roads with different number of intersections to snap
	/// </summary>
	/// <returns>The all connector transforms.</returns>
	private Transform[] FindAllConnectorTransforms() {
		List<Transform> transList = new List<Transform> ();
		Transform connectors = transform.Find ("RoadConnectors");
		foreach (Transform t in connectors) {
				transList.Add (t);
			}
		return transList.ToArray();
	}


	/// <summary>
	/// Getter function to return array of all the roadconnector transforms on the object in an array
	/// </summary>
	/// <returns>The road connector transforms.</returns>
	public Transform[] GetRoadConnectorTransforms () {
		return roadConnectorTransforms;
	}
		

}
