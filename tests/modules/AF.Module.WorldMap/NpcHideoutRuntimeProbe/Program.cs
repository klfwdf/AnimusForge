using System;
using System.IO;
using System.Reflection;

internal static class Program
{
	private static int _checks;
	private static void Check(bool value, string name)
	{
		_checks++;
		if (!value) throw new InvalidOperationException(name);
	}
	private static int Main(string[] args)
	{
		if (args.Length < 3) throw new ArgumentException("candidate.dll reference-dir harmony.dll [runtime-dir]");
		string[] roots = { Path.GetDirectoryName(Path.GetFullPath(args[0])), Path.GetFullPath(args[1]), Path.GetDirectoryName(Path.GetFullPath(args[2])), args.Length > 3 ? Path.GetFullPath(args[3]) : "" };
		AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
		{
			string name = new AssemblyName(request.Name).Name + ".dll";
			foreach (string root in roots)
			{
				string path = Path.Combine(root, name);
				if (File.Exists(path)) return Assembly.LoadFrom(path);
			}
			return null;
		};
		Assembly production = Assembly.LoadFrom(Path.GetFullPath(args[0]));
		Type behavior = production.GetType("AnimusForge.WorldMapPartyCommandBehavior", true);
		MethodInfo parse = behavior.GetMethod("TryParseTag", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo build = behavior.GetMethod("BuildTag", BindingFlags.Static | BindingFlags.NonPublic);
		Func<string, bool, string> roundTrip = (tag, expected) =>
		{
			object[] input = { tag, false, null, false }; // Isolated protocol test: does NOT authorize a game target.
			bool result = (bool)parse.Invoke(null, input);
			Check(result == expected, "parse " + tag);
			return result && input[2] != null ? (string)build.Invoke(null, new[] { input[2] }) : "";
		};
		Check(roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1:5]", true) == "[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1:5]", "new tag roundtrip");
		Check(roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1:invalid]", true).EndsWith(":5]"), "new invalid duration defaults to five");
		roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:hero:lord_1:5]", false);
		roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1]", false);
		roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1:5:FORCE]", false);
		roundTrip("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:bad]id:5]", false);
		Check(roundTrip("[ACTION:WORLDMAP_ORDER:FOLLOW:hero:main_hero:3]", true) == "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:main_hero:3]", "legacy follow unchanged");
		Check(roundTrip("[ACTION:WORLDMAP_ORDER:ATTACK:party:party_1:3:AI]", true) == "[ACTION:WORLDMAP_ORDER:ATTACK:party:party_1:3:AI]", "legacy attack unchanged");
		Assembly harmonyAssembly = Assembly.LoadFrom(Path.GetFullPath(args[2]));
		Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
		object harmony = Activator.CreateInstance(harmonyType, "AnimusForge.Tests.NpcHideoutCompletion");
		Type adapter = production.GetType("AnimusForge.WorldMapNpcHideoutCompletionPatch", true);
		adapter.GetMethod("EnsurePatched", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { harmony });
		Check((bool)adapter.GetProperty("IsReady", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), "both native hooks install on actual reference DLL");
		Check((bool)adapter.GetMethod("BeforeFinalizePrefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { null }), "without owned Campaign context native completion falls through");
		Console.WriteLine("PASS " + _checks + " actual candidate protocol/Harmony checks; target validation and game battle NOT-RUN.");
		return 0;
	}
}
