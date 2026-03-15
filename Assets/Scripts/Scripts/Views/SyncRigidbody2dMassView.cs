using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SyncRigidbody2DMassView : NetworkBehaviour {
    [SerializeField]
    private Rigidbody2D rb;
    public Animator[] ForwardWheels;
    public ParticleSystem[] WheelsParticles;

    private NetworkVariable<float> networkMass = new NetworkVariable<float>(1f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> networkAngle = new NetworkVariable<float>(1f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<NetworkPoints> networkPoints = new NetworkVariable<NetworkPoints>(new NetworkPoints(),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake() {
        rb = GetComponentInChildren<Rigidbody2D>();
        polygonCollider = GetComponentInChildren<PolygonCollider2D>();
    }

    public override void OnNetworkSpawn() {
      
      
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
    [ClientRpc]
    public void SetWheelsRotateClientRpc(float newAngle) {
        var needRot = Quaternion.Euler(0, 0, newAngle);
      //  Debug.Log(needRot+ " len "+ForwardWheels.Length);
        foreach (var wheel in ForwardWheels) 
            wheel.transform.rotation = needRot;
    }

    private PolygonCollider2D polygonCollider;
    
    // Вместо FixedList используем кастомный тип с массивом
   
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
      
            UpdateColliderFromNetworkPoints();
        
    }

    // Метод для обновления коллайдера на клиентах
    private void UpdateColliderFromNetworkPoints()
    {
        if (polygonCollider == null) return;
        
        // Применяем точки из NetworkVariable к коллайдеру
    //    if (networkPoints.Value.Points != null && networkPoints.Value.Points.Length > 0)
   //     {
            polygonCollider.SetPath(0, networkPoints.Value.Points);
     //   }
    }

    private void Update() {
        if (IsOwner) {
            networkAngle.Value = rb.transform.rotation.eulerAngles.z;
            Debug.Log(networkAngle.Value + "         "+ rb.transform.rotation.eulerAngles.z);
        }
            
        else
            rb.transform.rotation = Quaternion.Euler(0, 0, networkAngle.Value);
        //     SetRotationClientRpc(rb.transform.eulerAngles.z, NetcodeBetweenScenesService.Instance.OwnerClientId);
    }

    // Этот метод можно вызывать на сервере, чтобы изменить форму
    public void SetPolygonPath(Vector2[] newPoints)
    {
        if (!IsServer) return;
        
        // Обновляем физический коллайдер на сервере
        polygonCollider.SetPath(0, newPoints);
        
        // Обновляем сетевую переменную
        networkPoints.Value = new NetworkPoints { Points = newPoints };
    }
    
    // Клиенты могут запросить изменение через ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void ChangePolygonServerRpc(Vector2[] newPoints)
    {
        SetPolygonPath(newPoints);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestPushImpulseServerRpc(Vector2 force)
    {
        // Сервер применяет силу
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestPushForceServerRpc(Vector2 force)
    {
        // Сервер применяет силу
        Debug.Log($"Try sent force: {force}");
        rb.AddForce(force, ForceMode2D.Force);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestSetAngularVelocityServerRpc(float angularVelocity, float wheelAngle, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"{angularVelocity} wheel angle {wheelAngle} ");
        SetWheelsRotateClientRpc(wheelAngle);
     
       rb.angularVelocity = angularVelocity;
     //  SetAngularVelocityClientRpc(angularVelocity);
    }
    
    [ClientRpc]
    public void SetRotationClientRpc(float angularVelocity, ulong clientId) {
        Debug.Log($"{angularVelocity} av + need id {clientId} + owner {OwnerClientId}");
       // if (clientId == OwnerClientId)return;
        
       // Debug.Log(angularVelocity);
            rb.transform.rotation = Quaternion.Euler(0, 0, angularVelocity);
    }
}
