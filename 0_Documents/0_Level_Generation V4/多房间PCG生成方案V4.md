# **V4版多房间PCG生成方案架构深度研究报告：宏观拓扑与微观地形的异步构建策略**

## **1\. 执行摘要**

在现代2D动作游戏（Action-Roguelike）与地牢探索类（Dungeon Crawler）游戏的开发中，程序化内容生成（Procedural Content Generation, PCG）的核心挑战始终在于平衡**设计意图的可控性**与**生成结果的随机性**，同时在运行时维持极高的**性能稳定性**。本报告针对V4版多房间生成方案提出了系统性的架构设计，旨在解决传统单一生成脚本带来的逻辑耦合与主线程阻塞问题。

V4方案的核心创新在于实施了严格的“宏观-微观”二元分层架构：宏观层采用改进的“醉汉随机游走”（Drunkard’s Walk）算法在4x4网格中构建具有明确出入口逻辑的拓扑结构；微观层则基于策略模式（Strategy Pattern），利用ScriptableObject封装元胞自动机、柏林噪声与泊松盘采样等算法，实现房间内部地形的高度可配置化。

在工程实现层面，方案深度集成了Unity生态中的高性能工具链。通过**UniTask**实现跨线程的异步计算，将繁重的数学运算剥离出主线程；利用**Odin Inspector**的序列化能力构建可视化的调试管线；并针对**Tilemap**与**Composite Collider 2D**的物理构建瓶颈，提出了基于“时间切片”（Time-Slicing）与“批量提交”（Batching）的优化策略。本报告将从算法原理、数据结构设计、异步流水线构建及物理性能优化四个维度，对该方案进行详尽的技术论证与实施指导。

## ---

**2\. 架构范式：宏观布局与微观内容的解耦**

PCG系统的可维护性往往随着复杂度的增加而指数级下降。V4方案的首要设计原则是**关注点分离（Separation of Concerns）**，即将“关卡怎么走”（拓扑连接）与“关卡长什么样”（地形纹理）完全解耦。

### **2.1 宏观层（Macro Layer）：拓扑与连通性**

宏观层仅处理抽象的数据图结构（Graph）。在一个4x4的网格坐标系中，每一个节点（Node）仅存储元数据（Metadata），如房间类型（起始点、普通、BOSS房）、生物群落ID（Biome ID）以及与邻居的连接关系。此时不涉及任何具体的Tile（瓦片）或GameObject实例化。

这种轻量化的设计使得系统能够在毫秒级时间内生成成百上千种布局变体，并通过图论算法（如A\*或广度优先搜索BFS）快速验证地图的“可玩性”（Playability）。如果生成的布局无法打通入口到出口的路径，系统可以立即丢弃并重试，而无需承担销毁游戏对象的性能损耗。

### **2.2 微观层（Micro Layer）：策略与填充**

微观层专注于单个房间（例如40x40单元格）的内部构建。这一层不再关心全局地图的形状，而是聚焦于如何根据宏观层传递的配置（Config），在一个矩形边界内填充具体的墙壁、地面与装饰物。

为了实现高度的多样性，V4方案引入了**模块化生成管线**。房间生成器不再是一段硬编码的脚本，而是一个由多个可插拔的**算法策略（Algorithm Strategy）** 组成的流水线。例如，一个“沼泽房间”的生成流程可以由“噪声填充策略”+“平滑策略”+“植被散布策略”组合而成；而一个“矮人矿坑”则可以由“矩形挖掘策略”+“元胞自动机侵蚀策略”组成。

### **2.3 数据驱动的核心载体：ScriptableObject**

在Unity引擎中，实现上述解耦的最佳载体是**ScriptableObject (SO)**。SO不仅作为数据容器，更作为逻辑的载体 1。通过Odin Inspector的\`\`特性，我们可以在SO中序列化多态的算法策略列表 3，使得设计师无需修改代码即可组合出全新的房间生成逻辑。

## ---

**3\. 宏观生成逻辑：4x4网格中的醉汉游走与路径保障**

宏观生成的任务是在有限的4x4网格（共16个单元格）中，构建出一个既具有随机性又保证连通性的地牢结构。

### **3.1 醉汉随机游走算法（Drunkard's Walk）的适配**

在4x4这样的小型网格中，纯粹的随机选择极易导致生成的房间聚集成一团（2x2的方块）或形成一条单调的直线。为了构建具有探索感的“蜿蜒”路径，V4方案对标准醉汉游走算法进行了改良，引入了**方向惯性（Directional Bias）**。

#### **3.1.1 算法流程**

1. **初始化**：随机选择一个坐标 ![][image1] 作为起始点，标记为“已占用”。  
2. **游走步进**：游走者（Walker）决定下一步的移动方向（上、下、左、右）。  
   * **惯性机制**：为了防止游走者频繁回头或原地打转，算法设定了一个概率权重。例如，有50%的概率继续沿上一方向移动，25%转向左侧，25%转向右侧，0%掉头（除非无路可走）。这种机制在有限的4x4空间中能有效促使路径延伸，形成长走廊结构。  
3. **边界约束**：由于网格限制在4x4，任何超出 $$ 索引范围的移动都将被废弃并重试。  
4. **终止条件**：当已占用的房间数量达到预设阈值（例如8-12个房间）时停止。

### **3.2 核心路径与可达性验证**

仅仅生成连通的房间集合是不够的，必须确定\*\*入口（Entrance）**与**出口（Exit）\*\*的位置，并确保二者之间存在一条合理的关键路径（Critical Path）。

#### **3.2.1 距离计算与端点选择**

在游走结束后，系统将生成的数据转换为无向图。

1. **全源最短路径计算**：对所有已生成的房间节点，运行**Floyd-Warshall算法**（或针对稀疏图运行多次BFS），计算任意两点间的最短路径距离（即经过的房间数）。  
2. **直径判定**：选取距离最远的一对节点。通常将其中一个设为入口，另一个设为BOSS房或出口。这保证了玩家必须穿越尽可能多的房间才能完成关卡，最大化游戏内容的利用率。

#### **3.2.2 环路引入（Looping）**

单纯的随机游走容易产生大量的死胡同（Dead Ends）。为了提升关卡的连通性和回溯体验（Backtracking），宏观生成器会执行\*\*修剪与桥接（Pruning and Bridging）\*\*步骤。扫描所有相邻但未连接的房间对，以一定概率（如10%）强制打通墙壁，形成环路。这在Metroidvania（类银河恶魔城）设计中尤为重要，能有效减少玩家跑图的枯燥感。

### **3.3 数据结构设计**

宏观层的输出并非GameObject，而是一个纯C\#的数据对象DungeonGraph：

| 数据字段 | 类型 | 说明 |
| :---- | :---- | :---- |
| Nodes | Dictionary\<Vector2Int, DungeonNode\> | 存储所有房间节点的字典，Key为网格坐标。 |
| StartPos | Vector2Int | 入口的网格坐标。 |
| EndPos | Vector2Int | 出口的网格坐标。 |
| AdjacencyMatrix | bool\[,\] | 邻接矩阵，用于快速查找房间间的连通性。 |

该数据结构极其轻量，序列化后仅数百字节，非常适合用于存盘或网络传输。

## ---

**4\. 微观生成逻辑：基于策略模式的地形构建**

微观层接收宏观层传递的DungeonNode信息（包含种子Seed、房间类型、开口方向），并负责将其具象化为Tilemap数据。

### **4.1 策略模式（Strategy Pattern）与ScriptableObject架构**

为了满足“多种PCG算法集成”且“规则可配置”的需求，V4方案摒弃了传统的继承结构（如BaseRoomGenerator \-\> CaveGenerator），转而采用**组合模式**。

#### **4.1.1 核心接口定义**

C\#

public abstract class TerrainStrategySO : ScriptableObject  
{  
    // 异步执行，输入当前的瓦片数据，返回修改后的数据  
    public abstract UniTask ProcessAsync(  
        int tileData,   
        int width,   
        int height,   
        System.Random random,   
        Vector2Int gridPosition  
    );  
}

#### **4.1.2 房间配置资源（RoomProfileSO）**

通过Odin Inspector的\`\`功能，我们可以在ScriptableObject中定义一个多态列表：

C\#

\[CreateAssetMenu\]  
public class RoomProfileSO : ScriptableObject   
{  
     
    public List\<TerrainStrategySO\> WorkflowSteps;  
}

设计师可以在Inspector中像搭积木一样配置生成流程：

* Step 1: InitializeStrategy (全填充墙壁)  
* Step 2: DrunkardWalkDiggingStrategy (挖掘主要路径)  
* Step 3: CellularAutomataStrategy (平滑边缘)  
* Step 4: PoissonDiskScatterStrategy (放置装饰物)

这种架构极大地提升了系统的扩展性。若需要新增一种“熔岩河流”算法，程序员只需编写一个新的LavaRiverStrategySO类，设计师即可立即在任何房间配置中使用它，而无需修改核心生成代码。

### **4.2 核心算法集成详解**

#### **4.2.1 元胞自动机（Cellular Automata）**

元胞自动机是生成自然洞穴结构的首选算法 5。V4方案采用经典的\*\*“4-5规则”\*\*进行迭代演化。

* **初始化**：首先以约45%-50%的密度随机填充墙壁。  
* **演化迭代**：对每个细胞（Tile），统计其周围8个邻居（Moore Neighborhood）中墙壁的数量。  
  * 如果墙壁数 \> 4，则该细胞变为墙壁。  
  * 如果墙壁数 \< 4，则该细胞变为空地。  
  * 如果墙壁数 \== 4，保持状态不变。  
* **边界硬化**：为了防止玩家走出地图边界，迭代过程中强制将房间最外圈设为不可破坏的墙壁（除门口位置外）。  
* **性能优化**：为了避免频繁的二维数组访问带来的CPU开销，算法内部使用一维数组int\[width \* height\]进行扁平化存储，并利用位运算进行状态判断。

#### **4.2.2 柏林噪声（Perlin Noise）**

对于需要连续性纹理的地形（如草地、水洼、沼泽），柏林噪声提供了优于完全随机的自然梯度。

* **坐标映射问题**：Mathf.PerlinNoise(x, y)在相同的$(x, y)$输入下会输出相同的值。如果每个房间都使用局部坐标 ![][image2] 到 ![][image3] 采样，所有房间的地形将完全一致。  
* **全局偏移解决方案**：采样坐标必须加上房间在宏观网格中的偏移量：  
  ![][image4]  
  这确保了相邻房间的地形纹理（如一条横跨两个房间的河流）能够自然衔接，消除了“棋盘格”式的人工痕迹。

#### **4.2.3 泊松盘采样（Poisson Disk Sampling）**

在放置宝箱、怪物生成点或装饰性岩石时，单纯的随机位置会导致物体重叠或聚集，影响游戏体验。泊松盘采样算法 5 能生成一组随机点，同时保证任意两点间的距离不小于设定的半径 ![][image5]。

* **算法集成**：作为ScatterStrategySO的一部分，该策略首先生成采样点，然后检测这些点是否落在“空地”上（通过读取Tile数据）。  
* **多层级散布**：可以配置多个泊松盘策略串联运行。例如，先运行![][image6]的策略放置大型岩石，再运行![][image7]的策略在剩余空间放置碎石和草丛。

## ---

**5\. 异步计算与性能优化：UniTask全程护航**

在4x4网格下，每个房间若为50x50尺寸，总计需处理 ![][image8] 个Tile的数据。若加上复杂的元胞自动机迭代（假设迭代5次），总运算量级可达百万次。如果在Unity主线程的Update或Start中同步执行，将导致长达数秒的画面卡顿（Freeze）。V4方案利用**UniTask**库重构了整个生成管线。

### **5.1 线程模型设计**

Unity的API（如Tilemap.SetTile、Instantiate）只能在主线程调用，但纯数学运算（数组操作、噪声计算）是线程无关的。因此，生成管线被严格划分为**计算阶段**与**渲染阶段**。

#### **5.1.1 异步管线代码结构**

C\#

public async UniTask GenerateDungeonAsync(CancellationToken token) {  
    // 1\. 宏观生成（轻量级，主线程或后台均可）  
    var macroGraph \= MacroGenerator.BuildGraph();

    // 2\. 微观计算（重量级，切换到线程池）  
    await UniTask.SwitchToThreadPool();   
      
    var allRoomData \= new Dictionary\<Vector2Int, int\>();  
    foreach(var node in macroGraph.Nodes) {  
        // 在后台线程执行所有策略SO的ProcessAsync方法  
        // 这里没有任何Unity API调用，只有C\#数组操作  
        var rawData \= await node.Profile.ExecutePipelineAsync(node, token);  
        allRoomData.Add(node.GridPosition, rawData);  
    }

    // 3\. 渲染构建（切换回主线程）  
    await UniTask.SwitchToMainThread();  
      
    foreach(var kvp in allRoomData) {  
        // 分帧构建，避免单帧压力过大  
        await TilemapBuilder.BuildRoomAsync(kvp.Key, kvp.Value);  
    }  
}

通过UniTask.SwitchToThreadPool()，我们将最耗时的地形计算完全移出了主线程。玩家在加载界面看到的动画将保持丝滑流畅，因为主线程并未被阻塞。

### **5.2 垃圾回收（GC）优化**

在后台线程生成大量临时数组（如元胞自动机的中间状态数组）会产生严重的GC压力。虽然是在后台线程，但GC触发时会挂起所有线程（Stop-the-world）。

* **ArrayPool的应用**：利用System.Buffers.ArrayPool\<int\>来租赁（Rent）和归还（Return）计算所需的数组，而不是每次new int\[width \* height\]。这将内存分配压力降至最低。

## ---

**6\. 物理构建与Unity Tilemap深度集成**

计算出数据只是第一步，将其转化为游戏中的物理实体是性能瓶颈的高发区。研究资料表明，SetTiles的调用方式以及CompositeCollider2D的生成机制是主要的卡顿源头 7。

### **6.1 Tilemap 批量设置策略**

严禁使用Tilemap.SetTile(pos, tile)逐个设置瓦片 10。每次调用SetTile都会触发Tilemap的内部脏标记检查、网格重建以及可能的物理形状更新。 **V4方案要求**：必须构建完整的Vector3Int positions和TileBase tiles数组，然后调用Tilemap.SetTiles(positions, tiles)一次性提交。这能将渲染批次合并，并将网格重建开销降至![][image9]。

### **6.2 复合碰撞体（Composite Collider 2D）的性能陷阱**

CompositeCollider2D负责将大量独立的Tile碰撞体合并为优化的多边形，这对运行时性能至关重要，但其**生成过程**极其耗时。当调用SetTiles时，如果复合碰撞体处于激活状态，Unity会立即尝试重新计算多边形，导致巨大的主线程尖峰 9。

#### **6.2.1 优化方案：手动再生与时间切片**

1. **Generation Type设置为Manual**：在Prefab设置中，将CompositeCollider2D的Generation Type设为Manual 11。这意味着SetTiles不会自动触发物理重建。  
2. **异步分帧重建**：在所有Tile设置完毕后，通过代码显式调用GenerateGeometry()。  
   * **关键技巧**：不要在同一帧内对所有16个房间调用此方法。应使用UniTask进行**时间切片（Time-Slicing）**：

C\#  
foreach(var room in rooms) {  
    room.Tilemap.SetTiles(...);   
    room.CompositeCollider.GenerateGeometry(); // 显式调用  
    await UniTask.Yield(); // 暂停一帧，让出CPU给渲染  
}  
通过这种方式，原本可能导致500ms卡顿的操作被分散到了16帧中（约260ms），每帧仅占用数毫秒，用户感知完全无卡顿。

#### **6.2.2 Rule Tile 的预计算**

Rule Tile在放置时会进行邻居检查以决定使用哪个Sprite。如果运行时大量放置Rule Tile，这一计算开销不可忽视 12。

* **优化策略**：如果性能仍有瓶颈，可以将Rule Tile的逻辑前置到后台线程的计算阶段。即在C\#代码中模拟Rule Tile的邻居判断逻辑，直接算出具体的TileBase引用（例如直接引用“左上角墙壁”的Tile资源），从而跳过运行时的Rule Tile计算。

## ---

**7\. 可视化调试与工具链：Odin Inspector的应用**

PCG系统通常是一个“黑盒”，调试难度极大。V4方案利用Odin Inspector构建了透明化的调试环境。

### **7.1 宏观布局可视化**

在DungeonGenerator的Inspector中，使用Odin的\[OnInspectorGUI\]特性绘制4x4的网格预览。

* **实时反馈**：当设计师调整“惯性因子”或“房间数量”参数时，编辑器脚本实时运行宏观生成逻辑，并在Inspector中画出红绿色的方块图，直观展示布局的连通性和疏密程度。无需运行游戏即可验证拓扑算法的正确性。

### **7.2 策略管线调试**

在RoomProfileSO中，利用Odin的\`\`特性为每个策略添加“预览”功能。

* **单步调试**：设计师可以点击“Run Step 1”，Inspector下方会显示出初始化后的黑白纹理；接着点击“Run Step 2”，纹理更新为挖掘后的样子。这种所见即所得（WYSIWYG）的工作流极大地降低了参数调优的门槛。

### **7.3 多态序列化**

如前所述，\`\` 3 是实现策略模式资源化的关键。相比于Unity原生的序列化限制，Odin不仅支持多态列表的存储，还提供了优雅的下拉菜单来选择具体的策略类，使得添加新算法就像在Photoshop中添加滤镜一样简单。

## ---

**8\. 总结与展望**

V4版多房间PCG生成方案通过严谨的架构设计，成功解决了Unity 2D地牢生成中的核心痛点。

| 维度 | 传统方案 (V3) | V4 优化方案 | 收益 |
| :---- | :---- | :---- | :---- |
| **架构** | 单体脚本，逻辑耦合 | 宏观/微观分离，策略模式 | 极高的可维护性与扩展性 |
| **配置** | 参数硬编码或简单Inspector | ScriptableObject \+ Odin多态序列化 | 设计师友好的数据驱动工作流 |
| **计算** | 主线程同步执行 (卡顿) | UniTask后台线程异步计算 | 零卡顿的加载体验 |
| **物理** | 自动重构，帧率尖峰 | 手动触发，分帧切片 (Time-Slicing) | 平滑的运行时性能 |
| **数据** | 直接操作GameObject | 纯C\#数据结构 (Graph/Array) | 快速验证，易于存盘 |

该方案不仅满足了“宏观布局确保可达性”与“微观地形丰富多变”的功能需求，更在底层技术上充分利用了现代Unity开发的最佳实践（异步、批处理、内存池）。对于未来的V5版本，该架构已为引入更高级的特性（如波函数坍缩WFC、基于图语法的非欧几里得空间连接）奠定了坚实的数据与逻辑基础。通过严格遵循本报告所述的实施细节，开发团队构建出一套工业级的、可用于商业项目的程序化生成系统将成为可能。

#### **引用的著作**

1. How to Use Odin Inspector with Scriptable Objects, 访问时间为 一月 17, 2026， [https://odininspector.com/tutorials/using-attributes/how-to-use-odin-inspector-with-scriptable-objects](https://odininspector.com/tutorials/using-attributes/how-to-use-odin-inspector-with-scriptable-objects)  
2. I'm not sure that i'm following all the best practices, but Scriptable Objects is my love now : r/Unity3D \- Reddit, 访问时间为 一月 17, 2026， [https://www.reddit.com/r/Unity3D/comments/1lsbczg/im\_not\_sure\_that\_im\_following\_all\_the\_best/](https://www.reddit.com/r/Unity3D/comments/1lsbczg/im_not_sure_that_im_following_all_the_best/)  
3. Scripting API: SerializeReference \- Unity \- Manual, 访问时间为 一月 17, 2026， [https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html)  
4. Subclass Selector \- Community Made Tools, 访问时间为 一月 17, 2026， [https://odininspector.com/community-tools/5B5/subclass-selector](https://odininspector.com/community-tools/5B5/subclass-selector)  
5. Optimize performance of 2D games with Unity Tilemap, 访问时间为 一月 17, 2026， [https://unity.com/how-to/optimize-performance-2d-games-unity-tilemap](https://unity.com/how-to/optimize-performance-2d-games-unity-tilemap)  
6. Poisson-disc sampling in Unity \- Gregory Schlomoff, 访问时间为 一月 17, 2026， [http://gregschlom.com/devlog/2014/06/29/Poisson-disc-sampling-Unity.html](http://gregschlom.com/devlog/2014/06/29/Poisson-disc-sampling-Unity.html)  
7. Tilemap.SetTiles \- Unity \- Manual, 访问时间为 一月 17, 2026， [https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.SetTiles.html](https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.SetTiles.html)  
8. Rule Tile is slow \#327 \- Unity-Technologies/2d-extras \- GitHub, 访问时间为 一月 17, 2026， [https://github.com/Unity-Technologies/2d-extras/issues/327](https://github.com/Unity-Technologies/2d-extras/issues/327)  
9. Tilemap composite collider lag : r/Unity2D \- Reddit, 访问时间为 一月 17, 2026， [https://www.reddit.com/r/Unity2D/comments/1cwkno4/tilemap\_composite\_collider\_lag/](https://www.reddit.com/r/Unity2D/comments/1cwkno4/tilemap_composite_collider_lag/)  
10. Tilemap.SetTile(cellPosition, null) is pretty slow, right way? : r/Unity3D \- Reddit, 访问时间为 一月 17, 2026， [https://www.reddit.com/r/Unity3D/comments/cn2an1/tilemapsettilecellposition\_null\_is\_pretty\_slow/](https://www.reddit.com/r/Unity3D/comments/cn2an1/tilemapsettilecellposition_null_is_pretty_slow/)  
11. Composite Collider 2D component reference \- Unity \- Manual, 访问时间为 一月 17, 2026， [https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/collider/composite-collider/composite-collider-2d-reference.html](https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/collider/composite-collider/composite-collider-2d-reference.html)  
12. How Rule Tile can help you with level design \- Unity Tutorial \- YouTube, 访问时间为 一月 17, 2026， [https://www.youtube.com/watch?v=gj8SxK7kx7E](https://www.youtube.com/watch?v=gj8SxK7kx7E)  
13. Use Unity Rule Tile or Custom Algorithm for Tilemap? : r/Unity2D \- Reddit, 访问时间为 一月 17, 2026， [https://www.reddit.com/r/Unity2D/comments/fw9gl3/use\_unity\_rule\_tile\_or\_custom\_algorithm\_for/](https://www.reddit.com/r/Unity2D/comments/fw9gl3/use_unity_rule_tile_or_custom_algorithm_for/)

[image1]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAC0AAAAZCAYAAACl8achAAAClUlEQVR4Xu2WTahNURTHl1CEEClRIokoAx8hJUU9AwYMKDI2MKIoSiYGRkrJhGQgpRShDAxuvaGJASmlEBlIIuQjH//fW2d3913n3OPc1DuT+6t/7561znln7b0+9jEb0i4TC7XFZGlmNNbBAxekCdExjhDwNWsYAwFflC5HRwvMkY5ag8D3SM+lpdHREq+k9dGYc1j6IK2KjhYZkX4UfyvpSPekKcHeJnOlJ9J1aVLwjfFFOhGNGUyTedZdVLwehOnmNfsvqGca8oW0oNfl/JF2RGMBAVJf6Kt0VnokvZbeS7u7t9ZCEJThG+mzlcca/bQs2I5LP6XNwT7GW2lJNIqV0p3smgBZIAFcLX6TvibcNe+ZqdJ9aWPmW2FeomQhZ5v02zz4Ei+l+dEoNkm7susz5oEC9281D6IJ7DLltMa8HPPn9kvns+tEurcUNOnvF3QOu9AxL4n/4bR1Fw5k7Yr1bk6ib9DQJGhSSMCj0TEAs6WH1rvwNCWqynOdeR9VBv1JWh2N1p0SQArZoTyNW8x3oym8g3flC0+7GesZdpq/80h0AA6CyknlgI+GpFn5TTNy5J8qlI7aS4Uf7StskcXmU6dTXK+V3pk3WxX0EFkhyyX4R1WNcNB81D01b0q+TTg5H0snzYNPbJc+Wk23FxyQvkm3zf/vd/NZHEmbxqSpbHbGFnVFfUV4OA9ultUfKmSsLmjgeQ4YFsoiq8Zm6iGmTiU8zOr7nvMNoVTOmc/XCKXAwXLTfOfYiBvSL/P3RwiWCqCk+rLIPO17o6MhnHB8v1SNLjhkXmobpIXmzXfMerOYYPEPrHxqVsKn4LNobMg0abn1/wZmEvH5e8s8oLoRy31sYmOopRnROI4wr5lEQ4YMyl8BZnqqrawv7AAAAABJRU5ErkJggg==>

[image2]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACsAAAAXCAYAAACS5bYWAAACaElEQVR4Xu2WPWgVQRSFT9CAmoj4kwRBlFiIomBhpSRNSKGFgtoIQbBSC7ERFa0CYikIdoKIhRbRTgOCzQM7rQQljRaGSLCwEQyIv+dwdx7z7uysu3nwRPCDw9t3Znb2ztz5A/7TW/q90SP6qPXerGIL9dSbPULBXkTNwVpHPaPO+oKCA9RW1GwswypqT6FBVybU9u3iN4t6dY16TK12ZRuoGeoO9YT6RB3qqFGPFbB3H1L3iueyoN5Rx70Zs5v6QB10vgJXB14W/9WpU9TnUKEBythE9H8ndQXWZsw09Zoadn6bOeq689TIXeonNRn5Sl+L2hV5f+II9cubMO+M8zRAWjezsGmT8ANparUyNaJfqH2uTGk87LwqNBC5YNWWR/UXqe2+QCite523mXqPfLCXnVeF6ueCbSFdbMeQZrSNglJwMUqzFkG3wSqVSmmTYMeob0inCAao59Ra5ytABdptsGGONwk2fDv5RmjMv/A3p0EINpnPuWBzC0y7xH3YvKpL1QJTW377CsFedT5WwjbikRL/EazBeKcInfOjXcUU8sGWZSg7DcQC7F7gOQFrUB8LbKLewI7ngJ4fUEORFzMK+4YGIOYrtd95QoOj75Zuj+pFsvIKtCvo0FDASucSdamjBrCN+g47RHJoIeuUPE+dpt7CjnJPOIw0BUtvYS1Yyn3PAzrDFexRaqMrC2i7ueVNxw5YttSWnssImZt2fhud2/OwdC2Xc8hnpwm6n3yE3cxK0Wi9QEVvaqDMdNPZgC5ON5HuEB2Mw3YF3cCaomlywZvL5BXKF3uCenPSmz1iDXXDm/8kvwFkln6O4HuNNAAAAABJRU5ErkJggg==>

[image3]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAD4AAAAXCAYAAABTYvy6AAADGElEQVR4Xu2XTahNURTH1wvlM9+k5EUmQshACSMDBtRjQEmZMTRBmLySIZkoiV7IBLNHKdItBvJGREYK+cjARJHy+f+/fdZ96669973nXPcObt6//p331rr3/vbae529zxEZ17ioST7Qg+qDZ/tgMy2G7/pgD4qFH5GSizgTvgcf8gknTs4sHyzE+EZ4iU+0KbJOSp6nrFSBjF0srllxhk7Bw/AUl7PijwzBi3wCugG/hi/Dt+HtDdnqUtYbiXlzJPCU9VnSvFfwbh+0Wgm/h7f5hNMe+LfEA+FkjcDzi/85kV/gDfVPVJeyfOFkcYHIo8g6IIHnNQg/hxe4eF0v4dM+aMTZvwAfl3ggBHNltpoYVZOwEitcvIzIU5blKYsTYnnTJfA8i5PEPesOPNnlRvVL0q2iYrtchfslLpy7J2d/vYlRV+A/8A4XLyPylGV5yvoqaV6KxQX9CC/zCYptssYHje7DyyUMwBeusdRAWPgxF28lFkye/q7laSxXeIq1S+IOqcsXYzVRxnb6VOFsL7Z0aiBVCyfrvAReqnBlVSl8E/wDPugT0+CH8AyfKLRTxnb6VOEcQG4gVQsn66YEXqpwZeV4KZZ+J8rpxsCrVR98VhrP9VThnWr1HMvy2ml1LZz5BuUKJ4Tn4Af4bWH+zWJ4fQavlvzmdl3CZ3mPlVGOpTzL8oVzkchLsbTwEz7B+4rQhS4+QcL5xwlQcwPkec8rP89jh9+/JfGpUJN4gM2UYynPsjgZlqeLl2JlW516J+HxsJVSrU7thfe52Av4sYRHYWpt8f9hCQW0UqrVKbJYuOXNk8BTlhUnKHusckaiXc+IX9K2U/vV5EPQAwkD4tl5VELXqNiG3+GfkhmEUYpnWdzdyVPWNwk8L94CQxJuj+TbWk1CC5VZiZzYilskDGbA5az4TuBvi3ZEnrLmupxKO2HQxevibsoNZalPdFg8pq5J/GjZLfHd4xO8yidUnLEn0mRmOqTN8CX5t86qomH4nISWz4qD4u7ON7VuqB9+BK/ziS7qqZTbtEdnZr8P9qCmwmd88L/XX48ew1ZsKmi6AAAAAElFTkSuQmCC>

[image4]: <data:image/png;base64,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>

[image5]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAkAAAAaCAYAAABl03YlAAAAiUlEQVR4XmNgGAWkAgECfIYgIH4OxKuBWB+IDwHxCSAuBmJGkAJ+IF4DxOVA/B+IXwOxFRBbA/EBIOYBKXIB4llAPAmqKAMkCAS9QLwciFlAHE8gtgPiwwwQK5WgiviAmBvKhgOQKa3ogugApAhkKk4AchyyVVgBSHIrEHOgSyADBwZIEIwCCgAAxm8UgTb9h3UAAAAASUVORK5CYII=>

[image6]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAC0AAAAZCAYAAACl8achAAABWklEQVR4Xu2WvyuFURjHH2HiplCSVd1BJmWyKIlBKe7kX5DBoqz+A8pgkclisBksYlEWAzbFKNksyI/P13nfnPe4udfgfd9yPvUZznOe233ec5/zvNcsEon8NW3Yiy1BvD1Yl4p+vMVH3Eo8xSc/qWyo6CtzxW5iDTsyGSVERZ/jSLhRjx7L9lG4zoumix7FazzDKq7jCR6bKz5P0qJVk9pDzlhwESdwF5fxHbeThFe8x6Gv1G8M4/wvnDU3GX6igoc458XG8QU708B04h6+mXsIMYaDaVLBpBNlKty4xBscCOJlQKevdl0NN3T8Om0N96LQd6/hAy54cbXFEe54sU/UzythsAEb5j7XrH771SNtA+Xue/E+c4NC9y4TVHs0uiR5oAt4gN3JWkNBLxo9SGYEayZqahQxl0NUwyLemSv2Ap9xyU8SrVa+PyT69dXXk9gV7EUikch/4QPcy0RLgK8Z1gAAAABJRU5ErkJggg==>

[image7]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAC0AAAAZCAYAAACl8achAAABUklEQVR4Xu2WO0sDQRSFj6igWIgPENFSEBErISIGTGEROwv/g70itjb+AS1shJQ2WloIFqJlUkZIIyiIjYi94uPcvU68O5E1AdmsOB98kD0zAzPDnUuAQCCQFp10no7Rbm8skyzSGr2n7/SZbsZmZIgeekIrdMHkRfpGCybLDCP0Gnq75yYfp3d0x2QRQ7Qj4TsNeukxdNP7Jh+lt/TAZMhBT1imk3SXXtIL6ObTRB7gAOIXNkUf6ZoLlughXYeesAR9ra/0gU67id8wQ1dbcIUORyubZwL6IJ9suPzpEbTY5RBCHrqgnfTTM+jlSftr4IreQPtiVpAariLh8l6gt93lD7QJKdFTOmiyOfM7Qup5yw9/YA+6rllt+SUhG96mfV4e25/0RymPVh/JbyMdYwONh3XOfk3VjxLS78s+rh/7m3XKeB3pjX/iT0kgEAj8Mz4AV5pLA71OalIAAAAASUVORK5CYII=>

[image8]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAKUAAAAWCAYAAAChdVwBAAAFm0lEQVR4Xu2ZSYglRRCGY1DBZVwZHAeXbkQEUfDgMowgiigoooIIIwgeBBXEgwu4nQbEgzNzcRcXmjmI23gQF0TFeaioqOhFEVwOHpxBRURBQcUlvo6KflHx6tWraul+PXT+8POqIrOqIv+MzIzMJ1JQUFBQUFBQUFCwNDg2GxpwivJM5dpcsEzYoNykPFq5XyoDh2SD4lBptvP8WcrDckEA7aTNcP9UNm3g/83ZWCH6/X+wj5jeRyrXpDIHfhwn3bXspON65Tbln7kg4ADlvcq3lM8qv1feUKux9DhHubviv2L+5sDE/p2Yj48pv1b+IiasA3E3K39SPqn8Uvm2cjbUAQcpfxV7F6Q+OqwU0IZ/slFxv9T9pt8W4zd6fyWm46fKz5Vn1GqYlugykKGWs6EcoGP0ifpjfWIUENlE8ECsQzPodJz6TXlaZbtErC625QAj6xXlx8l+ofJv5bnBRhA+IObzVrE2RjBrfqD8SHl4sJ8v9Q4miP8I9w5sMcCnATrzeTH9c5/h2z3JRkD08Xtf5dPKPcrjg/0ksYCiHLiWUUeAjlvCPd/u7VNbUF4g1vEEhU+7RP6dymu9UgsmTdUHy2jgZDCTfyPmX0wbjqlsscEDaU8tPPh2ylBc4IK7wIjWpEf+3jRwq/JR5TMy6iN+X5Vs10s/v12LPHDXic2WHqiuZdQR5Gf5dm+f2oJyTsx+Vy7oiE+UJ2RjBaZ+lk3yxDYwM7wg5kcUgOewPRFsA2kPytvFntmR7LzrW+Wp0q4HtjfFBua08I7YgKQN0Uf3m5Uswle2rn57/YHUtfT3X1Tdu5YZ6MhS7VpSp82nRrR1AlGPnci/Q2xWY5b8S3ljqNeGi8Vyk9Ore4KR0f76Qo3FAVG+EOsgB0sa+c+VyhkxP7fLMPf0jhwXlIjl1016YKOsbSDR3it6kA3CuE1EBHVuq35BDsrYhggPgEl+OzzYBtIclJSD/H2Ha+datgUldRvRFpSet7wmw8SUaZkg/dErdQBTPUmwBzUj5IhajX5g9mXDw0lAxIdiweggocZ/BgHYm4Nyo9QHcg6KVReUcflmCd1Z2fsA8UnQXxVLkhcLniWouwwKbzwzKrnpcgTlUoA2o9vZwZaDYtUEJTsw7Jcnuzs0aSMTwRLKcnpZLugBZmtyyM9kfK4awRHG7xW53huDco3ybrEVhmtHDopVE5RzYnZ2Sw6EeaqydwHJNQ14pLqfEVv+Ny/U6AaCmvfEZB2BXCRv6DXD4vmkHJsn3n4fTxMAZdRZV90T+E3tw8YOtw3M4NTrypeVB84/2QxSHg6v6WAnA/J9see590Nr/I59BW6Rbn47XAt22q4HYKXhFITdOXAt88SUn6VOb5/agpKzQLb9cevu9ZsObjNYdp6T+mYDsDlhFxlHfhuoR5LPPwexc96Q4ShETL4TUwM/evCcGGFZyvNxhx9vuD/e7gxslE0b4/oM3/IxCx0f/Uafh5W7lCd6pQB0Qq9x55S+t3Atm84pH5Shljl+QPZpBDNiO1YaGAMH0HiSa0bIbGXbKJZrckA9CeRBW2T0veAosR38pMCknI1KnmFgPNTnGyxx8dyTzdXPUt8Q8S7OXn2m5rk5qeeo3u7ZYOP6JRnzT8Qyg4DwmTJqi9+xrwD30W+f4SCH5E24VEwj9AT0AdfYItAyrnj4go4nB1uOH36zTwugAbmTnTEHYFZ7T2z9v0+skzm47bJZuUnag473TcoNY46XiT3mSWykyDcZMDvE/uEhl4xAOHbl5JkcHb2o/EF5Xqwk1m6ev64i1wykacM3n5G01UFfRb/ZFEa/0etdsZlqV7BH0Gf8jYxGnBA8Xl1fHSuJaYmdmdC1bNIx+sRv9qmgYB4snaQ7BQUrAiybLN2bckFBwTTA0sym8SEZ/d+6oKCgoGDF4j8S+8wxr9AQzAAAAABJRU5ErkJggg==>

[image9]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAZCAYAAABD2GxlAAACW0lEQVR4Xu2WvWtVQRDFj5iIksSPKAnBBD+QVEKKgCJY2dgkQbSRSGobKwvFWgRBBAkG8g/YCRYiWlgs2AgWIiQIokVEFAQRArFQRM9h3ureyd6978kTBPODw+PN7MzO3Y+5F9jg36OXGvTGApuoXd7YCTupEWqzd2TYQd2jznhHARV4ARbbEQvUKvWeekt9p+ap7emgBE3wkLoCmzTHGPLxGq/YtorcRt2grlO7E/tJ6jP1ijqU2COXqKeoxqQo51dq2jtaKFY56h4Oo9Qz2ErVbZHOisb8cHYlfUkNO7uOxRDsmARYXF2BilWO26gp8j4swS3UDCA91F2sL3AvbPtLBJQLFMrxjjrgHULBOm+57Yuo8DuwsVsT+wx1OvmfI6C5QOXQGOWroOWVo3Z5W/Tj90RpgdeoieR/joDmAiepNVi+ClraD9RB73DoyTRJusWx6KZeFtBc4B5qGTb2F33UY+oJNZA6HFpZrbAm0TmJxAL1WyKgucCYS8oaS5PobOqMapKriV29Sy2iFCsC/rDAePBlLE2i260JlmCtI9LNFdQOaieDs+MybO91BupQf1SjPuLssUDfAz0BzQXqlboCO3LrUAF6S+xLbGq0s7CbNZ7YPRep497oeA4rcA72QZFDOb5R571DTFEfqS/UIuyqv27ZziXjchxDPmnalrxyK6md1C4d9o6IVky96Cx1itqPcl+M6KI8QLU3dopileMR7Hug63xC4cnbQLHKocX5K+jyvKGOekebKNZfwK6jr6AXqF60dtD4pnd51ziBzLu0wBbqpjdu8N/wE2M2fKKvuj/RAAAAAElFTkSuQmCC>