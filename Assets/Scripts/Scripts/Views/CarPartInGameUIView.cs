using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarPartInGameUIView : MonoBehaviour
{
    [SerializeField]
    private Image CarPartImage;
    [SerializeField]
    private TMP_Text CarPartHealthCount;

    public void SetHealthCount(Color needColor, int healthCount) {
        if (healthCount < 0)
            healthCount = 0;
        CarPartHealthCount.text = healthCount.ToString();
        CarPartImage.color = needColor;
    }

    public void SetSize(Vector2 size, Vector2 position) {
        CarPartImage.rectTransform.sizeDelta = size;
        CarPartImage.transform.localPosition = position;
    }
}
