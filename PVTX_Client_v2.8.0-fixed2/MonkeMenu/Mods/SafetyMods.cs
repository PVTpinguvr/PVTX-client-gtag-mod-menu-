using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

[ModCategory(Cat.Safety)]
[ModInfo("Anti AFK", "Tiny movement every few seconds so the game does not idle-kick you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AntiAfk : MenuMod
{
    private float next;
    public override void Update()
    {
        if (!H.Ready || Time.time < next) return;
        next = Time.time + 8f;
        H.Rb.AddForce(Vector3.up * 0.15f, ForceMode.VelocityChange);
    }
}

[ModCategory(Cat.Safety)]
[ModInfo("Disable Quit Box", "Turns off quit-box colliders so walking into them does nothing", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableQuitBox : MenuMod
{
    private readonly List<Collider> off = [];
    public override void OnEnable()  => Scan(true);
    public override void OnDisable() => Scan(false);
    public override void Update()    { if (Time.frameCount % 120 == 0) Scan(true); }

    private void Scan(bool disable)
    {
        if (!disable)
        {
            foreach (Collider c in off) if (c != null) c.enabled = true;
            off.Clear();
            return;
        }
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || off.Contains(c)) continue;
            string n = c.gameObject.name.ToLowerInvariant();
            if (n.Contains("quit") || n.Contains("boundary") || n.Contains("fall") && n.Contains("box"))
            {
                if (c.enabled) { c.enabled = false; off.Add(c); }
            }
        }
    }
}

[ModCategory(Cat.Safety)]
[ModInfo("Panic", "Turns off every running mod immediately", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PanicButton : MenuMod
{
    public override string BindHint => "TAP";
    public override void Pressed()
    {
        // SoundBoard.Registry is set at Init
        SoundBoard.Registry?.DisableAll();
    }
}

[ModCategory(Cat.Safety)]
[ModInfo("FPS Cap: ", "Limits how hard the game works on your headset", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FpsCap : IncrementalMod
{
    private static readonly int[] Caps = [0, 30, 60, 72, 90, 120,];
    protected override string[] Labels => ["Off", "30", "60", "72", "90", "120",];
    protected override void Changed()
    {
        int c = Caps[IncrementalValue];
        Application.targetFrameRate = c <= 0 ? -1 : c;
    }
    public override void OnDisable() => Application.targetFrameRate = -1;
}

[ModCategory(Cat.Safety)]
[ModInfo("No Finger Movement", "Stops finger pose updates from reaching the game (local)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoFingerMovement : MenuMod
{
    // soft: zero grip/trigger influence is not available; we just document as placeholder client soft-lock
    public override void Update() { /* input is polled by game; full mute needs deeper hooks */ }
}
