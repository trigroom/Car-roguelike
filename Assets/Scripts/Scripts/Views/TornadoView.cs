using System;
using UnityEngine;

public class TornadoView : MonoBehaviour
{
    private Action<Vector2Int, int, int> OnTriggerStayAction;
    public int entity { get; private set; }
    public void Construct(Action<Vector2Int, int, int> stay, int tornadoEntity) {
       
        OnTriggerStayAction = stay;
        entity = tornadoEntity;
    }
    private void OnTriggerStay2D(Collider2D other) {
        if (other.TryGetComponent(out CarPartStuffView carPartStuffView)) {
            OnTriggerStayAction?.Invoke(carPartStuffView._gridPosition, carPartStuffView._mainEntity, entity);
        }
    }
}
