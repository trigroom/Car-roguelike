using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsUIView : MonoBehaviour
{
  public Image AchievementsInfoIcon;
  public Image AchievementsInfoBackground;
  public TMP_Text AchievementsInfoRewardText;
  public TMP_Text AchievementsInfoNameText;
  public Transform AchievementsInfoContainer;
  public Transform AchievementsContainer;
  public Button BackFromAchievementButton;
  public AchievementStuffView AchievementStuffView;
  
  public Image AchievementsRewardIcon;
  public Image AchievementsRewardBackground;
  public TMP_Text AchievementsRewardText;
  public TMP_Text AchievementsRewardNameText;
  public Transform AchievementsRewardContainer;
  public Button NextRewardedAchievementButton;

  public AchievementStuffView GetAchievementStuffView() {
    return Instantiate(AchievementStuffView, AchievementsContainer);
  }
  private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
  public void SetAchievementInfoPanel(AchivementConfig achievement) {
    AchievementsInfoContainer.gameObject.SetActive(true);
    AchievementsInfoIcon.sprite = achievement.achievementIcon;
    AchievementsInfoBackground.sprite = achievement.achievementBackground;
    AchievementsInfoRewardText.text =GetAchievementTaskDescription(achievement) + "\n"+
      GetLocalization("Reward") + ":" + BetweenScenesService.Instance.GetAchievementReward(achievement);
    AchievementsInfoNameText.text = achievement.achivementName;
  }
  public void SetRewardAchievementInfoPanel(AchivementConfig achievement) {
    AchievementsRewardContainer.gameObject.SetActive(true);
    AchievementsRewardIcon.sprite = achievement.achievementIcon;
    AchievementsRewardBackground.sprite = achievement.achievementBackground;
    AchievementsRewardText.text =
      GetLocalization("Reward") + ":" + BetweenScenesService.Instance.GetAchievementReward(achievement);
    AchievementsRewardNameText.text = achievement.achivementName;
  }

  private string GetAchievementTaskDescription(AchivementConfig achievement) {
    switch (achievement.achievementType) {
      case AchievementType.MaxFuelInCar:
        return $"{GetLocalization("Have")} {achievement.needCount} {GetLocalization("Max fuel achievement")}";
      case AchievementType.CompletedTasks:
        return $"{GetLocalization("Complete")} {achievement.needCount} {GetLocalization("tasks")}";
      case AchievementType.SupportBoxOpened:
        return $"{GetLocalization("Open")} {achievement.needCount} {GetLocalization("support boxes")}";
      case AchievementType.WinWithoutUpgrades:
        return $"{GetLocalization("Win without car and car part upgrades")}";
      case AchievementType.CarKills:
        return $"{GetLocalization("Kill")} {achievement.needCount} {GetLocalization("Cars")}";
      case AchievementType.CompleteMap:
        var mapConfig = BetweenScenesService.Instance._carStuffListsConfig.MapConfigs[achievement.needSecondCount];
        return $"{GetLocalization("Complete")} " +
               $"{GetLocalization(mapConfig.MapName)} " +
               $"{GetLocalization("Difficulty")} \"{GetLocalization(mapConfig.MapDifficltyConfigs[achievement.needCount-2].DifficultyName)}\"";
      case AchievementType.GetMoney:
        return $"{GetLocalization("Get")} {achievement.needCount}$" ;
      case AchievementType.PlayerDamage:
        return $"{GetLocalization("Deal")} {achievement.needCount} {GetLocalization("Damage")}";
      case AchievementType.ModulesOnOneCarPart:
        return $"{GetLocalization("Equip")} {achievement.needCount} {GetLocalization("modules on one car part")}";
      case AchievementType.DistanceDrived:
        return $"{GetLocalization("Drive")} {achievement.needCount} {GetLocalization("m")}";
      default: return "";
    }
  }
  public void CloseAchievementInfoPanel() {
    AchievementsInfoContainer.gameObject.SetActive(false);
  }
}
