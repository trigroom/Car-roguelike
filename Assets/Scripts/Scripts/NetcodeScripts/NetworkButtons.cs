using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkButtons : NetworkBehaviour {
    [Header("UI Elements")]
    [SerializeField]
    private CoopLobbyUIManager _lobbyManager;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button changeNicknameButton;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Transform joinCodeContainer;
    [SerializeField] private SelectMapUiView selectMapUiView;
    [SerializeField] private SelectDifficultyUIView selectDifficultyUIView;
    private bool isPlayerReady = true;
    private void Awake()
    {
     
    }
    private void Start() {
        nicknameInput.text = DataPersistenceManagerSystem._gameData.CoopPlayerName;
        // Назначаем обработчики
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);
        _lobbyManager._readyButton.onClick.AddListener(OnReadyButtonClicked);
        disconnectButton.onClick.AddListener(OnLeaveButtonClicked);
        changeNicknameButton.onClick.AddListener(()=>DataPersistenceManagerSystem._gameData.CoopPlayerName = nicknameInput.text);
        // Изначально кнопка выхода неактивна
        
        disconnectButton.gameObject.SetActive(false);
        // Подписываемся на события NetworkManager
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        RelayManager.Instance.playerReadyStates.OnListChanged += OnPlayerReadyStatesChanged;
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        RelayManager.Instance.playerReadyStates.OnListChanged -= OnPlayerReadyStatesChanged;
    }

    private void OnReadyButtonClicked()
    {
        isPlayerReady = !isPlayerReady;
       Debug.Log(isPlayerReady);
        // Меняем текст кнопки
       // readyButton.GetComponentInChildren<TextMeshProUGUI>().text = 
       //     isPlayerReady ? "✅ Я ГОТОВ" : "Готов";
        
        // Отправляем статус на сервер
        SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, isPlayerReady);
    }
    public void OnReadyButtonClicked(int carId)
    {
        isPlayerReady = !isPlayerReady;
       
        SetPlayerCarIdServerRpc(NetworkManager.Singleton.LocalClientId, carId);
    }
    
    private void OnPlayerReadyStatesChanged(NetworkListEvent<PlayerReadyState> changeEvent)
    {
        Debug.Log(" update lobby ui");
        // Обновляем UI при изменении списка игроков
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI() {
        foreach (var slot in _lobbyManager._lobbyPlayerUIViews)
            slot.gameObject.SetActive(false);

        foreach (var state in RelayManager.Instance.playerReadyStates) {
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
            
           /* if (IsServer)
            {
                playerReadyStates.Add(new PlayerReadyState 
                { 
                    ClientId = NetworkManager.Singleton.LocalClientId,
                    IsReady = false,
                    PlayerNumber = 1,
                    PlayerNickname = DataPersistenceManagerSystem._gameData.CoopPlayerName,
                    PlayerCarId = 0
                });
            }*/
        }
        else
        {
            EnableMenuButtons();
        }
    }
    
    [ClientRpc]
    private void StartGameForAllClientRpc() {
        if (IsServer) {
            for (int i = 0; i < RelayManager.Instance.playerReadyStates.Count; i++) {
                var state = RelayManager.Instance.playerReadyStates[i];
                state.IsReady = false;
                RelayManager.Instance.ChangePlayerList(state, i);
            }

            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
    }

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
        {
            EnableMenuButtons();
        }
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
        UpdateDifficultyForAllClientRpc(difficultyIndex);
    }
    
    public void OnChangeMap(int mapIndex)
    {
        UpdateMapForAllClientRpc(mapIndex);
    }
   
    [ClientRpc]
    private void UpdateMapForAllClientRpc(int mapId)
    { 
        BetweenScenesService.Instance.CurrentSelectedMap = mapId;
        selectMapUiView._groundFieldSpriteRenderer.material = BetweenScenesService.Instance._carStuffListsConfig.MapConfigs[mapId].MapVisualInfoConfigs[0].BehindMapGroundMaterial;
       
    }
    [ClientRpc]
    private void UpdateDifficultyForAllClientRpc(int difficultyIndex) {
        BetweenScenesService.Instance.CurrentSelectedMapDifficlty = difficultyIndex;
       _lobbyManager._difficultyDescriptionText.text = selectDifficultyUIView.GetDifficultyDescription(BetweenScenesService.Instance.GetMapDifficultyConfig());
    }
    private void OnClientConnected(ulong clientId)
    {
        leaveButton.gameObject.SetActive(false);
        disconnectButton.gameObject.SetActive(true);
        if (!IsServer/*&& clientId != NetworkManager.Singleton.LocalClientId*/) return;
        
        // Новый клиент подключился - добавляем в список
        int playerNumber = RelayManager.Instance.playerReadyStates.Count + 1;
        RelayManager.Instance.playerReadyStates.Add(new PlayerReadyState 
        { 
            ClientId = clientId,
            IsReady = false,
            PlayerNumber = playerNumber,
            PlayerNickname = DataPersistenceManagerSystem._gameData.CoopPlayerName,
            PlayerCarId = 0
        });
        
        Debug.Log($"👤 Игрок {playerNumber} подключился");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        leaveButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
        if (!IsServer) return;
        
        // Удаляем игрока из списка
        for (int i = 0; i < RelayManager.Instance.playerReadyStates.Count; i++)
        {
            if (RelayManager.Instance.playerReadyStates[i].ClientId == clientId)
            {
                Debug.Log($"👋 Игрок {RelayManager.Instance.playerReadyStates[i].PlayerNumber} отключился");
                RelayManager.Instance.playerReadyStates.RemoveAt(i);
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
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
    {
        for (int i = 0; i < RelayManager.Instance.playerReadyStates.Count; i++)
        {
            if (RelayManager.Instance.playerReadyStates[i].ClientId == clientId)
            {
                var state = RelayManager.Instance.playerReadyStates[i];
                state.IsReady = isReady;
                RelayManager.Instance.ChangePlayerList(state,i);
                break;
            }
        }
        
        // Проверяем, все ли готовы
        CheckAllPlayersReady();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerCarIdServerRpc(ulong clientId, int carId)
    {
        for (int i = 0; i < RelayManager.Instance.playerReadyStates.Count; i++)
        {
            if (RelayManager.Instance.playerReadyStates[i].ClientId == clientId)
            {
                var state = RelayManager.Instance.playerReadyStates[i];
                state.PlayerCarId = carId;
                   
                RelayManager.Instance.ChangePlayerList(state, i);
            
             
                break;
            }
        }
        
        // Проверяем, все ли готовы
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        if (!IsServer) return;
        
        if (RelayManager.Instance.playerReadyStates.Count == 0) return;
        
        bool allReady = true;
        foreach (var state in RelayManager.Instance.playerReadyStates)
        {
            if (!state.IsReady)
            {
                allReady = false;
                break;
            }
        }
        
        if (allReady)
        {
            Debug.Log("🎮 ВСЕ ГОТОВЫ! Запускаем игру...");
            StartGameForAllClientRpc();
        }
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

    // Реализация IEquatable<PlayerReadyState>
    public bool Equals(PlayerReadyState other)
    {
        return ClientId == other.ClientId &&
               IsReady == other.IsReady &&
               PlayerNumber == other.PlayerNumber &&
               PlayerNickname.Equals(other.PlayerNickname) &&
               PlayerCarId == other.PlayerCarId;
    }

    // Переопределяем Equals(object) для полноты
    public override bool Equals(object obj)
    {
        return obj is PlayerReadyState other && Equals(other);
    }

    // Переопределяем GetHashCode - обязательно для IEquatable
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
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

    // Операторы равенства (рекомендуется для структур)
    public static bool operator ==(PlayerReadyState left, PlayerReadyState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerReadyState left, PlayerReadyState right)
    {
        return !(left == right);
    }
    
    public string GetNickname() => PlayerNickname.ToString();
}
