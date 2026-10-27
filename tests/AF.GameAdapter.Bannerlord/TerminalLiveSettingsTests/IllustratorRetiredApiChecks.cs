using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using Newtonsoft.Json.Linq;

// Real MCM JSON, terminal setters and director HTTP pipeline; transport is in memory.
static class IllustratorRetiredApiChecks
{
    const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static bool HostAvailable = true;
    static bool Primary(ref string apiUrl, ref string apiKey, ref string modelName, ref bool __result)
    {
        apiUrl = HostAvailable ? "https://main.fixture.invalid/v1/chat/completions" : "";
        apiKey = HostAvailable ? "main-fixture-key" : "";
        modelName = HostAvailable ? "main-fixture-model" : "";
        __result = HostAvailable; return false;
    }
    static object Get(object o, string name) => o.GetType().GetProperty(name, All).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name, All).SetValue(o, value);
    static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { return true; }
        catch (InvalidOperationException) { return true; }
    }
    sealed class Sender : HttpMessageHandler
    {
        internal readonly List<string> Bodies = new List<string>();
        internal string Url, Key, Reply = "A portrait against a pure black background.", Finish = "stop";
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal bool Timeout;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Url = request.RequestUri.ToString(); Key = request.Headers.Authorization?.Parameter;
            Bodies.Add(await request.Content.ReadAsStringAsync());
            if (Timeout) throw new TaskCanceledException();
            return new HttpResponseMessage(Status) { Content = new StringContent(new JObject {
                ["choices"] = new JArray(new JObject { ["finish_reason"] = Finish,
                    ["message"] = new JObject { ["content"] = Reply } }) }.ToString()) };
        }
    }
    sealed class ModelSender : HttpMessageHandler
    {
        internal string Url, Key, Method;
        internal string Body = "{\"data\":[{\"id\":\"vision-b\"},{\"id\":\"vision-a\"},{\"id\":\"vision-a\"},null]}";
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Url = request.RequestUri.ToString(); Key = request.Headers.Authorization?.Parameter; Method = request.Method.Method;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body) });
        }
    }
    sealed class OpenRouterSender : HttpMessageHandler
    {
        internal readonly List<string> Bodies = new List<string>();
        internal readonly List<string> Urls = new List<string>();
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal const string Png = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAAmklEQVR4nO1WWwrAIAxzIPjtCdb7n6o3ydjDaWeL22Cwj4IfElNjLKWdQghzTt+tyMT7Dt1iFScDV/m0CcgzNmNopN3ihXwI2DG84s3VYw0SeBWwPKLiwpzCJwUXAqpHmO9t/razePKvAr1H3Mo5W3wpoHnEm5zXvBYB2yN6/EnOvQ7gdZC9DqL3g+z9IHk/YJ+Lks9F+PlctADU3vi6jlqQAwAAAABJRU5ErkJggg==";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Urls.Add(request.RequestUri.ToString()); Bodies.Add(await request.Content.ReadAsStringAsync());
            return new HttpResponseMessage(Status) { Content = new StringContent(Status == HttpStatusCode.OK ? new JObject { ["data"] = new JArray(new JObject { ["b64_json"] = Png }) }.ToString() : "{\"error\":{\"message\":\"unsupported\"}}") };
        }
    }
    internal static void Run(Assembly af, object format, Action<bool, string> check)
    {
        Type settingsType = af.GetType("AnimusForge.Illustrator.IllustratorSettings", true);
        Type director = af.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        Type optionsType = af.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        var harmony = new Harmony("af.tests.illustrator.independent.api");
        MethodInfo host = director.GetMethod("TryGetHostPrimaryChatConfig", All);
        harmony.Patch(host, prefix: new HarmonyMethod(typeof(IllustratorRetiredApiChecks).GetMethod("Primary", All)));
        string Save(object o) => (string)format.GetType().GetMethod("SaveJson").Invoke(format, new[] { o });
        void Load(object o, string json) => format.GetType().GetMethod("LoadFromJson").Invoke(format, new[] { o, json });
        object[] Resolve(object settings)
        {
            object[] args = { settings, null, null, null };
            director.GetMethod("ResolveChatConfigForSnapshot", All).Invoke(null, args); return args;
        }
        object Options(object settings)
        {
            var v = Resolve(settings);
            return Activator.CreateInstance(optionsType, All, null, new[] { settings, v[1], v[2], v[3] }, null);
        }
        try
        {
            var settings = (BaseSettings)Activator.CreateInstance(settingsType);
            var legacy = JObject.Parse(Save(settings));
            legacy["DirectorApiBaseUrl"] = "https://director.fixture.invalid/v1";
            legacy["DirectorApiKey"] = "director-fixture-key";
            legacy["DirectorModelName"] = "director-fixture-model";
            legacy["UseExactEndpointUrl"] = true; Load(settings, legacy.ToString());
            check(!(bool)Get(settings, "UseCustomDirectorApi"), "old director fields do not silently enable independent mode");
            var v = Resolve(settings);
            check((string)v[1] == "https://main.fixture.invalid/v1/chat/completions" && (string)v[2] == "main-fixture-key" && (string)v[3] == "main-fixture-model", "default mode keeps main credential owner");
            af.GetType("AnimusForge.Illustrator.Engine.ImagePayload", true).GetMethod("Normalize", All).Invoke(null, new object[] { Convert.FromBase64String(OpenRouterSender.Png) });
            var imageClientType = af.GetType("AnimusForge.Illustrator.Core.UniversalOpenAiImageClient", true);
            var isOpenRouter = imageClientType.GetMethod("IsOpenRouterImages", All);
            bool IsRouter(string url) => (bool)isOpenRouter.Invoke(null, new object[] { url });
            check(IsRouter("https://openrouter.ai/api/v1") && !IsRouter("https://openrouter.ai/api/v1/chat/completions") && !IsRouter("https://openrouter.ai.example/v1"), "OpenRouter route detects exact host and preserves explicit chat");
            var routerUrl = imageClientType.GetMethod("ResolveOpenRouterUrl", All);
            check((string)routerUrl.Invoke(null, new object[] { "https://openrouter.ai/api/v1", true }) == "https://openrouter.ai/api/v1/images/models", "image model list uses dedicated endpoint");
            var dimensions = imageClientType.GetMethod("AddOpenRouterDimensions", All);
            void Dimensions(JObject data, string size) => dimensions.Invoke(null, new object[] { data, size });
            var square = new JObject(); Dimensions(square, "1024x1024");
            check((string)square["resolution"] == "1K" && (string)square["aspect_ratio"] == "1:1", "1024 maps to 1K and square");
            var wide = new JObject(); Dimensions(wide, "2048x1152");
            check((string)wide["resolution"] == "2K" && (string)wide["aspect_ratio"] == "16:9", "2048x1152 maps to 2K and 16:9");
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(imageClientType.TypeHandle);
            var imageHttpField = imageClientType.GetField("HttpClient", All);
            var originalImageHttp = imageHttpField.GetValue(null);
            var imageSender = new OpenRouterSender();
            using (var imageHttp = new HttpClient(imageSender, false))
            {
                try
                {
                    imageHttpField.SetValue(null, imageHttp);
                    Set(settings, "ApiBaseUrl", "https://openrouter.ai/api/v1"); Set(settings, "ApiKey", "fixture-key"); Set(settings, "ModelName", "qwen/qwen-image-3");
                    var generateImage = imageClientType.GetMethod("GenerateImageAsync", new[] { typeof(string), typeof(string), optionsType, typeof(CancellationToken) });
                    object GenerateImage(string input)
                    {
                        var task = (Task)generateImage.Invoke(null, new object[] { "A portrait", input, Options(settings), CancellationToken.None });
                        task.GetAwaiter().GetResult(); return task.GetType().GetProperty("Result").GetValue(task);
                    }
                    var image = GenerateImage(null); var body = JObject.Parse(imageSender.Bodies.Last());
                    check((bool)Get(image, "Success") && imageSender.Urls.Last() == "https://openrouter.ai/api/v1/images", "real OpenRouter text image request and response: " + Get(image, "ErrorMessage") + " url=" + imageSender.Urls.Last());
                    check((string)body["resolution"] == "1K" && (string)body["aspect_ratio"] == "1:1" && body["size"] == null, "OpenRouter sends both resolution and ratio");
                    image = GenerateImage(OpenRouterSender.Png); body = JObject.Parse(imageSender.Bodies.Last());
                    check((bool)Get(image, "Success") && ((JArray)body["input_references"]).Count == 1 && ((string)body["input_references"][0]["image_url"]["url"]).StartsWith("data:image/png;base64,"), "real OpenRouter request retains reference image");
                    int before = imageSender.Bodies.Count; imageSender.Status = HttpStatusCode.BadRequest;
                    image = GenerateImage(OpenRouterSender.Png);
                    check(!(bool)Get(image, "Success") && imageSender.Bodies.Count == before + 1, "OpenRouter failure never retries or drops references");
                    imageSender.Status = HttpStatusCode.OK;
                    Set(settings, "ApiBaseUrl", "https://gemini-relay.fixture.invalid/v1");
                    Set(settings, "ModelName", "gemini-3.1-flash-image"); Set(settings, "ImageSize", "2048x2048");
                    image = GenerateImage(OpenRouterSender.Png); body = JObject.Parse(imageSender.Bodies.Last());
                    var geminiConfig = body["extra_body"]?["google"]?["image_config"];
                    check((bool)Get(image, "Success") && imageSender.Urls.Last().EndsWith("/v1/chat/completions"), "Gemini keeps configured chat route");
                    check((string)geminiConfig?["image_size"] == "2K" && (string)geminiConfig?["aspect_ratio"] == "1:1", "Gemini chat sends 2K and aspect ratio extension");
                    check(body["messages"][0]["content"].ToString().Contains("image_url"), "Gemini chat retains reference image with size extension");
                    Set(settings, "ModelName", "gemini-nano-banana-2.1"); Set(settings, "ImageSize", "1024x1024");
                    image = GenerateImage(null); body = JObject.Parse(imageSender.Bodies.Last());
                    check((string)body["extra_body"]?["google"]?["image_config"]?["image_size"] == "1K", "nano banana alias maps to 1K");
                    Set(settings, "ModelName", "other-chat-image");
                    image = GenerateImage(null); body = JObject.Parse(imageSender.Bodies.Last());
                    check(body["extra_body"] == null, "other chat image models receive no Google extension");
                    Set(settings, "ModelName", "gemini-3.1-flash-image"); Set(settings, "ImageSize", "2048x2048");
                    int previous = imageSender.Bodies.Count; imageSender.Status = HttpStatusCode.BadRequest;
                    image = GenerateImage(OpenRouterSender.Png);
                    check(!(bool)Get(image, "Success") && imageSender.Bodies.Count == previous + 1, "Gemini size extension failure does not retry");
                    imageSender.Status = HttpStatusCode.OK; Set(settings, "ApiBaseUrl", "https://gemini-relay.fixture.invalid/v1beta");
                    image = GenerateImage(OpenRouterSender.Png); body = JObject.Parse(imageSender.Bodies.Last());
                    check((bool)Get(image, "Success") && imageSender.Urls.Last().EndsWith("/v1beta/models/gemini-3.1-flash-image:generateContent"), "Gemini native keeps original native endpoint");
                    check((string)body["generationConfig"]?["imageConfig"]?["imageSize"] == "2K" && body["contents"]?[0]?["parts"] != null && body["extra_body"] == null, "real native request has resolution and reference parts without chat extension");

                    Set(settings, "ImageSize", "1024x1024");

                }
                finally { imageHttpField.SetValue(null, originalImageHttp); }
            }
            Type geminiProtocol = af.GetType("AnimusForge.Illustrator.Core.GeminiNativeImageProtocol", true);
            var nativeConvert = geminiProtocol.GetMethod("ConvertPayload", All);
            var nativeSource = new JObject { ["model"] = "gemini-3.1-flash-image", ["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = new JArray(new JObject { ["type"] = "text", ["text"] = "A portrait" }, new JObject { ["type"] = "image_url", ["image_url"] = new JObject { ["url"] = "data:image/png;base64," + OpenRouterSender.Png } }) }) };
            var nativePayload = (JObject)nativeConvert.Invoke(null, new object[] { nativeSource, "2048x1152" });
            check((string)nativePayload["generationConfig"]?["imageConfig"]?["imageSize"] == "2K" && (string)nativePayload["generationConfig"]?["imageConfig"]?["aspectRatio"] == "16:9", "Gemini native sends 2K and frame");
            check(nativePayload["contents"][0]["parts"][1]["inlineData"] != null, "native size mapping retains reference bytes");
            nativeSource["model"] = "gemini-2.5-flash-image";
            nativePayload = (JObject)nativeConvert.Invoke(null, new object[] { nativeSource, "2048x2048" });
            check(nativePayload["generationConfig"]?["imageConfig"]?["imageSize"] == null, "Gemini 2.5 avoids unsupported high resolution parameter");
            nativeSource["model"] = "gemini-3.1-flash-lite-image";
            nativePayload = (JObject)nativeConvert.Invoke(null, new object[] { nativeSource, "2048x2048" });
            check(nativePayload["generationConfig"]?["imageConfig"]?["imageSize"] == null, "Flash Lite avoids unsupported high resolution parameter");
            var mainOptions = Options(settings); Set(settings, "UseCustomDirectorApi", true); v = Resolve(settings);
            check((string)v[1] == "https://director.fixture.invalid/v1" && (string)v[2] == "director-fixture-key" && (string)v[3] == "director-fixture-model", "independent mode resolves its full configuration");
            var customOptions = Options(settings); Set(settings, "DirectorModelName", "changed-after-capture");
            check((string)Get(customOptions, "DirectorModelName") == "director-fixture-model", "request snapshot freezes model");
            Set(settings, "DirectorModelName", "director-fixture-model");
            var preset = (BaseSettings)Activator.CreateInstance(settingsType); Load(preset, Save(settings));
            foreach (string name in new[] { "UseCustomDirectorApi", "DirectorApiBaseUrl", "DirectorApiKey", "DirectorModelName" })
            {
                check(Equals(Get(settings, name), Get(preset, name)), "native MCM JSON/preset roundtrip " + name);
                check(settingsType.GetProperty(name).GetCustomAttributes(true).Any(a => a.GetType().Name.StartsWith("SettingProperty") && a.GetType().Name != "SettingPropertyGroupAttribute"), "native MCM discovers " + name);
            }
            foreach (string retired in new[] { "UseExactEndpointUrl" })
                check(settingsType.GetProperty(retired, All) == null && JObject.Parse(Save(settings))[retired] == null, "unrequested control stays retired " + retired);
            HostAvailable = false; check((string)Resolve(settings)[1] == "https://director.fixture.invalid/v1", "independent mode works without host API");
            Set(settings, "DirectorApiKey", ""); check((string)Resolve(settings)[2] == "", "local service never borrows main key");
            var localOptions = Options(settings);
            Set(settings, "DirectorModelName", ""); check(Throws(() => Resolve(settings)), "missing independent model fails explicitly");
            Set(settings, "DirectorModelName", "director-fixture-model"); Set(settings, "DirectorApiBaseUrl", "");
            check(Throws(() => Resolve(settings)), "missing independent URL fails explicitly");
            Set(settings, "UseCustomDirectorApi", false); v = Resolve(settings);
            check(v.Skip(1).All(x => string.IsNullOrEmpty((string)x)), "missing main API never borrows image credentials"); HostAvailable = true;

            var owner = (BaseSettings)TerminalProviderProbe.Owners.Values.Single(o => o.GetType() == settingsType);
            var duel = TerminalProviderProbe.Owners["AnimusForge_global_settings"];
            Type registry = af.GetType("AnimusForge.TerminalSettingsRegistry", true);
            var defs = ((IEnumerable)registry.GetProperty("AllDefinitions").GetValue(null)).Cast<object>().ToDictionary(d => (string)Get(d, "Id"));
            var changes = new Dictionary<string, object> { ["UseCustomDirectorApi"] = true, ["DirectorApiBaseUrl"] = "https://terminal.fixture.invalid/v1", ["DirectorApiKey"] = "terminal-fixture-key", ["DirectorModelName"] = "terminal-fixture-model" };
            var prior = changes.ToDictionary(p => p.Key, p => Get(owner, p.Key));
            try
            {
                var pages = (FluentGlobalSettings[])af.GetType("AnimusForge.AnimusForgeMcmPages", true)
                    .GetMethod("CreatePages", All).Invoke(null, new object[] { TerminalProviderProbe.Owners.Values.Cast<BaseSettings>().ToArray() });
                var mcm = pages.SelectMany(page => page.GetAllSettingPropertyDefinitions()).ToDictionary(p => p.Id);
                check(!(bool)af.GetType("AnimusForge.TerminalSettingsCoverage", true).GetMethod("IsVisible", All).Invoke(null, new object[] { "Illustrator.DirectorMaxTokens" }) && !mcm.ContainsKey("Illustrator.DirectorMaxTokens"), "director reference length hidden in both settings entrances");
                check(defs.ContainsKey("Illustrator.DirectorApiMaxTokens") && mcm.ContainsKey("Illustrator.DirectorApiMaxTokens"), "director API output token budget remains visible");
                var hiddenReload = (BaseSettings)Activator.CreateInstance(settingsType);
                Set(hiddenReload, "DirectorMaxTokens", 3210);
                var hiddenRestored = (BaseSettings)Activator.CreateInstance(settingsType);
                Load(hiddenRestored, Save(hiddenReload));
                check((int)Get(hiddenRestored, "DirectorMaxTokens") == 3210, "hidden director reference length survives JSON round trip");
                var viewOrder = af.GetType("AnimusForge.TerminalSettingsCoverage", true).GetMethod("ViewOrder", All);
                var imageSequence = new[] { "EnableImageGeneration", "ApiBaseUrl", "ApiKey", "ModelName", "FetchModelList", "ModelDropdown", "TestImageApi" };
                var imageOrders = imageSequence.Select(id => (int)viewOrder.Invoke(null, new object[] { "Illustrator." + id })).ToArray();
                check(imageOrders.Zip(imageOrders.Skip(1), (left, right) => left < right).All(ok => ok), "terminal API configuration is ordered and grouped");
                var nativeOrders = imageSequence.Select(id => mcm["Illustrator." + id].Order).ToArray();
                check(nativeOrders.Zip(nativeOrders.Skip(1), (left, right) => left < right).All(ok => ok), "native MCM API configuration is ordered and grouped");
                string directorGroup = (string)Get(defs["Illustrator.DirectorApiBaseUrl"], "GroupName");
                string imageGroup = (string)Get(defs["Illustrator.ApiBaseUrl"], "GroupName");
                check(directorGroup != imageGroup, "terminal separates director and image API groups");
                check(mcm["Illustrator.DirectorApiBaseUrl"].GroupName != mcm["Illustrator.ApiBaseUrl"].GroupName, "MCM separates director and image API groups");
                foreach (string id in new[] { "FetchDirectorModelList", "DirectorModelDropdown" })
                {
                    check(defs.ContainsKey("Illustrator." + id) && mcm.ContainsKey("Illustrator." + id), "fetch and selector exposed in terminal and MCM: " + id);
                    check((string)Get(defs["Illustrator." + id], "GroupName") == directorGroup, "fetch/selector in director group: " + id);
                    check(mcm["Illustrator." + id].GroupName == mcm["Illustrator.DirectorApiBaseUrl"].GroupName, "MCM fetch/selector in director group: " + id);
                }
                check(Get(owner, "FetchDirectorModelList") is Action, "fetch action bound to original owner");
                foreach (var pair in changes)
                {
                    var def = defs["Illustrator." + pair.Key];
                    check(!((string)Get(def, "DisplayName")).Contains("{=") && !string.IsNullOrWhiteSpace((string)Get(def, "HintText")), "terminal localized metadata " + pair.Key);
                    ((Delegate)Get(def, "Setter")).DynamicInvoke(duel, pair.Value);
                    check(Equals(Get(owner, pair.Key), pair.Value), "terminal reaches original owner " + pair.Key);
                    check(Equals(((Delegate)Get(def, "Getter")).DynamicInvoke(duel), pair.Value), "terminal reads original owner " + pair.Key);
                    var mcmProperty = mcm["Illustrator." + pair.Key];
                    check(pages[0].GetAllSettingPropertyDefinitions().Any(p => p.Id == "Illustrator." + pair.Key), "director setting remains on native API page " + pair.Key);
                    check(Equals(mcmProperty.PropertyReference.Value, pair.Value), "native MCM page sees terminal edit " + pair.Key);
                    mcmProperty.PropertyReference.Value = prior[pair.Key];
                    check(Equals(Get(owner, pair.Key), prior[pair.Key]), "native MCM page writes original owner " + pair.Key);
                    mcmProperty.PropertyReference.Value = pair.Value;
                }
                var reloaded = (BaseSettings)Activator.CreateInstance(settingsType); Load(reloaded, Save(owner));
                foreach (var pair in changes) check(Equals(Get(reloaded, pair.Key), pair.Value), "terminal value persists " + pair.Key);
                settingsType.GetMethod("ApplyDirectorModels", All).Invoke(owner, new object[] { new List<string> { "terminal-vision-model" } });
                var directorChoices = (Dropdown<string>)Get(owner, "DirectorModelDropdown");
                ((Delegate)Get(defs["Illustrator.DirectorModelDropdown"], "Setter")).DynamicInvoke(duel, directorChoices.IndexOf("terminal-vision-model"));
                check((string)Get(owner, "DirectorModelName") == "terminal-vision-model", "terminal model dropdown updates director model owner");
                check((string)mcm["Illustrator.DirectorModelName"].PropertyReference.Value == "terminal-vision-model", "MCM model text reflects terminal dropdown selection");
                reloaded = (BaseSettings)Activator.CreateInstance(settingsType); Load(reloaded, Save(owner));
                check((string)Get(reloaded, "DirectorModelName") == "terminal-vision-model", "selected director model survives native JSON reload");
                TerminalProviderProbe.Saved.Clear();
                var coverage = af.GetType("AnimusForge.TerminalSettingsCoverage", true);
                check((bool)coverage.GetMethod("SaveDirtyOwners", All).Invoke(null, null) && TerminalProviderProbe.Saved.Contains(owner.Id), "terminal saves Illustrator owner");
            }
            finally { foreach (var pair in prior) Set(owner, pair.Key, pair.Value); }

            var fetch = settingsType.GetMethod("FetchDirectorModelsAsync", All);
            List<string> Fetch(ModelSender handler, string url, string key)
            {
                using (var client = new HttpClient(handler, false))
                    return ((Task<List<string>>)fetch.Invoke(null, new object[] { url, key, client, CancellationToken.None })).GetAwaiter().GetResult();
            }
            foreach (string baseUrl in new[] { "https://director.fixture.invalid", "https://director.fixture.invalid/v1", "https://director.fixture.invalid/v1/chat/completions" })
            {
                var handler = new ModelSender(); var list = Fetch(handler, baseUrl, "director-fixture-key");
                check(handler.Method == "GET" && handler.Url == "https://director.fixture.invalid/v1/models" && handler.Key == "director-fixture-key", "fetch uses director endpoint and independent key");
                check(handler.Requests == 1 && list.SequenceEqual(new[] { "vision-a", "vision-b" }), "fetch makes one request and filters/sorts IDs");
            }
            var anonymous = new ModelSender(); Fetch(anonymous, "https://director.fixture.invalid/v1/models", "");
            check(anonymous.Url == "https://director.fixture.invalid/v1/models" && anonymous.Key == null, "local anonymous fetch does not borrow a key");
            foreach (var bad in new[] { new ModelSender { Status = HttpStatusCode.Unauthorized }, new ModelSender { Body = "{bad" }, new ModelSender { Body = "{}" } })
            {
                bool failed = false;
                try { Fetch(bad, "https://director.fixture.invalid/v1", "director-fixture-key"); } catch { failed = true; }
                check(failed && bad.Requests == 1, "fetch failure does not retry");
            }
            Set(settings, "DirectorModelName", "existing-custom-model");
            settingsType.GetMethod("ApplyDirectorModels", All).Invoke(settings, new object[] { new List<string> { "vision-a", "vision-b" } });
            var listDropdown = (Dropdown<string>)Get(settings, "DirectorModelDropdown");
            check((string)Get(settings, "DirectorModelName") == "existing-custom-model" && listDropdown.Contains("vision-a"), "fetch retains current model and exposes fetched list");
            listDropdown.SelectedIndex = listDropdown.IndexOf("vision-a");
            check((string)Get(settings, "DirectorModelName") == "vision-a", "director selection updates actual request model");
            check((string)Get(settings, "ModelName") != "vision-a", "director selection does not change image model");
            Set(settings, "DirectorModelName", "manual-director-model");
            check((string)Get(settings, "DirectorModelName") == "manual-director-model", "manual director model remains supported");
            int revision = (int)settingsType.GetField("_directorConfigRevision", All).GetValue(settings);
            Set(settings, "DirectorApiBaseUrl", "https://changed.fixture.invalid/v1");
            check((int)settingsType.GetField("_directorConfigRevision", All).GetValue(settings) != revision && !((Dropdown<string>)Get(settings, "DirectorModelDropdown")).Contains("vision-a"), "endpoint change invalidates list and stale request revision");

            Type planType = af.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true);
            Type texts = af.GetType("AnimusForge.Illustrator.IllustratorText", true);
            string Raw(string key) => (string)texts.GetMethod("Raw", All).Invoke(null, new object[] { key });
            object Plan(string mode, string redraw = "") => Activator.CreateInstance(planType, new object[] { mode, "IMMUTABLE_FACT", "", "Narrative text previously rejected verbatim.", redraw });
            object Generate(Sender sender, object options, object plan, Array refs = null, CancellationToken token = default)
            {
                using (var client = new HttpClient(sender, false))
                {
                    var task = (Task)director.GetMethod("CreateDirectionWithClientAsync", All).Invoke(null, new object[] { plan, refs, options, client, token });
                    task.GetAwaiter().GetResult(); return task.GetType().GetProperty("Result").GetValue(task);
                }
            }
            foreach (var options in new[] { mainOptions, customOptions, localOptions })
            {
                var sender = new Sender(); Generate(sender, options, Plan(Raw("Director_1020")));
                string endpoint = ((string)Get(options, "DirectorApiBaseUrl")).TrimEnd('/');
                if (!endpoint.EndsWith("/chat/completions")) endpoint += "/chat/completions";
                check(sender.Url == endpoint && (sender.Key ?? "") == (string)Get(options, "DirectorApiKey"), "real request uses frozen endpoint/key");
                check((string)JObject.Parse(sender.Bodies.Single())["model"] == (string)Get(options, "DirectorModelName"), "real request sends frozen model");
            }
            foreach (string mode in new[] { "Director_1020", "Director_933", "Director_937", "Director_979" })
                foreach (string body in new[] { "不画任何背盾或背负长矛。", "A shield strapped to his back, a flag beside him.", "A portrait against a pure black background.", "A rider turns.", "Narrative text previously rejected verbatim.", "[[AFI_PROTOCOL_SUBJECT]]A\n[[AFI_PROTOCOL_SCENE]]B\n[[AFI_PROTOCOL_LIGHT]]C\n[[AFI_PROTOCOL_SPATIAL]]D" })
                {
                    var sender = new Sender { Reply = body }; var result = Generate(sender, customOptions, Plan(Raw(mode)));
                    check(!(bool)Get(result, "UsedLocalFallback") && (string)Get(result, "DirectionStatus") == "complete" && ((string)Get(result, "Prompt")).Contains(body), mode + " retains nonempty composition");
                    check(sender.Bodies.Count == 1, "artistic content causes no retry");
                }
            foreach (string body in new[] { "", " \t\n", "[[AFI_PROTOCOL_TITLE]]Metadata only", "[[AFI_PROTOCOL_SUBJECT]]\n[[AFI_PROTOCOL_SCENE]]\n[[AFI_PROTOCOL_LIGHT]]\n[[AFI_PROTOCOL_SPATIAL]]" })
            {
                var sender = new Sender { Reply = body }; check(Throws(() => Generate(sender, customOptions, Plan(Raw("Director_1020")))), "empty/label-only output stops without replacement");
                check(sender.Bodies.Count == 1, "unusable output is not retried");
            }
            foreach (var sender in new[] { new Sender { Finish = "length" }, new Sender { Finish = "content_filter" }, new Sender { Status = HttpStatusCode.Unauthorized }, new Sender { Timeout = true } })
            {
                check(Throws(() => Generate(sender, customOptions, Plan(Raw("Director_1020")))), "HTTP/truncation/filter failure stops"); check(sender.Bodies.Count == 1, "failure is not retried");
            }
            var cancelled = new Sender();
            try { Generate(cancelled, customOptions, Plan(Raw("Director_1020")), token: new CancellationToken(true)); check(false, "cancellation must propagate"); }
            catch (OperationCanceledException) { check(cancelled.Bodies.Count == 0, "caller cancellation sends no request"); }
            var redraw = Generate(new Sender(), customOptions, Plan(Raw("Director_1020"), "Use a black backdrop."));
            check(!((string)Get(redraw, "Prompt")).Contains("IMMUTABLE_FACT"), "redraw does not restore superseded facts");
            Type referenceType = af.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceImage", true);
            Type referenceKind = af.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceKind", true);
            Array sceneReferences = Array.CreateInstance(referenceType, 1);
            sceneReferences.SetValue(Activator.CreateInstance(referenceType, new object[] { "iVBORw0KGgo=", "SCENE_REFERENCE", Enum.Parse(referenceKind, "MissionScreenshot") }), 0);
            var screenshotSender = new Sender { Reply = "A guard turns in the recorded courtyard." };
            var screenshotPlan = Plan(Raw("ScreenshotRules_836"));
            var screenshot = Generate(screenshotSender, customOptions, screenshotPlan, sceneReferences);
            check(!(bool)Get(screenshot, "UsedLocalFallback") && ((string)Get(screenshot, "Prompt")).Contains(screenshotSender.Reply), "free-camera keeps nonempty unstructured direction");
            check(screenshotSender.Bodies.Count == 1 && screenshotSender.Bodies[0].Contains("image_url"), "free-camera still supplies selected reference");
            var missingScene = new Sender();
            check(Throws(() => Generate(missingScene, customOptions, screenshotPlan)) && missingScene.Bodies.Count == 0, "missing scene references still stop before HTTP");
            var emptyScene = new Sender { Reply = "[[AFI_PROTOCOL_TITLE]]Metadata only" };
            check(Throws(() => Generate(emptyScene, customOptions, screenshotPlan, sceneReferences)) && emptyScene.Bodies.Count == 1, "empty scene body stops without fallback or retry");
            Set(settings, "EnableLlmPromptExpansion", false);
            var disabledSender = new Sender(); var disabled = Generate(disabledSender, Options(settings), Plan(Raw("Director_1020")));
            check((bool)Get(disabled, "UsedLocalFallback") && disabledSender.Bodies.Count == 0, "disabled director retains explicit local path");
        }
        finally { HostAvailable = true; harmony.Unpatch(host, HarmonyPatchType.All, harmony.Id); }
    }
}
