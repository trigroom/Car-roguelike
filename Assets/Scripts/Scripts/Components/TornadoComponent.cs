
   using DG.Tweening;
   using UnityEngine;

   public struct TornadoComponent {
       public Vector2 TornadoDirection;
       public Animator TornadoAnimator;
       public float TimeToChangeDirection;
       public float LifeTime;
       public float TornadoSpeed;
       public bool TargetSpeedIsChanged;
       public float TornadoTargetSpeed;
       public Tween CounterTween;
       public EffectType EffectType;
   }
