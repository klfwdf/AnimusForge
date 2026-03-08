using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	/// <summary>A PatchClassProcessor used to turn <see cref="T:HarmonyLib.HarmonyAttribute" /> on a class/type into patches</summary>
	// Token: 0x02000091 RID: 145
	public class PatchClassProcessor
	{
		/// <summary name="Category">Name of the patch class's category</summary>
		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060002B2 RID: 690 RVA: 0x0000F7B0 File Offset: 0x0000D9B0
		// (set) Token: 0x060002B3 RID: 691 RVA: 0x0000F7B8 File Offset: 0x0000D9B8
		public string Category { get; set; }

		/// <summary>Creates a patch class processor by pointing out a class; similar to PatchAll() but without searching through all classes</summary>
		/// <param name="instance">The Harmony instance</param>
		/// <param name="type">The class to process</param>
		/// <note>Use this if you want to patch a class that is not annotated with HarmonyPatch</note>
		// Token: 0x060002B4 RID: 692 RVA: 0x0000F7C4 File Offset: 0x0000D9C4
		public PatchClassProcessor(Harmony instance, Type type)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			this.instance = instance;
			this.containerType = type;
			List<HarmonyMethod> harmonyAttributes = HarmonyMethodExtensions.GetFromType(type);
			this.containerAttributes = HarmonyMethod.Merge(harmonyAttributes);
			HarmonyMethod harmonyMethod = this.containerAttributes;
			MethodType value = harmonyMethod.methodType.GetValueOrDefault();
			if (harmonyMethod.methodType == null)
			{
				value = MethodType.Normal;
				harmonyMethod.methodType = new MethodType?(value);
			}
			this.Category = this.containerAttributes.category;
			this.auxilaryMethods = new Dictionary<Type, MethodInfo>();
			foreach (Type auxType in PatchClassProcessor.auxilaryTypes)
			{
				MethodInfo method = PatchTools.GetPatchMethod(this.containerType, auxType.FullName);
				if (method != null)
				{
					this.auxilaryMethods[auxType] = method;
				}
			}
			this.patchMethods = PatchTools.GetPatchMethods(this.containerType);
			foreach (AttributePatch patchMethod in this.patchMethods)
			{
				MethodInfo method2 = patchMethod.info.method;
				patchMethod.info = this.containerAttributes.Merge(patchMethod.info);
				patchMethod.info.method = method2;
			}
		}

		/// <summary>Applies the patches</summary>
		/// <returns>A list of all created replacement methods or null if patch class is not annotated</returns>
		// Token: 0x060002B5 RID: 693 RVA: 0x0000F948 File Offset: 0x0000DB48
		public List<MethodInfo> Patch()
		{
			Exception exception = null;
			if (!this.RunMethod<HarmonyPrepare, bool>(true, false, null, Array.Empty<object>()))
			{
				this.RunMethod<HarmonyCleanup>(ref exception, Array.Empty<object>());
				this.ReportException(exception, null);
				return new List<MethodInfo>();
			}
			List<MethodInfo> replacements = new List<MethodInfo>();
			MethodBase lastOriginal = null;
			try
			{
				List<MethodBase> originals = this.GetBulkMethods();
				if (originals.Count == 1)
				{
					lastOriginal = originals[0];
				}
				this.ReversePatch(ref lastOriginal);
				replacements = ((originals.Count > 0) ? this.BulkPatch(originals, ref lastOriginal, false) : this.PatchWithAttributes(ref lastOriginal, false));
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			this.RunMethod<HarmonyCleanup>(ref exception, new object[] { exception });
			this.ReportException(exception, lastOriginal);
			return replacements;
		}

		/// <summary>REmoves the patches</summary>
		// Token: 0x060002B6 RID: 694 RVA: 0x0000FA04 File Offset: 0x0000DC04
		public void Unpatch()
		{
			List<MethodBase> originals = this.GetBulkMethods();
			MethodBase lastOriginal = null;
			if (originals.Count > 0)
			{
				this.BulkPatch(originals, ref lastOriginal, true);
				return;
			}
			this.PatchWithAttributes(ref lastOriginal, true);
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x0000FA3C File Offset: 0x0000DC3C
		private void ReversePatch(ref MethodBase lastOriginal)
		{
			for (int i = 0; i < this.patchMethods.Count; i++)
			{
				AttributePatch patchMethod = this.patchMethods[i];
				if (patchMethod.type.GetValueOrDefault() == HarmonyPatchType.ReversePatch)
				{
					MethodBase annotatedOriginal = patchMethod.info.GetOriginalMethod();
					if (annotatedOriginal != null)
					{
						lastOriginal = annotatedOriginal;
					}
					ReversePatcher reversePatcher = this.instance.CreateReversePatcher(lastOriginal, patchMethod.info);
					object locker = PatchProcessor.locker;
					lock (locker)
					{
						reversePatcher.Patch(HarmonyReversePatchType.Original);
					}
				}
			}
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x0000FADC File Offset: 0x0000DCDC
		private List<MethodInfo> BulkPatch(List<MethodBase> originals, ref MethodBase lastOriginal, bool unpatch)
		{
			PatchJobs<MethodInfo> jobs = new PatchJobs<MethodInfo>();
			for (int i = 0; i < originals.Count; i++)
			{
				lastOriginal = originals[i];
				PatchJobs<MethodInfo>.Job job = jobs.GetJob(lastOriginal);
				foreach (AttributePatch patchMethod in this.patchMethods)
				{
					string note = "You cannot combine TargetMethod, TargetMethods or [HarmonyPatchAll] with individual annotations";
					HarmonyMethod info = patchMethod.info;
					if (info.methodName != null)
					{
						throw new ArgumentException(note + " [" + info.methodName + "]");
					}
					if (info.methodType != null && info.methodType.Value != MethodType.Normal)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
						defaultInterpolatedStringHandler.AppendFormatted(note);
						defaultInterpolatedStringHandler.AppendLiteral(" [");
						defaultInterpolatedStringHandler.AppendFormatted<MethodType?>(info.methodType);
						defaultInterpolatedStringHandler.AppendLiteral("]");
						throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (info.argumentTypes != null)
					{
						throw new ArgumentException(note + " [" + info.argumentTypes.Description() + "]");
					}
					job.AddPatch(patchMethod);
				}
			}
			foreach (PatchJobs<MethodInfo>.Job job2 in jobs.GetJobs())
			{
				lastOriginal = job2.original;
				if (unpatch)
				{
					this.ProcessUnpatchJob(job2);
				}
				else
				{
					this.ProcessPatchJob(job2);
				}
			}
			return jobs.GetReplacements();
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0000FC8C File Offset: 0x0000DE8C
		private List<MethodInfo> PatchWithAttributes(ref MethodBase lastOriginal, bool unpatch)
		{
			PatchJobs<MethodInfo> jobs = new PatchJobs<MethodInfo>();
			foreach (AttributePatch patchMethod in this.patchMethods)
			{
				lastOriginal = patchMethod.info.GetOriginalMethod();
				if (lastOriginal == null)
				{
					throw new ArgumentException("Undefined target method for patch method " + patchMethod.info.method.FullDescription());
				}
				PatchJobs<MethodInfo>.Job job = jobs.GetJob(lastOriginal);
				job.AddPatch(patchMethod);
			}
			foreach (PatchJobs<MethodInfo>.Job job2 in jobs.GetJobs())
			{
				lastOriginal = job2.original;
				if (unpatch)
				{
					this.ProcessUnpatchJob(job2);
				}
				else
				{
					this.ProcessPatchJob(job2);
				}
			}
			return jobs.GetReplacements();
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0000FD80 File Offset: 0x0000DF80
		private void ProcessPatchJob(PatchJobs<MethodInfo>.Job job)
		{
			MethodInfo replacement = null;
			bool individualPrepareResult = this.RunMethod<HarmonyPrepare, bool>(true, false, null, new object[] { job.original });
			Exception exception = null;
			if (individualPrepareResult)
			{
				object locker = PatchProcessor.locker;
				lock (locker)
				{
					try
					{
						PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(job.original) ?? new PatchInfo();
						patchInfo.AddPrefixes(this.instance.Id, job.prefixes.ToArray());
						patchInfo.AddPostfixes(this.instance.Id, job.postfixes.ToArray());
						patchInfo.AddTranspilers(this.instance.Id, job.transpilers.ToArray());
						patchInfo.AddFinalizers(this.instance.Id, job.finalizers.ToArray());
						patchInfo.AddInnerPrefixes(this.instance.Id, job.innerprefixes.ToArray());
						patchInfo.AddInnerPostfixes(this.instance.Id, job.innerpostfixes.ToArray());
						replacement = PatchFunctions.UpdateWrapper(job.original, patchInfo);
						HarmonySharedState.UpdatePatchInfo(job.original, replacement, patchInfo);
					}
					catch (Exception ex)
					{
						exception = ex;
					}
				}
			}
			this.RunMethod<HarmonyCleanup>(ref exception, new object[] { job.original, exception });
			this.ReportException(exception, job.original);
			job.replacement = replacement;
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000FF00 File Offset: 0x0000E100
		private void ProcessUnpatchJob(PatchJobs<MethodInfo>.Job job)
		{
			PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(job.original) ?? new PatchInfo();
			bool hasBody = job.original.HasMethodBody();
			if (hasBody)
			{
				job.postfixes.Do(delegate(HarmonyMethod patch)
				{
					patchInfo.RemovePatch(patch.method);
				});
				job.prefixes.Do(delegate(HarmonyMethod patch)
				{
					patchInfo.RemovePatch(patch.method);
				});
			}
			job.transpilers.Do(delegate(HarmonyMethod patch)
			{
				patchInfo.RemovePatch(patch.method);
			});
			if (hasBody)
			{
				job.finalizers.Do(delegate(HarmonyMethod patch)
				{
					patchInfo.RemovePatch(patch.method);
				});
			}
			MethodInfo replacement = PatchFunctions.UpdateWrapper(job.original, patchInfo);
			HarmonySharedState.UpdatePatchInfo(job.original, replacement, patchInfo);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0000FFC0 File Offset: 0x0000E1C0
		private List<MethodBase> GetBulkMethods()
		{
			bool isPatchAll = this.containerType.GetCustomAttributes(true).Any((object a) => a.GetType().FullName == PatchTools.harmonyPatchAllFullName);
			if (isPatchAll)
			{
				Type type = this.containerAttributes.declaringType;
				if (type == null)
				{
					throw new ArgumentException("Using " + PatchTools.harmonyPatchAllFullName + " requires an additional attribute for specifying the Class/Type");
				}
				List<MethodBase> list = new List<MethodBase>();
				list.AddRange(AccessTools.GetDeclaredConstructors(type, null).Cast<MethodBase>());
				list.AddRange(AccessTools.GetDeclaredMethods(type).Cast<MethodBase>());
				List<PropertyInfo> props = AccessTools.GetDeclaredProperties(type);
				list.AddRange((from prop in props
					select prop.GetGetMethod(true) into method
					where method != null
					select method).Cast<MethodBase>());
				list.AddRange((from prop in props
					select prop.GetSetMethod(true) into method
					where method != null
					select method).Cast<MethodBase>());
				return list;
			}
			else
			{
				List<MethodBase> result = new List<MethodBase>();
				IEnumerable<MethodBase> targetMethods = this.RunMethod<HarmonyTargetMethods, IEnumerable<MethodBase>>(null, null, null, Array.Empty<object>());
				if (targetMethods == null)
				{
					MethodBase targetMethod = this.RunMethod<HarmonyTargetMethod, MethodBase>(null, null, delegate(MethodBase method)
					{
						if (method != null)
						{
							return null;
						}
						return "null";
					}, Array.Empty<object>());
					if (targetMethod != null)
					{
						result.Add(targetMethod);
					}
					return result;
				}
				string error = null;
				result = targetMethods.ToList<MethodBase>();
				if (result == null)
				{
					error = "null";
				}
				else if (result.Any((MethodBase m) => m == null))
				{
					error = "some element was null";
				}
				if (error == null)
				{
					return result;
				}
				MethodInfo method2;
				if (this.auxilaryMethods.TryGetValue(typeof(HarmonyTargetMethods), out method2))
				{
					throw new Exception("Method " + method2.FullDescription() + " returned an unexpected result: " + error);
				}
				throw new Exception("Some method returned an unexpected result: " + error);
			}
		}

		// Token: 0x060002BD RID: 701 RVA: 0x00010204 File Offset: 0x0000E404
		private void ReportException(Exception exception, MethodBase original)
		{
			if (exception == null)
			{
				return;
			}
			if (this.containerAttributes.debug.GetValueOrDefault() || Harmony.DEBUG)
			{
				Version currentVersion;
				Harmony.VersionInfo(out currentVersion);
				FileLog.indentLevel = 0;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 2);
				defaultInterpolatedStringHandler.AppendLiteral("### Exception from user \"");
				defaultInterpolatedStringHandler.AppendFormatted(this.instance.Id);
				defaultInterpolatedStringHandler.AppendLiteral("\", Harmony v");
				defaultInterpolatedStringHandler.AppendFormatted<Version>(currentVersion);
				FileLog.Log(defaultInterpolatedStringHandler.ToStringAndClear());
				FileLog.Log("### Original: " + (((original != null) ? original.FullDescription() : null) ?? "NULL"));
				FileLog.Log("### Patch class: " + this.containerType.FullDescription());
				Exception logException = exception;
				HarmonyException hEx = logException as HarmonyException;
				if (hEx != null)
				{
					logException = hEx.InnerException;
				}
				string exStr = logException.ToString();
				while (exStr.Contains("\n\n"))
				{
					exStr = exStr.Replace("\n\n", "\n");
				}
				exStr = exStr.Split(new char[] { '\n' }).Join((string line) => "### " + line, "\n");
				FileLog.Log(exStr.Trim());
			}
			if (exception is HarmonyException)
			{
				throw exception;
			}
			throw new HarmonyException("Patching exception in method " + original.FullDescription(), exception);
		}

		// Token: 0x060002BE RID: 702 RVA: 0x00010368 File Offset: 0x0000E568
		private T RunMethod<S, T>(T defaultIfNotExisting, T defaultIfFailing, Func<T, string> failOnResult = null, params object[] parameters)
		{
			MethodInfo method;
			if (!this.auxilaryMethods.TryGetValue(typeof(S), out method))
			{
				return defaultIfNotExisting;
			}
			object[] input = (parameters ?? Array.Empty<object>()).Union(new object[] { this.instance }).ToArray<object>();
			object[] actualParameters = AccessTools.ActualParameters(method, input);
			if (method.ReturnType != typeof(void) && !typeof(T).IsAssignableFrom(method.ReturnType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Method ");
				defaultInterpolatedStringHandler.AppendFormatted(method.FullDescription());
				defaultInterpolatedStringHandler.AppendLiteral(" has wrong return type (should be assignable to ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(T).FullName);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			T result = defaultIfFailing;
			try
			{
				if (method.ReturnType == typeof(void))
				{
					method.Invoke(null, actualParameters);
					result = defaultIfNotExisting;
				}
				else
				{
					result = (T)((object)method.Invoke(null, actualParameters));
				}
				if (failOnResult != null)
				{
					string error = failOnResult(result);
					if (error != null)
					{
						throw new Exception("Method " + method.FullDescription() + " returned an unexpected result: " + error);
					}
				}
			}
			catch (Exception ex)
			{
				this.ReportException(ex, method);
			}
			return result;
		}

		// Token: 0x060002BF RID: 703 RVA: 0x000104D0 File Offset: 0x0000E6D0
		private void RunMethod<S>(ref Exception exception, params object[] parameters)
		{
			MethodInfo method;
			if (this.auxilaryMethods.TryGetValue(typeof(S), out method))
			{
				object[] input = (parameters ?? Array.Empty<object>()).Union(new object[] { this.instance }).ToArray<object>();
				object[] actualParameters = AccessTools.ActualParameters(method, input);
				try
				{
					object result = method.Invoke(null, actualParameters);
					if (method.ReturnType == typeof(Exception))
					{
						exception = result as Exception;
					}
				}
				catch (Exception ex)
				{
					this.ReportException(ex, method);
				}
			}
		}

		// Token: 0x040001D2 RID: 466
		private readonly Harmony instance;

		// Token: 0x040001D3 RID: 467
		private readonly Type containerType;

		// Token: 0x040001D4 RID: 468
		private readonly HarmonyMethod containerAttributes;

		// Token: 0x040001D5 RID: 469
		private readonly Dictionary<Type, MethodInfo> auxilaryMethods;

		// Token: 0x040001D6 RID: 470
		private readonly List<AttributePatch> patchMethods;

		// Token: 0x040001D7 RID: 471
		private static readonly List<Type> auxilaryTypes = new List<Type>(4)
		{
			typeof(HarmonyPrepare),
			typeof(HarmonyCleanup),
			typeof(HarmonyTargetMethod),
			typeof(HarmonyTargetMethods)
		};
	}
}
