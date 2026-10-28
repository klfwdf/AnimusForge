using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Harmony;

public class SubModule : MBSubModuleBase
{
	private const uint COLOR_RED = 16711680u;

	private const uint COLOR_ORANGE = 16744448u;

	private const string SWarningTitle = "{=qZXqV8GzUH}Warning from Bannerlord.Harmony!";

	private const string SErrorHarmonyNotFound = "{=EEVJa5azpB}Bannerlord.Harmony module was not found!";

	private const string SErrorHarmonyNotFirst = "{=NxkNTUUV32}Bannerlord.Harmony is not first in loading order!{EXPECT_ISSUES_WARNING}";

	private const string SErrorHarmonyWrongVersion = "{=Z4d2nSD38a}Loaded 0Harmony.dll version is wrong!{NL}Expected {P_VERSION}, but got {E_VERSION}!{EXPECT_ISSUES_WARNING}";

	private const string SErrorHarmonyLoadedFromAnotherPlace = "{=ASjx7sqkJs}0Harmony.dll was loaded from another location: {LOCATION}!{NL}It may be caused by a custom launcher or some other mod!{EXPECT_ISSUES_WARNING}";

	private const string SWarningExpectIssues = "{=xTeLdSrXk4}{NL}This is not recommended. Expect issues!{NL}If your game crashes and you had this warning, please, mention it in the bug report!";

	private const string SWarningMinVersion = "{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}";

	private static readonly HarmonyRef Harmony = new HarmonyRef("Bannerlord.Harmony.GauntletUISubModule");

	private static readonly HarmonyRef Harmony2 = new HarmonyRef("Bannerlord.Harmony.UnpatchAll");

	private readonly DebugUI _debugUI = new DebugUI();

	private static TextObject? GetExpectIssuesWarning()
	{
		return new TextObject("{=xTeLdSrXk4}{NL}This is not recommended. Expect issues!{NL}If your game crashes and you had this warning, please, mention it in the bug report!").SetTextVariable("NL", Environment.NewLine);
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		ValidateHarmony();
		Harmony2.Patch(SymbolExtensions2.GetMethodInfo((HarmonyLib.Harmony x) => x.UnpatchAll()), new HarmonyMethod(typeof(SubModule), "UnpatchAllPrefix"));
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		Harmony.Patch(AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeInitialModuleScreenSetAsRoot"), null, new HarmonyMethod(typeof(SubModule), "OnBeforeInitialModuleScreenSetAsRootPostfix"));
		ValidateGameVersion();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		if ((Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl)) && (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt)) && Input.IsKeyPressed(InputKey.H))
		{
			_debugUI.Visible = !_debugUI.Visible;
		}
		_debugUI.Update(dt);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
	{
		if (__instance.GetType().Name.Contains("GauntletUISubModule"))
		{
			ValidateLoadOrder();
			Harmony.Unpatch(AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeInitialModuleScreenSetAsRoot"), HarmonyPatchType.All, Harmony.Id);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool UnpatchAllPrefix(string? harmonyID)
	{
		return harmonyID != null;
	}

	private static void ValidateLoadOrder()
	{
		List<ModuleInfoExtendedHelper> list = ModuleInfoHelper.GetLoadedModules().ToList();
		ModuleInfoExtendedHelper moduleInfoExtendedHelper = list.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == "Bannerlord.Harmony");
		int num = (((object)moduleInfoExtendedHelper != null) ? list.IndexOf(moduleInfoExtendedHelper) : (-1));
		if (num == -1)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EEVJa5azpB}Bannerlord.Harmony module was not found!").ToString(), Color.FromUint(16711680u)));
		}
		if (num != 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NxkNTUUV32}Bannerlord.Harmony is not first in loading order!{EXPECT_ISSUES_WARNING}").SetTextVariable("EXPECT_ISSUES_WARNING", GetExpectIssuesWarning())?.ToString() ?? "ERROR", Color.FromUint(16711680u)));
		}
	}

	private static void ValidateHarmony()
	{
		Type typeFromHandle = typeof(HarmonyMethod);
		Assembly assembly = typeFromHandle.Assembly;
		Version version = assembly.GetName().Version ?? new Version(0, 0);
		AssemblyMetadataAttribute assemblyMetadataAttribute = typeof(SubModule).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault((AssemblyMetadataAttribute x) => x.Key == "HarmonyVersion");
		Version version2 = ((assemblyMetadataAttribute == null) ? new Version(0, 0) : (Version.TryParse(assemblyMetadataAttribute.Value, out var result) ? result : new Version(0, 0)));
		StringBuilder sb = new StringBuilder();
		ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(typeFromHandle);
		if ((object)moduleByType == null)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			TextObject textObject = new TextObject("{=ASjx7sqkJs}0Harmony.dll was loaded from another location: {LOCATION}!{NL}It may be caused by a custom launcher or some other mod!{EXPECT_ISSUES_WARNING}");
			textObject.SetTextVariable("LOCATION", new TextObject(string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetFullPath(assembly.Location)));
			textObject.SetTextVariable("EXPECT_ISSUES_WARNING", GetExpectIssuesWarning());
			textObject.SetTextVariable("NL", Environment.NewLine);
			sb.AppendLine(textObject.ToString());
		}
		if (version2.CompareTo(version) != 0)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			TextObject textObject2 = new TextObject("{=Z4d2nSD38a}Loaded 0Harmony.dll version is wrong!{NL}Expected {P_VERSION}, but got {E_VERSION}!{EXPECT_ISSUES_WARNING}");
			textObject2.SetTextVariable("P_VERSION", new TextObject(version2.ToString()));
			textObject2.SetTextVariable("E_VERSION", new TextObject(version.ToString()));
			textObject2.SetTextVariable("EXPECT_ISSUES_WARNING", GetExpectIssuesWarning());
			textObject2.SetTextVariable("NL", Environment.NewLine);
			sb.AppendLine(textObject2.ToString());
		}
		if (sb.Length > 0)
		{
			Task.Run(() => MessageBox.Show(sb.ToString(), new TextObject("{=qZXqV8GzUH}Warning from Bannerlord.Harmony!").ToString(), MessageBoxButtons.OK));
		}
	}

	private static void ValidateGameVersion()
	{
		ApplicationVersion? applicationVersion = ApplicationVersionHelper.GameVersion();
		if (!applicationVersion.HasValue)
		{
			return;
		}
		ApplicationVersion valueOrDefault = applicationVersion.GetValueOrDefault();
		if (ApplicationVersionHelper.TryParse("v1.0.0", out var version) && ApplicationVersionHelper.TryParse("v1.2.7", out var version2))
		{
			if (ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop && valueOrDefault < version2)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}").SetTextVariable("GAME_VERSION", valueOrDefault.ToString()).SetTextVariable("MIN_GAME_VERSION", version2.ToString()).SetTextVariable("EXPECT_ISSUES_WARNING", GetExpectIssuesWarning())
					.ToString(), Color.FromUint(16744448u)));
			}
			Platform currentPlatform = ApplicationPlatform.CurrentPlatform;
			bool flag = (((uint)currentPlatform <= 1u || (uint)(currentPlatform - 5) <= 2u) ? true : false);
			if (flag && valueOrDefault < version)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}").SetTextVariable("GAME_VERSION", valueOrDefault.ToString()).SetTextVariable("MIN_GAME_VERSION", version.ToString()).SetTextVariable("EXPECT_ISSUES_WARNING", GetExpectIssuesWarning())
					.ToString(), Color.FromUint(16744448u)));
			}
		}
	}
}
