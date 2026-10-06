using System;
using System.Diagnostics;

namespace TaleWorlds.Library;

public class ScopedTimer : IDisposable
{
	private readonly Stopwatch watch_;

	private readonly string scopeName_;

	public ScopedTimer(string scopeName)
	{
		scopeName_ = scopeName;
		watch_ = new Stopwatch();
		watch_.Start();
	}

	public void Dispose()
	{
		watch_.Stop();
		Console.WriteLine("ScopedTimer: " + scopeName_ + " elapsed ms: " + watch_.Elapsed.TotalMilliseconds);
	}
}
