using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FadeButtonTextView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Component")]
    [SerializeField]
    private TMP_Text _targetText;

    [Header("Color Settings")]
    [SerializeField]
    private Color _normalColor = Color.white;
    [SerializeField]
    private Color _hoverColor = Color.yellow;

    [Header("Animation Settings")]
    [SerializeField]
    private float _duration = 0.3f;
    [SerializeField]
    private Ease _easeType = Ease.OutCubic;

    public void OnPointerEnter(PointerEventData eventData) {
        _targetText.DOKill();
        _targetText.DOColor(_hoverColor, _duration).SetEase(_easeType);
    }

    public void OnPointerExit(PointerEventData eventData) {
        _targetText.DOKill();
        _targetText.DOColor(_normalColor, _duration).SetEase(_easeType);
    }

    private void OnDisable() {
        _targetText.DOKill();
        _targetText.color = _normalColor;
    }
}
