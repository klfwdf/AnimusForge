using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using psai.net;

namespace psai.Editor;

[Serializable]
public class AudioData : ICloneable
{
	private string _filePathRelativeToProjectDir = "";

	public int _prebeatLengthInSamplesEnteredManually;

	public int _postbeatLengthInSamplesEnteredManually;

	[XmlElement("Path")]
	public string FilePathRelativeToProjectDir
	{
		get
		{
			return _filePathRelativeToProjectDir;
		}
		set
		{
			string filePathRelativeToProjectDir = value.Replace(Path.DirectorySeparatorChar, '/');
			_filePathRelativeToProjectDir = filePathRelativeToProjectDir;
		}
	}

	[XmlIgnore]
	public string ModuleID { get; set; }

	[XmlIgnore]
	public string FilePathRelativeToProjectDirForCurrentSystem => _filePathRelativeToProjectDir.Replace('/', Path.DirectorySeparatorChar);

	public float Bpm { get; set; }

	public float PreBeats { get; set; }

	public float PostBeats { get; set; }

	public bool CalculatePostAndPrebeatLengthBasedOnBeats { get; set; }

	public int PreBeatLengthInSamples
	{
		get
		{
			if (CalculatePostAndPrebeatLengthBasedOnBeats)
			{
				return GetPrebeatLengthInSamplesBasedOnBeats();
			}
			return _prebeatLengthInSamplesEnteredManually;
		}
		set
		{
			_prebeatLengthInSamplesEnteredManually = value;
		}
	}

	public int PostBeatLengthInSamples
	{
		get
		{
			if (CalculatePostAndPrebeatLengthBasedOnBeats)
			{
				return GetPostbeatLengthInSamplesBasedOnBeats();
			}
			return _postbeatLengthInSamplesEnteredManually;
		}
		set
		{
			_postbeatLengthInSamplesEnteredManually = value;
		}
	}

	public int TotalLengthInSamples { get; set; }

	public int SampleRate { get; set; }

	[XmlIgnore]
	public int BitsPerSample { get; set; }

	[XmlIgnore]
	public int ChannelCount { get; set; }

	[XmlIgnore]
	public long ByteIndexOfWaveformDataWithinAudioFile { get; set; }

	[XmlIgnore]
	public int LengthOfWaveformDataInBytes { get; set; }

	public AudioData()
	{
		FilePathRelativeToProjectDir = "";
		BitsPerSample = 0;
		PostBeatLengthInSamples = 0;
		PreBeatLengthInSamples = 0;
		SampleRate = 0;
		LengthOfWaveformDataInBytes = 0;
		Bpm = 100f;
		PreBeats = 1f;
		PostBeats = 1f;
		CalculatePostAndPrebeatLengthBasedOnBeats = false;
	}

	public psai.net.AudioData CreatePsaiDotNetVersion()
	{
		psai.net.AudioData audioData = new psai.net.AudioData();
		audioData.filePathRelativeToProjectDir = FilePathRelativeToProjectDir;
		audioData.moduleId = ModuleID;
		if (CalculatePostAndPrebeatLengthBasedOnBeats)
		{
			audioData.sampleCountPreBeat = GetPrebeatLengthInSamplesBasedOnBeats();
			audioData.sampleCountPostBeat = GetPostbeatLengthInSamplesBasedOnBeats();
		}
		else
		{
			audioData.sampleCountPreBeat = PreBeatLengthInSamples;
			audioData.sampleCountPostBeat = PostBeatLengthInSamples;
		}
		audioData.sampleCountTotal = TotalLengthInSamples;
		audioData.sampleRateHz = SampleRate;
		audioData.bpm = Bpm;
		return audioData;
	}

	public int GetMillisecondsFromSampleCount(int sampleCount)
	{
		return (int)((long)sampleCount * 1000L / SampleRate);
	}

	public int GetSampleCountFromMilliseconds(int durationMs)
	{
		return SampleRate * durationMs / 1000;
	}

	public int GetLengthInSamplesBasedOnBeats(float bpm, float beats)
	{
		int num = (int)(60000f / bpm);
		return GetSampleCountFromMilliseconds((int)((float)num * beats));
	}

	public int GetPostbeatLengthInSamplesBasedOnBeats()
	{
		return GetLengthInSamplesBasedOnBeats(Bpm, PostBeats);
	}

	public int GetPrebeatLengthInSamplesBasedOnBeats()
	{
		return GetLengthInSamplesBasedOnBeats(Bpm, PreBeats);
	}

	public static int CalculateTotalLengthInSamples(int lengthOfWaveformDataInBytes, int bitsPerSample, int channelCount)
	{
		if (lengthOfWaveformDataInBytes > 0 && bitsPerSample > 0 && channelCount > 0)
		{
			return lengthOfWaveformDataInBytes / (bitsPerSample / 8) / channelCount;
		}
		return 0;
	}

	public bool DoUpdateMembersBasedOnWaveHeader(string fullPathToAudioFile, out string errorMessage)
	{
		bool result = false;
		if (fullPathToAudioFile != null && fullPathToAudioFile.Length > 0)
		{
			string text = fullPathToAudioFile.Replace('/', Path.DirectorySeparatorChar);
			text = text.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			if (File.Exists(text))
			{
				Stream stream = null;
				int num = 0;
				while (stream == null && num < 100)
				{
					try
					{
						stream = File.Open(text, FileMode.Open, FileAccess.Read, FileShare.Read);
					}
					catch (IOException ex)
					{
						errorMessage = ex.ToString() + "   numberOfTries=" + num;
						Thread.Sleep(50);
					}
					num++;
				}
				if (stream != null)
				{
					if (ReadWaveHeader(stream, out var outChannelCount, out var outSampleRate, out var outBitsPerSample, out var outLengthOfWaveformDatablockInBytes, out var outBytePositionOfWaveformData) == PsaiResult.OK)
					{
						ChannelCount = outChannelCount;
						SampleRate = outSampleRate;
						LengthOfWaveformDataInBytes = outLengthOfWaveformDatablockInBytes;
						BitsPerSample = outBitsPerSample;
						ByteIndexOfWaveformDataWithinAudioFile = outBytePositionOfWaveformData;
						TotalLengthInSamples = CalculateTotalLengthInSamples(outLengthOfWaveformDatablockInBytes, outBitsPerSample, outChannelCount);
						errorMessage = "";
						result = true;
					}
					else
					{
						errorMessage = "ERROR: file '" + text + "' contains an unsupported format. Please make sure your audio files are standard RIFF WAV files with up to 16 bits / 44.1kHz.";
					}
					stream.Close();
					return result;
				}
				errorMessage = "ERROR: audio file '" + text + "' could not be opened. ";
				return false;
			}
		}
		errorMessage = "ERROR: audio file '" + fullPathToAudioFile + "' could not be found. Please make sure that all audio files reside within a subfolder of your project directory";
		return false;
	}

	public static bool SeekChunkInWaveHeader(ref BinaryReader reader, string chunk)
	{
		if (chunk.Length != 4)
		{
			return false;
		}
		Queue<byte> queue = new Queue<byte>(4);
		try
		{
			while (reader.BaseStream.CanRead)
			{
				byte b = 0;
				do
				{
					b = reader.ReadByte();
					queue.Enqueue(b);
					if (queue.Count > 4)
					{
						queue.Dequeue();
					}
				}
				while (b != chunk[3]);
				if (Encoding.ASCII.GetString(queue.ToArray()).Equals(chunk))
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return false;
		}
		return false;
	}

	public static PsaiResult ReadWaveHeader(Stream stream, out int outChannelCount, out int outSampleRate, out int outBitsPerSample, out int outLengthOfWaveformDatablockInBytes, out long outBytePositionOfWaveformData)
	{
		outChannelCount = 0;
		outBitsPerSample = 0;
		outSampleRate = 0;
		outLengthOfWaveformDatablockInBytes = 0;
		outBytePositionOfWaveformData = 0L;
		BinaryReader reader = new BinaryReader(stream);
		if (new string(reader.ReadChars(4)) != "RIFF")
		{
			reader.Close();
			return PsaiResult.format_error;
		}
		reader.ReadInt32();
		if (new string(reader.ReadChars(4)) != "WAVE")
		{
			reader.Close();
			return PsaiResult.format_error;
		}
		try
		{
			if (!SeekChunkInWaveHeader(ref reader, "fmt "))
			{
				Console.WriteLine(".wave file corrupt! format-chunk not found.");
				reader.Close();
				return PsaiResult.format_error;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			reader.Close();
			return PsaiResult.format_error;
		}
		long position = reader.BaseStream.Position;
		int num = reader.ReadInt32();
		reader.ReadInt16();
		int num2 = reader.ReadInt16();
		int num3 = reader.ReadInt32();
		reader.ReadInt32();
		reader.ReadInt16();
		int num4 = reader.ReadInt16();
		if (num4 > 16)
		{
			Console.WriteLine("OpenAL does not support playback of 24 bits. Please convert to 16 bits.");
			return PsaiResult.output_format_error;
		}
		long offset = position + num + 4;
		reader.BaseStream.Seek(offset, SeekOrigin.Begin);
		if (!SeekChunkInWaveHeader(ref reader, "data"))
		{
			reader.BaseStream.Seek(0L, SeekOrigin.Begin);
			if (!SeekChunkInWaveHeader(ref reader, "data"))
			{
				Console.WriteLine("wave file corrupt! no 'data' chunk found!");
				reader.Close();
				return PsaiResult.format_error;
			}
		}
		int num5 = stream.ReadByte() + stream.ReadByte() * 256 + stream.ReadByte() * 65536 + stream.ReadByte() * 16777216;
		outLengthOfWaveformDatablockInBytes = num5;
		outBytePositionOfWaveformData = stream.Position;
		outChannelCount = num2;
		outBitsPerSample = num4;
		outSampleRate = num3;
		reader.Close();
		return PsaiResult.OK;
	}

	public static byte[] LoadWaveformDataToByteArray(string fullFilePath, long byteIndexOfWaveformDataWithinAudioFile, int lengthOfWaveformDataInBytes)
	{
		Stream stream = null;
		int num = 0;
		while (stream == null && num < 100)
		{
			string text = fullFilePath.Replace('/', Path.DirectorySeparatorChar);
			text = text.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			try
			{
				stream = File.Open(text, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (IOException ex)
			{
				Console.WriteLine(ex.ToString() + "   numberOfTries=" + num);
				Thread.Sleep(50);
			}
			num++;
		}
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		byte[] result = null;
		using (BinaryReader binaryReader = new BinaryReader(stream))
		{
			try
			{
				binaryReader.BaseStream.Position = byteIndexOfWaveformDataWithinAudioFile;
				result = binaryReader.ReadBytes(lengthOfWaveformDataInBytes);
			}
			catch (Exception ex2)
			{
				Console.WriteLine("Exception reading Audio Data! e=" + ex2.ToString() + "  " + ex2.Message);
			}
		}
		stream.Close();
		return result;
	}

	public object Clone()
	{
		return MemberwiseClone();
	}
}
