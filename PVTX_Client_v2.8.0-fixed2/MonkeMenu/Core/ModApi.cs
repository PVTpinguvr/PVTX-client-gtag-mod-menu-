using System;

namespace MonkeMenu.Core;

public enum ButtonType { Fixed, Togglable, Incremental, }
public enum AccessSetting { Public, }
public enum EnabledType { Disabled, Enabled, }

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModCategoryAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModInfoAttribute(string name, string description, ButtonType type, AccessSetting access,
                                     EnabledType startState, int reserved) : Attribute
{
    public string      Name        { get; } = name;
    public string      Description { get; } = description;
    public ButtonType  Type        { get; } = type;
    public EnabledType StartState  { get; } = startState;
}

/// <summary>Base class for every mod. Override only what you need.</summary>
public abstract class MenuMod
{
    internal ModInfoAttribute Info;
    internal string           Category;

    public ModInfoAttribute AssociatedAttribute => Info;
    public bool             Enabled             { get; internal set; }
    public int              IncrementalValue    { get; set; }
    public virtual string   ModName             => Info.Name;

    /// <summary>
    /// Tag shown next to the name on every mod row, e.g. "[A]", "[RT]", "[TOGGLE]", "[TAP]", "[+/-]".
    /// Override for controller binds; default is based on button type so every mod has a tag.
    /// </summary>
    public virtual string BindHint => Info?.Type switch
    {
        ButtonType.Togglable   => "TOGGLE",
        ButtonType.Incremental => "+/-",
        ButtonType.Fixed       => "TAP",
        _                      => "MOD",
    };

    /// <summary>Label used on the VR/desktop menu (name + tag).</summary>
    public string MenuLabel
    {
        get
        {
            string hint = BindHint;
            if (string.IsNullOrEmpty(hint)) return ModName;
            return ModName + "  <color=#B9B0D0>[" + hint + "]</color>";
        }
    }

    /// <summary>Lower numbers are listed first inside a category.</summary>
    public virtual int Priority => 0;

    /// <summary>False for menu settings (size, theme, ...) so they don't clutter the "Enabled" tab.</summary>
    public virtual bool ShowInEnabledList => true;

    /// <summary>
    /// True while the mod should receive Update/LateUpdate/FixedUpdate.
    /// Toggles: when switched on. Incremental mods: whenever they are not on their first ("Off") value.
    /// </summary>
    public bool Active => Info.Type switch
    {
        ButtonType.Togglable   => Enabled,
        ButtonType.Incremental => IncrementalValue != 0,
        _                      => false,
    };

    public virtual void Start()                      { }
    public virtual void OnEnable()                   { }
    public virtual void OnDisable()                  { }
    public virtual void Update()                     { }
    public virtual void LateUpdate()                 { }
    public virtual void FixedUpdate()                { }
    public virtual void Pressed()                    { }
    public virtual void Increment()                  { }
    public virtual void Decrement()                  { }
    public virtual void OnIncrementalStateLoaded()   { }
}
