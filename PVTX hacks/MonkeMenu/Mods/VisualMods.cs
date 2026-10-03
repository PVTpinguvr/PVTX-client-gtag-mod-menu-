using System;
using System.Collections.Generic;
using TMPro;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== LIGHTING / WORLD ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Fullbright", "Max ambient light so everything is bright", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Fullbright : MenuMod
{
    private AmbientMode mode; private Color color;

    public override void OnEnable()
    {
        mode = RenderSettings.ambientMode; color = RenderSettings.ambientLight;
    }

    public override void OnDisable()
    {
        RenderSettings.ambientMode = mode; RenderSettings.ambientLight = color;
    }

    public override void Update()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Night Mode", "Dark ambient light and a dark sky", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NightMode : MenuMod
{
    private AmbientMode mode; private Color color, bg; private CameraClearFlags flags;

    public override void OnEnable()
    {
        mode = RenderSettings.ambientMode; color = RenderSettings.ambientLight;
        if (Camera.main != null) { flags = Camera.main.clearFlags; bg = Camera.main.backgroundColor; }
    }

    public override void OnDisable()
    {
        RenderSettings.ambientMode = mode; RenderSettings.ambientLight = color;
        if (Camera.main != null) { Camera.main.clearFlags = flags; Camera.main.backgroundColor = bg; }
    }

    public override void Update()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.16f);
        if (Camera.main == null) return;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.02f, 0.02f, 0.06f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Disco Light", "Ambient light slowly drifts through colours (no flashing)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DiscoLight : MenuMod
{
    private AmbientMode mode; private Color color;

    public override void OnEnable()
    {
        mode = RenderSettings.ambientMode; color = RenderSettings.ambientLight;
    }

    public override void OnDisable()
    {
        RenderSettings.ambientMode = mode; RenderSettings.ambientLight = color;
    }

    public override void Update()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.HSVToRGB((Time.time * 0.08f) % 1f, 0.7f, 0.9f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Ambient Color: ", "Pick a flat ambient light colour", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AmbientColor : IncrementalMod
{
    private static bool saved; private static AmbientMode mode; private static Color color;
    protected override string[] Labels => Palette.Names;

    protected override void Changed()
    {
        if (IncrementalValue != 0)
        {
            if (!saved) { mode = RenderSettings.ambientMode; color = RenderSettings.ambientLight; saved = true; }
        }
        else if (saved)
        {
            RenderSettings.ambientMode = mode; RenderSettings.ambientLight = color; saved = false;
        }
    }

    public override void Update()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = Palette.Colors[IncrementalValue];
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Sky Color: ", "Pick a solid sky colour", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SkyColor : IncrementalMod
{
    private static bool saved; private static CameraClearFlags flags; private static Color bg;
    protected override string[] Labels => Palette.Names;

    protected override void Changed()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (IncrementalValue != 0)
        {
            if (!saved) { flags = cam.clearFlags; bg = cam.backgroundColor; saved = true; }
        }
        else if (saved)
        {
            cam.clearFlags = flags; cam.backgroundColor = bg; saved = false;
        }
    }

    public override void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Palette.Colors[IncrementalValue];
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Fog Color: ", "Pick a fog colour", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FogColor : IncrementalMod
{
    private static bool saved; private static bool fog; private static Color color;
    protected override string[] Labels => Palette.Names;

    protected override void Changed()
    {
        if (IncrementalValue != 0)
        {
            if (!saved) { fog = RenderSettings.fog; color = RenderSettings.fogColor; saved = true; }
        }
        else if (saved)
        {
            RenderSettings.fog = fog; RenderSettings.fogColor = color; saved = false;
        }
    }

    public override void Update()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = Palette.Colors[IncrementalValue];
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("No Fog", "Turns fog off", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoFog : MenuMod
{
    private bool fog;
    public override void OnEnable()  => fog = RenderSettings.fog;
    public override void OnDisable() => RenderSettings.fog = fog;
    public override void Update()    => RenderSettings.fog = false;
}

[ModCategory(Cat.Visual)]
[ModInfo("Rainbow Sky", "Background slowly cycles through colours", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RainbowSky : MenuMod
{
    private CameraClearFlags flags; private Color bg;

    public override void OnEnable()
    {
        if (Camera.main != null) { flags = Camera.main.clearFlags; bg = Camera.main.backgroundColor; }
    }

    public override void OnDisable()
    {
        if (Camera.main != null) { Camera.main.clearFlags = flags; Camera.main.backgroundColor = bg; }
    }

    public override void Update()
    {
        if (Camera.main == null) return;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = H.Rainbow(0.2f);
    }
}

// ============================== CAMERA ==============================

[ModCategory(Cat.Visual)]
[ModInfo("FOV: ", "Changes your field of view (desktop only - VR headsets control their own FOV)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FovChanger : IncrementalMod
{
    private static float orig = -1f;
    private static readonly float[] Values = [0f, 60f, 75f, 100f, 120f, 140f,];
    protected override string[] Labels => ["Off", "60", "75", "100", "120", "140",];

    protected override void Changed() => Apply();
    public override void Update()     => Apply();

    private void Apply()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (orig < 0f) orig = cam.fieldOfView;
        cam.fieldOfView = IncrementalValue == 0 ? orig : Values[IncrementalValue];
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Zoom", "Hold left trigger to zoom in (desktop/spectator camera only)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Zoom : MenuMod
{
    private float orig = -1f;

    public override string BindHint => "LT";
    public override void OnDisable()
    {
        if (Camera.main != null && orig > 0f) Camera.main.fieldOfView = orig;
    }

    public override void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || !H.Ready) return;
        if (orig < 0f) orig = cam.fieldOfView;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, H.LTrig ? 25f : orig, Time.deltaTime * 10f);
    }
}

// ============================== HUDs ==============================

[ModCategory(Cat.Visual)]
[ModInfo("FPS Counter", "Floating FPS text under your view", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FpsCounter : HudMod
{
    private float smooth = 60f;
    protected override Vector3 Offset => new(0f, -0.18f, 0.6f);

    protected override string Line()
    {
        smooth = Mathf.Lerp(smooth, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);
        return $"{Mathf.RoundToInt(smooth)} FPS";
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Speed HUD", "Shows your current speed", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SpeedHud : HudMod
{
    protected override Vector3 Offset => new(0f, -0.26f, 0.6f);
    protected override string Line() => $"{H.Rb.velocity.magnitude:F1} m/s";
}

[ModCategory(Cat.Visual)]
[ModInfo("Position HUD", "Shows your world position", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PositionHud : HudMod
{
    protected override Vector3 Offset => new(0f, -0.34f, 0.6f);

    protected override string Line()
    {
        Vector3 p = H.Head.position;
        return $"{p.x:F1}, {p.y:F1}, {p.z:F1}";
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Height HUD", "Shows how high up you are", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HeightHud : HudMod
{
    protected override Vector3 Offset => new(0f, -0.42f, 0.6f);
    protected override string Line() => $"Height {H.Head.position.y:F1} m";
}

[ModCategory(Cat.Visual)]
[ModInfo("Clock HUD", "Shows the time of day", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ClockHud : HudMod
{
    protected override Vector3 Offset => new(0.35f, -0.18f, 0.6f);
    protected override string Line() => DateTime.Now.ToString("HH:mm");
}

[ModCategory(Cat.Visual)]
[ModInfo("Ping HUD", "Shows your connection delay", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PingHud : HudMod
{
    private float next; private string cached = "";
    protected override Vector3 Offset => new(-0.35f, -0.18f, 0.6f);

    protected override string Line()
    {
        if (Time.time > next) { next = Time.time + 1f; cached = Net.Ping(); }
        return cached;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Room HUD", "Shows the room you're in and how many players", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RoomHud : HudMod
{
    private float next; private string cached = "";
    protected override Vector3 Offset => new(0f, -0.5f, 0.6f);

    protected override string Line()
    {
        if (Time.time > next) { next = Time.time + 1f; cached = Net.Room(); }
        return cached;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Compass HUD", "Shows which way you're facing", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CompassHud : HudMod
{
    private static readonly string[] Dir = ["N", "NE", "E", "SE", "S", "SW", "W", "NW",];
    protected override Vector3 Offset => new(0f, 0.3f, 0.6f);

    protected override string Line()
    {
        float yaw = H.Look.eulerAngles.y;
        return $"{Dir[Mathf.RoundToInt(yaw / 45f) % 8]}  {Mathf.RoundToInt(yaw)}";
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Crosshair", "A small + in the middle of your view", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CrosshairHud : HudMod
{
    protected override Vector3 Offset => new(0f, 0f, 0.8f);
    protected override string Line() => "+";
}

// ============================== TRAILS / LIGHTS / EFFECTS ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Rainbow Hand Trails", "Rainbow trails follow both hands", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HandTrails : MenuMod
{
    private readonly GameObject[] objs = new GameObject[2];
    private readonly TrailRenderer[] trails = new TrailRenderer[2];

    public override void OnEnable()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            objs[i] = new GameObject("HandTrail" + i);
            objs[i].transform.SetParent(H.Hand(i == 0), false);
            TrailRenderer t = objs[i].AddComponent<TrailRenderer>();
            t.time = 1.2f; t.startWidth = 0.08f; t.endWidth = 0f;
            t.material = H.SpriteMat(Color.white);
            trails[i] = t;
        }
    }

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) if (objs[i] != null) Object.Destroy(objs[i]);
    }

    public override void Update()
    {
        for (int i = 0; i < 2; i++)
        {
            if (trails[i] == null) continue;
            Color c = H.Rainbow(0.4f, i * 0.5f);
            trails[i].startColor = c; trails[i].endColor = new Color(c.r, c.g, c.b, 0f);
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Head Trail", "A rainbow trail follows your head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HeadTrail : MenuMod
{
    private GameObject obj;
    private TrailRenderer trail;

    public override void OnEnable()
    {
        obj = new GameObject("HeadTrail");
        trail = obj.AddComponent<TrailRenderer>();
        trail.time = 1.5f; trail.startWidth = 0.12f; trail.endWidth = 0f;
        trail.material = H.SpriteMat(Color.white);
    }

    public override void OnDisable()
    {
        if (obj != null) Object.Destroy(obj);
        obj = null; trail = null;
    }

    public override void Update()
    {
        if (!H.Ready || obj == null) return;
        obj.transform.position = H.Head.position + Vector3.down * 0.15f;
        Color c = H.Rainbow(0.3f);
        trail.startColor = c; trail.endColor = new Color(c.r, c.g, c.b, 0f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Hand Lights", "Coloured point lights on both hands", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HandLights : MenuMod
{
    private readonly GameObject[] objs = new GameObject[2];

    public override void OnEnable()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            objs[i] = new GameObject("HandLight" + i);
            objs[i].transform.SetParent(H.Hand(i == 0), false);
            Light l = objs[i].AddComponent<Light>();
            l.type = LightType.Point; l.range = 8f; l.intensity = 3f;
        }
    }

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) if (objs[i] != null) Object.Destroy(objs[i]);
    }

    public override void Update()
    {
        for (int i = 0; i < 2; i++)
            if (objs[i] != null) objs[i].GetComponent<Light>().color = H.Rainbow(0.5f, i * 0.5f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Headlamp", "A spotlight on your head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Headlamp : MenuMod
{
    private GameObject obj;

    public override void OnEnable()
    {
        if (!H.Ready) return;
        obj = new GameObject("Headlamp");
        obj.transform.SetParent(H.Look, false);
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Spot; l.range = 40f; l.spotAngle = 70f; l.intensity = 4f;
    }

    public override void OnDisable()
    {
        if (obj != null) Object.Destroy(obj);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Hand Orbs", "A glowing rainbow ball floats at each hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HandOrbs : MenuMod
{
    private readonly GameObject[] orbs = new GameObject[2];

    public override void OnEnable()
    {
        for (int i = 0; i < 2; i++) orbs[i] = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.08f, Color.white, false);
    }

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) if (orbs[i] != null) Object.Destroy(orbs[i]);
    }

    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            if (orbs[i] == null) continue;
            bool left = i == 0;
            orbs[i].transform.position = H.Hand(left).position + H.Point(left) * 0.12f;
            orbs[i].GetComponent<Renderer>().material.color = H.Rainbow(0.6f, i * 0.5f);
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Laser Pointers", "A rainbow laser comes out of each hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LaserPointers : MenuMod
{
    private readonly LineRenderer[] lines = new LineRenderer[2];

    public override void OnEnable()
    {
        for (int i = 0; i < 2; i++) lines[i] = H.MakeLine("Laser" + i, Color.red, 0.01f);
    }

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) if (lines[i] != null) Object.Destroy(lines[i].gameObject);
    }

    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            if (lines[i] == null) continue;
            bool left = i == 0;
            Vector3 o = H.Hand(left).position, d = H.Point(left);
            Vector3 end = Physics.Raycast(o, d, out RaycastHit hit, 8f) ? hit.point : o + d * 8f;
            lines[i].SetPosition(0, o);
            lines[i].SetPosition(1, end);
            Color c = H.Rainbow(0.5f, i * 0.5f);
            lines[i].startColor = lines[i].endColor = c;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Crown", "A gold crown floats above your head (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Crown : RingBase
{
    protected override int   Count  => 8;
    protected override float Radius => 0.14f;
    protected override float Height => 0.28f;
    protected override float Size   => 0.06f;
    protected override Color Tint(int i) => new(1f, 0.8f, 0.1f);
}

[ModCategory(Cat.Visual)]
[ModInfo("Head Halo", "A spinning rainbow ring over your head (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HeadHalo : RingBase
{
    protected override int   Count  => 12;
    protected override float Radius => 0.22f;
    protected override float Height => 0.4f;
    protected override float Size   => 0.05f;
    protected override float Spin   => 1.5f;
    protected override PrimitiveType Shape => PrimitiveType.Sphere;
    protected override Color Tint(int i) => H.Rainbow(0.4f, i / 12f);
}

[ModCategory(Cat.Visual)]
[ModInfo("Night Time", "Dark sky / ambient for night vibe", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NightTime : MenuMod
{
    public override void Update()
    {
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.12f);
        RenderSettings.ambientIntensity = 0.35f;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Day Time", "Bright day ambient", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DayTime : MenuMod
{
    public override void Update()
    {
        RenderSettings.ambientLight = new Color(0.95f, 0.92f, 0.85f);
        RenderSettings.ambientIntensity = 1.1f;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Box ESP", "Draws a box outline at every other player's head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BoxEsp : MenuMod
{
    private readonly List<LineRenderer> boxes = [];

    public override void OnDisable()
    {
        foreach (LineRenderer l in boxes)
            if (l != null) Object.Destroy(l.gameObject);
        boxes.Clear();
    }

    public override void Update()
    {
        if (!H.Ready) return;
        var rigs = Adv.OtherRigs();
        // 12 edges per box ≈ use one line strip of 16 points cycling
        while (boxes.Count < rigs.Count)
        {
            LineRenderer lr = H.MakeLine("BoxESP", Color.green, 0.008f);
            lr.loop = true;
            lr.positionCount = 16;
            boxes.Add(lr);
        }
        while (boxes.Count > rigs.Count)
        {
            int i = boxes.Count - 1;
            if (boxes[i] != null) Object.Destroy(boxes[i].gameObject);
            boxes.RemoveAt(i);
        }

        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 c = Adv.RigHead(rigs[i]);
            float s = 0.35f;
            Vector3[] p =
            {
                c + new Vector3(-s, -s * 1.5f, -s), c + new Vector3(s, -s * 1.5f, -s),
                c + new Vector3(s, -s * 1.5f, -s), c + new Vector3(s, -s * 1.5f, s),
                c + new Vector3(s, -s * 1.5f, s), c + new Vector3(-s, -s * 1.5f, s),
                c + new Vector3(-s, -s * 1.5f, s), c + new Vector3(-s, -s * 1.5f, -s),
                c + new Vector3(-s, s, -s), c + new Vector3(s, s, -s),
                c + new Vector3(s, s, -s), c + new Vector3(s, s, s),
                c + new Vector3(s, s, s), c + new Vector3(-s, s, s),
                c + new Vector3(-s, s, s), c + new Vector3(-s, s, -s),
            };
            boxes[i].positionCount = p.Length;
            boxes[i].SetPositions(p);
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Name Tags", "Shows a floating label with distance above other players", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NameTags : MenuMod
{
    private readonly List<TextMeshPro> tags = [];

    public override void OnDisable()
    {
        foreach (TextMeshPro t in tags)
            if (t != null) Object.Destroy(t.gameObject);
        tags.Clear();
    }

    public override void Update()
    {
        if (!H.Ready) return;
        var rigs = Adv.OtherRigs();
        while (tags.Count < rigs.Count)
            tags.Add(H.MakeHud("NameTag", Vector3.zero));
        while (tags.Count > rigs.Count)
        {
            int i = tags.Count - 1;
            if (tags[i] != null) Object.Destroy(tags[i].gameObject);
            tags.RemoveAt(i);
        }

        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 head = Adv.RigHead(rigs[i]);
            float d = Vector3.Distance(H.Head.position, head);
            TextMeshPro t = tags[i];
            t.transform.position = head + Vector3.up * 0.35f;
            t.transform.rotation = Quaternion.LookRotation(t.transform.position - H.Head.position);
            t.text = $"{rigs[i].name}\n{d:F1}m";
            t.fontSize = 0.25f;
        }
    }
}
