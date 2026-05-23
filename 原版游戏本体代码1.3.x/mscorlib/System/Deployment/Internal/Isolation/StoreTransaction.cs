using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B2 RID: 1714
	internal class StoreTransaction : IDisposable
	{
		// Token: 0x0600500D RID: 20493 RVA: 0x0011D222 File Offset: 0x0011B422
		public void Add(StoreOperationInstallDeployment o)
		{
			this._list.Add(o);
		}

		// Token: 0x0600500E RID: 20494 RVA: 0x0011D236 File Offset: 0x0011B436
		public void Add(StoreOperationPinDeployment o)
		{
			this._list.Add(o);
		}

		// Token: 0x0600500F RID: 20495 RVA: 0x0011D24A File Offset: 0x0011B44A
		public void Add(StoreOperationSetCanonicalizationContext o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005010 RID: 20496 RVA: 0x0011D25E File Offset: 0x0011B45E
		public void Add(StoreOperationSetDeploymentMetadata o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005011 RID: 20497 RVA: 0x0011D272 File Offset: 0x0011B472
		public void Add(StoreOperationStageComponent o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005012 RID: 20498 RVA: 0x0011D286 File Offset: 0x0011B486
		public void Add(StoreOperationStageComponentFile o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005013 RID: 20499 RVA: 0x0011D29A File Offset: 0x0011B49A
		public void Add(StoreOperationUninstallDeployment o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005014 RID: 20500 RVA: 0x0011D2AE File Offset: 0x0011B4AE
		public void Add(StoreOperationUnpinDeployment o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005015 RID: 20501 RVA: 0x0011D2C2 File Offset: 0x0011B4C2
		public void Add(StoreOperationScavenge o)
		{
			this._list.Add(o);
		}

		// Token: 0x06005016 RID: 20502 RVA: 0x0011D2D8 File Offset: 0x0011B4D8
		~StoreTransaction()
		{
			this.Dispose(false);
		}

		// Token: 0x06005017 RID: 20503 RVA: 0x0011D308 File Offset: 0x0011B508
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06005018 RID: 20504 RVA: 0x0011D314 File Offset: 0x0011B514
		[SecuritySafeCritical]
		private void Dispose(bool fDisposing)
		{
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
			StoreTransactionOperation[] storeOps = this._storeOps;
			this._storeOps = null;
			if (storeOps != null)
			{
				for (int num = 0; num != storeOps.Length; num++)
				{
					StoreTransactionOperation storeTransactionOperation = storeOps[num];
					if (storeTransactionOperation.Data.DataPtr != IntPtr.Zero)
					{
						switch (storeTransactionOperation.Operation)
						{
						case StoreTransactionOperationType.SetCanonicalizationContext:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationSetCanonicalizationContext));
							break;
						case StoreTransactionOperationType.StageComponent:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationStageComponent));
							break;
						case StoreTransactionOperationType.PinDeployment:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationPinDeployment));
							break;
						case StoreTransactionOperationType.UnpinDeployment:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationUnpinDeployment));
							break;
						case StoreTransactionOperationType.StageComponentFile:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationStageComponentFile));
							break;
						case StoreTransactionOperationType.InstallDeployment:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationInstallDeployment));
							break;
						case StoreTransactionOperationType.UninstallDeployment:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationUninstallDeployment));
							break;
						case StoreTransactionOperationType.SetDeploymentMetadata:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationSetDeploymentMetadata));
							break;
						case StoreTransactionOperationType.Scavenge:
							Marshal.DestroyStructure(storeTransactionOperation.Data.DataPtr, typeof(StoreOperationScavenge));
							break;
						}
						Marshal.FreeCoTaskMem(storeTransactionOperation.Data.DataPtr);
					}
				}
			}
		}

		// Token: 0x17000CAC RID: 3244
		// (get) Token: 0x06005019 RID: 20505 RVA: 0x0011D4D7 File Offset: 0x0011B6D7
		public StoreTransactionOperation[] Operations
		{
			get
			{
				if (this._storeOps == null)
				{
					this._storeOps = this.GenerateStoreOpsList();
				}
				return this._storeOps;
			}
		}

		// Token: 0x0600501A RID: 20506 RVA: 0x0011D4F4 File Offset: 0x0011B6F4
		[SecuritySafeCritical]
		private StoreTransactionOperation[] GenerateStoreOpsList()
		{
			StoreTransactionOperation[] array = new StoreTransactionOperation[this._list.Count];
			for (int num = 0; num != this._list.Count; num++)
			{
				object obj = this._list[num];
				Type type = obj.GetType();
				array[num].Data.DataPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(obj));
				Marshal.StructureToPtr(obj, array[num].Data.DataPtr, false);
				if (type == typeof(StoreOperationSetCanonicalizationContext))
				{
					array[num].Operation = StoreTransactionOperationType.SetCanonicalizationContext;
				}
				else if (type == typeof(StoreOperationStageComponent))
				{
					array[num].Operation = StoreTransactionOperationType.StageComponent;
				}
				else if (type == typeof(StoreOperationPinDeployment))
				{
					array[num].Operation = StoreTransactionOperationType.PinDeployment;
				}
				else if (type == typeof(StoreOperationUnpinDeployment))
				{
					array[num].Operation = StoreTransactionOperationType.UnpinDeployment;
				}
				else if (type == typeof(StoreOperationStageComponentFile))
				{
					array[num].Operation = StoreTransactionOperationType.StageComponentFile;
				}
				else if (type == typeof(StoreOperationInstallDeployment))
				{
					array[num].Operation = StoreTransactionOperationType.InstallDeployment;
				}
				else if (type == typeof(StoreOperationUninstallDeployment))
				{
					array[num].Operation = StoreTransactionOperationType.UninstallDeployment;
				}
				else if (type == typeof(StoreOperationSetDeploymentMetadata))
				{
					array[num].Operation = StoreTransactionOperationType.SetDeploymentMetadata;
				}
				else
				{
					if (!(type == typeof(StoreOperationScavenge)))
					{
						throw new Exception("How did you get here?");
					}
					array[num].Operation = StoreTransactionOperationType.Scavenge;
				}
			}
			return array;
		}

		// Token: 0x04002272 RID: 8818
		private ArrayList _list = new ArrayList();

		// Token: 0x04002273 RID: 8819
		private StoreTransactionOperation[] _storeOps;
	}
}
