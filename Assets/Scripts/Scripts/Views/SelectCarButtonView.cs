using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectCarButtonView : MonoBehaviour {
    public Button selectCarButton;
    public TMP_Text carNameText;

    public void SelectCarSetup(Action <int> onClickAction, string carName, int carId) {
        selectCarButton.onClick.AddListener(()=>onClickAction(carId));
        carNameText.text = BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(carName);
    }
}
