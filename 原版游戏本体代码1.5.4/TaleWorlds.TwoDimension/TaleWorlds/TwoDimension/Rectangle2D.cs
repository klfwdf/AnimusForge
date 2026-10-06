using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public struct Rectangle2D
{
	private struct RectangleRenderProperties
	{
		public Vector2 ScaleMultiplier;

		public Vector2 PositionOffsetPixel;

		public float RotationOffset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static RectangleRenderProperties CreateEmpty()
		{
			return new RectangleRenderProperties
			{
				ScaleMultiplier = Vector2.One,
				PositionOffsetPixel = Vector2.Zero,
				RotationOffset = 0f
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void FillValuesFrom(RectangleRenderProperties other)
		{
			ScaleMultiplier = other.ScaleMultiplier;
			PositionOffsetPixel = other.PositionOffsetPixel;
			RotationOffset = other.RotationOffset;
		}
	}

	private static class RectangleHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixFrame CreateMatrixFrame(float posX, float posY, float pivotX, float pivotY, float scaleX, float scaleY, float rotation)
		{
			if (rotation == 0f)
			{
				return new MatrixFrame(scaleX, 0f, 0f, 0f, 0f, scaleY, 0f, 0f, 0f, 0f, 1f, 0f, posX, posY, 0f, 1f);
			}
			Mat3 rot = new Mat3(1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f);
			Vec3 o = new Vec3(posX, posY, 1f);
			Vec3 v = new Vec3(scaleX * (0f - pivotX), scaleY * (0f - pivotY));
			o -= rot.TransformToParent(in v);
			rot.RotateAboutUp(rotation * (System.MathF.PI / 180f));
			o += rot.TransformToParent(in v);
			rot.ApplyScaleLocal(new Vec3(scaleX, scaleY, 1f));
			MatrixFrame result = new MatrixFrame(in rot, in o);
			result.Fill();
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SimpleRectangle GetBoundingBox(in Rectangle2D rectangle)
		{
			if (!rectangle._hasRotation)
			{
				return new SimpleRectangle(rectangle.TopLeft.X, rectangle.TopLeft.Y, rectangle.BottomRight.X - rectangle.TopLeft.X, rectangle.BottomRight.Y - rectangle.TopLeft.Y);
			}
			Vector2 vector = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 vector2 = new Vector2(float.MinValue, float.MinValue);
			Vector2[] array = new Vector2[4] { rectangle.TopLeft, rectangle.TopRight, rectangle.BottomRight, rectangle.BottomLeft };
			for (int i = 0; i < array.Length; i++)
			{
				vector.X = Mathf.Min(array[i].X, vector.X);
				vector.Y = Mathf.Min(array[i].Y, vector.Y);
				vector2.X = Mathf.Max(array[i].X, vector2.X);
				vector2.Y = Mathf.Max(array[i].Y, vector2.Y);
			}
			return new SimpleRectangle(vector.X, vector.Y, vector2.X - vector.X, vector2.Y - vector.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetTwoDimensionalCrossProduct(in Vector2 p1, in Vector2 p2)
		{
			return p1.X * p2.Y - p1.Y * p2.X;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AreLinesIntersecting(in Vector2 line1Start, in Vector2 line1End, in Vector2 line2Start, in Vector2 line2End)
		{
			Vector2 vector = line1Start;
			Vector2 vector2 = line2Start;
			Vector2 p = line1End - line1Start;
			Vector2 p2 = line2End - line2Start;
			float twoDimensionalCrossProduct = GetTwoDimensionalCrossProduct(in p, in p2);
			float num = GetTwoDimensionalCrossProduct(vector2 - vector, in p2) / twoDimensionalCrossProduct;
			float num2 = GetTwoDimensionalCrossProduct(vector2 - vector, in p) / twoDimensionalCrossProduct;
			if (num >= 0f && num <= 1f && num2 >= 0f && num2 <= 1f)
			{
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DoRectanglesIntersect(in Rectangle2D rect1, in Rectangle2D rect2)
		{
			if (!rect1._boundingBox.IsCollide(rect2._boundingBox))
			{
				return false;
			}
			if (rect1.IsSubRectOf(in rect2) || rect2.IsSubRectOf(in rect1))
			{
				return true;
			}
			if (!rect1._hasRotation && !rect2._hasRotation)
			{
				return true;
			}
			Vector2[] array = new Vector2[4] { rect1.TopLeft, rect1.TopRight, rect1.BottomRight, rect1.BottomLeft };
			_ = new Vector2[4] { rect2.TopLeft, rect2.TopRight, rect2.BottomRight, rect2.BottomLeft };
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = i + 1; j < array.Length; j++)
				{
					Vector2 line1Start = array[i];
					Vector2 line1End = array[j];
					for (int k = 0; k < array.Length; k++)
					{
						for (int l = k + 1; l < array.Length; l++)
						{
							Vector2 line2Start = array[k];
							Vector2 line2End = array[l];
							if (AreLinesIntersecting(in line1Start, in line1End, in line2Start, in line2End))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointInside(in Vector2 point, in Rectangle2D rect)
		{
			if (!rect._boundingBox.IsPointInside(point))
			{
				return false;
			}
			if (!rect._hasRotation)
			{
				return true;
			}
			Vector2 vector = rect.TransformScreenPositionToLocal(in point);
			if (vector.X >= 0f && vector.X < rect.LocalScale.X && vector.Y >= 0f)
			{
				return vector.Y < rect.LocalScale.Y;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSubRectOf(in Rectangle2D rect1, in Rectangle2D rect2)
		{
			if (!rect1._boundingBox.IsSubRectOf(rect2._boundingBox))
			{
				return false;
			}
			if (!rect1._hasRotation && !rect2._hasRotation)
			{
				return true;
			}
			if (IsPointInside(in rect1.TopLeft, in rect2) && IsPointInside(in rect1.TopRight, in rect2) && IsPointInside(in rect1.BottomRight, in rect2))
			{
				return IsPointInside(in rect1.BottomLeft, in rect2);
			}
			return false;
		}
	}

	public bool IsValid;

	public Vector2 TopLeft;

	public Vector2 TopRight;

	public Vector2 BottomRight;

	public Vector2 BottomLeft;

	public Vector2 LocalPosition;

	public Vector2 LocalScale;

	public Vector2 LocalPivot;

	public float LocalRotation;

	private RectangleRenderProperties _renderProperties;

	private bool _hasDifferentVisuals;

	private bool _visualsNeedCalculation;

	private Vector2 _cachedOrigin;

	private MatrixFrame _cachedOrthonormalMatrix;

	private MatrixFrame _cachedMatrixFrame;

	private MatrixFrame _cachedVisualMatrixFrame;

	private SimpleRectangle _boundingBox;

	private bool _hasRotation;

	public static Rectangle2D Invalid => CreateInvalid();

	private static Rectangle2D CreateInvalid()
	{
		return new Rectangle2D
		{
			IsValid = false
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rectangle2D Create()
	{
		return new Rectangle2D
		{
			IsValid = true,
			LocalScale = new Vector2(10f, 10f),
			_renderProperties = RectangleRenderProperties.CreateEmpty(),
			_cachedOrthonormalMatrix = MatrixFrame.Identity,
			_cachedMatrixFrame = MatrixFrame.Identity,
			_cachedVisualMatrixFrame = MatrixFrame.Identity
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rectangle2D FillLocalValuesFrom(in Rectangle2D other)
	{
		LocalPosition = other.LocalPosition;
		LocalScale = other.LocalScale;
		LocalRotation = other.LocalRotation;
		LocalPivot = other.LocalPivot;
		_renderProperties.FillValuesFrom(other._renderProperties);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 GetVisualScale()
	{
		return new Vector2(LocalScale.X * _renderProperties.ScaleMultiplier.X, LocalScale.Y * _renderProperties.ScaleMultiplier.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddVisualOffset(float offsetX, float offsetY)
	{
		_renderProperties.PositionOffsetPixel += new Vector2(offsetX, offsetY);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVisualOffset(float offsetX, float offsetY)
	{
		_renderProperties.PositionOffsetPixel = new Vector2(offsetX, offsetY);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddVisualScale(float scaleX, float scaleY)
	{
		_renderProperties.ScaleMultiplier += new Vector2(scaleX, scaleY);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVisualScale(float scaleX, float scaleY)
	{
		_renderProperties.ScaleMultiplier = new Vector2(scaleX, scaleY);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddVisualRotationOffset(float rotationOffset)
	{
		_renderProperties.RotationOffset += rotationOffset;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVisualRotationOffset(float rotationOffset)
	{
		_renderProperties.RotationOffset = rotationOffset;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ValidateVisuals()
	{
		_hasDifferentVisuals = _renderProperties.PositionOffsetPixel.X != 0f || _renderProperties.PositionOffsetPixel.Y != 0f || _renderProperties.ScaleMultiplier.X != 1f || _renderProperties.ScaleMultiplier.Y != 1f || _renderProperties.RotationOffset != 0f;
	}

	public void DrawBoundingBox()
	{
	}

	public void DrawCorners()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CalculateMatrixFrame(in Rectangle2D parentRectangle)
	{
		if (LocalScale == Vector2.Zero)
		{
			LocalScale = Vector2.One;
		}
		_hasRotation = LocalRotation != 0f || _renderProperties.RotationOffset != 0f;
		ValidateVisuals();
		_cachedMatrixFrame = RectangleHelper.CreateMatrixFrame(LocalPosition.X, LocalPosition.Y, LocalPivot.X, LocalPivot.Y, LocalScale.X, LocalScale.Y, LocalRotation);
		if (parentRectangle.IsValid)
		{
			_hasRotation = _hasRotation || parentRectangle._hasRotation;
			if (_hasRotation)
			{
				_cachedMatrixFrame = parentRectangle._cachedOrthonormalMatrix.TransformToParent(in _cachedMatrixFrame);
			}
			else
			{
				_cachedMatrixFrame.origin += parentRectangle._cachedOrthonormalMatrix.origin;
			}
		}
		_cachedMatrixFrame.Fill();
		_cachedOrigin = new Vector2(_cachedMatrixFrame.origin.x, _cachedMatrixFrame.origin.y);
		if (_hasRotation)
		{
			_cachedOrthonormalMatrix = _cachedMatrixFrame;
			_cachedOrthonormalMatrix.rotation.f.Normalize();
			_cachedOrthonormalMatrix.rotation.s = Vec3.CrossProduct(_cachedOrthonormalMatrix.rotation.f, _cachedOrthonormalMatrix.rotation.u);
			_cachedOrthonormalMatrix.rotation.s.Normalize();
			_cachedOrthonormalMatrix.rotation.u = Vec3.CrossProduct(_cachedOrthonormalMatrix.rotation.s, _cachedOrthonormalMatrix.rotation.f);
		}
		else
		{
			_cachedOrthonormalMatrix = MatrixFrame.Identity;
			_cachedOrthonormalMatrix.origin = _cachedMatrixFrame.origin;
		}
		TopLeft = _cachedOrigin;
		TopRight = _cachedOrigin + new Vector2(_cachedMatrixFrame.rotation.s.x, _cachedMatrixFrame.rotation.s.y);
		BottomRight = _cachedOrigin + new Vector2(_cachedMatrixFrame.rotation.s.x, _cachedMatrixFrame.rotation.s.y) + new Vector2(_cachedMatrixFrame.rotation.f.x, _cachedMatrixFrame.rotation.f.y);
		BottomLeft = _cachedOrigin + new Vector2(_cachedMatrixFrame.rotation.f.x, _cachedMatrixFrame.rotation.f.y);
		_boundingBox = RectangleHelper.GetBoundingBox(in this);
		_visualsNeedCalculation = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CalculateVisualMatrixFrame()
	{
		if (!_visualsNeedCalculation)
		{
			return;
		}
		_cachedVisualMatrixFrame = _cachedMatrixFrame;
		if (!_hasDifferentVisuals)
		{
			_visualsNeedCalculation = false;
			return;
		}
		Vec3 origin = _cachedVisualMatrixFrame.origin;
		Mat3 rotation = _cachedOrthonormalMatrix.rotation;
		Vector2 localScale = LocalScale;
		Vector2 vector = new Vector2(0.5f, 0.5f);
		Vec3 v = new Vec3(localScale.X * (0f - vector.X), localScale.Y * (0f - vector.Y));
		origin -= rotation.TransformToParent(in v);
		rotation.RotateAboutUp(_renderProperties.RotationOffset * (System.MathF.PI / 180f));
		origin += rotation.TransformToParent(in v);
		rotation.ApplyScaleLocal(new Vec3(LocalScale.X, LocalScale.Y, 1f));
		_cachedVisualMatrixFrame.origin = origin;
		_cachedVisualMatrixFrame.rotation = rotation;
		_cachedVisualMatrixFrame.rotation.ApplyScaleLocal(new Vec3(_renderProperties.ScaleMultiplier.X, _renderProperties.ScaleMultiplier.Y, 1f));
		if (_renderProperties.ScaleMultiplier.X < 0f)
		{
			_renderProperties.PositionOffsetPixel.X -= _renderProperties.ScaleMultiplier.X * LocalScale.X;
		}
		if (_renderProperties.ScaleMultiplier.Y < 0f)
		{
			_renderProperties.PositionOffsetPixel.Y -= _renderProperties.ScaleMultiplier.Y * LocalScale.Y;
		}
		if (_renderProperties.PositionOffsetPixel.X != 0f || _renderProperties.PositionOffsetPixel.Y != 0f)
		{
			_cachedVisualMatrixFrame.origin += _cachedOrthonormalMatrix.rotation.TransformToParent(new Vec3(_renderProperties.PositionOffsetPixel.X, _renderProperties.PositionOffsetPixel.Y));
		}
		_visualsNeedCalculation = false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 GetCachedOrigin()
	{
		return _cachedOrigin;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MatrixFrame GetCachedMatrixFrame()
	{
		return _cachedMatrixFrame;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MatrixFrame GetCachedVisualMatrixFrame()
	{
		return _cachedVisualMatrixFrame;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 GetCenter()
	{
		return _boundingBox.GetCenter();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SimpleRectangle GetBoundingBox()
	{
		return _boundingBox;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIdentical(in Rectangle2D other)
	{
		if (_cachedMatrixFrame == other._cachedMatrixFrame)
		{
			return _cachedVisualMatrixFrame == other._cachedVisualMatrixFrame;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsCollide(in Rectangle2D other)
	{
		return RectangleHelper.DoRectanglesIntersect(in this, in other);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsSubRectOf(in Rectangle2D other)
	{
		return RectangleHelper.IsSubRectOf(in this, in other);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPointInside(in Vector2 point)
	{
		return RectangleHelper.IsPointInside(in point, in this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 TransformScreenPositionToLocal(in Vector2 screenPosition)
	{
		Vec3 vec = _cachedOrthonormalMatrix.TransformToLocal(new Vec3(screenPosition.X, screenPosition.Y));
		return new Vector2(vec.x, vec.y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 TransformLocalPositionToScreen(in Vector2 localPosition)
	{
		Vec3 vec = _cachedOrthonormalMatrix.TransformToParent(new Vec3(localPosition.X, localPosition.Y));
		return new Vector2(vec.x, vec.y);
	}
}
