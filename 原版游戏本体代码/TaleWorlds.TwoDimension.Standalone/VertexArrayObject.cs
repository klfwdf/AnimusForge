using System;
using TaleWorlds.TwoDimension.Standalone.Native;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000E RID: 14
	public class VertexArrayObject
	{
		// Token: 0x060000C5 RID: 197 RVA: 0x00004AF3 File Offset: 0x00002CF3
		private VertexArrayObject(uint vertexArrayObject)
		{
			this._vertexArrayObject = vertexArrayObject;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00004B02 File Offset: 0x00002D02
		public void LoadVertexData(float[] vertices)
		{
			this.LoadDataToBuffer(this._vertexBuffer, vertices);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00004B11 File Offset: 0x00002D11
		public void LoadUVData(float[] uvs)
		{
			this.LoadDataToBuffer(this._uvBuffer, uvs);
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00004B20 File Offset: 0x00002D20
		public void LoadIndexData(uint[] indices)
		{
			this.LoadDataToIndexBuffer(this._indexBuffer, indices);
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00004B30 File Offset: 0x00002D30
		private void LoadDataToBuffer(uint buffer, float[] data)
		{
			this.Bind();
			using (AutoPinner autoPinner = new AutoPinner(data))
			{
				IntPtr data2 = autoPinner;
				Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, buffer);
				Opengl32ARB.BufferSubData(BufferBindingTarget.ArrayBuffer, 0, data.Length * 4, data2);
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00004B94 File Offset: 0x00002D94
		private void LoadDataToIndexBuffer(uint buffer, uint[] data)
		{
			using (AutoPinner autoPinner = new AutoPinner(data))
			{
				IntPtr data2 = autoPinner;
				Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, buffer);
				Opengl32ARB.BufferSubData(BufferBindingTarget.ElementArrayBuffer, 0, data.Length * 4, data2);
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00004BF4 File Offset: 0x00002DF4
		public void Bind()
		{
			Opengl32ARB.BindVertexArray(this._vertexArrayObject);
			Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, this._indexBuffer);
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00004C1B File Offset: 0x00002E1B
		public static void UnBind()
		{
			Opengl32ARB.BindVertexArray(0U);
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00004C28 File Offset: 0x00002E28
		private static uint CreateArrayBuffer()
		{
			uint[] array = new uint[1];
			Opengl32ARB.GenBuffers(1, array);
			uint num = array[0];
			Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, num);
			Opengl32ARB.BufferData(BufferBindingTarget.ArrayBuffer, 524288, IntPtr.Zero, 35048);
			return num;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00004C7C File Offset: 0x00002E7C
		private static uint CreateElementArrayBuffer()
		{
			uint[] array = new uint[1];
			Opengl32ARB.GenBuffers(1, array);
			uint num = array[0];
			Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, num);
			Opengl32ARB.BufferData(BufferBindingTarget.ElementArrayBuffer, 524288, IntPtr.Zero, 35048);
			return num;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00004CD0 File Offset: 0x00002ED0
		public static VertexArrayObject Create()
		{
			VertexArrayObject vertexArrayObject = new VertexArrayObject(VertexArrayObject.CreateVertexArray());
			uint num = VertexArrayObject.CreateArrayBuffer();
			VertexArrayObject.BindBuffer(0U, num);
			uint num2 = VertexArrayObject.CreateElementArrayBuffer();
			VertexArrayObject.BindIndexBuffer(num2);
			vertexArrayObject._vertexBuffer = num;
			vertexArrayObject._indexBuffer = num2;
			return vertexArrayObject;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00004D10 File Offset: 0x00002F10
		public static VertexArrayObject CreateWithUVBuffer()
		{
			VertexArrayObject vertexArrayObject = new VertexArrayObject(VertexArrayObject.CreateVertexArray());
			uint num = VertexArrayObject.CreateArrayBuffer();
			uint num2 = VertexArrayObject.CreateArrayBuffer();
			VertexArrayObject.BindBuffer(0U, num);
			VertexArrayObject.BindBuffer(1U, num2);
			uint num3 = VertexArrayObject.CreateElementArrayBuffer();
			VertexArrayObject.BindIndexBuffer(num3);
			vertexArrayObject._vertexBuffer = num;
			vertexArrayObject._uvBuffer = num2;
			vertexArrayObject._indexBuffer = num3;
			return vertexArrayObject;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00004D62 File Offset: 0x00002F62
		private static void BindBuffer(uint index, uint buffer)
		{
			Opengl32ARB.EnableVertexAttribArray(index);
			Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, buffer);
			Opengl32ARB.VertexAttribPointer(index, 2, DataType.Float, 0, 0, IntPtr.Zero);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00004D97 File Offset: 0x00002F97
		private static void BindIndexBuffer(uint buffer)
		{
			Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, buffer);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00004DAC File Offset: 0x00002FAC
		private static uint CreateVertexArray()
		{
			uint[] array = new uint[1];
			Opengl32ARB.GenVertexArrays(1, array);
			uint num = array[0];
			Opengl32ARB.BindVertexArray(num);
			return num;
		}

		// Token: 0x04000043 RID: 67
		private uint _vertexArrayObject;

		// Token: 0x04000044 RID: 68
		private uint _vertexBuffer;

		// Token: 0x04000045 RID: 69
		private uint _uvBuffer;

		// Token: 0x04000046 RID: 70
		private uint _indexBuffer;
	}
}
