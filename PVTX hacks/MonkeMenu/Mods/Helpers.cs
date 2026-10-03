using System;
using System.Collections.Generic;
using System.Reflection;
using GorillaLocomotion;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// Category names. Attribute args must be constants, so they live here.
public static class Cat
{
    public const string Movement = "Movement";
    public const string Physics  = "Physics";
    public const string World    = "World";
    public const string Player   = "Player";
    public const string Visual   = "Visual";
    public const string Info     = "Info";
    public const string Fun      = "Fun";
    public const string Sound    = "Sound";
    public const string Utility  = "Utility";
}

/// <summary>Colour list shared by the sky / ambient / fog colour mods. Index 0 is "Off".</summary>
public static class Palette
{
    public static readonly string[] Names =
            ["Off", "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Pink", "White", "Black",];

    public static readonly Color[] Colors =
    [
        Color.white, new(0.9f, 0.1f, 0.1f), new(1f, 0.55f, 0.05f), new(1f, 0.92f, 0.1f), new(0.1f, 0.8f, 0.2f),
        new(0.1f, 0.85f, 0.9f), new(0.1f, 0.25f, 0.95f), new(0.55f, 0.15f, 0.85f), new(1f, 0.4f, 0.75f),
        Color.white, Color.black,
    ];
}

/// <summary>Remembers a game value so we can scale it every frame without compounding or fighting the game.</summary>
public class Tweak
{
    private float orig, last;
    private bool  has;

    public float Apply(float current, float mult)
    {
        if (!has || !Mathf.Approximately(current, last)) { orig = current; has = true; }
        last = orig * mult;
        return last;
    }
}

/// <summary>
/// ALL game-API access lives here. If a Gorilla Tag update renames something
/// (hand transforms, input poller fields, etc.), you only fix it in this one file.
/// </summary>
public static class H
{
    private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    private static Rigidbody rb;
    private static Transform look;
    private static Shader    shader, spriteShader;

    public static float FlySpeed = 12f; // changed by the "Fly Speed" mod

    // Shared positions for the checkpoint / spawn buttons
    public static Vector3 CheckpointPos;
    public static bool    HasCheckpoint;
    public static Vector3 SpawnPos;
    private static bool   spawnSet;

    public static Rigidbody Rb
    {
        get
        {
            if (rb == null && GTPlayer.Instance != null)
            {
                rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                if (rb == null) rb = GTPlayer.Instance.GetComponentInChildren<Rigidbody>();
                if (rb == null) rb = GTPlayer.Instance.GetComponentInParent<Rigidbody>();
            }
            return rb;
        }
    }

    // Enough to open the menu (does not need the player rigidbody)
    public static bool MenuReady => GorillaTagger.Instance != null && ControllerInputPoller.instance != null &&
                                    GorillaTagger.Instance.headCollider != null;

    public static string WhyNotReady()
    {
        if (GorillaTagger.Instance == null) return "GorillaTagger.Instance is null";
        if (ControllerInputPoller.instance == null) return "ControllerInputPoller.instance is null";
        if (GorillaTagger.Instance.headCollider == null) return "headCollider is null";
        if (GTPlayer.Instance == null) return "GTPlayer.Instance is null";
        if (Rb == null) return "no Rigidbody found on GTPlayer";
        return "ready";
    }

    public static bool Ready => GTPlayer.Instance != null && GorillaTagger.Instance != null &&
                                ControllerInputPoller.instance != null && Rb != null;

    /// <summary>Remember where the player started so "Return To Spawn" works.</summary>
    public static void MarkSpawn()
    {
        if (spawnSet || !Ready) return;
        SpawnPos = Head.position;
        spawnSet = true;
    }

    // ---- Transforms -------------------------------------------------------------------
    public static Transform LHand => GorillaTagger.Instance.leftHandTransform;
    public static Transform RHand => GorillaTagger.Instance.rightHandTransform;
    public static Transform Head  => GorillaTagger.Instance.headCollider.transform;
    public static Transform Hand(bool left) => left ? LHand : RHand;

    /// <summary>
    /// The transform that really rotates with your view. The head collider doesn't always rotate,
    /// which makes "fly where I'm looking" go the wrong way, so use this for directions.
    /// </summary>
    public static Transform Look
    {
        get
        {
            if (look != null) return look;
            GorillaTagger g = GorillaTagger.Instance;
            if (g != null)
            {
                foreach (string n in new[] { "mainCamera", "MainCamera", })
                {
                    object o = null;
                    try
                    {
                        o = typeof(GorillaTagger).GetField(n, AnyMember)?.GetValue(g)
                         ?? typeof(GorillaTagger).GetProperty(n, AnyMember)?.GetValue(g);
                    }
                    catch { /* try the next name */ }

                    if (o is GameObject go) { look = go.transform; break; }
                    if (o is Camera c)      { look = c.transform;  break; }
                    if (o is Transform t)   { look = t;            break; }
                }
            }
            if (look == null && Camera.main != null) look = Camera.main.transform;
            return look != null ? look : Head;
        }
    }

    // Direction the palm faces (for wall walk / spider monke). Flip the sign here if it feels backwards.
    public static Vector3 Palm(bool left)  => left ? -LHand.right : RHand.right;
    // Direction the fingers point (for rocket hands / guns). Flip the sign here if backwards.
    public static Vector3 Point(bool left) => Hand(left).forward;

    // ---- Input ------------------------------------------------------------------------
    private static ControllerInputPoller In => ControllerInputPoller.instance;
    public static bool Grip(bool left)    => left ? In.leftGrab : In.rightGrab;
    public static bool LGrip              => In.leftGrab;
    public static bool RGrip              => In.rightGrab;
    public static bool Trigger(bool left) => (left ? In.leftControllerIndexFloat : In.rightControllerIndexFloat) > 0.5f;
    public static bool LTrig              => Trigger(true);
    public static bool RTrig              => Trigger(false);
    public static bool A                  => In.rightControllerPrimaryButton;
    public static bool B                  => In.rightControllerSecondaryButton;
    public static bool X                  => In.leftControllerPrimaryButton;
    public static bool Y                  => In.leftControllerSecondaryButton;
    public static Vector2 LStick          => In.leftControllerPrimary2DAxis;
    public static Vector2 RStick          => In.rightControllerPrimary2DAxis;

    // ---- Utility ----------------------------------------------------------------------
    public static Color Rainbow(float speed = 1f, float offset = 0f) =>
            Color.HSVToRGB((Time.time * speed + offset) % 1f, 1f, 1f);

    public static void Teleport(Vector3 headTarget)
    {
        Vector3 delta = headTarget - Head.position;
        Rb.velocity = Vector3.zero;
        GTPlayer.Instance.transform.position += delta;
        Physics.SyncTransforms();
    }

    // ---- Materials --------------------------------------------------------------------
    private static readonly string[] ShaderNames =
            ["Universal Render Pipeline/Unlit", "Unlit/Color", "Sprites/Default", "GorillaTag/UberShader", "Standard",];

    private static Shader SafeShader()
    {
        if (shader != null) return shader;
        foreach (string name in ShaderNames)
        {
            Shader s = Shader.Find(name);
            if (s != null) return shader = s;
        }
        return null;
    }

    /// <summary>A flat-coloured material. Never returns null even if every shader was stripped from the build.</summary>
    public static Material Mat(Color c) => Build(SafeShader(), c);

    /// <summary>Material for trails and lines (needs a shader that honours vertex colours).</summary>
    public static Material SpriteMat(Color c)
    {
        if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
        return Build(spriteShader != null ? spriteShader : SafeShader(), c);
    }

    private static Material Build(Shader s, Color c)
    {
        Material m;
        if (s != null) m = new Material(s);
        else
        {
            // Last resort: borrow the default material of a temporary primitive.
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
            Object.Destroy(tmp);
        }
        m.color = c;
        return m;
    }

    public static GameObject Prim(PrimitiveType type, Vector3 scale, Color color, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.localScale = scale;

        if (!collider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
        }

        go.GetComponent<Renderer>().material = Mat(color);
        return go;
    }

    public static LineRenderer MakeLine(string name, Color color, float width)
    {
        GameObject go = new(name);
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.positionCount = 2;
        l.startWidth = l.endWidth = width;
        l.material = SpriteMat(color);
        l.startColor = l.endColor = color;
        return l;
    }

    public static TextMeshPro MakeHud(string name, Vector3 localPos)
    {
        GameObject go = new(name);
        go.transform.SetParent(Look, false);
        go.transform.localPosition = localPos;
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize  = 0.3f;
        t.color     = Color.white;
        t.rectTransform.sizeDelta = new Vector2(2f, 0.4f);
        return t;
    }

    // ---- Spawned objects (so "Clear Spawned", Gravity Gun and Explode can find them) ---
    public static readonly List<GameObject> Spawned = [];

    public static GameObject Track(GameObject go, float life = 10f)
    {
        Spawned.RemoveAll(g => g == null);
        while (Spawned.Count > 400)
        {
            if (Spawned[0] != null) Object.Destroy(Spawned[0]);
            Spawned.RemoveAt(0);
        }
        Spawned.Add(go);
        Object.Destroy(go, life);
        return go;
    }

    public static void ClearSpawned()
    {
        foreach (GameObject g in Spawned)
            if (g != null) Object.Destroy(g);
        Spawned.Clear();
    }
}

/// <summary>Looks things up by name so a game update that renames them doesn't stop the plugin compiling.</summary>
public static class Net
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static Type Find(string fullName)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type t = a.GetType(fullName, false);
                if (t != null) return t;
            }
            catch { /* some assemblies refuse to be inspected */ }
        }
        return null;
    }

    private static object Member(object target, Type type, string name)
    {
        try
        {
            PropertyInfo p = type.GetProperty(name, Any);
            if (p != null) return p.GetValue(target);
            FieldInfo f = type.GetField(name, Any);
            return f?.GetValue(target);
        }
        catch { return null; }
    }

    /// <summary>Leave the current room. Tries the game's own method first, then Photon directly.</summary>
    public static bool Disconnect()
    {
        try
        {
            Type ns = Find("NetworkSystem");
            object inst = ns == null ? null : Member(null, ns, "Instance");
            MethodInfo m = ns?.GetMethod("ReturnToSinglePlayer", Any, null, Type.EmptyTypes, null);
            if (inst != null && m != null) { m.Invoke(inst, null); return true; }
        }
        catch (Exception e) { Plugin.Log.LogWarning("NetworkSystem disconnect failed: " + e.Message); }

        try
        {
            Type pn = Find("Photon.Pun.PhotonNetwork");
            MethodInfo m = pn?.GetMethod("Disconnect", Any, null, Type.EmptyTypes, null);
            if (m != null) { m.Invoke(null, null); return true; }
        }
        catch (Exception e) { Plugin.Log.LogWarning("Photon disconnect failed: " + e.Message); }

        Plugin.Log.LogWarning("Disconnect: couldn't find a way to leave the room in this game version.");
        return false;
    }

    public static string RoomName()
    {
        try
        {
            Type pn = Find("Photon.Pun.PhotonNetwork");
            object room = pn == null ? null : Member(null, pn, "CurrentRoom");
            if (room == null) return "";
            return Member(room, room.GetType(), "Name") as string ?? "";
        }
        catch { return ""; }
    }

    public static string Ping()
    {
        try
        {
            Type pn = Find("Photon.Pun.PhotonNetwork");
            MethodInfo m = pn?.GetMethod("GetPing", Any, null, Type.EmptyTypes, null);
            if (m != null) return m.Invoke(null, null) + " ms";
        }
        catch { /* fall through */ }
        return "? ms";
    }

    public static string Room()
    {
        try
        {
            Type pn = Find("Photon.Pun.PhotonNetwork");
            object room = pn == null ? null : Member(null, pn, "CurrentRoom");
            if (room == null) return "No room";
            Type rt = room.GetType();
            return $"{Member(room, rt, "Name")}  ({Member(room, rt, "PlayerCount")} players)";
        }
        catch { return "No room"; }
    }
}

/// <summary>Base for incremental mods: labels list in, cycling + save-loading handled.</summary>
public abstract class IncrementalMod : MenuMod
{
    protected abstract string[] Labels { get; }
    public override string ModName => AssociatedAttribute.Name + Labels[IncrementalValue];

    public override void Increment()
    {
        IncrementalValue = (IncrementalValue + 1) % Labels.Length;
        Changed();
    }

    public override void Decrement()
    {
        IncrementalValue = (IncrementalValue - 1 + Labels.Length) % Labels.Length;
        Changed();
    }

    public override void OnIncrementalStateLoaded() => Changed();
    protected virtual void Changed() { }
}

/// <summary>A button that needs a second tap within a couple of seconds, so you can't hit it by accident.</summary>
public abstract class ConfirmMod : MenuMod
{
    private float armedUntil;
    protected abstract string Idle { get; }
    protected abstract void Run();

    public override string ModName => Time.unscaledTime < armedUntil ? "SURE?  TAP AGAIN" : Idle;

    public override void Pressed()
    {
        if (Time.unscaledTime < armedUntil) { armedUntil = 0f; Run(); }
        else armedUntil = Time.unscaledTime + 2.5f;
    }
}

/// <summary>Floating text HUD in front of your face. Subclasses just return the text.</summary>
public abstract class HudMod : MenuMod
{
    private TextMeshPro text;
    protected abstract Vector3 Offset { get; }
    protected abstract string  Line();

    public override void OnDisable()
    {
        if (text != null) Object.Destroy(text.gameObject);
        text = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;
        if (text == null) text = H.MakeHud(GetType().Name, Offset);
        text.text = Line();
    }
}

/// <summary>Base for hand platforms (grip = spawn a platform under that hand).</summary>
public abstract class PlatformBase : MenuMod
{
    private readonly GameObject[] plats = new GameObject[2];
    protected virtual bool  Visible => true;
    protected virtual Color Tint(int i) => new(0.2f, 0.6f, 1f);

    public override void OnDisable()
    {
        for (int i = 0; i < 2; i++) Kill(i);
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
                    plats[i] = H.Prim(PrimitiveType.Cube, new Vector3(0.28f, 0.03f, 0.28f), Tint(i));
                    plats[i].AddComponent<KeepSolid>(); // the No Collide mods leave our own platforms alone
                    plats[i].GetComponent<Renderer>().enabled = Visible;
                }

                Transform hand = H.Hand(left);
                plats[i].transform.position = hand.TransformPoint(0f, -0.05f, 0f);
                plats[i].transform.rotation = hand.rotation;
                plats[i].GetComponent<Renderer>().material.color = Tint(i);
            }
            else Kill(i);
        }
    }

    private void Kill(int i)
    {
        if (plats[i] != null) Object.Destroy(plats[i]);
        plats[i] = null;
    }
}

/// <summary>Objects arranged in a ring that follows your head (crown, halo...).</summary>
public abstract class RingBase : MenuMod
{
    protected abstract int           Count  { get; }
    protected abstract float         Radius { get; }
    protected abstract float         Height { get; }
    protected abstract float         Size   { get; }
    protected virtual  float         Spin   => 0f;
    protected virtual  PrimitiveType Shape  => PrimitiveType.Cube;
    protected abstract Color         Tint(int i);

    private readonly List<GameObject> bits = [];

    public override void OnEnable()
    {
        for (int i = 0; i < Count; i++)
            bits.Add(H.Prim(Shape, Vector3.one * Size, Tint(i), false));
    }

    public override void OnDisable()
    {
        foreach (GameObject g in bits) if (g != null) Object.Destroy(g);
        bits.Clear();
    }

    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 c = H.Head.position + Vector3.up * Height;
        for (int i = 0; i < bits.Count; i++)
        {
            float a = Time.time * Spin + i * Mathf.PI * 2f / bits.Count;
            bits[i].transform.position = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Radius;
            bits[i].GetComponent<Renderer>().material.color = Tint(i);
        }
    }
}

/// <summary>Little particles that fall around you (snow, rain, confetti). World-anchored so you get proper parallax.</summary>
public abstract class FallingBase : MenuMod
{
    protected abstract Vector3 Fall  { get; }
    protected abstract Vector3 Scale { get; }
    protected abstract Color   Tint(int i);
    protected virtual  PrimitiveType Shape => PrimitiveType.Sphere;
    protected const int Count = 60;

    private readonly List<Transform> bits = [];
    private readonly Vector3[] pos = new Vector3[Count];

    public override void OnEnable()
    {
        for (int i = 0; i < Count; i++)
        {
            bits.Add(H.Prim(Shape, Scale, Tint(i), false).transform);
            pos[i] = Vector3.zero;
        }
        if (H.Ready) for (int i = 0; i < Count; i++) pos[i] = Spawn(true);
    }

    public override void OnDisable()
    {
        foreach (Transform t in bits) if (t != null) Object.Destroy(t.gameObject);
        bits.Clear();
    }

    private static Vector3 Spawn(bool anywhere) =>
            H.Head.position + new Vector3(Random.Range(-5f, 5f), anywhere ? Random.Range(-2f, 5f) : 5f, Random.Range(-5f, 5f));

    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 head = H.Head.position;
        for (int i = 0; i < bits.Count; i++)
        {
            pos[i] += Fall * Time.deltaTime;
            Vector3 d = pos[i] - head;
            if (pos[i].y < head.y - 2.5f || d.x * d.x + d.z * d.z > 36f) pos[i] = Spawn(false);
            bits[i].position = pos[i];
        }
    }
}
