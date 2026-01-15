using UnityEngine;

namespace CryptaGeometrica.LevelGeneration
{
    /// <summary>
    /// 难度配置 - 控制关卡难度和敌人数量
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "CryptaGeometrica/PCG/DifficultyConfig")]
    public class DifficultyConfig : ScriptableObject
    {
        [Header("关卡难度递增")]
        
        [Tooltip("基础难度系数 (第一关)")]
        [Range(0f, 1f)]
        public float BaseDifficulty = 0.2f;
        
        [Tooltip("每关卡难度增量")]
        [Range(0f, 0.5f)]
        public float DifficultyIncrement = 0.1f;
        
        [Tooltip("最大难度上限")]
        [Range(0f, 1f)]
        public float MaxDifficulty = 1.0f;
        
        [Header("敌人数量计算")]
        
        [Tooltip("基础敌人数量 (普通房间)")]
        [Range(1, 5)]
        public int BaseEnemyCount = 2;
        
        [Tooltip("每0.1难度增加的敌人数量")]
        [Range(0, 3)]
        public int EnemyPerDifficultyStep = 1;
        
        [Tooltip("单房间最大敌人数")]
        [Range(1, 15)]
        public int MaxEnemiesPerRoom = 8;
        
        [Header("房间类型敌人修正")]
        
        [Tooltip("关键路径房间敌人加成")]
        [Range(0, 3)]
        public int CriticalPathEnemyBonus = 1;
        
        [Tooltip("侧室敌人减少")]
        [Range(0, 2)]
        public int SideRoomEnemyReduction = 1;
        
        [Header("Boss房间设置")]
        
        [Tooltip("Boss房间敌人数量 (固定)")]
        [Range(1, 3)]
        public int BossRoomEnemyCount = 1;
        
        [Tooltip("Boss房间难度系数 (固定)")]
        [Range(0f, 1f)]
        public float BossRoomDifficulty = 1.0f;
        
        /// <summary>
        /// 计算指定关卡的难度系数
        /// </summary>
        /// <param name="levelIndex">关卡索引 (从0开始)</param>
        /// <returns>难度系数 (0.0-1.0)</returns>
        public float CalculateLevelDifficulty(int levelIndex)
        {
            float difficulty = BaseDifficulty + levelIndex * DifficultyIncrement;
            return Mathf.Clamp(difficulty, 0f, MaxDifficulty);
        }
        
        /// <summary>
        /// 计算房间内的难度系数
        /// </summary>
        /// <param name="levelDifficulty">关卡基础难度</param>
        /// <param name="roomType">房间类型</param>
        /// <param name="isCriticalPath">是否在关键路径上</param>
        /// <returns>房间难度系数</returns>
        public float CalculateRoomDifficulty(float levelDifficulty, RoomType roomType, bool isCriticalPath)
        {
            if (roomType == RoomType.Boss)
            {
                return BossRoomDifficulty;
            }
            
            if (roomType == RoomType.Start || roomType == RoomType.Shop)
            {
                return 0f;
            }
            
            float roomDifficulty = levelDifficulty;
            
            // 关键路径稍难
            if (isCriticalPath)
            {
                roomDifficulty += 0.1f;
            }
            
            // 侧室稍易
            if (roomType == RoomType.Side)
            {
                roomDifficulty -= 0.1f;
            }
            
            return Mathf.Clamp(roomDifficulty, 0f, MaxDifficulty);
        }
        
        /// <summary>
        /// 计算房间敌人数量
        /// </summary>
        /// <param name="roomDifficulty">房间难度系数</param>
        /// <param name="roomType">房间类型</param>
        /// <param name="isCriticalPath">是否在关键路径上</param>
        /// <returns>敌人数量</returns>
        public int CalculateEnemyCount(float roomDifficulty, RoomType roomType, bool isCriticalPath)
        {
            // Boss房间固定敌人数
            if (roomType == RoomType.Boss)
            {
                return BossRoomEnemyCount;
            }
            
            // 起点和商店无敌人
            if (roomType == RoomType.Start || roomType == RoomType.Shop)
            {
                return 0;
            }
            
            // 出口房间少量敌人
            if (roomType == RoomType.Exit)
            {
                return Mathf.Max(1, BaseEnemyCount / 2);
            }
            
            // 基础计算
            int difficultySteps = Mathf.FloorToInt(roomDifficulty / 0.1f);
            int enemyCount = BaseEnemyCount + difficultySteps * EnemyPerDifficultyStep;
            
            // 关键路径加成
            if (isCriticalPath)
            {
                enemyCount += CriticalPathEnemyBonus;
            }
            
            // 侧室减少
            if (roomType == RoomType.Side)
            {
                enemyCount -= SideRoomEnemyReduction;
            }
            
            return Mathf.Clamp(enemyCount, 1, MaxEnemiesPerRoom);
        }
        
        /// <summary>
        /// 获取难度描述文本
        /// </summary>
        /// <param name="difficulty">难度系数</param>
        /// <returns>描述文本</returns>
        public static string GetDifficultyDescription(float difficulty)
        {
            if (difficulty < 0.3f) return "简单";
            if (difficulty < 0.5f) return "普通";
            if (difficulty < 0.7f) return "困难";
            if (difficulty < 0.9f) return "噩梦";
            return "地狱";
        }
    }
}
