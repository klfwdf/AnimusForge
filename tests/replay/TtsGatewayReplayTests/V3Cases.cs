using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal static class V3Cases
{
    private static void Assert(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    public static async Task Run(Assembly assembly)
    {
        var requestType = assembly.GetType("AnimusForge.Refactor.Contracts.TtsSynthesisRequest", true);
        var gatewayType = assembly.GetType("AnimusForge.Refactor.Adapters.VolcTtsGateway", true);
        var parseAudio = assembly.GetType("AnimusForge.TtsEngine", true).GetMethod("ParseAudioData", BindingFlags.NonPublic | BindingFlags.Static);
        object Request(string url, string format = "pcm", float speed = 1f, int rate = 24000, string extra = "{\"disable_markdown_filter\":true}", string app = "", string text = "你好，旅行者！") =>
            Activator.CreateInstance(requestType, url, app, "seed-tts-2.0", "fixture-voice", text, format, rate, speed, 1f, extra);
        using var client = new HttpClient();
        // Use the production V3 handler configuration, including redirect protection.
        var v3client = (HttpClient)assembly.GetType("AnimusForge.TtsEngine", true).GetField("_v3HttpClient", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        async Task<(bool ok, byte[] audio, string code, string log)> Send(object request, CancellationToken cancel = default)
        {
            var gateway = Activator.CreateInstance(gatewayType, client, v3client);
            var task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { request, "fixture-api-key", cancel });
            await task;
            var result = task.GetType().GetProperty("Result").GetValue(task);
            object Get(string name) => result.GetType().GetProperty(name).GetValue(result);
            return ((bool)Get("Success"), (byte[])Get("AudioBytes"), (string)Get("ErrorCode"), (string)Get("LogId"));
        }
        const string standardPath = "/api/v3/tts/unidirectional";
        const string planPath = "/api/v3/plan/tts/unidirectional";
        string Url(ReplayServer server, string path = standardPath) => new Uri(new Uri(server.Url), path).ToString();
        const string frames = "{\"code\":0,\"sentence\":{\"text\":\"你好 { \\\" }\"}}\n{\n \"code\":0,\"data\":\"AQ==\"\n}{\"code\":0,\"data\":\"AgME\"}{\"code\":20000000,\"usage\":{\"text_words\":6}}";
        int passed = 0;
        foreach (string path in new[] { standardPath, planPath, "/API/V3/PLAN/TTS/UNIDIRECTIONAL/" })
        foreach (string format in new[] { "pcm", "wav" })
        {
            using var server = ReplayServer.Start(null, 0, body: frames, chunked: true, fragmentSize: 1);
            var result = await Send(Request(Url(server, path), format, 1.25f, 16000));
            Assert(result.ok && result.log == "replay-log-123", "V3 fragmented stream failed: " + result.code);
            byte[] pcm = { 1, 2, 3, 4 };
            if (format == "pcm") Assert(result.audio.SequenceEqual(pcm), "PCM reordered");
            else
            {
                Assert(result.audio.Length == 48 && Encoding.ASCII.GetString(result.audio, 0, 4) == "RIFF", "WAV not wrapped once");
                object[] args = { result.audio, "wav", null, 0 };
                parseAudio.Invoke(null, args);
                Assert(((byte[])args[2]).SequenceEqual(pcm) && (int)args[3] == 16000, "production player cannot parse V3 WAV");
            }
            string raw = server.RequestText; int bodyOffset = raw.IndexOf("\r\n\r\n") + 4;
            string body = raw.Substring(bodyOffset);
            Assert(raw.StartsWith("POST " + path + " HTTP/1.1\r\n"), "configured endpoint rewritten");
            Assert(raw.Contains("X-Api-Key: fixture-api-key") && raw.Contains("X-Api-Resource-Id: seed-tts-2.0") && raw.Contains("X-Api-Request-Id:"), "V3 headers missing");
            Assert(!raw.Contains("Authorization:") && !raw.Contains("X-Api-App-Id:") && !body.Contains("fixture-api-key"), "credential scheme leaked");
            var json = JsonDocument.Parse(body).RootElement.GetProperty("req_params");
            Assert(json.GetProperty("text").GetString() == "你好，旅行者！" && json.GetProperty("speaker").GetString() == "fixture-voice", "text/voice changed");
            var audio = json.GetProperty("audio_params");
            Assert(audio.GetProperty("format").GetString() == "pcm" && audio.GetProperty("sample_rate").GetInt32() == 16000 && audio.GetProperty("speech_rate").GetInt32() == 25 && audio.GetProperty("loudness_rate").GetInt32() == 0, "parameter mapping");
            Assert(JsonDocument.Parse(json.GetProperty("additions").GetString()).RootElement.GetProperty("disable_markdown_filter").GetBoolean(), "additions not serialized object");
            Assert(server.RequestCount == 1, "unexpected retry"); passed++;
        }
        foreach (var item in new[] {
            ("{\"code\":0,\"data\":\"AQIDBA==\"}", ""),
            ("{\"code\":0,\"data\":\"AQIDBA==\"}{\"code\":45000000,\"message\":\"private text\"}", "tts_provider_code_45000000"),
            ("{\"code\":0,\"data\":\"AQIDBA==\"}{", "tts_response_invalid_json"),
            ("{\"code\":0,\"data\":\"?\"}", "tts_audio_base64_invalid"),
            ("{\"code\":0,\"data\":123}", "tts_audio_base64_invalid"),
            ("{\"code\":0}", "tts_audio_empty"),
            ("{\"data\":\"AQIDBA==\"}", "tts_response_invalid_code"),
            ("{\"code\":0,\"data\":\"AQ==\"}", "tts_audio_pcm_invalid"),
            ("data: {\"code\":0}", "tts_response_invalid_json") })
        {
            using var server = ReplayServer.Start(null, 0, body: item.Item1, chunked: true);
            var r = await Send(Request(Url(server)));
            Assert(r.ok == (item.Item2 == "") && r.code == item.Item2 && (r.ok || r.audio.Length == 0), "frame result mismatch: " + item.Item2 + " vs " + r.code);
            passed++;
        }
        using (var server = ReplayServer.Start(null, 0, body: "{\"code\":0,\"data\":\"AQIDBA==\"}", chunked: true, truncate: true))
        {
            var r = await Send(Request(Url(server))); Assert(!r.ok && r.audio.Length == 0, "truncated HTTP exposed partial audio"); passed++;
        }
        foreach (string path in new[] { standardPath, planPath })
        foreach (int code in new[] { 401, 403, 429, 500, 302 })
        {
            using var server = ReplayServer.Start(null, 0, code, body: "not-json");
            var r = await Send(Request(Url(server, path))); Assert(!r.ok && r.code == "tts_http_" + code && server.RequestCount == 1, "HTTP error retried/misclassified"); passed++;
        }
        foreach (string path in new[] { planPath + "/stream", planPath + "/sse", planPath + "-other", "/api/v3/plan/tts/bidirection", "/api/v3/plan/sauc/bigmodel_async" })
        {
            using var server = ReplayServer.Start(null, 0);
            var r = await Send(Request(Url(server, path)));
            Assert(!r.ok && r.code == "tts_endpoint_unsupported" && server.RequestCount == 0, "unsupported Plan protocol reached network"); passed++;
        }
        foreach (string url in new[] { "wss://openspeech.bytedance.com" + planPath, "https://user:secret@openspeech.bytedance.com" + planPath })
        {
            var r = await Send(Request(url));
            Assert(!r.ok && r.code == "tts_endpoint_unsupported", "invalid Plan endpoint accepted"); passed++;
        }
        foreach (string expected in new[] { "tts_v3_speed_invalid", "tts_v3_sample_rate_invalid", "tts_extra_parameters_invalid", "tts_audio_format_unsupported", "tts_v3_text_invalid", "tts_endpoint_unsupported", "tts_configuration_incomplete" })
        {
            using var server = ReplayServer.Start(null, 0);
            object request = expected switch
            {
                "tts_v3_speed_invalid" => Request(Url(server), speed: 0.1f),
                "tts_v3_sample_rate_invalid" => Request(Url(server), rate: 12345),
                "tts_extra_parameters_invalid" => Request(Url(server), extra: "[]"),
                "tts_audio_format_unsupported" => Request(Url(server), format: "mp3"),
                "tts_v3_text_invalid" => Request(Url(server), text: new string('a', 65537)),
                "tts_endpoint_unsupported" => Request(Url(server) + "/sse"),
                _ => Request(server.Url)
            };
            var r = await Send(request); Assert(!r.ok && r.code == expected && server.RequestCount == 0, "preflight did not prevent request: " + r.code); passed++;
        }
        using (var server = ReplayServer.Start("AQID", 0))
        {
            var r = await Send(Request(server.Url, app: "v1-app")); Assert(r.ok && server.RequestText.Contains("Bearer;fixture-api-key"), "router broke V1"); passed++;
        }
        foreach (string path in new[] { standardPath, planPath })
        using (var server = ReplayServer.Start(null, 0, body: frames, chunked: true, stallAfterHeaders: 5000))
        using (var cancel = new CancellationTokenSource(150))
        {
            var r = await Send(Request(Url(server, path)), cancel.Token); Assert(!r.ok && r.code == "tts_cancelled" && r.audio.Length == 0, "body read ignored cancellation"); passed++;
        }
        using (var server = ReplayServer.Start(null, 0, body: frames, chunked: true, stallAfterHeaders: 35000))
        {
            var r = await Send(Request(Url(server))); Assert(!r.ok && r.code == "tts_timeout", "body stall escaped total deadline: " + r.code); passed++;
        }
        // Decode limit on a syntactically valid response, then the independent transport budget.
        foreach (string body in new[] {
            "{\"code\":0,\"data\":\"" + Convert.ToBase64String(new byte[16 * 1024 * 1024 + 2]) + "\"}",
            new string(' ', 32 * 1024 * 1024 + 1) })
        {
            using var server = ReplayServer.Start(null, 0, body: body, chunked: true, fragmentSize: 65536);
            var r = await Send(Request(Url(server))); Assert(!r.ok && r.code == "tts_response_too_large", "budget not enforced: " + r.code); passed++;
        }
        Console.WriteLine("PASS volcV3Replay cases=" + passed + " actualDll=" + assembly.Location);
    }
}
