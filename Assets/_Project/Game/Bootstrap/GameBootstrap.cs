using Sudoku.Core.Copy;
using Sudoku.Game.Analytics;
using Sudoku.Game.Audio;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Motion;
using Sudoku.Game.Save;
using Sudoku.Game.Screens;
using Sudoku.Game.Session;
using Sudoku.Game.Settings;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// The composition root. Builds the canvas and every screen in code
    /// and wires the object graph by hand - no DI container until the meta
    /// layer's wiring actually hurts.
    ///
    /// It is also the only place that knows which screen leads to which: every
    /// screen announces what the player asked for and the root routes it
    /// through the <see cref="Navigator"/>, so a new screen is registered here
    /// rather than wired into the screens around it.
    ///
    /// It installs itself so no scene has to be authored to run the game, which
    /// keeps the project free of binary scene edits.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>Reference portrait resolution the layout is designed against.</summary>
        static readonly Vector2 DesignResolution = new Vector2(1080, 1920);

        GameAnalytics _analytics;

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

            var settings = new GameSettings(new PlayerPrefsStore());

            // The palette comes first, before a single graphic exists: every one
            // of them asks the theme what colour it is on the frame it is built,
            // and Observe fires now, so the saved theme is already in force
            // rather than being corrected a frame later. It fires again on every
            // change, which is the whole of "instant, no restart" - flipping the
            // preference from the settings screen repaints every screen,
            // including the ones the navigator currently has deactivated.
            //
            // Nothing below this line names a colour.
            Themes.Install();
            settings.Theme.Observe(Themes.Use);

            // Motion is installed alongside the palette and for the same
            // reason: the press animator is a static the skin reaches for the
            // first time a button is built, so it has to be in place before one
            // exists. Observing the preference into Motions is the whole of
            // honouring Reduce Motion - one boolean, read everywhere.
            Motions.Install();
            settings.ReduceMotion.Observe(reduced => Motions.Reduced = reduced);

            var canvas = BuildCanvas();

            // The ground goes edge to edge, behind the notch and under the home
            // indicator, because a letterboxed background reads as a bug. Every
            // control goes inside the safe rect instead - see SafeAreaFitter.
            Ui.Stretch(Ui.Panel("Background", canvas.transform, ThemeSlot.ScreenBackground).rectTransform);
            var screens = SafeAreaFitter.Fit(canvas);

            var navigator = new Navigator();

            // Every screen change is an entrance, wired once here rather than
            // once per screen: the navigator already says which screen came to
            // the front, and a screen registered later gets the transition
            // without being told it exists - the same bargain Register makes.
            navigator.Navigated += screen => Motions.Enter(screen.Root);

            // One save file behind everything that remembers: the in-progress
            // puzzle per difficulty, and which puzzles the banks have already
            // dealt. Reading it is the first thing that happens, so a resume is
            // waiting before any screen is built.
            var saves = new SaveStore();

            // Sound and haptics are two switches, not one, and both are already
            // persisted preferences - so the service holds no preference of its
            // own and is written to from the settings it must obey. Observe
            // fires immediately, which is also what applies the saved mute
            // before the first screen can make a noise.
            var audio = AudioService.Create(transform);
            settings.SoundEnabled.Observe(on => audio.SoundEnabled = on);
            settings.HapticsEnabled.Observe(on => audio.HapticsEnabled = on);
            Ui.ButtonTapped = () => audio.Play(Sfx.ButtonTap);

            var home = HomeView.Create(screens);
            var difficulty = DifficultySelectView.Create(screens);
            var game = BuildGameScreen(screens, navigator, settings, saves, audio);
            var pause = PauseView.Create(screens);
            var settingsScreen = SettingsView.Create(screens, settings);
            var resume = ResumePromptView.Create(screens);
            var results = ResultsView.Create(screens);
            var gameOver = GameOverView.Create(screens);

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
                // The sweep across the finished board, which calls the flow on
                // when it has crossed.
                BoardCascade = game.Cascade,

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
            // One picker for the life of the app, because "no line twice in a
            // session" is a fact about what the player has already read, and
            // only something that outlives a single card can hold it.
            var reactions = new ReactionPicker();
            results.Reaction = reactions.Next;

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

            // Analytics is assembled last and subscribed to streams that were
            // already there, so nothing above it had to be told it exists. The
            // console stands in until an SDK is chosen; swapping one in is this
            // one argument.
            _analytics = new GameAnalytics(new ConsoleAnalyticsService());
            _analytics.Observe(settings);
            _analytics.Observe(navigator);
            _analytics.Observe(game);

            navigator.Go<HomeView>();
        }

        /// <summary>
        /// Batched events are held in memory, and a backgrounded mobile process
        /// may never be scheduled again - so the batch goes out here rather than
        /// waiting for a next event that never arrives.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (paused && _analytics != null) _analytics.Flush();
        }

        void OnApplicationQuit()
        {
            if (_analytics != null) _analytics.Flush();
        }

        /// <summary>
        /// Builds the board, numpad and status strip under one rect, so the
        /// navigator can put the whole puzzle aside without tearing it down.
        /// </summary>
        GamePresenter BuildGameScreen(Transform parent, Navigator navigator, GameSettings settings,
            SaveStore saves, IAudioService audio)
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

            var hud = HudView.Create(root, boardSize, 120 + boardSize / 2f + 105f);
            hud.BackTapped += navigator.Back;
            hud.PauseTapped += navigator.Go<PauseView>;
            hud.SettingsTapped += navigator.Go<SettingsView>;

            var presenter = gameObject.AddComponent<GamePresenter>();
            presenter.Initialise(new PuzzleLibrary(saves), saves, settings, root, board, numpad, hud,
                audio);
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
