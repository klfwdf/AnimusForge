using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientSessionLoginTask : ThreadedClientSessionTask
{
	private TaskCompletionSource<bool> _taskCompletionSource;

	private LoginMessage _message;

	private Task _task;

	public LoginResult LoginResult { get; private set; }

	public ThreadedClientSessionLoginTask(IClientSession session, LoginMessage message)
		: base(session)
	{
		_message = message;
		_taskCompletionSource = new TaskCompletionSource<bool>();
	}

	public override void BeginJob()
	{
		_task = Login();
	}

	private async Task Login()
	{
		LoginResult = await base.Session.Login(_message);
	}

	public override void DoMainThreadJob()
	{
		if (_task.IsCompleted)
		{
			_taskCompletionSource.SetResult(result: true);
			base.Finished = true;
		}
	}

	public async Task Wait()
	{
		await _taskCompletionSource.Task;
	}
}
