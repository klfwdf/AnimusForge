using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib;
using TaleWorlds.Engine;

namespace Bannerlord.Harmony
{
	// Token: 0x02000006 RID: 6
	[NullableContext(1)]
	[Nullable(0)]
	public class DebugUI
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000006 RID: 6 RVA: 0x0000209A File Offset: 0x0000029A
		// (set) Token: 0x06000007 RID: 7 RVA: 0x000020A2 File Offset: 0x000002A2
		public bool Visible { get; set; }

		// Token: 0x06000008 RID: 8 RVA: 0x000020AC File Offset: 0x000002AC
		public void Update(float dt)
		{
			bool visible = this.Visible;
			if (visible)
			{
				this.Begin();
				this.AddButtons();
				this.DisplayPatchedMethods();
				this.End();
				this._timeCounter += dt;
				bool flag = this._timeCounter >= 3f;
				if (flag)
				{
					this._timeCounter = 0f;
					this._patchesByModule.Clear();
					foreach (MethodBase originalMethod in Harmony.GetAllPatchedMethods())
					{
						Patches patches = Harmony.GetPatchInfo(originalMethod);
						bool flag2 = originalMethod == null || patches == null;
						if (!flag2)
						{
							foreach (Patch patch in patches.Prefixes)
							{
								bool flag3 = patch.PatchMethod.DeclaringType == null;
								if (!flag3)
								{
									ModuleInfoExtendedHelper moduleInfo = ModuleInfoHelper.GetModuleByType(patch.PatchMethod.DeclaringType);
									string moduleId = ((moduleInfo == null) ? "NOT WITHIN A MODULE" : moduleInfo.Id);
									Dictionary<MethodBase, DebugUI.HarmonyPatches> list;
									bool flag4 = !this._patchesByModule.TryGetValue(moduleId, out list);
									if (flag4)
									{
										list = new Dictionary<MethodBase, DebugUI.HarmonyPatches>();
										this._patchesByModule.Add(moduleId, list);
									}
									DebugUI.HarmonyPatches hPatches;
									bool flag5 = !list.TryGetValue(originalMethod, out hPatches);
									if (flag5)
									{
										hPatches = new DebugUI.HarmonyPatches();
										list.Add(originalMethod, hPatches);
									}
									hPatches.Prefixes.Add(patch);
								}
							}
							foreach (Patch patch2 in patches.Postfixes)
							{
								bool flag6 = patch2.PatchMethod.DeclaringType == null;
								if (!flag6)
								{
									ModuleInfoExtendedHelper moduleInfo2 = ModuleInfoHelper.GetModuleByType(patch2.PatchMethod.DeclaringType);
									string moduleId2 = ((moduleInfo2 == null) ? "NOT WITHIN A MODULE" : moduleInfo2.Id);
									Dictionary<MethodBase, DebugUI.HarmonyPatches> list2;
									bool flag7 = !this._patchesByModule.TryGetValue(moduleId2, out list2);
									if (flag7)
									{
										list2 = new Dictionary<MethodBase, DebugUI.HarmonyPatches>();
										this._patchesByModule.Add(moduleId2, list2);
									}
									DebugUI.HarmonyPatches hPatches2;
									bool flag8 = !list2.TryGetValue(originalMethod, out hPatches2);
									if (flag8)
									{
										hPatches2 = new DebugUI.HarmonyPatches();
										list2.Add(originalMethod, hPatches2);
									}
									hPatches2.Postfixes.Add(patch2);
								}
							}
							foreach (Patch patch3 in patches.Transpilers)
							{
								bool flag9 = patch3.PatchMethod.DeclaringType == null;
								if (!flag9)
								{
									ModuleInfoExtendedHelper moduleInfo3 = ModuleInfoHelper.GetModuleByType(patch3.PatchMethod.DeclaringType);
									string moduleId3 = ((moduleInfo3 == null) ? "NOT WITHIN A MODULE" : moduleInfo3.Id);
									Dictionary<MethodBase, DebugUI.HarmonyPatches> list3;
									bool flag10 = !this._patchesByModule.TryGetValue(moduleId3, out list3);
									if (flag10)
									{
										list3 = new Dictionary<MethodBase, DebugUI.HarmonyPatches>();
										this._patchesByModule.Add(moduleId3, list3);
									}
									DebugUI.HarmonyPatches hPatches3;
									bool flag11 = !list3.TryGetValue(originalMethod, out hPatches3);
									if (flag11)
									{
										hPatches3 = new DebugUI.HarmonyPatches();
										list3.Add(originalMethod, hPatches3);
									}
									hPatches3.Transpilers.Add(patch3);
								}
							}
							foreach (Patch patch4 in patches.Finalizers)
							{
								bool flag12 = patch4.PatchMethod.DeclaringType == null;
								if (!flag12)
								{
									ModuleInfoExtendedHelper moduleInfo4 = ModuleInfoHelper.GetModuleByType(patch4.PatchMethod.DeclaringType);
									string moduleId4 = ((moduleInfo4 == null) ? "NOT WITHIN A MODULE" : moduleInfo4.Id);
									Dictionary<MethodBase, DebugUI.HarmonyPatches> list4;
									bool flag13 = !this._patchesByModule.TryGetValue(moduleId4, out list4);
									if (flag13)
									{
										list4 = new Dictionary<MethodBase, DebugUI.HarmonyPatches>();
										this._patchesByModule.Add(moduleId4, list4);
									}
									DebugUI.HarmonyPatches hPatches4;
									bool flag14 = !list4.TryGetValue(originalMethod, out hPatches4);
									if (flag14)
									{
										hPatches4 = new DebugUI.HarmonyPatches();
										list4.Add(originalMethod, hPatches4);
									}
									hPatches4.Finalizers.Add(patch4);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002578 File Offset: 0x00000778
		private void DisplayPatchedMethods()
		{
			int displayModeChoisesIndex = this._displayModeChoisesIndex;
			int num = displayModeChoisesIndex;
			if (num != 0)
			{
				if (num == 1)
				{
					bool flag = !Imgui.TreeNode("Patch List Grouped by Modules");
					if (!flag)
					{
						this.DisplayPatchListGroupedByModules();
						Imgui.TreePop();
					}
				}
			}
			else
			{
				bool flag2 = !Imgui.TreeNode("Patch List");
				if (!flag2)
				{
					this.DisplayPatchList();
					Imgui.TreePop();
				}
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000025E0 File Offset: 0x000007E0
		private void DisplayPatchList()
		{
			foreach (KeyValuePair<MethodBase, DebugUI.HarmonyPatches> tuple in this._patchesByModule.SelectMany((KeyValuePair<string, Dictionary<MethodBase, DebugUI.HarmonyPatches>> x) => x.Value))
			{
				MethodBase methodBase;
				DebugUI.HarmonyPatches harmonyPatches;
				tuple.Deconstruct(out methodBase, out harmonyPatches);
				MethodBase originalMethod = methodBase;
				DebugUI.HarmonyPatches patches = harmonyPatches;
				DebugUI.DisplayOriginalWithPatches(originalMethod, patches);
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002668 File Offset: 0x00000868
		private void DisplayPatchListGroupedByModules()
		{
			foreach (KeyValuePair<string, Dictionary<MethodBase, DebugUI.HarmonyPatches>> tuple in this._patchesByModule)
			{
				string text;
				Dictionary<MethodBase, DebugUI.HarmonyPatches> dictionary;
				tuple.Deconstruct(out text, out dictionary);
				string moduleId = text;
				Dictionary<MethodBase, DebugUI.HarmonyPatches> patchesDictionary = dictionary;
				bool flag = !Imgui.TreeNode(moduleId);
				if (!flag)
				{
					foreach (KeyValuePair<MethodBase, DebugUI.HarmonyPatches> tuple2 in patchesDictionary)
					{
						MethodBase methodBase;
						DebugUI.HarmonyPatches harmonyPatches;
						tuple2.Deconstruct(out methodBase, out harmonyPatches);
						MethodBase originalMethod = methodBase;
						DebugUI.HarmonyPatches patches = harmonyPatches;
						DebugUI.DisplayOriginalWithPatches(originalMethod, patches);
					}
					Imgui.TreePop();
				}
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002738 File Offset: 0x00000938
		private static void DisplayOriginalWithPatches(MethodBase originalMethod, DebugUI.HarmonyPatches patches)
		{
			IEnumerable<Patch> allPatches = patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers).Concat(patches.Finalizers);
			bool flag = !allPatches.Any<Patch>() || !Imgui.TreeNode(originalMethod.FullDescription());
			if (!flag)
			{
				Imgui.Columns(7, "", true);
				Imgui.Text("Type");
				foreach (Patch _ in patches.Prefixes)
				{
					Imgui.Text("Prefix");
				}
				foreach (Patch _2 in patches.Postfixes)
				{
					Imgui.Text("Postfix");
				}
				foreach (Patch _3 in patches.Transpilers)
				{
					Imgui.Text("Transpiler");
				}
				foreach (Patch _4 in patches.Finalizers)
				{
					Imgui.Text("Finalizer");
				}
				Imgui.NextColumn();
				Imgui.Text("Owner");
				foreach (Patch patch in allPatches)
				{
					Imgui.Text(patch.owner);
				}
				Imgui.NextColumn();
				Imgui.Text("Method");
				foreach (Patch patch2 in allPatches)
				{
					Imgui.Text(patch2.PatchMethod.DeclaringType.FullName + "." + patch2.PatchMethod.Name);
				}
				Imgui.NextColumn();
				Imgui.Text("Index");
				foreach (Patch patch3 in allPatches)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler.AppendFormatted<int>(patch3.index);
					Imgui.Text(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				Imgui.NextColumn();
				Imgui.Text("Priority");
				foreach (Patch patch4 in allPatches)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler2.AppendFormatted<int>(patch4.priority);
					Imgui.Text(defaultInterpolatedStringHandler2.ToStringAndClear());
				}
				Imgui.NextColumn();
				Imgui.Text("Before");
				foreach (Patch patch5 in allPatches)
				{
					Imgui.Text(string.Join(",", patch5.before) ?? "");
				}
				Imgui.NextColumn();
				Imgui.Text("After");
				Imgui.Separator();
				foreach (Patch patch6 in allPatches)
				{
					Imgui.Text(string.Join(",", patch6.after) ?? "");
				}
				Imgui.Columns(1, "", true);
				Imgui.TreePop();
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002B5C File Offset: 0x00000D5C
		private void Begin()
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin(DebugUI._windowTitle);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002B70 File Offset: 0x00000D70
		private void AddButtons()
		{
			Imgui.NewLine();
			Imgui.SameLine(20f, 0f);
			bool flag = Imgui.SmallButton("Close DebugUI");
			if (flag)
			{
				this.Visible = false;
			}
			Imgui.NewLine();
			Imgui.SameLine(20f, 0f);
			Imgui.Combo("Display Mode", ref this._displayModeChoisesIndex, DebugUI._displayModeChoises);
			Imgui.NewLine();
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002BDE File Offset: 0x00000DDE
		private void End()
		{
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x04000004 RID: 4
		private static readonly string _windowTitle = "Harmony Debug UI";

		// Token: 0x04000005 RID: 5
		private static readonly string _displayModeChoises = "Patch List\0Patch List Grouped by Modules\0";

		// Token: 0x04000007 RID: 7
		private int _displayModeChoisesIndex;

		// Token: 0x04000008 RID: 8
		private float _timeCounter;

		// Token: 0x04000009 RID: 9
		private readonly Dictionary<string, Dictionary<MethodBase, DebugUI.HarmonyPatches>> _patchesByModule = new Dictionary<string, Dictionary<MethodBase, DebugUI.HarmonyPatches>>();

		// Token: 0x02000044 RID: 68
		[Nullable(0)]
		private class HarmonyPatches : IEquatable<DebugUI.HarmonyPatches>
		{
			// Token: 0x17000092 RID: 146
			// (get) Token: 0x06000401 RID: 1025 RVA: 0x0000FC5C File Offset: 0x0000DE5C
			[CompilerGenerated]
			protected virtual Type EqualityContract
			{
				[CompilerGenerated]
				get
				{
					return typeof(DebugUI.HarmonyPatches);
				}
			}

			// Token: 0x17000093 RID: 147
			// (get) Token: 0x06000402 RID: 1026 RVA: 0x0000FC68 File Offset: 0x0000DE68
			public ICollection<Patch> Prefixes { get; }

			// Token: 0x17000094 RID: 148
			// (get) Token: 0x06000403 RID: 1027 RVA: 0x0000FC70 File Offset: 0x0000DE70
			public ICollection<Patch> Postfixes { get; }

			// Token: 0x17000095 RID: 149
			// (get) Token: 0x06000404 RID: 1028 RVA: 0x0000FC78 File Offset: 0x0000DE78
			public ICollection<Patch> Transpilers { get; }

			// Token: 0x17000096 RID: 150
			// (get) Token: 0x06000405 RID: 1029 RVA: 0x0000FC80 File Offset: 0x0000DE80
			public ICollection<Patch> Finalizers { get; }

			// Token: 0x06000406 RID: 1030 RVA: 0x0000FC88 File Offset: 0x0000DE88
			[CompilerGenerated]
			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("HarmonyPatches");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append('}');
				return stringBuilder.ToString();
			}

			// Token: 0x06000407 RID: 1031 RVA: 0x0000FCD4 File Offset: 0x0000DED4
			[CompilerGenerated]
			protected virtual bool PrintMembers(StringBuilder builder)
			{
				RuntimeHelpers.EnsureSufficientExecutionStack();
				builder.Append("Prefixes = ");
				builder.Append(this.Prefixes);
				builder.Append(", Postfixes = ");
				builder.Append(this.Postfixes);
				builder.Append(", Transpilers = ");
				builder.Append(this.Transpilers);
				builder.Append(", Finalizers = ");
				builder.Append(this.Finalizers);
				return true;
			}

			// Token: 0x06000408 RID: 1032 RVA: 0x0000FD4B File Offset: 0x0000DF4B
			[NullableContext(2)]
			[CompilerGenerated]
			public static bool operator !=(DebugUI.HarmonyPatches left, DebugUI.HarmonyPatches right)
			{
				return !(left == right);
			}

			// Token: 0x06000409 RID: 1033 RVA: 0x0000FD57 File Offset: 0x0000DF57
			[NullableContext(2)]
			[CompilerGenerated]
			public static bool operator ==(DebugUI.HarmonyPatches left, DebugUI.HarmonyPatches right)
			{
				return left == right || (left != null && left.Equals(right));
			}

			// Token: 0x0600040A RID: 1034 RVA: 0x0000FD70 File Offset: 0x0000DF70
			[CompilerGenerated]
			public override int GetHashCode()
			{
				return (((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ICollection<Patch>>.Default.GetHashCode(this.<Prefixes>k__BackingField)) * -1521134295 + EqualityComparer<ICollection<Patch>>.Default.GetHashCode(this.<Postfixes>k__BackingField)) * -1521134295 + EqualityComparer<ICollection<Patch>>.Default.GetHashCode(this.<Transpilers>k__BackingField)) * -1521134295 + EqualityComparer<ICollection<Patch>>.Default.GetHashCode(this.<Finalizers>k__BackingField);
			}

			// Token: 0x0600040B RID: 1035 RVA: 0x0000FDE9 File Offset: 0x0000DFE9
			[NullableContext(2)]
			[CompilerGenerated]
			public override bool Equals(object obj)
			{
				return this.Equals(obj as DebugUI.HarmonyPatches);
			}

			// Token: 0x0600040C RID: 1036 RVA: 0x0000FDF8 File Offset: 0x0000DFF8
			[NullableContext(2)]
			[CompilerGenerated]
			public virtual bool Equals(DebugUI.HarmonyPatches other)
			{
				return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ICollection<Patch>>.Default.Equals(this.<Prefixes>k__BackingField, other.<Prefixes>k__BackingField) && EqualityComparer<ICollection<Patch>>.Default.Equals(this.<Postfixes>k__BackingField, other.<Postfixes>k__BackingField) && EqualityComparer<ICollection<Patch>>.Default.Equals(this.<Transpilers>k__BackingField, other.<Transpilers>k__BackingField) && EqualityComparer<ICollection<Patch>>.Default.Equals(this.<Finalizers>k__BackingField, other.<Finalizers>k__BackingField));
			}

			// Token: 0x0600040E RID: 1038 RVA: 0x0000FE8B File Offset: 0x0000E08B
			[CompilerGenerated]
			protected HarmonyPatches(DebugUI.HarmonyPatches original)
			{
				this.Prefixes = original.<Prefixes>k__BackingField;
				this.Postfixes = original.<Postfixes>k__BackingField;
				this.Transpilers = original.<Transpilers>k__BackingField;
				this.Finalizers = original.<Finalizers>k__BackingField;
			}

			// Token: 0x0600040F RID: 1039 RVA: 0x0000FEC5 File Offset: 0x0000E0C5
			public HarmonyPatches()
			{
				this.Prefixes = new List<Patch>();
				this.Postfixes = new List<Patch>();
				this.Transpilers = new List<Patch>();
				this.Finalizers = new List<Patch>();
				base..ctor();
			}
		}
	}
}
