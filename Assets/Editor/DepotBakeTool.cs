using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Запекание нефтебазы в сцену: Tools → Нефтебаза.
public static class DepotBakeTool
{
    [MenuItem("Tools/Нефтебаза/Собрать в сцене")]
    public static void Bake()
    {
        RemoveExisting();
        DepotBuilder.Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Нефтебаза собрана в сцене. Не забудьте сохранить сцену (Ctrl+S).");
    }

    [MenuItem("Tools/Нефтебаза/Удалить из сцены")]
    public static void Remove()
    {
        RemoveExisting();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Нефтебаза удалена из сцены.");
    }

    static void RemoveExisting()
    {
        var existing = GameObject.Find("Depot");
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
