# 玩家状态机系统

## 概述
基于有限状态机（FSM）的玩家控制系统，支持待机、行走、跳跃（二段跳）、攻击、受伤、死亡六种状态。

## 文件结构
```
1_Scripts/2_Player/
├── MD_PlayerStateMachine.md    # 本文档
├── PlayerController.cs         # 主控制器
├── PlayerStateMachine.cs       # 状态机核心
├── PlayerState.cs              # 状态基类
├── PlayerAttackHandler.cs      # 攻击判定处理器
├── IDamageable.cs              # 可受伤接口
├── States/
│   ├── PlayerIdleState.cs      # 待机状态
│   ├── PlayerWalkState.cs      # 行走状态
│   ├── PlayerJumpState.cs      # 跳跃状态（二段跳）
│   ├── PlayerAttackState.cs    # 攻击状态
│   ├── PlayerHurtState.cs      # 受伤状态
│   └── PlayerDeadState.cs      # 死亡状态
└── UI/
    ├── PlayerStatusUI.cs       # 状态栏UI控制器
    └── PlayerStatusUIManager.cs # 状态栏UI管理器（GameManager模块）
```

## 架构
- **PlayerController**: 主控制器，处理输入、组件引用、状态机驱动
- **PlayerStateMachine**: 状态机核心，管理状态切换
- **PlayerState**: 状态基类
- **States/**: 具体状态实现

## 状态说明
| 状态 | 动画 | 说明 |
|------|------|------|
| Idle | Idle | 无输入时播放 |
| Walk | Walk | AD/方向键移动，X轴翻转 |
| Jump | Walk | W/↑跳跃，支持二段跳，使用Walk动画 |
| Attack | Attack | 鼠标左键/J攻击 |
| Hurt | - | 红白闪烁1秒→透明闪烁1秒→扣血+击退 |
| Dead | Dead | 生命值归零时播放 |

## 输入绑定
- 移动: WASD / 方向键
- 跳跃: W / ↑
- 攻击: 鼠标左键 / J

## 地面检测
使用 CapsuleCollider2D 进行地面检测

## 使用方法
1. 在玩家GameObject上添加 `PlayerController` 组件
2. 确保有以下组件：Animator、SpriteRenderer、Rigidbody2D、CapsuleCollider2D
3. 设置 Ground Layer 用于地面检测
4. Animator 中需要有动画：Idle、Walk、Jump、Attack、Dead

## 受伤触发
调用 `PlayerController.TakeDamage(Vector2 damageSource)` 触发受伤状态

## 开发进度
- [x] 状态机核心框架
- [x] 待机状态
- [x] 行走状态
- [x] 跳跃状态（二段跳）
- [x] 攻击状态（动画播放）
- [x] 受伤状态（闪烁+击退）
- [x] 死亡状态
- [x] 攻击判定（动画事件+OverlapBox）
- [x] IDamageable 接口
- [x] 状态机可视化编辑器窗口
- [x] 玩家状态栏UI（生命值、受伤反馈、眨眼动画）
