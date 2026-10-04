using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

[ModCategory(Cat.Room)]
[ModInfo("Disconnect", "Leave the room (tap twice to confirm)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RoomDisconnect : ConfirmMod
{
    protected override string Idle => "Disconnect";
    protected override void Run() => Net.Disconnect();
}

[ModCategory(Cat.Room)]
[ModInfo("Copy Room Name", "Copies the current room code to the clipboard", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RoomCopyName : MenuMod
{
    public override void Pressed()
    {
        string n = Net.RoomName();
        if (!string.IsNullOrEmpty(n))
            GUIUtility.systemCopyBuffer = n;
    }
}

[ModCategory(Cat.Room)]
[ModInfo("Room Info HUD", "Shows room name and player count near your view", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class RoomInfoHud : HudMod
{
    protected override Vector3 Offset => new(0f, -0.55f, 0.65f);
    protected override string Line() => Net.Room() + "  ping " + Net.Ping();
}

[ModCategory(Cat.Room)]
[ModInfo("Join Last Room", "Tries to rejoin using the last room name on the clipboard / cache", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class JoinLastRoom : MenuMod
{
    public static string LastCode;
    public override void Pressed()
    {
        string code = !string.IsNullOrEmpty(LastCode) ? LastCode : GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(code)) return;
        // best-effort: PhotonNetwork.JoinRoom via reflection
        try
        {
            var pn = Net.Find("Photon.Pun.PhotonNetwork");
            var m = pn?.GetMethod("JoinRoom", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            m?.Invoke(null, new object[] { code.Trim() });
        }
        catch { }
    }
}
