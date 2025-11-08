using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour
{
    [field:SerializeField]
    public Button _setPixelizedEffectModeButton { get; private set; }
    [field:SerializeField]
    public Transform _setPixelizedEffectModeChecker{ get; private set; }
    [field:SerializeField]
    public Slider _redColorSlider{ get; private set; }
    [field:SerializeField]
    public Slider _greenColorSlider{ get; private set; }
    [field:SerializeField]
    public Slider _blueColorSlider{ get; private set; }
    [field:SerializeField]
    public Image _projectilesOffsetColorImage{ get; private set; }
    [field:SerializeField]
    public Transform _settingsContainer{ get; private set; }
    [field:SerializeField]
    public Button _settingsChangeStateButton{ get; private set; }
    [field:SerializeField]
    public Button _saveSettingsButton{ get; private set; }
    [field:SerializeField]
    public TMP_Text _redColorText{ get; private set; }
    [field:SerializeField]
    public TMP_Text _greenColorText{ get; private set; }
    [field:SerializeField]
    public TMP_Text _blueColorText{ get; private set; }
    
}
