using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Save;
using Sudoku.Game.Screens;
using Sudoku.Game.Session;
using Sudoku.Game.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// The composition root. Builds the canvas and the greybox screens in code
    /// and wires the object graph by hand - no DI container until the meta
    /// layer's wiring actually hurts.
    ///
    /// It is also the only place that knows which screen leads to which: every
    /// screen announces what the player asked for and the root routes it
    /// through the <see cref="Navigator"/>, so a new screen is registered here
    /// rather than wired into the screens around it.
    ///
    /// It installs itself so no scene has to be authored to run the game, which
    /// keeps the greybox free of binary scene edits.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>Reference portrait resolution the layout is designed against.</summary>
        static readonly Vector2 DesignResolution = new Vector2(1080, 1920);

        static readonly Color Background = new Color(0.96f, 0.96f, 0.94f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;

            var go = new GameObject("[Sudoku]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            EnsureEventSystem();

            var canvas = BuildCanvas();
            Ui.Stretch(Ui.Panel("Background", canvas.transform, Background).rectTransform);

            var navigator = new Navigator();
            var settings = new GameSettings(new PlayerPrefsStore());

            // One save file behind everything that remembers: the in-progress
            // puzzle per difficulty, and which puzzles the banks have already
            // dealt. Reading it is the first thing that happens, so a resume is
            // waiting before any screen is built.
            var saves = new SaveStore();

            var home = HomeView.Create(canvas.transform);
            var difficulty = DifficultySelectView.Create(canvas.transform);
            var game = BuildGameScreen(canvas.transform, navigator, settings, saves);
            var pause = PauseView.Create(canvas.transform);
            var settingsScreen = SettingsView.Create(canvas.transform, settings);
            var resume = ResumePromptView.Create(canvas.transform);
            var results = ResultsView.Create(canvas.transform);
            var gameOver = GameOverView.Create(canvas.transform);

            navigator.Register(home);
            navigator.Register(difficulty);
            navigator.Register(game);
            navigator.Register(pause);
            navigator.Register(settingsScreen);
            navigator.Register(resume);
            navigator.Register(results);
            navigator.Register(gameOver);

            // Continue is answered by the save file, never by whether a session
            // happens to be in memory: a puzzle left mid-solve is still there
            // after the process is killed, and that is exactly the launch where
            // being offered it matters most.
            home.ContinueTarget = () => saves.Data.MostRecent();
            home.ContinueTapped += () =>
            {
                var waiting = saves.Data.MostRecent();
                if (waiting == null) return;

                game.Resume(waiting);
                navigator.Go<GamePresenter>();
            };
            home.NewGameTapped += navigator.Go<DifficultySelectView>;
            home.SettingsTapped += navigator.Go<SettingsView>;

            // Settings is one screen with two ways in. Pushed from the game it
            // is an overlay in the only sense that matters: the puzzle is still
            // on the back stack, and leaving it suspended the clock.
            settingsScreen.BackTapped += navigator.Back;

            difficulty.BackTapped += navigator.Back;
            difficulty.Waiting = tier => saves.Data.ResumableFor(tier);
            difficulty.TierChosen += tier =>
            {
                // A tier with a game under it is a question, not an
                // instruction. Assuming either answer is wrong: resuming
                // ignores a player who wanted a clean start, and starting
                // fresh destroys work.
                var waiting = saves.Data.ResumableFor(tier);
                if (waiting != null)
                {
                    resume.Offer(waiting);
                    navigator.Go<ResumePromptView>();
                    return;
                }

                game.StartPuzzle(tier);
                navigator.Replace<GamePresenter>();
            };

            resume.BackTapped += navigator.Back;
            // Back first, so the puzzle takes the difficulty picker's place on
            // the stack rather than sitting on top of it: leaving the game
            // lands on Home either way in.
            resume.ResumeTapped += waiting =>
            {
                navigator.Back();
                game.Resume(waiting);
                navigator.Replace<GamePresenter>();
            };
            resume.StartFreshConfirmed += waiting =>
            {
                navigator.Back();
                game.StartFresh(waiting.Tier);
                navigator.Replace<GamePresenter>();
            };

            // Showing the pause screen hides the game screen, and that is what
            // stops the clock and the board taking input - there is no second
            // pause mechanism to keep in step with the first.
            pause.ResumeTapped += navigator.Back;
            pause.RestartTapped += () =>
            {
                game.Restart();
                navigator.Back();
            };
            // The session is only suspended, never dropped, so Home finds it
            // waiting under Continue.
            pause.HomeTapped += navigator.ResetTo<HomeView>;

            // Finishing a puzzle is a flow, not a screen change. The stages run
            // in order and a stage nobody has filled in is skipped, so today
            // this reaches the results card immediately - see CompletionFlow for
            // what the empty stages are reserved for.
            var completion = new CompletionFlow
            {
                // BoardCascade: ticket #10 assigns the completion animation.
                // Interstitial: RESERVED for an interstitial ad. Deliberately
                // left unassigned - there is no ad SDK in this milestone, and
                // the seam has to exist before there is something to put in it.
                ResultsCard = navigator.Go<ResultsView>
            };

            game.Solved += result =>
            {
                results.Show(result);
                completion.Run();
            };

            // Back from the results card pops it and leaves the player on the
            // puzzle that was just dealt, with Home still under it.
            results.NextTapped += () =>
            {
                game.StartPuzzle(results.Tier);
                navigator.Back();
            };
            results.HomeTapped += navigator.ResetTo<HomeView>;
            // results.Reaction: ticket #12 assigns the line the card says about
            // the solve, drawn from its reaction pools. Left unset here so the
            // card stays silent rather than repeating one hardcoded joke.

            // Losing gets its own screen rather than a banner over the board:
            // it is an outcome, and it is offered the same way out as a win.
            game.OutOfHearts += (tier, mistakes) =>
            {
                gameOver.Show(tier, mistakes);
                navigator.Go<GameOverView>();
            };

            // The offer and the answer both come from the session's consumable
            // service, so the day one is backed by a rewarded ad, neither this
            // wiring nor the screen changes.
            gameOver.RefillAvailable = () => game.CanRefillHearts;
            gameOver.MoreHeartsTapped += () =>
            {
                if (game.ContinueWithMoreHearts()) navigator.Back();
            };
            gameOver.RestartTapped += () =>
            {
                game.Restart();
                navigator.Back();
            };
            gameOver.HomeTapped += navigator.ResetTo<HomeView>;

            navigator.Go<HomeView>();
        }

        /// <summary>
        /// Builds the board, numpad and status strip under one rect, so the
        /// navigator can put the whole puzzle aside without tearing it down.
        /// </summary>
        GamePresenter BuildGameScreen(Transform parent, Navigator navigator, GameSettings settings,
            SaveStore saves)
        {
            var root = Ui.Rect("Game", parent);
            Ui.Stretch(root);

            // The board takes the full design width minus a margin; the numpad
            // and status strip sit above and below it.
            const float margin = 40f;
            var boardSize = DesignResolution.x - margin * 2f;

            var board = BoardView.Create(root, boardSize);
            Ui.Place(board.GetComponent<RectTransform>(), new Vector2(0, 120), new Vector2(boardSize, boardSize));

            var numpad = NumpadView.Create(root, boardSize, 120 - boardSize / 2f - 130f);

            var hud = HudView.Create(root, boardSize, 120 + boardSize / 2f + 90f);
            hud.BackTapped += navigator.Back;
            hud.PauseTapped += navigator.Go<PauseView>;
            hud.SettingsTapped += navigator.Go<SettingsView>;

            var presenter = gameObject.AddComponent<GamePresenter>();
            presenter.Initialise(new PuzzleLibrary(saves), saves, settings, root, board, numpad, hud);
            return presenter;
        }

        Canvas BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = DesignResolution;
            // Match width, so the board never overflows a narrow phone.
            scaler.matchWidthOrHeight = 0f;

            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            DontDestroyOnLoad(go);

            // The project is configured for the new Input System, so the legacy
            // StandaloneInputModule would silently receive nothing.
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
