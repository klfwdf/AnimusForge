using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200002C RID: 44
	[EngineClass("ftdnNative_object_array")]
	public sealed class NativeObjectArray : NativeObject, IEnumerable<NativeObject>, IEnumerable
	{
		// Token: 0x06000124 RID: 292 RVA: 0x0000568C File Offset: 0x0000388C
		internal NativeObjectArray(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000569B File Offset: 0x0000389B
		public static NativeObjectArray Create()
		{
			return LibraryApplicationInterface.INativeObjectArray.Create();
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000126 RID: 294 RVA: 0x000056A7 File Offset: 0x000038A7
		public int Count
		{
			get
			{
				return LibraryApplicationInterface.INativeObjectArray.GetCount(base.Pointer);
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x000056B9 File Offset: 0x000038B9
		public NativeObject GetElementAt(int index)
		{
			return LibraryApplicationInterface.INativeObjectArray.GetElementAtIndex(base.Pointer, index);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x000056CC File Offset: 0x000038CC
		public void AddElement(NativeObject nativeObject)
		{
			LibraryApplicationInterface.INativeObjectArray.AddElement(base.Pointer, (nativeObject != null) ? nativeObject.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000129 RID: 297 RVA: 0x000056F4 File Offset: 0x000038F4
		public void Clear()
		{
			LibraryApplicationInterface.INativeObjectArray.Clear(base.Pointer);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00005706 File Offset: 0x00003906
		IEnumerator<NativeObject> IEnumerable<NativeObject>.GetEnumerator()
		{
			int count = this.Count;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				NativeObject elementAt = this.GetElementAt(i);
				yield return elementAt;
				num = i;
			}
			yield break;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x00005715 File Offset: 0x00003915
		IEnumerator IEnumerable.GetEnumerator()
		{
			int count = this.Count;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				NativeObject elementAt = this.GetElementAt(i);
				yield return elementAt;
				num = i;
			}
			yield break;
		}
	}
}
