using System.Collections.Generic;
using UnityEngine;

namespace MonkeMenu.Mods;

using MonkeMenu.Core;

// All of these spawn LOCAL-only objects (only you see them) and clean themselves up.

[ModCategory(Cat.Fun)]
[ModInfo("Cube Gun", "Hold right trigger to fire cubes from your hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CubeGun : MenuMod
{
    private float next;

    public override string BindHint => "RT";
    public override void Update()
    {
        if (!H.Ready || !H.RTrig || Time.time < next) return;
        next = Time.time + 0.12f;

        GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 0.2f, H.Rainbow(1f));
        c.transform.position = H.RHand.position + H.Point(false) * 0.2f;
        c.AddComponent<Rigidbody>().velocity = H.Point(false) * 12f;
        H.Track(c, 8f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Ball Gun", "Hold left trigger to fire bouncy balls", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BallGun : MenuMod
{
    private float next;
    private PhysicsMaterial bouncy;

    public override string BindHint => "LT";
    public override void Update()
    {
        if (!H.Ready || !H.LTrig || Time.time < next) return;
        next = Time.time + 0.15f;

        bouncy ??= new PhysicsMaterial { bounciness = 0.9f, bounceCombine = PhysicsMaterialCombine.Maximum };

        GameObject b = H.Prim(PrimitiveType.Sphere, Vector3.one * Random.Range(0.15f, 0.4f), Random.ColorHSV(0f, 1f, 0.8f, 1f, 1f, 1f));
        b.GetComponent<Collider>().material = bouncy;
        b.transform.position = H.LHand.position + H.Point(true) * 0.2f;
        b.AddComponent<Rigidbody>().velocity = H.Point(true) * 10f;
        H.Track(b, 10f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Cube Rain", "Cubes rain down around you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CubeRain : MenuMod
{
    private float next;

    public override void Update()
    {
        if (!H.Ready || Time.time < next) return;
        next = Time.time + 0.08f;

        Vector2 r = Random.insideUnitCircle * 6f;
        GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * Random.Range(0.1f, 0.35f), Random.ColorHSV(0f, 1f, 0.7f, 1f, 1f, 1f));
        c.transform.position = H.Head.position + new Vector3(r.x, 8f, r.y);
        c.AddComponent<Rigidbody>();
        H.Track(c, 8f);
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Orbit Cubes", "Rainbow cubes orbit your head", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class OrbitCubes : MenuMod
{
    private const int Count = 8;
    private readonly List<GameObject> cubes = [];

    public override void OnEnable()
    {
        for (int i = 0; i < Count; i++)
            cubes.Add(H.Prim(PrimitiveType.Cube, Vector3.one * 0.12f, Color.white, false));
    }

    public override void OnDisable()
    {
        foreach (GameObject c in cubes) if (c != null) Object.Destroy(c);
        cubes.Clear();
    }

    public override void Update()
    {
        if (!H.Ready) return;
        Vector3 center = H.Head.position;
        for (int i = 0; i < cubes.Count; i++)
        {
            float a = Time.time * 2f + i * Mathf.PI * 2f / Count;
            cubes[i].transform.position = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 0.5f) * 0.3f, Mathf.Sin(a)) * 0.7f;
            cubes[i].transform.Rotate(60f * Time.deltaTime, 90f * Time.deltaTime, 0f);
            cubes[i].GetComponent<Renderer>().material.color = H.Rainbow(0.5f, i / (float)Count);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Slow Motion: ", "Slows down game time", ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlowMotion : IncrementalMod
{
    private static float baseFixed = -1f;
    private static readonly float[] Scales = [1f, 0.75f, 0.5f, 0.25f,];
    protected override string[] Labels => ["Off", "0.75x", "0.5x", "0.25x",];

    protected override void Changed()
    {
        if (baseFixed < 0f) baseFixed = Time.fixedDeltaTime;
        float s = Scales[IncrementalValue];
        Time.timeScale      = s;
        Time.fixedDeltaTime = baseFixed * s;
    }
}

// ============================== SPAWNERS (buttons) ==============================

[ModCategory(Cat.Fun)]
[ModInfo("Confetti", "Burst of colourful cubes from your right hand", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Confetti : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        for (int i = 0; i < 60; i++)
        {
            GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 0.07f, Random.ColorHSV(0f, 1f, 0.8f, 1f, 1f, 1f));
            c.transform.position = H.Head.position + H.Look.forward * 0.8f;
            c.AddComponent<Rigidbody>().velocity = Random.onUnitSphere * 4f + Vector3.up * 3f;
            H.Track(c, 8f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Ball Pit", "Drops 40 bouncy balls around you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class BallPit : MenuMod
{
    private PhysicsMaterial bouncy;

    public override void Pressed()
    {
        if (!H.Ready) return;
        bouncy ??= new PhysicsMaterial { bounciness = 0.85f, bounceCombine = PhysicsMaterialCombine.Maximum };
        for (int i = 0; i < 40; i++)
        {
            GameObject b = H.Prim(PrimitiveType.Sphere, Vector3.one * Random.Range(0.2f, 0.35f), Random.ColorHSV(0f, 1f, 0.8f, 1f, 1f, 1f));
            b.GetComponent<Collider>().material = bouncy;
            b.transform.position = H.Head.position + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1.5f, 4f), Random.Range(-1.5f, 1.5f));
            b.AddComponent<Rigidbody>();
            H.Track(b, 25f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Cube Tower", "Builds a tower of cubes in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CubeTower : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 flat = H.Look.forward; flat.y = 0f;
        Vector3 spot = H.Head.position + flat.normalized * 1.5f;
        float baseY = Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit hit, 10f) ? hit.point.y : spot.y - 1.2f;

        for (int i = 0; i < 15; i++)
        {
            GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 0.25f, H.Rainbow(1f, i / 15f));
            c.transform.position = new Vector3(spot.x, baseY + 0.14f + i * 0.26f, spot.z);
            c.AddComponent<Rigidbody>();
            H.Track(c, 120f);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Cube Wall", "Builds a wall of cubes in front of you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class CubeWall : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        Vector3 flat = H.Look.forward; flat.y = 0f; flat.Normalize();
        Vector3 spot = H.Head.position + flat * 2f;
        Vector3 side = Vector3.Cross(Vector3.up, flat);
        float baseY = Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit hit, 10f) ? hit.point.y : spot.y - 1.2f;

        for (int x = 0; x < 6; x++)
            for (int y = 0; y < 5; y++)
            {
                GameObject c = H.Prim(PrimitiveType.Cube, Vector3.one * 0.25f, H.Rainbow(1f, (x + y) / 11f));
                c.transform.position = spot + side * ((x - 2.5f) * 0.26f);
                c.transform.position = new Vector3(c.transform.position.x, baseY + 0.14f + y * 0.26f, c.transform.position.z);
                c.AddComponent<Rigidbody>();
                H.Track(c, 120f);
            }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Clear Spawned", "Deletes every cube/ball you've spawned", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ClearSpawned : MenuMod
{
    public override int Priority => -1;
    public override void Pressed() => H.ClearSpawned();
}

[ModCategory(Cat.Fun)]
[ModInfo("Explode", "Blasts every spawned object away from you", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Explode : MenuMod
{
    public override void Pressed()
    {
        if (!H.Ready) return;
        foreach (GameObject g in H.Spawned)
        {
            if (g == null) continue;
            Rigidbody rb = g.GetComponent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(14f, H.Head.position, 10f, 1f, ForceMode.VelocityChange);
        }
    }
}

[ModCategory(Cat.Fun)]
[ModInfo("Gravity Gun", "Hold right trigger to pull spawned objects to your hand", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class GravityGun : MenuMod
{
    public override string BindHint => "RT";
    public override void Update()
    {
        if (!H.Ready || !H.RTrig) return;
        Vector3 target = H.RHand.position + H.Point(false) * 0.6f;
        foreach (GameObject g in H.Spawned)
        {
            if (g == null) continue;
            Rigidbody rb = g.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = (target - g.transform.position) * 8f;
        }
    }
}

// ============================== WEATHER / WEARABLES ==============================

[ModCategory(Cat.Fun)]
[ModInfo("Snow", "Gentle snow falls around you (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Snow : FallingBase
{
    protected override Vector3 Fall  => new(0.3f, -0.9f, 0f);
    protected override Vector3 Scale => Vector3.one * 0.05f;
    protected override Color Tint(int i) => Color.white;
}

[ModCategory(Cat.Fun)]
[ModInfo("Rain", "Rain falls around you (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Rain : FallingBase
{
    protected override Vector3 Fall  => new(0f, -14f, 0f);
    protected override Vector3 Scale => new(0.01f, 0.35f, 0.01f);
    protected override PrimitiveType Shape => PrimitiveType.Cube;
    protected override Color Tint(int i) => new(0.6f, 0.75f, 1f);
}

[ModCategory(Cat.Fun)]
[ModInfo("Confetti Fall", "Colourful confetti drifts down around you", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ConfettiFall : FallingBase
{
    protected override Vector3 Fall  => new(0f, -1.3f, 0f);
    protected override Vector3 Scale => new(0.06f, 0.01f, 0.06f);
    protected override PrimitiveType Shape => PrimitiveType.Cube;
    protected override Color Tint(int i) => Color.HSVToRGB((i * 0.137f) % 1f, 0.9f, 1f);
}

[ModCategory(Cat.Fun)]
[ModInfo("Sword", "A glowing sword in your right hand (only you see it)", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class Sword : MenuMod
{
    private GameObject blade;

    public override void OnEnable()
    {
        if (!H.Ready) return;
        blade = H.Prim(PrimitiveType.Cube, new Vector3(0.04f, 0.04f, 1f), Color.cyan, false);
        blade.transform.SetParent(H.RHand, false);
        blade.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        blade.transform.localRotation = Quaternion.identity;
    }

    public override void OnDisable()
    {
        if (blade != null) Object.Destroy(blade);
        blade = null;
    }

    public override void Update()
    {
        if (blade != null) blade.GetComponent<Renderer>().material.color = H.Rainbow(0.3f);
    }
}
