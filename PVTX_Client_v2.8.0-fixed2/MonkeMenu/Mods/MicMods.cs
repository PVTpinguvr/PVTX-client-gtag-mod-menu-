using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== SOUND -> MIC ==============================

[ModCategory(Cat.Sound)]
[ModInfo("Sounds Through Mic", "Sounds you play are also sent into voice chat so other players hear them", ButtonType.Togglable, AccessSetting.Public, EnabledType.Enabled, 0)]
public class SoundsThroughMic : MenuMod
{
    public override int Priority => -12;
    public override bool ShowInEnabledList => false;
    public override void OnEnable()  => SoundBoard.ThroughMic = true;
    public override void OnDisable() => SoundBoard.ThroughMic = false;
}

[ModCategory(Cat.Sound)]
[ModInfo("Hear Sounds Locally", "Also play sounds on your own speakers / headset", ButtonType.Togglable, AccessSetting.Public, EnabledType.Enabled, 0)]
public class HearSoundsLocally : MenuMod
{
    public override int Priority => -11;
    public override bool ShowInEnabledList => false;
    public override void OnEnable()  => SoundBoard.HearLocal = true;
    public override void OnDisable() => SoundBoard.HearLocal = false;
}

[ModCategory(Cat.Sound)]
[ModInfo("Mic Mute", "Stops your real microphone from sending anything", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MicMute : MenuMod
{
    public override int Priority => -10;
    private float next;

    public override void OnEnable()  => MicBridge.SetTransmit(false);
    public override void OnDisable() => MicBridge.SetTransmit(true);

    public override void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 1f;
        MicBridge.SetTransmit(false);
    }
}

[ModCategory(Cat.Sound)]
[ModInfo("Mic Hook", "Shows whether the voice recorder was found (press to log it)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MicHook : MenuMod
{
    public override int Priority => -13;
    public override string ModName => "Mic Hook: " + (MicBridge.Found ? "ready" : "not found");

    public override void Pressed() =>
            Plugin.Log.LogInfo("Mic hook: " + (MicBridge.Found ? "voice recorder found" : "voice recorder NOT found"));
}

[ModCategory(Cat.Sound)]
[ModInfo("Random Sound", "Plays a random sound from the list", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RandomSound : MenuMod
{
    public override int Priority => -5;

    public override void Pressed()
    {
        var sounds = SoundBoard.Registry.All.Where(m => m is SoundMod).ToList();
        if (sounds.Count > 0) sounds[Random.Range(0, sounds.Count)].Pressed();
    }
}
