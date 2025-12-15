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

  public AchievementStuffView GetAchievementStuffView() {
    return Instantiate(AchievementStuffView, AchievementsContainer);
  }
  private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
  public void SetAchievementInfoPanel(AchivementConfig achievement) {
    AchievementsInfoContainer.gameObject.SetActive(true);
    AchievementsInfoIcon.sprite = achievement.achivementIcon;
    AchievementsInfoRewardText.text =
      GetLocalization("Reward") + ":" + GetLocalization(BetweenScenesService.Instance.GetAchievementReward(achievement));
    AchievementsInfoNameText.text = achievement.achivementName;
  }
  
  
  public void CloseAchievementInfoPanel() {
    AchievementsInfoContainer.gameObject.SetActive(false);
  }
}
