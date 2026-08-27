using System;
using UnityEngine;

namespace Sudoku.Game.Motion
{
    /// <summary>
    /// What the device has to say about reduced motion, and - just as
    /// importantly - where it says nothing.
    ///
    /// This is read once, to seed the default of the player's own Reduce Motion
    /// preference. After that the preference is the answer: a player who has
    /// touched the switch has said something more specific than their OS did,
    /// and silently overruling them at the next launch would make the switch
    /// look broken.
    ///
    /// What can actually be read, on this Unity version, with no native code:
    ///
    /// - <b>Android: yes.</b> The accessibility setting "Remove animations"
    ///   writes zero into the global animation scales, and those are ordinary
    ///   values in <c>Settings.Global</c> that any app may read through JNI.
    ///   That is what <see cref="ReadAndroid"/> does, and it needs no plugin,
    ///   no permission and no manifest entry.
    ///
    /// - <b>iOS and tvOS: no.</b> The setting exists - it is
    ///   <c>UIAccessibilityIsReduceMotionEnabled()</c> - but Unity 6000.5 does
    ///   not surface it. <c>UnityEngine.Accessibility.AccessibilitySettings</c>
    ///   exposes exactly three things (<c>fontScale</c>,
    ///   <c>isBoldTextEnabled</c>, <c>isClosedCaptioningEnabled</c>) plus
    ///   <c>AssistiveSupport.isScreenReaderEnabled</c>, and reduced motion is
    ///   not among them. Reaching it means an Objective-C plugin, which cannot
    ///   be compiled or verified outside a device build - so on Apple platforms
    ///   this answers "no preference" and the settings row is the whole of the
    ///   control the player gets. That row is not a consolation prize: it is
    ///   what makes the feature honest on a platform we cannot ask.
    ///
    /// - <b>Editor and everywhere else: no.</b> Same answer, same reason.
    /// </summary>
    public static class ReduceMotion
    {
        /// <summary>
        /// Android's animation scales. Zero on any of them means the player has
        /// asked the system to stop animating; "Remove animations" in
        /// accessibility settings sets all three at once, and the developer
        /// options set them individually.
        /// </summary>
        const string AnimatorScale = "animator_duration_scale";
        const string TransitionScale = "transition_animation_scale";

        static bool _asked;
        static bool _preferred;

        /// <summary>
        /// Whether the OS can be asked at all on this platform. False is not a
        /// failure - it is the honest answer on iOS, and the reason the game
        /// carries a Reduce Motion row of its own.
        /// </summary>
        public static bool ReadableFromOs => Application.platform == RuntimePlatform.Android;

        /// <summary>
        /// Whether the device has asked for less motion. False whenever the
        /// question cannot be put, which is every platform but Android - see the
        /// type's own summary before quoting this anywhere.
        ///
        /// Cached, because the answer seeds a preference default exactly once
        /// per launch and the JNI round trip is not free.
        /// </summary>
        public static bool PreferredByTheOs
        {
            get
            {
                if (_asked) return _preferred;

                _asked = true;
                _preferred = ReadableFromOs && ReadAndroid();
                return _preferred;
            }
        }

        /// <summary>
        /// Asks Android's settings provider for the animation scales.
        ///
        /// Wrapped, and deliberately forgiving: this runs during startup to
        /// decide how bouncy a button is, and a manufacturer that has moved,
        /// renamed or locked down a global setting must not be able to stop the
        /// game launching over it. A device that will not answer is treated the
        /// same as one that has nothing to say.
        /// </summary>
        static bool ReadAndroid()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var globals = new AndroidJavaClass("android.provider.Settings$Global"))
                {
                    return Scale(globals, resolver, AnimatorScale) <= 0f
                        || Scale(globals, resolver, TransitionScale) <= 0f;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read the device's animation scale: {e.Message}");
                return false;
            }
        }

        static float Scale(AndroidJavaClass globals, AndroidJavaObject resolver, string key) =>
            globals.CallStatic<float>("getFloat", resolver, key, 1f);
    }
}
