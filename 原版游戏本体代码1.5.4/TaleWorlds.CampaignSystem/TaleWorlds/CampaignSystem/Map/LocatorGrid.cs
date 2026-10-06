using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map;

internal class LocatorGrid<T> where T : ILocatable<T>
{
	private const float DefaultGridNodeSize = 5f;

	private const int DefaultGridWidth = 32;

	private const int DefaultGridHeight = 32;

	private readonly T[] _nodes;

	private readonly float _gridNodeSize;

	private readonly int _width;

	private readonly int _height;

	internal LocatorGrid(float gridNodeSize = 5f, int gridWidth = 32, int gridHeight = 32)
	{
		_width = gridWidth;
		_height = gridHeight;
		_gridNodeSize = gridNodeSize;
		_nodes = new T[_width * _height];
	}

	private int MapCoordinates(int x, int y)
	{
		x %= _width;
		if (x < 0)
		{
			x += _width;
		}
		y %= _height;
		if (y < 0)
		{
			y += _height;
		}
		return y * _width + x;
	}

	internal bool CheckWhetherPositionsAreInSameNode(Vec2 pos1, ILocatable<T> locatable)
	{
		int num = Pos2NodeIndex(pos1);
		int locatorNodeIndex = locatable.LocatorNodeIndex;
		return num == locatorNodeIndex;
	}

	internal bool UpdateLocator(T locatable)
	{
		ILocatable<T> locatable2 = locatable;
		Vec2 getPosition2D = locatable2.GetPosition2D;
		int num = Pos2NodeIndex(getPosition2D);
		if (num != locatable2.LocatorNodeIndex)
		{
			if (locatable2.LocatorNodeIndex >= 0)
			{
				RemoveFromList(locatable2);
			}
			AddToList(num, locatable);
			locatable2.LocatorNodeIndex = num;
			return true;
		}
		return false;
	}

	private void RemoveFromList(ILocatable<T> locatable)
	{
		if ((object)_nodes[locatable.LocatorNodeIndex] == locatable)
		{
			_nodes[locatable.LocatorNodeIndex] = locatable.NextLocatable;
			locatable.NextLocatable = default(T);
		}
		else
		{
			ILocatable<T> locatable2;
			if ((locatable2 = _nodes[locatable.LocatorNodeIndex]) == null)
			{
				return;
			}
			while (locatable2.NextLocatable != null)
			{
				if ((object)locatable2.NextLocatable == locatable)
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

	private void AddToList(int nodeIndex, T locator)
	{
		T nextLocatable = _nodes[nodeIndex];
		_nodes[nodeIndex] = locator;
		locator.NextLocatable = nextLocatable;
	}

	private T FindLocatableOnNextNode(ref LocatableSearchData<T> data)
	{
		T val = default(T);
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
				int num = MapCoordinates(data.CurrentX, data.CurrentY);
				val = _nodes[num];
			}
		}
		while (val == null && data.CurrentX <= data.MaxXInclusive);
		return val;
	}

	internal T FindNextLocatable(ref LocatableSearchData<T> data)
	{
		if (data.CurrentLocatable != null)
		{
			data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
			while (data.CurrentLocatable != null && data.CurrentLocatable.GetPosition2D.DistanceSquared(data.Position) >= data.RadiusSquared)
			{
				data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
			}
		}
		while (data.CurrentLocatable == null && data.CurrentX <= data.MaxXInclusive)
		{
			data.CurrentLocatable = FindLocatableOnNextNode(ref data);
			while (data.CurrentLocatable != null && data.CurrentLocatable.GetPosition2D.DistanceSquared(data.Position) >= data.RadiusSquared)
			{
				data.CurrentLocatable = data.CurrentLocatable.NextLocatable;
			}
		}
		return (T)data.CurrentLocatable;
	}

	internal LocatableSearchData<T> StartFindingLocatablesAroundPosition(Vec2 position, float radius)
	{
		GetBoundaries(position, radius, out var minX, out var minY, out var maxX, out var maxY);
		return new LocatableSearchData<T>(position, radius, minX, minY, maxX, maxY);
	}

	internal void RemoveLocatable(T locatable)
	{
		ILocatable<T> locatable2 = locatable;
		if (locatable2.LocatorNodeIndex >= 0)
		{
			RemoveFromList(locatable2);
		}
	}

	private void GetBoundaries(Vec2 position, float radius, out int minX, out int minY, out int maxX, out int maxY)
	{
		Vec2 vec = new Vec2(radius, radius);
		GetGridIndices(position - vec, out minX, out minY);
		GetGridIndices(position + vec, out maxX, out maxY);
		int num = Math.Min(maxX - minX, _width - 1);
		int num2 = Math.Min(maxY - minY, _height - 1);
		minX %= _width;
		minY %= _height;
		maxX = minX + num;
		maxY = minY + num2;
	}

	private void GetGridIndices(Vec2 position, out int x, out int y)
	{
		x = TaleWorlds.Library.MathF.Floor(position.x / _gridNodeSize);
		y = TaleWorlds.Library.MathF.Floor(position.y / _gridNodeSize);
	}

	private int Pos2NodeIndex(Vec2 position)
	{
		GetGridIndices(position, out var x, out var y);
		return MapCoordinates(x, y);
	}
}
