 
 using System.Collections.Generic;
 using DG.Tweening;
 using UnityEngine.EventSystems;
 using UnityEngine.UI;

 public struct CurrentGameComponent {
        public int CurrentRound;
        public RoundType CurrentRoundType;
       // public List<RoundType[]> RoundTypes;

        public int CurrentBattleRounds;
        
        public EventTrigger[] RoundButtons;
        public Sequence mapPointSequence;
        
        public float RoundMoneyMultiplayer;
        public int RoundWithMoneyMultiplayer;
        
        public float RoundTime;
        public int RoundAddedEnemies;

        public float DifficultyHealthMultiplayer;
        public float DifficultyDamageMultiplayer;

        public bool SupportBoxIsSend;
 }

 public enum RoundType {
     None,
     Task,
     Shop,
     Chest,
     TeammateForSupport,
     Transfer,
     BattleDefault,
     ZoneDefence,
     FromAToB,
     BossBattle,
     ModulesShop
 }