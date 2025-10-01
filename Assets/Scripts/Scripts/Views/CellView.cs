using System;
using Leopotam.EcsLite;using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour, IPointerEnterHandler
{
    [field: SerializeField]
    public Image _imageCarPart { get; private set; }
    [field: SerializeField]
    public Image _secondImageCarPart { get; private set; }
    [field: SerializeField]
    public Button _carPartButton { get; private set; }
    [field: SerializeField]
    public Vector2Int _gridPosition { get; private set; }//move to cmp
    public Action<CellView> SetPartFromInventory;
    public int _entity { get;private set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetPartFromInventory?.Invoke(this);
    }
    public void Construct(Vector2Int _gridPosition, Action<CellView> SetPartFromInventory, int _entity, Action<int> SetPanelInfo)
    {
        this._gridPosition = _gridPosition;
        this.SetPartFromInventory = SetPartFromInventory;
        this._entity = _entity;
        _carPartButton.onClick.AddListener(() => SetPanelInfo?.Invoke(_entity));
    }
}
