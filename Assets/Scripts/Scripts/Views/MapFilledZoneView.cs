using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapFilledZoneView : MonoBehaviour
{
   [FormerlySerializedAs("_filledZoneUI")]
   public Image FilledZoneUI;
   private Action _triggerAction;

   public void SetupFilledZone(Action triggerAction) {
      _triggerAction = triggerAction;
   }

   public void ChangeZonePosition(Vector2 position) {
      FilledZoneUI.transform.position = position;
      gameObject.transform.position = position;
      FilledZoneUI.fillAmount = 0;
   }

   private void OnTriggerStay2D(Collider2D other) {
      _triggerAction.Invoke();
   }
}
