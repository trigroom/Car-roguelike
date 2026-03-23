using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Core.Environments; // Этой строки не хватает!
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private bool useEncryption = false; // DTLS требует сертификаты, для тестов лучше false
    [SerializeField] private bool useWebSockets = false; // Для WebGL включите true
    public string JoinCode { get; private set; }

    private void Awake()
    {
        
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

    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                Debug.Log("✅ Unity Services already initialized");
                return;
            }

            var options = new InitializationOptions();
            
            #if UNITY_EDITOR
            options.SetEnvironmentName("production");
            #endif

            await UnityServices.InitializeAsync(options);
            
            Debug.Log("✅ Unity Services initialized successfully");
            
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"✅ Signed in as: {AuthenticationService.Instance.PlayerId}");
            };

            AuthenticationService.Instance.SignInFailed += (error) =>
            {
                Debug.LogError($"❌ Sign in failed: {error}");
            };

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to initialize services: {e.Message}");
        }
    }
    private void CleanupNetworkManager()
    {
        if (NetworkManager.Singleton == null) return;
        
        Debug.Log("🧹 Cleaning up NetworkManager before restart...");
        
        // 1. Если есть активная сессия - завершаем её
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // 2. Ждем кадр для завершения всех процессов
        StartCoroutine(DelayedCleanup());
    }
    
    public void StartSinglePlayer()
    {
        Debug.Log("🎮 Starting SINGLE PLAYER mode");
        
        CleanupNetworkManager();
        
        StartCoroutine(StartSinglePlayerCoroutine());
    }

    private System.Collections.IEnumerator StartSinglePlayerCoroutine()
    {
        yield return null;
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager not found in scene!");
            yield break;
        }
        
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            unityTransport.enabled = false;
        }
        
        var singleTransport = NetworkManager.Singleton.GetComponent<SinglePlayerTransport>();
        if (singleTransport == null)
        {
            singleTransport = NetworkManager.Singleton.gameObject.AddComponent<SinglePlayerTransport>();
        }
        singleTransport.enabled = true;
        
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = singleTransport;
        
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("✅ Single player started successfully");
        }
        else
        {
            Debug.LogError("❌ Failed to start single player");
        }
    }
    
    private System.Collections.IEnumerator DelayedCleanup()
    {
        yield return null; // Ждем один кадр
        
        // 3. Принудительно останавливаем транспорт
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Для UnityTransport 2.0+
            if (transport.GetType().GetMethod("Shutdown") != null)
            {
                transport.GetType().GetMethod("Shutdown")?.Invoke(transport, null);
            }
        }
        
        // 4. Сбрасываем NetworkManager в "чистое" состояние
        var networkManagerType = typeof(NetworkManager);
        var fieldInfo = networkManagerType.GetField("s_Singleton", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(null, null); // Сбрасываем синглтон
        }
        
        Debug.Log("✅ NetworkManager cleaned up");
    }
    public async Task<string> StartHostWithRelay()
    {
        try
        {
            // Важно: очищаем NetworkManager перед рестартом
           CleanupNetworkManager();
            
            // Даем время на очистку
            await Task.Delay(100);
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await InitializeUnityServices();
            }

            Debug.Log("📡 Creating Relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            Debug.Log($"✅ Relay allocation created with ID: {allocation.AllocationId}");

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"🔑 Join Code: {JoinCode}");

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("❌ NetworkManager.Singleton is null. Make sure NetworkManager exists in the scene.");
                return null;
            }

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("❌ UnityTransport component not found on NetworkManager");
                return null;
            }

            // НАСТРОЙКА RELAY ДЛЯ TRANSPORT 2.0 - 2.1
            // Используем конструктор RelayServerData с 7 параметрами [citation:5]
            var relayServerData = new RelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.ConnectionData,
                allocation.ConnectionData, // Для хоста HostConnectionData = ConnectionData
                allocation.Key,
                useEncryption // isSecure
            );
            
            transport.SetRelayServerData(relayServerData);
            
            Debug.Log($"✅ Relay configured (Encryption: {useEncryption})");
            Debug.Log("🚀 Starting host...");
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("❌ Failed to start host");
                return null;
            }

            Debug.Log($"✅ Host started successfully. Join code: {JoinCode}");

           /* if (!string.IsNullOrEmpty(gameSceneName))
            {
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }*/
          // no scene change

            return JoinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to start host: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    public async Task<bool> StartClientWithRelay(string joinCode) {
        try {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                await InitializeUnityServices();
            }

            if (string.IsNullOrEmpty(joinCode)) {
                Debug.LogError("❌ Join code is empty");
                return false;
            }

            Debug.Log($"📡 Joining Relay with code: {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"✅ Joined Relay allocation with ID: {joinAllocation.AllocationId}");

            if (!NetcodeBetweenScenesService.Instance.CanJoin())
            {
                Debug.LogWarning("❌ Game already started! Cannot join.");
                return false;
            }
            
            if (NetworkManager.Singleton == null) {
                Debug.LogError("❌ NetworkManager.Singleton is null");
                return false;
            }

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) {
                Debug.LogError("❌ UnityTransport component not found");
                return false;
            }

            // НАСТРОЙКА КЛИЕНТА ДЛЯ TRANSPORT 2.0 - 2.1
            var relayServerData = new RelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, // Для клиента это важно!
                joinAllocation.Key, useEncryption // isSecure
            );

            transport.SetRelayServerData(relayServerData);

            Debug.Log($"✅ Client Relay configured (Encryption: {useEncryption})");

            Debug.Log("🚀 Starting client...");
            bool result = NetworkManager.Singleton.StartClient();

            if (result) {
                Debug.Log("✅ Client started successfully");
            } else {
                Debug.LogError("❌ Failed to start client");
            }

          //  if (NetcodeBetweenScenesService.Instance.CanJoin())

                return result;
          /*  else {
                NetworkManager.Singleton.Shutdown();
                return false;
            }*/
                
        } catch (Exception e) {
            Debug.LogError($"❌ Failed to start client: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public void LeaveSession()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("👋 Host shutting down...");
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("👋 Client disconnecting...");
                NetworkManager.Singleton.Shutdown();
            }
        }
        
     //   SceneManager.LoadScene("Menu");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}