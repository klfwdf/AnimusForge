using System;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Explicit developer imports only. Validate the wire shape before defaults can turn
// an unrelated JSON object into an empty bundle that overwrites existing memory.
internal static class CompressedMemoryExportBundleReader
{
    internal static bool TryRead(string path, out CompressedMemoryExportBundle bundle, out string error)
    {
        bundle = null;
        error = "压缩记忆文件无效，请选择从“压缩记忆”导出的 JSON。";
        JObject document = PlayerExportsStore.ReadJson<JObject>(path);
        if (document == null) return false;

        bool hasMemoryField = false;
        bool hasPersonaField = false;
        foreach (JProperty property in document.Properties())
        {
            string name = property.Name;
            bool isOverview = string.Equals(name, "Overview", StringComparison.OrdinalIgnoreCase);
            bool isList = string.Equals(name, "DailyDrafts", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Blocks", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "SummaryQueue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "OverviewQueue", StringComparison.OrdinalIgnoreCase);
            hasPersonaField |= string.Equals(name, "Personality", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Background", StringComparison.OrdinalIgnoreCase);
            if (!isOverview && !isList) continue;
            hasMemoryField = true;
            JTokenType type = property.Value.Type;
            if (type != JTokenType.Null && type != (isOverview ? JTokenType.Object : JTokenType.Array))
            {
                error = "压缩记忆字段“" + name + "”类型错误，请选择有效的压缩记忆导出文件。";
                return false;
            }
        }
        if (!hasMemoryField)
        {
            error = hasPersonaField
                ? "这是个性/背景文件，不是压缩记忆。请在“单个 HeroNPC → 个性/背景导入”中读取；已有记忆未修改。"
                : "文件不含压缩记忆字段，请选择从“压缩记忆”导出的 JSON；已有记忆未修改。";
            return false;
        }
        try
        {
            CompressedMemoryExportBundle parsed = document.ToObject<CompressedMemoryExportBundle>();
            if (parsed == null) return false;
            bundle = parsed;
            error = "";
            return true;
        }
        catch
        {
            return false;
        }
    }
}
