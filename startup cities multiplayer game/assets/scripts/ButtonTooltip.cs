using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public string message;
	private GameObject tooltip;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnPointerEnter(PointerEventData eventData) {
		if (!string.IsNullOrEmpty (message)) {
			tooltip = (GameObject)Instantiate (Resources.Load ("ButtonTooltip"));
			tooltip.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			tooltip.transform.Find ("Text").GetComponent<Text> ().text = message;
		}
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}
}
