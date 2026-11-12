using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    public enum GameState { Menu, Playing, Paused, Ended }
    public enum GameMode { BattleRoyale, FreeForAll }

    /// <summary>
    /// Point d'entrée : construit le monde et l'interface, gère les modes de jeu.
    /// La scène ne contient que cet objet (créé par BattleZone ▸ Créer la scène).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap I;
        public GameState State = GameState.Menu;
        public GameMode Mode;
        public WorldBuilder World;
        public UIManager UI;
        public Camera Cam;
        public PlayerController Player;
        public ZoneController Zone;
        public List<BotAI> Bots = new List<BotAI>();
        public float MatchTime;

        static readonly string[] Names = { "Yoshi", "Zorg", "Nova", "Bobby", "Kiki", "Max", "Luna", "Tigre", "Pixel", "Rex", "Mochi", "Blaze", "Coco", "Ninja42", "Zed", "Pepito", "Ombre", "Gizmo", "Fifi", "Turbo", "Chaton", "Boum", "Vortex", "Pickle", "Nugget", "Dragon", "Fuzzy", "Sushi", "Titan", "Mango", "Raptor", "Biscuit", "Cactus", "Flash", "Lynx", "Panda", "Zigzag", "Waffle", "Comet", "Bruno" };
        static readonly Color[] Outfits = { new Color(.24f, .29f, .21f), new Color(.29f, .3f, .33f), new Color(.35f, .29f, .23f), new Color(.18f, .23f, .29f), new Color(.42f, .42f, .35f), new Color(.23f, .23f, .25f), new Color(.34f, .38f, .25f), new Color(.29f, .23f, .23f) };
        static readonly string[] BotWeapons = { "ar", "ar", "smg", "smg", "shotgun", "lmg", "sniper", "pistol" };
        int place; Health killer; bool ending;

        void Awake()
        {
            I = this;
            Application.runInBackground = true;
            Time.timeScale = 1f;

            var camGo = new GameObject("MainCamera"); camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            Cam.farClipPlane = 1500f; Cam.nearClipPlane = .03f; Cam.fieldOfView = Cfg.Fov; Cam.allowHDR = true;

            World = new GameObject("World").AddComponent<WorldBuilder>();
            World.Build();
            Sfx.Init(gameObject);
            UI = new GameObject("UI").AddComponent<UIManager>();
            UI.Build(this);
            UI.Show("main");
            SetCursor(false);
        }

        void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            if (State == GameState.Menu) OrbitCamera();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
            if (State == GameState.Playing)
            {
                MatchTime += Time.deltaTime;
                if (Mode == GameMode.FreeForAll) FfaRules();
                else if (!ending && Player != null && !Player.health.dead && AliveCount() == 1) { place = 1; ending = true; UI.Banner("DERNIER SURVIVANT !", 3f); Invoke("WinNow", 1.2f); }
            }
        }

        void LateUpdate() { if (State == GameState.Playing && Player != null) UI.UpdateHud(this); }

        void OrbitCamera()
        {
            float t = Time.time / 6f;
            Cam.transform.position = new Vector3(Mathf.Cos(t) * 105f, 70f, Mathf.Sin(t) * 105f);
            Cam.transform.LookAt(new Vector3(0, 12f, 0));
            Cam.fieldOfView = 65f;
        }

        // ---------- cycle de partie ----------
        public void StartGame(GameMode mode)
        {
            CleanupMatch();
            Mode = mode; MatchTime = 0; ending = false; place = 0; killer = null;
            SpawnPlayer();
            int n = mode == GameMode.BattleRoyale ? Cfg.Bots : Mathf.Min(Cfg.Bots, 15);
            for (int i = 0; i < n; i++) Bots.Add(SpawnBot(i, false));
            foreach (var spot in World.loot)
            {
                float r = Random.value; bool weapon = r < .12f;
                if (weapon) Pickup.Spawn(PickupType.Weapon, spot, Weapons.All[Random.Range(0, Weapons.All.Length)].id);
                else Pickup.Spawn(r < .5f ? PickupType.Ammo : r < .78f ? PickupType.Health : PickupType.Shield, spot);
            }
            if (mode == GameMode.BattleRoyale) { Zone = new GameObject("Zone").AddComponent<ZoneController>(); Zone.Init(); }
            State = GameState.Playing;
            UI.ShowHud(mode);
            SetCursor(true);
            UI.Banner(mode == GameMode.BattleRoyale ? "Largage ! Espace = parachute" : "Mêlée générale — 20 éliminations !", 3.5f);
        }

        void CleanupMatch()
        {
            Time.timeScale = 1f;
            if (Player != null) { Cam.transform.SetParent(null); Destroy(Player.gameObject); Player = null; }
            foreach (var b in Bots) if (b != null) Destroy(b.gameObject);
            Bots.Clear();
            Pickup.DestroyAll();
            if (Zone != null) { Destroy(Zone.gameObject); Zone = null; }
        }

        void SpawnPlayer()
        {
            var go = new GameObject("Player"); go.layer = 2;   // "Ignore Raycast" : ses propres tirs ne le touchent pas
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .35f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .5f; cc.slopeLimit = 50f;
            var h = go.AddComponent<Health>(); h.isPlayer = true; h.displayName = "Toi";
            var pc = go.AddComponent<PlayerController>(); pc.cc = cc; pc.health = h; pc.Init(World);
            var head = new GameObject("Head").transform; head.SetParent(go.transform, false); head.localPosition = new Vector3(0, 1.65f, 0); pc.head = head;
            var ws = go.AddComponent<WeaponSystem>(); pc.weapons = ws;
            Cam.transform.SetParent(head, false); Cam.transform.localPosition = Vector3.zero; Cam.transform.localRotation = Quaternion.identity;
            ws.Init(pc, Cam, Cfg.Primary, Cfg.Secondary);
            Player = pc;
            h.Died += OnDied; h.Damaged += (amt, src) => UI.PlayerHurt(amt, src);
            PlacePlayer();
        }

        void PlacePlayer()
        {
            var cc = Player.cc; cc.enabled = false;
            if (Mode == GameMode.BattleRoyale)
            {
                Player.transform.position = new Vector3(Random.Range(-70, 70), 150f, Random.Range(-70, 70));
                Player.dropping = true; Player.pitch = 20f; Player.yaw = Random.Range(0, 360f);
            }
            else
            {
                Vector3 p; World.RandomSpawn(25f, Vector3.zero, out p);
                Player.transform.position = p; Player.dropping = false; Player.health.invuln = 2.5f; Player.pitch = 0;
            }
            cc.enabled = true;
        }

        BotAI SpawnBot(int i, bool respawn, int keepKills = 0)
        {
            Vector3 p; World.RandomSpawn(respawn ? 30f : 45f, Player != null ? Player.transform.position : Vector3.zero, out p);
            var b = BotAI.Create(World, Names[i % Names.Length], Outfits[i % Outfits.Length], BotWeapons[Random.Range(0, BotWeapons.Length)], p, Mode == GameMode.BattleRoyale, respawn ? 2.5f : 0f);
            b.health.kills = keepKills;
            int idx = i; b.health.Died += (v, a, head) => OnDied(v, a, head, idx);
            return b;
        }

        void OnDied(Health v, Health a, bool head) { OnDied(v, a, head, -1); }

        void OnDied(Health victim, Health attacker, bool head, int botIdx)
        {
            UI.Feed(attacker != null ? attacker.displayName : null, victim.displayName, head, attacker != null && attacker.isPlayer);
            if (attacker != null && attacker.isPlayer) { Sfx.Play(Sfx.Kill(), .8f); UI.Banner("ÉLIMINÉ  " + victim.displayName + (head ? "  (tête)" : ""), 1.5f); }

            if (victim.isPlayer)
            {
                killer = attacker;
                if (Mode == GameMode.BattleRoyale) { place = AliveCount() + 1; ending = true; Invoke("LoseNow", 1.6f); }
                else { UI.Banner("Éliminé ! Réapparition…", 2.8f); Invoke("RespawnPlayer", 3f); }
                return;
            }
            var bot = victim.GetComponent<BotAI>();
            if (Mode == GameMode.BattleRoyale)
            {
                Vector3 p = victim.transform.position;
                Pickup.Spawn(PickupType.Ammo, p + Vector3.right * .5f);
                if (Random.value < .6f) Pickup.Spawn(PickupType.Weapon, p - Vector3.right * .5f, bot != null ? bot.wd.id : "ar");
                if (Random.value < .5f) Pickup.Spawn(Random.value < .5f ? PickupType.Health : PickupType.Shield, p + Vector3.forward * .6f);
                Destroy(victim.gameObject, 8f);
            }
            else
            {
                int kills = victim.kills; string nm = victim.displayName;
                StartCoroutine(RespawnBot(botIdx, victim.gameObject, kills));
            }
        }

        System.Collections.IEnumerator RespawnBot(int idx, GameObject old, int kills)
        {
            yield return new WaitForSeconds(3.5f);
            if (old != null) Destroy(old);
            if (State == GameState.Ended || idx < 0 || idx >= Bots.Count) yield break;
            Bots[idx] = SpawnBot(idx, true, kills);
        }

        void RespawnPlayer()
        {
            if (State == GameState.Ended || Player == null) return;
            var h = Player.health; h.dead = false; h.hp = 100; h.shield = 0;
            Player.weapons.slots[0].ammo = Player.weapons.slots[0].def.mag; Player.weapons.slots[0].reserve = Player.weapons.slots[0].def.reserve;
            Player.weapons.slots[1].ammo = Player.weapons.slots[1].def.mag; Player.weapons.slots[1].reserve = Player.weapons.slots[1].def.reserve;
            PlacePlayer();
        }

        public int AliveCount()
        {
            int n = (Player != null && !Player.health.dead) ? 1 : 0;
            foreach (var b in Bots) if (b != null && !b.health.dead) n++;
            return n;
        }

        void FfaRules()
        {
            int top = Player.health.kills; foreach (var b in Bots) if (b != null) top = Mathf.Max(top, b.health.kills);
            if (top >= 20 || MatchTime >= 300f)
            {
                bool win = Player.health.kills >= top;
                EndGame(win);
            }
        }

        void WinNow() { EndGame(true); }
        void LoseNow() { EndGame(false); }

        public void EndGame(bool win)
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            CancelInvoke(); State = GameState.Ended; Time.timeScale = 1f; SetCursor(false);
            int pk = Player != null ? Player.health.kills : 0;
            Cfg.Games++; if (win) Cfg.Wins++; Cfg.TotalKills += pk; Cfg.Best = Mathf.Max(Cfg.Best, pk);
            int rank = 1; foreach (var b in Bots) if (b != null && b.health.kills > pk) rank++;
            string sub = Mode == GameMode.BattleRoyale ? (win ? "Dernier survivant sur l'île !" : "Éliminé par " + (killer != null ? killer.displayName : "la tempête")) : (win ? "Tu domines la mêlée !" : "Fin du match");
            UI.ShowEnd(win, sub, Mode == GameMode.BattleRoyale ? Mathf.Max(1, place) : rank, pk, MatchTime);
        }

        public void Pause() { State = GameState.Paused; Time.timeScale = 0f; SetCursor(false); UI.Show("pause"); }
        public void Resume() { State = GameState.Playing; Time.timeScale = 1f; SetCursor(true); UI.ShowHud(Mode); }

        public void QuitToMenu()
        {
            CleanupMatch(); State = GameState.Menu; ending = false; SetCursor(false); UI.Show("main");
        }
    }
}
