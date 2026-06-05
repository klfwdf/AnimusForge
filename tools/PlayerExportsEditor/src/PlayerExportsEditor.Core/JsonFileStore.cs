using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlayerExportsEditor.Core;

public sealed class JsonFileStore
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public string ReadUtf8(string filePath)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public JsonNode? ParseNode(string json)
    {
        return JsonNode.Parse(json, nodeOptions: null, documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
    }

    public void ValidateJson(string json)
    {
        _ = ParseNode(json);
    }

    public string ToIndentedJson(JsonNode? node)
    {
        return node?.ToJsonString(JsonOptions) ?? "";
    }

    public string ToIndentedJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public string SaveUtf8WithBackup(string filePath, string contents, string packageRoot)
    {
        ValidateJson(contents);

        var backupPath = "";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        if (File.Exists(filePath))
        {
            backupPath = CreateBackup(filePath, packageRoot);
        }

        var tmpPath = filePath + ".tmp";
        File.WriteAllText(tmpPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        ValidateJson(ReadUtf8(tmpPath));

        if (File.Exists(filePath))
        {
            File.Replace(tmpPath, filePath, null);
        }
        else
        {
            File.Move(tmpPath, filePath);
        }

        return backupPath;
    }

    private static string CreateBackup(string filePath, string packageRoot)
    {
        var safePackageRoot = Path.GetFullPath(packageRoot);
        var safeFilePath = Path.GetFullPath(filePath);
        var relativePath = Path.GetRelativePath(safePackageRoot, safeFilePath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            relativePath = Path.GetFileName(filePath);
        }

        var backupRoot = Path.Combine(safePackageRoot, ".backups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var backupPath = Path.Combine(backupRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? backupRoot);
        File.Copy(filePath, backupPath, overwrite: false);
        return backupPath;
    }
}
