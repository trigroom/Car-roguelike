using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopButtonView : MonoBehaviour, IPointerDownHandler {
    public int _entity;
    public Button BuyButton;
    public Button ShowInfoButton;
    public Image ItemSprite;
    public TMP_Text ItemPriceText;
    public TMP_Text ItemSaleText;

    public Action<int> TakeItemFromInventory;

    public void Construct(int _entity, Action<int> _onBuy, Action<int> _onShowInfo, Action<int> _takeItemFromInventory) {
        BuyButton.onClick.AddListener(() => _onBuy?.Invoke(_entity));
        ShowInfoButton.onClick.AddListener(() => _onShowInfo?.Invoke(_entity));
        this._entity = _entity;
        this.TakeItemFromInventory = _takeItemFromInventory;
    }

    public void SetShopCellInfo(int price, Sprite sprite) {
        ItemSprite.sprite = sprite;
        ItemPriceText.text = price + "$";
        BuyButton.enabled = true;
    }
    
    public void SetShopSaleInfo(int percent) {
        ItemSaleText.text = percent + "%";
        ItemSaleText.gameObject.transform.parent.gameObject.SetActive(percent > 0);
    }

    public void SoldItemChangeButton() {
        //   ItemSprite.sprite = sprite;
        ItemPriceText.text = "sold";
        BuyButton.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (BuyButton.enabled && Input.GetMouseButtonDown(0))
            TakeItemFromInventory?.Invoke(_entity);
        // _world.GetPool<TakeInventoryItemEvent>().Add(_entity);
    }
}
