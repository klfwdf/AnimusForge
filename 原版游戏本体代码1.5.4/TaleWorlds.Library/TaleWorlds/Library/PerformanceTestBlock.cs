using System;
using System.Diagnostics;

namespace TaleWorlds.Library;

public class PerformanceTestBlock : IDisposable
{
	private readonly string _name;

	private readonly Stopwatch _stopwatch;

	public PerformanceTestBlock(string name)
	{
		_name = name;
		Debug.Print(_name + " block is started.");
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
	}

	void IDisposable.Dispose()
	{
		float num = (float)_stopwatch.ElapsedMilliseconds / 1000f;
		Debug.Print(_name + " completed in " + num + " seconds.");
	}
}
