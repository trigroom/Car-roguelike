using System;
using UnityEngine;

public class QuicksandView : MonoBehaviour {
    private Action<Vector2Int, int, int> OnTriggerEnterAction;
    private Action<Vector2Int, int, int> OnTriggerExitAction;
    public int entity { get; private set; }

    public void Construct(Action<Vector2Int, int, int> enter, Action<Vector2Int, int, int> exit, int quicksandEntity) {
        OnTriggerEnterAction = enter;
        OnTriggerExitAction = exit;
        entity = quicksandEntity;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.TryGetComponent(out CarPartStuffView carPartStuffView)) {
            OnTriggerEnterAction?.Invoke(carPartStuffView._gridPosition, carPartStuffView._mainEntity, entity);
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.TryGetComponent(out CarPartStuffView carPartStuffView)) {
            OnTriggerExitAction?.Invoke(carPartStuffView._gridPosition, carPartStuffView._mainEntity, entity);
        }
    }
}
