using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    [System.Serializable]
    public class WeaponDef
    {
        public string id, name;
        public float dmg, rpm, reload, spread, ads, recoil, bloom, range, zoom, len;
        public int mag, reserve, pellets;
        public bool auto, scope;
        public Color color;
        public int[] st; // stats affichées (dégâts, cadence, portée)
    }

    public static class Weapons
    {
        public static readonly WeaponDef[] All =
        {
            new WeaponDef{ id="ar", name="Fusil d'assaut", dmg=26, rpm=600, mag=30, reserve=120, reload=2.1f, spread=.022f, ads=.002f, recoil=.010f, bloom=.004f, auto=true, pellets=1, range=170, zoom=1.35f, color=new Color(.23f,.25f,.29f), len=.62f, st=new[]{60,65,75} },
            new WeaponDef{ id="smg", name="Pistolet-mitrailleur", dmg=17, rpm=900, mag=32, reserve=128, reload=1.6f, spread=.034f, ads=.008f, recoil=.008f, bloom=.004f, auto=true, pellets=1, range=75, zoom=1.2f, color=new Color(.19f,.2f,.23f), len=.45f, st=new[]{40,95,40} },
            new WeaponDef{ id="shotgun", name="Fusil à pompe", dmg=12, rpm=75, mag=6, reserve=30, reload=2.8f, spread=.07f, ads=.045f, recoil=.05f, bloom=0, auto=false, pellets=10, range=32, zoom=1.15f, color=new Color(.35f,.27f,.2f), len=.6f, st=new[]{95,20,25} },
            new WeaponDef{ id="sniper", name="Sniper", dmg=105, rpm=48, mag=5, reserve=20, reload=3.2f, spread=.05f, ads=.0004f, recoil=.06f, bloom=0, auto=false, pellets=1, range=400, zoom=4f, color=new Color(.25f,.29f,.23f), len=.85f, scope=true, st=new[]{100,10,100} },
            new WeaponDef{ id="lmg", name="Mitrailleuse", dmg=22, rpm=560, mag=75, reserve=150, reload=4.6f, spread=.03f, ads=.006f, recoil=.008f, bloom=.003f, auto=true, pellets=1, range=140, zoom=1.25f, color=new Color(.27f,.29f,.31f), len=.72f, st=new[]{55,60,70} },
            new WeaponDef{ id="pistol", name="Pistolet", dmg=30, rpm=380, mag=12, reserve=60, reload=1.3f, spread=.016f, ads=.003f, recoil=.02f, bloom=.006f, auto=false, pellets=1, range=90, zoom=1.15f, color=new Color(.17f,.18f,.19f), len=.3f, st=new[]{40,45,45} },
        };

        public static WeaponDef Get(string id)
        {
            foreach (var w in All) if (w.id == id) return w;
            return All[0];
        }
    }

    /// <summary>Réglages sauvegardés (PlayerPrefs).</summary>
    public static class Cfg
    {
        public static float Sens { get { return PlayerPrefs.GetFloat("bz_sens", 5f); } set { PlayerPrefs.SetFloat("bz_sens", value); } }
        public static float Fov { get { return PlayerPrefs.GetFloat("bz_fov", 85f); } set { PlayerPrefs.SetFloat("bz_fov", value); } }
        public static float Volume { get { return PlayerPrefs.GetFloat("bz_vol", .5f); } set { PlayerPrefs.SetFloat("bz_vol", value); AudioListener.volume = value; } }
        public static int Bots { get { return PlayerPrefs.GetInt("bz_bots", 20); } set { PlayerPrefs.SetInt("bz_bots", value); } }
        public static int Diff { get { return PlayerPrefs.GetInt("bz_diff", 1); } set { PlayerPrefs.SetInt("bz_diff", value); } }
        public static bool InvertY { get { return PlayerPrefs.GetInt("bz_invy", 0) == 1; } set { PlayerPrefs.SetInt("bz_invy", value ? 1 : 0); } }
        public static string Primary { get { return PlayerPrefs.GetString("bz_primary", "ar"); } set { PlayerPrefs.SetString("bz_primary", value); } }
        public static string Secondary { get { return PlayerPrefs.GetString("bz_secondary", "pistol"); } set { PlayerPrefs.SetString("bz_secondary", value); } }
        public static int Games { get { return PlayerPrefs.GetInt("bz_games", 0); } set { PlayerPrefs.SetInt("bz_games", value); } }
        public static int Wins { get { return PlayerPrefs.GetInt("bz_wins", 0); } set { PlayerPrefs.SetInt("bz_wins", value); } }
        public static int TotalKills { get { return PlayerPrefs.GetInt("bz_kills", 0); } set { PlayerPrefs.SetInt("bz_kills", value); } }
        public static int Best { get { return PlayerPrefs.GetInt("bz_best", 0); } set { PlayerPrefs.SetInt("bz_best", value); } }
    }

    /// <summary>Points de vie + bouclier, partagé par le joueur et les bots.</summary>
    public class Health : MonoBehaviour
    {
        public static readonly List<Health> All = new List<Health>();
        public string displayName = "?";
        public float hp = 100f, shield = 0f, invuln = 0f;
        public bool dead, isPlayer;
        public int kills;
        public System.Action<Health, Health, bool> Died;   // victime, tueur, tête
        public System.Action<float, Health> Damaged;        // dégâts, source

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Update() { if (invuln > 0) invuln -= Time.deltaTime; }

        public void Damage(float amount, Health attacker, bool head = false, bool bypassShield = false)
        {
            if (dead || invuln > 0) return;
            if (!bypassShield && shield > 0) { float a = Mathf.Min(shield, amount); shield -= a; amount -= a; }
            hp -= amount;
            if (Damaged != null) Damaged(amount, attacker);
            if (hp <= 0) { hp = 0; dead = true; if (attacker != null) attacker.kills++; if (Died != null) Died(this, attacker, head); }
        }
    }

    /// <summary>Zone touchable (corps ou tête) rattachée à un Health.</summary>
    public class HitZone : MonoBehaviour
    {
        public Health owner;
        public bool head;
    }
}
