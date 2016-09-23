using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class BudgetTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public string message;
	private GameObject tooltip;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnPointerEnter(PointerEventData eventData) {
		//this used to display your active company's budget
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}
}
