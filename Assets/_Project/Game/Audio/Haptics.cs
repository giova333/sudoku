using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sudoku.Game.Audio
{
    /// <summary>
    /// The one place that knows how a given phone is made to knock.
    ///
    /// There is no cross-platform impact API in the engine: <c>Handheld.Vibrate</c>
    /// is a single half-second buzz on both platforms, which cannot tell a
    /// placement from a mistake and is the wrong feel for either. So each
    /// platform is spoken to in its own terms - a UIImpactFeedbackGenerator on
    /// iOS, a VibrationEffect on Android - and everywhere else this is a no-op,
    /// including the editor, where there is nothing to knock.
    ///
    /// Every entry point swallows its own failures. A phone with a broken or
    /// absent vibrator must not take the game down with it.
    /// </summary>
    public static class Haptics
    {
        /// <summary>
        /// iOS impact styles, as UIImpactFeedbackStyle orders them. Only the
        /// two ends are used; medium is listed so the mapping is readable.
        /// </summary>
        const int StyleLight = 0;
        const int StyleHeavy = 2;

        /// <summary>Android VibrationEffect amplitudes, out of 255.</summary>
        const int AndroidLightAmplitude = 60;
        const int AndroidFirmAmplitude = 180;
        const int AndroidLightMilliseconds = 12;
        const int AndroidFirmMilliseconds = 32;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void _SudokuHapticsPrepare();

        [DllImport("__Internal")]
        static extern void _SudokuHapticsImpact(int style);
#endif

        /// <summary>
        /// Warms the taptic engine up. On iOS an unprepared generator answers
        /// the first tap of a session tens of milliseconds late, which reads as
        /// the game being slow rather than the hardware being cold, so the
        /// service calls this once on startup.
        /// </summary>
        public static void Prepare()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { _SudokuHapticsPrepare(); }
            catch (Exception) { }
#endif
        }

        /// <summary>Knocks once at the given strength.</summary>
        public static void Impact(Haptic strength)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { _SudokuHapticsImpact(strength == Haptic.Firm ? StyleHeavy : StyleLight); }
            catch (Exception) { }
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidImpact(strength);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _amplitudeControl;
        static bool _resolved;

        static void AndroidImpact(Haptic strength)
        {
            try
            {
                ResolveVibrator();
                if (_vibrator == null) return;

                var milliseconds = strength == Haptic.Firm
                    ? AndroidFirmMilliseconds : AndroidLightMilliseconds;

                // Without amplitude control every buzz is full strength, and a
                // light tap would be indistinguishable from a mistake - so the
                // difference is carried by duration alone instead.
                if (_amplitudeControl)
                {
                    var amplitude = strength == Haptic.Firm
                        ? AndroidFirmAmplitude : AndroidLightAmplitude;
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<AndroidJavaObject>(
                        "createOneShot", (long)milliseconds, amplitude))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (Exception)
            {
            }
        }

        static void ResolveVibrator()
        {
            if (_resolved) return;
            _resolved = true;

            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (_vibrator == null) return;
                _amplitudeControl = _vibrator.Call<bool>("hasAmplitudeControl");
            }
        }
#endif
    }
}
