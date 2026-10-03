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
}

// ============================== TAG GUN ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Gun", "Hold right grip to aim, right trigger to tag the player under the dot", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TagGun : MenuMod
{
    private GameObject   dot;
    private LineRenderer line;
    private Component    locked;

    public override string BindHint => "RG+RT";

    public override void OnDisable()
    {
        Cleanup();
        locked = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        if (!H.RGrip) { Cleanup(); locked = null; return; }

        if (dot == null)
        {
            dot  = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.12f, Color.red, false);
            line = H.MakeLine("TagGunLine", Color.red, 0.008f);
        }

        Vector3 origin = H.RHand.position, dir = H.Point(false);
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
            if (ang > 12f) continue;
            float score = ang + dist * 0.02f;
            if (score < best) { best = score; locked = rig; target = head; }
        }

        // also allow world raycast so the beam looks real when nobody is in the cone
        if (locked == null && Physics.Raycast(origin, dir, out RaycastHit hit, 80f))
            target = hit.point;

        dot.transform.position = target;
        dot.GetComponent<Renderer>().material.color = locked != null ? Color.green : Color.red;
        line.startColor = line.endColor = locked != null ? Color.green : Color.red;
        line.SetPosition(0, origin);
        line.SetPosition(1, target);

        if (H.RTrig && locked != null)
        {
            bool ok = Adv.TryTag(locked);
            // visual feedback pulse
            dot.transform.localScale = Vector3.one * (ok ? 0.22f : 0.12f);
        }
        else if (dot != null)
            dot.transform.localScale = Vector3.one * 0.12f;
    }

    private void Cleanup()
    {
        if (dot != null) Object.Destroy(dot);
        if (line != null) Object.Destroy(line.gameObject);
        dot  = null;
        line = null;
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

[ModCategory(Cat.Advantage)]
[ModInfo("Invis", "Hides your local monke mesh (others may still see you on networked games)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Invis : MenuMod
{
    public override void OnEnable()  => Apply(false);
    public override void OnDisable() => Apply(true);
    public override void Update()    => Apply(false);

    private static void Apply(bool visible)
    {
        if (!H.Ready) return;
        Component local = Adv.LocalRig();
        if (local != null) Adv.SetRenderers(local, visible);
        else
        {
            // fallback: hide renderers under the player / tagger roots
            Adv.SetRenderers(GTPlayer.Instance, visible);
            if (GorillaTagger.Instance != null) Adv.SetRenderers(GorillaTagger.Instance, visible);
        }
    }
}

// ============================== GHOST ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Ghost", "Invis + your colliders stop blocking (you can walk through monkes)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GhostMode : MenuMod
{
    private readonly List<Collider> changed = [];

    public override void OnEnable()  => Apply(true);
    public override void OnDisable() => Restore();
    public override void Update()    { if (changed.Count == 0) Apply(true); }

    private void Apply(bool ghost)
    {
        if (!H.Ready) return;
        Restore();
        // hide mesh
        Component local = Adv.LocalRig();
        if (local != null) Adv.SetRenderers(local, !ghost);
        else
        {
            Adv.SetRenderers(GTPlayer.Instance, !ghost);
            if (GorillaTagger.Instance != null) Adv.SetRenderers(GorillaTagger.Instance, !ghost);
        }

        // soft-disable player colliders (skip triggers)
        foreach (Collider c in GTPlayer.Instance.GetComponentsInChildren<Collider>(true))
        {
            if (c == null || c.isTrigger) continue;
            if (!c.enabled) continue;
            c.enabled = false;
            changed.Add(c);
        }
    }

    private void Restore()
    {
        foreach (Collider c in changed)
            if (c != null) c.enabled = true;
        changed.Clear();
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
[ModInfo("Pull Gun", "Hold right grip to aim, right trigger to yank the nearest player toward you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PullGun : MenuMod
{
    private GameObject   dot;
    private LineRenderer line;
    private Component    locked;

    public override string BindHint => "RG+RT";

    public override void OnDisable() => Cleanup();

    public override void Update()
    {
        if (!H.Ready) return;
        if (!H.RGrip) { Cleanup(); locked = null; return; }

        if (dot == null)
        {
            dot  = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.12f, Color.cyan, false);
            line = H.MakeLine("PullGunLine", Color.cyan, 0.008f);
        }

        Vector3 origin = H.RHand.position, dir = H.Point(false);
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

        dot.transform.position = target;
        line.SetPosition(0, origin);
        line.SetPosition(1, target);
        line.startColor = line.endColor = locked != null ? Color.cyan : new Color(0.3f, 0.3f, 0.4f);

        if (H.RTrig && locked != null)
        {
            // move their root toward us (client-side; networked pull needs game RPCs)
            Vector3 pull = (H.Head.position - Adv.RigHead(locked)).normalized * 8f * Time.deltaTime;
            locked.transform.root.position += pull;
        }
    }

    private void Cleanup()
    {
        if (dot != null) Object.Destroy(dot);
        if (line != null) Object.Destroy(line.gameObject);
        dot = null; line = null;
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
        if (best != null) Adv.TryTag(best);
    }

    public override void OnDisable() => has = false;
}
