namespace Scrpts
{
    public class MetaGameData
    {
        public const int WORDS_PER_ROUND = 4;
        public const int MIN_WORD_LENGTH = 3;
        public const int ROUNDS_COUNT = 6;
        public const int MAX_META_IMPROVEMENTS = 3;
        
        public static readonly int[] Rounds = new []{30,50, 75, 90, 130, 170};
        
        // public static readonly int[] Rounds = new []{15,30, 50, 70, 100, 150};
        // public static readonly int WORDS_PER_LEVEL = 4;
        // public static readonly int REQUIRED_WINS_PER_LEVEL = 3;
        // public static readonly int BASE_ROUND_SCORE = 50;
        // public static readonly float SCORE_GROWTH_FACTOR = 1.3f; // Коэффициент роста
        
        
        public int currentPlayerRecord;
    }
}