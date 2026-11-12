# BATTLE ZONE

FPS de bataille en 3D (inspiré Warzone / Fortnite) qui se joue dans le navigateur.
Tout est généré par code (textures, sons, modèles) : aucun fichier externe, ça marche hors-ligne.

## Lancer le jeu
**Le plus simple :** double-clique sur `index.html` (Chrome, Edge ou Firefox récent).

**Ou avec un petit serveur local** (recommandé si le curseur ne se verrouille pas) :
```bash
python3 -m http.server 8000
```
puis ouvre http://localhost:8000

## Modes
- **Battle Royale** : largage en parachute, loot, tempête qui rétrécit, 20 bots (réglable), dernier survivant gagne.
- **Mêlée générale** : réapparition, premier à 20 éliminations ou 5 minutes.

## Contrôles
| Touche | Action |
|---|---|
| Z Q S D (ou W A S D) | Se déplacer |
| Maj | Sprint |
| Espace | Sauter / parachute |
| C / Ctrl | Accroupi — en sprint : glissade |
| Clic gauche / droit | Tirer / viser |
| R | Recharger |
| 1, 2 / molette | Changer d'arme |
| E | Ramasser une arme |
| Tab (maintenir) | Grande carte |
| Échap | Pause |

Menus : Jouer, Équipement (choix des armes), Options (sensibilité, FOV, volume, bots, difficulté), Aide, Pause, écran de fin.
