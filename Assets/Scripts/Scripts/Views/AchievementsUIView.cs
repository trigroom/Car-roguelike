using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsUIView : MonoBehaviour
{
  public Image AchievementsInfoIcon;
  public TMP_Text AchievementsInfoRewardText;
  public TMP_Text AchievementsInfoNameText;
  public Transform AchievementsInfoContainer;
  public Transform AchievementsContainer;
  public Button BackFromAchievementButton;
  public AchievementStuffView AchievementStuffView;
  
  public Image AchievementsRewardIcon;
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
    AchievementsInfoIcon.sprite = achievement.achivementIcon;
    AchievementsInfoRewardText.text =
      GetLocalization("Reward") + ":" + BetweenScenesService.Instance.GetAchievementReward(achievement);
    AchievementsInfoNameText.text = achievement.achivementName;
  }
  public void SetRewardAchievementInfoPanel(AchivementConfig achievement) {
    AchievementsRewardContainer.gameObject.SetActive(true);
    AchievementsRewardIcon.sprite = achievement.achivementIcon;
    AchievementsRewardText.text =
      GetLocalization("Reward") + ":" + BetweenScenesService.Instance.GetAchievementReward(achievement);
    AchievementsRewardNameText.text = achievement.achivementName;
  }
  
  
  public void CloseAchievementInfoPanel() {
    AchievementsInfoContainer.gameObject.SetActive(false);
  }
}
