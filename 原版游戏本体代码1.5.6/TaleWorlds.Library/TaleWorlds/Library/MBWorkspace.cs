namespace TaleWorlds.Library;

public class MBWorkspace<T> where T : IMBCollection, new()
{
	private bool _isBeingUsed;

	private T _workspace;

	public T StartUsingWorkspace()
	{
		_isBeingUsed = true;
		if (_workspace == null)
		{
			_workspace = new T();
		}
		return _workspace;
	}

	public void StopUsingWorkspace()
	{
		_isBeingUsed = false;
		_workspace.Clear();
	}

	public T GetWorkspace()
	{
		return _workspace;
	}
}
