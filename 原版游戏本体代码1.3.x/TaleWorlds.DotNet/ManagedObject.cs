using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000025 RID: 37
	public abstract class ManagedObject
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000D7 RID: 215 RVA: 0x000042FA File Offset: 0x000024FA
		internal ManagedObjectOwner ManagedObjectOwner
		{
			get
			{
				return this._managedObjectOwner;
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00004304 File Offset: 0x00002504
		internal static void FinalizeManagedObjects()
		{
			Dictionary<int, ManagedObject.ManagedObjectKeeper> managedObjectKeepReferences = ManagedObject._managedObjectKeepReferences;
			lock (managedObjectKeepReferences)
			{
				ManagedObject._managedObjectKeepReferences.Clear();
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00004348 File Offset: 0x00002548
		protected void AddUnmanagedMemoryPressure(int size)
		{
			GC.AddMemoryPressure((long)size);
			this.forcedMemory = size;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00004364 File Offset: 0x00002564
		protected ManagedObject(UIntPtr ptr, bool createManagedObjectOwner)
		{
			if (createManagedObjectOwner)
			{
				this.SetOwnerManagedObject(ManagedObjectOwner.CreateManagedObjectOwner(ptr, this));
			}
			this.Initialize();
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00004382 File Offset: 0x00002582
		internal void SetOwnerManagedObject(ManagedObjectOwner owner)
		{
			this._managedObjectOwner = owner;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000438B File Offset: 0x0000258B
		private void Initialize()
		{
			ManagedObject.ManagedObjectFetched(this);
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00004394 File Offset: 0x00002594
		~ManagedObject()
		{
			if (this.forcedMemory > 0)
			{
				GC.RemoveMemoryPressure((long)this.forcedMemory);
			}
			ManagedObjectOwner.ManagedObjectGarbageCollected(this._managedObjectOwner, this);
			this._managedObjectOwner = null;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x000043E4 File Offset: 0x000025E4
		internal static void HandleManagedObjects()
		{
			Dictionary<int, ManagedObject.ManagedObjectKeeper> managedObjectKeepReferences = ManagedObject._managedObjectKeepReferences;
			lock (managedObjectKeepReferences)
			{
				List<int> list = new List<int>();
				foreach (KeyValuePair<int, ManagedObject.ManagedObjectKeeper> keyValuePair in ManagedObject._managedObjectKeepReferences)
				{
					keyValuePair.Value.TimerToReleaseStrongRef--;
					if (keyValuePair.Value.TimerToReleaseStrongRef == 0)
					{
						keyValuePair.Value.gcHandle.Free();
						list.Add(keyValuePair.Key);
					}
				}
				foreach (int key in list)
				{
					ManagedObject._managedObjectKeepReferences.Remove(key);
				}
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x000044E4 File Offset: 0x000026E4
		internal static void ManagedObjectFetched(ManagedObject managedObject)
		{
			Dictionary<int, ManagedObject.ManagedObjectKeeper> managedObjectKeepReferences = ManagedObject._managedObjectKeepReferences;
			lock (managedObjectKeepReferences)
			{
				if (!Managed.Closing)
				{
					ManagedObject._totalCreatedObjectCount++;
					ManagedObject.ManagedObjectKeeper managedObjectKeeper = new ManagedObject.ManagedObjectKeeper();
					managedObjectKeeper.gcHandle = GCHandle.Alloc(managedObject);
					managedObjectKeeper.TimerToReleaseStrongRef = 200;
					ManagedObject._managedObjectKeepReferences.Add(ManagedObject._totalCreatedObjectCount, managedObjectKeeper);
				}
			}
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00004560 File Offset: 0x00002760
		[LibraryCallback(null, false)]
		internal static int GetAliveManagedObjectCount()
		{
			return ManagedObjectOwner.NumberOfAliveManagedObjects;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00004567 File Offset: 0x00002767
		[LibraryCallback(null, false)]
		internal static string GetAliveManagedObjectNames()
		{
			return ManagedObjectOwner.GetAliveManagedObjectNames();
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000456E File Offset: 0x0000276E
		[LibraryCallback(null, false)]
		internal static string GetCreationCallstack(string name)
		{
			return ManagedObjectOwner.GetAliveManagedObjectCreationCallstacks(name);
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000E4 RID: 228 RVA: 0x00004576 File Offset: 0x00002776
		// (set) Token: 0x060000E5 RID: 229 RVA: 0x00004583 File Offset: 0x00002783
		internal UIntPtr Pointer
		{
			get
			{
				return this._managedObjectOwner.Pointer;
			}
			set
			{
				this._managedObjectOwner.Pointer = value;
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00004591 File Offset: 0x00002791
		public int GetManagedId()
		{
			return this._managedObjectOwner.NativeId;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000459E File Offset: 0x0000279E
		[LibraryCallback(null, false)]
		internal string GetClassOfObject()
		{
			return base.GetType().Name;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000045AB File Offset: 0x000027AB
		public override int GetHashCode()
		{
			return this._managedObjectOwner.NativeId;
		}

		// Token: 0x04000053 RID: 83
		private const int ManagedObjectFirstReferencesTickCount = 200;

		// Token: 0x04000054 RID: 84
		private static Dictionary<int, ManagedObject.ManagedObjectKeeper> _managedObjectKeepReferences = new Dictionary<int, ManagedObject.ManagedObjectKeeper>();

		// Token: 0x04000055 RID: 85
		private static int _totalCreatedObjectCount;

		// Token: 0x04000056 RID: 86
		private ManagedObjectOwner _managedObjectOwner;

		// Token: 0x04000057 RID: 87
		private int forcedMemory;

		// Token: 0x02000043 RID: 67
		private class ManagedObjectKeeper
		{
			// Token: 0x040000BA RID: 186
			internal int TimerToReleaseStrongRef;

			// Token: 0x040000BB RID: 187
			internal GCHandle gcHandle;
		}
	}
}
