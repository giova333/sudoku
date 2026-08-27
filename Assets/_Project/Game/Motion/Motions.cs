using System;
using PrimeTween;
using Sudoku.Game.Bootstrap;
using UnityEngine;

namespace Sudoku.Game.Motion
{
    /// <summary>
    /// The whole of how the game moves, as one table of numbers and one switch.
    ///
    /// Every duration, easing curve and travel distance in the project is read
    /// from here, and every one of them passes through <see cref="Seconds"/>,
    /// <see cref="Travel"/> or <see cref="Overshoot"/> on the way out. That is
    /// deliberate: honouring Reduce Motion then costs one boolean rather than a
    /// sweep through a dozen call sites, and a damping rule that has to be
    /// remembered at each of them is a damping rule that will be forgotten at
    /// one of them.
    ///
    /// Static for the same reason <see cref="Sudoku.Game.Theme.Themes"/> and
    /// <see cref="Ui.ButtonTapped"/> are: the interface is built by static
    /// factories, and <see cref="ChunkyBox.PressAnimator"/> - the seam the skin
    /// left for this - is a static field on a component that is created long
    /// before any composition root could hand it an instance.
    ///
    /// Nothing here allocates per frame or per tap. PrimeTween's tweens are
    /// pooled structs, the pool is sized once in <see cref="Install"/>, and
    /// every callback below is a <c>static</c> lambda, which the compiler caches
    /// in a field rather than building a closure at each call.
    /// </summary>
    public static class Motions
    {
        // ------------------------------------------------------------------
        // Durations, in seconds
        // ------------------------------------------------------------------

        /// <summary>How long a face takes to land on its shadow. Short enough
        /// that the button is already down by the time the finger registers
        /// having pressed it.</summary>
        public const float PressDuration = 0.07f;

        /// <summary>And how long it takes to come back up. Longer than the way
        /// down, because the release is the half that carries the overshoot -
        /// this is where a button feels springy rather than merely fast.</summary>
        public const float ReleaseDuration = 0.24f;

        /// <summary>A screen arriving.</summary>
        public const float ScreenDuration = 0.26f;

        /// <summary>An element arriving on a screen that is itself arriving.</summary>
        public const float ElementDuration = 0.30f;

        /// <summary>The gap between one element's entrance and the next, so a
        /// screen assembles rather than appearing.</summary>
        public const float ElementStagger = 0.045f;

        /// <summary>A cell reacting to a digit landing in it.</summary>
        public const float PopDuration = 0.26f;

        /// <summary>A cell refusing one.</summary>
        public const float ShakeDuration = 0.34f;

        /// <summary>How far apart two diagonal bands of the completion sweep
        /// start. Sixteen bands wide, so this sets the sweep's speed.</summary>
        public const float CascadeBand = 0.035f;

        /// <summary>And how long one cell's pop lasts inside that sweep.</summary>
        public const float CascadePop = 0.34f;

        // ------------------------------------------------------------------
        // Travel: scale factors and distances, in interface units
        // ------------------------------------------------------------------

        /// <summary>The scale a screen enters from.</summary>
        public const float ScreenFrom = 0.94f;

        /// <summary>The scale an element enters from. Smaller than the screen's,
        /// so the two reads as one thing settling rather than two.</summary>
        public const float ElementFrom = 0.86f;

        /// <summary>How much bigger a cell gets at the top of a placement pop.</summary>
        public const float PopStrength = 0.26f;

        /// <summary>A finished 3x3 box pops harder than one cell, because nine
        /// cells popping by the same amount reads as a flicker.</summary>
        public const float BoxPopStrength = 0.34f;

        /// <summary>And harder again for the finished puzzle.</summary>
        public const float CascadeStrength = 0.42f;

        /// <summary>How far a refused entry travels sideways.</summary>
        public const float ShakeDistance = 16f;

        // ------------------------------------------------------------------
        // Reduce Motion
        // ------------------------------------------------------------------

        /// <summary>How much of its duration a movement keeps when motion is
        /// reduced. Shorter, not instant: something that changes with no
        /// transition at all is harder to follow, not easier.</summary>
        const float DampedDuration = 0.55f;

        /// <summary>And how much of its travel. Nearly all of the nausea is in
        /// the distance rather than the time, so this damps much harder.</summary>
        const float DampedTravel = 0.3f;

        /// <summary>
        /// How many tweens can be in flight at once. The completion cascade
        /// puts all eighty-one cells in the air on the same frame, and PrimeTween
        /// grows its pool by allocating - so the pool is sized once, here, for
        /// the largest thing the game ever does.
        /// </summary>
        const int Capacity = 128;

        /// <summary>Oscillations per second in a pop. Low, so a pop is one
        /// bounce rather than a vibration.</summary>
        const float PopFrequency = 5f;

        /// <summary>And higher for a shake, which should read as a refusal.</summary>
        const float ShakeFrequency = 14f;

        /// <summary>
        /// The single switch. True damps every overshoot and shortens every
        /// duration in the game, because everything routes through the three
        /// members below.
        ///
        /// The composition root observes the player's preference into it; that
        /// preference in turn defaults to <see cref="ReduceMotion.PreferredByTheOs"/>,
        /// so a device that asks for reduced motion is honoured without the
        /// player being asked, and a player who disagrees can still say so.
        /// </summary>
        public static bool Reduced { get; set; }

        /// <summary>A duration, damped if the player has asked for less
        /// motion.</summary>
        public static float Seconds(float seconds) =>
            Reduced ? seconds * DampedDuration : seconds;

        /// <summary>A distance or a scale delta, damped the same way.</summary>
        public static float Travel(float units) =>
            Reduced ? units * DampedTravel : units;

        /// <summary>
        /// The house curve: it overshoots its target and settles back, which is
        /// what makes the interface feel springy rather than mechanical - and
        /// which is exactly the thing Reduce Motion exists to turn off. Damped,
        /// it becomes an ordinary deceleration that never passes its target.
        /// </summary>
        public static Ease Overshoot => Reduced ? Ease.OutQuad : Ease.OutBack;

        /// <summary>
        /// Installs the motion layer.
        ///
        /// Called once by the composition root, before any control is built.
        /// Like <see cref="Sudoku.Game.Theme.Themes.Install"/> it resets its own statics first,
        /// because the project runs with domain reload disabled and a delegate
        /// left over from the last Play mode session points at nothing.
        /// </summary>
        public static void Install()
        {
            Reduced = false;

            // The whole of the hand-off the skin pass left: the box knows how
            // to be pressed and does not have to learn what a tween is, and no
            // view, screen or button call site changes to gain the animation.
            ChunkyBox.PressAnimator = Depress;

            PrimeTweenConfig.SetTweensCapacity(Capacity);

            // A screen is deactivated the moment the navigator moves off it,
            // which can happen with its entrance still in flight. That is
            // ordinary here rather than a mistake worth a console line.
            PrimeTweenConfig.warnTweenOnDisabledTarget = false;

            // Re-pressing a button that is already down, or popping a cell that
            // is already at rest, is a no-op with nothing to say about it.
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        }

        /// <summary>
        /// Lowers a face onto its shadow, or lets it back up.
        ///
        /// The release carries the overshoot and the press does not: a button
        /// that springs on the way down feels loose, and one that springs on the
        /// way back up feels like a button.
        /// </summary>
        static void Depress(ChunkyBox box, bool pressed)
        {
            // A press and its release chase each other closely enough that the
            // second would otherwise fight the first over the same float.
            Tween.StopAll(box);

            Tween.Custom(box, box.ShadowDepth,
                pressed ? Skin.PressedShadow : Skin.RestingShadow,
                Seconds(pressed ? PressDuration : ReleaseDuration),
                static (target, depth) => target.ShadowDepth = depth,
                pressed ? Ease.OutQuad : Overshoot);
        }

        /// <summary>
        /// A screen arriving: the screen itself swells into place, and any
        /// chunky box sitting directly on it follows one after another.
        ///
        /// Only direct children are staggered, and that is the whole selection
        /// rule. A screen's own elements - its buttons, its card - hang off its
        /// root, so they arrive in sequence; the board, the numpad and the
        /// status strip are containers rather than boxes, so the game screen
        /// gets the swell and none of the shuffling, which is what a screen the
        /// player returns to mid-puzzle wants.
        /// </summary>
        public static void Enter(RectTransform root)
        {
            if (root == null) return;

            Tween.StopAll(root);
            root.localScale = new Vector3(ScreenFrom, ScreenFrom, 1f);
            Tween.Scale(root, ScreenFrom, 1f, Seconds(ScreenDuration), Overshoot);

            var delay = 0f;
            for (var i = 0; i < root.childCount; i++)
            {
                var box = root.GetChild(i).GetComponent<ChunkyBox>();
                if (box == null) continue;

                delay += Seconds(ElementStagger);

                // Set before the tween rather than left to it: with a start
                // delay the tween writes nothing until it begins, and the
                // element would sit at full size for a frame and then jump.
                Tween.StopAll(box.Rect);
                box.Rect.localScale = new Vector3(ElementFrom, ElementFrom, 1f);
                Tween.Scale(box.Rect, ElementFrom, 1f, Seconds(ElementDuration),
                    Overshoot, startDelay: delay);
            }
        }

        /// <summary>Something reacting to being filled in.</summary>
        public static void Pop(Transform target, float strength) => Pop(target, strength, 0f);

        /// <summary>
        /// The same, held back - what the completion sweep is made of.
        ///
        /// Whatever was already animating this object is completed rather than
        /// stopped, because a punch that is cut off half way leaves the thing it
        /// was punching at whatever size it had reached.
        /// </summary>
        public static void Pop(Transform target, float strength, float delay)
        {
            if (target == null) return;

            Tween.CompleteAll(target);
            Tween.PunchScale(target, Vector3.one * Travel(strength), Seconds(PopDuration),
                PopFrequency, startDelay: delay);
        }

        /// <summary>
        /// Something refusing what it was just given: a wrong digit, or an edit
        /// to a clue the puzzle handed over. Sideways only - a shake with a
        /// vertical component reads as the board coming loose.
        /// </summary>
        public static void Shake(Transform target)
        {
            if (target == null) return;

            Tween.CompleteAll(target);
            Tween.ShakeLocalPosition(target,
                new Vector3(Travel(ShakeDistance), 0f, 0f),
                Seconds(ShakeDuration), ShakeFrequency);
        }

        /// <summary>
        /// Calls back once a stretch of animation has finished.
        ///
        /// The continuation is handed to PrimeTween as the tween's own target,
        /// which is what lets the callback be a static lambda: passing it as a
        /// captured variable instead would allocate a closure every time a
        /// puzzle is completed.
        /// </summary>
        public static void After(float seconds, Action done)
        {
            if (done == null) return;

            Tween.Delay(done, seconds, static continuation => continuation());
        }
    }
}
