using UnityEngine;

[CreateAssetMenu(fileName = "AchivementConfig", menuName = "Scriptable Objects/AchivementConfig")]
public class AchivementConfig : ScriptableObject
{
    [field:SerializeField] public Sprite achivementIcon { get; private set; }

    [field:SerializeField]public string achivementName{ get; private set; }
    [field:SerializeField]public int needCount{ get; private set; }
    [field:SerializeField]public int needSecondCount{ get; private set; }
    
    [field:SerializeField]public AchivementRewardType rewardType{ get; private set; }
    [field:SerializeField]public AchivementType achievementType{ get; private set; }
    
      [field:SerializeField]public int rewardCount{ get; private set; }
}

public enum AchivementRewardType {
    Car,
    CarPart,
    Scrap
}
public enum AchivementType {
    CarKills,
    PlayerDamage,
    CompleteMap,
    GetMoney,
    DistanceDrived,
    WinWithFirstCarLevel
}
