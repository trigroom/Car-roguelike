 
 using System.Collections.Generic;
 using UnityEngine.EventSystems;
 using UnityEngine.UI;

 public struct CurrentGameComponent {
        public int CurrentRound;
        public RoundType CurrentRoundType;
        public List<RoundType[]> RoundTypes;

        public int CurrentBattleRounds;
        
        public EventTrigger[] RoundButtons;
        
        public float RoundMoneyMultiplayer;
        public int RoundWithMoneyMultiplayer;
        
        public float RoundTime;
 }

 public enum RoundType {
     None,
     Task,
     Shop,
     Chest,
     Transfer,
     BattleDefault,
     ZoneDefence,
     FromAToB
 }