using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The payoff. Finishing a puzzle is the moment the game has to say
    /// something back, so this screen exists to show how the solve went and to
    /// put the next puzzle one tap away.
    ///
    /// It renders a <see cref="PuzzleResult"/> and holds no session, so the
    /// finished puzzle can be cleared out from under it without the card losing
    /// what it was showing.
    /// </summary>
    public sealed class ResultsView : MonoBehaviour, IScreen
    {
        RectTransform _root;
        Text _tier;
        Text _time;
        Text _counters;
        Text _best;
        ThemedGraphic _bestTheme;
        Text _reaction;

        PuzzleResult _result;

        public Action NextTapped;
        public Action HomeTapped;

        /// <summary>
        /// What the card says about the solve. Ticket #12 assigns a function
        /// that picks from the reaction pools; until then the line is left
        /// blank, because a single hardcoded joke would be worse than silence
        /// and would have to be found and deleted later.
        /// </summary>
        public Func<PuzzleResult, string> Reaction;

        /// <summary>The tier that was just finished - what "another one" means.</summary>
        public DifficultyTier Tier => _result != null ? _result.Tier : DifficultyTier.Easy;

        public RectTransform Root => _root;

        public static ResultsView Create(Transform parent)
        {
            var rect = Ui.Rect("Results", parent);
            var view = rect.gameObject.AddComponent<ResultsView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 96, ThemeSlot.Title);
            Ui.Place(title.rectTransform, new Vector2(0, 560), new Vector2(800, 140));
            title.text = "Solved";

            view._tier = Ui.Label("Tier", rect, 34, ThemeSlot.Muted);
            Ui.Place(view._tier.rectTransform, new Vector2(0, 460), new Vector2(800, 60));

            // The time is the headline number: it is the one a player compares
            // against themselves, and the only one a record can be set on.
            view._time = Ui.Label("Time", rect, 120, ThemeSlot.Title);
            Ui.Place(view._time.rectTransform, new Vector2(0, 340), new Vector2(800, 160));

            view._counters = Ui.Label("Counters", rect, 30, ThemeSlot.Muted);
            Ui.Place(view._counters.rectTransform, new Vector2(0, 240), new Vector2(800, 50));

            view._best = Ui.Label("Best", rect, 32, ThemeSlot.Muted);
            view._bestTheme = view._best.GetComponent<ThemedGraphic>();
            Ui.Place(view._best.rectTransform, new Vector2(0, 170), new Vector2(800, 50));

            view._reaction = Ui.Label("Reaction", rect, 28, ThemeSlot.Muted);
            Ui.Place(view._reaction.rectTransform, new Vector2(0, 80), new Vector2(880, 60));
            view._reaction.text = string.Empty;

            var next = AddButton(rect, "Next Puzzle", -80, ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            next.onClick.AddListener(() => view.NextTapped?.Invoke());

            var home = AddButton(rect, "Home", -220, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            home.onClick.AddListener(() => view.HomeTapped?.Invoke());

            return view;
        }

        /// <summary>
        /// Hands the card what to show. Called before the navigator brings the
        /// screen up, so the card is never seen holding the previous solve.
        /// </summary>
        public void Show(PuzzleResult result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            Render();
        }

        public void OnShow() => Render();

        public void OnHide()
        {
        }

        void Render()
        {
            if (_result == null) return;

            _tier.text = _result.Tier.ToString();
            _time.text = Clock(_result.ElapsedSeconds);
            _counters.text = $"{Plural(_result.MistakeCount, "mistake")}    " +
                             $"{Plural(_result.HintsUsed, "hint")}";

            if (_result.IsNewBest)
            {
                _best.text = "New best time";
                _bestTheme.Use(ThemeSlot.Celebrate);
            }
            else
            {
                _best.text = $"Best {Clock(_result.BestSeconds)}";
                _bestTheme.Use(ThemeSlot.Muted);
            }

            _reaction.text = Reaction != null ? Reaction(_result) ?? string.Empty : string.Empty;
        }

        static string Clock(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var rest = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{rest:00}";
        }

        static string Plural(int count, string noun) =>
            count == 1 ? $"1 {noun}" : $"{count} {noun}s";

        static Button AddButton(Transform parent, string text, float y, ThemeSlot fill, ThemeSlot textSlot)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textSlot);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
