using System;
using System.Collections.Generic;
using System.Linq;
using MonkeMenu.Mods;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkeMenu.Core;

/// <summary>Look of the menu. Changed by the Menu Theme / Menu Size / Menu Hand mods.</summary>
public static class MenuStyle
{
    public static float Scale = 0.5f;   // world size of the menu (1 = 0.86m tall)
    public static bool  RightHand;      // false = menu on your left hand, poke with your right

    public static float BallRadius = 0.012f; // finger ball size in metres (Utility > Finger Ball)
    public static float BallReach  = 0.05f;   // how far in front of the hand the ball sits, in metres

    // PVTX purple (default): darker frame, lighter rows
    public static Color Bg     = new(0.36f, 0.23f, 0.64f);
    public static Color Nav    = new(0.56f, 0.38f, 0.90f);
    public static Color Panel  = new(0.45f, 0.30f, 0.78f);
    public static Color Accent = new(0.72f, 0.55f, 1.00f);
    public static Color Action = new(0.56f, 0.38f, 0.90f);

    public static readonly Color On     = new(0.15f, 0.65f, 0.25f);
    public static readonly Color Off    = new(0.28f, 0.12f, 0.12f);
    public static readonly Color Danger = new(0.70f, 0.15f, 0.15f);

    public static string[] ThemeNames => ["PVTX Purple", "Dark", "Ocean", "Forest", "Berry",];

    public static void SetTheme(int i)
    {
        switch (i)
        {
            case 1:  Set(0.05f, 0.05f, 0.08f,  0.25f, 0.25f, 0.30f,  0.10f, 0.10f, 0.20f,  0.90f, 0.50f, 0.10f,  0.20f, 0.30f, 0.55f); break;
            case 2:  Set(0.02f, 0.08f, 0.12f,  0.10f, 0.30f, 0.40f,  0.05f, 0.20f, 0.30f,  0.10f, 0.80f, 0.90f,  0.10f, 0.40f, 0.60f); break;
            case 3:  Set(0.03f, 0.08f, 0.04f,  0.15f, 0.30f, 0.15f,  0.08f, 0.20f, 0.10f,  0.70f, 0.90f, 0.20f,  0.20f, 0.45f, 0.25f); break;
            case 4:  Set(0.10f, 0.03f, 0.08f,  0.35f, 0.12f, 0.30f,  0.25f, 0.08f, 0.20f,  1.00f, 0.40f, 0.70f,  0.50f, 0.20f, 0.50f); break;
            default: Set(0.36f, 0.23f, 0.64f,  0.56f, 0.38f, 0.90f,  0.45f, 0.30f, 0.78f,  0.72f, 0.55f, 1.00f,  0.56f, 0.38f, 0.90f); break;
        }
    }

    private static void Set(float br, float bg, float bb, float nr, float ng, float nb, float pr, float pg, float pb,
                            float ar, float ag, float ab, float cr, float cg, float cb)
    {
        Bg = new Color(br, bg, bb); Nav = new Color(nr, ng, nb); Panel = new Color(pr, pg, pb);
        Accent = new Color(ar, ag, ab); Action = new Color(cr, cg, cb);
    }
}

/// <summary>
/// PVTX hacks - a small menu that floats just above one of your hands. Open/close with X+Y.
/// Layout: title, FPS, "&lt;" / "&gt;" page rows, then 7 rows (home = categories, inside a category = Back + mods).
/// Poke the buttons with the ball on the finger of your other hand. The search button sits under the bottom-right corner.
/// </summary>
public class VrMenu
{
    private const float Width  = 0.52f, Height = 0.68f;
    private const float Inner  = 0.48f;
    private const float RowH   = 0.052f, Pitch = 0.0615f, FirstY = 0.19f;
    private const string EnabledTab = "Enabled Mods";

    private class Btn
    {
        public GameObject   Go;
        public Renderer     Rend;
        public TextMeshPro  Text;
        public Vector2      Pos, Size;
        public string       Id;
        public Color        LastColor;
        public Func<string> Label;
        public Func<Color>  Tint;
        public Action       Click;
    }

    private readonly ModRegistry registry;
    private readonly List<Btn>   buttons = [];
    private GameObject           root;
    private Renderer             bgRend;
    private GameObject           ball;
    private Renderer             ballRend;
    private Vector3              lastBallPos;
    private bool                 hasLastBall, ballHot;
    private int                  page, seenVersion, seenState;
    private string               current;            // null = home page, otherwise a category name or EnabledTab
    private bool                 searching, armed = true;
    private string               query = "", lastId;
    private float                ignoreUntil, fps = 60f, disconnectArmedUntil;

    public bool Open => root != null;

    public VrMenu(ModRegistry registry) => this.registry = registry;

    public void Toggle()
    {
        if (Open) Close(); else Show();
    }

    private void Show()
    {
        root = new GameObject("PVTX_VR");

        GameObject bg = H.Prim(PrimitiveType.Cube, new Vector3(Width, Height, 0.012f), MenuStyle.Bg, false);
        bg.transform.SetParent(root.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.008f);
        bgRend = bg.GetComponent<Renderer>();

        ball = H.Prim(PrimitiveType.Sphere, Vector3.one, MenuStyle.Accent, false);
        ballRend    = ball.GetComponent<Renderer>();
        hasLastBall = false;
        ballHot     = false;

        ignoreUntil = Time.unscaledTime + 0.5f; // don't press whatever the hand is over when it opens
        Follow();
        Build();
        SoundBoard.PlayMenuOpen();
    }

    public void Close()
    {
        if (root != null) Object.Destroy(root);
        if (ball != null) Object.Destroy(ball);
        root = null;
        ball = null;
        ballRend = null;
        buttons.Clear();
    }

    /// <summary>Where the finger ball is: a little in front of the poking hand.</summary>
    private static Vector3 BallPos()
    {
        Transform poke = H.Hand(MenuStyle.RightHand);
        return poke.position + poke.forward * MenuStyle.BallReach;
    }

    private void UpdateBall()
    {
        if (ball == null) return;
        ball.transform.position   = BallPos();
        ball.transform.localScale = Vector3.one * (MenuStyle.BallRadius * 2f);
    }

    /// <summary>Keeps the menu stuck to the hand. Call from LateUpdate so it doesn't lag a frame behind.</summary>
    public void Follow()
    {
        if (!Open) return;

        Transform hand = H.Hand(!MenuStyle.RightHand);
        Transform head = H.Head;

        Vector3 toHead = head.position - hand.position;
        // a bit of extra lift so the search button hanging under the menu doesn't sit inside your hand
        Vector3 pos = hand.position + toHead.normalized * 0.08f
                      + Vector3.up * ((Height * 0.5f + 0.09f) * MenuStyle.Scale + 0.02f);

        Vector3 fwd = pos - head.position; // +Z points away from you, so the text faces you
        if (fwd.sqrMagnitude < 0.0001f) fwd = H.Look.forward;

        root.transform.localScale = Vector3.one * MenuStyle.Scale;
        root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd.normalized, Vector3.up));

        UpdateBall();
    }

    // ---- Layout ---------------------------------------------------------------------------
    /// <summary>Splits one row into pieces with the given relative widths.</summary>
    private static (float x, float w)[] Cut(params float[] weights)
    {
        const float gap = 0.008f;
        float sum = weights.Sum();
        float usable = Inner - gap * (weights.Length - 1);
        var res = new (float x, float w)[weights.Length];
        float x = -Inner / 2f;
        for (int i = 0; i < weights.Length; i++)
        {
            float w = usable * weights[i] / sum;
            res[i] = (x + w / 2f, w);
            x += w + gap;
        }
        return res;
    }

    /// <summary>Row 0 = "&lt;", row 1 = "&gt;", rows 2-8 = the seven item rows.</summary>
    private static float RowY(int row) => FirstY - row * Pitch;

    private static Color ModTint(MenuMod m) => m.Info.Type switch
    {
        ButtonType.Togglable   => m.Enabled ? MenuStyle.On : MenuStyle.Off,
        ButtonType.Incremental => m.IncrementalValue != 0 ? MenuStyle.On : MenuStyle.Action,
        _                      => MenuStyle.Action,
    };

    private static Color NavTint()   => MenuStyle.Nav;
    private static Color PanelTint() => MenuStyle.Panel;
    private static Color FrameTint() => MenuStyle.Bg;

    private void GoHome() { current = null; query = ""; searching = false; page = 0; Build(); }

    private void Build()
    {
        foreach (Btn b in buttons) Object.Destroy(b.Go);
        buttons.Clear();
        seenVersion = registry.Version;
        seenState   = registry.StateVersion;

        // Header: PVTX Client title, FPS, then how many people are using the client.
        Add(0f, 0.300f, Inner, 0.055f,
            () => $"<b>PVTX Client</b> <color=#B9B0D0>[{page + 1}]</color>", FrameTint, null);
        Add(0f, 0.255f, Inner, 0.028f, () => "FPS: " + Mathf.RoundToInt(fps), FrameTint, null);
        Add(0f, 0.228f, Inner, 0.026f,
            () => "Users: " + MonkeMenu.Mods.ClientPresence.UserCount, FrameTint, null);

        AddSearchButton();
        AddDisconnectButton();

        if (searching) { BuildKeyboard(); return; }

        // ---- What goes in the item rows? ----
        bool results = query.Length > 0;
        bool home    = current == null && !results;
        bool enabledView = current == EnabledTab && !results;

        List<Action<float>> items = [];
        if (home)
        {
            items.Add(y => Add(0f, y, Inner, RowH, () => $"{EnabledTab} ({registry.EnabledMods().Count})", NavTint,
                               () => { current = EnabledTab; page = 0; Build(); }));
            foreach (string cat in registry.Categories)
            {
                string c = cat;
                items.Add(y => Add(0f, y, Inner, RowH, () => c, NavTint, () => { current = c; page = 0; Build(); }));
            }
        }
        else
        {
            List<MenuMod> list = results                 ? registry.Search(query)
                                 : current == EnabledTab ? registry.EnabledMods()
                                                         : registry.InCategory(current);
            foreach (MenuMod m in list)
            {
                MenuMod mod = m;
                items.Add(y => AddModRow(mod, y, enabledView));
            }
            if (list.Count == 0)
            {
                string msg = results ? "Nothing found" : enabledView ? "No mods are on right now" : "Empty";
                items.Add(y => Add(0f, y, Inner, RowH, () => msg, PanelTint, null));
            }
        }

        int perPage = home ? 7 : 6;                       // non-home pages keep row 1 for "Back"
        int pages   = Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)perPage));
        page        = Mathf.Clamp(page, 0, pages - 1);

        // ---- Page rows, like the reference: "<" then ">" as full-width rows ----
        Add(0f, RowY(0), Inner, RowH, () => "<", NavTint, () => { page = (page - 1 + pages) % pages; Build(); });
        Add(0f, RowY(1), Inner, RowH, () => ">", NavTint, () => { page = (page + 1) % pages; Build(); });

        int slot = 2;
        if (!home)
        {
            Add(0f, RowY(slot), Inner, RowH,
                () => results ? "Back  (search)" : "Back  (" + current + ")", PanelTint, GoHome);
            slot++;
        }

        for (int i = page * perPage; i < items.Count && slot <= 8; i++, slot++)
            items[i](RowY(slot));
    }

    private void AddModRow(MenuMod mod, float y, bool enabledView = false)
    {
        if (enabledView)
        {
            // Enabled Mods: one big button per running mod. Tap it and the mod turns off and drops off the list.
            Add(0f, y, Inner, RowH, () => mod.MenuLabel, () => MenuStyle.On,
                () => { registry.Disable(mod); Build(); });
        }
        else if (mod.Info.Type == ButtonType.Incremental)
        {
            var c = Cut(1f, 7f);
            Add(c[0].x, y, c[0].w, RowH, () => "<", NavTint, () => registry.Activate(mod, true));
            Add(c[1].x, y, c[1].w, RowH, () => mod.MenuLabel, () => ModTint(mod), () => registry.Activate(mod, false));
        }
        else
        {
            Add(0f, y, Inner, RowH, () => mod.MenuLabel, () => ModTint(mod), () => registry.Activate(mod, false));
        }
    }

    private void BuildKeyboard()
    {
        // row 0: what you typed.  rows 1-3: letters.  row 4: clear/space/done.  rows 5-8: first four results.
        Add(0f, RowY(0), Inner, RowH, () => query.Length == 0 ? "type to search..." : query + "_", PanelTint, null);

        string[] letters = ["qwertyuiop", "asdfghjkl", "zxcvbnm",];
        for (int r = 0; r < letters.Length; r++)
        {
            string s = letters[r];
            float y = RowY(r + 1);
            bool last = r == letters.Length - 1;

            float[] weights = Enumerable.Repeat(1f, s.Length).ToArray();
            if (last) weights = weights.Concat(new[] { 2f, }).ToArray();
            var cut = Cut(weights);

            for (int k = 0; k < s.Length; k++)
            {
                char ch = s[k];
                Add(cut[k].x, y, cut[k].w, RowH, () => ch.ToString().ToUpperInvariant(), NavTint,
                    () => { if (query.Length < 24) query += ch; page = 0; Build(); });
            }

            if (last)
                Add(cut[s.Length].x, y, cut[s.Length].w, RowH, () => "DEL", () => MenuStyle.Danger,
                    () => { if (query.Length > 0) query = query.Substring(0, query.Length - 1); Build(); });
        }

        float by = RowY(4);
        var b = Cut(2f, 3f, 2f);
        Add(b[0].x, by, b[0].w, RowH, () => "CLEAR", () => MenuStyle.Danger, () => { query = ""; Build(); });
        Add(b[1].x, by, b[1].w, RowH, () => "SPACE", NavTint,
            () => { if (query.Length > 0 && !query.EndsWith(" ")) query += " "; Build(); });
        Add(b[2].x, by, b[2].w, RowH, () => "DONE", () => MenuStyle.On, () => { searching = false; page = 0; Build(); });

        // The first few results stay usable while you type.
        List<MenuMod> results = registry.Search(query);
        for (int i = 0; i < 4 && i < results.Count; i++)
            AddModRow(results[i], RowY(5 + i));
    }

    /// <summary>Red Disconnect bar sitting on top of the menu. Tap twice so you can't hit it by accident.</summary>
    private void AddDisconnectButton()
    {
        Add(0f, Height / 2f + 0.032f, Inner, 0.05f,
            () => Time.unscaledTime < disconnectArmedUntil ? "SURE?  TAP AGAIN" : "Disconnect",
            () => MenuStyle.Danger,
            () =>
            {
                if (Time.unscaledTime < disconnectArmedUntil) { disconnectArmedUntil = 0f; Net.Disconnect(); }
                else disconnectArmedUntil = Time.unscaledTime + 2.5f;
            });
    }

    /// <summary>Square magnifier button hanging under the bottom-right corner, like the reference.</summary>
    private void AddSearchButton()
    {
        const float size = 0.09f;
        float x = Width / 2f - size / 2f - 0.005f;
        float y = -Height / 2f - size / 2f - 0.012f;
        Add(x, y, size, size, () => "", () => searching ? MenuStyle.Accent : MenuStyle.Nav,
            () => { searching = !searching; page = 0; Build(); });

        GameObject go = buttons[buttons.Count - 1].Go;
        Icon(go, PrimitiveType.Sphere, size, size, new Vector3(-0.009f, 0.010f, 0.004f), new Vector3(0.044f, 0.044f, 0.004f), Color.white, 0f);
        Icon(go, PrimitiveType.Sphere, size, size, new Vector3(-0.009f, 0.010f, 0.0075f), new Vector3(0.030f, 0.030f, 0.004f),
             searching ? MenuStyle.Accent : MenuStyle.Nav, 0f);
        Icon(go, PrimitiveType.Cube, size, size, new Vector3(0.016f, -0.016f, 0.004f), new Vector3(0.028f, 0.008f, 0.004f), Color.white, -45f);
    }

    /// <summary>Adds a little decoration shape to a button. Positions/sizes are in metres at menu scale 1.</summary>
    private static void Icon(GameObject parent, PrimitiveType type, float pw, float ph, Vector3 pos, Vector3 size,
                             Color color, float zRot)
    {
        GameObject g = H.Prim(type, Vector3.one, color, false);
        g.transform.SetParent(parent.transform, false);
        g.transform.localScale    = new Vector3(size.x / pw, size.y / ph, size.z / 0.008f);
        g.transform.localPosition = new Vector3(pos.x / pw, pos.y / ph, -pos.z / 0.008f); // -z = towards the viewer
        g.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
    }

    private void Add(float x, float y, float w, float h, Func<string> label, Func<Color> tint, Action click)
    {
        Color first = tint();
        GameObject go = H.Prim(PrimitiveType.Cube, new Vector3(w, h, 0.008f), first, false);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0f);

        GameObject tgo = new("Label");
        tgo.transform.SetParent(go.transform, false);
        tgo.transform.localScale    = new Vector3(1f / w, 1f / h, 1f / 0.008f); // undo parent's scale
        tgo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        TextMeshPro t = tgo.AddComponent<TextMeshPro>();
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.05f; t.fontSizeMax = 0.25f;
        t.enableWordWrapping = false;
        t.richText = true;
        t.color = Color.white;
        t.rectTransform.sizeDelta = new Vector2(w * 0.95f, h * 0.9f);
        t.text = label();

        buttons.Add(new Btn
        {
            Go = go, Rend = go.GetComponent<Renderer>(), Text = t,
            Pos = new Vector2(x, y), Size = new Vector2(w, h), Id = $"{x:F3},{y:F3}",
            LastColor = first, Label = label, Tint = tint, Click = click,
        });
    }

    // ---- Per-frame ------------------------------------------------------------------------
    public void Tick()
    {
        if (!Open) return;

        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);

        if (registry.Version != seenVersion) { Build(); return; }

        // Keep the home counter / Enabled list honest if a mod switched itself off while it's showing.
        if (registry.StateVersion != seenState)
        {
            seenState = registry.StateVersion;
            if (!searching && query.Length == 0 && (current == null || current == EnabledTab)) { Build(); return; }
        }

        if (bgRend != null) bgRend.material.color = MenuStyle.Bg;

        foreach (Btn b in buttons)
        {
            Color col = b.Tint();
            if (col != b.LastColor) { b.Rend.material.color = col; b.LastColor = col; }
            string l = b.Label();
            if (b.Text.text != l) b.Text.text = l;
        }

        UpdateBall();
        Vector3 ballNow = ball != null ? ball.transform.position : BallPos();

        if (Time.unscaledTime < ignoreUntil)
        {
            lastBallPos = ballNow;
            hasLastBall = true;
            return;
        }

        // The ball on the finger of the hand opposite the menu does the poking.
        // It checks a few points between last frame and now, so a fast poke can't skip through a button.
        Vector3 from = hasLastBall ? lastBallPos : ballNow;
        lastBallPos = ballNow;
        hasLastBall = true;

        float rLocal = MenuStyle.BallRadius / Mathf.Max(0.01f, MenuStyle.Scale);
        int hit = -1;
        float bestDist = float.MaxValue;

        const int samples = 4;
        for (int sIdx = 0; sIdx <= samples; sIdx++)
        {
            Vector3 p = root.transform.InverseTransformPoint(Vector3.Lerp(from, ballNow, sIdx / (float)samples));

            // Depth: the ball's surface has to reach the front of the menu (a little early is fine)
            // and not be way out the back.
            if (p.z < -(rLocal + 0.03f) || p.z > rLocal + 0.06f) continue;

            for (int i = 0; i < buttons.Count; i++)
            {
                Btn b = buttons[i];
                if (b.Click == null) continue;
                float dx = Mathf.Abs(p.x - b.Pos.x) - b.Size.x / 2f;
                float dy = Mathf.Abs(p.y - b.Pos.y) - b.Size.y / 2f;

                // Ball centre has to be over the button, give or take a little, so brushing past the
                // edge of a big ball doesn't press the neighbour.
                const float margin = 0.012f;
                if (dx > margin || dy > margin) continue;

                float d = Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f);
                // tie-break on distance to the button's middle so the closest one wins
                d += ((p.x - b.Pos.x) * (p.x - b.Pos.x) + (p.y - b.Pos.y) * (p.y - b.Pos.y)) * 0.001f;
                if (d < bestDist) { bestDist = d; hit = i; }
            }
        }

        SetBallHot(hit >= 0);

        if (hit < 0) { armed = true; lastId = null; return; }

        Btn hb = buttons[hit];
        if (hb.Id != lastId) { armed = true; lastId = hb.Id; } // sliding onto a different button counts as a new press

        if (armed)
        {
            armed = false;
            SoundBoard.PlayMenuClick();
            hb.Click(); // may call Build(), which rebuilds `buttons`
            // 0.2 second cooldown so accidental double-pokes / brush-past presses don't fire again
            ignoreUntil = Time.unscaledTime + 0.2f;
        }
    }

    /// <summary>The ball lights up while it is touching a button, so you can see what you are about to press.</summary>
    private void SetBallHot(bool hot)
    {
        if (ballRend == null || hot == ballHot) return;
        ballHot = hot;
        ballRend.material.color = hot ? Color.white : MenuStyle.Accent;
    }
}
