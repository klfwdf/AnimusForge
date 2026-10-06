using System.Numerics;

namespace TaleWorlds.TwoDimension;

internal class TextMeshGenerator
{
	private struct MeshNormalizationInfo
	{
		public readonly float MeshWidth;

		public readonly float MeshHeight;

		public readonly float[] NormalizedVertices;

		public MeshNormalizationInfo(float meshWidth, float meshHeight, float[] normalizedVertices)
		{
			MeshWidth = meshWidth;
			MeshHeight = meshHeight;
			NormalizedVertices = normalizedVertices;
		}
	}

	private Font _font;

	private float[] _vertices;

	private MeshNormalizationInfo _meshNormalizationInfo;

	private float[] _uvs;

	private uint[] _indices;

	private int _textMeshCharacterCount;

	private float _scaleValue;

	private bool _areVerticesNormalized;

	private Rectangle2D _drawRectangle;

	internal TextMeshGenerator()
	{
		_scaleValue = 1f;
		_drawRectangle = Rectangle2D.Create();
	}

	internal void Refresh(Font font, int possibleMaxCharacterLength, float scaleValue)
	{
		_font = font;
		_textMeshCharacterCount = 0;
		int num = possibleMaxCharacterLength * 8 * 2;
		int num2 = possibleMaxCharacterLength * 8 * 2;
		if (_vertices == null || _vertices.Length < num)
		{
			_vertices = new float[num];
		}
		if (_uvs == null || _uvs.Length < num2)
		{
			_uvs = new float[num2];
		}
		_scaleValue = scaleValue;
		_areVerticesNormalized = false;
	}

	internal TextDrawObject GenerateMesh()
	{
		if (!_areVerticesNormalized)
		{
			_indices = new uint[_textMeshCharacterCount * 6];
			for (uint num = 0u; num < _textMeshCharacterCount; num++)
			{
				int num2 = (int)(6 * num);
				uint num3 = 4 * num;
				_indices[num2] = num3;
				_indices[num2 + 1] = 1 + num3;
				_indices[num2 + 2] = 2 + num3;
				_indices[num2 + 3] = num3;
				_indices[num2 + 4] = 2 + num3;
				_indices[num2 + 5] = 3 + num3;
			}
			_meshNormalizationInfo = GetNormalizedVertices();
			_drawRectangle.LocalScale = new Vector2(_meshNormalizationInfo.MeshWidth, _meshNormalizationInfo.MeshHeight);
			_areVerticesNormalized = true;
		}
		TextDrawObject result = TextDrawObject.Create(_meshNormalizationInfo.NormalizedVertices, _uvs, _indices, _meshNormalizationInfo.MeshWidth, _meshNormalizationInfo.MeshHeight, in _drawRectangle);
		result.ConvertToHashInPlace();
		return result;
	}

	private MeshNormalizationInfo GetNormalizedVertices()
	{
		if (_textMeshCharacterCount == 0)
		{
			return new MeshNormalizationInfo(0f, 0f, new float[0]);
		}
		int num = _textMeshCharacterCount * 8;
		float[] array = new float[num];
		float num2 = float.MaxValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		float num5 = float.MinValue;
		for (int i = 0; i < num; i++)
		{
			if (i % 2 == 0)
			{
				if (_vertices[i] < num2)
				{
					num2 = _vertices[i];
				}
				else if (_vertices[i] > num4)
				{
					num4 = _vertices[i];
				}
			}
			else if (i % 2 == 1)
			{
				if (_vertices[i] < num3)
				{
					num3 = _vertices[i];
				}
				else if (_vertices[i] > num5)
				{
					num5 = _vertices[i];
				}
			}
		}
		float num6 = num4 - num2;
		float num7 = num5 - num3;
		for (int j = 0; j < num; j++)
		{
			if (j % 2 == 0)
			{
				array[j] = _vertices[j] / num6;
			}
			else if (j % 2 == 1)
			{
				array[j] = _vertices[j] / num7;
			}
		}
		return new MeshNormalizationInfo(num6, num7, array);
	}

	internal void AddCharacterToMesh(float x, float y, BitmapFontCharacter fontCharacter)
	{
		float minU = _font.FontSprite.MinU;
		float minV = _font.FontSprite.MinV;
		float num = ((float?)_font.FontSprite.Texture?.Width) ?? 1f;
		float num2 = ((float?)_font.FontSprite.Texture?.Height) ?? 1f;
		float num3 = 1f / num;
		float num4 = 1f / num2;
		float num5 = minU + (float)fontCharacter.X * num3;
		float num6 = minV + (float)fontCharacter.Y * num4;
		float num7 = num5 + (float)fontCharacter.Width * num3;
		float num8 = num6 + (float)fontCharacter.Height * num4;
		float num9 = (float)fontCharacter.Width * _scaleValue;
		float num10 = (float)fontCharacter.Height * _scaleValue;
		_uvs[8 * _textMeshCharacterCount] = num5;
		_uvs[8 * _textMeshCharacterCount + 1] = num6;
		_uvs[8 * _textMeshCharacterCount + 2] = num7;
		_uvs[8 * _textMeshCharacterCount + 3] = num6;
		_uvs[8 * _textMeshCharacterCount + 4] = num7;
		_uvs[8 * _textMeshCharacterCount + 5] = num8;
		_uvs[8 * _textMeshCharacterCount + 6] = num5;
		_uvs[8 * _textMeshCharacterCount + 7] = num8;
		_vertices[8 * _textMeshCharacterCount] = x;
		_vertices[8 * _textMeshCharacterCount + 1] = y;
		_vertices[8 * _textMeshCharacterCount + 2] = x + num9;
		_vertices[8 * _textMeshCharacterCount + 3] = y;
		_vertices[8 * _textMeshCharacterCount + 4] = x + num9;
		_vertices[8 * _textMeshCharacterCount + 5] = y + num10;
		_vertices[8 * _textMeshCharacterCount + 6] = x;
		_vertices[8 * _textMeshCharacterCount + 7] = y + num10;
		_textMeshCharacterCount++;
		_areVerticesNormalized = false;
	}

	internal void AddValueToX(float value)
	{
		for (int i = 0; i < _vertices.Length; i += 2)
		{
			_vertices[i] += value;
		}
		_areVerticesNormalized = false;
	}

	internal void AddValueToY(float value)
	{
		for (int i = 0; i < _vertices.Length; i += 2)
		{
			_vertices[i + 1] += value;
		}
		_areVerticesNormalized = false;
	}
}
