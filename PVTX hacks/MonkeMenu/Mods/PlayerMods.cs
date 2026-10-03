using System.Collections.Generic;
using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// ============================== JUMPING ==============================

[ModCategory(Cat.Player)]
[ModInfo("Air Jumps: ", "Press A in the air to jump again", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AirJumps : IncrementalMod
{
    private static readonly int[] Counts = [0, 1, 2, 3, 99,];
    protected override string[] Labels => ["Off", "1", "2", "3", "Infinite",];
    public override string BindHint => "A";

    private int  used;
    private bool was;

    public override void Update()
    {
        if (!H.Ready) return;
        bool now = H.A;
        if (Ground.Near(1.8f)) used = 0;
        else if (now && !was && used < Counts[IncrementalValue])
        {
            used++;
            Vector3 v = H.Rb.velocity;
            v.y = 6.5f;
            H.Rb.velocity = v;
        }
        was = now;
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Super Jump", "Press A while standing on something to launch yourself upward", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SuperJump : MenuMod
{
    private bool was;
    public override string BindHint => "A";

    public override void Update()
    {
        if (!H.Ready) return;
        bool now = H.A;
        if (now && !was && Ground.Near(1.8f))
        {
            Vector3 v = H.Rb.velocity;
            v.y = 10f;
            H.Rb.velocity = v;
        }
        was = now;
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Bouncy Monke", "You bounce back up when you land hard", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BouncyMonke : MenuMod
{
    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 v = H.Rb.velocity;
        if (v.y < -3f && Ground.Near(1.5f))
        {
            v.y = -v.y * 0.8f;
            H.Rb.velocity = v;
        }
    }
}

// ============================== SAFETY NETS ==============================

[ModCategory(Cat.Player)]
[ModInfo("Anti Void", "If you fall far below where you started, you're sent back to spawn", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AntiVoid : MenuMod
{
    public override void Update()
    {
        if (H.Ready && H.Head.position.y < H.SpawnPos.y - 80f) H.Teleport(H.SpawnPos);
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Snap To Ground", "Teleports you down onto whatever is under you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SnapToGround : MenuMod
{
    public override void Pressed()
    {
        if (H.Ready && Ground.Find(300f, out Vector3 p)) H.Teleport(p + Vector3.up * 1.3f);
    }
}

[ModCategory(Cat.Player)]
[ModInfo("Rewind", "Hold both triggers to run your movement backwards", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Rewind : MenuMod
{
    private readonly List<Vector3> history = [];
    private float next;

    public override string BindHint => "LT+RT";
    public override void OnDisable() => history.Clear();

    public override void Update()
    {
        if (!H.Ready) return;

        if (H.LTrig && H.RTrig)
        {
            if (history.Count > 0)
            {
                H.Teleport(history[history.Count - 1]);
                history.RemoveAt(history.Count - 1);
            }
            return;
        }

        if (Time.time < next) return;
        next = Time.time + 0.05f;
        history.Add(H.Head.position);
        if (history.Count > 400) history.RemoveAt(0); // about 20 seconds
    }
}

// ============================== FLOORS ==============================

[ModCategory(Cat.Player)]
[ModInfo("Air Walk", "An invisible floor follows you at a fixed height. Press B to move the floor to your feet", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class AirWalk : MenuMod
{
    private GameObject floor;
    private float      y;
    private bool       was;

    public override string BindHint => "B";
    public override void OnDisable()
    {
        if (floor != null) Object.Destroy(floor);
        floor = null;
    }

    public override void Update()
    {
        if (!H.Ready) return;

        if (floor == null)
        {
            floor = H.Prim(PrimitiveType.Cube, new Vector3(2f, 0.05f, 2f), new Color(0.4f, 0.8f, 1f));
            floor.AddComponent<KeepSolid>();
            floor.GetComponent<Renderer>().enabled = false;
            y = H.Head.position.y - 1.3f;
        }

        bool now = H.B;
        if (now && !was) y = H.Head.position.y - 1.3f;
        was = now;

        Vector3 head = H.Head.position;
        floor.transform.position = new Vector3(head.x, y, head.z);
    }
}
