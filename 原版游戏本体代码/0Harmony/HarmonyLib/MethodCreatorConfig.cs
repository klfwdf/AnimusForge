using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x02000037 RID: 55
	internal class MethodCreatorConfig
	{
		// Token: 0x0600011D RID: 285 RVA: 0x00008FB4 File Offset: 0x000071B4
		internal MethodCreatorConfig(MethodBase original, MethodBase source, List<MethodInfo> prefixes, List<MethodInfo> postfixes, List<MethodInfo> transpilers, List<MethodInfo> finalizers, List<Infix> innerprefixes, List<Infix> innerpostfixes, bool debug)
		{
			this.original = original;
			this.source = source;
			this.prefixes = prefixes;
			this.postfixes = postfixes;
			this.transpilers = transpilers;
			this.finalizers = finalizers;
			this.innerprefixes = innerprefixes;
			this.innerpostfixes = innerpostfixes;
			this.debug = debug;
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000900C File Offset: 0x0000720C
		internal bool Prepare()
		{
			PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(this.original) ?? new PatchInfo();
			this.patchIndex = patchInfo.VersionCount + 1;
			MethodBase methodBase = this.original;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
			defaultInterpolatedStringHandler.AppendLiteral("_Patch");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.patchIndex);
			this.patch = MethodPatcherTools.CreateDynamicMethod(methodBase, defaultInterpolatedStringHandler.ToStringAndClear(), this.debug);
			if (this.patch == null)
			{
				return false;
			}
			this.injections = this.Fixes.Union(from fix in this.InnerFixes
				select fix.OuterMethod).ToDictionary((MethodInfo fix) => fix, (MethodInfo fix) => (from p in fix.GetParameters()
				select new InjectedParameter(fix, p)).ToList<InjectedParameter>());
			this.returnType = AccessTools.GetReturnedType(this.original);
			this.il = this.patch.GetILGenerator();
			this.instructions = new List<CodeInstruction>();
			return true;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00009132 File Offset: 0x00007332
		internal void AddCode(CodeInstruction code)
		{
			this.instructions.Add(code);
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00009140 File Offset: 0x00007340
		internal void AddCodes(IEnumerable<CodeInstruction> codes)
		{
			this.instructions.AddRange(codes);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000914E File Offset: 0x0000734E
		internal void AddLocal(InjectionType type, LocalBuilder local)
		{
			this.localVariables.Add(type, local);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000915D File Offset: 0x0000735D
		internal void AddLocal(string name, LocalBuilder local)
		{
			this.localVariables.Add(name, local);
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000916C File Offset: 0x0000736C
		internal LocalBuilder GetLocal(InjectionType type)
		{
			return this.localVariables[type];
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000917A File Offset: 0x0000737A
		internal LocalBuilder GetLocal(string name)
		{
			return this.localVariables[name];
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00009188 File Offset: 0x00007388
		internal bool HasLocal(string name)
		{
			LocalBuilder localBuilder;
			return this.localVariables.TryGetValue(name, out localBuilder);
		}

		// Token: 0x06000126 RID: 294 RVA: 0x000091A3 File Offset: 0x000073A3
		internal LocalBuilder DeclareLocal(Type type, bool isPinned = false)
		{
			return this.il.DeclareLocal(type, isPinned);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x000091B2 File Offset: 0x000073B2
		internal Label DefineLabel()
		{
			return this.il.DefineLabel();
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000128 RID: 296 RVA: 0x000091BF File Offset: 0x000073BF
		internal MethodBase MethodBase
		{
			get
			{
				return this.source ?? this.original;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000129 RID: 297 RVA: 0x000091D1 File Offset: 0x000073D1
		internal bool OriginalIsStatic
		{
			get
			{
				return this.original.IsStatic;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600012A RID: 298 RVA: 0x000091DE File Offset: 0x000073DE
		internal IEnumerable<MethodInfo> Fixes
		{
			get
			{
				return this.prefixes.Union(this.postfixes).Union(this.finalizers);
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600012B RID: 299 RVA: 0x000091FC File Offset: 0x000073FC
		internal IEnumerable<Infix> InnerFixes
		{
			get
			{
				return this.innerprefixes.Union(this.innerpostfixes);
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00009210 File Offset: 0x00007410
		internal IEnumerable<InjectedParameter> InjectionsFor(MethodInfo fix, InjectionType type = InjectionType.Unknown)
		{
			List<InjectedParameter> list;
			if (!this.injections.TryGetValue(fix, out list))
			{
				return Array.Empty<InjectedParameter>();
			}
			if (type != InjectionType.Unknown)
			{
				return from pair in list
					where pair.injectionType == type
					select pair;
			}
			return list;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000925C File Offset: 0x0000745C
		internal bool AnyFixHas(InjectionType type)
		{
			return this.injections.Values.SelectMany((List<InjectedParameter> list) => list).Any((InjectedParameter pair) => pair.injectionType == type);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x000092B8 File Offset: 0x000074B8
		internal void WithFixes(Action<MethodInfo> action)
		{
			foreach (MethodInfo fix in this.Fixes)
			{
				action(fix);
			}
			foreach (Infix fix2 in this.InnerFixes)
			{
				action(fix2.OuterMethod);
			}
		}

		// Token: 0x040000B6 RID: 182
		internal readonly MethodBase original;

		// Token: 0x040000B7 RID: 183
		internal readonly MethodBase source;

		// Token: 0x040000B8 RID: 184
		internal readonly List<MethodInfo> prefixes;

		// Token: 0x040000B9 RID: 185
		internal readonly List<MethodInfo> postfixes;

		// Token: 0x040000BA RID: 186
		internal readonly List<MethodInfo> transpilers;

		// Token: 0x040000BB RID: 187
		internal readonly List<MethodInfo> finalizers;

		// Token: 0x040000BC RID: 188
		internal readonly List<Infix> innerprefixes;

		// Token: 0x040000BD RID: 189
		internal readonly List<Infix> innerpostfixes;

		// Token: 0x040000BE RID: 190
		internal readonly bool debug;

		// Token: 0x040000BF RID: 191
		internal int patchIndex;

		// Token: 0x040000C0 RID: 192
		internal DynamicMethodDefinition patch;

		// Token: 0x040000C1 RID: 193
		internal Dictionary<MethodInfo, List<InjectedParameter>> injections;

		// Token: 0x040000C2 RID: 194
		internal Type returnType;

		// Token: 0x040000C3 RID: 195
		internal ILGenerator il;

		// Token: 0x040000C4 RID: 196
		internal List<CodeInstruction> instructions;

		// Token: 0x040000C5 RID: 197
		internal LocalBuilder[] originalVariables;

		// Token: 0x040000C6 RID: 198
		internal VariableState localVariables;

		// Token: 0x040000C7 RID: 199
		internal LocalBuilder resultVariable;

		// Token: 0x040000C8 RID: 200
		internal Label? skipOriginalLabel;

		// Token: 0x040000C9 RID: 201
		internal LocalBuilder runOriginalVariable;

		// Token: 0x040000CA RID: 202
		internal LocalBuilder exceptionVariable;

		// Token: 0x040000CB RID: 203
		internal LocalBuilder finalizedVariable;
	}
}
