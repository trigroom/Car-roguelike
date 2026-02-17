using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    // Вызывается на сервере при загрузке сцены
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Подписываемся на события подключения клиентов
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Выбираем точку спавна (можно сделать более сложную логику)
        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Создаем игрока
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Получаем NetworkObject
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        
        // Спавним с привязкой к конкретному клиенту
        networkObject.SpawnAsPlayerObject(clientId);
        
        Debug.Log($"🎮 Spawned player for client {clientId} at position {spawnPoint.position}");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}