using System;
using System.Collections.Generic;
using System.Linq;
using Scrpts;
using UnityEngine;
using YG;

public class MetaImprovementManager
{
   
    private int _maxMetaImprovements;
   
    private ImprovementPanel _improvementPanel;
    private MetaImprovementPopup _metaImprovementPopup;
    private ImprovementSystem _improvementSystem;
    
    public event Action<List<ImprovementOption>> OnMetaImprovementsChanged;
    public static event Action<bool> OnImprovementSelected;
    public event Action OnReplacementModeStarted;
    public event Action OnReplacementModeEnded;
    
    private ImprovementOption _selectedImprovementForReplacement;
    private bool _isReplacementMode = false;
    
    public void Initialize(ImprovementSystem improvementSystem, MetaImprovementPopup metaImprovementPopup, 
        ImprovementPanel improvementPanel)
    {
        _maxMetaImprovements = MetaGameData.MAX_META_IMPROVEMENTS;
        _metaImprovementPopup = metaImprovementPopup;
        _improvementPanel  = improvementPanel;
        _improvementSystem = improvementSystem;
      
        _metaImprovementPopup.OnImprovementSelected += HandleImprovementSelected;
        // _metaImprovementPopup.OnImprovementReplacement += HandleImprovementReplacement;
            // metaImprovementPopup.OnRerollRequested += HandleRerollRequested;
      
        _improvementPanel.OnImprovementClicked += HandlePanelImprovementClicked;
        
        UpdateImprovementPanel();
    }
    
    
    private void HandleImprovementSelected(ImprovementOption newImprovement)
    {
        // Проверяем автоматическое улучшение
        var existingImprovement = GetImprovement(newImprovement.EffectType);
        if (existingImprovement != null)
        {
            // АВТОМАТИЧЕСКОЕ УЛУЧШЕНИЕ - совпадающий тип
            UpgradeMetaImprovement(existingImprovement, newImprovement.Rarity);
            OnImprovementSelected?.Invoke(true);
        }
        else if (CanAddMoreImprovements())
        {
            // ДОБАВЛЕНИЕ - есть свободные слоты
            AddMetaImprovement(newImprovement);
            _metaImprovementPopup.ClosePopup();
            OnImprovementSelected?.Invoke(true);
        }
        else
        {
            // ЗАМЕНА - нет свободных слотов, переходим в режим замены
            StartReplacementMode(newImprovement);
        }
        
        // OnImprovementSelected?.Invoke(newImprovement);
    }
    
    private void HandleImprovementReplacement(ImprovementOption newImprovement, ImprovementOption oldImprovement)
    {
        ReplaceMetaImprovement(oldImprovement, newImprovement);
    }
    
    private void HandlePanelImprovementClicked(ImprovementOption improvement)
    {
        if (_isReplacementMode && _selectedImprovementForReplacement != null)
        {
            // Выполняем замену
            ReplaceMetaImprovement(improvement, _selectedImprovementForReplacement);
            EndReplacementMode();
        }
    }
    
    // private void HandleRerollRequested()
    // {
    //     Debug.Log("Rerolling meta improvement options");
    //     
    //     var currentOptions = metaImprovementPopup?.GetCurrentOptions();
    //     
    //     if (currentOptions == null || currentOptions.Count == 0) return;
    //     
    //     // Получаем редкости текущих опций
    //     var rarities = currentOptions.Select(opt => opt.Rarity).ToList();
    //     
    //     // Генерируем новые опции
    //     var newOptions = GenerateNewMetaOptions(rarities);
    //     
    //     metaImprovementPopup?.RerollImprovements(newOptions);
    // }
    // private List<ImprovementOption> GenerateNewMetaOptions(List<ImprovementRarity> rarities)
        // {
        //     // Временная реализация - нужно будет интегрировать с ImprovementSystem
        //     var newOptions = new List<ImprovementOption>();
        //     
        //     var testImprovements = new[]
        //     {
        //         new ImprovementOption { EffectType = "DoubleWord", shortDescription = "Удвоение слова", modifier = "2.0", Rarity = rarities[0] },
        //         new ImprovementOption { EffectType = "BonusPoints", shortDescription = "Бонусные очки", modifier = "5.0", Rarity = rarities[1] },
        //         new ImprovementOption { EffectType = "ExtraTime", shortDescription = "Доп. время", modifier = "3.0", Rarity = rarities[2] }
        //     };
        //     
        //     newOptions.AddRange(testImprovements);
        //     return newOptions;
        // }
        
        
    private void StartReplacementMode(ImprovementOption newImprovement)
    {
        _selectedImprovementForReplacement = newImprovement;
        _isReplacementMode = true;
        
        OnReplacementModeStarted?.Invoke();
        Debug.Log("Режим замены активирован - выберите улучшение для замены");
    }
    
    private void EndReplacementMode()
    {
        _isReplacementMode = false;
        _selectedImprovementForReplacement = null;
        
        OnReplacementModeEnded?.Invoke();
        _metaImprovementPopup.ClosePopup();
        OnImprovementSelected?.Invoke(true);
    }

    private bool CanAddMoreImprovements()
    {
        return YG2.saves.ActiveImprovements.Count < _maxMetaImprovements;
    }

    private void AddMetaImprovement(ImprovementOption newImprovement)
    {
        if (YG2.saves.ActiveImprovements.Count >= _maxMetaImprovements)
        {
            Debug.LogWarning("Cannot add more meta improvements - limit reached");
            return;
        }
        
        YG2.saves.ActiveImprovements.Add(newImprovement);
        YG2.saves.CurrentImprovementOptions.Clear();
        UpdateImprovementPanel();
        OnMetaImprovementsChanged?.Invoke(YG2.saves.ActiveImprovements);
    }

    private void ReplaceMetaImprovement(ImprovementOption oldImprovement, ImprovementOption newImprovement)
    {
        int index = YG2.saves.ActiveImprovements.IndexOf(oldImprovement);
        if (index >= 0)
        {
            YG2.saves.ActiveImprovements[index] = newImprovement;
            UpdateImprovementPanel();
            OnMetaImprovementsChanged?.Invoke(YG2.saves.ActiveImprovements);
        }
    }

    private void UpgradeMetaImprovement(ImprovementOption improvement, ImprovementRarity newRarity)
    {
        improvement.Modifier += improvement.ModifierBonus;
            
        YG2.saves.CurrentImprovementOptions.Clear();
            
        UpdateImprovementPanel();
        OnMetaImprovementsChanged?.Invoke(YG2.saves.ActiveImprovements);
            
        _improvementPanel?.HighlightImprovement(improvement.EffectType, 1f);
    }
    
    public bool IsInReplacementMode()
    {
        return _isReplacementMode;
    }
    
    public ImprovementOption GetSelectedImprovementForReplacement()
    {
        return _selectedImprovementForReplacement;
    }
    
    private void UpdateImprovementPanel()
    {
        _improvementPanel?.ShowImprovements(YG2.saves.ActiveImprovements);
    }
    
    public List<ImprovementOption> GetActiveMetaImprovements()
    {
        return YG2.saves.ActiveImprovements;
    }
    
    public bool HasImprovement(string effectType)
    {
        return YG2.saves.ActiveImprovements.Any(imp => imp.EffectType == effectType);
    }

    private ImprovementOption GetImprovement(string effectType)
    {
        return YG2.saves.ActiveImprovements.FirstOrDefault(imp => imp.EffectType == effectType);
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
       
            _metaImprovementPopup.OnImprovementSelected -= HandleImprovementSelected;
            // _metaImprovementPopup.OnImprovementReplacement -= HandleImprovementReplacement;
            // metaImprovementPopup.OnRerollRequested -= HandleRerollRequested;
      
            _improvementPanel.OnImprovementClicked -= HandlePanelImprovementClicked;
        
    }
}