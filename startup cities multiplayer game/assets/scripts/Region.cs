using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CityHall))]

public class Region : NetworkBehaviour {
	
	public struct NetId {
		public NetworkInstanceId id;
	}

	public class SyncListNetId : SyncListStruct<NetId> {	
		public void addId(NetworkInstanceId id) {
			NetId w;
			w.id = id;
			this.Add (w);
		}

		public void removeId(NetworkInstanceId id) {
			List<NetId> toRemove = this.Where (w => (w.id == id)).ToList ();
			if (toRemove.Count > 0) {
				this.Remove (toRemove[0]);
			}
		}
	}

	private SyncListNetId regionalObjects = new SyncListNetId();
	public CityHall cityHall; // city hall which governs this area
	[SyncVar]
	public string regionName;
	// Use this for initialization
	void Start () {
		cityHall = GetComponent<CityHall> ();
		if (string.IsNullOrEmpty (regionName)) {
			regionName = NameGen ();
		}
	}

	public void AddItem(NetworkInstanceId n) {
		regionalObjects.addId (n);
	}

	public void RemoveItem(NetworkInstanceId n) {
		regionalObjects.removeId (n);
	}

	public void AddItem(OwnableObject n) {
		regionalObjects.addId (n.netId);
	}

	public void RemoveItem(OwnableObject n) {
		regionalObjects.removeId (n.netId);
	}

	/// <summary>
	/// Gets the region objects as a list of ownable objects.
	/// </summary>
	/// <returns>The region objects.</returns>
	public List<OwnableObject> GetRegionObjects() {
		List<OwnableObject> objects = new List<OwnableObject> ();
		foreach (NetId n in regionalObjects) {
			objects.Add (GetLocalInstance (n.id).GetComponent<OwnableObject> ());
		}
		return objects;
	}

	public void MessageOwner(string s) {
		if (isServer) {
			if (cityHall.getOwner () > -1) {
				cityHall.RpcMessageOwner (s);
			}
		}
	}

	/// <summary>
	/// Sets the owner for all junk/decorative items in the region so that the current city hall owner has authority to remove them.
	/// </summary>
	/// <param name="p">Player.</param>
	public void SetOwner(Player p) {
		if (isServer) {
			List<OwnableObject> objects = GetRegionObjects ();

			foreach (OwnableObject o in objects) {
				if (!o.validLot () && (o is Decoration || o is Junk)) {
					o.setOwner (p.netId);
				}
			}
		}
	}

	public void UnsetOwner() {
		if (isServer) {
			List<OwnableObject> objects = GetRegionObjects ();

			foreach (OwnableObject o in objects) {
				if (!o.validLot () && (o is Decoration || o is Junk)) {
					o.unsetOwner ();
				}
			}
		}
	}

	protected string NameGen() {
		bool mononym = false;
		string s;

		if (Random.value < .7f) {
			mononym = true;
		}

		if (mononym) {
			string[] name = {"Springfield", "York", "Salisbury", "Alexandria", "Chester", "Douglas", "Greenville", "Washington", "Pleasantville", "Boston", "Mechanicsburg", "Rome",
				"London", "Atlanta", "Bristol", "Harrisburg", "Nottingham", "Lancaster", "Vegas", "Berlin", "Dublin", "Stockholm", "Smyrna", "Wyatt", "Victoria", "Jackson"
			};
			s = name[(int)Random.Range(0, name.Length)];
		} else {
			string[] lName = { "North", "South", "East", "West", "Douglas", "Ocean", "Mountain", "Golden", "Pleasant", "Imperial", "Grand"};
			string[] rName = { "City", "Town", "Village", "Borough", "Municipality", "Plains", "Woods" };
			s = lName [(int)Random.Range (0, lName.Length)] + " " + rName [(int)Random.Range (0, rName.Length)];; 
		}
		return s;
	}

	protected GameObject GetLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}
}
