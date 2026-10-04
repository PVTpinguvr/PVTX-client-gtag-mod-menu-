using System;
using System.Collections;
using System.Reflection;
using MonkeMenu.Mods;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Core;

/// <summary>
/// Sends a clip into voice chat by switching Photon Voice's Recorder from "Microphone" to "AudioClip" while the
/// sound plays, then switching it back. Everything is looked up by name (reflection) so it still compiles if the
/// game's voice code changes - if the Recorder can't be found the sound just plays locally.
/// </summary>
public static class MicBridge
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static Type      recType;
    private static Component rec;
    private static float     nextLook;
    private static int       token;
    private static bool      active;
    private static object    origSource;
    private static AudioClip origClip;
    private static bool      origLoop;

    public static bool Found => Recorder != null;

    private static Component Recorder
    {
        get
        {
            if (rec != null) return rec;
            if (Time.unscaledTime < nextLook) return null;
            nextLook = Time.unscaledTime + 2f;
            recType ??= Net.Find("Photon.Voice.Unity.Recorder");
            if (recType == null) return null;
            rec = Object.FindObjectOfType(recType) as Component;
            if (rec != null) Plugin.Log.LogInfo("Mic: found the voice Recorder");
            return rec;
        }
    }

    private static object Get(string name)
    {
        PropertyInfo p = recType.GetProperty(name, Any);
        if (p != null) return p.GetValue(rec);
        FieldInfo f = recType.GetField(name, Any);
        return f?.GetValue(rec);
    }

    private static bool Set(string name, object value)
    {
        PropertyInfo p = recType.GetProperty(name, Any);
        if (p != null && p.CanWrite) { p.SetValue(rec, value); return true; }
        FieldInfo f = recType.GetField(name, Any);
        if (f != null) { f.SetValue(rec, value); return true; }
        return false;
    }

    private static void Restart()
    {
        MethodInfo m = recType.GetMethod("RestartRecording", Any, null, Type.EmptyTypes, null);
        if (m != null) { m.Invoke(rec, null); return; }
        m = recType.GetMethod("RestartRecording", Any, null, new[] { typeof(bool), }, null);
        if (m != null) { m.Invoke(rec, new object[] { true, }); return; }
        Plugin.Log.LogWarning("Mic: Recorder has no RestartRecording method in this game version.");
    }

    /// <summary>Starts sending the clip into voice chat. Returns false if the voice Recorder wasn't found.</summary>
    public static bool Send(AudioClip clip, bool loop, MonoBehaviour host)
    {
        if (clip == null || Recorder == null) return false;
        try
        {
            if (!active)
            {
                origSource = Get("SourceType");
                origClip   = Get("AudioClip") as AudioClip;
                origLoop   = Get("LoopAudioClip") is bool b && b;
            }

            PropertyInfo sp = recType.GetProperty("SourceType", Any);
            Set("SourceType", Enum.Parse(sp.PropertyType, "AudioClip"));
            Set("AudioClip", clip);
            Set("LoopAudioClip", loop);
            Restart();
            active = true;

            int mine = ++token;
            if (!loop && host != null) host.StartCoroutine(RestoreAfter(clip.length + 0.3f, mine));
            return true;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning("Mic: couldn't send the sound into voice chat: " + e.Message);
            return false;
        }
    }

    /// <summary>Stops the current sound and puts your real microphone back.</summary>
    public static void Stop()
    {
        token++;
        Restore();
    }

    private static void Restore()
    {
        if (!active) return;
        active = false;
        if (rec == null) return;
        try
        {
            if (origSource != null) Set("SourceType", origSource);
            Set("AudioClip", origClip);
            Set("LoopAudioClip", origLoop);
            Restart();
        }
        catch (Exception e) { Plugin.Log.LogWarning("Mic: couldn't restore the microphone: " + e.Message); }
    }

    private static IEnumerator RestoreAfter(float seconds, int mine)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (mine == token) Restore();
    }

    /// <summary>Mute / unmute what you transmit to voice chat.</summary>
    public static void SetTransmit(bool on)
    {
        if (Recorder == null) return;
        try { Set("TransmitEnabled", on); }
        catch (Exception e) { Plugin.Log.LogWarning("Mic: couldn't change transmit state: " + e.Message); }
    }
}
