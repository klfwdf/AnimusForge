using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace HarmonyLib
{
	/// <summary>Patch serialization</summary>
	// Token: 0x020000A5 RID: 165
	internal static class PatchInfoSerialization
	{
		/// <summary>Serializes a patch info</summary>
		/// <param name="patchInfo">The <see cref="T:HarmonyLib.PatchInfo" /></param>
		/// <returns>The serialized data</returns>
		// Token: 0x0600034B RID: 843 RVA: 0x00011B88 File Offset: 0x0000FD88
		internal static byte[] Serialize(this PatchInfo patchInfo)
		{
			byte[] result;
			using (MemoryStream streamMemory = new MemoryStream())
			{
				PatchInfoSerialization.binaryFormatter.Serialize(streamMemory, patchInfo);
				result = streamMemory.ToArray();
			}
			return result;
		}

		/// <summary>Deserialize a patch info</summary>
		/// <param name="bytes">The serialized data</param>
		/// <returns>A <see cref="T:HarmonyLib.PatchInfo" /></returns>
		// Token: 0x0600034C RID: 844 RVA: 0x00011BCC File Offset: 0x0000FDCC
		internal static PatchInfo Deserialize(byte[] bytes)
		{
			PatchInfo result;
			using (MemoryStream streamMemory = new MemoryStream(bytes))
			{
				result = (PatchInfo)PatchInfoSerialization.binaryFormatter.Deserialize(streamMemory);
			}
			return result;
		}

		/// <summary>Compare function to sort patch priorities</summary>
		/// <param name="obj">The patch</param>
		/// <param name="index">Zero-based index</param>
		/// <param name="priority">The priority</param>
		/// <returns>A standard sort integer (-1, 0, 1)</returns>
		// Token: 0x0600034D RID: 845 RVA: 0x00011C10 File Offset: 0x0000FE10
		internal static int PriorityComparer(object obj, int index, int priority)
		{
			Traverse trv = Traverse.Create(obj);
			int theirPriority = trv.Field("priority").GetValue<int>();
			int theirIndex = trv.Field("index").GetValue<int>();
			if (priority != theirPriority)
			{
				return -priority.CompareTo(theirPriority);
			}
			return index.CompareTo(theirIndex);
		}

		// Token: 0x0400023B RID: 571
		internal static readonly BinaryFormatter binaryFormatter = new BinaryFormatter
		{
			Binder = new PatchInfoSerialization.Binder()
		};

		// Token: 0x020000A6 RID: 166
		private class Binder : SerializationBinder
		{
			/// <summary>Control the binding of a serialized object to a type</summary>
			/// <param name="assemblyName">Specifies the assembly name of the serialized object</param>
			/// <param name="typeName">Specifies the type name of the serialized object</param>
			/// <returns>The type of the object the formatter creates a new instance of</returns>
			// Token: 0x0600034F RID: 847 RVA: 0x00011C74 File Offset: 0x0000FE74
			public override Type BindToType(string assemblyName, string typeName)
			{
				Type[] types = new Type[]
				{
					typeof(PatchInfo),
					typeof(Patch[]),
					typeof(Patch)
				};
				foreach (Type type in types)
				{
					if (typeName == type.FullName)
					{
						return type;
					}
				}
				return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
			}
		}
	}
}
