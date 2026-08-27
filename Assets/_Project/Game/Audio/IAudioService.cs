namespace Sudoku.Game.Audio
{
    /// <summary>
    /// Everything the game is allowed to know about how it sounds and feels.
    ///
    /// Sound and haptics are two mutes rather than one, because the reason a
    /// player silences a puzzle game - a meeting, a train, a sleeping child -
    /// is usually a reason to keep the phone buzzing quietly in their hand, or
    /// the exact opposite. Neither switch is allowed to imply the other.
    ///
    /// Both mutes are plain properties here: the service does not read
    /// preferences and the settings screen does not play sounds. The
    /// composition root observes <c>GameSettings</c> and writes them, which is
    /// also what makes them persist across launches.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Whether effects are audible. Persisted by the settings service, not by this one.</summary>
        bool SoundEnabled { get; set; }

        /// <summary>Whether the phone is allowed to knock.</summary>
        bool HapticsEnabled { get; set; }

        /// <summary>Fires one effect. Silently does nothing when muted, or when the clip is missing.</summary>
        void Play(Sfx effect);

        /// <summary>Knocks once. Silently does nothing when muted, or off a device with no haptics.</summary>
        void Impact(Haptic strength);
    }
}
