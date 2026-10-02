using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Exercises production diagnostics in a fresh directory below repository artifacts only.
// Private constructor root injection avoids CacheRoot and all actual player diagnostic files.
public static class GenerationDiagnosticsAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string Secret = "key-AUDIT-123";
    private static Type diagnostics;
    private static Assembly module;
    private static int passed, failed;
    private static readonly object OutputLock = new object();
    private static string root;

    private static void Check(bool pass, string name)
    {
        lock (OutputLock)
        {
            if (pass) passed++; else failed++;
            Console.WriteLine((pass ? "PASS " : "FAIL ") + name);
        }
    }
    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = diagnostics.GetMethod(name, All);
        ParameterInfo[] parameters = method.GetParameters();
        if (args.Length < parameters.Length)
        {
            var complete = new object[parameters.Length];
            Array.Copy(args, complete, args.Length);
            for (int i = args.Length; i < complete.Length; i++)
            {
                if (!parameters[i].IsOptional) throw new ArgumentException("Missing required audit argument: " + name);
                complete[i] = parameters[i].DefaultValue;
            }
            args = complete;
        }
        return method.Invoke(target, args);
    }
    private static object Current()
    { return diagnostics.GetProperty("Current", All).GetValue(null, null); }
    private static string Id(object record)
    { return (string)diagnostics.GetProperty("Id", All).GetValue(record, null); }
    private static string DirectoryFor(object record)
    { return (string)diagnostics.GetField("_directory", All).GetValue(record); }
    private static string TraceFor(object record)
    { return Path.Combine(DirectoryFor(record), "trace.json"); }
    private static JObject Trace(object record)
    { return JObject.Parse(File.ReadAllText(TraceFor(record))); }
    private static object New(string storage, string subject)
    {
        object record = diagnostics.GetConstructor(All, null, new[] { typeof(string), typeof(string), typeof(string) }, null)
            .Invoke(new object[] { "audit-campaign", "conversation", storage });
        if (subject != null) Call(record, "SetSubject", subject);
        return record;
    }
    private static void Close(object record)
    { ((IDisposable)record).Dispose(); }
    private static void Stage(object record, string name, JObject data)
    { Call(record, "RecordStage", name, data); }
    private static byte[] Png(Color color, int padBytes)
    {
        using (var bitmap = new Bitmap(3, 2))
        using (var stream = new MemoryStream())
        {
            for (int x = 0; x < 3; x++) for (int y = 0; y < 2; y++) bitmap.SetPixel(x, y, color);
            bitmap.Save(stream, ImageFormat.Png);
            byte[] png = stream.ToArray();
            if (padBytes <= png.Length) return png;
            // Trailing PNG bytes allow deterministic large encoded fixtures with tiny dimensions.
            var padded = new byte[padBytes];
            Buffer.BlockCopy(png, 0, padded, 0, png.Length);
            return padded;
        }
    }
    private static string Hash(byte[] bytes)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", ""); }
    private static JObject RequestPayload(byte[] image, string prompt)
    {
        return new JObject { { "model", "audit-model" }, { "messages", new JArray(new JObject {
            { "role", "user" }, { "content", new JArray(new JObject { { "type", "text" }, { "text", prompt } },
                new JObject { { "type", "image_url" }, { "image_url", new JObject { { "url", "data:image/png;base64," + Convert.ToBase64String(image) } } } }) } }) } };
    }
    private static JObject Event(object record, string stage)
    { return (JObject)Trace(record)["events"].First(x => (string)x["stage"] == stage); }
    private static void RecordRequest(object record, HttpRequestMessage request, string protocol)
    { ((Task)Call(record, "RecordImageRequestAsync", request, protocol)).GetAwaiter().GetResult(); }

    private static void AuditRequests()
    {
        object record = New(Path.Combine(root, "requests"), "actual requests");
        try
        {
            Call(record, "RegisterSecret", Secret);
            byte[] png = Png(Color.FromArgb(112, 212, 37, 73), 0);
            JObject payload = RequestPayload(png, "EXACT DIRECTOR PROMPT");
            payload["api_key"] = "sensitive-field-value";
            payload["source_url"] = "https://user:password@example.test/path?token=QUERY_SECRET#fragment";
            string original = payload.ToString(Formatting.None);
            Call(record, "RecordDirectorRequest", "https://user:password@example.test/" + Secret + "/v1?token=QUERY_SECRET", "audit-model", payload, null);
            Check(payload.ToString(Formatting.None) == original, "director logging leaves original JObject unchanged");
            JObject logged = Event(record, "director_request");
            var saved = (JObject)logged["payload"]["messages"][0]["content"][1]["image_url"]["url"];
            Check((string)logged["payload"]["messages"][0]["content"][0]["text"] == "EXACT DIRECTOR PROMPT", "director's exact transmitted text is retained");
            Check((string)saved["sha256"] == Hash(png) && (int)saved["bytes"] == png.Length && (int)saved["width"] == 3 && (int)saved["height"] == 2, "reference hash bytes and dimensions reflect original encoded PNG");
            Check(File.ReadAllBytes(Path.Combine(DirectoryFor(record), (string)saved["file"])).SequenceEqual(png), "stored PNG retains exact transmitted bytes including alpha");

            var details = new JObject { { "error", "provider echoed " + Secret }, { "authorization", "Bearer ANOTHER_SECRET" }, { "url", "https://example.test/a?token=QUERY_SECRET" } };
            original = details.ToString(Formatting.None);
            Stage(record, "stage-" + Secret, details);
            Check(details.ToString(Formatting.None) == original, "stage logging leaves original details JObject unchanged");
            Check(!File.ReadAllText(TraceFor(record)).Contains(Secret), "all director endpoints stages and details redact registered credentials");

            var imagePayload = RequestPayload(png, "EXACT CHAT IMAGE PROMPT");
            string json = imagePayload.ToString(Formatting.None);
            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://u:p@example.test/" + Secret + "/v1/chat/completions?key=QUERY_SECRET"))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Secret);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                RecordRequest(record, request, "Chat");
                Check(request.Content.ReadAsStringAsync().GetAwaiter().GetResult() == json && request.Headers.Authorization.Parameter == Secret, "JSON body and Authorization header are unchanged after diagnostic capture");
            }
            logged = Event(record, "image_request");
            Check((string)logged["payload"]["messages"][0]["content"][0]["text"] == "EXACT CHAT IMAGE PROMPT", "image JSON retains the actual routed prompt");
            Check(Directory.GetFiles(DirectoryFor(record), "*.png").Length == 1, "identical director and image references deduplicate on encoded SHA256");

            byte[] secondPng = Png(Color.Blue, 0);
            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/v1/images/edits?key=QUERY_SECRET"))
            using (var multipart = new MultipartFormDataContent("diagnostic-audit-boundary"))
            {
                multipart.Add(new StringContent("EXACT EDITS PROMPT"), "prompt");
                multipart.Add(new StringContent("audit-image-model"), "model");
                multipart.Add(new ByteArrayContent(secondPng), "image[]", "ref-" + Secret + ".png");
                request.Content = multipart;
                byte[] before = multipart.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                RecordRequest(record, request, "ImagesEdits");
                Check(multipart.ReadAsByteArrayAsync().GetAwaiter().GetResult().SequenceEqual(before), "multipart content and boundary are unchanged after capture");
            }
            logged = (JObject)Trace(record)["events"].Last(x => (string)x["stage"] == "image_request");
            Check((string)logged["prompt"] == "EXACT EDITS PROMPT" && (string)logged["model"] == "audit-image-model" && (string)logged["protocol"] == "ImagesEdits", "multipart retains exact text fields and protocol");
            Check((string)logged["images"][0]["sha256"] == Hash(secondPng), "multipart reference identity matches the transmitted file");

            Call(record, "RecordDirectorResponse", "{\"message\":\"Bearer HEADER_SECRET https://example.test/path?token=QUERY_SECRET\",\"api_key\":\"API_FIELD_SECRET\",\"usage\":{\"total_tokens\":567}}", Secret);
            Call(record, "RecordImageResponse", "{\"data\":[{\"b64_json\":\"SERVER_IMAGE_BASE64\",\"url\":\"https://example.test/result?sig=QUERY_SECRET\"}]}", 200);
            string trace = File.ReadAllText(TraceFor(record));
            foreach (string secret in new[] { Secret, "QUERY_SECRET", "HEADER_SECRET", "ANOTHER_SECRET", "API_FIELD_SECRET", "password", "SERVER_IMAGE_BASE64", "sensitive-field-value" })
                Check(!trace.Contains(secret), "trace redacts credential/query/returned-image sentinel: " + secret);
            Check(trace.Contains("567") && trace.Contains("[redacted]"), "response usage survives redaction");
        }
        finally { Close(record); }
    }

    private static async Task AuditIsolation()
    {
        object previous = Current();
        object parent = New(Path.Combine(root, "isolation"), "parent");
        try
        {
            Check(object.ReferenceEquals(Current(), parent), "synchronous constructor sets ambient generation");
            object child = New(Path.Combine(root, "isolation"), "nested");
            Check(object.ReferenceEquals(Current(), child), "nested scope has its own ambient generation");
            Close(child);
            Check(object.ReferenceEquals(Current(), parent), "nested disposal restores previous ambient generation");

            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrived = new CountdownEvent(4);
            var jobs = Enumerable.Range(0, 4).Select(index => Task.Run(async () =>
            {
                object record = New(Path.Combine(root, "isolation"), "job-" + index);
                try
                {
                    Stage(record, "before", new JObject { { "job", index } });
                    arrived.Signal();
                    await release.Task.ConfigureAwait(false);
                    Check(object.ReferenceEquals(Current(), record), "ambient generation survives await for job " + index);
                    Stage(Current(), "after", new JObject { { "job", index } });
                    JArray events = (JArray)Trace(record)["events"];
                    Check(events.Count == 3 && events.Count(x => (string)x["stage"] == "pipeline_begin") == 1 && events.Where(x => (string)x["stage"] != "pipeline_begin").All(x => (int)x["job"] == index), "concurrent generation events stay isolated for job " + index);
                }
                finally { Close(record); }
                Check(object.ReferenceEquals(Current(), parent), "worker restores inherited parent after child disposal " + index);
            })).ToArray();
            Check(arrived.Wait(5000), "concurrent fixture workers reached deterministic await barrier");
            Check(object.ReferenceEquals(Current(), parent), "parallel child generation cannot replace caller's ambient scope");
            release.SetResult(true);
            await Task.WhenAll(jobs).ConfigureAwait(false);
            Check(object.ReferenceEquals(Current(), parent), "caller keeps its ambient generation after parallel joins");
        }
        finally { Close(parent); }
        Check(object.ReferenceEquals(Current(), previous), "outer disposal restores original ambient value");
    }

    private static void AuditOutcomes()
    {
        Type resultType = module.GetType("AnimusForge.Illustrator.Core.ImageGenerationResult", true);
        foreach (string outcome in new[] { "success", "failed", "cancelled" })
        {
            object record = New(Path.Combine(root, "outcomes"), outcome);
            if (outcome != "cancelled")
            {
                object result = Activator.CreateInstance(resultType);
                resultType.GetProperty("Success").SetValue(result, outcome == "success", null);
                resultType.GetProperty("ErrorMessage").SetValue(result, outcome == "failed" ? "provider empty reply" : "", null);
                resultType.GetProperty("ResolvedPrompt").SetValue(result, "ACTUAL IMAGE PROMPT", null);
                Call(record, "RecordImageResult", result);
            }
            else Call(record, "Finish", "cancelled", "caller cancelled");
            Close(record);
            JObject trace = Trace(record);
            Check((string)trace["outcome"] == outcome && trace["elapsedMs"] != null, outcome + " result persists after scope disposal");
            Check(trace["id"] != null && trace["startedUtc"] != null && File.Exists(TraceFor(record)), outcome + " trace has identifiable timing and on-disk record");
            Check((string)trace["moduleVersionId"] == module.ManifestModule.ModuleVersionId.ToString("D") &&
                (string)trace["assemblyVersion"] == module.GetName().Version.ToString(), outcome + " trace identifies the actual loaded module MVID and assembly version");
        }
        object outer = New(Path.Combine(root, "write-failure"), "outer");
        object failing = New(Path.Combine(root, "write-failure"), "failing");
        string failingDir = DirectoryFor(failing);
        using (var held = File.Open(TraceFor(failing), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Stage(failing, "blocked-write", new JObject());
            Close(failing);
        }
        Check(object.ReferenceEquals(Current(), outer), "write failure cannot prevent ambient restoration in Dispose");
        var active = (HashSet<string>)diagnostics.GetField("Active", All).GetValue(null);
        Check(!active.Contains(failingDir), "write failure cannot retain disposed record in active retention set");
        Close(outer);
        using (var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/images"))
        {
            request.Content = new StringContent(RequestPayload(Png(Color.Lime, 0), "after dispose").ToString());
            RecordRequest(failing, request, "Chat");
        }
        Check(Directory.GetFiles(failingDir, "*.png").Length == 0, "late capture after disposal cannot append reference files");
    }

    private static void AuditBudgets()
    {
        int maxMetadata = (int)diagnostics.GetField("MaxMetadataBytes", All).GetRawConstantValue();
        int maxReference = (int)diagnostics.GetField("MaxReferenceBytes", All).GetRawConstantValue();
        int maxRecords = (int)diagnostics.GetField("MaxRecords", All).GetRawConstantValue();
        object record = New(Path.Combine(root, "budget"), "budget");
        try
        {
            for (int index = 0; index < 8; index++) Stage(record, "metadata-" + index, new JObject { { "payload", new string('x', 65000) } });
            Call(record, "SetSubject", new string('中', 65536));
            Call(record, "Finish", "failed", new string('错', 65536));
            JObject trace = Trace(record);
            Check(new FileInfo(TraceFor(record)).Length <= maxMetadata, "metadata hard limit also covers late subject and failure text");
            Check((bool?)trace["metadataTruncated"] == true && (string)trace["outcome"] == "failed" && (string)trace["id"] == Id(record), "bounded metadata retains outcome and identity with explicit truncation marker");
            for (int index = 0; index < (int)diagnostics.GetField("MaxEvents", All).GetRawConstantValue() + 5; index++) Stage(record, "many-" + index, new JObject { { "number", index } });
            trace = Trace(record);
            Check(((JArray)trace["events"]).Count <= (int)diagnostics.GetField("MaxEvents", All).GetRawConstantValue() && (bool?)trace["eventsOmitted"] == true, "event-count cap is explicit and bounded");
        }
        finally { Close(record); }

        record = New(Path.Combine(root, "reference-budget"), "references");
        try
        {
            byte[] first = Png(Color.Red, 8 * 1024 * 1024), second = Png(Color.Blue, 8 * 1024 * 1024);
            foreach (byte[] png in new[] { first, second })
                Call(record, "RecordDirectorRequest", "https://example.test/chat", "audit-model", RequestPayload(png, "bounded"), null);
            JObject trace = Trace(record);
            var files = Directory.GetFiles(DirectoryFor(record), "*.png");
            Check(files.Sum(path => new FileInfo(path).Length) <= maxReference && files.Length == 1, "reference storage is bounded by encoded bytes across all requests");
            Check(trace.ToString().Contains("reference storage budget") && trace.ToString().Contains(Hash(second)), "omitted reference retains SHA256 identity and explicit budget reason");
        }
        finally { Close(record); }

        string retention = Path.Combine(root, "retention");
        Directory.CreateDirectory(retention);
        string sentinelDir = Path.Combine(retention, "unrelated-directory");
        Directory.CreateDirectory(sentinelDir);
        string sentinel = Path.Combine(sentinelDir, "keep.txt");
        File.WriteAllText(sentinel, "do not delete");
        string sibling = Path.Combine(root, "outside-retention.txt");
        File.WriteAllText(sibling, "keep sibling");
        object activeRecord = New(retention, "active");
        try
        {
            for (int index = 0; index < maxRecords + 5; index++) Close(New(retention, "closed-" + index));
            Check(Directory.GetDirectories(retention).Count(path => File.Exists(Path.Combine(path, "trace.json"))) <= maxRecords, "retention bounds complete and active generation directories");
            Check(Directory.Exists(DirectoryFor(activeRecord)), "retention never prunes an active generation");
            Check(File.ReadAllText(sentinel) == "do not delete" && File.ReadAllText(sibling) == "keep sibling", "retention leaves unrelated directories and sibling files intact");
        }
        finally { Close(activeRecord); }
    }

    public static void Run(string dllPath, string artifactRoot)
    {
        passed = failed = 0;
        module = Assembly.LoadFrom(dllPath);
        diagnostics = module.GetType("AnimusForge.Illustrator.Core.GenerationDiagnostics", true);
        root = Path.Combine(Path.GetFullPath(artifactRoot), "diagnostics-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Console.WriteLine("ARTIFACT ROOT " + root);
        AuditRequests();
        AuditIsolation().GetAwaiter().GetResult();
        AuditOutcomes();
        AuditBudgets();
        object tail = New(Path.Combine(root, "tail"), "late-failure");
        try
        {
            Call(tail, "RegisterSecret", Secret);
            for (int i = 0; i < 140; i++) Stage(tail, "capture_noise", new JObject { { "index", i } });
            Stage(tail, "portrait_decode", new JObject { { "failureCode", "portrait.png_decode_failed" }, { "error", "bad png " + Secret } });
            Stage(tail, "portrait_full_body_result", new JObject { { "failureCode", "portrait.full_body_unavailable" } });
            Call(tail, "Finish", "failed", "bad png " + Secret);
            JObject end = Trace(tail);
            Check((string)end["failedStage"] == "portrait_decode", "most specific failure survives later generic portrait error");
            Check(((JArray)end["events"]).Any(x => (string)x["stage"] == "pipeline_finish") && ((JArray)end["events"]).Any(x => (string)x["stage"] == "portrait_decode"), "late failure and finish survive event retention overflow");
            string steps = File.ReadAllText(Path.Combine(DirectoryFor(tail), "steps.log"));
            Check(steps.Contains("portrait.png_decode_failed") && !steps.Contains(Secret), "readable step log persists precise failure and redacts secrets");
            Check(!end.ToString().Contains(Secret), "terminal JSON redacts secret from failure and summary");
        }
        finally { Close(tail); }

        Check(Current() == null, "all tests leave no ambient generation behind");
        Console.WriteLine("RESULT: " + passed + " PASS / " + failed + " FAIL");
        if (failed > 0) throw new Exception("Generation diagnostics audit failed: " + failed);
    }
}
