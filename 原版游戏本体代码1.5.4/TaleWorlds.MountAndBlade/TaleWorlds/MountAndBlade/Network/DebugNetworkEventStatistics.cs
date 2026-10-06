using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network;

public static class DebugNetworkEventStatistics
{
	public class TotalEventData
	{
		public readonly int TotalPackets;

		public readonly int TotalUpload;

		public readonly int TotalConstantsUpload;

		public readonly int TotalReliableUpload;

		public readonly int TotalReplicationUpload;

		public readonly int TotalUnreliableUpload;

		public readonly int TotalOtherUpload;

		public bool HasData => TotalUpload > 0;

		protected bool Equals(TotalEventData other)
		{
			if (TotalPackets == other.TotalPackets && TotalUpload == other.TotalUpload && TotalConstantsUpload == other.TotalConstantsUpload && TotalReliableUpload == other.TotalReliableUpload && TotalReplicationUpload == other.TotalReplicationUpload && TotalUnreliableUpload == other.TotalUnreliableUpload)
			{
				return TotalOtherUpload == other.TotalOtherUpload;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj.GetType() == GetType())
			{
				return Equals((TotalEventData)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((TotalPackets * 397) ^ TotalUpload) * 397) ^ TotalConstantsUpload) * 397) ^ TotalReliableUpload) * 397) ^ TotalReplicationUpload) * 397) ^ TotalUnreliableUpload) * 397) ^ TotalOtherUpload;
		}

		public TotalEventData()
		{
		}

		public TotalEventData(int totalPackets, int totalUpload, int totalConstants, int totalReliable, int totalReplication, int totalUnreliable)
		{
			TotalPackets = totalPackets;
			TotalUpload = totalUpload;
			TotalConstantsUpload = totalConstants;
			TotalReliableUpload = totalReliable;
			TotalReplicationUpload = totalReplication;
			TotalUnreliableUpload = totalUnreliable;
			TotalOtherUpload = totalUpload - (totalConstants + totalReliable + totalReplication + totalUnreliable);
		}

		public static TotalEventData operator -(TotalEventData d1, TotalEventData d2)
		{
			return new TotalEventData(d1.TotalPackets - d2.TotalPackets, d1.TotalUpload - d2.TotalUpload, d1.TotalConstantsUpload - d2.TotalConstantsUpload, d1.TotalReliableUpload - d2.TotalReliableUpload, d1.TotalReplicationUpload - d2.TotalReplicationUpload, d1.TotalUnreliableUpload - d2.TotalUnreliableUpload);
		}

		public static bool operator ==(TotalEventData d1, TotalEventData d2)
		{
			if (d1.TotalPackets == d2.TotalPackets && d1.TotalUpload == d2.TotalUpload && d1.TotalConstantsUpload == d2.TotalConstantsUpload && d1.TotalReliableUpload == d2.TotalReliableUpload && d1.TotalReplicationUpload == d2.TotalReplicationUpload)
			{
				return d1.TotalUnreliableUpload == d2.TotalUnreliableUpload;
			}
			return false;
		}

		public static bool operator !=(TotalEventData d1, TotalEventData d2)
		{
			return !(d1 == d2);
		}
	}

	private class PerEventData : IComparable<PerEventData>
	{
		public string Name;

		public int DataSize;

		public int TotalDataSize;

		public int Count;

		public int CompareTo(PerEventData other)
		{
			return other.TotalDataSize - TotalDataSize;
		}
	}

	public class PerSecondEventData
	{
		public readonly int TotalUploadPerSecond;

		public readonly int ConstantsUploadPerSecond;

		public readonly int ReliableUploadPerSecond;

		public readonly int ReplicationUploadPerSecond;

		public readonly int UnreliableUploadPerSecond;

		public readonly int OtherUploadPerSecond;

		public PerSecondEventData(int totalUploadPerSecond, int constantsUploadPerSecond, int reliableUploadPerSecond, int replicationUploadPerSecond, int unreliableUploadPerSecond, int otherUploadPerSecond)
		{
			TotalUploadPerSecond = totalUploadPerSecond;
			ConstantsUploadPerSecond = constantsUploadPerSecond;
			ReliableUploadPerSecond = reliableUploadPerSecond;
			ReplicationUploadPerSecond = replicationUploadPerSecond;
			UnreliableUploadPerSecond = unreliableUploadPerSecond;
			OtherUploadPerSecond = otherUploadPerSecond;
		}
	}

	private class TotalData
	{
		public float TotalTime;

		public int TotalFrameCount;

		public int TotalCount;

		public int TotalDataSize;
	}

	private static TotalData _totalData;

	private static int _curEventType;

	private static Dictionary<int, PerEventData> _statistics;

	private static int _samplesPerSecond;

	public static int MaxGraphPointCount;

	private static bool _showUploadDataText;

	private static bool _useAbsoluteMaximum;

	private static float _collectSampleCheck;

	private static float _collectFpsSampleCheck;

	private static float _curMaxGraphHeight;

	private static float _targetMaxGraphHeight;

	private static float _currMaxLossGraphHeight;

	private static float _targetMaxLossGraphHeight;

	private static PerSecondEventData UploadPerSecondEventData;

	private static readonly Queue<TotalEventData> _eventSamples;

	private static readonly Queue<float> _lossSamples;

	private static TotalEventData _prevEventData;

	private static TotalEventData _currEventData;

	private static readonly List<float> _fpsSamplesUntilNextSampling;

	private static readonly Queue<float> _fpsSamples;

	private static bool _useImgui;

	public static bool TrackFps;

	public static int SamplesPerSecond
	{
		get
		{
			return _samplesPerSecond;
		}
		set
		{
			_samplesPerSecond = value;
			MaxGraphPointCount = value * 5;
		}
	}

	public static bool IsActive { get; private set; }

	public static event Action<IEnumerable<TotalEventData>> OnEventDataUpdated;

	public static event Action<PerSecondEventData> OnPerSecondEventDataUpdated;

	public static event Action<IEnumerable<float>> OnFPSEventUpdated;

	public static event Action OnOpenExternalMonitor;

	static DebugNetworkEventStatistics()
	{
		_totalData = new TotalData();
		_curEventType = -1;
		_statistics = new Dictionary<int, PerEventData>();
		_showUploadDataText = false;
		_useAbsoluteMaximum = false;
		_collectSampleCheck = 0f;
		_collectFpsSampleCheck = 0f;
		_curMaxGraphHeight = 0f;
		_targetMaxGraphHeight = 0f;
		_currMaxLossGraphHeight = 0f;
		_targetMaxLossGraphHeight = 0f;
		_eventSamples = new Queue<TotalEventData>();
		_lossSamples = new Queue<float>();
		_prevEventData = new TotalEventData();
		_currEventData = new TotalEventData();
		_fpsSamplesUntilNextSampling = new List<float>();
		_fpsSamples = new Queue<float>();
		_useImgui = !GameNetwork.IsDedicatedServer;
		TrackFps = false;
		SamplesPerSecond = 10;
	}

	internal static void StartEvent(string eventName, int eventType)
	{
		if (IsActive)
		{
			_curEventType = eventType;
			if (!_statistics.ContainsKey(_curEventType))
			{
				_statistics.Add(_curEventType, new PerEventData
				{
					Name = eventName
				});
			}
			_statistics[_curEventType].Count++;
			_totalData.TotalCount++;
		}
	}

	internal static void EndEvent()
	{
		if (IsActive)
		{
			PerEventData perEventData = _statistics[_curEventType];
			perEventData.DataSize = perEventData.TotalDataSize / perEventData.Count;
			_curEventType = -1;
		}
	}

	internal static void AddDataToStatistic(int bitCount)
	{
		if (IsActive)
		{
			_statistics[_curEventType].TotalDataSize += bitCount;
			_totalData.TotalDataSize += bitCount;
		}
	}

	public static void OpenExternalMonitor()
	{
		if (DebugNetworkEventStatistics.OnOpenExternalMonitor != null)
		{
			DebugNetworkEventStatistics.OnOpenExternalMonitor();
		}
	}

	public static void ControlActivate()
	{
		IsActive = true;
	}

	public static void ControlDeactivate()
	{
		IsActive = false;
	}

	public static void ControlJustDump()
	{
		DumpData();
	}

	public static void ControlDumpAll()
	{
		DumpData();
		DumpReplicationData();
	}

	public static void ControlClear()
	{
		Clear();
	}

	public static void ClearNetGraphs()
	{
		_eventSamples.Clear();
		_lossSamples.Clear();
		_prevEventData = new TotalEventData();
		_currEventData = new TotalEventData();
		_collectSampleCheck = 0f;
	}

	public static void ClearFpsGraph()
	{
		_fpsSamplesUntilNextSampling.Clear();
		_fpsSamples.Clear();
		_collectFpsSampleCheck = 0f;
	}

	public static void ControlClearAll()
	{
		Clear();
		ClearFpsGraph();
		ClearNetGraphs();
		ClearReplicationData();
	}

	public static void ControlDumpReplicationData()
	{
		DumpReplicationData();
		ClearReplicationData();
	}

	public static void EndTick(float dt)
	{
		if (_useImgui && Input.DebugInput.IsHotKeyPressed("DebugNetworkEventStatisticsHotkeyToggleActive"))
		{
			ToggleActive();
			if (IsActive)
			{
				Imgui.NewFrame();
			}
		}
		if (!IsActive)
		{
			return;
		}
		_totalData.TotalTime += dt;
		_totalData.TotalFrameCount++;
		if (_useImgui)
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin("Network panel");
			if (Imgui.Button("Disable Network Panel"))
			{
				ToggleActive();
			}
			Imgui.Separator();
			if (Imgui.Button("Show Upload Data (screen)"))
			{
				_showUploadDataText = !_showUploadDataText;
			}
			Imgui.Separator();
			if (Imgui.Button("Clear Data"))
			{
				Clear();
			}
			if (Imgui.Button("Dump Data (console)"))
			{
				DumpData();
			}
			Imgui.Separator();
			if (Imgui.Button("Clear Replication Data"))
			{
				ClearReplicationData();
			}
			if (Imgui.Button("Dump Replication Data (console)"))
			{
				DumpReplicationData();
			}
			if (Imgui.Button("Dump & Clear Replication Data (console)"))
			{
				DumpReplicationData();
				ClearReplicationData();
			}
			if (_showUploadDataText)
			{
				Imgui.Separator();
				ShowUploadData();
			}
			Imgui.End();
		}
		if (!IsActive)
		{
			return;
		}
		CollectFpsSample(dt);
		_collectSampleCheck += dt;
		if (_collectSampleCheck >= 1f / (float)SamplesPerSecond)
		{
			_currEventData = GetCurrentEventData();
			if (_currEventData.HasData && _prevEventData.HasData && _currEventData != _prevEventData)
			{
				_lossSamples.Enqueue(GameNetwork.GetAveragePacketLossRatio());
				_eventSamples.Enqueue(_currEventData - _prevEventData);
				_prevEventData = _currEventData;
				if (_eventSamples.Count > MaxGraphPointCount)
				{
					_eventSamples.Dequeue();
					_lossSamples.Dequeue();
				}
				if (_eventSamples.Count >= SamplesPerSecond)
				{
					List<TotalEventData> range = _eventSamples.ToList().GetRange(_eventSamples.Count - SamplesPerSecond, SamplesPerSecond);
					UploadPerSecondEventData = new PerSecondEventData(range.Sum((TotalEventData x) => x.TotalUpload), range.Sum((TotalEventData x) => x.TotalConstantsUpload), range.Sum((TotalEventData x) => x.TotalReliableUpload), range.Sum((TotalEventData x) => x.TotalReplicationUpload), range.Sum((TotalEventData x) => x.TotalUnreliableUpload), range.Sum((TotalEventData x) => x.TotalOtherUpload));
					if (DebugNetworkEventStatistics.OnPerSecondEventDataUpdated != null)
					{
						DebugNetworkEventStatistics.OnPerSecondEventDataUpdated(UploadPerSecondEventData);
					}
				}
				if (DebugNetworkEventStatistics.OnEventDataUpdated != null)
				{
					DebugNetworkEventStatistics.OnEventDataUpdated(_eventSamples.ToList());
				}
				_collectSampleCheck -= 1f / (float)SamplesPerSecond;
			}
		}
		if (_useImgui)
		{
			Imgui.Begin("Network Graph panel");
			float[] array = _eventSamples.Select((TotalEventData x) => (float)x.TotalUpload / 8192f).ToArray();
			float num = ((array.Length != 0) ? array.Max() : 0f);
			_targetMaxGraphHeight = (_useAbsoluteMaximum ? TaleWorlds.Library.MathF.Max(num, _targetMaxGraphHeight) : num);
			float amount = MBMath.ClampFloat(3f * dt, 0f, 1f);
			_curMaxGraphHeight = MBMath.Lerp(_curMaxGraphHeight, _targetMaxGraphHeight, amount);
			if (UploadPerSecondEventData != null)
			{
				Imgui.Text("Taking " + SamplesPerSecond + " samples per second. Total KiB per second:" + (float)UploadPerSecondEventData.TotalUploadPerSecond / 8192f);
			}
			float[] values = _eventSamples.Select((TotalEventData x) => (float)x.TotalConstantsUpload / 8192f).ToArray();
			Imgui.PlotLines("", values, _eventSamples.Count, 0, "Constants upload (in KiB)", 0f, _curMaxGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _curMaxGraphHeight);
			float[] values2 = _eventSamples.Select((TotalEventData x) => (float)x.TotalReliableUpload / 8192f).ToArray();
			Imgui.PlotLines("", values2, _eventSamples.Count, 0, "Reliable upload (in KiB)", 0f, _curMaxGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _curMaxGraphHeight);
			float[] values3 = _eventSamples.Select((TotalEventData x) => (float)x.TotalReplicationUpload / 8192f).ToArray();
			Imgui.PlotLines("", values3, _eventSamples.Count, 0, "Replication upload (in KiB)", 0f, _curMaxGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _curMaxGraphHeight);
			float[] values4 = _eventSamples.Select((TotalEventData x) => (float)x.TotalUnreliableUpload / 8192f).ToArray();
			Imgui.PlotLines("", values4, _eventSamples.Count, 0, "Unreliable upload (in KiB)", 0f, _curMaxGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _curMaxGraphHeight);
			float[] values5 = _eventSamples.Select((TotalEventData x) => (float)x.TotalOtherUpload / 8192f).ToArray();
			Imgui.PlotLines("", values5, _eventSamples.Count, 0, "Other upload (in KiB)", 0f, _curMaxGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _curMaxGraphHeight);
			Imgui.Separator();
			float[] values6 = _eventSamples.Select((TotalEventData x) => (float)x.TotalUpload / (float)x.TotalPackets / 8f).ToArray();
			Imgui.PlotLines("", values6, _eventSamples.Count, 0, "Data per package (in B)", 0f, 1400f, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + 1400);
			Imgui.Separator();
			float num2 = ((_lossSamples.Count > 0) ? _lossSamples.Max() : 0f);
			_targetMaxLossGraphHeight = (_useAbsoluteMaximum ? TaleWorlds.Library.MathF.Max(num2, _targetMaxLossGraphHeight) : num2);
			float amount2 = MBMath.ClampFloat(3f * dt, 0f, 1f);
			_currMaxLossGraphHeight = MBMath.Lerp(_currMaxLossGraphHeight, _targetMaxLossGraphHeight, amount2);
			Imgui.PlotLines("", _lossSamples.ToArray(), _lossSamples.Count, 0, "Averaged loss ratio", 0f, _currMaxLossGraphHeight, 400f, 45f, 4);
			Imgui.SameLine();
			Imgui.Text("Y-range: " + _currMaxLossGraphHeight);
			Imgui.Checkbox("Use absolute Maximum", ref _useAbsoluteMaximum);
			Imgui.End();
		}
		Imgui.EndMainThreadScope();
	}

	private static void CollectFpsSample(float dt)
	{
		if (!TrackFps)
		{
			return;
		}
		float fps = Utilities.GetFps();
		if (!float.IsInfinity(fps) && !float.IsNegativeInfinity(fps) && !float.IsNaN(fps))
		{
			_fpsSamplesUntilNextSampling.Add(fps);
		}
		_collectFpsSampleCheck += dt;
		if (!(_collectFpsSampleCheck >= 1f / (float)SamplesPerSecond))
		{
			return;
		}
		if (_fpsSamplesUntilNextSampling.Count > 0)
		{
			_fpsSamples.Enqueue(_fpsSamplesUntilNextSampling.Min());
			_fpsSamplesUntilNextSampling.Clear();
			if (_fpsSamples.Count > MaxGraphPointCount)
			{
				_fpsSamples.Dequeue();
			}
			DebugNetworkEventStatistics.OnFPSEventUpdated?.Invoke(_fpsSamples.ToList());
		}
		_collectFpsSampleCheck -= 1f / (float)SamplesPerSecond;
	}

	private static void ToggleActive()
	{
		IsActive = !IsActive;
	}

	private static void Clear()
	{
		_totalData = new TotalData();
		_statistics = new Dictionary<int, PerEventData>();
		GameNetwork.ResetDebugUploads();
		_curEventType = -1;
	}

	private static void DumpData()
	{
		MBStringBuilder outStr = default(MBStringBuilder);
		outStr.Initialize(16, "DumpData");
		outStr.AppendLine();
		outStr.AppendLine("///GENERAL DATA///");
		outStr.AppendLine("Total elapsed time: " + _totalData.TotalTime + " seconds.");
		outStr.AppendLine("Total frame count: " + _totalData.TotalFrameCount);
		outStr.AppendLine("Total avg packet count: " + (int)(_totalData.TotalTime / 60f));
		outStr.AppendLine("Total event data size: " + _totalData.TotalDataSize + " bits.");
		outStr.AppendLine("Total event count: " + _totalData.TotalCount);
		outStr.AppendLine();
		outStr.AppendLine("///ALL DATA///");
		List<PerEventData> list = new List<PerEventData>();
		list.AddRange(_statistics.Values);
		list.Sort();
		foreach (PerEventData item in list)
		{
			outStr.AppendLine("Event name: " + item.Name);
			outStr.AppendLine("\tEvent size (for one event): " + item.DataSize + " bits.");
			outStr.AppendLine("\tTotal count: " + item.Count);
			outStr.AppendLine("\tTotal size: " + item.TotalDataSize + "bits | ~" + (item.TotalDataSize / 8 + ((item.TotalDataSize % 8 != 0) ? 1 : 0)) + " bytes.");
			outStr.AppendLine("\tTotal count per frame: " + (float)item.Count / (float)_totalData.TotalFrameCount);
			outStr.AppendLine("\tTotal size per frame: " + (float)item.TotalDataSize / (float)_totalData.TotalFrameCount + " bits per frame.");
			outStr.AppendLine();
		}
		GetFormattedDebugUploadDataOutput(ref outStr);
		outStr.AppendLine("NetworkEventStaticticsLogLength: " + outStr.Length + "\n");
		MBDebug.Print(outStr.ToStringAndRelease());
	}

	private static void GetFormattedDebugUploadDataOutput(ref MBStringBuilder outStr)
	{
		GameNetwork.DebugNetworkPacketStatisticsStruct networkStatisticsStruct = default(GameNetwork.DebugNetworkPacketStatisticsStruct);
		GameNetwork.DebugNetworkPositionCompressionStatisticsStruct posStatisticsStruct = default(GameNetwork.DebugNetworkPositionCompressionStatisticsStruct);
		GameNetwork.GetDebugUploadsInBits(ref networkStatisticsStruct, ref posStatisticsStruct);
		outStr.AppendLine("REAL NETWORK UPLOAD PERCENTS");
		if (networkStatisticsStruct.TotalUpload == 0)
		{
			outStr.AppendLine("Total Upload is ZERO");
			return;
		}
		int num = networkStatisticsStruct.TotalUpload - (networkStatisticsStruct.TotalConstantsUpload + networkStatisticsStruct.TotalReliableEventUpload + networkStatisticsStruct.TotalReplicationUpload + networkStatisticsStruct.TotalUnreliableEventUpload);
		if (num == networkStatisticsStruct.TotalUpload)
		{
			outStr.AppendLine("USE_DEBUG_NETWORK_PACKET_PERCENTS not defined!");
		}
		else
		{
			outStr.AppendLine("\tAverage Ping: " + networkStatisticsStruct.AveragePingTime);
			outStr.AppendLine("\tTime out period: " + networkStatisticsStruct.TimeOutPeriod);
			outStr.AppendLine("\tLost Percent: " + networkStatisticsStruct.LostPercent);
			outStr.AppendLine("\tlost_count: " + networkStatisticsStruct.LostCount);
			outStr.AppendLine("\ttotal_count_on_lost_check: " + networkStatisticsStruct.TotalCountOnLostCheck);
			outStr.AppendLine("\tround_trip_time: " + networkStatisticsStruct.RoundTripTime);
			float num2 = networkStatisticsStruct.TotalUpload;
			float num3 = 1f / (float)networkStatisticsStruct.TotalPackets;
			outStr.AppendLine("\tConstants Upload: percent: " + (float)networkStatisticsStruct.TotalConstantsUpload / num2 * 100f + "; size in bits: " + (float)networkStatisticsStruct.TotalConstantsUpload * num3 + ";");
			outStr.AppendLine("\tReliable Upload: percent: " + (float)networkStatisticsStruct.TotalReliableEventUpload / num2 * 100f + "; size in bits: " + (float)networkStatisticsStruct.TotalReliableEventUpload * num3 + ";");
			outStr.AppendLine("\tReplication Upload: percent: " + (float)networkStatisticsStruct.TotalReplicationUpload / num2 * 100f + "; size in bits: " + (float)networkStatisticsStruct.TotalReplicationUpload * num3 + ";");
			outStr.AppendLine("\tUnreliable Upload: percent: " + (float)networkStatisticsStruct.TotalUnreliableEventUpload / num2 * 100f + "; size in bits: " + (float)networkStatisticsStruct.TotalUnreliableEventUpload * num3 + ";");
			outStr.AppendLine("\tOthers (headers, ack etc.) Upload: percent: " + (float)num / num2 * 100f + "; size in bits: " + (float)num * num3 + ";");
			_ = posStatisticsStruct.totalPositionCoarseBitCountX + posStatisticsStruct.totalPositionCoarseBitCountY + posStatisticsStruct.totalPositionCoarseBitCountZ;
			_ = 1f / (float)networkStatisticsStruct.TotalCellPriorityChecks;
			outStr.AppendLine("\n\tTotal PPS: " + (float)networkStatisticsStruct.TotalPackets / _totalData.TotalTime + "; bps: " + (float)networkStatisticsStruct.TotalUpload / _totalData.TotalTime + ";");
		}
		outStr.AppendLine("\n\tTotal packets: " + networkStatisticsStruct.TotalPackets + "; bits per packet: " + (float)networkStatisticsStruct.TotalUpload / (float)networkStatisticsStruct.TotalPackets + ";");
		outStr.AppendLine("Total Upload: " + networkStatisticsStruct.TotalUpload + " in bits");
	}

	private static void ShowUploadData()
	{
		MBStringBuilder outStr = default(MBStringBuilder);
		outStr.Initialize(16, "ShowUploadData");
		GetFormattedDebugUploadDataOutput(ref outStr);
		string[] array = outStr.ToStringAndRelease().Split(new char[1] { '\n' });
		for (int i = 0; i < array.Length; i++)
		{
			Imgui.Text(array[i]);
		}
	}

	private static TotalEventData GetCurrentEventData()
	{
		GameNetwork.DebugNetworkPacketStatisticsStruct networkStatisticsStruct = default(GameNetwork.DebugNetworkPacketStatisticsStruct);
		GameNetwork.DebugNetworkPositionCompressionStatisticsStruct posStatisticsStruct = default(GameNetwork.DebugNetworkPositionCompressionStatisticsStruct);
		GameNetwork.GetDebugUploadsInBits(ref networkStatisticsStruct, ref posStatisticsStruct);
		return new TotalEventData(networkStatisticsStruct.TotalPackets, networkStatisticsStruct.TotalUpload, networkStatisticsStruct.TotalConstantsUpload, networkStatisticsStruct.TotalReliableEventUpload, networkStatisticsStruct.TotalReplicationUpload, networkStatisticsStruct.TotalUnreliableEventUpload);
	}

	private static void DumpReplicationData()
	{
		GameNetwork.PrintReplicationTableStatistics();
	}

	private static void ClearReplicationData()
	{
		GameNetwork.ClearReplicationTableStatistics();
	}
}
