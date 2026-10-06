using System.Collections.Concurrent;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class ChatLogMessageManager : MessageManagerBase
{
	public struct ChatLineData
	{
		public string Text;

		public uint Color;

		public ChatLineData(string text, uint color)
		{
			Text = text;
			Color = color;
		}
	}

	private const uint WarningColor = 4292235858u;

	private const uint SuccessColor = 4285126986u;

	private MPChatVM _chatDataSource;

	private ConcurrentQueue<ChatLineData> _queue;

	public ChatLogMessageManager(MPChatVM chatDataSource)
	{
		_chatDataSource = chatDataSource;
		_queue = new ConcurrentQueue<ChatLineData>();
		InformationManager.DisplayMessageInternal += OnDisplayMessageReceived;
	}

	private void OnDisplayMessageReceived(InformationMessage message)
	{
		if (!string.IsNullOrEmpty(message.SoundEventPath))
		{
			SoundEvent.PlaySound2D(message.SoundEventPath);
		}
	}

	public void Update()
	{
		ChatLineData result;
		while (_queue.TryDequeue(out result))
		{
			InformationManager.DisplayMessage(new InformationMessage(result.Text, Color.FromUint(result.Color), "Default"));
		}
	}

	protected override void PostWarningLine(string text)
	{
	}

	protected override void PostSuccessLine(string text)
	{
	}

	protected override void PostMessageLineFormatted(string text, uint color)
	{
	}

	protected override void PostMessageLine(string text, uint color)
	{
	}
}
