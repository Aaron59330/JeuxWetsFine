using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    [System.Serializable] public class Poi { public Vector2 pos; public float h; public float R = 40f; }

    /// <summary>
    /// Construit toute l'île par code : terrain (collines, falaises, plages, lagon), eau, ciel,
    /// forêts, rochers, villages. Pour des arbres/rochers plus réalistes, place tes prefabs dans
    /// Assets/BattleZone/Resources/Trees et Resources/Rocks : ils seront utilisés automatiquement.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public const float ISL = 150f, MAP = 150f, TS = 420f, TH = 90f, BASE = -12f;
        public const int RES = 513;

        public Terrain terrain;
        public List<Poi> pois = new List<Poi>();
        public List<Vector3> loot = new List<Vector3>();
        public List<Rect> rects = new List<Rect>();
        public Texture2D minimapTex;
        public Light sun;

        System.Random rng = new System.Random(20240611);
        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        static float Fbm(float x, float z, int o = 4)
        {
            float a = .5f, f = 1f, s = 0f;
            for (int i = 0; i < o; i++) { s += a * Mathf.PerlinNoise(x * f + 17.3f * i + 100f, z * f - 9.1f * i + 100f); f *= 2.03f; a *= .5f; }
            return s;
        }

        // ---------- hauteur du terrain (formule) ----------
        static float RawH(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z), mask = 1f - Sm(.72f, 1.05f, d / ISL);
            float land = 5f + (Fbm(x * .011f + 3f, z * .011f - 5f) - .47f) * 52f;
            land += Pk(x, z, -58, -62, 40, 30) + Pk(x, z, 72, 52, 28, 26) + Pk(x, z, -30, 72, 20, 20) + Pk(x, z, 95, -70, 22, 18);
            float t = land / 5f, fl = Mathf.Floor(t), fr = t - fl, terr = (fl + Sm(.55f, .92f, fr)) * 5f;   // paliers = falaises
            land = Mathf.Lerp(land, terr, Sm(6f, 16f, land) * .8f);
            land -= 17f * (1f - Sm(.3f, 1f, Mathf.Sqrt(Mathf.Pow((x - 48f) / 34f, 2) + Mathf.Pow((z + 40f) / 21f, 2))));   // lagon
            return Mathf.Lerp(-8f, land, mask);
        }
        static float Pk(float x, float z, float cx, float cz, float a, float s) { return a * Mathf.Exp(-((x - cx) * (x - cx) + (z - cz) * (z - cz)) / (2f * s * s)); }

        public float H(float x, float z)
        {
            float h = RawH(x, z);
            for (int i = 0; i < pois.Count; i++)
            {
                var p = pois[i]; float dx = x - p.pos.x, dz = z - p.pos.y, d2 = dx * dx + dz * dz;
                if (d2 < p.R * p.R) h = Mathf.Lerp(h, p.h, 1f - Sm(.6f * p.R, p.R, Mathf.Sqrt(d2)));
            }
            return h;
        }
        /// <summary>Hauteur réelle (mètres) à la position monde.</summary>
        public float HeightAt(float x, float z)
        {
            return terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : H(x, z);
        }

        // ---------- construction ----------
        public void Build()
        {
            ChoosePois();
            BuildTerrain();
            BuildWaterAndSky();
            BuildBuildings();
            BuildVegetation();
            BuildOpenLoot();
            BuildMinimap();
        }

        void ChoosePois()
        {
            System.Action<float, float> tryAdd = (x, z) =>
            {
                float h = RawH(x, z); if (h < 1.8f || h > 14f) return;
                if (Mathf.Sqrt(Mathf.Pow((x - 48f) / 34f, 2) + Mathf.Pow((z + 40f) / 21f, 2)) < 1.7f) return;
                foreach (var p in pois) if (Vector2.Distance(p.pos, new Vector2(x, z)) < 46f) return;
                float[,] o = { { 14, 0 }, { -14, 0 }, { 0, 14 }, { 0, -14 } };
                for (int i = 0; i < 4; i++) if (Mathf.Abs(RawH(x + o[i, 0], z + o[i, 1]) - h) > 9f) return;
                pois.Add(new Poi { pos = new Vector2(x, z), h = h });
            };
            tryAdd(0, 0);
            for (int i = 0; i < 1500 && pois.Count < 8; i++)
            {
                float a = R(0, 6.283f), r = Mathf.Sqrt((float)rng.NextDouble()) * 105f;
                tryAdd(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
        }

        void BuildTerrain()
        {
            var td = new TerrainData();
            td.heightmapResolution = RES;
            td.size = new Vector3(TS, TH, TS);
            var hs = new float[RES, RES];
            for (int y = 0; y < RES; y++)
                for (int x = 0; x < RES; x++)
                {
                    float wx = -TS / 2f + x * TS / (RES - 1), wz = -TS / 2f + y * TS / (RES - 1);
                    hs[y, x] = Mathf.Clamp01((H(wx, wz) - BASE) / TH);
                }
            td.SetHeights(0, 0, hs);

            // couches de texture procédurales : herbe, roche, sable, fond marin
            var grass = new TerrainLayer { diffuseTexture = U.NoiseTex(new Color(.2f, .34f, .12f), new Color(.38f, .5f, .2f), 6, .12f), tileSize = new Vector2(7, 7) };
            var rock = new TerrainLayer { diffuseTexture = U.NoiseTex(new Color(.32f, .26f, .23f), new Color(.55f, .46f, .4f), 5, .1f, 256, 6f), tileSize = new Vector2(12, 12) };
            var sand = new TerrainLayer { diffuseTexture = U.NoiseTex(new Color(.78f, .66f, .45f), new Color(.9f, .8f, .58f), 8, .06f), tileSize = new Vector2(6, 6) };
            var sea = new TerrainLayer { diffuseTexture = U.NoiseTex(new Color(.05f, .22f, .35f), new Color(.15f, .5f, .55f), 4, .05f), tileSize = new Vector2(10, 10) };
            td.terrainLayers = new[] { grass, rock, sand, sea };

            int ar = 512; td.alphamapResolution = ar;
            var am = new float[ar, ar, 4];
            for (int y = 0; y < ar; y++)
                for (int x = 0; x < ar; x++)
                {
                    float nx = x / (float)(ar - 1), nz = y / (float)(ar - 1), wx = -TS / 2f + nx * TS, wz = -TS / 2f + nz * TS;
                    float h = hs[Mathf.Min(RES - 1, y * (RES - 1) / (ar - 1)), Mathf.Min(RES - 1, x * (RES - 1) / (ar - 1))] * TH + BASE;
                    float steep = td.GetSteepness(nx, nz);
                    float rk = Mathf.Max(Sm(26f, 46f, steep), Sm(26f, 38f, h) * .6f);
                    float sd = (1f - Sm(.8f, 2.4f, h + Mathf.PerlinNoise(wx * .3f, wz * .3f) * .8f)) * (1f - rk * .8f);
                    float sb = Sm(.3f, -.2f, h);
                    float w0 = (1f - rk) * (1f - sd), w1 = rk * (1f - sb), w2 = sd * (1f - sb), w3 = sb;
                    float sum = w0 + w1 + w2 + w3 + 1e-5f;
                    am[y, x, 0] = w0 / sum; am[y, x, 1] = w1 / sum; am[y, x, 2] = w2 / sum; am[y, x, 3] = w3 / sum;
                }
            td.SetAlphamaps(0, 0, am);

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(-TS / 2f, BASE, -TS / 2f);
            terrain = go.GetComponent<Terrain>();
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 400f;
        }

        void BuildWaterAndSky()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(water.GetComponent<Collider>());
            water.name = "Water"; water.transform.SetParent(transform, false);
            water.transform.localScale = new Vector3(300, 1, 300);
            var wr = water.GetComponent<Renderer>(); wr.sharedMaterial = U.Unlit(new Color(.1f, .55f, .66f, .62f)); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var deep = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(deep.GetComponent<Collider>());
            deep.name = "Deep"; deep.transform.SetParent(transform, false);
            deep.transform.position = new Vector3(0, -9, 0); deep.transform.localScale = new Vector3(300, 1, 300);
            deep.GetComponent<Renderer>().sharedMaterial = U.Unlit(new Color(.04f, .21f, .35f, 1f));

            // soleil (coucher de soleil doré)
            var lg = new GameObject("Sun"); lg.transform.SetParent(transform, false);
            sun = lg.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, .86f, .66f); sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .9f;
            lg.transform.rotation = Quaternion.Euler(32f, -130f, 0f);
            RenderSettings.sun = sun;

            var sky = Shader.Find("Skybox/Procedural");
            if (sky != null)
            {
                var sm = new Material(sky);
                sm.SetFloat("_SunSize", .06f); sm.SetFloat("_AtmosphereThickness", 1.15f);
                sm.SetColor("_SkyTint", new Color(.5f, .55f, .7f)); sm.SetColor("_GroundColor", new Color(.55f, .55f, .5f));
                sm.SetFloat("_Exposure", 1.2f);
                RenderSettings.skybox = sm;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.78f, .82f, .86f); RenderSettings.fogDensity = .0011f;
        }

        // ---------- bâtiments ----------
        GameObject Box(float x0, float y0, float z0, float x1, float y1, float z1, Material m, bool collide = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.SetParent(transform, false);
            g.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
            g.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Destroy(g.GetComponent<Collider>());
            return g;
        }

        readonly List<Rect> placed = new List<Rect>();
        bool Free(Rect r, float m = 3f)
        {
            r = new Rect(r.xMin - m, r.yMin - m, r.width + 2 * m, r.height + 2 * m);
            foreach (var p in placed) if (p.Overlaps(r)) return false;
            return true;
        }

        void BuildBuildings()
        {
            Color[] wallCols = { new Color(.73f, .65f, .54f), new Color(.6f, .64f, .66f), new Color(.56f, .57f, .53f), new Color(.79f, .72f, .6f), new Color(.49f, .54f, .56f), new Color(.64f, .52f, .42f) };
            Color[] roofCols = { new Color(.29f, .31f, .33f), new Color(.48f, .25f, .21f), new Color(.24f, .29f, .25f), new Color(.33f, .35f, .37f) };
            var concrete = U.Lit(new Color(.7f, .72f, .75f));
            var wood = U.Lit(new Color(.5f, .36f, .22f));
            var crateM = U.Lit(new Color(.65f, .45f, .22f));
            foreach (var p in pois)
            {
                float by = p.h; int count = 3 + rng.Next(3), made = 0, tries = 0;
                while (made < count && tries++ < 60)
                {
                    float w = R(8, 13), d = R(8, 11), h = new[] { 3.2f, 3.6f, 4.2f }[rng.Next(3)];
                    float cx = p.pos.x + R(-24, 24), cz = p.pos.y + R(-24, 24);
                    if (!Free(new Rect(cx - w / 2, cz - d / 2, w + 2, d))) continue;
                    Building(cx, cz, w, d, h, by, U.Lit(wallCols[rng.Next(wallCols.Length)]), U.Lit(roofCols[rng.Next(roofCols.Length)]), concrete, wood);
                    made++;
                    // caisses
                    int nc = 1 + rng.Next(3);
                    for (int i = 0; i < nc; i++)
                    {
                        float x = cx + w / 2 + 4 + R(-3, 3), z = cz + R(-5, 5), gy = HeightAt(x, z);
                        if (!Free(new Rect(x - .6f, z - .6f, 1.2f, 1.2f), 0f)) continue;
                        Box(x - .6f, gy - .5f, z - .6f, x + .6f, gy + 1.2f, z + .6f, crateM);
                        placed.Add(new Rect(x - .6f, z - .6f, 1.2f, 1.2f));
                    }
                }
                for (int k = 0; k < 2; k++)
                {
                    float cx = p.pos.x + R(-28, 28), cz = p.pos.y + R(-28, 28);
                    bool rot = rng.NextDouble() < .5; float a = rot ? 2.4f : 6f, b = rot ? 6f : 2.4f;
                    if (!Free(new Rect(cx - a / 2, cz - b / 2, a, b), 1f)) continue;
                    var cols = new[] { new Color(.48f, .18f, .16f), new Color(.18f, .29f, .42f), new Color(.54f, .42f, .16f), new Color(.23f, .35f, .23f), new Color(.33f, .35f, .38f) };
                    float gy = HeightAt(cx, cz) + .2f;
                    Box(cx - a / 2, gy - 1f, cz - b / 2, cx + a / 2, gy + 2.6f, cz + b / 2, U.Lit(cols[rng.Next(cols.Length)], .35f));
                    placed.Add(new Rect(cx - a / 2 - 1, cz - b / 2 - 1, a + 2, b + 2));
                    rects.Add(new Rect(cx - a / 2, cz - b / 2, a, b));
                }
            }
        }

        void Building(float cx, float cz, float w, float d, float h, float by, Material wm, Material rm, Material concrete, Material wood)
        {
            const float T = .4f;
            float x0 = cx - w / 2, x1 = cx + w / 2, z0 = cz - d / 2, z1 = cz + d / 2, b0 = by - 1.5f;
            Box(x0 - .5f, b0, z0 - .5f, x1 + .5f, by + .08f, z1 + .5f, concrete, false);
            // murs : Z (porte) et X (fenêtre)
            WallZ(x0, x1, z0, h, wm, rng.NextDouble() < .5 ? cx : float.NaN, by, T);
            WallZ(x0, x1, z1 - T, h, wm, cx, by, T);
            WallX(x0, z0 + T, z1 - T, h, wm, cz, by, T);
            WallX(x1 - T, z0 + T, z1 - T, h, wm, rng.NextDouble() < .6 ? cz : float.NaN, by, T);
            Box(x0, by + h, z0, x1, by + h + .3f, z1, rm);
            Box(x0, by + h + .3f, z0, x1, by + h + 1.1f, z0 + .3f, rm);
            Box(x0, by + h + .3f, z1 - .3f, x1, by + h + 1.1f, z1, rm);
            Box(x0, by + h + .3f, z0, x0 + .3f, by + h + 1.1f, z1, rm);
            float top = h + .3f; int n = Mathf.CeilToInt(top / .5f);       // escalier extérieur vers le toit
            for (int i = 0; i < n; i++)
                Box(x1, b0, z0 + 1 + i * .8f, x1 + 1.8f, by + top * (i + 1) / n, z0 + 1.8f + i * .8f, concrete);
            if (rng.NextDouble() < .7) Box(cx - 1, b0, cz - 1, cx + 1, by + .9f, cz + 1, wood);
            placed.Add(new Rect(x0 - 3, z0 - 3, w + 7.5f, d + 6));
            rects.Add(new Rect(x0, z0, w, d));
            loot.Add(new Vector3(cx + R(-w / 3, w / 3), by + .1f, cz + R(-d / 3, d / 3)));
            loot.Add(new Vector3(cx + R(-w / 3, w / 3), by + .1f, cz + R(-d / 3, d / 3)));
            loot.Add(new Vector3(cx, by + top + .1f, cz));
        }

        void WallZ(float xa, float xb, float z, float h, Material m, float hole, float by, float T)
        {
            float b0 = by - 1.5f;
            if (float.IsNaN(hole)) { Box(xa, b0, z, xb, by + h, z + T, m); return; }
            Box(xa, b0, z, hole - 1.2f, by + h, z + T, m); Box(hole + 1.2f, b0, z, xb, by + h, z + T, m); Box(hole - 1.2f, by + 2.7f, z, hole + 1.2f, by + h, z + T, m);
        }
        void WallX(float x, float za, float zb, float h, Material m, float win, float by, float T)
        {
            float b0 = by - 1.5f;
            if (float.IsNaN(win)) { Box(x, b0, za, x + T, by + h, zb, m); return; }
            Box(x, b0, za, x + T, by + h, win - 1.2f, m); Box(x, b0, win + 1.2f, x + T, by + h, zb, m);
            Box(x, b0, win - 1.2f, x + T, by + 1f, win + 1.2f, m); Box(x, by + 2.1f, win - 1.2f, x + T, by + h, win + 1.2f, m);
        }

        // ---------- végétation ----------
        class Variant { public Mesh mesh; public Material[] mats; public bool pine; }

        Variant MakeBroad(int seed, Color lo, Color hi)
        {
            var r = new System.Random(seed); System.Func<float, float, float> rr = (a, b) => a + (float)r.NextDouble() * (b - a);
            var trunk = new List<CombineInstance>(); var leaf = new List<CombineInstance>();
            float H = rr(3.4f, 4.8f), sw = rr(-.4f, .4f);
            trunk.Add(new CombineInstance { mesh = U.PrimMesh(PrimitiveType.Cylinder), transform = Matrix4x4.TRS(new Vector3(sw / 2, H / 2, 0), Quaternion.Euler(0, 0, -sw * 6f), new Vector3(.75f, H / 2f + .2f, .75f)) });
            var blobs = new[] { U.Blob(seed, .3f), U.Blob(seed + 5, .3f), U.Blob(seed + 9, .3f) };
            int n = 8 + r.Next(4);
            for (int i = 0; i < n; i++)
            {
                float a = rr(0, 6.28f), rad = rr(0, 1.9f), s = rr(2.4f, 4f);
                leaf.Add(new CombineInstance { mesh = blobs[i % 3], transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * rad + sw, H + .2f + rr(0, 1.9f), Mathf.Sin(a) * rad), Quaternion.Euler(rr(0, 360), rr(0, 360), 0), new Vector3(s, s * .8f, s)) });
            }
            return Combine(trunk, leaf, new Color(.33f, .24f, .17f), Color.Lerp(lo, hi, .5f), false);
        }

        Variant MakePine(int seed, Color lo, Color hi)
        {
            var r = new System.Random(seed);
            var trunk = new List<CombineInstance>(); var leaf = new List<CombineInstance>();
            float H = 7f + (float)r.NextDouble() * 2.5f;
            trunk.Add(new CombineInstance { mesh = U.PrimMesh(PrimitiveType.Cylinder), transform = Matrix4x4.TRS(new Vector3(0, H / 2, 0), Quaternion.identity, new Vector3(.7f, H / 2f, .7f)) });
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f, rad = Mathf.Lerp(2.3f, .6f, t) * (.9f + (float)r.NextDouble() * .2f), h = Mathf.Lerp(2.4f, 1.7f, t);
                leaf.Add(new CombineInstance { mesh = U.Cone(11, .15f, seed + i), transform = Matrix4x4.TRS(new Vector3(0, 1.7f + t * (H - 2.6f), 0), Quaternion.Euler(0, i * 37f, 0), new Vector3(rad, h, rad)) });
            }
            return Combine(trunk, leaf, new Color(.29f, .21f, .15f), Color.Lerp(lo, hi, .5f), true);
        }

        Variant Combine(List<CombineInstance> trunk, List<CombineInstance> leaf, Color bark, Color foliage, bool pine)
        {
            var a = new Mesh(); a.CombineMeshes(trunk.ToArray(), true, true);
            var b = new Mesh(); b.CombineMeshes(leaf.ToArray(), true, true);
            var f = new Mesh(); f.CombineMeshes(new[] { new CombineInstance { mesh = a, transform = Matrix4x4.identity }, new CombineInstance { mesh = b, transform = Matrix4x4.identity } }, false, false);
            var bm = U.Lit(bark, .05f); var lm = U.Lit(foliage, .08f); bm.enableInstancing = true; lm.enableInstancing = true;
            return new Variant { mesh = f, mats = new[] { bm, lm }, pine = pine };
        }

        void BuildVegetation()
        {
            var root = new GameObject("Vegetation").transform; root.SetParent(transform, false);
            var customTrees = Resources.LoadAll<GameObject>("Trees");
            var customRocks = Resources.LoadAll<GameObject>("Rocks");

            var broad = new[] { MakeBroad(11, new Color(.17f, .31f, .12f), new Color(.42f, .6f, .22f)), MakeBroad(23, new Color(.21f, .38f, .17f), new Color(.48f, .66f, .25f)), MakeBroad(37, new Color(.16f, .29f, .12f), new Color(.34f, .5f, .18f)), MakeBroad(51, new Color(.29f, .35f, .12f), new Color(.65f, .66f, .23f)) };
            var pines = new[] { MakePine(5, new Color(.09f, .21f, .12f), new Color(.21f, .42f, .23f)), MakePine(9, new Color(.1f, .24f, .13f), new Color(.25f, .48f, .25f)), MakePine(14, new Color(.11f, .25f, .15f), new Color(.18f, .42f, .28f)) };

            int count = 0;
            for (int i = 0; i < 4000 && count < 520; i++)
            {
                float x = R(-ISL, ISL), z = R(-ISL, ISL);
                if (Mathf.Sqrt(x * x + z * z) > 130f) continue;
                float h = H(x, z); if (h < 1.7f) continue;
                if (Mathf.Max(Mathf.Abs(H(x + 2, z) - h), Mathf.Abs(H(x, z + 2) - h)) / 2f > .5f) continue;
                if (Fbm(x * .022f + 90f, z * .022f - 40f, 3) < .45f && rng.NextDouble() > .1) continue;
                var fr = new Rect(x - 1.2f, z - 1.2f, 2.4f, 2.4f);
                if (!Free(fr, .5f)) continue;
                placed.Add(fr); count++;
                float sc = R(.85f, 1.5f);
                GameObject tree;
                if (customTrees.Length > 0)
                {
                    tree = Instantiate(customTrees[rng.Next(customTrees.Length)], new Vector3(x, h, z), Quaternion.Euler(0, R(0, 360), 0), root);
                    tree.transform.localScale *= sc;
                }
                else
                {
                    bool pine = h > 9f ? rng.NextDouble() < .8 : rng.NextDouble() < .35;
                    var v = pine ? pines[rng.Next(pines.Length)] : broad[rng.Next(broad.Length)];
                    tree = new GameObject("Tree");
                    tree.transform.SetParent(root, false);
                    tree.transform.position = new Vector3(x, h - .2f, z);
                    tree.transform.rotation = Quaternion.Euler(0, R(0, 360), 0);
                    tree.transform.localScale = Vector3.one * sc;
                    tree.AddComponent<MeshFilter>().sharedMesh = v.mesh;
                    tree.AddComponent<MeshRenderer>().sharedMaterials = v.mats;
                    var cc = tree.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0, 2.5f, 0); cc.radius = .4f; cc.height = 5f;
                }
            }

            // rochers
            var rockMeshes = new[] { U.Blob(1, .5f), U.Blob(4, .5f), U.Blob(8, .5f) };
            var rockMat = U.Lit(new Color(.42f, .38f, .34f), .05f);
            int rc = 0;
            for (int i = 0; i < 900 && rc < 120; i++)
            {
                float x = R(-ISL, ISL), z = R(-ISL, ISL); if (Mathf.Sqrt(x * x + z * z) > 130f) continue;
                float h = H(x, z); if (h < 1.5f) continue;
                float slope = Mathf.Max(Mathf.Abs(H(x + 2, z) - h), Mathf.Abs(H(x, z + 2) - h)) / 2f;
                if (slope < .12f && rng.NextDouble() > .15) continue;
                float s = R(2.2f, 6.4f); var fr = new Rect(x - s / 2, z - s / 2, s, s);
                if (!Free(fr, .5f)) continue;
                placed.Add(fr); rc++;
                GameObject rock;
                if (customRocks.Length > 0) { rock = Instantiate(customRocks[rng.Next(customRocks.Length)], new Vector3(x, h, z), Quaternion.Euler(0, R(0, 360), 0), root); rock.transform.localScale *= s / 3f; }
                else
                {
                    rock = new GameObject("Rock"); rock.transform.SetParent(root, false);
                    rock.transform.position = new Vector3(x, h + s * .1f, z);
                    rock.transform.rotation = Quaternion.Euler(0, R(0, 360), 0);
                    rock.transform.localScale = new Vector3(s, s * .75f, s);
                    rock.AddComponent<MeshFilter>().sharedMesh = rockMeshes[rng.Next(3)];
                    rock.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
                    rock.AddComponent<SphereCollider>().radius = .42f;
                }
            }
        }

        void BuildOpenLoot()
        {
            for (int i = 0; i < 90 && loot.Count < 400; i++)
            {
                float x = R(-ISL, ISL), z = R(-ISL, ISL); if (Mathf.Sqrt(x * x + z * z) > 125f) continue;
                float h = H(x, z); if (h > 1f) loot.Add(new Vector3(x, h + .1f, z));
            }
        }

        // ---------- minimap ----------
        void BuildMinimap()
        {
            const int N = 256; float ext = MAP * 2f + 20f;
            var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
            var px = new Color[N * N];
            for (int py = 0; py < N; py++)
                for (int pxl = 0; pxl < N; pxl++)
                {
                    float x = -MAP - 10f + pxl / (float)N * ext, z = -MAP - 10f + py / (float)N * ext, h = H(x, z);
                    float sl = H(x + 2, z + 2) - H(x - 2, z - 2), sh = Mathf.Clamp(1f - sl * .035f, .6f, 1.3f);
                    Color c;
                    if (h < 0f) c = Color.Lerp(new Color(.25f, .65f, .7f), new Color(.04f, .2f, .36f), Mathf.Clamp01(-h / 4f));
                    else if (h < 1.8f) c = new Color(.86f, .75f, .52f);
                    else if (Mathf.Abs(sl) > 3f) c = new Color(.45f, .38f, .33f);
                    else c = Color.Lerp(new Color(.28f, .46f, .2f), new Color(.5f, .58f, .3f), Mathf.Clamp01(h / 25f));
                    px[py * N + pxl] = c * sh; px[py * N + pxl].a = 1;
                }
            tex.SetPixels(px); tex.Apply();
            minimapTex = tex;
        }

        // ---------- tirage de positions ----------
        public bool RandomSpawn(float minDistFrom, Vector3 from, out Vector3 pos)
        {
            for (int i = 0; i < 60; i++)
            {
                float x = Random.Range(-MAP + 8, MAP - 8), z = Random.Range(-MAP + 8, MAP - 8);
                if (Mathf.Sqrt(x * x + z * z) > 125f) continue;
                float h = H(x, z); if (h < 1.2f) continue;
                if (Mathf.Abs(H(x + 2, z) - h) > 1.4f || Mathf.Abs(H(x, z + 2) - h) > 1.4f) continue;
                pos = new Vector3(x, h + .1f, z);
                if (minDistFrom > 0 && Vector3.Distance(pos, from) < minDistFrom) continue;
                if (Physics.CheckSphere(pos + Vector3.up * 1f, .6f)) continue;
                return true;
            }
            pos = new Vector3(0, HeightAt(0, 0) + .2f, 0);
            return false;
        }
    }
}
