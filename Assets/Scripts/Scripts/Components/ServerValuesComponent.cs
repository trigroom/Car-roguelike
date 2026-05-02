
    using System.Collections.Generic;

   public struct ServerValuesComponent {
        public Dictionary<ulong,int> ClientsEntities;
        
        public float TickFuelServerTime;
        public float TickCarPartHealthServerTime;

        public int DeathPlayers;
        
        public HashSet<int> VersusBattleChangedHealth;

        public int ReadyPlayers;
   }
