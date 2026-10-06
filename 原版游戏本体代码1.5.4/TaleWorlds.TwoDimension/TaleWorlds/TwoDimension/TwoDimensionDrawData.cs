namespace TaleWorlds.TwoDimension;

internal struct TwoDimensionDrawData
{
	private bool _scissorTestEnabled;

	private ScissorTestInfo _scissorTestInfo;

	private SimpleMaterial _imageMaterial;

	private ImageDrawObject _imageDrawObject;

	private TextMaterial _textMaterial;

	private TextDrawObject _textDrawObject;

	public TwoDimensionDrawData(bool scissorTestEnabled, in ScissorTestInfo scissorTestInfo, SimpleMaterial imageMaterial, in ImageDrawObject imageDrawObject)
	{
		_scissorTestEnabled = scissorTestEnabled;
		_scissorTestInfo = scissorTestInfo;
		_imageMaterial = imageMaterial;
		_imageDrawObject = imageDrawObject;
		_textMaterial = null;
		_textDrawObject = TextDrawObject.Invalid;
	}

	public TwoDimensionDrawData(bool scissorTestEnabled, in ScissorTestInfo scissorTestInfo, TextMaterial textMaterial, in TextDrawObject textDrawObject)
	{
		_scissorTestEnabled = scissorTestEnabled;
		_scissorTestInfo = scissorTestInfo;
		_imageMaterial = null;
		_imageDrawObject = ImageDrawObject.Invalid;
		_textMaterial = textMaterial;
		_textDrawObject = textDrawObject;
	}

	public void DrawTo(TwoDimensionContext twoDimensionContext, int layer)
	{
		if (_scissorTestEnabled)
		{
			twoDimensionContext.SetScissor(_scissorTestInfo);
		}
		if (_imageDrawObject.IsValid && _imageMaterial != null)
		{
			twoDimensionContext.DrawImage(_imageMaterial, in _imageDrawObject, layer);
		}
		else if (_textDrawObject.IsValid && _textMaterial != null)
		{
			twoDimensionContext.DrawText(_textMaterial, in _textDrawObject, layer);
		}
		if (_scissorTestEnabled)
		{
			twoDimensionContext.ResetScissor();
		}
	}

	public void UpdateVisualRect()
	{
		if (_imageDrawObject.IsValid)
		{
			_imageDrawObject.Rectangle.CalculateVisualMatrixFrame();
		}
		else if (_textDrawObject.IsValid)
		{
			_textDrawObject.Rectangle.CalculateVisualMatrixFrame();
		}
	}
}
