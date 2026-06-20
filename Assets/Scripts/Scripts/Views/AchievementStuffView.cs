using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AchievementStuffView : MonoBehaviour
{
    public Image achievementImage;
    public Image filledImage;
    public Image achievementImageIcon;
    public Image filledImageIcon;
    public TMP_Text achievementCount;
    public Button achievementInfoButton;
    public Material grayscaleMaterial;
    public AchivementConfig achivementConfig;
   
    
    public void SetAchievementInfo(AchivementConfig achievement, float value)
    {
        achivementConfig = achievement;
        achievementCount.text = (int)value+"/"+(ChallangeAchievments.Contains(achievement.achievementType)?1: achivementConfig.needCount);
        achievementImage.material = grayscaleMaterial;
        achievementImageIcon.material = grayscaleMaterial;
        filledImage.fillAmount = value/achivementConfig.needCount;
        filledImageIcon.fillAmount = value/achivementConfig.needCount;
        achievementImage.sprite = achievement.achievementBackground;
        achievementImageIcon.sprite = achievement.achievementIcon;
        filledImage.sprite = achievement.achievementBackground;
        filledImageIcon.sprite = achievement.achievementIcon;
    }

    private List<AchievementType> ChallangeAchievments = new() {
        AchievementType.CompleteMap,
        AchievementType.MaxFuelInCar,
        AchievementType.ModulesOnOneCarPart,
        AchievementType.WinWithoutUpgrades
    };
    public void CompleteAchievement(AchivementConfig achievement)
    {
        achievementCount.gameObject.SetActive(false);
        achivementConfig = achievement;
        filledImage.gameObject.SetActive(false);
        filledImageIcon.gameObject.SetActive(false);
        achievementImage.sprite = achievement.achievementBackground;
        achievementImageIcon.sprite = achievement.achievementIcon;
    }
}
