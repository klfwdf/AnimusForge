using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

static class Program
{
    static int assertions;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        assertions++;
    }
    static void Reject(Action action, string name)
    {
        try { action(); }
        catch (ArgumentException) { assertions++; return; }
        catch (InvalidDataException) { assertions++; return; }
        throw new Exception("Expected rejection: " + name);
    }
    static byte[] Wave()
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(38);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(32000); writer.Write(64000);
        writer.Write((ushort)2); writer.Write((ushort)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(2); writer.Write((short)100);
        return memory.ToArray();
    }
    static JObject Profile(string id, string gender, string hash) => new()
    {
        ["id"] = id, ["label"] = id, ["style_target"] = "fixture",
        ["speaker_metadata"] = new JObject { ["gender"] = gender },
        ["reference_file"] = "reference.wav", ["reference_sha256"] = hash,
        ["prompt_lang"] = "en", ["prompt_text"] = "Welcome, traveler."
    };
    static string Manifest(string root, params JObject[] profiles)
    {
        string file = Path.Combine(root, "manifest.json");
        File.WriteAllText(file, new JObject { ["profiles"] = new JArray(profiles) }.ToString());
        return file;
    }
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1) throw new ArgumentException("Pass a fresh fixture directory under repository artifacts.");
        BattleGateTests.Run(Check);
        ToneTests.Run(Check);
        string root = Path.GetFullPath(args[0]);
        if (Directory.Exists(root)) throw new IOException("Fixture directory already exists; refusing overwrite.");
        Directory.CreateDirectory(root);
        byte[] wav = Wave(); File.WriteAllBytes(Path.Combine(root, "reference.wav"), wav);
        string hash = Convert.ToHexString(SHA256.HashData(wav));
        var a = Profile("male-a", "male", hash); var b = Profile("male-b", "male", hash); var f = Profile("female-a", "female", hash);
        var catalog = GptSoVitsVoiceCatalog.Load(Manifest(root, a, b, f));
        for (int i = 0; i < 64; i++)
        {
            Check(!catalog.ResolveAssignment(null, false).IsFemale, "male random stays male");
            Check(catalog.ResolveAssignment(null, true).IsFemale, "female random stays female");
        }
        string fixedId = catalog.ResolveAssignment(null, false).Id;
        string savedId = JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(fixedId));
        var reordered = GptSoVitsVoiceCatalog.Load(Manifest(root, f, b, a));
        Check(reordered.ResolveAssignment(savedId, false).Id == fixedId, "saved assignment survives ordering and reload");
        Check(reordered.ResolveAssignment("male-b", false).Id == "male-b", "manual choice overrides random");
        Reject(() => catalog.ResolveAssignment("female-a", false), "wrong gender");
        Reject(() => catalog.ResolveAssignment("removed-voice", false), "missing fixed voice must not reroll");
        var maleOnly = GptSoVitsVoiceCatalog.Load(Manifest(root, a));
        Reject(() => maleOnly.ResolveAssignment(null, true), "empty gender pool must not use opposite gender");

        var custom = Profile("custom-a", "female", hash); custom["category"] = "custom";
        var classified = GptSoVitsVoiceCatalog.Load(Manifest(root, a, f, custom));
        Check(classified.ForGender(false).Count == 1 && classified.ForGender(true).Count == 1, "custom excluded from both default pools");
        Check(classified.ForSelection(false).Count == 2 && classified.ForSelection(true).Count == 2, "custom offered in manual picker for either gender");
        Check(classified.ResolveAssignment("custom-a", false).IsCustom, "explicit custom assignment may cross gender");
        for (int i = 0; i < 64; i++) Check(classified.ResolveAssignment(null, true).Id == "female-a", "custom is never randomized");
        var onlyCustom = GptSoVitsVoiceCatalog.Load(Manifest(root, custom));
        Reject(() => onlyCustom.ResolveAssignment(null, true), "custom-only catalog cannot silently become a default pool");
        var wrongCategory = Profile("bad", "female", hash); wrongCategory["category"] = "male";
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, wrongCategory)), "category must match default gender");

        File.WriteAllBytes(Path.Combine(root, "happy.wav"), wav);
        var moodVoice = Profile("mood-male", "male", hash);
        var happy = Profile("same-speaker-reference", "male", hash);
        happy["reference_file"] = "happy.wav"; happy["prompt_text"] = "This is the happy reference.";
        moodVoice["emotion_variants"] = new JObject { ["happy"] = happy };
        var moods = GptSoVitsVoiceCatalog.Load(Manifest(root, moodVoice));
        Check(moods.Get("mood-male").ForMood(1).Id == "mood-male", "manual tone never changes speaker identity");
        Check(moods.Get("mood-male").ForMood(1).ReferencePath.EndsWith("happy.wav"), "manual tone selects its reference");
        Check(moods.Get("mood-male").ForMood(2).ReferencePath.EndsWith("reference.wav"), "absent tone stays with same voice base");
        var moodHandler = new Handler(async (request, _) =>
        {
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            Check(((string)body["ref_audio_path"]).EndsWith("happy.wav") && (string)body["prompt_text"] == "This is the happy reference.", "tone audio and matching transcript travel together");
            Check(body["emotion"] == null, "no invented native GPT-SoVITS emotion parameter");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(wav) };
        });
        using (var moodClient = new HttpClient(moodHandler))
        {
            var toneResult = await new GptSoVitsTtsGateway(moodClient).SynthesizeAsync(
                new GptSoVitsSynthesisRequest("http://127.0.0.1:9880/tts", "hello", moods.Get("mood-male").ForMood(1), 1), CancellationToken.None);
            Check(toneResult.Success, "actual tone gateway fixture succeeds");
        }
        var aliasFile = Manifest(root, a, f, custom);
        var aliasJson = JObject.Parse(File.ReadAllText(aliasFile)); aliasJson["aliases"] = new JObject { ["old-male"] = "male-a" };
        File.WriteAllText(aliasFile, aliasJson.ToString());
        var aliases = GptSoVitsVoiceCatalog.Load(aliasFile);
        Check(aliases.ResolveAssignment("old-male", false).Id == "male-a", "retired fixed ID migrates deterministically");
        Reject(() => aliases.ResolveAssignment("old-male", true), "migration cannot change default gender");
        aliasJson["aliases"] = new JObject { ["old-male"] = "missing-target" }; File.WriteAllText(aliasFile, aliasJson.ToString());
        Reject(() => GptSoVitsVoiceCatalog.Load(aliasFile), "broken migration target rejected");
        happy["reference_file"] = "../escape.wav";
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, moodVoice)), "tone path traversal rejected");
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, a, a)), "duplicate voice IDs");
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, Profile(" padded ", "male", hash))), "ID cannot change when stored voice is trimmed");
        var escaping = Profile("escape", "male", hash); escaping["reference_file"] = "../outside.wav";
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, escaping)), "reference path escape");
        var derived = Profile("derived", "male", hash);
        derived["inference_reference"] = new JObject { ["source_sha256"] = new string('0', 64), ["reference_file"] = "reference.wav", ["reference_sha256"] = hash };
        Reject(() => GptSoVitsVoiceCatalog.Load(Manifest(root, derived)), "derived provenance mismatch");
        var voice = catalog.Get("male-a"); voice.VerifyReference();
        using (var oversized = new FileStream(voice.ReferencePath, FileMode.Open)) oversized.SetLength(8 * 1024 * 1024 + 1);
        Reject(voice.VerifyReference, "reference size is bounded before hashing");
        File.WriteAllBytes(voice.ReferencePath, new byte[wav.Length]);
        File.SetLastWriteTimeUtc(voice.ReferencePath, DateTime.UtcNow.AddMinutes(1));
        Reject(voice.VerifyReference, "modified cached reference");
        File.WriteAllBytes(voice.ReferencePath, wav); File.SetLastWriteTimeUtc(voice.ReferencePath, DateTime.UtcNow.AddMinutes(2));
        var input = new GptSoVitsSynthesisRequest("http://127.0.0.1:9880/tts", "欢迎你，旅行者。", voice, 1f);
        Reject(() => new GptSoVitsSynthesisRequest("https://remote.invalid/tts", "hello", voice, 1), "non-local endpoint");
        Reject(() => new GptSoVitsSynthesisRequest("http://user:secret@localhost:9880/tts", "hello", voice, 1), "URI credentials");
        Reject(() => new GptSoVitsSynthesisRequest("http://localhost:9880/tts", new string('字', 501), voice, 1), "unbounded text");
        var handler = new Handler(async (request, token) =>
        {
            Check(request.Method == HttpMethod.Post && request.RequestUri == input.Endpoint, "local POST endpoint");
            Check(request.Headers.Authorization == null, "no cloud credential forwarding");
            var body = JObject.Parse(await request.Content.ReadAsStringAsync(token));
            Check((string)body["text"] == input.Text && (string)body["text_lang"] == "auto", "body is read without translation");
            Check((string)body["prompt_lang"] == "en", "reference language is independent of body");
            Check(body["app"] == null && body["token"] == null, "no fake cloud requirements");
            Check((int)body["batch_size"] == 1 && !(bool)body["use_cuda_graph"], "bounded inference parameters");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(wav) };
        });
        using var client = new HttpClient(handler);
        var result = await new GptSoVitsTtsGateway(client).SynthesizeAsync(input, CancellationToken.None);
        Check(result.Success && result.AudioBytes.SequenceEqual(wav) && handler.Calls == 1, "valid WAV / one attempt");
        handler.Reply = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"tts failed\"}") });
        result = await new GptSoVitsTtsGateway(client).SynthesizeAsync(input, CancellationToken.None);
        Check(!result.Success && result.ErrorCode == "gpt_http_400" && handler.Calls == 2, "failure does not retry or switch providers");
        handler.Reply = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not audio") });
        result = await new GptSoVitsTtsGateway(client).SynthesizeAsync(input, CancellationToken.None);
        Check(!result.Success && result.ErrorCode == "gpt_invalid_wav", "HTTP 200 error body rejected");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        int calls = handler.Calls;
        try { await new GptSoVitsTtsGateway(client).SynthesizeAsync(input, cancelled.Token); throw new Exception("Cancellation lost"); }
        catch (OperationCanceledException) { Check(handler.Calls == calls, "cancel before any HTTP attempt"); }
        handler.Reply = async (_, token) => { await Task.Delay(Timeout.Infinite, token); return null; };
        using var pending = new CancellationTokenSource(30);
        try { await new GptSoVitsTtsGateway(client).SynthesizeAsync(input, pending.Token); throw new Exception("Pending cancellation lost"); }
        catch (OperationCanceledException) { Check(true, "cancel in-flight attempt"); }
        handler.Reply = async (request, _) =>
        {
            Check(request.Headers.GetValues("Authorization").Single() == "Bearer;fixture-token", "legacy cloud authorization unchanged");
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            Check((string)body["request"]?["operation"] == "query" && (string)body["audio"]?["voice_type"] == "cloud-voice", "legacy cloud protocol unchanged");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new JObject { ["code"] = 3000, ["data"] = Convert.ToBase64String(wav) }.ToString()) };
        };
        result = await new LegacyVolcTtsGateway(client).SynthesizeAsync(new TtsSynthesisRequest("https://fixture.invalid/tts", "fixture-app", "fixture-resource", "cloud-voice", "hello", "wav", 24000, 1, 1, "{}"), "fixture-token", CancellationToken.None);
        Check(result.Success && result.AudioBytes.SequenceEqual(wav), "cloud backend still works");
        if (args.Length >= 2 && args[1] != "--manifest")
        {
            // Resolve only explicitly supplied reference roots; do not scan installed modules.
            AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
            {
                string name = new AssemblyName(eventArgs.Name).Name + ".dll";
                foreach (string directory in args.Skip(3))
                {
                    string file = Path.Combine(Path.GetFullPath(directory), name);
                    if (File.Exists(file)) return Assembly.LoadFrom(file);
                }
                return null;
            };
            var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
            Type profileType = assembly.GetType("AnimusForge.MyBehavior+NpcPersonaProfile", throwOnError: true);
            var record = Activator.CreateInstance(profileType, nonPublic: true);
            profileType.GetField("VoiceId").SetValue(record, "cloud-existing");
            profileType.GetField("GptSoVitsVoiceId").SetValue(record, fixedId);
            var restored = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(record), profileType);
            Check((string)profileType.GetField("VoiceId").GetValue(restored) == "cloud-existing" && (string)profileType.GetField("GptSoVitsVoiceId").GetValue(restored) == fixedId, "actual compiled persona JSON preserves separate bindings");
            var legacy = JsonConvert.DeserializeObject("{\"VoiceId\":\"old-cloud\"}", profileType);
            Check((string)profileType.GetField("VoiceId").GetValue(legacy) == "old-cloud" && profileType.GetField("GptSoVitsVoiceId").GetValue(legacy) == null, "legacy persona JSON still loads");
        }
        if (args.Length >= 3)
        {
            var live = GptSoVitsVoiceCatalog.Load(args[2]);
            if (File.ReadAllText(args[2]).Contains("LOCAL_TTS_TONE_TAG_TRIAL"))
                await ToneTests.CheckPack(args[2], wav, Check);
            foreach (bool female in new[] { false, true })
            {
                foreach (var entry in live.ForGender(female)) { entry.VerifyReference(); Check(entry.IsFemale == female, "actual pack metadata / reference hash"); }
                Console.WriteLine($"Actual pack {(female ? "female" : "male")}: {live.ForGender(female).Count}");
            }
            foreach (var entry in live.ForSelection(false).Where(v => v.IsCustom)) { entry.VerifyReference(); Check(entry.IsCustom, "custom pack hashes verified"); }
            foreach (var entry in live.ForSelection(false).Concat(live.ForSelection(true)).GroupBy(v => v.Id).Select(g => g.First()))
                for (int mood = 0; mood < 4; mood++) { entry.ForMood(mood).VerifyReference(); Check(entry.ForMood(mood).Id == entry.Id, "actual pack tones keep identity and verify hashes"); }
        }
        Console.WriteLine($"PASS {assertions} assertions; no model, game, paid endpoint or native playback started.");
        return 0;
    }
    sealed class Handler : HttpMessageHandler
    {
        internal int Calls;
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Reply;
        internal Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) => Reply = reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Calls++; return Reply(request, cancellationToken); }
    }
}
