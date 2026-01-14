/// <summary>
/// 全局消息类型定义
/// 按模块分类管理所有消息类型
/// </summary>
public enum MessageType
{
    // ========== 系统消息 ==========
    GAME_START,
    GAME_PAUSE,
    GAME_RESUME,
    GAME_OVER,
    SCENE_LOADED,

    // ========== 玩家消息 ==========
    PLAYER_SPAWN,
    PLAYER_MOVE,
    PLAYER_ATTACK,
    PLAYER_HURT,
    PLAYER_DEATH,
    PLAYER_RESPAWN,
    PLAYER_LEVEL_UP,

    // ========== UI消息 ==========
    UI_OPEN,
    UI_CLOSE,
    UI_REFRESH,

    // ========== 战斗消息 ==========
    ENEMY_SPAWN,
    ENEMY_DEATH,
    DAMAGE_DEALT,

    // ========== 道具消息 ==========
    ITEM_PICKUP,
    ITEM_USE,

    // ========== 音频消息 ==========
    AUDIO_PLAY_BGM,
    AUDIO_PLAY_SFX,
    AUDIO_STOP,

    // ========== 场景加载消息 ==========
    /// <summary>加载开始 - 参数: string (目标场景名)</summary>
    SCENE_LOADING_START,
    /// <summary>加载进度 - 参数: float (0.0~1.0)</summary>
    SCENE_LOADING_PROGRESS,
    /// <summary>加载完成 - 参数: string (已加载场景名)</summary>
    SCENE_LOADING_COMPLETED,

    // ========== 存档系统消息 ==========
    /// <summary>保存游戏请求 - 参数: int (槽位索引)</summary>
    SAVE_GAME_REQUEST,
    /// <summary>加载游戏请求 - 参数: int (槽位索引)</summary>
    LOAD_GAME_REQUEST,
    /// <summary>保存操作完成 - 参数: bool (是否成功), string (消息)</summary>
    SAVE_OPERATION_DONE,
    /// <summary>加载操作完成 - 参数: bool (是否成功), string (消息)</summary>
    LOAD_OPERATION_DONE,
    /// <summary>自动保存触发 - 参数: int (槽位索引)</summary>
    AUTO_SAVE_TRIGGERED,
    /// <summary>进入关卡 - 参数: string (关卡名)</summary>
    LEVEL_ENTERED,
    /// <summary>关卡通关 - 参数: string (关卡名)</summary>
    LEVEL_COMPLETED,
}
