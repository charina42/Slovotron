using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ImprovementPanel : MonoBehaviour
{
    public Transform parentPanel;
    public GameObject improvementUnitPrefab;
    
    public event Action<ImprovementOption> OnImprovementClicked;

    public void ShowImprovements(List<ImprovementOption> activeImprovements)
    {
        foreach (Transform child in parentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (var improvement in activeImprovements)
        {
            GameObject unitInstance = Instantiate(improvementUnitPrefab, parentPanel);
            var unit = unitInstance.GetComponent<ImprovementUnit>();
            
            if (unit != null)
            {
                unit.Initialize(improvement);
                
                var button = unitInstance.GetComponent<Button>();
                if (button == null)
                {
                    button = unitInstance.AddComponent<Button>();
                }
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnImprovementClicked?.Invoke(improvement));
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
                    Color originalColor = image.color;
                    Vector3 originalScale = child.localScale;
                
                    Sequence highlightSequence = DOTween.Sequence();
                    highlightSequence.Join(image.DOColor(Color.Lerp(originalColor, Color.white, 0.5f), duration * 0.3f));
                    highlightSequence.Join(child.DOScale(originalScale * 1.2f, duration * 0.3f));
                    highlightSequence.Append(image.DOColor(originalColor, duration * 0.7f));
                    highlightSequence.Join(child.DOScale(originalScale, duration * 0.7f));
                }
                break;
            }
        }
    }
}