using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache;

public abstract class NavigationCache<T> where T : ISettlementDataHolder
{
	private Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, (float, float)>> _settlementToSettlementDistanceWithLandRatio;

	private Dictionary<T, MBReadOnlyList<T>> _fortificationNeighbors;

	private Dictionary<int, NavigationCacheElement<T>> _closestSettlementsToFaceIndices;

	protected const float AgentRadius = 0.3f;

	protected const float ExtraCostMultiplierForNeighborDetection = 2f;

	public float MaximumDistanceBetweenTwoConnectedSettlements { get; protected set; }

	protected MobileParty.NavigationType _navigationType { get; private set; }

	protected NavigationCache(MobileParty.NavigationType navigationType)
	{
		_navigationType = navigationType;
		_settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, (float, float)>>();
		_fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>();
		_closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>();
	}

	protected void FinalizeCacheInitialization()
	{
		if (_fortificationNeighbors == null || _fortificationNeighbors.AnyQ((KeyValuePair<T, MBReadOnlyList<T>> x) => x.Value.Count == 0))
		{
			Debug.FailedAssert("There is settlement with zero neighbor in neighbor cache, this should not be happening, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\NavigationCache.cs", "FinalizeCacheInitialization", 44);
			GenerateNeighborSettlementsCache();
		}
	}

	public static void CopyTo<T1>(NavigationCache<T1> source, NavigationCache<T> target) where T1 : ISettlementDataHolder
	{
		target._navigationType = source._navigationType;
		target.MaximumDistanceBetweenTwoConnectedSettlements = source.MaximumDistanceBetweenTwoConnectedSettlements;
		target._settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, (float, float)>>(source._settlementToSettlementDistanceWithLandRatio.Count);
		foreach (KeyValuePair<NavigationCacheElement<T1>, Dictionary<NavigationCacheElement<T1>, (float, float)>> item in source._settlementToSettlementDistanceWithLandRatio)
		{
			NavigationCacheElement<T> cacheElement = target.GetCacheElement(target.GetCacheElement(item.Key.StringId), item.Key.IsPortUsed);
			Dictionary<NavigationCacheElement<T>, (float, float)> dictionary = new Dictionary<NavigationCacheElement<T>, (float, float)>(item.Value.Count);
			target._settlementToSettlementDistanceWithLandRatio.Add(cacheElement, dictionary);
			foreach (KeyValuePair<NavigationCacheElement<T1>, (float, float)> item2 in item.Value)
			{
				NavigationCacheElement<T> cacheElement2 = target.GetCacheElement(target.GetCacheElement(item2.Key.StringId), item2.Key.IsPortUsed);
				dictionary.Add(cacheElement2, item2.Value);
			}
		}
		target._fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>(source._fortificationNeighbors.Count);
		foreach (KeyValuePair<T1, MBReadOnlyList<T1>> fortificationNeighbor in source._fortificationNeighbors)
		{
			T cacheElement3 = target.GetCacheElement(fortificationNeighbor.Key.StringId);
			List<T> list = new List<T>(fortificationNeighbor.Value.Count);
			target._fortificationNeighbors.Add(cacheElement3, list.ToMBList());
			foreach (T1 item3 in fortificationNeighbor.Value)
			{
				T cacheElement4 = target.GetCacheElement(item3.StringId);
				list.Add(cacheElement4);
			}
		}
		target._closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>();
		foreach (KeyValuePair<int, NavigationCacheElement<T1>> closestSettlementsToFaceIndex in source._closestSettlementsToFaceIndices)
		{
			NavigationCacheElement<T> cacheElement5 = target.GetCacheElement(target.GetCacheElement(closestSettlementsToFaceIndex.Value.StringId), closestSettlementsToFaceIndex.Value.IsPortUsed);
			target._closestSettlementsToFaceIndices.Add(closestSettlementsToFaceIndex.Key, cacheElement5);
		}
	}

	public MBReadOnlyList<T> GetNeighbors(T settlement)
	{
		if (!_fortificationNeighbors.TryGetValue(settlement, out var value))
		{
			return new MBReadOnlyList<T>();
		}
		return value;
	}

	public T GetClosestSettlementToFaceIndex(int faceId, out bool isAtSea)
	{
		if (_closestSettlementsToFaceIndices.TryGetValue(faceId, out var value))
		{
			isAtSea = value.IsPortUsed;
			return value.Settlement;
		}
		isAtSea = false;
		return default(T);
	}

	public void GenerateCacheData()
	{
		GenerateClosestSettlementToFaceCache();
		GenerateSettlementToSettlementDistanceCache();
		GenerateNeighborSettlementsCache();
	}

	protected float GetSettlementToSettlementDistanceWithLandRatio(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, out float landRatio)
	{
		NavigationCacheElement<T>.Sort(ref settlement1, ref settlement2, out var _);
		if (!_settlementToSettlementDistanceWithLandRatio.TryGetValue(settlement1, out var value))
		{
			value = new Dictionary<NavigationCacheElement<T>, (float, float)>();
			_settlementToSettlementDistanceWithLandRatio.Add(settlement1, value);
		}
		if (!value.TryGetValue(settlement2, out var value2))
		{
			float realDistanceAndLandRatioBetweenSettlements = GetRealDistanceAndLandRatioBetweenSettlements(settlement1, settlement2, out landRatio);
			SetSettlementToSettlementDistanceWithLandRatio(settlement1, settlement2, realDistanceAndLandRatioBetweenSettlements, landRatio);
			value2 = (realDistanceAndLandRatioBetweenSettlements, landRatio);
		}
		landRatio = value2.Item2;
		return value2.Item1;
	}

	protected void SetSettlementToSettlementDistanceWithLandRatio(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, float distance, float landRatio)
	{
		NavigationCacheElement<T>.Sort(ref settlement1, ref settlement2, out var _);
		if (!_settlementToSettlementDistanceWithLandRatio.TryGetValue(settlement1, out var value))
		{
			value = new Dictionary<NavigationCacheElement<T>, (float, float)>();
			_settlementToSettlementDistanceWithLandRatio.Add(settlement1, value);
		}
		if (value.TryGetValue(settlement2, out var _))
		{
			Debug.FailedAssert("Element already exists", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\NavigationCache.cs", "SetSettlementToSettlementDistanceWithLandRatio", 215);
		}
		else
		{
			value.Add(settlement2, (distance, landRatio));
		}
		if (distance < 100000000f && distance > MaximumDistanceBetweenTwoConnectedSettlements)
		{
			MaximumDistanceBetweenTwoConnectedSettlements = distance;
		}
	}

	protected void AddNeighbor(T settlement1, T settlement2)
	{
		bool flag = false;
		foreach (KeyValuePair<T, MBReadOnlyList<T>> fortificationNeighbor in _fortificationNeighbors)
		{
			if ((fortificationNeighbor.Key.StringId.Equals(settlement1.StringId) && fortificationNeighbor.Value.Contains(settlement2)) || (fortificationNeighbor.Key.StringId.Equals(settlement2.StringId) && fortificationNeighbor.Value.Contains(settlement1)))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (!_fortificationNeighbors.TryGetValue(settlement1, out var value))
			{
				_fortificationNeighbors.Add(settlement1, new MBReadOnlyList<T>());
			}
			MBList<T> mBList;
			if (value != null)
			{
				mBList = new MBList<T>(value.Count + 1);
				mBList.AddRange(value);
			}
			else
			{
				mBList = new MBList<T>(1);
			}
			mBList.Add(settlement2);
			_fortificationNeighbors[settlement1] = mBList;
			if (!_fortificationNeighbors.TryGetValue(settlement2, out var value2))
			{
				_fortificationNeighbors.Add(settlement2, new MBReadOnlyList<T>());
			}
			if (value2 != null)
			{
				mBList = new MBList<T>(value2.Count + 1);
				mBList.AddRange(value2);
			}
			else
			{
				mBList = new MBList<T>(1);
			}
			mBList.Add(settlement1);
			_fortificationNeighbors[settlement2] = mBList;
		}
	}

	protected void SetClosestSettlementToFaceIndex(int faceId, NavigationCacheElement<T> settlement)
	{
		_closestSettlementsToFaceIndices.Add(faceId, settlement);
	}

	protected abstract float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, out float landRatio);

	protected abstract T GetCacheElement(string settlementId);

	protected abstract NavigationCacheElement<T> GetCacheElement(T settlement, bool isPortUsed);

	protected float GetLandRatioOfPath(NavigationPath path, Vec2 startPosition)
	{
		float num = 0f;
		float num2 = 0f;
		if (path.Size > 1)
		{
			List<Vec2> list = new List<Vec2>(path.PathPoints);
			list.Insert(0, startPosition);
			for (int i = 0; i < list.Count - 1; i++)
			{
				Vec2 vec = list[i];
				Vec2 vec2 = list[i + 1];
				if (vec2 == Vec2.Zero)
				{
					break;
				}
				Vec2 vec3 = vec2 - vec;
				float num3 = vec3.Length / 0.5f;
				vec3.Normalize();
				for (int j = 0; (float)j < num3 - 1f; j++)
				{
					Vec2 position = vec + vec3 * j * 0.5f;
					Vec2 vec4 = vec + vec3 * (j + 1) * 0.5f;
					GetFaceRecordForPoint(position, out var isOnRegion);
					GetFaceRecordForPoint(vec4, out var isOnRegion2);
					float num4 = position.Distance(vec4);
					if (isOnRegion2 && isOnRegion)
					{
						num += num4;
					}
					else if (isOnRegion2 != isOnRegion)
					{
						num += num4 / 2f;
					}
					num2 += num4;
				}
			}
			return MBMath.ClampFloat(num / num2, 0f, 1f);
		}
		GetFaceRecordForPoint(startPosition, out var isOnRegion3);
		GetFaceRecordForPoint(path[0], out var isOnRegion4);
		if (isOnRegion4 == isOnRegion3)
		{
			if (isOnRegion4)
			{
				return 1f;
			}
			return 0f;
		}
		return 0.5f;
	}

	protected abstract void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1);

	protected void GenerateClosestSettlementToFaceCache()
	{
		int navMeshFaceCount = GetNavMeshFaceCount();
		for (int i = 0; i < navMeshFaceCount; i++)
		{
			Debug.Print($"Face-Settlement cache creation progress % {i * 100 / navMeshFaceCount}     {_navigationType}");
			Vec2 navMeshFaceCenterPosition = GetNavMeshFaceCenterPosition(i);
			PathFaceRecord faceRecordAtIndex = GetFaceRecordAtIndex(i);
			bool isPort = false;
			T closestSettlementToPosition = GetClosestSettlementToPosition(navMeshFaceCenterPosition, faceRecordAtIndex, GetExcludedFaceIds(), GetAllRegisteredSettlements(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1(), float.MaxValue, out isPort);
			if (!object.Equals(closestSettlementToPosition, default(T)))
			{
				SetClosestSettlementToFaceIndex(i, new NavigationCacheElement<T>(closestSettlementToPosition, isPort));
			}
		}
	}

	protected abstract int GetNavMeshFaceCount();

	protected abstract Vec2 GetNavMeshFaceCenterPosition(int faceIndex);

	protected abstract PathFaceRecord GetFaceRecordAtIndex(int faceIndex);

	protected abstract int[] GetExcludedFaceIds();

	protected abstract int GetRegionSwitchCostTo0();

	protected abstract int GetRegionSwitchCostTo1();

	protected void GenerateSettlementToSettlementDistanceCache()
	{
		List<T> allRegisteredSettlements = GetAllRegisteredSettlements();
		for (int i = 0; i < allRegisteredSettlements.Count; i++)
		{
			Debug.Print($"Settlement to settlement cache creation index {i},    total count: {allRegisteredSettlements.Count}     {_navigationType}");
			T settlement = allRegisteredSettlements[i];
			for (int j = ((_navigationType == MobileParty.NavigationType.All) ? i : (i + 1)); j < allRegisteredSettlements.Count; j++)
			{
				T settlement2 = allRegisteredSettlements[j];
				if (_navigationType == MobileParty.NavigationType.Default)
				{
					AddClosestEntrancePairBase(settlement, isPort1: false, settlement2, isPort2: false);
				}
				else if (_navigationType == MobileParty.NavigationType.Naval)
				{
					if (settlement.HasPort && settlement2.HasPort)
					{
						AddClosestEntrancePairBase(settlement, isPort1: true, settlement2, isPort2: true);
					}
				}
				else if (_navigationType == MobileParty.NavigationType.All)
				{
					AddClosestEntrancePairBase(settlement, isPort1: false, settlement2, isPort2: false);
					if (settlement.HasPort && settlement2.HasPort)
					{
						AddClosestEntrancePairBase(settlement, isPort1: true, settlement2, isPort2: true);
					}
					if (settlement2.HasPort)
					{
						AddClosestEntrancePairBase(settlement, isPort1: false, settlement2, isPort2: true);
					}
					if (settlement.HasPort)
					{
						AddClosestEntrancePairBase(settlement, isPort1: true, settlement2, isPort2: false);
					}
				}
			}
		}
	}

	private void AddClosestEntrancePairBase(T settlement1, bool isPort1, T settlement2, bool isPort2)
	{
		NavigationCacheElement<T> settlement3 = GetCacheElement(settlement1, isPort1);
		NavigationCacheElement<T> settlement4 = GetCacheElement(settlement2, isPort2);
		float landRatio;
		float realDistanceAndLandRatioBetweenSettlements = GetRealDistanceAndLandRatioBetweenSettlements(settlement3, settlement4, out landRatio);
		float landRatio2;
		float realDistanceAndLandRatioBetweenSettlements2 = GetRealDistanceAndLandRatioBetweenSettlements(settlement4, settlement3, out landRatio2);
		float num = (realDistanceAndLandRatioBetweenSettlements + realDistanceAndLandRatioBetweenSettlements2) * 0.5f;
		if (num > 0f)
		{
			float landRatio3 = 1f;
			if (_navigationType == MobileParty.NavigationType.Naval)
			{
				landRatio3 = 0f;
			}
			else if (_navigationType == MobileParty.NavigationType.All)
			{
				landRatio3 = landRatio;
			}
			NavigationCacheElement<T>.Sort(ref settlement3, ref settlement4, out var isPairChanged);
			if (isPairChanged)
			{
				landRatio3 = landRatio2;
			}
			SetSettlementToSettlementDistanceWithLandRatio(settlement3, settlement4, num, landRatio3);
		}
	}

	protected void GenerateNeighborSettlementsCache()
	{
		_fortificationNeighbors.Clear();
		List<T> updatedSettlementsForNeighborDetection = GetUpdatedSettlementsForNeighborDetection(GetAllRegisteredSettlements());
		for (int i = 0; i < updatedSettlementsForNeighborDetection.Count - 1; i++)
		{
			Debug.Print($"Neighbor cache progress for navigation {_navigationType}, current index: {i}  - total count: {updatedSettlementsForNeighborDetection.Count}");
			T settlement = updatedSettlementsForNeighborDetection[i];
			if (!settlement.IsFortification)
			{
				continue;
			}
			for (int j = i + 1; j < updatedSettlementsForNeighborDetection.Count; j++)
			{
				T settlement2 = updatedSettlementsForNeighborDetection[j];
				if (settlement2.IsFortification && CheckBeingNeighbor(updatedSettlementsForNeighborDetection, settlement, settlement2))
				{
					AddNeighbor(settlement, settlement2);
				}
			}
		}
	}

	private void CheckNeighbourAux(List<T> settlementsToConsider, T settlement1, T settlement2, bool useGate1, bool useGate2, ref float distance, ref bool isNeighbour)
	{
		float foundDistance;
		bool flag = CheckBeingNeighbor(settlementsToConsider, settlement1, settlement2, useGate1, useGate2, out foundDistance);
		if (foundDistance < distance)
		{
			distance = foundDistance;
			isNeighbour = flag;
		}
	}

	protected bool CheckBeingNeighbor(List<T> settlementsToConsider, T settlement1, T settlement2)
	{
		float distance = float.MaxValue;
		bool isNeighbour = false;
		if (_navigationType == MobileParty.NavigationType.Default || _navigationType == MobileParty.NavigationType.All)
		{
			CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, useGate1: true, useGate2: true, ref distance, ref isNeighbour);
			CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, useGate1: true, useGate2: true, ref distance, ref isNeighbour);
		}
		if (_navigationType == MobileParty.NavigationType.Naval || _navigationType == MobileParty.NavigationType.All)
		{
			bool hasPort = settlement1.HasPort;
			bool hasPort2 = settlement2.HasPort;
			if (hasPort)
			{
				CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, useGate1: false, useGate2: true, ref distance, ref isNeighbour);
				CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, useGate1: true, useGate2: false, ref distance, ref isNeighbour);
			}
			if (hasPort2)
			{
				CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, useGate1: true, useGate2: false, ref distance, ref isNeighbour);
				CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, useGate1: false, useGate2: true, ref distance, ref isNeighbour);
			}
			if (hasPort2 && hasPort)
			{
				CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, useGate1: false, useGate2: false, ref distance, ref isNeighbour);
				CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, useGate1: false, useGate2: false, ref distance, ref isNeighbour);
			}
		}
		return isNeighbour;
	}

	protected abstract List<T> GetAllRegisteredSettlements();

	protected List<T> GetUpdatedSettlementsForNeighborDetection(List<T> settlements)
	{
		if (_navigationType == MobileParty.NavigationType.Naval)
		{
			return settlements.Where((T x) => x.IsFortification && x.HasPort).ToList();
		}
		return settlements.Where((T x) => x.IsFortification).ToList();
	}

	protected abstract bool CheckBeingNeighbor(List<T> settlementsToConsider, T settlement1, T settlement2, bool useGate1, bool useGate2, out float foundDistance);

	protected abstract float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, T currentSettlementToLook, out bool isPort);

	protected T GetClosestSettlementToPosition(Vec2 checkPosition, PathFaceRecord currentFaceRecord, int[] excludedFaceIds, List<T> settlementRecords, int regionSwitchCostTo0, int regionSwitchCostTo1, float minPathScoreEverFound, out bool isPort)
	{
		isPort = false;
		T result = default(T);
		foreach (T item in GetClosestSettlementsToPositionInCache(checkPosition, settlementRecords))
		{
			bool isPort2;
			float realPathDistanceFromPositionToSettlement = GetRealPathDistanceFromPositionToSettlement(checkPosition, currentFaceRecord, minPathScoreEverFound * 2f, item, out isPort2);
			if (realPathDistanceFromPositionToSettlement < minPathScoreEverFound)
			{
				minPathScoreEverFound = realPathDistanceFromPositionToSettlement;
				result = item;
				isPort = isPort2;
			}
		}
		return result;
	}

	protected abstract IEnumerable<T> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<T> settlements);

	public abstract void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc);

	public bool GetSettlementsDistanceCacheFileForCapability(string moduleId, out string filePath)
	{
		string text = ModuleHelper.GetModuleFullPath(moduleId) + "ModuleData/DistanceCaches";
		string text2 = _navigationType.ToString();
		filePath = text + "/settlements_distance_cache_" + text2 + ".bin";
		bool num = File.Exists(filePath);
		if (num)
		{
			Debug.Print($"Found distance cache at: {moduleId}, {text}, {_navigationType}");
		}
		return num;
	}

	public void Serialize(string path)
	{
		System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(File.Open(path, FileMode.Create));
		GetSceneXmlCrcValues(out var sceneXmlCrc, out var sceneNavigationMeshCrc);
		binaryWriter.Write(sceneXmlCrc);
		binaryWriter.Write(sceneNavigationMeshCrc);
		binaryWriter.Write(_settlementToSettlementDistanceWithLandRatio.Count);
		foreach (KeyValuePair<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, (float, float)>> item in _settlementToSettlementDistanceWithLandRatio)
		{
			binaryWriter.Write(item.Key.StringId);
			binaryWriter.Write(item.Key.IsPortUsed);
			binaryWriter.Write(item.Value.Count);
			foreach (KeyValuePair<NavigationCacheElement<T>, (float, float)> item2 in item.Value)
			{
				binaryWriter.Write(item2.Key.StringId);
				binaryWriter.Write(item2.Key.IsPortUsed);
				binaryWriter.Write(item2.Value.Item1);
				if (_navigationType == MobileParty.NavigationType.All)
				{
					binaryWriter.Write(item2.Value.Item2);
				}
			}
		}
		binaryWriter.Write(_fortificationNeighbors.SumQ((KeyValuePair<T, MBReadOnlyList<T>> x) => x.Value.Count));
		foreach (KeyValuePair<T, MBReadOnlyList<T>> fortificationNeighbor in _fortificationNeighbors)
		{
			string stringId = fortificationNeighbor.Key.StringId;
			foreach (T item3 in fortificationNeighbor.Value)
			{
				binaryWriter.Write(stringId);
				binaryWriter.Write(item3.StringId);
			}
		}
		binaryWriter.Write(_closestSettlementsToFaceIndices.Count);
		foreach (KeyValuePair<int, NavigationCacheElement<T>> closestSettlementsToFaceIndex in _closestSettlementsToFaceIndices)
		{
			binaryWriter.Write(closestSettlementsToFaceIndex.Key);
			binaryWriter.Write(closestSettlementsToFaceIndex.Value.StringId);
			binaryWriter.Write(closestSettlementsToFaceIndex.Value.IsPortUsed);
		}
		binaryWriter.Close();
	}

	public void Deserialize(string path)
	{
		Debug.Print("Reading SettlementsDistanceCacheFilePath: " + path);
		System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
		binaryReader.ReadUInt32();
		binaryReader.ReadUInt32();
		Campaign.Current.MapSceneWrapper.GetSceneXmlCrc();
		Campaign.Current.MapSceneWrapper.GetSceneNavigationMeshCrc();
		int num = binaryReader.ReadInt32();
		_settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, (float, float)>>(num);
		for (int i = 0; i < num; i++)
		{
			T cacheElement = GetCacheElement(binaryReader.ReadString());
			bool isPortUsed = binaryReader.ReadBoolean();
			NavigationCacheElement<T> settlement = GetCacheElement(cacheElement, isPortUsed);
			int num2 = binaryReader.ReadInt32();
			_settlementToSettlementDistanceWithLandRatio.Add(settlement, new Dictionary<NavigationCacheElement<T>, (float, float)>(num2));
			for (int j = 0; j < num2; j++)
			{
				T cacheElement2 = GetCacheElement(binaryReader.ReadString());
				bool isPortUsed2 = binaryReader.ReadBoolean();
				NavigationCacheElement<T> settlement2 = GetCacheElement(cacheElement2, isPortUsed2);
				NavigationCacheElement<T>.Sort(ref settlement, ref settlement2, out var _);
				float distance = binaryReader.ReadSingle();
				float landRatio = ((_navigationType == MobileParty.NavigationType.Naval) ? 0f : 1f);
				if (_navigationType == MobileParty.NavigationType.All)
				{
					landRatio = binaryReader.ReadSingle();
				}
				SetSettlementToSettlementDistanceWithLandRatio(settlement, settlement2, distance, landRatio);
			}
		}
		int num3 = binaryReader.ReadInt32();
		_fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>(num3);
		for (int k = 0; k < num3; k++)
		{
			T cacheElement3 = GetCacheElement(binaryReader.ReadString());
			T cacheElement4 = GetCacheElement(binaryReader.ReadString());
			AddNeighbor(cacheElement3, cacheElement4);
		}
		int num4 = binaryReader.ReadInt32();
		_closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>(num4);
		for (int l = 0; l < num4; l++)
		{
			int faceId = binaryReader.ReadInt32();
			T cacheElement5 = GetCacheElement(binaryReader.ReadString());
			bool isPortUsed3 = binaryReader.ReadBoolean();
			NavigationCacheElement<T> cacheElement6 = GetCacheElement(cacheElement5, isPortUsed3);
			SetClosestSettlementToFaceIndex(faceId, cacheElement6);
		}
		binaryReader.Close();
	}
}
