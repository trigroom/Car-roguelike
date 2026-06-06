using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class RoundUtilits 
{
    private static readonly List<RoundType> BattleRoundsTypes = new List<RoundType> {
        RoundType.BattleDefault, RoundType.FromAToB, RoundType.ZoneDefence
    };
    
    public static bool IsNotBattleRound(RoundType type) => SupportRoundsTypes.Contains(type) || type == RoundType.None;

  public static List<RoundType> SupportRoundsTypes = new List<RoundType> {
       RoundType.Chest, RoundType.Shop, RoundType.Task, RoundType.SlotMachine, RoundType.ModulesShop, RoundType.VersusBattle
    };
   /*  public static List<RoundType> SupportRoundsTypes = new List<RoundType> {
        RoundType.VersusBattle
    };*/
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
    
    public static string[] VersusBattlesNames = new[] {
        "Buck", "Kyle", "Jack", "Travis", "Duke", "Billy", "Jake", "Clint", "Jedd", "Darryl", "Cody", "Shane", "Boyd", "Floyd", "Garrett",
        "Lou", "Milo", "Otis", "Roscoe", "Jed", "Lyle", "Wade", "Curtis", "Russ", "Dallas", "Elvis", "Merle", "Coyote", "Jared", "Lance",
        "Cletus", "Bo", "Waylon", "Hank", "Jethro", "Beau"
    };

    public static string[] VersusBattlesSurnames = new[] {
        "", "Coyote", "Buzzard", "Mole", "Hound", "Scorpion", "Jackal", "Badger", "Vulture", "Roach",
       
        "Red", "Baldy", "Scar", "Beardo", "Chick", "Slick", "Deadeye", "Tooth", "Fang", "Crutch", "Stitches",
       
        "Sledge", "Dynamite", "Gas-Wrench", "Poker", "Chainsaw", "Solder", "Hammer", "Wrench", "Torch", "Greaser",
       
        "Preacher", "Doc", "Liar", "Quiet", "Grouch", "Sneak", "Hustler", "Joker", "Lucky", "Boomer", "Stranger", "Rook",
        
        "Rusty", "Diesel", "Exhaust", "Propane", "Spark", "Tar", "Dust", "Fume", "Scale", "Radiator",
       
        "Crusher", "Claw", "Grip", "Nightmare", "Ghost", "Raven", "Spider", "Hawk", "Thunder", "Reaper","Viper"
    };
    
    private static string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

    public static string GetLocalizatedCarNameByIndex(Vector2 indexes) {
        return $"{GetLocalization(VersusBattlesNames[(int)indexes.x])} {GetLocalization(VersusBattlesSurnames[(int)indexes.y])}";
    }
    
    public static Vector2 GetRandomCarNameIndexes() {
        return new Vector2(Random.Range(0, VersusBattlesNames.Length), Random.Range(1, VersusBattlesSurnames.Length));
    }
}
