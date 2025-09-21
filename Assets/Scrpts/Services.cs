// using Scrpts.UI;


using Scrpts;

public class Services
    {
        public static WordGameManager Game { get; set; }
        public static ScoreManager Score { get; set; }
        public static UIManager UI { get; set; }
        public static RoundManager Round { get; set; }
        public static LetterBag LetterBag { get; set; }
        public static WordPanelManager WordPanelManager { get; set; }
        public static MetaGameData MetaGameData { get; set; }
        public static ImprovementSystem ImprovementSystem { get; set; }
        public static TutorialManager TutorialManager { get; set; }
        public static DictionaryManager DictionaryManager { get; set; }
    }
