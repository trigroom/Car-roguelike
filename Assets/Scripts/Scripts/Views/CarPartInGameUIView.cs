using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarPartInGameUIView : MonoBehaviour
{
    [SerializeField]
    private Image CarPartImage;
    [SerializeField]
    private TMP_Text CarPartHealthCount;

    public void SetHealthCount(Color needColor, int healthCount, bool ignoreTextActive = false) {
        if (healthCount < 0)
            healthCount = 0;
        if (ignoreTextActive || CarPartHealthCount.gameObject.activeInHierarchy ) {
            CarPartHealthCount.text = healthCount.ToString();
            CarPartHealthCount.color = needColor;
        }

        CarPartImage.color = needColor;
    }

    public void SetSize(Vector2 size, Vector2 position, Sprite sprite, bool hasText) {
        CarPartHealthCount.gameObject.SetActive(hasText);
        CarPartImage.rectTransform.sizeDelta = size;
        CarPartImage.transform.localPosition = position;
        CarPartImage.sprite = sprite;
    }
}
