using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectMapButtonView : MonoBehaviour
{
 public Button SelectMapButton;
 public TMP_Text MapName;

 public void SetupSelectMapButton(Action<int> buttonAction, int mapIndex) {
  SelectMapButton.onClick.AddListener(()=>buttonAction(mapIndex));
 }

 public void SetMapName(string mapName) {
  MapName.text = BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(mapName);
 }
}
