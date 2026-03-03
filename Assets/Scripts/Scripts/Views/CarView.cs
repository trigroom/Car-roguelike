using System;
using System.Collections.Generic;
using UnityEngine;

public class CarView : MonoBehaviour {
    public Rigidbody2D Rigidbody;
    public Transform MainCarContainer;
    public Transform CarPartsContainer;
    public Transform ForwardWheelsContainer;
    public SyncRigidbody2DMassView SyncRigidbodyView;
    public int _entity { get;private set;}
    private Action<int, int> _collisionHitAction;
    
    public void Construct(int entity, Action<int, int> CollisionHitAction, string tag) {
        _entity = entity;
        _collisionHitAction = CollisionHitAction;
        gameObject.tag = tag;
    }

    
    private void OnCollisionEnter2D(Collision2D other) {
     //   _collisionHitAction.Invoke(_entity,other.gameObject.GetComponent<CarView>()._entity); 
    }
}
