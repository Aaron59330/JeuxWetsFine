using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    public class WeaponSlot { public WeaponDef def; public int ammo, reserve; }

    /// <summary>Tir hitscan, recul, visée (ADS), rechargement, changement d'arme, ramassage.</summary>
    public class WeaponSystem : MonoBehaviour
    {
        public WeaponSlot[] slots = new WeaponSlot[2];
        public int cur;
        public float ads, reloadT;
        public bool FireHeldWithAmmo { get { return Input.GetMouseButton(0) && slots[cur].ammo > 0; } }
        public WeaponDef Current { get { return slots[cur].def; } }

        PlayerController pc; Camera cam; Transform vm; GameObject gun; Light flash;
        float fireCd, switchT, bloom, kick, flashT;
        int mask = ~(1 << 2);   // tout sauf "Ignore Raycast" (le joueur)

        public void Init(PlayerController p, Camera c, string primary, string secondary)
        {
            pc = p; cam = c;
            slots[0] = NewSlot(Weapons.Get(primary)); slots[1] = NewSlot(Weapons.Get(secondary));
            cur = 0;
            vm = new GameObject("ViewModel").transform;
            vm.SetParent(cam.transform, false);
            var fl = new GameObject("Flash"); fl.transform.SetParent(vm, false);
            flash = fl.AddComponent<Light>(); flash.type = LightType.Point; flash.range = 6f; flash.intensity = 2.5f; flash.color = new Color(1f, .8f, .4f); flash.enabled = false;
            BuildGun();
        }

        static WeaponSlot NewSlot(WeaponDef d) { return new WeaponSlot { def = d, ammo = d.mag, reserve = d.reserve }; }

        void BuildGun()
        {
            if (gun != null) Destroy(gun);
            gun = BuildGunModel(Current, true);
            gun.transform.SetParent(vm, false);
            gun.transform.localRotation = Quaternion.Euler(0, 180, 0);   // le modèle pointe vers -Z, la caméra regarde vers +Z
            fireCdReset();
        }
        void fireCdReset() { fireCd = 0; }

        /// <summary>Modèle d'arme en primitives (vue FPS ou objet au sol).</summary>
        public static GameObject BuildGunModel(WeaponDef w, bool hands)
        {
            var g = new GameObject("Gun_" + w.id);
            float L = w.len;
            var body = U.Lit(w.color, .3f); var dark = U.Lit(new Color(.1f, .1f, .12f), .2f); var metal = U.Lit(new Color(.42f, .44f, .48f), .5f);
            U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, 0, -L * .2f), new Vector3(.07f, .1f, L * .6f), body, false, true);
            U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, .02f, -L * .7f), new Vector3(.035f, .035f, L * .5f), dark, false, true);
            U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, .058f, -L * .2f), new Vector3(.085f, .015f, L * .6f), metal, false, true);
            if (L > .35f) U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.01f, L * .22f), new Vector3(.06f, .09f, L * .3f), dark, false, true);
            var grip = U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.09f, L * .04f), new Vector3(.05f, .1f, .05f), dark, false, true); grip.transform.localRotation = Quaternion.Euler(17, 0, 0);
            if (w.id != "pistol") U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.12f, -L * .15f), new Vector3(.045f, .17f, .06f), metal, false, true);
            if (w.scope) { var s = U.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, .1f, -L * .2f), new Vector3(.06f, .14f, .06f), dark, false, true); s.transform.localRotation = Quaternion.Euler(90, 0, 0); }
            else U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, .08f, -L * .55f), new Vector3(.03f, .03f, .05f), metal, false, true);
            if (w.id == "shotgun") U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.05f, -L * .55f), new Vector3(.06f, .06f, .2f), metal, false, true);
            if (w.id == "lmg") U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.12f, -L * .1f), new Vector3(.14f, .12f, .1f), metal, false, true);
            if (hands)
            {
                var skin = U.Lit(new Color(.85f, .66f, .49f), .1f); var sleeve = U.Lit(new Color(.24f, .29f, .21f), .05f);
                U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.07f, L * .04f), new Vector3(.075f, .075f, .085f), skin, false, true);
                U.Prim(PrimitiveType.Cube, g.transform, new Vector3(.02f, -.1f, L * .04f + .2f), new Vector3(.09f, .09f, .3f), sleeve, false, true);
                U.Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.06f, -L * .4f), new Vector3(.075f, .075f, .085f), skin, false, true);
                var arm = U.Prim(PrimitiveType.Cube, g.transform, new Vector3(-.12f, -.12f, -L * .4f + .22f), new Vector3(.09f, .09f, .3f), sleeve, false, true); arm.transform.localRotation = Quaternion.Euler(0, 23, 0);
                U.SetLayerRecursive(g, 2);
            }
            var mz = new GameObject("Muzzle"); mz.transform.SetParent(g.transform, false); mz.transform.localPosition = new Vector3(0, .02f, -L * .95f);
            return g;
        }

        void Update()
        {
            var gb = GameBootstrap.I;
            if (gb.State != GameState.Playing || pc.health.dead) { vm.gameObject.SetActive(false); return; }
            float dt = Time.deltaTime;
            var slot = slots[cur]; var w = slot.def;

            fireCd -= dt; switchT -= dt; bloom = Mathf.Max(0, bloom - dt * .05f);
            bool wantAds = Input.GetMouseButton(1) && reloadT <= 0 && !pc.dropping && switchT <= 0;
            ads = Mathf.Clamp01(ads + (wantAds ? 1 : -1) * dt * (w.scope ? 5f : 8f));

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Ampersand)) Switch(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Switch(1);
            if (Mathf.Abs(Input.mouseScrollDelta.y) > .1f) Switch(cur == 0 ? 1 : 0);
            if (Input.GetKeyDown(KeyCode.R)) Reload();

            if (reloadT > 0)
            {
                reloadT -= dt;
                if (reloadT <= 0) { int take = Mathf.Min(w.mag - slot.ammo, slot.reserve); slot.ammo += take; slot.reserve -= take; }
            }

            bool trigger = w.auto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (trigger && fireCd <= 0 && reloadT <= 0 && switchT <= 0 && !pc.dropping)
            {
                if (slot.ammo > 0) Fire(); else { Sfx.Play(Sfx.Click(), .6f); Reload(); }
            }

            HandlePickups();

            // caméra : FOV de visée
            float tgtFov = Cfg.Fov / Mathf.Lerp(1f, w.zoom, ads) + (pc.sprint ? 6f : 0f) + (pc.dropping && !pc.chute ? 10f : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, tgtFov, 1f - Mathf.Exp(-12f * dt));

            // position de l'arme dans la vue
            bool scoped = w.scope && ads > .92f;
            vm.gameObject.SetActive(!scoped && !pc.dropping);
            gb.UI.SetScope(scoped);
            Vector3 hip = new Vector3(.2f, -.19f, .42f), adsP = new Vector3(0, -.105f, .3f);
            Vector3 pos = Vector3.Lerp(hip, adsP, ads);
            float bobAmt = pc.MoveSpeed01 * (1f - ads);
            pos.x += Mathf.Cos(Time.time * 8f) * .012f * bobAmt; pos.y += -Mathf.Abs(Mathf.Sin(Time.time * 8f)) * .014f * bobAmt;
            kick = Mathf.Max(0, kick - dt * 9f);
            pos.z -= kick * .04f; pos.y += kick * .012f;
            Vector3 rot = new Vector3(-kick * 2.8f, 0, 0);
            if (pc.sprint) { rot = new Vector3(8f, -28f, 0); pos.y -= .03f; pos.x += .05f; }
            if (reloadT > 0) { float k = Mathf.Sin(Mathf.Clamp01(1f - reloadT / w.reload) * Mathf.PI); pos.y -= .13f * k; rot.x += 28f * k; rot.z = -23f * k; }
            if (switchT > 0) pos.y -= Mathf.Clamp01(switchT / .35f) * .3f;
            vm.localPosition = pos; vm.localRotation = Quaternion.Euler(rot);
            if (flashT > 0) { flashT -= dt; if (flashT <= 0) flash.enabled = false; }
        }

        void Switch(int i)
        {
            if (i == cur || slots[i] == null || pc.dropping) return;
            cur = i; reloadT = 0; switchT = .35f; BuildGun();
        }

        void Reload()
        {
            var s = slots[cur];
            if (reloadT > 0 || s.ammo >= s.def.mag || s.reserve <= 0 || pc.dropping) return;
            reloadT = s.def.reload; Sfx.Play(Sfx.Reload(), .7f);
        }

        void Fire()
        {
            var s = slots[cur]; var w = s.def;
            s.ammo--; fireCd = 60f / w.rpm;
            bool moving = pc.MoveSpeed01 > .2f;
            float sp = Mathf.Lerp(w.spread, w.ads, ads) * (moving ? 1.5f : 1f) * (pc.cc.isGrounded ? 1f : 2.2f) * (pc.crouch ? .7f : 1f) + bloom;
            bloom = Mathf.Min(.05f, bloom + w.bloom);
            Transform ct = cam.transform;
            Vector3 from = ct.position + ct.forward * .9f + ct.right * .2f * (1f - ads) - ct.up * .14f * (1f - ads);

            for (int i = 0; i < w.pellets; i++)
            {
                Vector3 d = (ct.forward + ct.right * Random.Range(-sp, sp) + ct.up * Random.Range(-sp, sp)).normalized;
                Vector3 end = ct.position + d * w.range;
                RaycastHit hit;
                if (Physics.Raycast(ct.position, d, out hit, w.range, mask, QueryTriggerInteraction.Collide))
                {
                    end = hit.point;
                    var hz = hit.collider.GetComponent<HitZone>();
                    if (hz != null && hz.owner != null && !hz.owner.dead)
                    {
                        float dmg = w.dmg * (hz.head ? (w.scope ? 2.5f : 1.8f) : 1f) * Mathf.Clamp((1f - (hit.distance - w.range * .5f) / w.range), .35f, 1f);
                        hz.owner.Damage(dmg, pc.health, hz.head);
                        GameBootstrap.I.UI.Hitmarker(hz.owner.dead);
                        Sfx.Play(Sfx.Hit(), .6f);
                        U.Spark(hit.point, new Color(1f, .3f, .3f));
                    }
                    else U.Spark(hit.point, new Color(.9f, .88f, .78f));
                }
                if (i < 4 || w.pellets == 1) U.Tracer(from, end, new Color(1f, .9f, .5f, .9f));
            }

            Sfx.Play(Sfx.Shot(w), .8f);
            pc.AddRecoil(w.recoil * (ads > .5f ? .65f : 1f) * Random.Range(.7f, 1.2f), Random.Range(-.5f, .5f) * w.recoil * .8f);
            kick = Mathf.Min(1f, kick + 1f);
            flash.enabled = true; flashT = .04f; flash.transform.position = from;
            foreach (var b in BotAI.All) if (b != null && !b.health.dead && !b.HasTarget && Vector3.Distance(b.transform.position, transform.position) < 60f) b.Alert();
            if (s.ammo <= 0) Reload();
        }

        // ---------- ramassage ----------
        void HandlePickups()
        {
            Pickup near = null; float bd = 9f;
            foreach (var p in Pickup.All.ToArray())
            {
                if (p == null || p.taken) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (p.type != PickupType.Weapon && d < 2.4f && Mathf.Abs(p.transform.position.y - transform.position.y) < 2.5f) { Take(p); continue; }
                if (p.type == PickupType.Weapon && d < bd && p.cooldown <= 0) { bd = d; near = p; }
            }
            var ui = GameBootstrap.I.UI;
            if (pc.dropping) ui.Prompt(pc.chute ? "Parachute ouvert — Espace pour couper" : "Espace : ouvrir le parachute");
            else if (near != null) { ui.Prompt("[E] Ramasser " + Weapons.Get(near.weaponId).name); if (Input.GetKeyDown(KeyCode.E)) Take(near); }
            else ui.Prompt(null);
        }

        void Take(Pickup p)
        {
            var h = pc.health;
            switch (p.type)
            {
                case PickupType.Ammo:
                    foreach (var s in slots) s.reserve = Mathf.Min(s.def.reserve * 3, s.reserve + s.def.mag);
                    GameBootstrap.I.UI.Toast("+ Munitions"); break;
                case PickupType.Health:
                    if (h.hp >= 100) return; h.hp = Mathf.Min(100, h.hp + 50); GameBootstrap.I.UI.Toast("+ Soin"); break;
                case PickupType.Shield:
                    if (h.shield >= 100) return; h.shield = Mathf.Min(100, h.shield + 50); GameBootstrap.I.UI.Toast("+ Bouclier"); break;
                case PickupType.Weapon:
                    var have = slots[0].def.id == p.weaponId ? slots[0] : slots[1].def.id == p.weaponId ? slots[1] : null;
                    if (have != null) { have.reserve = Mathf.Min(have.def.reserve * 3, have.reserve + have.def.mag * 2); }
                    else
                    {
                        var old = slots[cur].def.id;
                        Pickup.Spawn(PickupType.Weapon, transform.position + transform.right * 1.2f, old).cooldown = 2f;
                        slots[cur] = NewSlot(Weapons.Get(p.weaponId)); reloadT = 0; BuildGun();
                        GameBootstrap.I.UI.Toast("Arme : " + slots[cur].def.name);
                    }
                    break;
            }
            Sfx.Play(Sfx.Pickup(), .7f);
            p.taken = true; Destroy(p.gameObject);
        }
    }
}
