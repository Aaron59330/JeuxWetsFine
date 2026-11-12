using UnityEngine;

namespace BattleZone
{
    /// <summary>Tempête qui se resserre (Battle Royale).</summary>
    public class ZoneController : MonoBehaviour
    {
        struct Phase { public float r, wait, shrink, dmg; public Phase(float r, float w, float s, float d) { this.r = r; wait = w; shrink = s; dmg = d; } }
        static readonly Phase[] P = { new Phase(130, 40, 35, 1.5f), new Phase(85, 30, 30, 3), new Phase(50, 25, 25, 5), new Phase(25, 20, 20, 8), new Phase(8, 15, 15, 12), new Phase(2, 10, 12, 20) };

        public Vector2 Center; public float Radius = 230f, Damage = 1f;
        public Vector2 NextCenter; public float NextRadius; public bool Shrinking;
        public int phase; public float t;
        Vector2 fromC; float fromR; Transform tube; float warnT;

        public void Init()
        {
            Center = Vector2.zero; Radius = 230f; Pick();
            var go = new GameObject("StormWall"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = U.Tube(72, 320f);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = U.Unlit(new Color(.6f, .24f, 1f, .3f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tube = go.transform; tube.position = new Vector3(0, -20f, 0);
        }

        void Pick()
        {
            var ph = P[Mathf.Min(phase, P.Length - 1)];
            fromC = Center; fromR = Radius; NextRadius = ph.r; Damage = ph.dmg;
            float a = Random.Range(0, 6.28f), d = Random.value * Mathf.Max(0, fromR - ph.r) * .85f;
            float lim = WorldBuilder.MAP - ph.r * .3f;
            NextCenter = new Vector2(Mathf.Clamp(fromC.x + Mathf.Cos(a) * d, -lim, lim), Mathf.Clamp(fromC.y + Mathf.Sin(a) * d, -lim, lim));
        }

        public string TimerText()
        {
            var ph = P[Mathf.Min(phase, P.Length - 1)];
            int rem = Mathf.Max(0, Mathf.CeilToInt(Shrinking ? ph.shrink - t : ph.wait - t));
            return (Shrinking ? "Rétrécit " : "Dans ") + rem / 60 + ":" + (rem % 60).ToString("00");
        }

        void Update()
        {
            var gb = GameBootstrap.I;
            if (gb.State != GameState.Playing) return;
            float dt = Time.deltaTime; var ph = P[Mathf.Min(phase, P.Length - 1)];
            t += dt;
            if (!Shrinking) { if (t >= ph.wait) { Shrinking = true; t = 0; gb.UI.Banner("La tempête se resserre !", 2.5f); Sfx.Play(Sfx.Warn(), .6f); } }
            else
            {
                float k = Mathf.Clamp01(t / ph.shrink);
                Radius = Mathf.Lerp(fromR, NextRadius, k); Center = Vector2.Lerp(fromC, NextCenter, k);
                if (k >= 1f) { if (phase < P.Length - 1) { phase++; Shrinking = false; t = 0; Pick(); } else t = ph.shrink; }
            }
            tube.position = new Vector3(Center.x, -20f, Center.y); tube.localScale = new Vector3(Radius, 1f, Radius);

            warnT -= dt;
            foreach (var h in Health.All.ToArray())
            {
                if (h == null || h.dead) continue;
                Vector2 p = new Vector2(h.transform.position.x, h.transform.position.z);
                if (Vector2.Distance(p, Center) > Radius)
                {
                    h.Damage(Damage * dt, null, false, true);
                    if (h.isPlayer && warnT <= 0) { warnT = 1.5f; Sfx.Play(Sfx.Warn(), .5f); gb.UI.Toast("Retourne dans le cercle !"); }
                }
            }
        }
    }
}
