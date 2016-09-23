using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

//public class Chat : NetworkBehaviour {
//
//
//	//Server Initiates a SyncList at start
//	//Synclist is of type string, empty
//	//Synclist is displayed on Scroll View/Viewport/Content/Messages
//	//UI elements inputField and MessagePanel are spawned by server
//	//UI element inputField at Canvas/Chat Panel/InputField
//	//Client has no control over MessagePanel
//	//Client has their own inputField
//	//inputField.onEndEdit unity event
//	//Takes inputted string
//	//send [Command] to add string to Synclist<string> on server in object ChatLog
//	//Server syncs the Synclist
//	//Contents of synclist are displayed on client MessagePanel
//
//	public class Chatlog : SyncList<string> {
//		public SyncList<string> ChatLog = new SyncListString ();
//		private GameObject DisplayPanel = GameObject.Find ("Canvas/Chat Panel/Scroll View/Viewport/Content/Messages");
//		private string currentMessage = string.Empty;
//		private GameObject inputBox = GameObject.Find ("Canvas/Chat Panel/InputField");
//		private GameObject inputField = inputBox.GetComponent<InputField>();
//
//		private void WriteMessage() {
//			currentMessage = UI.InputField.onEndEdit 
//		
//
//		}
//	}
//}
//	}
//
//	public class ChatInput : 
//				[ClientRpc]
//		public void RpcSendMessage () {
//
//		}
//
//
//		if (!string == "")
//
//			}		
//	
//	
//	}
//
//
//	foreach (string c int Chatlog)
//	{
//		
//	}
//	public string message;
//
//	[SyncList]
//	public SyncListString chatlog;
//
//
//	public GameObject inputBox;
//	public GameObject inputField;
//
//	public string InputMessage() {
//		
//	}
//}
//
//
//
//public class ChatPanelInput : NetworkBehaviour {
//	public string message;
////	public string GameObject("inputBox");
////	public string GameObject("inputField");
////	public string GameObject("messageDisplay");
//
//
//	//OnExit pass message to Send Function
//	}
//
//
////	public void SendMessage() {
//		
////		(GameObject) messageDisplay = NetworkIdentity.GetComponentInChildren<Messages>;
//
////	}
//
//
////}
//	
//
////public void BroadcastMessage (); {
////	private GameObject messageDisplay;
//
//
//
//
//
////
////	public void ShowMessage (string message){
////		text.text = message;
////	}
////}
//
//
//
//
////player
////OnKey "Enter"
////Send message to server
