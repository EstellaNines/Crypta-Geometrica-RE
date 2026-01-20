namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人状态接口
    /// 定义状态生命周期方法，所有敌人状态需实现此接口
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// 状态名称（用于调试和状态切换）
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// 初始化状态引用
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        /// <param name="stateMachine">状态机引用</param>
        void Initialize(EnemyController enemy, EnemyStateMachine stateMachine);

        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void Enter();

        /// <summary>
        /// 每帧更新（Update）
        /// </summary>
        void Update();

        /// <summary>
        /// 物理更新（FixedUpdate）
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        void Exit();

        /// <summary>
        /// 是否可被打断进入受伤/死亡状态
        /// </summary>
        /// <returns>true 表示可被打断</returns>
        bool CanBeInterrupted();
    }
}
