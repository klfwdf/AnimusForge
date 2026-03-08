using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000026 RID: 38
	internal class ManagedObjectOwner
	{
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x000045B8 File Offset: 0x000027B8
		internal static int NumberOfAliveManagedObjects
		{
			get
			{
				return ManagedObjectOwner._numberOfAliveManagedObjects;
			}
		}

		// Token: 0x060000EA RID: 234 RVA: 0x000045C0 File Offset: 0x000027C0
		static ManagedObjectOwner()
		{
			ManagedObjectOwner._managedObjectOwners = new Dictionary<int, WeakReference>();
			ManagedObjectOwner._lastframedeletedManagedObjects = new List<ManagedObjectOwner>();
			ManagedObjectOwner._managedObjectOwnerReferences = new HashSet<ManagedObjectOwner>();
			ManagedObjectOwner._lastframedeletedManagedObjectBuffer = new List<ManagedObjectOwner>(1024);
			ManagedObjectOwner._pool = new List<ManagedObjectOwner>(8192);
			ManagedObjectOwner._managedObjectOwnerWeakReferences = new List<WeakReference>(8192);
			for (int i = 0; i < 8192; i++)
			{
				ManagedObjectOwner item = new ManagedObjectOwner();
				ManagedObjectOwner._pool.Add(item);
				WeakReference item2 = new WeakReference(null);
				ManagedObjectOwner._managedObjectOwnerWeakReferences.Add(item2);
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00004658 File Offset: 0x00002858
		internal static void GarbageCollect()
		{
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				ManagedObjectOwner._lastframedeletedManagedObjectBuffer.AddRange(ManagedObjectOwner._lastframedeletedManagedObjects);
				ManagedObjectOwner._lastframedeletedManagedObjects.Clear();
				foreach (ManagedObjectOwner managedObjectOwner in ManagedObjectOwner._lastframedeletedManagedObjectBuffer)
				{
					if (managedObjectOwner._ptr != UIntPtr.Zero)
					{
						LibraryApplicationInterface.IManaged.ReleaseManagedObject(managedObjectOwner._ptr);
						managedObjectOwner._ptr = UIntPtr.Zero;
					}
					ManagedObjectOwner._numberOfAliveManagedObjects--;
					WeakReference weakReference = ManagedObjectOwner._managedObjectOwners[managedObjectOwner.NativeId];
					ManagedObjectOwner._managedObjectOwners.Remove(managedObjectOwner.NativeId);
					weakReference.Target = null;
					ManagedObjectOwner._managedObjectOwnerWeakReferences.Add(weakReference);
				}
			}
			foreach (ManagedObjectOwner managedObjectOwner2 in ManagedObjectOwner._lastframedeletedManagedObjectBuffer)
			{
				managedObjectOwner2.Destruct();
				ManagedObjectOwner._pool.Add(managedObjectOwner2);
			}
			ManagedObjectOwner._lastframedeletedManagedObjectBuffer.Clear();
		}

		// Token: 0x060000EC RID: 236 RVA: 0x000047B0 File Offset: 0x000029B0
		internal static void LogFinalize()
		{
			Debug.Print("Checking if any managed object still lives...", 0, Debug.DebugColor.White, 17592186044416UL);
			int num = 0;
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				foreach (KeyValuePair<int, WeakReference> keyValuePair in ManagedObjectOwner._managedObjectOwners)
				{
					if (keyValuePair.Value.Target != null)
					{
						Debug.Print("An object with type of " + keyValuePair.Value.Target.GetType().Name + " still lives", 0, Debug.DebugColor.White, 17592186044416UL);
						num++;
					}
				}
			}
			if (num == 0)
			{
				Debug.Print("There are no living managed objects.", 0, Debug.DebugColor.White, 17592186044416UL);
				return;
			}
			Debug.Print("There are " + num + " living managed objects.", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x000048C4 File Offset: 0x00002AC4
		internal static void PreFinalizeManagedObjects()
		{
			ManagedObjectOwner.GarbageCollect();
		}

		// Token: 0x060000EE RID: 238 RVA: 0x000048CC File Offset: 0x00002ACC
		internal static ManagedObject GetManagedObjectWithId(int id)
		{
			if (id == 0)
			{
				return null;
			}
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				ManagedObjectOwner managedObjectOwner = ManagedObjectOwner._managedObjectOwners[id].Target as ManagedObjectOwner;
				if (managedObjectOwner != null)
				{
					ManagedObject managedObject = managedObjectOwner.TryGetManagedObject();
					ManagedObject.ManagedObjectFetched(managedObject);
					return managedObject;
				}
			}
			return null;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00004938 File Offset: 0x00002B38
		internal static void ManagedObjectGarbageCollected(ManagedObjectOwner owner, ManagedObject managedObject)
		{
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				if (owner != null && owner._managedObjectLongReference.Target as ManagedObject == managedObject)
				{
					ManagedObjectOwner._lastframedeletedManagedObjects.Add(owner);
					ManagedObjectOwner._managedObjectOwnerReferences.Remove(owner);
				}
			}
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x000049A0 File Offset: 0x00002BA0
		internal static ManagedObjectOwner CreateManagedObjectOwner(UIntPtr ptr, ManagedObject managedObject)
		{
			ManagedObjectOwner managedObjectOwner = null;
			if (ManagedObjectOwner._pool.Count > 0)
			{
				managedObjectOwner = ManagedObjectOwner._pool[ManagedObjectOwner._pool.Count - 1];
				ManagedObjectOwner._pool.RemoveAt(ManagedObjectOwner._pool.Count - 1);
			}
			else
			{
				managedObjectOwner = new ManagedObjectOwner();
			}
			managedObjectOwner.Construct(ptr, managedObject);
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				ManagedObjectOwner._numberOfAliveManagedObjects++;
				ManagedObjectOwner._managedObjectOwnerReferences.Add(managedObjectOwner);
			}
			return managedObjectOwner;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00004A40 File Offset: 0x00002C40
		internal static string GetAliveManagedObjectNames()
		{
			string text = "";
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				foreach (WeakReference weakReference in ManagedObjectOwner._managedObjectOwners.Values)
				{
					ManagedObjectOwner managedObjectOwner = weakReference.Target as ManagedObjectOwner;
					if (!dictionary.ContainsKey(managedObjectOwner._typeInfo.Name))
					{
						dictionary.Add(managedObjectOwner._typeInfo.Name, 1);
					}
					else
					{
						dictionary[managedObjectOwner._typeInfo.Name] = dictionary[managedObjectOwner._typeInfo.Name] + 1;
					}
				}
				foreach (string text2 in dictionary.Keys)
				{
					text = string.Concat(new object[]
					{
						text,
						text2,
						",",
						dictionary[text2],
						"-"
					});
				}
			}
			return text;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00004BB4 File Offset: 0x00002DB4
		internal static string GetAliveManagedObjectCreationCallstacks(string name)
		{
			return "";
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00004BBB File Offset: 0x00002DBB
		internal int NativeId
		{
			get
			{
				return this._nativeId;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000F4 RID: 244 RVA: 0x00004BC3 File Offset: 0x00002DC3
		// (set) Token: 0x060000F5 RID: 245 RVA: 0x00004BCB File Offset: 0x00002DCB
		internal UIntPtr Pointer
		{
			get
			{
				return this._ptr;
			}
			set
			{
				if (value != UIntPtr.Zero)
				{
					LibraryApplicationInterface.IManaged.IncreaseReferenceCount(value);
				}
				this._ptr = value;
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00004BEC File Offset: 0x00002DEC
		private ManagedObjectOwner()
		{
			this._ptr = UIntPtr.Zero;
			this._managedObject = new WeakReference(null, false);
			this._managedObjectLongReference = new WeakReference(null, true);
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00004C1C File Offset: 0x00002E1C
		private void Construct(UIntPtr ptr, ManagedObject managedObject)
		{
			this._typeInfo = managedObject.GetType();
			this._managedObject.Target = managedObject;
			this._managedObjectLongReference.Target = managedObject;
			this.Pointer = ptr;
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				this._nativeId = ManagedObjectOwner._lastId;
				ManagedObjectOwner._lastId++;
				WeakReference weakReference;
				if (ManagedObjectOwner._managedObjectOwnerWeakReferences.Count > 0)
				{
					weakReference = ManagedObjectOwner._managedObjectOwnerWeakReferences[ManagedObjectOwner._managedObjectOwnerWeakReferences.Count - 1];
					ManagedObjectOwner._managedObjectOwnerWeakReferences.RemoveAt(ManagedObjectOwner._managedObjectOwnerWeakReferences.Count - 1);
					weakReference.Target = this;
				}
				else
				{
					weakReference = new WeakReference(this);
				}
				ManagedObjectOwner._managedObjectOwners.Add(this.NativeId, weakReference);
			}
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00004CF4 File Offset: 0x00002EF4
		private void Destruct()
		{
			this._managedObject.Target = null;
			this._managedObjectLongReference.Target = null;
			this._typeInfo = null;
			this._ptr = UIntPtr.Zero;
			this._nativeId = 0;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00004D28 File Offset: 0x00002F28
		protected override void Finalize()
		{
			try
			{
				HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
				lock (managedObjectOwnerReferences)
				{
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00004D74 File Offset: 0x00002F74
		private ManagedObject TryGetManagedObject()
		{
			ManagedObject managedObject = null;
			HashSet<ManagedObjectOwner> managedObjectOwnerReferences = ManagedObjectOwner._managedObjectOwnerReferences;
			lock (managedObjectOwnerReferences)
			{
				managedObject = this._managedObject.Target as ManagedObject;
				if (managedObject == null)
				{
					managedObject = (ManagedObject)this._typeInfo.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
					{
						typeof(UIntPtr),
						typeof(bool)
					}, null).Invoke(new object[] { this._ptr, false });
					managedObject.SetOwnerManagedObject(this);
					this._managedObject.Target = managedObject;
					this._managedObjectLongReference.Target = managedObject;
					if (!ManagedObjectOwner._managedObjectOwnerReferences.Contains(this))
					{
						ManagedObjectOwner._managedObjectOwnerReferences.Add(this);
					}
					ManagedObjectOwner._lastframedeletedManagedObjects.Remove(this);
				}
			}
			return managedObject;
		}

		// Token: 0x04000058 RID: 88
		private const int PooledManagedObjectOwnerCount = 8192;

		// Token: 0x04000059 RID: 89
		private static readonly List<ManagedObjectOwner> _pool;

		// Token: 0x0400005A RID: 90
		private static readonly List<WeakReference> _managedObjectOwnerWeakReferences;

		// Token: 0x0400005B RID: 91
		private static readonly Dictionary<int, WeakReference> _managedObjectOwners;

		// Token: 0x0400005C RID: 92
		private static readonly HashSet<ManagedObjectOwner> _managedObjectOwnerReferences;

		// Token: 0x0400005D RID: 93
		private static int _lastId = 10;

		// Token: 0x0400005E RID: 94
		private static readonly List<ManagedObjectOwner> _lastframedeletedManagedObjects;

		// Token: 0x0400005F RID: 95
		private static int _numberOfAliveManagedObjects = 0;

		// Token: 0x04000060 RID: 96
		private static readonly List<ManagedObjectOwner> _lastframedeletedManagedObjectBuffer;

		// Token: 0x04000061 RID: 97
		private Type _typeInfo;

		// Token: 0x04000062 RID: 98
		private int _nativeId;

		// Token: 0x04000063 RID: 99
		private UIntPtr _ptr;

		// Token: 0x04000064 RID: 100
		private readonly WeakReference _managedObject;

		// Token: 0x04000065 RID: 101
		private readonly WeakReference _managedObjectLongReference;
	}
}
