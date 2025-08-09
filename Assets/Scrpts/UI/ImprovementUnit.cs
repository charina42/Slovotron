
    using TMPro;
    using UnityEngine;
    // using UnityEngine.EventSystems;

    public class ImprovementUnit: MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI modifierText;
        private ImprovementOption _option;

        public void Initialize(ImprovementOption option)
        {
            _option = option;
            descriptionText.text = option.shortDescription;
            modifierText.text = option.modifier;
        }

    }
