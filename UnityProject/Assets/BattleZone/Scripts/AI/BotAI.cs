using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    /// <summary>Soldat contrôlé par l'IA : patrouille, repère, se bat, évite le danger.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class BotAI : MonoBehaviour
    {
        public static readonly List<BotAI> All = new List<BotAI>();
        public Health health;
        public WeaponDef wd;
        public float lastShot = 99f;
        public bool HasTarget { get { return target != null; } }

        CharacterController cc; WorldBuilder world;
        Health target; Transform legL, legR;
        float thinkT, react, fireCd, pause = 1f, goalT, stuckT, walk, vy, yaw;
        int burst, strafe = 1; Vector3 goal, lastPos, vel; float idleDeadT;

        struct Diff { public float acc, dmg, react, spd; }
        static readonly Diff[] D = {
            new Diff{acc=.4f, dmg=.55f, react=1.1f, spd=3.8f}, new Diff{acc=.55f, dmg=.75f, react=.7f, spd=4.4f},
            new Diff{acc=.72f, dmg=.95f, react=.45f, spd=5f}, new Diff{acc=.88f, dmg=1.2f, react=.25f, spd=5.6f} };
        static readonly Dictionary<string, float> Ideal = new Dictionary<string, float> { { "shotgun", 10 }, { "smg", 18 }, { "ar", 28 }, { "lmg", 32 }, { "sniper", 70 }, { "pistol", 16 } };

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static BotAI Create(WorldBuilder w, string name, Color outfit, string weaponId, Vector3 pos, bool br, float invuln)
        {
            var go = new GameObject("Bot_" + name);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .3f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .5f; cc.slopeLimit = 50f;
            var h = go.AddComponent<Health>(); h.displayName = name; h.invuln = invuln; h.shield = br ? new[] { 0f, 0f, 25f, 50f }[Random.Range(0, 4)] : 0f;
            var hz = go.AddComponent<HitZone>(); hz.owner = h;
            var b = go.AddComponent<BotAI>();
            b.health = h; b.cc = cc; b.world = w; b.wd = Weapons.Get(weaponId); b.yaw = Random.Range(0, 360f);
            b.goal = pos; b.BuildVisual(outfit);
            return b;
        }

        void BuildVisual(Color col)
        {
            var body = U.Lit(col, .1f); var skin = U.Lit(new Color(.85f, .66f, .49f), .1f); var pants = U.Lit(new Color(.18f, .19f, .22f), .05f);
            var hat = U.Lit(new Color[] { new Color(.16f, .17f, .19f), new Color(.23f, .25f, .19f), new Color(.33f, .31f, .29f) }[Random.Range(0, 3)], .1f);
            var vis = new GameObject("Vis").transform; vis.SetParent(transform, false);
            var ll = U.Prim(PrimitiveType.Cube, vis, new Vector3(-.14f, .4f, 0), new Vector3(.24f, .8f, .26f), pants); legL = ll.transform;
            var lr = U.Prim(PrimitiveType.Cube, vis, new Vector3(.14f, .4f, 0), new Vector3(.24f, .8f, .26f), pants); legR = lr.transform;
            U.Prim(PrimitiveType.Cube, vis, new Vector3(0, 1.1f, 0), new Vector3(.6f, .62f, .34f), body);
            U.Prim(PrimitiveType.Sphere, vis, new Vector3(0, 1.58f, 0), Vector3.one * .5f, skin);
            U.Prim(PrimitiveType.Cube, vis, new Vector3(0, 1.8f, 0), new Vector3(.56f, .16f, .56f), hat);
            var a1 = U.Prim(PrimitiveType.Cube, vis, new Vector3(-.38f, 1.2f, .25f), new Vector3(.16f, .16f, .6f), body);
            var a2 = U.Prim(PrimitiveType.Cube, vis, new Vector3(.38f, 1.2f, .25f), new Vector3(.16f, .16f, .6f), body);
            U.Prim(PrimitiveType.Cube, vis, new Vector3(.15f, 1.12f, .42f), new Vector3(.1f, .12f, .62f), U.Lit(wd.color, .3f));
            var head = new GameObject("Head"); head.transform.SetParent(transform, false); head.transform.localPosition = new Vector3(0, 1.62f, 0);
            var sc = head.AddComponent<SphereCollider>(); sc.radius = .32f; sc.isTrigger = true;
            var hz = head.AddComponent<HitZone>(); hz.owner = health; hz.head = true;
        }

        public void Alert() { thinkT = Mathf.Min(thinkT, Random.value * .3f); }

        void Update()
        {
            var gb = GameBootstrap.I;
            if (gb == null || gb.State == GameState.Menu) return;
            if (health.dead)
            {
                idleDeadT += Time.deltaTime;
                var vis = transform.GetChild(0);
                vis.localRotation = Quaternion.Slerp(vis.localRotation, Quaternion.Euler(-85f, 0, 0), Time.deltaTime * 6f);
                if (cc.enabled) cc.enabled = false;
                return;
            }
            if (gb.State != GameState.Playing && gb.State != GameState.Ended) return;

            float dt = Time.deltaTime; var df = D[Mathf.Clamp(Cfg.Diff, 0, 3)];
            fireCd -= dt; thinkT -= dt; goalT -= dt; lastShot += dt;
            if (thinkT <= 0) { thinkT = Random.Range(.3f, .6f); Think(df); }

            Vector3 want = Vector3.zero; float spd = df.spd * (transform.position.y < -.2f ? .55f : 1f);
            if (target != null && !target.dead)
            {
                Vector3 to = target.transform.position - transform.position; to.y = 0; float dist = to.magnitude; Vector3 fwd = to / Mathf.Max(dist, .01f);
                yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-9f * dt));
                float ideal; if (!Ideal.TryGetValue(wd.id, out ideal)) ideal = 25f;
                if (dist > ideal + 8f) want += fwd; else if (dist < ideal - 8f) want -= fwd;
                strafe = Mathf.Sin(Time.time * .7f + GetInstanceID()) > 0 ? 1 : -1;
                want += new Vector3(fwd.z, 0, -fwd.x) * strafe * .8f; spd *= .85f;
                react -= dt;
                if (react <= 0 && fireCd <= 0)
                {
                    if (burst <= 0) { pause -= dt * 2f; if (pause <= 0) { burst = Random.Range(3, 8); pause = Random.Range(.3f, 1.1f); } }
                    if (burst > 0) { burst--; fireCd = 60f / wd.rpm * (wd.auto ? 1f : 1.2f); Shoot(target, dist, df); }
                }
            }
            else
            {
                if (goalT <= 0 || (goal - transform.position).sqrMagnitude < 9f) NewGoal();
                Vector3 to = goal - transform.position; to.y = 0; want = to.normalized;
                yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-6f * dt));
            }
            if (want.sqrMagnitude > 1) want.Normalize();
            vel = Vector3.Lerp(vel, want * spd, 1f - Mathf.Exp(-8f * dt));

            // ne pas marcher dans l'eau profonde
            Vector3 p = transform.position, next = p + vel * dt * 3f;
            bool deep = world.HeightAt(next.x, next.z) < -2.2f;
            if (deep) { vel = Vector3.zero; if (target == null) NewGoal(); }

            if (cc.isGrounded && vy < 0) vy = -2f;
            vy -= 24f * dt;
            var before = transform.position;
            cc.Move(new Vector3(vel.x, vy, vel.z) * dt);
            if (new Vector2(transform.position.x, transform.position.z).magnitude > WorldBuilder.MAP - 6f) { transform.position = before; }
            if (cc.isGrounded && vel.sqrMagnitude > .5f && (cc.collisionFlags & CollisionFlags.Sides) != 0 && Random.value < .08f) vy = 8f;

            stuckT += dt;
            if (stuckT > 1.5f) { if ((transform.position - lastPos).sqrMagnitude < .36f) { NewGoal(); if (cc.isGrounded) vy = 8f; } lastPos = transform.position; stuckT = 0; }

            transform.rotation = Quaternion.Euler(0, yaw, 0);
            walk += vel.magnitude * dt * 2.2f;
            float sw = Mathf.Sin(walk) * 40f * Mathf.Min(1f, vel.magnitude);
            legL.localRotation = Quaternion.Euler(sw, 0, 0); legR.localRotation = Quaternion.Euler(-sw, 0, 0);
        }

        void NewGoal()
        {
            goalT = Random.Range(6f, 14f);
            var z = GameBootstrap.I.Zone;
            Vector3 g = transform.position;
            for (int k = 0; k < 10; k++)
            {
                if (z != null && Vector2.Distance(new Vector2(transform.position.x, transform.position.z), z.Center) > z.Radius * .8f)
                    g = new Vector3(z.Center.x + Random.Range(-z.Radius * .4f, z.Radius * .4f), 0, z.Center.y + Random.Range(-z.Radius * .4f, z.Radius * .4f));
                else
                {
                    float a = Random.Range(0, 6.28f), d = Random.Range(15f, 60f);
                    g = transform.position + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                }
                g.x = Mathf.Clamp(g.x, -WorldBuilder.MAP + 5, WorldBuilder.MAP - 5); g.z = Mathf.Clamp(g.z, -WorldBuilder.MAP + 5, WorldBuilder.MAP - 5);
                if (world.H(g.x, g.z) > .8f && new Vector2(g.x, g.z).magnitude < 120f) break;
            }
            goal = g;
        }

        void Think(Diff df)
        {
            Health best = null; float range = Mathf.Min(wd.range * .7f, 90f), bd = 1e9f;
            var list = new List<Health>(Health.All);
            list.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude));
            int checkedN = 0;
            foreach (var e in list)
            {
                if (e == health || e.dead || e.invuln > 0) continue;
                if (e.isPlayer && GameBootstrap.I.Player != null && GameBootstrap.I.Player.dropping) continue;
                float d = Vector3.Distance(e.transform.position, transform.position); if (d > range) break;
                if (++checkedN > 3) break;
                Vector3 a = transform.position + Vector3.up * 1.5f, b = e.transform.position + Vector3.up * 1.1f;
                RaycastHit hit;
                bool blocked = Physics.Linecast(a, b, out hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (blocked && hit.collider.GetComponentInParent<Health>() != e) continue;
                best = e; bd = d; break;
            }
            if (best != null && best != target) react = Random.Range(.8f, 1.6f) * df.react;
            target = best;
        }

        void Shoot(Health t, float dist, Diff df)
        {
            float acc = Mathf.Clamp(df.acc - dist * (wd.pellets > 1 ? .02f : .0045f), .05f, .95f);
            if (wd.scope) acc = Mathf.Min(.95f, df.acc + .1f);
            Vector3 from = transform.position + Vector3.up * 1.25f + transform.forward * .6f;
            bool hit = Random.value < acc;
            Vector3 aim = t.transform.position + Vector3.up * 1.1f;
            if (!hit) aim += new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-.8f, .8f), Random.Range(-1.5f, 1.5f));
            U.Tracer(from, aim, new Color(1f, .6f, .6f, .9f));
            var pl = GameBootstrap.I.Player;
            if (pl != null) { float dp = Vector3.Distance(pl.transform.position, transform.position); if (dp < 90f) Sfx.At(Sfx.Shot(wd), transform.position, Mathf.Clamp(1f - dp / 90f, .05f, .6f)); }
            lastShot = 0;
            if (hit)
            {
                float dmg = wd.dmg * df.dmg * (wd.pellets > 1 ? 4f : 1f) * Mathf.Clamp(1f - (dist - wd.range * .5f) / wd.range, .25f, 1f);
                if (Random.value < .12f) dmg *= 1.8f;
                t.Damage(dmg, health);
            }
        }
    }
}
