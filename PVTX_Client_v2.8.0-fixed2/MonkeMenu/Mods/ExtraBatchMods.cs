using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// Extra batch of original mods — names not already on the menu.

// ============================== LOBBY ==============================

[ModCategory(Cat.Lobby)]
[ModInfo("Leave Room Soft", "Disconnect without confirm (quick leave)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XLeaveSoft : MenuMod
{
    public override void Pressed() => Net.Disconnect();
}

[ModCategory(Cat.Lobby)]
[ModInfo("Room Code Toast", "Logs current room code", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XRoomToast : MenuMod
{
    public override void Pressed() => Plugin.Log.LogInfo("Room: " + (Net.RoomName() ?? "(none)"));
}

[ModCategory(Cat.Lobby)]
[ModInfo("Save Room Code", "Stores current room as last-join code", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSaveRoomCode : MenuMod
{
    public override void Pressed()
    {
        string n = Net.RoomName();
        if (!string.IsNullOrEmpty(n)) JoinLastRoom.LastCode = n;
    }
}

// ============================== MOVEMENT ==============================

[ModCategory(Cat.Movement)]
[ModInfo("Moon Hop", "Extra bounce whenever you leave the ground", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XMoonHop : MenuMod
{
    private bool wasGround;
    public override void Update()
    {
        if (!H.Ready) return;
        bool g = Ground.Near(1.6f);
        if (wasGround && !g) H.Rb.AddForce(Vector3.up * 6f, ForceMode.VelocityChange);
        wasGround = g;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Sticky Feet", "Stronger grip on slopes while grounded", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XStickyFeet : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready || !Ground.Near(1.7f)) return;
        H.Rb.AddForce(-H.Rb.velocity * 2f, ForceMode.Acceleration);
        H.Rb.AddForce(Vector3.down * 15f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Coyote Jump", "Short grace jump after leaving a ledge", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XCoyoteJump : MenuMod
{
    private float coyote;
    private bool wasG;
    public override string BindHint => "A";
    public override void Update()
    {
        if (!H.Ready) return;
        bool g = Ground.Near(1.6f);
        if (g) coyote = Time.time + 0.18f;
        if (!wasG && H.A && Time.time < coyote)
            H.Rb.AddForce(Vector3.up * 7f, ForceMode.VelocityChange);
        wasG = g;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Boost Pad Pulse", "Periodic upward boost while enabled", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBoostPadPulse : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 1.2f;
        if (Ground.Near(1.8f)) H.Rb.AddForce(Vector3.up * 8f + H.Look.forward * 3f, ForceMode.VelocityChange);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Ice Skate", "Very low friction while grounded", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XIceSkate : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready || !Ground.Near(1.7f)) return;
        Vector3 v = H.Rb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        if (flat.magnitude > 0.5f)
            H.Rb.velocity = flat.normalized * Mathf.Max(flat.magnitude, 6f) + Vector3.up * v.y;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Helicopter Spin", "Spin while holding both grips", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XHelicopter : MenuMod
{
    public override string BindHint => "LG+RG";
    public override void Update()
    {
        if (!H.Ready || !(H.LGrip && H.RGrip)) return;
        H.Rb.AddTorque(Vector3.up * 20f, ForceMode.Acceleration);
        H.Rb.AddForce(Vector3.up * 4f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Dive", "Hold B to dive downward fast", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XDive : MenuMod
{
    public override string BindHint => "B";
    public override void FixedUpdate()
    {
        if (!H.Ready || !H.B) return;
        H.Rb.AddForce(Vector3.down * 30f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Swim Mode", "Water-like drag and upward hold on trigger", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSwimMode : MenuMod
{
    public override string BindHint => "RT";
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        H.Rb.velocity *= 0.96f;
        if (H.RTrig) H.Rb.AddForce(H.Look.forward * 12f + Vector3.up * 4f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Tarzan Swing", "Pull toward where your right hand points while gripping", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XTarzan : MenuMod
{
    public override string BindHint => "RG";
    public override void FixedUpdate()
    {
        if (!H.Ready || !H.RGrip) return;
        if (Physics.Raycast(H.RHand.position, H.Point(false), out RaycastHit hit, 25f))
        {
            Vector3 to = hit.point - H.Head.position;
            H.Rb.AddForce(to.normalized * 18f, ForceMode.Acceleration);
        }
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Micro Steps", "Limits max horizontal speed for careful movement", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XMicroSteps : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        Vector3 v = H.Rb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        if (flat.magnitude > 2.5f)
            H.Rb.velocity = flat.normalized * 2.5f + Vector3.up * v.y;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Launch Pad", "Tap A while grounded for a big launch", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XLaunchPad : MenuMod
{
    private bool was;
    public override string BindHint => "A";
    public override void Update()
    {
        if (!H.Ready) return;
        bool now = H.A;
        if (now && !was && Ground.Near(1.7f))
            H.Rb.AddForce(Vector3.up * 14f + H.Look.forward * 8f, ForceMode.VelocityChange);
        was = now;
    }
}

[ModCategory(Cat.Movement)]
[ModInfo("Backflip Impulse", "Add upward+back flip impulse on A", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBackflip : MenuMod
{
    private bool was;
    public override string BindHint => "A";
    public override void Update()
    {
        if (!H.Ready) return;
        bool now = H.A;
        if (now && !was)
        {
            H.Rb.AddForce(Vector3.up * 8f - H.Look.forward * 5f, ForceMode.VelocityChange);
            H.Rb.AddTorque(-H.Look.right * 15f, ForceMode.VelocityChange);
        }
        was = now;
    }
}

// ============================== VISUAL ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Wireframe Hands", "Tiny cubes follow your hands", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XWireHands : MenuMod
{
    private GameObject l, r;
    public override void OnEnable()
    {
        l = H.Prim(PrimitiveType.Cube, Vector3.one * 0.08f, Color.green, false);
        r = H.Prim(PrimitiveType.Cube, Vector3.one * 0.08f, Color.red, false);
    }
    public override void OnDisable()
    {
        if (l) Object.Destroy(l); if (r) Object.Destroy(r); l = r = null;
    }
    public override void Update()
    {
        if (!H.Ready) return;
        if (l) l.transform.position = H.LHand.position;
        if (r) r.transform.position = H.RHand.position;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Player Beacons", "Vertical beams on other players", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPlayerBeacons : MenuMod
{
    private readonly List<LineRenderer> lines = [];
    public override void OnDisable()
    {
        foreach (var lr in lines) if (lr) Object.Destroy(lr.gameObject);
        lines.Clear();
    }
    public override void Update()
    {
        if (!H.Ready) return;
        var rigs = Adv.OtherRigs();
        while (lines.Count < rigs.Count) lines.Add(H.MakeLine("Beacon", Color.yellow, 0.03f));
        while (lines.Count > rigs.Count)
        {
            int i = lines.Count - 1;
            if (lines[i]) Object.Destroy(lines[i].gameObject);
            lines.RemoveAt(i);
        }
        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 h = Adv.RigHead(rigs[i]);
            lines[i].SetPosition(0, h + Vector3.up * 0.2f);
            lines[i].SetPosition(1, h + Vector3.up * 4f);
            lines[i].startColor = lines[i].endColor = Color.yellow;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Danger Radius", "Red ring on the floor around you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XDangerRadius : MenuMod
{
    private readonly List<GameObject> dots = [];
    public override void OnEnable()
    {
        for (int i = 0; i < 24; i++)
            dots.Add(H.Prim(PrimitiveType.Sphere, Vector3.one * 0.08f, Color.red, false));
    }
    public override void OnDisable()
    {
        foreach (var d in dots) if (d) Object.Destroy(d);
        dots.Clear();
    }
    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 c = H.SnapToGround(H.Head.position) + Vector3.up * 0.05f;
        for (int i = 0; i < dots.Count; i++)
        {
            float a = i * Mathf.PI * 2f / dots.Count;
            dots[i].transform.position = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Night Vision", "Green ambient boost", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XNightVision : MenuMod
{
    public override void Update()
    {
        RenderSettings.ambientLight = new Color(0.15f, 0.9f, 0.2f);
        RenderSettings.ambientIntensity = 1.3f;
        RenderSettings.fog = false;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Sepia World", "Brownish ambient tint", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSepia : MenuMod
{
    public override void Update() => RenderSettings.ambientLight = new Color(0.85f, 0.7f, 0.45f);
}

[ModCategory(Cat.Visual)]
[ModInfo("Mirror Hands", "Ghost cubes mirrored across your body", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XMirrorHands : MenuMod
{
    private GameObject l, r;
    public override void OnEnable()
    {
        l = H.Prim(PrimitiveType.Cube, Vector3.one * 0.07f, Color.cyan, false);
        r = H.Prim(PrimitiveType.Cube, Vector3.one * 0.07f, Color.magenta, false);
    }
    public override void OnDisable()
    {
        if (l) Object.Destroy(l); if (r) Object.Destroy(r); l = r = null;
    }
    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 mid = H.Head.position;
        if (l)
        {
            Vector3 p = H.RHand.position - mid; p.x = -p.x;
            l.transform.position = mid + p;
        }
        if (r)
        {
            Vector3 p = H.LHand.position - mid; p.x = -p.x;
            r.transform.position = mid + p;
        }
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Trail Breadcrumbs", "Drops fading dots as you move", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBreadcrumbs : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.25f;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.06f, H.Rainbow(2f), false);
        g.transform.position = H.SnapToGround(H.Head.position) + Vector3.up * 0.05f;
        H.Track(g, 8f);
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Spotlight Self", "Bright point light on your head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSpotlightSelf : MenuMod
{
    private Light lit;
    public override void OnEnable()
    {
        var go = new GameObject("SelfSpot");
        lit = go.AddComponent<Light>();
        lit.type = LightType.Point; lit.range = 12f; lit.intensity = 3f; lit.color = Color.white;
    }
    public override void OnDisable()
    {
        if (lit) Object.Destroy(lit.gameObject); lit = null;
    }
    public override void Update()
    {
        if (lit && H.Ready) lit.transform.position = H.Head.position + Vector3.up * 0.3f;
    }
}

[ModCategory(Cat.Visual)]
[ModInfo("Enemy Flash", "Flash ambient when a player is under 5m", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XEnemyFlash : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        bool close = false;
        foreach (var r in Adv.OtherRigs())
            if (Vector3.Distance(H.Head.position, Adv.RigHead(r)) < 5f) { close = true; break; }
        if (close) RenderSettings.ambientLight = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 3f, 1f));
    }
}

// ============================== FUN ==============================

[ModCategory(Cat.Fun)]
[ModInfo("Pet Rock", "A rock that follows you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPetRock : MenuMod
{
    private GameObject pet;
    public override void OnEnable()
    {
        pet = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.25f, Color.gray);
        H.AddSolidBody(pet, 0.5f).isKinematic = true;
    }
    public override void OnDisable()
    {
        if (pet) Object.Destroy(pet); pet = null;
    }
    public override void Update()
    {
        if (!H.Ready || pet == null) return;
        Vector3 target = H.Head.position + H.Look.right * 0.6f + Vector3.down * 0.4f + H.Look.forward * 0.3f;
        pet.transform.position = Vector3.Lerp(pet.transform.position, target, Time.deltaTime * 5f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Balloon Head", "Big sphere over your head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBalloonHead : MenuMod
{
    private GameObject b;
    public override void OnEnable() => b = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.5f, Color.red, false);
    public override void OnDisable() { if (b) Object.Destroy(b); b = null; }
    public override void Update()
    {
        if (b && H.Ready) b.transform.position = H.Head.position + Vector3.up * 0.55f;
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Confetti Aura", "Continuous confetti around you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XConfettiAura : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.08f;
        var g = H.Prim(PrimitiveType.Cube, Vector3.one * 0.05f, Random.ColorHSV());
        g.transform.position = H.Head.position + Random.insideUnitSphere * 0.8f;
        H.AddSolidBody(g, 0.05f).velocity = Vector3.up * 2f + Random.insideUnitSphere;
        H.Track(g, 2f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Disco Floor", "Colored tiles under you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XDiscoFloor : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.4f;
        var g = H.Prim(PrimitiveType.Cube, new Vector3(0.5f, 0.03f, 0.5f), H.Rainbow(3f), false);
        g.transform.position = H.SnapToGround(H.Head.position) + Vector3.up * 0.02f;
        H.Track(g, 6f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Giant Hands Visual", "Large cubes on your hands", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XGiantHands : MenuMod
{
    private GameObject l, r;
    public override void OnEnable()
    {
        l = H.Prim(PrimitiveType.Cube, Vector3.one * 0.35f, Color.yellow, false);
        r = H.Prim(PrimitiveType.Cube, Vector3.one * 0.35f, Color.yellow, false);
    }
    public override void OnDisable()
    {
        if (l) Object.Destroy(l); if (r) Object.Destroy(r); l = r = null;
    }
    public override void Update()
    {
        if (!H.Ready) return;
        if (l) { l.transform.position = H.LHand.position; l.transform.rotation = H.LHand.rotation; }
        if (r) { r.transform.position = H.RHand.position; r.transform.rotation = H.RHand.rotation; }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Party Popper", "Burst of spheres from both hands", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPartyPopper : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        for (int h = 0; h < 2; h++)
        for (int i = 0; i < 20; i++)
        {
            var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.06f, Random.ColorHSV());
            g.transform.position = H.Hand(h == 0).position;
            H.AddSolidBody(g, 0.05f).velocity = H.Point(h == 0) * 6f + Random.insideUnitSphere * 3f;
            H.Track(g, 4f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Echo Clones", "Leaves frozen pose cubes behind you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XEchoClones : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.6f;
        var g = H.Prim(PrimitiveType.Capsule, new Vector3(0.3f, 0.5f, 0.3f), new Color(1f, 1f, 1f, 0.4f), false);
        g.transform.position = H.Head.position + Vector3.down * 0.5f;
        H.Track(g, 3f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Rubber Duck", "Yellow duck buddy follows", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XRubberDuck : MenuMod
{
    private GameObject duck;
    public override void OnEnable()
    {
        duck = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.2f, Color.yellow, false);
    }
    public override void OnDisable() { if (duck) Object.Destroy(duck); duck = null; }
    public override void Update()
    {
        if (!duck || !H.Ready) return;
        Vector3 t = H.Head.position - H.Look.forward * 0.8f + Vector3.up * 0.2f;
        duck.transform.position = Vector3.Lerp(duck.transform.position, t, Time.deltaTime * 4f);
    }
}

// ============================== PROJECTILES ==============================

[ModCategory(Cat.Projectiles)]
[ModInfo("Paint Ball Gun", "Random color paint balls", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPaintBall : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.12f;
        IdeaProj.Fire(PrimitiveType.Sphere, Random.ColorHSV(0f, 1f, 0.8f, 1f, 1f, 1f), 15f, 0.1f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Boulder Toss", "Big slow rocks", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBoulderToss : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.4f;
        IdeaProj.Fire(PrimitiveType.Sphere, new Color(0.4f, 0.3f, 0.2f), 8f, 0.45f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Star Shot", "Star-colored cubes", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XStarShot : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.1f;
        IdeaProj.Fire(PrimitiveType.Cube, Color.yellow, 18f, 0.1f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Water Balloon", "Blue balloons that arc", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XWaterBalloon : MenuMod
{
    private float n;
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!Enabled || !H.RTrig || Time.time < n) return;
        n = Time.time + 0.25f;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.2f, Color.blue);
        g.transform.position = H.RHand.position + H.Point(false) * 0.2f;
        var rb = H.AddSolidBody(g, 0.3f);
        rb.velocity = H.Point(false) * 9f + Vector3.up * 3f;
        H.Track(g, 8f);
    }
}

[ModCategory(Cat.Projectiles)]
[ModInfo("Meteor Shower", "Rocks fall from the sky", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XMeteorShower : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.3f;
        var g = H.Prim(PrimitiveType.Sphere, Vector3.one * Random.Range(0.15f, 0.4f), new Color(0.5f, 0.2f, 0.05f));
        g.transform.position = H.Head.position + new Vector3(Random.Range(-6f, 6f), 12f, Random.Range(-6f, 6f));
        H.AddSolidBody(g, 2f).velocity = Vector3.down * 15f;
        H.Track(g, 10f);
    }
}

// ============================== BUILDING ==============================

[ModCategory(Cat.Building)]
[ModInfo("Place Cube", "Places a solid cube where you look", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPlaceCube : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 p = H.Head.position + H.Look.forward * 2f;
        if (Physics.Raycast(H.Head.position, H.Look.forward, out RaycastHit hit, 8f))
            p = hit.point + hit.normal * 0.2f;
        var g = H.Prim(PrimitiveType.Cube, Vector3.one * 0.5f, Color.gray);
        g.transform.position = p;
        H.AddSolidBody(g, 3f).isKinematic = true;
        g.AddComponent<KeepSolid>();
        H.Track(g, 300f);
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Place Ramp", "Places a tilted ramp", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPlaceRamp : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 flat = H.Look.forward; flat.y = 0; flat.Normalize();
        var g = H.Prim(PrimitiveType.Cube, new Vector3(1.2f, 0.1f, 2f), new Color(0.6f, 0.5f, 0.3f));
        g.transform.position = H.SnapToGround(H.Head.position + flat * 2f) + Vector3.up * 0.5f;
        g.transform.rotation = Quaternion.LookRotation(flat) * Quaternion.Euler(-20f, 0f, 0f);
        H.AddSolidBody(g, 5f).isKinematic = true;
        g.AddComponent<KeepSolid>();
        H.Track(g, 300f);
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Place Pillar", "Tall pillar in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XPlacePillar : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 flat = H.Look.forward; flat.y = 0; flat.Normalize();
        var g = H.Prim(PrimitiveType.Cube, new Vector3(0.4f, 3f, 0.4f), Color.white);
        g.transform.position = H.SnapToGround(H.Head.position + flat * 2f) + Vector3.up * 1.5f;
        H.AddSolidBody(g, 8f).isKinematic = true;
        g.AddComponent<KeepSolid>();
        H.Track(g, 300f);
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Bridge Builder", "Lays a line of platform cubes", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XBridgeBuilder : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 flat = H.Look.forward; flat.y = 0; flat.Normalize();
        Vector3 start = H.SnapToGround(H.Head.position + flat) + Vector3.up * 0.15f;
        for (int i = 0; i < 10; i++)
        {
            var g = H.Prim(PrimitiveType.Cube, new Vector3(0.8f, 0.1f, 0.8f), new Color(0.5f, 0.5f, 0.55f));
            g.transform.position = start + flat * (i * 0.85f);
            H.AddSolidBody(g, 4f).isKinematic = true;
            g.AddComponent<KeepSolid>();
            H.Track(g, 300f);
        }
    }
}

[ModCategory(Cat.Building)]
[ModInfo("Clear Blocks", "Deletes tracked PVTX spawn objects", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XClearBlocks : MenuMod
{
    public override void Pressed() => H.ClearSpawned();
}

// ============================== ADVANTAGE ==============================

[ModCategory(Cat.Advantage)]
[ModInfo("Tag Pulse Wave", "Tags everyone in expanding range pulses", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XTagPulse : MenuMod
{
    private float n, range = 1.5f;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.5f;
        range += 0.8f;
        if (range > 10f) range = 1.5f;
        Vector3 me = H.Head.position;
        foreach (var r in Adv.OtherRigs())
            if (Vector3.Distance(me, Adv.RigHead(r)) < range)
                Adv.TryTag(r);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("Tag On Touch", "Tags players you physically near", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XTagOnTouch : MenuMod
{
    private float n;
    public override void Update()
    {
        if (!H.Ready || Time.time < n) return;
        n = Time.time + 0.2f;
        foreach (var r in Adv.OtherRigs())
            if (Vector3.Distance(H.Head.position, Adv.RigHead(r)) < 1.4f)
                Adv.TryTag(r);
    }
}

[ModCategory(Cat.Advantage)]
[ModInfo("ESP Distance Colors", "ESP lines colored by distance", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XEspDistColors : MenuMod
{
    private readonly List<LineRenderer> lines = [];
    public override void OnDisable()
    {
        foreach (var l in lines) if (l) Object.Destroy(l.gameObject);
        lines.Clear();
    }
    public override void Update()
    {
        if (!H.Ready) return;
        var rigs = Adv.OtherRigs();
        while (lines.Count < rigs.Count) lines.Add(H.MakeLine("EspD", Color.white, 0.008f));
        while (lines.Count > rigs.Count)
        {
            int i = lines.Count - 1;
            if (lines[i]) Object.Destroy(lines[i].gameObject);
            lines.RemoveAt(i);
        }
        for (int i = 0; i < rigs.Count; i++)
        {
            Vector3 to = Adv.RigHead(rigs[i]);
            float d = Vector3.Distance(H.Head.position, to);
            Color c = Color.Lerp(Color.red, Color.green, Mathf.Clamp01(d / 30f));
            lines[i].startColor = lines[i].endColor = c;
            lines[i].SetPosition(0, H.Head.position);
            lines[i].SetPosition(1, to);
        }
    }
}

// ============================== PHYSICS / WORLD ==============================

[ModCategory(Cat.Physics)]
[ModInfo("Super Bounce", "Bounce higher off the ground", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSuperBounce : MenuMod
{
    private bool wasG;
    public override void Update()
    {
        if (!H.Ready) return;
        bool g = Ground.Near(1.55f);
        if (!wasG && g && H.Rb.velocity.y < -1f)
            H.Rb.velocity = new Vector3(H.Rb.velocity.x, Mathf.Abs(H.Rb.velocity.y) * 1.2f + 2f, H.Rb.velocity.z);
        wasG = g;
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Heavy Monke", "Increased mass feel via downward force", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XHeavyMonke : MenuMod
{
    public override void FixedUpdate()
    {
        if (H.Ready) H.Rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
    }
}

[ModCategory(Cat.Physics)]
[ModInfo("Feather Fall", "Slow safe descent", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XFeatherFall : MenuMod
{
    public override void FixedUpdate()
    {
        if (!H.Ready) return;
        if (H.Rb.velocity.y < -2f)
            H.Rb.velocity = new Vector3(H.Rb.velocity.x, -2f, H.Rb.velocity.z);
    }
}

[ModCategory(Cat.World)]
[ModInfo("Clear Weather Soft", "Disables fog and brightens ambient", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XClearWeather : MenuMod
{
    public override void Update()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientIntensity = 1.2f;
    }
}

[ModCategory(Cat.World)]
[ModInfo("Storm Mood", "Dark foggy ambient", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XStormMood : MenuMod
{
    public override void Update()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.15f, 0.15f, 0.2f);
        RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.3f);
    }
}

// ============================== TOOLS / SAFETY / PLAYER ==============================

[ModCategory(Cat.Tools)]
[ModInfo("Stopwatch Reset", "Resets a simple session timer display", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XStopwatchReset : MenuMod
{
    public new static float Start = Time.unscaledTime;
    public override void Pressed() => Start = Time.unscaledTime;
}

[ModCategory(Cat.Tools)]
[ModInfo("Session Timer HUD", "Shows time since stopwatch reset", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSessionTimerHud : HudMod
{
    protected override Vector3 Offset => new(0f, 0.5f, 0.7f);
    protected override string Line()
    {
        float t = Time.unscaledTime - XStopwatchReset.Start;
        return $"t {t:F1}s";
    }
}

[ModCategory(Cat.Tools)]
[ModInfo("Log Player Count", "Logs how many other rigs are found", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XLogPlayerCount : MenuMod
{
    public override void Pressed() => Plugin.Log.LogInfo("Others: " + Adv.OtherRigs().Count);
}

[ModCategory(Cat.Safety)]
[ModInfo("Soft Panic", "Disables movement-ish feel by zeroing velocity", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XSoftPanic : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready) H.Rb.velocity = Vector3.zero;
        SoundBoard.Registry?.DisableAll();
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Hand Magnet", "Hands gently pull toward each other", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XHandMagnet : MenuMod
{
    public override void LateUpdate()
    {
        if (!H.Ready || H.LHand == null || H.RHand == null) return;
        Vector3 mid = (H.LHand.position + H.RHand.position) * 0.5f;
        H.LHand.position = Vector3.Lerp(H.LHand.position, mid, 0.02f);
        H.RHand.position = Vector3.Lerp(H.RHand.position, mid, 0.02f);
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Tall Stance", "Offsets head upward visually with a marker", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XTallStance : MenuMod
{
    private GameObject marker;
    public override void OnEnable() => marker = H.Prim(PrimitiveType.Sphere, Vector3.one * 0.1f, Color.white, false);
    public override void OnDisable() { if (marker) Object.Destroy(marker); marker = null; }
    public override void Update()
    {
        if (marker && H.Ready) marker.transform.position = H.Head.position + Vector3.up * 0.8f;
    }
}

[ModCategory(Cat.Sound)]
[ModInfo("Click Spam", "Plays menu click while gripping", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XClickSpam : MenuMod
{
    private float n;
    public override string BindHint => "Grip";
    public override void Update()
    {
        if (!(H.LGrip || H.RGrip) || Time.time < n) return;
        n = Time.time + 0.15f;
        SoundBoard.PlayMenuClick();
    }
}

[ModCategory(Cat.Sound)]
[ModInfo("Open Sound Spam", "Plays open sound on A press", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XOpenSpam : MenuMod
{
    private bool was;
    public override string BindHint => "A";
    public override void Update()
    {
        bool now = H.A;
        if (now && !was) SoundBoard.PlayMenuOpen();
        was = now;
    }
}

[ModCategory(Cat.Utility)]
[ModInfo("Force Ground Snap", "Snaps you onto ground under you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XForceGroundSnap : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 p = H.SnapToGround(H.Head.position);
        H.Teleport(p + Vector3.up * 1.2f);
    }
}

[ModCategory(Cat.Utility)]
[ModInfo("Face North", "Resets Y rotation roughly toward world Z", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XFaceNorth : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        var t = GTPlayer.Instance.transform;
        Vector3 e = t.eulerAngles; e.y = 0f; t.eulerAngles = e;
    }
}
