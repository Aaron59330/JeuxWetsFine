using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    public enum PickupType { Ammo, Health, Shield, Weapon }

    /// <summary>Objet à ramasser (munitions, soin, bouclier, arme) avec faisceau lumineux.</summary>
    public class Pickup : MonoBehaviour
    {
        public static readonly List<Pickup> All = new List<Pickup>();
        public PickupType type; public string weaponId; public bool taken; public float cooldown;
        float t, baseY;
        static Material beamMat;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static Pickup Spawn(PickupType type, Vector3 pos, string weaponId = null)
        {
            var go = new GameObject("Pickup_" + type);
            go.transform.position = pos + Vector3.up * .6f;
            var p = go.AddComponent<Pickup>();
            p.type = type; p.weaponId = weaponId; p.baseY = go.transform.position.y; p.t = Random.value * 6f;

            Color col;
            var vis = new GameObject("Vis"); vis.transform.SetParent(go.transform, false);
            if (type == PickupType.Weapon)
            {
                var w = Weapons.Get(weaponId);
                var gun = WeaponSystem.BuildGunModel(w, false); gun.transform.SetParent(vis.transform, false); gun.transform.localScale = Vector3.one * 1.6f;
                switch (weaponId) { case "ar": col = new Color(.42f, .36f, 1f); break; case "smg": col = new Color(1f, .6f, .18f); break; case "shotgun": col = new Color(.9f, .23f, .23f); break; case "sniper": col = new Color(.2f, .77f, .42f); break; case "lmg": col = new Color(1f, .77f, 0f); break; default: col = new Color(.6f, .64f, .72f); break; }
            }
            else
            {
                switch (type) { case PickupType.Ammo: col = new Color(1f, .82f, .25f); break; case PickupType.Health: col = new Color(1f, .23f, .23f); break; default: col = new Color(.24f, .65f, 1f); break; }
                U.Prim(PrimitiveType.Cube, vis.transform, Vector3.zero, new Vector3(.45f, .35f, .45f), U.Lit(col, .4f), false, true);
                if (type == PickupType.Health)
                {
                    U.Prim(PrimitiveType.Cube, vis.transform, new Vector3(0, .19f, 0), new Vector3(.5f, .05f, .14f), U.Lit(Color.white), false, true);
                    U.Prim(PrimitiveType.Cube, vis.transform, new Vector3(0, .19f, 0), new Vector3(.14f, .05f, .5f), U.Lit(Color.white), false, true);
                }
                if (type == PickupType.Shield) U.Prim(PrimitiveType.Sphere, vis.transform, new Vector3(0, .3f, 0), Vector3.one * .4f, U.Lit(new Color(.6f, .88f, 1f), .8f), false, true);
            }
            if (beamMat == null) beamMat = U.Unlit(Color.white);
            var beam = U.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.4f, 0), new Vector3(.7f, 1.6f, .7f), null, false, true);
            var br = beam.GetComponent<Renderer>(); br.sharedMaterial = U.Unlit(new Color(col.r, col.g, col.b, .22f));
            return p;
        }

        void Update()
        {
            t += Time.deltaTime; cooldown -= Time.deltaTime;
            transform.GetChild(0).localRotation = Quaternion.Euler(0, t * 90f, 0);
            transform.GetChild(0).localPosition = new Vector3(0, Mathf.Sin(t * 3f) * .1f, 0);
        }

        public static void DestroyAll()
        {
            foreach (var p in All.ToArray()) if (p != null) Destroy(p.gameObject);
        }
    }
}
