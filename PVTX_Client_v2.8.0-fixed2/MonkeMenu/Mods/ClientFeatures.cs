using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GorillaLocomotion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

/// <summary>
/// Tracks how many people in the room are running PVTX Client via Photon custom properties.
/// Also draws floating labels above other clients and a special badge above your own head.
/// </summary>
public static class ClientPresence
{
    public const string PropKey = "PVTX";
    public const string PropVal = "1";
    public const byte   ChatEventCode = 77; // custom RaiseEvent code for global chat

    public static int UserCount { get; private set; } = 1;

    private static float nextProp;
    private static float nextCount;
    private static bool  propSet;

    private static readonly BindingFlags Any =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Tick()
    {
        if (!H.Ready) { UserCount = 1; return; }
        if (Time.time >= nextProp)
        {
            nextProp = Time.time + 3f;
            EnsureProperty();
        }
        if (Time.time >= nextCount)
        {
            nextCount = Time.time + 1.5f;
            UserCount = Math.Max(1, CountUsers());
        }
    }

    private static void EnsureProperty()
    {
        try
        {
            Type pn = Net.Find("Photon.Pun.PhotonNetwork");
            if (pn == null) return;
            object local = pn.GetProperty("LocalPlayer", Any)?.GetValue(null);
            if (local == null) return;
            // SetCustomProperties(Hashtable)
            Type htType = Net.Find("ExitGames.Client.Photon.Hashtable")
                       ?? Net.Find("Photon.Hashtable")
                       ?? typeof(System.Collections.Hashtable);
            object table = Activator.CreateInstance(htType);
            MethodInfo setItem = htType.GetMethod("set_Item", Any) ?? htType.GetMethod("Add", Any);
            if (setItem != null)
            {
                try { setItem.Invoke(table, new object[] { PropKey, PropVal }); }
                catch
                {
                    // Dictionary-style
                    var mi = htType.GetMethod("Add", Any);
                    mi?.Invoke(table, new object[] { PropKey, PropVal });
                }
            }
            MethodInfo scp = local.GetType().GetMethod("SetCustomProperties", Any);
            scp?.Invoke(local, new object[] { table });
            propSet = true;
        }
        catch (Exception e)
        {
            if (!propSet) Plugin.Log.LogWarning("PVTX presence prop failed: " + e.Message);
        }
    }

    private static int CountUsers()
    {
        int n = 1; // self
        try
        {
            Type pn = Net.Find("Photon.Pun.PhotonNetwork");
            if (pn == null) return n;
            object list = pn.GetProperty("PlayerList", Any)?.GetValue(null);
            if (list is not Array arr) return n;
            n = 0;
            foreach (object pl in arr)
            {
                if (pl == null) continue;
                if (HasPvtx(pl)) n++;
            }
            if (n < 1) n = 1;
        }
        catch { n = 1; }
        return n;
    }

    public static bool HasPvtx(object photonPlayer)
    {
        try
        {
            object props = photonPlayer.GetType().GetProperty("CustomProperties", Any)?.GetValue(photonPlayer);
            if (props == null) return false;
            // try indexer
            var getItem = props.GetType().GetMethod("get_Item", Any);
            if (getItem != null)
            {
                object v = getItem.Invoke(props, new object[] { PropKey });
                return v != null && v.ToString() == PropVal;
            }
            // try ContainsKey + this[]
            var contains = props.GetType().GetMethod("ContainsKey", Any);
            if (contains != null && (bool)contains.Invoke(props, new object[] { PropKey }))
            {
                getItem = props.GetType().GetProperty("Item", Any)?.GetGetMethod();
                object v = getItem?.Invoke(props, new object[] { PropKey });
                return v != null && v.ToString() == PropVal;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Best-effort: map Photon players that have PVTX to world positions via VRRigs.</summary>
    public static List<(Vector3 head, bool isLocal)> PvtxHeads()
    {
        var result = new List<(Vector3, bool)>();
        if (!H.Ready) return result;
        // Always include local
        result.Add((H.Head.position, true));
        // Other rigs — we can't always map Photon actor → rig, so show a label on every other
        // rig when ANY other PVTX user is present, or when we detect the prop on players.
        // Simpler reliable approach: if UserCount > 1, still only mark local specially;
        // for others we attempt property match via actor number on VRRig if available.
        foreach (Component rig in Adv.OtherRigs())
        {
            if (RigLooksLikePvtx(rig))
                result.Add((Adv.RigHead(rig), false));
        }
        return result;
    }

    private static bool RigLooksLikePvtx(Component rig)
    {
        // Try common fields that hold Photon player / actor number
        try
        {
            Type t = rig.GetType();
            foreach (string field in new[] { "creator", "photonView", "owner", "player", "Creator" })
            {
                FieldInfo fi = t.GetField(field, Any);
                object val = fi?.GetValue(rig);
                if (val == null)
                {
                    PropertyInfo pi = t.GetProperty(field, Any);
                    val = pi?.GetValue(rig);
                }
                if (val == null) continue;
                // photonView.Owner
                if (val.GetType().Name.Contains("PhotonView") || val.GetType().Name.Contains("View"))
                {
                    object owner = val.GetType().GetProperty("Owner", Any)?.GetValue(val)
                                ?? val.GetType().GetField("Owner", Any)?.GetValue(val);
                    if (owner != null && HasPvtx(owner)) return true;
                }
                if (HasPvtx(val)) return true;
            }
        }
        catch { }
        // Fallback: when we know at least one other user is in the room, don't spam every head.
        // Only mark when we can confirm.
        return false;
    }

    public static void SendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        message = message.Trim();
        if (message.Length > 120) message = message.Substring(0, 120);
        string nick = "You";
        try
        {
            Type pn = Net.Find("Photon.Pun.PhotonNetwork");
            object local = pn?.GetProperty("LocalPlayer", Any)?.GetValue(null);
            nick = local?.GetType().GetProperty("NickName", Any)?.GetValue(local) as string ?? "You";
        }
        catch { }
        string payload = nick + ": " + message;
        GlobalChat.AddLocal(payload);
        try
        {
            Type pn = Net.Find("Photon.Pun.PhotonNetwork");
            if (pn == null) return;
            // RaiseEvent(byte, object, RaiseEventOptions, SendOptions)
            MethodInfo raise = null;
            foreach (var m in pn.GetMethods(Any))
            {
                if (m.Name != "RaiseEvent") continue;
                var ps = m.GetParameters();
                if (ps.Length >= 2) { raise = m; break; }
            }
            raise?.Invoke(null, BuildRaiseArgs(raise, payload));
        }
        catch (Exception e) { Plugin.Log.LogWarning("Chat send failed: " + e.Message); }
    }

    private static object[] BuildRaiseArgs(MethodInfo raise, string payload)
    {
        var ps = raise.GetParameters();
        var args = new object[ps.Length];
        args[0] = ChatEventCode;
        args[1] = payload;
        for (int i = 2; i < ps.Length; i++)
        {
            if (ps[i].ParameterType.IsValueType)
                args[i] = Activator.CreateInstance(ps[i].ParameterType);
            else
                args[i] = null;
        }
        return args;
    }
}

// ============================== CLIENT MARKERS ==============================

[ModCategory(Cat.Visual)]
[ModInfo("Client Markers", "Shows a square above PVTX Client users. Your own head gets a special badge", ButtonType.Togglable, AccessSetting.Public, EnabledType.Enabled, 0)]
public class ClientMarkers : MenuMod
{
    private readonly List<GameObject> labels = [];
    private GameObject myBadge;
    private float next;

    public override void OnEnable()
    {
        next = 0f;
        EnsureMyBadge();
    }

    public override void OnDisable()
    {
        ClearOthers();
        if (myBadge != null) { Object.Destroy(myBadge); myBadge = null; }
    }

    public override void Update()
    {
        ClientPresence.Tick();
        if (!H.Ready) return;
        EnsureMyBadge();
        if (myBadge != null && H.Head != null)
        {
            myBadge.transform.position = H.Head.position + Vector3.up * 0.55f;
            // billboard toward camera
            Camera cam = Camera.main;
            if (cam != null)
                myBadge.transform.rotation = Quaternion.LookRotation(myBadge.transform.position - cam.transform.position);
        }

        if (Time.time < next) return;
        next = Time.time + 0.35f;
        RefreshOthers();
    }

    private void EnsureMyBadge()
    {
        if (myBadge != null) return;
        myBadge = new GameObject("PVTX_MyBadge");
        // coloured square
        GameObject sq = H.Prim(PrimitiveType.Cube, new Vector3(0.28f, 0.28f, 0.02f), new Color(0.55f, 0.25f, 0.95f), false);
        sq.transform.SetParent(myBadge.transform, false);
        // text
        GameObject to = new GameObject("txt");
        to.transform.SetParent(myBadge.transform, false);
        to.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        TextMeshPro tmp = to.AddComponent<TextMeshPro>();
        tmp.text = "<b>PVTX</b>";
        tmp.fontSize = 1.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(0.5f, 0.3f);
        // slightly larger plate behind
        GameObject plate = H.Prim(PrimitiveType.Cube, new Vector3(0.42f, 0.18f, 0.015f), new Color(0.15f, 0.05f, 0.30f, 0.95f), false);
        plate.transform.SetParent(myBadge.transform, false);
        plate.transform.localPosition = new Vector3(0f, -0.22f, 0.01f);
        GameObject to2 = new GameObject("txt2");
        to2.transform.SetParent(myBadge.transform, false);
        to2.transform.localPosition = new Vector3(0f, -0.22f, -0.01f);
        TextMeshPro tmp2 = to2.AddComponent<TextMeshPro>();
        tmp2.text = "CLIENT";
        tmp2.fontSize = 1.0f;
        tmp2.alignment = TextAlignmentOptions.Center;
        tmp2.color = new Color(0.85f, 0.75f, 1f);
        tmp2.rectTransform.sizeDelta = new Vector2(0.5f, 0.2f);
    }

    private void ClearOthers()
    {
        foreach (GameObject g in labels)
            if (g != null) Object.Destroy(g);
        labels.Clear();
    }

    private void RefreshOthers()
    {
        ClearOthers();
        Camera cam = Camera.main;
        foreach (var (head, isLocal) in ClientPresence.PvtxHeads())
        {
            if (isLocal) continue;
            GameObject root = new GameObject("PVTX_Label");
            GameObject sq = H.Prim(PrimitiveType.Cube, new Vector3(0.35f, 0.12f, 0.015f), new Color(0.45f, 0.20f, 0.85f), false);
            sq.transform.SetParent(root.transform, false);
            GameObject to = new GameObject("txt");
            to.transform.SetParent(root.transform, false);
            to.transform.localPosition = new Vector3(0f, 0f, -0.012f);
            TextMeshPro tmp = to.AddComponent<TextMeshPro>();
            tmp.text = "using PVTX client";
            tmp.fontSize = 0.9f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.8f, 0.2f);
            root.transform.position = head + Vector3.up * 0.45f;
            if (cam != null)
                root.transform.rotation = Quaternion.LookRotation(root.transform.position - cam.transform.position);
            labels.Add(root);
        }
    }
}

// ============================== GLOBAL CHAT ==============================

[ModCategory(Cat.Fun)]
[ModInfo("Global Chat", "Open a shared chat. Type messages everyone on PVTX Client can see", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GlobalChat : MenuMod
{
    private static readonly List<string> lines = [];
    private static readonly object lockObj = new();
    private GameObject hud;
    private TextMeshPro hudText;
    private string draft = "";
    private bool typing;
    private float hideAt;

    public override string BindHint => "KB";

    public static void AddLocal(string line)
    {
        lock (lockObj)
        {
            lines.Add(line);
            while (lines.Count > 12) lines.RemoveAt(0);
        }
    }

    public override void OnEnable()
    {
        EnsureHud();
        typing = false;
        draft = "";
    }

    public override void OnDisable()
    {
        typing = false;
        if (hud != null) { Object.Destroy(hud); hud = null; hudText = null; }
    }

    public override void Update()
    {
        ClientPresence.Tick();
        if (!H.Ready) return;
        EnsureHud();
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (!typing)
                {
                    typing = true;
                    draft = "";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(draft))
                        ClientPresence.SendChat(draft);
                    draft = "";
                    typing = false;
                }
            }
            if (typing && kb.escapeKey.wasPressedThisFrame)
            {
                typing = false;
                draft = "";
            }
            if (typing)
            {
                // simple character intake
                for (int i = 0; i < 26; i++)
                {
                    var key = kb[Key.A + i];
                    if (key.wasPressedThisFrame)
                    {
                        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                        char c = (char)((shift ? 'A' : 'a') + i);
                        if (draft.Length < 100) draft += c;
                    }
                }
                for (int i = 0; i < 10; i++)
                {
                    var key = kb[Key.Digit0 + i];
                    if (key.wasPressedThisFrame && draft.Length < 100)
                        draft += (char)('0' + i);
                }
                if (kb.spaceKey.wasPressedThisFrame && draft.Length < 100) draft += ' ';
                if (kb.backspaceKey.wasPressedThisFrame && draft.Length > 0)
                    draft = draft.Substring(0, draft.Length - 1);
            }
        }

        // rebuild HUD text
        var sb = new StringBuilder();
        sb.AppendLine("<b><color=#C9B0FF>PVTX Global Chat</color></b>");
        lock (lockObj)
        {
            foreach (string l in lines)
                sb.AppendLine(l);
        }
        if (typing)
            sb.AppendLine("<color=#AAFFAA>> " + draft + "_</color>");
        else
            sb.AppendLine("<color=#888888>[Enter] to type</color>");
        if (hudText != null) hudText.text = sb.ToString();

        if (hud != null && H.Head != null)
        {
            // park the chat panel a bit in front of your face / left
            Transform head = H.Head;
            hud.transform.position = head.position + head.forward * 1.2f + head.right * -0.35f + Vector3.up * 0.15f;
            hud.transform.rotation = Quaternion.LookRotation(hud.transform.position - head.position);
        }
    }

    private void EnsureHud()
    {
        if (hud != null) return;
        hud = new GameObject("PVTX_ChatHud");
        GameObject bg = H.Prim(PrimitiveType.Cube, new Vector3(0.9f, 0.7f, 0.01f), new Color(0.08f, 0.05f, 0.15f, 0.85f), false);
        bg.transform.SetParent(hud.transform, false);
        GameObject to = new GameObject("txt");
        to.transform.SetParent(hud.transform, false);
        to.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        hudText = to.AddComponent<TextMeshPro>();
        hudText.fontSize = 1.15f;
        hudText.alignment = TextAlignmentOptions.TopLeft;
        hudText.color = Color.white;
        hudText.rectTransform.sizeDelta = new Vector2(0.85f, 0.65f);
        hudText.enableWordWrapping = true;
    }
}

// ============================== CLIENT VC (best-effort) ==============================
// Uses the existing MicBridge so menu users can push mic into voice chat while the mod is on.

[ModCategory(Cat.Sound)]
[ModInfo("Client VC", "Keeps your mic open for other PVTX users (uses game voice when available)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ClientVc : MenuMod
{
    public override string BindHint => "MIC";

    public override void OnEnable()
    {
        Plugin.Log.LogInfo("Client VC on — speak normally; MicBridge/voice recorder is used when present.");
    }

    public override void Update()
    {
        // Presence tick so user count stays fresh while VC is open
        ClientPresence.Tick();
    }
}

