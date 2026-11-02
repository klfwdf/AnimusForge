using System;
using System.Collections.Generic;

namespace AnimusForge;

// Main-thread scene-owned scratch. The cached grid already includes terrain, body clearance and props.
// Queries do no engine calls or full-array clears; four-neighbour paths cannot cut a blocked corner.
internal sealed class FieldCampGroundRouter
{
    private readonly int _width, _height;
    private readonly bool[] _open;
    private readonly float[] _heights;
    private readonly int[] _seen, _parent, _queue;
    private int _generation;
    internal int LastSearchVisited { get; private set; }
    internal const int SearchBudget = 32768;
    internal const int MaxCorners = 128;

    internal FieldCampGroundRouter(int width, int height, bool[] open, float[] heights)
    {
        if (width <= 0 || height <= 0 || (long)width * height > 98304 || open == null || heights == null
            || open.Length != width * height || heights.Length != open.Length) throw new ArgumentException("Invalid camp route grid.");
        _width = width; _height = height; _open = open; _heights = heights;
        _seen = new int[open.Length]; _parent = new int[open.Length]; _queue = new int[open.Length];
    }

    internal bool IsTraversable(int cell) => cell >= 0 && cell < _open.Length && _open[cell]
        && !float.IsNaN(_heights[cell]) && !float.IsInfinity(_heights[cell]);

    internal bool TryRoute(int start, int goal, out int[] corners)
    {
        corners = Array.Empty<int>(); LastSearchVisited = 0;
        if (!IsTraversable(start) || !IsTraversable(goal)) return false;
        if (_generation == int.MaxValue) { Array.Clear(_seen, 0, _seen.Length); _generation = 0; }
        int stamp = ++_generation, head = 0, tail = 1;
        _queue[0] = start; _seen[start] = stamp; _parent[start] = -1;
        while (head < tail && head < SearchBudget)
        {
            int cell = _queue[head++]; LastSearchVisited = head;
            if (cell == goal)
            {
                var reverse = new List<int>(16) { goal };
                int current = goal, direction = 0;
                while (current != start)
                {
                    int parent = _parent[current];
                    int nextDirection = current - parent;
                    if (direction != 0 && nextDirection != direction) reverse.Add(current);
                    if (reverse.Count >= MaxCorners) return false;
                    direction = nextDirection; current = parent;
                }
                if (goal != start) reverse.Add(start);
                reverse.Reverse(); corners = reverse.ToArray(); return true;
            }
            int x = cell % _width, y = cell / _width;
            if (x > 0) Visit(cell - 1, cell, stamp, ref tail);
            if (x + 1 < _width) Visit(cell + 1, cell, stamp, ref tail);
            if (y > 0) Visit(cell - _width, cell, stamp, ref tail);
            if (y + 1 < _height) Visit(cell + _width, cell, stamp, ref tail);
        }
        return false;
    }

    private void Visit(int next, int from, int stamp, ref int tail)
    {
        if (!_open[next] || _seen[next] == stamp || float.IsNaN(_heights[next]) || float.IsInfinity(_heights[next])
            || Math.Abs(_heights[next] - _heights[from]) > 0.35f) return;
        _seen[next] = stamp; _parent[next] = from; _queue[tail++] = next;
    }
}
