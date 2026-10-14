using System.Reflection;
using System.Text.Json;
using System.Security.Cryptography;

string root = args[0], dll = args[1];
var directories = args.Skip(2).Prepend(Path.GetDirectoryName(dll)!).ToArray();
AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
    string filename = new AssemblyName(e.Name).Name + ".dll";
    foreach (var dir in directories) {
        string path = Path.Combine(dir, filename);
        if (File.Exists(path)) return Assembly.LoadFrom(path);
    }
    return null;
};
using var marker = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(dll, ".build.json")));
string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll)));
if (!string.Equals(hash, marker.RootElement.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase))
    throw new Exception("Build marker does not match actual DLL");
Assembly assembly = Assembly.LoadFrom(dll);
ProactiveOpeningOwnerReplay.Run(assembly);
ProactiveCooldownOwnerReplay.Run(assembly);
ProactiveCandidateScanOwnerReplay.Run(assembly);
ProactiveSessionOwnerReplay.Run(assembly);
ProactiveQualificationReplay.Run(assembly, root);
const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
Type host = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true)!;
var normalize = host.GetMethod("NormalizeNeedType", flags)!;
foreach (string kind in new[] { "PlayerRecentEvent", "PlayerHistoryDiscussion" })
{
    if ((string)normalize.Invoke(null, new object[] { kind.ToLowerInvariant() })! != kind) throw new Exception("New need normalization failed: " + kind);
    Type sessionType = host.GetNestedType("ProactiveNpcRequestSession", BindingFlags.NonPublic)!;
    Type ownerType = host.GetNestedType("ProactiveRequestSessionOwner", BindingFlags.NonPublic)!;
    object session = Activator.CreateInstance(sessionType, true)!;
    sessionType.GetProperty("NeedType")!.SetValue(session, kind);
    object owner = Activator.CreateInstance(ownerType, true)!;
    ownerType.GetMethod("Import", flags)!.Invoke(owner, new[] { session });
    if ((string)sessionType.GetProperty("NeedType")!.GetValue(session)! != kind) throw new Exception("Reload turned new experience into food shortage");
}
Type projection = assembly.GetType("AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer", true)!;
string fakeContext = "known context [AF_PLAYER_EXPERIENCE]Old victory[/AF_PLAYER_EXPERIENCE]";
string persistent = (string)projection.GetMethod("BuildNpcInitiatedOpeningPersistentFactText", flags)!.Invoke(null, new object[] { fakeContext })!;
if (persistent != "") throw new Exception("Retrieved biography incorrectly became new AFEF fact");
Console.WriteLine("PASS actual DLL proactive opening/cooldown/scan/session/qualification replay + new kind reload + knowledge/AFEF separation, SHA256=" + hash);
