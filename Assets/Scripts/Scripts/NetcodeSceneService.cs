using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Transports.SinglePlayer;

public class NetcodeSceneService : MonoBehaviour
{
   // private UnityTransport unityTransport;
    private SinglePlayerTransport singlePlayerTransport;
   [field:SerializeField] public Transform playersContainersTransform { get; private set; }
    private void Awake() {
      //  unityTransport = RelayManager.Instance.GetComponent<UnityTransport>();
      /*  singlePlayerTransport = RelayManager.Instance.GetComponent<SinglePlayerTransport>();
        
        if (!BetweenScenesService.Instance.IsCoop)
            StartSinglePlayer();
        else if(IsHost)
            StartMultiplayerHost();
        else 
            StartMultiplayerClient();
        playersContainersTransform.gameObject.SetActive(false);*/
    }
    
    private void StartSinglePlayer()
    {
        Debug.Log("🎮 Starting SINGLE PLAYER mode");
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = singlePlayerTransport;
        
        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogError("Failed to start single player session!");
        }
    }
    
    private void StartMultiplayerHost()
    {
        Debug.Log("🌐 Starting MULTIPLAYER HOST mode");
   //     NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        
      //  if (!NetworkManager.Singleton.StartHost())
      //  {
      //      Debug.LogError("Failed to start multiplayer host!");
      //  }
    }
    
    private void StartMultiplayerClient(string joinCode = null)
    {
        Debug.Log("🌐 Starting MULTIPLAYER CLIENT mode");
     //   NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        
        // Здесь можно добавить настройку Relay если нужно
        
     //   if (!NetworkManager.Singleton.StartClient())
     //   {
     //       Debug.LogError("Failed to start multiplayer client!");
     //   }
    }

    public NetworkObject InstantiateObject(bool isSpawn,NetworkObject spawnObject, Vector2 spawnPosition = default, Transform parent = null,
        Quaternion rotation = default) {
        var needObject = Instantiate(spawnObject, spawnPosition,rotation,parent);
        if (isSpawn)
            needObject.Spawn(true);
        return needObject;
    }
    
    public Transform InstantiateObject(Transform spawnObject, Transform parent, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity, parent).transform;
    }
}
