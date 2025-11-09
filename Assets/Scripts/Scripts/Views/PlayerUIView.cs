using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIView : MonoBehaviour
{
    [field: SerializeField]
    public Transform _fuelArrowImage { get; private set; }

    [field: SerializeField]
    public TMP_Text _fuelText { get; private set; }
    [field: SerializeField]
    public RectTransform _carPratsUpLevelHealthContainer { get; private set; }
    [field: SerializeField]
    public RectTransform _carPratsDownLevelHealthContainer { get; private set; }
    [field: SerializeField]
    public TMP_Text _gameTimerText { get; private set; }
    [field: SerializeField]
    public TMP_Text[] _taskTexts { get; private set; }
    [field: SerializeField]
    public Transform _arrowImageTransform { get; private set; }
    [field: SerializeField]
    public Image _lampLightImage { get; private set; }
}
