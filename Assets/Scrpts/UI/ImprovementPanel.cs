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
                // Анимация подсветки
                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    Sequence highlightSequence = DOTween.Sequence();
                    highlightSequence.Append(image.DOColor(Color.yellow, duration * 0.3f));
                    highlightSequence.Append(image.DOColor(Color.white, duration * 0.7f));
                }
                break;
            }
        }
    }
}
