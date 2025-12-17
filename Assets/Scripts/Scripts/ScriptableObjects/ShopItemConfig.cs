using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemConfig", menuName = "Scriptable Objects/ShopItemConfig")]
public class ShopItemConfig : ScriptableObject
{
   public int ScrapCost;
   public int RewardCount;
   public AchievementRewardType RewardType;
}
