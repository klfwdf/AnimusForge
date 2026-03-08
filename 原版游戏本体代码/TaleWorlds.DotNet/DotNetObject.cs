using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200000C RID: 12
	public class DotNetObject
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000036 RID: 54 RVA: 0x000028DE File Offset: 0x00000ADE
		internal static int NumberOfAliveDotNetObjects
		{
			get
			{
				return DotNetObject._numberOfAliveDotNetObjects;
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000028E5 File Offset: 0x00000AE5
		static DotNetObject()
		{
			DotNetObject.DotnetKeepReferences = new Dictionary<int, DotNetObject.DotNetObjectKeeper>();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002908 File Offset: 0x00000B08
		protected DotNetObject()
		{
			object locker = DotNetObject.Locker;
			lock (locker)
			{
				DotNetObject._totalCreatedObjectCount++;
				this._objectId = DotNetObject._totalCreatedObjectCount;
				DotNetObject.DotNetObjectReferenceCounter value = default(DotNetObject.DotNetObjectReferenceCounter);
				value.DotNetObject = this;
				value.ReferenceCount = 0;
				value.CreationFrame = DotNetObject._frameNo;
				DotNetObject.DotnetObjectReferences.Add(this._objectId, value);
				DotNetObject.DotNetObjectKeeper dotNetObjectKeeper = new DotNetObject.DotNetObjectKeeper();
				dotNetObjectKeeper.DotNetObject = this;
				dotNetObjectKeeper.TimerToReleaseStrongRef = 200;
				dotNetObjectKeeper.gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
				DotNetObject.DotnetKeepReferences.Add(this._objectId, dotNetObjectKeeper);
				DotNetObject._numberOfAliveDotNetObjects++;
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000029D8 File Offset: 0x00000BD8
		protected override void Finalize()
		{
			try
			{
				object locker = DotNetObject.Locker;
				lock (locker)
				{
					DotNetObject._numberOfAliveDotNetObjects--;
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002A30 File Offset: 0x00000C30
		[LibraryCallback(null, false)]
		internal static int GetAliveDotNetObjectCount()
		{
			return DotNetObject._numberOfAliveDotNetObjects;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002A38 File Offset: 0x00000C38
		[LibraryCallback(null, false)]
		internal static void IncreaseReferenceCount(int dotnetObjectId)
		{
			object locker = DotNetObject.Locker;
			lock (locker)
			{
				if (DotNetObject.DotnetObjectReferences.ContainsKey(dotnetObjectId))
				{
					DotNetObject.DotNetObjectReferenceCounter value = DotNetObject.DotnetObjectReferences[dotnetObjectId];
					value.ReferenceCount++;
					DotNetObject.DotnetObjectReferences[dotnetObjectId] = value;
				}
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002AA4 File Offset: 0x00000CA4
		[LibraryCallback(null, false)]
		internal static void DecreaseReferenceCount(int dotnetObjectId)
		{
			object locker = DotNetObject.Locker;
			lock (locker)
			{
				DotNetObject.DotNetObjectReferenceCounter dotNetObjectReferenceCounter = DotNetObject.DotnetObjectReferences[dotnetObjectId];
				dotNetObjectReferenceCounter.ReferenceCount--;
				if (dotNetObjectReferenceCounter.ReferenceCount == 0)
				{
					DotNetObject.DotnetObjectReferences.Remove(dotnetObjectId);
					DotNetObject.DotNetObjectKeeper dotNetObjectKeeper;
					if (DotNetObject.DotnetKeepReferences.TryGetValue(dotnetObjectId, out dotNetObjectKeeper))
					{
						dotNetObjectKeeper.gcHandle.Free();
						DotNetObject.DotnetKeepReferences.Remove(dotnetObjectId);
					}
				}
				else
				{
					DotNetObject.DotnetObjectReferences[dotnetObjectId] = dotNetObjectReferenceCounter;
				}
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002B40 File Offset: 0x00000D40
		internal static DotNetObject GetManagedObjectWithId(int dotnetObjectId)
		{
			object locker = DotNetObject.Locker;
			DotNetObject result;
			lock (locker)
			{
				DotNetObject.DotNetObjectReferenceCounter dotNetObjectReferenceCounter;
				if (DotNetObject.DotnetObjectReferences.TryGetValue(dotnetObjectId, out dotNetObjectReferenceCounter))
				{
					result = dotNetObjectReferenceCounter.DotNetObject;
				}
				else if (dotnetObjectId == 0)
				{
					result = null;
				}
				else
				{
					result = new DotNetObject();
				}
			}
			return result;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002BA0 File Offset: 0x00000DA0
		internal int GetManagedId()
		{
			return this._objectId;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002BA8 File Offset: 0x00000DA8
		[LibraryCallback(null, false)]
		internal static string GetAliveDotNetObjectNames()
		{
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "GetAliveDotNetObjectNames");
			object locker = DotNetObject.Locker;
			lock (locker)
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				foreach (DotNetObject.DotNetObjectReferenceCounter dotNetObjectReferenceCounter in DotNetObject.DotnetObjectReferences.Values)
				{
					Type type = dotNetObjectReferenceCounter.DotNetObject.GetType();
					if (!dictionary.ContainsKey(type.Name))
					{
						dictionary.Add(type.Name, 1);
					}
					else
					{
						dictionary[type.Name] = dictionary[type.Name] + 1;
					}
				}
				foreach (string text in dictionary.Keys)
				{
					mbstringBuilder.Append<string>(string.Concat(new object[]
					{
						text,
						",",
						dictionary[text],
						"-"
					}));
				}
			}
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002D00 File Offset: 0x00000F00
		internal static void HandleDotNetObjects()
		{
			object locker = DotNetObject.Locker;
			lock (locker)
			{
				DotNetObject._frameNo += 1L;
				List<int> list = new List<int>();
				foreach (KeyValuePair<int, DotNetObject.DotNetObjectKeeper> keyValuePair in DotNetObject.DotnetKeepReferences)
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
					DotNetObject.DotnetKeepReferences.Remove(key);
				}
			}
		}

		// Token: 0x04000020 RID: 32
		private static readonly object Locker = new object();

		// Token: 0x04000021 RID: 33
		private const int DotnetObjectFirstReferencesTickCount = 200;

		// Token: 0x04000022 RID: 34
		private static long _frameNo;

		// Token: 0x04000023 RID: 35
		private static Dictionary<int, DotNetObject.DotNetObjectKeeper> DotnetKeepReferences;

		// Token: 0x04000024 RID: 36
		private static readonly Dictionary<int, DotNetObject.DotNetObjectReferenceCounter> DotnetObjectReferences = new Dictionary<int, DotNetObject.DotNetObjectReferenceCounter>();

		// Token: 0x04000025 RID: 37
		private static int _totalCreatedObjectCount;

		// Token: 0x04000026 RID: 38
		private readonly int _objectId;

		// Token: 0x04000027 RID: 39
		private static int _numberOfAliveDotNetObjects;

		// Token: 0x0200003A RID: 58
		private struct DotNetObjectReferenceCounter
		{
			// Token: 0x04000085 RID: 133
			internal int ReferenceCount;

			// Token: 0x04000086 RID: 134
			internal long CreationFrame;

			// Token: 0x04000087 RID: 135
			internal DotNetObject DotNetObject;
		}

		// Token: 0x0200003B RID: 59
		private class DotNetObjectKeeper
		{
			// Token: 0x04000088 RID: 136
			internal DotNetObject DotNetObject;

			// Token: 0x04000089 RID: 137
			internal int TimerToReleaseStrongRef;

			// Token: 0x0400008A RID: 138
			internal GCHandle gcHandle;
		}
	}
}
