using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PersistNotification : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public string message; 

	private GameObject tooltip;
	private GameObject tenantPanel;
	private CanvasManager ui;
	private static Sprite[] icons;
	private Sprite icon;
	// Use this for initialization
	void Start () {
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		if (icons == null) {
			icons = Resources.LoadAll<Sprite> ("Icons and Portraits/64 flat icons/png/32px");
		}
	}
		
	/// <summary>
	/// destroys the gameobject and the tooltip associated with it (if one exists)
	/// </summary>
	public void buttonDestroy() {
		if (tooltip != null) {
			Destroy (tooltip);
		}
		Destroy (gameObject);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		if (tooltip == null) {
			Vector3 mouse = new Vector3 (Input.mousePosition.x, Input.mousePosition.y - 50, Input.mousePosition.z);
			tooltip = (GameObject)Instantiate (Resources.Load ("TenantTooltip"), mouse, Quaternion.identity);
			tooltip.transform.SetParent (GameObject.Find ("Canvas").transform, true);
			tooltip.transform.Find ("Text").GetComponent<Text> ().text = message;
		}
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}

	public void setImage(Sprite s, Color c) {
		Image image = transform.Find ("Image").GetComponent<Image>();
		image.sprite = s;
		image.color = c;
		image.gameObject.SetActive (true);
	}
}