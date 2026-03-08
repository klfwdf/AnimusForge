using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000017 RID: 23
	public class BrushRenderer
	{
		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000176 RID: 374 RVA: 0x00007C4B File Offset: 0x00005E4B
		private float _brushTimer
		{
			get
			{
				if (!this.UseLocalTimer)
				{
					return this._globalTime;
				}
				return this._brushLocalTimer;
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000177 RID: 375 RVA: 0x00007C62 File Offset: 0x00005E62
		// (set) Token: 0x06000178 RID: 376 RVA: 0x00007C6A File Offset: 0x00005E6A
		public ulong LastUpdatedFrameNumber { get; private set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000179 RID: 377 RVA: 0x00007C73 File Offset: 0x00005E73
		// (set) Token: 0x0600017A RID: 378 RVA: 0x00007C7B File Offset: 0x00005E7B
		public bool ForcePixelPerfectPlacement { get; set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x0600017B RID: 379 RVA: 0x00007C84 File Offset: 0x00005E84
		public Style CurrentStyle
		{
			get
			{
				return this._styleOfCurrentState;
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600017C RID: 380 RVA: 0x00007C8C File Offset: 0x00005E8C
		// (set) Token: 0x0600017D RID: 381 RVA: 0x00007C94 File Offset: 0x00005E94
		public Brush Brush
		{
			get
			{
				return this._brush;
			}
			set
			{
				if (this._brush != value)
				{
					this._brush = value;
					this._brushLocalTimer = 0f;
					int capacity = ((this._brush != null) ? this._brush.Layers.Count : 0);
					if (this._startBrushLayerState == null)
					{
						this._startBrushLayerState = new Dictionary<string, BrushLayerState>(capacity);
						this._currentBrushLayerState = new Dictionary<string, BrushLayerState>(capacity);
					}
					else
					{
						this._startBrushLayerState.Clear();
						this._currentBrushLayerState.Clear();
					}
					if (this._brush != null)
					{
						this._styleOfCurrentState = this._brush.DefaultStyle;
						if (!string.IsNullOrEmpty(this.CurrentState))
						{
							this._styleOfCurrentState = this._brush.GetStyleOrDefault(this.CurrentState);
						}
						BrushState brushState = default(BrushState);
						brushState.FillFrom(this._styleOfCurrentState);
						this._startBrushState = brushState;
						this._currentBrushState = brushState;
						foreach (StyleLayer styleLayer in this._styleOfCurrentState.GetLayers())
						{
							BrushLayerState value2 = default(BrushLayerState);
							value2.FillFrom(styleLayer);
							this._startBrushLayerState[styleLayer.Name] = value2;
							this._currentBrushLayerState[styleLayer.Name] = value2;
						}
					}
				}
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600017E RID: 382 RVA: 0x00007DCE File Offset: 0x00005FCE
		// (set) Token: 0x0600017F RID: 383 RVA: 0x00007DD8 File Offset: 0x00005FD8
		public string CurrentState
		{
			get
			{
				return this._currentState;
			}
			set
			{
				if (this._currentState != value)
				{
					string currentState = this._currentState;
					this._brushLocalTimer = 0f;
					this._currentState = value;
					this._startBrushState = this._currentBrushState;
					foreach (KeyValuePair<string, BrushLayerState> keyValuePair in this._currentBrushLayerState)
					{
						this._startBrushLayerState[keyValuePair.Key] = keyValuePair.Value;
					}
					if (this.Brush != null)
					{
						Style styleOrDefault = this.Brush.GetStyleOrDefault(this.CurrentState);
						this._styleOfCurrentState = styleOrDefault;
						this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.None;
						if (styleOrDefault.AnimationMode == StyleAnimationMode.BasicTransition)
						{
							if (!string.IsNullOrEmpty(currentState))
							{
								this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.PlayingBasicTranisition;
								return;
							}
						}
						else if (styleOrDefault.AnimationMode == StyleAnimationMode.Animation && (!string.IsNullOrEmpty(currentState) || !string.IsNullOrEmpty(styleOrDefault.AnimationToPlayOnBegin)))
						{
							this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.PlayingAnimation;
						}
					}
				}
			}
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00007EDC File Offset: 0x000060DC
		public BrushRenderer()
		{
			this._startBrushState = default(BrushState);
			this._currentBrushState = default(BrushState);
			this._startBrushLayerState = new Dictionary<string, BrushLayerState>();
			this._currentBrushLayerState = new Dictionary<string, BrushLayerState>();
			this._brushLocalTimer = 0f;
			this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.None;
			this._cachedImageFit = new ImageFit();
			this._randomXOffset = -1f;
			this._randomYOffset = -1f;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00007F50 File Offset: 0x00006150
		private float GetRandomXOffset()
		{
			if (this._randomXOffset < 0f)
			{
				Random random = new Random(this._offsetSeed);
				this._randomXOffset = (float)random.Next(0, 2048);
				this._randomYOffset = (float)random.Next(0, 2048);
			}
			return this._randomXOffset;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00007FA4 File Offset: 0x000061A4
		private float GetRandomYOffset()
		{
			if (this._randomYOffset < 0f)
			{
				Random random = new Random(this._offsetSeed);
				this._randomXOffset = (float)random.Next(0, 2048);
				this._randomYOffset = (float)random.Next(0, 2048);
			}
			return this._randomYOffset;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x00007FF8 File Offset: 0x000061F8
		public void Update(ulong frameNumber, float globalAnimTime, float dt)
		{
			this._globalTime = globalAnimTime;
			this.LastUpdatedFrameNumber = frameNumber;
			this._brushLocalTimer += dt;
			if (this.Brush != null)
			{
				Style styleOfCurrentState = this._styleOfCurrentState;
				if ((this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.None || this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.Ended) && (!string.IsNullOrEmpty(styleOfCurrentState.AnimationToPlayOnBegin) || this._styleOfCurrentState.Version != this._latestStyleVersion))
				{
					this._latestStyleVersion = styleOfCurrentState.Version;
					BrushState brushState = default(BrushState);
					brushState.FillFrom(styleOfCurrentState);
					this._startBrushState = brushState;
					this._currentBrushState = brushState;
					foreach (StyleLayer styleLayer in styleOfCurrentState.GetLayers())
					{
						BrushLayerState value = default(BrushLayerState);
						value.FillFrom(styleLayer);
						this._currentBrushLayerState[styleLayer.Name] = value;
						this._startBrushLayerState[styleLayer.Name] = value;
					}
					return;
				}
				if (this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.PlayingBasicTranisition)
				{
					float num = (this.UseLocalTimer ? this._brushLocalTimer : globalAnimTime);
					if (num >= this.Brush.TransitionDuration)
					{
						this.EndAnimation();
						return;
					}
					float num2 = num / this.Brush.TransitionDuration;
					if (num2 > 1f)
					{
						num2 = 1f;
					}
					BrushState startBrushState = this._startBrushState;
					BrushState currentBrushState = default(BrushState);
					currentBrushState.LerpFrom(startBrushState, styleOfCurrentState, num2);
					this._currentBrushState = currentBrushState;
					foreach (StyleLayer styleLayer2 in styleOfCurrentState.GetLayers())
					{
						BrushLayerState start = this._startBrushLayerState[styleLayer2.Name];
						BrushLayerState value2 = default(BrushLayerState);
						value2.LerpFrom(start, styleLayer2, num2);
						this._currentBrushLayerState[styleLayer2.Name] = value2;
					}
					return;
				}
				else if (this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.PlayingAnimation)
				{
					string animationToPlayOnBegin = styleOfCurrentState.AnimationToPlayOnBegin;
					BrushAnimation animation = this.Brush.GetAnimation(animationToPlayOnBegin);
					if (animation == null || (!animation.Loop && this._brushTimer >= animation.Duration))
					{
						this.EndAnimation();
						return;
					}
					float brushStateTimer = this._brushTimer % animation.Duration;
					bool isFirstCycle = this._brushTimer < animation.Duration;
					BrushState startBrushState2 = this._startBrushState;
					BrushLayerAnimation styleAnimation = animation.StyleAnimation;
					BrushState currentBrushState2 = this.AnimateBrushState(animation, styleAnimation, brushStateTimer, isFirstCycle, startBrushState2, styleOfCurrentState);
					this._currentBrushState = currentBrushState2;
					foreach (StyleLayer styleLayer3 in styleOfCurrentState.GetLayers())
					{
						BrushLayerState startState = this._startBrushLayerState[styleLayer3.Name];
						BrushLayerAnimation layerAnimation = animation.GetLayerAnimation(styleLayer3.Name);
						BrushLayerState value3 = this.AnimateBrushLayerState(animation, layerAnimation, brushStateTimer, isFirstCycle, startState, styleLayer3);
						this._currentBrushLayerState[styleLayer3.Name] = value3;
					}
				}
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x000082B0 File Offset: 0x000064B0
		private BrushLayerState AnimateBrushLayerState(BrushAnimation animation, BrushLayerAnimation layerAnimation, float brushStateTimer, bool isFirstCycle, BrushLayerState startState, IBrushLayerData source)
		{
			BrushLayerState result = default(BrushLayerState);
			result.FillFrom(source);
			if (layerAnimation != null)
			{
				foreach (BrushAnimationProperty brushAnimationProperty in layerAnimation.Collections)
				{
					BrushAnimationProperty.BrushAnimationPropertyType propertyType = brushAnimationProperty.PropertyType;
					BrushAnimationKeyFrame brushAnimationKeyFrame = null;
					BrushAnimationKeyFrame brushAnimationKeyFrame2;
					if (animation.Loop)
					{
						BrushAnimationKeyFrame frameAt = brushAnimationProperty.GetFrameAt(0);
						if (isFirstCycle && this._brushTimer < frameAt.Time)
						{
							brushAnimationKeyFrame2 = frameAt;
						}
						else
						{
							brushAnimationKeyFrame2 = brushAnimationProperty.GetFrameAfter(brushStateTimer);
							if (brushAnimationKeyFrame2 == null)
							{
								brushAnimationKeyFrame2 = frameAt;
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1);
							}
							else if (brushAnimationKeyFrame2 == frameAt)
							{
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1);
							}
							else
							{
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationKeyFrame2.Index - 1);
							}
						}
					}
					else
					{
						brushAnimationKeyFrame2 = brushAnimationProperty.GetFrameAfter(brushStateTimer);
						if (brushAnimationKeyFrame2 != null)
						{
							brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationKeyFrame2.Index - 1);
						}
						else
						{
							brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1);
						}
					}
					BrushAnimationKeyFrame brushAnimationKeyFrame3 = null;
					BrushLayerState brushLayerState = default(BrushLayerState);
					IBrushLayerData brushLayerData = null;
					BrushAnimationKeyFrame brushAnimationKeyFrame4 = null;
					float num3;
					if (brushAnimationKeyFrame2 != null)
					{
						if (brushAnimationKeyFrame != null)
						{
							float num;
							float num2;
							if (animation.Loop)
							{
								if (brushAnimationKeyFrame2.Index == 0)
								{
									num = brushAnimationKeyFrame2.Time + (animation.Duration - brushAnimationKeyFrame.Time);
									if (brushStateTimer >= brushAnimationKeyFrame.Time)
									{
										num2 = brushStateTimer - brushAnimationKeyFrame.Time;
									}
									else
									{
										num2 = animation.Duration - brushAnimationKeyFrame.Time + brushStateTimer;
									}
								}
								else
								{
									num = brushAnimationKeyFrame2.Time - brushAnimationKeyFrame.Time;
									num2 = brushStateTimer - brushAnimationKeyFrame.Time;
								}
							}
							else
							{
								num = brushAnimationKeyFrame2.Time - brushAnimationKeyFrame.Time;
								num2 = brushStateTimer - brushAnimationKeyFrame.Time;
							}
							num3 = num2 * (1f / num);
							brushAnimationKeyFrame3 = brushAnimationKeyFrame;
							brushAnimationKeyFrame4 = brushAnimationKeyFrame2;
						}
						else
						{
							num3 = brushStateTimer * (1f / brushAnimationKeyFrame2.Time);
							brushLayerState = startState;
							brushAnimationKeyFrame4 = brushAnimationKeyFrame2;
						}
					}
					else
					{
						num3 = (brushStateTimer - brushAnimationKeyFrame.Time) * (1f / (animation.Duration - brushAnimationKeyFrame.Time));
						brushAnimationKeyFrame3 = brushAnimationKeyFrame;
						brushLayerData = source;
					}
					num3 = AnimationInterpolation.Ease(animation.InterpolationType, animation.InterpolationFunction, num3);
					switch (propertyType)
					{
					case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
					{
						float valueFrom = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsFloat() : brushLayerState.GetValueAsFloat(propertyType));
						float valueTo = ((brushLayerData != null) ? brushLayerData.GetValueAsFloat(propertyType) : brushAnimationKeyFrame4.GetValueAsFloat());
						result.SetValueAsFloat(propertyType, MathF.Lerp(valueFrom, valueTo, num3, 1E-05f));
						break;
					}
					case BrushAnimationProperty.BrushAnimationPropertyType.Color:
					case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
					{
						Color start = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsColor() : brushLayerState.GetValueAsColor(propertyType));
						Color end = ((brushLayerData != null) ? brushLayerData.GetValueAsColor(propertyType) : brushAnimationKeyFrame4.GetValueAsColor());
						BrushAnimationProperty.BrushAnimationPropertyType propertyType2 = propertyType;
						Color color = Color.Lerp(start, end, num3);
						result.SetValueAsColor(propertyType2, color);
						break;
					}
					case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite:
					{
						Sprite sprite = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsSprite() : null) ?? brushLayerState.GetValueAsSprite(propertyType);
						Sprite sprite2 = ((brushLayerData != null) ? brushLayerData.GetValueAsSprite(propertyType) : null) ?? brushAnimationKeyFrame4.GetValueAsSprite();
						result.SetValueAsSprite(propertyType, ((double)num3 <= 0.9) ? sprite : sprite2);
						break;
					}
					}
				}
			}
			return result;
		}

		// Token: 0x06000185 RID: 389 RVA: 0x000086A0 File Offset: 0x000068A0
		public bool IsUpdateNeeded()
		{
			return this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.PlayingBasicTranisition || this._brushRendererAnimationState == BrushRenderer.BrushRendererAnimationState.PlayingAnimation || (this._styleOfCurrentState != null && this._styleOfCurrentState.Version != this._latestStyleVersion);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x000086D8 File Offset: 0x000068D8
		private BrushState AnimateBrushState(BrushAnimation animation, BrushLayerAnimation layerAnimation, float brushStateTimer, bool isFirstCycle, BrushState startState, Style source)
		{
			BrushState result = default(BrushState);
			result.FillFrom(source);
			if (layerAnimation != null)
			{
				foreach (BrushAnimationProperty brushAnimationProperty in layerAnimation.Collections)
				{
					BrushAnimationProperty.BrushAnimationPropertyType propertyType = brushAnimationProperty.PropertyType;
					BrushAnimationKeyFrame brushAnimationKeyFrame = null;
					BrushAnimationKeyFrame brushAnimationKeyFrame2;
					if (animation.Loop)
					{
						BrushAnimationKeyFrame frameAt = brushAnimationProperty.GetFrameAt(0);
						if (isFirstCycle && this._brushTimer < frameAt.Time)
						{
							brushAnimationKeyFrame2 = frameAt;
						}
						else
						{
							brushAnimationKeyFrame2 = brushAnimationProperty.GetFrameAfter(brushStateTimer);
							if (brushAnimationKeyFrame2 == null)
							{
								brushAnimationKeyFrame2 = frameAt;
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1);
							}
							else if (brushAnimationKeyFrame2 == frameAt)
							{
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1);
							}
							else
							{
								brushAnimationKeyFrame = brushAnimationProperty.GetFrameAt(brushAnimationKeyFrame2.Index - 1);
							}
						}
					}
					else
					{
						brushAnimationKeyFrame2 = brushAnimationProperty.GetFrameAfter(brushStateTimer);
						brushAnimationKeyFrame = ((brushAnimationKeyFrame2 != null) ? brushAnimationProperty.GetFrameAt(brushAnimationKeyFrame2.Index - 1) : brushAnimationProperty.GetFrameAt(brushAnimationProperty.Count - 1));
					}
					BrushAnimationKeyFrame brushAnimationKeyFrame3 = null;
					BrushState brushState = default(BrushState);
					Style style = null;
					BrushAnimationKeyFrame brushAnimationKeyFrame4 = null;
					float num3;
					if (brushAnimationKeyFrame2 != null)
					{
						if (brushAnimationKeyFrame != null)
						{
							float num;
							float num2;
							if (animation.Loop)
							{
								if (brushAnimationKeyFrame2.Index == 0)
								{
									num = brushAnimationKeyFrame2.Time + (animation.Duration - brushAnimationKeyFrame.Time);
									if (brushStateTimer >= brushAnimationKeyFrame.Time)
									{
										num2 = brushStateTimer - brushAnimationKeyFrame.Time;
									}
									else
									{
										num2 = animation.Duration - brushAnimationKeyFrame.Time + brushStateTimer;
									}
								}
								else
								{
									num = brushAnimationKeyFrame2.Time - brushAnimationKeyFrame.Time;
									num2 = brushStateTimer - brushAnimationKeyFrame.Time;
								}
							}
							else
							{
								num = brushAnimationKeyFrame2.Time - brushAnimationKeyFrame.Time;
								num2 = brushStateTimer - brushAnimationKeyFrame.Time;
							}
							num3 = num2 * (1f / num);
							brushAnimationKeyFrame3 = brushAnimationKeyFrame;
							brushAnimationKeyFrame4 = brushAnimationKeyFrame2;
						}
						else
						{
							num3 = brushStateTimer * (1f / brushAnimationKeyFrame2.Time);
							brushState = startState;
							brushAnimationKeyFrame4 = brushAnimationKeyFrame2;
						}
					}
					else
					{
						num3 = (brushStateTimer - brushAnimationKeyFrame.Time) * (1f / (animation.Duration - brushAnimationKeyFrame.Time));
						brushAnimationKeyFrame3 = brushAnimationKeyFrame;
						style = source;
					}
					num3 = MathF.Clamp(num3, 0f, 1f);
					num3 = AnimationInterpolation.Ease(animation.InterpolationType, animation.InterpolationFunction, num3);
					switch (propertyType)
					{
					case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
					case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
					case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
					{
						float valueFrom = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsFloat() : brushState.GetValueAsFloat(propertyType));
						float valueTo = ((style != null) ? style.GetValueAsFloat(propertyType) : brushAnimationKeyFrame4.GetValueAsFloat());
						result.SetValueAsFloat(propertyType, MathF.Lerp(valueFrom, valueTo, num3, 1E-05f));
						break;
					}
					case BrushAnimationProperty.BrushAnimationPropertyType.Color:
					case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
					case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
					{
						Color start = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsColor() : brushState.GetValueAsColor(propertyType));
						Color end = ((style != null) ? style.GetValueAsColor(propertyType) : brushAnimationKeyFrame4.GetValueAsColor());
						BrushAnimationProperty.BrushAnimationPropertyType propertyType2 = propertyType;
						Color color = Color.Lerp(start, end, num3);
						result.SetValueAsColor(propertyType2, color);
						break;
					}
					case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
					case BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite:
					{
						Sprite sprite = ((brushAnimationKeyFrame3 != null) ? brushAnimationKeyFrame3.GetValueAsSprite() : null) ?? brushState.GetValueAsSprite(propertyType);
						Sprite sprite2 = ((style != null) ? style.GetValueAsSprite(propertyType) : null) ?? brushAnimationKeyFrame4.GetValueAsSprite();
						result.SetValueAsSprite(propertyType, ((double)num3 <= 0.9) ? sprite : sprite2);
						break;
					}
					}
				}
			}
			return result;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x00008AD0 File Offset: 0x00006CD0
		private void EndAnimation()
		{
			if (this.Brush != null)
			{
				Style styleOfCurrentState = this._styleOfCurrentState;
				BrushState brushState = default(BrushState);
				brushState.FillFrom(styleOfCurrentState);
				this._startBrushState = brushState;
				this._currentBrushState = brushState;
				if (this.Brush.TransitionDuration == 0f)
				{
					this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.None;
				}
				foreach (StyleLayer styleLayer in styleOfCurrentState.GetLayers())
				{
					BrushLayerState value = default(BrushLayerState);
					value.FillFrom(styleLayer);
					this._startBrushLayerState[styleLayer.Name] = value;
					this._currentBrushLayerState[styleLayer.Name] = value;
				}
				this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.Ended;
			}
		}

		// Token: 0x06000188 RID: 392 RVA: 0x00008B80 File Offset: 0x00006D80
		public void Render(TwoDimensionDrawContext drawContext, in Rectangle2D rect, float scale, float contextAlpha, Vector2 overlayOffset = default(Vector2), Vector2 overlaySize = default(Vector2))
		{
			if (this.Brush != null)
			{
				Vector2 vector;
				vector..ctor(rect.LocalPosition.X, rect.LocalPosition.Y);
				Vector2 vector2;
				vector2..ctor(rect.LocalScale.X, rect.LocalScale.Y);
				if (this.ForcePixelPerfectPlacement)
				{
					vector.X = (float)MathF.Round(vector.X);
					vector.Y = (float)MathF.Round(vector.Y);
				}
				Style styleOfCurrentState = this._styleOfCurrentState;
				for (int i = 0; i < styleOfCurrentState.LayerCount; i++)
				{
					Rectangle2D rectangle2D = rect;
					StyleLayer layer = styleOfCurrentState.GetLayer(i);
					if (!layer.IsHidden)
					{
						BrushLayerState brushLayerState;
						if (this._currentBrushLayerState.Count == 1)
						{
							Dictionary<string, BrushLayerState>.ValueCollection.Enumerator enumerator = this._currentBrushLayerState.Values.GetEnumerator();
							enumerator.MoveNext();
							brushLayerState = enumerator.Current;
						}
						else
						{
							brushLayerState = this._currentBrushLayerState[layer.Name];
						}
						Sprite sprite = brushLayerState.Sprite;
						Texture texture = ((sprite != null) ? sprite.Texture : null);
						if (texture != null)
						{
							float num = vector.X + brushLayerState.XOffset * scale;
							float num2 = vector.Y + brushLayerState.YOffset * scale;
							SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
							simpleMaterial.OverlayEnabled = false;
							simpleMaterial.CircularMaskingEnabled = false;
							Vector2 vector3;
							if (layer.OverlayMethod == BrushOverlayMethod.CoverWithTexture && layer.OverlaySprite != null)
							{
								Sprite overlaySprite = layer.OverlaySprite;
								Texture texture2 = overlaySprite.Texture;
								if (texture2 != null)
								{
									simpleMaterial.OverlayEnabled = true;
									simpleMaterial.StartCoordinate = new Vector2(num, num2);
									simpleMaterial.Size = vector2;
									simpleMaterial.OverlayTexture = texture2;
									simpleMaterial.UseOverlayAlphaAsMask = layer.UseOverlayAlphaAsMask;
									vector3 = default(Vector2);
									float num3;
									float num4;
									if (overlayOffset != vector3)
									{
										num3 = overlayOffset.X;
										num4 = overlayOffset.Y;
									}
									else if (layer.UseOverlayAlphaAsMask)
									{
										num3 = brushLayerState.XOffset + brushLayerState.OverlayXOffset;
										num4 = brushLayerState.YOffset + brushLayerState.OverlayYOffset;
									}
									else
									{
										num3 = brushLayerState.OverlayXOffset;
										num4 = brushLayerState.OverlayYOffset;
									}
									if (layer.UseRandomBaseOverlayXOffset)
									{
										num3 += this.GetRandomXOffset();
									}
									if (layer.UseRandomBaseOverlayYOffset)
									{
										num4 += this.GetRandomYOffset();
									}
									simpleMaterial.OverlayXOffset = num3 * scale;
									simpleMaterial.OverlayYOffset = num4 * scale;
									simpleMaterial.Scale = scale;
									vector3 = default(Vector2);
									if (overlaySize != vector3)
									{
										simpleMaterial.OverlayTextureWidth = overlaySize.X;
										simpleMaterial.OverlayTextureHeight = overlaySize.Y;
									}
									else if (layer.UseOverlayAlphaAsMask)
									{
										simpleMaterial.OverlayTextureWidth = vector2.X;
										simpleMaterial.OverlayTextureHeight = vector2.Y;
									}
									else
									{
										simpleMaterial.OverlayTextureWidth = (float)overlaySprite.Width;
										simpleMaterial.OverlayTextureHeight = (float)overlaySprite.Height;
									}
								}
							}
							simpleMaterial.Texture = texture;
							simpleMaterial.NinePatchParameters = sprite.NinePatchParameters;
							simpleMaterial.Color = brushLayerState.Color * this.Brush.GlobalColor;
							simpleMaterial.ColorFactor = brushLayerState.ColorFactor * this.Brush.GlobalColorFactor;
							simpleMaterial.AlphaFactor = brushLayerState.AlphaFactor * this.Brush.GlobalAlphaFactor * contextAlpha;
							simpleMaterial.HueFactor = brushLayerState.HueFactor;
							simpleMaterial.SaturationFactor = brushLayerState.SaturationFactor;
							simpleMaterial.ValueFactor = brushLayerState.ValueFactor;
							float num5 = 0f;
							float num6 = 0f;
							this._cachedImageFit.Type = layer.ImageFitType;
							this._cachedImageFit.HorizontalAlignment = layer.ImageFitHorizontalAlignment;
							this._cachedImageFit.VerticalAlignment = layer.ImageFitVerticalAlignment;
							this._cachedImageFit.OffsetX = 0f;
							this._cachedImageFit.OffsetY = 0f;
							ImageFit cachedImageFit = this._cachedImageFit;
							vector3 = new Vector2((float)sprite.Width, (float)sprite.Height);
							ImageFitResult fittedRectangle = cachedImageFit.GetFittedRectangle(vector2, vector3);
							if (layer.WidthPolicy == BrushLayerSizePolicy.StretchToTarget)
							{
								float num7 = brushLayerState.ExtendLeft;
								if (layer.HorizontalFlip)
								{
									num7 = brushLayerState.ExtendRight;
								}
								num5 = fittedRectangle.Width;
								num5 += (brushLayerState.ExtendRight + brushLayerState.ExtendLeft) * scale;
								num -= num7 * scale;
							}
							else if (layer.WidthPolicy == BrushLayerSizePolicy.Original)
							{
								num5 = (float)sprite.Width * scale;
							}
							else if (layer.WidthPolicy == BrushLayerSizePolicy.Overriden)
							{
								num5 = layer.OverridenWidth * scale;
							}
							if (layer.HeightPolicy == BrushLayerSizePolicy.StretchToTarget)
							{
								float num8 = brushLayerState.ExtendTop;
								if (layer.HorizontalFlip)
								{
									num8 = brushLayerState.ExtendBottom;
								}
								num6 = fittedRectangle.Height;
								num6 += (brushLayerState.ExtendTop + brushLayerState.ExtendBottom) * scale;
								num2 -= num8 * scale;
							}
							else if (layer.HeightPolicy == BrushLayerSizePolicy.Original)
							{
								num6 = (float)sprite.Height * scale;
							}
							else if (layer.HeightPolicy == BrushLayerSizePolicy.Overriden)
							{
								num6 = layer.OverridenHeight * scale;
							}
							if (layer.HorizontalFlip)
							{
								num5 *= -1f;
							}
							if (layer.VerticalFlip)
							{
								num6 *= -1f;
							}
							float num9 = ((vector2.X == 0f) ? 1f : (num5 / vector2.X));
							float num10 = ((vector2.Y == 0f) ? 1f : (num6 / vector2.Y));
							Vector2 vector4;
							vector4..ctor(num - vector.X + fittedRectangle.OffsetX, num2 - vector.Y + fittedRectangle.OffsetY);
							Vector2 vector5;
							vector5..ctor(num9, num10);
							rectangle2D.AddVisualOffset(vector4.X, vector4.Y);
							rectangle2D.AddVisualScale(vector5.X - 1f, vector5.Y - 1f);
							rectangle2D.AddVisualRotationOffset(brushLayerState.Rotation);
							rectangle2D.ValidateVisuals();
							drawContext.DrawSprite(sprite, simpleMaterial, rectangle2D, scale);
						}
					}
				}
			}
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00009170 File Offset: 0x00007370
		public TextMaterial CreateTextMaterial(TwoDimensionDrawContext drawContext)
		{
			TextMaterial textMaterial = this._currentBrushState.CreateTextMaterial(drawContext);
			if (this.Brush != null)
			{
				textMaterial.ColorFactor *= this.Brush.GlobalColorFactor;
				textMaterial.AlphaFactor *= this.Brush.GlobalAlphaFactor;
				textMaterial.Color *= this.Brush.GlobalColor;
			}
			return textMaterial;
		}

		// Token: 0x0600018A RID: 394 RVA: 0x000091E0 File Offset: 0x000073E0
		public void RestartAnimation()
		{
			if (this.Brush != null)
			{
				this._brushLocalTimer = 0f;
				Style styleOfCurrentState = this._styleOfCurrentState;
				this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.None;
				if (styleOfCurrentState != null)
				{
					if (styleOfCurrentState.AnimationMode == StyleAnimationMode.BasicTransition)
					{
						this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.PlayingBasicTranisition;
						return;
					}
					if (styleOfCurrentState.AnimationMode == StyleAnimationMode.Animation)
					{
						this._brushRendererAnimationState = BrushRenderer.BrushRendererAnimationState.PlayingAnimation;
					}
				}
			}
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00009232 File Offset: 0x00007432
		public void SetSeed(int seed)
		{
			this._offsetSeed = seed;
		}

		// Token: 0x04000087 RID: 135
		private BrushState _startBrushState;

		// Token: 0x04000088 RID: 136
		private BrushState _currentBrushState;

		// Token: 0x04000089 RID: 137
		private Dictionary<string, BrushLayerState> _startBrushLayerState;

		// Token: 0x0400008A RID: 138
		private Dictionary<string, BrushLayerState> _currentBrushLayerState;

		// Token: 0x0400008B RID: 139
		public bool UseLocalTimer;

		// Token: 0x0400008C RID: 140
		private float _brushLocalTimer;

		// Token: 0x0400008D RID: 141
		private float _globalTime;

		// Token: 0x0400008E RID: 142
		private int _offsetSeed;

		// Token: 0x0400008F RID: 143
		private float _randomXOffset;

		// Token: 0x04000090 RID: 144
		private float _randomYOffset;

		// Token: 0x04000091 RID: 145
		private BrushRenderer.BrushRendererAnimationState _brushRendererAnimationState;

		// Token: 0x04000094 RID: 148
		private Brush _brush;

		// Token: 0x04000095 RID: 149
		private long _latestStyleVersion;

		// Token: 0x04000096 RID: 150
		private string _currentState;

		// Token: 0x04000097 RID: 151
		private Style _styleOfCurrentState;

		// Token: 0x04000098 RID: 152
		private ImageFit _cachedImageFit;

		// Token: 0x0200007A RID: 122
		public enum BrushRendererAnimationState
		{
			// Token: 0x0400042C RID: 1068
			None,
			// Token: 0x0400042D RID: 1069
			PlayingAnimation,
			// Token: 0x0400042E RID: 1070
			PlayingBasicTranisition,
			// Token: 0x0400042F RID: 1071
			Ended
		}
	}
}
