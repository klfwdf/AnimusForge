using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000004 RID: 4
	public struct AnimResult
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		internal static AnimResult CreateWithPointer(UIntPtr pointer)
		{
			return new AnimResult
			{
				_nativePointer = pointer
			};
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002076 File Offset: 0x00000276
		public Transformation GetEntitialOutTransform(sbyte boneIndex, Skeleton skeleton)
		{
			return skeleton.GetEntitialOutTransform(this._nativePointer, boneIndex);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002085 File Offset: 0x00000285
		public void SetOutBoneDisplacement(sbyte boneIndex, Vec3 position, Skeleton skeleton)
		{
			skeleton.SetOutBoneDisplacement(this._nativePointer, boneIndex, position);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002095 File Offset: 0x00000295
		public void SetOutQuat(sbyte boneIndex, Mat3 rotation, Skeleton skeleton)
		{
			skeleton.SetOutQuat(this._nativePointer, boneIndex, rotation);
		}

		// Token: 0x04000001 RID: 1
		private UIntPtr _nativePointer;
	}
}
