using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ImprovementCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    private ImprovementOption _option;
    private System.Action<ImprovementOption> _onSelectCallback;

    public void Initialize(ImprovementOption option, System.Action<ImprovementOption> onSelect)
    {
        _option = option;
        _onSelectCallback = onSelect;
        descriptionText.text = option.Description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onSelectCallback?.Invoke(_option);
    }
}