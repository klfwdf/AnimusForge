using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000514 RID: 1300
	internal sealed class SimpleNativeDetour : IDisposable
	{
		// Token: 0x17000654 RID: 1620
		// (get) Token: 0x06001D2F RID: 7471 RVA: 0x0005D616 File Offset: 0x0005B816
		public ReadOnlyMemory<byte> DetourBackup
		{
			get
			{
				return this.backup;
			}
		}

		// Token: 0x17000655 RID: 1621
		// (get) Token: 0x06001D30 RID: 7472 RVA: 0x0005D623 File Offset: 0x0005B823
		public IntPtr Source
		{
			get
			{
				return this.detourInfo.From;
			}
		}

		// Token: 0x17000656 RID: 1622
		// (get) Token: 0x06001D31 RID: 7473 RVA: 0x0005D630 File Offset: 0x0005B830
		public IntPtr Destination
		{
			get
			{
				return this.detourInfo.To;
			}
		}

		// Token: 0x06001D32 RID: 7474 RVA: 0x0005D63D File Offset: 0x0005B83D
		internal SimpleNativeDetour([Nullable(1)] PlatformTriple triple, NativeDetourInfo detourInfo, Memory<byte> backup, [Nullable(2)] IDisposable allocHandle)
		{
			this.triple = triple;
			this.detourInfo = detourInfo;
			this.backup = backup;
			this.AllocHandle = allocHandle;
		}

		// Token: 0x06001D33 RID: 7475 RVA: 0x0005D664 File Offset: 0x0005B864
		public unsafe void ChangeTarget(IntPtr newTarget)
		{
			this.CheckDisposed();
			bool flag;
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(47, 3, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("Retargeting simple detour 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Source, "x16");
				debugLogTraceStringHandler.AppendLiteral(" => 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Destination, "x16");
				debugLogTraceStringHandler.AppendLiteral(" to target 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(newTarget, "x16");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			NativeDetourInfo retarget = this.triple.Architecture.ComputeRetargetInfo(this.detourInfo, newTarget, this.detourInfo.Size);
			int size = retarget.Size;
			Span<byte> retargetBytes = new Span<byte>(stackalloc byte[(UIntPtr)size], size);
			IDisposable alloc;
			bool repatch;
			bool disposeOldAlloc;
			this.triple.Architecture.GetRetargetBytes(this.detourInfo, retarget, retargetBytes, out alloc, out repatch, out disposeOldAlloc);
			if (repatch)
			{
				byte[] newBackup = null;
				if (retarget.Size > this.backup.Length)
				{
					newBackup = new byte[retarget.Size];
				}
				this.triple.System.PatchData(PatchTargetKind.Executable, this.Source, retargetBytes, newBackup);
				if (newBackup != null)
				{
					this.backup.Span.CopyTo(newBackup);
					this.backup = newBackup;
				}
			}
			this.detourInfo = retarget;
			IDisposable allocHandle = this.AllocHandle;
			IDisposable allocHandle2 = alloc;
			alloc = allocHandle;
			this.AllocHandle = allocHandle2;
			if (disposeOldAlloc && alloc != null)
			{
				alloc.Dispose();
			}
		}

		// Token: 0x06001D34 RID: 7476 RVA: 0x0005D7D9 File Offset: 0x0005B9D9
		public void Undo()
		{
			this.CheckDisposed();
			this.UndoCore(true);
		}

		// Token: 0x06001D35 RID: 7477 RVA: 0x0005D7E8 File Offset: 0x0005B9E8
		private void CheckDisposed()
		{
			if (this.disposedValue)
			{
				throw new ObjectDisposedException("SimpleNativeDetour");
			}
		}

		// Token: 0x06001D36 RID: 7478 RVA: 0x0005D800 File Offset: 0x0005BA00
		private void UndoCore(bool disposing)
		{
			bool flag;
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(30, 2, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("Undoing simple detour 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Source, "x16");
				debugLogTraceStringHandler.AppendLiteral(" => 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.Destination, "x16");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			this.triple.System.PatchData(PatchTargetKind.Executable, this.Source, this.DetourBackup.Span, default(Span<byte>));
			if (disposing)
			{
				this.Cleanup();
			}
			this.disposedValue = true;
		}

		// Token: 0x06001D37 RID: 7479 RVA: 0x0005D89C File Offset: 0x0005BA9C
		private void Cleanup()
		{
			IDisposable allocHandle = this.AllocHandle;
			if (allocHandle == null)
			{
				return;
			}
			allocHandle.Dispose();
		}

		// Token: 0x06001D38 RID: 7480 RVA: 0x0005D8AE File Offset: 0x0005BAAE
		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				this.UndoCore(disposing);
				this.disposedValue = true;
			}
		}

		// Token: 0x06001D39 RID: 7481 RVA: 0x0005D8C8 File Offset: 0x0005BAC8
		~SimpleNativeDetour()
		{
			this.Dispose(false);
		}

		// Token: 0x06001D3A RID: 7482 RVA: 0x0005D8F8 File Offset: 0x0005BAF8
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x04001200 RID: 4608
		private bool disposedValue;

		// Token: 0x04001201 RID: 4609
		[Nullable(1)]
		private readonly PlatformTriple triple;

		// Token: 0x04001202 RID: 4610
		private NativeDetourInfo detourInfo;

		// Token: 0x04001203 RID: 4611
		private Memory<byte> backup;

		// Token: 0x04001204 RID: 4612
		[Nullable(2)]
		private IDisposable AllocHandle;
	}
}
