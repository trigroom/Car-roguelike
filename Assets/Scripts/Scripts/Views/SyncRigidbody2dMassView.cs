using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SyncRigidbody2DMassView : NetworkBehaviour {
    private Rigidbody2D rb;

    private NetworkVariable<float> networkMass = new NetworkVariable<float>(1f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server 
    );

    public override void OnNetworkSpawn() {
        rb = GetComponent<Rigidbody2D>();
        polygonCollider = GetComponent<PolygonCollider2D>();
      
        networkMass.OnValueChanged += OnMassChanged;
        networkPoints.OnValueChanged += OnPointsChanged;
    }

    public override void OnNetworkDespawn() {
        networkMass.OnValueChanged -= OnMassChanged;
        networkPoints.OnValueChanged -= OnPointsChanged;
    }

    private void OnMassChanged(float oldValue, float newValue) {
        rb.mass = newValue;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMassServerRpc(float newMass) {
        rb.mass = newMass;
        networkMass.Value = newMass;
    }
    
    
    
  private PolygonCollider2D polygonCollider;
    
    // Вместо FixedList используем кастомный тип с массивом
    private NetworkVariable<NetworkPoints> networkPoints = 
        new NetworkVariable<NetworkPoints>(
            new NetworkPoints(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Кастомная структура для сериализации массива точек
    [System.Serializable]
    public struct NetworkPoints : INetworkSerializable
    {
        public Vector2[] Points;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Работаем с длиной массива
            int length = Points?.Length ?? 0;
            serializer.SerializeValue(ref length);
            
            if (serializer.IsReader)
            {
                Points = new Vector2[length];
            }
            
            for (int i = 0; i < length; i++)
            {
                Vector2 point = Points[i];
                serializer.SerializeValue(ref point);
                Points[i] = point;
            }
        }
    }


    private void OnPointsChanged(NetworkPoints oldValue, NetworkPoints newValue)
    {
        // Когда сервер обновляет точки, клиенты применяют новую форму
        if (!IsServer)
        {
            UpdateColliderFromNetworkPoints();
        }
    }

    // Метод для обновления коллайдера на клиентах
    private void UpdateColliderFromNetworkPoints()
    {
        if (polygonCollider == null) return;
        
        // Применяем точки из NetworkVariable к коллайдеру
        if (networkPoints.Value.Points != null && networkPoints.Value.Points.Length > 0)
        {
            polygonCollider.SetPath(0, networkPoints.Value.Points);
        }
    }

    // Метод для обновления сетевых точек с коллайдера (вызывается на сервере)
    private void UpdateNetworkPointsFromCollider()
    {
        if (!IsServer || polygonCollider == null) return;
        
        var points = polygonCollider.GetPath(0);
        networkPoints.Value = new NetworkPoints { Points = points };
    }

    // Этот метод можно вызывать на сервере, чтобы изменить форму
    public void SetPolygonPathOnServer(Vector2[] newPoints)
    {
        if (!IsServer) return;
        
        // Обновляем физический коллайдер на сервере
        polygonCollider.SetPath(0, newPoints);
        
        // Обновляем сетевую переменную
        networkPoints.Value = new NetworkPoints { Points = newPoints };
    }
    
    // Клиенты могут запросить изменение через ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void ChangePolygonServerRpc(Vector2[] newPoints, ServerRpcParams rpcParams = default)
    {
        SetPolygonPathOnServer(newPoints);
    }
    
    [ServerRpc]
    public void RequestPushImpulseServerRpc(Vector2 force)
    {
        // Сервер применяет силу
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    [ServerRpc]
    public void RequestPushForceServerRpc(Vector2 force)
    {
        // Сервер применяет силу
        rb.AddForce(force, ForceMode2D.Force);
    }
    
    [ServerRpc]
    public void RequestSetAngularVelocityServerRpc(float angularVelocity)
    {
        // Сервер устанавливает угловую скорость
        rb.angularVelocity = angularVelocity;
    }
}
