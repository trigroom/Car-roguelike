using TMPro;
using UnityEngine;

public class StaticTextsView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text[] _texts;
    [SerializeField]
    private string[] _keys;

    public void SetTextLanguage() {
        for (int i = 0; i < _texts.Length; i++) 
            _texts[i].text = BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(_keys[i]);
    }
}
