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
    /// The composition root. Builds the canvas and the greybox screen in code
    /// and wires the object graph by hand - no DI container until the meta
    /// layer's wiring actually hurts.
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

            // The board takes the full design width minus a margin; the numpad
            // and status strip sit above and below it.
            const float margin = 40f;
            var boardSize = DesignResolution.x - margin * 2f;

            var board = BoardView.Create(canvas.transform, boardSize);
            Ui.Place(board.GetComponent<RectTransform>(), new Vector2(0, 120), new Vector2(boardSize, boardSize));

            var numpad = NumpadView.Create(canvas.transform, DesignResolution.x - margin * 2f,
                120 - boardSize / 2f - 130f);

            var hud = HudView.Create(canvas.transform, DesignResolution.x - margin * 2f,
                120 + boardSize / 2f + 90f);

            var presenter = gameObject.AddComponent<GamePresenter>();
            presenter.Initialise(new PuzzleLibrary(), board, numpad, hud);
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
