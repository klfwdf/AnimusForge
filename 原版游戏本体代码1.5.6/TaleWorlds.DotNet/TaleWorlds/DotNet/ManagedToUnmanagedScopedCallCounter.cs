using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TaleWorlds.DotNet;

public class ManagedToUnmanagedScopedCallCounter : IDisposable
{
	private static ThreadLocal<Dictionary<int, List<StackTrace>>> _table = new ThreadLocal<Dictionary<int, List<StackTrace>>>();

	private static ThreadLocal<int> _depth = new ThreadLocal<int>();

	private static int _depthThreshold = 4;

	private StackTrace _st;

	public ManagedToUnmanagedScopedCallCounter()
	{
		if (!_table.IsValueCreated)
		{
			_table.Value = new Dictionary<int, List<StackTrace>>();
		}
		_depth.Value++;
		if (_depth.Value >= _depthThreshold)
		{
			_st = new StackTrace(fNeedFileInfo: true);
			if (_table.Value.TryGetValue(_depth.Value, out var value))
			{
				value.Add(_st);
				return;
			}
			_table.Value.Add(_depth.Value, new List<StackTrace> { _st });
		}
	}

	public void Dispose()
	{
		_depth.Value--;
	}
}
