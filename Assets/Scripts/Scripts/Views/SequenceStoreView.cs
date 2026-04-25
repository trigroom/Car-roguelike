using System;
using DG.Tweening;
using UnityEngine;

public class SequenceStoreView : MonoBehaviour
{
    public Sequence ImageSequence;
    public Transform TargetTransform;
    public Vector2 OffsetPosition;

    private void Update() {
        if (TargetTransform == null) return;
        Vector3 worldOffset = TargetTransform.TransformDirection(OffsetPosition);
        transform.position = TargetTransform.position + worldOffset;
    }
}
