using System;
using System.Collections.Generic;
using UnityEngine;

public class CarView : MonoBehaviour {
    public Rigidbody2D Rigidbody;
    public Transform MainCarContainer;
    public Transform CarPartsContainer;
    public Transform ForwardWheelsContainer;
    public SyncRigidbody2DMassView SyncRigidbodyView;
    public CarPartStuffView[,] CarPartStuffViews;
    public Dictionary<int, SpriteRenderer> CarPartSprites = new Dictionary<int, SpriteRenderer>();
    public Dictionary<int,List< Transform>> Wheels = new Dictionary<int, List< Transform>>();
    
    public int _entity { get;private set;}
    private Action<int, int> _collisionHitAction;
    
    
    public void Construct(int entity, Action<int, int> CollisionHitAction, string tag) {
        _entity = entity;
        _collisionHitAction = CollisionHitAction;
        gameObject.tag = tag;
    }

    public void SetupForwardWheels(Animator[] wheels, ParticleSystem[] wheelsParticles) {
        SyncRigidbodyView.ForwardWheels = wheels;
        SyncRigidbodyView.WheelsParticles = wheelsParticles;
    }

    private void Update() {
        ForwardWheelsContainer.transform.position = transform.position;
        ForwardWheelsContainer.transform.rotation = transform.rotation;
    }

    private void OnCollisionEnter2D(Collision2D other) {
     //   _collisionHitAction.Invoke(_entity,other.gameObject.GetComponent<CarView>()._entity); 
    }
}
