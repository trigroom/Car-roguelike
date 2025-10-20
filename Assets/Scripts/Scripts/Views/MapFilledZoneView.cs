using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapFilledZoneView : MonoBehaviour {
   [FormerlySerializedAs("_filledZoneUI")]
   public Image FilledZoneUI;

   private Action<bool> _triggerAction;
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
      if (other.CompareTag("Player")) {
         if (collidersCount == 0)
            _triggerAction.Invoke(true);
         collidersCount++;
      }
   }

   private void OnTriggerExit2D(Collider2D other) {
      if (other.CompareTag("Player")) {
         if (collidersCount == 1)
            _triggerAction.Invoke(false);
         collidersCount--;
      }
   }
}
