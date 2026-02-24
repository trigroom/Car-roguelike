using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Transports.SinglePlayer;

public class NetcodeSceneService : MonoBehaviour
{
   // private UnityTransport unityTransport;
    private SinglePlayerTransport singlePlayerTransport;
   [field:SerializeField] public Transform playersContainersTransform { get; private set; }
   [field:SerializeField] public TMP_Text clientsMessegeText { get; private set; }
   [field:SerializeField] public NetworkObject networkObjectsMapContainer { get; private set; }
    private void Awake() {
        NetcodeBetweenScenesService.Instance.NetcodeSceneService = this;
     
    }

    public NetworkObject InstantiateObject(bool isSpawn,NetworkObject spawnObject, Vector2 spawnPosition = default,
        Quaternion rotation = default) {
        var needObject = Instantiate(spawnObject, spawnPosition,rotation);
        
        if (isSpawn)
            needObject.Spawn(true);
        return needObject;
    }
    public void ShowMessageClient(string text) {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.text = GetLocalization(text);
        clientsMessegeText.transform.parent.gameObject.SetActive(true);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    [ClientRpc]
    public void HideMessageClient() {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.transform.parent.gameObject.SetActive(false);
    }
    public Transform InstantiateObject(Transform spawnObject, Transform parent, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity, parent).transform;
    }
}
