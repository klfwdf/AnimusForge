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

namespace Bannerlord.Harmony
{
	// Token: 0x02000008 RID: 8
	[NullableContext(1)]
	[Nullable(0)]
	public class SubModule : MBSubModuleBase
	{
		// Token: 0x06000013 RID: 19 RVA: 0x00002C22 File Offset: 0x00000E22
		[NullableContext(2)]
		private static TextObject GetExpectIssuesWarning()
		{
			return new TextObject("{=xTeLdSrXk4}{NL}This is not recommended. Expect issues!{NL}If your game crashes and you had this warning, please, mention it in the bug report!", null).SetTextVariable("NL", Environment.NewLine);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002C40 File Offset: 0x00000E40
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			SubModule.ValidateHarmony();
			SubModule.Harmony2.Patch(SymbolExtensions2.GetMethodInfo<Harmony>((Harmony x) => x.UnpatchAll(null)), new HarmonyMethod(typeof(SubModule), "UnpatchAllPrefix", null), null, null, null);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002CD8 File Offset: 0x00000ED8
		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			base.OnBeforeInitialModuleScreenSetAsRoot();
			SubModule.Harmony.Patch(AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeInitialModuleScreenSetAsRoot", null, null, true), null, new HarmonyMethod(typeof(SubModule), "OnBeforeInitialModuleScreenSetAsRootPostfix", null), null, null);
			SubModule.ValidateGameVersion();
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002D30 File Offset: 0x00000F30
		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			bool flag = (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl)) && (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt)) && Input.IsKeyPressed(InputKey.H);
			if (flag)
			{
				this._debugUI.Visible = !this._debugUI.Visible;
			}
			this._debugUI.Update(dt);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002DA8 File Offset: 0x00000FA8
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void OnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
		{
			bool flag = __instance.GetType().Name.Contains("GauntletUISubModule");
			if (flag)
			{
				SubModule.ValidateLoadOrder();
				SubModule.Harmony.Unpatch(AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeInitialModuleScreenSetAsRoot", null, null, true), HarmonyPatchType.All, SubModule.Harmony.Id);
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002E04 File Offset: 0x00001004
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static bool UnpatchAllPrefix(string harmonyID)
		{
			return harmonyID != null;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002E0C File Offset: 0x0000100C
		private static void ValidateLoadOrder()
		{
			List<ModuleInfoExtendedHelper> loadedModules = ModuleInfoHelper.GetLoadedModules().ToList<ModuleInfoExtendedHelper>();
			ModuleInfoExtendedHelper harmonyModule = loadedModules.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == "Bannerlord.Harmony");
			int harmonyModuleIndex = ((harmonyModule != null) ? loadedModules.IndexOf(harmonyModule) : (-1));
			bool flag = harmonyModuleIndex == -1;
			if (flag)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EEVJa5azpB}Bannerlord.Harmony module was not found!", null).ToString(), Color.FromUint(16711680U)));
			}
			bool flag2 = harmonyModuleIndex != 0;
			if (flag2)
			{
				TextObject textObject = new TextObject("{=NxkNTUUV32}Bannerlord.Harmony is not first in loading order!{EXPECT_ISSUES_WARNING}", null).SetTextVariable("EXPECT_ISSUES_WARNING", SubModule.GetExpectIssuesWarning());
				InformationManager.DisplayMessage(new InformationMessage(((textObject != null) ? textObject.ToString() : null) ?? "ERROR", Color.FromUint(16711680U)));
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002EDC File Offset: 0x000010DC
		private static void ValidateHarmony()
		{
			Type harmonyType = typeof(HarmonyMethod);
			Assembly currentExistingHarmony = harmonyType.Assembly;
			Version currentHarmonyVersion = currentExistingHarmony.GetName().Version ?? new Version(0, 0);
			AssemblyMetadataAttribute attr = typeof(SubModule).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault((AssemblyMetadataAttribute x) => x.Key == "HarmonyVersion");
			Version v;
			Version requiredHarmonyVersion = ((attr != null) ? (Version.TryParse(attr.Value, out v) ? v : new Version(0, 0)) : new Version(0, 0));
			StringBuilder sb = new StringBuilder();
			ModuleInfoExtendedHelper harmonyModule = ModuleInfoHelper.GetModuleByType(harmonyType);
			bool flag = harmonyModule == null;
			if (flag)
			{
				bool flag2 = sb.Length != 0;
				if (flag2)
				{
					sb.AppendLine();
				}
				TextObject textObject = new TextObject("{=ASjx7sqkJs}0Harmony.dll was loaded from another location: {LOCATION}!{NL}It may be caused by a custom launcher or some other mod!{EXPECT_ISSUES_WARNING}", null);
				textObject.SetTextVariable("LOCATION", new TextObject(string.IsNullOrEmpty(currentExistingHarmony.Location) ? string.Empty : Path.GetFullPath(currentExistingHarmony.Location), null));
				textObject.SetTextVariable("EXPECT_ISSUES_WARNING", SubModule.GetExpectIssuesWarning());
				textObject.SetTextVariable("NL", Environment.NewLine);
				sb.AppendLine(textObject.ToString());
			}
			bool flag3 = requiredHarmonyVersion.CompareTo(currentHarmonyVersion) != 0;
			if (flag3)
			{
				bool flag4 = sb.Length != 0;
				if (flag4)
				{
					sb.AppendLine();
				}
				TextObject textObject2 = new TextObject("{=Z4d2nSD38a}Loaded 0Harmony.dll version is wrong!{NL}Expected {P_VERSION}, but got {E_VERSION}!{EXPECT_ISSUES_WARNING}", null);
				textObject2.SetTextVariable("P_VERSION", new TextObject(requiredHarmonyVersion.ToString(), null));
				textObject2.SetTextVariable("E_VERSION", new TextObject(currentHarmonyVersion.ToString(), null));
				textObject2.SetTextVariable("EXPECT_ISSUES_WARNING", SubModule.GetExpectIssuesWarning());
				textObject2.SetTextVariable("NL", Environment.NewLine);
				sb.AppendLine(textObject2.ToString());
			}
			bool flag5 = sb.Length > 0;
			if (flag5)
			{
				Task.Run<DialogResult>(() => MessageBox.Show(sb.ToString(), new TextObject("{=qZXqV8GzUH}Warning from Bannerlord.Harmony!", null).ToString(), MessageBoxButtons.OK));
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000310C File Offset: 0x0000130C
		private static void ValidateGameVersion()
		{
			ApplicationVersion? applicationVersion = ApplicationVersionHelper.GameVersion();
			ApplicationVersion gameVersion;
			int num;
			if (applicationVersion != null)
			{
				gameVersion = applicationVersion.GetValueOrDefault();
				num = 1;
			}
			else
			{
				num = 0;
			}
			bool flag = num == 0;
			if (!flag)
			{
				ApplicationVersion v100Version;
				bool flag2 = !ApplicationVersionHelper.TryParse("v1.0.0", out v100Version);
				if (!flag2)
				{
					ApplicationVersion v127Version;
					bool flag3 = !ApplicationVersionHelper.TryParse("v1.2.7", out v127Version);
					if (!flag3)
					{
						bool flag4 = ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop;
						if (flag4)
						{
							bool flag5 = gameVersion < v127Version;
							if (flag5)
							{
								InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}", null).SetTextVariable("GAME_VERSION", gameVersion.ToString()).SetTextVariable("MIN_GAME_VERSION", v127Version.ToString()).SetTextVariable("EXPECT_ISSUES_WARNING", SubModule.GetExpectIssuesWarning())
									.ToString(), Color.FromUint(16744448U)));
							}
						}
						Platform currentPlatform = ApplicationPlatform.CurrentPlatform;
						bool flag6 = currentPlatform <= Platform.WindowsEpic || currentPlatform - Platform.WindowsNoPlatform <= 2;
						bool flag7 = flag6;
						if (flag7)
						{
							bool flag8 = gameVersion < v100Version;
							if (flag8)
							{
								InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}", null).SetTextVariable("GAME_VERSION", gameVersion.ToString()).SetTextVariable("MIN_GAME_VERSION", v100Version.ToString()).SetTextVariable("EXPECT_ISSUES_WARNING", SubModule.GetExpectIssuesWarning())
									.ToString(), Color.FromUint(16744448U)));
							}
						}
					}
				}
			}
		}

		// Token: 0x0400000A RID: 10
		private const uint COLOR_RED = 16711680U;

		// Token: 0x0400000B RID: 11
		private const uint COLOR_ORANGE = 16744448U;

		// Token: 0x0400000C RID: 12
		private const string SWarningTitle = "{=qZXqV8GzUH}Warning from Bannerlord.Harmony!";

		// Token: 0x0400000D RID: 13
		private const string SErrorHarmonyNotFound = "{=EEVJa5azpB}Bannerlord.Harmony module was not found!";

		// Token: 0x0400000E RID: 14
		private const string SErrorHarmonyNotFirst = "{=NxkNTUUV32}Bannerlord.Harmony is not first in loading order!{EXPECT_ISSUES_WARNING}";

		// Token: 0x0400000F RID: 15
		private const string SErrorHarmonyWrongVersion = "{=Z4d2nSD38a}Loaded 0Harmony.dll version is wrong!{NL}Expected {P_VERSION}, but got {E_VERSION}!{EXPECT_ISSUES_WARNING}";

		// Token: 0x04000010 RID: 16
		private const string SErrorHarmonyLoadedFromAnotherPlace = "{=ASjx7sqkJs}0Harmony.dll was loaded from another location: {LOCATION}!{NL}It may be caused by a custom launcher or some other mod!{EXPECT_ISSUES_WARNING}";

		// Token: 0x04000011 RID: 17
		private const string SWarningExpectIssues = "{=xTeLdSrXk4}{NL}This is not recommended. Expect issues!{NL}If your game crashes and you had this warning, please, mention it in the bug report!";

		// Token: 0x04000012 RID: 18
		private const string SWarningMinVersion = "{=jhD6BVx78D}The current game version {GAME_VERSION} is not supported! Please upgrade your game to {MIN_GAME_VERSION} or higher!{EXPECT_ISSUES_WARNING}";

		// Token: 0x04000013 RID: 19
		private static readonly HarmonyRef Harmony = new HarmonyRef("Bannerlord.Harmony.GauntletUISubModule");

		// Token: 0x04000014 RID: 20
		private static readonly HarmonyRef Harmony2 = new HarmonyRef("Bannerlord.Harmony.UnpatchAll");

		// Token: 0x04000015 RID: 21
		private readonly DebugUI _debugUI = new DebugUI();
	}
}
