using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class NetcodeBetweenScenesService : NetworkBehaviour {
    public NetworkList<PlayerReadyState> playerReadyStates;
    public NetworkVariable<int> readyClientsCount = new NetworkVariable<int>(0);
    public static NetcodeBetweenScenesService Instance { get; private set; }
    private Action<RoundType, float> startRoundAction;
    private Action<int[]> updateAction;
    private Action<Transform> quicksandAction;
    private Action<Transform, int> addTrainTrailAction;

    public Transform MapContainer;
    public SpriteRenderer GroundSpriteRenderer;
    public Transform BackgroundObjectSpriteRenderer;
    public MapEventsContentView MapEventsContentView;
    public NetcodeSceneService NetcodeSceneService;
    public NetworkButtons _networkButtonsView;

    private NetworkVariable<bool> _isGameStarted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake() {
        playerReadyStates = new NetworkList<PlayerReadyState>();
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public bool CanJoin() {
        return !_isGameStarted.Value;
    }

    public void ChangeGameIsStartedStateValue(bool value) {
        _isGameStarted.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddToPlayerListServerRpc(ulong id, int playerNumber) {
        playerReadyStates.Add(new PlayerReadyState {
            ClientId = id,
            IsReady = false,
            PlayerNumber = playerNumber,
            PlayerNickname = DataPersistenceManagerSystem._gameData.CoopPlayerName,
            PlayerCarId = 0
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportSceneLoadedServerRpc() {
        if (!IsServer) return;

        readyClientsCount.Value++;
        Debug.Log($"📥 Client reported scene loaded. Total ready: {readyClientsCount.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyCountToZeroServerRpc() {
        readyClientsCount.Value = 0;
        Debug.Log($"📥 Client reported scene loaded. Total ready: {readyClientsCount.Value}");
    }

    [ClientRpc]
    public void ChangeMapClientRpc(int[] mapGrid) {
        updateAction?.Invoke(mapGrid);
    }

    [ClientRpc]
    public void SpawnMapObstacleClientRpc(int index, int rotationIndex, bool isCollideObstacle, Vector3 spawnPosition = default) {
      //  if (isCollideObstacle) rotationIndex = 0;
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        Transform mapObject =
            Instantiate(
                isCollideObstacle
                    ? BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[0].MapObstacles[index]
                    : BackgroundObjectSpriteRenderer, spawnPosition,
                Quaternion.identity, MapContainer);
        // mapObject.GetComponent<NetworkObject>()?.TrySetParent(MapContainer);
        if (!isCollideObstacle)
            mapObject.GetComponent<SpriteRenderer>().sprite =
                BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[0].MapGroundPatterns[index];
        mapObject.rotation = randomRotation;
    }

    [ClientRpc]
    public void SpawnMapPillarClientRpc(int index, int rotationIndex, Vector3 spawnPosition = default) {
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        var mapObject = Instantiate(BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[1].MapObstacles[index], spawnPosition,
            Quaternion.identity, MapContainer);
        mapObject.rotation = randomRotation;
    }

    [ClientRpc]
    public void SpawnQuicksandClientRpc(int index, int rotationIndex, Vector3 spawnPosition = default) {
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        var mapObject = Instantiate(MapEventsContentView._quicksandPrefabs[index], spawnPosition, Quaternion.identity, MapContainer);
        mapObject.rotation = randomRotation;
        quicksandAction.Invoke(mapObject.transform);
    }

    [ClientRpc]
    public void SpawnTrainTrailsClientRpc(int rotationIndex) {
        var mapObject = Instantiate(MapEventsContentView._trainTunnelsPrefab, MapContainer);
        addTrainTrailAction.Invoke(mapObject.transform, rotationIndex);
    }

    [ClientRpc]
    public void SpawnGroundClientRpc(Vector3 spawnPosition = default) {
        var mapObject = Instantiate(GroundSpriteRenderer, spawnPosition, Quaternion.identity, MapContainer);
        mapObject.material = BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[1].GroundMaterial;
        mapObject.transform.localScale = new(5, 5);
    }

    [ClientRpc]
    public void ShowMessageClientRpc(string text) {
        if (NetworkManager.Singleton.IsHost) return;
        NetcodeSceneService.clientsMessegeText.text = GetLocalization(text);
        NetcodeSceneService.clientsMessegeText.transform.parent.gameObject.SetActive(true);
    }

    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

    [ClientRpc]
    public void StartRoundClientRpc(RoundType roundType, float moneyMultiplayer) {
        startRoundAction.Invoke(roundType, moneyMultiplayer);
        //    NetcodeSceneService.HideMessageClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady) {
        for (int i = 0; i < playerReadyStates.Count; i++) {
            if (playerReadyStates[i].ClientId == clientId) {
                var state = playerReadyStates[i];
                state.IsReady = isReady;
                playerReadyStates.Set(i, state, true);
                break;
            }
        }

        // Проверяем, все ли готовы
        if (CheckAllPlayersReady())
            StartGameForAllClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayersToNotReadyServerRpc() {
        for (int i = 0; i < playerReadyStates.Count; i++) {
            var state = playerReadyStates[i];
            state.IsReady = false;
            playerReadyStates.Set(i, state, true);
        }
    }

    public bool CheckAllPlayersReady() {
        if (playerReadyStates.Count == 0) return false;

        bool allReady = true;
        foreach (var state in playerReadyStates) {
            if (!state.IsReady) {
                allReady = false;
                break;
            }
        }

        return allReady;
    }

    [ClientRpc]
    public void StartGameForAllClientRpc() {
        if (IsServer) {
            for (int i = 0; i < NetcodeBetweenScenesService.Instance.playerReadyStates.Count; i++) {
                var state = NetcodeBetweenScenesService.Instance.playerReadyStates[i];
                state.IsReady = false;
                NetcodeBetweenScenesService.Instance.playerReadyStates.Set(i, state, true);
            }

            NetcodeBetweenScenesService.Instance.ChangeGameIsStartedStateValue(true);
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
    }

    [ClientRpc]
    public void UpdateMapForAllClientRpc(int mapId) {
        BetweenScenesService.Instance.CurrentSelectedMap = mapId;
        _networkButtonsView.selectMapUiView._groundFieldSpriteRenderer.material = BetweenScenesService.Instance._carStuffListsConfig.MapConfigs[mapId]
            .MapVisualInfoConfigs[0].BehindMapGroundMaterial;
    }

    [ClientRpc]
    public void UpdateDifficultyForAllClientRpc(int difficultyIndex) {
        BetweenScenesService.Instance.CurrentSelectedMapDifficlty = difficultyIndex;
        _networkButtonsView._lobbyManager._difficultyDescriptionText.text =
            _networkButtonsView.selectDifficultyUIView.GetDifficultyDescription(BetweenScenesService.Instance.GetMapDifficultyConfig());
    }

  /*  [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady) {
        for (int i = 0; i < NetcodeBetweenScenesService.Instance.playerReadyStates.Count; i++) {
            if (NetcodeBetweenScenesService.Instance.playerReadyStates[i].ClientId == clientId) {
                var state = NetcodeBetweenScenesService.Instance.playerReadyStates[i];
                state.IsReady = isReady;
                NetcodeBetweenScenesService.Instance.playerReadyStates.Set(i, state, true);
                break;
            }
        }

        if (NetcodeBetweenScenesService.Instance.CheckAllPlayersReady())
            StartGameForAllClientRpc();
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerCarIdServerRpc(ulong clientId, int carId) {
        for (int i = 0; i < NetcodeBetweenScenesService.Instance.playerReadyStates.Count; i++) {
            if (NetcodeBetweenScenesService.Instance.playerReadyStates[i].ClientId == clientId) {
                var state = NetcodeBetweenScenesService.Instance.playerReadyStates[i];
                state.PlayerCarId = carId;

                NetcodeBetweenScenesService.Instance.playerReadyStates.Set(i, state, true);

                break;
            }
        }
    }

    public void SetupService(Action<int[]> updateAction, Action<Transform> quicksandAction, Action<Transform, int> addTrainTrailAction,
        Action<RoundType, float> startRoundAction) {
        this.updateAction = updateAction;
        this.quicksandAction = quicksandAction;
        this.addTrainTrailAction = addTrainTrailAction;
        this.startRoundAction = startRoundAction;
    }
}