using System;
using System.Collections.Generic;
using System.Reflection;
using GorillaLocomotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== ADVANTAGE HELPERS ==============================
// Soft reflection helpers so a fan-game rename doesn't stop the menu compiling.

static class Adv
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static Type RigType => Net.Find("VRRig") ?? Net.Find("GorillaTag.VRRig");

    /// <summary>Works on older Unity versions that lack Object.FindObjectsByType(Type, ...).</summary>
    private static Object[] FindAllOfType(Type t)
    {
        if (t == null) return Array.Empty<Object>();
        try { return Resources.FindObjectsOfTypeAll(t); }
        catch { return Array.Empty<Object>(); }
    }

    /// <summary>Every VRRig in the scene that isn't ours.</summary>
    public static List<Component> OtherRigs()
    {
        List<Component> list = [];
        Type t = RigType;
        if (t == null || !H.Ready) return list;

        Object[] found = FindAllOfType(t);
        Transform self = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance != null ? GorillaTagger.Instance.transform.root : null;

        foreach (Object o in found)
        {
            if (o is not Component c || c == null) continue;
            // skip assets / prefabs not in a real scene
            if (c.gameObject.scene.name == null || !c.gameObject.scene.IsValid()) continue;
            if (c.transform.IsChildOf(self)) continue;
            if (tagger != null && c.transform.IsChildOf(tagger)) continue;
            string n = c.name;
            if (n.IndexOf("Offline", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            list.Add(c);
        }
        return list;
    }

    public static Vector3 RigHead(Component rig)
    {
        if (rig == null) return Vector3.zero;
        // Prefer a head transform field if the game exposes one
        foreach (string name in new[] { "headMesh", "head", "Head", "mainCamera", "headCollider" })
        {
            FieldInfo f = rig.GetType().GetField(name, Any);
            if (f != null)
            {
                object v = f.GetValue(rig);
                if (v is Transform tr) return tr.position;
                if (v is Component co) return co.transform.position;
            }
            PropertyInfo p = rig.GetType().GetProperty(name, Any);
            if (p != null)
            {
                object v = p.GetValue(rig);
                if (v is Transform tr) return tr.position;
                if (v is Component co) return co.transform.position;
            }
        }
        // Fallback: top of the bounds / a bit above the root
        Renderer r = rig.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.center + Vector3.up * (r.bounds.extents.y * 0.6f);
        return rig.transform.position + Vector3.up * 0.6f;
    }

    public static bool TryTag(Component rig)
    {
        if (rig == null) return false;
        Type t = rig.GetType();

        // Common method names across GTAG forks / fan games
        foreach (string m in new[]
                 {
                     "TagPlayer", "TaggedBy", "SetTagged", "Tag", "OnTagged", "RequestTag",
                     "RPC_TagPlayer", "LocalTag", "BecomeTagged",
                 })
        {
            MethodInfo mi = t.GetMethod(m, Any);
            if (mi == null) continue;
            try
            {
                ParameterInfo[] ps = mi.GetParameters();
                if (ps.Length == 0) { mi.Invoke(rig, null); return true; }
                if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
                { mi.Invoke(rig, new object[] { true }); return true; }
                if (ps.Length == 1 && typeof(Component).IsAssignableFrom(ps[0].ParameterType))
                { mi.Invoke(rig, new object[] { rig }); return true; }
            }
            catch { /* try next */ }
        }

        // Try a tagged bool field
        foreach (string f in new[] { "isTagged", "tagged", "infected", "isInfected" })
        {
            FieldInfo fi = t.GetField(f, Any);
            if (fi != null && fi.FieldType == typeof(bool))
            {
                try { fi.SetValue(rig, true); return true; } catch { }
            }
            PropertyInfo pi = t.GetProperty(f, Any);
            if (pi != null && pi.CanWrite && pi.PropertyType == typeof(bool))
            {
                try { pi.SetValue(rig, true); return true; } catch { }
            }
        }
        return false;
    }

    public static void SetRenderers(Component root, bool on)
    {
        if (root == null) return;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            // keep particle systems optional; disable mesh/skinned
            if (r is ParticleSystemRenderer) continue;
            r.enabled = on;
        }
    }

    public static Component LocalRig()
    {
        Type t = RigType;
        if (t == null || !H.Ready) return null;
        Transform self = GTPlayer.Instance.transform.root;
        foreach (Object o in FindAllOfType(t))
        {
            if (o is Component c && c != null && c.gameObject.scene.IsValid() && c.transform.IsChildOf(self))
                return c;
        }
        // offlineVRRig field on GorillaTagger
        try
        {
            FieldInfo f = typeof(GorillaTagger).GetField("offlineVRRig", Any);
            if (f != null && f.GetValue(GorillaTagger.Instance) is Component c) return c;
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Teleport local player onto target for one physics step, attempt tag, restore pose.
    /// This is the classic "tp tag gun" approach so tag colliders actually overlap.
    /// </summary>
    public static void FlickTagAt(Component target)
    {
        if (target == null || !H.Ready) return;
        Transform root = GTPlayer.Instance.transform;
        Rigidbody rb = H.Rb;
        Vector3 pos = root.position;
        Quaternion rot = root.rotation;
        Vector3 vel = rb != null ? rb.velocity : Vector3.zero;
        Vector3 aim = RigHead(target);
        // TP your whole rig onto them so tag colliders overlap, tag, then TP back.
        Vector3[] offsets =
        {
            Vector3.up * 0.12f,
            Vector3.zero,
            Vector3.up * 0.05f + Vector3.forward * 0.08f,
            -Vector3.up * 0.05f,
        };
        foreach (Vector3 off in offsets)
        {
            Vector3 p = aim + off;
            root.position = p;
            if (rb != null) { rb.position = p; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            TryTag(target);
            try
            {
                if (GorillaTagger.Instance != null)
                {
                    foreach (string name in new[] { "TagPlayer", "TryToTag", "AttemptTag", "Tag" })
                    {
                        var mi = typeof(GorillaTagger).GetMethod(name, Any);
                        if (mi == null) continue;
                        var ps = mi.GetParameters();
                        if (ps.Length == 0) mi.Invoke(GorillaTagger.Instance, null);
                        else if (ps.Length == 1) mi.Invoke(GorillaTagger.Instance, new object[] { target });
                    }
                }
            }
            catch { }
        }
        // restore pose
        root.position = pos;
        root.rotation = rot;
        if (rb != null) { rb.position = pos; rb.rotation = rot; rb.velocity = vel; rb.angularVelocity = Vector3.zero; }
    }
}

// ============================== TAG GUN ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Gun", "Gun on right hand. Aim + RT: TP to them, tag, TP back", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagGun : MenuMod
{
    private GameObject   gun, dot;
    private LineRenderer line;
    private Component    locked;
    private float        nextShot;

    public override string BindHint => "RT";

    public override void OnEnable()
    {
        if (gun == null) gun = H.MakeGun(new Color(0.85f, 0.15f, 0.15f));
        if (dot == null) dot = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.1f, Color.red, false);
        if (line == null) line = H.MakeLine("TagGunLine", Color.red, 0.01f);
    }

    public override void OnDisable()
    {
        Cleanup();
        locked = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        if (gun == null) OnEnable();

        H.AttachGun(gun, false);
        Vector3 origin = H.RHand.position + H.Point(false) * 0.15f;
        Vector3 dir    = H.Point(false);
        Vector3 target = origin + dir * 80f;
        locked = null;

        float best = float.MaxValue;
        foreach (Component rig in Adv.OtherRigs())
        {
            Vector3 head = Adv.RigHead(rig);
            Vector3 to   = head - origin;
            float dist   = to.magnitude;
            if (dist < 0.3f || dist > 80f) continue;
            float ang = Vector3.Angle(dir, to);
            if (ang > 14f) continue;
            float score = ang + dist * 0.02f;
            if (score < best) { best = score; locked = rig; target = head; }
        }

        if (locked == null && Physics.Raycast(origin, dir, out RaycastHit hit, 80f))
            target = hit.point;

        Color col = locked != null ? Color.green : Color.red;
        if (dot != null)
        {
            dot.transform.position = target;
            dot.transform.localScale = Vector3.one * (locked != null ? 0.16f : 0.1f);
            Renderer r = dot.GetComponent<Renderer>();
            if (r != null) r.material.color = col;
        }
        if (line != null)
        {
            line.startColor = line.endColor = col;
            line.SetPosition(0, origin);
            line.SetPosition(1, target);
        }

        if (H.RTrig && locked != null && Time.time >= nextShot)
        {
            nextShot = Time.time + 0.15f;
            // Flick-tag: TP your rig onto the target for one frame, tag, TP back
            Adv.FlickTagAt(locked);
            SoundBoard.PlayMenuClick();
        }
    }

    private void Cleanup()
    {
        if (gun != null) Object.Destroy(gun);
        if (dot != null) Object.Destroy(dot);
        if (line != null) Object.Destroy(line.gameObject);
        gun = null; dot = null; line = null;
    }
}

// ============================== TAG NEARBY ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Nearby", "Hold right trigger to tag every player within 3 metres", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagNearby : MenuMod
{
    private float next;
    public override string BindHint => "RT";

    public override void Update()
    {
        if (!H.Ready || !H.RTrig || Time.time < next) return;
        next = Time.time + 0.25f;
        Vector3 me = H.Head.position;
        foreach (Component rig in Adv.OtherRigs())
            if (Vector3.Distance(me, Adv.RigHead(rig)) < 3f)
                Adv.TryTag(rig);
    }
}

// ============================== INVIS ==============================
// Enable the mod from the menu, then press B once to delete/hide your avatar.
// Press B again to bring it back. (Others may still see you on some networked builds.)

[ModCategory(Cat.Advantage)]
[ModInfo("Invis", "Enable then press B to delete your avatar. Press B again to restore", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Invis : MenuMod
{
    private static readonly List<(Renderer r, bool was)> saved = [];
    private bool avatarGone;
    private bool wasB;

    public override string BindHint => "B";

    public override void OnEnable()
    {
        avatarGone = false;
        wasB = false;
    }

    public override void OnDisable()
    {
        Restore();
        avatarGone = false;
        wasB = false;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        bool b = H.B;
        if (b && !wasB)
        {
            avatarGone = !avatarGone;
            if (avatarGone) HideAvatar();
            else Restore();
            SoundBoard.PlayMenuClick();
        }
        wasB = b;

        if (avatarGone)
        {
            // keep forced hidden every frame in case the game re-enables meshes
            foreach (var (r, _) in saved)
                if (r != null) r.enabled = false;
            if (saved.Count == 0) HideAvatar();
        }
    }

    private static void HideAvatar()
    {
        Restore();
        void HideUnder(Component root)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                saved.Add((r, r.enabled));
                r.enabled = false;
            }
        }
        Component local = Adv.LocalRig();
        HideUnder(local);
        HideUnder(GTPlayer.Instance);
        if (GorillaTagger.Instance != null) HideUnder(GorillaTagger.Instance);
        try
        {
            Type t = Adv.RigType;
            if (t != null)
            {
                foreach (Object o in Resources.FindObjectsOfTypeAll(t))
                {
                    if (o is not Component c || c == null) continue;
                    if (!c.gameObject.scene.IsValid()) continue;
                    if (c.transform.IsChildOf(GTPlayer.Instance.transform.root))
                        HideUnder(c);
                }
            }
        }
        catch { }
    }

    private static void Restore()
    {
        foreach (var (r, was) in saved)
            if (r != null) r.enabled = was;
        saved.Clear();
    }
}

// ============================== GHOST ==============================
// Enable from menu, press B to delete avatar + phase through players.
// While avatar is deleted, Wall NoClip is also applied (walls only — some walls may still block).

[ModCategory(Cat.Advantage)]
[ModInfo("Ghost", "Enable then press B to delete avatar + wall noclip. B again restores", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GhostMode : MenuMod
{
    private readonly List<Collider> changed = [];
    private readonly List<Collider> wallOff = [];
    private readonly HashSet<Collider> wallKnown = [];
    private bool avatarGone;
    private bool wasB;
    private float nextWallScan;

    private static readonly string[] GroundWords =
    [
        "floor", "ground", "terrain", "platform", "surface", "mesh_road", "road", "path",
        "grass", "dirt", "concrete_floor", "ceiling",
    ];

    public override string BindHint => "B";

    public override void OnEnable()
    {
        avatarGone = false;
        wasB = false;
        nextWallScan = 0f;
    }

    public override void OnDisable()
    {
        RestoreAll();
        avatarGone = false;
        wasB = false;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        bool b = H.B;
        if (b && !wasB)
        {
            avatarGone = !avatarGone;
            if (avatarGone) ApplyGhost();
            else RestoreAll();
            SoundBoard.PlayMenuClick();
        }
        wasB = b;

        if (avatarGone)
        {
            // keep mesh hidden
            Component local = Adv.LocalRig();
            if (local != null) Adv.SetRenderers(local, false);
            else
            {
                Adv.SetRenderers(GTPlayer.Instance, false);
                if (GorillaTagger.Instance != null) Adv.SetRenderers(GorillaTagger.Instance, false);
            }
            // refresh player colliders if game re-enabled them
            if (changed.Count == 0) SoftDisablePlayerColliders();
            // wall noclip scan
            if (Time.time >= nextWallScan)
            {
                nextWallScan = Time.time + 1.5f;
                ScanWalls();
            }
        }
    }

    private void ApplyGhost()
    {
        RestoreAll();
        Component local = Adv.LocalRig();
        if (local != null) Adv.SetRenderers(local, false);
        else
        {
            Adv.SetRenderers(GTPlayer.Instance, false);
            if (GorillaTagger.Instance != null) Adv.SetRenderers(GorillaTagger.Instance, false);
        }
        SoftDisablePlayerColliders();
        ScanWalls();
    }

    private void SoftDisablePlayerColliders()
    {
        foreach (Collider c in GTPlayer.Instance.GetComponentsInChildren<Collider>(true))
        {
            if (c == null || c.isTrigger) continue;
            if (!c.enabled) continue;
            c.enabled = false;
            changed.Add(c);
        }
    }

    private void ScanWalls()
    {
        Transform self = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance != null ? GorillaTagger.Instance.transform.root : null;
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || wallKnown.Contains(c)) continue;
            if (!c.enabled) continue;
            Transform t = c.transform;
            if (t.IsChildOf(self)) continue;
            if (tagger != null && t.IsChildOf(tagger)) continue;
            if (c.GetComponentInParent<KeepSolid>() != null) continue;
            if (IsGround(c)) continue;
            wallKnown.Add(c);
            wallOff.Add(c);
            c.enabled = false;
        }
    }

    private static bool IsGround(Collider c)
    {
        string n = c.gameObject.name.ToLowerInvariant();
        string p = c.transform.parent != null ? c.transform.parent.name.ToLowerInvariant() : "";
        foreach (string w in GroundWords)
            if (n.Contains(w) || p.Contains(w)) return true;
        Bounds b = c.bounds;
        float y = b.size.y;
        float xz = Mathf.Max(b.size.x, b.size.z);
        if (y < 1.25f && xz > y * 1.8f) return true;
        if (c is BoxCollider box)
        {
            Vector3 s = Vector3.Scale(box.size, c.transform.lossyScale);
            float by = Mathf.Abs(s.y), bx = Mathf.Abs(s.x), bz = Mathf.Abs(s.z);
            if (by < 0.6f && Mathf.Max(bx, bz) > by * 2f) return true;
        }
        Vector3 center = b.center;
        if (Physics.Raycast(center + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider == c && hit.normal.y > 0.55f)
            return true;
        return false;
    }

    private void RestoreAll()
    {
        foreach (Collider c in changed)
            if (c != null) c.enabled = true;
        changed.Clear();
        foreach (Collider c in wallOff)
            if (c != null) c.enabled = true;
        wallOff.Clear();
        wallKnown.Clear();
        if (H.Ready)
        {
            Component local = Adv.LocalRig();
            if (local != null) Adv.SetRenderers(local, true);
            else
            {
                Adv.SetRenderers(GTPlayer.Instance, true);
                if (GorillaTagger.Instance != null) Adv.SetRenderers(GorillaTagger.Instance, true);
            }
        }
    }
}

// ============================== ESP ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Player ESP", "Draws a line from your hand to every other player's head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PlayerEsp : MenuMod
{
    private readonly List<LineRenderer> lines = [];

    public override void OnDisable()
    {
        foreach (LineRenderer l in lines)
            if (l != null) Object.Destroy(l.gameObject);
        lines.Clear();
    }

    public override void Update()
    {
        if (!H.Ready) return;
        List<Component> rigs = Adv.OtherRigs();

        while (lines.Count < rigs.Count)
            lines.Add(H.MakeLine("ESP", new Color(1f, 0.2f, 0.2f, 0.85f), 0.01f));
        while (lines.Count > rigs.Count)
        {
            int last = lines.Count - 1;
            if (lines[last] != null) Object.Destroy(lines[last].gameObject);
            lines.RemoveAt(last);
        }

        Vector3 from = H.Head.position;
        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 to = Adv.RigHead(rigs[i]);
            lines[i].SetPosition(0, from);
            lines[i].SetPosition(1, to);
            float d = Vector3.Distance(from, to);
            // closer = greener
            lines[i].startColor = lines[i].endColor = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(d / 40f));
        }
    }
}

// ============================== NO TAG / IMMUNITY ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("No Tag", "Turns off your body/hand tag triggers so others can't tag you as easily", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoTag : MenuMod
{
    private readonly List<(Collider col, bool wasTrigger)> saved = [];

    public override void OnEnable()  => Apply(true);
    public override void OnDisable() => Restore();
    public override void Update()    { if (saved.Count == 0) Apply(true); }

    private void Apply(bool on)
    {
        if (!H.Ready) return;
        Restore();
        foreach (Collider c in GTPlayer.Instance.GetComponentsInChildren<Collider>(true))
        {
            if (c == null) continue;
            string n = c.name.ToLowerInvariant();
            // typical tag sensor names
            if (n.Contains("tag") || n.Contains("infect") || c.isTrigger)
            {
                saved.Add((c, c.isTrigger));
                if (on) c.enabled = false;
            }
        }
        // also hands on GorillaTagger
        if (GorillaTagger.Instance != null)
        {
            foreach (Collider c in GorillaTagger.Instance.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                string n = c.name.ToLowerInvariant();
                if (n.Contains("tag") || n.Contains("hand") || c.isTrigger)
                {
                    saved.Add((c, c.isTrigger));
                    if (on) c.enabled = false;
                }
            }
        }
    }

    private void Restore()
    {
        foreach ((Collider col, bool wasTrigger) in saved)
            if (col != null) { col.enabled = true; col.isTrigger = wasTrigger; }
        saved.Clear();
    }
}

// ============================== LONG ARMS ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Long Arms: ", "Stretches how far your hands reach from your body", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LongArms : IncrementalMod
{
    private static readonly float[] Mult = [1f, 1.15f, 1.3f, 1.5f, 1.8f, 2.2f,];
    protected override string[] Labels => ["Off", "1.15x", "1.3x", "1.5x", "1.8x", "2.2x",];

    public override void LateUpdate()
    {
        if (!H.Ready || IncrementalValue == 0) return;
        float m = Mult[IncrementalValue];
        if (Mathf.Approximately(m, 1f)) return;

        // Extend each hand further from the body/head along the current head→hand vector.
        // Done in LateUpdate so we stretch after the game placed the hands this frame.
        Vector3 head = H.Head.position;
        Stretch(H.LHand, head, m);
        Stretch(H.RHand, head, m);
    }

    private static void Stretch(Transform hand, Vector3 head, float m)
    {
        if (hand == null) return;
        Vector3 offset = hand.position - head;
        if (offset.sqrMagnitude < 0.0001f) return;
        hand.position = head + offset * m;
    }
}

// ============================== TINY / BIG ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Size: ", "Scales your monke body (local visual + locomotion scale when possible)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SizeMod : IncrementalMod
{
    private static readonly float[] Scales = [1f, 0.5f, 0.7f, 1.3f, 1.6f, 2f,];
    protected override string[] Labels => ["Normal", "Tiny", "Small", "Big", "Huge", "Giant",];

    private Vector3 origScale = Vector3.one;
    private bool    has;

    protected override void Changed() => Apply();
    public override void OnDisable()  => Restore();
    public override void Update()     { if (IncrementalValue != 0) Apply(); }

    private void Apply()
    {
        if (!H.Ready) return;
        Transform root = GTPlayer.Instance.transform;
        if (!has) { origScale = root.localScale; has = true; }
        float s = Scales[IncrementalValue];
        root.localScale = origScale * s;

        // try scale factor on GTPlayer if the game uses one
        try
        {
            foreach (string n in new[] { "scale", "playerScale", "locomotionScale", "nativeScale" })
            {
                FieldInfo f = typeof(GTPlayer).GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.FieldType == typeof(float))
                {
                    f.SetValue(GTPlayer.Instance, s);
                    break;
                }
            }
        }
        catch { }
    }

    private void Restore()
    {
        if (!has || GTPlayer.Instance == null) return;
        GTPlayer.Instance.transform.localScale = origScale;
        has = false;
    }
}

// ============================== HITBOX EXPAND ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Big Hitbox", "Enlarges your hand colliders so tagging is easier", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BigHitbox : MenuMod
{
    private readonly List<(Transform t, Vector3 scale)> saved = [];

    public override void OnEnable()  => Apply();
    public override void OnDisable() => Restore();
    public override void Update()    { if (saved.Count == 0) Apply(); }

    private void Apply()
    {
        if (!H.Ready) return;
        Restore();
        foreach (Transform hand in new[] { H.LHand, H.RHand })
        {
            if (hand == null) continue;
            foreach (Collider c in hand.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                Transform t = c.transform;
                saved.Add((t, t.localScale));
                t.localScale = t.localScale * 2.5f;
            }
        }
    }

    private void Restore()
    {
        foreach ((Transform t, Vector3 scale) in saved)
            if (t != null) t.localScale = scale;
        saved.Clear();
    }
}

// ============================== PULL GUN ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Pull Gun", "Gun on your right hand. Hold right trigger to yank the aimed player toward you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PullGun : MenuMod
{
    private GameObject   gun, dot;
    private LineRenderer line;
    private Component    locked;

    public override string BindHint => "RT";

    public override void OnEnable()
    {
        if (gun == null) gun = H.MakeGun(new Color(0.15f, 0.75f, 0.95f));
        if (dot == null) dot = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.1f, Color.cyan, false);
        if (line == null) line = H.MakeLine("PullGunLine", Color.cyan, 0.01f);
    }

    public override void OnDisable() => Cleanup();

    public override void Update()
    {
        if (!H.Ready) return;
        if (gun == null) OnEnable();
        H.AttachGun(gun, false);

        Vector3 origin = H.RHand.position + H.Point(false) * 0.15f;
        Vector3 dir    = H.Point(false);
        Vector3 target = origin + dir * 60f;
        locked = null;
        float best = float.MaxValue;

        foreach (Component rig in Adv.OtherRigs())
        {
            Vector3 head = Adv.RigHead(rig);
            Vector3 to   = head - origin;
            float dist   = to.magnitude;
            if (dist < 0.5f || dist > 60f) continue;
            float ang = Vector3.Angle(dir, to);
            if (ang > 14f) continue;
            float score = ang + dist * 0.02f;
            if (score < best) { best = score; locked = rig; target = head; }
        }

        if (locked == null && Physics.Raycast(origin, dir, out RaycastHit hit, 60f))
            target = hit.point;

        Color col = locked != null ? Color.cyan : new Color(0.3f, 0.4f, 0.5f);
        if (dot != null) { dot.transform.position = target; var r = dot.GetComponent<Renderer>(); if (r) r.material.color = col; }
        if (line != null)
        {
            line.startColor = line.endColor = col;
            line.SetPosition(0, origin);
            line.SetPosition(1, target);
        }

        if (H.RTrig && locked != null)
        {
            Vector3 pull = (H.Head.position - Adv.RigHead(locked)).normalized * 10f * Time.deltaTime;
            locked.transform.root.position += pull;
        }
    }

    private void Cleanup()
    {
        if (gun != null) Object.Destroy(gun);
        if (dot != null) Object.Destroy(dot);
        if (line != null) Object.Destroy(line.gameObject);
        gun = null; dot = null; line = null;
    }
}

// ============================== FLICK TAG ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Flick Tag", "Tag the closest player in front of you when you flick your right wrist (fast rotation)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FlickTag : MenuMod
{
    private Quaternion lastRot;
    private bool       has;
    private float      cool;

    public override string BindHint => "Flick";

    public override void Update()
    {
        if (!H.Ready) return;
        Quaternion now = H.RHand.rotation;
        if (!has) { lastRot = now; has = true; return; }

        float ang = Quaternion.Angle(lastRot, now) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastRot = now;
        if (Time.time < cool || ang < 420f) return; // deg/sec threshold

        cool = Time.time + 0.4f;
        Vector3 origin = H.RHand.position, dir = H.Point(false);
        Component best = null;
        float bestScore = float.MaxValue;
        foreach (Component rig in Adv.OtherRigs())
        {
            Vector3 head = Adv.RigHead(rig);
            Vector3 to = head - origin;
            float dist = to.magnitude;
            if (dist > 4f) continue;
            float a = Vector3.Angle(dir, to);
            if (a > 50f) continue;
            float s = a + dist;
            if (s < bestScore) { bestScore = s; best = rig; }
        }
        if (best != null) Adv.FlickTagAt(best);
    }

    public override void OnDisable() => has = false;
}

// ============================== MORE TAG MODS (ii-style) ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Tag All", "Attempts to tag every other player in the room", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagAll : MenuMod
{
    public override string BindHint => "TAP";
    public override void Pressed()
    {
        if (!H.Ready) return;
        foreach (Component rig in Adv.OtherRigs())
            Adv.TryTag(rig);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Aura", "Automatically tags any player who comes within range", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagAura : MenuMod
{
    private float next;
    public override void Update()
    {
        if (!H.Ready || Time.time < next) return;
        next = Time.time + 0.2f;
        Vector3 me = H.Head.position;
        float range = TagAuraRange.Meters;
        foreach (Component rig in Adv.OtherRigs())
            if (Vector3.Distance(me, Adv.RigHead(rig)) < range)
                Adv.TryTag(rig);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Aura Range: ", "How far Tag Aura reaches", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagAuraRange : IncrementalMod
{
    private static readonly float[] R = [1.5f, 2.5f, 3.5f, 5f, 8f, 12f,];
    protected override string[] Labels => ["1.5m", "2.5m", "3.5m", "5m", "8m", "12m",];
    private static int idx = 1; // default 2.5m
    public static float Meters => R[Mathf.Clamp(idx, 0, R.Length - 1)];
    protected override void Changed() => idx = IncrementalValue;
    public override void OnIncrementalStateLoaded() => idx = IncrementalValue;
    public override bool ShowInEnabledList => false; // setting, not a running mod
}

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Reach", "Enlarges your hand tag colliders so you can tag from further away", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagReach : MenuMod
{
    private readonly List<(Transform t, Vector3 s)> saved = [];
    public override void OnEnable()  => Apply();
    public override void OnDisable() => Restore();
    public override void Update()    { if (saved.Count == 0) Apply(); }

    private void Apply()
    {
        if (!H.Ready) return;
        Restore();
        foreach (Transform hand in new[] { H.LHand, H.RHand })
        {
            if (hand == null) continue;
            foreach (Collider c in hand.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                saved.Add((c.transform, c.transform.localScale));
                c.transform.localScale = c.transform.localScale * 4f;
            }
        }
    }

    private void Restore()
    {
        foreach ((Transform t, Vector3 s) in saved)
            if (t != null) t.localScale = s;
        saved.Clear();
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Anti Tag", "Disables your tag sensors so others have a harder time tagging you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AntiTag : MenuMod
{
    private readonly List<(Collider col, bool was)> saved = [];
    public override void OnEnable()  => Apply();
    public override void OnDisable() => Restore();
    public override void Update()    { if (saved.Count == 0) Apply(); }
    private void Apply()
    {
        if (!H.Ready) return;
        Restore();
        foreach (Collider c in GTPlayer.Instance.GetComponentsInChildren<Collider>(true))
        {
            if (c == null) continue;
            string n = c.name.ToLowerInvariant();
            if (n.Contains("tag") || n.Contains("infect") || c.isTrigger)
            { saved.Add((c, c.enabled)); c.enabled = false; }
        }
    }
    private void Restore()
    {
        foreach ((Collider col, bool was) in saved)
            if (col != null) col.enabled = was;
        saved.Clear();
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Blink", "Hold right trigger to teleport a short distance where you look", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Blink : MenuMod
{
    private float next;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!H.Ready || !H.RTrig || Time.time < next) return;
        next = Time.time + 0.35f;
        Vector3 dir = H.Look.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = H.Look.forward;
        dir.Normalize();
        H.Teleport(H.Head.position + dir * 4f);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Self", "Marks yourself as tagged (client-side if the game exposes it)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagSelf : MenuMod
{
    public override void Pressed()
    {
        Component local = Adv.LocalRig();
        if (local != null) Adv.TryTag(local);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Untag Self", "Tries to clear your tagged state (client-side)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class UntagSelf : MenuMod
{
    public override void Pressed()
    {
        Component local = Adv.LocalRig();
        if (local == null) return;
        Type t = local.GetType();
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (string f in new[] { "isTagged", "tagged", "infected", "isInfected" })
        {
            FieldInfo fi = t.GetField(f, Any);
            if (fi != null && fi.FieldType == typeof(bool))
            { try { fi.SetValue(local, false); } catch { } }
            PropertyInfo pi = t.GetProperty(f, Any);
            if (pi != null && pi.CanWrite && pi.PropertyType == typeof(bool))
            { try { pi.SetValue(local, false); } catch { } }
        }
    }
}
