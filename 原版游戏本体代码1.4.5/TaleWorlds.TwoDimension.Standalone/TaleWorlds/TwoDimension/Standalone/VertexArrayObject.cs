using System;
using TaleWorlds.TwoDimension.Standalone.Native;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone;

public class VertexArrayObject
{
	private uint _vertexArrayObject;

	private uint _vertexBuffer;

	private uint _uvBuffer;

	private uint _indexBuffer;

	private VertexArrayObject(uint vertexArrayObject)
	{
		_vertexArrayObject = vertexArrayObject;
	}

	public void LoadVertexData(float[] vertices)
	{
		LoadDataToBuffer(_vertexBuffer, vertices);
	}

	public void LoadUVData(float[] uvs)
	{
		LoadDataToBuffer(_uvBuffer, uvs);
	}

	public void LoadIndexData(uint[] indices)
	{
		LoadDataToIndexBuffer(_indexBuffer, indices);
	}

	private void LoadDataToBuffer(uint buffer, float[] data)
	{
		Bind();
		using AutoPinner autoPinner = new AutoPinner(data);
		IntPtr data2 = autoPinner;
		Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, buffer);
		Opengl32ARB.BufferSubData(BufferBindingTarget.ArrayBuffer, 0, data.Length * 4, data2);
	}

	private void LoadDataToIndexBuffer(uint buffer, uint[] data)
	{
		using AutoPinner autoPinner = new AutoPinner(data);
		IntPtr data2 = autoPinner;
		Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, buffer);
		Opengl32ARB.BufferSubData(BufferBindingTarget.ElementArrayBuffer, 0, data.Length * 4, data2);
	}

	public void Bind()
	{
		Opengl32ARB.BindVertexArray(_vertexArrayObject);
		Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, _indexBuffer);
	}

	public static void UnBind()
	{
		Opengl32ARB.BindVertexArray(0u);
	}

	private static uint CreateArrayBuffer()
	{
		uint[] array = new uint[1];
		Opengl32ARB.GenBuffers(1, array);
		uint num = array[0];
		Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, num);
		Opengl32ARB.BufferData(BufferBindingTarget.ArrayBuffer, 524288, IntPtr.Zero, 35048);
		return num;
	}

	private static uint CreateElementArrayBuffer()
	{
		uint[] array = new uint[1];
		Opengl32ARB.GenBuffers(1, array);
		uint num = array[0];
		Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, num);
		Opengl32ARB.BufferData(BufferBindingTarget.ElementArrayBuffer, 524288, IntPtr.Zero, 35048);
		return num;
	}

	public static VertexArrayObject Create()
	{
		VertexArrayObject vertexArrayObject = new VertexArrayObject(CreateVertexArray());
		uint num = CreateArrayBuffer();
		BindBuffer(0u, num);
		uint num2 = CreateElementArrayBuffer();
		BindIndexBuffer(num2);
		vertexArrayObject._vertexBuffer = num;
		vertexArrayObject._indexBuffer = num2;
		return vertexArrayObject;
	}

	public static VertexArrayObject CreateWithUVBuffer()
	{
		VertexArrayObject vertexArrayObject = new VertexArrayObject(CreateVertexArray());
		uint num = CreateArrayBuffer();
		uint num2 = CreateArrayBuffer();
		BindBuffer(0u, num);
		BindBuffer(1u, num2);
		uint num3 = CreateElementArrayBuffer();
		BindIndexBuffer(num3);
		vertexArrayObject._vertexBuffer = num;
		vertexArrayObject._uvBuffer = num2;
		vertexArrayObject._indexBuffer = num3;
		return vertexArrayObject;
	}

	private static void BindBuffer(uint index, uint buffer)
	{
		Opengl32ARB.EnableVertexAttribArray(index);
		Opengl32ARB.BindBuffer(BufferBindingTarget.ArrayBuffer, buffer);
		Opengl32ARB.VertexAttribPointer(index, 2, DataType.Float, 0, 0, IntPtr.Zero);
	}

	private static void BindIndexBuffer(uint buffer)
	{
		Opengl32ARB.BindBuffer(BufferBindingTarget.ElementArrayBuffer, buffer);
	}

	private static uint CreateVertexArray()
	{
		uint[] array = new uint[1];
		Opengl32ARB.GenVertexArrays(1, array);
		uint num = array[0];
		Opengl32ARB.BindVertexArray(num);
		return num;
	}
}
