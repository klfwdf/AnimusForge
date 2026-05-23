using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientSessionFunctionTask : ThreadedClientSessionTask
{
	private TaskCompletionSource<bool> _taskCompletionSource;

	private Message _message;

	private Task _task;

	public FunctionResult FunctionResult { get; private set; }

	public ThreadedClientSessionFunctionTask(IClientSession session, Message message)
		: base(session)
	{
		_message = message;
		_taskCompletionSource = new TaskCompletionSource<bool>();
	}

	public override void BeginJob()
	{
		_task = CallFunction();
	}

	private async Task CallFunction()
	{
		FunctionResult = await base.Session.CallFunction<FunctionResult>(_message);
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
