using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIView : MonoBehaviour
{
    [field: SerializeField]
    public RectTransform _rotateCarPartIconContainer { get;private set; }
    [field: SerializeField]
    public TMP_Text _healCarText { get;private set; }
    [field: SerializeField]
    public Button _healCarButton { get;private set; }
    [field: SerializeField]
    public Transform _carPartsUpContainer { get;private set; }
    [field: SerializeField]
    public RectTransform _carPartsDownContainer { get;private set; }
    [field: SerializeField]
    public Transform _carPartsContainer { get;private set; }
    [field: SerializeField]
    public TMP_Text _upgradeCarButtonText { get;private set; }
    [field: SerializeField]
    public Button _upgradeCarButton { get;private set; }
    [field: SerializeField]
    public CanvasGroup _carPartsUpLevelContainer { get;private set; }
    [field: SerializeField]
    public TMP_Text _changeToRedactModeText { get;private set; }
    [field: SerializeField]
    public Button _changeToRedactModeButton { get;private set; }
    [field: SerializeField]
    public Image _changeToRedactModeImage{ get;private set; }
    [field: SerializeField]
    public Sprite _changeToRedactCarPartsModeSprite { get;private set; }
    [field: SerializeField]
    public Sprite _changeToRedactCarContainersModeSprite { get;private set; }
     [field: SerializeField]
        public Transform _carStuffContainer { get;private set; }
    [field: SerializeField]
    public GridLayoutGroup _gridLayoutGroup { get;private set; }
    [field: SerializeField]
    public CellView _cellView { get;private set; }
    
    [field: SerializeField]
    public TMP_Text _pageNumberText { get;private set; }

    [field: SerializeField]
    public Image _draggedInventoryCellUp { get;private set; }
    [field: SerializeField]
    public Image _draggedInventoryCell { get;private set; }
    [field: SerializeField]
    public Button _backInventoryCellsButton { get;private set; }
    
    [field: SerializeField]
    public Button _nextInventoryCellsButton { get;private set; }
    [field: SerializeField]
    public InventoryCellView[] _inventoryCellViews { get;private set; }
    [field: SerializeField]
    public Transform _inventoryCellViewContainer { get;private set; }
    [field: SerializeField]
    public Transform _inventoryArrowsContainer { get;private set; }
    
    [field: SerializeField]
        public ButtonWithSpritesView[] _sortButtons { get;private set; }
    [field: SerializeField]
    public Sprite[] _sortButtonsActiveSprites { get;private set; }
    [field: SerializeField]
    public Sprite[] _sortButtonsUnactiveSprites { get;private set; }
    [field: SerializeField]
    public CellView[] _cancelCellPlaceButtons { get;private set; }
    [field: SerializeField]
    public Button _playButton { get;private set; }
    [field: SerializeField]
    public SelectCarButtonView[] _selectCarButtons { get;private set; }
    
    public void SetUIBottomRight() {
        SetToBottomRight(_rotateCarPartIconContainer, _draggedInventoryCell.rectTransform);
    }

    private float _lastRotateAngle;

    public void SetToBottomRight(RectTransform target, RectTransform reference) {
        Vector3[] refCorners = new Vector3[4];
        Vector3[] targetCorners = new Vector3[4];

        reference.GetWorldCorners(refCorners);
        int cornerIndex;
        float speedMultiplayer;
        switch (reference.localEulerAngles.z) {
            case < 90:
                cornerIndex = 3;
                speedMultiplayer = reference.sizeDelta.y;
                break;
            case < 180:
                cornerIndex = 0;
                speedMultiplayer = reference.sizeDelta.x;
                break;
            case < 270:
                cornerIndex = 1;
                speedMultiplayer = reference.sizeDelta.y;
                break;
            case < 360:
                cornerIndex = 2;
                speedMultiplayer = reference.sizeDelta.x;
                break;
            default:
                return;
                break;
        }

        Vector3 refBottomRightWorld = refCorners[cornerIndex];

        RectTransform parentRect = target.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, RectTransformUtility.WorldToScreenPoint(null, refBottomRightWorld),
            null, out localPos);
        if (_lastRotateAngle != reference.localEulerAngles.z)
            target.anchoredPosition = Vector3.MoveTowards(target.anchoredPosition, localPos, 5 * speedMultiplayer * Time.deltaTime);
        else
            target.anchoredPosition = localPos;

        _lastRotateAngle = reference.localEulerAngles.z;
    }
}
