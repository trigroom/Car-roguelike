using System.Collections.Generic;
using UnityEngine;

    public struct EnemyAIComponent {
        public List<Transform> CheckedEnemyLeftPoints;
        public List<Transform> CheckedEnemyRightPoints;
        public EnemyDirection CurrentEnemyDirection;
        public float LastCheckedTime;
        public Transform TargetToMove;
    }

    public enum EnemyDirection {
        None,
        Left,
        Right, 
        Back
    }
