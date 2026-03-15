using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetcodeBetweenScenesService : NetworkBehaviour
{
    public NetworkList<PlayerReadyState> playerReadyStates;
    public NetworkVariable<int> readyClientsCount = new NetworkVariable<int>(0);
    public static NetcodeBetweenScenesService Instance { get; private set; }
    private Action<RoundType> startRoundAction;
    private Action<int[]> updateAction;
    private Action<Transform> quicksandAction;
    private Action<Transform, int> addTrainTrailAction;
    public Transform MapContainer;
    public SpriteRenderer GroundSpriteRenderer;
    public MapEventsContentView MapEventsContentView;
    public NetcodeSceneService NetcodeSceneService;
    private void Awake()
    {
        playerReadyStates = new NetworkList<PlayerReadyState>();
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
          
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void AddToPlayerListServerRpc(ulong id, int playerNumber) {
        playerReadyStates.Add(new PlayerReadyState 
        { 
            ClientId = id,
            IsReady = false,
            PlayerNumber = playerNumber,
            PlayerNickname = DataPersistenceManagerSystem._gameData.CoopPlayerName,
            PlayerCarId = 0
        });
    }
    [ServerRpc(RequireOwnership = false)]
    public void ReportSceneLoadedServerRpc()
    {
        if (!IsServer) return;
        
        readyClientsCount.Value++;
        Debug.Log($"📥 Client reported scene loaded. Total ready: {readyClientsCount.Value}");
    }
    [ClientRpc]
    public void ChangeMapClientRpc(int[] mapGrid)
    {
        updateAction?.Invoke(mapGrid);
    }
    
    [ClientRpc]
    public void SpawnMapObstacleClientRpc(int index, int rotationIndex, Vector3 spawnPosition = default) {
     
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        var mapObject = Instantiate(BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[0].MapObstacles[index], spawnPosition,
            Quaternion.identity, MapContainer);
       // mapObject.GetComponent<NetworkObject>()?.TrySetParent(MapContainer);
        mapObject.rotation = randomRotation;
    }
    [ClientRpc]
    public void SpawnMapPillarClientRpc(int index,int rotationIndex, Vector3 spawnPosition = default) {
      
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        var mapObject = Instantiate(BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[1].MapObstacles[index],spawnPosition,Quaternion.identity,MapContainer);
        mapObject.rotation = randomRotation;
    }
    [ClientRpc]
    public void SpawnQuicksandClientRpc(int index,int rotationIndex, Vector3 spawnPosition = default) {
      
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, rotationIndex * 90);
        var mapObject = Instantiate(MapEventsContentView._quicksandPrefabs[index],spawnPosition,Quaternion.identity,MapContainer);
        mapObject.rotation = randomRotation;
        quicksandAction.Invoke(mapObject.transform);
    }
    [ClientRpc]
    public void SpawnTrainTrailsClientRpc(int rotationIndex) {
       
        var mapObject = Instantiate(MapEventsContentView._trainTunnelsPrefab,MapContainer);
        addTrainTrailAction.Invoke(mapObject.transform, rotationIndex);
    }
    
    [ClientRpc]
    public void SpawnGroundClientRpc(Vector3 spawnPosition = default) {
     
        var mapObject = Instantiate(GroundSpriteRenderer,spawnPosition,Quaternion.identity,MapContainer);
        mapObject.material = BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[1].GroundMaterial;
        mapObject.transform.localScale = new (5,5);
    }
    [ClientRpc]
    public void ShowMessageClientRpc(string text) {
        if (NetworkManager.Singleton.IsHost) return;
        NetcodeSceneService.clientsMessegeText.text = GetLocalization(text);
        NetcodeSceneService.clientsMessegeText.transform.parent.gameObject.SetActive(true);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    [ClientRpc]
    public void StartRoundClientRpc(RoundType roundType) {
        startRoundAction.Invoke(roundType);
    //    NetcodeSceneService.HideMessageClientRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
    {
        for (int i = 0; i < playerReadyStates.Count; i++)
        {
            if (playerReadyStates[i].ClientId == clientId)
            {
                var state = playerReadyStates[i];
                state.IsReady = isReady;
                playerReadyStates.Set(i,state, true);
                break;
            }
        }
        
        // Проверяем, все ли готовы
      //  CheckAllPlayersReady();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayersToNotReadyServerRpc() {
        for (int i = 0; i < playerReadyStates.Count; i++) {
            var state = playerReadyStates[i];
            state.IsReady = false;
            playerReadyStates.Set(i, state, true);
        }
    }
    public bool CheckAllPlayersReady()
    {
        if (playerReadyStates.Count == 0) return false;
        
        bool allReady = true;
        foreach (var state in playerReadyStates)
        {
            if (!state.IsReady)
            {
                allReady = false;
                break;
            }
        }
        return allReady;
    }
    public void SetupService(Action<int[]> updateAction, Action<Transform> quicksandAction, Action<Transform,int> addTrainTrailAction,
        Action<RoundType> startRoundAction) {
        this.updateAction = updateAction;
        this.quicksandAction = quicksandAction;
        this.addTrainTrailAction = addTrainTrailAction;
        this.startRoundAction = startRoundAction;
    }
}
