using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.GamepadNavigation
{
	// Token: 0x0200004C RID: 76
	internal static class GamepadNavigationHelper
	{
		// Token: 0x06000479 RID: 1145 RVA: 0x00011F38 File Offset: 0x00010138
		internal static void GetRelatedLineOfScope(GamepadNavigationScope scope, Vector2 fromPosition, GamepadNavigationTypes movement, out Vector2 lineBegin, out Vector2 lineEnd, out bool isFromWidget)
		{
			SimpleRectangle discoveryRectangle = scope.GetDiscoveryRectangle();
			if (discoveryRectangle.IsPointInside(fromPosition))
			{
				Widget approximatelyClosestWidgetToPosition = scope.GetApproximatelyClosestWidgetToPosition(fromPosition, movement, false);
				if (approximatelyClosestWidgetToPosition != null)
				{
					isFromWidget = true;
					GamepadNavigationHelper.GetRelatedLineOfWidget(approximatelyClosestWidgetToPosition, movement, out lineBegin, out lineEnd);
					return;
				}
			}
			isFromWidget = false;
			float scale = scope.ParentWidget.EventManager.Context.Scale;
			Vector2 vector;
			vector..ctor(discoveryRectangle.X, discoveryRectangle.Y);
			Vector2 vector2;
			vector2..ctor(discoveryRectangle.X2, discoveryRectangle.Y);
			Vector2 vector3;
			vector3..ctor(discoveryRectangle.X2, discoveryRectangle.Y2);
			Vector2 vector4;
			vector4..ctor(discoveryRectangle.X, discoveryRectangle.Y2);
			if (movement == GamepadNavigationTypes.Up)
			{
				lineBegin = vector4;
				lineEnd = vector3;
				return;
			}
			if (movement == GamepadNavigationTypes.Right)
			{
				lineBegin = vector;
				lineEnd = vector4;
				return;
			}
			if (movement == GamepadNavigationTypes.Down)
			{
				lineBegin = vector;
				lineEnd = vector2;
				return;
			}
			if (movement == GamepadNavigationTypes.Left)
			{
				lineBegin = vector2;
				lineEnd = vector3;
				return;
			}
			lineBegin = Vector2.Zero;
			lineEnd = Vector2.Zero;
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x00012044 File Offset: 0x00010244
		internal static void GetRelatedLineOfWidget(Widget widget, GamepadNavigationTypes movement, out Vector2 lineBegin, out Vector2 lineEnd)
		{
			Vector2 topLeft = widget.AreaRect.TopLeft;
			Vector2 topRight = widget.AreaRect.TopRight;
			Vector2 bottomRight = widget.AreaRect.BottomRight;
			Vector2 bottomLeft = widget.AreaRect.BottomLeft;
			if (movement == GamepadNavigationTypes.Up)
			{
				lineBegin = bottomLeft;
				lineEnd = bottomRight;
				return;
			}
			if (movement == GamepadNavigationTypes.Right)
			{
				lineBegin = topLeft;
				lineEnd = bottomLeft;
				return;
			}
			if (movement == GamepadNavigationTypes.Down)
			{
				lineBegin = topLeft;
				lineEnd = topRight;
				return;
			}
			if (movement == GamepadNavigationTypes.Left)
			{
				lineBegin = topRight;
				lineEnd = bottomRight;
				return;
			}
			lineBegin = Vector2.Zero;
			lineEnd = Vector2.Zero;
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x000120E4 File Offset: 0x000102E4
		internal static float GetDistanceToClosestWidgetEdge(Widget widget, Vector2 point, GamepadNavigationTypes movement, out Vector2 closestPointOnEdge)
		{
			if (movement == GamepadNavigationTypes.Up)
			{
				Vector2 bottomLeft = widget.AreaRect.BottomLeft;
				Vector2 bottomRight = widget.AreaRect.BottomRight;
				closestPointOnEdge = GamepadNavigationHelper.GetClosestPointOnLineSegment(bottomLeft, bottomRight, point);
				return Vector2.Distance(closestPointOnEdge, point);
			}
			if (movement == GamepadNavigationTypes.Right)
			{
				Vector2 topLeft = widget.AreaRect.TopLeft;
				Vector2 bottomLeft2 = widget.AreaRect.BottomLeft;
				closestPointOnEdge = GamepadNavigationHelper.GetClosestPointOnLineSegment(topLeft, bottomLeft2, point);
				return Vector2.Distance(closestPointOnEdge, point);
			}
			if (movement == GamepadNavigationTypes.Down)
			{
				Vector2 topLeft2 = widget.AreaRect.TopLeft;
				Vector2 topRight = widget.AreaRect.TopRight;
				closestPointOnEdge = GamepadNavigationHelper.GetClosestPointOnLineSegment(topLeft2, topRight, point);
				return Vector2.Distance(closestPointOnEdge, point);
			}
			if (movement == GamepadNavigationTypes.Left)
			{
				Vector2 topRight2 = widget.AreaRect.TopRight;
				Vector2 bottomRight2 = widget.AreaRect.BottomRight;
				closestPointOnEdge = GamepadNavigationHelper.GetClosestPointOnLineSegment(topRight2, bottomRight2, point);
				return Vector2.Distance(closestPointOnEdge, point);
			}
			closestPointOnEdge = widget.AreaRect.GetCenter();
			return Vector2.Distance(closestPointOnEdge, point);
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x000121F4 File Offset: 0x000103F4
		internal static float GetDistanceToClosestWidgetEdge(Widget widget, Vector2 point, GamepadNavigationTypes movement)
		{
			Vector2 vector;
			return GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(widget, point, movement, out vector);
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0001220C File Offset: 0x0001040C
		internal static Vector2 GetClosestPointOnLineSegment(Vector2 lineBegin, Vector2 lineEnd, Vector2 point)
		{
			Vector2 vector = point - lineBegin;
			Vector2 vector2 = lineEnd - lineBegin;
			float num = vector2.LengthSquared();
			float num2 = Vector2.Dot(vector, vector2) / num;
			if (num2 < 0f)
			{
				return lineBegin;
			}
			if (num2 > 1f)
			{
				return lineEnd;
			}
			return lineBegin + vector2 * num2;
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x0001225C File Offset: 0x0001045C
		internal static GamepadNavigationTypes GetMovementsToReachRectangle(Vector2 fromPosition, SimpleRectangle rect)
		{
			GamepadNavigationTypes gamepadNavigationTypes = GamepadNavigationTypes.None;
			if (fromPosition.X > rect.X + rect.Width)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Left;
			}
			else if (fromPosition.X < rect.X)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Right;
			}
			if (fromPosition.Y > rect.Y + rect.Height)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Up;
			}
			else if (fromPosition.Y < rect.Y)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Down;
			}
			return gamepadNavigationTypes;
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x000122C8 File Offset: 0x000104C8
		internal static Vector2 GetMovementVectorForNavigation(GamepadNavigationTypes navigationMovement)
		{
			Vector2 vector = default(Vector2);
			vector.X = (float)((navigationMovement == GamepadNavigationTypes.Right) ? 1 : ((navigationMovement == GamepadNavigationTypes.Left) ? (-1) : 0));
			vector.Y = (float)((navigationMovement == GamepadNavigationTypes.Up) ? (-1) : ((navigationMovement == GamepadNavigationTypes.Down) ? 1 : 0));
			return Vector2.Normalize(vector);
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x00012311 File Offset: 0x00010511
		internal static GamepadNavigationScope GetClosestChildScopeAtDirection(GamepadNavigationScope parentScope, Vector2 fromPosition, GamepadNavigationTypes movement, bool checkForAutoGain, out float distanceToScope)
		{
			return GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(parentScope.ChildScopes.ToList<GamepadNavigationScope>(), fromPosition, movement, checkForAutoGain, true, out distanceToScope, Array.Empty<GamepadNavigationScope>());
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x00012330 File Offset: 0x00010530
		internal static GamepadNavigationScope GetClosestScopeAtDirectionFromList(List<GamepadNavigationScope> scopesList, GamepadNavigationScope fromScope, Vector2 fromPosition, GamepadNavigationTypes movement, bool checkForAutoGain, out float distanceToScope)
		{
			distanceToScope = -1f;
			if (fromScope != null)
			{
				Widget lastNavigatedWidget = fromScope.LastNavigatedWidget;
				SimpleRectangle simpleRectangle = (fromScope.UseDiscoveryAreaAsScopeEdges ? fromScope.GetDiscoveryRectangle() : fromScope.GetRectangle());
				if (fromScope.NavigateFromScopeEdges || !simpleRectangle.IsPointInside(fromPosition))
				{
					if (lastNavigatedWidget != null)
					{
						fromPosition = lastNavigatedWidget.AreaRect.GetCenter();
					}
					if (movement == GamepadNavigationTypes.Up)
					{
						fromPosition.Y = simpleRectangle.Y;
					}
					else if (movement == GamepadNavigationTypes.Right)
					{
						fromPosition.X = simpleRectangle.X2;
					}
					else if (movement == GamepadNavigationTypes.Down)
					{
						fromPosition.Y = simpleRectangle.Y2;
					}
					else if (movement == GamepadNavigationTypes.Left)
					{
						fromPosition.X = simpleRectangle.X;
					}
				}
			}
			return GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(scopesList, fromPosition, movement, checkForAutoGain, false, out distanceToScope, new GamepadNavigationScope[] { fromScope });
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x000123EC File Offset: 0x000105EC
		internal static GamepadNavigationScope GetClosestScopeFromList(List<GamepadNavigationScope> scopeList, Vector2 fromPosition, bool checkForAutoGain)
		{
			float num = float.MaxValue;
			int num2 = -1;
			if (scopeList.Count > 0)
			{
				GamepadNavigationTypes[] array = new GamepadNavigationTypes[]
				{
					GamepadNavigationTypes.Up,
					GamepadNavigationTypes.Right,
					GamepadNavigationTypes.Down,
					GamepadNavigationTypes.Left
				};
				for (int i = 0; i < scopeList.Count; i++)
				{
					if ((!checkForAutoGain || !scopeList[i].DoNotAutoGainNavigationOnInit) && scopeList[i].IsAvailable())
					{
						if (scopeList[i].GetRectangle().IsPointInside(fromPosition))
						{
							num2 = i;
							break;
						}
						GamepadNavigationTypes movementsToReachMyPosition = scopeList[i].GetMovementsToReachMyPosition(fromPosition);
						foreach (GamepadNavigationTypes gamepadNavigationTypes in array)
						{
							if (movementsToReachMyPosition.HasAnyFlag(gamepadNavigationTypes))
							{
								Vector2 movementVectorForNavigation = GamepadNavigationHelper.GetMovementVectorForNavigation(gamepadNavigationTypes);
								Vector2 lineBegin;
								Vector2 lineEnd;
								bool flag;
								GamepadNavigationHelper.GetRelatedLineOfScope(scopeList[i], fromPosition, gamepadNavigationTypes, out lineBegin, out lineEnd, out flag);
								Vector2 closestPointOnLineSegment = GamepadNavigationHelper.GetClosestPointOnLineSegment(lineBegin, lineEnd, fromPosition);
								Vector2 vector = Vector2.Normalize(closestPointOnLineSegment - fromPosition);
								float num3 = (flag ? 1f : Vector2.Dot(movementVectorForNavigation, vector));
								float num4 = Vector2.Distance(closestPointOnLineSegment, fromPosition) / num3;
								if (num3 > 0.2f && num4 < num)
								{
									num = num4;
									num2 = i;
								}
							}
						}
					}
				}
				if (num2 != -1)
				{
					return scopeList[num2];
				}
			}
			return null;
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x0001252C File Offset: 0x0001072C
		internal static GamepadNavigationScope GetClosestScopeAtDirectionFromList(List<GamepadNavigationScope> scopesList, Vector2 fromPosition, GamepadNavigationTypes movement, bool checkForAutoGain, bool checkOnlyOneDirection, out float distanceToScope, params GamepadNavigationScope[] scopesToIgnore)
		{
			distanceToScope = -1f;
			if (scopesList == null || scopesList.Count == 0)
			{
				return null;
			}
			scopesList = scopesList.ToList<GamepadNavigationScope>();
			for (int i = 0; i < scopesToIgnore.Length; i++)
			{
				scopesList.Remove(scopesToIgnore[i]);
				if (scopesToIgnore[i].ParentScope != null)
				{
					scopesList.Remove(scopesToIgnore[i].ParentScope);
				}
			}
			Vector2 movementVectorForNavigation = GamepadNavigationHelper.GetMovementVectorForNavigation(movement);
			Vec2 resolution = Input.Resolution;
			float num = (((movement & GamepadNavigationTypes.Vertical) != GamepadNavigationTypes.None) ? (resolution.Y * 0.85f) : (((movement & GamepadNavigationTypes.Horizontal) != GamepadNavigationTypes.None) ? (resolution.X * 0.85f) : 0f));
			float num2 = float.MaxValue;
			int num3 = -1;
			if (scopesList != null && scopesList.Count > 0)
			{
				for (int j = 0; j < scopesList.Count; j++)
				{
					if ((!checkForAutoGain || !scopesList[j].DoNotAutoGainNavigationOnInit) && scopesList[j].IsAvailable())
					{
						Vector2 lineBegin;
						Vector2 lineEnd;
						bool flag;
						GamepadNavigationHelper.GetRelatedLineOfScope(scopesList[j], fromPosition, movement, out lineBegin, out lineEnd, out flag);
						Vector2 closestPointOnLineSegment = GamepadNavigationHelper.GetClosestPointOnLineSegment(lineBegin, lineEnd, fromPosition);
						Vector2 vector = Vector2.Normalize(closestPointOnLineSegment - fromPosition);
						float num4 = (flag ? 1f : Vector2.Dot(movementVectorForNavigation, vector));
						if (num4 > 0.2f)
						{
							float num5;
							if (checkOnlyOneDirection)
							{
								num5 = GamepadNavigationHelper.GetDirectionalDistanceBetweenTwoPoints(movement, fromPosition, closestPointOnLineSegment);
							}
							else
							{
								num5 = Vector2.Distance(closestPointOnLineSegment, fromPosition) / num4;
							}
							if (num5 < num2 && num5 < num)
							{
								num2 = num5;
								distanceToScope = num5;
								num3 = j;
							}
						}
					}
				}
				if (num3 != -1)
				{
					return scopesList[num3];
				}
			}
			return null;
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x000126BC File Offset: 0x000108BC
		internal static float GetDirectionalDistanceBetweenTwoPoints(GamepadNavigationTypes movement, Vector2 p1, Vector2 p2)
		{
			if (movement == GamepadNavigationTypes.Right || movement == GamepadNavigationTypes.Left)
			{
				return MathF.Abs(p1.X - p2.X);
			}
			if (movement == GamepadNavigationTypes.Up || movement == GamepadNavigationTypes.Down)
			{
				return MathF.Abs(p1.Y - p2.Y);
			}
			Debug.FailedAssert("Invalid gamepad movement type:" + movement, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GamepadNavigationHelper.cs", "GetDirectionalDistanceBetweenTwoPoints", 406);
			return 0f;
		}
	}
}
