using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WordPanelManager: MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private RectTransform wordSlotsPanel;
    [SerializeField] private GameObject slotPrefab;
    
     private readonly List<GameObject> _wordSlots = new List<GameObject>();
     private readonly List<GameObject> _inactiveWordSlots = new List<GameObject>();
    private readonly List<GameObject> _lettersInWordSlots = new List<GameObject>() ; 
    
    [SerializeField] public int slotsInPanel = 8;
    [SerializeField] public int poolingSize = 8;

    public static event Action<GameObject> OnRemoveInGameLetter;

    public void Initialize()
    {
        for (int i = 0; i < slotsInPanel; i++)
        {
            var slot = Instantiate(slotPrefab, wordSlotsPanel);
            slot.GetComponent<Slot>().SlotID = i;
            _wordSlots.Add(slot);
        }
        for (int i = 0; i < poolingSize; i++)
        {
            var slot = Instantiate(slotPrefab, wordSlotsPanel);
            slot.GetComponent<Slot>().SlotID = slotsInPanel + i;
            slot.SetActive(false);
            _inactiveWordSlots.Add(slot);
        }
    }
    
    private GameObject GetInactiveSlot()
    {
        if (_inactiveWordSlots.Count > 0)
        {
            GameObject slot = _inactiveWordSlots[0];
            _inactiveWordSlots.RemoveAt(0);
            slot.SetActive(true);
            return slot;
        }
        GameObject newSlot = Instantiate(slotPrefab, wordSlotsPanel);
        return newSlot;
    }

    private void AddSlot()
    {
        if (_wordSlots.Count > slotsInPanel + poolingSize) return;
        
        var slot = GetInactiveSlot();
        _wordSlots.Add(slot);
    }

    private void RemoveSlot()
    {
        if (_wordSlots.Count <= slotsInPanel) return;
        
        var slot = _wordSlots[^1];
        slot.SetActive(false);
        _inactiveWordSlots.Insert(0,slot);
        _wordSlots.Remove(slot);
    }
    
     private void UpdatePanelsState()
     {
         CleanTrailingNulls();
         
        // int filledSlotsCount = FilledSlotsCount();
        int dif = _lettersInWordSlots.Count - _wordSlots.Count;
        if (dif > 0)
        {
            for (int i = 0; i < dif; i++)
            {
                AddSlot();
            }
        }
        else if (dif < 0)
        {
            for (int i = 0; i < -dif; i++)
            {
                RemoveSlot();
            }
        }
    }
     
    private void CleanTrailingNulls()
    {
        // Удаляем null'ы только с конца списка
        for (int i = _lettersInWordSlots.Count - 1; i >= 0; i--)
        {
            if (!_lettersInWordSlots[i])
            {
                _lettersInWordSlots.RemoveAt(i);
            }
            else
            {
                break; // Прерываем при первом ненулевом элементе
            }
        }
    }

    private int FilledSlotsCount()
    {
        int filledSlotsCount = 0;
        foreach (var letter in _lettersInWordSlots)
        {
            if (letter)
                filledSlotsCount++;
        }
        return filledSlotsCount;
    }
    
    public (string word, List<LetterData> letterList) GetWordAndLetters()
    {
        string word = "";
        List<LetterData> letterList = new List<LetterData>();
    
        foreach (var letterObj in _lettersInWordSlots)
        {
            if (!letterObj) continue;
        
            var letterData = letterObj.GetComponent<LetterTile>().Letter;
            letterList.Add(letterData);
            word += letterData.LetterChar;
        }
    
        return (word, letterList);
    }

    public bool ContainsLetter(GameObject letterObj)
    {
        return _lettersInWordSlots.Contains(letterObj);
    }

    private void AddLetterToWordSlot(GameObject letterObj, int slotIndex)
    {
        _lettersInWordSlots[slotIndex] = letterObj;
        UpdatePanelsState();
        
        var tileComponent = letterObj.GetComponent<LetterTile>();
        tileComponent.MoveToSlot(_wordSlots[slotIndex].transform, false);
       
    }
    
    private IEnumerator MoveLetterToSlotCoroutine(GameObject letterObj, int slotIndex)
    {
        _lettersInWordSlots[slotIndex] = letterObj;
        UpdatePanelsState();
        
        // Ждём конец кадра, чтобы Unity обработал изменения
        yield return null;
        var slot = _wordSlots[slotIndex].transform;
        
        // Принудительно обновляем лэйаут
        LayoutRebuilder.ForceRebuildLayoutImmediate(slot.parent.GetComponent<RectTransform>());

        // Теперь перемещаем букву (уже в правильную позицию)
        var tileComponent = letterObj.GetComponent<LetterTile>();
        tileComponent.MoveToSlot(slot, false);
    }

    public void RemoveLetterFromWordSlot(GameObject letterObj)
    {
        if (!letterObj) return;
    
        int index = _lettersInWordSlots.IndexOf(letterObj);
        if (index >= 0)
        {
            _lettersInWordSlots[index] = null;
        
            // Очищаем null-элементы в конце списка
            while (_lettersInWordSlots.Count > 0 && _lettersInWordSlots[_lettersInWordSlots.Count - 1] == null)
            {
                _lettersInWordSlots.RemoveAt(_lettersInWordSlots.Count - 1);
            }
        
            UpdatePanelsState();
        }
    }

    public void MoveToFirstFreeWordSlot(GameObject letterObj)
    {
        // Debug.Log(letterObj.name + " is moving to first free word slot");
        // Проверяем, есть ли уже этот объект в слотах
        if (_lettersInWordSlots.Contains(letterObj))
            return;

        // Ищем первый пустой слот
        for (int i = 0; i < _lettersInWordSlots.Count; i++)
        {
            if (_lettersInWordSlots[i] == null)
            {
                AddLetterToWordSlot(letterObj, i);
                return;
            }
        }

        // Если все слоты заняты, добавляем новый
        // Debug.Log(" Adding new slot");
        _lettersInWordSlots.Add(letterObj);

        StartCoroutine(MoveLetterToSlotCoroutine(letterObj, _lettersInWordSlots.Count - 1));
        // AddLetterToWordSlot(letterObj, _lettersInWordSlots.Count - 1);
        
    }

    public void ClearWordSlots(bool isWordConfirmed)
    {
        foreach (var letter in _lettersInWordSlots.ToList())
        {
            if (letter == null) continue;
            
            var draggable = letter.GetComponent<LetterTile>();
            if (draggable == null) continue;
            
            if (isWordConfirmed)
            {
                draggable.HomeSlot = null;
                
                OnRemoveInGameLetter?.Invoke(letter);
            }
            else
            {
                draggable.ReturnHome();
            }
        }
        _lettersInWordSlots.Clear();
        UpdatePanelsState();
    }
    
    public void PlayWordJumpAnimation(Action onComplete = null, float jumpPower = 0.5f, float duration = 0.3f, float delayBetweenLetters = 0.1f)
    {
        Debug.Log("Playing word jump animation");
        Sequence sequence = DOTween.Sequence();
        CleanTrailingNulls();

        foreach (var letterObj in _lettersInWordSlots)
        {
            if (letterObj == null) continue;
        
            var letterTile = letterObj.GetComponent<LetterTile>();
            if (letterTile != null)
            {
                sequence.AppendCallback(() => letterTile.PlayJumpAnimation(jumpPower, duration));
                sequence.AppendInterval(delayBetweenLetters);
            }
        }

        sequence.OnComplete(() => onComplete?.Invoke());
    }
    
    public void PlayLetterAnimation(LetterData letter, int position, float duration = 0.3f)
    {
        var letterObj = _lettersInWordSlots[position];
        if (letterObj == null) return;
    
        var letterTile = letterObj.GetComponent<LetterTile>();
        if (letterTile != null && letterTile.Letter.Equals(letter))
        {
            letterTile.PlayJumpAnimation(duration: duration);
        }
    }
    
}
