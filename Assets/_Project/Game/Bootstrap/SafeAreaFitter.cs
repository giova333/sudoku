using UnityEngine;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// The rect every control is built inside: the screen, less whatever the
    /// device has taken out of it.
    ///
    /// The player build draws edge to edge - Android is configured to render
    /// into the cutout and iOS shows the home indicator - which is what the
    /// off-white ground wants, since a letterboxed background looks like a bug.
    /// The controls are a different matter: a notch over the HUD's back button
    /// or a home indicator across the numpad is a control the player cannot
    /// reach. So the background stays on the canvas and everything else hangs
    /// off this.
    ///
    /// It is driven from <see cref="Screen.safeArea"/> in normalised terms, so
    /// the answer holds at any resolution the canvas scaler is working at, and
    /// it is re-read rather than cached for the life of the app: the safe area
    /// moves when the device rotates, when the keyboard comes up, and on
    /// desktop whenever the window is resized.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rect;
        Rect _applied;
        Vector2Int _resolution;

        /// <summary>Builds the safe rect under a canvas and returns it. Screens
        /// parent themselves to what comes back.</summary>
        public static RectTransform Fit(Canvas canvas)
        {
            var rect = Ui.Rect("SafeArea", canvas.transform);
            var fitter = rect.gameObject.AddComponent<SafeAreaFitter>();
            fitter._rect = rect;
            fitter.Apply();
            return rect;
        }

        /// <summary>
        /// Polled rather than driven from an event, because Unity raises none
        /// for this. It is four floats compared per frame and a write only when
        /// one of them moved.
        /// </summary>
        void Update() => Apply();

        void Apply()
        {
            var area = Screen.safeArea;
            var resolution = new Vector2Int(Screen.width, Screen.height);

            if (area == _applied && resolution == _resolution) return;
            if (resolution.x <= 0 || resolution.y <= 0) return;

            _applied = area;
            _resolution = resolution;

            _rect.anchorMin = new Vector2(area.xMin / resolution.x, area.yMin / resolution.y);
            _rect.anchorMax = new Vector2(area.xMax / resolution.x, area.yMax / resolution.y);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
