using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== FUN ==============================
// (all local-only, like the other fun mods)

[ModCategory(Cat.Fun)]
[ModInfo("Cube Trail", "Rainbow cubes drop behind you while you move", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CubeTrail : MenuMod
{
    private float next;

    public override void Update()
    {
        if (!H.Ready || Time.time < next || H.Rb.velocity.magnitude < 1f) return;
        next = Time.time + 0.06f;
        GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 0.15f, H.Rainbow(1f), false);
        c.transform.position = H.Head.position - Vector3.up * 0.5f;
        H.Track(c, 2.5f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Fireworks", "A burst of colourful sparks in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Fireworks : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 centre = H.Head.position + H.Look.forward * 6f + Vector3.up * 3f;
        for (int i = 0; i < 40; i++)
        {
            GameObject s = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.12f, H.Rainbow(1f, i / 40f), false);
            s.transform.position = centre;
            s.AddComponent<Rigidbody>().velocity = Random.onUnitSphere * Random.Range(4f, 8f);
            H.Track(s, 2.5f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Companion Ball", "A glowing ball floats along next to you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CompanionBall : MenuMod
{
    private GameObject ball;
    private Vector3    vel;

    public override void OnDisable()
    {
        if (ball != null) Object.Destroy(ball);
        ball = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        if (ball == null)
        {
            ball = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.18f, Color.white, false);
            ball.transform.position = H.Head.position;
        }

        Transform look = H.Look;
        Vector3 target = H.Head.position + look.right * 0.5f + look.forward * 0.4f
                         + Vector3.up * (0.2f + Mathf.Sin(Time.time * 2f) * 0.06f);
        ball.transform.position = Vector3.SmoothDamp(ball.transform.position, target, ref vel, 0.25f);
        ball.GetComponent<Renderer>().material.color = H.Rainbow(0.3f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Sparkler", "Hold right grip to throw sparks from your hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Sparkler : MenuMod
{
    private float next;

    public override string BindHint => "RG";
    public override void Update()
    {
        if (!H.Ready || !H.RGrip || Time.time < next) return;
        next = Time.time + 0.03f;
        GameObject s = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.05f, H.Rainbow(2f), false);
        s.transform.position = H.RHand.position;
        s.AddComponent<Rigidbody>().velocity = Random.onUnitSphere * 1.5f + Vector3.up;
        H.Track(s, 1.2f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Big Cube", "Drops a heavy 1.5 m cube in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BigCube : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 1.5f, H.Rainbow(1f));
        c.transform.position = H.Head.position + H.Look.forward * 2.5f;
        c.AddComponent<Rigidbody>().mass = 20f;
        H.Track(c, 30f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Domino Row", "Sets up a row of dominoes in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DominoRow : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 fwd = H.Look.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 start = Ground.Find(5f, out Vector3 floor) ? floor : H.Head.position - Vector3.up * 1.3f;
        start += fwd * 1.5f + Vector3.up * 0.26f;

        for (int i = 0; i < 12; i++)
        {
            GameObject d = H.Prim(PrimitiveType.Cube, new Vector3(0.35f, 0.5f, 0.08f), H.Rainbow(1f, i / 12f));
            d.transform.position = start + fwd * (i * 0.3f);
            d.transform.rotation = Quaternion.LookRotation(fwd);
            d.AddComponent<Rigidbody>();
            H.Track(d, 40f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Planet Ring", "Six glowing planets orbit your head (only you see them)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PlanetRing : RingBase
{
    protected override int           Count  => 6;
    protected override float         Radius => 0.9f;
    protected override float         Height => 0f;
    protected override float         Size   => 0.18f;
    protected override float         Spin   => 1.2f;
    protected override PrimitiveType Shape  => PrimitiveType.Sphere;
    protected override Color         Tint(int i) => H.Rainbow(0.3f, i / 6f);
}

[ModCategory(Cat.Fun)]
[ModInfo("Hula Hoop", "A rainbow hoop spins around your waist (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HulaHoop : RingBase
{
    protected override int           Count  => 28;
    protected override float         Radius => 0.45f;
    protected override float         Height => -0.65f;
    protected override float         Size   => 0.05f;
    protected override float         Spin   => 3f;
    protected override PrimitiveType Shape  => PrimitiveType.Sphere;
    protected override Color         Tint(int i) => H.Rainbow(0.5f, i / 28f);
}

// ============================== VISUAL ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Velocity Line", "A line in front of you that shows where you're heading and how fast", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class VelocityLine : MenuMod
{
    private LineRenderer line;

    public override void OnEnable()  => line = H.MakeLine("VelocityLine", Color.green, 0.02f);
    public override void OnDisable() { if (line != null) Object.Destroy(line.gameObject); }

    public override void Update()
    {
        if (!H.Ready || line == null) return;
        Vector3 o = H.Head.position + H.Look.forward * 0.5f - Vector3.up * 0.3f;
        Vector3 v = H.Rb.velocity;
        line.SetPosition(0, o);
        line.SetPosition(1, o + v * 0.15f);
        line.startColor = line.endColor = Color.Lerp(Color.green, Color.red, v.magnitude / 20f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Breathing Light", "Ambient light slowly gets brighter and darker", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BreathingLight : MenuMod
{
    private UnityEngine.Rendering.AmbientMode mode;
    private Color color;

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
        float k = Mathf.Lerp(0.2f, 1f, (Mathf.Sin(Time.time * 0.6f) + 1f) * 0.5f);
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(k, k, k);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Sunset Light", "Warm orange ambient light", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SunsetLight : MenuMod
{
    private UnityEngine.Rendering.AmbientMode mode;
    private Color color;

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
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(1f, 0.55f, 0.3f);
    }
}

// ============================== PHYSICS ==============================

[ModCategory(Cat.Physics)]
[ModInfo("Air Drag: ", "Air resistance slows you down the faster you go", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AirDrag : IncrementalMod
{
    private static readonly float[] Drag = [0f, 0.3f, 0.8f, 1.5f, 3f,];
    private static float orig = -1f;
    protected override string[] Labels => ["Off", "Light", "Medium", "Heavy", "Thick",];

    protected override void Changed() => Apply();
    public override void Update()     => Apply();

    private void Apply()
    {
        if (!H.Ready) return;
        if (orig < 0f) orig = H.Rb.drag;
        H.Rb.drag = IncrementalValue == 0 ? orig : Drag[IncrementalValue];
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Speed Limit: ", "Stops you from going faster than this", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SpeedLimit : IncrementalMod
{
    private static readonly float[] Limits = [0f, 6f, 10f, 15f, 25f,];
    protected override string[] Labels => ["Off", "6 m/s", "10 m/s", "15 m/s", "25 m/s",];

    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 v = H.Rb.velocity;
        float max = Limits[IncrementalValue];
        if (v.magnitude > max) H.Rb.velocity = v.normalized * max;
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Max Fall Speed: ", "You can't fall faster than this", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MaxFallSpeed : IncrementalMod
{
    private static readonly float[] Limits = [0f, 3f, 6f, 10f,];
    protected override string[] Labels => ["Off", "3 m/s", "6 m/s", "10 m/s",];

    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 v = H.Rb.velocity;
        float max = Limits[IncrementalValue];
        if (v.y < -max) { v.y = -max; H.Rb.velocity = v; }
    }
}
