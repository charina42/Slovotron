using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class TutorialManager
{
    [System.Serializable]
    public class TutorialStep
    {
        public string stepKey;
        public string title;
        [TextArea(3, 5)] public string description;
        public bool waitForCompletion;
        public Func<bool> CompletionCondition;
    }
    
    private TutorialPopup _tutorialPopup;
    private readonly List<TutorialStep> _tutorialSteps = new List<TutorialStep>();
    private int _currentStepIndex = 0;
    private bool _isTutorialActive = false;
    private Coroutine _typingCoroutine;
    public event Action OnTutorialStepCompleted;
   
    public void Initialize(TutorialPopup popup)
    {
        _tutorialPopup = popup;
        CreateTutorialSteps();

        _tutorialPopup.OnContinueSelected += CompleteCurrentStep;
    }

    public int GetCurrentStepIndex()
    {
        return _currentStepIndex;
    }
    
    public bool IsTutorialActive()
    {
        return _isTutorialActive;
    }

    private void CreateTutorialSteps()
    {
        // Шаг 1: Составление слов
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "word_formation",
            title = "Составляйте слова",
            description = "Выберите фишки, чтобы составить слово минимум из четырех букв, и нажмите кнопку 'Подтвердить'!",
            waitForCompletion = false
        });
        
        // Шаг 2: Механика фишек
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "tile_mechanics",
            title = "Фишки не возвращаются",
            description = "Фишки, использованные в слове, уходят из мешочка до конца раунда, но каждая из них навсегда получает +1 к своему номиналу!",
            waitForCompletion = false
        });
        
        // Шаг 3: Улучшения мешочка
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "bag_improvements",
            title = "Улучшения мешочка",
            description = "После каждого составленного слова выбирайте улучшения для мешочка. Эффект применяется мгновенно!",
            waitForCompletion = true,
            // CompletionCondition = () => gameManager.HasSeenImprovement()
        });
        
        // Шаг 4: Цель раунда
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "round_goal",
            title = "Цель раунда",
            description = "Составьте 4 слова за раунд и наберите нужное количество очков, чтобы победить. За успешное прохождение раунда вы получите особое улучшение!",
            waitForCompletion = false
        });

        // Шаг 5: Обновление поля
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "refresh_mechanic",
            title = "Обновление поля",
            description = "Не нравится набор фишек? Кнопка 'Поменять' уберёт все неиспользованные фишки с поля и заменит их новыми из мешочка.",
            waitForCompletion = false
        });

        

        //  Шаг 5: Улучшения игры
        // _tutorialSteps.Add(new TutorialStep
        // {
        //     stepKey = "game_improvements",
        //     title = "Улучшения игры",
        //     description = "В конце каждого раунда вас ждут улучшения, которые могут кардинально изменить правила подсчёта очков!",
        //     waitForCompletion = false
        // });

        // Шаг 6: Новый раунд
        _tutorialSteps.Add(new TutorialStep
        {
            stepKey = "new_round",
            title = "Новый раунд",
            description = "В начале каждого раунда все фишки возвращаются в мешочек, и на поле появляются новые фишки.",
            waitForCompletion = false
        });
    }

    public void StartTutorial()
    {
        if (_tutorialSteps.Count == 0 || _isTutorialActive) return;
        
        _isTutorialActive = true;
        _currentStepIndex = 0;
        // ShowStep(_currentStepIndex);
    }

    private void ShowStep(int stepIndex)
    {
        if (_tutorialPopup == null) return;
        
        var step = _tutorialSteps[stepIndex];
        
        _tutorialPopup.SetTitle(step.title);
        _tutorialPopup.SetMessage(step.description);
        
        _tutorialPopup.Show();
    }
    
    public void ShowSpecificStep(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < _tutorialSteps.Count && _isTutorialActive)
        {
            _currentStepIndex = stepIndex;
            ShowStep(_currentStepIndex);
        }
    }

    public void ShowSpecificStep(string stepKey)
    {
        var stepIndex = _tutorialSteps.FindIndex(step => step.stepKey == stepKey);
        if (stepIndex < 0 || !_isTutorialActive) return;
        if (stepIndex != _currentStepIndex) return;
        _currentStepIndex = stepIndex;
        ShowStep(_currentStepIndex);
    }
    

    private void CompleteCurrentStep()
    {
        _currentStepIndex++;
        
        if (_currentStepIndex >= _tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }
        OnTutorialStepCompleted?.Invoke();
    }

    public void ShowNextStep()
    {
        ShowStep(_currentStepIndex);
    }

    public void CheckStepCompletion()
    {
        if (!_isTutorialActive || _currentStepIndex >= _tutorialSteps.Count) return;
        
        var currentStep = _tutorialSteps[_currentStepIndex];
        if (currentStep.waitForCompletion && currentStep.CompletionCondition != null)
        {
            if (currentStep.CompletionCondition.Invoke())
            {
                CompleteCurrentStep();
            }
        }
    }

    private void EndTutorial()
    {
        _isTutorialActive = false;
        // if (tutorialPopup != null)
        // {
        //     tutorialPopup.Hide();
        // }
        
        // Сохраняем в YG2 вместо PlayerPrefs
        YG2.saves.IsTutorialCompleted = true;
        YG2.SaveProgress();
    }

    public void SkipTutorial()
    {
        if (_isTutorialActive)
        {
            EndTutorial();
        }
    }

    public bool IsTutorialCompleted()
    {
        return YG2.saves.IsTutorialCompleted;
    }
}