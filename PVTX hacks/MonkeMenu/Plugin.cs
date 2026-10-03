using BepInEx;
using BepInEx.Logging;
using MonkeMenu.Core;

namespace MonkeMenu;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid    = "org.monkemenu.standalone";
    public const string Name    = "PVTX hacks";
    public const string Version = "2.7.0";

    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("PVTX hacks loaded!");
        try { gameObject.AddComponent<MenuManager>(); }
        catch (System.Exception e) { Log.LogError("Failed to start menu: " + e); }
    }
}
