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
    public TMP_Text _expCountText { get; private set; }
    [field:SerializeField]
    public TMP_Text _updateItemsText { get; private set; }
     [field:SerializeField]
        public Sprite _lockSprite { get; private set; }
        [field:SerializeField]
        public Sprite _unlockSprite { get; private set; }
        [field:SerializeField]
        public TMP_Text _inflationText { get; private set; }

    public void AddMoneyAnimation() {
        PopAnimation(_moneyCountText.transform);
    }
    public void AddExpAnimation() {
        PopAnimation(_expCountText.transform);
    }
    private void PopAnimation(Transform needObject) {
        needObject.DOKill();
        needObject.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            needObject.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
    }
    private void NotEnoughAnimation(Transform needObject) {
        needObject.DOKill();
        needObject.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
            needObject.DOScale(1f, 0.2f).SetEase(Ease.InBack));
    }
    public void NotEnoughMoneyAnimation() {
        NotEnoughAnimation(_moneyCountText.transform);
    }
    public void NotEnoughExpAnimation() {
        NotEnoughAnimation(_expCountText.transform);
    }
}
