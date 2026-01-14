# Unity 2D 横版闯关 + Roguelike 游戏 API 参考

## 目录
1. [核心系统 API](#核心系统-api)
2. [2D 物理系统](#2d-物理系统)
3. [角色控制](#角色控制)
4. [Tilemaps 瓦片地图](#tilemaps-瓦片地图)
5. [动画系统](#动画系统)
6. [随机生成系统](#随机生成系统)
7. [战斗系统](#战斗系统)
8. [道具和库存系统](#道具和库存系统)
9. [UI 系统](#ui-系统)
10. [音频系统](#音频系统)
11. [存档系统](#存档系统)
12. [完整示例](#完整示例)

---

## 核心系统 API

### GameObject 和 Transform

#### GameObject
```csharp
// 创建对象
GameObject player = new GameObject("Player");
GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

// 查找对象
GameObject player = GameObject.Find("Player");
GameObject player = GameObject.FindGameObjectWithTag("Player");
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

// 激活/禁用
gameObject.SetActive(true);
bool isActive = gameObject.activeSelf;
bool isActiveInHierarchy = gameObject.activeInHierarchy;

// 销毁
Destroy(gameObject);
Destroy(gameObject, 2f); // 2秒后销毁
DestroyImmediate(gameObject); // 立即销毁(慎用)

// 标签和层级
gameObject.tag = "Enemy";
bool isPlayer = gameObject.CompareTag("Player");
gameObject.layer = LayerMask.NameToLayer("Enemy");

// 组件操作
Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
Health health = gameObject.GetComponent<Health>();
```

#### Transform
```csharp
// 位置
transform.position = new Vector3(0, 0, 0);
transform.localPosition = new Vector3(1, 0, 0);
transform.Translate(Vector3.right * speed * Time.deltaTime);

// 旋转
transform.rotation = Quaternion.Euler(0, 0, 45);
transform.localRotation = Quaternion.identity;
transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);

// 缩放
transform.localScale = new Vector3(1, 1, 1);
transform.localScale = Vector3.one * 2f;

// 层级关系
transform.parent = parentTransform;
transform.SetParent(parentTransform, worldPositionStays: true);
Transform child = transform.GetChild(0);
int childCount = transform.childCount;

// 方向
Vector3 forward = transform.right; // 2D中通常用right作为前方
Vector3 up = transform.up;

// 查找
Transform child = transform.Find("ChildName");
Transform child = transform.Find("Child/GrandChild");
```

---

## 2D 物理系统

### Rigidbody2D

```csharp
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Start()
    {
        // 基本设置
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        
        // 约束
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // 碰撞检测
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // 插值(平滑移动)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    
    void FixedUpdate()
    {
        // 移动方式1: 直接设置速度
        rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
        
        // 移动方式2: 施加力
        rb.AddForce(Vector2.right * force);
        rb.AddForce(Vector2.right * force, ForceMode2D.Impulse);
        
        // 移动方式3: MovePosition(推荐用于平台游戏)
        Vector2 newPos = rb.position + movement * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
        
        // 跳跃
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            // 或
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        
        // 获取速度
        float currentSpeed = rb.velocity.magnitude;
        Vector2 velocity = rb.velocity;
    }
}
```

### Collider2D

```csharp
// BoxCollider2D
BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
boxCollider.size = new Vector2(1f, 2f);
boxCollider.offset = new Vector2(0, 0.5f);
boxCollider.isTrigger = false;

// CircleCollider2D
CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
circleCollider.radius = 0.5f;
circleCollider.offset = Vector2.zero;

// CapsuleCollider2D
CapsuleCollider2D capsuleCollider = GetComponent<CapsuleCollider2D>();
capsuleCollider.size = new Vector2(1f, 2f);
capsuleCollider.direction = CapsuleDirection2D.Vertical;

// PolygonCollider2D
PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();

// 碰撞器通用属性
collider.enabled = true;
collider.isTrigger = false;
Bounds bounds = collider.bounds;
```

### Physics2D 射线检测

```csharp
public class GroundCheck : MonoBehaviour
{
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    
    bool isGrounded;
    
    void Update()
    {
        // 方式1: OverlapCircle
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, 
            groundCheckRadius, 
            groundLayer
        );
        
        // 方式2: Raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            1f, 
            groundLayer
        );
        isGrounded = hit.collider != null;
        
        // 方式3: BoxCast
        RaycastHit2D boxHit = Physics2D.BoxCast(
            transform.position,
            new Vector2(0.8f, 0.1f),
            0f,
            Vector2.down,
            0.1f,
            groundLayer
        );
        
        // 方式4: CircleCast
        RaycastHit2D circleHit = Physics2D.CircleCast(
            transform.position,
            0.5f,
            Vector2.down,
            1f,
            groundLayer
        );
        
        // 获取所有碰撞
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position,
            Vector2.down,
            10f
        );
        
        // OverlapBox
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position,
            new Vector2(2f, 2f),
            0f,
            enemyLayer
        );
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
```

### 物理材质

```csharp
// 创建物理材质
PhysicsMaterial2D material = new PhysicsMaterial2D();
material.friction = 0.4f;      // 摩擦力
material.bounciness = 0.5f;    // 弹性

// 应用到碰撞器
GetComponent<Collider2D>().sharedMaterial = material;
```

---

## 角色控制

### 2D 平台角色控制器

```csharp
using UnityEngine;

public class PlatformerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float deceleration = 10f;
    
    [Header("跳跃")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;
    [SerializeField] int maxJumps = 2;
    
    [Header("地面检测")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;
    
    [Header("墙壁检测")]
    [SerializeField] Transform wallCheck;
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] LayerMask wallLayer;
    
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    
    float horizontal;
    bool isGrounded;
    bool isTouchingWall;
    int jumpsRemaining;
    bool facingRight = true;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        // 输入
        horizontal = Input.GetAxisRaw("Horizontal");
        
        // 地面检测
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, 
            groundCheckRadius, 
            groundLayer
        );
        
        // 墙壁检测
        isTouchingWall = Physics2D.Raycast(
            wallCheck.position,
            transform.right * (facingRight ? 1 : -1),
            wallCheckDistance,
            wallLayer
        );
        
        // 重置跳跃次数
        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
        
        // 跳跃
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            Jump();
        }
        
        // 更好的跳跃手感
        if (rb.velocity.y < 0)
        {
            // 下落时增加重力
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            // 松开跳跃键时减小上升速度
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
        
        // 翻转
        if (horizontal > 0 && !facingRight)
        {
            Flip();
        }
        else if (horizontal < 0 && facingRight)
        {
            Flip();
        }
        
        // 动画
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        // 移动
        float targetSpeed = horizontal * moveSpeed;
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate;
        
        rb.AddForce(movement * Vector2.right);
        
        // 限制最大速度
        if (Mathf.Abs(rb.velocity.x) > moveSpeed)
        {
            rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * moveSpeed, rb.velocity.y);
        }
    }
    
    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpsRemaining--;
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
        // 或
        // spriteRenderer.flipX = !facingRight;
    }
    
    void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontal));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VelocityY", rb.velocity.y);
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + transform.right * wallCheckDistance
            );
        }
    }
}
```

---

## Tilemaps 瓦片地图

### Tilemap 基础

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    [SerializeField] Tilemap tilemap;
    [SerializeField] Tile grassTile;
    [SerializeField] Tile wallTile;
    
    void Start()
    {
        // 设置单个瓦片
        Vector3Int position = new Vector3Int(0, 0, 0);
        tilemap.SetTile(position, grassTile);
        
        // 获取瓦片
        TileBase tile = tilemap.GetTile(position);
        
        // 删除瓦片
        tilemap.SetTile(position, null);
        
        // 清空所有瓦片
        tilemap.ClearAllTiles();
        
        // 批量设置瓦片
        BoundsInt bounds = new BoundsInt(-10, -10, 0, 20, 20, 1);
        TileBase[] tileArray = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
        for (int i = 0; i < tileArray.Length; i++)
        {
            tileArray[i] = grassTile;
        }
        tilemap.SetTilesBlock(bounds, tileArray);
        
        // 世界坐标转瓦片坐标
        Vector3 worldPos = transform.position;
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        
        // 瓦片坐标转世界坐标
        Vector3 worldPosFromCell = tilemap.CellToWorld(cellPos);
        
        // 获取瓦片中心点
        Vector3 centerPos = tilemap.GetCellCenterWorld(cellPos);
    }
    
    // 检查位置是否有瓦片
    public bool HasTile(Vector3Int position)
    {
        return tilemap.HasTile(position);
    }
    
    // 获取所有瓦片位置
    public void GetAllTiles()
    {
        BoundsInt bounds = tilemap.cellBounds;
        
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                TileBase tile = tilemap.GetTile(pos);
                // 处理瓦片
            }
        }
    }
}
```

### 程序化生成地图

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralMapGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap wallTilemap;
    [SerializeField] Tile groundTile;
    [SerializeField] Tile wallTile;
    
    [SerializeField] int width = 50;
    [SerializeField] int height = 30;
    [SerializeField] float fillPercent = 45f;
    [SerializeField] int smoothIterations = 5;
    
    int[,] map;
    
    void Start()
    {
        GenerateMap();
    }
    
    void GenerateMap()
    {
        // 初始化随机地图
        map = new int[width, height];
        RandomFillMap();
        
        // 平滑处理
        for (int i = 0; i < smoothIterations; i++)
        {
            SmoothMap();
        }
        
        // 绘制到Tilemap
        DrawMap();
    }
    
    void RandomFillMap()
    {
        System.Random random = new System.Random();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 边界设为墙
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (random.Next(0, 100) < fillPercent) ? 1 : 0;
                }
            }
        }
    }
    
    void SmoothMap()
    {
        int[,] newMap = new int[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);
                
                if (neighborWallTiles > 4)
                    newMap[x, y] = 1;
                else if (neighborWallTiles < 4)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = map[x, y];
            }
        }
        
        map = newMap;
    }
    
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        
        for (int x = gridX - 1; x <= gridX + 1; x++)
        {
            for (int y = gridY - 1; y <= gridY + 1; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (x != gridX || y != gridY)
                    {
                        wallCount += map[x, y];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        
        return wallCount;
    }
    
    void DrawMap()
    {
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                if (map[x, y] == 1)
                {
                    wallTilemap.SetTile(pos, wallTile);
                }
                else
                {
                    groundTilemap.SetTile(pos, groundTile);
                }
            }
        }
    }
}
```

---

## 动画系统

### Animator 控制

```csharp
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    Animator animator;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // 设置参数
        animator.SetFloat("Speed", Mathf.Abs(horizontal));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetInteger("Health", currentHealth);
        
        // 触发器
        if (Input.GetButtonDown("Fire1"))
        {
            animator.SetTrigger("Attack");
        }
        
        // 播放动画
        animator.Play("Idle");
        animator.Play("Run", 0, 0.5f); // 从50%位置开始播放
        
        // 交叉淡入淡出
        animator.CrossFade("Jump", 0.2f);
        
        // 获取当前状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isPlaying = stateInfo.IsName("Attack");
        float normalizedTime = stateInfo.normalizedTime; // 0-1
        
        // 动画事件(在动画中设置)
        // 会调用脚本中的同名方法
    }
    
    // 动画事件方法
    void OnAttackHit()
    {
        // 在攻击动画的特定帧调用
        DealDamage();
    }
    
    void OnFootstep()
    {
        // 播放脚步声
        PlayFootstepSound();
    }
}
```

---

## 随机生成系统

### Random 和 Roguelike 生成

```csharp
using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Room
    {
        public Vector2Int position;
        public Vector2Int size;
        public List<Vector2Int> doors;
        
        public Room(Vector2Int pos, Vector2Int sz)
        {
            position = pos;
            size = sz;
            doors = new List<Vector2Int>();
        }
        
        public bool Overlaps(Room other)
        {
            return !(position.x + size.x < other.position.x ||
                    position.x > other.position.x + other.size.x ||
                    position.y + size.y < other.position.y ||
                    position.y > other.position.y + other.size.y);
        }
    }
    
    [SerializeField] int numRooms = 10;
    [SerializeField] Vector2Int minRoomSize = new Vector2Int(5, 5);
    [SerializeField] Vector2Int maxRoomSize = new Vector2Int(10, 10);
    [SerializeField] int maxAttempts = 100;
    
    List<Room> rooms = new List<Room>();
    
    void Start()
    {
        GenerateDungeon();
    }
    
    void GenerateDungeon()
    {
        // 生成房间
        for (int i = 0; i < numRooms; i++)
        {
            bool roomPlaced = false;
            int attempts = 0;
            
            while (!roomPlaced && attempts < maxAttempts)
            {
                Vector2Int size = new Vector2Int(
                    Random.Range(minRoomSize.x, maxRoomSize.x),
                    Random.Range(minRoomSize.y, maxRoomSize.y)
                );
                
                Vector2Int position = new Vector2Int(
                    Random.Range(0, 100),
                    Random.Range(0, 100)
                );
                
                Room newRoom = new Room(position, size);
                
                bool overlaps = false;
                foreach (Room room in rooms)
                {
                    if (newRoom.Overlaps(room))
                    {
                        overlaps = true;
                        break;
                    }
                }
                
                if (!overlaps)
                {
                    rooms.Add(newRoom);
                    roomPlaced = true;
                }
                
                attempts++;
            }
        }
        
        // 连接房间
        ConnectRooms();
        
        // 生成敌人和道具
        SpawnEnemiesAndItems();
    }
    
    void ConnectRooms()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2Int start = rooms[i].position + rooms[i].size / 2;
            Vector2Int end = rooms[i + 1].position + rooms[i + 1].size / 2;
            
            CreateCorridor(start, end);
        }
    }
    
    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        
        // L型走廊
        while (current.x != end.x)
        {
            current.x += (end.x > current.x) ? 1 : -1;
            // 在此位置放置地板瓦片
        }
        
        while (current.y != end.y)
        {
            current.y += (end.y > current.y) ? 1 : -1;
            // 在此位置放置地板瓦片
        }
    }
    
    void SpawnEnemiesAndItems()
    {
        foreach (Room room in rooms)
        {
            // 随机生成敌人
            int enemyCount = Random.Range(1, 4);
            for (int i = 0; i < enemyCount; i++)
            {
                Vector2 spawnPos = new Vector2(
                    Random.Range(room.position.x, room.position.x + room.size.x),
                    Random.Range(room.position.y, room.position.y + room.size.y)
                );
                
                // Instantiate enemy
            }
            
            // 随机生成道具
            if (Random.value < 0.3f) // 30%概率
            {
                Vector2 itemPos = new Vector2(
                    Random.Range(room.position.x, room.position.x + room.size.x),
                    Random.Range(room.position.y, room.position.y + room.size.y)
                );
                
                // Instantiate item
            }
        }
    }
}

// 随机工具类
public static class RandomUtils
{
    // 加权随机选择
    public static T WeightedRandom<T>(List<T> items, List<float> weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < items.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return items[i];
            }
        }
        
        return items[items.Count - 1];
    }
    
    // 随机点在圆内
    public static Vector2 RandomPointInCircle(float radius)
    {
        return Random.insideUnitCircle * radius;
    }
    
    // 随机点在球内
    public static Vector3 RandomPointInSphere(float radius)
    {
        return Random.insideUnitSphere * radius;
    }
}
```

---

## 战斗系统

### 生命值系统

```csharp
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] int maxHealth = 100;
    [SerializeField] int currentHealth;
    [SerializeField] bool isInvulnerable = false;
    [SerializeField] float invulnerabilityDuration = 1f;
    
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnDamageTaken;
    
    float invulnerabilityTimer;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    void Update()
    {
        if (invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvulnerable || invulnerabilityTimer > 0)
            return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth);
        OnDamageTaken?.Invoke();
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            invulnerabilityTimer = invulnerabilityDuration;
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    void Die()
    {
        OnDeath?.Invoke();
        // Destroy(gameObject);
    }
    
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
}
```

### 攻击系统

```csharp
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] Transform attackPoint;
    [SerializeField] float attackRange = 0.5f;
    [SerializeField] LayerMask enemyLayers;
    [SerializeField] int damage = 10;
    [SerializeField] float attackCooldown = 0.5f;
    
    float nextAttackTime = 0f;
    Animator animator;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }
    
    void Attack()
    {
        // 播放攻击动画
        animator.SetTrigger("Attack");
        
        // 检测敌人
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position, 
            attackRange, 
            enemyLayers
        );
        
        // 对每个敌人造成伤害
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Health>(out Health health))
            {
                health.TakeDamage(damage);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
```

---

## 道具和库存系统

### 道具系统

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
    public ItemType type;
    
    public virtual void Use()
    {
        Debug.Log($"Using {itemName}");
    }
}

public enum ItemType
{
    Consumable,
    Weapon,
    Armor,
    Quest
}

public class Consumable : Item
{
    public int healthRestore;
    public int manaRestore;
    
    public override void Use()
    {
        // 恢复生命值
        FindObjectOfType<Health>().Heal(healthRestore);
    }
}
```

### 库存系统

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    
    [SerializeField] int maxSlots = 20;
    public List<ItemSlot> items = new List<ItemSlot>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public bool AddItem(Item item, int amount = 1)
    {
        // 查找现有堆叠
        ItemSlot existingSlot = items.Find(slot => 
            slot.item == item && slot.amount < item.maxStack
        );
        
        if (existingSlot != null)
        {
            existingSlot.amount += amount;
            return true;
        }
        
        // 添加新槽位
        if (items.Count < maxSlots)
        {
            items.Add(new ItemSlot(item, amount));
            return true;
        }
        
        return false; // 库存已满
    }
    
    public void RemoveItem(Item item, int amount = 1)
    {
        ItemSlot slot = items.Find(s => s.item == item);
        if (slot != null)
        {
            slot.amount -= amount;
            if (slot.amount <= 0)
            {
                items.Remove(slot);
            }
        }
    }
    
    public bool HasItem(Item item, int amount = 1)
    {
        ItemSlot slot = items.Find(s => s.item == item);
        return slot != null && slot.amount >= amount;
    }
}

[System.Serializable]
public class ItemSlot
{
    public Item item;
    public int amount;
    
    public ItemSlot(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}
```

---

## UI 系统

### 生命值UI

```csharp
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Image fillImage;
    [SerializeField] Text healthText;
    
    void Start()
    {
        if (health != null)
        {
            health.OnHealthChanged.AddListener(UpdateHealthBar);
            UpdateHealthBar(health.GetHealthPercent());
        }
    }
    
    void UpdateHealthBar(int currentHealth)
    {
        float percent = health.GetHealthPercent();
        fillImage.fillAmount = percent;
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{health.maxHealth}";
        }
        
        // 颜色渐变
        fillImage.color = Color.Lerp(Color.red, Color.green, percent);
    }
}
```

---

## 音频系统

```csharp
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }
    
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
    
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position)
    {
        AudioSource.PlayClipAtPoint(clip, position);
    }
}
```

---

## 存档系统

```csharp
using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
    string savePath;
    
    void Awake()
    {
        savePath = Application.persistentDataPath + "/save.json";
    }
    
    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            playerPosition = player.transform.position,
            health = player.GetComponent<Health>().currentHealth,
            level = currentLevel
        };
        
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }
    
    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            player.transform.position = data.playerPosition;
            player.GetComponent<Health>().currentHealth = data.health;
            currentLevel = data.level;
        }
    }
}

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public int health;
    public int level;
}
```

---

**文档来源**: Unity 2022.3 官方文档
**最后更新**: 2026-01-14
**游戏类型**: 2D 横版闯关 + Platform & Roguelike
