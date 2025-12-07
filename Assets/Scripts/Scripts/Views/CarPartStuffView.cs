using System;
using UnityEngine;

public class CarPartStuffView : MonoBehaviour
{
    [field: SerializeField]
    public BoxCollider2D _carPartCollider { get;protected set;}

    private Action<int> _takeDamageAction;
    public Vector2Int _gridPosition { get;private set; }
    [field: SerializeField]
    public int _mainEntity { get;private set; }

    public void SetupCarPart(Vector2Int gridposition, string carPartLayer, int mainEntity) {
        _gridPosition = gridposition;
        gameObject.layer = LayerMask.NameToLayer(carPartLayer);
        _mainEntity = mainEntity;
    }

    public void SetColliderEnableState(bool state) {
        _carPartCollider.enabled = state;
    }
    
    public void MakeTriggerCollider() {
        _carPartCollider.isTrigger = true;
    }
}
