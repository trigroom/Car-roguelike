using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class ProjectileView : MonoBehaviour {
    public int entity;
    private int gunId;
    private Action<AttackType,int, Vector2Int,int,Vector2,ProjectileView,int, string> hitTargetAction;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    public string needTag { get;private set; }
    [SerializeField]
    private BoxCollider2D collider;
    [SerializeField]
    private ParticleSystem particleSystem;
    [field: SerializeField]
    public Rigidbody2D rigidbody2D { get; private set; }
    private Sequence animationSequence;
    private Collider2D lastCollider;

    [SerializeField]
    private Material _defaultMaterial;
    [field: SerializeField]
    public NetworkObject networkObject { get; private set; }
    public void Construct(Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction, int gunId,
        Sprite sprite, string tag, Material projectileMaterial) {
        this.hitTargetAction = hitTargetAction;
        this.gunId = gunId;
        spriteRenderer.sprite = sprite;
        spriteRenderer.material = projectileMaterial;
        spriteRenderer.color = tag == "Enemy" ? Color.white : projectileMaterial.GetColor("_OffsetColor");
        needTag = tag;
        collider.enabled = true;
        Bounds spriteBounds = spriteRenderer.bounds;

        collider.size = spriteBounds.size -
                        new Vector3(2f / GridUtils.InWorldCellSize, 2f / GridUtils.InWorldCellSize, 2f / GridUtils.InWorldCellSize);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle")) {
            if (other.gameObject.TryGetComponent(out CarPartStuffView damagedCarPart))
                hitTargetAction?.Invoke(AttackType.Projectile, gunId, damagedCarPart._gridPosition, damagedCarPart._mainEntity,
                    EstimateCollisionPoint(other, collider), this, entity, needTag);
            else
                hitTargetAction?.Invoke(AttackType.Projectile, gunId, -Vector2Int.one, -1, EstimateCollisionPoint(other, collider), this,
                    entity, needTag);
        } else if (other.CompareTag(needTag)) {
            lastCollider = other;

            var damagedCarPart = other.gameObject.GetComponent<CarPartStuffView>();

            hitTargetAction?.Invoke(AttackType.Projectile, gunId, damagedCarPart._gridPosition, damagedCarPart._mainEntity,
                EstimateCollisionPoint(other, collider), this, entity, needTag);
        }
    }

    public void AnimateProjectile() {
        if(lastCollider == null)return;
        var other = lastCollider;
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
        } else // Верх/низ
        {
            bool isTopSide = localDir.y > 0;
            targetAngle = isTopSide ? 0f : 180f;
        }

        targetAngle += other.transform.eulerAngles.z - 180;
        if (targetAngle >= 360)
            targetAngle -= 360;
        transform.eulerAngles = new Vector3(0, 0, +targetAngle);
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
        spriteRenderer.color = Color.white;
        spriteRenderer.material = _defaultMaterial;
        AnimateProjectile();
        lastCollider = null;
        collider.enabled = false;
        var main = particleSystem.main;
        main.startSpeed = radius * 1.5f + 2;
        var emission = particleSystem.emission;

        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 10 + (int)radius * 5) 
        });
        if (animationSprites != null) {
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
        } else {
            hideAction?.Invoke(this);
        }
    }
}
