using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapFilledZoneView : MonoBehaviour {
   [FormerlySerializedAs("_filledZoneUI")]
   public Image FilledZoneUI;
   public SpriteRenderer FilledZoneLight;
   public SpriteRenderer FilledZoneGround;
   public Color defaultLightColor;
   public Color greenLightColor;
   private Action<bool> _triggerAction;
   private Sequence _lightTween;
   public int collidersCount = 0;

   public void SetupFilledZone(Action<bool> triggerAction) {
      _triggerAction = triggerAction;
   }

   public void ChangeZonePosition(Vector2 position) {
      FilledZoneUI.transform.position = position;
      gameObject.transform.position = position;
      FilledZoneUI.fillAmount = 0;
   }

   private void OnTriggerEnter2D(Collider2D other) {
      if(!NetworkManager.Singleton.IsServer)return;
      if (other.CompareTag("Player")) {
         if (collidersCount == 0) {
            _triggerAction.Invoke(true);
            StartBlinkAnimation();
         }
            
         collidersCount++;
      }
   }

   private void StartBlinkAnimation() {
      _lightTween.Kill();
      _lightTween = DOTween.Sequence();
      _lightTween.Append(FilledZoneLight.DOColor(greenLightColor, 0.5f));
      _lightTween.Append(FilledZoneLight.DOColor(defaultLightColor, 0.5f));
      _lightTween.SetLoops(-1, LoopType.Restart);
   }
   private void OnTriggerExit2D(Collider2D other) {
      if (other.CompareTag("Player")) {
         if (collidersCount == 1) {
            _triggerAction.Invoke(false);
            _lightTween.Kill();
            _lightTween = DOTween.Sequence();
            _lightTween.Append(FilledZoneLight.DOColor(defaultLightColor, 0.5f));
         }
         
            
         collidersCount--;
      }
   }
}
