namespace Scrpts
{
    public class MetaGameData
    {
        public const int WORDS_PER_ROUND = 4;
        public const int MIN_WORD_LENGTH = 3;
        public const int ROUNDS_COUNT = 6;
        public static readonly int[] Rounds = new []{15,30, 50, 70, 100, 150};
        public int currentPlayerRecord;
    }
}