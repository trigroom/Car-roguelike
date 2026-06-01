using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapPointColiderView : MonoBehaviour {
    [SerializeField]
    private CircleCollider2D _pointCollider;
    private Action _triggerEnterAction;
    [SerializeField]
    public SpriteRenderer _spriteRenderer;
    [SerializeField]
    public Sprite[] _pointSprites;
    

    public void SetupPoint(Action triggerEnterAction) {
        
        _triggerEnterAction = triggerEnterAction;
    }

    public void ChangePosition(Vector2 newPos) {
        gameObject.transform.position = newPos;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(!other.gameObject.CompareTag("Player") )return;
        _triggerEnterAction.Invoke();
        _spriteRenderer.sprite = _pointSprites[Random.Range(0, _pointSprites.Length)];
    }
}
