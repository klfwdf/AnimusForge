using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000096 RID: 150
	[Serializable]
	public struct Transformation
	{
		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600055B RID: 1371 RVA: 0x00013164 File Offset: 0x00011364
		public static Transformation Identity
		{
			get
			{
				return new Transformation(new Vec3(0f, 0f, 0f, 1f), Mat3.Identity, Vec3.One);
			}
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x0001318E File Offset: 0x0001138E
		public Transformation(Vec3 origin, Mat3 rotation, Vec3 scale)
		{
			this.Origin = origin;
			this.Rotation = rotation;
			this.Scale = scale;
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x0600055D RID: 1373 RVA: 0x000131A8 File Offset: 0x000113A8
		public MatrixFrame AsMatrixFrame
		{
			get
			{
				MatrixFrame result = default(MatrixFrame);
				result.origin = this.Origin;
				result.rotation = this.Rotation;
				result.rotation.ApplyScaleLocal(this.Scale);
				result.Fill();
				return result;
			}
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x000131F4 File Offset: 0x000113F4
		public static Transformation CreateFromMatrixFrame(MatrixFrame matrixFrame)
		{
			Mat3 rotation = matrixFrame.rotation;
			Vec3 scaleVector = matrixFrame.rotation.GetScaleVector();
			Vec3 vec = new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z, -1f);
			rotation.ApplyScaleLocal(vec);
			return new Transformation(matrixFrame.origin, rotation, scaleVector);
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x0001325A File Offset: 0x0001145A
		public static Transformation CreateFromRotation(Mat3 rotation)
		{
			return new Transformation(Vec3.Zero, rotation, Vec3.One);
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x0001326C File Offset: 0x0001146C
		public Vec3 TransformToParent(Vec3 v)
		{
			return this.AsMatrixFrame.TransformToParent(v);
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x0001328C File Offset: 0x0001148C
		public Transformation TransformToParent(Transformation t)
		{
			MatrixFrame asMatrixFrame = this.AsMatrixFrame;
			MatrixFrame asMatrixFrame2 = t.AsMatrixFrame;
			return Transformation.CreateFromMatrixFrame(asMatrixFrame.TransformToParent(asMatrixFrame2));
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x000132B8 File Offset: 0x000114B8
		public Vec3 TransformToLocal(Vec3 v)
		{
			return this.AsMatrixFrame.TransformToLocal(v);
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x000132D8 File Offset: 0x000114D8
		public Transformation TransformToLocal(Transformation t)
		{
			MatrixFrame asMatrixFrame = this.AsMatrixFrame;
			MatrixFrame asMatrixFrame2 = t.AsMatrixFrame;
			return Transformation.CreateFromMatrixFrame(asMatrixFrame.TransformToLocal(asMatrixFrame2));
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x00013304 File Offset: 0x00011504
		public void Rotate(float radian, Vec3 axis)
		{
			Transformation transformation = this;
			transformation.Scale = Vec3.One;
			MatrixFrame asMatrixFrame = transformation.AsMatrixFrame;
			asMatrixFrame.Rotate(radian, axis);
			this.Rotation = asMatrixFrame.rotation;
			this.Origin = asMatrixFrame.origin;
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x0001334E File Offset: 0x0001154E
		public static bool operator ==(Transformation t1, Transformation t2)
		{
			return t1.Origin == t2.Origin && (t1.Rotation) == (t2.Rotation) && t1.Scale == t2.Scale;
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x0001338C File Offset: 0x0001158C
		public void ApplyScale(Vec3 vec3)
		{
			this.Scale.x = this.Scale.x * vec3.x;
			this.Scale.y = this.Scale.y * vec3.y;
			this.Scale.z = this.Scale.z * vec3.z;
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x000133D8 File Offset: 0x000115D8
		public static bool operator !=(Transformation t1, Transformation t2)
		{
			return t1.Origin != t2.Origin || (t1.Rotation) != (t2.Rotation) || t1.Scale != t2.Scale;
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00013415 File Offset: 0x00011615
		public override bool Equals(object obj)
		{
			return this == (Transformation)obj;
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x00013428 File Offset: 0x00011628
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x0001343C File Offset: 0x0001163C
		public override string ToString()
		{
			string text = "Transformation:\n";
			text = string.Concat(new object[]
			{
				text,
				"Origin: ",
				this.Origin.x,
				", ",
				this.Origin.y,
				", ",
				this.Origin.z,
				"\n"
			});
			text += "Rotation:\n";
			text += this.Rotation.ToString();
			return string.Concat(new object[]
			{
				text,
				"Scale: ",
				this.Scale.x,
				", ",
				this.Scale.y,
				", ",
				this.Scale.z,
				"\n"
			});
		}

		// Token: 0x040001AA RID: 426
		public Vec3 Origin;

		// Token: 0x040001AB RID: 427
		public Mat3 Rotation;

		// Token: 0x040001AC RID: 428
		public Vec3 Scale;
	}
}
