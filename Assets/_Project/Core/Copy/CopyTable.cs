using System.Collections.Generic;
using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Copy
{
    /// <summary>
    /// Every word the game says to the player, in one file.
    ///
    /// This is the point of it: replacing the voice - a rewrite, a
    /// localization, a copy experiment - is editing this file and nothing else.
    /// A joke trapped in a prefab or a component is a joke nobody can find
    /// again, so no view is allowed to hold a string literal.
    ///
    /// It lives in Core rather than in the presentation layer because copy is
    /// data, not rendering: it has no engine dependency, it is the input to a
    /// pure selection rule (<see cref="ReactionPicker"/>), and putting it here
    /// is what makes the voice testable at all - the Unity layer has no test
    /// seam by design.
    ///
    /// The voice is deadpan, self-deprecating and observational. It notices;
    /// it never congratulates. Nothing here ends in an exclamation mark, and
    /// nothing here appears inside the puzzle - while the player is solving,
    /// the only personality is motion, and the strings the board, numpad and
    /// status strip use are deliberately flat.
    /// </summary>
    public static class CopyTable
    {
        // ------------------------------------------------------------------
        // Home
        // ------------------------------------------------------------------

        public const string AppTitle = "Sudoku";

        /// <summary>The one line of voice on the launch screen. It sets the
        /// register for everything after it, so it undersells on purpose.</summary>
        public const string HomeTagline = "A grid, some numbers, and your afternoon.";

        public const string HomeContinue = "Continue";
        public const string HomeNewGame = "New Game";
        public const string HomeDaily = "Daily";
        public const string HomeSettings = "Settings";

        /// <summary>Which puzzle Continue means: a difficulty and a clock.</summary>
        public static string HomeContinueDetail(string tier, string clock) => $"{tier}  -  {clock}";

        // ------------------------------------------------------------------
        // Difficulty select
        // ------------------------------------------------------------------

        public const string DifficultyTitle = "New Game";
        public const string DifficultyBack = "Back";

        /// <summary>Marks a tier the player already has a game under.</summary>
        public static string DifficultyWaiting(string clock) => $"in progress  {clock}";

        /// <summary>
        /// What a tier is called. Routed through the table like everything
        /// else, so renaming the ladder is a copy change rather than an enum
        /// change with a save-format consequence.
        /// </summary>
        public static string Tier(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Easy: return "Easy";
                case DifficultyTier.Medium: return "Medium";
                case DifficultyTier.Hard: return "Hard";
                case DifficultyTier.Expert: return "Expert";
                case DifficultyTier.Master: return "Master";
                default: return tier.ToString();
            }
        }

        // ------------------------------------------------------------------
        // Resume prompt
        // ------------------------------------------------------------------

        public const string ResumeResume = "Resume";
        public const string ResumeStartFresh = "Start Fresh";

        /// <summary>The armed state of Start Fresh. It is a warning, so it is
        /// said plainly - this is the one place the voice steps aside.</summary>
        public const string ResumeStartFreshConfirm = "Lose this puzzle? Tap again";

        public const string ResumeBack = "Back";
        public const string ResumeNote = "Starting fresh throws this puzzle away for good.";

        public static string ResumeTitle(string tier) => $"{tier} in progress";

        public static string ResumeDetail(string clock) => $"You are {clock} into this puzzle.";

        // ------------------------------------------------------------------
        // In the puzzle - the status strip and the numpad
        //
        // Story 72: no joke reaches the player while they are solving. These
        // strings are labels and numbers and are meant to stay that way.
        // ------------------------------------------------------------------

        public const string HudBack = "Back";
        public const string HudPause = "Pause";
        public const string HudSettings = "Settings";

        /// <summary>What the clock reads when the player has turned it off.</summary>
        public const string HudTimerHidden = "--:--";

        public static string HudStatus(string clock, int hearts, int mistakes, int remaining) =>
            $"{clock}    Hearts {hearts}    Mistakes {mistakes}    Left {remaining}";

        /// <summary>
        /// The same strip with the mistake counter left out, for a player who
        /// has turned immediate mistake feedback off. A count that climbs the
        /// moment a wrong digit lands is that feedback written in numbers, so it
        /// goes with the colour rather than outliving it.
        /// </summary>
        public static string HudStatusUnchecked(string clock, int hearts, int remaining) =>
            $"{clock}    Hearts {hearts}    Left {remaining}";

        public static string HudSolvedBanner(string clock) => $"Solved in {clock}";

        public const string HudFailedBanner = "Out of hearts";

        public const string PadUndo = "Undo";
        public const string PadErase = "Erase";
        public const string PadNotes = "Notes";
        public const string PadHint = "Hint";

        /// <summary>The hint button while a hint is revealed: it says what the
        /// second tap will do, not how many are left.</summary>
        public const string PadHintFill = "Fill it";

        public static string PadHintCount(int remaining) => $"Hint {remaining}";

        // ------------------------------------------------------------------
        // Pause
        // ------------------------------------------------------------------

        public const string PauseTitle = "Paused";
        public const string PauseResume = "Resume";
        public const string PauseRestart = "Restart";
        public const string PauseRestartConfirm = "Start over? Tap again";
        public const string PauseHome = "Home";

        /// <summary>Reassurance, delivered flatly. Leaving costs nothing and
        /// the player should not have to wonder.</summary>
        public const string PauseNote = "The puzzle waits. It has nothing else on.";

        // ------------------------------------------------------------------
        // Settings
        // ------------------------------------------------------------------

        public const string SettingsTitle = "Settings";
        public const string SettingsBack = "Back";

        public const string SettingsMistakeLimit = "Mistake limit";
        public const string SettingsMistakeLimitNote = "Applies from the next puzzle";
        public const string SettingsHighlightMistakes = "Highlight mistakes";
        public const string SettingsAutoRemoveNotes = "Auto-remove notes";
        public const string SettingsShowTimer = "Show timer";

        /// <summary>The accessibility row. It names what turning it on does
        /// rather than what it is called on any one platform, because the
        /// device it is following may call it something else - or, on iOS, may
        /// not be answering at all.</summary>
        public const string SettingsReduceMotion = "Reduce motion";

        public const string SettingsSound = "Sound";
        public const string SettingsHaptics = "Haptics";

        /// <summary>The theme row. It names the look it turns on rather than the
        /// setting it is - "Dark theme: On" is what the player is asking for,
        /// where "Theme: Dark" would need a second word to say which way the
        /// switch goes.</summary>
        public const string SettingsDarkTheme = "Dark theme";

        public const string SettingsNote = "None of these are secretly scored.";

        public static string SettingsToggle(bool on) => on ? "On" : "Off";

        // ------------------------------------------------------------------
        // Results
        // ------------------------------------------------------------------

        public const string ResultsTitle = "Solved";
        public const string ResultsNewBest = "New best time";
        public const string ResultsNext = "Next Puzzle";
        public const string ResultsHome = "Home";

        public static string ResultsBest(string clock) => $"Best {clock}";

        public static string ResultsCounters(int mistakes, int hints) =>
            $"{Plural(mistakes, "mistake")}    {Plural(hints, "hint")}";

        // ------------------------------------------------------------------
        // Out of hearts
        // ------------------------------------------------------------------

        public const string GameOverTitle = "Out of hearts";

        /// <summary>A loss is an outcome, not a telling-off. The line says the
        /// puzzle survived, because it did.</summary>
        public const string GameOverBlurb = "The puzzle is still here. It is not going anywhere.";

        public const string GameOverMoreHearts = "More Hearts";
        public const string GameOverRefillUnavailable = "More hearts are not a thing yet. Start over instead.";
        public const string GameOverRestart = "Start Over";
        public const string GameOverHome = "Home";

        public static string GameOverMistakes(int mistakes) => Plural(mistakes, "mistake");

        // ------------------------------------------------------------------
        // Results-card reactions
        //
        // Five to ten lines per outcome bucket, so the card can say something
        // different every solve for a whole session. Which bucket a solve falls
        // in is <see cref="ReactionPicker"/>'s job; which words the bucket has
        // is this file's.
        //
        // Every line is unique across every pool, which is what lets the picker
        // track one "already said" set instead of one per bucket.
        // ------------------------------------------------------------------

        static readonly string[] FastLines =
        {
            "That was quick. Suspiciously quick.",
            "Solved before the kettle boiled.",
            "Fast. The grid did not see it coming.",
            "Quick work. We will pretend that was the plan.",
            "You did not hesitate once, which is mildly unnerving.",
            "Over in less time than it takes to read the rules.",
            "Brisk. Nobody was timing you, but we were.",
        };

        static readonly string[] SlowLines =
        {
            "That took a while. No judgement.",
            "You got there. The route was scenic.",
            "A long one. The grid was in no hurry either.",
            "Somewhere in the middle, time stopped meaning much.",
            "Finished. Eventually. Which still counts.",
            "The clock has opinions. You are not obliged to read them.",
            "Slow and finished beats fast and abandoned.",
        };

        static readonly string[] PerfectLines =
        {
            "No mistakes, no hints. Nothing much to comment on.",
            "Clean. Slightly annoying, but clean.",
            "Not one wrong number. We checked twice.",
            "Flawless. The app has nothing useful to add.",
            "You did all of that without us. Noted.",
            "No hints taken. The hint button is sulking.",
            "A perfect run. Enjoy it before the next one.",
        };

        static readonly string[] MistakeHeavyLines =
        {
            "A few wrong turns. The grid forgave you.",
            "That got messy in the middle. It happens.",
            "Several mistakes, one solved puzzle. Only one of those is on the card.",
            "You argued with the grid and won on points.",
            "Not elegant. Still finished.",
            "The eraser earned its keep today.",
            "Some of those numbers were guesses. We both know it.",
        };

        static readonly string[] HintHeavyLines =
        {
            "We helped. We will not bring it up again.",
            "A few hints in. That is what they are there for, allegedly.",
            "A collaborative effort. Mostly yours.",
            "The hint button had a busy afternoon.",
            "You asked, we answered. Solved is solved.",
            "Some assistance was involved. The grid keeps no records.",
        };

        static readonly string[] SteadyLines =
        {
            "Solved. No notes.",
            "That went roughly how it should have.",
            "Steady work. The grid is empty, which was the whole job.",
            "Nothing dramatic. A perfectly reasonable puzzle.",
            "Done. Every number is where it belongs.",
            "A solid, unremarkable solve. Most of them are.",
            "That one behaved itself.",
        };

        /// <summary>
        /// The pool a bucket draws from. Returned rather than indexed into so
        /// the arrays stay private and the picker cannot reorder them.
        /// </summary>
        public static IReadOnlyList<string> Reactions(ReactionBucket bucket)
        {
            switch (bucket)
            {
                case ReactionBucket.Fast: return FastLines;
                case ReactionBucket.Slow: return SlowLines;
                case ReactionBucket.Perfect: return PerfectLines;
                case ReactionBucket.MistakeHeavy: return MistakeHeavyLines;
                case ReactionBucket.HintHeavy: return HintHeavyLines;
                default: return SteadyLines;
            }
        }

        /// <summary>
        /// Every word the player can see while a puzzle is in front of them -
        /// story 72. Listed rather than left implied, so the rule that no joke
        /// reaches the board is something a test can hold the table to: these
        /// are labels and counters, and none of them is a sentence.
        /// </summary>
        public static IReadOnlyList<string> InPuzzle { get; } = new[]
        {
            HudBack, HudPause, HudSettings, HudTimerHidden, HudFailedBanner,
            PadUndo, PadErase, PadNotes, PadHint, PadHintFill,
            HudStatus("00:00", 3, 0, 81),
            HudStatusUnchecked("00:00", 3, 81),
            HudSolvedBanner("00:00"),
            PadHintCount(3),
        };

        /// <summary>Every bucket, so a test can sweep the whole voice.</summary>
        public static IReadOnlyList<ReactionBucket> Buckets { get; } = new[]
        {
            ReactionBucket.Fast,
            ReactionBucket.Slow,
            ReactionBucket.Perfect,
            ReactionBucket.MistakeHeavy,
            ReactionBucket.HintHeavy,
            ReactionBucket.Steady,
        };

        static string Plural(int count, string noun) =>
            count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
