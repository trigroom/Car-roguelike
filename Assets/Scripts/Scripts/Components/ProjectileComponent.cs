
using UnityEngine;

struct ProjectileComponent {
    public float LifeTime;
    public bool IsActive;

    public float ExplosionRadius;
    public int CurrentBreakingThroughCount;
    public float Damage;
    public int GunId;

    public int MainEntity;
    public Transform TargetToMove;
    
    public ProjectileView ProjectileView;
}
