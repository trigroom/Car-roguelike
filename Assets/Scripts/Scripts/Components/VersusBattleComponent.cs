
   public struct VersusBattleComponent {
       public int CurrentBet;
       
       public int LeftOpponentEntity;
       public int RightOpponentEntity;

       public bool BetOnLeft;
       public bool IsCircularSawsSended;
       public bool IsBattleEnded;
       public CarPartInGameUIView[,] LeftCarPartInfoUIViewsGrid;
       public CarPartInGameUIView[,] LeftCarPartInfoUIViewsUpLevelGrid;
       
       public CarPartInGameUIView[,] RightCarPartInfoUIViewsGrid;
       public CarPartInGameUIView[,] RightCarPartInfoUIViewsUpLevelGrid;
       
   }
