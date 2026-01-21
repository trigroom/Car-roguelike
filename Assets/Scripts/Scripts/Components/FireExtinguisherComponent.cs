
    using System.Collections.Generic;
    using UnityEngine;

   public struct FireExtinguisherComponent {
        public List<Vector2Int> CheckedCells;
        public float CountdownTime;
        public float CurrentFireRemove;
        public EffectType EffectType;
   }
