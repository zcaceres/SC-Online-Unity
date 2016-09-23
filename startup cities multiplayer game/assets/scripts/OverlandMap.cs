using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OverlandMap : MonoBehaviour {
	private Camera overlandMapCam;
	private Camera playerMainCamera;
	private GameObject tooltip;
	private GameObject beacon;
	private GameObject arrow;
	private int beaconBuilding;
	//private Player localPlayer;

	// Use this for initialization
	void Start () {
		overlandMapCam = gameObject.GetComponent<Camera> ();
		beaconBuilding = -1; // building id of building which has a beacon over it. -1 = no beacons
	}

	void Update() {
		if (overlandMapCam.enabled) {
			if (Input.GetButtonDown ("Fire1")) {
				if (tooltip != null) {
					Destroy (tooltip);
				}
				Ray ray;
				Vector3 m = Input.mousePosition;
				RaycastHit hit;
				ray = overlandMapCam.ScreenPointToRay (m);
				if (Physics.Raycast (ray, out hit)) {
					Building b = hit.collider.GetComponent<Building> ();
					if (b != null) {
						tooltip = (GameObject)Instantiate (Resources.Load ("TenantTooltip"), new Vector3(m.x, m.y - 15, m.z), Quaternion.identity);
						tooltip.transform.SetParent (GameObject.Find ("Canvas").transform, true);
						tooltip.transform.Find("Text").GetComponent<Text> ().text = getMapReadout(b);
					}
				}
			}

			if (Input.GetButtonDown ("Fire2")) {
				Ray ray;
				Vector3 m = Input.mousePosition;
				RaycastHit hit;
				ray = overlandMapCam.ScreenPointToRay (m);
				if (Physics.Raycast (ray, out hit)) {
					Building b = hit.collider.GetComponent<Building> ();
					if (b != null) {
						makeBeacon (b);
					}
				}
			}
		}
	}
	public void EnableOverlandMap () {
		overlandMapCam.enabled = true;
	
	}

	public void DisableOverlandMap() {
		overlandMapCam.enabled = false;
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}

	/// <summary>
	/// Spawns a beacon over the building
	/// </summary>
	/// <param name="b">The building.</param>
	public void makeBeacon(Building b) {
		if (beacon != null) {
			if (arrow != null) {
				Destroy (arrow);
			}
			Destroy (beacon);
		}
		if (b.id != beaconBuilding) {
			beacon = (GameObject)Instantiate (Resources.Load ("Beacon"), b.transform.position, Quaternion.identity);
			arrow = (GameObject)Instantiate (Resources.Load ("Pointer"));
			arrow.transform.SetParent(FindObjectOfType<Player>().localPlayer.transform.Find("MainCamera").transform, false);
			arrow.GetComponent<DestinationPointer> ().waypoint = beacon.transform;
			beaconBuilding = b.id;
		} else { // Current beacon is active on this building. Beacon was destroyed, reset the beaconBuilding id to -1 (which means no building currently has a beacon)
			beaconBuilding = -1;
		}
	}

	/// <summary>
	/// Points to the destination with an arrow without spawning a beacon
	/// </summary>
	/// <param name="b">The building to be pointed to.</param>
	public void makeWaypoint(Building b) {
		if (arrow != null) {
			Destroy (arrow);
		}
		Destroy (beacon);

		if (b.id != beaconBuilding) {
			arrow = (GameObject)Instantiate (Resources.Load ("Pointer"));
			arrow.transform.SetParent(FindObjectOfType<Player>().localPlayer.transform.Find("MainCamera").transform, false);
			arrow.GetComponent<DestinationPointer> ().waypoint = b.transform;
			beaconBuilding = b.id;
		} else { // Current waypoint is active on this building. waypoint was destroyed, reset the beaconBuilding id to -1 (which means no building currently has a waypoint)
			beaconBuilding = -1;
		}
	}

	public string getMapReadout(Building b) {
		string s = b.buildingName + "\n" + b.typeName;
		if (b.getOwner () > -1) {
			s += "\n<color=#" + ColorUtility.ToHtmlStringRGBA(b.getPlayerOwner().color) + ">" + b.getPlayerOwner().getName() + "</color>";
		}
		if (b.fire) {
			s += "\n<color=#ff0000ff>On Fire</color>";
		}
		if (b.occupied) {
			s += "\nTenant: " + b.tenant.resident.residentName;
			if (b.tenant.resident.criminal) {
				s += " <color=#ff0000ff>(Criminal)</color>";
			}
		} else {
			s += "\nNo Tenant";
		}
		return s;
	}
}
