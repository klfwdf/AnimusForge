using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000509 RID: 1289
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class PlatformTripleDetourFactory : IDetourFactory
	{
		// Token: 0x06001CF6 RID: 7414 RVA: 0x0005C96A File Offset: 0x0005AB6A
		public PlatformTripleDetourFactory(PlatformTriple triple)
		{
			this.triple = triple;
		}

		// Token: 0x06001CF7 RID: 7415 RVA: 0x0005C97C File Offset: 0x0005AB7C
		public ICoreDetour CreateDetour(CreateDetourRequest request)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(request.Source, "request.Source");
			Helpers.ThrowIfArgumentNull<MethodBase>(request.Target, "request.Target");
			if (!this.triple.TryDisableInlining(request.Source))
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(66, 1, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Could not disable inlining of method ");
					debugLogWarningStringHandler.AppendFormatted<MethodBase>(request.Source);
					debugLogWarningStringHandler.AppendLiteral("; detours may not be reliable");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}
			PlatformTripleDetourFactory.Detour detour = new PlatformTripleDetourFactory.Detour(this.triple, request.Source, request.Target);
			if (request.ApplyByDefault)
			{
				detour.Apply();
			}
			return detour;
		}

		// Token: 0x06001CF8 RID: 7416 RVA: 0x0005CA24 File Offset: 0x0005AC24
		public ICoreNativeDetour CreateNativeDetour(CreateNativeDetourRequest request)
		{
			PlatformTripleDetourFactory.NativeDetour detour = new PlatformTripleDetourFactory.NativeDetour(this.triple, request.Source, request.Target);
			if (request.ApplyByDefault)
			{
				detour.Apply();
			}
			return detour;
		}

		// Token: 0x17000648 RID: 1608
		// (get) Token: 0x06001CF9 RID: 7417 RVA: 0x0005CA5C File Offset: 0x0005AC5C
		public bool SupportsNativeDetourOrigEntrypoint
		{
			get
			{
				return this.triple.SupportedFeatures.Architecture.Has(ArchitectureFeature.CreateAltEntryPoint);
			}
		}

		// Token: 0x040011D4 RID: 4564
		private readonly PlatformTriple triple;

		// Token: 0x0200050A RID: 1290
		[Nullable(0)]
		private abstract class DetourBase : ICoreDetourBase, IDisposable
		{
			// Token: 0x06001CFA RID: 7418 RVA: 0x0005CA82 File Offset: 0x0005AC82
			protected DetourBase(PlatformTriple triple)
			{
				this.Triple = triple;
				this.DetourBox = null;
			}

			// Token: 0x06001CFB RID: 7419 RVA: 0x0005CA98 File Offset: 0x0005AC98
			protected TBox GetDetourBox<[Nullable(0)] TBox>() where TBox : PlatformTripleDetourFactory.DetourBase.DetourBoxBase
			{
				return Unsafe.As<TBox>(this.DetourBox);
			}

			// Token: 0x17000649 RID: 1609
			// (get) Token: 0x06001CFC RID: 7420 RVA: 0x0005CAA5 File Offset: 0x0005ACA5
			public bool IsApplied
			{
				get
				{
					return this.DetourBox.IsApplied;
				}
			}

			// Token: 0x06001CFD RID: 7421 RVA: 0x0005CAB2 File Offset: 0x0005ACB2
			[NullableContext(2)]
			protected static void ReplaceDetourInLock([Nullable(1)] PlatformTripleDetourFactory.DetourBase.DetourBoxBase nativeDetour, SimpleNativeDetour newDetour, out SimpleNativeDetour oldDetour)
			{
				Thread.MemoryBarrier();
				oldDetour = Interlocked.Exchange<SimpleNativeDetour>(ref nativeDetour.Detour, newDetour);
				if (oldDetour != null)
				{
					nativeDetour.OldDetours.Add(oldDetour);
				}
			}

			// Token: 0x06001CFE RID: 7422
			protected abstract SimpleNativeDetour CreateDetour();

			// Token: 0x06001CFF RID: 7423 RVA: 0x0005CAD8 File Offset: 0x0005ACD8
			public void Apply()
			{
				PlatformTripleDetourFactory.DetourBase.DetourBoxBase detourBox = this.DetourBox;
				lock (detourBox)
				{
					if (this.IsApplied)
					{
						throw new InvalidOperationException("Cannot apply a detour which is already applied");
					}
					try
					{
						this.DetourBox.IsApplying = true;
						this.DetourBox.IsApplied = true;
						SimpleNativeDetour oldDetour;
						PlatformTripleDetourFactory.DetourBase.ReplaceDetourInLock(this.DetourBox, this.CreateDetour(), out oldDetour);
					}
					catch
					{
						this.DetourBox.IsApplied = false;
						throw;
					}
					finally
					{
						this.DetourBox.IsApplying = false;
					}
				}
			}

			// Token: 0x06001D00 RID: 7424
			protected abstract void BeforeUndo();

			// Token: 0x06001D01 RID: 7425
			protected abstract void AfterUndo();

			// Token: 0x06001D02 RID: 7426 RVA: 0x0005CB88 File Offset: 0x0005AD88
			public void Undo()
			{
				PlatformTripleDetourFactory.DetourBase.DetourBoxBase detourBox = this.DetourBox;
				lock (detourBox)
				{
					if (!this.IsApplied)
					{
						throw new InvalidOperationException("Cannot undo a detour which is not applied");
					}
					try
					{
						this.DetourBox.IsApplying = true;
						SimpleNativeDetour simpleNativeDetour;
						this.UndoCore(out simpleNativeDetour);
						this.DetourBox.ClearOldDetours();
					}
					finally
					{
						this.DetourBox.IsApplying = false;
					}
				}
			}

			// Token: 0x06001D03 RID: 7427 RVA: 0x0005CC10 File Offset: 0x0005AE10
			[NullableContext(2)]
			private void UndoCore(out SimpleNativeDetour oldDetour)
			{
				this.BeforeUndo();
				this.DetourBox.IsApplied = false;
				PlatformTripleDetourFactory.DetourBase.ReplaceDetourInLock(this.DetourBox, null, out oldDetour);
				this.AfterUndo();
			}

			// Token: 0x06001D04 RID: 7428
			protected abstract void BeforeDispose();

			// Token: 0x06001D05 RID: 7429 RVA: 0x0005CC38 File Offset: 0x0005AE38
			private void Dispose(bool disposing)
			{
				if (!this.disposedValue)
				{
					this.BeforeDispose();
					PlatformTripleDetourFactory.DetourBase.DetourBoxBase detourBox = this.DetourBox;
					lock (detourBox)
					{
						SimpleNativeDetour simpleNativeDetour;
						this.UndoCore(out simpleNativeDetour);
						this.DetourBox.ClearOldDetours();
					}
					this.disposedValue = true;
				}
			}

			// Token: 0x06001D06 RID: 7430 RVA: 0x0005CC9C File Offset: 0x0005AE9C
			~DetourBase()
			{
				this.Dispose(false);
			}

			// Token: 0x06001D07 RID: 7431 RVA: 0x0005CCCC File Offset: 0x0005AECC
			public void Dispose()
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}

			// Token: 0x040011D5 RID: 4565
			protected readonly PlatformTriple Triple;

			// Token: 0x040011D6 RID: 4566
			protected PlatformTripleDetourFactory.DetourBase.DetourBoxBase DetourBox;

			// Token: 0x040011D7 RID: 4567
			private bool disposedValue;

			// Token: 0x0200050B RID: 1291
			[Nullable(0)]
			protected abstract class DetourBoxBase
			{
				// Token: 0x1700064A RID: 1610
				// (get) Token: 0x06001D08 RID: 7432 RVA: 0x0005CCDB File Offset: 0x0005AEDB
				// (set) Token: 0x06001D09 RID: 7433 RVA: 0x0005CCE8 File Offset: 0x0005AEE8
				public bool IsApplied
				{
					get
					{
						return Volatile.Read(ref this.applyDetours);
					}
					set
					{
						Volatile.Write(ref this.applyDetours, value);
						Thread.MemoryBarrier();
					}
				}

				// Token: 0x1700064B RID: 1611
				// (get) Token: 0x06001D0A RID: 7434 RVA: 0x0005CCFB File Offset: 0x0005AEFB
				// (set) Token: 0x06001D0B RID: 7435 RVA: 0x0005CD08 File Offset: 0x0005AF08
				public bool IsApplying
				{
					get
					{
						return Volatile.Read(ref this.isApplying);
					}
					set
					{
						Volatile.Write(ref this.isApplying, value);
						Thread.MemoryBarrier();
					}
				}

				// Token: 0x06001D0C RID: 7436 RVA: 0x0005CD1B File Offset: 0x0005AF1B
				protected DetourBoxBase(PlatformTriple triple)
				{
					this.Triple = triple;
					this.applyDetours = false;
					this.isApplying = false;
				}

				// Token: 0x06001D0D RID: 7437 RVA: 0x0005CD50 File Offset: 0x0005AF50
				public void ClearOldDetours()
				{
					foreach (SimpleNativeDetour simpleNativeDetour in this.OldDetours)
					{
						simpleNativeDetour.Dispose();
					}
					this.OldDetours.Clear();
				}

				// Token: 0x040011D8 RID: 4568
				[Nullable(2)]
				public SimpleNativeDetour Detour;

				// Token: 0x040011D9 RID: 4569
				public readonly List<SimpleNativeDetour> OldDetours = new List<SimpleNativeDetour>();

				// Token: 0x040011DA RID: 4570
				protected readonly PlatformTriple Triple;

				// Token: 0x040011DB RID: 4571
				protected readonly object Sync = new object();

				// Token: 0x040011DC RID: 4572
				private bool applyDetours;

				// Token: 0x040011DD RID: 4573
				private bool isApplying;
			}
		}

		// Token: 0x0200050C RID: 1292
		[Nullable(0)]
		private sealed class Detour : PlatformTripleDetourFactory.DetourBase, ICoreDetour, ICoreDetourBase, IDisposable
		{
			// Token: 0x1700064C RID: 1612
			// (get) Token: 0x06001D0E RID: 7438 RVA: 0x0005CDAC File Offset: 0x0005AFAC
			private new PlatformTripleDetourFactory.Detour.ManagedDetourBox DetourBox
			{
				get
				{
					return base.GetDetourBox<PlatformTripleDetourFactory.Detour.ManagedDetourBox>();
				}
			}

			// Token: 0x06001D0F RID: 7439 RVA: 0x0005CDB4 File Offset: 0x0005AFB4
			public Detour(PlatformTriple triple, MethodBase src, MethodBase dst)
				: base(triple)
			{
				this.Source = triple.GetIdentifiable(src);
				this.Target = dst;
				this.realTarget = triple.GetRealDetourTarget(src, dst);
				this.DetourBox = new PlatformTripleDetourFactory.Detour.ManagedDetourBox(triple, this.Source, this.realTarget);
				if (triple.SupportedFeatures.Has(RuntimeFeature.CompileMethodHook))
				{
					PlatformTripleDetourFactory.Detour.EnsureSubscribed(triple);
					this.DetourBox.SubscribeCompileMethod();
				}
			}

			// Token: 0x06001D10 RID: 7440 RVA: 0x0005CE24 File Offset: 0x0005B024
			private static void EnsureSubscribed(PlatformTriple triple)
			{
				if (Volatile.Read(ref PlatformTripleDetourFactory.Detour.hasSubscribed))
				{
					return;
				}
				object obj = PlatformTripleDetourFactory.Detour.subLock;
				lock (obj)
				{
					if (!Volatile.Read(ref PlatformTripleDetourFactory.Detour.hasSubscribed))
					{
						Volatile.Write(ref PlatformTripleDetourFactory.Detour.hasSubscribed, true);
						IRuntime runtime = triple.Runtime;
						OnMethodCompiledCallback value;
						if ((value = PlatformTripleDetourFactory.Detour.<>O.<0>__OnMethodCompiled) == null)
						{
							value = (PlatformTripleDetourFactory.Detour.<>O.<0>__OnMethodCompiled = new OnMethodCompiledCallback(PlatformTripleDetourFactory.Detour.OnMethodCompiled));
						}
						runtime.OnMethodCompiled += value;
					}
				}
			}

			// Token: 0x06001D11 RID: 7441 RVA: 0x0005CEAC File Offset: 0x0005B0AC
			private static void AddRelatedDetour(MethodBase m, PlatformTripleDetourFactory.Detour.ManagedDetourBox cmh)
			{
				for (;;)
				{
					PlatformTripleDetourFactory.Detour.RelatedDetourBag related = PlatformTripleDetourFactory.Detour.relatedDetours.GetOrAdd(i, (MethodBase m) => new PlatformTripleDetourFactory.Detour.RelatedDetourBag(m));
					PlatformTripleDetourFactory.Detour.RelatedDetourBag obj = related;
					lock (obj)
					{
						if (!related.IsValid)
						{
							continue;
						}
						related.RelatedDetours.Add(cmh);
						if (related.RelatedDetours.Count > 1)
						{
							bool flag2;
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(115, 1, ref flag2);
							if (flag2)
							{
								debugLogWarningStringHandler.AppendLiteral("Multiple related detours for method ");
								debugLogWarningStringHandler.AppendFormatted<MethodBase>(i);
								debugLogWarningStringHandler.AppendLiteral("! This means that the method has been detoured twice. Detour cleanup will fail.");
							}
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
						}
					}
					break;
				}
			}

			// Token: 0x06001D12 RID: 7442 RVA: 0x0005CF68 File Offset: 0x0005B168
			private static void RemoveRelatedDetour(MethodBase m, PlatformTripleDetourFactory.Detour.ManagedDetourBox cmh)
			{
				PlatformTripleDetourFactory.Detour.RelatedDetourBag related;
				if (PlatformTripleDetourFactory.Detour.relatedDetours.TryGetValue(m, out related))
				{
					PlatformTripleDetourFactory.Detour.RelatedDetourBag obj = related;
					lock (obj)
					{
						related.RelatedDetours.Remove(cmh);
						if (related.RelatedDetours.Count == 0)
						{
							related.IsValid = false;
							PlatformTripleDetourFactory.Detour.RelatedDetourBag relatedDetourBag;
							Helpers.Assert(PlatformTripleDetourFactory.Detour.relatedDetours.TryRemove(related.Method, out relatedDetourBag), null, "relatedDetours.TryRemove(related.Method, out _)");
						}
						return;
					}
				}
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(79, 1, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Attempted to remove a related detour from method ");
					debugLogWarningStringHandler.AppendFormatted<MethodBase>(m);
					debugLogWarningStringHandler.AppendLiteral(" which has no RelatedDetourBag");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}

			// Token: 0x06001D13 RID: 7443 RVA: 0x0005D020 File Offset: 0x0005B220
			[NullableContext(2)]
			private static void OnMethodCompiled(RuntimeMethodHandle methodHandle, MethodBase method, IntPtr codeStart, IntPtr codeStartRw, ulong codeSize)
			{
				if (method == null)
				{
					return;
				}
				method = PlatformTriple.Current.GetIdentifiable(method);
				PlatformTripleDetourFactory.Detour.RelatedDetourBag related;
				if (PlatformTripleDetourFactory.Detour.relatedDetours.TryGetValue(method, out related))
				{
					PlatformTripleDetourFactory.Detour.RelatedDetourBag obj = related;
					lock (obj)
					{
						foreach (PlatformTripleDetourFactory.Detour.ManagedDetourBox managedDetourBox in related.RelatedDetours)
						{
							managedDetourBox.OnMethodCompiled(method, codeStart, codeStartRw, codeSize);
						}
					}
				}
			}

			// Token: 0x1700064D RID: 1613
			// (get) Token: 0x06001D14 RID: 7444 RVA: 0x0005D0B8 File Offset: 0x0005B2B8
			public MethodBase Source { get; }

			// Token: 0x1700064E RID: 1614
			// (get) Token: 0x06001D15 RID: 7445 RVA: 0x0005D0C0 File Offset: 0x0005B2C0
			public MethodBase Target { get; }

			// Token: 0x06001D16 RID: 7446 RVA: 0x0005D0C8 File Offset: 0x0005B2C8
			protected override SimpleNativeDetour CreateDetour()
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(33, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Applying managed detour from ");
					debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.Source);
					debugLogTraceStringHandler.AppendLiteral(" to ");
					debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.realTarget);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
				this.srcPin = this.Triple.PinMethodIfNeeded(this.Source);
				this.dstPin = this.Triple.PinMethodIfNeeded(this.realTarget);
				this.Triple.Compile(this.Source);
				IntPtr from = this.Triple.GetNativeMethodBody(this.Source);
				this.Triple.Compile(this.realTarget);
				IntPtr to = this.Triple.Runtime.GetMethodHandle(this.realTarget).GetFunctionPointer();
				return this.Triple.CreateSimpleDetour(from, to, -1, (IntPtr)0);
			}

			// Token: 0x06001D17 RID: 7447 RVA: 0x0005D1B0 File Offset: 0x0005B3B0
			protected override void BeforeUndo()
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(32, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Undoing managed detour from ");
					debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.Source);
					debugLogTraceStringHandler.AppendLiteral(" to ");
					debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.realTarget);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			}

			// Token: 0x06001D18 RID: 7448 RVA: 0x0005D204 File Offset: 0x0005B404
			protected override void AfterUndo()
			{
				IDisposable disposable = Interlocked.Exchange<IDisposable>(ref this.srcPin, null);
				if (disposable != null)
				{
					disposable.Dispose();
				}
				IDisposable disposable2 = Interlocked.Exchange<IDisposable>(ref this.dstPin, null);
				if (disposable2 == null)
				{
					return;
				}
				disposable2.Dispose();
			}

			// Token: 0x06001D19 RID: 7449 RVA: 0x0005D234 File Offset: 0x0005B434
			protected override void BeforeDispose()
			{
				if (this.Triple.SupportedFeatures.Has(RuntimeFeature.CompileMethodHook))
				{
					this.DetourBox.UnsubscribeCompileMethod();
				}
			}

			// Token: 0x040011DE RID: 4574
			private readonly MethodBase realTarget;

			// Token: 0x040011DF RID: 4575
			private static readonly object subLock = new object();

			// Token: 0x040011E0 RID: 4576
			private static bool hasSubscribed;

			// Token: 0x040011E1 RID: 4577
			private static readonly ConcurrentDictionary<MethodBase, PlatformTripleDetourFactory.Detour.RelatedDetourBag> relatedDetours = new ConcurrentDictionary<MethodBase, PlatformTripleDetourFactory.Detour.RelatedDetourBag>();

			// Token: 0x040011E4 RID: 4580
			[Nullable(2)]
			private IDisposable srcPin;

			// Token: 0x040011E5 RID: 4581
			[Nullable(2)]
			private IDisposable dstPin;

			// Token: 0x0200050D RID: 1293
			[Nullable(0)]
			private sealed class ManagedDetourBox : PlatformTripleDetourFactory.DetourBase.DetourBoxBase
			{
				// Token: 0x06001D1B RID: 7451 RVA: 0x0005D278 File Offset: 0x0005B478
				public ManagedDetourBox(PlatformTriple triple, MethodBase src, MethodBase target)
					: base(triple)
				{
					this.src = src;
					this.target = target;
					this.Detour = null;
				}

				// Token: 0x06001D1C RID: 7452 RVA: 0x0005D296 File Offset: 0x0005B496
				public void SubscribeCompileMethod()
				{
					PlatformTripleDetourFactory.Detour.AddRelatedDetour(this.src, this);
				}

				// Token: 0x06001D1D RID: 7453 RVA: 0x0005D2A4 File Offset: 0x0005B4A4
				public void UnsubscribeCompileMethod()
				{
					PlatformTripleDetourFactory.Detour.RemoveRelatedDetour(this.src, this);
				}

				// Token: 0x06001D1E RID: 7454 RVA: 0x0005D2B4 File Offset: 0x0005B4B4
				public void OnMethodCompiled(MethodBase method, IntPtr codeStart, IntPtr codeStartRw, ulong codeSize)
				{
					if (!base.IsApplied)
					{
						return;
					}
					method = this.Triple.GetIdentifiable(method);
					object sync = this.Sync;
					lock (sync)
					{
						if (base.IsApplied)
						{
							if (!base.IsApplying)
							{
								bool flag2;
								<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(43, 4, ref flag2);
								if (flag2)
								{
									debugLogTraceStringHandler.AppendLiteral("Updating detour from ");
									debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.src);
									debugLogTraceStringHandler.AppendLiteral(" to ");
									debugLogTraceStringHandler.AppendFormatted<MethodBase>(this.target);
									debugLogTraceStringHandler.AppendLiteral(" (recompiled ");
									debugLogTraceStringHandler.AppendFormatted<MethodBase>(method);
									debugLogTraceStringHandler.AppendLiteral(" to ");
									debugLogTraceStringHandler.AppendFormatted<IntPtr>(codeStart, "x16");
									debugLogTraceStringHandler.AppendLiteral(")");
								}
								<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
								try
								{
									base.IsApplying = true;
									SimpleNativeDetour detour = this.Detour;
									IntPtr to;
									IntPtr from;
									IntPtr fromRw;
									if (detour != null)
									{
										IntPtr source = detour.Source;
										to = detour.Destination;
										from = codeStart;
										fromRw = codeStartRw;
									}
									else
									{
										from = codeStart;
										fromRw = codeStartRw;
										to = this.Triple.Runtime.GetMethodHandle(this.target).GetFunctionPointer();
									}
									SimpleNativeDetour newDetour = this.Triple.CreateSimpleDetour(from, to, (int)codeSize, fromRw);
									SimpleNativeDetour simpleNativeDetour;
									PlatformTripleDetourFactory.DetourBase.ReplaceDetourInLock(this, newDetour, out simpleNativeDetour);
								}
								finally
								{
									base.IsApplying = false;
								}
							}
						}
					}
				}

				// Token: 0x040011E6 RID: 4582
				private readonly MethodBase src;

				// Token: 0x040011E7 RID: 4583
				private readonly MethodBase target;
			}

			// Token: 0x0200050E RID: 1294
			[Nullable(0)]
			private sealed class RelatedDetourBag
			{
				// Token: 0x06001D1F RID: 7455 RVA: 0x0005D440 File Offset: 0x0005B640
				public RelatedDetourBag(MethodBase method)
				{
					this.Method = method;
				}

				// Token: 0x040011E8 RID: 4584
				public readonly MethodBase Method;

				// Token: 0x040011E9 RID: 4585
				public readonly List<PlatformTripleDetourFactory.Detour.ManagedDetourBox> RelatedDetours = new List<PlatformTripleDetourFactory.Detour.ManagedDetourBox>();

				// Token: 0x040011EA RID: 4586
				public bool IsValid = true;
			}

			// Token: 0x0200050F RID: 1295
			[CompilerGenerated]
			private static class <>O
			{
				// Token: 0x040011EB RID: 4587
				[Nullable(0)]
				public static OnMethodCompiledCallback <0>__OnMethodCompiled;
			}
		}

		// Token: 0x02000511 RID: 1297
		[NullableContext(0)]
		private sealed class NativeDetour : PlatformTripleDetourFactory.DetourBase, ICoreNativeDetour, ICoreDetourBase, IDisposable
		{
			// Token: 0x1700064F RID: 1615
			// (get) Token: 0x06001D23 RID: 7459 RVA: 0x0005D475 File Offset: 0x0005B675
			public IntPtr Source
			{
				get
				{
					return this.DetourBox.From;
				}
			}

			// Token: 0x17000650 RID: 1616
			// (get) Token: 0x06001D24 RID: 7460 RVA: 0x0005D482 File Offset: 0x0005B682
			public IntPtr Target
			{
				get
				{
					return this.DetourBox.To;
				}
			}

			// Token: 0x17000651 RID: 1617
			// (get) Token: 0x06001D25 RID: 7461 RVA: 0x0005D48F File Offset: 0x0005B68F
			public bool HasOrigEntrypoint
			{
				get
				{
					return this.OrigEntrypoint != IntPtr.Zero;
				}
			}

			// Token: 0x17000652 RID: 1618
			// (get) Token: 0x06001D26 RID: 7462 RVA: 0x0005D4A1 File Offset: 0x0005B6A1
			// (set) Token: 0x06001D27 RID: 7463 RVA: 0x0005D4A9 File Offset: 0x0005B6A9
			public IntPtr OrigEntrypoint { get; private set; }

			// Token: 0x17000653 RID: 1619
			// (get) Token: 0x06001D28 RID: 7464 RVA: 0x0005D4B2 File Offset: 0x0005B6B2
			[Nullable(1)]
			private new PlatformTripleDetourFactory.NativeDetour.NativeDetourBox DetourBox
			{
				[NullableContext(1)]
				get
				{
					return base.GetDetourBox<PlatformTripleDetourFactory.NativeDetour.NativeDetourBox>();
				}
			}

			// Token: 0x06001D29 RID: 7465 RVA: 0x0005D4BA File Offset: 0x0005B6BA
			[NullableContext(1)]
			public NativeDetour(PlatformTriple triple, IntPtr from, IntPtr to)
				: base(triple)
			{
				this.DetourBox = new PlatformTripleDetourFactory.NativeDetour.NativeDetourBox(triple, from, to);
			}

			// Token: 0x06001D2A RID: 7466 RVA: 0x0005D4D4 File Offset: 0x0005B6D4
			[NullableContext(1)]
			protected override SimpleNativeDetour CreateDetour()
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(32, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Applying native detour from ");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Source, "x16");
					debugLogTraceStringHandler.AppendLiteral(" to ");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Target, "x16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
				SimpleNativeDetour simpleNativeDetour;
				IntPtr intPtr;
				IDisposable disposable;
				this.Triple.CreateNativeDetour(this.Source, this.Target, -1, (IntPtr)0).Deconstruct(out simpleNativeDetour, out intPtr, out disposable);
				SimpleNativeDetour result = simpleNativeDetour;
				IntPtr altEntry = intPtr;
				IDisposable altHandle = disposable;
				IDisposable disposable2 = this.origHandle;
				disposable = altHandle;
				this.origHandle = disposable;
				this.OrigEntrypoint = altEntry;
				return result;
			}

			// Token: 0x06001D2B RID: 7467 RVA: 0x0005D57C File Offset: 0x0005B77C
			protected override void BeforeUndo()
			{
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(31, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("Undoing native detour from ");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Source, "x16");
					debugLogTraceStringHandler.AppendLiteral(" to ");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Target, "x16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			}

			// Token: 0x06001D2C RID: 7468 RVA: 0x0005D5DA File Offset: 0x0005B7DA
			protected override void AfterUndo()
			{
				this.OrigEntrypoint = IntPtr.Zero;
				IDisposable disposable = this.origHandle;
				if (disposable != null)
				{
					disposable.Dispose();
				}
				this.origHandle = null;
			}

			// Token: 0x06001D2D RID: 7469 RVA: 0x0001B842 File Offset: 0x00019A42
			protected override void BeforeDispose()
			{
			}

			// Token: 0x040011EF RID: 4591
			[Nullable(2)]
			private IDisposable origHandle;

			// Token: 0x02000512 RID: 1298
			private sealed class NativeDetourBox : PlatformTripleDetourFactory.DetourBase.DetourBoxBase
			{
				// Token: 0x06001D2E RID: 7470 RVA: 0x0005D5FF File Offset: 0x0005B7FF
				[NullableContext(1)]
				public NativeDetourBox(PlatformTriple triple, IntPtr from, IntPtr to)
					: base(triple)
				{
					this.From = from;
					this.To = to;
				}

				// Token: 0x040011F0 RID: 4592
				public readonly IntPtr From;

				// Token: 0x040011F1 RID: 4593
				public readonly IntPtr To;
			}
		}
	}
}
