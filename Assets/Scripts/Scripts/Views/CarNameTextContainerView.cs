using System;
using TMPro;
using UnityEngine;

public class CarNameTextContainerView : MonoBehaviour {
    public TMP_Text NameText;
    public Action<TMP_Text> ReleaseNameText;
    public Transform TargetPosition;
    public void SetupNameTextContainer(TMP_Text nameText, Action<TMP_Text> releaseNameText) {
        NameText = nameText;
        ReleaseNameText = releaseNameText;
    }

    void Update() {
        if (NameText)
            NameText.transform.position = new Vector2(TargetPosition.transform.position.x, TargetPosition.transform.position.y - 3);
    }

    private void OnDisable() {
        ReleaseNameText?.Invoke(NameText);
        ReleaseNameText = null;
        NameText = null;
    }
}
