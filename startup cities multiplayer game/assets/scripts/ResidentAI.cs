using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//namespace UnityStandardAssets.Characters.ThirdPerson
//{
    [RequireComponent(typeof (NavMeshAgent))]
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class ResidentAI : NetworkBehaviour
    {
        public NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
		private Resident residentController;
		private MonthManager monthManager;
		private Criminal criminalController;
		private static ResidentManager rm;
		public Transform destinationWayPoint;


        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();
			residentController = gameObject.GetComponent<Resident> ();
			monthManager = GameObject.Find ("Clock").GetComponent<MonthManager> ();
			criminalController = gameObject.GetComponent<Criminal> ();
			rm = GameObject.Find ("ResidentManager").GetComponent<ResidentManager> ();
			//Randomizes the walk speed for residents!
			if (isServer) {
				if (criminalController == null) {
					GetResidentPoints (monthManager.isDay); //Sets resident movement locations
					agent.speed = 0.5f; //Sets resident speed
				} else {
					GoCommitCrime (); //Get Crime Waypoints
					agent.speed = 0.8f; //Make criminal walk faster
				}
				agent.updateRotation = false;
				agent.updatePosition = true;
			}
        }


        private void Update()
        {
			if (isServer) {
				if (target != null) {
					agent.SetDestination (target.position);
				}
				if (agent.remainingDistance > agent.stoppingDistance) {
					character.Move (agent.desiredVelocity, false, false);
				} else {
					character.Move (Vector3.zero, false, false);
				}
			}
        }


        public void SetTarget(Transform target)
        {
            this.target = target;
        }

		public void GoCommitCrime ()
		{
			Transform[] crimePoints = GetCrimeWaypoints ().ToArray ();
			int loiterPointRoll = UnityEngine.Random.Range (0, rm.loiterPoints.Length - 1);
			if (criminalController != null) {
				if (crimePoints.Length == 0) { //No buildings to rob, going to loiter!
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform;
					SetTarget (destinationWayPoint);
				} else { //There are buildings to rob, I'm going to rob one.
					destinationWayPoint = crimePoints [UnityEngine.Random.Range (0, crimePoints.Length - 1)].transform; //Get a random crime point
					SetTarget (destinationWayPoint);
				}
			}
		}


		/// <summary>
		/// Gets the resident points. Called when time of day, residency, or work info changes
		/// </summary>
		public void GetResidentPoints (bool isDay)
		{
			Building homeBuilding = residentController.residenceBuilding;
			Building workBuilding = residentController.jobBuilding;
			int residentRoll = UnityEngine.Random.Range (0, 10);
			int loiterPointRoll = UnityEngine.Random.Range (0, rm.loiterPoints.Length - 1);
			//Resident is employed and has a home!
			if (residentController.employed () && residentController.residenceBuilding != null) {
				if (residentController.jobBuilding != null) {
					if (UnityEngine.Random.Range (0f, 10f) > 5.0f) {
						if (isDay) {
							destinationWayPoint = residentController.jobBuilding.transform.Find ("wayPoint").transform; // GO TO WORK
						} else {
							destinationWayPoint = residentController.residenceBuilding.transform.Find ("wayPoint").transform; //GO HOME
						}
					} else {
						if (isDay) {
							destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
						} else {
							destinationWayPoint = residentController.residenceBuilding.transform.Find ("wayPoint").transform; //GO HOME
						}
					}
					SetTarget (destinationWayPoint);
				}
			}
		//Resident is employed but is HOMELESS
		else if (residentController.employed () && residentController.residenceBuilding == null) {
				if (residentController.jobBuilding != null) {
					if (UnityEngine.Random.Range (0f, 10f) > 5.0f) {
						if (isDay) {
							destinationWayPoint = residentController.jobBuilding.transform.Find ("wayPoint").transform; //GO TO WORK
						} else {
							destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
						}
					} else {
						if (isDay) {
							destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
						} else {
							destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
						}
					}
					SetTarget (destinationWayPoint);
				}
			}
		//Resident is not employed and is HOMELESS
		else if (!residentController.employed () && residentController.residenceBuilding == null) {
				if (isDay) {
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
				} else {
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
				}
				SetTarget (destinationWayPoint);
			}

		//Resident is NOT employed but has a home!
		else if (!residentController.employed () && residentController.residenceBuilding != null) {
				if (isDay) {
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
				} else {
					destinationWayPoint = residentController.residenceBuilding.transform.Find ("wayPoint").transform; //GO HOME
				}
				SetTarget (destinationWayPoint);
			} 
		//Default option. Normally outside of else block. Takes waypoints which are set as initial random array
		else {
				if (isDay) {
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
				} else {
					destinationWayPoint = rm.loiterPoints [loiterPointRoll].transform; //GO LOITER
				}
				SetTarget (destinationWayPoint);
			}
		RpcSetActive (true); //Enables the resident
		}

	public void ReachedDestination () {
		if (isServer) {
			if (criminalController != null) {
				criminalController.TriggerCrimeEvent ();
				Destroy (gameObject);
			} else {
				RpcSetActive (false);
			}
		}
	}


	/// <summary>
	/// Networks the active state of this gameObject.
	/// </summary>
	/// <param name="active">If set to <c>true</c> active.</param>
	[ClientRpc]
	void RpcSetActive (bool active /*ResidentRoll used to be here*/ ) {
		gameObject.SetActive (active);
		Renderer[] rends = gameObject.GetComponentsInChildren<Renderer> ();
		AudioSource[] audios = gameObject.GetComponents<AudioSource> ();
		if (active) {
			//if (residentRoll > RESIDENT_VISIBILITY) {
			//	} else {
			gameObject.GetComponent<Rigidbody> ().isKinematic = false;
			gameObject.GetComponent<Collider> ().enabled = true;
			foreach (Renderer r in rends) {
				r.enabled = true;
			}
			foreach (AudioSource a in audios) {
				a.enabled = true;
			}
		} else {
			gameObject.GetComponent<Rigidbody> ().isKinematic = true;
			gameObject.GetComponent<Collider> ().enabled = false;
			foreach (Renderer r in rends) {
				r.enabled = false;
			}
			foreach (AudioSource a in audios) {
				a.enabled = false;
			}
		}
	}


	/// <summary>
	/// Utility function for getting all buildings that are targets for crime in the city
	/// </summary>
	/// <returns>The crime waypoints.</returns>
	/// <param name="criminalsTransform">The transform of the criminal's parent object (a wayPoint where he was spawned).</param>
	public List<Transform> GetCrimeWaypoints() {
		GameObject[] crimeWaypoints = GameObject.FindGameObjectsWithTag ("wayPoint");
		List<Transform> crimeTransforms = new List<Transform> ();
		foreach (GameObject go in crimeWaypoints) {
			if (go.transform.parent.GetComponent<Building> () != null) {
				if (go.transform.parent.GetComponent<Building> ().validOwner () && go.transform.parent.GetComponent<Building>().isOccupied()) {
					//Checks to make sure building has a valid owner and is occupied to rob it
					crimeTransforms.Add (go.transform);
				}
			}
		}
		return crimeTransforms;
	}
	}
//}
