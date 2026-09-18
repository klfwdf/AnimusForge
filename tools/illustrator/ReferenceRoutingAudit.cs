using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

// Uses built production helpers, compiled UI call sites and intercepted HTTP payloads.
// No game state, GPU or external image provider is accessed.
public static class ReferenceRoutingAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static Type reference, kind, list, routing, portraits, optionsType;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static string Text(object value, string name) { return (string)value.GetType().GetProperty(name).GetValue(value); }
    private static object Call(Type type, string name, params object[] args) { return type.GetMethod(name, All).Invoke(null, args); }
    private static IList Refs() { return (IList)Activator.CreateInstance(list); }
    private static object Ref(string data, string label, string role)
    { return Activator.CreateInstance(reference, new object[] { data, label, Enum.Parse(kind, role) }); }
    private static void Character(IList director, IList image, string full, string head, string name)
    { Call(routing, "AddCharacter", director, image, Activator.CreateInstance(portraits, new object[] { full, head }), name, "人物【" + name + "】全身身份参考"); }
    private static void Option(object options, string name, object value)
    { optionsType.GetField("<" + name + ">k__BackingField", All).SetValue(options, value); }
    private static string Png(int index)
    {
        using (var bitmap = new Bitmap(12, 12))
        using (var memory = new MemoryStream())
        {
            using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.FromArgb(255, index * 19, index * 11, index * 7));
            bitmap.Save(memory, ImageFormat.Png); return Convert.ToBase64String(memory.ToArray());
        }
    }
    private static Dictionary<string, object> Json(string data)
    { return (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(data); }
    private static object[] ChatParts(string payload, int message)
    { return (object[])((Dictionary<string, object>)((object[])Json(payload)["messages"])[message])["content"]; }
    private static List<string> ChatImages(object[] parts)
    { return parts.Cast<Dictionary<string, object>>().Where(p => (string)p["type"] == "image_url").Select(p => (string)((Dictionary<string, object>)p["image_url"])["url"]).ToList(); }
    private static string ChatText(object[] parts)
    { return string.Join("\n", parts.Cast<Dictionary<string, object>>().Where(p => (string)p["type"] == "text").Select(p => (string)p["text"])); }

    private sealed class Handler : HttpMessageHandler
    {
        public string JsonBody, Prompt;
        public readonly List<byte[]> Images = new List<byte[]>();
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++; Images.Clear(); JsonBody = null; Prompt = null;
            var form = request.Content as MultipartFormDataContent;
            if (form == null) JsonBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            else foreach (var part in form)
            {
                string name = part.Headers.ContentDisposition.Name.Trim('"');
                if (name == "image[]") Images.Add(await part.ReadAsByteArrayAsync().ConfigureAwait(false));
                if (name == "prompt") Prompt = await part.ReadAsStringAsync().ConfigureAwait(false);
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("offline request capture") };
        }
    }

    private sealed class Instruction { public int Offset, Target; public OpCode Code; public object Operand; }
    private static List<Instruction> Instructions(MethodBase method)
    {
        var result = new List<Instruction>();
        if (method.GetMethodBody() == null) return result;
        var bytes = method.GetMethodBody().GetILAsByteArray();
        var opcodes = typeof(OpCodes).GetFields(All).Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
        for (int i = 0; i < bytes.Length;)
        {
            var item = new Instruction { Offset = i, Target = -1 };
            ushort code = bytes[i++]; if (code == 0xfe) code = (ushort)(0xfe00 | bytes[i++]);
            item.Code = opcodes[code]; int size;
            switch (item.Code.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                default: size = 4; break;
            }
            if (item.Code.OperandType == OperandType.InlineMethod)
                item.Operand = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            if (item.Code.OperandType == OperandType.InlineBrTarget) item.Target = i + size + BitConverter.ToInt32(bytes, i);
            if (item.Code.OperandType == OperandType.ShortInlineBrTarget) item.Target = i + size + (sbyte)bytes[i];
            result.Add(item); i += size;
        }
        return result;
    }
    private static bool IsCall(Instruction instruction, string name)
    { return instruction.Operand is MethodBase && ((MethodBase)instruction.Operand).Name == name; }
    private static List<Instruction> Workflow(Assembly assembly, string owner, string entry)
    {
        return assembly.GetTypes().Where(t => t.FullName.StartsWith(owner + "+") && t.Name.Contains("<" + entry + ">"))
            .SelectMany(t => t.GetMethods(All).Where(m => m.DeclaringType == t && m.Name == "MoveNext"))
            .SelectMany(Instructions).ToList();
    }
    private static void CheckWiring(Assembly assembly)
    {
        string popup = "AnimusForge.Illustrator.UI.Overlays.IllustrationCardPopup";
        string weekly = "AnimusForge.Illustrator.UI.Patches.WeeklyReportPopupIllustrationPatch";
        var encyclopedia = Workflow(assembly, popup, "ExecuteEncyclopediaGenerationCore");
        var conversation = Workflow(assembly, popup, "ExecuteConversationGenerationCore");
        var report = Workflow(assembly, weekly, "TriggerRegenerateCore");
        foreach (var workflow in new[] { encyclopedia, conversation, report })
        {
            Check(workflow.Any(i => IsCall(i, "AddCharacter")), "compiled workflow uses paired character-reference routing");
            Check(workflow.Any(i => IsCall(i, "CreateDirectionAsync")) && workflow.Any(i => IsCall(i, "GenerateImageAsync")),
                "compiled workflow routes references through both director and image client");
            Check(workflow.Any(i => IsCall(i, "ComposeToBase64Async")), "compiled workflow retains its real native emblem export path");
        }
        Check(encyclopedia.Any(i => IsCall(i, "ExtractHeroPortraitReferencesAsync")) && report.Any(i => IsCall(i, "ExtractHeroPortraitReferencesAsync")),
            "encyclopedia and weekly report request the new paired native captures");
        Check(conversation.Any(i => IsCall(i, "CaptureConversationSceneReferencesAsync")) && conversation.Any(i => IsCall(i, "AddRange")) && conversation.Any(i => IsCall(i, "SelectSceneAnchor")),
            "conversation includes the panorama list and separately selects one image anchor");
        int gate = encyclopedia.FindIndex(i => IsCall(i, "get_HasHeraldicArmor"));
        int render = encyclopedia.FindIndex(i => IsCall(i, "ComposeToBase64Async"));
        Check(gate >= 0 && render > gate && encyclopedia.Skip(gate + 1).Take(3)
            .Any(i => (i.Code == OpCodes.Brfalse || i.Code == OpCodes.Brfalse_S) && i.Target > encyclopedia[render].Offset),
            "encyclopedia branches around native emblem rendering before export when no worn carrier exists");
        Check(!report.Any(i => IsCall(i, "get_HasHeraldicArmor")), "encyclopedia-only carrier gate does not remove weekly event emblem references");

        var extractor = assembly.GetType("AnimusForge.Illustrator.Context.HeroVisualExtractor", true);
        var carrier = extractor.GetMethod("IsHeraldicArmorSlot", All);
        Check(carrier != null, "heraldic carrier test includes actual worn slots");
        var slotType = carrier.GetParameters()[0].ParameterType;
        foreach (string slot in new[] { "Head", "Body", "Leg", "Gloves", "Cape" })
        {
            object value = Enum.Parse(slotType, slot);
            Check((bool)carrier.Invoke(null, new object[] { value, true }), "tableau-worn carrier accepted: " + slot);
            Check(!(bool)carrier.Invoke(null, new object[] { value, false }), "ordinary worn item does not request an emblem: " + slot);
        }
        foreach (string slot in new[] { "Weapon0", "ExtraWeaponSlot", "Horse", "HorseHarness" })
            Check(!(bool)carrier.Invoke(null, new object[] { Enum.Parse(slotType, slot), true }), "non-armor inventory is not a portrait emblem carrier: " + slot);
        var armorCalls = Instructions(extractor.GetMethod("ExtractArmorSlot", All));
        Check(armorCalls.Any(i => IsCall(i, "get_IsUsingTableau")) && armorCalls.Any(i => IsCall(i, "IsHeraldicArmorSlot")) && armorCalls.Any(i => IsCall(i, "set_HasHeraldicArmor")),
            "real armor extraction wires the native tableau flag into the worn-carrier predicate");
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        reference = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceImage", true);
        kind = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceKind", true);
        list = typeof(List<>).MakeGenericType(reference);
        routing = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceRouting", true);
        portraits = assembly.GetType("AnimusForge.Illustrator.Engine.CharacterPortraitReferences", true);
        optionsType = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        var directorRefs = Refs(); var imageRefs = Refs();
        Character(directorRefs, imageRefs, "full", "head", "测试人物");
        Check(directorRefs.Count == 2 && imageRefs.Count == 2 && ReferenceEquals(directorRefs[0], imageRefs[0]) && ReferenceEquals(directorRefs[1], imageRefs[1]),
            "both endpoints receive the same full-body and head-detail identities");
        Check(Text(imageRefs[1], "Label").Contains("同一个人") && Text(imageRefs[1], "Label").Contains("保留装备遮挡") && Text(imageRefs[1], "Label").Contains("不是新增人物"),
            "head detail has explicit same-person and equipment-cover instructions");
        Check(imageRefs[0].GetType().GetProperty("Kind").GetValue(imageRefs[0]).ToString() == "Character" &&
            imageRefs[1].GetType().GetProperty("Kind").GetValue(imageRefs[1]).ToString() == "CharacterDetail",
            "head detail uses its own role and never increments full-body person count");
        Check(Convert.ToInt32(Enum.Parse(kind, "Character")) == 1 && Convert.ToInt32(Enum.Parse(kind, "Emblem")) == 2 && Convert.ToInt32(Enum.Parse(kind, "Scene")) == 3,
            "adding detail role preserves previously recorded reference-kind values");
        var shared = Refs(); Character(shared, shared, "full", "head", "人物");
        Check(shared.Count == 2, "aliased destination lists never duplicate either reference");
        var missing = Refs(); Character(missing, null, null, "orphan head", "人物");
        Check(missing.Count == 0, "orphaned head image is not introduced as another person");
        Character(missing, null, "full", null, "人物"); Check(missing.Count == 1, "full-body-only fallback stays usable");
        Check(Call(routing, "SelectSceneAnchor", new object[] { null }) == null && Call(routing, "SelectSceneAnchor", Refs()) == null,
            "unavailable scene does not invent an anchor");
        var scenes = Refs(); scenes.Add(null); scenes.Add(Ref("wrong", "identity", "Character")); scenes.Add(Ref(" ", "empty scene", "Scene"));
        var current = Ref(Png(1), "当前玩家视角", "Scene"); scenes.Add(current);
        for (int i = 2; i <= 5; i++) scenes.Add(Ref(Png(i), "ENVIRONMENT_VIEW_" + i, "Scene"));
        var anchor = Call(routing, "SelectSceneAnchor", scenes);
        Check(Text(anchor, "Base64Image") == Text(current, "Base64Image") && !ReferenceEquals(anchor, current), "first valid real scene becomes an independent annotated anchor");
        Check(Text(anchor, "Label").Contains("实际建筑布局") && Text(anchor, "Label").Contains("不复制UI") && Text(anchor, "Label").Contains("不作为必须保留的像素底图"),
            "scene anchor requests reconstruction without screenshot copying");

        directorRefs = Refs(); imageRefs = Refs();
        foreach (var scene in scenes.Cast<object>().Skip(3)) directorRefs.Add(scene);
        imageRefs.Add(anchor);
        Character(directorRefs, imageRefs, Png(6), Png(7), "玩家");
        Character(directorRefs, imageRefs, Png(8), Png(9), "对话对象");
        directorRefs.Add(Ref(Png(10), "双方已确认载体的纹章", "Emblem")); imageRefs.Add(directorRefs[directorRefs.Count - 1]);
        Check(directorRefs.Count == 10 && imageRefs.Count == 6, "five scene views go to director and one scene with four character views plus emblem go to image");

        var options = FormatterServices.GetUninitializedObject(optionsType);
        Option(options, "EnableImageGeneration", true); Option(options, "EnableReferenceImageForGeneration", true);
        Option(options, "UseExactEndpointUrl", true); Option(options, "ModelName", "offline-audit");
        Option(options, "SelectedStyle", "classic-oil"); Option(options, "ImageSize", "1024x1024");
        Option(options, "DirectorModelName", "offline-director");
        var director = assembly.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        var plan = Activator.CreateInstance(assembly.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true),
            new object[] { "最近三轮对话联动的场景插画", "现场只有玩家与对话对象两人。", "据实重绘环境。" });
        var directorPayload = Call(director, "BuildDirectorPayload", plan, options, directorRefs, false);
        var directorParts = ChatParts(directorPayload.ToString(), 1);
        var directorImages = ChatImages(directorParts);
        Check(directorImages.Count == 10 && directorImages.Distinct().Count() == 10, "actual director payload keeps every supplied camera and identity exactly once");
        Check(ChatText(directorParts).Contains("ENVIRONMENT_VIEW_5") && ChatText(directorParts).Contains("同一个人"), "director payload preserves view labels and same-person head labels");

        var client = assembly.GetType("AnimusForge.Illustrator.Core.UniversalOpenAiImageClient", true);
        var generate = client.GetMethod("GenerateImageAsync", All, null, new[] { typeof(string), typeof(IReadOnlyList<>).MakeGenericType(reference), optionsType, typeof(CancellationToken) }, null);
        var httpField = client.GetField("HttpClient", All); var original = httpField.GetValue(null);
        var handler = new Handler();
        using (var http = new HttpClient(handler))
        {
            httpField.SetValue(null, http);
            try
            {
                foreach (string route in new[] { "chat/completions", "images/edits" })
                {
                    Option(options, "ApiBaseUrl", "http://offline.invalid/v1/" + route);
                    int requests = handler.Requests;
                    ((Task)generate.Invoke(null, new object[] { "画面只有玩家与对话对象两人，依据导演行动与实际环境统一重绘。", imageRefs, options, CancellationToken.None })).GetAwaiter().GetResult();
                    Check(handler.Requests == requests + 1, "single intercepted request for " + route);
                    if (route.StartsWith("chat"))
                    {
                        var parts = ChatParts(handler.JsonBody, 0); var images = ChatImages(parts); var prompt = ChatText(parts);
                        Check(images.Count == 6 && images.Distinct().Count() == 6, "Chat sends one scene, four distinct character views and the emblem once");
                        Check(images.Any(x => x.EndsWith(Png(1))) && !images.Any(x => x.EndsWith(Png(5))), "Chat includes only the current-scene anchor");
                        Check(prompt.Contains("同一个人") && prompt.Contains("保留装备遮挡") && prompt.Contains("实际建筑布局"), "Chat keeps identity pairing and scene geometry labels");
                        Check(prompt.Contains("【人物身份参考图 1】") && prompt.Contains("【人物身份参考图 2】") && !prompt.Contains("【人物身份参考图 3】") &&
                            prompt.Split(new[] { "【同名人物头肩细节补充，不增加人物数量】" }, StringSplitOptions.None).Length - 1 == 2,
                            "Chat identifies exactly two people and labels their two detail images separately");
                    }
                    else
                    {
                        Check(handler.Images.Count == 6, "Edits uploads six real image parts");
                        Check(handler.Images.Select(Convert.ToBase64String).SequenceEqual(imageRefs.Cast<object>().Select(x => Text(x, "Base64Image"))),
                            "Edits uploads exactly the routed bytes in reference-label order");
                        Check(handler.Prompt.Contains("同一个人") && handler.Prompt.Contains("实际建筑布局") && !handler.Prompt.Contains("ENVIRONMENT_VIEW_5"),
                            "Edits labels head views as same identity and exposes only one scene");
                        Check(handler.Prompt.Split(new[] { "同名人物头肩细节补充，不增加画面人物数量。" }, StringSplitOptions.None).Length - 1 == 2,
                            "Edits gives both head images a detail role without introducing new people");
                    }
                }
            }
            finally { httpField.SetValue(null, original); }
        }
        CheckWiring(assembly);
        Console.WriteLine("REFERENCE ROUTING AUDIT: " + checks + " PASS / 0 FAIL (no GPU or external API)");
    }
}
