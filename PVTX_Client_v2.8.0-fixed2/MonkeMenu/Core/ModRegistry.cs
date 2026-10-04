using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonkeMenu.Mods;
using UnityEngine;

namespace MonkeMenu.Core;

/// <summary>Finds every mod, runs it safely, and saves/restores its state.</summary>
public class ModRegistry
{
    public readonly List<MenuMod>                   All        = [];
    public readonly List<string>                    Categories = [];
    private readonly Dictionary<string, List<MenuMod>> byCat   = new();
    private readonly HashSet<MenuMod>               broken     = [];
    private bool                                    restored;

    private static readonly string[] Order =
    [
        Cat.Lobby, Cat.Room, Cat.Safety, Cat.Movement, Cat.Physics, Cat.World, Cat.Player,
        Cat.Advantage, Cat.Visual, Cat.Projectiles, Cat.Building, Cat.Tools,
        Cat.Info, Cat.Fun, Cat.Sound, Cat.Utility,
    ];

    /// <summary>Goes up whenever mods are added/removed so the menu knows to redraw.</summary>
    public int Version { get; private set; }

    /// <summary>Goes up whenever any mod is switched on/off or changes value (the Enabled tab watches this).</summary>
    public int StateVersion { get; private set; }

    public ModRegistry()
    {
        Type[] types;
        try { types = Assembly.GetExecutingAssembly().GetTypes(); }
        catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

        foreach (Type t in types.OrderBy(t => t.FullName))
        {
            if (t.IsAbstract || !typeof(MenuMod).IsAssignableFrom(t)) continue;
            ModInfoAttribute     info = t.GetCustomAttribute<ModInfoAttribute>();
            ModCategoryAttribute cat  = t.GetCustomAttribute<ModCategoryAttribute>();
            if (info == null) continue;

            MenuMod mod;
            try { mod = (MenuMod)Activator.CreateInstance(t); }
            catch (Exception e) { Plugin.Log.LogError($"Could not create {t.Name}: {e}"); continue; }
            mod.Info     = info;
            mod.Category = cat?.Name ?? "Main";
            Insert(mod);
        }

        SortCategories();
        foreach (List<MenuMod> list in byCat.Values) SortList(list);
        foreach (MenuMod m in All.ToList()) Safe(m, m.Start, "Start");
        Plugin.Log.LogInfo($"Loaded {All.Count} mods in {Categories.Count} categories: " + string.Join(", ", Categories));
    }

    private void Insert(MenuMod mod)
    {
        All.Add(mod);
        if (!byCat.TryGetValue(mod.Category, out List<MenuMod> list))
        {
            byCat[mod.Category] = list = [];
            Categories.Add(mod.Category);
        }
        list.Add(mod);
    }

    private void SortCategories() =>
            Categories.Sort((a, b) => Rank(a).CompareTo(Rank(b)));

    private static int Rank(string c)
    {
        int i = Array.IndexOf(Order, c);
        return i < 0 ? 99 : i;
    }

    private static void SortList(List<MenuMod> list) =>
            list.Sort((a, b) =>
            {
                int p = a.Priority.CompareTo(b.Priority);
                return p != 0 ? p : string.Compare(a.Info.Name, b.Info.Name, StringComparison.OrdinalIgnoreCase);
            });

    public List<MenuMod> InCategory(string category) =>
            byCat.TryGetValue(category, out List<MenuMod> l) ? l : [];

    // ---- Dynamic mods (soundboard files etc.) -----------------------------------------------
    public void AddDynamic(MenuMod mod, string category)
    {
        mod.Category = category;
        Insert(mod);
        SortCategories();
        SortList(byCat[category]);
        Version++;
    }

    public void RemoveDynamic(Predicate<MenuMod> match)
    {
        foreach (MenuMod m in All.Where(x => match(x)).ToList())
        {
            if (m.Enabled) SetEnabled(m, false);
            All.Remove(m);
            if (byCat.TryGetValue(m.Category, out List<MenuMod> l)) l.Remove(m);
        }
        Version++;
    }

    // ---- Enabled mods ---------------------------------------------------------------------
    /// <summary>Every mod that is currently doing something (toggles that are on, "Name: value" mods not on their first value).</summary>
    public List<MenuMod> EnabledMods() =>
            All.Where(m => m.Active && m.ShowInEnabledList)
               .OrderBy(m => Rank(m.Category))
               .ThenBy(m => m.Info.Name, StringComparer.OrdinalIgnoreCase)
               .ToList();

    /// <summary>Turns one mod fully off: toggles are switched off, "Name: value" mods go back to their first value.</summary>
    public void Disable(MenuMod mod)
    {
        switch (mod.Info.Type)
        {
            case ButtonType.Togglable:
                SetEnabled(mod, false);
                break;
            case ButtonType.Incremental:
                if (mod.IncrementalValue != 0)
                {
                    mod.IncrementalValue = 0;
                    Safe(mod, mod.OnIncrementalStateLoaded, "Load");
                    Save(mod);
                }
                break;
        }
        StateVersion++;
    }

    /// <summary>Turns off every running mod in one tab.</summary>
    public void DisableCategory(string category)
    {
        foreach (MenuMod m in InCategory(category).ToList())
            if (m.Active) Disable(m);
    }

    // ---- Presets: remember which toggles are on, and bring them back later ----------------
    public void SavePreset(int slot)
    {
        string names = string.Join(",", All.Where(m => m.Info.Type == ButtonType.Togglable && m.Enabled)
                                           .Select(m => m.GetType().Name));
        PlayerPrefs.SetString("PVTX_Preset_" + slot, names);
        PlayerPrefs.Save();
    }

    public void LoadPreset(int slot)
    {
        string saved = PlayerPrefs.GetString("PVTX_Preset_" + slot, "");
        HashSet<string> want = new(saved.Split(new[] { ',', }, StringSplitOptions.RemoveEmptyEntries));
        foreach (MenuMod m in All.ToList())
            if (m.Info.Type == ButtonType.Togglable)
                SetEnabled(m, want.Contains(m.GetType().Name));
    }

    // ---- Search ---------------------------------------------------------------------------
    public List<MenuMod> Search(string query)
    {
        string[] words = (query ?? "").ToLowerInvariant().Split(new[] { ' ', }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return [];
        return All.Where(m =>
                  {
                      string n = m.Info.Name.ToLowerInvariant();
                      string c = m.Category.ToLowerInvariant();
                      return words.All(w => n.Contains(w) || c.Contains(w));
                  })
                  .OrderBy(m => m.Info.Name, StringComparer.OrdinalIgnoreCase)
                  .ToList();
    }

    // ---- Actions --------------------------------------------------------------------------
    public void Activate(MenuMod mod, bool decrement)
    {
        broken.Remove(mod);
        switch (mod.Info.Type)
        {
            case ButtonType.Fixed:       Safe(mod, mod.Pressed, "Pressed"); break;
            case ButtonType.Togglable:   SetEnabled(mod, !mod.Enabled);     break;
            case ButtonType.Incremental:
                Safe(mod, decrement ? mod.Decrement : mod.Increment, "Increment");
                Save(mod);
                break;
        }
        StateVersion++;
    }

    public void SetEnabled(MenuMod mod, bool on)
    {
        if (mod.Enabled == on) return;
        mod.Enabled = on;
        if (on) { broken.Remove(mod); Safe(mod, mod.OnEnable, "OnEnable"); }
        else Safe(mod, mod.OnDisable, "OnDisable");
        Save(mod);
        StateVersion++;
    }

    public void DisableAll()
    {
        foreach (MenuMod m in All.Where(m => m.Enabled).ToList())
        {
            m.Enabled = false;
            Safe(m, m.OnDisable, "OnDisable");
            Save(m);
        }
        StateVersion++;
    }

    /// <summary>Turns everything off, puts every "Name: value" mod back to its first value and forgets saved settings.</summary>
    public void ResetAll()
    {
        DisableAll();
        foreach (MenuMod m in All)
        {
            if (m.Info.Type == ButtonType.Incremental && m.IncrementalValue != 0)
            {
                m.IncrementalValue = 0;
                Safe(m, m.OnIncrementalStateLoaded, "Load");
            }
            PlayerPrefs.DeleteKey(Key(m, "on"));
            PlayerPrefs.DeleteKey(Key(m, "val"));
        }
        PlayerPrefs.Save();
        StateVersion++;
    }

    // ---- Per-frame (0 = Update, 1 = LateUpdate, 2 = FixedUpdate) --------------------------
    public void Tick(int phase)
    {
        if (!restored) Restore();
        for (int i = 0; i < All.Count; i++)
        {
            MenuMod m = All[i];
            if (!m.Active || broken.Contains(m)) continue;
            try
            {
                switch (phase)
                {
                    case 0:  m.Update();      break;
                    case 1:  m.LateUpdate();  break;
                    default: m.FixedUpdate(); break;
                }
            }
            catch (Exception e)
            {
                // One broken mod must never take the menu down.
                Plugin.Log.LogError($"[{m.Info.Name}] failed and was switched off: {e}");
                broken.Add(m);
                m.Enabled = false;
                if (m.Info.Type == ButtonType.Incremental) m.IncrementalValue = 0;
                Safe(m, m.OnDisable, "OnDisable");
                StateVersion++;
            }
        }
    }

    private void Safe(MenuMod mod, Action action, string where)
    {
        try { action(); }
        catch (Exception e) { Plugin.Log.LogError($"[{mod.Info.Name}] {where} failed: {e}"); }
    }

    // ---- Persistence ----------------------------------------------------------------------
    private static string Key(MenuMod m, string s) => $"MonkeMenu_{m.GetType().Name}_{s}";

    private void Save(MenuMod m)
    {
        if (m.Info.Type == ButtonType.Fixed) return;
        PlayerPrefs.SetInt(Key(m, "on"), m.Enabled ? 1 : 0);
        PlayerPrefs.SetInt(Key(m, "val"), m.IncrementalValue);
        PlayerPrefs.Save();
    }

    private void Restore()
    {
        restored = true;
        foreach (MenuMod m in All.ToList())
        {
            if (m.Info.Type == ButtonType.Incremental)
            {
                m.IncrementalValue = PlayerPrefs.GetInt(Key(m, "val"), 0);
                if (m.IncrementalValue != 0) Safe(m, m.OnIncrementalStateLoaded, "Load");
            }
            else if (m.Info.Type == ButtonType.Togglable)
            {
                bool on = PlayerPrefs.GetInt(Key(m, "on"), m.Info.StartState == EnabledType.Enabled ? 1 : 0) == 1;
                if (on) SetEnabled(m, true);
            }
        }
    }
}
