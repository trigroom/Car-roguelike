using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapUIView : MonoBehaviour {
    public Image MapImage;
    public EventTrigger RoundButton;
    public TMP_Text RoundDifficltyContainerText;
    public Transform RoundDifficltyContainer;
    public TMP_Text RoundNumberText;
    public TMP_Text RoundDescriptionText;
    [SerializedDictionary("Round Type", "Sprite")]
    public SerializedDictionary<RoundType, Sprite> RoundSprites;

    public void SetMoneyMultiplayerText(float multiplayerMoney, Vector2 needPosition) {
        Debug.Log("setup round mult");
        RoundDifficltyContainer.gameObject.SetActive(true);
        RoundDifficltyContainer.position = needPosition;
        RoundDifficltyContainerText.text = "x"+multiplayerMoney;
    }
    
}
