using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleZone
{
    /// <summary>Menus + HUD générés par code (style sombre et épuré façon Warzone / Fortnite).</summary>
    public class UIManager : MonoBehaviour
    {
        GameBootstrap gb; Canvas canvas; Font font;
        static readonly Color Acc = new Color(.78f, 1f, .18f), Dark = new Color(.04f, .05f, .07f, .9f), Dim = new Color(.6f, .65f, .72f), Clear = new Color(0, 0, 0, 0);
        readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>();
        string prev = "main";

        // HUD
        GameObject hud;
        Text tAlive, tKills, tZone, tAmmo, tRes, tWeapon, tPrompt, tBanner, tHp, tSh, tSlot0, tSlot1, mainStats, endTitle, endSub, arrow;
        RectTransform hpFill, shFill, killfeed, endStats, zoneRing, nextRing, mapRoot;
        Image vignette, hitV, hitH, dmgDir, slot0Bg, slot1Bg; RectTransform dmgDirRoot, hitRoot;
        GameObject scopeGo;
        float bannerT, vignT, hitT, dirT; bool hitKill;
        GameMode hudMode;
        Text lblSens, lblFov, lblVol, lblBots, btnDiffTxt, btnInvTxt;
        RectTransform loPrimary, loSecondary; Text loDetail;

        // ---------- helpers ----------
        RectTransform NewRT(string n, Transform p) { var go = new GameObject(n, typeof(RectTransform)); go.transform.SetParent(p, false); return go.GetComponent<RectTransform>(); }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; }
        Image Img(Transform p, string n, Color c) { var r = NewRT(n, p); var i = r.gameObject.AddComponent<Image>(); i.color = c; i.raycastTarget = false; return i; }
        Text Txt(Transform p, string s, int size, TextAnchor al, Color c, FontStyle st = FontStyle.Bold)
        {
            var r = NewRT("Text", p); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.alignment = al; t.color = c; t.fontStyle = st; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
        }
        static void Le(Component c, float h) { var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; }

        RectTransform MakeScreen(string key, bool left, float width, string title)
        {
            var root = Img(canvas.transform, "S_" + key, left ? Clear : new Color(.02f, .03f, .05f, .82f)); Stretch(root.rectTransform); root.raycastTarget = true;
            var panel = Img(root.transform, "Panel", Dark); var pr = panel.rectTransform; panel.raycastTarget = true;
            if (left) { pr.anchorMin = new Vector2(0, 0); pr.anchorMax = new Vector2(0, 1); pr.pivot = new Vector2(0, .5f); pr.sizeDelta = new Vector2(width, 0); pr.anchoredPosition = Vector2.zero; }
            else { pr.anchorMin = pr.anchorMax = new Vector2(.5f, .5f); pr.pivot = new Vector2(.5f, .5f); pr.sizeDelta = new Vector2(width, 0); }
            var vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(52, 52, 44, 44); vl.spacing = 10; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            if (!left) panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var line = Img(panel.transform, "Accent", Acc); line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1); line.rectTransform.pivot = new Vector2(.5f, 1); line.rectTransform.sizeDelta = new Vector2(0, 5);
            if (title != null) { var t = Txt(panel.transform, title.ToUpper(), 46, TextAnchor.MiddleLeft, Color.white); Le(t, 70); }
            screens[key] = root.gameObject; root.gameObject.SetActive(false);
            return pr;
        }

        Button Btn(Transform p, string label, UnityAction act, bool big = false, bool small = false)
        {
            var img = Img(p, "Btn", Color.white); img.raycastTarget = true; Le(img, big ? 74 : small ? 46 : 58);
            var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = big ? Acc : new Color(1, 1, 1, .07f); cb.highlightedColor = big ? new Color(.92f, 1f, .55f) : new Color(.78f, 1f, .18f, .35f);
            cb.pressedColor = big ? Color.white : new Color(.78f, 1f, .18f, .65f); cb.selectedColor = cb.normalColor; cb.fadeDuration = .08f;
            b.colors = cb;
            if (act != null) b.onClick.AddListener(act); b.onClick.AddListener(() => Sfx.Play(Sfx.Click(), .5f));
            var t = Txt(img.transform, label.ToUpper(), big ? 34 : small ? 22 : 26, TextAnchor.MiddleLeft, big ? Color.black : Color.white); Stretch(t.rectTransform); t.rectTransform.offsetMin = new Vector2(26, 0);
            t.rectTransform.gameObject.name = "Label";
            return b;
        }

        Slider MakeSlider(Transform p, float min, float max, float val, bool whole, UnityAction<float> cb)
        {
            var root = NewRT("Slider", p); Le(root, 30);
            var s = root.gameObject.AddComponent<Slider>();
            var bg = Img(root, "BG", new Color(1, 1, 1, .15f)); bg.raycastTarget = true; bg.rectTransform.anchorMin = new Vector2(0, .35f); bg.rectTransform.anchorMax = new Vector2(1, .65f); bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var fa = NewRT("FillArea", root); fa.anchorMin = new Vector2(0, .35f); fa.anchorMax = new Vector2(1, .65f); fa.offsetMin = new Vector2(0, 0); fa.offsetMax = new Vector2(-12, 0);
            var fill = Img(fa, "Fill", Acc); fill.rectTransform.sizeDelta = new Vector2(10, 0);
            var ha = NewRT("HandleArea", root); Stretch(ha); ha.offsetMin = new Vector2(10, 0); ha.offsetMax = new Vector2(-10, 0);
            var handle = Img(ha, "Handle", Color.white); handle.raycastTarget = true; handle.rectTransform.sizeDelta = new Vector2(16, 0);
            handle.rectTransform.anchorMin = new Vector2(0, 0); handle.rectTransform.anchorMax = new Vector2(0, 1);
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole; s.value = val; s.onValueChanged.AddListener(cb);
            return s;
        }

        Text SliderRow(Transform p, string label, float min, float max, float val, bool whole, System.Func<float, string> fmt, System.Action<float> set)
        {
            var t = Txt(p, label.ToUpper() + "   " + fmt(val), 22, TextAnchor.MiddleLeft, Color.white); Le(t, 34);
            MakeSlider(p, min, max, val, whole, v => { set(v); t.text = label.ToUpper() + "   " + fmt(v); });
            return t;
        }

        // ---------- construction ----------
        public void Build(GameBootstrap g)
        {
            gb = g;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Rajdhani", "Segoe UI", "Arial" }, 32);
            var cg = new GameObject("Canvas"); cg.transform.SetParent(transform, false);
            canvas = cg.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cg.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;
            cg.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            BuildHud(); BuildMain(); BuildModes(); BuildLoadout(); BuildSettings(); BuildHelp(); BuildPause(); BuildEnd();
        }

        void BuildMain()
        {
            var p = MakeScreen("main", true, 620, null);
            var title = Txt(p, "BATTLE", 120, TextAnchor.LowerLeft, Color.white); Le(title, 120);
            var zone = Txt(p, "Z O N E", 76, TextAnchor.UpperLeft, Acc); Le(zone, 84);
            var tag = Txt(p, "SAISON 1  ·  BATAILLE TOTALE", 20, TextAnchor.MiddleLeft, Dim, FontStyle.Normal); Le(tag, 50);
            Btn(p, "Jouer", () => Show("modes"), true);
            Btn(p, "Équipement", () => { RefreshLoadout(); Show("loadout"); });
            Btn(p, "Options", () => { prev = "main"; Show("settings"); });
            Btn(p, "Comment jouer", () => { prev = "main"; Show("help"); });
            Btn(p, "Quitter", () => { Application.Quit(); });
            mainStats = Txt(p, "", 20, TextAnchor.LowerLeft, Dim, FontStyle.Normal); Le(mainStats, 80);
        }

        void BuildModes()
        {
            var p = MakeScreen("modes", false, 900, "Choisis ton mode");
            var d1 = Txt(p, "Largage en parachute sur l'île, loot, tempête qui se resserre. Une seule vie : le dernier survivant gagne.", 22, TextAnchor.UpperLeft, Dim, FontStyle.Normal); Le(d1, 70);
            Btn(p, "Battle Royale", () => gb.StartGame(GameMode.BattleRoyale), true);
            var d2 = Txt(p, "Combat sans pause avec réapparition. Premier à 20 éliminations ou meilleur score après 5 minutes.", 22, TextAnchor.UpperLeft, Dim, FontStyle.Normal); Le(d2, 70);
            Btn(p, "Mêlée générale", () => gb.StartGame(GameMode.FreeForAll), true);
            Btn(p, "Retour", () => Show("main"), false, true);
        }

        string[] LoPrimary = { "ar", "smg", "shotgun", "sniper", "lmg" }, LoSecondary = { "pistol", "smg", "shotgun", "ar" };
        void BuildLoadout()
        {
            var p = MakeScreen("loadout", false, 1100, "Équipement");
            var h1 = Txt(p, "ARME PRINCIPALE", 20, TextAnchor.MiddleLeft, Dim); Le(h1, 30);
            loPrimary = NewRT("Primary", p); var hl = loPrimary.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = true; Le(loPrimary, 64);
            var h2 = Txt(p, "ARME SECONDAIRE", 20, TextAnchor.MiddleLeft, Dim); Le(h2, 30);
            loSecondary = NewRT("Secondary", p); var hl2 = loSecondary.gameObject.AddComponent<HorizontalLayoutGroup>(); hl2.spacing = 8; hl2.childControlWidth = hl2.childControlHeight = true; hl2.childForceExpandWidth = true; Le(loSecondary, 64);
            loDetail = Txt(p, "", 22, TextAnchor.UpperLeft, Color.white, FontStyle.Normal); Le(loDetail, 120);
            Btn(p, "Retour", () => Show("main"), false, true);
        }

        void RefreshLoadout()
        {
            Fill(loPrimary, LoPrimary, true); Fill(loSecondary, LoSecondary, false);
            Detail(Weapons.Get(Cfg.Primary));
        }
        void Fill(RectTransform row, string[] ids, bool primary)
        {
            foreach (Transform c in row) Destroy(c.gameObject);
            foreach (var id in ids)
            {
                var w = Weapons.Get(id); string cur = primary ? Cfg.Primary : Cfg.Secondary; var wid = id;
                var b = Btn(row, w.name, () => { if (primary) Cfg.Primary = wid; else Cfg.Secondary = wid; RefreshLoadout(); }, false, true);
                var img = b.GetComponent<Image>(); var cb = b.colors;
                if (cur == id) { cb.normalColor = new Color(.78f, 1f, .18f, .5f); b.colors = cb; }
                var lbl = b.transform.Find("Label").GetComponent<Text>(); lbl.fontSize = 18; lbl.alignment = TextAnchor.MiddleCenter; lbl.rectTransform.offsetMin = Vector2.zero;
                var ev = b.gameObject.AddComponent<EventTrigger>(); var en = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter }; en.callback.AddListener(_ => Detail(w)); ev.triggers.Add(en);
            }
        }
        void Detail(WeaponDef w)
        {
            loDetail.text = "<b>" + w.name.ToUpper() + "</b>  —  " + (w.auto ? "automatique" : "semi-auto") + "\nDégâts " + w.dmg + (w.pellets > 1 ? " x" + w.pellets : "") + "   ·   Cadence " + w.rpm + "/min   ·   Chargeur " + w.mag + "   ·   Rechargement " + w.reload + "s   ·   Portée " + w.range + " m";
            loDetail.supportRichText = true;
        }

        void BuildSettings()
        {
            var p = MakeScreen("settings", false, 820, "Options");
            lblSens = SliderRow(p, "Sensibilité souris", 1, 15, Cfg.Sens, false, v => v.ToString("0.0"), v => Cfg.Sens = v);
            lblFov = SliderRow(p, "Champ de vision", 60, 110, Cfg.Fov, true, v => v.ToString("0") + "°", v => Cfg.Fov = v);
            lblVol = SliderRow(p, "Volume", 0, 1, Cfg.Volume, false, v => Mathf.RoundToInt(v * 100) + "%", v => Cfg.Volume = v);
            lblBots = SliderRow(p, "Nombre de bots", 5, 40, Cfg.Bots, true, v => v.ToString("0"), v => Cfg.Bots = (int)v);
            string[] dn = { "Facile", "Normal", "Difficile", "Cauchemar" };
            var bd = Btn(p, "", null, false, true); btnDiffTxt = bd.transform.Find("Label").GetComponent<Text>(); btnDiffTxt.text = ("Difficulté : " + dn[Cfg.Diff]).ToUpper();
            bd.onClick.AddListener(() => { Cfg.Diff = (Cfg.Diff + 1) % 4; btnDiffTxt.text = ("Difficulté : " + dn[Cfg.Diff]).ToUpper(); });
            var bi = Btn(p, "", null, false, true); btnInvTxt = bi.transform.Find("Label").GetComponent<Text>(); btnInvTxt.text = ("Inverser l'axe Y : " + (Cfg.InvertY ? "oui" : "non")).ToUpper();
            bi.onClick.AddListener(() => { Cfg.InvertY = !Cfg.InvertY; btnInvTxt.text = ("Inverser l'axe Y : " + (Cfg.InvertY ? "oui" : "non")).ToUpper(); });
            Btn(p, "Retour", () => Show(prev), false, true);
        }

        void BuildHelp()
        {
            var p = MakeScreen("help", false, 1000, "Comment jouer");
            var t = Txt(p, "<b>Z Q S D</b> (ou W A S D) — se déplacer\n<b>Maj</b> — sprint        <b>Espace</b> — sauter / ouvrir le parachute\n<b>C</b> ou <b>Ctrl</b> — s'accroupir ; en sprint : <b>glissade</b>\n<b>Clic gauche</b> — tirer        <b>Clic droit</b> — viser\n<b>R</b> — recharger        <b>1 / 2</b> ou molette — changer d'arme\n<b>E</b> — ramasser une arme        <b>Échap</b> — pause\n\nEn Battle Royale, reste dans le cercle : la tempête violette se resserre.\nRamasse les soins (rouge), boucliers (bleu) et munitions (jaune). Les tirs à la tête font plus de dégâts.", 24, TextAnchor.UpperLeft, Color.white, FontStyle.Normal);
            t.supportRichText = true; Le(t, 330);
            Btn(p, "Retour", () => Show(prev), false, true);
        }

        void BuildPause()
        {
            var p = MakeScreen("pause", false, 600, "Pause");
            Btn(p, "Reprendre", () => gb.Resume(), true);
            Btn(p, "Options", () => { prev = "pause"; Show("settings"); });
            Btn(p, "Quitter la partie", () => gb.QuitToMenu());
        }

        void BuildEnd()
        {
            var p = MakeScreen("end", false, 900, null);
            endTitle = Txt(p, "VICTOIRE", 90, TextAnchor.MiddleLeft, Acc); Le(endTitle, 110);
            endSub = Txt(p, "", 26, TextAnchor.MiddleLeft, Dim, FontStyle.Normal); Le(endSub, 44);
            endStats = NewRT("Stats", p); var hl = endStats.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 12; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = true; Le(endStats, 120);
            Btn(p, "Rejouer", () => gb.StartGame(gb.Mode), true);
            Btn(p, "Menu principal", () => gb.QuitToMenu());
        }

        // ---------- HUD ----------
        void BuildHud()
        {
            var h = NewRT("HUD", canvas.transform); Stretch(h); hud = h.gameObject;
            vignette = Img(h, "Vignette", new Color(1, 0, 0, 0)); Stretch(vignette.rectTransform);
            // réticule
            var cr = NewRT("Cross", h); Place(cr, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            foreach (var o in new[] { new Vector2(0, 14), new Vector2(0, -14), new Vector2(14, 0), new Vector2(-14, 0) })
            { var i = Img(cr, "c", Color.white); Place(i.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), o, o.x == 0 ? new Vector2(3, 12) : new Vector2(12, 3)); }
            // hitmarker
            hitRoot = NewRT("Hit", h); Place(hitRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 40)); hitRoot.localRotation = Quaternion.Euler(0, 0, 45);
            hitV = Img(hitRoot, "v", Color.white); Place(hitV.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(4, 40));
            hitH = Img(hitRoot, "h", Color.white); Place(hitH.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 4));
            hitRoot.gameObject.SetActive(false);
            // direction des dégâts
            dmgDirRoot = NewRT("DmgDir", h); Place(dmgDirRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            dmgDir = Img(dmgDirRoot, "d", new Color(1, 0, 0, 0)); Place(dmgDir.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 260), new Vector2(160, 30));
            // lunette
            var sg = NewRT("Scope", h); Stretch(sg); scopeGo = sg.gameObject;
            var holeTex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var px = new Color[512 * 512];
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(256, 256)) / 256f; px[y * 512 + x] = new Color(0, 0, 0, d < .72f ? 0 : 1); }
            holeTex.SetPixels(px); holeTex.Apply();
            var ring = NewRT("Ring", sg); Place(ring, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1080, 1080)); var ri = ring.gameObject.AddComponent<RawImage>(); ri.texture = holeTex; ri.raycastTarget = false;
            var l = Img(sg, "L", Color.black); l.rectTransform.anchorMin = new Vector2(0, 0); l.rectTransform.anchorMax = new Vector2(.5f, 1); l.rectTransform.offsetMin = Vector2.zero; l.rectTransform.offsetMax = new Vector2(-539, 0);
            var r = Img(sg, "R", Color.black); r.rectTransform.anchorMin = new Vector2(.5f, 0); r.rectTransform.anchorMax = new Vector2(1, 1); r.rectTransform.offsetMin = new Vector2(539, 0); r.rectTransform.offsetMax = Vector2.zero;
            var cl1 = Img(sg, "h", Color.black); Place(cl1.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1080, 2));
            var cl2 = Img(sg, "v", Color.black); Place(cl2.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(2, 1080));
            scopeGo.SetActive(false);

            // infos du haut
            var top = NewRT("Top", h); Place(top, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(700, 50));
            var hl = top.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = false;
            tAlive = Pill(top, 150); tKills = Pill(top, 150); tZone = Pill(top, 300);
            // minimap
            mapRoot = NewRT("Map", h); Place(mapRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(230, 230));
            var mbg = mapRoot.gameObject.AddComponent<Image>(); mbg.color = new Color(0, 0, 0, .6f); mapRoot.gameObject.AddComponent<RectMask2D>();
            var mimg = NewRT("Tex", mapRoot); Place(mimg, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(230, 230)); mimg.gameObject.AddComponent<RawImage>().raycastTarget = false;
            mimg.name = "MapTex";
            var ringTex = U.CircleTex(256, true);
            zoneRing = NewRT("Zone", mapRoot); var zr = zoneRing.gameObject.AddComponent<RawImage>(); zr.texture = ringTex; zr.color = new Color(.65f, .3f, 1f, 1f); zr.raycastTarget = false;
            nextRing = NewRT("Next", mapRoot); var nr = nextRing.gameObject.AddComponent<RawImage>(); nr.texture = ringTex; nr.color = Color.white; nr.raycastTarget = false;
            arrow = Txt(mapRoot, ">", 34, TextAnchor.MiddleCenter, Color.white); Place(arrow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 40));
            // killfeed
            killfeed = NewRT("Feed", h); Place(killfeed, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -270), new Vector2(420, 220));
            var kv = killfeed.gameObject.AddComponent<VerticalLayoutGroup>(); kv.childAlignment = TextAnchor.UpperRight; kv.spacing = 4; kv.childControlWidth = kv.childControlHeight = true; kv.childForceExpandHeight = false;
            // bannière / prompt
            tBanner = Txt(h, "", 56, TextAnchor.MiddleCenter, Color.white); Place(tBanner.rectTransform, new Vector2(.5f, .78f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1400, 90)); AddShadow(tBanner);
            tPrompt = Txt(h, "", 28, TextAnchor.MiddleCenter, Color.white); Place(tPrompt.rectTransform, new Vector2(.5f, .35f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000, 50)); AddShadow(tPrompt);
            // bas gauche : barres
            var bars = NewRT("Bars", h); Place(bars, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(400, 70));
            shFill = Bar(bars, 40, new Color(.45f, .78f, 1f), out tSh); hpFill = Bar(bars, 0, new Color(.55f, .95f, .4f), out tHp);
            // bas centre : armes
            var ws = NewRT("Slots", h); Place(ws, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 30), new Vector2(480, 52));
            var wl = ws.gameObject.AddComponent<HorizontalLayoutGroup>(); wl.spacing = 10; wl.childControlWidth = wl.childControlHeight = true; wl.childForceExpandWidth = true;
            slot0Bg = Img(ws, "S0", new Color(0, 0, 0, .6f)); tSlot0 = Txt(slot0Bg.transform, "", 20, TextAnchor.MiddleCenter, Color.white); Stretch(tSlot0.rectTransform);
            slot1Bg = Img(ws, "S1", new Color(0, 0, 0, .6f)); tSlot1 = Txt(slot1Bg.transform, "", 20, TextAnchor.MiddleCenter, Color.white); Stretch(tSlot1.rectTransform);
            // bas droite : munitions
            tAmmo = Txt(h, "30", 90, TextAnchor.LowerRight, Color.white); Place(tAmmo.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-190, 24), new Vector2(240, 100)); AddShadow(tAmmo);
            tRes = Txt(h, "/ 120", 34, TextAnchor.LowerLeft, Dim); Place(tRes.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(160, 50));
            tWeapon = Txt(h, "", 24, TextAnchor.LowerRight, Acc); Place(tWeapon.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 120), new Vector2(400, 36));
            hud.SetActive(false);
            // texture de minimap appliquée au premier affichage
            mapImg = mimg.GetComponent<RawImage>();
        }
        RawImage mapImg;

        Text Pill(Transform p, float w)
        {
            var bg = Img(p, "Pill", new Color(.03f, .04f, .06f, .75f)); bg.gameObject.AddComponent<LayoutElement>().preferredWidth = w;
            var line = Img(bg.transform, "l", Acc); line.rectTransform.anchorMin = Vector2.zero; line.rectTransform.anchorMax = new Vector2(1, 0); line.rectTransform.sizeDelta = new Vector2(0, 3);
            var t = Txt(bg.transform, "", 26, TextAnchor.MiddleCenter, Color.white); Stretch(t.rectTransform); return t;
        }
        RectTransform Bar(Transform p, float y, Color c, out Text label)
        {
            var bg = Img(p, "Bar", new Color(0, 0, 0, .6f)); Place(bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, y), new Vector2(400, 28));
            var fill = Img(bg.transform, "Fill", c); fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.pivot = new Vector2(0, .5f); fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            label = Txt(bg.transform, "", 20, TextAnchor.MiddleRight, Color.white); Stretch(label.rectTransform); label.rectTransform.offsetMax = new Vector2(-10, 0);
            return fill.rectTransform;
        }
        static void AddShadow(Text t) { var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, .8f); s.effectDistance = new Vector2(2, -2); }

        // ---------- navigation ----------
        public void Show(string key)
        {
            foreach (var s in screens.Values) s.SetActive(false);
            hud.SetActive(false);
            if (screens.ContainsKey(key)) screens[key].SetActive(true);
            if (key == "main") mainStats.text = "PARTIES " + Cfg.Games + "   ·   VICTOIRES " + Cfg.Wins + "   ·   ÉLIMINATIONS " + Cfg.TotalKills + "   ·   RECORD " + Cfg.Best;
            if (key == "main" || key == "modes" || key == "loadout") gb.Cam.fieldOfView = 65f;
        }

        public void ShowHud(GameMode mode)
        {
            foreach (var s in screens.Values) s.SetActive(false);
            hud.SetActive(true); hudMode = mode;
            if (mapImg.texture == null) mapImg.texture = gb.World.minimapTex;
            zoneRing.gameObject.SetActive(mode == GameMode.BattleRoyale); nextRing.gameObject.SetActive(mode == GameMode.BattleRoyale);
        }

        public void ShowEnd(bool win, string sub, int rank, int kills, float time)
        {
            hud.SetActive(false); Show("end");
            endTitle.text = win ? "VICTOIRE" : "ÉLIMINÉ"; endTitle.color = win ? Acc : new Color(1f, .42f, .42f); endSub.text = sub.ToUpper();
            foreach (Transform c in endStats) Destroy(c.gameObject);
            Stat("#" + rank, "Classement"); Stat(kills.ToString(), "Éliminations"); Stat(Mathf.FloorToInt(time / 60f) + ":" + (Mathf.FloorToInt(time) % 60).ToString("00"), "Temps");
        }
        void Stat(string v, string l)
        {
            var bg = Img(endStats, "Stat", new Color(1, 1, 1, .07f));
            var a = Txt(bg.transform, v, 46, TextAnchor.UpperCenter, Color.white); a.rectTransform.anchorMin = new Vector2(0, .3f); a.rectTransform.anchorMax = Vector2.one; a.rectTransform.offsetMin = a.rectTransform.offsetMax = Vector2.zero;
            var b = Txt(bg.transform, l.ToUpper(), 18, TextAnchor.LowerCenter, Dim); b.rectTransform.anchorMin = Vector2.zero; b.rectTransform.anchorMax = new Vector2(1, .4f); b.rectTransform.offsetMin = b.rectTransform.offsetMax = Vector2.zero;
        }

        // ---------- retours visuels ----------
        public void Banner(string s, float sec) { tBanner.text = s; tBanner.fontSize = 56; bannerT = sec; }
        public void Toast(string s) { tBanner.text = s; tBanner.fontSize = 32; bannerT = 1.2f; }
        public void Prompt(string s) { if (string.IsNullOrEmpty(s)) { if (tPrompt.text != "") tPrompt.text = ""; } else if (tPrompt.text != s) tPrompt.text = s; }
        public void SetScope(bool on) { if (scopeGo.activeSelf != on) scopeGo.SetActive(on); }
        public void Hitmarker(bool kill)
        {
            hitT = .25f; hitKill = kill; hitRoot.gameObject.SetActive(true);
            hitV.color = hitH.color = kill ? new Color(1, .25f, .25f) : Color.white;
        }
        public void PlayerHurt(float amt, Health src)
        {
            if (amt < .5f) return;
            vignT = .35f; vignette.color = new Color(1, 0, 0, Mathf.Clamp(.35f + amt / 80f, .3f, .85f));
            Sfx.Play(Sfx.Hurt(), .7f);
            if (src != null && gb.Player != null)
            {
                Vector3 to = src.transform.position - gb.Player.transform.position; to.y = 0;
                float ang = Vector3.SignedAngle(gb.Player.transform.forward, to, Vector3.up);
                dmgDirRoot.localRotation = Quaternion.Euler(0, 0, -ang); dirT = .6f; dmgDir.color = new Color(1, 0, 0, .9f);
            }
        }
        public void Feed(string attacker, string victim, bool head, bool mine)
        {
            if (killfeed == null) return;
            var bg = Img(killfeed, "K", new Color(0, 0, 0, .6f)); bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;
            var t = Txt(bg.transform, (attacker != null ? attacker : "Tempête") + "  >  " + victim + (head ? "  (tête)" : ""), 20, TextAnchor.MiddleRight, mine ? Acc : Color.white); Stretch(t.rectTransform); t.rectTransform.offsetMax = new Vector2(-10, 0);
            Destroy(bg.gameObject, 5f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (bannerT > 0) { bannerT -= dt; var c = tBanner.color; c.a = Mathf.Clamp01(bannerT * 3f); tBanner.color = c; } else if (tBanner.color.a > 0) { var c = tBanner.color; c.a = 0; tBanner.color = c; }
            if (vignT > 0) { vignT -= dt; var c = vignette.color; c.a *= Mathf.Clamp01(vignT / .35f); vignette.color = c; } else if (vignette.color.a > 0) vignette.color = new Color(1, 0, 0, 0);
            if (dirT > 0) { dirT -= dt; dmgDir.color = new Color(1, 0, 0, Mathf.Clamp01(dirT / .6f)); }
            if (hitT > 0) { hitT -= dt; hitRoot.localScale = Vector3.one * (1f + hitT * 2f); if (hitT <= 0) hitRoot.gameObject.SetActive(false); }
        }

        public void UpdateHud(GameBootstrap g)
        {
            var pc = g.Player; var h = pc.health; var ws = pc.weapons; var slot = ws.slots[ws.cur];
            tAlive.text = "JOUEURS  " + (g.Mode == GameMode.BattleRoyale ? g.AliveCount() : g.Bots.Count + 1);
            tKills.text = "ÉLIM.  " + h.kills;
            if (g.Mode == GameMode.BattleRoyale && g.Zone != null) tZone.text = g.Zone.TimerText().ToUpper();
            else { int rem = Mathf.Max(0, Mathf.CeilToInt(300f - g.MatchTime)); int top = h.kills; foreach (var b in g.Bots) if (b != null) top = Mathf.Max(top, b.health.kills); tZone.text = rem / 60 + ":" + (rem % 60).ToString("00") + "   ·   " + top + "/20"; }
            hpFill.localScale = new Vector3(Mathf.Clamp01(h.hp / 100f), 1, 1); tHp.text = Mathf.CeilToInt(h.hp).ToString();
            shFill.localScale = new Vector3(Mathf.Clamp01(h.shield / 100f), 1, 1); tSh.text = Mathf.CeilToInt(h.shield).ToString();
            tAmmo.text = ws.reloadT > 0 ? "..." : slot.ammo.ToString(); tRes.text = "/ " + slot.reserve; tWeapon.text = slot.def.name.ToUpper();
            tSlot0.text = "1  " + ws.slots[0].def.name.ToUpper(); tSlot1.text = "2  " + ws.slots[1].def.name.ToUpper();
            slot0Bg.color = ws.cur == 0 ? new Color(.78f, 1f, .18f, .35f) : new Color(0, 0, 0, .6f); slot1Bg.color = ws.cur == 1 ? new Color(.78f, 1f, .18f, .35f) : new Color(0, 0, 0, .6f);

            // minimap
            float s = 230f / 320f; Vector3 p = pc.transform.position;
            arrow.rectTransform.anchoredPosition = new Vector2(p.x * s, p.z * s); arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 90f - pc.yaw);
            if (g.Zone != null)
            {
                zoneRing.anchoredPosition = g.Zone.Center * s; zoneRing.sizeDelta = Vector2.one * g.Zone.Radius * 2f * s;
                nextRing.anchoredPosition = g.Zone.NextCenter * s; nextRing.sizeDelta = Vector2.one * g.Zone.NextRadius * 2f * s; nextRing.gameObject.SetActive(!g.Zone.Shrinking);
            }
        }
    }
}
