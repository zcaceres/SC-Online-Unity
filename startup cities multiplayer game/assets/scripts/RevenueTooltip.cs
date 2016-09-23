using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class RevenueTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public string message;
	private GameObject tooltip;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnPointerEnter(PointerEventData eventData) {
		//this used to display the revenue for your active company
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}
}
