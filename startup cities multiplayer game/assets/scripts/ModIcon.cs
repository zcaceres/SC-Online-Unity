using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ModIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	private GameObject tooltip;
	private string text;
	private CanvasManager ui;
	// Use this for initialization
	void Start () {
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		ui.addButton (gameObject);
	}
		
	/// <summary>
	/// Destroys the buttons and the tooltip
	/// </summary>
	public void buttonDestroy() {
		if (tooltip != null) {
			Destroy (tooltip);
		}
		Destroy (gameObject);
	}

	/// <summary>
	/// Sets the tooltip text.
	/// </summary>
	/// <param name="s">the tooltip text.</param>
	public void setTooltipText(string s) {
		text = s;
	}

	/// <summary>
	/// Raises the pointer enter event.
	/// </summary>
	/// <param name="eventData">Event data.</param>
	public void OnPointerEnter(PointerEventData eventData) {
		tooltip = (GameObject)Instantiate (Resources.Load ("TenantTooltip"));
		tooltip.transform.SetParent(GameObject.Find("Canvas").transform.Find("ReadoutPanel").transform, false);
		tooltip.transform.localPosition = new Vector3 (tooltip.transform.localPosition.x, 0, tooltip.transform.localPosition.z);
		tooltip.transform.Find("Text").GetComponent<Text> ().text = text;
	}

	/// <summary>
	/// Raises the pointer exit event.
	/// </summary>
	/// <param name="eventData">Event data.</param>
	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}
}
