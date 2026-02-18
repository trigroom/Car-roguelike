using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
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
    // Состояние игроков (синхронизируется через NetworkList)
    public NetworkList<PlayerReadyState> playerReadyStates;
    private bool isPlayerReady = true;
    private void Awake()
    {
        playerReadyStates = new NetworkList<PlayerReadyState>();
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
        
        playerReadyStates.OnListChanged += OnPlayerReadyStatesChanged;
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        playerReadyStates.OnListChanged -= OnPlayerReadyStatesChanged;
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
    
    private void UpdateLobbyUI()
    {
        // Очищаем старые слоты
       foreach (var slot in _lobbyManager._lobbyPlayerUIViews)
        {
           slot.gameObject.SetActive(false);
        }

        // Создаем новые слоты
        foreach (var state in playerReadyStates) {
            var playerSlotUIView = _lobbyManager._lobbyPlayerUIViews[state.PlayerNumber - 1];
            playerSlotUIView.gameObject.SetActive(true);
            playerSlotUIView.ChangeCarSprite(BetweenScenesService.Instance._carStuffListsConfig.CarConfigs[state.PlayerCarId].CarIcon,
                state.GetNickname(), state.IsReady);
           /* string readyText = state.IsReady ? "✅ ГОТОВ" : "⏳...";
            slot.SetPlayerInfo($"Игрок {state.PlayerNumber}", readyText, state.IsReady);
            
            // Подсвечиваем текущего игрока
            if (state.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                slot.HighlightAsCurrent();
            }
            
            playerSlots.Add(slot);*/
        }

        // Обновляем кнопки и статус
       /* if (IsHost)
        {
            bool allReady = true;
            foreach (var state in playerReadyStates)
            {
                if (!state.IsReady)
                {
                    allReady = false;
                    break;
                }
            }
            
            startButton.gameObject.SetActive(allReady && playerReadyStates.Count > 0);
            lobbyStatusText.text = allReady ? 
                "🎮 Все готовы! Можно начинать игру" : 
                "⏳ Ожидание готовности игроков...";
        }
        else
        {
            startButton.gameObject.SetActive(false);
            lobbyStatusText.text = "⏳ Ожидание готовности всех игроков...";
        }*/
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

    private void OnClientConnected(ulong clientId)
    {
        leaveButton.gameObject.SetActive(false);
        disconnectButton.gameObject.SetActive(true);
        if (!IsServer/*&& clientId != NetworkManager.Singleton.LocalClientId*/) return;
        
        // Новый клиент подключился - добавляем в список
        int playerNumber = playerReadyStates.Count + 1;
        playerReadyStates.Add(new PlayerReadyState 
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
        for (int i = 0; i < playerReadyStates.Count; i++)
        {
            if (playerReadyStates[i].ClientId == clientId)
            {
                Debug.Log($"👋 Игрок {playerReadyStates[i].PlayerNumber} отключился");
                playerReadyStates.RemoveAt(i);
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
        for (int i = 0; i < playerReadyStates.Count; i++)
        {
            if (playerReadyStates[i].ClientId == clientId)
            {
                var state = playerReadyStates[i];
                state.IsReady = isReady;
                playerReadyStates.Set(i,state);
                break;
            }
        }
        
        // Проверяем, все ли готовы
        CheckAllPlayersReady();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerCarIdServerRpc(ulong clientId, int carId)
    {
        for (int i = 0; i < playerReadyStates.Count; i++)
        {
            if (playerReadyStates[i].ClientId == clientId)
            {
                var state = playerReadyStates[i];
                state.PlayerCarId = carId;
                playerReadyStates.Set(i,state);
                break;
            }
        }
        
        // Проверяем, все ли готовы
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        if (!IsServer) return;
        
        if (playerReadyStates.Count == 0) return;
        
        bool allReady = true;
        foreach (var state in playerReadyStates)
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
            
            // Здесь два варианта:
            // Вариант 1: Загрузить новую сцену
            // NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            
            // Вариант 2: Спавнить игроков прямо в этой сцене
           // StartGameInCurrentSceneClientRpc();
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
