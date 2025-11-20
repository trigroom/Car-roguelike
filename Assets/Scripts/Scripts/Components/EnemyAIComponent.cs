using System.Collections.Generic;
using UnityEngine;

    public struct EnemyAIComponent {
        public List<Transform> CheckedEnemyLeftPoints;
        public List<Transform> CheckedEnemyRightPoints;
        public EnemyDirection CurrentEnemyDirection;
        public float LastCheckedTime;
        public float LastCollisionWithObstacleTime;
        public Transform TargetToMove;
        public bool IsEnemy;
        public float RadiusToCheckTarget;
        public float LastTargetCheckTime;
    }

    public enum EnemyDirection {
        None,
        Left,
        Right, 
        Back
    }
