using System.Collections.Generic;
using UnityEngine;

namespace BattleZone
{
    /// <summary>Outils : matériaux compatibles Built-in / URP / HDRP, primitives, meshes procéduraux.</summary>
    public static class U
    {
        static Material baseLit;
        static readonly Dictionary<PrimitiveType, Mesh> primMeshes = new Dictionary<PrimitiveType, Mesh>();

        public static Material Lit(Color c, float smooth = .12f)
        {
            if (baseLit == null)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseLit = g.GetComponent<Renderer>().sharedMaterial;   // matériau par défaut du pipeline actif
                Object.Destroy(g);
            }
            var m = new Material(baseLit);
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Material Unlit(Color c)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.color = c;
            return m;
        }

        public static Mesh PrimMesh(PrimitiveType t)
        {
            Mesh m;
            if (!primMeshes.TryGetValue(t, out m))
            {
                var g = GameObject.CreatePrimitive(t);
                m = g.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(g);
                primMeshes[t] = m;
            }
            return m;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 ls, Material m, bool keepCollider = false, bool noShadow = false)
        {
            var g = GameObject.CreatePrimitive(t);
            if (!keepCollider) { var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            g.transform.SetParent(parent, false);
            g.transform.localPosition = lp;
            g.transform.localScale = ls;
            var r = g.GetComponent<Renderer>();
            if (m != null) r.sharedMaterial = m;
            if (noShadow) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        public static void SetLayerRecursive(GameObject g, int layer)
        {
            g.layer = layer;
            foreach (Transform t in g.transform) SetLayerRecursive(t.gameObject, layer);
        }

        static Material tracerMat;
        public static void Tracer(Vector3 a, Vector3 b, Color c)
        {
            if (tracerMat == null) tracerMat = Unlit(Color.white);
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = tracerMat;
            lr.startColor = lr.endColor = c;
            lr.startWidth = .035f; lr.endWidth = .015f;
            lr.positionCount = 2;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(go, .07f);
        }

        public static void Spark(Vector3 p, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.position = p; g.transform.localScale = Vector3.one * .12f;
            g.GetComponent<Renderer>().sharedMaterial = Unlit(c);
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(g, .12f);
        }

        /// <summary>Cône (base en y=0, pointe en y=1, rayon 1).</summary>
        public static Mesh Cone(int seg = 12, float jag = 0f, int seed = 0)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            v.Add(new Vector3(0, 1, 0)); uv.Add(new Vector2(.5f, 1));
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f, r = 1f + jag * Mathf.Sin(a * 7f + seed) * Mathf.Cos(a * 3f);
                v.Add(new Vector3(Mathf.Cos(a) * r, -jag * .5f * Mathf.Sin(a * 5f + seed), Mathf.Sin(a) * r)); uv.Add(new Vector2(i / (float)seg, 0));
            }
            for (int i = 1; i <= seg; i++) { t.Add(0); t.Add(i + 1); t.Add(i); }
            v.Add(new Vector3(0, 0, 0)); uv.Add(new Vector2(.5f, .5f));
            int c = v.Count - 1;
            for (int i = 1; i <= seg; i++) { t.Add(c); t.Add(i); t.Add(i + 1); }
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Sphère déformée (feuillage / rochers).</summary>
        public static Mesh Blob(int seed, float rough)
        {
            var src = PrimMesh(PrimitiveType.Sphere);
            var m = Object.Instantiate(src);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = v[i] * 2f;
                float d = 1f + rough * .6f * (Mathf.Sin(p.x * 3.7f + seed) * Mathf.Cos(p.z * 3.1f + seed * 1.7f) + .6f * Mathf.Sin(p.y * 5.3f + seed * 2.3f) * Mathf.Sin(p.x * 4.1f + seed));
                v[i] = p * d * .5f;
            }
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Tube ouvert (mur de la tempête).</summary>
        public static Mesh Tube(int seg = 64, float height = 300f)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); v.Add(new Vector3(Mathf.Cos(a), height, Mathf.Sin(a)));
            }
            for (int i = 0; i < seg; i++) { int k = i * 2; t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k + 1); t.Add(k + 3); t.Add(k + 2); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateBounds();
            m.bounds = new Bounds(new Vector3(0, height / 2, 0), new Vector3(2000, height, 2000));
            return m;
        }

        public static Texture2D NoiseTex(Color a, Color b, float scale, float speckle, int size = 256, float bands = 0f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x / (float)size * scale, y / (float)size * scale) * .6f + Mathf.PerlinNoise(x / (float)size * scale * 4f, y / (float)size * scale * 4f) * .4f;
                    if (bands > 0) n = Mathf.Clamp01(n * .5f + .25f * Mathf.Sin(y / (float)size * bands * Mathf.PI * 2f + n * 4f) + .25f);
                    Color c = Color.Lerp(a, b, n) * (1f - speckle + Random.value * speckle * 2f);
                    c.a = 1; px[y * size + x] = c;
                }
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }

        public static Texture2D CircleTex(int size, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - .97f) / .03f) : (d < 1 ? 1 : 0);
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }
    }

    /// <summary>Sons générés par code (aucun fichier audio).</summary>
    public static class Sfx
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            AudioListener.volume = Cfg.Volume;
        }

        static AudioClip Make(string key, float dur, System.Func<float, float> f)
        {
            AudioClip c;
            if (cache.TryGetValue(key, out c)) return c;
            int sr = 44100, n = (int)(sr * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)sr), -1f, 1f);
            c = AudioClip.Create(key, n, 1, sr, false);
            c.SetData(d, 0);
            cache[key] = c;
            return c;
        }

        public static AudioClip Shot(WeaponDef w)
        {
            float tone = Mathf.Clamp(w.range / 400f, 0.2f, 1f);
            float lp = 0f;
            return Make("shot_" + w.id, .22f, t =>
            {
                float noise = Random.value * 2f - 1f;
                lp += (noise - lp) * Mathf.Lerp(.9f, .15f, tone * .5f + (w.pellets > 1 ? .5f : 0f));
                return lp * Mathf.Exp(-t * 22f) * .8f + Mathf.Sin(t * 2f * Mathf.PI * (110f - t * 200f)) * Mathf.Exp(-t * 28f) * .5f;
            });
        }
        public static AudioClip Hit() { return Make("hit", .06f, t => Mathf.Sin(t * 2f * Mathf.PI * 1400f) * Mathf.Exp(-t * 60f) * .5f); }
        public static AudioClip Kill() { return Make("kill", .22f, t => (Mathf.Sin(t * 2f * Mathf.PI * (t < .08f ? 900f : 1300f))) * Mathf.Exp(-t * 14f) * .5f); }
        public static AudioClip Hurt() { return Make("hurt", .2f, t => Mathf.Sin(t * 2f * Mathf.PI * (170f - t * 300f)) * Mathf.Exp(-t * 12f) * .6f); }
        public static AudioClip Click() { return Make("click", .05f, t => Mathf.Sin(t * 2f * Mathf.PI * 600f) * Mathf.Exp(-t * 70f) * .4f); }
        public static AudioClip Reload() { return Make("reload", .7f, t => (t < .05f || (t > .55f && t < .6f)) ? (Random.value * 2f - 1f) * .4f : 0f); }
        public static AudioClip Pickup() { return Make("pickup", .2f, t => Mathf.Sin(t * 2f * Mathf.PI * (t < .08f ? 700f : 1000f)) * Mathf.Exp(-t * 14f) * .4f); }
        public static AudioClip Warn() { return Make("warn", .5f, t => Mathf.Sin(t * 2f * Mathf.PI * (220f + t * 220f)) * Mathf.Exp(-t * 4f) * .4f); }

        public static void Play(AudioClip c, float vol = 1f) { if (src != null && c != null) src.PlayOneShot(c, vol); }
        public static void At(AudioClip c, Vector3 p, float vol) { if (c != null) AudioSource.PlayClipAtPoint(c, p, vol); }
    }
}
