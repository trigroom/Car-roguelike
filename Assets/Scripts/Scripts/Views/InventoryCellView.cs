using System;
using Leopotam.EcsLite;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellView : MonoBehaviour, IPointerDownHandler
{
   // private EcsWorld _world;
    public int _entity { get; private set; }
   [field: SerializeField]
   public Button _buttonInventoryCell { get; private set; }
    [field: SerializeField]
    public Image _inventoryCellImage { get; private set; }
    [field: SerializeField]
    public Image _inventoryUpCellImage { get; private set; }
    [SerializeField]
    private int _cellId;
    
    public Action<int> TakeItemFromInventory;

    public void OnPointerDown(PointerEventData eventData) {
        if (Input.GetMouseButtonDown(0))
            TakeItemFromInventory?.Invoke(_entity);
        // _world.GetPool<TakeInventoryItemEvent>().Add(_entity);
    }

    public void SetInventoryCellSprite(Sprite sprite, Sprite upSprite, Vector2 size, Vector2 position) {
        _inventoryCellImage.sprite = sprite;
        _inventoryUpCellImage.sprite = upSprite;
        if(size == Vector2.zero)return;
        _inventoryUpCellImage.rectTransform.sizeDelta = size;
        _inventoryUpCellImage.rectTransform.localPosition = position;
    }

    public void Construct( /*EcsWorld _world,*/ int _entity, Action<int> _takeItemFromInventory, int _cellId, Action<int> SetPanelInfo) {
         this._entity = _entity;
         /*this._world = _world;*/

        _buttonInventoryCell.onClick.AddListener(() => SetPanelInfo?.Invoke(_entity));
        this.TakeItemFromInventory = _takeItemFromInventory;
        this._cellId = _cellId;
    }
}
