using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopButtonView : MonoBehaviour, IPointerDownHandler {
    public int Entity;
    public Button BuyButton;
    public Button ShowInfoButton;
    public Image ItemSprite;
    public Image UpperItemSprite;
    public TMP_Text ItemPriceText;
    public TMP_Text ItemSaleText;

    public Action<int> TakeItemFromInventory;

    public void Construct(int _entity, Action<int> _onBuy, Action<int, bool> _onShowInfo, Action<int> _takeItemFromInventory) {
        BuyButton.onClick.AddListener(() => _onBuy?.Invoke(_entity));
        ShowInfoButton?.onClick.AddListener(() => _onShowInfo?.Invoke(_entity, true));
        Entity = _entity;
        TakeItemFromInventory = _takeItemFromInventory;
    }

    public void SetShopCellInfo(int price, Sprite sprite, Sprite upperSprite, Vector2 upperImageSpritePos, Vector2 upperImageSize) {
        SetPriceText(price);

        ItemSprite.sprite = sprite;
        UpperItemSprite.sprite = upperSprite;
        if (upperImageSize == Vector2.zero)
            return;
        UpperItemSprite.transform.localPosition = upperImageSpritePos;
        UpperItemSprite.rectTransform.sizeDelta = upperImageSize;
    }

    public void SetPriceText(int price) {
        ItemPriceText.text = price + "$";
        BuyButton.enabled = true;
    }

    public void SetShopSaleInfo(int percent) {
        ItemSaleText.gameObject.transform.parent.gameObject.SetActive(percent > 0);
        if (percent == 0) return;
        ItemSaleText.fontSize = 10 + (10f / 80f * percent);
        ItemSaleText.text = percent + "%";
    }

    public void SoldItemChangeButton() {
        //   ItemSprite.sprite = sprite;
        ItemPriceText.text = "sold";
        BuyButton.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (!ItemSprite) return;
        if (BuyButton.enabled && Input.GetMouseButtonDown(0))
            TakeItemFromInventory?.Invoke(Entity);
    }
}
