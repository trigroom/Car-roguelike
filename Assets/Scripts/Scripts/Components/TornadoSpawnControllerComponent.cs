
   using System.Collections.Generic;

   public struct TornadoSpawnControllerComponent {
       public float TimeToNextTornado;
       public List<int> TornadoToSpawn;
       public Dictionary<int, int> TornadoHostAndLocalEnities;
   }
