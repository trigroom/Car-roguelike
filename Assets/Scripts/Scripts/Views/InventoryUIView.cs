using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIView : MonoBehaviour
{
     [field: SerializeField]
        public Transform _carStuffContainer { get;private set; }
    [field: SerializeField]
    public GridLayoutGroup _gridLayoutGroup { get;private set; }
    [field: SerializeField]
    public CellView _cellView { get;private set; }
    
    [field: SerializeField]
    public Image _carBackground { get;private set; }
    [field: SerializeField]
    public TMP_Text _pageNumberText { get;private set; }

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
        public Button[] _sortButtons { get;private set; }
    [field: SerializeField]
    public CellView[] _cancelCellPlaceButtons { get;private set; }
    [field: SerializeField]
    public Button _playButton { get;private set; }
}
