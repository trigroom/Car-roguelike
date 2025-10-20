using UnityEngine;

[CreateAssetMenu(fileName = "GunConfig", menuName = "CarPartConfigs/GunConfig")]
public class GunConfig : CarPartConfig
{
    public Sprite UpPartSprite;
    public Vector2 UpPartPosition;
    
    public float MaxRotateAngle;
    public float RotateSpeed;
    
    public float Damage;
    public float AttackCouldown;
    public int BulletsCountInMagazine;
    public int BulletsCount;
    public float BulletSpeed;
    public float ReloadDuration;
    public MissleType MissleType;
    public Sprite MissleSprite;
    public Vector2 FirePointPosition;
    public float SpreadAngle;
    public float ExplosionRadius;
    public float ImpulseForce;
    public EffectType EffectType;
    public float EffectDuration;
}

public enum MissleType {
    bullet,
    rocket,
    laser
}

public enum EffectType {
    none,
    fire,
    armorWeak,
    shock,
}