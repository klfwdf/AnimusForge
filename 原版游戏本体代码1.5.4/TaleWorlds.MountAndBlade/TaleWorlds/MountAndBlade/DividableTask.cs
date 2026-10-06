using System;

namespace TaleWorlds.MountAndBlade;

public class DividableTask
{
	private bool _isTaskCompletelyFinished;

	private bool _isMainTaskFinished;

	private bool _lastActionCalled;

	private DividableTask _continueToTask;

	private Action _lastAction;

	public DividableTask(DividableTask continueToTask = null)
	{
		_continueToTask = continueToTask;
		ResetTaskStatus();
	}

	public void ResetTaskStatus()
	{
		_isMainTaskFinished = false;
		_isTaskCompletelyFinished = false;
		_lastActionCalled = false;
	}

	public void SetTaskFinished(bool callLastAction = false)
	{
		if (callLastAction)
		{
			_lastAction?.Invoke();
			_lastActionCalled = true;
		}
		_isTaskCompletelyFinished = true;
		_isMainTaskFinished = true;
	}

	public bool Update()
	{
		if (!_isTaskCompletelyFinished)
		{
			if (!_isMainTaskFinished && UpdateExtra())
			{
				_isMainTaskFinished = true;
			}
			if (_isMainTaskFinished)
			{
				_isTaskCompletelyFinished = _continueToTask?.Update() ?? true;
			}
		}
		if (_isTaskCompletelyFinished && !_lastActionCalled)
		{
			_lastAction?.Invoke();
			_lastActionCalled = true;
		}
		return _isTaskCompletelyFinished;
	}

	public void SetLastAction(Action action)
	{
		_lastAction = action;
	}

	protected virtual bool UpdateExtra()
	{
		return true;
	}
}
