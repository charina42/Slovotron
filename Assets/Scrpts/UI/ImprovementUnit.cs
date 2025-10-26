using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImprovementUnit: MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI modifierText;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private TextMeshProUGUI rarityText;
    
    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    private ImprovementOption _option;

    public void Initialize(ImprovementOption option)
    {
        _option = option;
        descriptionText.text = option.ShortDescription;
        modifierText.text = option.Modifier.ToString();
        
        // Устанавливаем отображение редкости
        SetRarityDisplay(option.Rarity);
    }

    private void SetRarityDisplay(ImprovementRarity rarity)
    {
        string rarityString = "";
        Color color = Color.white;
        
        switch (rarity)
        {
            case ImprovementRarity.Common:
                rarityString = "Обычное";
                color = commonColor;
                break;
            case ImprovementRarity.Rare:
                rarityString = "Редкое";
                color = rareColor;
                break;
            case ImprovementRarity.Epic:
                rarityString = "Эпическое";
                color = epicColor;
                break;
            case ImprovementRarity.Legendary:
                rarityString = "Легендарное";
                color = legendaryColor;
                break;
        }
        
        if (rarityText != null)
        {
            rarityText.text = rarityString;
            rarityText.color = color;
        }
        
        if (rarityBackground != null)
        {
            rarityBackground.color = new Color(color.r, color.g, color.b, 0.2f);
        }
    }

    public string GetImprovementType()
    {
        return _option.EffectType.ToString();
    }
    
    public ImprovementOption GetImprovementOption()
    {
        return _option;
    }
}
