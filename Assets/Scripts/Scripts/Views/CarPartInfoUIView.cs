using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarPartInfoUIView : MonoBehaviour
{
    [field: SerializeField] public Button _moveToInventoryButton { get; private set; }
    [field: SerializeField] public Button _upgradeButton { get; private set; }
    [field: SerializeField] public TMP_Text _upgradeButtonText { get; private set; }
    [field: SerializeField] public Button _destroyButton { get; private set; }
    [field: SerializeField] public Button _backButton { get; private set; }
    [field: SerializeField] public Button _nextButton { get; private set; }

    [field: SerializeField]
    public Transform _switchButtonsContainer;
    [field: SerializeField] public int _currentPartInfoId { get; private set; }
    [field: SerializeField] public TMP_Text _nameText { get; private set; }
    [field: SerializeField] public TMP_Text _descriptionText { get; private set; }
    public TMP_Text CarDescriptionText;
    [field: SerializeField] public Image _carPartIcon { get; private set; }

    public void SetupButtonsInfoPanel(Action backCarPart, Action destroy, Action upgrade,Action moveToInventory) {
        _backButton.onClick.AddListener(() => backCarPart?.Invoke());
        _moveToInventoryButton.onClick.AddListener(() => moveToInventory?.Invoke());
        _destroyButton.onClick.AddListener(() => destroy?.Invoke());
        _upgradeButton.onClick.AddListener(() => upgrade?.Invoke());
    }

    public void SetPanelForNeededPart(CarPartConfig carPartConfig, bool showDestroyButton, bool showMoveButton) {
        _destroyButton.gameObject.SetActive(showDestroyButton);
        _moveToInventoryButton.gameObject.SetActive(showMoveButton);
        if (carPartConfig.UpgradedPartCost != 0)
            _upgradeButtonText.text = $"{carPartConfig.UpgradedPartCost.ToString()}$ to upgrade";
        //_upgradeButton.gameObject.SetActive(carPartConfig.UpgradedPartId != 0);
        _currentPartInfoId = carPartConfig.Id; 
        _carPartIcon.sprite = carPartConfig.PartSprite;
        _descriptionText.text = $"{carPartConfig.MaxHealth} HP \n Part type: {carPartConfig.PartType}\n Weight: {carPartConfig.Weight}";
        _nameText.text = carPartConfig.PartName;
    }
}
