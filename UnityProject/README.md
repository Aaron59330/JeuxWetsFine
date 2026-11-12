# BATTLE ZONE — version Unity

Le jeu complet (île avec relief, forêts, villages, armes, bots, tempête, menus) construit **par code** :
la scène ne contient qu'un seul objet, tout le reste est généré au lancement.
Compatible **Unity 2021.3 LTS, 2022.3 LTS et Unity 6** (Built-in, URP ou HDRP).

## Installation (5 minutes)

1. **Unity Hub ▸ Projets ▸ Nouveau projet.** Choisis le modèle **« 3D (Built-in Render Pipeline) »** (ou « Universal 3D »), nomme-le `BattleZone`, crée-le.
2. Dans l'explorateur de fichiers, copie le dossier **`Assets/BattleZone`** de ce dépôt dans le dossier **`Assets`** de ton projet Unity.
   (Chemin dans le dépôt : `UnityProject/Assets/BattleZone`.)
3. Retourne dans Unity et attends la fin de la compilation (barre de progression en bas à droite).
4. Menu du haut : **BattleZone ▸ 1 - Créer la scène et configurer le projet**.
   - Il crée la scène `Assets/Scenes/BattleZone.unity`, l'ajoute au build et règle le projet.
   - Si Unity propose de **redémarrer l'éditeur** (changement du système d'entrée), accepte.
5. Appuie sur **Play ▶**. La génération du monde prend quelques secondes, puis le menu apparaît.

Pour rouvrir la scène plus tard : **BattleZone ▸ 2 - Ouvrir la scène**.

## Contrôles
| Touche | Action |
|---|---|
| Z Q S D (ou W A S D) | Se déplacer |
| Maj | Sprint |
| Espace | Sauter / parachute |
| C ou Ctrl | Accroupi (en sprint : glissade) |
| Clic gauche / droit | Tirer / viser |
| R · 1 · 2 · molette | Recharger · changer d'arme |
| E | Ramasser une arme |
| Échap | Pause |

## Exporter un .exe
**File ▸ Build Settings** (la scène est déjà dedans) ▸ choisis *Windows* ▸ **Build**.

## Rendre le jeu plus réaliste
Le jeu génère lui-même des arbres et rochers simples. Pour un rendu vraiment réaliste, utilise des modèles faits par des artistes :
- **Arbres** : place tes prefabs (Asset Store, par ex. des packs « Nature »/« Forest » gratuits ou payants) dans `Assets/BattleZone/Resources/Trees`. Ils remplacent automatiquement les arbres générés.
- **Rochers** : même principe dans `Assets/BattleZone/Resources/Rocks`.
- **URP/HDRP** : avec un projet *Universal 3D*, ajoute un **Volume** de post-traitement (Bloom, Ambient Occlusion, Color Adjustments) pour un rendu plus « cinéma ».
- **Eau** : l'eau actuelle est un plan transparent simple. Unity 6 (URP/HDRP) propose un vrai système d'eau (*GameObject ▸ Water*) que tu peux ajouter par-dessus.
- **Armes / soldats** : remplace les modèles en cubes par tes propres modèles dans `WeaponSystem.BuildGunModel` et `BotAI.BuildVisual`.

## Si ça ne marche pas
- **`InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`** : *Edit ▸ Project Settings ▸ Player ▸ Other Settings ▸ Active Input Handling* ▸ **Both**, puis redémarre Unity.
- **Écran rose / matériaux roses** : projet URP/HDRP sans matériaux compatibles. Utilise plutôt le modèle *3D (Built-in)*, ou *Edit ▸ Rendering ▸ Materials ▸ Convert*.
- **Erreur de compilation (texte rouge dans la Console)** : copie le message complet et envoie-le-moi, je le corrige.
