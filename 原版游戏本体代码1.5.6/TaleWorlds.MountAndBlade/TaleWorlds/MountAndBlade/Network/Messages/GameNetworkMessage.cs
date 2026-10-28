using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Network.Messages;

public abstract class GameNetworkMessage
{
	public delegate bool ClientMessageHandlerDelegate<T>(NetworkCommunicator peer, T message) where T : GameNetworkMessage;

	public delegate void ServerMessageHandlerDelegate<T>(T message) where T : GameNetworkMessage;

	private static readonly Encoding StringEncoding = new UTF8Encoding();

	private static CompressionInfo.Integer TestValueCompressionInfo = new CompressionInfo.Integer(0, 3);

	private const int ConstTestValue = 5;

	public int MessageId { get; set; }

	public static bool IsClientMissionOver
	{
		get
		{
			if (GameNetwork.IsClient && !NetworkMain.GameClient.IsInGame)
			{
				return !NetworkMain.CommunityClient.IsInGame;
			}
			return false;
		}
	}

	internal void Write()
	{
		DebugNetworkEventStatistics.StartEvent(GetType().Name, MessageId);
		WriteIntToPacket(MessageId, GameNetwork.IsClientOrReplay ? CompressionBasic.NetworkComponentEventTypeFromClientCompressionInfo : CompressionBasic.NetworkComponentEventTypeFromServerCompressionInfo);
		OnWrite();
		WriteIntToPacket(5, TestValueCompressionInfo);
		DebugNetworkEventStatistics.EndEvent();
	}

	protected abstract void OnWrite();

	internal bool Read()
	{
		bool result = OnRead();
		bool bufferReadValid = true;
		if (ReadIntFromPacket(TestValueCompressionInfo, ref bufferReadValid) != 5)
		{
			throw new MBNetworkBitException(GetType().Name);
		}
		return result;
	}

	protected abstract bool OnRead();

	internal MultiplayerMessageFilter GetLogFilter()
	{
		return OnGetLogFilter();
	}

	protected abstract MultiplayerMessageFilter OnGetLogFilter();

	internal string GetLogFormat()
	{
		return OnGetLogFormat();
	}

	protected abstract string OnGetLogFormat();

	public static bool ReadBoolFromPacket(ref bool bufferReadValid)
	{
		CompressionInfo.Integer compressionInfo = new CompressionInfo.Integer(0, 1);
		int output = 0;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadIntFromPacket(ref compressionInfo, out output);
		return output != 0;
	}

	public static void WriteBoolToPacket(bool value)
	{
		CompressionInfo.Integer compressionInfo = new CompressionInfo.Integer(0, 1);
		MBAPI.IMBNetwork.WriteIntToPacket(value ? 1 : 0, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static int ReadIntFromPacket(CompressionInfo.Integer compressionInfo, ref bool bufferReadValid)
	{
		int output = 0;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadIntFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteIntToPacket(int value, CompressionInfo.Integer compressionInfo)
	{
		MBAPI.IMBNetwork.WriteIntToPacket(value, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static uint ReadUintFromPacket(CompressionInfo.UnsignedInteger compressionInfo, ref bool bufferReadValid)
	{
		uint output = 0u;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadUintFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteUintToPacket(uint value, CompressionInfo.UnsignedInteger compressionInfo)
	{
		MBAPI.IMBNetwork.WriteUintToPacket(value, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static long ReadLongFromPacket(CompressionInfo.LongInteger compressionInfo, ref bool bufferReadValid)
	{
		long output = 0L;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadLongFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteLongToPacket(long value, CompressionInfo.LongInteger compressionInfo)
	{
		MBAPI.IMBNetwork.WriteLongToPacket(value, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static ulong ReadUlongFromPacket(CompressionInfo.UnsignedLongInteger compressionInfo, ref bool bufferReadValid)
	{
		ulong output = 0uL;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadUlongFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteUlongToPacket(ulong value, CompressionInfo.UnsignedLongInteger compressionInfo)
	{
		MBAPI.IMBNetwork.WriteUlongToPacket(value, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static float ReadFloatFromPacket(CompressionInfo.Float compressionInfo, ref bool bufferReadValid)
	{
		float output = 0f;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadFloatFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteFloatToPacket(float value, CompressionInfo.Float compressionInfo)
	{
		MBAPI.IMBNetwork.WriteFloatToPacket(value, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static string ReadStringFromPacket(ref bool bufferReadValid)
	{
		byte[] array = new byte[1024];
		int count = ReadByteArrayFromPacket(array, 0, 1024, ref bufferReadValid);
		return StringEncoding.GetString(array, 0, count);
	}

	public static void WriteStringToPacket(string value)
	{
		byte[] array = (string.IsNullOrEmpty(value) ? new byte[0] : StringEncoding.GetBytes(value));
		WriteByteArrayToPacket(array, 0, array.Length);
	}

	public static int ReadByteArrayFromPacket(byte[] buffer, int offset, int bufferCapacity, ref bool bufferReadValid)
	{
		return MBAPI.IMBNetwork.ReadByteArrayFromPacket(buffer, offset, bufferCapacity, ref bufferReadValid);
	}

	public static void WriteBannerCodeToPacket(string bannerCode)
	{
		Banner.TryGetBannerDataFromCode(bannerCode, out var bannerDataList);
		WriteIntToPacket(bannerDataList.Count, CompressionBasic.BannerDataCountCompressionInfo);
		for (int i = 0; i < bannerDataList.Count; i++)
		{
			BannerData bannerData = bannerDataList[i];
			WriteIntToPacket(bannerData.MeshId, CompressionBasic.BannerDataMeshIdCompressionInfo);
			WriteIntToPacket(bannerData.ColorId, CompressionBasic.BannerDataColorIndexCompressionInfo);
			WriteIntToPacket(bannerData.ColorId2, CompressionBasic.BannerDataColorIndexCompressionInfo);
			WriteIntToPacket((int)bannerData.Size.X, CompressionBasic.BannerDataSizeCompressionInfo);
			WriteIntToPacket((int)bannerData.Size.Y, CompressionBasic.BannerDataSizeCompressionInfo);
			WriteIntToPacket((int)bannerData.Position.X, CompressionBasic.BannerDataSizeCompressionInfo);
			WriteIntToPacket((int)bannerData.Position.Y, CompressionBasic.BannerDataSizeCompressionInfo);
			WriteBoolToPacket(bannerData.DrawStroke);
			WriteBoolToPacket(bannerData.Mirror);
			WriteIntToPacket((int)bannerData.Rotation, CompressionBasic.BannerDataRotationCompressionInfo);
		}
	}

	public static string ReadBannerCodeFromPacket(ref bool bufferReadValid)
	{
		int num = ReadIntFromPacket(CompressionBasic.BannerDataCountCompressionInfo, ref bufferReadValid);
		MBList<BannerData> mBList = new MBList<BannerData>(num);
		for (int i = 0; i < num; i++)
		{
			BannerData item = new BannerData(ReadIntFromPacket(CompressionBasic.BannerDataMeshIdCompressionInfo, ref bufferReadValid), ReadIntFromPacket(CompressionBasic.BannerDataColorIndexCompressionInfo, ref bufferReadValid), ReadIntFromPacket(CompressionBasic.BannerDataColorIndexCompressionInfo, ref bufferReadValid), new Vec2(ReadIntFromPacket(CompressionBasic.BannerDataSizeCompressionInfo, ref bufferReadValid), ReadIntFromPacket(CompressionBasic.BannerDataSizeCompressionInfo, ref bufferReadValid)), new Vec2(ReadIntFromPacket(CompressionBasic.BannerDataSizeCompressionInfo, ref bufferReadValid), ReadIntFromPacket(CompressionBasic.BannerDataSizeCompressionInfo, ref bufferReadValid)), ReadBoolFromPacket(ref bufferReadValid), ReadBoolFromPacket(ref bufferReadValid), (float)ReadIntFromPacket(CompressionBasic.BannerDataRotationCompressionInfo, ref bufferReadValid) * 0.0027777778f);
			mBList.Add(item);
		}
		return Banner.GetBannerCodeFromBannerDataList(mBList);
	}

	public static void WriteByteArrayToPacket(byte[] value, int offset, int size)
	{
		MBAPI.IMBNetwork.WriteByteArrayToPacket(value, offset, size);
		DebugNetworkEventStatistics.AddDataToStatistic(MathF.Min(size, 1024) + 10);
	}

	public static MBActionSet ReadActionSetReferenceFromPacket(CompressionInfo.Integer compressionInfo, ref bool bufferReadValid)
	{
		if (bufferReadValid)
		{
			bufferReadValid = MBAPI.IMBNetwork.ReadIntFromPacket(ref compressionInfo, out var output);
			return new MBActionSet(output);
		}
		return MBActionSet.InvalidActionSet;
	}

	public static void WriteActionSetReferenceToPacket(MBActionSet actionSet, CompressionInfo.Integer compressionInfo)
	{
		MBAPI.IMBNetwork.WriteIntToPacket(actionSet.Index, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static int ReadAgentIndexFromPacket(ref bool bufferReadValid)
	{
		CompressionInfo.Integer compressionInfo = CompressionMission.AgentCompressionInfo;
		int output = -1;
		bufferReadValid = bufferReadValid && MBAPI.IMBNetwork.ReadIntFromPacket(ref compressionInfo, out output);
		return output;
	}

	public static void WriteAgentIndexToPacket(int agentIndex)
	{
		CompressionInfo.Integer compressionInfo = CompressionMission.AgentCompressionInfo;
		MBAPI.IMBNetwork.WriteIntToPacket(agentIndex, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static MBObjectBase ReadObjectReferenceFromPacket(MBObjectManager objectManager, CompressionInfo.UnsignedInteger compressionInfo, ref bool bufferReadValid)
	{
		uint num = ReadUintFromPacket(compressionInfo, ref bufferReadValid);
		if (bufferReadValid && num != 0)
		{
			MBGUID objectId = new MBGUID(num);
			return objectManager.GetObject(objectId);
		}
		return null;
	}

	public static void WriteObjectReferenceToPacket(MBObjectBase value, CompressionInfo.UnsignedInteger compressionInfo)
	{
		MBAPI.IMBNetwork.WriteUintToPacket(value?.Id.InternalValue ?? 0, ref compressionInfo);
		DebugNetworkEventStatistics.AddDataToStatistic(compressionInfo.GetNumBits());
	}

	public static VirtualPlayer ReadVirtualPlayerReferenceToPacket(ref bool bufferReadValid, bool canReturnNull = false)
	{
		int num = ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		bool flag = ReadBoolFromPacket(ref bufferReadValid);
		if ((num >= 0 && !IsClientMissionOver) & bufferReadValid)
		{
			if (!flag)
			{
				return GameNetwork.VirtualPlayers[num];
			}
			return GameNetwork.DisconnectedNetworkPeers[num].VirtualPlayer;
		}
		return null;
	}

	public static NetworkCommunicator ReadNetworkPeerReferenceFromPacket(ref bool bufferReadValid, bool canReturnNull = false)
	{
		return ReadVirtualPlayerReferenceToPacket(ref bufferReadValid, canReturnNull)?.Communicator as NetworkCommunicator;
	}

	public static void WriteVirtualPlayerReferenceToPacket(VirtualPlayer virtualPlayer)
	{
		bool value = false;
		int num = virtualPlayer?.Index ?? (-1);
		if (num >= 0 && GameNetwork.VirtualPlayers[num] != virtualPlayer)
		{
			for (int i = 0; i < GameNetwork.DisconnectedNetworkPeers.Count; i++)
			{
				if (GameNetwork.DisconnectedNetworkPeers[i].VirtualPlayer == virtualPlayer)
				{
					num = i;
					value = true;
					break;
				}
			}
		}
		WriteIntToPacket(num, CompressionBasic.PlayerCompressionInfo);
		WriteBoolToPacket(value);
	}

	public static void WriteNetworkPeerReferenceToPacket(NetworkCommunicator networkCommunicator)
	{
		WriteVirtualPlayerReferenceToPacket(networkCommunicator?.VirtualPlayer);
	}

	public static int ReadTeamIndexFromPacket(ref bool bufferReadValid)
	{
		return ReadIntFromPacket(CompressionMission.TeamCompressionInfo, ref bufferReadValid);
	}

	public static void WriteTeamIndexToPacket(int teamIndex)
	{
		WriteIntToPacket(teamIndex, CompressionMission.TeamCompressionInfo);
	}

	public static MissionObjectId ReadMissionObjectIdFromPacket(ref bool bufferReadValid)
	{
		bool createdAtRuntime = ReadBoolFromPacket(ref bufferReadValid);
		int num = ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		if (!bufferReadValid || num == -1 || IsClientMissionOver)
		{
			if (num != -1)
			{
				MBDebug.Print("Reading null MissionObject because IsClientMissionOver: " + IsClientMissionOver.ToString() + " valid read: " + bufferReadValid.ToString() + " MissionObject ID: " + num + " runtime: " + createdAtRuntime.ToString());
			}
			return new MissionObjectId(-1);
		}
		return new MissionObjectId(num, createdAtRuntime);
	}

	public static void WriteMissionObjectIdToPacket(MissionObjectId value)
	{
		WriteBoolToPacket(value.CreatedAtRuntime);
		WriteIntToPacket(value.Id, CompressionBasic.MissionObjectIDCompressionInfo);
	}

	public static Vec3 ReadVec3FromPacket(CompressionInfo.Float compressionInfo, ref bool bufferReadValid)
	{
		float x = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
		float y = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
		float z = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
		return new Vec3(x, y, z);
	}

	public static void WriteVec3ToPacket(Vec3 value, CompressionInfo.Float compressionInfo)
	{
		WriteFloatToPacket(value.x, compressionInfo);
		WriteFloatToPacket(value.y, compressionInfo);
		WriteFloatToPacket(value.z, compressionInfo);
	}

	public static Vec2 ReadVec2FromPacket(CompressionInfo.Float compressionInfo, ref bool bufferReadValid)
	{
		float a = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
		float b = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
		return new Vec2(a, b);
	}

	public static void WriteVec2ToPacket(Vec2 value, CompressionInfo.Float compressionInfo)
	{
		WriteFloatToPacket(value.x, compressionInfo);
		WriteFloatToPacket(value.y, compressionInfo);
	}

	public static Mat3 ReadRotationMatrixFromPacket(ref bool bufferReadValid)
	{
		return new Mat3(ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid), ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid), ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid));
	}

	public static void WriteRotationMatrixToPacket(Mat3 value)
	{
		WriteVec3ToPacket(value.s, CompressionBasic.UnitVectorCompressionInfo);
		WriteVec3ToPacket(value.f, CompressionBasic.UnitVectorCompressionInfo);
		WriteVec3ToPacket(value.u, CompressionBasic.UnitVectorCompressionInfo);
	}

	public static MatrixFrame ReadMatrixFrameFromPacket(ref bool bufferReadValid)
	{
		Vec3 o = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Vec3 scalingVector = ReadVec3FromPacket(CompressionBasic.ScaleCompressionInfo, ref bufferReadValid);
		MatrixFrame result = new MatrixFrame(ReadRotationMatrixFromPacket(ref bufferReadValid), in o);
		result.Scale(in scalingVector);
		return result;
	}

	public static void WriteMatrixFrameToPacket(MatrixFrame frame)
	{
		Vec3 scaleVector = frame.rotation.GetScaleVector();
		MatrixFrame matrixFrame = frame;
		matrixFrame.Scale(new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z));
		WriteVec3ToPacket(matrixFrame.origin, CompressionBasic.PositionCompressionInfo);
		WriteVec3ToPacket(scaleVector, CompressionBasic.ScaleCompressionInfo);
		WriteRotationMatrixToPacket(matrixFrame.rotation);
	}

	public static MatrixFrame ReadNonUniformTransformFromPacket(CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo, ref bool bufferReadValid)
	{
		MatrixFrame result = ReadUnitTransformFromPacket(positionCompressionInfo, quaternionCompressionInfo, ref bufferReadValid);
		Vec3 scaleAmountXYZ = ReadVec3FromPacket(CompressionBasic.ScaleCompressionInfo, ref bufferReadValid);
		result.rotation.ApplyScaleLocal(in scaleAmountXYZ);
		return result;
	}

	public static void WriteNonUniformTransformToPacket(MatrixFrame frame, CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo)
	{
		MatrixFrame frame2 = frame;
		Vec3 value = frame2.rotation.MakeUnit();
		WriteUnitTransformToPacket(frame2, positionCompressionInfo, quaternionCompressionInfo);
		WriteVec3ToPacket(value, CompressionBasic.ScaleCompressionInfo);
	}

	public static MatrixFrame ReadTransformFromPacket(CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo, ref bool bufferReadValid)
	{
		MatrixFrame result = ReadUnitTransformFromPacket(positionCompressionInfo, quaternionCompressionInfo, ref bufferReadValid);
		if (ReadBoolFromPacket(ref bufferReadValid))
		{
			float scaleAmount = ReadFloatFromPacket(CompressionBasic.ScaleCompressionInfo, ref bufferReadValid);
			result.rotation.ApplyScaleLocal(scaleAmount);
		}
		return result;
	}

	public static void WriteTransformToPacket(MatrixFrame frame, CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo)
	{
		MatrixFrame frame2 = frame;
		Vec3 vec = frame2.rotation.MakeUnit();
		WriteUnitTransformToPacket(frame2, positionCompressionInfo, quaternionCompressionInfo);
		bool num = !vec.x.ApproximatelyEqualsTo(1f, CompressionBasic.ScaleCompressionInfo.GetPrecision());
		WriteBoolToPacket(num);
		if (num)
		{
			WriteFloatToPacket(vec.x, CompressionBasic.ScaleCompressionInfo);
		}
	}

	public static MatrixFrame ReadUnitTransformFromPacket(CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo, ref bool bufferReadValid)
	{
		return new MatrixFrame
		{
			origin = ReadVec3FromPacket(positionCompressionInfo, ref bufferReadValid),
			rotation = ReadQuaternionFromPacket(quaternionCompressionInfo, ref bufferReadValid).ToMat3()
		};
	}

	public static void WriteUnitTransformToPacket(MatrixFrame frame, CompressionInfo.Float positionCompressionInfo, CompressionInfo.Float quaternionCompressionInfo)
	{
		WriteVec3ToPacket(frame.origin, positionCompressionInfo);
		WriteQuaternionToPacket(frame.rotation.ToQuaternion(), quaternionCompressionInfo);
	}

	public static Quaternion ReadQuaternionFromPacket(CompressionInfo.Float compressionInfo, ref bool bufferReadValid)
	{
		Quaternion result = default(Quaternion);
		float num = 0f;
		int num2 = ReadIntFromPacket(CompressionBasic.OmittedQuaternionComponentIndexCompressionInfo, ref bufferReadValid);
		for (int i = 0; i < 4; i++)
		{
			if (i != num2)
			{
				result[i] = ReadFloatFromPacket(compressionInfo, ref bufferReadValid);
				num += result[i] * result[i];
			}
		}
		result[num2] = MathF.Sqrt(1f - num);
		result.SafeNormalize();
		return result;
	}

	public static void WriteQuaternionToPacket(Quaternion q, CompressionInfo.Float compressionInfo)
	{
		int num = -1;
		float num2 = 0f;
		Quaternion quaternion = q;
		quaternion.SafeNormalize();
		for (int i = 0; i < 4; i++)
		{
			float num3 = MathF.Abs(quaternion[i]);
			if (num3 > num2)
			{
				num2 = num3;
				num = i;
			}
		}
		if (quaternion[num] < 0f)
		{
			quaternion.Flip();
		}
		WriteIntToPacket(num, CompressionBasic.OmittedQuaternionComponentIndexCompressionInfo);
		for (int j = 0; j < 4; j++)
		{
			if (j != num)
			{
				WriteFloatToPacket(quaternion[j], compressionInfo);
			}
		}
	}

	public static void WriteBodyPropertiesToPacket(BodyProperties bodyProperties)
	{
		WriteFloatToPacket(bodyProperties.Age, CompressionBasic.AgentAgeCompressionInfo);
		WriteFloatToPacket(bodyProperties.Weight, CompressionBasic.FaceKeyDataCompressionInfo);
		WriteFloatToPacket(bodyProperties.Build, CompressionBasic.FaceKeyDataCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart1, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart2, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart3, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart4, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart5, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart6, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart7, CompressionBasic.DebugULongNonCompressionInfo);
		WriteUlongToPacket(bodyProperties.KeyPart8, CompressionBasic.DebugULongNonCompressionInfo);
	}

	public static BodyProperties ReadBodyPropertiesFromPacket(ref bool bufferReadValid)
	{
		float age = ReadFloatFromPacket(CompressionBasic.AgentAgeCompressionInfo, ref bufferReadValid);
		float weight = ReadFloatFromPacket(CompressionBasic.FaceKeyDataCompressionInfo, ref bufferReadValid);
		float build = ReadFloatFromPacket(CompressionBasic.FaceKeyDataCompressionInfo, ref bufferReadValid);
		ulong keyPart = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart2 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart3 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart4 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart5 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart6 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart7 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong keyPart8 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			return new BodyProperties(new DynamicBodyProperties(age, weight, build), new StaticBodyProperties(keyPart, keyPart2, keyPart3, keyPart4, keyPart5, keyPart6, keyPart7, keyPart8));
		}
		return default(BodyProperties);
	}
}
