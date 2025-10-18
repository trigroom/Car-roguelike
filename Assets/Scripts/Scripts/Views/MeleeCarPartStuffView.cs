using System;
using UnityEngine;

public class MeleeCarPartStuffView : CarPartStuffView {
    private Action<AttackType,int, Vector2Int,int,Vector2,ProjectileView,int,string> hitTargetAction;
    private string _needTag;
    private int _weaponId;
    private int _entity;

    public void ConstructMelee(SpriteRenderer carPartSprite, Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction, string tag,
        int weaponId, int entity) {
        this.hitTargetAction = hitTargetAction;
        _needTag = tag;
        _weaponId = weaponId;
        _entity = entity;
        Bounds spriteBounds = carPartSprite.sprite.bounds;

        _carPartCollider.size = spriteBounds.size;
        _carPartCollider.offset = spriteBounds.center;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer(_needTag)) {
            var damagedCarPart = other.gameObject.GetComponent<CarPartStuffView>();
            hitTargetAction?.Invoke(AttackType.Melee, _weaponId, damagedCarPart._gridPosition, damagedCarPart._mainEntity, Vector2.zero, null,
                _entity, _needTag);
        }
    }
}
