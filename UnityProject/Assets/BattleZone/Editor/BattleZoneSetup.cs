#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleZone.EditorTools
{
    /// <summary>Menu BattleZone : crée la scène du jeu et règle le projet en un clic.</summary>
    public static class BattleZoneSetup
    {
        const string ScenePath = "Assets/Scenes/BattleZone.unity";

        [MenuItem("BattleZone/1 - Créer la scène et configurer le projet")]
        public static void Setup()
        {
            bool restart = SetInputHandlerBoth();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<BattleZone.GameBootstrap>();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("BattleZone",
                restart
                    ? "Scène créée !\n\nLe mode d'entrée a été réglé sur 'Les deux' : Unity va demander de redémarrer l'éditeur. Accepte, puis appuie sur Play."
                    : "Scène créée !\n\nAppuie sur Play (▶) pour lancer le jeu.",
                "OK");
        }

        [MenuItem("BattleZone/2 - Ouvrir la scène")]
        public static void OpenScene()
        {
            if (!System.IO.File.Exists(ScenePath)) { Setup(); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        // Le jeu utilise l'ancien système d'entrée (Input.GetKey). Unity 6 le désactive par défaut : on active "Les deux".
        static bool SetInputHandlerBoth()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return false;
            var so = new SerializedObject(assets[0]);
            var p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue == 2) return false;   // 0 = ancien, 1 = nouveau, 2 = les deux
            p.intValue = 2; so.ApplyModifiedProperties();
            return true;
        }
    }
}
#endif
