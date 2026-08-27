using System;
using System.IO;
using System.Threading;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Persistence;
using UnityEngine;

namespace Sudoku.Game.Save
{
    /// <summary>
    /// The save file: where it lives, when it is written, and how a half-written
    /// one is made impossible. Everything about what is in it belongs to
    /// <see cref="SaveSerializer"/> in Core - this class knows only bytes.
    ///
    /// Routine writes go to a pool thread because autosave fires after every
    /// move and a phone's storage is not fast. Pause and focus loss take the
    /// synchronous path instead: the process may never be scheduled again.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "sudoku-save.json";

        readonly string _directory;
        readonly string _filePath;
        readonly string _tempPath;

        readonly object _queueGate = new object();
        readonly object _fileGate = new object();

        string _pending;
        bool _draining;

        public SaveStore() : this(Application.persistentDataPath) { }

        /// <summary>
        /// Takes the directory explicitly so a test or a tool can point the
        /// store somewhere other than the platform's persistent data path.
        /// </summary>
        public SaveStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _filePath = Path.Combine(_directory, FileName);
            _tempPath = _filePath + ".tmp";

            Load();
        }

        /// <summary>Everything the game remembers. Only ever touched on the main thread.</summary>
        public SaveData Data { get; private set; }

        public string FilePath => _filePath;

        /// <summary>
        /// Re-reads the file from disk. A payload this build cannot understand
        /// is set aside rather than deleted - it is the only copy of the
        /// player's progress, and a later build may still make sense of it.
        /// </summary>
        public void Load()
        {
            try
            {
                Data = File.Exists(_filePath)
                    ? SaveSerializer.Read(File.ReadAllText(_filePath))
                    : new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[sudoku] the save at {_filePath} could not be read ({e.Message}); " +
                                 "starting fresh and keeping the old file.");
                Quarantine();
                Data = new SaveData();
            }
        }

        /// <summary>The in-progress puzzle for a difficulty, or null when there is none waiting.</summary>
        public SaveSlot Slot(DifficultyTier tier) => Data.SlotFor(tier);

        /// <summary>
        /// The in-progress daily puzzle, but only when it is the one for
        /// <paramref name="date"/> - yesterday's daily is not resumable.
        /// </summary>
        public SaveSlot DailySlot(DateTime date)
        {
            var slot = Data.DailySlot;
            return slot != null && slot.DateKey == SaveSlot.DateKeyFor(date) ? slot : null;
        }

        /// <summary>
        /// Stores a slot and schedules a write. This is the autosave call: it
        /// stamps the slot so a Continue button knows which puzzle is newest.
        /// </summary>
        public void Put(SaveSlot slot)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));

            slot.SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Data.PutSlot(slot);
            Schedule();
        }

        /// <summary>Forgets one slot - what finishing or abandoning a puzzle does.</summary>
        public void Clear(string slotId)
        {
            if (Data.ClearSlot(slotId))
                Schedule();
        }

        public void Clear(DifficultyTier tier) => Clear(SaveSlot.IdFor(tier));

        /// <summary>
        /// Schedules a write after a change made directly to <see cref="Data"/>,
        /// such as bank progress moving on.
        /// </summary>
        public void Touch() => Schedule();

        /// <summary>
        /// Writes now, on the calling thread, and does not return until the file
        /// is on disk. For application pause and focus loss, where there may be
        /// no later.
        /// </summary>
        public void Flush()
        {
            // Serialized before taking the file, because reading Data belongs to
            // the main thread and the file may be busy with a queued write.
            var payload = SaveSerializer.Write(Data);

            lock (_fileGate)
            {
                // Anything queued is by definition older than what was just
                // serialized, so dropping it is what keeps the newest write last.
                lock (_queueGate)
                    _pending = null;

                WriteFile(payload);
            }
        }

        void Schedule()
        {
            // Serialized here rather than on the writer thread: the object graph
            // belongs to the main thread, and a string cannot be raced.
            var payload = SaveSerializer.Write(Data);

            lock (_queueGate)
            {
                _pending = payload;
                if (_draining)
                    return;

                _draining = true;
            }

            ThreadPool.QueueUserWorkItem(Drain);
        }

        void Drain(object state)
        {
            while (true)
            {
                // The file is taken before the payload, so a flush can never
                // slip a newer write in between the two and be overwritten by
                // the older one this thread was already holding.
                lock (_fileGate)
                {
                    string payload;
                    lock (_queueGate)
                    {
                        payload = _pending;
                        _pending = null;

                        if (payload == null)
                        {
                            _draining = false;
                            return;
                        }
                    }

                    WriteFile(payload);
                }
            }
        }

        /// <summary>Callers hold <c>_fileGate</c>: only one write is ever in flight.</summary>
        void WriteFile(string payload)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(_tempPath, payload);
                Commit();
            }
            catch (Exception e)
            {
                Debug.LogError($"[sudoku] could not write the save at {_filePath}: {e.Message}");
            }
        }

        /// <summary>
        /// The rename is what makes the write atomic: the old file stays intact
        /// until a complete new one exists to take its place.
        /// </summary>
        void Commit()
        {
            if (!File.Exists(_filePath))
            {
                File.Move(_tempPath, _filePath);
                return;
            }

            try
            {
                File.Replace(_tempPath, _filePath, null);
            }
            catch (Exception)
            {
                // Not every mobile file system supports an atomic replace.
                // Delete-then-move leaves a far smaller window than writing in
                // place would.
                File.Delete(_filePath);
                File.Move(_tempPath, _filePath);
            }
        }

        void Quarantine()
        {
            try
            {
                File.Copy(_filePath, _filePath + ".unreadable", true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[sudoku] could not set aside the unreadable save: {e.Message}");
            }
        }
    }
}
