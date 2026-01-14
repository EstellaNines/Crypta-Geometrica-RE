using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 存档工具类
/// 提供 JSON 序列化、XOR 加密和文件读写功能
/// </summary>
public static class SaveUtility
{
    #region 常量

    /// <summary>存档版本号</summary>
    public const string SAVE_VERSION = "1.0.0";

    /// <summary>魔数标识 - 用于验证文件完整性</summary>
    public const string MAGIC_NUMBER = "CG2026";

    /// <summary>存档文件夹名</summary>
    private const string SAVE_FOLDER = "Save";

    /// <summary>JSON 文件扩展名</summary>
    private const string JSON_EXTENSION = ".json";

    /// <summary>Crypta 文件扩展名</summary>
    private const string CRYPTA_EXTENSION = ".crypta";

    /// <summary>存档槽位前缀</summary>
    private const string SLOT_PREFIX = "slot_";

    /// <summary>测试槽位名称</summary>
    private const string DEBUG_SLOT = "slot_debug";

    // XOR 加密密钥 (混淆存储)
    private static readonly byte[] EncryptionKey = {
        0x43, 0x52, 0x59, 0x50, 0x54, 0x41, // "CRYPTA"
        0x47, 0x45, 0x4F, 0x4D, 0x45, 0x54, // "GEOMET"
        0x52, 0x49, 0x43, 0x41, 0x32, 0x30, // "RICA20"
        0x32, 0x36                           // "26"
    };

    #endregion

    #region 路径相关

    /// <summary>
    /// 获取存档目录路径
    /// 开发模式: Assets/Resources/Save
    /// 发布模式: Application.persistentDataPath/Save
    /// </summary>
    public static string GetSaveDirectory()
    {
#if UNITY_EDITOR
        // 开发模式：使用 Resources/Save
        return Path.Combine(Application.dataPath, "Resources", SAVE_FOLDER);
#else
        // 发布模式：使用持久化数据路径
        return Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
#endif
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    /// <param name="slotIndex">槽位索引 (0-2 为正式槽位, -1 为测试槽位)</param>
    /// <param name="encrypted">是否加密格式</param>
    public static string GetSavePath(int slotIndex, bool encrypted = false)
    {
        string directory = GetSaveDirectory();
        string fileName = slotIndex < 0 ? DEBUG_SLOT : $"{SLOT_PREFIX}{slotIndex}";
        string extension = encrypted ? CRYPTA_EXTENSION : JSON_EXTENSION;
        return Path.Combine(directory, fileName + extension);
    }

    /// <summary>
    /// 确保存档目录存在
    /// </summary>
    public static void EnsureSaveDirectoryExists()
    {
        string directory = GetSaveDirectory();
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"[SaveUtility] 创建存档目录: {directory}");
        }
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 将存档数据序列化为 JSON
    /// </summary>
    public static string ToJson(SaveData data)
    {
        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// 从 JSON 反序列化存档数据
    /// </summary>
    public static SaveData FromJson(string json)
    {
        return JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>
    /// 仅反序列化头信息 (快速预览)
    /// </summary>
    public static SaveHeader HeaderFromJson(string json)
    {
        // 先完整解析，再提取头部
        // 如果性能敏感，可以用正则或手动解析
        SaveData data = FromJson(json);
        return data?.header;
    }

    #endregion

    #region 加密解密

    /// <summary>
    /// XOR 加密
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(byte[] data)
    {
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ EncryptionKey[i % EncryptionKey.Length]);
        }
        return result;
    }

    /// <summary>
    /// XOR 解密 (对称加密，解密逻辑与加密相同)
    /// </summary>
    /// <param name="data">加密数据</param>
    /// <returns>解密后的数据</returns>
    public static byte[] Decrypt(byte[] data)
    {
        return Encrypt(data); // XOR 是对称的
    }

    /// <summary>
    /// 加密 JSON 字符串
    /// </summary>
    public static byte[] EncryptString(string json)
    {
        byte[] data = Encoding.UTF8.GetBytes(json);
        return Encrypt(data);
    }

    /// <summary>
    /// 解密为 JSON 字符串
    /// </summary>
    public static string DecryptToString(byte[] encryptedData)
    {
        byte[] decrypted = Decrypt(encryptedData);
        return Encoding.UTF8.GetString(decrypted);
    }

    #endregion

    #region 文件操作

    /// <summary>
    /// 保存数据到文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="data">存档数据</param>
    /// <param name="encrypt">是否加密</param>
    public static void SaveToFile(string path, SaveData data, bool encrypt = false)
    {
        EnsureSaveDirectoryExists();

        string json = ToJson(data);

        if (encrypt)
        {
            // 加密模式：Magic Number + 加密数据
            byte[] encryptedData = EncryptString(json);
            byte[] magicBytes = Encoding.ASCII.GetBytes(MAGIC_NUMBER);
            byte[] finalData = new byte[magicBytes.Length + encryptedData.Length];

            Array.Copy(magicBytes, 0, finalData, 0, magicBytes.Length);
            Array.Copy(encryptedData, 0, finalData, magicBytes.Length, encryptedData.Length);

            File.WriteAllBytes(path, finalData);
        }
        else
        {
            // 明文模式：直接写入 JSON
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        Debug.Log($"[SaveUtility] 保存成功: {path}");
    }

    /// <summary>
    /// 从文件加载数据
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="encrypted">是否为加密文件</param>
    /// <returns>存档数据，失败返回 null</returns>
    public static SaveData LoadFromFile(string path, bool encrypted = false)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveUtility] 文件不存在: {path}");
            return null;
        }

        try
        {
            string json;

            if (encrypted)
            {
                byte[] fileData = File.ReadAllBytes(path);

                // 验证 Magic Number
                if (!ValidateMagicNumber(fileData))
                {
                    Debug.LogError($"[SaveUtility] 文件损坏或格式错误: {path}");
                    return null;
                }

                // 提取加密数据 (跳过 Magic Number)
                int magicLength = MAGIC_NUMBER.Length;
                byte[] encryptedData = new byte[fileData.Length - magicLength];
                Array.Copy(fileData, magicLength, encryptedData, 0, encryptedData.Length);

                json = DecryptToString(encryptedData);
            }
            else
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }

            SaveData data = FromJson(json);
            Debug.Log($"[SaveUtility] 加载成功: {path}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveUtility] 加载失败: {path}, 错误: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 仅加载头信息 (快速预览)
    /// </summary>
    public static SaveHeader LoadHeaderOnly(string path, bool encrypted = false)
    {
        SaveData data = LoadFromFile(path, encrypted);
        return data?.header;
    }

    /// <summary>
    /// 删除存档文件
    /// </summary>
    public static bool DeleteSave(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log($"[SaveUtility] 删除存档: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveUtility] 删除失败: {e.Message}");
                return false;
            }
        }
        return false;
    }

    #endregion

    #region 校验

    /// <summary>
    /// 验证文件是否有效
    /// </summary>
    public static bool ValidateFile(string path)
    {
        if (!File.Exists(path))
            return false;

        // 判断是否为加密文件
        bool encrypted = path.EndsWith(CRYPTA_EXTENSION);

        if (encrypted)
        {
            byte[] fileData = File.ReadAllBytes(path);
            return ValidateMagicNumber(fileData);
        }
        else
        {
            // JSON 文件：尝试解析
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = FromJson(json);
                return data != null && data.header != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 验证 Magic Number
    /// </summary>
    private static bool ValidateMagicNumber(byte[] fileData)
    {
        if (fileData.Length < MAGIC_NUMBER.Length)
            return false;

        string magic = Encoding.ASCII.GetString(fileData, 0, MAGIC_NUMBER.Length);
        return magic == MAGIC_NUMBER;
    }

    /// <summary>
    /// 检查存档槽位是否有数据
    /// </summary>
    public static bool HasSaveData(int slotIndex)
    {
        // 检查 JSON 和 Crypta 两种格式
        string jsonPath = GetSavePath(slotIndex, false);
        string cryptaPath = GetSavePath(slotIndex, true);

        return File.Exists(jsonPath) || File.Exists(cryptaPath);
    }

    #endregion
}
