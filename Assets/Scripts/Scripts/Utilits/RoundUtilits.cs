using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoundUtilits 
{
    private static readonly List<RoundType> BattleRoundsTypes = new List<RoundType> {
        RoundType.BattleDefault, RoundType.FromAToB, RoundType.ZoneDefence
    };
    
    public static bool IsNotBattleRound(RoundType type) => SupportRoundsTypes.Contains(type) || type == RoundType.None;

    public static readonly List<RoundType> SupportRoundsTypes = new List<RoundType> {
        RoundType.Chest, RoundType.Shop, RoundType.Task, RoundType.Transfer/*, RoundType.TeammateForSupport*/
    };
    public static (RoundType[], int) GetRandomBattleRound(int battleRounds, int needCount,  List<RoundType> currentSupportRoundTypes) {
        List<RoundType> currentBattleRoundTypes = BattleRoundsTypes.Distinct().ToList();
        
        int moneyMultiplayerRound = -1;
        if (battleRounds > 1) {
            moneyMultiplayerRound = Random.Range(0, battleRounds);
        }
        
        RoundType[] currentRoundTypes = new RoundType[needCount];
        for (int i = 0; i < needCount; i++) {
            
            if (battleRounds != 0) {
                battleRounds--;
                currentRoundTypes[i] = currentBattleRoundTypes[Random.Range(0, currentBattleRoundTypes.Count)];
                currentBattleRoundTypes.Remove(currentRoundTypes[i]);
            } else {
                currentRoundTypes[i] = currentSupportRoundTypes[Random.Range(0, currentSupportRoundTypes.Count)];
                currentSupportRoundTypes.Remove(currentRoundTypes[i]);
            }
        }

        return (currentRoundTypes,moneyMultiplayerRound );
    }
    public static bool IsBattleRound(RoundType roundType) => BattleRoundsTypes.Contains(roundType);
}
