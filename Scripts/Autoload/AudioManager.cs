using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Global audio manager with procedural music synthesis.
/// BGM uses layered chord pads, bass drones, and arpeggios built from
/// musical scales — no external audio files needed.
/// SFX uses short tonal beeps generated via AudioStreamGenerator.
/// </summary>
public partial class AudioManager : Node
{
    private static AudioManager? _instance;

    // BGM
    private AudioStreamPlayer? _bgmPlayer;
    private float _bgmTime;
    private BgmPreset _currentPreset;
    private int _currentBgmIdx = -1;

    // SFX pool
    private readonly List<AudioStreamPlayer> _sfxPool = new();
    private int _sfxIdx;
    private const int SfxPoolSize = 8;

    // Volume
    private float _masterVol = 1f;
    private float _musicVol = 1f;

    // ═══════════════════════════════════════════════════════
    // BGM Presets — musical patterns per scene
    // ═══════════════════════════════════════════════════════

    private struct BgmPreset
    {
        public float[] Scale;        // frequencies of scale notes (Hz)
        public float[] BassNotes;    // bass drone note indices into Scale
        public int[] ChordShape;     // offsets from bass for chord pad (e.g. {0,2,4} = triad)
        public float BassInterval;   // how often bass changes (seconds)
        public float ArpSpeed;       // arpeggio note speed
        public int[] ArpPattern;     // arpeggio offsets
        public float Tempo;          // overall feel
    }

    // D minor pentatonic scale — dark fantasy feel
    private static readonly float[] DmPenta = { 146.83f, 174.61f, 196.00f, 220.00f, 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880.00f };
    // C phrygian — exotic dark
    private static readonly float[] CPhrygian = { 130.81f, 155.56f, 174.61f, 196.00f, 220.00f, 261.63f, 277.18f, 311.13f, 349.23f, 392.00f, 415.30f, 466.16f, 523.25f, 554.37f, 622.25f, 698.46f };

    private static readonly BgmPreset BgmMainMenu = new()
    {
        Scale = DmPenta, BassNotes = new float[] { 0, 3, 2, 4, 1, 3, 0, 5 },
        ChordShape = new[] { 0, 2, 4 }, BassInterval = 3.2f, ArpSpeed = 0.28f,
        ArpPattern = new[] { 0, 2, 4, 7, 4, 2 }, Tempo = 0.55f
    };
    private static readonly BgmPreset BgmMap = new()
    {
        Scale = DmPenta, BassNotes = new float[] { 0, 2, 4, 3, 1, 2, 0, 3 },
        ChordShape = new[] { 0, 3, 4 }, BassInterval = 4.0f, ArpSpeed = 0.35f,
        ArpPattern = new[] { 0, 3, 5, 7, 5, 3 }, Tempo = 0.45f
    };
    private static readonly BgmPreset BgmBattleNormal = new()
    {
        Scale = DmPenta, BassNotes = new float[] { 0, 5, 3, 4, 0, 5, 2, 3 },
        ChordShape = new[] { 0, 2, 4 }, BassInterval = 1.6f, ArpSpeed = 0.12f,
        ArpPattern = new[] { 0, 2, 4, 5, 7, 5, 4, 2 }, Tempo = 0.7f
    };
    private static readonly BgmPreset BgmBattleElite = new()
    {
        Scale = CPhrygian, BassNotes = new float[] { 0, 3, 6, 2, 0, 4, 5, 1 },
        ChordShape = new[] { 0, 2, 4 }, BassInterval = 1.2f, ArpSpeed = 0.09f,
        ArpPattern = new[] { 0, 3, 4, 7, 6, 4, 3, 0 }, Tempo = 0.85f
    };
    private static readonly BgmPreset BgmBattleBoss = new()
    {
        Scale = CPhrygian, BassNotes = new float[] { 0, 5, 2, 6, 0, 3, 5, 7 },
        ChordShape = new[] { 0, 2, 4 }, BassInterval = 0.9f, ArpSpeed = 0.07f,
        ArpPattern = new[] { 0, 4, 7, 6, 4, 7, 6, 3 }, Tempo = 1.0f
    };
    private static readonly BgmPreset BgmShop = new()
    {
        Scale = DmPenta, BassNotes = new float[] { 0, 3, 1, 4, 2, 3, 0, 1 },
        ChordShape = new[] { 0, 2, 3 }, BassInterval = 2.8f, ArpSpeed = 0.22f,
        ArpPattern = new[] { 0, 2, 4, 6, 4, 2 }, Tempo = 0.5f
    };
    private static readonly BgmPreset BgmEvent = new()
    {
        Scale = CPhrygian, BassNotes = new float[] { 0, 4, 1, 5, 2, 3, 0, 2 },
        ChordShape = new[] { 0, 3, 5 }, BassInterval = 3.5f, ArpSpeed = 0.40f,
        ArpPattern = new[] { 0, 3, 5, 8, 5, 3 }, Tempo = 0.4f
    };
    private static readonly BgmPreset BgmVictory = new()
    {
        Scale = DmPenta, BassNotes = new float[] { 0, 3, 5, 7, 4, 5, 0, 7 },
        ChordShape = new[] { 0, 2, 3, 4 }, BassInterval = 1.5f, ArpSpeed = 0.10f,
        ArpPattern = new[] { 0, 2, 4, 7, 9, 7, 4, 2 }, Tempo = 0.75f
    };
    private static readonly BgmPreset BgmDefeat = new()
    {
        Scale = CPhrygian, BassNotes = new float[] { 0, 1, 0, 2, 0, 1, 0, 0 },
        ChordShape = new[] { 0, 2 }, BassInterval = 4.5f, ArpSpeed = 0.55f,
        ArpPattern = new[] { 0, 2, 3, 2 }, Tempo = 0.3f
    };

    private static readonly Dictionary<string, BgmPreset> BgmPresets = new()
    {
        ["main_menu"] = BgmMainMenu, ["map"] = BgmMap,
        ["battle_normal"] = BgmBattleNormal, ["battle_elite"] = BgmBattleElite,
        ["battle_boss"] = BgmBattleBoss, ["shop"] = BgmShop,
        ["event"] = BgmEvent, ["victory"] = BgmVictory, ["defeat"] = BgmDefeat,
    };

    // ═══════════════════════════════════════════════════════
    // SFX definitions
    // ═══════════════════════════════════════════════════════

    private static readonly Dictionary<string, (float Hz, float Dur, float Vol)> SfxDefs = new()
    {
        ["card_play"] = (880f, 0.08f, 0.6f), ["card_draw"] = (660f, 0.06f, 0.5f),
        ["card_discard"] = (440f, 0.05f, 0.3f), ["attack_hit"] = (220f, 0.12f, 0.7f),
        ["attack_heavy"] = (150f, 0.18f, 0.8f), ["block_gain"] = (520f, 0.08f, 0.5f),
        ["damage_taken"] = (180f, 0.15f, 0.7f), ["enemy_death"] = (300f, 0.3f, 0.6f),
        ["buff_apply"] = (780f, 0.1f, 0.5f), ["potion_drink"] = (600f, 0.1f, 0.5f),
        ["gold_gain"] = (1040f, 0.06f, 0.6f), ["relic_pickup"] = (980f, 0.12f, 0.7f),
        ["ui_click"] = (700f, 0.04f, 0.4f), ["ui_hover"] = (560f, 0.03f, 0.3f),
        ["ui_error"] = (200f, 0.15f, 0.5f), ["turn_start"] = (500f, 0.1f, 0.5f),
        ["turn_end"] = (400f, 0.1f, 0.4f), ["victory_fanfare"] = (660f, 0.5f, 0.8f),
        ["defeat_sting"] = (150f, 0.6f, 0.7f), ["heal"] = (720f, 0.12f, 0.5f),
        ["card_cancel"] = (350f, 0.04f, 0.3f), ["card_grab"] = (800f, 0.05f, 0.4f),
        ["card_hover"] = (560f, 0.03f, 0.3f), ["ui_hint"] = (600f, 0.04f, 0.4f),
    };

    // ═══════════════════════════════════════════════════════
    // Init
    // ═══════════════════════════════════════════════════════

    public override void _Ready()
    {
        _instance = this;
        _bgmPlayer = new AudioStreamPlayer();
        AddChild(_bgmPlayer);
        for (var i = 0; i < SfxPoolSize; i++) { var p = new AudioStreamPlayer(); AddChild(p); _sfxPool.Add(p); }
        SyncVolume();
        AppSettings.Instance.SettingsChanged += SyncVolume;
    }

    public override void _ExitTree() => AppSettings.Instance.SettingsChanged -= SyncVolume;
    private void SyncVolume()
    {
        _masterVol = AppSettings.Instance.MasterVolumePercent / 100f;
        _musicVol = AppSettings.Instance.MusicVolumePercent / 100f;
    }

    // ═══════════════════════════════════════════════════════
    // BGM playback
    // ═══════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        if (_bgmPlayer == null || !_bgmPlayer.Playing) return;
        var pb = _bgmPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (pb == null) return;

        float dt = (float)delta;
        int frames = Mathf.CeilToInt(44100 * dt);
        float vol = _masterVol * _musicVol;

        for (int i = 0; i < frames && pb.CanPushBuffer(1); i++)
        {
            float t = _bgmTime;
            _bgmTime += 1f / 44100f;

            var p = _currentPreset;
            float[] s = p.Scale;
            int[] chord = p.ChordShape;
            int[] arp = p.ArpPattern;

            // Bass note index: changes every BassInterval
            int bassIdx = (int)(t / p.BassInterval) % p.BassNotes.Length;
            int bassRoot = (int)p.BassNotes[bassIdx];
            float bassFreq = s[Math.Min(bassRoot, s.Length - 1)];

            // Chord pad: soft sustained chord
            float chordSum = 0f;
            for (int c = 0; c < chord.Length; c++)
            {
                int noteIdx = Math.Min(bassRoot + chord[c], s.Length - 1);
                float freq = s[noteIdx] * 0.5f; // lower octave
                float ph = freq * Mathf.Tau * t;
                chordSum += Mathf.Sin(ph) * 0.12f + Mathf.Sin(ph * 2.003f) * 0.05f;
            }

            // Arpeggio: cycling through pattern
            int arpStep = (int)(t / p.ArpSpeed) % arp.Length;
            int arpOff = arp[arpStep];
            int arpNote = Math.Min(bassRoot + arpOff, s.Length - 1);
            float arpFreq = s[arpNote];
            float arpPh = arpFreq * Mathf.Tau * t;
            float arpT = (t % p.ArpSpeed) / p.ArpSpeed;
            float arpEnv = Mathf.Clamp(arpT < 0.15f ? arpT / 0.15f : 1f - (arpT - 0.15f) / 0.85f, 0f, 1f);
            float arpSig = (Mathf.Sin(arpPh) * 0.18f + Mathf.Sin(arpPh * 3.005f) * 0.06f) * arpEnv;

            // Subtle melody on top
            int melStep = (int)(t / (p.ArpSpeed * 2f)) % arp.Length;
            int melOff = arp[arp.Length - 1 - melStep];
            int melNote = Math.Min(bassRoot + melOff + 7, s.Length - 1);
            float melFreq = s[melNote];
            float melPh = melFreq * Mathf.Tau * t;
            float melEnv = 0.5f + 0.5f * Mathf.Sin(t * 0.7f);
            float melSig = Mathf.Sin(melPh) * 0.08f * melEnv;

            // Mix
            float sample = (chordSum + arpSig + melSig) * vol * 0.35f;
            sample = Mathf.Clamp(sample, -1f, 1f);
            pb.PushFrame(new Vector2(sample, sample));
        }
    }

    // ═══════════════════════════════════════════════════════
    // BGM API
    // ═══════════════════════════════════════════════════════

    public static void PlayBgm(string sceneId)
    {
        if (_instance == null) return;
        _instance.PlayBgmInternal(sceneId);
    }

    public static void StopBgm()
    {
        if (_instance?._bgmPlayer != null)
            _instance._bgmPlayer.Stop();
    }

    private void PlayBgmInternal(string sceneId)
    {
        if (_bgmPlayer == null) return;
        if (!BgmPresets.TryGetValue(sceneId, out var preset))
            preset = BgmPresets["map"];

        _currentPreset = preset;
        _bgmTime = (float)GD.RandRange(0, 100); // random phase start

        var gen = new AudioStreamGenerator { MixRate = 44100, BufferLength = 0.3f };
        _bgmPlayer.Stream = gen;
        _bgmPlayer.Play();
    }

    // ═══════════════════════════════════════════════════════
    // SFX API
    // ═══════════════════════════════════════════════════════

    public static void PlaySfx(string soundId)
    {
        if (_instance == null) return;
        _instance.PlaySfxInternal(soundId);
    }

    private void PlaySfxInternal(string soundId)
    {
        if (!SfxDefs.TryGetValue(soundId, out var def))
            def = (700f, 0.04f, 0.4f);

        var player = _sfxPool[_sfxIdx];
        _sfxIdx = (_sfxIdx + 1) % SfxPoolSize;
        player.Stop();

        var gen = new AudioStreamGenerator { MixRate = 44100, BufferLength = Mathf.Max(0.05f, def.Dur * 1.2f) };
        player.Stream = gen;
        player.Play();

        var pb = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (pb == null) return;

        float vol = _masterVol * def.Vol * 0.5f;
        int total = (int)(44100 * def.Dur);
        float step = def.Hz * Mathf.Tau / 44100f;
        float ph = 0f;
        for (int i = 0; i < total && pb.CanPushBuffer(1); i++)
        {
            float t = (float)i / total;
            float env = Mathf.Clamp(t < 0.1f ? t / 0.1f : (1f - t) / 0.9f, 0f, 1f);
            float s = Mathf.Sin(ph) * vol * env;
            pb.PushFrame(new Vector2(s, s));
            ph += step;
        }
    }
}
