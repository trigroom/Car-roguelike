using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class AnimateScaleButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private Vector3 normalScale;
    
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    
    private RectTransform _rectTransform;
    private Tween _currentTween;
    private bool _buttonIsDisabled;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        normalScale = _rectTransform.localScale;
        _rectTransform.localScale = normalScale;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _currentTween?.Kill();
        
        _currentTween = _rectTransform.DOScale(normalScale*hoverScale, duration)
            .SetEase(easeType);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (_buttonIsDisabled) return;
        _currentTween?.Kill();

        _currentTween = _rectTransform.DOScale(normalScale * clickScale, duration * 0.5f).SetEase(Ease.OutQuad);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _currentTween?.Kill();
        
        _currentTween = _rectTransform.DOScale(normalScale, duration)
            .SetEase(easeType);
    }
    
    public void ResetScale()
    {
        _currentTween?.Kill();
        _rectTransform.localScale = normalScale;
    }
    
    private void OnDisable()=>ResetScale();
 
    
    
    private void OnDestroy() =>_currentTween?.Kill();
  
        
}
