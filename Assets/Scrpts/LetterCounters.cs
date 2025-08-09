[System.Serializable]
public class LetterCounters
{
    public int InBag{ get; set; }
    public int OnBoard{ get; set; }
    public int Used{ get; set; }

    // Метод для изменения количества в определённом месте
    public void ChangeCount(LetterLocation location, int delta)
    {
        switch (location)
        {
            case LetterLocation.InBag:   InBag += delta; break;
            case LetterLocation.OnBoard: OnBoard += delta; break;
            case LetterLocation.Used:    Used += delta; break;
        }
    }
    
    public int GetCount(LetterLocation location)
    {
        return location switch
        {
            LetterLocation.InBag => InBag,
            LetterLocation.OnBoard => OnBoard,
            LetterLocation.Used => Used,
            _ => 0
        };
    }

    // Общее количество экземпляров буквы
    public int TotalCount => InBag + OnBoard + Used;
}