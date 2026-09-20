using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Xml;
using HarmonyLib;
using TaleWorlds.Library;
using AnimusForge;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Shout;

internal static class Program
{
    private const string RegistrationOwner = "AnimusForge.DialogueUI.ContractProbe.Registration";
    private const string IsolationOwner = "AnimusForge.DialogueUI.ContractProbe.Isolation";
    private const string FixtureOwner = "AnimusForge.DialogueUI.ContractProbe.Fixture";
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly List<string> SearchDirectories = new List<string>();
    private static readonly HashSet<string> Resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<object> FixtureContexts = new HashSet<object>();
    private static readonly HashSet<object> TrackedHosts = new HashSet<object>();
    private static string _gameRoot, _afPath, _uiPath, _logsDirectory;
    private static StreamWriter _output;
    private static bool _fixtureContextValid;
    private static int _hostFinalizations;
    private static int _passed, _failed;

    // BCL-only entry point: resolution is installed before RunProbe's external types are JIT-resolved.
    private static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Usage: AnimusForge.DialogueUI.ContractProbe.exe <game-root> <installed-af.dll> <dialogue-ui.dll> <output-directory>");
            return 2;
        }
        try
        {
            _gameRoot = Path.GetFullPath(args[0]);
            _afPath = Path.GetFullPath(args[1]);
            _uiPath = Path.GetFullPath(args[2]);
            string outputRoot = Path.GetFullPath(args[3]);
            string gamePrefix = _gameRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (string.Equals(outputRoot.TrimEnd(Path.DirectorySeparatorChar), _gameRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
                || outputRoot.StartsWith(gamePrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Probe outputs must be outside the game installation.");
            if (!File.Exists(_afPath) || !File.Exists(_uiPath)) throw new FileNotFoundException("Both actual binaries are required.");
            Directory.CreateDirectory(outputRoot);
            _logsDirectory = Path.Combine(outputRoot, "isolated-logs");
            Directory.CreateDirectory(_logsDirectory);
            SearchDirectories.Add(Path.GetDirectoryName(_afPath));
            SearchDirectories.Add(Path.GetDirectoryName(_uiPath));
            SearchDirectories.Add(Path.Combine(_gameRoot, "bin", "Win64_Shipping_Client"));
            foreach (string module in new[] { "Native", "SandBox", "SandBoxCore", "StoryMode", "CustomBattle", "Bannerlord.Harmony", "Bannerlord.MBOptionScreen", "Bannerlord.UIExtenderEx", "Bannerlord.ButterLib", "AnimusForge" })
                SearchDirectories.Add(Path.Combine(_gameRoot, "Modules", module, "bin", "Win64_Shipping_Client"));
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            using (_output = new StreamWriter(Path.Combine(outputRoot, "contract-probe.log"), false))
            {
                _output.AutoFlush = true;
                try { return RunProbe(); }
                catch (Exception ex) { Write("FAIL unexpected " + Unwrap(ex)); return 1; }
            }
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string simple = new AssemblyName(args.Name).Name;
        if (simple.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) || !Resolving.Add(simple)) return null;
        try
        {
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(loaded.GetName().Name, simple, StringComparison.OrdinalIgnoreCase)) return loaded;
            if (simple == "AnimusForge") return Assembly.LoadFrom(_afPath);
            if (simple == "AnimusForge.DialogueUI") return Assembly.LoadFrom(_uiPath);
            foreach (string directory in SearchDirectories)
            {
                string candidate = Path.Combine(directory, simple + ".dll");
                if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            }
            Write("UNRESOLVED " + args.Name);
            return null;
        }
        finally { Resolving.Remove(simple); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunProbe()
    {
        Write("UTC " + DateTime.UtcNow.ToString("O") + "; CLR=" + Environment.Version + "; x64=" + Environment.Is64BitProcess);
        string afHash = Hash(_afPath), uiHash = Hash(_uiPath);
        Assembly af = Assembly.LoadFrom(_afPath), ui = Assembly.LoadFrom(_uiPath);
        Describe("AF", af, afHash);
        Describe("UI", ui, uiHash);
        InstallLogIsolation(af, ui);
        var registration = new Harmony(RegistrationOwner);
        try
        {
            VerifyRegistration(af, ui, registration);
            PrefabRegistrationContracts(ui);
            NativeWrapperContracts();
            ShoutWrapperContracts(ui);
        }
        finally
        {
            Invoke(ui.GetType("AnimusForge.DialogueUI.PresentationRouter", true), "Shutdown");
            registration.UnpatchAll(RegistrationOwner);
            new Harmony(FixtureOwner).UnpatchAll(FixtureOwner);
        }
        Check("source AF DLL unchanged", afHash == Hash(_afPath));
        Check("source UI DLL unchanged", uiHash == Hash(_uiPath));
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("TaleWorlds.") || a.GetName().Name == "0Harmony"))
            Write("RUNTIME_DEPENDENCY " + loaded.FullName + "; MVID=" + loaded.ManifestModule.ModuleVersionId + "; path=" + loaded.Location);
        Write("LIMITS: No Campaign/Mission/Agent was created; no game launched, UI rendered, texture native call, LLM request or deployment performed.");
        Write("LIMITS: Shout valid-context cases patch IsCurrent only for fixture-owned opaque contexts and inject a counted read-only history provider; this tests the actual wrapper DLL, not engine identity/lifecycle correctness.");
        Write("LIMITS: Dependency versions printed above are authoritative. Testing a 1.3 product with installed 1.4 dependencies does not establish a 1.3 game-runtime pass.");
        Write("TOTAL PASS=" + _passed + " FAIL=" + _failed);
        return _failed == 0 ? 0 : 1;
    }

    private static void InstallLogIsolation(Assembly af, Assembly ui)
    {
        var isolation = new Harmony(IsolationOwner);
        MethodInfo logs = Required(af.GetType("AnimusForge.AnimusForgeModulePaths", true), "GetLogsDirectory");
        isolation.Patch(logs, prefix: new HarmonyMethod(typeof(Program), nameof(LogDirectoryPrefix)));
        Type logger = af.GetType("AnimusForge.Logger", true);
        int isolated = 0;
        foreach (MethodInfo method in logger.GetMethods(StaticFlags).Where(m => m.Name.StartsWith("Log", StringComparison.Ordinal) && m.ReturnType == typeof(void) && !m.IsGenericMethodDefinition))
        {
            isolation.Patch(method, prefix: new HarmonyMethod(typeof(Program), nameof(SuppressedLogPrefix)));
            isolated++;
        }
        Check("AF logging isolated", isolated > 0);
        isolation.Patch(Required(ui.GetType("AnimusForge.DialogueUI.DialogueUiRuntime", true), "Log"), prefix: new HarmonyMethod(typeof(Program), nameof(SuppressedLogPrefix)));
        foreach (MethodInfo method in typeof(TaleWorlds.Library.Debug).GetMethods(StaticFlags).Where(m => m.Name.StartsWith("Print", StringComparison.Ordinal) && m.ReturnType == typeof(void) && !m.IsGenericMethodDefinition))
            isolation.Patch(method, prefix: new HarmonyMethod(typeof(Program), nameof(SuppressedLogPrefix)));
        Write("LOG_ISOLATION output=" + _logsDirectory + "; AF void Log* entries=" + isolated + "; no original logging backend invoked.");
    }

    private static bool LogDirectoryPrefix(ref string __result) { __result = _logsDirectory; return false; }
    private static bool SuppressedLogPrefix(object[] __args)
    {
        string message = string.Join(" | ", (__args ?? new object[0]).OfType<string>());
        if (!string.IsNullOrWhiteSpace(message)) Write("ISOLATED_LOG " + message);
        return false;
    }

    private static void VerifyRegistration(Assembly af, Assembly ui, Harmony harmony)
    {
        var installs = new[] { "AnimusForge.DialogueUI.Shout.ShoutUiAdapter", "AnimusForge.DialogueUI.Native.NativeUiAdapter", "AnimusForge.DialogueUI.PresentationRouter", "AnimusForge.DialogueUI.DialogueUiSprites" };
        foreach (string name in installs)
        {
            Type type = ui.GetType(name, true);
            Invoke(type, "Install", harmony);
            Write("INSTALLED " + name);
        }
        Type shout = ui.GetType(installs[0], true);
        Check("shout required reflection seam installed", (bool)shout.GetField("_installed", StaticFlags).GetValue(null));
        foreach (string name in new[] { "_packetIndex", "_tradePacket", "_tradeAgent", "_tradeActionOnly", "_targetingContext", "_conversationEpoch", "_popupDataSource", "_sceneSession", "_history", "_submit", "_cancel", "_subtitle" })
            Check("shout seam " + name, shout.GetField(name, StaticFlags)?.GetValue(null) != null);
        ExpectPatch(af.GetType("AnimusForge.ShoutBehavior", true), "OpenShoutTextInput", "DirectInputPrefix", "prefix");
        ExpectPatch(af.GetType("AnimusForge.ShoutBehavior", true), "OpenShoutTextInput", "InputFinalizer", "finalizer");
        ExpectPatch(af.GetType("AnimusForge.ShoutBehavior", true), "ShowShoutTradeChatInput", "TradeInputPrefix", "prefix");
        ExpectPatch(af.GetType("AnimusForge.ShoutBehavior", true), "ShowShoutTradeChatInput", "InputFinalizer", "finalizer");
        ExpectPatch(af.GetType("AnimusForge.ShoutTextInputPopupVM", true), "ExecuteSubmit", "SubmitPrefix", "prefix");
        ExpectPatch(af.GetType("AnimusForge.ShoutTextInputPopup", true), "Close", "PopupClosedPostfix", "postfix");
        ExpectPatch(af.GetType("AnimusForge.AnimusForgeNativeConversationOverlay", true), "IsMouseOverTopRightButtons", "MouseHitPrefix", "prefix");
        ExpectPatch(af.GetType("AnimusForge.AnimusForgeNativeConversationOverlay", true), "Close", "OverlayClosed", "postfix");
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.Conversation.MissionConversationVM"), "OnFinalize", "NativeFinalizing", "prefix");
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.GauntletUI.Data.GauntletMovie"), "Load", "LoadPrefix", "prefix");
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.GauntletUI.Data.GauntletMovie"), "Release", "MovieReleasePrefix", "prefix");
        Type movieType = AccessTools.TypeByName("TaleWorlds.GauntletUI.Data.GauntletMovie");
        ConstructorInfo constructor = movieType?.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .SingleOrDefault(c => c.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(new[] { "String", "UIContext", "WidgetFactory", "IViewModel", "Boolean" }));
        Check("partial load cleanup observes exact private movie constructor", constructor != null
            && Harmony.GetPatchInfo(constructor)?.Postfixes.Any(p => p.owner == RegistrationOwner && p.PatchMethod.Name == "MovieConstructedPostfix") == true);
        Type router = ui.GetType("AnimusForge.DialogueUI.PresentationRouter", true);
        foreach (string name in new[] { "MoviePrefabField", "MovieRootField", "ResourceChangedMethod" })
            Check("partial load cleanup seam " + name, router.GetField(name, StaticFlags)?.GetValue(null) != null);
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.Engine.GauntletUI.GauntletLayer"), "LoadMovie", "LayerLoadPostfix", "postfix");
        Type layerType = AccessTools.TypeByName("TaleWorlds.Engine.GauntletUI.GauntletLayer");
        MethodInfo privateLoad = layerType?.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .SingleOrDefault(m => m.Name == "LoadMovie" && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType.Name == "GauntletMovieIdentifier");
        Check("resource refresh seam uses private identifier LoadMovie overload", privateLoad != null
            && Harmony.GetPatchInfo(privateLoad)?.Postfixes.Any(p => p.owner == RegistrationOwner && p.PatchMethod.Name == "LayerLoadPostfix") == true);
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.Engine.GauntletUI.GauntletLayer"), "ReleaseMovie", "LayerReleasePrefix", "prefix");
        ExpectPatch(AccessTools.TypeByName("TaleWorlds.Engine.GauntletUI.UIResourceManager"), "RefreshSpriteData", "OnSpriteDataRefresh", "postfix");
        int targets = 0, count = 0;
        foreach (MethodBase target in Harmony.GetAllPatchedMethods().OrderBy(m => m.DeclaringType.FullName).ThenBy(m => m.Name))
        {
            Patches patches = Harmony.GetPatchInfo(target);
            int own = patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Finalizers).Concat(patches.Transpilers).Count(p => p.owner == RegistrationOwner);
            if (own == 0) continue;
            targets++; count += own;
            Write("PATCH " + target.DeclaringType.FullName + "." + target.Name + " count=" + own);
        }
        Check("all 13 required registration targets", targets == 13);
        Check("all 15 required registration patches", count == 15);
        Write("PATCH_TOTAL targets=" + targets + " patches=" + count + "; isolation and fixture owners excluded.");
    }

    private static void ExpectPatch(Type type, string methodName, string patchName, string kind)
    {
        bool found = false;
        if (type != null)
        {
            foreach (MethodInfo method in type.GetMethods(StaticFlags | BindingFlags.Instance).Where(m => m.Name == methodName))
            {
                Patches patches = Harmony.GetPatchInfo(method);
                if (patches == null) continue;
                IEnumerable<Patch> source = kind == "prefix" ? patches.Prefixes : kind == "postfix" ? patches.Postfixes : patches.Finalizers;
                if (source.Any(p => p.owner == RegistrationOwner && p.PatchMethod.Name == patchName)) found = true;
            }
        }
        Check("patch " + (type?.FullName ?? "MISSING_TYPE") + "." + methodName + " -> " + patchName, found);
    }

    private static void NativeWrapperContracts()
    {
        int submits = 0, switches = 0, history = 0, gifts = 0, persona = 0, tags = 0;
        string submitted = null;
        var original = new AnimusForgeNativeConversationOverlayVM(text => { submits++; submitted = text; }, () => switches++, () => history++, () => gifts++, () => persona++, () => tags++);
        var wrapper = new NativeOverlayVM(original);
        var notifications = new Dictionary<string, int>();
        wrapper.PropertyChanged += (sender, e) => { notifications[e.PropertyName] = Count(notifications, e.PropertyName) + 1; };
        wrapper.InputText = "你好，领主。";
        Check("native draft writes through original", original.InputText == wrapper.InputText && original.InputText == "你好，领主。");
        Check("native draft notifies once", Count(notifications, "InputText") == 1);
        original.SetInputVisible(true);
        Check("native visibility reflects original", wrapper.IsCustomAnswerVisible);
        Check("native visibility notification", Count(notifications, "IsCustomAnswerVisible") == 1);
        original.SetBusy(true);
        wrapper.ExecuteSubmit();
        Check("native busy rejects submission", submits == 0 && !wrapper.IsInputEnabled);
        original.SetBusy(false);
        wrapper.ExecuteSubmit();
        Check("native submit forwarded exactly once", submits == 1 && submitted == "你好，领主。");
        wrapper.SwitchTalk(); wrapper.ShowLogView(); wrapper.ShowGiveShowMenu(); wrapper.EditPersona(); wrapper.OpenTagTest();
        Check("native five tool commands each forwarded once", switches == 1 && history == 1 && gifts == 1 && persona == 1 && tags == 1);
        original.SetPersonaEditVisible(true);
        Check("native derived more action state updates", wrapper.HasMoreActions && Count(notifications, "HasMoreActions") == 1);
        wrapper.ToggleMore();
        Check("native more opens", wrapper.IsMoreVisible);
        wrapper.ShowLogView();
        Check("native tool action closes more and remains unique", !wrapper.IsMoreVisible && history == 2);
        original.RequestInputFocus();
        original.AIChatboxOffset = 12f;
        Check("native int and float notifications", Count(notifications, "InputFocusVersion") == 1 && Count(notifications, "AIChatboxOffset") == 1);
        wrapper.OnFinalize(); wrapper.OnFinalize();
        notifications.Clear();
        original.InputText = "仍由原VM持有";
        original.SetBusy(true); original.SetPersonaEditVisible(false); original.RequestInputFocus(); original.AIChatboxOffset = 15f;
        Check("native all subscribed notification kinds detach", notifications.Count == 0);
        Check("native finalization leaves original usable", original.InputText == "仍由原VM持有" && !original.IsInputEnabled);
        original.SetBusy(false);
        wrapper.ExecuteSubmit(); wrapper.SwitchTalk(); wrapper.ShowLogView(); wrapper.ShowGiveShowMenu(); wrapper.EditPersona(); wrapper.OpenTagTest();
        wrapper.InputText = "不应写回已释放宿主";
        Check("native disposed commands cannot reach owner", submits == 1 && switches == 1 && history == 2 && gifts == 1 && persona == 1 && tags == 1);
        Check("native disposed draft setter cannot reach owner", original.InputText == "仍由原VM持有");
    }

    private static void PrefabRegistrationContracts(Assembly ui)
    {
        string moduleRoot = null;
        var directory = new DirectoryInfo(Path.GetDirectoryName(_uiPath));
        for (int i = 0; directory != null && i < 8; i++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GUI", "Prefabs", "AFDialogueShout.xml")))
            { moduleRoot = directory.FullName; break; }
        }
        Check("probe locates actual module prefab tree", moduleRoot != null);
        if (moduleRoot == null) return;
        Type runtime = ui.GetType("AnimusForge.DialogueUI.DialogueUiRuntime", true);
        PropertyInfo rootProperty = runtime.GetProperty("ModuleRoot", StaticFlags);
        object oldRoot = rootProperty.GetValue(null);
        Type factoryType = AccessTools.TypeByName("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory");
        object factory = FormatterServices.GetUninitializedObject(factoryType);
        var customPaths = new Dictionary<string, string>();
        factoryType.GetField("_customTypePaths", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(factory, customPaths);
        MethodInfo prepare = Required(ui.GetType("AnimusForge.DialogueUI.PresentationRouter", true), "PreparePrefab");
        rootProperty.GetSetMethod(true).Invoke(null, new object[] { moduleRoot });
        try
        {
            foreach (string name in new[] { "AFDialogueShout", "AFDialogueNativeOverlay" })
            {
                Check(name + " first registration", (bool)prepare.Invoke(null, new object[] { factory, name }));
                string registered = customPaths[name];
                string expected = Path.GetFullPath(Path.Combine(moduleRoot, "GUI", "Prefabs", name + ".xml"));
                Check(name + " stores directory with trailing slash", registered.EndsWith("/", StringComparison.Ordinal) && !registered.EndsWith(".xml/", StringComparison.OrdinalIgnoreCase));
                Check(name + " directory concatenation resolves actual XML", string.Equals(Path.GetFullPath(registered + name + ".xml"), expected, StringComparison.OrdinalIgnoreCase));
                Check(name + " second registration matches owner", (bool)prepare.Invoke(null, new object[] { factory, name }) && customPaths.Count <= 2);
                customPaths[name] = Path.Combine(_logsDirectory, "foreign-owner").Replace('\\', '/') + "/";
                bool rejected = false;
                try { prepare.Invoke(null, new object[] { factory, name }); }
                catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
                Check(name + " foreign owner directory rejected", rejected);
                customPaths[name] = registered;
                var xml = new XmlDocument { XmlResolver = null };
                xml.Load(expected);
                Check(name + " root is Prefab", xml.DocumentElement?.Name == "Prefab");
                int bindings = 0, commands = 0;
                ValidateBindings(xml.DocumentElement, name == "AFDialogueShout" ? typeof(ShoutPresentationVM) : typeof(NativeOverlayVM), name, ref bindings, ref commands);
                Check(name + " property and command metadata present", bindings > 0 && commands > 0);
                Write("PREFAB_METADATA " + name + " bindings=" + bindings + " commands=" + commands + "; no UIContext/native widget construction.");
            }
        }
        finally { rootProperty.GetSetMethod(true).Invoke(null, new[] { oldRoot }); }
    }

    private static void ValidateBindings(XmlNode node, Type scope, string prefab, ref int bindings, ref int commands)
    {
        if (node is XmlElement element)
        {
            string source = element.GetAttribute("DataSource");
            if (source == "{Host}")
            {
                Check(prefab + " Host section owned by shout wrapper", scope == typeof(ShoutPresentationVM));
                scope = typeof(ShoutTextInputPopupVM);
            }
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute.Name.StartsWith("Command.", StringComparison.Ordinal))
                {
                    commands++;
                    Check(prefab + " " + scope.Name + "." + attribute.Value + " command", scope.GetMethod(attribute.Value, BindingFlags.Public | BindingFlags.Instance) != null);
                }
                else if (attribute.Value.StartsWith("@", StringComparison.Ordinal))
                {
                    bindings++;
                    string property = attribute.Value.Substring(1);
                    Check(prefab + " " + scope.Name + "." + property + " binding", scope.GetProperty(property, BindingFlags.Public | BindingFlags.Instance) != null);
                }
            }
        }
        foreach (XmlNode child in node.ChildNodes) ValidateBindings(child, scope, prefab, ref bindings, ref commands);
    }

    private static void ShoutWrapperContracts(Assembly ui)
    {
        Type adapter = typeof(ShoutUiAdapter);
        Type contextType = adapter.GetNestedType("ShoutContext", BindingFlags.NonPublic);
        var fixture = new Harmony(FixtureOwner);
        fixture.Patch(Required(contextType, "IsCurrent", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(Program), nameof(ContextAvailabilityPrefix)));
        fixture.Patch(Required(typeof(ViewModel), "OnFinalize", BindingFlags.Instance | BindingFlags.Public), prefix: new HarmonyMethod(typeof(Program), nameof(TrackHostFinalizationPrefix)));
        Write("FIXTURE controlled IsCurrent and history provider for wrapper-only cases; no Mission/Agent created.");
        object context = FormatterServices.GetUninitializedObject(contextType);
        contextType.GetField("<AgentIndex>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(context, 37);
        Check("shout actual empty context is invalid without fixture override", !(bool)Required(contextType, "IsCurrent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, null));
        FixtureContexts.Add(context);
        int submitted = 0, cancelled = 0, historyReads = 0, historyAgent = -1, historyLimit = -1;
        string sentText = null;
        var host = new ShoutTextInputPopupVM("测试对象", "给予摘要", "输入", "第一行\n第二行", text => { submitted++; sentText = text; }, () => cancelled++);
        TrackedHosts.Add(host);
        ViewModel presentation;
        Check("shout no caller scope keeps original popup", !ShoutUiAdapter.TryWrap(host, out presentation) && presentation == null);
        FieldInfo historyField = adapter.GetField("_history", StaticFlags);
        object savedHistory = historyField.GetValue(null);
        historyField.SetValue(null, new Func<int, int, List<string>>((agent, limit) => { historyReads++; historyAgent = agent; historyLimit = limit; return new List<string> { "你：上一句", "对象：回应" }; }));
        FieldInfo opening = adapter.GetField("_openingContext", StaticFlags);
        try
        {
            _fixtureContextValid = true;
            opening.SetValue(null, context);
            Check("shout scoped host wraps", ShoutUiAdapter.TryWrap(host, out presentation));
            opening.SetValue(null, null);
            var wrapper = presentation as ShoutPresentationVM;
            Check("shout actual presentation type", wrapper != null);
            if (wrapper == null) return;
            Check("shout original VM retained as Host", ReferenceEquals(wrapper.Host, host));
            ViewModel reloaded;
            Check("shout resource refresh reuses live wrapper outside opening scope", ShoutUiAdapter.TryWrap(host, out reloaded) && ReferenceEquals(wrapper, reloaded));
            Check("shout trade summary retained", wrapper.HasSubtitle && host.SubtitleText == "给予摘要");
            Check("shout history remains lazy", historyReads == 0);
            wrapper.ExecuteToggleHistory();
            Check("shout history binds original agent and 260 limit", historyReads == 1 && historyAgent == 37 && historyLimit == 260);
            Check("shout history drawer displays provider output", wrapper.IsHistoryVisible && wrapper.HistoryText.Contains("对象：回应"));
            wrapper.ExecuteToggleHistory(); wrapper.ExecuteToggleHistory();
            Check("shout history read once across reopen", historyReads == 1);
            Check("shout drawer preserves multiline draft", host.InputText == "第一行\n第二行");
            wrapper.ExecuteSubmit();
            Check("shout submit forwards once to original callback", submitted == 1 && sentText == "第一行\n第二行");
            _fixtureContextValid = false;
            ShoutUiAdapter.Tick();
            wrapper.ExecuteSubmit(); host.ExecuteSubmit();
            Check("shout invalid target blocks both button and Host-enter route", submitted == 1 && !wrapper.CanSubmit);
            Check("shout invalid target preserves draft and explains state", host.InputText == "第一行\n第二行" && wrapper.StatusText.Contains("失效"));
            wrapper.ExecuteCancel();
            Check("shout invalid target still allows original cancel once", cancelled == 1);
            ShoutUiAdapter.Release(host);
            Check("shout release does not finalize original Host", _hostFinalizations == 0);
            Check("shout release drops Host reference", wrapper.Host == null);
            wrapper.ExecuteSubmit(); wrapper.ExecuteCancel(); wrapper.ExecuteToggleHistory();
            Check("shout released wrapper cannot forward commands or history", submitted == 1 && cancelled == 1 && historyReads == 1);
            Check("shout release removes active wrapper", ((ICollection)adapter.GetField("Active", StaticFlags).GetValue(null)).Count == 0);
            host.ExecuteSubmit();
            Check("shout unwrapped Host remains unmodified", submitted == 2);
            ShoutUiAdapter.Release(host);
            Check("shout release is idempotent", _hostFinalizations == 0);
        }
        finally
        {
            opening.SetValue(null, null);
            historyField.SetValue(null, savedHistory);
            ShoutUiAdapter.Release(host);
            FixtureContexts.Clear(); TrackedHosts.Clear();
        }
    }

    private static bool ContextAvailabilityPrefix(object __instance, ref bool __result)
    {
        if (!FixtureContexts.Contains(__instance)) return true;
        __result = _fixtureContextValid;
        return false;
    }
    private static void TrackHostFinalizationPrefix(object __instance) { if (TrackedHosts.Contains(__instance)) _hostFinalizations++; }
    private static int Count(Dictionary<string, int> values, string key) => values.TryGetValue(key, out int value) ? value : 0;
    private static void Check(string name, bool passed) { if (passed) _passed++; else _failed++; Write((passed ? "PASS " : "FAIL ") + name); }
    private static MethodInfo Required(Type type, string name, BindingFlags flags = StaticFlags) => type?.GetMethod(name, flags) ?? throw new MissingMethodException(type?.FullName, name);
    private static object Invoke(Type type, string name, params object[] args)
    {
        try { return Required(type, name).Invoke(null, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }
    private static Exception Unwrap(Exception ex) => ex is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : ex;
    private static void Describe(string label, Assembly assembly, string hash) => Write(label + " " + assembly.FullName + "; MVID=" + assembly.ManifestModule.ModuleVersionId + "; SHA256=" + hash + "; path=" + assembly.Location);
    private static string Hash(string file) { using (var algorithm = SHA256.Create()) using (var stream = File.OpenRead(file)) return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", ""); }
    private static void Write(string message) { Console.WriteLine(message); _output?.WriteLine(message); }
}
