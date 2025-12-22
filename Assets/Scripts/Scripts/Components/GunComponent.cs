
   using UnityEngine;
   using UnityEngine.UI;

   public struct GunComponent {
       public float CurrentCountdown;
       public int CurrentMagazineCount;
       public SpriteRenderer GunSpriteRenderer;
       public Image GunSpriteImage;
       public float CurrentRotateDegrees;
       public bool GunOnTarget;
       public float GunOnTargetTime;
       public Transform UpPartContainer;
       public Transform FirePointTransform;

       public float CurrentReloadSpeed;
       public float CurrentMaxAttackCountdown;
       public float CurrentDamage;
       
       public int MaxPiercingCount;
   }