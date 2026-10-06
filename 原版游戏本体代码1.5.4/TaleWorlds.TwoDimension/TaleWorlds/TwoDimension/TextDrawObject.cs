namespace TaleWorlds.TwoDimension;

public struct TextDrawObject : IDrawObject
{
	public bool IsValid;

	public float[] Text_Vertices;

	public float[] Text_TextureCoordinates;

	public uint[] Text_Indices;

	public float Text_MeshWidth;

	public float Text_MeshHeight;

	public ulong HashCode1;

	public ulong HashCode2;

	public Rectangle2D Rectangle;

	public static TextDrawObject Invalid => CreateInvalid();

	bool IDrawObject.IsValid => IsValid;

	Rectangle2D IDrawObject.Rectangle
	{
		get
		{
			return Rectangle;
		}
		set
		{
			Rectangle = value;
		}
	}

	private static TextDrawObject CreateInvalid()
	{
		return new TextDrawObject
		{
			IsValid = false
		};
	}

	public static TextDrawObject Create(float[] vertices, float[] uvs, uint[] indices, float text_MeshWidth, float text_MeshHeight, in Rectangle2D rectangle)
	{
		return new TextDrawObject
		{
			IsValid = true,
			Text_Vertices = vertices,
			Text_TextureCoordinates = uvs,
			Text_Indices = indices,
			Text_MeshWidth = text_MeshWidth,
			Text_MeshHeight = text_MeshHeight,
			Rectangle = rectangle
		};
	}

	public void ConvertToHashInPlace()
	{
		ulong num = 5381uL;
		ulong num2 = 5381uL;
		for (int i = 0; i < Text_Vertices.Length; i++)
		{
			num = (num << 5) + num + (ulong)(Text_Vertices[i] * 10000f);
		}
		for (int j = 0; j < Text_Indices.Length; j++)
		{
			num2 = (num2 << 5) + num2 + Text_Indices[j];
		}
		for (int k = 0; k < Text_TextureCoordinates.Length; k++)
		{
			num = (num << 5) + num + (ulong)(Text_TextureCoordinates[k] * 1000f);
		}
		num2 = (num2 << 5) + num2 + (ulong)Text_MeshWidth;
		num = (num << 5) + num + (ulong)Text_MeshHeight;
		HashCode1 = num;
		HashCode2 = num2;
	}
}
