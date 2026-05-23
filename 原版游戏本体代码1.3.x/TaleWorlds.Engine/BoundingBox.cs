using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200000E RID: 14
	[EngineStruct("rglBounding_box::Plain_bounding_box", false, null)]
	public struct BoundingBox
	{
		// Token: 0x17000007 RID: 7
		public Vec3 this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return new Vec3(this.min.x, this.min.y, this.min.z, -1f);
				case 1:
					return new Vec3(this.max.x, this.max.y, this.max.z, -1f);
				case 2:
					return new Vec3(this.min.x, this.max.y, this.min.z, -1f);
				case 3:
					return new Vec3(this.max.x, this.max.y, this.min.z, -1f);
				case 4:
					return new Vec3(this.min.x, this.min.y, this.max.z, -1f);
				case 5:
					return new Vec3(this.max.x, this.min.y, this.max.z, -1f);
				case 6:
					return new Vec3(this.min.x, this.max.y, this.max.z, -1f);
				case 7:
					return new Vec3(this.max.x, this.min.y, this.min.z, -1f);
				default:
					throw new IndexOutOfRangeException();
				}
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000029D9 File Offset: 0x00000BD9
		public BoundingBox(in Vec3 point)
		{
			this.min = point;
			this.max = point;
			this.center = point;
			this.radius = 0f;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002A0C File Offset: 0x00000C0C
		public void RelaxMinMaxWithPoint(in Vec3 point)
		{
			this.min.x = MathF.Min(this.min.x, point.x);
			this.min.y = MathF.Min(this.min.y, point.y);
			this.min.z = MathF.Min(this.min.z, point.z);
			this.max.x = MathF.Max(this.max.x, point.x);
			this.max.y = MathF.Max(this.max.y, point.y);
			this.max.z = MathF.Max(this.max.z, point.z);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002AE0 File Offset: 0x00000CE0
		public void RelaxMinMaxWithPointAndRadius(in Vec3 point, float radius)
		{
			this.min.x = MathF.Min(this.min.x, point.x - radius);
			this.min.y = MathF.Min(this.min.y, point.y - radius);
			this.min.z = MathF.Min(this.min.z, point.z - radius);
			this.max.x = MathF.Max(this.max.x, point.x + radius);
			this.max.y = MathF.Max(this.max.y, point.y + radius);
			this.max.z = MathF.Max(this.max.z, point.z + radius);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002BC0 File Offset: 0x00000DC0
		public void RecomputeRadius()
		{
			this.center = 0.5f * (this.min + this.max);
			this.radius = (this.max - this.center).Length;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002C10 File Offset: 0x00000E10
		public BoundingBox.TransformedBoundingBoxPointsContainer GetTransformedTipPointsToParent(in MatrixFrame parentFrame)
		{
			BoundingBox.TransformedBoundingBoxPointsContainer result = default(BoundingBox.TransformedBoundingBoxPointsContainer);
			for (int i = 0; i < 8; i++)
			{
				int index = i;
				MatrixFrame matrixFrame = parentFrame;
				Vec3 vec = this[i];
				result[index] = matrixFrame.TransformToParent(vec);
			}
			return result;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002C54 File Offset: 0x00000E54
		public BoundingBox.TransformedBoundingBoxPointsContainer GetTransformedTipPointsToChild(in MatrixFrame childFrame)
		{
			BoundingBox.TransformedBoundingBoxPointsContainer result = default(BoundingBox.TransformedBoundingBoxPointsContainer);
			for (int i = 0; i < 8; i++)
			{
				int index = i;
				MatrixFrame matrixFrame = childFrame;
				Vec3 vec = this[i];
				result[index] = matrixFrame.TransformToLocal(vec);
			}
			return result;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002C98 File Offset: 0x00000E98
		public void RelaxWithBoundingBox(BoundingBox modifiedBoundingBox)
		{
			for (int i = 0; i < 8; i++)
			{
				Vec3 vec = modifiedBoundingBox[i];
				this.RelaxMinMaxWithPoint(vec);
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002CC4 File Offset: 0x00000EC4
		public void RelaxWithArbitraryBoundingBox(BoundingBox otherBoundingBox, MatrixFrame otherGlobalFrame, MatrixFrame globalFrameOfThisBoundingBox)
		{
			BoundingBox.TransformedBoundingBoxPointsContainer transformedTipPointsToParent = otherBoundingBox.GetTransformedTipPointsToParent(otherGlobalFrame);
			for (int i = 0; i < 8; i++)
			{
				Vec3 vec = transformedTipPointsToParent[i];
				Vec3 vec2 = globalFrameOfThisBoundingBox.TransformToLocal(vec);
				this.RelaxMinMaxWithPoint(vec2);
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002D04 File Offset: 0x00000F04
		public void RelaxWithChildBoundingBox(BoundingBox childBoundingBox, MatrixFrame childFrame)
		{
			BoundingBox.TransformedBoundingBoxPointsContainer transformedTipPointsToParent = childBoundingBox.GetTransformedTipPointsToParent(childFrame);
			for (int i = 0; i < 8; i++)
			{
				Vec3 vec = transformedTipPointsToParent[i];
				this.RelaxMinMaxWithPoint(vec);
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002D38 File Offset: 0x00000F38
		public void BeginRelaxation()
		{
			this.min = new Vec3(100000000f, 100000000f, 100000000f, -1f);
			this.max = new Vec3(-100000000f, -100000000f, -100000000f, -1f);
			this.radius = 0f;
			this.center = new Vec3(0f, 0f, 0f, -1f);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002DB0 File Offset: 0x00000FB0
		private static bool ModifyPlane(ref float plane, float otherPlane, float modifyAmount, float changeTolerance, bool isMin)
		{
			bool result = false;
			if (isMin)
			{
				if (otherPlane < plane)
				{
					plane = otherPlane - modifyAmount;
					result = true;
				}
				else if (otherPlane - plane >= modifyAmount * 2.5f)
				{
					plane = otherPlane - changeTolerance;
					result = true;
				}
			}
			else if (otherPlane > plane)
			{
				plane = otherPlane + modifyAmount;
				result = true;
			}
			else if (plane - otherPlane >= modifyAmount * 2.5f)
			{
				plane = otherPlane + changeTolerance;
				result = true;
			}
			return result;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002E0C File Offset: 0x0000100C
		public static bool ArrangeWithAnotherBoundingBox(ref BoundingBox boundingBox, BoundingBox otherBoundingBox, float changeAmount)
		{
			bool flag = false;
			float changeTolerance = changeAmount * 0.25f;
			flag = BoundingBox.ModifyPlane(ref boundingBox.min.x, otherBoundingBox.min.x, changeAmount, changeTolerance, true) || flag;
			flag = BoundingBox.ModifyPlane(ref boundingBox.max.x, otherBoundingBox.max.x, changeAmount, changeTolerance, false) || flag;
			flag = BoundingBox.ModifyPlane(ref boundingBox.min.y, otherBoundingBox.min.y, changeAmount, changeTolerance, true) || flag;
			flag = BoundingBox.ModifyPlane(ref boundingBox.max.y, otherBoundingBox.max.y, changeAmount, changeTolerance, false) || flag;
			flag = BoundingBox.ModifyPlane(ref boundingBox.min.z, otherBoundingBox.min.z, changeAmount, changeTolerance, true) || flag;
			flag = BoundingBox.ModifyPlane(ref boundingBox.max.z, otherBoundingBox.max.z, changeAmount, changeTolerance, false) || flag;
			if (flag)
			{
				boundingBox.RecomputeRadius();
			}
			return flag;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00002EF4 File Offset: 0x000010F4
		public bool PointInsideBox(Vec3 point, float epsilon)
		{
			return point.x + epsilon <= this.max.x && point.x - epsilon >= this.min.x && point.y + epsilon <= this.max.y && point.y - epsilon >= this.min.y && point.z + epsilon <= this.max.z && point.z - epsilon >= this.min.z;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00002F88 File Offset: 0x00001188
		public static float GetLongestHalfDimensionOfBoundingBox(BoundingBox boundingBox)
		{
			Vec3 vec = boundingBox.max - boundingBox.center;
			Vec3 vec2 = boundingBox.center - boundingBox.min;
			return MathF.Max(MathF.Max(vec.x, vec.y, vec.z), MathF.Max(vec2.x, vec2.y, vec2.z));
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002FEC File Offset: 0x000011EC
		public void RenderBoundingBox()
		{
		}

		// Token: 0x04000021 RID: 33
		[CustomEngineStructMemberData("box_min_")]
		public Vec3 min;

		// Token: 0x04000022 RID: 34
		[CustomEngineStructMemberData("box_max_")]
		public Vec3 max;

		// Token: 0x04000023 RID: 35
		[CustomEngineStructMemberData("box_center_")]
		public Vec3 center;

		// Token: 0x04000024 RID: 36
		[CustomEngineStructMemberData("radius_")]
		public float radius;

		// Token: 0x020000B0 RID: 176
		public struct TransformedBoundingBoxPointsContainer
		{
			// Token: 0x170000C3 RID: 195
			public Vec3 this[int index]
			{
				get
				{
					switch (index)
					{
					case 0:
						return this.p0;
					case 1:
						return this.p1;
					case 2:
						return this.p2;
					case 3:
						return this.p3;
					case 4:
						return this.p4;
					case 5:
						return this.p5;
					case 6:
						return this.p6;
					case 7:
						return this.p7;
					default:
						throw new IndexOutOfRangeException(string.Format("Invalid index: {0}", index));
					}
				}
				set
				{
					switch (index)
					{
					case 0:
						this.p0 = value;
						return;
					case 1:
						this.p1 = value;
						return;
					case 2:
						this.p2 = value;
						return;
					case 3:
						this.p3 = value;
						return;
					case 4:
						this.p4 = value;
						return;
					case 5:
						this.p5 = value;
						return;
					case 6:
						this.p6 = value;
						return;
					case 7:
						this.p7 = value;
						return;
					default:
						throw new IndexOutOfRangeException(string.Format("Invalid index: {0}", index));
					}
				}
			}

			// Token: 0x06000FA3 RID: 4003 RVA: 0x00013814 File Offset: 0x00011A14
			public ValueTuple<Vec3, Vec3> ComputeTransformedMinMax()
			{
				Vec3 vec = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue, -1f);
				Vec3 vec2 = new Vec3(float.MinValue, float.MinValue, float.MinValue, -1f);
				for (int i = 0; i < 8; i++)
				{
					vec = Vec3.Vec3Min(vec, this[i]);
					vec2 = Vec3.Vec3Max(vec2, this[i]);
				}
				return new ValueTuple<Vec3, Vec3>(vec, vec2);
			}

			// Token: 0x0400034F RID: 847
			public Vec3 p0;

			// Token: 0x04000350 RID: 848
			public Vec3 p1;

			// Token: 0x04000351 RID: 849
			public Vec3 p2;

			// Token: 0x04000352 RID: 850
			public Vec3 p3;

			// Token: 0x04000353 RID: 851
			public Vec3 p4;

			// Token: 0x04000354 RID: 852
			public Vec3 p5;

			// Token: 0x04000355 RID: 853
			public Vec3 p6;

			// Token: 0x04000356 RID: 854
			public Vec3 p7;
		}
	}
}
