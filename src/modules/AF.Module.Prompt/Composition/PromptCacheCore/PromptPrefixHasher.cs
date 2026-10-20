// Imported from AnimusForge.PromptCache/Core/PromptPrefixHasher.cs. Host adaptations: see import receipt.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge;

/// <summary>
/// 单条消息的指纹：内容哈希 + 字符长度。
/// 用指纹序列判断“本轮消息流是否严格以上一轮为前缀”，避免每轮拼接整个 prompt 造成的大对象分配。
/// </summary>
internal struct PromptFingerprint
{
    public string Hash;
    public int Length;
}

/// <summary>
/// 前缀一致性校验器（Prefix Guard）。
/// 目标：用可验证的方式证明“本轮请求 = 上一轮请求 + 纯追加”，而不是靠代码注释假设。
///
/// 性能设计：
/// - 每条消息的哈希按对象引用缓存（ConditionalWeakTable），历史消息对象跨轮复用时不会重复哈希；
/// - 每轮只需哈希“新增的 cue”，并逐条比较指纹序列，避免拼接 20 万字符的大字符串与 UTF8 字节数组。
/// </summary>
internal static class PromptPrefixHasher
{
    private static readonly ConditionalWeakTable<object, string> _hashCache =
        new ConditionalWeakTable<object, string>();

    /// <summary>
    /// 计算（或从缓存取）单条消息的哈希。只取 role/content。
    /// </summary>
    public static string Fingerprint(object message)
    {
        if (message == null)
            return "null";

        try
        {
            return _hashCache.GetValue(message, ComputeHash);
        }
        catch
        {
            return ComputeHash(message);
        }
    }

    private static string ComputeHash(object message)
    {
        if (!PromptReassembler.TryGetRoleAndContent(message, out string role, out string content))
            return "unsupported";

        try
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] roleBytes = Encoding.UTF8.GetBytes(role ?? string.Empty);
                byte[] contentBytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
                byte[] separator = { 0x01 };

                sha.TransformBlock(roleBytes, 0, roleBytes.Length, null, 0);
                sha.TransformBlock(separator, 0, separator.Length, null, 0);
                sha.TransformBlock(contentBytes, 0, contentBytes.Length, null, 0);
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                byte[] hash = sha.Hash;
                StringBuilder sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }
        catch
        {
            return "hash_error";
        }
    }

    /// <summary>
    /// 为整个消息流生成指纹序列，并输出正文字符总数。
    /// </summary>
    public static List<PromptFingerprint> Build(IEnumerable<object> messages, out int totalChars)
    {
        List<PromptFingerprint> fingerprints = new List<PromptFingerprint>();
        totalChars = 0;

        if (messages == null)
            return fingerprints;

        foreach (object message in messages)
        {
            if (message == null)
                continue;

            PromptReassembler.TryGetRoleAndContent(message, out string role, out string content);
            int length = (role?.Length ?? 0) + (content?.Length ?? 0);

            fingerprints.Add(new PromptFingerprint
            {
                Hash = Fingerprint(message),
                Length = length
            });

            totalChars += content?.Length ?? 0;
        }

        return fingerprints;
    }

    /// <summary>
    /// 判断 current 的前 N 条是否与 previous 完全一致（即本轮严格包含上一轮全部字节）。
    /// sharedChars 在断链时返回已匹配部分的字符数，便于定位断点。
    /// </summary>
    public static bool IsPrefix(List<PromptFingerprint> previous, List<PromptFingerprint> current, out int sharedChars)
    {
        sharedChars = 0;

        if (previous == null || previous.Count == 0)
            return true;

        if (current == null || current.Count < previous.Count)
            return false;

        for (int i = 0; i < previous.Count; i++)
        {
            if (previous[i].Length != current[i].Length ||
                !string.Equals(previous[i].Hash, current[i].Hash, StringComparison.Ordinal))
            {
                return false;
            }

            sharedChars += previous[i].Length;
        }

        return true;
    }

    /// <summary>
    /// 把整个消息流的指纹合并成一个短哈希，仅用于日志比对。
    /// </summary>
    public static string CombineHash(List<PromptFingerprint> fingerprints)
    {
        if (fingerprints == null || fingerprints.Count == 0)
            return "empty";

        try
        {
            StringBuilder sb = new StringBuilder(fingerprints.Count * 16);
            foreach (PromptFingerprint fingerprint in fingerprints)
                sb.Append(fingerprint.Hash);

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                StringBuilder result = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                    result.Append(hash[i].ToString("x2"));
                return result.ToString();
            }
        }
        catch
        {
            return "hash_error";
        }
    }
}
