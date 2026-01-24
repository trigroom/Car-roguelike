using System;
using UnityEngine;

public class MapMeleeWeapon : MonoBehaviour
{
    private Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction;

    public void ConstructMapMelee(Action<AttackType, int, Vector2Int, int, Vector2, ProjectileView, int, string> hitTargetAction) {
        this.hitTargetAction = hitTargetAction;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.TryGetComponent(out CarPartStuffView damagedCarPart)) {
            hitTargetAction?.Invoke(AttackType.Melee, 0, damagedCarPart._gridPosition, damagedCarPart._mainEntity,
                other.transform.position, null,-1,null);
        }
    }
}
