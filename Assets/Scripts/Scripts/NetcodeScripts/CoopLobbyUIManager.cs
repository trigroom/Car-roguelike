using UnityEngine;
using UnityEngine.UI;

public class CoopLobbyUIManager : MonoBehaviour
{
    [field:SerializeField]
    public LobbyPlayerUIView[] _lobbyPlayerUIViews{get;private set;}
    [field:SerializeField]
    public Button _readyButton{get;private set;}
    
    [field:SerializeField]
    public Button _backButton{get;private set;}

}
