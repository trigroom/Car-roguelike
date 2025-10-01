using System;
using UnityEngine;

public class MeleeCarPartStuffView : CarPartStuffView {
    private Action<AttackType,int, int,Vector2,ProjectileView,int,string> hitTargetAction;
    private string needTag;
    private int weaponId;

    public void ConstructMelee(Action<AttackType,int, int,Vector2, ProjectileView,int,string> hitTargetAction, string tag, int weaponId) {
        this.hitTargetAction = hitTargetAction;
        this.needTag = tag;
        this.weaponId = weaponId;

        Bounds spriteBounds = _carPartSpriteRenderer.sprite.bounds;

        _carPartCollider.size = spriteBounds.size;
        _carPartCollider.offset = spriteBounds.center;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer(needTag))
            hitTargetAction?.Invoke(AttackType.Melee,weaponId, other.gameObject.GetComponent<CarPartStuffView>()._entity, Vector2.zero,null,_entity,needTag);
    }
}
