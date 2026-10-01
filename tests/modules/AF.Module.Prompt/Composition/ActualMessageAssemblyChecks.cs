using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

internal static class ActualMessageAssemblyChecks
{
    static int checks;
    static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception("FAIL " + name); }
    static string Field(object value, string name) => (string)value.GetType().GetProperty(name).GetValue(value);
    static object Call(Type type, string name, params object[] values) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, values);
    static int Main(string[] args)
    {
        try
        {
            Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))) == args[1], "candidate hash");
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                string name = new AssemblyName(e.Name).Name;
                if (name == "AnimusForge") return null; // Never resolve an old Stage product.
                foreach (string directory in args.Skip(2)) {
                    string path = Path.Combine(directory, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            Assembly product = Assembly.LoadFrom(args[0]);
            Type owner = product.GetType("AnimusForge.MainPromptMessageAssemblyOwner", true);
            Type dto = product.GetType("AnimusForge.ConversationMessage", true);
            object Message(string role, string content, string speaker, int agent = -1) {
                object result = Activator.CreateInstance(dto);
                foreach (var entry in new Dictionary<string, object> { ["Role"] = role, ["Content"] = content, ["SpeakerName"] = speaker, ["SpeakerAgentIndex"] = agent, ["TargetAgentIndex"] = 7 })
                    dto.GetProperty(entry.Key).SetValue(result, entry.Value);
                return result;
            }
            var history = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dto));
            history.Add(Message("user", "Player earlier question", "Player"));
            history.Add(Message("assistant", "Alda previous answer", "Alda", 7));
            history.Add(Message("assistant", "Borin overheard answer", "Borin", 8));
            history.Add(Message("system", "[AFEF玩家行为补充] confirmed transfer", "AFEF"));
            history.Add(Message("system", "[AFEF NPC行为补充] confirmed reply", "AFEF"));
            var reply = (IList)Call(owner, "BuildCourierReplyMessages", "Alda", "Player", "current letter", "rules", "delivery fact", "history", history, "persona", "exclude", "recent", "identity", "relationship", "location", "date", "custom");
            var inbound = (IList)Call(owner, "BuildInboundNpcLetterMessages", "Alda", "Player", "intent", "rules", "delivery fact", "history", history, "persona", "exclude", "recent", "identity", "relationship", "location", "date", 220, "private letter", "custom");
            Check(reply.Count == 8 && inbound.Count == 8, "Courier complete history cardinality");
            Check(Field(reply[0], "role") == "system" && Field(reply[0], "content").StartsWith("custom\n"), "Courier system/custom rule order");
            string[] roles = { "user", "assistant", "user", "user", "user" };
            for (int i = 0; i < history.Count; i++) {
                Check(Field(reply[i + 2], "role") == roles[i], "Courier role " + i);
                Check(Field(reply[i + 2], "content") == Field(inbound[i + 2], "content"), "Courier direction history " + i);
            }
            Check(Field(reply[5], "content").Contains("[AFEF玩家行为补充]"), "Courier player AFEF preserved");
            Check(Field(reply[6], "content").Contains("[AFEF NPC行为补充]"), "Courier NPC AFEF preserved");
            Check(Field(reply[7], "content").Contains("current letter") && Field(reply[7], "content").Contains("delivery fact"), "Courier current input/fact last");
            Type layout = owner.GetNestedType("SceneSingleSpeakerLayout", BindingFlags.NonPublic);
            foreach (string name in Enum.GetNames(layout)) {
                var actual = (string[])Call(owner, "BuildSceneSingleSpeakerPrefixSections", Enum.Parse(layout, name), "private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", "knowledge", "rules");
                string J(params string[] values) => string.Join("\n\n", values);
                string[] expected = name switch {
                    "GroupFallback" => new[] { "private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", J("knowledge", "rules") },
                    "Passive" => new[] { "private", "persisted", "runtime", "local", "trust", "misc", "patience", J("knowledge", "rules") },
                    "GroupTurn" => new[] { "private", "persisted", J("runtime", "local", "fact", "trust", "misc", "patience"), J("knowledge", "rules") },
                    "ImmediateReaction" => new[] { "private", "persisted", J("runtime", "knowledge", J(J("local", "trust", "misc"), "fact")) },
                    "CompactArrival" => new[] { "private", "persisted", J("runtime", "knowledge"), J(J("local", "trust", "misc"), "fact") },
                    _ => throw new Exception("unknown product layout")
                };
                Check(actual.SequenceEqual(expected), "actual DLL Scene layout " + name);
            }
            Type shout = product.GetType("AnimusForge.ShoutBehavior", true);
            MethodInfo convert = shout.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name == "TryConvertSceneMessageToStrictChatMessage" && m.GetParameters().Length == 5);
            // Managed-only Game/Character snapshots expose a null Hero fallback.
            // No Hero, Mission, save, or native engine is created.
            Type game = Assembly.Load("TaleWorlds.Core").GetType("TaleWorlds.Core.Game", true);
            PropertyInfo currentGame = game.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
            object previousGame = currentGame.GetValue(null);
            object emptyGame = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(game);
            Type character = Assembly.Load("TaleWorlds.CampaignSystem").GetType("TaleWorlds.CampaignSystem.CharacterObject", true);
            game.GetProperty("PlayerTroop").SetValue(emptyGame, System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(character));
            currentGame.SetValue(null, emptyGame);
            try
            {
            foreach (bool distance in new[] { false, true }) {
                for (int i = 0; i < history.Count; i++) {
                    object[] input = { history[i], 7, null, null, distance };
                    Check((bool)convert.Invoke(null, input), "real Native/Scene accepts history " + i);
                    Check(Field(input[2], "role") == roles[i], "real Native/Scene role " + distance + "/" + i);
                    if (i >= 3) Check(Field(input[2], "content").Contains(i == 3 ? "[AFEF玩家行为补充]" : "[AFEF NPC行为补充]"), "real Native/Scene AFEF " + distance + "/" + i);
                }
            }
            }
            finally { currentGame.SetValue(null, previousGame); }
            Console.WriteLine("PASS " + checks + " actual candidate DLL message/history checks; LIVE=NOT_RUN");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
