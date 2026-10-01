using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Every sound in the game, driven by what the sim reports: guns by family, shields and hull
    // hits, losses, an engine bed that swells with the fleet, UI feedback, and music that follows
    // the situation (map, council, battle, a battle going badly, the end). Clips live in
    // Resources/Audio (all CC0, see CREDITS.md); a clip that is missing is simply silent.
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio I { get; private set; }

        public enum Cue { None, Map, Council, Battle, BattleHot, Victory, Defeat }

        const int Voices = 40;
        const float MaxDistance = 14000f;

        readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, float> _last = new Dictionary<string, float>();
        readonly List<AudioSource> _voices = new List<AudioSource>();
        int _next;
        AudioSource _ui, _bed;
        AudioSource[] _music = new AudioSource[2];
        int _live;
        Cue _cue = Cue.None;
        bool _loop = true;

        SimRunner _runner;
        CampaignDirector _director;
        float _find;
        int _firedThisFrame, _frame;

        public float MusicVolume = 0.5f, SfxVolume = 0.8f;
        bool _musicOn = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a.StartsWith("-grCapture") || a.StartsWith("-grBench")) return;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            go.AddComponent<GameAudio>();
        }

        void Awake()
        {
            I = this;
            Load("Audio/Sfx/Weapons");
            Load("Audio/Sfx/Impacts");
            Load("Audio/Sfx/Engines");
            Load("Audio/Sfx/Ship");
            Load("Audio/Ui");
            foreach (var c in Resources.LoadAll<AudioClip>("Audio/Music")) _clips[c.name] = new[] { c };
            _musicOn = PlayerPrefs.GetInt("music", 1) == 1;

            for (int i = 0; i < Voices; i++)
            {
                var s = NewSource("Voice", true);
                _voices.Add(s);
            }
            _ui = NewSource("Ui", false);
            _bed = NewSource("Bed", false);
            _bed.loop = true;
            _bed.volume = 0f;
            for (int i = 0; i < 2; i++)
            {
                _music[i] = NewSource("Music" + i, false);
                _music[i].volume = 0f;
            }
        }

        static Camera Cam() => Camera.main != null ? Camera.main : Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null;

        AudioSource NewSource(string name, bool spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = spatial ? 0.9f : 0f;
            if (spatial)
            {
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 700f;
                s.maxDistance = MaxDistance;
                s.dopplerLevel = 0f;
            }
            return s;
        }

        // Clips named like "laserSmall_003" are a family "laserSmall" with variations.
        void Load(string path)
        {
            var groups = new Dictionary<string, List<AudioClip>>();
            foreach (var c in Resources.LoadAll<AudioClip>(path))
            {
                int i = c.name.LastIndexOf('_');
                string family = i > 0 ? c.name.Substring(0, i) : c.name;
                if (!groups.TryGetValue(family, out var list)) groups[family] = list = new List<AudioClip>();
                list.Add(c);
            }
            foreach (var kv in groups) _clips[kv.Key] = kv.Value.ToArray();
        }

        AudioClip Pick(string family) => _clips.TryGetValue(family, out var arr) && arr.Length > 0 ? arr[Random.Range(0, arr.Length)] : null;

        // --- One-shots --------------------------------------------------------------------------

        bool Throttle(string key, float gap)
        {
            float now = Time.unscaledTime;
            if (_last.TryGetValue(key, out float t) && now - t < gap) return false;
            _last[key] = now;
            return true;
        }

        void Play(string family, Vector3 at, float volume, float pitch = 1f)
        {
            var clip = Pick(family);
            if (clip == null) return;
            var cam = Cam();
            if (cam != null)
            {
                float d = Vector3.Distance(cam.transform.position, at);
                if (d > MaxDistance) return;
            }
            var s = _voices[_next];
            _next = (_next + 1) % _voices.Count;
            s.transform.position = at;
            s.pitch = pitch * Random.Range(0.94f, 1.06f);
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * SfxVolume;
            s.Play();
        }

        public static void Ui(string family, float volume = 0.55f)
        {
            if (I == null) return;
            var clip = I.Pick(family);
            if (clip == null || !I.Throttle("ui_" + family, 0.04f)) return;
            I._ui.PlayOneShot(clip, volume * I.SfxVolume);
        }

        // A sim event, as the presenter sees it.
        public static void OnEvent(in SimEvent e, World w)
        {
            if (I == null) return;
            I.Handle(e, w);
        }

        void Handle(in SimEvent e, World w)
        {
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                _firedThisFrame = 0;
            }
            Vector3 at = e.Position.ToUnity();
            switch (e.Kind)
            {
                case SimEventKind.Fired:
                {
                    var ship = w.ShipById(e.Ship);
                    if (ship == null || e.Mount >= ship.Mounts.Count || _firedThisFrame >= 6) return;
                    var def = ship.Mounts[e.Mount].Def;
                    string key = "fire" + def.Family;
                    if (!Throttle(key + e.Ship, 0.07f)) return;
                    _firedThisFrame++;
                    bool big = def.Size == MountSize.Large, small = def.Size == MountSize.Small;
                    if (def.Class == MountClass.PointDefence) Play("laserSmall", at, 0.14f, 1.9f);
                    else if (def.Channel == DamageChannel.Energy) Play(big ? "laserLarge" : small ? "laserSmall" : "laserRetro", at, big ? 0.55f : 0.4f, big ? 0.9f : 1.1f);
                    else if (def.Channel == DamageChannel.Disruptive) Play("forceField", at, 0.4f, 1.7f);
                    else if (big) Play("lowFrequency_explosion", at, 0.5f, 1.6f);
                    else if (small) Play("impactMetal_light", at, 0.3f, 1.2f);
                    else Play("impactPlate_heavy", at, 0.45f, 0.85f);
                    break;
                }
                case SimEventKind.ShieldHit:
                    if (Throttle("shield" + e.Ship, 0.12f)) Play("forceField", at, Mathf.Clamp(e.Value / 200f, 0.12f, 0.5f), 1.15f);
                    break;
                case SimEventKind.ShieldsDown:
                    Play("forceField", at, 0.8f, 0.55f);
                    break;
                case SimEventKind.HullHit:
                    if (Throttle("hull" + e.Ship, 0.09f))
                        Play(e.Value > 120f ? "impactMetal_heavy" : e.Value > 40f ? "impactMetal_medium" : "impactMetal_light", at, Mathf.Clamp(0.25f + e.Value / 400f, 0.25f, 0.7f));
                    break;
                case SimEventKind.MountDestroyed:
                    Play("explosionCrunch", at, 0.6f, 1.2f);
                    break;
                case SimEventKind.ShipDestroyed:
                {
                    var ship = w.ShipById(e.Ship);
                    float size = ship != null ? Mathf.Clamp(ship.Def.Radius / 25f, 0.5f, 1.4f) : 0.8f;
                    Play("lowFrequency_explosion", at, 1f, 0.8f / size);
                    Play("explosionCrunch", at, 0.9f, 0.7f / size);
                    break;
                }
                case SimEventKind.AbilityUsed:
                    Play("thrusterFire", at, 0.5f, 0.9f);
                    break;
                case SimEventKind.RockHit:
                    if (Throttle("rock", 0.1f)) Play("impactPlate_light", at, 0.25f, 0.8f);
                    break;
                case SimEventKind.ShipSpawned:
                    if (e.Value > 0f) Play("forceField", w.ShipById(e.Ship)?.Position.ToUnity() ?? at, 0.6f, 0.7f);
                    break;
            }
        }

        // --- Bed and music ----------------------------------------------------------------------

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame && !kb.ctrlKey.isPressed)
            {
                _musicOn = !_musicOn;
                PlayerPrefs.SetInt("music", _musicOn ? 1 : 0);
            }
            _find -= Time.unscaledDeltaTime;
            if (_find <= 0f)
            {
                _find = 1f;
                if (_runner == null) _runner = FindFirstObjectByType<SimRunner>();
                if (_director == null) _director = FindFirstObjectByType<CampaignDirector>();
                EnsureListener();
            }
            UpdateBed();
            Cue want = ChooseCue();
            if (want != _cue) SwitchTo(want);
            FadeMusic();
        }

        void EnsureListener()
        {
            if (FindFirstObjectByType<AudioListener>() != null) return;
            var cam = Cam();
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }

        // The fleet's engines as a low bed: louder with more ships under way near the camera.
        void UpdateBed()
        {
            float target = 0f;
            var w = _runner != null ? _runner.World : null;
            var cam = Cam();
            if (w != null && cam != null && !_runner.Paused)
            {
                int moving = 0;
                foreach (var s in w.Ships)
                {
                    if (!s.Alive || s.Speed < 5f) continue;
                    if ((s.Position.ToUnity() - cam.transform.position).sqrMagnitude < 9000f * 9000f) moving++;
                }
                target = Mathf.Clamp01(moving / 8f) * 0.22f;
            }
            if (target > 0.01f && !_bed.isPlaying)
            {
                var clip = Pick("spaceEngineLow");
                if (clip != null)
                {
                    _bed.clip = clip;
                    _bed.Play();
                }
            }
            _bed.volume = Mathf.MoveTowards(_bed.volume, target * SfxVolume, Time.unscaledDeltaTime * 0.3f);
            if (_bed.volume <= 0.001f && target <= 0.01f && _bed.isPlaying) _bed.Stop();
        }

        Cue ChooseCue()
        {
            if (_runner != null && _runner.World != null)
            {
                switch (_runner.State)
                {
                    case BattleState.Victory: return Cue.Victory;
                    case BattleState.Defeat: return Cue.Defeat;
                    case BattleState.Withdrawn: return Cue.Map;
                }
                // Hot once we have lost a fifth of what we brought, or are outnumbered.
                int own = 0, foe = 0, all = 0;
                foreach (var s in _runner.World.Ships)
                {
                    if (s.Def.MaxSpeed <= 0f) continue;
                    all++;
                    if (!s.Alive) continue;
                    if (s.Side == 0) own++; else foe++;
                }
                return own < foe || own * 5 < all * 4 && own < all ? Cue.BattleHot : Cue.Battle;
            }
            if (_director != null && _director.Campaign != null && _director.Campaign.Council != null) return Cue.Council;
            return Cue.Map;
        }

        static string TrackFor(Cue c) => c switch
        {
            Cue.Map => "sector",
            Cue.Council => "transmission",
            Cue.Battle => "pulse",
            Cue.BattleHot => "urgent",
            Cue.Victory => "victory",
            Cue.Defeat => "hover",
            _ => null,
        };

        void SwitchTo(Cue c)
        {
            _cue = c;
            string name = TrackFor(c);
            if (name == null || !_clips.TryGetValue(name, out var arr)) return;
            _live = 1 - _live;
            var s = _music[_live];
            s.clip = arr[0];
            _loop = c != Cue.Victory && c != Cue.Defeat;
            s.loop = _loop;
            s.volume = 0f;
            s.Play();
        }

        void FadeMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                float target = i == _live && _musicOn ? MusicVolume : 0f;
                _music[i].volume = Mathf.MoveTowards(_music[i].volume, target, Time.unscaledDeltaTime * 0.35f);
                if (i != _live && _music[i].volume <= 0.001f && _music[i].isPlaying) _music[i].Stop();
            }
        }
    }
}
