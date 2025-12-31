using UnityEngine;

[CreateAssetMenu(fileName = "MeleeWeaponConfig", menuName = "CarPartConfigs/MeleeWeaponConfig")]
public class MeleeWeaponConfig : CarPartConfig {
    public float Damage;
    public float AttackCountdown;
    public float DamageAddedFromOneSpeed;
    public Sprite UpSprite;
    public Vector2 UpSpritePosition;

    public bool IsAttackAfterCountdown;
    public float FuelConsumption;
}
