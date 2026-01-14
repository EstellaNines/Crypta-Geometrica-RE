using UnityEngine;
using UnityEditor;

/// <summary>
/// GameManager 编辑器工具
/// 用于快速创建 GameManager 层级结构
/// </summary>
public class GameManagerCreator : Editor
{
    [MenuItem("GameObject/Game Framework/Create GameManager Structure", false, 10)]
    public static void CreateGameManagerStructure()
    {
        // 检查是否已存在 GameManager
        GameManager existingGM = FindObjectOfType<GameManager>();
        if (existingGM != null)
        {
            EditorUtility.DisplayDialog("提示", "场景中已存在 GameManager！", "确定");
            Selection.activeGameObject = existingGM.gameObject;
            return;
        }

        // 创建 GameManager 根节点
        GameObject gmRoot = new GameObject("[GameManager]");
        gmRoot.AddComponent<GameManager>();

        // 创建 AsyncSceneManager 子节点
        GameObject asyncSceneMgr = new GameObject("[AsyncSceneManager]");
        asyncSceneMgr.transform.SetParent(gmRoot.transform);
        asyncSceneMgr.AddComponent<AsyncSceneManager>();

        // 创建 SaveManager 子节点
        GameObject saveMgr = new GameObject("[SaveManager]");
        saveMgr.transform.SetParent(gmRoot.transform);
        saveMgr.AddComponent<SaveManager>();

        // 选中创建的对象
        Selection.activeGameObject = gmRoot;

        // 标记场景为已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[GameManagerCreator] GameManager 结构创建完成！");
        Debug.Log("  - [GameManager]");
        Debug.Log("    - [AsyncSceneManager]");
        Debug.Log("    - [SaveManager]");
    }

    [MenuItem("GameObject/Game Framework/Add Module to GameManager/AsyncSceneManager", false, 20)]
    public static void AddAsyncSceneManager()
    {
        AddModuleToGameManager<AsyncSceneManager>("[AsyncSceneManager]");
    }

    [MenuItem("GameObject/Game Framework/Add Module to GameManager/SaveManager", false, 21)]
    public static void AddSaveManager()
    {
        AddModuleToGameManager<SaveManager>("[SaveManager]");
    }

    /// <summary>
    /// 通用方法：向 GameManager 添加模块
    /// </summary>
    private static void AddModuleToGameManager<T>(string moduleName) where T : Component
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中不存在 GameManager！请先创建 GameManager 结构。", "确定");
            return;
        }

        // 检查是否已存在该模块
        T existingModule = gm.GetComponentInChildren<T>();
        if (existingModule != null)
        {
            EditorUtility.DisplayDialog("提示", $"GameManager 下已存在 {typeof(T).Name}！", "确定");
            Selection.activeGameObject = existingModule.gameObject;
            return;
        }

        // 创建模块子节点
        GameObject moduleObj = new GameObject(moduleName);
        moduleObj.transform.SetParent(gm.transform);
        moduleObj.AddComponent<T>();

        Selection.activeGameObject = moduleObj;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[GameManagerCreator] 已添加模块: {typeof(T).Name}");
    }
}
