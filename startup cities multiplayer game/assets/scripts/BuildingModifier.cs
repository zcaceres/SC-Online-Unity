using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BuildingModifier : NetworkBehaviour {

	public struct Mod {
		public float rentModifier;
		public float conditionModifier;
		public float safetyModifier;
		public float radius;

		public string modName;
		public int costToRemove;
		public int icon;
	}
		
	public class SyncListMod : SyncListStruct<Mod> {}

	public SyncListMod mods = new SyncListMod();
	private static Sprite[] icons;
	private Building building;
	public List<GameObject> buttons = new List<GameObject> ();

	// Use this for initialization
	void Start () {
		if (icons == null) {
			icons = Resources.LoadAll<Sprite> ("Icons and Portraits/64 flat icons/png/32px");
		}
		building = gameObject.GetComponent<Building> ();
	}

	/// <summary>
	/// Adds a rent only modifier
	/// </summary>
	/// <param name="Name">Name.</param>
	/// <param name="modifier">Rent modifier.</param>
	/// <param name="rad">effect radius.</param>
	public void addRentMod(string Name, float modifier, float rad, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = modifier;
			m.conditionModifier = 1;
			m.safetyModifier = 1;
			m.radius = rad;
			m.costToRemove = 0;
			m.icon = icon;
			modBomb (m);
		}
	}

	/// <summary>
	/// Adds a condition only modifier
	/// </summary>
	/// <param name="Name">Name.</param>
	/// <param name="modifier">condition modifier.</param>
	/// <param name="rad">effect radius.</param>
	public void addConditionMod(string Name, float modifier, float rad, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = 1;
			m.conditionModifier = modifier;
			m.safetyModifier = 1;
			m.radius = rad;
			m.costToRemove = 0;
			m.icon = icon;
			modBomb (m);
		}
	}

	/// <summary>
	/// Adds a safety only modifier
	/// </summary>
	/// <param name="Name">Name.</param>
	/// <param name="modifier">safety modifier.</param>
	/// <param name="rad">effect radius.</param>
	public void addSafetyMod(string Name, float modifier, float rad, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = 1;
			m.conditionModifier = 1;
			m.safetyModifier = modifier;
			m.radius = rad;
			m.costToRemove = 0;
			m.icon = icon;
			modBomb (m);
		}
	}

	/// <summary>
	/// Adds a modifier which may effect all 3 fields
	/// </summary>
	/// <param name="Name">Mod name.</param>
	/// <param name="r">The rent modifier</param>
	/// <param name="c">Condition modifier</param>
	/// <param name="s">Safety modifier</param>
	/// <param name="rad">Radius.</param>
	public void addMod(string Name, float r, float c, float s, float rad, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = r;
			m.conditionModifier = c;
			m.safetyModifier = s;
			m.radius = rad;
			m.costToRemove = 0;
			m.icon = icon;
			modBomb (m);
		}
	}

	/// <summary>
	/// Adds the mod on the calling object only: no bomb effect
	/// </summary>
	/// <param name="Name">Mod name.</param>
	/// <param name="r">The rent modifier</param>
	/// <param name="c">Condition modifier</param>
	/// <param name="s">Safety modifier</param>
	/// <param name="rad">Radius.</param>
	public void addModOnMe(string Name, float r, float c, float s, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = r;
			m.conditionModifier = c;
			m.safetyModifier = s;
			m.radius = 0;
			m.costToRemove = 0;
			m.icon = icon;
			mods.Add (m);
		}
	}

	/// <summary>
	/// Adds the starting malus on the calling object: special type of mod which can be bought off
	/// </summary>
	/// <param name="Name">Mod name.</param>
	/// <param name="r">The rent modifier</param>
	/// <param name="c">Condition modifier</param>
	/// <param name="s">Safety modifier</param>
	/// <param name="rad">Radius.</param>
	/// <param name="cost">Price to remove.</param>
	public void addMalus(string Name, float r, float c, float s, int cost, int icon) {
		if (isServer) {
			Mod m;
			m.modName = Name;
			m.rentModifier = r;
			m.conditionModifier = c;
			m.safetyModifier = s;
			m.radius = 0;
			m.costToRemove = cost;
			m.icon = icon;
			mods.Add (m);
		}
	}

	/// <summary>
	/// Adds the starting malus on the calling object: special type of mod which can be bought off
	/// Unique version: no duplicate maluses
	/// </summary>
	/// <param name="Name">Mod name.</param>
	/// <param name="r">The rent modifier</param>
	/// <param name="c">Condition modifier</param>
	/// <param name="s">Safety modifier</param>
	/// <param name="rad">Radius.</param>
	/// <param name="cost">Price to remove.</param>
	public void addMalusUnique(string Name, float r, float c, float s, int cost, int icon) {
		if (isServer) {
			
			bool duplicate = false;
			foreach (Mod mod in mods) {
				if (mod.modName == Name) {
					duplicate = true;
					break;
				}
			}

			if (!duplicate) {
				Mod m;
				m.modName = Name;
				m.rentModifier = r;
				m.conditionModifier = c;
				m.safetyModifier = s;
				m.radius = 0;
				m.costToRemove = cost;
				m.icon = icon;
				mods.Add (m);
			}
		}
	}

	/// <summary>
	/// For adding mods which should not duplicate on buildings
	/// </summary>
	/// <param name="Name">Mod name.</param>
	/// <param name="r">The rent modifier</param>
	/// <param name="c">Condition modifier</param>
	/// <param name="s">Safety modifier</param>
	/// <param name="rad">Radius.</param>
	public void addModUnique(string Name, float r, float c, float s, float rad, int icon) {
		if (isServer) {				
			Mod m;
			m.modName = Name;
			m.rentModifier = r;
			m.conditionModifier = c;
			m.safetyModifier = s;
			m.radius = rad;
			m.costToRemove = 0;
			m.icon = icon;
			modBombUnique (m);
		}
	}

	/// <summary>
	/// Hits all buildings in the passed mod's radius with the effect.
	/// </summary>
	/// <param name="mod">Mod.</param>
	public void modBomb(Mod mod) {
		Collider[] colliding = Physics.OverlapSphere(building.c.transform.position, mod.radius);
		foreach (Collider hit in colliding) {
			BuildingModifier bm = hit.GetComponent<BuildingModifier> ();

			if (bm != null) {
				bm.mods.Add (mod);
			}
		}
	}

	/// <summary>
	/// Hits all buildings in the radius with a unique mod--will not duplicate
	/// </summary>
	/// <param name="mod">Mod.</param>
	public void modBombUnique(Mod mod) {
		Collider[] colliding = Physics.OverlapSphere(building.c.transform.position, mod.radius);
		foreach (Collider hit in colliding) {
			BuildingModifier bm = hit.GetComponent<BuildingModifier> ();
			if (bm != null) {
				
				bool duplicate = false;
				foreach (Mod currentMod in bm.mods) {
					if (mod.modName == currentMod.modName ) {
						duplicate = true;
						break;
					}
				}
					if (!duplicate) {
					bm.mods.Add (mod);
					}
			}
		}
	}

	/// <summary>
	/// Returns the Name/Rent/Condition/Safety modifiers as a string
	/// for use in the building readout
	/// </summary>
	public string readout(NetworkInstanceId pid) {
		/*string s = "";
		bool doOnce = true;
		foreach (Mod m in mods) {
			if (doOnce) {
				s += "\nModifiers: ";
				doOnce = false;
			}

			s += "\n\t" + m.modName;
			if (m.rentModifier > 1) {
				s += "\n\t +" + ((m.rentModifier - 1) * 100).ToString ("n2") + "% Rent";
			} else if (m.rentModifier < 1) {
				s += "\n\t -" + ((1 - m.rentModifier) * 100).ToString ("n2") + "% Rent";
			}

			if (m.conditionModifier > 1) {
				s += "\n\t +" + ((m.conditionModifier - 1) * 100).ToString ("n2") + "% Condition";
			} else if (m.conditionModifier < 1) {
				s += "\n\t -" + ((1 - m.conditionModifier) * 100).ToString ("n2") + "% Condition";
			}

			if (m.safetyModifier > 1) {
				s += "\n\t +" + ((m.safetyModifier - 1) * 100).ToString ("n2") + "% Safety";
			} else if (m.safetyModifier < 1) {
				s += "\n\t -" + ((1 - m.safetyModifier) * 100).ToString ("n2") + "% Safety";
			}
		}*/
		string s = "";
		if (mods.Count > 0) {
			s = "\nModifiers: ";
		}
		generateButtons (pid);
		return s;
	}

	public string modToString(Mod m) {
		
		string s = m.modName;
		if (m.rentModifier > 1) {
			s += "\n\t +" + ((m.rentModifier - 1) * 100).ToString ("n2") + "% Rent";
		} else if (m.rentModifier < 1) {
			s += "\n\t -" + ((1 - m.rentModifier) * 100).ToString ("n2") + "% Rent";
		}

		if (m.conditionModifier > 1) {
			s += "\n\t +" + ((m.conditionModifier - 1) * 100).ToString ("n2") + "% Condition";
		} else if (m.conditionModifier < 1) {
			s += "\n\t -" + ((1 - m.conditionModifier) * 100).ToString ("n2") + "% Condition";
		}

		if (m.safetyModifier > 1) {
			s += "\n\t +" + ((m.safetyModifier - 1) * 100).ToString ("n2") + "% Safety";
		} else if (m.safetyModifier < 1) {
			s += "\n\t -" + ((1 - m.safetyModifier) * 100).ToString ("n2") + "% Safety";
		}

		return s;
	}

	/// <summary>
	/// Applies this mod's maluses or bonuses to the building
	/// </summary>
	public void apply() {
		if (building == null) {
			building = GetComponent<Building> ();
		}
		foreach (Mod m in mods) {
			if (m.rentModifier > 1)
				building.rent += (int)(building.baseRent *( m.rentModifier - 1));
			else if (m.rentModifier < 1)
				building.rent -= (int)(building.baseRent * ( 1 - m.rentModifier));
			if (m.conditionModifier > 1)
				building.condition += (int)(building.condition * (m.conditionModifier - 1 ));
			else if (m.conditionModifier < 1)
				building.condition -= (int)(building.condition * (1 - m.conditionModifier));
			
			if (m.safetyModifier > 1)
				building.safety += (int)(building.safety * (m.safetyModifier - 1));
			else if (m.safetyModifier < 1)
				building.safety -= (int)(building.safety * (1 - m.safetyModifier));
		}

		// Don't let any of these values go negative
		if (building.safety < 0)
			building.safety = 0;
		if (building.rent < 0)
			building.rent = 0;
		if (building.condition < 0)
			building.condition = 0;
	}

	public void removeMod(string m) {
		if (isServer) {
			foreach (Mod mod in mods) {
				if (mod.modName == m) {
					mods.Remove (mod);
					break;
				}
			}
		}
	}

	public int removeAllMods() {
		int toRemove = 0;
		foreach (Mod m in mods) {
			toRemove += m.costToRemove;
		}
		mods.Clear ();
		return toRemove;
	}

	/// <summary>
	/// Removes the mod from all buildings within the mod's radius.
	/// </summary>
	/// <param name="mod">Mod.</param>
	public void removeModBomb(string modName) {
		float radius = 0;
		foreach (Mod mod in mods) {
			if (mod.modName == modName) {
				radius = mod.radius;
				break;
			}
		}
		Collider[] colliding = Physics.OverlapSphere(building.c.transform.position, radius);
		foreach (Collider hit in colliding) {
			BuildingModifier bm = hit.GetComponent<BuildingModifier> ();

			if (bm != null) {
				bm.removeMod (modName);
			}
		}
		removeMod (modName);
	}

	public void clearButtons() {
		foreach (GameObject g in buttons) {
			if (g != null) {
				Destroy (g);
			}
		}
		buttons.Clear ();
	}

	private void generateButtons(NetworkInstanceId pid) {
		float x = 0;
		float buttonWidth = 32f;

		GameObject obj;
		if (isClient) {
			obj = ClientScene.FindLocalObject (pid);
		} else {
			obj = NetworkServer.FindLocalObject (pid); 
		}

		Player player = obj.GetComponent<Player> ();
		foreach (Mod mod in mods) {
			if (mod.costToRemove > 0) {
				GameObject removeButton = (GameObject)Instantiate (Resources.Load ("ModifierIcon"));

				if (isPositive (mod)) {
					removeButton.GetComponent<Image> ().color = Color.green;
				}
				Transform parent = GameObject.Find ("Canvas").transform.Find ("ReadoutPanel/Readout Scroll/Viewport/ReadoutDisplay/Mods");
				Transform dummy = parent.Find ("Dummy");
				removeButton.transform.SetParent (parent, false);
				removeButton.transform.position = new Vector3 (dummy.position.x + x, dummy.position.y, dummy.position.z);

				removeButton.GetComponent<Image> ().sprite = icons [mod.icon];
				Text tmpText = removeButton.transform.Find ("Text").GetComponent<Text> ();
				Button tmpButton = removeButton.GetComponent<Button> ();


				removeButton.GetComponent<ModIcon> ().setTooltipText (modToString (mod) + "\nRemoval cost:" + " $" + mod.costToRemove + "");
				if (building.ownedBy(player.netId)) {
					string toRemove = mod.modName;
					int price = mod.costToRemove;
					tmpButton.onClick.AddListener (delegate {
						player.CmdRemoveMod (toRemove, price, pid, gameObject.GetComponent<NetworkIdentity> ().netId);
					});
				}
				buttons.Add (removeButton);

				x += buttonWidth;
			} else {
				GameObject removeButton = (GameObject)Instantiate (Resources.Load ("ModifierIcon"));

				if (isPositive (mod)) {
					removeButton.GetComponent<Image> ().color = Color.green;
				}
				removeButton.transform.SetParent (GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").transform, false);
				removeButton.transform.localPosition = new Vector3 (x, -32, 0);
				removeButton.transform.localScale = new Vector3 (1, 1, 1);

				removeButton.GetComponent<Image> ().sprite = icons [mod.icon];


				removeButton.GetComponent<ModIcon> ().setTooltipText (modToString (mod));

				buttons.Add (removeButton);
				x += buttonWidth;
			}
		}
	}

	private bool isPositive(Mod mod) {
		bool positive = false;
		if (mod.rentModifier > 1) {
			positive = true;
		} else if (mod.conditionModifier > 1) {
			positive = true;
		} else if (mod.safetyModifier > 1) {
			positive = true;
		}
		return positive;
	}
}
