using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AchievementStuffView : MonoBehaviour
{
    public Image achievementImage;
    public Image filledImage;
    public TMP_Text achievementCount;
    public Button achievementInfoButton;
    public Material grayscaleMaterial;
    public AchivementConfig achivementConfig;
   
    
    public void SetAchievementInfo(AchivementConfig achievement, float value)
    {
        achivementConfig = achievement;
        achievementCount.text = value+"/"+achivementConfig.needCount;
        achievementImage.material = grayscaleMaterial;
        filledImage.fillAmount = value/achivementConfig.needCount;
        achievementImage.sprite = achievement.achivementIcon;
    }

    public void CompleteAchievement(AchivementConfig achievement)
    {
        achievementCount.gameObject.SetActive(false);
        achivementConfig = achievement;
        filledImage.gameObject.SetActive(false);
        achievementImage.sprite = achievement.achivementIcon;
    }
}
