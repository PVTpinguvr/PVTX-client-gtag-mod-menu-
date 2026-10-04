using System.Collections.Generic;
using GorillaLocomotion;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== SHARED BITS ==============================

/// <summary>Put this on anything of ours that must stay solid (platforms etc.) - the No Collide mods skip it.</summary>
public class KeepSolid : MonoBehaviour { }

public static class WorldScan
{
    /// <summary>Every physics body in the scene that isn't you. Kinematic ones only if asked for.</summary>
    public static List<Rigidbody> Bodies(bool includeKinematic = false)
    {
        List<Rigidbody> list = [];
        if (!H.Ready) return list;
        Transform self   = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance.transform.root;
        foreach (Rigidbody r in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (r == null || r == H.Rb) continue;
            if (!includeKinematic && r.isKinematic) continue;
            if (r.transform.IsChildOf(self) || r.transform.IsChildOf(tagger)) continue;
            list.Add(r);
        }
        return list;
    }
}

/// <summary>Ray helpers that ignore your own body, so "am I on the ground" is honest.</summary>
public static class Ground
{
    public static bool Find(float dist, out Vector3 point)
    {
        point = Vector3.zero;
        if (!H.Ready) return false;
        Transform self   = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance.transform.root;
        float best = float.MaxValue;
        bool found = false;
        foreach (RaycastHit h in Physics.RaycastAll(H.Head.position, Vector3.down, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform t = h.collider.transform;
            if (t.IsChildOf(self) || t.IsChildOf(tagger)) continue;
            if (h.distance < best) { best = h.distance; point = h.point; found = true; }
        }
        return found;
    }

    /// <summary>True when there is something solid within about "dist" metres under your head.</summary>
    public static bool Near(float dist) => Find(dist, out _);
}

/// <summary>Base for "turn solid stuff into ghosts". Colliders are switched off (not deleted) and come back when the mod is turned off.</summary>
public abstract class NoCollideBase : MenuMod
{
    private readonly List<Collider>    cached = [];
    private readonly HashSet<Collider> known  = [];
    private float nextScan;

    protected abstract bool Wants(Collider c);

    public override void OnEnable()
    {
        nextScan = 0f;
        Scan();
    }

    public override void OnDisable()
    {
        foreach (Collider c in cached)
            if (c != null) c.enabled = true;
        cached.Clear();
        known.Clear();
    }

    // New map pieces load in while you play, so look again every couple of seconds.
    private void Scan()
    {
        if (!H.Ready) return;
        Transform self   = GTPlayer.Instance.transform.root;
        Transform tagger = GorillaTagger.Instance.transform.root;

        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || known.Contains(c)) continue;
            if (!c.enabled) continue; // the game turned it off, leave it alone
            Transform t = c.transform;
            if (t.IsChildOf(self) || t.IsChildOf(tagger)) continue;
            if (c.GetComponentInParent<KeepSolid>() != null) continue;
            if (!Wants(c)) continue;

            known.Add(c);
            cached.Add(c);
            c.enabled = false;
        }
    }

    public override void Update()
    {
        if (!H.Ready || Time.time < nextScan) return;
        nextScan = Time.time + 2f;
        Scan();
    }
}

// ============================== NO COLLIDE ==============================

[ModCategory(Cat.World)]
[ModInfo("No Collide Map", "Everything solid in the map stops colliding, so you fall through it. Turn off to bring it back", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoCollideMap : NoCollideBase
{
    public override int Priority => -10;
    protected override bool Wants(Collider c) => true;
}

[ModCategory(Cat.World)]
[ModInfo("No Collide Blocks", "Blocks, cubes, crates and bricks in the map stop colliding (the floor stays)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoCollideBlocks : NoCollideBase
{
    public override int Priority => -9;
    private static readonly string[] Words = ["block", "cube", "crate", "box", "brick", "pillar", "stone", "obstacle",];

    protected override bool Wants(Collider c)
    {
        string n = c.gameObject.name.ToLowerInvariant();
        string p = c.transform.parent != null ? c.transform.parent.name.ToLowerInvariant() : "";
        foreach (string w in Words)
            if (n.Contains(w) || p.Contains(w)) return true;
        return false;
    }
}

[ModCategory(Cat.World)]
[ModInfo("No Collide Props", "Anything that is a physics object (balls, barrels, loose blocks) stops colliding", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class NoCollideProps : NoCollideBase
{
    public override int Priority => -8;
    protected override bool Wants(Collider c) => c.attachedRigidbody != null;
}

// ============================== PHYSICS PROPS ==============================

[ModCategory(Cat.World)]
[ModInfo("Freeze Props", "Every loose physics object in the map freezes in place", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FreezeProps : MenuMod
{
    private readonly List<Rigidbody> frozen = [];
    private float nextScan;

    public override void OnEnable() => nextScan = 0f;

    public override void OnDisable()
    {
        foreach (Rigidbody r in frozen)
            if (r != null) r.isKinematic = false;
        frozen.Clear();
    }

    public override void Update()
    {
        if (!H.Ready || Time.time < nextScan) return;
        nextScan = Time.time + 1.5f;
        foreach (Rigidbody r in WorldScan.Bodies())
        {
            r.velocity = Vector3.zero;
            r.isKinematic = true;
            frozen.Add(r);
        }
    }
}

[ModCategory(Cat.World)]
[ModInfo("Float Props", "Loose physics objects stop falling and drift in the air", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FloatProps : MenuMod
{
    private readonly List<Rigidbody> floating = [];
    private float nextScan;

    public override void OnEnable() => nextScan = 0f;

    public override void OnDisable()
    {
        foreach (Rigidbody r in floating)
            if (r != null) r.useGravity = true;
        floating.Clear();
    }

    public override void Update()
    {
        if (!H.Ready || Time.time < nextScan) return;
        nextScan = Time.time + 1.5f;
        foreach (Rigidbody r in WorldScan.Bodies(true))
        {
            if (!r.useGravity) continue;
            r.useGravity = false;
            floating.Add(r);
        }
    }
}

[ModCategory(Cat.World)]
[ModInfo("Prop Magnet", "Hold left grip to pull nearby physics objects to your left hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PropMagnet : MenuMod
{
    private List<Rigidbody> bodies = [];
    private float nextScan;

    public override string BindHint => "LG";
    public override void Update()
    {
        if (!H.Ready || !H.LGrip || Time.time < nextScan) return;
        nextScan = Time.time + 1.5f;
        bodies = WorldScan.Bodies();
    }

    public override void FixedUpdate()
    {
        if (!H.Ready || !H.LGrip) return;
        Vector3 target = H.LHand.position;
        foreach (Rigidbody r in bodies)
        {
            if (r == null) continue;
            Vector3 to = target - r.position;
            if (to.sqrMagnitude > 400f) continue; // only things within 20 m
            r.AddForce(to.normalized * 30f, ForceMode.Acceleration);
        }
    }
}

[ModCategory(Cat.World)]
[ModInfo("Prop Blast", "Blasts every loose physics object away from you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PropBlast : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        foreach (Rigidbody r in WorldScan.Bodies())
            r.AddExplosionForce(40f, H.Head.position, 15f, 1f, ForceMode.Impulse);
    }
}

[ModCategory(Cat.World)]
[ModInfo("Hide Props", "Loose physics objects become invisible (only for you)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class HideProps : MenuMod
{
    private readonly List<Renderer>    hidden = [];
    private readonly HashSet<Renderer> seen   = [];
    private float nextScan;

    public override void OnEnable() => nextScan = 0f;

    public override void OnDisable()
    {
        foreach (Renderer r in hidden)
            if (r != null) r.enabled = true;
        hidden.Clear();
        seen.Clear();
    }

    public override void Update()
    {
        if (!H.Ready || Time.time < nextScan) return;
        nextScan = Time.time + 1.5f;
        foreach (Rigidbody body in WorldScan.Bodies(true))
            foreach (Renderer r in body.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !seen.Add(r)) continue;
                r.enabled = false;
                hidden.Add(r);
            }
    }
}

// ============================== CUSTOM LOBBY BOARDS ==============================
// Replaces text on the in-map Leaderboard / Update (UPD) / TOS boards with your own copy.
// Works by finding TextMeshPro / TextMesh components whose parent/object name matches
// common Gorilla Tag / fan-game board names, then overwriting their text every frame.

/// <summary>Shared logic for the three custom board mods.</summary>
public abstract class CustomBoardMod : MenuMod
{
    private readonly List<Component> targets = [];
    private float nextScan;
    private string lastApplied;

    /// <summary>Object-name substrings (case-insensitive) that identify this board.</summary>
    protected abstract string[] NameHints { get; }

    /// <summary>The text written onto the board while this mod is on.</summary>
    protected abstract string BoardText { get; }

    public override void OnDisable()
    {
        targets.Clear();
        lastApplied = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 3f;
            Rescan();
        }

        string text = BoardText;
        if (text == lastApplied && targets.Count > 0) return;
        lastApplied = text;
        Apply(text);
    }

    private void Rescan()
    {
        targets.Clear();
        // TextMeshPro (3D) and legacy TextMesh both show up on lobby boards.
        foreach (TextMeshPro tmp in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
            if (tmp != null && NameMatches(tmp.transform)) targets.Add(tmp);
        foreach (TextMesh tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            if (tm != null && NameMatches(tm.transform)) targets.Add(tm);
        // Also catch UI TextMeshProUGUI if the fan game uses canvas boards.
        foreach (TextMeshProUGUI ugui in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (ugui != null && NameMatches(ugui.transform)) targets.Add(ugui);
    }

    private bool NameMatches(Transform t)
    {
        for (Transform c = t; c != null; c = c.parent)
        {
            string n = c.name;
            if (string.IsNullOrEmpty(n)) continue;
            foreach (string hint in NameHints)
                if (n.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
        }
        return false;
    }

    private void Apply(string text)
    {
        foreach (Component c in targets)
        {
            if (c == null) continue;
            switch (c)
            {
                case TextMeshPro tmp:     tmp.text = text; break;
                case TextMeshProUGUI ugui: ugui.text = text; break;
                case TextMesh tm:         tm.text = text; break;
            }
        }
    }
}

[ModCategory(Cat.World)]
[ModInfo("Custom Leaderboard", "Replaces the lobby leaderboard / scoreboard text with a custom message", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CustomLeaderboard : CustomBoardMod
{
    protected override string[] NameHints =>
    [
        "Leaderboard", "LeaderBoard", "Scoreboard", "ScoreBoard", "score board", "HighScore", "RankBoard",
    ];

    protected override string BoardText =>
        "<b>PVTX LEADERBOARD</b>\n" +
        "--------------------\n" +
        "1. You          ∞\n" +
        "2. Monke        999\n" +
        "3. Tag King     420\n" +
        "4. Speedster    301\n" +
        "5. Ghost        150\n" +
        "--------------------\n" +
        "Fan game ranks · have fun";
}

[ModCategory(Cat.World)]
[ModInfo("Custom UPD Board", "Replaces the Update / news board text with a custom message", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CustomUpdBoard : CustomBoardMod
{
    protected override string[] NameHints =>
    [
        "Update", "Updates", "UPD", "News", "Changelog", "PatchNotes", "Announcement", "InfoBoard",
    ];

    protected override string BoardText =>
        "<b>UPDATES</b>\n" +
        "--------------------\n" +
        "• PVTX hacks menu active\n" +
        "• Custom boards online\n" +
        "• 0.2s button delay on menu\n" +
        "• Bind hints on mod rows\n" +
        "--------------------\n" +
        "Modded fan game build";
}

[ModCategory(Cat.World)]
[ModInfo("Custom TOS Board", "Replaces the Terms of Service / Code of Conduct board text", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CustomTosBoard : CustomBoardMod
{
    protected override string[] NameHints =>
    [
        "TOS", "Terms", "CodeOfConduct", "Code of Conduct", "Rules", "Conduct", "Disclaimer", "Policy",
    ];

    protected override string BoardText =>
        "<b>TERMS · FAN GAME</b>\n" +
        "--------------------\n" +
        "1. Be cool to other monkes\n" +
        "2. No toxicity / hate\n" +
        "3. Mods are for fun only\n" +
        "4. Don't grief the lobby\n" +
        "5. Have a good time\n" +
        "--------------------\n" +
        "Private modded copy · not official";
}
