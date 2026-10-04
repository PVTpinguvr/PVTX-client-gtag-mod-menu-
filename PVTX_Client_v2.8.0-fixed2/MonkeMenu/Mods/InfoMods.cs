using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// Little floating read-outs. Each one sits in its own spot so you can turn on as many as you like.

[ModCategory(Cat.Info)]
[ModInfo("Top Speed HUD", "Shows the fastest you've gone since turning this on", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TopSpeedHud : HudMod
{
    private float max;
    protected override Vector3 Offset => new(0.35f, -0.26f, 0.6f);
    public override void OnDisable() { base.OnDisable(); max = 0f; }

    protected override string Line()
    {
        max = Mathf.Max(max, H.Rb.velocity.magnitude);
        return $"Top {max:F1} m/s";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Distance HUD", "Shows how far you've travelled since turning this on", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DistanceHud : HudMod
{
    private Vector3 last;
    private bool    has;
    private float   total;
    protected override Vector3 Offset => new(0.35f, -0.34f, 0.6f);
    public override void OnDisable() { base.OnDisable(); has = false; total = 0f; }

    protected override string Line()
    {
        Vector3 p = H.Head.position;
        if (has)
        {
            float d = Vector3.Distance(last, p);
            if (d < 3f) total += d; // teleports don't count
        }
        last = p;
        has  = true;
        return $"Dist {total:F0} m";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Stopwatch HUD", "Counts up from the moment you turn it on", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class StopwatchHud : HudMod
{
    private float start;
    protected override Vector3 Offset => new(0.35f, -0.42f, 0.6f);
    public override void OnEnable() => start = Time.time;

    protected override string Line()
    {
        float t = Time.time - start;
        return $"{(int)(t / 60f):00}:{t % 60f:00.0}";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Air Time HUD", "Shows how long you've been in the air", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AirTimeHud : HudMod
{
    private float air;
    protected override Vector3 Offset => new(0.35f, -0.5f, 0.6f);
    public override void OnDisable() { base.OnDisable(); air = 0f; }

    protected override string Line()
    {
        if (Ground.Near(1.8f)) air = 0f; else air += Time.deltaTime;
        return $"Air {air:F1} s";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Battery HUD", "Shows your headset / device battery", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BatteryHud : HudMod
{
    protected override Vector3 Offset => new(-0.35f, -0.26f, 0.6f);

    protected override string Line()
    {
        float b = SystemInfo.batteryLevel;
        return b < 0f ? "Battery n/a" : $"Battery {Mathf.RoundToInt(b * 100f)}%";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Session HUD", "Shows how long the game has been running", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SessionHud : HudMod
{
    protected override Vector3 Offset => new(-0.35f, -0.34f, 0.6f);

    protected override string Line()
    {
        float t = Time.realtimeSinceStartup;
        return $"Playing {(int)(t / 3600f)}h {(int)(t / 60f) % 60:00}m";
    }
}

[ModCategory(Cat.Info)]
[ModInfo("Vertical Speed HUD", "Shows how fast you're rising or falling", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class VerticalSpeedHud : HudMod
{
    protected override Vector3 Offset => new(-0.35f, -0.42f, 0.6f);
    protected override string Line() => $"Vert {H.Rb.velocity.y:+0.0;-0.0;0.0} m/s";
}

[ModCategory(Cat.Info)]
[ModInfo("Grounded HUD", "Shows whether you're standing on something or in the air", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GroundedHud : HudMod
{
    protected override Vector3 Offset => new(-0.35f, -0.5f, 0.6f);
    protected override string Line() => Ground.Near(1.8f) ? "On ground" : "In air";
}

[ModCategory(Cat.Info)]
[ModInfo("Frame Time HUD", "Shows how many milliseconds each frame takes", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FrameTimeHud : HudMod
{
    protected override Vector3 Offset => new(0f, -0.58f, 0.6f);
    protected override string Line() => $"{Time.unscaledDeltaTime * 1000f:F1} ms";
}
