using Sudoku.Game.Audio;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Save;
using Sudoku.Game.Screens;
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

            // Sound and haptics are two switches, not one, and both are already
            // persisted preferences - so the service holds no preference of its
            // own and is written to from the settings it must obey. Observe
            // fires immediately, which is also what applies the saved mute
            // before the first screen can make a noise.
            var audio = AudioService.Create(transform);
            settings.SoundEnabled.Observe(on => audio.SoundEnabled = on);
            settings.HapticsEnabled.Observe(on => audio.HapticsEnabled = on);
            Ui.ButtonTapped = () => audio.Play(Sfx.ButtonTap);

            var home = HomeView.Create(canvas.transform);
            var difficulty = DifficultySelectView.Create(canvas.transform);
            var game = BuildGameScreen(canvas.transform, navigator, settings, saves, audio);
            var pause = PauseView.Create(canvas.transform);
            var settingsScreen = SettingsView.Create(canvas.transform, settings);

            navigator.Register(home);
            navigator.Register(difficulty);
            navigator.Register(game);
            navigator.Register(pause);
            navigator.Register(settingsScreen);

            home.ContinueAvailable = () => game.HasSession;
            home.ContinueTapped += navigator.Go<GamePresenter>;
            home.NewGameTapped += navigator.Go<DifficultySelectView>;
            home.SettingsTapped += navigator.Go<SettingsView>;

            // Settings is one screen with two ways in. Pushed from the game it
            // is an overlay in the only sense that matters: the puzzle is still
            // on the back stack, and leaving it suspended the clock.
            settingsScreen.BackTapped += navigator.Back;

            difficulty.BackTapped += navigator.Back;
            difficulty.TierChosen += tier =>
            {
                game.StartPuzzle(tier);
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

            navigator.Go<HomeView>();
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

            var hud = HudView.Create(root, boardSize, 120 + boardSize / 2f + 90f);
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
