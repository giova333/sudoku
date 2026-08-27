using UnityEngine;
using UnityEngine.Audio;

namespace Sudoku.Game.Audio
{
    /// <summary>
    /// The one voice of the game: eight effects out of a mixer, and the two
    /// mutes that govern them.
    ///
    /// Everything is routed through an AudioMixer rather than played on bare
    /// sources, because a mixer is where a mute can be a single number instead
    /// of a flag every call site has to remember to check - and because the
    /// group that music would one day play through has to exist before there is
    /// any music, or adding it becomes a rewrite. There is no music today and
    /// none ships: most Sudoku players mute it before the first puzzle is out.
    ///
    /// The iOS silent switch is respected by the audio session Unity installs
    /// for us. With iOS "Mute Other Audio Sources" left off, the player builds
    /// against AVAudioSessionCategoryAmbient, which both mixes with whatever
    /// the player was already listening to and goes quiet when the ringer
    /// switch is flipped. Nothing here overrides that, and nothing should:
    /// asking for Playback would make the game the one app that ignores the
    /// switch.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        /// <summary>Where the mixer and the clips are loaded from.</summary>
        const string ResourceRoot = "Audio/";

        /// <summary>The exposed mixer parameter each mute writes to, in decibels.</summary>
        const string SfxVolumeParameter = "SfxVolume";

        /// <summary>Full and silent, in the decibels the mixer speaks.</summary>
        const float Audible = 0f;
        const float Silent = -80f;

        static readonly Sfx[] AllEffects =
        {
            Sfx.Place, Sfx.Erase, Sfx.Error, Sfx.Hint,
            Sfx.BoxComplete, Sfx.PuzzleComplete, Sfx.ButtonTap, Sfx.HeartLost
        };

        AudioMixer _mixer;
        AudioSource _source;
        AudioClip[] _clips;

        bool _soundEnabled = true;
        bool _hapticsEnabled = true;

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set
            {
                _soundEnabled = value;
                ApplyMute();
            }
        }

        public bool HapticsEnabled
        {
            get => _hapticsEnabled;
            set => _hapticsEnabled = value;
        }

        /// <summary>
        /// Builds the service under the composition root. It carries its own
        /// listener check for the same reason the root carries an event-system
        /// check: the bootstrap installs itself into whatever scene is open,
        /// and a scene with no listener is silent with no error to explain it.
        /// </summary>
        public static AudioService Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);

            var service = go.AddComponent<AudioService>();
            service.Build();
            return service;
        }

        void Build()
        {
            EnsureListener();

            _mixer = Resources.Load<AudioMixer>(ResourceRoot + "Sudoku");

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            // Effects belong to the interface, not to a place in the world.
            _source.spatialBlend = 0f;

            if (_mixer != null)
            {
                var groups = _mixer.FindMatchingGroups("SFX");
                if (groups != null && groups.Length > 0)
                    _source.outputAudioMixerGroup = groups[0];
            }

            _clips = new AudioClip[AllEffects.Length];
            foreach (var effect in AllEffects)
                _clips[(int)effect] = Resources.Load<AudioClip>(ResourceRoot + FileNameOf(effect));

            ApplyMute();
            Haptics.Prepare();
        }

        /// <summary>
        /// One shot on one source. PlayOneShot mixes rather than interrupts, so
        /// a fast player hears every tap instead of each one cutting off the
        /// last, and no pool of sources is needed to get that.
        /// </summary>
        public void Play(Sfx effect)
        {
            if (!_soundEnabled || _source == null) return;

            var clip = ClipFor(effect);
            if (clip == null) return;

            _source.PlayOneShot(clip);
        }

        public void Impact(Haptic strength)
        {
            if (!_hapticsEnabled) return;
            Haptics.Impact(strength);
        }

        AudioClip ClipFor(Sfx effect)
        {
            var index = (int)effect;
            if (_clips == null || index < 0 || index >= _clips.Length) return null;
            return _clips[index];
        }

        /// <summary>
        /// The mute is written to the mixer so that anything already sounding
        /// goes quiet with it, and gated again in <see cref="Play"/> so the
        /// mute still holds if the mixer asset is ever missing. Two guards for
        /// one switch is cheap; a game that keeps making noise after the player
        /// muted it is not.
        /// </summary>
        void ApplyMute()
        {
            if (_mixer == null) return;
            _mixer.SetFloat(SfxVolumeParameter, _soundEnabled ? Audible : Silent);
        }

        /// <summary>Clip file names are lower-kebab-case; the enum is not.</summary>
        static string FileNameOf(Sfx effect)
        {
            switch (effect)
            {
                case Sfx.BoxComplete: return "box-complete";
                case Sfx.PuzzleComplete: return "puzzle-complete";
                case Sfx.ButtonTap: return "button-tap";
                case Sfx.HeartLost: return "heart-lost";
                default: return effect.ToString().ToLowerInvariant();
            }
        }

        static void EnsureListener()
        {
            if (FindFirstObjectByType<AudioListener>() != null) return;

            var go = new GameObject("AudioListener", typeof(AudioListener));
            DontDestroyOnLoad(go);
        }
    }
}
