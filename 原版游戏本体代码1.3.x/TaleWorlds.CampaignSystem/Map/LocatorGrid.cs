using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x0200021F RID: 543
	internal class LocatorGrid<T> where T : ILocatable<T>
	{
		// Token: 0x06002080 RID: 8320 RVA: 0x0008F261 File Offset: 0x0008D461
		internal LocatorGrid(float gridNodeSize = 5f, int gridWidth = 32, int gridHeight = 32)
		{
			this._width = gridWidth;
			this._height = gridHeight;
			this._gridNodeSize = gridNodeSize;
			this._nodes = new T[this._width * this._height];
		}

		// Token: 0x06002081 RID: 8321 RVA: 0x0008F296 File Offset: 0x0008D496
		private int MapCoordinates(int x, int y)
		{
			x %= this._width;
			if (x < 0)
			{
				x += this._width;
			}
			y %= this._height;
			if (y < 0)
			{
				y += this._height;
			}
			return y * this._width + x;
		}

		// Token: 0x06002082 RID: 8322 RVA: 0x0008F2D4 File Offset: 0x0008D4D4
		internal bool CheckWhetherPositionsAreInSameNode(Vec2 pos1, ILocatable<T> locatable)
		{
			int num = this.Pos2NodeIndex(pos1);
			int locatorNodeIndex = locatable.LocatorNodeIndex;
			return num == locatorNodeIndex;
		}

		// Token: 0x06002083 RID: 8323 RVA: 0x0008F2F4 File Offset: 0x0008D4F4
		internal bool UpdateLocator(T locatable)
		{
			ILocatable<T> locatable2 = locatable;
			Vec2 getPosition2D = locatable2.GetPosition2D;
			int num = this.Pos2NodeIndex(getPosition2D);
			if (num != locatable2.LocatorNodeIndex)
			{
				if (locatable2.LocatorNodeIndex >= 0)
				{
					this.RemoveFromList(locatable2);
				}
				this.AddToList(num, locatable);
				locatable2.LocatorNodeIndex = num;
				return true;
			}
			return false;
		}

		// Token: 0x06002084 RID: 8324 RVA: 0x0008F344 File Offset: 0x0008D544
		private void RemoveFromList(ILocatable<T> locatable)
		{
			if (this._nodes[locatable.LocatorNodeIndex] == locatable)
			{
				this._nodes[locatable.LocatorNodeIndex] = locatable.NextLocatable;
				locatable.NextLocatable = default(T);
				return;
			}
			ILocatable<T> locatable2;
			if ((locatable2 = this._nodes[locatable.LocatorNodeIndex]) != null)
			{
				while (locatable2.NextLocatable != null)
				{
					if (locatable2.NextLocatable == locatable)
					{
						locatable2.NextLocatable = locatable.NextLocatable;
						locatable.NextLocatable = default(T);
						return;
					}
					locatable2 = locatable2.NextLocatable;
				}
				Debug.FailedAssert("cannot remove party from MapLocator: " + locatable.ToString(), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\LocatorGrid.cs", "RemoveFromList", 134);
			}
		}

		// Token: 0x06002085 RID: 8325 RVA: 0x0008F414 File Offset: 0x0008D614
		private void AddToList(int nodeIndex, T locator)
		{
			T nextLocatable = this._nodes[nodeIndex];
			this._nodes[nodeIndex] = locator;
			locator.NextLocatable = nextLocatable;
		}

		// Token: 0x06002086 RID: 8326 RVA: 0x0008F448 File Offset: 0x0008D648
		private T FindLocatableOnNextNode(ref LocatableSearchData<T> data)
		{
			T t = default(T);
			do
			{
				data.CurrentY++;
				if (data.CurrentY > data.MaxYInclusive)
				{
					data.CurrentY = data.MinY;
					data.CurrentX++;
				}
				if (data.CurrentX <= data.MaxXInclusive)
				{
					int num = this.MapCoordinates(data.CurrentX, data.CurrentY);
					t = this._nodes[num];
				}
			}
			while (t == null && data.CurrentX <= data.MaxXInclusive);
			return t;
		}

		// Token: 0x06002087 RID: 8327 RVA: 0x0008F4D4 File Offset: 0x0008D6D4
		internal T FindNextLocatable(ref LocatableSearchData<T> data)
		{
			if (data.CurrentLocatable != null)
			{
				data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
				while (data.CurrentLocatable != null)
				{
					if (data.CurrentLocatable.GetPosition2D.DistanceSquared(data.Position) < data.RadiusSquared)
					{
						break;
					}
					data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
				}
			}
			while (data.CurrentLocatable == null && data.CurrentX <= data.MaxXInclusive)
			{
				data.CurrentLocatable = this.FindLocatableOnNextNode(ref data);
				while (data.CurrentLocatable != null && data.CurrentLocatable.GetPosition2D.DistanceSquared(data.Position) >= data.RadiusSquared)
				{
					data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
				}
			}
			return (T)((object)data.CurrentLocatable);
		}

		// Token: 0x06002088 RID: 8328 RVA: 0x0008F5BC File Offset: 0x0008D7BC
		internal LocatableSearchData<T> StartFindingLocatablesAroundPosition(Vec2 position, float radius)
		{
			int minX;
			int minY;
			int maxX;
			int maxY;
			this.GetBoundaries(position, radius, out minX, out minY, out maxX, out maxY);
			return new LocatableSearchData<T>(position, radius, minX, minY, maxX, maxY);
		}

		// Token: 0x06002089 RID: 8329 RVA: 0x0008F5E4 File Offset: 0x0008D7E4
		internal void RemoveLocatable(T locatable)
		{
			ILocatable<T> locatable2 = locatable;
			if (locatable2.LocatorNodeIndex >= 0)
			{
				this.RemoveFromList(locatable2);
			}
		}

		// Token: 0x0600208A RID: 8330 RVA: 0x0008F608 File Offset: 0x0008D808
		private void GetBoundaries(Vec2 position, float radius, out int minX, out int minY, out int maxX, out int maxY)
		{
			Vec2 v = new Vec2(radius, radius);
			this.GetGridIndices(position - v, out minX, out minY);
			this.GetGridIndices(position + v, out maxX, out maxY);
			int num = Math.Min(maxX - minX, this._width - 1);
			int num2 = Math.Min(maxY - minY, this._height - 1);
			minX %= this._width;
			minY %= this._height;
			maxX = minX + num;
			maxY = minY + num2;
		}

		// Token: 0x0600208B RID: 8331 RVA: 0x0008F68F File Offset: 0x0008D88F
		private void GetGridIndices(Vec2 position, out int x, out int y)
		{
			x = MathF.Floor(position.x / this._gridNodeSize);
			y = MathF.Floor(position.y / this._gridNodeSize);
		}

		// Token: 0x0600208C RID: 8332 RVA: 0x0008F6BC File Offset: 0x0008D8BC
		private int Pos2NodeIndex(Vec2 position)
		{
			int x;
			int y;
			this.GetGridIndices(position, out x, out y);
			return this.MapCoordinates(x, y);
		}

		// Token: 0x0400097F RID: 2431
		private const float DefaultGridNodeSize = 5f;

		// Token: 0x04000980 RID: 2432
		private const int DefaultGridWidth = 32;

		// Token: 0x04000981 RID: 2433
		private const int DefaultGridHeight = 32;

		// Token: 0x04000982 RID: 2434
		private readonly T[] _nodes;

		// Token: 0x04000983 RID: 2435
		private readonly float _gridNodeSize;

		// Token: 0x04000984 RID: 2436
		private readonly int _width;

		// Token: 0x04000985 RID: 2437
		private readonly int _height;
	}
}
