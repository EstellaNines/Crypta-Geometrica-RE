using UnityEngine;

/// <summary>
/// 可保存实体组件
/// 为游戏对象提供唯一的 GUID 标识
/// 在编辑器模式下自动生成，运行时保持不变
/// </summary>
public class SaveableEntity : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string uniqueId;

    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string ID
    {
        get
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                GenerateId();
            }
            return uniqueId;
        }
    }

    /// <summary>
    /// 检查是否有有效的 ID
    /// </summary>
    public bool HasValidId => !string.IsNullOrEmpty(uniqueId);

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器模式下自动生成 GUID
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            GenerateId();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// 重置时生成新 ID
    /// </summary>
    private void Reset()
    {
        GenerateId();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 编辑器菜单：强制生成新 ID
    /// </summary>
    [ContextMenu("Generate New ID")]
    private void ForceGenerateNewId()
    {
        GenerateId();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[SaveableEntity] 生成新 ID: {uniqueId}");
    }
#endif

    /// <summary>
    /// 生成新的唯一 ID
    /// </summary>
    private void GenerateId()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 获取此对象上所有 ISaveable 组件的状态
    /// </summary>
    /// <returns>状态数据字典</returns>
    public SerializableDictionary<string, string> CaptureAllStates()
    {
        var states = new SerializableDictionary<string, string>();
        var saveables = GetComponents<ISaveable>();

        foreach (var saveable in saveables)
        {
            object state = saveable.CaptureState();
            if (state != null)
            {
                string json = JsonUtility.ToJson(state);
                string typeName = saveable.GetType().Name;
                states[typeName] = json;
            }
        }

        return states;
    }

    /// <summary>
    /// 恢复此对象上所有 ISaveable 组件的状态
    /// </summary>
    /// <param name="states">状态数据字典</param>
    public void RestoreAllStates(SerializableDictionary<string, string> states)
    {
        var saveables = GetComponents<ISaveable>();

        foreach (var saveable in saveables)
        {
            string typeName = saveable.GetType().Name;
            if (states.TryGetValue(typeName, out string json))
            {
                // 注意：这里需要知道具体类型才能反序列化
                // 实际使用时，ISaveable 实现类应该自己处理反序列化
                saveable.RestoreState(json);
            }
        }
    }
}
