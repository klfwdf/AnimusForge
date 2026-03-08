using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod;

namespace HarmonyLib
{
	/// <summary>The Harmony instance is the main entry to Harmony. After creating one with an unique identifier, it is used to patch and query the current application domain</summary>
	// Token: 0x0200007C RID: 124
	public class Harmony
	{
		/// <summary>The unique identifier</summary>
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000237 RID: 567 RVA: 0x0000E249 File Offset: 0x0000C449
		// (set) Token: 0x06000238 RID: 568 RVA: 0x0000E251 File Offset: 0x0000C451
		public string Id { get; private set; }

		/// <summary>Creates a new Harmony instance</summary>
		/// <param name="id">A unique identifier (you choose your own)</param>
		/// <returns>A Harmony instance</returns>
		// Token: 0x06000239 RID: 569 RVA: 0x0000E25C File Offset: 0x0000C45C
		public Harmony(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException("id cannot be null or empty");
			}
			try
			{
				string envDebug = Environment.GetEnvironmentVariable("HARMONY_DEBUG");
				if (envDebug != null && envDebug.Length > 0)
				{
					envDebug = envDebug.Trim();
					Harmony.DEBUG = envDebug == "1" || bool.Parse(envDebug);
				}
			}
			catch
			{
			}
			if (Harmony.DEBUG)
			{
				Assembly assembly = typeof(Harmony).Assembly;
				Version version = assembly.GetName().Version;
				string location = assembly.Location;
				string environment = Environment.Version.ToString();
				string platform = Environment.OSVersion.Platform.ToString();
				if (string.IsNullOrEmpty(location))
				{
					location = new Uri(assembly.CodeBase).LocalPath;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 5);
				defaultInterpolatedStringHandler.AppendLiteral("### Harmony id=");
				defaultInterpolatedStringHandler.AppendFormatted(id);
				defaultInterpolatedStringHandler.AppendLiteral(", version=");
				defaultInterpolatedStringHandler.AppendFormatted<Version>(version);
				defaultInterpolatedStringHandler.AppendLiteral(", location=");
				defaultInterpolatedStringHandler.AppendFormatted(location);
				defaultInterpolatedStringHandler.AppendLiteral(", env/clr=");
				defaultInterpolatedStringHandler.AppendFormatted(environment);
				defaultInterpolatedStringHandler.AppendLiteral(", platform=");
				defaultInterpolatedStringHandler.AppendFormatted(platform);
				FileLog.Log(defaultInterpolatedStringHandler.ToStringAndClear());
				MethodBase callingMethod = AccessTools.GetOutsideCaller();
				if (callingMethod.DeclaringType != null)
				{
					Assembly callingAssembly = callingMethod.DeclaringType.Assembly;
					location = callingAssembly.Location;
					if (string.IsNullOrEmpty(location))
					{
						location = new Uri(callingAssembly.CodeBase).LocalPath;
					}
					FileLog.Log("### Started from " + callingMethod.FullDescription() + ", location " + location);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(7, 1);
					defaultInterpolatedStringHandler2.AppendLiteral("### At ");
					defaultInterpolatedStringHandler2.AppendFormatted<DateTime>(DateTime.Now, "yyyy-MM-dd hh.mm.ss");
					FileLog.Log(defaultInterpolatedStringHandler2.ToStringAndClear());
				}
			}
			this.Id = id;
		}

		/// <summary>Searches the current assembly for Harmony annotations and uses them to create patches</summary>
		/// <remarks>This method can fail to use the correct assembly when being inlined. It calls StackTrace.GetFrame(1) which can point to the wrong method/assembly. If you are unsure or run into problems, use <code>PatchAll(Assembly.GetExecutingAssembly())</code> instead.</remarks>
		// Token: 0x0600023A RID: 570 RVA: 0x0000E450 File Offset: 0x0000C650
		public void PatchAll()
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchAll(assembly);
		}

		/// <summary>Creates a empty patch processor for an original method</summary>
		/// <param name="original">The original method/constructor</param>
		/// <returns>A new <see cref="T:HarmonyLib.PatchProcessor" /> instance</returns>
		// Token: 0x0600023B RID: 571 RVA: 0x0000E481 File Offset: 0x0000C681
		public PatchProcessor CreateProcessor(MethodBase original)
		{
			return new PatchProcessor(this, original);
		}

		/// <summary>Creates a patch class processor from a class</summary>
		/// <param name="type">The class/type</param>
		/// <returns>A new <see cref="T:HarmonyLib.PatchClassProcessor" /> instance</returns>
		// Token: 0x0600023C RID: 572 RVA: 0x0000E48A File Offset: 0x0000C68A
		public PatchClassProcessor CreateClassProcessor(Type type)
		{
			return new PatchClassProcessor(this, type);
		}

		/// <summary>Creates a reverse patcher for one of your stub methods</summary>
		/// <param name="original">The original method/constructor</param>
		/// <param name="standin">The stand-in stub method as <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		/// <returns>A new <see cref="T:HarmonyLib.ReversePatcher" /> instance</returns>
		// Token: 0x0600023D RID: 573 RVA: 0x0000E493 File Offset: 0x0000C693
		public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)
		{
			return new ReversePatcher(this, original, standin);
		}

		/// <summary>Searches an assembly for HarmonyPatch-annotated classes/structs and uses them to create patches</summary>
		/// <param name="assembly">The assembly</param>
		// Token: 0x0600023E RID: 574 RVA: 0x0000E49D File Offset: 0x0000C69D
		public void PatchAll(Assembly assembly)
		{
			AccessTools.GetTypesFromAssembly(assembly).DoIf((Type type) => type.HasHarmonyAttribute(), delegate(Type type)
			{
				this.CreateClassProcessor(type).Patch();
			});
		}

		/// <summary>Searches an assembly for HarmonyPatch-annotated classes/structs without category annotations and uses them to create patches</summary>
		// Token: 0x0600023F RID: 575 RVA: 0x0000E4D8 File Offset: 0x0000C6D8
		public void PatchAllUncategorized()
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchAllUncategorized(assembly);
		}

		/// <summary>Searches an assembly for HarmonyPatch-annotated classes/structs without category annotations and uses them to create patches</summary>
		/// <param name="assembly">The assembly</param>
		// Token: 0x06000240 RID: 576 RVA: 0x0000E50C File Offset: 0x0000C70C
		public void PatchAllUncategorized(Assembly assembly)
		{
			PatchClassProcessor[] patchClasses = (from type in AccessTools.GetTypesFromAssembly(assembly)
				where type.HasHarmonyAttribute()
				select type).Select(new Func<Type, PatchClassProcessor>(this.CreateClassProcessor)).ToArray<PatchClassProcessor>();
			patchClasses.DoIf((PatchClassProcessor patchClass) => string.IsNullOrEmpty(patchClass.Category), delegate(PatchClassProcessor patchClass)
			{
				patchClass.Patch();
			});
		}

		/// <summary>Searches the current assembly for Harmony annotations with a specific category and uses them to create patches</summary>
		/// <param name="category">Name of patch category</param>
		// Token: 0x06000241 RID: 577 RVA: 0x0000E5A0 File Offset: 0x0000C7A0
		public void PatchCategory(string category)
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchCategory(assembly, category);
		}

		/// <summary>Searches an assembly for HarmonyPatch-annotated classes/structs with a specific category and uses them to create patches</summary>
		/// <param name="assembly">The assembly</param>
		/// <param name="category">Name of patch category</param>
		// Token: 0x06000242 RID: 578 RVA: 0x0000E5D4 File Offset: 0x0000C7D4
		public void PatchCategory(Assembly assembly, string category)
		{
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> assemblyCachedCategories = Harmony.AssemblyCachedCategories;
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback createValueCallback;
			if ((createValueCallback = Harmony.<>O.<0>__BuildCategoryCache) == null)
			{
				createValueCallback = (Harmony.<>O.<0>__BuildCategoryCache = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback(Harmony.BuildCategoryCache));
			}
			Dictionary<string, List<Type>> categoryCache = assemblyCachedCategories.GetValue(assembly, createValueCallback);
			List<Type> toPatch;
			if (categoryCache.TryGetValue(category, out toPatch))
			{
				toPatch.Do(delegate(Type type)
				{
					this.CreateClassProcessor(type).Patch();
				});
			}
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000E628 File Offset: 0x0000C828
		private static Dictionary<string, List<Type>> BuildCategoryCache(Assembly assembly)
		{
			Dictionary<string, List<Type>> toBuild = new Dictionary<string, List<Type>>();
			foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
			{
				List<HarmonyMethod> harmonyAttributes = HarmonyMethodExtensions.GetFromType(type);
				if (harmonyAttributes.Count != 0)
				{
					HarmonyMethod containerAttributes = HarmonyMethod.Merge(harmonyAttributes);
					string category = containerAttributes.category;
					if (!string.IsNullOrEmpty(category))
					{
						List<Type> typeList;
						if (!toBuild.TryGetValue(category, out typeList) && typeList == null)
						{
							typeList = new List<Type>();
						}
						typeList.Add(type);
						toBuild[category] = typeList;
					}
				}
			}
			return toBuild;
		}

		/// <summary>Creates patches by manually specifying the methods</summary>
		/// <param name="original">The original method/constructor</param>
		/// <param name="prefix">An optional prefix method wrapped in a <see cref="T:HarmonyLib.HarmonyMethod" /> object</param>
		/// <param name="postfix">An optional postfix method wrapped in a <see cref="T:HarmonyLib.HarmonyMethod" /> object</param>
		/// <param name="transpiler">An optional transpiler method wrapped in a <see cref="T:HarmonyLib.HarmonyMethod" /> object</param>
		/// <param name="finalizer">An optional finalizer method wrapped in a <see cref="T:HarmonyLib.HarmonyMethod" /> object</param>
		/// <returns>The replacement method that was created to patch the original method</returns>
		// Token: 0x06000244 RID: 580 RVA: 0x0000E6AC File Offset: 0x0000C8AC
		public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
		{
			PatchProcessor processor = this.CreateProcessor(original);
			processor.AddPrefix(prefix);
			processor.AddPostfix(postfix);
			processor.AddTranspiler(transpiler);
			processor.AddFinalizer(finalizer);
			return processor.Patch();
		}

		/// <summary>Patches a foreign method onto a stub method of yours and optionally applies transpilers during the process</summary>
		/// <param name="original">The original method/constructor you want to duplicate</param>
		/// <param name="standin">Your stub method as <see cref="T:HarmonyLib.HarmonyMethod" /> that will become the original. Needs to have the correct signature (either original or whatever your transpilers generates)</param>
		/// <param name="transpiler">An optional transpiler as method that will be applied during the process</param>
		/// <returns>The replacement method that was created to patch the stub method</returns>
		// Token: 0x06000245 RID: 581 RVA: 0x0000E6E9 File Offset: 0x0000C8E9
		public static MethodInfo ReversePatch(MethodBase original, HarmonyMethod standin, MethodInfo transpiler = null)
		{
			return PatchFunctions.ReversePatch(standin, original, transpiler);
		}

		/// <summary>Unpatches methods by patching them with zero patches. Fully unpatching is not supported. Be careful, unpatching is global</summary>
		/// <param name="harmonyID">The optional Harmony ID to restrict unpatching to a specific Harmony instance</param>
		/// <remarks>This method could be static if it wasn't for the fact that unpatching creates a new replacement method that contains your harmony ID</remarks>
		// Token: 0x06000246 RID: 582 RVA: 0x0000E6F4 File Offset: 0x0000C8F4
		public void UnpatchAll(string harmonyID = null)
		{
			Harmony.<>c__DisplayClass19_0 CS$<>8__locals1 = new Harmony.<>c__DisplayClass19_0();
			CS$<>8__locals1.harmonyID = harmonyID;
			CS$<>8__locals1.<>4__this = this;
			List<MethodBase> originals = Harmony.GetAllPatchedMethods().ToList<MethodBase>();
			using (List<MethodBase>.Enumerator enumerator = originals.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					MethodBase original = enumerator.Current;
					bool hasBody = original.HasMethodBody();
					Patches info = Harmony.GetPatchInfo(original);
					if (hasBody)
					{
						info.Postfixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						info.Prefixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						info.InnerPostfixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						info.InnerPrefixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
					}
					info.Transpilers.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
					{
						CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
					});
					if (hasBody)
					{
						info.Finalizers.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
					}
				}
			}
		}

		/// <summary>Unpatches a method by patching it with zero patches. Fully unpatching is not supported. Be careful, unpatching is global</summary>
		/// <param name="original">The original method/constructor</param>
		/// <param name="type">The <see cref="T:HarmonyLib.HarmonyPatchType" /></param>
		/// <param name="harmonyID">The optional Harmony ID to restrict unpatching to a specific Harmony instance</param>
		// Token: 0x06000247 RID: 583 RVA: 0x0000E89C File Offset: 0x0000CA9C
		public void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = "*")
		{
			PatchProcessor processor = this.CreateProcessor(original);
			processor.Unpatch(type, harmonyID);
		}

		/// <summary>Unpatches a method by patching it with zero patches. Fully unpatching is not supported. Be careful, unpatching is global</summary>
		/// <param name="original">The original method/constructor</param>
		/// <param name="patch">The patch method as method to remove</param>
		// Token: 0x06000248 RID: 584 RVA: 0x0000E8BC File Offset: 0x0000CABC
		public void Unpatch(MethodBase original, MethodInfo patch)
		{
			PatchProcessor processor = this.CreateProcessor(original);
			processor.Unpatch(patch);
		}

		/// <summary>Searches the current assembly for types with a specific category annotation and uses them to unpatch existing patches. Fully unpatching is not supported. Be careful, unpatching is global</summary>
		/// <param name="category">Name of patch category</param>
		// Token: 0x06000249 RID: 585 RVA: 0x0000E8DC File Offset: 0x0000CADC
		public void UnpatchCategory(string category)
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.UnpatchCategory(assembly, category);
		}

		/// <summary>Searches an assembly for HarmonyPatch-annotated classes/structs with a specific category annotation and uses them to unpatch existing patches. Fully unpatching is not supported. Be careful, unpatching is global</summary>
		/// <param name="assembly">The assembly</param>
		/// <param name="category">Name of patch category</param>
		// Token: 0x0600024A RID: 586 RVA: 0x0000E910 File Offset: 0x0000CB10
		public void UnpatchCategory(Assembly assembly, string category)
		{
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> assemblyCachedCategories = Harmony.AssemblyCachedCategories;
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback createValueCallback;
			if ((createValueCallback = Harmony.<>O.<0>__BuildCategoryCache) == null)
			{
				createValueCallback = (Harmony.<>O.<0>__BuildCategoryCache = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback(Harmony.BuildCategoryCache));
			}
			Dictionary<string, List<Type>> categoryCache = assemblyCachedCategories.GetValue(assembly, createValueCallback);
			List<Type> toPatch;
			if (categoryCache.TryGetValue(category, out toPatch))
			{
				toPatch.Do(delegate(Type type)
				{
					this.CreateClassProcessor(type).Unpatch();
				});
			}
		}

		/// <summary>Test for patches from a specific Harmony ID</summary>
		/// <param name="harmonyID">The Harmony ID</param>
		/// <returns>True if patches for this ID exist</returns>
		// Token: 0x0600024B RID: 587 RVA: 0x0000E964 File Offset: 0x0000CB64
		public static bool HasAnyPatches(string harmonyID)
		{
			IEnumerable<MethodBase> allPatchedMethods = Harmony.GetAllPatchedMethods();
			Func<MethodBase, Patches> selector;
			if ((selector = Harmony.<>O.<1>__GetPatchInfo) == null)
			{
				selector = (Harmony.<>O.<1>__GetPatchInfo = new Func<MethodBase, Patches>(Harmony.GetPatchInfo));
			}
			return allPatchedMethods.Select(selector).Any((Patches info) => info.Owners.Contains(harmonyID));
		}

		/// <summary>Gets patch information for a given original method</summary>
		/// <param name="method">The original method/constructor</param>
		/// <returns>The patch information as <see cref="T:HarmonyLib.Patches" /></returns>
		// Token: 0x0600024C RID: 588 RVA: 0x0000E9B4 File Offset: 0x0000CBB4
		public static Patches GetPatchInfo(MethodBase method)
		{
			return PatchProcessor.GetPatchInfo(method);
		}

		/// <summary>Gets the methods this instance has patched</summary>
		/// <returns>An enumeration of original methods/constructors</returns>
		// Token: 0x0600024D RID: 589 RVA: 0x0000E9BC File Offset: 0x0000CBBC
		public IEnumerable<MethodBase> GetPatchedMethods()
		{
			return from original in Harmony.GetAllPatchedMethods()
				where Harmony.GetPatchInfo(original).Owners.Contains(this.Id)
				select original;
		}

		/// <summary>Gets all patched original methods in the appdomain</summary>
		/// <returns>An enumeration of patched original methods/constructors</returns>
		// Token: 0x0600024E RID: 590 RVA: 0x0000E9D4 File Offset: 0x0000CBD4
		public static IEnumerable<MethodBase> GetAllPatchedMethods()
		{
			return PatchProcessor.GetAllPatchedMethods();
		}

		/// <summary>Gets the original method from a given replacement method</summary>
		/// <param name="replacement">A replacement method (patched original method)</param>
		/// <returns>The original method/constructor or <c>null</c> if not found</returns>
		// Token: 0x0600024F RID: 591 RVA: 0x0000E9DB File Offset: 0x0000CBDB
		public static MethodBase GetOriginalMethod(MethodInfo replacement)
		{
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			return HarmonySharedState.GetRealMethod(replacement, false);
		}

		/// <summary>Tries to get the method from a stackframe including dynamic replacement methods</summary>
		/// <param name="frame">The <see cref="T:System.Diagnostics.StackFrame" /></param>
		/// <returns>For normal frames, <c>frame.GetMethod()</c> is returned. For frames containing patched methods, the replacement method is returned or <c>null</c> if no method can be found</returns>
		// Token: 0x06000250 RID: 592 RVA: 0x0000E9F8 File Offset: 0x0000CBF8
		public static MethodBase GetMethodFromStackframe(StackFrame frame)
		{
			if (frame == null)
			{
				throw new ArgumentNullException("frame");
			}
			return HarmonySharedState.GetStackFrameMethod(frame, true);
		}

		/// <summary>Gets the original method from the stackframe and uses original if method is a dynamic replacement</summary>
		/// <param name="frame">The <see cref="T:System.Diagnostics.StackFrame" /></param>
		/// <returns>The original method from that stackframe</returns>
		// Token: 0x06000251 RID: 593 RVA: 0x0000EA0F File Offset: 0x0000CC0F
		public static MethodBase GetOriginalMethodFromStackframe(StackFrame frame)
		{
			if (frame == null)
			{
				throw new ArgumentNullException("frame");
			}
			return HarmonySharedState.GetStackFrameMethod(frame, false);
		}

		/// <summary>Gets Harmony version for all active Harmony instances</summary>
		/// <param name="currentVersion">[out] The current Harmony version</param>
		/// <returns>A dictionary containing assembly versions keyed by Harmony IDs</returns>
		// Token: 0x06000252 RID: 594 RVA: 0x0000EA26 File Offset: 0x0000CC26
		public static Dictionary<string, Version> VersionInfo(out Version currentVersion)
		{
			return PatchProcessor.VersionInfo(out currentVersion);
		}

		/// <summary>Sets a MonoMod switch value (e.g., "DMDDebug", "DMDDumpTo")</summary>
		/// <param name="name">The switch name</param>
		/// <param name="value">The value to set (bool, string, etc.)</param>
		// Token: 0x06000253 RID: 595 RVA: 0x0000EA2E File Offset: 0x0000CC2E
		public static void SetSwitch(string name, object value)
		{
			Switches.SetSwitchValue(name, value);
		}

		/// <summary>Clears a MonoMod switch value</summary>
		/// <param name="name">The switch name</param>
		// Token: 0x06000254 RID: 596 RVA: 0x0000EA37 File Offset: 0x0000CC37
		public static void ClearSwitch(string name)
		{
			Switches.ClearSwitchValue(name);
		}

		/// <summary>Tries to get a MonoMod switch value</summary>
		/// <param name="name">The switch name</param>
		/// <param name="value">The switch value if found</param>
		/// <returns>True if the switch was found, false otherwise</returns>
		// Token: 0x06000255 RID: 597 RVA: 0x0000EA3F File Offset: 0x0000CC3F
		public static bool TryGetSwitch(string name, out object value)
		{
			return Switches.TryGetSwitchValue(name, out value);
		}

		/// <summary>Tries to determine if a MonoMod switch is enabled</summary>
		/// <param name="name">The switch name</param>
		/// <param name="isEnabled">True if the switch is enabled, false otherwise</param>
		/// <returns>True if the switch enablement state could be determined, false otherwise</returns>
		// Token: 0x06000256 RID: 598 RVA: 0x0000EA48 File Offset: 0x0000CC48
		public static bool TryIsSwitchEnabled(string name, out bool isEnabled)
		{
			return Switches.TryGetSwitchEnabled(name, out isEnabled);
		}

		/// <summary>Set to true before instantiating Harmony to debug Harmony or use an environment variable to set HARMONY_DEBUG to '1' like this: cmd /C "set HARMONY_DEBUG=1 &amp;&amp; game.exe"</summary>
		/// <remarks>This is for full debugging. To debug only specific patches, use the <see cref="T:HarmonyLib.HarmonyDebug" /> attribute</remarks>
		// Token: 0x04000192 RID: 402
		public static bool DEBUG;

		// Token: 0x04000193 RID: 403
		private static readonly ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> AssemblyCachedCategories = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>();

		// Token: 0x0200007D RID: 125
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04000194 RID: 404
			public static ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback <0>__BuildCategoryCache;

			// Token: 0x04000195 RID: 405
			public static Func<MethodBase, Patches> <1>__GetPatchInfo;
		}
	}
}
