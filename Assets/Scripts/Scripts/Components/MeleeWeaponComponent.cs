
   using UnityEngine;
   using UnityEngine.UI;

   public struct MeleeWeaponComponent {
       public float Countdown;
       public float CurrentMaxCountdown;
       public SpriteRenderer MeleeWeaponRenderer;
    //   public float CurrentRotateDegrees;
       public Image MeleeSpriteImage;
       public MeleeCarPartStuffView MeleeStuff;

       public float CurrentDamage;
       public float DamageBySpeed;

       public bool isTriggered;
   }
