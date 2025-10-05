using System;
using DG.Tweening;
using UnityEngine;

public class ProjectileView : MonoBehaviour {
    public int entity;
    private int gunId;
    private Action<AttackType,int, int,Vector2,ProjectileView,int, string> hitTargetAction;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    private string needTag;
    [SerializeField]
    private Collider2D collider;
    [SerializeField]
    private ParticleSystem particleSystem;
    [field: SerializeField]
    public Rigidbody2D rigidbody2D { get; private set; }
    private Sequence animationSequence;

    public void Construct( Action<AttackType,int, int,Vector2, ProjectileView, int, string> hitTargetAction, int gunId, Sprite sprite, string tag) {
        this.hitTargetAction = hitTargetAction;
        this.gunId = gunId;
        spriteRenderer.sprite = sprite;
        needTag = tag;
        collider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag(needTag)) {
            Vector2 direction = other.bounds.center - transform.position;
        
            // Определяем ближайшую сторону квадрата
            Vector2 localDir = other.transform.InverseTransformDirection(direction);
            float absX = Mathf.Abs(localDir.x);
            float absY = Mathf.Abs(localDir.y);
        
            float targetAngle = 0f;
        
            // Определяем сторону столкновения
            if (absX > absY) // Боковые стороны
            {
                bool isRightSide = localDir.x > 0;
                targetAngle = isRightSide ? 90f : 270f;
            
            }
            else // Верх/низ
            {
                bool isTopSide = localDir.y > 0;
                targetAngle = isTopSide ? 0f : 180f;
            
            }

            targetAngle += other.transform.eulerAngles.z-180;
            if(targetAngle >= 360)
                targetAngle -= 360;
            transform.eulerAngles = new Vector3(0, 0, + targetAngle);
             hitTargetAction?.Invoke (AttackType.Projectile,gunId, other.gameObject.GetComponent<CarPartStuffView>()._entity,EstimateCollisionPoint(other, collider)
                 ,this, entity, needTag);
             
        }
           
    }
    
    Vector2 EstimateCollisionPoint(Collider2D colliderA, Collider2D colliderB)
    {
        // Получаем bounds обоих коллайдеров
        Bounds boundsA = colliderA.bounds;
        Bounds boundsB = colliderB.bounds;
    
        // Находим пересечение bounds
        boundsA.Intersects(boundsB);
    
        // Возвращаем центр пересечения (примерная точка столкновения)
        return boundsA.ClosestPoint(boundsB.center);
    }

    public void PlayHitAnimation(Sprite[] animationSprites, float radius, Action<ProjectileView> hideAction) {
        var main = particleSystem.main;
        main.startSpeed = radius * 1.5f + 2;
        var emission = particleSystem.emission;

        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 10 + (int)radius * 5) 
        });
        particleSystem.Play();
        animationSequence = DOTween.Sequence();

        for (int i = 0; i < animationSprites.Length; i++) {
            int index = i;

            animationSequence.AppendCallback(() => {
                if (index < animationSprites.Length)
                    spriteRenderer.sprite = animationSprites[index];
            });

            animationSequence.AppendInterval(0.15f);
        }

        animationSequence.OnComplete(() => { hideAction?.Invoke(this); });

        animationSequence.Play();
    }
}
