using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000074 RID: 116
	[EngineClass("rglPath")]
	public sealed class Path : NativeObject
	{
		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000A77 RID: 2679 RVA: 0x0000AA3B File Offset: 0x00008C3B
		public int NumberOfPoints
		{
			get
			{
				return EngineApplicationInterface.IPath.GetNumberOfPoints(base.Pointer);
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000A78 RID: 2680 RVA: 0x0000AA4D File Offset: 0x00008C4D
		public float TotalDistance
		{
			get
			{
				return EngineApplicationInterface.IPath.GetTotalLength(base.Pointer);
			}
		}

		// Token: 0x06000A79 RID: 2681 RVA: 0x0000AA5F File Offset: 0x00008C5F
		internal Path(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000A7A RID: 2682 RVA: 0x0000AA70 File Offset: 0x00008C70
		public MatrixFrame GetHermiteFrameForDt(float phase, int first_point)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			EngineApplicationInterface.IPath.GetHermiteFrameForDt(base.Pointer, ref identity, phase, first_point);
			return identity;
		}

		// Token: 0x06000A7B RID: 2683 RVA: 0x0000AA98 File Offset: 0x00008C98
		public MatrixFrame GetFrameForDistance(float distance)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			EngineApplicationInterface.IPath.GetHermiteFrameForDistance(base.Pointer, ref identity, distance);
			return identity;
		}

		// Token: 0x06000A7C RID: 2684 RVA: 0x0000AAC0 File Offset: 0x00008CC0
		public MatrixFrame GetNearestFrameWithValidAlphaForDistance(float distance, bool searchForward = true, float alphaThreshold = 0.5f)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			EngineApplicationInterface.IPath.GetNearestHermiteFrameWithValidAlphaForDistance(base.Pointer, ref identity, distance, searchForward, alphaThreshold);
			return identity;
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x0000AAE9 File Offset: 0x00008CE9
		public void GetFrameAndColorForDistance(float distance, out MatrixFrame frame, out Vec3 color)
		{
			frame = MatrixFrame.Identity;
			EngineApplicationInterface.IPath.GetHermiteFrameAndColorForDistance(base.Pointer, out frame, out color, distance);
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x0000AB09 File Offset: 0x00008D09
		public float GetArcLength(int first_point)
		{
			return EngineApplicationInterface.IPath.GetArcLength(base.Pointer, first_point);
		}

		// Token: 0x06000A7F RID: 2687 RVA: 0x0000AB1C File Offset: 0x00008D1C
		public void GetPoints(MatrixFrame[] points)
		{
			EngineApplicationInterface.IPath.GetPoints(base.Pointer, points);
		}

		// Token: 0x06000A80 RID: 2688 RVA: 0x0000AB2F File Offset: 0x00008D2F
		public float GetTotalLength()
		{
			return EngineApplicationInterface.IPath.GetTotalLength(base.Pointer);
		}

		// Token: 0x06000A81 RID: 2689 RVA: 0x0000AB41 File Offset: 0x00008D41
		public int GetVersion()
		{
			return EngineApplicationInterface.IPath.GetVersion(base.Pointer);
		}

		// Token: 0x06000A82 RID: 2690 RVA: 0x0000AB53 File Offset: 0x00008D53
		public void SetFrameOfPoint(int pointIndex, ref MatrixFrame frame)
		{
			EngineApplicationInterface.IPath.SetFrameOfPoint(base.Pointer, pointIndex, ref frame);
		}

		// Token: 0x06000A83 RID: 2691 RVA: 0x0000AB67 File Offset: 0x00008D67
		public void SetTangentPositionOfPoint(int pointIndex, int tangentIndex, ref Vec3 position)
		{
			EngineApplicationInterface.IPath.SetTangentPositionOfPoint(base.Pointer, pointIndex, tangentIndex, ref position);
		}

		// Token: 0x06000A84 RID: 2692 RVA: 0x0000AB7C File Offset: 0x00008D7C
		public int AddPathPoint(int newNodeIndex)
		{
			return EngineApplicationInterface.IPath.AddPathPoint(base.Pointer, newNodeIndex);
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x0000AB8F File Offset: 0x00008D8F
		public void DeletePathPoint(int nodeIndex)
		{
			EngineApplicationInterface.IPath.DeletePathPoint(base.Pointer, nodeIndex);
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x0000ABA2 File Offset: 0x00008DA2
		public bool HasValidAlphaAtPathPoint(int nodeIndex, float alphaThreshold = 0.5f)
		{
			return EngineApplicationInterface.IPath.HasValidAlphaAtPathPoint(base.Pointer, nodeIndex, alphaThreshold);
		}

		// Token: 0x06000A87 RID: 2695 RVA: 0x0000ABB6 File Offset: 0x00008DB6
		public string GetName()
		{
			return EngineApplicationInterface.IPath.GetName(base.Pointer);
		}
	}
}
