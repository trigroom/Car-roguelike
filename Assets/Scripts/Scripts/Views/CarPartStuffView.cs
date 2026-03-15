using System;
using UnityEngine;

public class CarPartStuffView : MonoBehaviour
{
    [field: SerializeField]
    public BoxCollider2D _carPartCollider { get;protected set;}
    public Vector2Int _gridPosition { get;private set; }
    [field: SerializeField]
    public int _mainEntity { get;private set; }
    [field: SerializeField]
    public ulong _clientId { get;private set; }

    public void SetupCarPart(Vector2Int gridPosition, string carPartLayer, int mainEntity, ulong clientId) {
        _gridPosition = gridPosition;
        gameObject.layer = LayerMask.NameToLayer(carPartLayer);
        _mainEntity = mainEntity;
        if (clientId != 0)
            _clientId = clientId;
    }

    public void SetColliderEnableState(bool state) {
        _carPartCollider.enabled = state;
    }
    
    public void MakeTriggerCollider() {
        _carPartCollider.isTrigger = true;
    }
}
