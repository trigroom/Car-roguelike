using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineUiView : MonoBehaviour
{
    [field: SerializeField]
    public Button _startMachineButton { get; private set; }
    [field: SerializeField]
    public Image[] _firstSlotColumnIcons { get; private set; }
    [field: SerializeField]
    public Image[] _secondSlotColumnIcons { get; private set; }
    [field: SerializeField]
    public Image[] _thirdSlotColumnIcons { get; private set; }
    [field: SerializeField]
   public SlotItemConfig[] _slotItemConfigs { get; private set; }
    [field: SerializeField]
    public Transform[] _slotColumnsContainers { get; private set; }
    [field: SerializeField]
    public Sprite _switchUpSprite { get; private set; }
    [field: SerializeField]
    public Sprite _switchDownSprite { get; private set; }
    [field: SerializeField]
    public Image _switchImage { get; private set; }
    [field: SerializeField]
    public Button _leaveRoundButton { get; private set; }
    [field: SerializeField]
    public TMP_Text _tryLeftText { get; private set; }
    [field: SerializeField]
    public TMP_Text _currentBetText { get; private set; }
    [field: SerializeField]
    public Button _bet1Button { get; private set; }
    [field: SerializeField]
    public Button _bet10Button { get; private set; }
    [field: SerializeField]
    public Button _bet100Button { get; private set; }
    [field: SerializeField]
    public Button _betRemove1Button { get; private set; }
    [field: SerializeField]
    public Button _betRemove10Button { get; private set; }
    [field: SerializeField]
    public Button _betRemove100Button { get; private set; }
}
