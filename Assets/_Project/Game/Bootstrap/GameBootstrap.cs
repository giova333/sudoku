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
    /// <see cref="Awake"/> is a list of what has to happen and in what order,
    /// and each step is a method of its own. The order is the load-bearing part:
    /// the palette and the motion table are statics that every graphic reads on
    /// the frame it is built, so they come before anything is built; the save
    /// file comes before the screens that offer to continue from it; and
    /// analytics comes last, subscribing to streams that were already there.
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
            InstallLook(settings);

            var canvas = BuildCanvas();
            var screens = BuildGround(canvas);

            // One save file behind everything that remembers: the in-progress
            // puzzle per difficulty, and which puzzles the banks have already
            // dealt. Reading it is the first thing that happens, so a resume is
            // waiting before any screen is built.
            var saves = new SaveStore();
            var audio = InstallAudio(settings);

            var navigator = new Navigator();

            // Every screen change is an entrance, wired once here rather than
            // once per screen: the navigator already says which screen came to
            // the front, and a screen registered later gets the transition
            // without being told it exists - the same bargain Register makes.
            navigator.Navigated += screen => Motions.Enter(screen.Root);

            var game = BuildGameScreen(screens, navigator, settings, saves, audio);
            var app = RegisterScreens(screens, navigator, settings, game);

            WireMenus(navigator, saves, app);
            WirePause(navigator, game, app);
            WireCompletion(navigator, game, app);
            WireGameOver(navigator, game, app);
            InstallAnalytics(settings, navigator, game);

            navigator.Go<HomeView>();
        }

        /// <summary>
        /// Every screen the navigator knows about, gathered so that the wiring
        /// methods below take one argument rather than eight. It is a bundle of
        /// references and nothing else - who leads where is decided by the
        /// methods that read it, not by the screens themselves.
        /// </summary>
        sealed class Screens
        {
            public HomeView Home;
            public DifficultySelectView Difficulty;
            public GamePresenter Game;
            public PauseView Pause;
            public SettingsView Settings;
            public ResumePromptView Resume;
            public ResultsView Results;
            public GameOverView GameOver;
        }

        /// <summary>
        /// The palette and the motion table, before a single graphic exists.
        ///
        /// Every graphic asks the theme what colour it is on the frame it is
        /// built, and the press animator is a static the skin reaches for the
        /// first time a button exists - so both have to be in place before
        /// anything is built. Observe fires now, which is what puts the saved
        /// theme and the saved Reduce Motion answer into force rather than
        /// correcting them a frame later. It fires again on every change, which
        /// is the whole of "instant, no restart": flipping the preference
        /// repaints every screen, including the ones the navigator currently has
        /// deactivated.
        ///
        /// Nothing below this line names a colour.
        /// </summary>
        static void InstallLook(GameSettings settings)
        {
            Themes.Install();
            settings.Theme.Observe(Themes.Use);

            Motions.Install();
            settings.ReduceMotion.Observe(reduced => Motions.Reduced = reduced);
        }

        /// <summary>
        /// The ground and the rect every control hangs off.
        ///
        /// The ground goes edge to edge, behind the notch and under the home
        /// indicator, because a letterboxed background reads as a bug. Every
        /// control goes inside the safe rect instead - see SafeAreaFitter.
        /// </summary>
        static RectTransform BuildGround(Canvas canvas)
        {
            Ui.Stretch(Ui.Panel("Background", canvas.transform, ThemeSlot.ScreenBackground).rectTransform);
            return SafeAreaFitter.Fit(canvas);
        }

        /// <summary>
        /// Sound and haptics are two switches, not one, and both are already
        /// persisted preferences - so the service holds no preference of its own
        /// and is written to from the settings it must obey. Observe fires
        /// immediately, which is also what applies the saved mute before the
        /// first screen can make a noise.
        /// </summary>
        IAudioService InstallAudio(GameSettings settings)
        {
            var audio = AudioService.Create(transform);
            settings.SoundEnabled.Observe(on => audio.SoundEnabled = on);
            settings.HapticsEnabled.Observe(on => audio.HapticsEnabled = on);
            Ui.ButtonTapped = () => audio.Play(Sfx.ButtonTap);
            return audio;
        }

        /// <summary>Builds the screens the game screen does not build, and hands
        /// every one of them to the navigator.</summary>
        static Screens RegisterScreens(Transform parent, Navigator navigator, GameSettings settings,
            GamePresenter game)
        {
            var app = new Screens
            {
                Home = HomeView.Create(parent),
                Difficulty = DifficultySelectView.Create(parent),
                Game = game,
                Pause = PauseView.Create(parent),
                Settings = SettingsView.Create(parent, settings),
                Resume = ResumePromptView.Create(parent),
                Results = ResultsView.Create(parent),
                GameOver = GameOverView.Create(parent)
            };

            navigator.Register(app.Home);
            navigator.Register(app.Difficulty);
            navigator.Register(app.Game);
            navigator.Register(app.Pause);
            navigator.Register(app.Settings);
            navigator.Register(app.Resume);
            navigator.Register(app.Results);
            navigator.Register(app.GameOver);

            return app;
        }

        /// <summary>Home, the difficulty picker, the resume question and
        /// settings - everything on the way in to a puzzle.</summary>
        static void WireMenus(Navigator navigator, SaveStore saves, Screens app)
        {
            // Continue is answered by the save file, never by whether a session
            // happens to be in memory: a puzzle left mid-solve is still there
            // after the process is killed, and that is exactly the launch where
            // being offered it matters most.
            app.Home.ContinueTarget = () => saves.Data.MostRecent();
            app.Home.ContinueTapped += () =>
            {
                var waiting = saves.Data.MostRecent();
                if (waiting == null) return;

                app.Game.Resume(waiting);
                navigator.Go<GamePresenter>();
            };
            app.Home.NewGameTapped += navigator.Go<DifficultySelectView>;
            app.Home.SettingsTapped += navigator.Go<SettingsView>;

            // Settings is one screen with two ways in. Pushed from the game it
            // is an overlay in the only sense that matters: the puzzle is still
            // on the back stack, and leaving it suspended the clock.
            app.Settings.BackTapped += navigator.Back;

            app.Difficulty.BackTapped += navigator.Back;
            app.Difficulty.Waiting = tier => saves.Data.ResumableFor(tier);
            app.Difficulty.TierChosen += tier =>
            {
                // A tier with a game under it is a question, not an
                // instruction. Assuming either answer is wrong: resuming
                // ignores a player who wanted a clean start, and starting
                // fresh destroys work.
                var waiting = saves.Data.ResumableFor(tier);
                if (waiting != null)
                {
                    app.Resume.Offer(waiting);
                    navigator.Go<ResumePromptView>();
                    return;
                }

                app.Game.StartPuzzle(tier);
                navigator.Replace<GamePresenter>();
            };

            app.Resume.BackTapped += navigator.Back;
            // Back first, so the puzzle takes the difficulty picker's place on
            // the stack rather than sitting on top of it: leaving the game
            // lands on Home either way in.
            app.Resume.ResumeTapped += waiting =>
            {
                navigator.Back();
                app.Game.Resume(waiting);
                navigator.Replace<GamePresenter>();
            };
            app.Resume.StartFreshConfirmed += waiting =>
            {
                navigator.Back();
                app.Game.StartFresh(waiting.Tier);
                navigator.Replace<GamePresenter>();
            };
        }

        /// <summary>
        /// Showing the pause screen hides the game screen, and that is what
        /// stops the clock and the board taking input - there is no second pause
        /// mechanism to keep in step with the first.
        /// </summary>
        static void WirePause(Navigator navigator, GamePresenter game, Screens app)
        {
            app.Pause.ResumeTapped += navigator.Back;
            app.Pause.RestartTapped += () =>
            {
                game.Restart();
                navigator.Back();
            };
            // The session is only suspended, never dropped, so Home finds it
            // waiting under Continue.
            app.Pause.HomeTapped += navigator.ResetTo<HomeView>;
        }

        /// <summary>
        /// Finishing a puzzle is a flow, not a screen change. The stages run in
        /// order and a stage nobody has filled in is skipped, so today this
        /// reaches the results card immediately - see CompletionFlow for what
        /// the empty stages are reserved for.
        /// </summary>
        static void WireCompletion(Navigator navigator, GamePresenter game, Screens app)
        {
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
                app.Results.Show(result);
                completion.Run();
            };

            // Back from the results card pops it and leaves the player on the
            // puzzle that was just dealt, with Home still under it.
            app.Results.NextTapped += () =>
            {
                game.StartPuzzle(app.Results.Tier);
                navigator.Back();
            };
            app.Results.HomeTapped += navigator.ResetTo<HomeView>;

            // One picker for the life of the app, because "no line twice in a
            // session" is a fact about what the player has already read, and
            // only something that outlives a single card can hold it.
            var reactions = new ReactionPicker();
            app.Results.Reaction = reactions.Next;
        }

        /// <summary>
        /// Losing gets its own screen rather than a banner over the board: it is
        /// an outcome, and it is offered the same way out as a win.
        /// </summary>
        static void WireGameOver(Navigator navigator, GamePresenter game, Screens app)
        {
            game.OutOfHearts += (tier, mistakes) =>
            {
                app.GameOver.Show(tier, mistakes);
                navigator.Go<GameOverView>();
            };

            // The offer and the answer both come from the session's consumable
            // service, so the day one is backed by a rewarded ad, neither this
            // wiring nor the screen changes.
            app.GameOver.RefillAvailable = () => game.CanRefillHearts;
            app.GameOver.MoreHeartsTapped += () =>
            {
                if (game.ContinueWithMoreHearts()) navigator.Back();
            };
            app.GameOver.RestartTapped += () =>
            {
                game.Restart();
                navigator.Back();
            };
            app.GameOver.HomeTapped += navigator.ResetTo<HomeView>;
        }

        /// <summary>
        /// Assembled last and subscribed to streams that were already there, so
        /// nothing above it had to be told it exists. The console stands in
        /// until an SDK is chosen; swapping one in is this one argument.
        /// </summary>
        void InstallAnalytics(GameSettings settings, Navigator navigator, GamePresenter game)
        {
            _analytics = new GameAnalytics(new ConsoleAnalyticsService());
            _analytics.Observe(settings);
            _analytics.Observe(navigator);
            _analytics.Observe(game);
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
