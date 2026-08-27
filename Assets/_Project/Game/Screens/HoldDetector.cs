using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// Fires when a control is held rather than tapped. Long-pressing a digit
    /// enters it as a note - an undiscoverable shortcut, which is why the Notes
    /// toggle exists as well rather than instead.
    /// </summary>
    public sealed class HoldDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float HoldSeconds = 0.35f;

        float _downAt = -1f;
        bool _fired;

        public Action Held;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downAt = Time.unscaledTime;
            _fired = false;
        }

        public void OnPointerUp(PointerEventData eventData) => _downAt = -1f;

        /// <summary>
        /// True once if a hold just fired. The button's own click still arrives
        /// on release, so the tap handler asks this first and stands down.
        /// </summary>
        public bool ConsumeHeld()
        {
            if (!_fired) return false;
            _fired = false;
            return true;
        }

        public void OnPointerExit(PointerEventData eventData) => _downAt = -1f;

        void Update()
        {
            if (_fired || _downAt < 0f) return;
            if (Time.unscaledTime - _downAt < HoldSeconds) return;

            _fired = true;
            _downAt = -1f;
            Held?.Invoke();
        }
    }
}
