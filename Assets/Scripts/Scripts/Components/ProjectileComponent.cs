
struct ProjectileComponent {
    public float LifeTime;
    public bool IsActive;

    public float ImpulseForce;
    public float ExplosionRadius;
    public int CurrentBreakingThroughCount;
    public float Damage;

    public int MainEntity;
    
    public ProjectileView ProjectileView;
}
