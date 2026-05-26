using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectCarButtonView : MonoBehaviour
{
    public Button selectCarButton;
    public Button sellCarButton;
    public TMP_Text carNameText;
    public TMP_Text sellCarText;

    public void SelectCarButtonSetup(string carName, float price) {
        carNameText.text = carName;
        gameObject.SetActive(true);
        sellCarText.text = CSVLocalization.Instance.GetLocalizedValue("Sell for")+price+"$";
    }

    public void HideSelectCarButton() {
        gameObject.SetActive(false);
    }
}
