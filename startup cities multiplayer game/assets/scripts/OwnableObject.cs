using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class OwnableObject : NetworkBehaviour {
	const int ATTRACTIVENESS_EFFECT = 0;

	[SyncVar]
	public int cost;
	[SyncVar]
	protected int baseCost;
	[SyncVar]
	protected NetworkInstanceId owner;
	[SyncVar]
	public NetworkInstanceId lot;
	[SyncVar]
	public bool notForSale;

	public Lot localLot;
	// Use this for initialization
	void Start () {
		if (isServer) {
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
			}
		}
	}

	public virtual int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	/// <summary>
	/// Returns the data associated with the object
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadout(NetworkInstanceId pid) {
		string s;
		string ownerName = "";
		GameObject l = getLocalInstance (lot);
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "\nOwner: " + ownerName + "\nPrice: " + cost;

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}

		if (l != null) {
			Lot tmp = l.GetComponent<Lot> ();
			s += "\nAttractiveness: " + tmp.getAttractiveness ();
		}
		return s;
	}

	/// <summary>
	/// Returns the data associated with the object, does not do anything with buttons
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadoutText(NetworkInstanceId pid) {
		string s;
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else  {
			ownerName = getPlayer(owner).getName();
		}
		s = "\nOwner: " + ownerName + "\nPrice: " + cost;

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}

		GameObject l = getLocalInstance (lot);
		if (l != null) {
			Lot tmp = l.GetComponent<Lot> ();
			s += "\nAttractiveness: " + tmp.getAttractiveness ();
		}
		return s;
	}


	/// <summary>
	/// returns the owner ID (player number) or -1 if unowned
	/// </summary>
	/// <returns>The owner ID.</returns>
	public int getOwner() {
		int id;

		if (!validOwner()) {
			id = -1;
		} else {
			id = getPlayer(owner).id;
		}

		return id;
	}

	/// <summary>
	/// Sets the owner and removes the object from the owned list of its previous owner.
	/// </summary>
	/// <param name="newOwner">New owner's id.</param>
	public virtual void setOwner(NetworkInstanceId newOwner) {
		if (newOwner == owner)
			return;
		Player oldOwner = getPlayerOwner ();
		if (oldOwner != null) {
			oldOwner.owned.removeId (this.netId);
		}
		Player p = getLocalInstance (newOwner).GetComponent<Player> ();
		p.owned.addId (this.netId);
		owner = newOwner;
	}

	/// <summary>
	/// Messages the owner.
	/// </summary>
	/// <param name="s">Message.</param>
	public void messageOwner(string s) {
		if (isServer) {
			if (validOwner()) {
				getPlayer(owner).message = s;
			}
		}
	}

	/// <summary>
	/// Messages the owner without using the message syncvar
	/// </summary>
	/// <param name="s">message string.</param>
	[ClientRpc]
	public void RpcMessageOwner (string s)
	{
		if (validOwner ()) {
			getPlayer (owner).showMessage (s);
		}
	}

	/// <summary>
	/// Gives the owner money.
	/// </summary>
	/// <param name="money">amount of money.</param>
	public void giveOwnerMoney(int money) {
		if (isServer) {
			if (validOwner()) {
				getPlayer(owner).budget += money;
			}
		}
	}

	public virtual bool validOwner() {
		bool isValid = false;
		if (!owner.IsEmpty() && (owner != NetworkInstanceId.Invalid) && (getLocalInstance(owner) != null)) {
			isValid = true;
		}
		return isValid;
	}

	public virtual bool validLot() {
		bool isValid = false;
		if (!lot.IsEmpty() && (lot != NetworkInstanceId.Invalid) && (getLocalInstance(lot) != null)) {
			isValid = true;
		}
		return isValid;
	}

	protected GameObject getLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}

	/// <summary>
	/// gives the base cost of the object (cost before modifiers/condition changes/rent changes)
	/// </summary>
	public virtual int appraise() {
		return baseCost;
	}

	public virtual Player getPlayer(NetworkInstanceId playerId) {
		GameObject tmp = getLocalInstance (playerId);
		Player p;
		if (tmp != null) {
			p = tmp.GetComponent<Player> ();
		} else {
			p = null;
		}
		return p;
	}

	public virtual NetworkInstanceId getOwnerNetId() {
		return owner;
	}

	public virtual Player getPlayerOwner() {
		Player p;
		if (validOwner()) {
			p = getPlayer (owner);
		} else {
			p = null;
		}
		return p;
	}

	public virtual Lot getLot() {
		Lot l;

		if (this is Lot) {
			l = this.GetComponent<Lot> ();
		} else {
			if (validLot ()) {
				l = getLocalInstance (lot).GetComponent<Lot> ();
			} else {
				l = null;
			}
		}
		return l;
	}

	public virtual Neighborhood getNeighborhood() {
		Neighborhood n = null;
		if (validLot ()) {
			n = getLot ().getNeighborhood ();
		}
		return n;
	}

	public virtual bool inNeighborhood() {
		bool b = false;
		if (validLot ()) {
			if (getLot ().inNeighborhood ()) {
				b = true;
			}
		}
		return b;
	}

	/// <summary>
	/// Checks ownership by netId
	/// </summary>
	/// <returns><c>true</c>, if owned by the object (company or player) whose netId was passed, <c>false</c> otherwise.</returns>
	/// <param name="o">Owner.</param>
	public virtual bool ownedBy(NetworkInstanceId o) {
		bool owned = false;
		if (validOwner()) {
			if (owner == o) {
				owned = true;
			}
		}
		return owned;
	}

	public virtual bool ownedBy(Player p) {
		bool owned = false;
		if (owner == p.netId) {
			owned = true;
		}
		return owned;
	}

	public virtual void unsetOwner() {
		owner = NetworkInstanceId.Invalid;
	}

	protected virtual void updateNeighborhoodValue() {
		if (validLot ()) {
			Neighborhood n = getLot ().getNeighborhood ();
			if (n != null) {
				n.calcPrice ();
			}
		}
	}

	public int getAttractiveness() {
		int a = 0;
		GameObject tmp = getLocalInstance (lot);
		if (tmp != null) {
			Lot l = tmp.GetComponent<Lot> ();
			a += l.getAttractiveness ();
		}
		return a;
	}

	public virtual bool isDestructable() {
		return true;
	}

	public virtual int getCost() {
		return cost;
	}

	public virtual int getBaseCost() {
		return baseCost;
	}

	/// <summary>
	/// Returns the highest point of the object's mesh.
	/// </summary>
	/// <returns>Highest point.</returns>
	public float getHighest()
	{
		Collider c = gameObject.GetComponent<Collider> ();
		if ((c != null) && (c.gameObject.GetComponent<MeshCollider>() != null) && (c.gameObject.GetComponent<MeshCollider>().sharedMesh != null)) {
			Vector3[] verts = c.gameObject.GetComponent<MeshCollider> ().sharedMesh.vertices;
			Vector3 topVertex = new Vector3 (0, float.NegativeInfinity, 0);
			for (int i = 0; i < verts.Length; i++) {
				Vector3 vert = transform.TransformPoint (verts [i]);
				if (vert.y > topVertex.y) {
					topVertex = vert;
				}
			}

			return topVertex.y;
		} else {
			return 0;
		}
	}

	/// <summary>
	/// City reclaims the object and pays the player the base cost
	/// </summary>
	public virtual void repo() {
		if (validOwner ()) {
			Player p = getPlayerOwner ();
			p.budget += this.appraise ();
			p.owned.removeId (this.netId);
			owner = NetworkInstanceId.Invalid;
			notForSale = false;
		}
	}
}