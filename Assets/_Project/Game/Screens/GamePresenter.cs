using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Game.Audio;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Save;
using Sudoku.Game.Settings;
using UnityEngine;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// Turns taps into player intent on the session, and the session's state
    /// back into pixels. It holds no rules of its own - every decision about
    /// what a move means lives in Sudoku.Core.
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour, IScreen
    {
        PuzzleLibrary _library;
        SaveStore _saves;
        GameSettings _settings;
        RectTransform _root;
        BoardView _board;
        NumpadView _numpad;
        HudView _hud;
        IAudioService _audio;
        GameAudio _sounds;
        BoardMotion _motion;

        GameSession _session;
        SaveSlot _slot;
        Puzzle _puzzle;
        RulesConfig _rules;
        DifficultyTier _tier = DifficultyTier.Easy;

        int _selected = -1;
        bool _notesMode;

        /// <summary>
        /// The puzzle was solved. Carries everything the results card shows, so
        /// the card needs no reference to a session that is about to be put
        /// down. What happens next - the completion flow, and the interstitial
        /// seam inside it - is the composition root's decision, not gameplay's.
        /// </summary>
        public Action<PuzzleResult> Solved;

        /// <summary>
        /// The last heart went. Carries the tier and the run's mistake count,
        /// which is all the out-of-hearts screen shows.
        /// </summary>
        public Action<DifficultyTier, int> OutOfHearts;

        public RectTransform Root => _root;

        /// <summary>
        /// Raised as a session is dealt, before it announces anything, so a
        /// listener attached here hears the whole play-through from its first
        /// event. Analytics (#13) is the caller; the presenter neither knows nor
        /// cares who takes it, which is what keeps analytics out of gameplay.
        ///
        /// Every path that builds a session raises it - including the one that
        /// restores a saved puzzle purely to abandon it, because a drop-off
        /// nobody hears about is the one event this stream exists for.
        /// </summary>
        public event Action<GameSession> SessionStarted;

        /// <summary>The difficulty being played. Read by anything that reports on
        /// the puzzle in hand.</summary>
        public DifficultyTier Tier => _tier;

        /// <summary>
        /// Which puzzle is in play, as its bank and index. Null before the first
        /// deal. The bank reference is bookkeeping rather than truth (a re-bake
        /// shifts every index), which is exactly the granularity analytics
        /// wants: it names the puzzle within the content that produced it.
        /// </summary>
        public string PuzzleId =>
            _slot == null ? null : _slot.BankName + "#" + _slot.BankIndex;

        /// <summary>
        /// Whether the session in hand is still playable. Leaving the game
        /// screen suspends it rather than ending it, so coming back to the same
        /// puzzle costs nothing - but what Home offers to continue is read from
        /// the save file, not from here: after a cold start there is a puzzle
        /// waiting and no session to say so.
        /// </summary>
        public bool HasSession => _session != null && _session.Status == SessionStatus.InProgress;

        /// <summary>
        /// Whether the game screen is the one on show. The clock must not run -
        /// and returning to the app must not restart it - on a session the
        /// player has left behind on Home.
        /// </summary>
        bool IsVisible => _root != null && _root.gameObject.activeInHierarchy;

        public void Initialise(PuzzleLibrary library, SaveStore saves, GameSettings settings,
            RectTransform root, BoardView board, NumpadView numpad, HudView hud,
            IAudioService audio)
        {
            _library = library;
            _saves = saves;
            _settings = settings;
            _root = root;
            _board = board;
            _numpad = numpad;
            _hud = hud;
            _audio = audio;

            // Almost everything audible is decided from the session's own event
            // stream rather than from here, so the presenter only has to hand
            // over each session it deals.
            if (_audio != null) _sounds = new GameAudio(_audio);

            // The board moves off the same stream, for the same reason, and
            // needs no wiring from outside: how much it moves is the motion
            // layer's business, and a player who has asked for less of it has
            // already been answered before this is built.
            _motion = new BoardMotion(_board);

            _board.CellTapped += OnCellTapped;
            _numpad.DigitTapped += OnDigitTapped;
            _numpad.DigitHeld += OnDigitHeld;
            _numpad.ActionTapped += OnAction;

            _settings.Changed += OnSettingChanged;
        }

        /// <summary>
        /// Deals a puzzle of the given tier - or hands back the half-finished
        /// one already saved under it. The difficulty-select screen calls this
        /// on the way in; nothing starts a puzzle on launch, because launching
        /// lands on Home.
        /// </summary>
        public void StartPuzzle(DifficultyTier tier)
        {
            // A half-finished puzzle of this difficulty outranks a fresh one:
            // starting a quick Easy game must never eat a stalled Expert.
            var waiting = _saves != null ? _saves.Slot(tier) : null;
            if (waiting != null && waiting.CanResume)
            {
                Resume(waiting);
                return;
            }

            Deal(tier);
        }

        /// <summary>
        /// Puts the player back in front of one particular saved puzzle - what
        /// Home's Continue does. It takes the slot rather than a tier because
        /// the save file is what offers it: after a cold start there is no
        /// session in memory to ask, and the newest slot is not necessarily the
        /// one the player last chose a difficulty for.
        /// </summary>
        public void Resume(SaveSlot slot)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));

            // The session already in hand is this same puzzle, only never
            // older than the file - rebuilding it from the last write would
            // gain nothing and drop whatever has happened since.
            if (_slot != null && _slot.SlotId == slot.SlotId && HasSession) return;

            _tier = slot.Tier;
            _slot = slot;

            // A resumed puzzle keeps the rules it was dealt under: the mistake
            // limit is a snapshot taken at deal time, and a settings change
            // must not rewrite a game already being scored.
            if (_slot.Rules == null) _slot.Rules = _settings.BuildRules();
            _rules = _slot.Rules;

            // Auto-removal is the one rule that is not a snapshot, so a resumed
            // puzzle picks up whatever the toggle says now.
            _rules.AutoRemoveNotes = _settings.AutoRemoveNotes.Value;

            _puzzle = _slot.ToPuzzle();
            Adopt(_slot.ToSession());
        }

        /// <summary>
        /// Throws away whatever was waiting under this tier and deals a new
        /// puzzle over it. Only ever reached through a confirmation, because
        /// what it discards cannot be got back.
        /// </summary>
        public void StartFresh(DifficultyTier tier)
        {
            AbandonWaiting(tier);
            Deal(tier);
        }

        /// <summary>Hands the player a puzzle of this tier they have not been
        /// served before.</summary>
        void Deal(DifficultyTier tier)
        {
            _tier = tier;
            _rules = _settings.BuildRules();
            _puzzle = _library.Next(tier, out var bankIndex);
            _slot = SaveSlot.ForTier(tier, PuzzleLibrary.BankName(tier), bankIndex, _puzzle, _rules);
            Adopt(_slot.ToSession());
        }

        /// <summary>
        /// Takes a session on: listen to it, announce it, start it, forget
        /// whatever the last puzzle had selected, write it down and draw it.
        /// </summary>
        void Adopt(GameSession session)
        {
            _session = session;
            _session.Emitted += OnGameEvent;

            // Attached before Start for the same reason as the announcement
            // below: the stream is where nearly every sound is decided, so a
            // listener that joins late misses the puzzle's own opening event.
            // This is the only path a played session travels - the one in
            // AbandonWaiting exists purely to say it was dropped, and is
            // deliberately left silent.
            if (_sounds != null) _sounds.Follow(_session);
            _motion.Follow(_session);

            // Announced before Start, so whoever is listening hears the puzzle
            // begin rather than joining it a move late.
            SessionStarted?.Invoke(_session);

            // Idempotent, and a restored session carries the fact that it has
            // already started - so a resume is never counted as a second start.
            _session.Start();

            _selected = -1;
            _notesMode = false;
            Save();
            Render();
        }

        /// <summary>
        /// Says out loud that the puzzle waiting under this tier is being
        /// thrown away, and then forgets it.
        ///
        /// The announcement goes out on the session's own event stream rather
        /// than a channel of its own, so a drop-off carries the same counters -
        /// the clock and how much of the board was filled - as every other
        /// event a listener sees. When there is no session in memory to speak
        /// for the puzzle, the saved one is restored purely so that it can.
        ///
        /// Leaving a puzzle for another difficulty is not this: that one is
        /// still sitting under its own tier waiting to be continued, and
        /// counting it as abandoned would make the drop-off numbers a measure
        /// of tier-hopping instead of frustration.
        /// </summary>
        void AbandonWaiting(DifficultyTier tier)
        {
            var isCurrent = _slot != null && _slot.SlotId == SaveSlot.IdFor(tier);
            if (isCurrent && HasSession)
            {
                _session.Abandon();
            }
            else
            {
                var waiting = _saves != null ? _saves.Slot(tier) : null;
                if (waiting == null || !waiting.CanResume) return;

                // The reported tier and puzzle are read off this presenter, so
                // they have to name the puzzle being dropped rather than
                // whatever was last played - otherwise a drop-off from Easy is
                // filed against the Expert game the player wandered off from,
                // which is worse than not reporting it at all. StartFresh deals
                // over both a line later, so nothing outlives the announcement.
                _tier = tier;
                _slot = waiting;

                var abandoned = waiting.ToSession();
                abandoned.Emitted += OnGameEvent;

                // Announced like any other session: a puzzle restored only to
                // be thrown away still has to be heard by whoever is counting
                // drop-offs, which is the whole point of the event.
                SessionStarted?.Invoke(abandoned);
                abandoned.Abandon();
            }

            if (_saves != null) _saves.Clear(tier);
        }

        /// <summary>
        /// Plays the same puzzle again from its clues. What "from the
        /// beginning" means to the board, the clock and every counter is the
        /// session's business, so this only forgets what the player was
        /// pointing at, writes the reset state down and redraws.
        /// </summary>
        public void Restart()
        {
            if (_session == null) return;

            _session.Restart();
            _selected = -1;
            _notesMode = false;
            Save();
            Render();
        }

        /// <summary>
        /// Autosave. It fires after every committed move because a mobile
        /// process is killed without warning - there is no later to write in.
        /// </summary>
        void Save()
        {
            if (_saves == null || _slot == null || _session == null) return;

            _slot.Session = _session.Capture();
            _saves.Put(_slot);
        }

        /// <summary>Leaving for another screen suspends the clock; coming back
        /// restarts it on the same session.</summary>
        public void OnShow()
        {
            if (_session != null) _session.Resume();
        }

        public void OnHide()
        {
            if (_session == null) return;

            _session.Pause();
            Save();
        }

        void Update()
        {
            if (_session == null || !IsVisible) return;

            _session.Tick(Time.unscaledDeltaTime);
            _hud.Render(_session, _tier, _settings.TimerVisible.Value);
        }

        void OnApplicationPause(bool paused)
        {
            if (_session == null) return;

            if (!paused)
            {
                // Only the clock is gated on visibility: a session the player
                // left behind on Home must not start ticking again just
                // because the app came back.
                if (IsVisible) _session.Resume();
                return;
            }

            // The save is not gated on anything. The process may never be
            // scheduled again, so this write cannot wait for a background
            // thread - and a suspended session sitting behind Home is still
            // the player's puzzle, so a hidden screen is no reason to skip it.
            _session.Pause();
            Save();
            Flush();
        }

        void OnApplicationFocus(bool focused)
        {
            if (_session == null) return;

            if (focused)
            {
                if (IsVisible) _session.Resume();
                return;
            }

            // Unconditional, for the same reason as above.
            _session.Pause();
            Save();
            Flush();
        }

        /// <summary>
        /// Puts the autosave on disk before returning. Only for pause and focus
        /// loss, where there may be no later.
        /// </summary>
        void Flush()
        {
            if (_saves != null) _saves.Flush();
        }

        void OnCellTapped(int index)
        {
            // Looking somewhere else answers "no" to a revealed hint, and a
            // hint that is never taken is never spent.
            _session.CancelHint();

            _selected = index;
            Render();
        }

        void OnDigitTapped(int digit)
        {
            if (_selected < 0) return;

            if (_notesMode)
            {
                _session.ToggleNote(_selected, digit);
            }
            else if (!_session.Place(_selected, digit) && _puzzle.IsGiven(_selected))
            {
                // The one refusal the player can provoke on purpose and would
                // otherwise get no answer to. Every other reason Place says no -
                // the same digit twice, a run already over - is either invisible
                // or already on screen.
                _motion.Refused(_selected);
            }

            Save();
            Render();
        }

        void OnDigitHeld(int digit)
        {
            if (_selected < 0) return;
            _session.ToggleNote(_selected, digit);
            Save();
            Render();
        }

        void OnAction(PadAction action)
        {
            // Every other button is the player changing the subject, which
            // drops a revealed hint without charging for it.
            if (action != PadAction.Hint)
                _session.CancelHint();

            switch (action)
            {
                case PadAction.Undo:
                    _session.Undo();
                    break;
                case PadAction.Erase:
                    // Erasing is the one move the session does not announce, so
                    // it is the one move the presenter has to give a voice to.
                    if (_selected >= 0 && _session.Erase(_selected)) Play(Sfx.Erase);
                    break;
                case PadAction.Notes:
                    _notesMode = !_notesMode;
                    Play(Sfx.ButtonTap);
                    break;
                case PadAction.Hint:
                    TapHint();
                    break;
            }

            Save();
            Render();
        }

        /// <summary>
        /// The hint button is one button doing two jobs: the first tap shows
        /// the cell and the reasoning for free, the second takes it. The
        /// session owns which of the two the next tap is, so the two halves
        /// can never disagree about the cell.
        /// </summary>
        void TapHint()
        {
            if (_session.PendingHint != null)
            {
                _session.TakeHint();
                return;
            }

            var revealed = _session.RevealHint(_selected);
            if (revealed == null) return;

            _selected = revealed.CellIndex;

            // Revealing is free, so the session says nothing about it. The tap
            // still has to be answered, or half the gesture is silent.
            Play(Sfx.ButtonTap);
        }

        /// <summary>
        /// Stage one of the completion flow: the sweep across the finished
        /// board, calling back when it has crossed. The composition root hands
        /// this to <see cref="Sudoku.Game.Session.CompletionFlow.BoardCascade"/>
        /// - the presenter neither runs the flow nor knows what comes after it.
        /// </summary>
        public void Cascade(Action done) => _motion.Cascade(done);

        /// <summary>Plays an effect the event stream cannot announce for us.</summary>
        void Play(Sfx effect)
        {
            if (_audio != null) _audio.Play(effect);
        }

        void Render()
        {
            _board.Render(_session, _puzzle, _selected, _settings.HighlightMistakes.Value);
            _numpad.Render(_session, _notesMode);
            _hud.Render(_session, _tier, _settings.TimerVisible.Value);
        }

        /// <summary>
        /// A preference change reaches the puzzle already in play, because a
        /// toggle that appears to do nothing reads as a broken toggle.
        ///
        /// The one exception is the mistake limit, which <see cref="GameSettings.BuildRules"/>
        /// snapshots at deal time - the settings screen says so rather than
        /// leaving the player to wonder.
        /// </summary>
        void OnSettingChanged(IPreference preference)
        {
            if (_session == null) return;

            // The session reads auto-removal from the rules object it was dealt
            // on every placement, so writing to that same object is what puts
            // the change into the puzzle in hand. It is the slot's rules object
            // too, so the change is saved with the puzzle rather than lost.
            _rules.AutoRemoveNotes = _settings.AutoRemoveNotes.Value;
            Save();

            Render();
        }

        /// <summary>
        /// The two events that end a run. Everything else on the stream is
        /// nobody's business here: reporting is a listener's job, and #13's
        /// analytics takes the same stream through <see cref="SessionStarted"/>
        /// rather than through this handler, so neither side can double-report
        /// or quietly drop the other's work.
        /// </summary>
        void OnGameEvent(GameEvent e)
        {
            switch (e.Kind)
            {
                case GameEventKind.PuzzleCompleted:
                    OnCompleted(e);
                    break;
                case GameEventKind.HeartsDepleted:
                    OnHeartsDepleted(e);
                    break;
            }
        }

        /// <summary>
        /// The puzzle is finished, so the slot holding it is finished with:
        /// dropping it is what stops Continue offering a solved board, and
        /// forgetting it here rather than in the save layer is what keeps the
        /// autosave that runs a moment later from writing it straight back.
        ///
        /// The record is counted before the card is built, so the card shows the
        /// best time including this solve - a new record and the number that set
        /// it are then the same number.
        /// </summary>
        void OnCompleted(GameEvent e)
        {
            _slot = null;

            var best = 0f;
            var isNewBest = false;

            if (_saves != null)
            {
                _saves.Clear(_tier);
                isNewBest = _saves.Data.RecordBestTime(_tier, e.ElapsedSeconds);
                best = _saves.Data.BestTimeFor(_tier).Seconds;
                _saves.Touch();
            }

            Solved?.Invoke(new PuzzleResult(_tier, e.ElapsedSeconds, e.MistakeCount,
                e.HintsUsed, best, isNewBest));
        }

        /// <summary>
        /// The last heart is gone. The puzzle stays in its slot so that starting
        /// it over is one tap away; a slot whose session failed is not
        /// resumable, so Continue does not offer it either.
        /// </summary>
        void OnHeartsDepleted(GameEvent e)
        {
            Save();
            OutOfHearts?.Invoke(_tier, e.MistakeCount);
        }

        /// <summary>
        /// Whether more hearts can be had right now, asked of the service the
        /// session spends from rather than of a second one that could disagree
        /// with it. False for the whole of this milestone.
        /// </summary>
        public bool CanRefillHearts =>
            _session != null && _session.Consumables.CanRefill(Consumable.Heart);

        /// <summary>
        /// Carries on a run that ended out of hearts, if and only if the
        /// consumable service supplies them. Returns false today; the day it
        /// returns true, nothing here changes.
        /// </summary>
        public bool ContinueWithMoreHearts()
        {
            if (_session == null || !_session.ContinueWithMoreHearts())
                return false;

            Save();
            Render();
            return true;
        }
    }
}
