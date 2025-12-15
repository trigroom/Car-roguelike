using UnityEngine;

[CreateAssetMenu(fileName = "AchivementConfig", menuName = "Scriptable Objects/AchivementConfig")]
public class AchivementConfig : ScriptableObject
{
    [field:SerializeField] public int achievementId { get; private set; }
    [field:SerializeField] public Sprite achivementIcon { get; private set; }

    [field:SerializeField]public string achivementName{ get; private set; }
    [field:SerializeField]public int needCount{ get; private set; }
    [field:SerializeField]public int needSecondCount{ get; private set; }
    
    [field:SerializeField]public AchievementRewardType rewardType{ get; private set; }
    [field:SerializeField]public AchievementType achievementType{ get; private set; }
    
      [field:SerializeField]public int rewardCount{ get; private set; }
}

public enum AchievementRewardType {
    Car,
    CarPart,
    Scrap
}
public enum AchievementType {
    CarKills,
    PlayerDamage,
    CompleteMap,
    GetMoney,
    DistanceDrived,
    WinWithFirstCarLevel
}
