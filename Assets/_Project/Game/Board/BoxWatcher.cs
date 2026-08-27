using System;
using Sudoku.Core.Session;

namespace Sudoku.Game.Board
{
    /// <summary>
    /// Watches a session for 3x3 boxes being finished, and says so once each.
    ///
    /// A finished box is the one thing on the board the event stream cannot
    /// announce: it is a consequence of a placement rather than something the
    /// player did, so it has to be worked out from the board the session already
    /// exposes. Both the chime and the swell want that moment, and they want the
    /// same one - two copies of "which boxes are done" could drift apart by a
    /// rule or by an event, and the two celebrations would land separately.
    /// </summary>
    public sealed class BoxWatcher
    {
        readonly Action<int> _onCompleted;

        /// <summary>Which boxes were already finished, so a box is announced once
        /// rather than on every later move inside it.</summary>
        readonly bool[] _complete = new bool[BoardBoxes.Count];

        GameSession _session;

        public BoxWatcher(Action<int> onCompleted)
        {
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
        }

        /// <summary>
        /// Takes stock of a session's finished boxes without announcing any of
        /// them - what a puzzle being dealt, resumed or restarted starts from.
        /// A resumed puzzle may arrive with six boxes already done, and the
        /// player has heard about all six.
        /// </summary>
        public void Follow(GameSession session)
        {
            _session = session;
            Refresh(false);
        }

        /// <summary>
        /// Recounts which boxes stand finished, announcing any that just became
        /// so when <paramref name="announce"/> is true.
        ///
        /// All nine are recounted on every board change rather than only the box
        /// that was touched, because undo can un-finish one - and nine passes
        /// over nine cells, a few times a second at most, costs less than the
        /// bookkeeping needed to avoid it.
        /// </summary>
        public void Refresh(bool announce)
        {
            if (_session == null) return;

            for (var box = 0; box < BoardBoxes.Count; box++)
            {
                var complete = BoardBoxes.IsComplete(_session, box);
                if (complete && !_complete[box] && announce) _onCompleted(box);

                _complete[box] = complete;
            }
        }
    }
}
