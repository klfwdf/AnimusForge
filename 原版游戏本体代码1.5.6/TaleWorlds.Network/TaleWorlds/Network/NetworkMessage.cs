using System;
using System.Text;

namespace TaleWorlds.Network;

public class NetworkMessage : INetworkMessageWriter, INetworkMessageReader
{
	private int _readCursor;

	private int _writeCursor;

	private bool _finalized;

	private byte[] Buffer => MessageBuffer.Buffer;

	internal MessageBuffer MessageBuffer { get; private set; }

	internal int DataLength
	{
		get
		{
			return MessageBuffer.DataLength - 4;
		}
		set
		{
			MessageBuffer.DataLength = value + 4;
		}
	}

	private NetworkMessage()
	{
		_writeCursor = 4;
		_readCursor = 4;
		_finalized = false;
	}

	internal static NetworkMessage CreateForReading(MessageBuffer messageBuffer)
	{
		return new NetworkMessage
		{
			MessageBuffer = messageBuffer
		};
	}

	internal static NetworkMessage CreateForWriting()
	{
		NetworkMessage networkMessage = new NetworkMessage();
		networkMessage.MessageBuffer = new MessageBuffer(new byte[16777216]);
		networkMessage.Reset();
		return networkMessage;
	}

	public void Write(string data)
	{
		int writeCursor = _writeCursor;
		_writeCursor += 4;
		int num;
		if (data != null)
		{
			num = Encoding.UTF8.GetBytes(data, 0, data.Length, Buffer, _writeCursor);
			_writeCursor += num;
		}
		else
		{
			num = -1;
		}
		byte[] bytes = BitConverter.GetBytes(num);
		Buffer[writeCursor] = bytes[0];
		Buffer[writeCursor + 1] = bytes[1];
		Buffer[writeCursor + 2] = bytes[2];
		Buffer[writeCursor + 3] = bytes[3];
	}

	public void Write(int data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		Buffer[_writeCursor + 1] = bytes[1];
		Buffer[_writeCursor + 2] = bytes[2];
		Buffer[_writeCursor + 3] = bytes[3];
		_writeCursor += 4;
	}

	public void Write(short data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		Buffer[_writeCursor + 1] = bytes[1];
		_writeCursor += 2;
	}

	public void Write(bool data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		_writeCursor++;
	}

	public void Write(byte data)
	{
		Buffer[_writeCursor] = data;
		_writeCursor++;
	}

	public void Write(float data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		Buffer[_writeCursor + 1] = bytes[1];
		Buffer[_writeCursor + 2] = bytes[2];
		Buffer[_writeCursor + 3] = bytes[3];
		_writeCursor += 4;
	}

	public void Write(long data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		Buffer[_writeCursor + 1] = bytes[1];
		Buffer[_writeCursor + 2] = bytes[2];
		Buffer[_writeCursor + 3] = bytes[3];
		Buffer[_writeCursor + 4] = bytes[4];
		Buffer[_writeCursor + 5] = bytes[5];
		Buffer[_writeCursor + 6] = bytes[6];
		Buffer[_writeCursor + 7] = bytes[7];
		_writeCursor += 8;
	}

	public void Write(ulong data)
	{
		byte[] bytes = BitConverter.GetBytes(data);
		Buffer[_writeCursor] = bytes[0];
		Buffer[_writeCursor + 1] = bytes[1];
		Buffer[_writeCursor + 2] = bytes[2];
		Buffer[_writeCursor + 3] = bytes[3];
		Buffer[_writeCursor + 4] = bytes[4];
		Buffer[_writeCursor + 5] = bytes[5];
		Buffer[_writeCursor + 6] = bytes[6];
		Buffer[_writeCursor + 7] = bytes[7];
		_writeCursor += 8;
	}

	public void Write(Guid data)
	{
		byte[] array = data.ToByteArray();
		for (int i = 0; i < array.Length; i++)
		{
			Buffer[_writeCursor + i] = array[i];
		}
		_writeCursor += array.Length;
	}

	public void Write(byte[] data)
	{
		Write(data.Length);
		for (int i = 0; i < data.Length; i++)
		{
			Buffer[_writeCursor + i] = data[i];
		}
		_writeCursor += data.Length;
	}

	internal void Write(MessageContract messageContract)
	{
		Write(messageContract.MessageId);
		messageContract.SerializeToNetworkMessage(this);
	}

	public int ReadInt32()
	{
		int result = BitConverter.ToInt32(Buffer, _readCursor);
		_readCursor += 4;
		return result;
	}

	public short ReadInt16()
	{
		short result = BitConverter.ToInt16(Buffer, _readCursor);
		_readCursor += 2;
		return result;
	}

	public bool ReadBoolean()
	{
		bool result = BitConverter.ToBoolean(Buffer, _readCursor);
		_readCursor++;
		return result;
	}

	public byte ReadByte()
	{
		byte result = Buffer[_readCursor];
		_readCursor++;
		return result;
	}

	public string ReadString()
	{
		int num = ReadInt32();
		string result = null;
		if (num >= 0)
		{
			result = Encoding.UTF8.GetString(Buffer, _readCursor, num);
			_readCursor += num;
		}
		return result;
	}

	public float ReadFloat()
	{
		float result = BitConverter.ToSingle(Buffer, _readCursor);
		_readCursor += 4;
		return result;
	}

	public long ReadInt64()
	{
		long result = BitConverter.ToInt64(Buffer, _readCursor);
		_readCursor += 8;
		return result;
	}

	public ulong ReadUInt64()
	{
		ulong result = BitConverter.ToUInt64(Buffer, _readCursor);
		_readCursor += 8;
		return result;
	}

	public Guid ReadGuid()
	{
		byte[] array = new byte[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Buffer[_readCursor + i];
		}
		_readCursor += array.Length;
		return new Guid(array);
	}

	public byte[] ReadByteArray()
	{
		byte[] array = new byte[ReadInt32()];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Buffer[_readCursor + i];
		}
		_readCursor += array.Length;
		return array;
	}

	internal void Reset()
	{
		_writeCursor = 4;
		_readCursor = 4;
		_finalized = false;
		DataLength = 0;
	}

	internal void ResetRead()
	{
		_readCursor = 4;
	}

	internal void BeginRead()
	{
		_readCursor = 4;
	}

	internal void BeginWrite()
	{
		_writeCursor = 4;
		_finalized = false;
	}

	internal void FinalizeWrite()
	{
		if (!_finalized)
		{
			_finalized = true;
			DataLength = _writeCursor - 4;
		}
	}

	internal void UpdateHeader()
	{
		Buffer[0] = (byte)(DataLength & 0xFF);
		Buffer[1] = (byte)((DataLength >> 8) & 0xFF);
		Buffer[2] = (byte)((DataLength >> 16) & 0xFF);
		Buffer[3] = (byte)((DataLength >> 24) & 0xFF);
	}

	internal string GetDebugText()
	{
		return BitConverter.ToString(Buffer, 0, DataLength);
	}
}
