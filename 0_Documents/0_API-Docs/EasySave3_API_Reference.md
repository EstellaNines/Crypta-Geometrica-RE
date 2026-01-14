# Easy Save 3 API 完整参考文档

## 目录
1. [ES3 类 - 主要API类](#es3-类)
2. [ES3Settings 类 - 设置配置](#es3settings-类)
3. [ES3Writer 类 - 数据写入](#es3writer-类)
4. [ES3Reader 类 - 数据读取](#es3reader-类)
5. [ES3Cloud 类 - 云存储](#es3cloud-类)
6. [ES3Spreadsheet 类 - 电子表格](#es3spreadsheet-类)

---

## ES3 类

**描述**: 包含 Easy Save 主要方法的静态类,导入后可直接使用。

### Save Methods (保存方法)

| 方法 | 描述 |
|------|------|
| `ES3.Save` | 保存数据到文件 |
| `ES3.SaveRaw` | 保存原始数据 |
| `ES3.AppendRaw` | 追加原始数据到文件 |
| `ES3.SaveImage` | 保存图片数据 |

### Load Methods (加载方法)

| 方法 | 描述 |
|------|------|
| `ES3.Load` | 从文件加载数据 |
| `ES3.LoadInto` | 加载数据到现有对象 |
| `ES3.LoadRawBytes` | 加载原始字节数据 |
| `ES3.LoadRawString` | 加载原始字符串数据 |
| `ES3.LoadImage` | 加载图片数据 |
| `ES3.LoadAudio` | 加载音频数据 |

### Exists Methods (检查存在)

| 方法 | 描述 |
|------|------|
| `ES3.KeyExists` | 检查键是否存在 |
| `ES3.FileExists` | 检查文件是否存在 |
| `ES3.DirectoryExists` | 检查目录是否存在 |

### Delete Methods (删除方法)

| 方法 | 描述 |
|------|------|
| `ES3.DeleteKey` | 删除指定键 |
| `ES3.DeleteFile` | 删除文件 |
| `ES3.DeleteDirectory` | 删除目录 |

### Key, File and Directory Methods (文件管理)

| 方法 | 描述 |
|------|------|
| `ES3.RenameFile` | 重命名文件 |
| `ES3.CopyFile` | 复制文件 |
| `ES3.GetKeys` | 获取文件中所有键 |
| `ES3.GetFiles` | 获取目录中所有文件 |
| `ES3.GetDirectories` | 获取所有子目录 |
| `ES3.GetTimestamp` | 获取文件时间戳 |

### Caching Methods (缓存方法)

| 方法 | 描述 |
|------|------|
| `ES3.CacheFile` | 将文件缓存到内存 |
| `ES3.StoreCachedFile` | 将缓存文件存储到磁盘 |

### Backup Methods (备份方法)

| 方法 | 描述 |
|------|------|
| `ES3.CreateBackup` | 创建文件备份 |
| `ES3.RestoreBackup` | 恢复文件备份 |

---

## ES3Settings 类

**描述**: 用于覆盖默认设置(如加密设置),并指定文件和目录路径。

### 构造函数

| 构造函数 | 描述 |
|----------|------|
| `ES3Settings Constructor` | 创建设置对象 |

### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `location` | ES3.Location | 存储位置 (File/PlayerPrefs/Resources等) |
| `path` | string | 文件路径 |
| `encryptionType` | ES3.EncryptionType | 加密类型 |
| `encryptionPassword` | string | 加密密码 |
| `compressionType` | ES3.CompressionType | 压缩类型 |
| `directory` | string | 目录路径 |
| `bufferSize` | int | 缓冲区大小 |
| `encoding` | System.Text.Encoding | 文本编码 |

### 使用示例
```csharp
// 使用自定义设置保存数据
var settings = new ES3Settings(ES3.EncryptionType.AES, "myPassword");
ES3.Save("key", value, settings);
```

---

## ES3Writer 类

**描述**: 用于将数据写入流,通常在 ES3Type 文件的 Write 方法中直接操作。

### 构造函数

| 方法 | 描述 |
|------|------|
| `ES3Writer.Create` | 创建 ES3Writer 实例 |

### Write Methods (写入方法)

| 方法 | 描述 |
|------|------|
| `ES3Writer.WriteProperty` | 写入属性数据 |

### 使用场景
- 自定义序列化类型
- 在 ES3Type 中控制序列化过程

---

## ES3Reader 类

**描述**: 用于从流中读取数据,通常在 ES3Type 文件的 Read 方法中直接操作。

### 构造函数

| 方法 | 描述 |
|------|------|
| `ES3Reader.Create` | 创建 ES3Reader 实例 |

### 属性

| 属性 | 描述 |
|------|------|
| `ES3Reader.Properties` | 获取读取器属性 |

### Read Methods (读取方法)

| 方法 | 描述 |
|------|------|
| `ES3Reader.Read` | 读取数据 |

### 使用场景
- 自定义反序列化类型
- 在 ES3Type 中控制反序列化过程

---

## ES3Cloud 类

**描述**: 允许上传和下载文件到云存储。需要在服务器上安装 ES3.php 文件和 MySQL 表。

**重要**: 大多数情况下只需使用 `ES3Cloud.Sync` 方法。

### 构造函数

| 构造函数 | 描述 |
|----------|------|
| `ES3Cloud Constructor` | 创建 ES3Cloud 实例 |

### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `isError` | bool | 是否发生错误 |
| `error` | string | 错误信息 |
| `errorCode` | int | 错误代码 |
| `data` | byte[] | 下载的数据 |
| `text` | string | 下载的文本 |
| `timestamp` | long | 时间戳 |
| `filenames` | string[] | 文件名列表 |

### Sync Methods (同步方法)

| 方法 | 描述 |
|------|------|
| `ES3Cloud.Sync` | 同步本地和云端文件 (推荐使用) |

### Other Methods (其他方法)

| 方法 | 描述 |
|------|------|
| `ES3Cloud.UploadFile` | 上传文件到云端 |
| `ES3Cloud.DownloadFile` | 从云端下载文件 |
| `ES3Cloud.DeleteFile` | 删除云端文件 |
| `ES3Cloud.RenameFile` | 重命名云端文件 |
| `ES3Cloud.DownloadFilenames` | 下载文件名列表 |
| `ES3Cloud.SearchFilenames` | 搜索文件名 |
| `ES3Cloud.DownloadTimestamp` | 下载时间戳 |
| `ES3Cloud.AddPOSTField` | 添加 POST 字段 |

### 使用示例
```csharp
// 同步文件到云端
var cloud = new ES3Cloud("url", "apiKey");
yield return cloud.Sync("saveFile.es3");
if(cloud.isError)
    Debug.LogError(cloud.error);
```

---

## ES3Spreadsheet 类

**描述**: 表示电子表格的单元格,可使用 `ES3Spreadsheet.Save` 方法写入 CSV 文件。

### 构造函数

| 构造函数 | 描述 |
|----------|------|
| `ES3Spreadsheet Constructor` | 创建电子表格实例 |

### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `RowCount` | int | 行数 |
| `ColumnCount` | int | 列数 |

### Methods (方法)

| 方法 | 描述 |
|------|------|
| `ES3Spreadsheet.SetCell` | 设置单元格值 |
| `ES3Spreadsheet.GetCell` | 获取单元格值 |
| `ES3Spreadsheet.Save` | 保存为 CSV 文件 |
| `ES3Spreadsheet.Load` | 从 CSV 文件加载 |
| `ES3Spreadsheet.GetColumnLength` | 获取列长度 |
| `ES3Spreadsheet.GetRowLength` | 获取行长度 |

### 使用示例
```csharp
// 创建并保存电子表格
var spreadsheet = new ES3Spreadsheet();
spreadsheet.SetCell(0, 0, "Name");
spreadsheet.SetCell(0, 1, "Score");
spreadsheet.SetCell(1, 0, "Player1");
spreadsheet.SetCell(1, 1, 100);
spreadsheet.Save("data.csv");
```

---

## 完整方法列表

### ES3 类方法 (30个)
1. ES3.Save
2. ES3.SaveRaw
3. ES3.AppendRaw
4. ES3.SaveImage
5. ES3.Load
6. ES3.LoadInto
7. ES3.LoadRawBytes
8. ES3.LoadRawString
9. ES3.LoadImage
10. ES3.LoadAudio
11. ES3.KeyExists
12. ES3.FileExists
13. ES3.DirectoryExists
14. ES3.DeleteKey
15. ES3.DeleteFile
16. ES3.DeleteDirectory
17. ES3.RenameFile
18. ES3.CopyFile
19. ES3.GetKeys
20. ES3.GetFiles
21. ES3.GetDirectories
22. ES3.GetTimestamp
23. ES3.CacheFile
24. ES3.StoreCachedFile
25. ES3.CreateBackup
26. ES3.RestoreBackup

### ES3Settings 类 (1个构造函数 + 8个属性)
- ES3Settings Constructor
- location, path, encryptionType, encryptionPassword, compressionType, directory, bufferSize, encoding

### ES3Writer 类 (2个方法)
- ES3Writer.Create
- ES3Writer.WriteProperty

### ES3Reader 类 (2个方法 + 1个属性)
- ES3Reader.Create
- ES3Reader.Read
- ES3Reader.Properties

### ES3Cloud 类 (1个构造函数 + 8个属性 + 9个方法)
- ES3Cloud Constructor
- isError, error, errorCode, data, text, timestamp, filenames
- ES3Cloud.Sync, UploadFile, DownloadFile, DeleteFile, RenameFile, DownloadFilenames, SearchFilenames, DownloadTimestamp, AddPOSTField

### ES3Spreadsheet 类 (1个构造函数 + 2个属性 + 6个方法)
- ES3Spreadsheet Constructor
- RowCount, ColumnCount
- SetCell, GetCell, Save, Load, GetColumnLength, GetRowLength

---

## 快速参考

### 基本保存/加载
```csharp
// 保存
ES3.Save("key", value);
ES3.Save("key", value, "filename.es3");

// 加载
var value = ES3.Load<Type>("key");
var value = ES3.Load<Type>("key", "filename.es3");

// 检查存在
if(ES3.KeyExists("key"))
    var value = ES3.Load<Type>("key");
```

### 使用设置
```csharp
var settings = new ES3Settings();
settings.encryptionType = ES3.EncryptionType.AES;
settings.encryptionPassword = "password";
ES3.Save("key", value, settings);
```

### 云同步
```csharp
var cloud = new ES3Cloud("url", "apiKey");
yield return cloud.Sync("file.es3");
```

### 电子表格
```csharp
var sheet = new ES3Spreadsheet();
sheet.SetCell(0, 0, "Data");
sheet.Save("data.csv");
```

---

## 详细方法参数说明

### ES3.Save

**描述**: 保存值到文件中的指定键。如果文件已存在,键会被添加到文件中。如果键已存在,会覆盖原有值。

**参数**:
- `string key` - 保存数据的键名
- `T value` - 要保存的值
- `string filePath` (可选) - 文件路径,默认为 "SaveFile.es3"
- `ES3Settings settings` (可选) - 自定义设置

**示例**:
```csharp
// 保存到默认文件
ES3.Save("playerName", "John");

// 保存到指定文件
ES3.Save("score", 100, "player.es3");

// 使用自定义设置
var settings = new ES3Settings(ES3.EncryptionType.AES, "password");
ES3.Save("secretData", data, settings);
```

---

### ES3.Load

**描述**: 从文件中加载指定键的值。如果未指定 defaultValue 参数,数据不存在时会抛出 KeyNotFoundException 或 FileNotFoundException。

**参数**:
- `string key` - 要加载的键名
- `string filePath` (可选) - 文件路径
- `T defaultValue` (可选) - 数据不存在时返回的默认值
- `ES3Settings settings` (可选) - 自定义设置

**返回值**: 类型为 T 的加载数据,或默认值(如果使用了 defaultValue 参数且数据不存在)

**示例**:
```csharp
// 基本加载
var playerName = ES3.Load<string>("playerName");

// 从指定文件加载
var score = ES3.Load<int>("score", "player.es3");

// 使用默认值(推荐)
var level = ES3.Load<int>("level", defaultValue: 1);

// 加载字符串时使用命名参数避免歧义
var name = ES3.Load<string>("name", defaultValue: "Guest");
```

---

### ES3.LoadInto

**描述**: 将文件中的值加载到现有对象中,而不是创建新实例。也可以接受数组或 List 作为参数。

**参数**:
- `string key` - 要加载的键名
- `object obj` - 要加载数据到的现有对象
- `string filePath` (可选) - 文件路径
- `ES3Settings settings` (可选) - 自定义设置

**示例**:
```csharp
// 加载到现有对象
var player = new Player();
ES3.LoadInto("playerData", player);

// 加载到数组中的每个对象
var enemies = new Enemy[3];
ES3.LoadInto("enemiesData", enemies);
```

---

### ES3.KeyExists

**描述**: 检查文件中是否存在指定的键。

**参数**:
- `string key` - 要检查的键名
- `string filePath` (可选) - 文件路径
- `ES3Settings settings` (可选) - 自定义设置

**返回值**: 如果键存在返回 true,否则返回 false

**示例**:
```csharp
// 检查键是否存在
if(ES3.KeyExists("playerName"))
{
    var name = ES3.Load<string>("playerName");
}

// 检查指定文件中的键
if(ES3.KeyExists("score", "player.es3"))
{
    var score = ES3.Load<int>("score", "player.es3");
}
```

---

### ES3.FileExists

**描述**: 检查文件是否存在。

**参数**:
- `string filePath` (可选) - 文件路径,默认为 "SaveFile.es3"
- `ES3Settings settings` (可选) - 自定义设置

**返回值**: 如果文件存在返回 true,否则返回 false

**示例**:
```csharp
// 检查默认文件
if(ES3.FileExists())
{
    // 文件存在
}

// 检查指定文件
if(ES3.FileExists("player.es3"))
{
    // 加载数据
}
```

---

### ES3.DeleteKey

**描述**: 从文件中删除指定的键。如果键或文件不存在,不会抛出异常。

**参数**:
- `string key` - 要删除的键名
- `string filePath` (可选) - 文件路径
- `ES3Settings settings` (可选) - 自定义设置

**示例**:
```csharp
// 删除键
ES3.DeleteKey("oldData");

// 从指定文件删除键
ES3.DeleteKey("tempScore", "temp.es3");
```

---

### ES3.DeleteFile

**描述**: 删除文件。如果文件不存在,不会抛出异常。

**参数**:
- `string filePath` (可选) - 文件路径,默认为 "SaveFile.es3"
- `ES3Settings settings` (可选) - 自定义设置

**示例**:
```csharp
// 删除默认文件
ES3.DeleteFile();

// 删除指定文件
ES3.DeleteFile("temp.es3");
```

---

### ES3.GetKeys

**描述**: 获取文件中所有键名的数组。

**参数**:
- `string filePath` (可选) - 文件路径
- `ES3Settings settings` (可选) - 自定义设置

**返回值**: 键名数组 (string[])

**示例**:
```csharp
// 获取所有键
string[] keys = ES3.GetKeys();
foreach(var key in keys)
{
    Debug.Log("Found key: " + key);
}

// 从指定文件获取键
string[] playerKeys = ES3.GetKeys("player.es3");
```

---

### ES3.SaveImage

**描述**: 将 Texture2D 保存为 PNG 或 JPG 文件,根据文件扩展名决定格式。

**参数**:
- `Texture2D texture` - 要保存的纹理
- `string imagePath` - 图片文件路径 (.png 或 .jpg/.jpeg)
- `ES3Settings settings` (可选) - 自定义设置

**示例**:
```csharp
// 保存为 PNG
Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
ES3.SaveImage(screenshot, "screenshot.png");

// 保存为 JPG
ES3.SaveImage(texture, "photo.jpg");
```

---

### ES3.LoadImage

**描述**: 从 JPG 或 PNG 文件加载为 Texture2D。如果文件不存在会抛出 FileNotFoundException。

**参数**:
- `string imagePath` - 图片文件路径
- `ES3Settings settings` (可选) - 自定义设置

**返回值**: 加载的 Texture2D

**示例**:
```csharp
// 加载图片
if(ES3.FileExists("screenshot.png"))
{
    Texture2D texture = ES3.LoadImage("screenshot.png");
    // 使用纹理
}

// 从字节加载图片
byte[] imageBytes = ES3.LoadRawBytes("image.png");
Texture2D texture = ES3.LoadImage(imageBytes);
```

---

### 常用方法组合模式

#### 安全加载模式
```csharp
// 模式1: 使用默认值
var score = ES3.Load<int>("score", defaultValue: 0);

// 模式2: 先检查再加载
if(ES3.KeyExists("playerData"))
{
    var data = ES3.Load<PlayerData>("playerData");
}
else
{
    // 使用默认数据
    var data = new PlayerData();
}
```

#### 批量操作模式
```csharp
// 获取所有键并批量处理
string[] keys = ES3.GetKeys("save.es3");
foreach(var key in keys)
{
    if(key.StartsWith("temp_"))
    {
        ES3.DeleteKey(key, "save.es3");
    }
}
```

#### 加密保存模式
```csharp
// 创建加密设置
var settings = new ES3Settings();
settings.encryptionType = ES3.EncryptionType.AES;
settings.encryptionPassword = "MySecurePassword123";

// 保存加密数据
ES3.Save("sensitiveData", data, settings);

// 加载加密数据(需要相同的设置)
var loadedData = ES3.Load<DataType>("sensitiveData", settings);
```

---

**文档来源**: https://docs.moodkie.com/product/easy-save-3/es3-api/
**最后更新**: 2026-01-14
