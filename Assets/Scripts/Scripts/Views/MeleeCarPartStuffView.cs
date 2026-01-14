using System;
using UnityEngine;

public class MeleeCarPartStuffView : CarPartStuffView {
    private Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction;
    private string _needTag;

    public LayerMask _layerMask { get; private set; }

    private int _weaponId;
    private int _entity;

    public void ConstructMelee(Sprite carPartSprite,
        Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction, string tag, int weaponId, int entity) {
        this.hitTargetAction = hitTargetAction;
        _needTag = tag;
        _weaponId = weaponId;
        _entity = entity;
        Bounds spriteBounds = carPartSprite.bounds;

        _carPartCollider.size = spriteBounds.size;
        _carPartCollider.offset = spriteBounds.center;
        if (tag == "Player")
            _layerMask = LayerMask.GetMask("CarBase", "UpCarPart", "Obstacle");
        else
            _layerMask = LayerMask.GetMask("CarBaseEnemy", "UpCarPartEnemy", "Obstacle", "PlayerDamageable");
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if ((_layerMask & (1 << other.gameObject.layer)) != 0 && other.gameObject.TryGetComponent(out CarPartStuffView damagedCarPart)) {
            hitTargetAction?.Invoke(AttackType.Melee, _weaponId, damagedCarPart._gridPosition, damagedCarPart._mainEntity,
                other.transform.position, null, _entity, _needTag);
        }
    }
}
