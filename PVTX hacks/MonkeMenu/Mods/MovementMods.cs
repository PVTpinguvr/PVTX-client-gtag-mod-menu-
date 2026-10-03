using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== SPEED / JUMP ==============================
// These now re-apply every frame, so the game can't quietly undo them.

[ModCategory(Cat.Movement)]
[ModInfo("Speed Boost: ", "Multiplies your max jump/launch speed", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SpeedBoost : IncrementalMod
{
    private static readonly Tweak T = new();
    private static readonly float[] Mult = [1f, 1.2f, 1.5f, 2f, 3f, 5f,];
    protected override string[] Labels => ["Off", "1.2x", "1.5x", "2x", "3x", "5x",];

    protected override void Changed() => Apply();
    public override void Update()     => Apply();

    private void Apply()
    {
        if (!H.Ready) return;
        GTPlayer.Instance.maxJumpSpeed = T.Apply(GTPlayer.Instance.maxJumpSpeed, Mult[IncrementalValue]);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Jump Boost: ", "Multiplies how hard you launch off surfaces", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class JumpBoost : IncrementalMod
{
    private static readonly Tweak T = new();
    private static readonly float[] Mult = [1f, 1.25f, 1.5f, 2f, 3f, 4f,];
    protected override string[] Labels => ["Off", "1.25x", "1.5x", "2x", "3x", "4x",];

    protected override void Changed() => Apply();
    public override void Update()     => Apply();

    private void Apply()
    {
        if (!H.Ready) return;
        GTPlayer.Instance.jumpMultiplier = T.Apply(GTPlayer.Instance.jumpMultiplier, Mult[IncrementalValue]);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Fly Speed: ", "How fast all the fly mods go", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FlySpeedMod : IncrementalMod
{
    private static readonly float[] Speeds = [12f, 18f, 25f, 40f, 60f, 90f,];
    protected override string[] Labels => ["12", "18", "25", "40", "60", "90",];
    protected override void Changed() => H.FlySpeed = Speeds[IncrementalValue];
}

// ============================== FLIGHT ==============================

[ModCategory(Cat.Movement)]
[ModInfo("A Fly", "Hold A to fly where you're looking", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AFly : MenuMod
{
    public override string BindHint => "A";
    public override void Update()
    {
        if (H.Ready && H.A) H.Rb.velocity = H.Look.forward * H.FlySpeed;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Trigger Fly", "Hold right trigger to fly where you're looking", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TriggerFly : MenuMod
{
    public override string BindHint => "RT";
    public override void Update()
    {
        if (H.Ready && H.RTrig) H.Rb.velocity = H.Look.forward * H.FlySpeed;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Left Trigger Fly", "Hold left trigger to fly where you're looking", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LeftTriggerFly : MenuMod
{
    public override string BindHint => "LT";
    public override void Update()
    {
        if (H.Ready && H.LTrig) H.Rb.velocity = H.Look.forward * H.FlySpeed;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Joystick Fly", "Left stick flies, releasing it hovers in place", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class JoystickFly : MenuMod
{
    public override string BindHint => "LStick";
    public override void Update()
    {
        if (!H.Ready) return;
        Vector2 s = H.LStick;
        if (s.magnitude < 0.1f) { H.Rb.velocity = Vector3.zero; return; }
        Transform look = H.Look;
        H.Rb.velocity = (look.forward * s.y + look.right * s.x) * H.FlySpeed;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Up And Down", "Right stick up = rise, down = sink", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class UpAndDown : MenuMod
{
    public override string BindHint => "RStick";
    public override void Update()
    {
        if (!H.Ready) return;
        float y = H.RStick.y;
        if (Mathf.Abs(y) > 0.2f)
        {
            Vector3 v = H.Rb.velocity;
            v.y = y * H.FlySpeed;
            H.Rb.velocity = v;
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Jetpack", "Hold B to boost upward", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Jetpack : MenuMod
{
    public override string BindHint => "B";
    public override void FixedUpdate()
    {
        if (H.Ready && H.B) H.Rb.AddForce(Vector3.up * 28f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Rocket Hands", "Hold a trigger to blast away from where that hand points", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RocketHands : MenuMod
{
    public override string BindHint => "LT/RT";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (H.Trigger(left))
                H.Rb.AddForce(-H.Point(left) * 25f, ForceMode.Acceleration);
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Dash", "Tap A to dash forward", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Dash : MenuMod
{
    private bool was;
    public override string BindHint => "A";
    public override void Update()
    {
        if (!H.Ready) return;
        bool now = H.A;
        if (now && !was) H.Rb.AddForce(H.Look.forward * 12f, ForceMode.VelocityChange);
        was = now;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Freeze", "Hold left grip + left trigger to freeze in mid air", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Freeze : MenuMod
{
    public override string BindHint => "LG+LT";
    public override void Update()
    {
        if (H.Ready && H.LGrip && H.LTrig) H.Rb.velocity = Vector3.zero;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Hover", "Hold left grip to stay at your current height", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Hover : MenuMod
{
    public override string BindHint => "LG";
    public override void Update()
    {
        if (!H.Ready || !H.LGrip) return;
        Vector3 v = H.Rb.velocity;
        v.y = 0f;
        H.Rb.velocity = v;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Glide", "Hold right grip in the air to fall slowly", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Glide : MenuMod
{
    public override string BindHint => "RG";
    public override void Update()
    {
        if (!H.Ready || !H.RGrip) return;
        Vector3 v = H.Rb.velocity;
        if (v.y < -2f) { v.y = -2f; H.Rb.velocity = v; }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Fast Fall", "Push the left stick down to drop quickly", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FastFall : MenuMod
{
    public override string BindHint => "LStick";
    public override void Update()
    {
        if (!H.Ready || H.LStick.y > -0.6f) return;
        Vector3 v = H.Rb.velocity;
        v.y = Mathf.Min(v.y, -H.FlySpeed);
        H.Rb.velocity = v;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Air Brake", "Hold both grips to slow down fast", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AirBrake : MenuMod
{
    public override string BindHint => "LG+RG";
    public override void Update()
    {
        if (H.Ready && H.LGrip && H.RGrip) H.Rb.velocity *= 0.9f;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Auto Run", "Hold left stick forward to sprint where you look, keeps your height", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AutoRun : MenuMod
{
    public override string BindHint => "LStick";
    public override void Update()
    {
        if (!H.Ready || H.LStick.y < 0.5f) return;
        Vector3 f = H.Look.forward; f.y = 0f; f.Normalize();
        Vector3 v = H.Rb.velocity;
        H.Rb.velocity = new Vector3(f.x * H.FlySpeed * 0.6f, v.y, f.z * H.FlySpeed * 0.6f);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Slingshot", "Hold right grip to anchor, release to fling yourself toward the anchor", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Slingshot : MenuMod
{
    private bool    held;
    private Vector3 anchor;

    public override string BindHint => "RG";
    public override void Update()
    {
        if (!H.Ready) return;
        bool grip = H.RGrip;
        if (grip && !held) anchor = H.RHand.position;
        if (!grip && held)
        {
            Vector3 pull = anchor - H.RHand.position;
            H.Rb.AddForce(pull * 18f, ForceMode.VelocityChange);
        }
        held = grip;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Grapple", "Grip = shoot a rope at what that hand points at and get pulled to it", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Grapple : MenuMod
{
    private readonly bool[]         hooked = new bool[2];
    private readonly Vector3[]      anchor = new Vector3[2];
    private readonly LineRenderer[] ropes  = new LineRenderer[2];

    public override string BindHint => "Grip";
    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) Release(i);
    }

    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (!H.Grip(left)) { Release(i); continue; }

            if (!hooked[i])
            {
                if (!Physics.Raycast(H.Hand(left).position, H.Point(left), out RaycastHit hit, 60f)) continue;
                hooked[i] = true;
                anchor[i] = hit.point;
                ropes[i]  = H.MakeLine("Rope" + i, Color.white, 0.015f);
            }

            ropes[i].SetPosition(0, H.Hand(left).position);
            ropes[i].SetPosition(1, anchor[i]);
        }
    }

    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            if (!hooked[i]) continue;
            Vector3 dir = (anchor[i] - H.Head.position).normalized;
            H.Rb.AddForce(dir * 35f, ForceMode.Acceleration);
            H.Rb.AddForce(Vector3.up * 9.81f * 0.5f, ForceMode.Acceleration);
        }
    }

    private void Release(int i)
    {
        hooked[i] = false;
        if (ropes[i] != null) Object.Destroy(ropes[i].gameObject);
        ropes[i] = null;
    }
}

// ============================== WALL / SURFACE ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Wall Walk", "Hold grip with your palm near a surface to stick to it", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class WallWalk : MenuMod
{
    public override string BindHint => "Grip";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (!H.Grip(left)) continue;
            if (Physics.Raycast(H.Hand(left).position, H.Palm(left), out RaycastHit hit, 0.4f))
            {
                H.Rb.AddForce(-hit.normal * 20f, ForceMode.Acceleration);
                H.Rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration); // cancel gravity while stuck
            }
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Spider Monke", "Grip anywhere in range of a surface to get pulled to it", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SpiderMonke : MenuMod
{
    public override string BindHint => "Grip";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (!H.Grip(left)) continue;
            if (Physics.Raycast(H.Hand(left).position, H.Point(left), out RaycastHit hit, 6f))
            {
                Vector3 dir = (hit.point - H.Head.position).normalized;
                H.Rb.AddForce(dir * 30f, ForceMode.Acceleration);
                H.Rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);
            }
        }
    }
}

// ============================== NOCLIP ==============================

[ModCategory(Cat.Movement)]
[ModInfo("NoClip", "Hold right trigger to phase through the map", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoClip : MenuMod
{
    private readonly List<Collider>    cached = [];
    private readonly HashSet<Collider> known  = [];
    private bool  off;
    private float nextScan;

    public override string BindHint => "RT";
    public override void OnEnable() => Scan();

    public override void OnDisable()
    {
        SetColliders(true);
        cached.Clear();
        known.Clear();
    }

    // Look for colliders again every few seconds (new areas load in as you move around).
    private void Scan()
    {
        if (!H.Ready) return;
        Transform self = GTPlayer.Instance.transform.root;
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || c.transform.IsChildOf(self)) continue;
            if (!(c is MeshCollider || c is BoxCollider)) continue;
            if (!c.enabled && !known.Contains(c)) continue; // disabled by the game, not by us
            if (known.Add(c)) cached.Add(c);
        }
    }

    public override void Update()
    {
        bool want = H.Ready && H.RTrig;
        if (want && Time.time > nextScan) { nextScan = Time.time + 3f; Scan(); }
        if (want == off) return;
        off = want;
        SetColliders(!want);
    }

    private void SetColliders(bool state)
    {
        foreach (Collider c in cached)
            if (c != null) c.enabled = state;
        if (state) off = false;
    }
}

// ============================== WALL NOCLIP ==============================
// Walk through walls / vertical surfaces, but keep floors and ground solid.

[ModCategory(Cat.Movement)]
[ModInfo("Wall NoClip", "Phase through walls and vertical surfaces. Floors and ground stay solid so you don't fall", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class WallNoClip : MenuMod
{
    private readonly List<Collider>    disabled = [];
    private readonly HashSet<Collider> known    = [];
    private float nextScan;

    private static readonly string[] GroundWords =
    [
        "floor", "ground", "terrain", "platform", "surface", "mesh_road", "road", "path",
        "grass", "dirt", "concrete_floor", "ceiling", // ceilings kept so you don't pop up through them by accident? actually user wants walls only - keep floors
    ];

    public override void OnEnable()
    {
        nextScan = 0f;
        Scan();
    }

    public override void OnDisable()
    {
        foreach (Collider c in disabled)
            if (c != null) c.enabled = true;
        disabled.Clear();
        known.Clear();
    }

    public override void Update()
    {
        if (!H.Ready || Time.time < nextScan) return;
        nextScan = Time.time + 2f;
        Scan();
    }

    private void Scan()
    {
        if (!H.Ready) return;
        Transform self   = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance != null ? GorillaTagger.Instance.transform.root : null;

        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || known.Contains(c)) continue;
            if (!c.enabled) continue;
            Transform t = c.transform;
            if (t.IsChildOf(self)) continue;
            if (tagger != null && t.IsChildOf(tagger)) continue;
            if (c.GetComponentInParent<KeepSolid>() != null) continue;
            if (IsGround(c)) continue; // leave floors alone

            known.Add(c);
            disabled.Add(c);
            c.enabled = false;
        }
    }

    /// <summary>
    /// Heuristic: flat / mostly-horizontal colliders and anything named like a floor stay solid.
    /// Tall vertical-ish colliders are treated as walls and get disabled.
    /// </summary>
    private static bool IsGround(Collider c)
    {
        string n = c.gameObject.name.ToLowerInvariant();
        string p = c.transform.parent != null ? c.transform.parent.name.ToLowerInvariant() : "";
        foreach (string w in GroundWords)
            if (n.Contains(w) || p.Contains(w)) return true;

        Bounds b = c.bounds;
        float y  = b.size.y;
        float xz = Mathf.Max(b.size.x, b.size.z);

        // Wide and not tall → floor / platform
        if (y < 1.25f && xz > y * 1.8f) return true;

        // Thin horizontal box (common floor slabs)
        if (c is BoxCollider box)
        {
            Vector3 s = Vector3.Scale(box.size, c.transform.lossyScale);
            float by = Mathf.Abs(s.y), bx = Mathf.Abs(s.x), bz = Mathf.Abs(s.z);
            if (by < 0.6f && Mathf.Max(bx, bz) > by * 2f) return true;
        }

        // Mesh under the player that supports upward normals: sample a few points
        // If a ray from above hits this collider with a steep upward normal, treat as ground.
        Vector3 center = b.center;
        if (Physics.Raycast(center + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider == c && hit.normal.y > 0.55f)
            return true;

        return false;
    }
}

// ============================== TELEPORT ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Checkpoint", "X = set checkpoint, Y = teleport back (X+Y together opens the menu)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Checkpoint : MenuMod
{
    private bool       wasX, wasY;
    private GameObject marker;

    public override string BindHint => "X/Y";
    public override void OnDisable()
    {
        if (marker != null) Object.Destroy(marker);
        marker = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        if (H.X && !H.Y && !wasX)
        {
            H.CheckpointPos = H.Head.position; H.HasCheckpoint = true;
            if (marker == null) marker = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.25f, Color.green, false);
            marker.transform.position = H.CheckpointPos;
        }
        if (H.Y && !H.X && !wasY && H.HasCheckpoint) H.Teleport(H.CheckpointPos);
        wasX = H.X; wasY = H.Y;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Set Checkpoint", "Remember where you're standing", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SetCheckpoint : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        H.CheckpointPos = H.Head.position;
        H.HasCheckpoint = true;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Go To Checkpoint", "Teleport back to your checkpoint", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GoCheckpoint : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready && H.HasCheckpoint) H.Teleport(H.CheckpointPos);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Return To Spawn", "Teleport back to where you started this session", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ReturnToSpawn : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Teleport(H.SpawnPos);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Teleport Up 20m", "Go straight up", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TeleportUp : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Teleport(H.Head.position + Vector3.up * 20f);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Teleport Down 20m", "Go straight down", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TeleportDown : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Teleport(H.Head.position + Vector3.down * 20f);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Stop Moving", "Kills all your speed right now", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class StopMoving : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Rb.velocity = Vector3.zero;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Teleport Gun", "Right grip to aim, right trigger to teleport to the dot", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TeleportGun : MenuMod
{
    private GameObject dot;
    private LineRenderer line;

    public override string BindHint => "RG+RT";
    public override void OnDisable() => Cleanup();

    public override void Update()
    {
        if (!H.Ready) return;
        if (!H.RGrip) { Cleanup(); return; }

        if (dot == null)
        {
            dot = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.15f, Color.magenta, false);
            line = H.MakeLine("TeleportLine", Color.magenta, 0.01f);
        }

        Vector3 origin = H.RHand.position, dir = H.Point(false);
        Vector3 target = Physics.Raycast(origin, dir, out RaycastHit hit, 200f) ? hit.point : origin + dir * 200f;
        dot.transform.position = target;
        line.SetPosition(0, origin);
        line.SetPosition(1, target);

        if (H.RTrig) H.Teleport(target + Vector3.up * 0.8f);
    }

    private void Cleanup()
    {
        if (dot != null) Object.Destroy(dot);
        if (line != null) Object.Destroy(line.gameObject);
        dot = null; line = null;
    }
}

// ============================== PLATFORMS ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Platforms", "Grip spawns a solid platform under that hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Platforms : PlatformBase { }

[ModCategory(Cat.Movement)]
[ModInfo("Invisible Platforms", "Same as Platforms but you can't see them", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class InvisiblePlatforms : PlatformBase
{
    protected override bool Visible => false;
}

[ModCategory(Cat.Movement)]
[ModInfo("Rainbow Platforms", "Platforms that cycle through colours", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RainbowPlatforms : PlatformBase
{
    protected override Color Tint(int i) => H.Rainbow(0.5f, i * 0.5f);
}

// ============================== PHYSICS ==============================

[ModCategory(Cat.Physics)]
[ModInfo("Gravity: ", "Pick how heavy or floaty you are (Inverted pulls you upward)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GravityMod : IncrementalMod
{
    // 1 = normal. 0 = no gravity. Negative = falls upward.
    private static readonly float[] Strength = [1f, 0.7f, 0.4f, 0f, -0.5f, 1.5f, 2.5f,];
    protected override string[] Labels => ["Normal", "Low", "Very Low", "Zero", "Inverted", "High", "Very High",];

    public override void FixedUpdate()
    {
        if (H.Ready) H.Rb.AddForce(Vector3.up * 9.81f * (1f - Strength[IncrementalValue]), ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Low Gravity", "Moon-style gravity", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LowGravity : MenuMod
{
    public override void FixedUpdate()
    {
        if (H.Ready) H.Rb.AddForce(Vector3.up * 9.81f * 0.7f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Zero Gravity", "Float forever", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ZeroGravity : MenuMod
{
    public override void FixedUpdate()
    {
        if (H.Ready) H.Rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("High Gravity", "Heavy monke", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HighGravity : MenuMod
{
    public override void FixedUpdate()
    {
        if (H.Ready) H.Rb.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Wind: ", "A steady wind pushes you the way you were facing", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class WindMod : IncrementalMod
{
    private static readonly float[] Force = [0f, 3f, 7f, 14f,];
    protected override string[] Labels => ["Off", "Breeze", "Strong", "Storm",];

    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        Vector3 f = H.Look.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) return;
        H.Rb.AddForce(f.normalized * Force[IncrementalValue], ForceMode.Acceleration);
    }
}

// ============================== PLATFORMS (ii-style) ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Platforms", "Hold grip to spawn a platform under each hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Platforms : MenuMod
{
    private readonly GameObject[] plats = new GameObject[2];
    public override string BindHint => "Grip";

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++)
        {
            if (plats[i] != null) Object.Destroy(plats[i]);
            plats[i] = null;
        }
    }

    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (H.Grip(left))
            {
                if (plats[i] == null)
                {
                    plats[i] = H.Prim(PrimitiveType.Cube, new Vector3(0.4f, 0.06f, 0.4f), new Color(0.6f, 0.3f, 1f));
                    plats[i].AddComponent<KeepSolid>();
                }
                Transform hand = H.Hand(left);
                plats[i].transform.position = hand.position + Vector3.down * 0.05f;
                plats[i].transform.rotation = Quaternion.Euler(0f, hand.eulerAngles.y, 0f);
            }
            else if (plats[i] != null)
            {
                Object.Destroy(plats[i]);
                plats[i] = null;
            }
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Trigger Platforms", "Hold trigger to spawn a platform under each hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TriggerPlatforms : MenuMod
{
    private readonly GameObject[] plats = new GameObject[2];
    public override string BindHint => "LT/RT";

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++)
        {
            if (plats[i] != null) Object.Destroy(plats[i]);
            plats[i] = null;
        }
    }

    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            bool left = i == 0;
            if (H.Trigger(left))
            {
                if (plats[i] == null)
                {
                    plats[i] = H.Prim(PrimitiveType.Cube, new Vector3(0.4f, 0.06f, 0.4f), new Color(0.2f, 0.8f, 1f));
                    plats[i].AddComponent<KeepSolid>();
                }
                Transform hand = H.Hand(left);
                plats[i].transform.position = hand.position + Vector3.down * 0.05f;
                plats[i].transform.rotation = Quaternion.Euler(0f, hand.eulerAngles.y, 0f);
            }
            else if (plats[i] != null)
            {
                Object.Destroy(plats[i]);
                plats[i] = null;
            }
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Iron Man", "Hold both grips to blast in the direction of your hands", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IronMan : MenuMod
{
    public override string BindHint => "LG+RG";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        if (!(H.LGrip && H.RGrip)) return;
        Vector3 force = (H.Point(true) + H.Point(false)).normalized;
        H.Rb.AddForce(force * 35f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Frozone", "Hold grip while sliding to keep ice-like speed on the ground", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Frozone : MenuMod
{
    public override string BindHint => "Grip";
    public override void Update()
    {
        if (!H.Ready) return;
        if (!(H.LGrip || H.RGrip)) return;
        if (!Ground.Near(1.8f)) return;
        Vector3 v = H.Rb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        if (flat.magnitude < 2f)
        {
            Vector3 f = H.Look.forward; f.y = 0f; f.Normalize();
            H.Rb.velocity = new Vector3(f.x * 8f, v.y, f.z * 8f);
        }
        else
            H.Rb.velocity = new Vector3(v.x * 1.02f, v.y, v.z * 1.02f);
    }
}
