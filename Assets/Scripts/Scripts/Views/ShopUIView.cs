using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIView : MonoBehaviour
{
    [field:SerializeField]
    public Button _upgradeShopButton { get; private set; }
    [field:SerializeField]
    public TMP_Text _upgradeShopButtonText { get; private set; }
    [field:SerializeField]
    public TMP_Text _shopChancesText { get; private set; }
    [field:SerializeField]
    public ShopButtonView[] _buttonsCarShop { get; private set; }
    [field:SerializeField]
    public Transform _shopCarContainer { get; private set; }
    [field:SerializeField]
    public Transform _shopCarPartsContainer { get; private set; }
     [field:SerializeField]
        public Button _shopModeButton { get; private set; }
        [field:SerializeField]
        public TMP_Text _shopModeText { get; private set; }
    [field:SerializeField]
    public ShopButtonView[] _buttonsShop { get; private set; }
    [field:SerializeField]
    public Button _buttonUpdateShop { get; private set; }
    [field:SerializeField]
    public TMP_Text _moneyCountText { get; private set; }
    [field:SerializeField]
    public TMP_Text _updateItemsText { get; private set; }
     [field:SerializeField]
        public Sprite _lockSprite { get; private set; }
        [field:SerializeField]
        public Sprite _unlockSprite { get; private set; }
        [field:SerializeField]
        public TMP_Text _inflationText { get; private set; }

    public void AddMoneyAnimation() {
        _moneyCountText.transform.DOKill();
        _moneyCountText.transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        _moneyCountText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
    }
    
    public void NotEnoughMoneyAnimation() {
        _moneyCountText.transform.DOKill();
        _moneyCountText.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
            _moneyCountText.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
    }
}
