# DOTween API 完整参考文档

## 目录
1. [DOTween 简介](#dotween-简介)
2. [创建 Tweener](#创建-tweener)
3. [Transform 快捷方法](#transform-快捷方法)
4. [其他组件快捷方法](#其他组件快捷方法)
5. [Unity UI 快捷方法](#unity-ui-快捷方法)
6. [创建 Sequence](#创建-sequence)
7. [设置和回调](#设置和回调)
8. [控制方法](#控制方法)
9. [详细示例](#详细示例)

---

## DOTween 简介

**DOTween** 是 Unity 的高性能补间动画引擎 (HOTween v2)。

### 初始化
```csharp
DOTween.Init(); // 可选,通常自动完成
```

---

## 创建 Tweener

### A. 通用方式
```csharp
// 语法
DOTween.To(getter, setter, endValue, duration);

// 示例
DOTween.To(()=> myFloat, x=> myFloat = x, 100, 2f);
DOTween.To(()=> myVector, x=> myVector = x, new Vector3(10,5,0), 1.5f);

// FROM 补间
DOTween.To(()=> myFloat, x=> myFloat = x, 52, 1).From();
```

### B. 快捷方式
```csharp
// TO 补间
transform.DOMove(new Vector3(2,3,4), 1);
material.DOColor(Color.green, 1);

// FROM 补间(立即跳转到FROM位置)
transform.DOMove(new Vector3(2,3,4), 1).From();
```

---

## Transform 快捷方法

### 移动 (Move)
```csharp
DOMove(Vector3 endValue, float duration)           // 世界坐标移动
DOMoveX/Y/Z(float endValue, float duration)        // 单轴移动
DOLocalMove(Vector3 endValue, float duration)      // 本地坐标移动
DOLocalMoveX/Y/Z(float endValue, float duration)   // 本地单轴移动
DOJump(Vector3 endValue, float jumpPower, int numJumps, float duration)  // 跳跃
DOLocalJump(Vector3 endValue, float jumpPower, int numJumps, float duration)
```

### 旋转 (Rotate)
```csharp
DORotate(Vector3 endValue, float duration, RotateMode mode)
DOLocalRotate(Vector3 endValue, float duration, RotateMode mode)
DORotateQuaternion(Quaternion endValue, float duration)
DOLocalRotateQuaternion(Quaternion endValue, float duration)
DOLookAt(Vector3 towards, float duration, AxisConstraint axisConstraint)
```

**RotateMode**: `Fast`, `FastBeyond360`, `WorldAxisAdd`, `LocalAxisAdd`

### 缩放 (Scale)
```csharp
DOScale(Vector3 endValue, float duration)
DOScale(float endValue, float duration)  // 统一缩放
DOScaleX/Y/Z(float endValue, float duration)
```

### 冲击/震动 (Punch/Shake) - 无FROM版本
```csharp
DOPunchPosition(Vector3 punch, float duration, int vibrato, float elasticity)
DOPunchRotation(Vector3 punch, float duration, int vibrato, float elasticity)
DOPunchScale(Vector3 punch, float duration, int vibrato, float elasticity)

DOShakePosition(float duration, float/Vector3 strength, int vibrato, float randomness)
DOShakeRotation(float duration, float/Vector3 strength, int vibrato, float randomness)
DOShakeScale(float duration, float/Vector3 strength, int vibrato, float randomness)
```

### 路径 (Path) - 无FROM版本
```csharp
DOPath(Vector3[] path, float duration, PathType pathType, PathMode pathMode)
DOLocalPath(Vector3[] path, float duration, PathType pathType, PathMode pathMode)

// PathType: Linear, CatmullRom, CubicBezier
// 特殊方法
.SetOptions(bool closePath, AxisConstraint lockPosition, AxisConstraint lockRotation)
.SetLookAt(Vector3/Transform/float)
```

### 可混合补间 (Blendable)
```csharp
DOBlendableMoveBy(Vector3 byValue, float duration)
DOBlendableLocalMoveBy(Vector3 byValue, float duration)
DOBlendableRotateBy(Vector3 byValue, float duration, RotateMode rotateMode)
DOBlendableLocalRotateBy(Vector3 byValue, float duration, RotateMode rotateMode)
DOBlendableScaleBy(Vector3 byValue, float duration)
```

---

## 其他组件快捷方法

### Camera
```csharp
DOAspect(float endValue, float duration)
DOColor(Color endValue, float duration)
DOFarClipPlane(float endValue, float duration)
DOFieldOfView(float endValue, float duration)
DONearClipPlane(float endValue, float duration)
DOOrthoSize(float endValue, float duration)
DOPixelRect(Rect endValue, float duration)
DORect(Rect endValue, float duration)
DOShakePosition(float duration, float/Vector3 strength, int vibrato, float randomness)
DOShakeRotation(float duration, float/Vector3 strength, int vibrato, float randomness)
```

### Material
```csharp
DOColor(Color endValue, float duration)
DOColor(Color endValue, string property, float duration)
DOFade(float endValue, float duration)
DOGradientColor(Gradient gradient, float duration)
DOOffset(Vector2 endValue, float duration)
DOTiling(Vector2 endValue, float duration)
DOVector(Vector4 endValue, string property, float duration)
DOFloat(float endValue, string property, float duration)
DOBlendableColor(Color endValue, float duration)  // 可混合
```

### Rigidbody / Rigidbody2D
```csharp
DOMove(Vector3 endValue, float duration)
DOMoveX/Y/Z(float endValue, float duration)
DORotate(Vector3 endValue, float duration, RotateMode mode)
DOLookAt(Vector3 towards, float duration, AxisConstraint axisConstraint)
DOJump(Vector3 endValue, float jumpPower, int numJumps, float duration)
DOPath(Vector3[] path, float duration, PathType pathType, PathMode pathMode)
DOLocalPath(Vector3[] path, float duration, PathType pathType, PathMode pathMode)
```

### AudioSource
```csharp
DOFade(float endValue, float duration)    // volume
DOPitch(float endValue, float duration)   // pitch
DOSetFloat(string floatName, float endValue, float duration)  // AudioMixer
```

### Light
```csharp
DOColor(Color endValue, float duration)
DOIntensity(float endValue, float duration)
DOShadowStrength(float endValue, float duration)
DOBlendableColor(Color endValue, float duration)
```

### SpriteRenderer
```csharp
DOColor(Color endValue, float duration)
DOFade(float endValue, float duration)
DOBlendableColor(Color endValue, float duration)
```

### LineRenderer
```csharp
DOColor(Color2 startValue, Color2 endValue, float duration)
```

### TrailRenderer
```csharp
DOResize(float toStartWidth, float toEndWidth, float duration)
DOTime(float endValue, float duration)
```

---

## Unity UI 快捷方法

### CanvasGroup
```csharp
DOFade(float endValue, float duration)
```

### Graphic (Image/Text/RawImage基类)
```csharp
DOColor(Color endValue, float duration)
DOFade(float endValue, float duration)
DOBlendableColor(Color endValue, float duration)
```

### Image
```csharp
DOColor(Color endValue, float duration)
DOFade(float endValue, float duration)
DOFillAmount(float endValue, float duration)
DOGradientColor(Gradient gradient, float duration)
DOBlendableColor(Color endValue, float duration)
```

### Text
```csharp
DOColor(Color endValue, float duration)
DOFade(float endValue, float duration)
DOText(string endValue, float duration, bool richTextEnabled, ScrambleMode scrambleMode)
DOBlendableColor(Color endValue, float duration)
```

**ScrambleMode**: `None`, `All`, `Uppercase`, `Lowercase`, `Numerals`, `Custom`

### RectTransform
```csharp
DOAnchorPos(Vector2 endValue, float duration)
DOAnchorPosX/Y(float endValue, float duration)
DOAnchorPos3D(Vector3 endValue, float duration)
DOAnchorPos3DX/Y/Z(float endValue, float duration)
DOAnchorMax(Vector2 endValue, float duration)
DOAnchorMin(Vector2 endValue, float duration)
DOPivot(Vector2 endValue, float duration)
DOPivotX/Y(float endValue, float duration)
DOSizeDelta(Vector2 endValue, float duration)
DOPunchAnchorPos(Vector2 punch, float duration, int vibrato, float elasticity)
DOShakeAnchorPos(float duration, float/Vector2 strength, int vibrato, float randomness)
DOJumpAnchorPos(Vector2 endValue, float jumpPower, int numJumps, float duration)
DOShapeCircle(float endValue, float duration)  // 形状补间
```

### ScrollRect
```csharp
DONormalizedPos(Vector2 endValue, float duration)
DOHorizontalNormalizedPos(float endValue, float duration)
DOVerticalNormalizedPos(float endValue, float duration)
```

### Slider
```csharp
DOValue(float endValue, float duration)
```

### LayoutElement
```csharp
DOFlexibleSize(Vector2 endValue, float duration)
DOMinSize(Vector2 endValue, float duration)
DOPreferredSize(Vector2 endValue, float duration)
```

### Outline
```csharp
DOColor(Color endValue, float duration)
DOFade(float endValue, float duration)
```

---

## 创建 Sequence

```csharp
// 1. 创建序列
Sequence mySequence = DOTween.Sequence();

// 2. 添加补间
mySequence.Append(tween);              // 末尾添加
mySequence.Prepend(tween);             // 开头添加
mySequence.Insert(atPosition, tween);  // 指定位置插入
mySequence.Join(tween);                // 与上一个同时播放

// 添加回调和间隔
mySequence.AppendCallback(callback);
mySequence.AppendInterval(interval);
mySequence.PrependCallback(callback);
mySequence.PrependInterval(interval);
mySequence.InsertCallback(atPosition, callback);
```

### Sequence 示例
```csharp
Sequence seq = DOTween.Sequence();
seq.Append(transform.DOMoveX(45, 1))
   .Append(transform.DORotate(new Vector3(0,180,0), 1))
   .PrependInterval(1)
   .Insert(0, transform.DOScale(3, seq.Duration()));
```

---

## 设置和回调

### 链式设置
```csharp
SetAutoKill(bool autoKillOnCompletion)  // 默认true
SetId(int/string id)
SetTarget(object target)
SetLoops(int loops, LoopType loopType)  // -1无限循环
SetEase(Ease/AnimationCurve/EaseFunction ease)
SetRecyclable(bool recyclable)
SetUpdate(UpdateType updateType, bool isIndependentUpdate)
SetLink(GameObject target, LinkBehaviour linkBehaviour)
SetInverted()  // 反转播放
SetRelative()  // 相对值
SetAs(Tween/TweenParams)  // 复制设置
```

### LoopType
- `Restart` - 重新开始
- `Yoyo` - 来回播放
- `Incremental` - 递增(仅Tweener)

### UpdateType
- `Normal` - Update
- `Late` - LateUpdate
- `Fixed` - FixedUpdate
- `Manual` - 手动更新

### LinkBehaviour
- `KillOnDestroy` - 销毁时杀死
- `KillOnDisable` - 禁用时杀死
- `PauseOnDisable` - 禁用时暂停
- `PauseOnDisablePlayOnEnable` - 禁用暂停/启用播放
- `PauseOnDisableRestartOnEnable` - 禁用暂停/启用重启

### 回调
```csharp
OnComplete(callback)      // 完成时
OnKill(callback)          // 销毁时
OnPlay(callback)          // 播放时
OnPause(callback)         // 暂停时
OnRewind(callback)        // 倒回时
OnStart(callback)         // 开始时(延迟后)
OnStepComplete(callback)  // 每次循环完成
OnUpdate(callback)        // 每帧更新
OnWaypointChange(callback)  // 路径点变化
```

### Tweener特定设置
```csharp
From()  // FROM补间
From(bool isRelative)  // 相对FROM
SetDelay(float delay)  // 延迟
SetSpeedBased()  // 基于速度
SetOptions(...)  // 特定选项
```

---

## 控制方法

### 三种控制方式
```csharp
// A. 静态方法
DOTween.Play(id/target);
DOTween.Pause(id/target);
DOTween.Kill(id/target);

// B. 直接引用
myTween.Play();
myTween.Pause();
myTween.Kill();

// C. 快捷引用(带DO前缀)
transform.DOPlay();
transform.DOPause();
transform.DOKill();
```

### 控制方法列表
```csharp
Play()           // 播放
Pause()          // 暂停
Kill()           // 销毁
Complete()       // 立即完成
Rewind()         // 倒回开始
Restart()        // 重新开始
Goto(time)       // 跳转时间
PlayForward()    // 向前播放
PlayBackwards()  // 向后播放
TogglePause()    // 切换暂停
Flip()           // 翻转方向
SmoothRewind()   // 平滑倒回
```

### 获取信息
```csharp
Duration()            // 持续时间
Elapsed()             // 已播放时间
ElapsedPercentage()   // 已播放百分比
IsActive()            // 是否激活
IsBackwards()         // 是否向后
IsComplete()          // 是否完成
IsPlaying()           // 是否播放中
Loops()               // 循环次数
PathLength()          // 路径长度
```

### 全局控制
```csharp
DOTween.PlayAll()
DOTween.PauseAll()
DOTween.KillAll()
DOTween.CompleteAll()
DOTween.RestartAll()
DOTween.TotalPlayingTweens()
```

---

## 详细示例

### 基础移动
```csharp
transform.DOMove(new Vector3(10, 5, 0), 2f)
    .SetEase(Ease.InOutQuad)
    .OnComplete(() => Debug.Log("Done!"));
```

### 路径移动
```csharp
Vector3[] path = new Vector3[] {
    new Vector3(0, 0, 0),
    new Vector3(5, 2, 0),
    new Vector3(10, 0, 0)
};
transform.DOPath(path, 3f, PathType.CatmullRom)
    .SetOptions(true)  // 闭合路径
    .SetLookAt(0.01f)  // 朝向前方
    .SetEase(Ease.Linear)
    .SetLoops(-1);
```

### UI动画
```csharp
// 淡入
canvasGroup.DOFade(1, 0.5f).From(0);

// 打字机效果
dialogText.DOText("Hello World!", 2f, true, ScrambleMode.None);

// 进度条
healthBar.DOFillAmount(0.5f, 1f).SetEase(Ease.Linear);

// UI滑入
rectTransform.DOAnchorPosX(0, 0.5f).From(1000).SetEase(Ease.OutBack);
```

### 序列动画
```csharp
Sequence seq = DOTween.Sequence();
seq.Append(transform.DOMove(new Vector3(5, 0, 0), 1f))
   .Join(transform.DORotate(new Vector3(0, 180, 0), 1f))
   .Append(transform.DOScale(2f, 0.5f))
   .AppendInterval(0.5f)
   .Append(transform.DOMove(Vector3.zero, 1f))
   .OnComplete(() => Debug.Log("Sequence done!"));
```

### 震动效果
```csharp
// 相机震动
Camera.main.transform.DOShakePosition(0.5f, 0.3f, 20, 90);

// 受击效果
transform.DOPunchPosition(new Vector3(0.5f, 0, 0), 0.3f, 10, 1);
transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
```

### 材质动画
```csharp
// 颜色渐变
material.DOColor(Color.red, 1f);
material.DOColor(Color.green, "_SpecColor", 1f);

// 闪烁
material.DOColor(Color.red, 0.2f)
    .SetLoops(6, LoopType.Yoyo)
    .SetEase(Ease.Linear);

// 淡入淡出
material.DOFade(0, 1f);  // 淡出
material.DOFade(1, 1f);  // 淡入
```

### 协程支持
```csharp
// 等待补间完成
yield return myTween.WaitForCompletion();
yield return myTween.WaitForRewind();
yield return myTween.WaitForKill();
```

### Async/Await
```csharp
await myTween.AsyncWaitForCompletion();
```

---

## 常用模式

### 安全销毁
```csharp
Tweener myTween;

void Start() {
    myTween = transform.DOMove(target, 2f)
        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
}
```

### 可重用补间
```csharp
Tweener reusableTween = transform.DOMove(target, 2f)
    .SetAutoKill(false)
    .Pause();

void PlayAnimation() {
    reusableTween.Restart();
}
```

### 性能优化
```csharp
// 启用回收
DOTween.Init(true);

// 批量控制
DOTween.PauseAll();
DOTween.PlayAll();
```

---

**文档来源**: https://dotween.demigiant.com/documentation.php
**最后更新**: 2026-01-14
