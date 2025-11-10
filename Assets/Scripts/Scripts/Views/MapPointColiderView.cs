using System;
using UnityEngine;

public class MapPointColiderView : MonoBehaviour {
    [SerializeField]
    private CircleCollider2D _pointCollider;
    private Action _triggerEnterAction;

    public void SetupPoint(Action triggerEnterAction) {
        
        _triggerEnterAction = triggerEnterAction;
    }

    public void ChangePosition(Vector2 newPos) {
        gameObject.transform.position = newPos;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(!other.gameObject.CompareTag("Player") )return;
        _triggerEnterAction.Invoke();
       
    }
}
