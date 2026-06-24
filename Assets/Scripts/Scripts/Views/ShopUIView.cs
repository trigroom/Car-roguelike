using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIView : MonoBehaviour {
    [field: SerializeField]
    public Button _upgradeShopButton { get; private set; }

    [field: SerializeField]
    public TMP_Text _upgradeShopButtonText { get; private set; }

    [field: SerializeField]
    public TMP_Text _shopChancesText { get; private set; }

    [field: SerializeField]
    public ShopButtonView[] _buttonsCarShop { get; private set; }

    [field: SerializeField]
    public Transform _shopCarContainer { get; private set; }

    [field: SerializeField]
    public Transform _shopCarPartsContainer { get; private set; }

    [field: SerializeField]
    public Button _shopModeButton { get; private set; }

    [field: SerializeField]
    public TMP_Text _shopModeText { get; private set; }

    [field: SerializeField]
    public ShopButtonView[] _buttonsShop { get; private set; }

    [field: SerializeField]
    public Button _buttonUpdateShop { get; private set; }

    [field: SerializeField]
    public TMP_Text _moneyCountText { get; private set; }

    [field: SerializeField]
    public TMP_Text _expCountText { get; private set; }

    [field: SerializeField]
    public TMP_Text _updateItemsText { get; private set; }

    [field: SerializeField]
    public Sprite _lockSprite { get; private set; }

    [field: SerializeField]
    public Sprite _unlockSprite { get; private set; }

    [field: SerializeField]
    public TMP_Text _inflationText { get; private set; }

    [field: SerializeField]
    public Button _buttonAskForMoney { get; private set; }

    [field: SerializeField]
    public Button _yesButtonAskForMoney { get; private set; }

    [field: SerializeField]
    public Button _noButtonAskForMoney { get; private set; }

    [field: SerializeField]
    public TMP_InputField _inputAmountAskForMoney { get; private set; }

    [field: SerializeField]
    public Transform _inputAmountAskForMoneyContainer { get; private set; }

    [field: SerializeField]
    public Transform _askForMoneyContainer { get; private set; }

    public void Awake() {
        if (BetweenScenesService.Instance.IsCoop) {
            _askForMoneyContainer.gameObject.SetActive(true);
            _inputAmountAskForMoneyContainer.gameObject.SetActive(false);
            
            _buttonAskForMoney.onClick.AddListener(SetAskForMoneyInputActiveState);
            _yesButtonAskForMoney.onClick.AddListener(()=>SetAskForMoneyInputActiveState(false));
            _noButtonAskForMoney.onClick.AddListener(()=>SetAskForMoneyInputActiveState(false));
            _inputAmountAskForMoney.onValueChanged.AddListener(CheckInputForMinus);
        } else 
            _askForMoneyContainer.gameObject.SetActive(false);
        
    }
    private void CheckInputForMinus(string value) {
        if (value == "-")
            _inputAmountAskForMoney.text = "";
    }
    private void SetAskForMoneyInputActiveState() {
        _inputAmountAskForMoneyContainer.gameObject.SetActive(!_inputAmountAskForMoneyContainer.gameObject.activeInHierarchy);
        _inputAmountAskForMoney.text = "10";
    }
    private void SetAskForMoneyInputActiveState(bool state) {
        _inputAmountAskForMoneyContainer.gameObject.SetActive(state);
    }
    public void AddMoneyAnimation() {
        PopAnimation(_moneyCountText.transform);
    }

    public void AddExpAnimation() {
        PopAnimation(_expCountText.transform);
    }

    private void PopAnimation(Transform needObject) {
        needObject.DOKill();
        needObject.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).OnComplete(() => needObject.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
    }

    private void NotEnoughAnimation(Transform needObject) {
        needObject.DOKill();
        needObject.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() => needObject.DOScale(1f, 0.2f).SetEase(Ease.InBack));
    }

    public void NotEnoughMoneyAnimation() {
        NotEnoughAnimation(_moneyCountText.transform);
    }

    public void NotEnoughExpAnimation() {
        NotEnoughAnimation(_expCountText.transform);
    }
}
