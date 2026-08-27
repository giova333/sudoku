using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Screens;
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

            var home = HomeView.Create(canvas.transform);
            var difficulty = DifficultySelectView.Create(canvas.transform);
            var game = BuildGameScreen(canvas.transform, navigator);
            var pause = PauseView.Create(canvas.transform);

            navigator.Register(home);
            navigator.Register(difficulty);
            navigator.Register(game);
            navigator.Register(pause);

            home.ContinueAvailable = () => game.HasSession;
            home.ContinueTapped += navigator.Go<GamePresenter>;
            home.NewGameTapped += navigator.Go<DifficultySelectView>;

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
        GamePresenter BuildGameScreen(Transform parent, Navigator navigator)
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

            var presenter = gameObject.AddComponent<GamePresenter>();
            presenter.Initialise(new PuzzleLibrary(), root, board, numpad, hud);
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
