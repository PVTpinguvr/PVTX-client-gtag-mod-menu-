using System.Collections.Generic;
using MonkeMenu.Mods;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonkeMenu.Core;

public class MenuManager : MonoBehaviour
{
    private ModRegistry registry;
    private VrMenu      vr;
    private bool        wasCombo;

    // Desktop / debug menu
    private bool    showGui;
    private Rect    window = new(30, 30, 540, 600);
    private int     tab;
    private Vector2 scroll;
    private string  search = "";

    private float nextLog;
    private bool  loggedReady, loggedCombo;

    private void Awake()
    {
        registry = new ModRegistry();
        vr       = new VrMenu(registry);
        SoundBoard.Init(this, registry);
        Plugin.Log.LogInfo("MenuManager started. Waiting for the game to be ready...");
    }

    private void Update()
    {
        // The game uses the NEW Input System, so UnityEngine.Input would throw every frame.
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.tabKey.wasPressedThisFrame) showGui = !showGui;
            if (kb.mKey.wasPressedThisFrame && H.MenuReady && !showGui) vr.Toggle();
        }
        if (!H.MenuReady)
        {
            if (Time.unscaledTime > nextLog)
            {
                nextLog = Time.unscaledTime + 5f;
                Plugin.Log.LogInfo("Not ready yet: " + H.WhyNotReady());
            }
            return;
        }

        if (!loggedReady)
        {
            loggedReady = true;
            Plugin.Log.LogInfo("Game ready for the menu. Mods ready: " + H.WhyNotReady());
        }

        H.MarkSpawn();

        bool combo = H.X && H.Y; // X + Y together
        if (combo && !loggedCombo) { loggedCombo = true; Plugin.Log.LogInfo("X+Y detected"); }
        if (combo && !wasCombo) vr.Toggle();
        wasCombo = combo;

        vr.Tick();
        if (H.Ready) registry.Tick(0);
    }

    private void LateUpdate()
    {
        if (H.MenuReady) vr.Follow(); // after the hands have moved this frame, so the menu stays glued to them
        if (H.Ready) registry.Tick(1);
    }

    private void FixedUpdate() { if (H.Ready) registry.Tick(2); }

    private void OnDestroy()
    {
        vr?.Close();
        registry?.DisableAll();
    }

    // ---- Desktop IMGUI (press Tab) ----------------------------------------------------------
    private void OnGUI()
    {
        if (!showGui) return;
        window = GUI.Window(0x4D4D, window, Draw, "PVTX hacks  (Tab to hide)");
    }

    private void Draw(int id)
    {
        string[] cats = System.Linq.Enumerable.ToArray(registry.Categories);
        if (cats.Length == 0) return;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(55));
        search = GUILayout.TextField(search);
        if (GUILayout.Button("X", GUILayout.Width(26))) search = "";
        if (GUILayout.Button("Disconnect", GUILayout.Width(90))) Net.Disconnect();
        GUILayout.EndHorizontal();

        tab = GUILayout.Toolbar(Mathf.Clamp(tab, 0, cats.Length - 1), cats);
        scroll = GUILayout.BeginScrollView(scroll);

        List<MenuMod> list = search.Trim().Length > 0 ? registry.Search(search) : registry.InCategory(cats[tab]);
        foreach (MenuMod mod in list)
        {
            GUILayout.BeginHorizontal();
            // Strip rich-text color tags for the desktop IMGUI (it doesn't support them).
            string label = System.Text.RegularExpressions.Regex.Replace(mod.MenuLabel, "<.*?>", "");
            switch (mod.Info.Type)
            {
                case ButtonType.Togglable:
                    bool on = GUILayout.Toggle(mod.Enabled, label);
                    if (on != mod.Enabled) registry.SetEnabled(mod, on);
                    break;
                case ButtonType.Incremental:
                    if (GUILayout.Button("<", GUILayout.Width(28))) registry.Activate(mod, true);
                    GUILayout.Label(label);
                    if (GUILayout.Button(">", GUILayout.Width(28))) registry.Activate(mod, false);
                    break;
                default:
                    if (GUILayout.Button(label)) registry.Activate(mod, false);
                    break;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(mod.Info.Description, GUI.skin.box);
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }
}
