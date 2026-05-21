using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkButtons : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField]
    public CoopLobbyUIManager _lobbyManager;
    [SerializeField] private Button copyButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button changeNicknameButton;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Transform joinCodeContainer;
    [SerializeField] public SelectMapUiView selectMapUiView;
    [SerializeField] public SelectDifficultyUIView selectDifficultyUIView;
    private bool isPlayerReady = true;
    
    public void SetupNetworkButtons() {
        Debug.Log("Start NetworkButtons");
        nicknameInput.text = DataPersistenceManagerSystem._gameData.CoopPlayerName;
        // Назначаем обработчики
        copyButton.onClick.AddListener(CopyJoinCode);
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);
        _lobbyManager._readyButton.onClick.AddListener(OnReadyButtonClicked);
        disconnectButton.onClick.AddListener(OnLeaveButtonClicked);
        changeNicknameButton.onClick.AddListener(()=> {
            DataPersistenceManagerSystem._gameData.CoopPlayerName = nicknameInput.text;
            DataPersistenceManagerSystem.SaveGame();
        });
        // Изначально кнопка выхода неактивна
        
        disconnectButton.gameObject.SetActive(false);
        // Подписываемся на события NetworkManager
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        NetcodeBetweenScenesService.Instance.playerReadyStates.OnListChanged += OnPlayerReadyStatesChanged;
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        NetcodeBetweenScenesService.Instance.playerReadyStates.OnListChanged -= OnPlayerReadyStatesChanged;
    }

    private void OnReadyButtonClicked()
    {
        isPlayerReady = !isPlayerReady;
       Debug.Log(isPlayerReady);
        // Меняем текст кнопки
       // readyButton.GetComponentInChildren<TextMeshProUGUI>().text = 
       //     isPlayerReady ? "✅ Я ГОТОВ" : "Готов";
        
        // Отправляем статус на сервер
        NetcodeBetweenScenesService.Instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, isPlayerReady);
    }
    public void OnReadyButtonClicked(int carId)
    {
        isPlayerReady = !isPlayerReady;
       
        NetcodeBetweenScenesService.Instance.SetPlayerCarIdServerRpc(NetworkManager.Singleton.LocalClientId, carId);
    }
    
    private void OnPlayerReadyStatesChanged(NetworkListEvent<PlayerReadyState> changeEvent)
    {
        Debug.Log(" update lobby ui");
        // Обновляем UI при изменении списка игроков
        UpdateLobbyUI();
    }

    public void UpdateLobbyUI() {
        foreach (var slot in _lobbyManager._lobbyPlayerUIViews)
            slot.gameObject.SetActive(false);

        foreach (var state in NetcodeBetweenScenesService.Instance.playerReadyStates) {
            var playerSlotUIView = _lobbyManager._lobbyPlayerUIViews[state.PlayerNumber - 1];
            playerSlotUIView.gameObject.SetActive(true);
            playerSlotUIView.ChangeCarSprite(BetweenScenesService.Instance._carStuffListsConfig.CarConfigs[state.PlayerCarId].CarIcon,
                state.GetNickname(), state.IsReady);
        }
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    private async void OnHostButtonClicked()
    {
        DisableMenuButtons();
        
        string joinCode = await RelayManager.Instance.StartHostWithRelay();
        
        if (!string.IsNullOrEmpty(joinCode))
        {
            joinCodeDisplay.text = $"{GetLocalization("Join code")}: {joinCode}";
            joinCodeContainer.gameObject.SetActive(true);
            disconnectButton.gameObject.SetActive(true);
            
        }
        else
        {
            EnableMenuButtons();
        }
    }
    
  
   
   /* [ClientRpc]
    private void RejectClientClientRpc(ulong targetClientId, string reason)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.LogWarning($"❌ Отказано в подключении: {reason}");
            // Показываем сообщение игроку
            
            // Отключаемся
            NetworkManager.Singleton.Shutdown();
        }
    }*/
    private async void OnClientButtonClicked()
    {
        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogWarning("⚠️ Please enter a join code");
            return;
        }

        DisableMenuButtons();
        
        bool connected = await RelayManager.Instance.StartClientWithRelay(joinCodeInput.text);
        
        if (connected)
        {
           // leaveButton.gameObject.SetActive(true);
        }
        else
            EnableMenuButtons();
    }

    private void CopyJoinCode() {
        GUIUtility.systemCopyBuffer = RelayManager.Instance.JoinCode;
    }
    private void OnLeaveButtonClicked()
    {
        RelayManager.Instance.LeaveSession();
        joinCodeContainer.gameObject.SetActive(false);
        joinCodeInput.text = "";
        EnableMenuButtons();
        leaveButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
    }
    public void OnChangeDifficultyDescription(int difficultyIndex)
    {
       NetcodeBetweenScenesService.Instance. UpdateDifficultyForAllClientRpc(difficultyIndex);
    }
    
    public void OnChangeMap(int mapIndex)
    {
        NetcodeBetweenScenesService.Instance.UpdateMapForAllClientRpc(mapIndex);
    }
   
    
    private void OnClientConnected(ulong clientId)
    {
        leaveButton.gameObject.SetActive(false);
        disconnectButton.gameObject.SetActive(true);
        if (!NetworkManager.Singleton.IsServer/*&& clientId != NetworkManager.Singleton.LocalClientId*/) return;
        
        // Новый клиент подключился - добавляем в список
        int playerNumber = NetcodeBetweenScenesService.Instance.playerReadyStates.Count + 1;
        NetcodeBetweenScenesService.Instance.playerReadyStates.Add(new PlayerReadyState 
        { 
            ClientId = clientId,
            IsReady = false,
            PlayerNumber = playerNumber,
            PlayerNickname = DataPersistenceManagerSystem._gameData.CoopPlayerName,
            PlayerCarId = 0
        });
        
        Debug.Log($"👤 Игрок {playerNumber} подключился");
    }

    private void OnClientDisconnected(ulong clientId) {
        leaveButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
        if (clientId == NetworkManager.Singleton.LocalClientId)
            NetworkManager.Singleton.Shutdown();
        if (!NetworkManager.Singleton.IsServer) return;

        // Удаляем игрока из списка
        for (int i = 0; i < NetcodeBetweenScenesService.Instance.playerReadyStates.Count; i++) {
            if (NetcodeBetweenScenesService.Instance.playerReadyStates[i].ClientId == clientId) {
                //  Debug.Log($"👋 Игрок {NetcodeBetweenScenesService.Instance.playerReadyStates[i].PlayerNumber} отключился");
                NetcodeBetweenScenesService.Instance.playerReadyStates.RemoveAt(i);
                break;
            }
        }
    }

    private void DisableMenuButtons()
    {
        hostButton.interactable = false;
        clientButton.interactable = false;
        joinCodeInput.interactable = false;
    }

    private void EnableMenuButtons()
    {
        joinCodeContainer.gameObject.SetActive(false);
        hostButton.interactable = true;
        clientButton.interactable = true;
        joinCodeInput.interactable = true;
        leaveButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
    }
    
  

}

[System.Serializable]
public struct PlayerReadyState : INetworkSerializable, IEquatable<PlayerReadyState>
{
    public ulong ClientId;
    public bool IsReady;
    public int PlayerNumber;
    public FixedString64Bytes PlayerNickname;
    public int PlayerCarId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref PlayerNumber);
        serializer.SerializeValue(ref PlayerNickname);
        serializer.SerializeValue(ref PlayerCarId);
    }

    public bool Equals(PlayerReadyState other)
    {
        return ClientId == other.ClientId &&
               IsReady == other.IsReady &&
               PlayerNumber == other.PlayerNumber &&
               PlayerNickname.Equals(other.PlayerNickname) &&
               PlayerCarId == other.PlayerCarId;
    }

    public override int GetHashCode()
    {
        unchecked 
        {
            int hash = 17;
            hash = hash * 23 + ClientId.GetHashCode();
            hash = hash * 23 + IsReady.GetHashCode();
            hash = hash * 23 + PlayerNumber.GetHashCode();
            hash = hash * 23 + PlayerNickname.GetHashCode();
            hash = hash * 23 + PlayerCarId.GetHashCode();
            return hash;
        }
    }
    
    public string GetNickname() => PlayerNickname.ToString();
}
