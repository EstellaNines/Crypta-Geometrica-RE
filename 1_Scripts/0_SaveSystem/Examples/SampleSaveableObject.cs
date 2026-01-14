using System;
using UnityEngine;

/// <summary>
/// 示例：可保存对象
/// 展示如何实现 ISaveable 接口
/// </summary>
public class SampleSaveableObject : MonoBehaviour, ISaveable
{
    #region 组件引用

    [Header("必须组件")]
    [SerializeField, Tooltip("SaveableEntity 组件 (提供唯一ID)")]
    private SaveableEntity saveableEntity;

    #endregion

    #region 可保存的状态

    [Header("状态数据 (会被保存)")]
    [SerializeField] private int health = 100;
    [SerializeField] private bool isActive = true;
    [SerializeField] private Vector3 lastPosition;

    #endregion

    #region ISaveable 实现

    /// <summary>
    /// 返回唯一标识符
    /// </summary>
    public string SaveID => saveableEntity != null ? saveableEntity.ID : gameObject.name;

    /// <summary>
    /// 捕获当前状态
    /// 返回一个可序列化的对象
    /// </summary>
    public object CaptureState()
    {
        return new SampleState
        {
            health = this.health,
            isActive = this.isActive,
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z
        };
    }

    /// <summary>
    /// 恢复状态
    /// 参数是 JSON 字符串，需要反序列化
    /// </summary>
    public void RestoreState(object state)
    {
        if (state is string json)
        {
            var data = JsonUtility.FromJson<SampleState>(json);
            
            this.health = data.health;
            this.isActive = data.isActive;
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            
            Debug.Log($"[SampleSaveableObject] 状态已恢复: Health={health}, Active={isActive}");
        }
    }

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 确保有 SaveableEntity 组件
        if (saveableEntity == null)
        {
            saveableEntity = GetComponent<SaveableEntity>();
        }
    }

    private void OnEnable()
    {
        // 注册到 SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }

    private void OnDisable()
    {
        // 从 SaveManager 取消注册
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    #endregion

    #region 公共方法 (游戏逻辑)

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            isActive = false;
        }
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > 100) health = 100;
    }

    #endregion

    #region 状态数据结构

    /// <summary>
    /// 可序列化的状态数据
    /// </summary>
    [Serializable]
    private class SampleState
    {
        public int health;
        public bool isActive;
        public float posX;
        public float posY;
        public float posZ;
    }

    #endregion
}
