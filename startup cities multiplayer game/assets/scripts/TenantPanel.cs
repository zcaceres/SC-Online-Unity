using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TenantPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public string person;
	public string job;
	public int portNum;
	private GameObject tooltip;
	private GameObject portrait;
	private Sprite newSprite;
	private Sprite portraitSet;
	private Sprite[] portraits;
	private GameObject tenantPanel;
	private Image testButton;
	private GameObject[] buttons;
	private CanvasManager ui;

	// Use this for initialization
	void Start () {
		portraits = Resources.LoadAll<Sprite> ("Icons and Portraits/Char Portraits/portraits");
		newSprite = portraits [portNum];
		testButton = gameObject.transform.Find ("Button").GetComponent<Image>();
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		PortraitAdd (testButton, newSprite);
		ui.addButton (gameObject);
	}
		
	public void buttonDestroy() {
		if (tooltip != null) {
			Destroy (tooltip);
		}
		Destroy (gameObject);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		tooltip = (GameObject)Instantiate (Resources.Load ("TenantTooltip"));
		tooltip.transform.SetParent(GameObject.Find("Canvas").transform.Find("ReadoutPanel").transform, false);
		tooltip.transform.Find("Text").GetComponent<Text> ().text = person;
		tooltip.transform.Find("Job").GetComponent<Text> ().text = job;
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (tooltip != null) {
			Destroy (tooltip);
		}
	}

	private void PortraitAdd (Image portraitImage, Sprite newSprite) {
		portraitImage.sprite = newSprite;
	}

}
