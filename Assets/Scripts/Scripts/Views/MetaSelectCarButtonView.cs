using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaSelectCarButtonView : MonoBehaviour {
    public Button selectCarButton;
    public TMP_Text carNameText;
    public Image carIconImage;

    public void SelectCarSetup(Action <int> onClickAction, string carName, int carId, Sprite carIcon) {
        selectCarButton.onClick.AddListener(()=>onClickAction(carId));
        SetCarButtonText(carName);
        carIconImage.sprite = carIcon;
    }

    public void SetCarButtonText(string carName) {
        carNameText.text = CSVLocalization.Instance.GetLocalizedValue(carName);
    }
}
