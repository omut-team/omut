using Omut.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Omut.EditorTools
{
    // Сборщик сцены Main. Меню Unity: Omut > Собрать сцену Main.
    // Добавляет в сцену системные объекты и интерфейс, если их там ещё нет.
    // Можно запускать сколько угодно раз: то, что уже есть, не дублируется.
    // Из командной строки: Unity.exe -batchmode -quit -projectPath game -executeMethod Omut.EditorTools.OmutSceneBuilder.BuildFromCommandLine
    public static class OmutSceneBuilder
    {
        public const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Omut/Собрать сцену Main")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            GameObject ui = EnsureRoot("UI");
            EnsureComponent<MainMenu>(ui);
            EnsureComponent<PauseMenu>(ui);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[OmutSceneBuilder] Сцена Main собрана");
        }

        public static void BuildFromCommandLine()
        {
            BuildMainScene();
        }

        public static GameObject EnsureRoot(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);
            return go;
        }

        public static GameObject EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
