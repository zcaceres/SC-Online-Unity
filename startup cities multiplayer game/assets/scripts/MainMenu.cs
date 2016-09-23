using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour {
	public Canvas quitMenu;
	public Button StartButton;
	public Button ExitButton;
	public Button YesQuit;
	public Button NoQuit;

	private Material MenuOption;
	private Renderer MenuOptionRenderer;
	private CursorLockMode cursorMode = CursorLockMode.None;

	// Use this for initialization
	void Start () {
		quitMenu = quitMenu.GetComponent<Canvas> ();
		StartButton = StartButton.GetComponent<Button> ();
		ExitButton = ExitButton.GetComponent<Button> ();
		quitMenu.enabled = false;
		Cursor.lockState = cursorMode;
		Cursor.visible = true;
		}
		
	public void ExitPress ()
	{
		quitMenu.enabled = true;
		StartButton.enabled = false;
		ExitButton.enabled = false;
	}

	public void NoPress()
	{
		quitMenu.enabled = false;
		StartButton.enabled = true;
		ExitButton.enabled = true;
	}

	public void StartLevel()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene ("urban_city_demo");
	}

	public void ExitGame()
	{
		Application.Quit() ;
	}

}
