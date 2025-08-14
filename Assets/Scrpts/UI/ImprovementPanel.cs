using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class ImprovementPanel : MonoBehaviour

{
    public Transform parentPanel;
    public GameObject improvementUnitPrefab;

    public void ShowImprovements(List<ImprovementOption> ActiveImprovements)
    {
        foreach (Transform child in parentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (var improvement in ActiveImprovements)
        {
            GameObject unitInstance = Instantiate(improvementUnitPrefab, parentPanel);
            var unit = unitInstance.GetComponent<ImprovementUnit>();
            
            Debug.Log(improvement.shortDescription);

            if (unit != null)
            {
                unit.Initialize(improvement);
            }
        }
    }
    
    public void HighlightImprovement(string improvementType, float duration)
    {
        foreach (Transform child in parentPanel)
        {
            var unit = child.GetComponent<ImprovementUnit>();
            if (unit != null && unit.GetImprovementType() == improvementType)
            {
                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    // Сохраняем исходные значения
                    Color originalColor = image.color;
                    Vector3 originalScale = child.localScale;
                
                    // Создаем последовательность анимации
                    Sequence highlightSequence = DOTween.Sequence();
                
                    // Первая часть: увеличиваем размер и осветляем
                    highlightSequence.Join(image.DOColor(Color.Lerp(originalColor, Color.white, 0.5f), duration * 0.3f));
                    highlightSequence.Join(child.DOScale(originalScale * 1.2f, duration * 0.3f));
                
                    // Вторая часть: возвращаем к исходному состоянию
                    highlightSequence.Append(image.DOColor(originalColor, duration * 0.7f));
                    highlightSequence.Join(child.DOScale(originalScale, duration * 0.7f));
                }
                break;
            }
        }
    }
}
