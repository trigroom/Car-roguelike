using System;
using UnityEngine;

public class QuicksandView : MonoBehaviour {
    private Action<Vector2Int,int> OnTriggerEnterAction;
    private Action<Vector2Int, int > OnTriggerExitAction;

    public void Construct(Action<Vector2Int, int> enter,Action<Vector2Int,int> exit ) {
        OnTriggerEnterAction = enter;
        OnTriggerExitAction = exit;
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.TryGetComponent(out CarPartStuffView carPartStuffView)) {
            OnTriggerEnterAction?.Invoke(carPartStuffView._gridPosition, carPartStuffView._mainEntity);
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.TryGetComponent(out CarPartStuffView carPartStuffView)) {
            OnTriggerExitAction?.Invoke(carPartStuffView._gridPosition, carPartStuffView._mainEntity);
        }
    }
}
