using System.Collections.Generic;
using System.IO;
using GorillaLocomotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// Ideas from user list — only mods not already on the menu. Client-side where possible.

// ============================== LOBBY ==============================

[ModCategory(Cat.Lobby)]
[ModInfo("Join Random", "Leaves and tries to land in a public-style room (best-effort)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaJoinRandom : MenuMod
{
    public override void Pressed()
    {
        try
        {
            var pn = Net.Find("Photon.Pun.PhotonNetwork");
            var m = pn?.GetMethod("JoinRandomRoom", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m != null) m.Invoke(null, null);
            else Net.Disconnect();
        }
        catch { Net.Disconnect(); }
    }
}

[ModCategory(Cat.Lobby)]
[ModInfo("Auto Join Room", "Keeps the last room code and retries Join every few seconds", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaAutoJoinRoom : MenuMod
{
    private float next;
    public override void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 3f;
        string code = JoinLastRoom.LastCode;
        if (string.IsNullOrEmpty(code)) code = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(code)) return;
        try
        {
            var pn = Net.Find("Photon.Pun.PhotonNetwork");
            var m = pn?.GetMethod("JoinRoom", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(string) }, null);
            m?.Invoke(null, new object[] { code.Trim() });
        }
        catch { }
    }
}

[ModCategory(Cat.Lobby)]
[ModInfo("Safe Restart", "Quits the game so Steam/launcher can restart cleanly", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaSafeRestart : ConfirmMod
{
    protected override string Idle => "Safe Restart";
    protected override void Run() => Application.Quit();
}

[ModCategory(Cat.Lobby)]
[ModInfo("Reconnect", "Disconnect then try to rejoin last room code", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaReconnect : MenuMod
{
    public override void Pressed()
    {
        string n = Net.RoomName();
        if (!string.IsNullOrEmpty(n)) JoinLastRoom.LastCode = n;
        Net.Disconnect();
    }
}

[ModCategory(Cat.Lobby)]
[ModInfo("Open Game Folder", "Copies the game data path to clipboard", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaOpenGameFolder : MenuMod
{
    public override void Pressed()
    {
        try { GUIUtility.systemCopyBuffer = Application.dataPath; }
        catch { }
    }
}

[ModCategory(Cat.Lobby)]
[ModInfo("Backup Preferences", "Writes a backup of MonkeMenu prefs to a text file", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBackupPrefs : MenuMod
{
    public override void Pressed()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "MonkeMenu");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "prefs_backup.txt");
            File.WriteAllText(path, "MonkeMenu backup " + System.DateTime.Now + "\nRoom=" + Net.RoomName());
        }
        catch { }
    }
}

[ModCategory(Cat.Lobby)]
[ModInfo("Friend Pings", "Spawns a bright ping marker at your head for a moment", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFriendPings : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.35f, Color.yellow, false);
        g.transform.position = H.Head.position + Vector3.up * 0.4f;
        H.Track(g, 2f);
    }
}

// ============================== MOVEMENT (new only) ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Auto Pinch Climb", "While near a surface, pulls you along the wall like climbing", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaAutoPinchClimb : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            Transform hand = H.Hand(i == 0);
            if (hand == null) continue;
            if (Physics.Raycast(hand.position, H.Palm(i == 0), out RaycastHit hit, 0.5f))
            {
                H.Rb.AddForce(-hit.normal * 18f + Vector3.up * 6f, ForceMode.Acceleration);
            }
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Wall Walk Strength: ", "How hard wall walk pulls (used by Auto Pinch Climb)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaWallWalkStrength : IncrementalMod
{
    private static readonly float[] S = [10f, 18f, 28f, 40f];
    protected override string[] Labels => ["Soft", "Normal", "Strong", "Max"];
    public static float Force => S[Mathf.Clamp(Instance, 0, S.Length - 1)];
    private static int Instance;
    protected override void Changed() => Instance = IncrementalValue;
    public override void OnIncrementalStateLoaded() => Instance = IncrementalValue;
    public override bool ShowInEnabledList => false;
}

[ModCategory(Cat.Movement)]
[ModInfo("Both Hands Wall Walk", "Grip either hand on a wall to stick and walk", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBothHandsWallWalk : MenuMod
{
    public override string BindHint => "Grip";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            if (!H.Grip(i == 0)) continue;
            if (Physics.Raycast(H.Hand(i == 0).position, H.Palm(i == 0), out RaycastHit hit, 0.45f))
            {
                H.Rb.AddForce(-hit.normal * IdeaWallWalkStrength.Force, ForceMode.Acceleration);
                H.Rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);
            }
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Grippy Hands", "Extra stick force when gripping near surfaces", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaGrippyHands : MenuMod
{
    public override string BindHint => "Grip";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 2; i++)
        {
            if (!H.Grip(i == 0)) continue;
            if (Physics.Raycast(H.Hand(i == 0).position, H.Palm(i == 0), out RaycastHit hit, 0.35f))
                H.Rb.AddForce(-hit.normal * 25f, ForceMode.Acceleration);
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Uncap Arm Length", "Lets Long Arms style stretch go further", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaUncapArmLength : MenuMod
{
    public override void LateUpdate()
    {
        if (!H.Ready) return;
        Vector3 head = H.Head.position;
        void Stretch(Transform hand)
        {
            if (hand == null) return;
            Vector3 o = hand.position - head;
            if (o.sqrMagnitude < 0.0001f) return;
            hand.position = head + o.normalized * Mathf.Min(o.magnitude * 1.15f, 4.5f);
        }
        Stretch(H.LHand); Stretch(H.RHand);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Reverse Velocity", "Flips your velocity the other way", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaReverseVelocity : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Rb.velocity = -H.Rb.velocity;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Velocity Multiplier: ", "Scales your speed every frame", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaVelocityMultiplier : IncrementalMod
{
    private static readonly float[] M = [1f, 1.15f, 1.35f, 1.6f, 2f];
    protected override string[] Labels => ["Off", "1.15x", "1.35x", "1.6x", "2x"];
    public override void Update()
    {
        if (!H.Ready || IncrementalValue == 0) return;
        Vector3 v = H.Rb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        if (flat.magnitude > 0.15f)
            H.Rb.velocity = flat.normalized * (flat.magnitude * M[IncrementalValue]) + Vector3.up * v.y;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Fun Move", "Wobble bounce movement", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFunMove : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        float s = Mathf.Sin(Time.time * 8f) * 2f;
        H.Rb.AddForce(H.Look.right * s, ForceMode.Acceleration);
        if (Ground.Near(1.6f)) H.Rb.AddForce(Vector3.up * 1.5f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Still Beyblade", "Spin in place at high speed", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaStillBeyblade : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        H.Rb.angularVelocity = Vector3.up * 12f;
        Vector3 v = H.Rb.velocity; H.Rb.velocity = new Vector3(0f, v.y, 0f);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Za Warudo", "Brief slow-mo time freeze effect", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaZaWarudo : MenuMod
{
    private float until;
    public override void Pressed()
    {
        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * 0.15f;
        until = Time.unscaledTime + 2f;
    }
    public override void Update()
    {
        if (until > 0f && Time.unscaledTime >= until)
        {
            until = 0f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Fly Towards Gun", "Hold RT to fly toward where your right hand points", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFlyTowardsGun : MenuMod
{
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!H.Ready || !H.RTrig) return;
        H.Rb.velocity = H.Point(false) * H.FlySpeed;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Constant Noclip", "Walk through map colliders until turned off", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaConstantNoclip : MenuMod
{
    private readonly List<Collider> off = [];
    public override void OnDisable() => Restore();
    public override void Update()
    {
        if (!H.Ready) return;
        if (off.Count > 0) return;
        Transform self = GTPlayer.Instance.transform.root;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || !c.enabled) continue;
            if (c.transform.IsChildOf(self)) continue;
            if (c.GetComponentInParent<KeepSolid>() != null) continue;
            if (!(c is MeshCollider || c is BoxCollider || c is TerrainCollider)) continue;
            c.enabled = false; off.Add(c);
        }
    }
    void Restore()
    {
        foreach (var c in off) if (c != null) c.enabled = true;
        off.Clear();
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Strafe Boost", "Extra sideways speed while airborne", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaStrafeBoost : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready || Ground.Near(1.5f)) return;
        Vector3 right = H.Look.right; right.y = 0f; right.Normalize();
        float stick = 0f;
        // approximate: use hand spread as strafe intent
        if (H.LHand && H.RHand)
        {
            Vector3 mid = (H.LHand.position + H.RHand.position) * 0.5f;
            Vector3 to = mid - H.Head.position; to.y = 0;
            stick = Vector3.Dot(to.normalized, right);
        }
        H.Rb.AddForce(right * stick * 12f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Dynamic Strafe", "Strafe force scales with your current speed", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaDynamicStrafe : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        Vector3 v = H.Rb.velocity; float sp = new Vector3(v.x, 0, v.z).magnitude;
        Vector3 right = H.Look.right; right.y = 0; right.Normalize();
        H.Rb.AddForce(right * (sp * 0.35f) * Mathf.Sin(Time.time * 3f), ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Blocky Animations", "Snaps your rotation to 45° steps for a blocky feel", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBlockyAnim : MenuMod
{
    public override void LateUpdate()
    {
        if (!H.Ready) return;
        Vector3 e = GTPlayer.Instance.transform.eulerAngles;
        e.y = Mathf.Round(e.y / 45f) * 45f;
        GTPlayer.Instance.transform.eulerAngles = e;
    }
}

// ============================== VISUAL (new only) ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Chams", "Brighten other players so they read through clutter", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaChams : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        foreach (var r in Adv.OtherRigs())
        {
            foreach (var rend in r.GetComponentsInChildren<Renderer>())
            {
                if (rend == null) continue;
                rend.enabled = true;
                if (rend.material != null) rend.material.color = Color.green;
            }
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Velocity Name Tags", "Speed readout above other players", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaVelNameTags : MenuMod
{
    private readonly List<TextMesh> tags = [];
    public override void OnDisable()
    {
        foreach (var t in tags) if (t) Object.Destroy(t.gameObject);
        tags.Clear();
    }
    public override void Update()
    {
        if (!H.Ready) return;
        var rigs = Adv.OtherRigs();
        while (tags.Count < rigs.Count)
        {
            var go = new GameObject("VelTag");
            var tm = go.AddComponent<TextMesh>();
            tm.fontSize = 24; tm.characterSize = 0.05f; tm.anchor = TextAnchor.MiddleCenter;
            tags.Add(tm);
        }
        while (tags.Count > rigs.Count)
        {
            int i = tags.Count - 1;
            if (tags[i]) Object.Destroy(tags[i].gameObject);
            tags.RemoveAt(i);
        }
        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 h = Adv.RigHead(rigs[i]);
            var rb = rigs[i].GetComponentInParent<Rigidbody>();
            float sp = rb != null ? rb.velocity.magnitude : 0f;
            tags[i].transform.position = h + Vector3.up * 0.4f;
            tags[i].transform.rotation = Quaternion.LookRotation(tags[i].transform.position - H.Head.position);
            tags[i].text = $"{sp:F1} m/s";
            tags[i].color = Color.cyan;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Nearby Overlay", "HUD listing distance to nearest other players", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaNearbyOverlay : HudMod
{
    protected override Vector3 Offset => new(0f, -0.4f, 0.7f);
    protected override string Line()
    {
        if (!H.Ready) return "Nearby: -";
        float best = 999f; int n = 0;
        foreach (var r in Adv.OtherRigs())
        {
            float d = Vector3.Distance(H.Head.position, Adv.RigHead(r));
            if (d < 40f) n++;
            if (d < best) best = d;
        }
        return n == 0 ? "Nearby: none" : $"Nearby: {n}  closest {best:F1}m";
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Velocity Overlay", "On-screen speed readout", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaVelocityOverlay : HudMod
{
    protected override Vector3 Offset => new(0f, -0.5f, 0.7f);
    protected override string Line() => !H.Ready ? "Vel -" : $"Vel {H.Rb.velocity.magnitude:F1}";
}

[ModCategory(Cat.Visual)]
[ModInfo("Frametime Counter", "Frame time in ms", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFrametime : HudMod
{
    protected override Vector3 Offset => new(0f, 0.35f, 0.7f);
    protected override string Line() => $"ft {Time.unscaledDeltaTime * 1000f:F1}ms";
}

[ModCategory(Cat.Visual)]
[ModInfo("Average FPS Counter", "Smoothed FPS", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaAvgFps : HudMod
{
    private float avg = 72f;
    protected override Vector3 Offset => new(0f, 0.42f, 0.7f);
    protected override string Line()
    {
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        avg = Mathf.Lerp(avg, fps, 0.05f);
        return $"avg {avg:F0} fps";
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Tag Range Visualizer", "Draws a sphere showing ~tag range", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaTagRangeViz : MenuMod
{
    private GameObject ball;
    public override void OnEnable()
    {
        ball = H.Prim(PrimitiveType.Sphere, Vector3.one * 3f, new Color(1f, 0.2f, 0.2f, 0.25f), false);
    }
    public override void OnDisable()
    {
        if (ball) Object.Destroy(ball); ball = null;
    }
    public override void Update()
    {
        if (!H.Ready || ball == null) return;
        ball.transform.position = H.Head.position;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Fog Toggle", "Turns fog on/off", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFogToggle : MenuMod
{
    private bool was;
    public override void OnEnable() { was = RenderSettings.fog; RenderSettings.fog = false; }
    public override void OnDisable() => RenderSettings.fog = was;
    public override void Update() => RenderSettings.fog = false;
}

[ModCategory(Cat.Visual)]
[ModInfo("Remove Leaves", "Hides objects with leaf/foliage in the name", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaRemoveLeaves : MenuMod
{
    private readonly List<GameObject> hid = [];
    public override void OnEnable()
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null) continue;
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("leaf") || n.Contains("leaves") || n.Contains("foliage") || n.Contains("bush"))
            {
                if (r.gameObject.activeSelf) { r.gameObject.SetActive(false); hid.Add(r.gameObject); }
            }
        }
    }
    public override void OnDisable()
    {
        foreach (var g in hid) if (g) g.SetActive(true);
        hid.Clear();
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Flip Camera", "Flips your view upside down", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFlipCamera : MenuMod
{
    public override void LateUpdate()
    {
        if (!H.Ready) return;
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.Rotate(0f, 0f, 180f * Time.deltaTime * 0f); // set once
        Vector3 e = cam.transform.eulerAngles;
        e.z = 180f;
        cam.transform.eulerAngles = e;
    }
    public override void OnDisable()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 e = cam.transform.eulerAngles; e.z = 0f; cam.transform.eulerAngles = e;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Nausea", "Wobbly camera effect on your view only", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaNausea : MenuMod
{
    public override void LateUpdate()
    {
        var cam = Camera.main; if (cam == null) return;
        cam.transform.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 6f) * 2f, Mathf.Cos(Time.time * 5f) * 2f, Mathf.Sin(Time.time * 4f));
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Psychedelic Filter", "Cycles ambient colors", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaPsychedelic : MenuMod
{
    public override void Update()
    {
        RenderSettings.ambientLight = Color.HSVToRGB((Time.time * 0.15f) % 1f, 0.7f, 1f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Lightning Spawn", "Flash + thunder-colored light at aim point", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaLightning : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 p = H.Head.position + H.Look.forward * 8f;
        if (Physics.Raycast(H.Head.position, H.Look.forward, out RaycastHit hit, 40f)) p = hit.point;
        var light = new GameObject("Bolt").AddComponent<Light>();
        light.type = LightType.Point; light.color = Color.cyan; light.intensity = 8f; light.range = 20f;
        light.transform.position = p + Vector3.up * 2f;
        H.Track(light.gameObject, 0.4f);
        RenderSettings.ambientLight = Color.white;
    }
}

// ============================== PROJECTILES ==============================

static class IdeaProj
{
    public static void Fire(PrimitiveType shape, Color col, float speed, float scale = 0.15f)
    {
        if (!H.Ready) return;
        var g = H.Prim(shape, Vector3.one * scale, col);
        g.transform.position = H.RHand.position + H.Point(false) * 0.2f;
        var rb = H.AddSolidBody(g, 0.25f);
        rb.velocity = H.Point(false) * speed;
        H.Track(g, 8f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Chicken Gun", "Fires chunky yellow 'chicken' spheres", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaChickenGun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.15f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(1f, 0.85f, 0.2f), 14f, 0.22f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Whoopee Cushion Gun", "Soft pink blobs with a local click", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaWhoopeeGun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.2f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(1f, 0.4f, 0.7f), 10f, 0.2f);
        SoundBoard.PlayMenuClick();
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Firecracker Gun", "Small red bursts", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFirecrackerGun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.12f;
        IdeaProj.Fire(PrimitiveType.Cube, Color.red, 16f, 0.08f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Bubble Gun", "Slow translucent bubbles", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBubbleGun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.1f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(0.6f, 0.9f, 1f), 4f, 0.18f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Fire Gun", "Orange flame-like shots", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFireGun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.08f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(1f, 0.4f, 0.05f), 12f, 0.12f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Ice Cream Projectiles", "Pastel scoops", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaIceCream : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.18f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(1f, 0.7f, 0.85f), 9f, 0.16f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Paper Projectiles", "Flat plane cubes", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaPaper : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.15f;
        var g = H.Prim(PrimitiveType.Cube, new Vector3(0.25f, 0.02f, 0.15f), Color.white);
        g.transform.position = H.RHand.position + H.Point(false) * 0.2f;
        g.transform.rotation = Quaternion.LookRotation(H.Point(false));
        H.AddSolidBody(g, 0.1f).velocity = H.Point(false) * 8f;
        H.Track(g, 8f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Apple Projectiles", "Red apple spheres", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaApple : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.2f;
        IdeaProj.Fire(PrimitiveType.Sphere, Color.red, 11f, 0.14f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Bomb Projectiles", "Black bomb spheres", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBomb : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.25f;
        IdeaProj.Fire(PrimitiveType.Sphere, Color.black, 10f, 0.2f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Snowball Rain", "Snowballs fall from above you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaSnowRain : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.15f;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.12f, Color.white);
        g.transform.position = H.Head.position + new Vector3(Random.Range(-2f, 2f), 5f, Random.Range(-2f, 2f));
        H.AddSolidBody(g, 0.2f);
        H.Track(g, 6f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Snowball Hail", "Fast hail of snowballs", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaSnowHail : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.05f;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.08f, Color.white);
        g.transform.position = H.Head.position + new Vector3(Random.Range(-3f, 3f), 6f, Random.Range(-3f, 3f));
        var rb = H.AddSolidBody(g, 0.15f);
        rb.velocity = Vector3.down * 20f;
        H.Track(g, 4f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Shotgun", "Spread of projectiles on RT", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaShotgun : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.35f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = (H.Point(false) + Random.insideUnitSphere * 0.15f).normalized;
            var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.08f, Color.gray);
            g.transform.position = H.RHand.position + dir * 0.2f;
            H.AddSolidBody(g, 0.15f).velocity = dir * 18f;
            H.Track(g, 5f);
        }
    }
}

// ============================== FUN (new only) ==============================

[ModCategory(Cat.Fun)]
[ModInfo("Jumpscare On Tag", "Flash screen when someone is very close", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaJumpscareOnTag : MenuMod
{
    private float cool;
    public override void Update()
    {
        if (!H.Ready || Time.time < cool) return;
        foreach (var r in Adv.OtherRigs())
        {
            if (Vector3.Distance(H.Head.position, Adv.RigHead(r)) < 1.2f)
            {
                cool = Time.time + 5f;
                RenderSettings.ambientLight = Color.red;
                SoundBoard.PlayMenuOpen();
                break;
            }
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Rock Self", "Turns your local mesh dark grey", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaRockSelf : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        foreach (var r in GTPlayer.Instance.GetComponentsInChildren<Renderer>())
            if (r != null && r.material != null) r.material.color = Color.gray;
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Red Blue Strobe", "Strobes ambient between red and blue", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaRedBlueStrobe : MenuMod
{
    public override void Update()
    {
        RenderSettings.ambientLight = (Time.time % 0.4f < 0.2f) ? Color.red : Color.blue;
    }
}

// ============================== BUILDING ==============================

[ModCategory(Cat.Building)]
[ModInfo("Block Rain", "Blocks rain from above", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBlockRain : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.2f;
        var g = H.Prim(PrimitiveType.Cube, Vector3.one * 0.3f, Color.gray);
        g.transform.position = H.SnapToGround(H.Head.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f))) + Vector3.up * 4f;
        H.AddSolidBody(g, 1f);
        H.Track(g, 20f);
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Block Orbit", "Cubes orbit around you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaBlockOrbit : MenuMod
{
    private readonly List<GameObject> blocks = [];
    public override void OnEnable()
    {
        for (int i = 0; i < 8; i++)
        {
            var g = H.Prim(PrimitiveType.Cube, Vector3.one * 0.25f, H.Rainbow(1f, i / 8f));
            blocks.Add(g);
        }
    }
    public override void OnDisable()
    {
        foreach (var g in blocks) if (g) Object.Destroy(g);
        blocks.Clear();
    }
    public override void Update()
    {
        if (!H.Ready) return;
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null) continue;
            float a = Time.time * 1.5f + i * Mathf.PI * 2f / blocks.Count;
            blocks[i].transform.position = H.Head.position + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * 1.5f;
        }
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Grab All Nearby", "Pulls nearby spawned PVTX objects toward your hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaGrabAllNearby : MenuMod
{
    public override string BindHint => "RG";
    public override void Update()
    {
        if (!H.Ready || !H.RGrip) return;
        Vector3 hand = H.RHand.position;
        foreach (var rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (rb == null || rb == H.Rb) continue;
            if (!rb.name.Contains("PVTX") && rb.gameObject.name.IndexOf("PVTX") < 0) continue;
            if (Vector3.Distance(rb.position, hand) < 4f)
                rb.velocity = (hand - rb.position) * 5f;
        }
    }
}

// ============================== TOOLS ==============================

[ModCategory(Cat.Tools)]
[ModInfo("Copy Map ID", "Copies room/map name to clipboard", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaCopyMapId : MenuMod
{
    public override void Pressed()
    {
        try { GUIUtility.systemCopyBuffer = Net.RoomName() ?? Application.productName; } catch { }
    }
}

[ModCategory(Cat.Tools)]
[ModInfo("FPS Cap Selector: ", "Target frame rate", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaFpsCapSel : IncrementalMod
{
    private static readonly int[] C = [0, 72, 90, 120];
    protected override string[] Labels => ["Off", "72", "90", "120"];
    protected override void Changed()
    {
        int c = C[IncrementalValue];
        Application.targetFrameRate = c <= 0 ? -1 : c;
    }
    public override void OnDisable() => Application.targetFrameRate = -1;
}

[ModCategory(Cat.Tools)]
[ModInfo("Credits Page", "Shows menu credit toast in the log", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaCredits : MenuMod
{
    public override void Pressed() => Plugin.Log.LogInfo("MonkeMenu / PVTX fan menu — thanks for playing.");
}

[ModCategory(Cat.Tools)]
[ModInfo("Info Button", "Logs room and player readiness", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaInfoButton : MenuMod
{
    public override void Pressed() => Plugin.Log.LogInfo(Net.Room() + " ready=" + H.Ready);
}

[ModCategory(Cat.Tools)]
[ModInfo("Info Watch", "Wrist-style HUD with room + speed", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaInfoWatch : HudMod
{
    protected override Vector3 Offset => new(-0.15f, -0.25f, 0.4f);
    protected override string Line()
    {
        if (!H.Ready) return "Watch --";
        return $"{Net.RoomName()}\n{H.Rb.velocity.magnitude:F1} m/s";
    }
}

[ModCategory(Cat.Tools)]
[ModInfo("Hide Menu Title", "No-op placeholder for title preference", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class IdeaHideMenuTitle : MenuMod
{
    public override void Update() { }
}
