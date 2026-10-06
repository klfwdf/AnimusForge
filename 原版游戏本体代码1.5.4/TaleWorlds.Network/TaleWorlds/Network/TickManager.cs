using System.Diagnostics;
using System.Threading;

namespace TaleWorlds.Network;

public class TickManager
{
	public delegate void TickDelegate();

	private Stopwatch _stopwatch;

	private int _tickRate = 5000;

	private TickDelegate _tickMethod;

	private double _residualWaitTime;

	private double _numberOfTicksPerMilisecond;

	private double _inverseNumberOfTicksPerMilisecond;

	private double _maxTickMilisecond;

	public TickManager(int tickRate, TickDelegate tickMethod)
	{
		_tickRate = tickRate;
		_tickMethod = tickMethod;
		_numberOfTicksPerMilisecond = (double)Stopwatch.Frequency / 1000.0;
		_inverseNumberOfTicksPerMilisecond = 1000.0 / (double)Stopwatch.Frequency;
		_maxTickMilisecond = 1000.0 / (double)_tickRate;
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
	}

	public void Tick()
	{
		long elapsedTicks = _stopwatch.ElapsedTicks;
		_tickMethod();
		double num = _stopwatch.ElapsedTicks - elapsedTicks;
		double num2 = _inverseNumberOfTicksPerMilisecond * num;
		if (num2 < _maxTickMilisecond)
		{
			double num3 = _maxTickMilisecond - num2;
			num3 += _residualWaitTime;
			int num4 = (int)num3;
			_residualWaitTime = num3 - (double)num4;
			if (num4 > 0)
			{
				Thread.Sleep(num4);
			}
		}
	}
}
