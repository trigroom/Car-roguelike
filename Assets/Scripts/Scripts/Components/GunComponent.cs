
   using UnityEngine;
   using UnityEngine.UI;

   public struct GunComponent {
       public int MainEntity;
       public float CurrentCouldown;
       public int CurrentMagazineCount;
       public SpriteRenderer GunSpriteRenderer;
       public Image GunSpriteImage;
       public float currentRotateDegrees;
       public float currentRotateSide;
       public Transform UpPartContainer;
       public Transform FirePointTransform;
   }