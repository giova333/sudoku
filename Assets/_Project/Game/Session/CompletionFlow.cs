using System;

namespace Sudoku.Game.Session
{
    /// <summary>
    /// What happens between the last digit landing and the results card
    /// appearing, as three named stages run in order:
    ///
    ///   1. <see cref="BoardCascade"/> - the sweep across the finished board.
    ///   2. <see cref="Interstitial"/> - RESERVED. Nothing occupies it.
    ///   3. <see cref="ResultsCard"/>  - the payoff the player is waiting for.
    ///
    /// Every stage is optional and a null one is skipped, so the flow ran
    /// correctly with only the third filled in, which is how it shipped before
    /// there was a cascade to put in the first.
    ///
    /// The middle stage exists now, empty, on purpose. An interstitial ad is
    /// added later by assigning one callback here; the alternative - discovering
    /// at monetization time that completion is a single hard-wired call from
    /// gameplay to a screen - is a rewrite of the flow under commercial
    /// pressure. Motion (ticket #10) filled the first stage the same way, and
    /// nothing in here changed to let it.
    ///
    /// Stages take a continuation rather than returning, because both of the
    /// ones still empty are asynchronous: an animation finishes on a later
    /// frame, and an ad finishes when the player closes it.
    /// </summary>
    public sealed class CompletionFlow
    {
        /// <summary>
        /// Stage 1. The board's completion animation, which calls its
        /// continuation when the sweep has finished. Assigned by the composition
        /// root from <see cref="Sudoku.Game.Screens.GamePresenter.Cascade"/>.
        /// </summary>
        public Action<Action> BoardCascade;

        /// <summary>
        /// Stage 2. The reserved interstitial seam. It is deliberately never
        /// assigned in this milestone - there is no ad SDK and no ad - and a
        /// null stage is skipped, so the flow behaves as though it were not
        /// there. Assigning it is the whole of introducing an interstitial:
        /// show the ad, call the continuation when it is dismissed.
        /// </summary>
        public Action<Action> Interstitial;

        /// <summary>Stage 3. Shows the results card. The one stage that is wired today.</summary>
        public Action ResultsCard;

        /// <summary>
        /// True while the flow is between stages. A second completion cannot
        /// start one, which matters once a stage takes frames to finish.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>Runs the stages in order, skipping the ones nobody has filled in.</summary>
        public void Run()
        {
            if (IsRunning) return;

            IsRunning = true;
            Stage(BoardCascade, () => Stage(Interstitial, Finish));
        }

        void Finish()
        {
            IsRunning = false;
            ResultsCard?.Invoke();
        }

        static void Stage(Action<Action> stage, Action next)
        {
            if (stage == null)
            {
                next();
                return;
            }

            stage(next);
        }
    }
}
