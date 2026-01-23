using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VersusBattleView : MonoBehaviour {
    [field: SerializeField]
    public VersusBattleParticipantInfo _leftVersusBattleParticipantInfo { get; private set; }

    [field: SerializeField]
    public VersusBattleParticipantInfo _rightVersusBattleParticipantInfo { get; private set; }

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

    [field: SerializeField]
    public Button _speedGameSpeedBoostButton { get; private set; }
    [field: SerializeField]
    public TMP_Text _speedGameSpeedBoostText { get; private set; }
    [field: SerializeField]
    public TMP_Text _betText { get; private set; }
    [field: SerializeField] public SpriteRenderer _behindMapBorderSpriteRenderer { get; private set; }
    [field: SerializeField] public SpriteRenderer _mapGroundSpriteRenderer { get; private set; }
    [field: SerializeField] public SpriteRenderer _behindMapGroundSpriteRenderer { get; private set; }
    
    [field: SerializeField] public Transform _betContainer { get; private set; }
    [field: SerializeField] public Transform _versusMapContainer { get; private set; }
}

[System.Serializable]
public class VersusBattleParticipantInfo {
    public RectTransform CarPratsUpLevelHealthContainer;

    public RectTransform CarPratsDownLevelHealthContainer;

    public Image LampLightImage;

    public TMP_Text NameText;

    public Button BetButton;
}
