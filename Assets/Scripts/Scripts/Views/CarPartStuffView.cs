using System;
using UnityEngine;

public class CarPartStuffView : MonoBehaviour
{
    [field: SerializeField]
    public SpriteRenderer _carPartSpriteRenderer { get;protected set;}
    [field: SerializeField]
    public BoxCollider2D _carPartCollider { get;protected set;}

    private Action<int> _takeDamageAction;
    public int _entity { get;private set; }

    public void SetupCarPart(int cellEntity, string carPartLayer) {
        _entity = cellEntity;
        gameObject.layer = LayerMask.NameToLayer(carPartLayer);
    }

    public void SetColliderEnableState(bool state) {
        _carPartCollider.enabled = state;
    }
    
    public void MakeTriggerCollider() {
        _carPartCollider.isTrigger = true;
    }
}
