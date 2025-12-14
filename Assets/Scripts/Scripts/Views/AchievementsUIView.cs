using UnityEngine;

public class AchievementsUIView : MonoBehaviour
{
  public Transform AchievementsContainer;
  public AchievementStuffView AchievementStuffView;

  public AchievementStuffView GetAchievementStuffView() {
    return Instantiate(AchievementStuffView, AchievementsContainer);
  }
}
