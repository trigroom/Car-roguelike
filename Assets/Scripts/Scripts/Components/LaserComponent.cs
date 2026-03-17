using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public struct LaserComponent {
    public LineRenderer LaserLineRenderer;
    public NetworkObject LaserDamageText;
    public int LastDamagedEntity;
    public ulong LastDamagedPlayerId;
    public Vector2Int LastDamagedCarPartPosition;
    public bool TextIsAdded;
    public float DamageCountForLastPart;
    public Sequence CurrentSequence;
}