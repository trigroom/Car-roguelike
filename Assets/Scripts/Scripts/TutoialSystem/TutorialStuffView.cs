using UnityEngine;

public class TutorialStuffView : MonoBehaviour
{
   [field:SerializeField] public Transform _shopContainer { get; private set; }
   [field:SerializeField] public Transform _sortButtonsContainer { get; private set; }
    
   [field:SerializeField] public int _tutorialMapIndex { get; private set; }
}
