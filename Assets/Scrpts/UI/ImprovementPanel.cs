using System.Collections.Generic;
using UnityEngine;


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
}
