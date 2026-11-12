/* ============================================================
   BATTLE ZONE — FPS de bataille en 3D (Three.js), style cartoon
   Tout est généré par code : aucune texture ni son externe.
   ============================================================ */
(() => {
'use strict';
const $ = id => document.getElementById(id);
const rnd = (a, b) => a + Math.random() * (b - a);
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const lerp = (a, b, t) => a + (b - a) * t;
const pick = a => a[Math.floor(Math.random() * a.length)];
function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
const angLerp = (a, b, t) => { let d = ((b - a + Math.PI) % (Math.PI * 2) + Math.PI * 2) % (Math.PI * 2) - Math.PI; return a + d * t; };
const store = {
  get(k, d) { try { return Object.assign({}, d, JSON.parse(localStorage.getItem(k) || '{}')); } catch (e) { return Object.assign({}, d); } },
  set(k, v) { try { localStorage.setItem(k, JSON.stringify(v)); } catch (e) {} }
};

/* ---------------- Réglages & stats ---------------- */
const DEF = { sens: 5, fov: 85, vol: 50, bots: 20, diff: 1, invy: false, fps: false, primary: 'ar', secondary: 'pistol' };
const S = store.get('bz_settings', DEF);
const ST = store.get('bz_stats', { games: 0, wins: 0, kills: 0, best: 0 });
const saveS = () => store.set('bz_settings', S);

/* ---------------- Armes ---------------- */
const WEAPONS = {
  ar:      { name: "Fusil d'assaut", e: '🔫', dmg: 26, rpm: 600, mag: 30, res: 120, reload: 2.1, spread: .022, ads: .002, recoil: .010, bloom: .004, auto: true,  pellets: 1,  range: 170, zoom: 1.35, snd: 2800, color: 0x3a3f47, len: .62, st: [60, 65, 75] },
  smg:     { name: 'Pistolet-mitrailleur', e: '🔫', dmg: 17, rpm: 900, mag: 32, res: 128, reload: 1.6, spread: .034, ads: .008, recoil: .008, bloom: .004, auto: true,  pellets: 1,  range: 75,  zoom: 1.2,  snd: 3600, color: 0x30343a, len: .45, st: [40, 95, 40] },
  shotgun: { name: 'Fusil à pompe', e: '💥', dmg: 12, rpm: 75,  mag: 6,  res: 30,  reload: 2.8, spread: .07,  ads: .045, recoil: .05,  bloom: .0,   auto: false, pellets: 10, range: 32,  zoom: 1.15, snd: 1400, color: 0x5a4632, len: .6,  st: [95, 20, 25] },
  sniper:  { name: 'Sniper', e: '🎯', dmg: 105, rpm: 48, mag: 5, res: 20, reload: 3.2, spread: .05, ads: .0004, recoil: .06, bloom: 0, auto: false, pellets: 1, range: 400, zoom: 4, snd: 900, color: 0x3f4a3a, len: .85, scope: true, st: [100, 10, 100] },
  lmg:     { name: 'Mitrailleuse', e: '⛓', dmg: 22, rpm: 560, mag: 75, res: 150, reload: 4.6, spread: .03, ads: .006, recoil: .008, bloom: .003, auto: true, pellets: 1, range: 140, zoom: 1.25, snd: 2200, color: 0x45494f, len: .72, st: [55, 60, 70] },
  pistol:  { name: 'Pistolet', e: '🔹', dmg: 30, rpm: 380, mag: 12, res: 60, reload: 1.3, spread: .016, ads: .003, recoil: .02, bloom: .006, auto: false, pellets: 1, range: 90, zoom: 1.15, snd: 3200, color: 0x2b2d31, len: .3, st: [40, 45, 45] }
};
const WIDS = Object.keys(WEAPONS);

/* ---------------- Audio procédural ---------------- */
let AC = null, master = null, noiseBuf = null;
function audioInit() {
  if (!AC) {
    AC = new (window.AudioContext || window.webkitAudioContext)();
    master = AC.createGain(); master.connect(AC.destination);
    noiseBuf = AC.createBuffer(1, AC.sampleRate, AC.sampleRate);
    const d = noiseBuf.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
  }
  master.gain.value = S.vol / 100;
  if (AC.state === 'suspended') AC.resume();
}
function tone(f, d, type = 'sine', v = .2, f2) {
  if (!AC) return; const t = AC.currentTime, o = AC.createOscillator(), g = AC.createGain();
  o.type = type; o.frequency.setValueAtTime(f, t); if (f2) o.frequency.exponentialRampToValueAtTime(f2, t + d);
  g.gain.setValueAtTime(v, t); g.gain.exponentialRampToValueAtTime(.001, t + d);
  o.connect(g); g.connect(master); o.start(t); o.stop(t + d);
}
function noise(d, v, fq) {
  if (!AC) return; const t = AC.currentTime, s = AC.createBufferSource(), f = AC.createBiquadFilter(), g = AC.createGain();
  s.buffer = noiseBuf; f.type = 'lowpass'; f.frequency.setValueAtTime(fq, t); f.frequency.exponentialRampToValueAtTime(200, t + d);
  g.gain.setValueAtTime(v, t); g.gain.exponentialRampToValueAtTime(.001, t + d);
  s.connect(f); f.connect(g); g.connect(master); s.start(t); s.stop(t + d);
}
const sfx = {
  shot(w, vol = 1) { noise(.16, .55 * vol, w.snd); tone(130, .12, 'square', .2 * vol, 40); },
  hit() { tone(1400, .05, 'square', .12); },
  kill() { tone(900, .08, 'square', .15); setTimeout(() => tone(1300, .12, 'square', .15), 70); },
  hurt() { tone(160, .18, 'sawtooth', .22, 70); },
  reload() { tone(500, .05, 'square', .1); setTimeout(() => tone(380, .06, 'square', .1), 600); },
  empty() { tone(300, .04, 'square', .08); },
  pickup() { tone(700, .08, 'triangle', .2); setTimeout(() => tone(1000, .12, 'triangle', .2), 70); },
  zone() { tone(220, .5, 'sawtooth', .15, 330); },
  click() { tone(600, .05, 'triangle', .12); }
};

/* ---------------- Rendu ---------------- */
const canvas = $('game');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.autoClear = false;
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.05;
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(S.fov, innerWidth / innerHeight, .05, 1200);
camera.rotation.order = 'YXZ'; scene.add(camera);
const vmScene = new THREE.Scene(), vmCam = new THREE.PerspectiveCamera(60, innerWidth / innerHeight, .02, 10);
vmScene.add(new THREE.AmbientLight(0xffffff, .9)); { const l = new THREE.DirectionalLight(0xffffff, 1.6); l.position.set(1, 2, 1); vmScene.add(l); }
function resize() { renderer.setSize(innerWidth, innerHeight); camera.aspect = vmCam.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); vmCam.updateProjectionMatrix(); }
addEventListener('resize', resize); resize();

const SKY = 0xb4cfe4;
scene.background = new THREE.Color(SKY);
const skyDome = new THREE.Mesh(new THREE.SphereGeometry(650, 32, 16), new THREE.ShaderMaterial({ side: THREE.BackSide, depthWrite: false, fog: false,
  uniforms: { sunDir: { value: new THREE.Vector3(.5, .55, .35).normalize() } },
  vertexShader: 'varying vec3 vP; void main(){ vP = position; gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.); }',
  fragmentShader: `varying vec3 vP; uniform vec3 sunDir; void main(){ vec3 d = normalize(vP); float h = clamp(d.y,0.,1.);
    vec3 top = vec3(.16,.38,.72), mid = vec3(.46,.68,.9), hor = vec3(.82,.88,.92);
    vec3 c = mix(hor, mid, smoothstep(0.,.25,h)); c = mix(c, top, smoothstep(.2,.9,h));
    float s = max(dot(d, sunDir), 0.); c += vec3(1.,.8,.5) * (pow(s,24.)*.35 + pow(s,600.)*3.);
    if (d.y < 0.) c = hor * .85; gl_FragColor = vec4(pow(c, vec3(1.)), 1.); }` }));
scene.add(skyDome);
scene.fog = new THREE.FogExp2(SKY, .0042);
scene.add(new THREE.HemisphereLight(0xcfe3ff, 0x4f6a3a, .75));
const sun = new THREE.DirectionalLight(0xfff0d0, 1.7);
sun.position.set(60, 100, 40); sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, { left: -45, right: 45, top: 45, bottom: -45, near: 10, far: 260 });
sun.shadow.bias = -.0006; scene.add(sun); scene.add(sun.target);

/* ---------------- Textures cartoon (canvas) ---------------- */
function ctex(size, fn, rx = 1, ry = 1) {
  const c = document.createElement('canvas'); c.width = c.height = size;
  const g = c.getContext('2d'); fn(g, size);
  const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(rx, ry);
  t.anisotropy = 8; return t;
}
const TEX = {
  grass: ctex(512, (g, s) => {
    g.fillStyle = '#4c6f2f'; g.fillRect(0, 0, s, s);
    for (let i = 0; i < 90; i++) { g.fillStyle = pick(['#56793a', '#44662a', '#5f7f3b', '#3d5d27', '#6a6f3a']); g.globalAlpha = .55; g.beginPath(); g.ellipse(Math.random() * s, Math.random() * s, rnd(14, 46), rnd(8, 26), Math.random() * 3, 0, 7); g.fill(); }
    g.globalAlpha = 1; g.lineWidth = 1.4;
    for (let i = 0; i < 2200; i++) { const x = Math.random() * s, y = Math.random() * s; g.strokeStyle = pick(['#6f9441', '#3a5a24', '#7ea348', '#2f4d1d', '#8a9a4a']); g.beginPath(); g.moveTo(x, y); g.lineTo(x + rnd(-3, 3), y - rnd(4, 10)); g.stroke(); }
  }, 90, 90),
  brick: ctex(128, (g, s) => {
    g.fillStyle = '#f3f3f6'; g.fillRect(0, 0, s, s); g.strokeStyle = '#b9bccb'; g.lineWidth = 4;
    for (let r = 0; r < 4; r++) { const y = r * 32; g.strokeRect(-2, y, s + 4, 32); const o = r % 2 ? 32 : 0; for (let x = o; x < s; x += 64) { g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + 32); g.stroke(); } }
    g.fillStyle = 'rgba(255,255,255,.5)'; for (let i = 0; i < 12; i++) g.fillRect(Math.random() * s, Math.random() * s, 18, 4);
  }),
  roof: ctex(128, (g, s) => {
    g.fillStyle = '#f6f6f6'; g.fillRect(0, 0, s, s); g.strokeStyle = '#9a9db5'; g.lineWidth = 4;
    for (let y = 0; y < s; y += 32) { g.beginPath(); g.moveTo(0, y); g.lineTo(s, y); g.stroke(); for (let x = (y / 32 % 2) * 32; x < s; x += 64) { g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + 32); g.stroke(); } }
  }),
  wood: ctex(128, (g, s) => {
    g.fillStyle = '#f2e0c4'; g.fillRect(0, 0, s, s); g.strokeStyle = '#a07a4a'; g.lineWidth = 4;
    for (let x = 0; x < s; x += 32) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x, s); g.stroke(); }
    g.lineWidth = 1.5; g.strokeStyle = '#c9a779'; for (let i = 0; i < 20; i++) { const x = Math.random() * s, y = Math.random() * s; g.beginPath(); g.moveTo(x, y); g.lineTo(x + 2, y + 14); g.stroke(); }
  }),
  crate: ctex(128, (g, s) => {
    g.fillStyle = '#d99a52'; g.fillRect(0, 0, s, s); g.strokeStyle = '#7a4a1c'; g.lineWidth = 10; g.strokeRect(5, 5, s - 10, s - 10);
    g.lineWidth = 8; g.beginPath(); g.moveTo(8, 8); g.lineTo(s - 8, s - 8); g.moveTo(s - 8, 8); g.lineTo(8, s - 8); g.stroke();
    g.fillStyle = '#7a4a1c'; for (const [x, y] of [[14, 14], [s - 14, 14], [14, s - 14], [s - 14, s - 14]]) { g.beginPath(); g.arc(x, y, 4, 0, 7); g.fill(); }
  }),
  metal: ctex(128, (g, s) => {
    g.fillStyle = '#f2f2f2'; g.fillRect(0, 0, s, s); g.strokeStyle = '#b5b8c7'; g.lineWidth = 6;
    for (let x = 0; x < s; x += 16) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x, s); g.stroke(); }
  }),
  concrete: ctex(128, (g, s) => {
    g.fillStyle = '#e6e8ee'; g.fillRect(0, 0, s, s);
    for (let i = 0; i < 200; i++) { g.fillStyle = Math.random() < .5 ? '#d3d6df' : '#f4f5f8'; g.fillRect(Math.random() * s, Math.random() * s, 3, 3); }
    g.strokeStyle = '#b0b4c2'; g.lineWidth = 3; g.strokeRect(0, 0, s, s);
  })
};
const matCache = {};
function mat(tex, color) { const k = tex + color; return matCache[k] || (matCache[k] = new THREE.MeshStandardMaterial({ map: tex ? TEX[tex] : null, color, roughness: tex === 'metal' ? .45 : .9, metalness: tex === 'metal' ? .5 : 0 })); }
const basic = (c, o = {}) => new THREE.MeshBasicMaterial(Object.assign({ color: c }, o));
const lam = (c, o = {}) => new THREE.MeshStandardMaterial(Object.assign({ color: c, roughness: .88, metalness: 0 }, o));

/* ---------------- Monde ---------------- */
const MAP = 150;               // demi-taille de l'île
const boxes = [];              // colliders AABB
const lootSpots = [];
const rects = [];              // POI (minimap)
let worldBuilt = false;

function boxGeo(w, h, d, s = 3) {
  const g = new THREE.BoxGeometry(w, h, d), uv = g.attributes.uv, dims = [[d, h], [d, h], [w, d], [w, d], [w, h], [w, h]];
  for (let f = 0; f < 6; f++) for (let i = 0; i < 4; i++) { const k = f * 4 + i; uv.setXY(k, uv.getX(k) * dims[f][0] / s, uv.getY(k) * dims[f][1] / s); }
  return g;
}
function addBox(x0, y0, z0, x1, y1, z1, material, collide = true, shadow = true) {
  const w = x1 - x0, h = y1 - y0, d = z1 - z0;
  const m = new THREE.Mesh(boxGeo(w, h, d), material);
  m.position.set(x0 + w / 2, y0 + h / 2, z0 + d / 2); m.castShadow = shadow; m.receiveShadow = true; scene.add(m);
  if (collide) boxes.push({ x0, x1, y0, y1, z0, z1 });
  return m;
}

function buildWorld() {
  const R = mulberry(20240611);
  const rr = (a, b) => a + R() * (b - a);
  // sol
  const ground = new THREE.Mesh(new THREE.PlaneGeometry(MAP * 2 + 10, MAP * 2 + 10), new THREE.MeshStandardMaterial({ map: TEX.grass, roughness: 1 }));
  ground.rotation.x = -Math.PI / 2; ground.receiveShadow = true; scene.add(ground);
  const sand = new THREE.Mesh(new THREE.PlaneGeometry(MAP * 2 + 50, MAP * 2 + 50), lam(0xf3d98a));
  sand.rotation.x = -Math.PI / 2; sand.position.y = -.12; scene.add(sand);
  const sea = new THREE.Mesh(new THREE.PlaneGeometry(4000, 4000), lam(0x2fa8ff, { transparent: true, opacity: .95 }));
  sea.rotation.x = -Math.PI / 2; sea.position.y = -.5; scene.add(sea);
  // ciel : soleil + nuages
  const sunMesh = new THREE.Mesh(new THREE.SphereGeometry(22, 16, 16), basic(0xfff3a0, { fog: false }));
  sunMesh.position.set(300, 420, 200); sunMesh.visible = false; scene.add(sunMesh);
  const cloudM = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 1, transparent: true, opacity: .85 });
  for (let i = 0; i < 26; i++) {
    const c = new THREE.Group(); const n = 3 + Math.floor(R() * 3);
    for (let j = 0; j < n; j++) { const s = new THREE.Mesh(new THREE.IcosahedronGeometry(rr(8, 15), 0), cloudM); s.position.set(j * 11 - n * 5, rr(-2, 3), rr(-4, 4)); s.scale.y = .6; c.add(s); }
    c.position.set(rr(-450, 450), rr(110, 170), rr(-450, 450)); scene.add(c);
  }

  // bâtiments
  const placed = [];
  const free = (x0, z0, x1, z1, m = 3) => !placed.some(p => x0 - m < p[2] && x1 + m > p[0] && z0 - m < p[3] && z1 + m > p[1]);
  const wallCols = [0xb9a58a, 0x9aa3a8, 0x8f9288, 0xc9b79a, 0x7d8a8f, 0xa3846b, 0xd0cfc8];
  const roofCols = [0x4a4f55, 0x7a4036, 0x3d4a3f, 0x55585e, 0x6b5a45];
  const T = .4;
  function wallZ(xa, xb, z, h, m, hole) { // mur le long de X (porte optionnelle)
    if (hole == null) return addBox(xa, 0, z, xb, h, z + T, m);
    addBox(xa, 0, z, hole - 1.2, h, z + T, m); addBox(hole + 1.2, 0, z, xb, h, z + T, m); addBox(hole - 1.2, 2.7, z, hole + 1.2, h, z + T, m);
  }
  function wallX(x, za, zb, h, m, win) { // mur le long de Z avec fenêtre
    if (win == null) return addBox(x, 0, za, x + T, h, zb, m);
    addBox(x, 0, za, x + T, h, win - 1.2, m); addBox(x, 0, win + 1.2, x + T, h, zb, m);
    addBox(x, 0, win - 1.2, x + T, 1, win + 1.2, m); addBox(x, 2.1, win - 1.2, x + T, h, win + 1.2, m);
  }
  function building(cx, cz, w, d, h) {
    const x0 = cx - w / 2, x1 = cx + w / 2, z0 = cz - d / 2, z1 = cz + d / 2;
    const wm = mat('brick', pick(wallCols)), rm = mat('roof', pick(roofCols)), wd = mat('wood', 0xffffff);
    wallZ(x0, x1, z0, h, wm, R() < .5 ? cx : null); wallZ(x0, x1, z1 - T, h, wm, cx);
    wallX(x0, z0 + T, z1 - T, h, wm, cz); wallX(x1 - T, z0 + T, z1 - T, h, wm, R() < .6 ? cz : null);
    addBox(x0, h, z0, x1, h + .3, z1, rm);
    // parapet 3 côtés (pas côté escalier +X)
    addBox(x0, h + .3, z0, x1, h + 1.1, z0 + .3, rm); addBox(x0, h + .3, z1 - .3, x1, h + 1.1, z1, rm); addBox(x0, h + .3, z0, x0 + .3, h + 1.1, z1, rm);
    // escalier extérieur
    const top = h + .3, n = Math.ceil(top / .5);
    for (let i = 0; i < n; i++) addBox(x1, 0, z0 + 1 + i * .8, x1 + 1.8, top * (i + 1) / n, z0 + 1.8 + i * .8, mat('concrete', 0xffffff));
    // intérieur
    if (R() < .7) addBox(cx - 1, 0, cz - 1, cx + 1, .9, cz + 1, wd);
    placed.push([x0 - 3, z0 - 3, x1 + 4.5, z1 + 3]);
    rects.push([x0, z0, x1, z1]);
    lootSpots.push([cx + rr(-w / 3, w / 3), 0, cz + rr(-d / 3, d / 3), 'w'], [cx + rr(-w / 3, w / 3), 0, cz + rr(-d / 3, d / 3)], [cx, top, cz, 'w']);
  }
  function container(cx, cz, rot) {
    const col = pick([0x7a2e2a, 0x2e4a6b, 0x8a6a2a, 0x3a5a3a, 0x555a60]), L = 6, W = 2.4;
    const [a, b] = rot ? [W, L] : [L, W];
    addBox(cx - a / 2, 0, cz - b / 2, cx + a / 2, 2.6, cz + b / 2, mat('metal', col));
    placed.push([cx - a / 2 - 1, cz - b / 2 - 1, cx + a / 2 + 1, cz + b / 2 + 1]); rects.push([cx - a / 2, cz - b / 2, cx + a / 2, cz + b / 2]);
  }
  function crates(cx, cz) {
    const n = 1 + Math.floor(R() * 3);
    for (let i = 0; i < n; i++) {
      const x = cx + rr(-3, 3), z = cz + rr(-3, 3);
      if (!free(x - .7, z - .7, x + .7, z + .7, 0)) continue;
      addBox(x - .6, 0, z - .6, x + .6, 1.2, z + .6, mat('crate', 0xffffff));
      if (R() < .4) addBox(x - .55, 1.2, z - .55, x + .55, 2.3, z + .55, mat('crate', 0xffe0b0));
      placed.push([x - .6, z - .6, x + .6, z + .6]);
    }
  }
  // 9 zones d'intérêt
  const pois = [];
  for (let i = -1; i <= 1; i++) for (let j = -1; j <= 1; j++) pois.push([i * 85 + rr(-12, 12), j * 85 + rr(-12, 12)]);
  for (const [px, pz] of pois) {
    const count = (px === 0 && pz === 0) ? 6 : 3 + Math.floor(R() * 3);
    let tries = 0, made = 0;
    while (made < count && tries++ < 60) {
      const w = rr(8, 13), d = rr(8, 11), h = pick([3.2, 3.6, 4.2]);
      const cx = px + rr(-26, 26), cz = pz + rr(-26, 26);
      if (Math.abs(cx) > MAP - 12 || Math.abs(cz) > MAP - 12) continue;
      if (!free(cx - w / 2, cz - d / 2, cx + w / 2 + 2, cz + d / 2)) continue;
      building(cx, cz, w, d, h); made++; crates(cx + w / 2 + 4, cz + rr(-5, 5));
    }
    for (let k = 0; k < 2; k++) { const cx = px + rr(-30, 30), cz = pz + rr(-30, 30); if (free(cx - 3.5, cz - 3.5, cx + 3.5, cz + 3.5, 1)) container(cx, cz, R() < .5); }
  }
  // arbres (instanciés)
  const trees = [];
  for (let i = 0; i < 260; i++) {
    const x = rr(-MAP + 6, MAP - 6), z = rr(-MAP + 6, MAP - 6);
    if (!free(x - 1.2, z - 1.2, x + 1.2, z + 1.2, 1)) continue;
    placed.push([x - 1, z - 1, x + 1, z + 1]); trees.push([x, z, rr(.8, 1.4), R() < .5]);
    boxes.push({ x0: x - .35, x1: x + .35, y0: 0, y1: 4, z0: z - .35, z1: z + .35 });
  }
  const trunkG = new THREE.CylinderGeometry(.25, .35, 2.4, 6).translate(0, 1.2, 0);
  const coneG = new THREE.ConeGeometry(1.7, 2.8, 7).translate(0, 3.6, 0), cone2G = new THREE.ConeGeometry(1.25, 2.4, 7).translate(0, 5.0, 0);
  const ballG = new THREE.IcosahedronGeometry(1.9, 1).translate(0, 3.9, 0);
  const pines = trees.filter(t => t[3]), balls = trees.filter(t => !t[3]);
  const inst = (geo, material, list, colors) => {
    const im = new THREE.InstancedMesh(geo, material, list.length), m4 = new THREE.Matrix4(), c = new THREE.Color();
    list.forEach((t, i) => { m4.makeScale(t[2], t[2], t[2]); m4.setPosition(t[0], 0, t[1]); im.setMatrixAt(i, m4); if (colors) { c.set(pick(colors)); im.setColorAt(i, c); } });
    im.castShadow = true; im.receiveShadow = true; scene.add(im); return im;
  };
  inst(trunkG, lam(0x8a5a2b), trees);
  inst(coneG, lam(0x2d5a2d), pines, [0x2d5a2d, 0x264d2a, 0x35652f]); inst(cone2G, lam(0x3a6e34), pines);
  inst(ballG, lam(0x4a7a35), balls, [0x4a7a35, 0x3f6e30, 0x5c8a3a, 0x7a8a3a]);
  // rochers
  for (let i = 0; i < 40; i++) {
    const x = rr(-MAP + 6, MAP - 6), z = rr(-MAP + 6, MAP - 6), s = rr(1, 2.2);
    if (!free(x - s, z - s, x + s, z + s, 1)) continue; placed.push([x - s, z - s, x + s, z + s]);
    const m = new THREE.Mesh(new THREE.DodecahedronGeometry(s, 0), lam(0x8d9096, { flatShading: true, roughness: 1 })); m.position.set(x, s * .55, z); m.rotation.set(R() * 3, R() * 3, 0); m.castShadow = true; scene.add(m);
    boxes.push({ x0: x - s * .7, x1: x + s * .7, y0: 0, y1: s * 1.0, z0: z - s * .7, z1: z + s * .7 });
  }
  // lieux de loot en terrain ouvert
  for (let i = 0; i < 50; i++) { const x = rr(-MAP + 8, MAP - 8), z = rr(-MAP + 8, MAP - 8); if (!collideAt(x, 0, z, .5, 1)) lootSpots.push([x, 0, z]); }
  // zone (tempête)
  worldBuilt = true;
}

/* ---------------- Collisions ---------------- */
function collideAt(x, y, z, r, h) {
  for (let i = 0; i < boxes.length; i++) { const b = boxes[i]; if (x + r > b.x0 && x - r < b.x1 && y + h > b.y0 && y < b.y1 && z + r > b.z0 && z - r < b.z1) return b; }
  return null;
}
const G = 24;
function moveEnt(e, dt, gravity = true) {
  const r = e.r, h = e.hh; let blocked = false;
  let n = e.x + e.vx * dt, b = collideAt(n, e.y, e.z, r, h);
  if (b) { if (e.onGround && b.y1 - e.y <= .6 && !collideAt(n, b.y1 + .001, e.z, r, h)) { e.x = n; e.y = b.y1 + .001; } else { e.vx = 0; blocked = true; } } else e.x = n;
  n = e.z + e.vz * dt; b = collideAt(e.x, e.y, n, r, h);
  if (b) { if (e.onGround && b.y1 - e.y <= .6 && !collideAt(e.x, b.y1 + .001, n, r, h)) { e.z = n; e.y = b.y1 + .001; } else { e.vz = 0; blocked = true; } } else e.z = n;
  e.x = clamp(e.x, -MAP + 1, MAP - 1); e.z = clamp(e.z, -MAP + 1, MAP - 1);
  if (gravity) e.vy -= G * dt;
  let ny = e.y + e.vy * dt; e.onGround = false;
  b = collideAt(e.x, ny, e.z, r, h);
  if (b) { if (e.vy < 0) { ny = b.y1; e.onGround = true; } else ny = b.y0 - h - .001; e.vy = 0; }
  if (ny <= 0) { ny = 0; if (e.vy < 0) e.vy = 0; e.onGround = true; }
  e.y = ny; return blocked;
}
function rayBox(ox, oy, oz, dx, dy, dz, b) {
  let tmin = 0, tmax = 1e9, t1, t2;
  if (Math.abs(dx) < 1e-9) { if (ox < b.x0 || ox > b.x1) return -1; } else { t1 = (b.x0 - ox) / dx; t2 = (b.x1 - ox) / dx; if (t1 > t2) { const t = t1; t1 = t2; t2 = t; } if (t1 > tmin) tmin = t1; if (t2 < tmax) tmax = t2; if (tmin > tmax) return -1; }
  if (Math.abs(dy) < 1e-9) { if (oy < b.y0 || oy > b.y1) return -1; } else { t1 = (b.y0 - oy) / dy; t2 = (b.y1 - oy) / dy; if (t1 > t2) { const t = t1; t1 = t2; t2 = t; } if (t1 > tmin) tmin = t1; if (t2 < tmax) tmax = t2; if (tmin > tmax) return -1; }
  if (Math.abs(dz) < 1e-9) { if (oz < b.z0 || oz > b.z1) return -1; } else { t1 = (b.z0 - oz) / dz; t2 = (b.z1 - oz) / dz; if (t1 > t2) { const t = t1; t1 = t2; t2 = t; } if (t1 > tmin) tmin = t1; if (t2 < tmax) tmax = t2; if (tmin > tmax) return -1; }
  return tmin;
}
function raySphere(ox, oy, oz, dx, dy, dz, cx, cy, cz, r) {
  const px = ox - cx, py = oy - cy, pz = oz - cz, bb = px * dx + py * dy + pz * dz, c = px * px + py * py + pz * pz - r * r, disc = bb * bb - c;
  if (disc < 0) return -1; const t = -bb - Math.sqrt(disc); return t >= 0 ? t : -1;
}
function worldRay(ox, oy, oz, dx, dy, dz, maxT) {
  let best = maxT;
  for (let i = 0; i < boxes.length; i++) { const t = rayBox(ox, oy, oz, dx, dy, dz, boxes[i]); if (t >= 0 && t < best) best = t; }
  if (dy < 0) { const t = -oy / dy; if (t >= 0 && t < best) best = t; }
  return best;
}
function castRay(ox, oy, oz, dx, dy, dz, maxT, ignore) {
  let tw = worldRay(ox, oy, oz, dx, dy, dz, maxT), hit = null, head = false;
  const list = bots.concat([P]);
  for (const e of list) {
    if (e === ignore || !e.alive || e.inv > 0) continue;
    const ddx = e.x - ox, ddz = e.z - oz; if (ddx * ddx + ddz * ddz > (tw + 2) * (tw + 2)) continue;
    let t = raySphere(ox, oy, oz, dx, dy, dz, e.x, e.y + e.hh - .22, e.z, .27), hd = true;
    for (const [fy, fr] of [[.62, .42], [.3, .42]]) { const t2 = raySphere(ox, oy, oz, dx, dy, dz, e.x, e.y + e.hh * fy, e.z, fr); if (t2 >= 0 && (t < 0 || t2 < t)) { t = t2; hd = false; } }
    if (t >= 0 && t < tw) { tw = t; hit = e; head = hd; }
  }
  return { t: tw, ent: hit, head, x: ox + dx * tw, y: oy + dy * tw, z: oz + dz * tw };
}
const losClear = (a, b) => { const dx = b.x - a.x, dy = b.y - a.y, dz = b.z - a.z, d = Math.hypot(dx, dy, dz); return worldRay(a.x, a.y, a.z, dx / d, dy / d, dz / d, d) >= d - .3; };

/* ---------------- Effets : traçantes & particules ---------------- */
const tracers = [], parts = [];
const trGeo = new THREE.BoxGeometry(.035, .035, 1).translate(0, 0, .5);
for (let i = 0; i < 40; i++) { const m = new THREE.Mesh(trGeo, basic(0xffe680, { transparent: true })); m.visible = false; scene.add(m); tracers.push({ m, life: 0 }); }
function tracer(a, b, col = 0xffe680) {
  const t = tracers.find(t => t.life <= 0) || tracers[0];
  t.m.position.copy(a); t.m.lookAt(b); t.m.scale.z = a.distanceTo(b); t.m.material.color.setHex(col); t.m.visible = true; t.life = .09;
}
const pGeo = new THREE.BoxGeometry(.12, .12, .12);
for (let i = 0; i < 80; i++) { const m = new THREE.Mesh(pGeo, basic(0xffffff, { transparent: true })); m.visible = false; scene.add(m); parts.push({ m, life: 0, v: new THREE.Vector3() }); }
function burst(x, y, z, col, n = 6, sp = 4) {
  for (let i = 0; i < n; i++) {
    const p = parts.find(p => p.life <= 0); if (!p) return;
    p.m.position.set(x, y, z); p.m.material.color.setHex(col); p.m.visible = true; p.life = rnd(.25, .5);
    p.v.set(rnd(-sp, sp), rnd(0, sp), rnd(-sp, sp));
  }
}
function updateFx(dt) {
  for (const t of tracers) if (t.life > 0) { t.life -= dt; t.m.material.opacity = Math.max(0, t.life / .09); if (t.life <= 0) t.m.visible = false; }
  for (const p of parts) if (p.life > 0) { p.life -= dt; p.v.y -= 14 * dt; p.m.position.addScaledVector(p.v, dt); p.m.scale.setScalar(Math.max(.1, p.life * 2.5)); if (p.life <= 0) p.m.visible = false; }
}

/* ---------------- Modèles : armes (vue FPS) ---------------- */
function makeGun(w, withHands = true) {
  const g = new THREE.Group(), L = w.len, col = w.color;
  const M = c => lam(c), dark = M(0x2a2a3c), acc = M(col), light = M(0x6b7078);
  const add = (geo, m, x, y, z) => { const o = new THREE.Mesh(geo, m); o.position.set(x, y, z); g.add(o); return o; };
  add(new THREE.BoxGeometry(.07, .1, L * .6), acc, 0, 0, -L * .2);
  add(new THREE.BoxGeometry(.035, .035, L * .5), dark, 0, .02, -L * .7);
  add(new THREE.BoxGeometry(.085, .015, L * .6), light, 0, .058, -L * .2);
  if (L > .35) add(new THREE.BoxGeometry(.06, .09, L * .3), dark, 0, -.01, L * .22);
  add(new THREE.BoxGeometry(.05, .1, .05), dark, 0, -.09, L * .04).rotation.x = .3;
  if (w === WEAPONS.pistol) { } else add(new THREE.BoxGeometry(.045, .17, .06), light, 0, -.12, -L * .15).rotation.x = -.12;
  if (w.scope) { const s = add(new THREE.CylinderGeometry(.03, .03, .28, 8), dark, 0, .1, -L * .2); s.rotation.x = Math.PI / 2; }
  else add(new THREE.BoxGeometry(.03, .03, .05), light, 0, .08, -L * .55);
  if (w === WEAPONS.shotgun) add(new THREE.BoxGeometry(.06, .06, .2), light, 0, -.05, -L * .55);
  if (w === WEAPONS.lmg) add(new THREE.BoxGeometry(.14, .12, .1), light, 0, -.12, -L * .1);
  if (withHands) {
    const skin = M(0xd9a97c), sleeve = M(0x3d4a36);
    add(new THREE.BoxGeometry(.075, .075, .085), skin, 0, -.07, L * .04);
    add(new THREE.BoxGeometry(.09, .09, .3), sleeve, .02, -.1, L * .04 + .2);
    add(new THREE.BoxGeometry(.075, .075, .085), skin, 0, -.06, -L * .4);
    add(new THREE.BoxGeometry(.09, .09, .3), sleeve, -.12, -.12, -L * .4 + .22).rotation.y = .4;
  }
  const mz = new THREE.Object3D(); mz.position.set(0, .02, -L * .95); g.add(mz); g.userData.muzzle = mz;
  return g;
}
const vm = new THREE.Group(); vm.scale.setScalar(.8); vmScene.add(vm);
const flash = new THREE.Mesh(new THREE.PlaneGeometry(.22, .22), basic(0xffd84a, { transparent: true, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide }));
flash.visible = false; vmScene.add(flash);
let gunMesh = null;
function setGunModel(id) {
  if (gunMesh) vm.remove(gunMesh);
  gunMesh = makeGun(WEAPONS[id]); vm.add(gunMesh);
}

/* ---------------- Joueur / entités ---------------- */
const P = { name: 'Toi', isP: true, x: 0, y: 0, z: 0, vx: 0, vy: 0, vz: 0, yaw: 0, pitch: 0, r: .35, hh: 1.8, eye: 1.65, onGround: true, hp: 100, sh: 0, alive: true, inv: 0,
  wp: [], cur: 0, reloadT: 0, switchT: 0, fireCd: 0, bloom: 0, kills: 0, dmgDealt: 0, crouch: false, slideT: 0, slideSpd: 0, sdx: 0, sdz: 0, sprint: false,
  dropping: false, chute: false, ads: 0, adsHeld: false, fireHeld: false, fireClick: false, kick: 0, bob: 0, lastHurt: 99, deaths: 0 };
const bots = [];
const pickups = [];
const NAMES = ['Yoshi', 'Zorg', 'Nova', 'Bobby', 'Kiki', 'Max', 'Luna', 'Tigre', 'Pixel', 'Rex', 'Mochi', 'Blaze', 'Coco', 'Ninja42', 'Zed', 'Pepito', 'Ombre', 'Gizmo', 'Fifi', 'Turbo', 'Chaton', 'Boum', 'Vortex', 'Pickle', 'Nugget', 'Dragon', 'Fuzzy', 'Sushi', 'Titan', 'Mango', 'Raptor', 'Biscuit', 'Cactus', 'Flash', 'Lynx', 'Panda', 'Zigzag', 'Waffle', 'Comet', 'Bruno'];
const OUTFIT = [0x3d4a36, 0x4a4d55, 0x5a4a3a, 0x2f3b4a, 0x6b6b58, 0x3b3b3f, 0x56603f, 0x4a3a3a];

function newWeaponState(id) { const w = WEAPONS[id]; return { id, ammo: w.mag, res: w.res }; }
const curW = () => WEAPONS[P.wp[P.cur].id];

function outlined(mesh, s = 1.12) { return mesh; const o = new THREE.Mesh(mesh.geometry, new THREE.MeshBasicMaterial({ color: 0x14122b, side: THREE.BackSide })); o.scale.setScalar(s); mesh.add(o); return mesh; }
function makeBotMesh(col, wcol) {
  const g = new THREE.Group(), body = lam(col), skin = lam(0xd9a97c), pants = lam(0x2e3138);
  const part = (geo, m, x, y, z, out = 1.12) => { const o = new THREE.Mesh(geo, m); o.position.set(x, y, z); o.castShadow = true; if (out) outlined(o, out); g.add(o); return o; };
  const legL = part(new THREE.BoxGeometry(.24, .8, .26).translate(0, -.4, 0), pants, -.14, .8, 0, 1.18);
  const legR = part(new THREE.BoxGeometry(.24, .8, .26).translate(0, -.4, 0), pants, .14, .8, 0, 1.18);
  part(new THREE.BoxGeometry(.6, .62, .34), body, 0, 1.1, 0, 1.1);
  part(new THREE.SphereGeometry(.27, 12, 10), skin, 0, 1.58, 0, 1.1);
  part(new THREE.BoxGeometry(.56, .16, .56), lam(pick([0x2a2d31, 0x3a4030, 0x55504a])), 0, 1.8, 0, 1.12); // casquette/bonnet
  part(new THREE.BoxGeometry(.1, .1, .04), lam(0x111122), -.09, 1.62, -.26, 0); part(new THREE.BoxGeometry(.1, .1, .04), lam(0x111122), .09, 1.62, -.26, 0);
  const armL = part(new THREE.BoxGeometry(.16, .55, .16).translate(0, -.25, 0), body, -.38, 1.35, 0, 1.15);
  const armR = part(new THREE.BoxGeometry(.16, .55, .16).translate(0, -.25, 0), body, .38, 1.35, 0, 1.15);
  armL.rotation.x = armR.rotation.x = -1.2;
  part(new THREE.BoxGeometry(.1, .12, .62), lam(wcol), .15, 1.12, -.42, 1.1);
  g.userData = { legL, legR };
  return g;
}

/* ---------------- Boucle bots ---------------- */
function randomSpawn(minDistFromP = 0) {
  for (let i = 0; i < 40; i++) {
    const x = rnd(-MAP + 8, MAP - 8), z = rnd(-MAP + 8, MAP - 8);
    if (collideAt(x, 0, z, .6, 2)) continue;
    if (minDistFromP && Math.hypot(x - P.x, z - P.z) < minDistFromP) continue;
    return [x, z];
  }
  return [0, 0];
}
function spawnBot(i, respawn) {
  let b = bots[i];
  const col = OUTFIT[i % OUTFIT.length], wid = pick(['ar', 'ar', 'smg', 'smg', 'shotgun', 'lmg', 'sniper', 'pistol']);
  if (!b) { b = bots[i] = { isP: false, name: NAMES[i % NAMES.length], r: .35, hh: 1.8, kills: 0, mesh: null }; }
  if (b.mesh) scene.remove(b.mesh);
  const [x, z] = randomSpawn(respawn ? 30 : 45);
  Object.assign(b, { x, y: 0, z, vx: 0, vy: 0, vz: 0, yaw: rnd(0, 6.28), onGround: true, alive: true, hp: 100, sh: mode === 'br' ? pick([0, 0, 25, 50]) : 0, inv: respawn ? 2.5 : 0,
    wid, wdef: WEAPONS[wid], target: null, thinkT: rnd(0, .5), react: 0, burst: 0, fireCd: 0, pause: rnd(.5, 2), goalX: x, goalZ: z, goalT: 0, stuck: 0, lx: x, lz: z, deadT: 0, lastShot: 99, strafe: Math.random() < .5 ? 1 : -1, walk: 0, col });
  b.mesh = makeBotMesh(col, b.wdef.color); b.mesh.position.set(x, 0, z); scene.add(b.mesh);
}
const DIFF = [{ acc: .4, dmg: .55, react: 1.1, spd: 3.8 }, { acc: .55, dmg: .75, react: .7, spd: 4.4 }, { acc: .72, dmg: .95, react: .45, spd: 5 }, { acc: .88, dmg: 1.2, react: .25, spd: 5.6 }];
const IDEAL = { shotgun: 10, smg: 18, ar: 28, lmg: 32, sniper: 70, pistol: 16 };

function botThink(b) {
  const D = DIFF[S.diff], range = b.wdef.range * .7; let best = null;
  const cands = [];
  for (const e of bots.concat([P])) {
    if (e === b || !e.alive || e.inv > 0 || (e.isP && P.y > 12)) continue;
    const d = Math.hypot(e.x - b.x, e.z - b.z); if (d < Math.min(range, 90)) cands.push([d, e]);
  }
  cands.sort((a, c) => a[0] - c[0]);
  for (let i = 0; i < Math.min(3, cands.length); i++) { const e = cands[i][1]; if (losClear({ x: b.x, y: b.y + 1.5, z: b.z }, { x: e.x, y: e.y + e.hh * .6, z: e.z })) { best = e; break; } }
  if (best && b.target !== best) b.react = rnd(.8, 1.6) * D.react;
  b.target = best;
}
function botGoal(b) {
  let gx, gz;
  if (mode === 'br' && zone.r < 400 && Math.hypot(b.x - zone.cx, b.z - zone.cz) > zone.r * .8) { gx = zone.cx + rnd(-zone.r * .4, zone.r * .4); gz = zone.cz + rnd(-zone.r * .4, zone.r * .4); }
  else {
    const near = pickups.filter(p => !p.taken && Math.hypot(p.x - b.x, p.z - b.z) < 50);
    if (near.length && Math.random() < .5) { const p = pick(near); gx = p.x; gz = p.z; }
    else { const a = rnd(0, 6.28), d = rnd(15, 60); gx = b.x + Math.cos(a) * d; gz = b.z + Math.sin(a) * d; }
  }
  b.goalX = clamp(gx, -MAP + 5, MAP - 5); b.goalZ = clamp(gz, -MAP + 5, MAP - 5); b.goalT = rnd(6, 14);
}
function updateBot(b, dt, i) {
  const D = DIFF[S.diff];
  if (!b.alive) {
    b.deadT += dt; if (b.mesh) { b.mesh.rotation.x = Math.max(-1.5, b.mesh.rotation.x - dt * 5); b.mesh.position.y = Math.max(0, b.y - b.deadT * .1); if (b.deadT > 6 && mode === 'br') { scene.remove(b.mesh); b.mesh = null; } }
    if (mode === 'ffa' && b.deadT > 3.5 && state !== 'ended') spawnBot(i, true);
    return;
  }
  b.inv = Math.max(0, b.inv - dt); b.lastShot += dt; b.fireCd -= dt; b.thinkT -= dt; b.goalT -= dt;
  if (b.thinkT <= 0) { b.thinkT = rnd(.3, .6); botThink(b); }
  let wx = 0, wz = 0, spd = D.spd;
  const t = b.target;
  if (t && t.alive) {
    const dx = t.x - b.x, dz = t.z - b.z, d = Math.hypot(dx, dz) || 1;
    b.yaw = angLerp(b.yaw, Math.atan2(-dx, -dz), 1 - Math.exp(-9 * dt));
    const ideal = IDEAL[b.wid] || 25, fx = dx / d, fz = dz / d;
    if (d > ideal + 8) { wx += fx; wz += fz; } else if (d < ideal - 8) { wx -= fx; wz -= fz; }
    if (Math.sin(performance.now() / 1000 * .7 + i) > 0) b.strafe = 1; else b.strafe = -1;
    wx += -fz * b.strafe * .8; wz += fx * b.strafe * .8; spd *= .85;
    b.react -= dt;
    if (b.react <= 0 && b.fireCd <= 0) {
      if (b.burst <= 0) { b.pause -= dt * 2; if (b.pause <= 0) { b.burst = 3 + Math.floor(Math.random() * 5); b.pause = rnd(.3, 1.1); } }
      if (b.burst > 0) {
        b.burst--; b.fireCd = 60 / b.wdef.rpm * (b.wdef.auto ? 1 : 1.2);
        botShoot(b, t, d, D);
      }
    }
  } else {
    if (b.goalT <= 0 || Math.hypot(b.goalX - b.x, b.goalZ - b.z) < 3) botGoal(b);
    const dx = b.goalX - b.x, dz = b.goalZ - b.z, d = Math.hypot(dx, dz) || 1; wx = dx / d; wz = dz / d;
    b.yaw = angLerp(b.yaw, Math.atan2(-dx, -dz), 1 - Math.exp(-6 * dt));
  }
  const l = Math.hypot(wx, wz) || 1; b.vx = lerp(b.vx, wx / l * spd * (wx || wz ? 1 : 0), 1 - Math.exp(-8 * dt)); b.vz = lerp(b.vz, wz / l * spd * (wx || wz ? 1 : 0), 1 - Math.exp(-8 * dt));
  const blocked = moveEnt(b, dt);
  if (blocked && b.onGround && Math.random() < .08) b.vy = 8;
  b.stuck += dt; if (b.stuck > 1.5) { if (Math.hypot(b.x - b.lx, b.z - b.lz) < .6) { botGoal(b); if (b.onGround) b.vy = 8; } b.lx = b.x; b.lz = b.z; b.stuck = 0; }
  // zone
  if (mode === 'br' && Math.hypot(b.x - zone.cx, b.z - zone.cz) > zone.r) hurtEnt(b, zone.dmg * dt, null, false, true);
  // visuel
  b.walk += Math.hypot(b.vx, b.vz) * dt * 2.2;
  const m = b.mesh; if (m) { m.position.set(b.x, b.y, b.z); m.rotation.y = b.yaw; const s = Math.sin(b.walk) * .7 * Math.min(1, Math.hypot(b.vx, b.vz)); m.userData.legL.rotation.x = s; m.userData.legR.rotation.x = -s; }
}
function botShoot(b, t, d, D) {
  const w = b.wdef; let acc = clamp(D.acc - d * (w.pellets > 1 ? .02 : .0045), .05, .95);
  if (w.scope) acc = Math.min(.95, D.acc + .1);
  const ex = Math.sin(b.yaw), ez = Math.cos(b.yaw);
  const from = new THREE.Vector3(b.x - ex * .6, b.y + 1.25, b.z - ez * .6);
  const hit = Math.random() < acc;
  const to = new THREE.Vector3(t.x + (hit ? 0 : rnd(-1.5, 1.5)), t.y + t.hh * .6 + (hit ? 0 : rnd(-.8, .8)), t.z + (hit ? 0 : rnd(-1.5, 1.5)));
  tracer(from, to, 0xff9a9a);
  const dist = Math.hypot(P.x - b.x, P.z - b.z); if (dist < 90) sfx.shot(w, clamp(1 - dist / 90, .05, .6));
  b.lastShot = 0;
  if (hit) {
    let dmg = w.dmg * D.dmg * (w.pellets > 1 ? 4 : 1) * clamp(1 - (d - w.range * .5) / w.range, .25, 1);
    if (Math.random() < .12) dmg *= 1.8;
    hurtEnt(t, dmg, b, false);
  }
}

/* ---------------- Dégâts & éliminations ---------------- */
function hurtEnt(t, amt, att, head, bypass) {
  if (!t.alive || t.inv > 0) return;
  if (!bypass && t.sh > 0) { const a = Math.min(t.sh, amt); t.sh -= a; amt -= a; }
  t.hp -= amt;
  if (t.isP) {
    P.lastHurt = 0; $('dmgvignette').style.opacity = clamp(.4 + amt / 60, .3, 1); setTimeout(() => $('dmgvignette').style.opacity = 0, 150);
    if (att) { const rel = Math.atan2(-(att.x - P.x), -(att.z - P.z)) - P.yaw; const dd = $('dmgdir'); dd.style.transform = `rotate(${-rel}rad)`; dd.style.opacity = 1; setTimeout(() => dd.style.opacity = 0, 600); }
    if (amt > 0.5) sfx.hurt();
  }
  if (att && att.isP) { P.dmgDealt += amt; hitmarker(false); sfx.hit(); if (!t.isP) burst(t.x, t.y + t.hh * .7, t.z, 0xff4d4d, 5, 3); }
  if (t.hp <= 0) kill(t, att, head);
}
function hitmarker(kill) { const h = $('hitmarker'); h.className = ''; void h.offsetWidth; h.className = 'show' + (kill ? ' kill' : ''); }
function feed(a, b, head) {
  const f = $('killfeed'), d = document.createElement('div'); d.innerHTML = `${a ? `<b>${a.name}</b> 🔫${head ? '🎯' : ''}` : '🌀'} ${b.name}`;
  if (a && a.isP) d.className = 'me'; f.appendChild(d); setTimeout(() => d.remove(), 5000); while (f.children.length > 5) f.firstChild.remove();
}
function aliveCount() { return bots.filter(b => b.alive).length + (P.alive ? 1 : 0); }
function kill(t, att, head) {
  t.alive = false; t.hp = 0; feed(att, t, head);
  if (att) att.kills = (att.kills || 0) + 1;
  if (att && att.isP) { hitmarker(true); sfx.kill(); banner(`ÉLIMINÉ  ${t.name}${head ? '  🎯' : ''}`, 1500); }
  if (!t.isP) {
    t.deadT = 0;
    if (mode === 'br') {
      dropPickup('ammo', t.x + .5, t.y, t.z); if (Math.random() < .6) dropPickup('weapon', t.x - .5, t.y, t.z, t.wid); if (Math.random() < .5) dropPickup(pick(['health', 'shield']), t.x, t.y, t.z + .6);
    }
  } else {
    P.deaths++; P.deadAt = performance.now();
    if (mode === 'br') { P.place = aliveCount() + 1; P.killer = att; setTimeout(() => endGame(false), 1600); }
    else { P.respawnT = 3; banner('Éliminé ! Réapparition…', 2800); }
  }
  if (mode === 'br' && state === 'playing' && P.alive && aliveCount() === 1) { P.place = 1; setTimeout(() => endGame(true), 1200); banner('DERNIER SURVIVANT !', 3000); }
}

/* ---------------- Loot ---------------- */
const BEAM = { ar: 0x6a5cff, smg: 0xff9a2e, shotgun: 0xe83a3a, sniper: 0x33c46a, lmg: 0xffc400, pistol: 0x9aa4b8 };
const PK = { ammo: 0xffd23f, health: 0xff3b3b, shield: 0x3da5ff };
function dropPickup(type, x, y, z, wid) {
  let g;
  if (type === 'weapon') {
    g = new THREE.Group(); const gun = makeGun(WEAPONS[wid], false); gun.scale.setScalar(1.6); gun.traverse(o => { if (o.material) { o.material = o.material.clone(); o.material.emissive = new THREE.Color(BEAM[wid]); o.material.emissiveIntensity = .35; } }); g.add(gun);
    const beam = new THREE.Mesh(new THREE.CylinderGeometry(.35, .35, 5, 8, 1, true), basic(BEAM[wid], { transparent: true, opacity: .25, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide })); beam.position.y = 2.5; g.add(beam);
  } else {
    g = new THREE.Group();
    const col = PK[type], box = new THREE.Mesh(new THREE.BoxGeometry(.45, .35, .45), lam(col)); outlined(box, 1.15); g.add(box);
    if (type === 'health') { g.add(new THREE.Mesh(new THREE.BoxGeometry(.5, .12, .14).translate(0, .1, 0), lam(0xffffff))); const c2 = new THREE.Mesh(new THREE.BoxGeometry(.14, .12, .5).translate(0, .1, 0), lam(0xffffff)); g.add(c2); }
    if (type === 'shield') { const s = new THREE.Mesh(new THREE.SphereGeometry(.22, 10, 8), lam(0x9fe0ff)); s.position.y = .3; g.add(s); }
    if (type === 'ammo') { g.add(new THREE.Mesh(new THREE.BoxGeometry(.5, .1, .2).translate(0, .1, 0), lam(0x333344))); }
    const ring = new THREE.Mesh(new THREE.CylinderGeometry(.55, .55, 3, 10, 1, true), basic(col, { transparent: true, opacity: .2, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide })); ring.position.y = 1.5; g.add(ring);
  }
  g.position.set(x, y + .6, z); scene.add(g);
  pickups.push({ type, wid, x, y, z, mesh: g, taken: false, t: Math.random() * 6 });
}
function spawnLoot() {
  pickups.forEach(p => scene.remove(p.mesh)); pickups.length = 0;
  for (const s of lootSpots) {
    const wantW = s[3] === 'w'; const r = Math.random();
    if (wantW || r < .12) dropPickup('weapon', s[0], s[1], s[2], pick(WIDS));
    else dropPickup(r < .5 ? 'ammo' : r < .78 ? 'health' : 'shield', s[0], s[1], s[2]);
  }
}
function takePickup(p) {
  if (p.type === 'ammo') { P.wp.forEach(w => { const d = WEAPONS[w.id]; w.res = Math.min(d.res * 3, w.res + d.mag); }); toast('+ Munitions'); }
  else if (p.type === 'health') { if (P.hp >= 100) return false; P.hp = Math.min(100, P.hp + 50); toast('+ Soin'); }
  else if (p.type === 'shield') { if (P.sh >= 100) return false; P.sh = Math.min(100, P.sh + 50); toast('+ Bouclier'); }
  else if (p.type === 'weapon') {
    const have = P.wp.find(w => w.id === p.wid);
    if (have) { have.res = Math.min(WEAPONS[p.wid].res * 3, have.res + WEAPONS[p.wid].mag * 2); toast('+ Munitions ' + WEAPONS[p.wid].name); }
    else {
      const old = P.wp[P.cur]; dropPickup('weapon', P.x + 1.2 * Math.sin(P.yaw + 2), P.y, P.z + 1.2 * Math.cos(P.yaw + 2), old.id);
      pickups[pickups.length - 1].noPick = 2;
      P.wp[P.cur] = newWeaponState(p.wid); P.reloadT = 0; setGunModel(p.wid); toast('Arme : ' + WEAPONS[p.wid].name);
    }
  }
  sfx.pickup(); scene.remove(p.mesh); p.taken = true; return true;
}
let toastT = 0;
function toast(t) { const el = $('banner'); el.textContent = t; el.style.fontSize = '26px'; el.classList.add('show'); clearTimeout(toastT); toastT = setTimeout(() => el.classList.remove('show'), 1200); }
function banner(t, ms = 2000) { const el = $('banner'); el.textContent = t; el.style.fontSize = '44px'; el.classList.add('show'); clearTimeout(toastT); toastT = setTimeout(() => el.classList.remove('show'), ms); }

/* ---------------- Tempête (zone) ---------------- */
const PHASES = [{ r: 130, wait: 40, shrink: 35, dmg: 1.5 }, { r: 85, wait: 30, shrink: 30, dmg: 3 }, { r: 50, wait: 25, shrink: 25, dmg: 5 }, { r: 25, wait: 20, shrink: 20, dmg: 8 }, { r: 8, wait: 15, shrink: 15, dmg: 12 }, { r: 2, wait: 10, shrink: 12, dmg: 20 }];
const zone = { phase: 0, state: 'wait', t: 0, cx: 0, cz: 0, r: 230, fromR: 230, toR: 130, fromX: 0, fromZ: 0, toX: 0, toZ: 0, dmg: 1 };
const zoneMesh = new THREE.Mesh(new THREE.CylinderGeometry(1, 1, 220, 64, 1, true), basic(0x9a3dff, { transparent: true, opacity: .3, side: THREE.DoubleSide, depthWrite: false }));
zoneMesh.position.y = 100; scene.add(zoneMesh); zoneMesh.visible = false;
function zoneTarget() {
  const ph = PHASES[zone.phase]; zone.fromR = zone.r; zone.fromX = zone.cx; zone.fromZ = zone.cz; zone.toR = ph.r;
  const a = Math.random() * 6.28, d = Math.random() * Math.max(0, zone.fromR - ph.r) * .85;
  zone.toX = clamp(zone.fromX + Math.cos(a) * d, -MAP + ph.r * .3, MAP - ph.r * .3); zone.toZ = clamp(zone.fromZ + Math.sin(a) * d, -MAP + ph.r * .3, MAP - ph.r * .3); zone.dmg = ph.dmg;
}
function zoneReset() { Object.assign(zone, { phase: 0, state: 'wait', t: 0, cx: 0, cz: 0, r: 230, dmg: 1 }); zoneTarget(); }
function updateZone(dt) {
  const ph = PHASES[Math.min(zone.phase, PHASES.length - 1)]; zone.t += dt;
  if (zone.state === 'wait') { if (zone.t >= ph.wait) { zone.state = 'shrink'; zone.t = 0; banner('🌀 La tempête se resserre !', 2500); sfx.zone(); } }
  else {
    const k = clamp(zone.t / ph.shrink, 0, 1); zone.r = lerp(zone.fromR, zone.toR, k); zone.cx = lerp(zone.fromX, zone.toX, k); zone.cz = lerp(zone.fromZ, zone.toZ, k);
    if (k >= 1) { if (zone.phase < PHASES.length - 1) { zone.phase++; zone.state = 'wait'; zone.t = 0; zoneTarget(); } else zone.t = ph.shrink; }
  }
  zoneMesh.position.x = zone.cx; zoneMesh.position.z = zone.cz; zoneMesh.scale.set(zone.r, 1, zone.r);
  zoneMesh.material.opacity = .25 + Math.sin(performance.now() / 400) * .05;
  if (P.alive && Math.hypot(P.x - zone.cx, P.z - zone.cz) > zone.r) { hurtEnt(P, zone.dmg * dt, null, false, true); if (!zone.warn || performance.now() - zone.warn > 1500) { zone.warn = performance.now(); sfx.zone(); toast('⚠ Retourne dans le cercle !'); } }
}

/* ---------------- État de partie ---------------- */
let mode = 'br', state = 'menu', matchT = 0, locked = false;
const keys = {};
function resetPlayer() {
  Object.assign(P, { hp: 100, sh: 0, alive: true, kills: mode === 'ffa' && P.kills ? P.kills : 0, vx: 0, vy: 0, vz: 0, reloadT: 0, switchT: 0, fireCd: 0, bloom: 0, crouch: false, slideT: 0, hh: 1.8, ads: 0, cur: 0, inv: 0, respawnT: 0, dropping: false, chute: false, lastHurt: 99 });
  P.wp = [newWeaponState(S.primary), newWeaponState(S.secondary)]; setGunModel(P.wp[0].id);
  P.pitch = 0;
  if (mode === 'br') { P.x = rnd(-60, 60); P.z = rnd(-60, 60); P.y = 140; P.dropping = true; P.onGround = false; P.yaw = rnd(0, 6.28); P.pitch = -.35; }
  else { const [x, z] = randomSpawn(25); P.x = x; P.z = z; P.y = 0; P.inv = 2.5; P.onGround = true; }
}
function startGame(m) {
  audioInit(); mode = m; $('loading').classList.remove('hidden');
  setTimeout(() => {
    if (!worldBuilt) buildWorld();
    Object.assign(P, { kills: 0, dmgDealt: 0, deaths: 0 });
    bots.forEach(b => b.mesh && scene.remove(b.mesh)); bots.length = 0;
    resetPlayer();
    const n = mode === 'br' ? S.bots : Math.min(S.bots, 15);
    for (let i = 0; i < n; i++) spawnBot(i);
    bots.forEach(b => { b.kills = 0; });
    spawnLoot(); if (mode === 'ffa') { pickups.forEach(p => { if (p.type === 'weapon') p.taken = false; }); }
    zoneReset(); zoneMesh.visible = mode === 'br'; $('h-zonebox').firstChild.textContent = mode === 'br' ? '🌀 ' : '⏱ ';
    $('killfeed').innerHTML = ''; matchT = 0; state = 'playing';
    hideAll(); $('hud').classList.remove('hidden'); $('loading').classList.add('hidden');
    setGunModel(P.wp[0].id);
    canvas.requestPointerLock && canvas.requestPointerLock();
    banner(mode === 'br' ? '🪂 Largage ! Espace = parachute' : '⚔ Mêlée générale — 20 éliminations !', 3500);
  }, 30);
}
function endGame(win) {
  if (state !== 'playing') return; state = 'ended'; document.exitPointerLock && document.exitPointerLock();
  ST.games++; if (win) ST.wins++; ST.kills += P.kills; ST.best = Math.max(ST.best, P.kills); store.set('bz_stats', ST); mainStats();
  $('end-title').textContent = win ? '🏆 VICTOIRE !' : '💀 ÉLIMINÉ';
  $('end-title').style.color = win ? '#ffe14a' : '#ff6b6b';
  const sorted = [P].concat(bots).sort((a, b) => (b.kills || 0) - (a.kills || 0)); const rank = sorted.indexOf(P) + 1;
  $('end-sub').textContent = mode === 'br' ? (win ? 'Dernier survivant sur l\'île !' : `Éliminé par ${P.killer ? P.killer.name : 'la tempête'}`) : (win ? 'Tu domines la mêlée !' : 'Fin du match');
  $('end-stats').innerHTML = `<div><b>${mode === 'br' ? '#' + (P.place || 1) : '#' + rank}</b>Classement</div><div><b>${P.kills}</b>Éliminations</div><div><b>${Math.round(P.dmgDealt)}</b>Dégâts</div><div><b>${Math.floor(matchT / 60)}:${String(Math.floor(matchT % 60)).padStart(2, '0')}</b>Temps</div>`;
  hideAll(); $('hud').classList.add('hidden'); show('menu-end');
}
function quitToMenu() { state = 'menu'; document.exitPointerLock && document.exitPointerLock(); $('hud').classList.add('hidden'); hideAll(); show('menu-main'); }

/* ---------------- Menus ---------------- */
const screens = ['menu-main', 'menu-play', 'menu-loadout', 'menu-settings', 'menu-help', 'menu-pause', 'menu-end'];
let prevScreen = 'menu-main';
function hideAll() { screens.forEach(s => $(s).classList.add('hidden')); $('clickhint').classList.add('hidden'); }
function show(id) { hideAll(); $(id).classList.remove('hidden'); }
function mainStats() { $('main-stats').innerHTML = `Parties : ${ST.games} · Victoires : ${ST.wins} · Éliminations : ${ST.kills} · Record : ${ST.best}`; }
document.querySelectorAll('[data-go]').forEach(b => b.addEventListener('click', () => {
  audioInit(); sfx.click(); const g = b.dataset.go;
  if (g === 'back') show(prevScreen);
  else {
    if (g === 'settings' || g === 'help') prevScreen = (state === 'paused') ? 'menu-pause' : (document.querySelector('.screen:not(.hidden)') || { id: 'menu-main' }).id;
    show('menu-' + g);
  }
}));
document.querySelectorAll('.card').forEach(c => c.addEventListener('click', () => startGame(c.dataset.mode)));
$('b-resume').onclick = () => resume();
$('b-pset').onclick = () => { prevScreen = 'menu-pause'; show('menu-settings'); };
$('b-quit').onclick = quitToMenu;
$('b-again').onclick = () => startGame(mode);
$('b-menu').onclick = quitToMenu;
function pause() { if (state !== 'playing') return; state = 'paused'; for (const k in keys) keys[k] = false; P.fireHeld = P.adsHeld = false; show('menu-pause'); }
function resume() { hideAll(); state = 'playing'; canvas.requestPointerLock && canvas.requestPointerLock(); setTimeout(() => { if (state === 'playing' && !locked) $('clickhint').classList.remove('hidden'); }, 400); }
$('clickhint').onclick = () => { canvas.requestPointerLock && canvas.requestPointerLock(); $('clickhint').classList.add('hidden'); };
document.addEventListener('pointerlockchange', () => {
  locked = document.pointerLockElement === canvas;
  if (locked) $('clickhint').classList.add('hidden');
  else if (state === 'playing') pause();
});

// Loadout
function buildLoadout() {
  const fill = (id, slot, allowed) => {
    const el = $(id); el.innerHTML = '';
    allowed.forEach(k => {
      const w = WEAPONS[k], d = document.createElement('div'); d.className = 'witem' + (S[slot] === k ? ' sel' : '');
      d.innerHTML = `<span class="e">${w.e}</span>${w.name}`;
      d.onclick = () => { S[slot] = k; saveS(); sfx.click(); buildLoadout(); detail(k); };
      d.onmouseenter = () => detail(k); el.appendChild(d);
    });
  };
  fill('lo-primary', 'primary', ['ar', 'smg', 'shotgun', 'sniper', 'lmg']);
  fill('lo-secondary', 'secondary', ['pistol', 'smg', 'shotgun', 'ar']);
  detail(S.primary);
}
function detail(k) {
  const w = WEAPONS[k], bar = v => `<span class="statbar"><i style="width:${v}%"></i></span>`;
  $('lo-detail').innerHTML = `<b>${w.e} ${w.name}</b> — ${w.auto ? 'automatique' : 'semi-auto'}<br>Dégâts ${bar(w.st[0])} Cadence ${bar(w.st[1])} Portée ${bar(w.st[2])}<br>${w.dmg}${w.pellets > 1 ? '×' + w.pellets : ''} dmg · ${w.rpm} cps/min · chargeur ${w.mag} · rechargement ${w.reload}s`;
}
document.querySelector('[data-go="loadout"]').addEventListener('click', buildLoadout);
// Options
function bindSettings() {
  const rng = (id, key, fmt = v => v) => { const e = $('s-' + id), l = $('v-' + id); e.value = S[key]; l.textContent = fmt(S[key]); e.oninput = () => { S[key] = +e.value; l.textContent = fmt(S[key]); saveS(); if (key === 'vol' && master) master.gain.value = S.vol / 100; if (key === 'fov') { camera.fov = S.fov; camera.updateProjectionMatrix(); } }; };
  rng('sens', 'sens'); rng('fov', 'fov', v => v + '°'); rng('vol', 'vol', v => v + '%'); rng('bots', 'bots');
  $('s-diff').value = S.diff; $('s-diff').onchange = e => { S.diff = +e.target.value; saveS(); };
  $('s-invy').checked = S.invy; $('s-invy').onchange = e => { S.invy = e.target.checked; saveS(); };
  $('s-fps').checked = S.fps; $('s-fps').onchange = e => { S.fps = e.target.checked; saveS(); };
}

/* ---------------- Entrées ---------------- */
addEventListener('keydown', e => {
  if (e.code === 'Tab') e.preventDefault();
  if (state !== 'playing') return;
  if (e.repeat && e.code !== 'Space') return;
  keys[e.code] = true;
  if (e.code === 'Digit1' || e.code === 'Numpad1') switchWeapon(0);
  if (e.code === 'Digit2' || e.code === 'Numpad2') switchWeapon(1);
  if (e.code === 'KeyR') reload();
  if (e.code === 'KeyE') interact();
  if (e.code === 'Tab') $('minimap').classList.add('big');
  if (e.code === 'Space' && P.dropping) { P.chute = !P.chute; }
  if ((e.code === 'KeyC' || e.code === 'ControlLeft') && P.onGround && P.sprint && !P.slideT) startSlide();
});
addEventListener('keyup', e => { keys[e.code] = false; if (e.code === 'Tab') $('minimap').classList.remove('big'); });
addEventListener('mousedown', e => { if (state !== 'playing' || !locked) return; if (e.button === 0) { P.fireHeld = true; P.fireClick = true; } if (e.button === 2) P.adsHeld = true; });
addEventListener('mouseup', e => { if (e.button === 0) P.fireHeld = false; if (e.button === 2) P.adsHeld = false; });
addEventListener('contextmenu', e => e.preventDefault());
addEventListener('wheel', e => { if (state === 'playing') switchWeapon(P.cur ? 0 : 1); });
addEventListener('mousemove', e => {
  if (state !== 'playing' || !locked) return;
  const k = .0022 * (S.sens / 5) / (1 + P.ads * (curW().zoom - 1));
  P.yaw -= e.movementX * k; P.pitch = clamp(P.pitch - e.movementY * k * (S.invy ? -1 : 1), -1.5, 1.5);
});
addEventListener('blur', () => { if (state === 'playing') for (const k in keys) keys[k] = false; });

function switchWeapon(i) { if (P.cur === i || !P.wp[i] || P.dropping) return; P.cur = i; P.reloadT = 0; P.switchT = .35; setGunModel(P.wp[i].id); }
function reload() {
  const ws = P.wp[P.cur], w = curW(); if (P.reloadT > 0 || ws.ammo >= w.mag || ws.res <= 0 || P.dropping) return;
  P.reloadT = w.reload; sfx.reload();
}
function interact() {
  let best = null, bd = 9;
  for (const p of pickups) if (!p.taken && p.type === 'weapon' && !(p.noPick > 0)) { const d = (p.x - P.x) ** 2 + (p.z - P.z) ** 2 + (p.y - P.y) ** 2; if (d < bd) { bd = d; best = p; } }
  if (best) takePickup(best);
}
function startSlide() { const sp = Math.hypot(P.vx, P.vz); if (sp < 4) return; P.slideT = .9; P.slideSpd = 11.5; P.sdx = P.vx / sp; P.sdz = P.vz / sp; }

/* ---------------- Tir du joueur ---------------- */
let flashT = 0;
function fire() {
  const w = curW(), ws = P.wp[P.cur];
  ws.ammo--; P.fireCd = 60 / w.rpm;
  const moving = Math.hypot(P.vx, P.vz) > 1.5;
  let sp = lerp(w.spread, w.ads, P.ads) * (moving ? 1.5 : 1) * (P.onGround ? 1 : 2.2) * (P.crouch ? .7 : 1) + P.bloom;
  P.bloom = Math.min(.05, P.bloom + w.bloom);
  camera.updateMatrixWorld(true);
  const fwd = new THREE.Vector3(0, 0, -1).applyQuaternion(camera.quaternion), right = new THREE.Vector3(1, 0, 0).applyQuaternion(camera.quaternion), up = new THREE.Vector3(0, 1, 0).applyQuaternion(camera.quaternion);
  const o = camera.position, from = o.clone().addScaledVector(fwd, .9).addScaledVector(right, .2 * (1 - P.ads)).addScaledVector(up, -.14 * (1 - P.ads));
  for (let i = 0; i < w.pellets; i++) {
    const d = fwd.clone().addScaledVector(right, rnd(-sp, sp)).addScaledVector(up, rnd(-sp, sp)).normalize();
    const r = castRay(o.x, o.y, o.z, d.x, d.y, d.z, w.range, P);
    if (r.ent) {
      let dmg = w.dmg * (r.head ? (w.scope ? 2.5 : 1.8) : 1) * clamp(1 - (r.t - w.range * .5) / w.range, .35, 1);
      hurtEnt(r.ent, dmg, P, r.head);
    } else if (r.t < w.range) burst(r.x, r.y, r.z, 0xe9e2c8, 3, 2);
    if (i < 4 || w.pellets === 1) tracer(from, new THREE.Vector3(r.x, r.y, r.z));
  }
  sfx.shot(w);
  P.pitch += w.recoil * (P.ads > .5 ? .65 : 1) * rnd(.7, 1.2); P.yaw += rnd(-.5, .5) * w.recoil * .8; P.kick = Math.min(1, P.kick + 1);
  flashT = .05;
  // alerter les bots proches
  for (const b of bots) if (b.alive && !b.target && Math.hypot(b.x - P.x, b.z - P.z) < 60) b.thinkT = Math.min(b.thinkT, Math.random() * .3);
  if (ws.ammo <= 0) reload();
}

/* ---------------- Mise à jour du joueur ---------------- */
function updatePlayer(dt) {
  if (!P.alive) {
    if (mode === 'ffa' && P.respawnT > 0 && (P.respawnT -= dt) <= 0) { resetPlayer(); P.hp = 100; }
    return;
  }
  P.inv = Math.max(0, P.inv - dt); P.lastHurt += dt; P.fireCd -= dt; P.switchT -= dt;
  const w = curW(), ws = P.wp[P.cur];
  const fwdK = (keys.KeyW || keys.ArrowUp ? 1 : 0) - (keys.KeyS || keys.ArrowDown ? 1 : 0), strK = (keys.KeyD || keys.ArrowRight ? 1 : 0) - (keys.KeyA || keys.ArrowLeft ? 1 : 0);
  const sin = Math.sin(P.yaw), cos = Math.cos(P.yaw);
  let wx = -sin * fwdK + cos * strK, wz = -cos * fwdK - sin * strK; const wl = Math.hypot(wx, wz); if (wl > 0) { wx /= wl; wz /= wl; }
  // visée
  const wantAds = P.adsHeld && P.reloadT <= 0 && !P.dropping && P.switchT <= 0;
  P.ads = clamp(P.ads + (wantAds ? 1 : -1) * dt * (w.scope ? 5 : 8), 0, 1);

  if (P.dropping) {
    // parachutage
    if (P.y < 28) P.chute = true;
    const tgtVy = P.chute ? -7 : -38; P.vy = lerp(P.vy, tgtVy, 1 - Math.exp(-3 * dt));
    const sp = P.chute ? 10 : 16; P.vx = lerp(P.vx, wx * sp, 1 - Math.exp(-1.5 * dt)); P.vz = lerp(P.vz, wz * sp, 1 - Math.exp(-1.5 * dt));
    moveEnt(P, dt, false);
    if (P.onGround) { P.dropping = false; P.chute = false; banner('À toi de jouer !', 1500); }
  } else {
    P.sprint = !!(keys.ShiftLeft || keys.ShiftRight) && fwdK > 0 && P.ads < .2 && !P.crouch && P.onGround && !(P.fireHeld && ws.ammo > 0);
    const wantCrouch = !!(keys.KeyC || keys.ControlLeft) || P.slideT > 0;
    if (wantCrouch) { P.crouch = true; P.hh = 1.2; }
    else if (P.crouch && !collideAt(P.x, P.y, P.z, P.r, 1.8)) { P.crouch = false; P.hh = 1.8; }
    let speed = P.crouch ? 2.8 : P.sprint ? 8.4 : 5.4; speed *= lerp(1, .62, P.ads);
    if (P.slideT > 0) {
      P.slideT -= dt; P.slideSpd = Math.max(3, P.slideSpd - 13 * dt);
      P.vx = P.sdx * P.slideSpd; P.vz = P.sdz * P.slideSpd;
      if (keys.Space) { P.slideT = 0; P.vy = 7; }
    } else {
      const k = P.onGround ? 11 : 2.2, a = 1 - Math.exp(-k * dt);
      P.vx = lerp(P.vx, wx * speed, a); P.vz = lerp(P.vz, wz * speed, a);
    }
    if (keys.Space && P.onGround && !P.slideT) { P.vy = 8.2; P.onGround = false; }
    moveEnt(P, dt);
    if (P.onGround && wl > 0) P.bob += dt * (P.sprint ? 12 : P.crouch ? 6 : 8.5);
  }
  P.bloom = Math.max(0, P.bloom - dt * .05);
  // recharge
  if (P.reloadT > 0) { P.reloadT -= dt; if (P.reloadT <= 0) { const need = w.mag - ws.ammo, take = Math.min(need, ws.res); ws.ammo += take; ws.res -= take; } }
  // tir
  if (P.fireHeld && P.fireCd <= 0 && P.reloadT <= 0 && P.switchT <= 0 && !P.dropping && !(P.sprint && P.fireHeld === 'x')) {
    if (ws.ammo > 0) { if (w.auto || P.fireClick) { fire(); P.fireClick = false; } }
    else if (P.fireClick) { sfx.empty(); reload(); P.fireClick = false; }
  }
  if (!P.fireHeld) P.fireClick = false;
  P.kick = Math.max(0, P.kick - dt * 9);
  // pickups
  for (const p of pickups) {
    if (p.taken) continue; p.t += dt; p.mesh.rotation.y = p.t * 1.6; p.mesh.position.y = p.y + .6 + Math.sin(p.t * 3) * .1;
    if (p.noPick > 0) p.noPick -= dt;
    if (p.type !== 'weapon' && (p.x - P.x) ** 2 + (p.z - P.z) ** 2 < 2.2 && Math.abs(p.y - P.y) < 2.5) takePickup(p);
  }
}

/* ---------------- Caméra & vue arme ---------------- */
let eyeH = 1.65, fovCur = S.fov, bobT = 0;
function updateCamera(dt) {
  const tgtEye = P.alive ? P.hh - .15 : .4; eyeH = lerp(eyeH, tgtEye, 1 - Math.exp(-14 * dt));
  const w = curW();
  camera.position.set(P.x, P.y + eyeH + (P.onGround ? Math.sin(P.bob * 2) * .025 * (1 - P.ads) : 0), P.z);
  camera.rotation.set(P.pitch + (P.alive ? 0 : -.8), P.yaw, P.alive ? (P.slideT > 0 ? .06 : 0) : .6);
  const tgtFov = S.fov / lerp(1, w.zoom, P.ads) + (P.sprint ? 6 : 0) + (P.slideT > 0 ? 5 : 0) + (P.dropping && !P.chute ? 10 : 0);
  fovCur = lerp(fovCur, tgtFov, 1 - Math.exp(-12 * dt)); camera.fov = fovCur; camera.updateProjectionMatrix();
  // arme
  const scoped = w.scope && P.ads > .92; vm.visible = P.alive && !scoped && !P.dropping; $('scope').classList.toggle('hidden', !scoped);
  const hip = [.2, -.19, -.42], ads = [0, -.105, -.3], a = P.ads;
  let x = lerp(hip[0], ads[0], a), y = lerp(hip[1], ads[1], a), z = lerp(hip[2], ads[2], a);
  const bob = P.onGround ? Math.hypot(P.vx, P.vz) / 8 : 0;
  x += Math.cos(P.bob) * .012 * bob * (1 - a); y += Math.abs(Math.sin(P.bob)) * -.014 * bob * (1 - a);
  z += P.kick * .04; y += P.kick * .012;
  let rx = P.kick * .05, ry = 0, rz = 0;
  if (P.sprint) { rx = -.15; ry = .5; y -= .03; x += .05; }
  if (P.reloadT > 0) { const k = Math.sin(clamp(1 - P.reloadT / w.reload, 0, 1) * Math.PI); y -= .13 * k; rx -= .5 * k; rz = .4 * k; }
  if (P.switchT > 0) y -= clamp(P.switchT / .35, 0, 1) * .3;
  vm.position.set(x, y, z); vm.rotation.set(rx, ry, rz);
  if (flashT > 0 && gunMesh) { flashT -= dt; flash.visible = true; const m = gunMesh.userData.muzzle; const v = new THREE.Vector3(); vm.updateMatrixWorld(true); m.getWorldPosition(v); flash.position.copy(v); flash.rotation.z = Math.random() * 6; flash.scale.setScalar(rnd(.7, 1.3)); flash.lookAt(0, 0, 0); } else flash.visible = false;
  // soleil suit le joueur (ombres)
  sun.position.set(P.x + 60, 100, P.z + 40); sun.target.position.set(P.x, 0, P.z);
}

/* ---------------- HUD ---------------- */
const mm = $('minimap'), mmc = mm.getContext('2d');
const lastV = {}; const setT = (id, v) => { if (lastV[id] !== v) { lastV[id] = v; $(id).textContent = v; } };
function updateHud(dt) {
  setT('h-alive', mode === 'br' ? aliveCount() : bots.filter(b => b.alive).length + 1);
  setT('h-kills', P.kills);
  if (mode === 'br') { const ph = PHASES[Math.min(zone.phase, PHASES.length - 1)], rem = Math.max(0, Math.ceil(zone.state === 'wait' ? ph.wait - zone.t : ph.shrink - zone.t)); setT('h-zone', (zone.state === 'wait' ? 'Dans ' : 'Rétrécit ') + Math.floor(rem / 60) + ':' + String(rem % 60).padStart(2, '0')); }
  else { const rem = Math.max(0, Math.ceil(300 - matchT)); setT('h-zone', Math.floor(rem / 60) + ':' + String(rem % 60).padStart(2, '0') + ' · 🏆' + Math.max(P.kills, ...bots.map(b => b.kills || 0)) + '/20'); }
  $('b-health').style.width = P.hp + '%'; $('b-shield').style.width = P.sh + '%'; setT('t-health', Math.ceil(P.hp)); setT('t-shield', Math.ceil(P.sh));
  const w = curW(), ws = P.wp[P.cur];
  setT('a-mag', P.reloadT > 0 ? '↻' : ws.ammo); setT('a-res', ws.res); setT('a-name', w.name);
  for (let i = 0; i < 2; i++) { const el = $('ws' + i); el.className = 'wslot' + (i === P.cur ? ' act' : ''); const t = `${i + 1} · ${WEAPONS[P.wp[i].id].name}`; setT('ws' + i, t); }
  $('crosshair').style.setProperty('--sp', Math.round((lerp(w.spread, w.ads, P.ads) + P.bloom) * 500 + (Math.hypot(P.vx, P.vz) > 1.5 ? 6 : 0)) + 'px');
  $('crosshair').style.opacity = P.ads > .3 ? 0 : 1;
  // prompt arme
  let near = null, bd = 9; for (const p of pickups) if (!p.taken && p.type === 'weapon') { const d = (p.x - P.x) ** 2 + (p.z - P.z) ** 2 + (p.y - P.y) ** 2; if (d < bd) { bd = d; near = p; } }
  const pr = $('prompt'); if (near && P.alive && !P.dropping) { pr.style.display = 'block'; pr.textContent = `[E] Ramasser ${WEAPONS[near.wid].name}`; } else if (P.dropping) { pr.style.display = 'block'; pr.textContent = P.chute ? '🪂 Parachute ouvert — Espace pour couper' : '🪂 Espace : ouvrir le parachute'; } else pr.style.display = 'none';
  drawMinimap();
}
function drawMinimap() {
  const s = mm.width / (MAP * 2 + 20), c = mmc, W = mm.width; const X = x => (x + MAP + 10) * s, Z = z => (z + MAP + 10) * s;
  c.clearRect(0, 0, W, W); c.fillStyle = '#3c8f3a'; c.fillRect(0, 0, W, W);
  c.fillStyle = '#9aa3b8'; for (const r of rects) c.fillRect(X(r[0]), Z(r[1]), (r[2] - r[0]) * s, (r[3] - r[1]) * s);
  if (mode === 'br') {
    c.save(); c.beginPath(); c.rect(0, 0, W, W); c.arc(X(zone.cx), Z(zone.cz), zone.r * s, 0, 7, true); c.fillStyle = 'rgba(150,60,255,.45)'; c.fill('evenodd'); c.restore();
    c.strokeStyle = '#fff'; c.lineWidth = 2; if (zone.state === 'wait') { c.beginPath(); c.arc(X(zone.toX), Z(zone.toZ), zone.toR * s, 0, 7); c.stroke(); }
  }
  c.fillStyle = '#ff4d4d'; for (const b of bots) if (b.alive && b.lastShot < 1.5) { c.beginPath(); c.arc(X(b.x), Z(b.z), 3, 0, 7); c.fill(); }
  c.save(); c.translate(X(P.x), Z(P.z)); c.rotate(-P.yaw); c.fillStyle = '#fff'; c.strokeStyle = '#000'; c.beginPath(); c.moveTo(0, -8); c.lineTo(5, 6); c.lineTo(-5, 6); c.closePath(); c.fill(); c.stroke(); c.restore();
}

/* ---------------- Boucle principale ---------------- */
let last = performance.now(), fpsN = 0, fpsT = 0;
function frame(now) {
  requestAnimationFrame(frame);
  const dt = Math.min(.05, (now - last) / 1000); last = now;
  fpsN++; fpsT += dt; if (fpsT >= .5 && S.fps) { $('fps').classList.remove('hidden'); $('fps').textContent = Math.round(fpsN / fpsT) + ' FPS'; fpsN = 0; fpsT = 0; } else if (!S.fps) $('fps').classList.add('hidden');
  if (state === 'playing' || state === 'ended') {
    if (state === 'playing') { matchT += dt; updatePlayer(dt); if (mode === 'br') updateZone(dt); else ffaRules(); }
    bots.forEach((b, i) => updateBot(b, dt, i));
    updateFx(dt);
    if (state === 'playing') { updateCamera(dt); updateHud(dt); }
  } else if (state === 'menu' || state === 'paused') {
    // caméra qui tourne doucement derrière les menus
    if (!worldBuilt) { buildWorld(); }
    if (state === 'menu') { const t = now / 6000; camera.position.set(Math.cos(t) * 90, 40, Math.sin(t) * 90); camera.rotation.set(-.3, -t + Math.PI / 2, 0); camera.fov = 70; camera.updateProjectionMatrix(); vm.visible = false; sun.position.set(60, 100, 40); sun.target.position.set(0, 0, 0); }
  }
  renderer.clear(); renderer.render(scene, camera);
  if (state === 'playing' && vm.visible) { renderer.clearDepth(); renderer.render(vmScene, vmCam); }
}
function ffaRules() {
  const top = Math.max(P.kills, ...bots.map(b => b.kills || 0));
  if (top >= 20 || matchT >= 300) { const best = [P].concat(bots).sort((a, b) => (b.kills || 0) - (a.kills || 0))[0]; endGame(best === P); }
}

/* ---------------- Démarrage ---------------- */
bindSettings(); mainStats();
buildWorld();
$('loading').classList.add('hidden'); show('menu-main'); state = 'menu';
requestAnimationFrame(frame);
window.__bz = { P, bots, S, startGame, get state() { return state; }, zone, pickups, boxes, kill, hurtEnt };
})();
