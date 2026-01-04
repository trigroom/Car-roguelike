using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectMapButtonView : MonoBehaviour {
 public Button SelectMapButton;
 public TMP_Text MapName;
 public Image MapImage;

 public void SetupSelectMapButton(Action<int> buttonAction, int mapIndex) {
  SelectMapButton.onClick.AddListener(() => buttonAction(mapIndex));
 }
 private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
 public void SetMapName(MapConfig mapConfig) {
  MapImage.sprite = mapConfig.MapSprite;
  MapImage.color = Color.white;
  MapName.text = GetLocalization(mapConfig.MapName);
  return;
  if (mapConfig.MapId != 0 && DataPersistenceManagerSystem._gameData.LevelRounds[mapConfig.MapId - 1] < mapConfig.RoundOnLastMapToUnlock) {
   SelectMapButton.enabled = false;
   var lastMapConfig = BetweenScenesService.Instance._carStuffListsConfig.MapConfigs[mapConfig.MapId - 1];
   MapName.text =
    $"{GetLocalization("Complete")} \"{GetLocalization(lastMapConfig.MapDifficltyConfigs[mapConfig.RoundOnLastMapToUnlock - 2].DifficultyName)}\" " +
    $"{GetLocalization("difficulty in")} \"{GetLocalization(lastMapConfig.MapName)}\" {GetLocalization("to unlock this map")}";

  } else {
   MapImage.sprite = mapConfig.MapSprite;
   MapImage.color = Color.white;
   MapName.text = GetLocalization(mapConfig.MapName);
  }
 }
}
