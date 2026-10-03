using System.IO;
using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== NETWORK ==============================

[ModCategory(Cat.Utility)]
[ModInfo("Disconnect", "Leave the room you're in (tap twice to confirm)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisconnectMod : ConfirmMod
{
    public override int Priority => -2;
    protected override string Idle => "Disconnect";
    protected override void Run() => Net.Disconnect();
}

// ============================== MENU SETTINGS ==============================

[ModCategory(Cat.Utility)]
[ModInfo("Menu Size: ", "How big the hand menu is", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MenuSize : IncrementalMod
{
    public override bool ShowInEnabledList => false;
    public override int Priority => -1;
    private static readonly float[] Sizes = [0.5f, 0.4f, 0.65f, 0.8f,];
    protected override string[] Labels => ["Medium", "Small", "Large", "Huge",];
    protected override void Changed() => MenuStyle.Scale = Sizes[IncrementalValue];
}

[ModCategory(Cat.Utility)]
[ModInfo("Menu Hand: ", "Which hand the menu floats over (you poke it with the other one)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MenuHand : IncrementalMod
{
    public override bool ShowInEnabledList => false;
    public override int Priority => -1;
    protected override string[] Labels => ["Left", "Right",];
    protected override void Changed() => MenuStyle.RightHand = IncrementalValue == 1;
}

[ModCategory(Cat.Utility)]
[ModInfo("Finger Ball: ", "How big the ball on your poking finger is (bigger = easier to press buttons)", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class FingerBall : IncrementalMod
{
    public override int Priority => -1;
    public override bool ShowInEnabledList => false;
    private static readonly float[] Radii = [0.012f, 0.007f, 0.02f, 0.03f,];
    protected override string[] Labels => ["Medium", "Small", "Large", "Huge",];
    protected override void Changed() => MenuStyle.BallRadius = Radii[IncrementalValue];
}

[ModCategory(Cat.Utility)]
[ModInfo("Menu Theme: ", "Colours of the menu", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class MenuTheme : IncrementalMod
{
    public override bool ShowInEnabledList => false;
    public override int Priority => -1;
    protected override string[] Labels => MenuStyle.ThemeNames;
    protected override void Changed() => MenuStyle.SetTheme(IncrementalValue);
}

// ============================== ROOM ==============================

[ModCategory(Cat.Utility)]
[ModInfo("Copy Room Name", "Copies the name of the room you're in to the clipboard", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CopyRoomName : MenuMod
{
    public override void Pressed()
    {
        string n = Net.RoomName();
        if (n.Length > 0) GUIUtility.systemCopyBuffer = n;
    }
}

// ============================== PRESETS ==============================

public static class PresetState { public static int Slot = 1; }

[ModCategory(Cat.Utility)]
[ModInfo("Preset: ", "Which of the 3 preset slots Save / Load use", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class PresetSlot : IncrementalMod
{
    public override int Priority => -3;
    public override bool ShowInEnabledList => false;
    protected override string[] Labels => ["1", "2", "3",];
    protected override void Changed() => PresetState.Slot = IncrementalValue + 1;
}

[ModCategory(Cat.Utility)]
[ModInfo("Save Preset", "Remembers which mods are on right now in the chosen slot", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SavePreset : MenuMod
{
    public override int Priority => -3;
    public override void Pressed() => SoundBoard.Registry.SavePreset(PresetState.Slot);
}

[ModCategory(Cat.Utility)]
[ModInfo("Load Preset", "Turns on exactly the mods saved in the chosen slot (everything else goes off)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class LoadPreset : MenuMod
{
    public override int Priority => -3;
    public override void Pressed() => SoundBoard.Registry.LoadPreset(PresetState.Slot);
}

// ============================== HOUSEKEEPING ==============================

public abstract class DisableCategoryMod : MenuMod
{
    protected abstract string Target { get; }
    public override void Pressed() => SoundBoard.Registry.DisableCategory(Target);
}

[ModCategory(Cat.Utility)]
[ModInfo("Disable Movement Mods", "Turns off every mod in the Movement tab", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableMovementMods : DisableCategoryMod { protected override string Target => Cat.Movement; }

[ModCategory(Cat.Utility)]
[ModInfo("Disable World Mods", "Turns off every mod in the World tab (collisions come back)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableWorldMods : DisableCategoryMod { protected override string Target => Cat.World; }

[ModCategory(Cat.Utility)]
[ModInfo("Disable Visual Mods", "Turns off every mod in the Visual tab", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableVisualMods : DisableCategoryMod { protected override string Target => Cat.Visual; }

[ModCategory(Cat.Utility)]
[ModInfo("Disable Fun Mods", "Turns off every mod in the Fun tab", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableFunMods : DisableCategoryMod { protected override string Target => Cat.Fun; }

[ModCategory(Cat.Utility)]
[ModInfo("Disable All Mods", "Turns every mod off", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class DisableAllMods : MenuMod
{
    public override void Pressed() => SoundBoard.Registry.DisableAll();
}

[ModCategory(Cat.Utility)]
[ModInfo("Reset Everything", "Turns everything off and forgets all saved settings", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ResetEverything : ConfirmMod
{
    protected override string Idle => "Reset Everything";

    protected override void Run()
    {
        SoundBoard.Registry.ResetAll();
        SoundBoard.SetVolume(1f);
        SoundBoard.SetLoop(false);
    }
}

[ModCategory(Cat.Utility)]
[ModInfo("Copy Position", "Copies your x, y, z to the clipboard", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CopyPosition : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 p = H.Head.position;
        GUIUtility.systemCopyBuffer = $"{p.x:F2}, {p.y:F2}, {p.z:F2}";
    }
}

[ModCategory(Cat.Utility)]
[ModInfo("Screenshot", "Saves a picture to BepInEx\\MonkeMenuShots", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class TakeScreenshot : MenuMod
{
    public override void Pressed()
    {
        string dir = Path.Combine(BepInEx.Paths.BepInExRootPath, "MonkeMenuShots");
        Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"shot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png"));
    }
}

[ModCategory(Cat.Utility)]
[ModInfo("Quit Game", "Closes Gorilla Tag (tap twice to confirm)", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class QuitGame : ConfirmMod
{
    protected override string Idle => "Quit Game";
    protected override void Run() => Application.Quit();
}

// ============================== SOUNDBOARD CONTROLS ==============================

[ModCategory(Cat.Sound)]
[ModInfo("Stop Sound", "Stops whatever is playing", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class StopSound : MenuMod
{
    public override int Priority => -10;
    public override void Pressed() => SoundBoard.Stop();
}

[ModCategory(Cat.Sound)]
[ModInfo("Sound Volume: ", "Soundboard volume", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SoundVolume : IncrementalMod
{
    public override bool ShowInEnabledList => false;
    public override int Priority => -9;
    private static readonly float[] Levels = [1f, 0.75f, 0.5f, 0.25f, 0.1f,];
    protected override string[] Labels => ["100%", "75%", "50%", "25%", "10%",];
    protected override void Changed() => SoundBoard.SetVolume(Levels[IncrementalValue]);
}

[ModCategory(Cat.Sound)]
[ModInfo("Sound Loop", "Repeat the sound until you stop it", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SoundLoop : MenuMod
{
    public override int Priority => -8;
    public override void OnEnable()  => SoundBoard.SetLoop(true);
    public override void OnDisable() => SoundBoard.SetLoop(false);
}

[ModCategory(Cat.Sound)]
[ModInfo("Reload Sounds", "Re-scan the MonkeMenuSounds folder for new files", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ReloadSounds : MenuMod
{
    public override int Priority => -7;
    public override void Pressed() => SoundBoard.Reload();
}

[ModCategory(Cat.Sound)]
[ModInfo("Open Sounds Folder", "Opens the folder where you drop your own .wav / .ogg / .mp3 files", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class OpenSoundsFolder : MenuMod
{
    public override int Priority => -6;

    public override void Pressed()
    {
        Directory.CreateDirectory(SoundBoard.Folder);
        Application.OpenURL(SoundBoard.Folder);
    }
}
