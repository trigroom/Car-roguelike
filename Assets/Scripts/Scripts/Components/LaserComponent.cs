using DG.Tweening;
using TMPro;
using UnityEngine;

public struct LaserComponent {
    public LineRenderer LaserLineRenderer;
    public TMP_Text LaserDamageText;
    public int LastDamagedEntity;
    public float DamageCountForLastPart;
    public Sequence CurrentSequence;
}