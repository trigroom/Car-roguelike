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
    public Vector2Int _gridPosition;
    public Action<CellView> SetPartFromInventory;
    public int _entity { get;private set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("On pointer");
        SetPartFromInventory?.Invoke(this);
    }

    public void Construct(Vector2Int _gridPosition, Action<CellView> SetPartFromInventory, int _entity, Action<int, bool> SetPanelInfo) {
        this._gridPosition = _gridPosition;
        this.SetPartFromInventory = SetPartFromInventory;
        this._entity = _entity;
        if (_carPartButton != null)
            _carPartButton.onClick.AddListener(() => SetPanelInfo?.Invoke(_entity, true));
    }
}
