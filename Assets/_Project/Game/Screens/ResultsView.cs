using System;
using Sudoku.Core.Copy;
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

        /// <summary>
        /// The line drawn for the solve in hand. Held rather than asked for
        /// again on every render, because the picker is spending a pool that
        /// never repeats within a session - drawing twice for one solve would
        /// burn two lines to show one, and the first would never be read.
        /// </summary>
        string _reactionLine = string.Empty;

        public Action NextTapped;
        public Action HomeTapped;

        /// <summary>
        /// What the card says about the solve, supplied by the composition root
        /// from <see cref="Sudoku.Core.Copy.ReactionPicker"/>. The card asks
        /// rather than chooses, because "which line" depends on what the player
        /// has already read this session and no view should be keeping that.
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
            title.text = CopyTable.ResultsTitle;

            // Everything the solve is worth goes on one card. The screen is
            // called a results card in the spec and it should look like one -
            // a single object the player is handed, not five lines of text at
            // five different heights.
            var card = Ui.Box("Card", rect, ThemeSlot.CardSurface);
            Ui.Place(card.Rect, new Vector2(0, 270), new Vector2(900, 460));
            card.Fill.raycastTarget = false;

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

            var next = Ui.ScreenButton(rect, CopyTable.ResultsNext, -80,
                ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            next.onClick.AddListener(() => view.NextTapped?.Invoke());

            var home = Ui.ScreenButton(rect, CopyTable.ResultsHome, -220,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
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

            // Once per result, here rather than in Render: the card is rendered
            // again every time the navigator brings it up.
            _reactionLine = Reaction != null ? Reaction(_result) ?? string.Empty : string.Empty;

            Render();
        }

        public void OnShow() => Render();

        public void OnHide()
        {
        }

        void Render()
        {
            if (_result == null) return;

            _tier.text = CopyTable.Tier(_result.Tier);
            _time.text = Ui.Clock(_result.ElapsedSeconds);
            _counters.text = CopyTable.ResultsCounters(_result.MistakeCount, _result.HintsUsed);

            if (_result.IsNewBest)
            {
                _best.text = CopyTable.ResultsNewBest;
                _bestTheme.Use(ThemeSlot.Celebrate);
            }
            else
            {
                _best.text = CopyTable.ResultsBest(Ui.Clock(_result.BestSeconds));
                _bestTheme.Use(ThemeSlot.Muted);
            }

            _reaction.text = _reactionLine;
        }
    }
}
