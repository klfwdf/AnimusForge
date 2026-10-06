using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class TwoDimensionDrawContext
{
	private List<ScissorTestInfo> _scissorStack;

	private bool _scissorTestEnabled;

	private bool _circularMaskEnabled;

	private float _circularMaskRadius;

	private float _circularMaskSmoothingRadius;

	private Vector2 _circularMaskCenter;

	private List<TwoDimensionDrawData> _drawData;

	private MaterialPool<SimpleMaterial> _simpleMaterialPool = new MaterialPool<SimpleMaterial>(8);

	private MaterialPool<TextMaterial> _textMaterialPool = new MaterialPool<TextMaterial>(8);

	public bool ScissorTestEnabled => _scissorTestEnabled;

	public bool CircularMaskEnabled => _circularMaskEnabled;

	public Vector2 CircularMaskCenter => _circularMaskCenter;

	public float CircularMaskRadius => _circularMaskRadius;

	public float CircularMaskSmoothingRadius => _circularMaskSmoothingRadius;

	public ScissorTestInfo CurrentScissor => _scissorStack[_scissorStack.Count - 1];

	public TwoDimensionDrawContext()
	{
		_scissorStack = new List<ScissorTestInfo>();
		_scissorTestEnabled = false;
		_drawData = new List<TwoDimensionDrawData>();
	}

	public void Reset()
	{
		_scissorStack.Clear();
		_scissorTestEnabled = false;
		_drawData.Clear();
		_simpleMaterialPool.ResetAll();
		_textMaterialPool.ResetAll();
	}

	public SimpleMaterial CreateSimpleMaterial()
	{
		SimpleMaterial simpleMaterial = _simpleMaterialPool.New();
		simpleMaterial.Texture = null;
		return simpleMaterial;
	}

	public TextMaterial CreateTextMaterial()
	{
		TextMaterial textMaterial = _textMaterialPool.New();
		textMaterial.Texture = null;
		return textMaterial;
	}

	public void PushScissor(in Rectangle2D newScissorRectangle)
	{
		SimpleRectangle boundingBox = newScissorRectangle.GetBoundingBox();
		ScissorTestInfo item = new ScissorTestInfo(boundingBox.X, boundingBox.Y, boundingBox.X2, boundingBox.Y2);
		if (_scissorStack.Count > 0)
		{
			item.ReduceToIntersection(_scissorStack[_scissorStack.Count - 1]);
		}
		_scissorStack.Add(item);
		_scissorTestEnabled = true;
	}

	public void PopScissor()
	{
		_scissorStack.RemoveAt(_scissorStack.Count - 1);
		if (_scissorTestEnabled && _scissorStack.Count == 0)
		{
			_scissorTestEnabled = false;
		}
	}

	public bool IsDiscardedByAnyScissor(in Rectangle2D rect)
	{
		for (int i = 0; i < _scissorStack.Count; i++)
		{
			if (!_scissorStack[i].IsCollide(in rect))
			{
				return true;
			}
		}
		return false;
	}

	public void SetCircualMask(Vector2 position, float radius, float smoothingRadius)
	{
		_circularMaskEnabled = true;
		_circularMaskCenter = position;
		_circularMaskRadius = radius;
		_circularMaskSmoothingRadius = smoothingRadius;
	}

	public void ClearCircualMask()
	{
		_circularMaskEnabled = false;
	}

	private void UpdateVisualMatricesAux(int startIndexInclusive, int endIndexInclusive)
	{
		for (int i = startIndexInclusive; i < endIndexInclusive; i++)
		{
			TwoDimensionDrawData value = _drawData[i];
			value.UpdateVisualRect();
			_drawData[i] = value;
		}
	}

	public void DrawTo(TwoDimensionContext twoDimensionContext)
	{
		if (_drawData.Count > 32)
		{
			TWParallel.ForWithoutRenderThread(0, _drawData.Count, UpdateVisualMatricesAux);
		}
		else
		{
			UpdateVisualMatricesAux(0, _drawData.Count);
		}
		for (int i = 0; i < _drawData.Count; i++)
		{
			_drawData[i].DrawTo(twoDimensionContext, i);
		}
	}

	public void DrawSprite(Sprite sprite, SimpleMaterial material, in Rectangle2D rectangle, float scale)
	{
		ImageDrawObject drawObject = ImageDrawObject.Create(in rectangle, sprite.GetMinUvs(), sprite.GetMaxUvs());
		drawObject.Scale = scale;
		material.Texture = sprite.Texture;
		if (_circularMaskEnabled)
		{
			material.CircularMaskingEnabled = true;
			material.CircularMaskingCenter = _circularMaskCenter;
			material.CircularMaskingRadius = _circularMaskRadius;
			material.CircularMaskingSmoothingRadius = _circularMaskSmoothingRadius;
		}
		Draw(material, in drawObject);
	}

	public void Draw(SimpleMaterial material, in ImageDrawObject drawObject)
	{
		ScissorTestInfo scissorTestInfo = ((_scissorStack.Count > 0) ? _scissorStack[_scissorStack.Count - 1] : default(ScissorTestInfo));
		TwoDimensionDrawData item = new TwoDimensionDrawData(_scissorTestEnabled, in scissorTestInfo, material, in drawObject);
		_drawData.Add(item);
	}

	public void Draw(TextMaterial material, in TextDrawObject drawObject)
	{
		ScissorTestInfo scissorTestInfo = ((_scissorStack.Count > 0) ? _scissorStack[_scissorStack.Count - 1] : default(ScissorTestInfo));
		TwoDimensionDrawData item = new TwoDimensionDrawData(_scissorTestEnabled, in scissorTestInfo, material, in drawObject);
		_drawData.Add(item);
	}

	public void Draw(Text text, TextMaterial materialOriginal, in Rectangle2D parentRectangle, in Rectangle2D rectangle)
	{
		text.UpdateSize((int)rectangle.LocalScale.X, (int)rectangle.LocalScale.Y);
		foreach (TextPart part in text.GetParts())
		{
			TextDrawObject drawObject = part.DrawObject2D;
			TextMaterial textMaterial = CreateTextMaterial();
			textMaterial.CopyFrom(materialOriginal);
			if (drawObject.IsValid)
			{
				drawObject.Rectangle.FillLocalValuesFrom(in rectangle);
				drawObject.Rectangle.LocalScale = new Vector2(drawObject.Text_MeshWidth, drawObject.Text_MeshHeight);
				drawObject.Rectangle.CalculateMatrixFrame(in parentRectangle);
				textMaterial.Texture = part.DefaultFont.FontSprite.Texture;
				textMaterial.ScaleFactor = part.DefaultFont.Size;
				textMaterial.SmoothingConstant = part.DefaultFont.SmoothingConstant;
				textMaterial.Smooth = part.DefaultFont.Smooth;
				if (textMaterial.GlowRadius > 0f || textMaterial.Blur > 0f || textMaterial.OutlineAmount > 0f)
				{
					TextMaterial textMaterial2 = CreateTextMaterial();
					textMaterial2.CopyFrom(textMaterial);
					Draw(textMaterial2, in drawObject);
				}
				textMaterial.GlowRadius = 0f;
				textMaterial.Blur = 0f;
				textMaterial.OutlineAmount = 0f;
				Draw(textMaterial, in drawObject);
			}
		}
	}
}
