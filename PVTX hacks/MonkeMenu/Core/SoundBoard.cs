using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonkeMenu.Mods;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonkeMenu.Core;

/// <summary>
/// Plays sounds on YOUR machine. Built-in beeps are generated in code; your own .wav / .ogg / .mp3
/// files dropped in BepInEx\MonkeMenuSounds show up as buttons in the Sound tab.
/// </summary>
public static class SoundBoard
{
    private const int Rate = 44100;

    public static MonoBehaviour Host;
    public static ModRegistry   Registry;
    public static float         Volume = 1f;
    public static bool          Loop;
    public static bool          ThroughMic = true;  // also send sounds into voice chat (Sound > Sounds Through Mic)
    public static bool          HearLocal  = true;  // also play them on your own speakers (Sound > Hear Sounds Locally)

    private static AudioSource src;
    private static AudioSource uiSrc;
    private static AudioClip   menuOpenClip;
    private static AudioClip   menuClickClip;

    public static string Folder => Path.Combine(BepInEx.Paths.BepInExRootPath, "MonkeMenuSounds");

    private static AudioSource Src
    {
        get
        {
            if (src != null) return src;
            GameObject go = new("MonkeMenu_Audio");
            Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.playOnAwake  = false;
            return src;
        }
    }

    /// <summary>Separate source for menu UI beeps so they never stop / get mixed into the soundboard or mic.</summary>
    private static AudioSource UiSrc
    {
        get
        {
            if (uiSrc != null) return uiSrc;
            GameObject go = new("MonkeMenu_UI_Audio");
            Object.DontDestroyOnLoad(go);
            uiSrc = go.AddComponent<AudioSource>();
            uiSrc.spatialBlend = 0f;
            uiSrc.playOnAwake  = false;
            uiSrc.volume       = 0.7f;
            return uiSrc;
        }
    }

    public static void Init(MonoBehaviour host, ModRegistry registry)
    {
        Host = host;
        Registry = registry;

        // Custom UI sounds (Wii home open + click). Shipped as WAV next to the DLL under Sounds/.
        menuOpenClip  = LoadUiWav("menu_open.wav")  ?? Synth("MenuOpen", 0.22f, t =>
        {
            float f = Mathf.Lerp(520f, 980f, t / 0.22f);
            return Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f * (1f - t / 0.22f);
        });
        menuClickClip = LoadUiWav("menu_click.wav") ?? Synth("MenuClick", 0.06f, t =>
            Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.5f * Mathf.Exp(-40f * t));

        AddBuiltIn("Beep",      0.25f, t => Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f);
        AddBuiltIn("Siren",     2.0f,  t => Mathf.Sin(2f * Mathf.PI * (700f * t - 400f / (4f * Mathf.PI) * Mathf.Cos(4f * Mathf.PI * t))) * 0.4f);
        AddBuiltIn("Laser",     0.35f, t => Mathf.Sin(2f * Mathf.PI * (1800f * t - 1600f / 0.35f * t * t / 2f)) * (1f - t / 0.35f) * 0.6f);
        AddBuiltIn("Alarm",     2.0f,  t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (Mathf.FloorToInt(t * 4f) % 2 == 0 ? 600f : 800f) * t)) * 0.25f);
        AddBuiltIn("Drum",      0.5f,  t => Mathf.Sin(2f * Mathf.PI * (60f * t + 100f * (1f - Mathf.Exp(-18f * t)) / 18f)) * Mathf.Exp(-9f * t));
        AddBuiltIn("Horn",      0.8f,  t => (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 554f * t) + Mathf.Sin(2f * Mathf.PI * 659f * t)) * 0.18f);
        AddBuiltIn("Bubble",    0.15f, t => Mathf.Sin(2f * Mathf.PI * (300f * t + 700f / 0.15f * t * t / 2f)) * (1f - t / 0.15f) * 0.6f);
        AddBuiltIn("Power Up",  0.4f,  t => Mathf.Sin(2f * Mathf.PI * new[] { 400f, 500f, 600f, 800f, }[Mathf.Min(3, (int)(t / 0.1f))] * t) * 0.4f);
        AddBuiltIn("Power Down", 0.4f, t => Mathf.Sin(2f * Mathf.PI * new[] { 800f, 600f, 500f, 400f, }[Mathf.Min(3, (int)(t / 0.1f))] * t) * 0.4f);
        AddBuiltIn("Chime",     1.2f,  t => (Mathf.Sin(2f * Mathf.PI * 1046f * t) + Mathf.Sin(2f * Mathf.PI * 1318f * t)) * 0.25f * Mathf.Exp(-3f * t));
        AddBuiltIn("Buzz",      0.5f,  t => (2f * ((t * 120f) % 1f) - 1f) * 0.3f);

        float prev = 0f;
        AddBuiltIn("Wind", 1.5f, t =>
        {
            prev = prev * 0.95f + (Random.value * 2f - 1f) * 0.05f;
            return prev * 6f * Mathf.Sin(Mathf.PI * t / 1.5f);
        });

        AddBuiltIn("Coin",       0.35f, t => Mathf.Sin(2f * Mathf.PI * (t < 0.08f ? 988f : 1319f) * t) * 0.4f * Mathf.Exp(-4f * Mathf.Max(0f, t - 0.08f)));
        AddBuiltIn("Error",      0.4f,  t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 150f * t)) * 0.3f);
        AddBuiltIn("Zap",        0.3f,  t => (2f * ((t * (900f - 1500f * t)) % 1f) - 1f) * 0.3f * (1f - t / 0.3f));
        AddBuiltIn("Sweep Up",   0.6f,  t => Mathf.Sin(2f * Mathf.PI * (300f * t + 1000f * t * t)) * 0.4f);
        AddBuiltIn("Sweep Down", 0.6f,  t => Mathf.Sin(2f * Mathf.PI * (1500f * t - 1000f * t * t)) * 0.4f);
        AddBuiltIn("Ding Dong",  1.2f,  t => (t < 0.5f ? Mathf.Sin(2f * Mathf.PI * 659f * t) * Mathf.Exp(-4f * t)
                                                       : Mathf.Sin(2f * Mathf.PI * 523f * t) * Mathf.Exp(-4f * (t - 0.5f))) * 0.5f);
        AddBuiltIn("Phone Ring", 2.0f,  t => (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t)) * 0.2f * ((t % 1f) < 0.4f ? 1f : 0f));
        AddBuiltIn("Pew Pew",    0.45f, t =>
        {
            float tt = t % 0.22f;
            return Mathf.Sin(2f * Mathf.PI * (1500f * tt - 3000f * tt * tt)) * (1f - tt / 0.22f) * 0.5f;
        });
        AddBuiltIn("Heartbeat",  1.0f,  t =>
        {
            float a = Mathf.Exp(-25f * t);
            float b = t > 0.25f ? 0.7f * Mathf.Exp(-25f * (t - 0.25f)) : 0f;
            return Mathf.Sin(2f * Mathf.PI * 55f * t) * (a + b) * 0.8f;
        });

        float prevBoom = 0f;
        AddBuiltIn("Explosion", 1.2f, t =>
        {
            prevBoom = prevBoom * 0.9f + (Random.value * 2f - 1f) * 0.1f;
            return prevBoom * 5f * Mathf.Exp(-3.5f * t);
        });

        float prevWhoosh = 0f;
        AddBuiltIn("Whoosh", 1.0f, t =>
        {
            prevWhoosh = prevWhoosh * 0.8f + (Random.value * 2f - 1f) * 0.2f;
            return prevWhoosh * 3f * Mathf.Sin(Mathf.PI * t);
        });

        Reload();
    }

    private static void AddBuiltIn(string name, float seconds, Func<float, float> f) =>
            Registry.AddDynamic(new SoundMod(name, () => Synth(name, seconds, f), null), Cat.Sound);

    /// <summary>Re-reads the sounds folder. Built-in sounds stay.</summary>
    public static void Reload()
    {
        Registry.RemoveDynamic(m => m is SoundMod s && s.FromFile);
        try
        {
            Directory.CreateDirectory(Folder);
            int count = 0;
            foreach (string file in Directory.GetFiles(Folder))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".wav" && ext != ".ogg" && ext != ".mp3") continue;
                if (++count > 300) break;
                Registry.AddDynamic(new SoundMod(Path.GetFileNameWithoutExtension(file), null, file), Cat.Sound);
            }
            Plugin.Log.LogInfo($"Soundboard: {count} file(s) in {Folder}");
        }
        catch (Exception e) { Plugin.Log.LogWarning("Soundboard folder problem: " + e.Message); }
    }

    // ---- Playback ---------------------------------------------------------------------------
    public static void Play(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource s = Src;
        s.Stop();
        if (HearLocal)
        {
            s.clip   = clip;
            s.volume = Volume;
            s.loop   = Loop;
            s.Play();
        }

        if (ThroughMic) MicBridge.Send(clip, Loop, Host);
    }

    /// <summary>Local-only UI beep when the VR menu opens. Never goes through the mic.</summary>
    public static void PlayMenuOpen()
    {
        if (menuOpenClip == null) return;
        AudioSource s = UiSrc;
        s.PlayOneShot(menuOpenClip, 0.85f);
    }

    /// <summary>Local-only UI tick when a menu button is poked. Never goes through the mic.</summary>
    public static void PlayMenuClick()
    {
        if (menuClickClip == null) return;
        AudioSource s = UiSrc;
        s.PlayOneShot(menuClickClip, 0.9f);
    }

    public static void Stop()
    {
        if (src != null) src.Stop();
        MicBridge.Stop();
    }

    public static void SetVolume(float v) { Volume = v; if (src != null) src.volume = v; }
    public static void SetLoop(bool on)   { Loop = on;  if (src != null) src.loop = on; }

    private static AudioClip Synth(string name, float seconds, Func<float, float> f)
    {
        int n = (int)(seconds * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)Rate;
            float env = Mathf.Clamp01(Mathf.Min(t / 0.005f, (seconds - t) / 0.03f)); // tiny fades stop clicks
            data[i]   = Mathf.Clamp(f(t) * env, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Loads a mono/stereo 16-bit PCM WAV from BepInEx\plugins\MonkeMenuSounds\ or
    /// BepInEx\plugins\Sounds\ (copied there by the installer / build).
    /// Returns null if the file is missing or not a supported PCM WAV.
    /// </summary>
    private static AudioClip LoadUiWav(string fileName)
    {
        try
        {
            string[] roots =
            [
                Path.Combine(BepInEx.Paths.PluginPath, "MonkeMenuSounds"),
                Path.Combine(BepInEx.Paths.PluginPath, "Sounds"),
                Path.Combine(BepInEx.Paths.BepInExRootPath, "MonkeMenuSounds"),
                Path.GetDirectoryName(typeof(SoundBoard).Assembly.Location) ?? "",
            ];
            string path = null;
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                string candidate = Path.Combine(root, fileName);
                if (File.Exists(candidate)) { path = candidate; break; }
                // also accept the original mp3 names if the user dropped them in
                if (fileName == "menu_open.wav")
                {
                    string alt = Path.Combine(root, "wii-home-button-sound-effects-hd-audiotrimmer_Yb6RcCL.mp3");
                    if (File.Exists(alt)) { /* mp3 needs async load; skip for sync UI */ }
                }
            }
            if (path == null) return null;

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 44) return null;
            // "RIFF....WAVE"
            if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F')
                return null;
            if (bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
                return null;

            int pos = 12;
            int channels = 1, sampleRate = Rate, bits = 16, dataOffset = -1, dataSize = 0;
            while (pos + 8 <= bytes.Length)
            {
                string chunk = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                int size = BitConverter.ToInt32(bytes, pos + 4);
                pos += 8;
                if (chunk == "fmt ")
                {
                    channels   = BitConverter.ToInt16(bytes, pos + 2);
                    sampleRate = BitConverter.ToInt32(bytes, pos + 4);
                    bits       = BitConverter.ToInt16(bytes, pos + 14);
                }
                else if (chunk == "data")
                {
                    dataOffset = pos;
                    dataSize   = size;
                    break;
                }
                pos += size;
            }
            if (dataOffset < 0 || bits != 16 || channels < 1) return null;

            int sampleCount = dataSize / (2 * channels);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = dataOffset + i * 2 * channels;
                // mix down to mono if needed
                int sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += BitConverter.ToInt16(bytes, byteIndex + c * 2);
                samples[i] = (sum / (float)channels) / 32768f;
            }

            AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(fileName), sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            Plugin.Log.LogInfo($"UI sound loaded: {path}");
            return clip;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not load UI sound {fileName}: {e.Message}");
            return null;
        }
    }

    public static IEnumerator LoadFile(string path, string name, Action<AudioClip> done)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        AudioType type = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.MPEG : AudioType.WAV;

        using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Plugin.Log.LogWarning($"Couldn't load sound {path}: {req.error}");
            done(null);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
        clip.name = name;
        done(clip);
    }
}

/// <summary>One button on the soundboard.</summary>
public sealed class SoundMod : MenuMod
{
    private readonly Func<AudioClip> make;
    private readonly string          path;
    private AudioClip                clip;
    private bool                     loading;

    public bool FromFile => path != null;

    public SoundMod(string name, Func<AudioClip> make, string path)
    {
        this.make = make;
        this.path = path;
        Info = new ModInfoAttribute(name, path ?? "Built-in sound", ButtonType.Fixed, AccessSetting.Public,
                                    EnabledType.Disabled, 0);
    }

    public override void Pressed()
    {
        if (clip != null) { SoundBoard.Play(clip); return; }

        if (make != null)
        {
            clip = make();
            SoundBoard.Play(clip);
            return;
        }

        if (loading || SoundBoard.Host == null) return;
        loading = true;
        SoundBoard.Host.StartCoroutine(SoundBoard.LoadFile(path, Info.Name, c =>
        {
            loading = false;
            clip = c;
            SoundBoard.Play(c);
        }));
    }
}
