using DG.Tweening;
using TMPro;
using UnityEngine;

public class TutorialStuffView : MonoBehaviour
{
   [field:SerializeField] public TMP_Text _tutorialTextPrefab { get; private set; }
   [field:SerializeField] public Transform _shopContainer { get; private set; }
   [field:SerializeField] public Transform _shopText { get; private set; }
   [field:SerializeField] public Transform _sortButtonsContainer { get; private set; }
   public TMP_Text TutorialText;
   public Sequence _tutorialSequence;
   [field:SerializeField] public int _tutorialMapIndex { get; private set; }
   public void AnimateTMPTextWithHideShow(
       TMP_Text tmpText, 
       string newText, 
       float duration = 1f,
       Ease easeType = Ease.OutQuad)
   {
       if (tmpText == null) return;
        
       string originalText = tmpText.text;
       int originalLength = originalText.Length;
       tmpText.maxVisibleCharacters = originalLength;
       _tutorialSequence.Append(
           DOTween.To(
               () => tmpText.maxVisibleCharacters,
               x => tmpText.maxVisibleCharacters = x,
               0,
               duration / 3
           ).SetEase(Ease.InQuad)
       );
        
       // 2. Меняем текст
       _tutorialSequence.AppendCallback(() =>
       {
           tmpText.text = newText;
           tmpText.maxVisibleCharacters = 0; 
       });
        
       _tutorialSequence.Append(
           DOTween.To(
               () => tmpText.maxVisibleCharacters,
               x => tmpText.maxVisibleCharacters = x,
               newText.Length,
               duration / 3
           ).SetEase(Ease.OutQuad)
       );
        
   }

   public void AnimateTMPWithEffects(string newText, Vector2 targetPosition, float fontSize, float duration = 1f) {
       _tutorialSequence.Kill();
       _tutorialSequence = DOTween.Sequence();
       AnimateTMPTextWithHideShow(TutorialText, newText, duration);
       _tutorialSequence.Join(DOTween.To(() => TutorialText.fontSize, x => TutorialText.fontSize = x, fontSize, duration)
           .SetEase(Ease.OutBack));

       _tutorialSequence.Join(TutorialText.rectTransform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic));

       //  sequence.Join(TutorialText.DOFade(1, duration / 2).From(0).SetEase(Ease.InQuad));

       _tutorialSequence.Play();
   }
   
   public void HideText(bool hideText, Vector2 startPosition = default,string neededText = "", float duration = 1f) {
       if (startPosition != Vector2.zero) {
           TutorialText.transform.position = startPosition;
           TutorialText.maxVisibleCharacters = neededText.Length;
           TutorialText.text = neededText;
       }
           
       _tutorialSequence.Kill();
       _tutorialSequence = DOTween.Sequence();
       _tutorialSequence.Join(TutorialText.DOFade(hideText ? 0 : 1, duration).SetEase(Ease.InQuad));

       _tutorialSequence.Play();
   }
}
