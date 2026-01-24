using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class AnimateScaleButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private Vector3 normalScale;
    
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    
    [SerializeField] private MonoBehaviour targetButton;
    [SerializeField] private RectTransform _rectTransform;
    private Tween _currentTween;
    
    
    
    private void Awake()
    {
        if (!_rectTransform)
            _rectTransform = GetComponent<RectTransform>();
        normalScale = _rectTransform.localScale;
        _rectTransform.localScale = normalScale;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _currentTween?.Kill();
        
        _currentTween = _rectTransform.DOScale(normalScale*hoverScale, duration)
            .SetEase(easeType).SetUpdate(UpdateType.Normal, true);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (targetButton && !targetButton.enabled) return;
        _currentTween?.Kill();

        _currentTween = _rectTransform.DOScale(normalScale * clickScale, duration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(UpdateType.Normal, true);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _currentTween?.Kill();
        
        _currentTween = _rectTransform.DOScale(normalScale, duration)
            .SetEase(easeType).SetUpdate(UpdateType.Normal, true);
    }
    
    public void ResetScale()
    {
        _currentTween?.Kill();
        _rectTransform.localScale = normalScale;
    }
    
    private void OnDisable()=>ResetScale();
 
    
    
    private void OnDestroy() =>_currentTween?.Kill();
  
        
}
