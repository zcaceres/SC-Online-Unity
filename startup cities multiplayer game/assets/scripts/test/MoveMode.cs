// MoveTo.cs
using UnityEngine;
using System.Collections;

public class MoveMode : MonoBehaviour {

	public Transform goal;
	private Transform oldGoal;

	void Start () {
		NavMeshAgent agent = GetComponent<NavMeshAgent>();
		agent.destination = goal.position; 
		oldGoal = goal;
	}

	void Update() {
		if (oldGoal != goal) {
			NavMeshAgent agent = GetComponent<NavMeshAgent>();
			agent.destination = goal.position; 
			oldGoal = goal;
		}
	}
}