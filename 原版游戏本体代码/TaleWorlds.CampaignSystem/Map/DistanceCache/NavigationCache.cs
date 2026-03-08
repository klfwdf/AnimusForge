using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache
{
	// Token: 0x02000222 RID: 546
	public abstract class NavigationCache<T> where T : ISettlementDataHolder
	{
		// Token: 0x17000804 RID: 2052
		// (get) Token: 0x06002092 RID: 8338 RVA: 0x0008F715 File Offset: 0x0008D915
		// (set) Token: 0x06002093 RID: 8339 RVA: 0x0008F71D File Offset: 0x0008D91D
		public float MaximumDistanceBetweenTwoConnectedSettlements { get; protected set; }

		// Token: 0x17000805 RID: 2053
		// (get) Token: 0x06002094 RID: 8340 RVA: 0x0008F726 File Offset: 0x0008D926
		// (set) Token: 0x06002095 RID: 8341 RVA: 0x0008F72E File Offset: 0x0008D92E
		private protected MobileParty.NavigationType _navigationType { protected get; private set; }

		// Token: 0x06002096 RID: 8342 RVA: 0x0008F737 File Offset: 0x0008D937
		protected NavigationCache(MobileParty.NavigationType navigationType)
		{
			this._navigationType = navigationType;
			this._settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>>();
			this._fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>();
			this._closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>();
		}

		// Token: 0x06002097 RID: 8343 RVA: 0x0008F768 File Offset: 0x0008D968
		protected void FinalizeCacheInitialization()
		{
			if (this._fortificationNeighbors != null)
			{
				if (!this._fortificationNeighbors.AnyQ((KeyValuePair<T, MBReadOnlyList<T>> x) => x.Value.Count == 0))
				{
					return;
				}
			}
			Debug.FailedAssert("There is settlement with zero neighbor in neighbor cache, this should not be happening, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\NavigationCache.cs", "FinalizeCacheInitialization", 44);
			this.GenerateNeighborSettlementsCache();
		}

		// Token: 0x06002098 RID: 8344 RVA: 0x0008F7C8 File Offset: 0x0008D9C8
		public static void CopyTo<T1>(NavigationCache<T1> source, NavigationCache<T> target) where T1 : ISettlementDataHolder
		{
			target._navigationType = source._navigationType;
			target.MaximumDistanceBetweenTwoConnectedSettlements = source.MaximumDistanceBetweenTwoConnectedSettlements;
			target._settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>>(source._settlementToSettlementDistanceWithLandRatio.Count);
			foreach (KeyValuePair<NavigationCacheElement<T1>, Dictionary<NavigationCacheElement<T1>, ValueTuple<float, float>>> keyValuePair in source._settlementToSettlementDistanceWithLandRatio)
			{
				NavigationCacheElement<T> cacheElement = target.GetCacheElement(target.GetCacheElement(keyValuePair.Key.StringId), keyValuePair.Key.IsPortUsed);
				Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>> dictionary = new Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>(keyValuePair.Value.Count);
				target._settlementToSettlementDistanceWithLandRatio.Add(cacheElement, dictionary);
				foreach (KeyValuePair<NavigationCacheElement<T1>, ValueTuple<float, float>> keyValuePair2 in keyValuePair.Value)
				{
					NavigationCacheElement<T> cacheElement2 = target.GetCacheElement(target.GetCacheElement(keyValuePair2.Key.StringId), keyValuePair2.Key.IsPortUsed);
					dictionary.Add(cacheElement2, keyValuePair2.Value);
				}
			}
			target._fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>(source._fortificationNeighbors.Count);
			foreach (KeyValuePair<T1, MBReadOnlyList<T1>> keyValuePair3 in source._fortificationNeighbors)
			{
				T1 key = keyValuePair3.Key;
				T cacheElement3 = target.GetCacheElement(key.StringId);
				List<T> list = new List<T>(keyValuePair3.Value.Count);
				target._fortificationNeighbors.Add(cacheElement3, list.ToMBList<T>());
				foreach (T1 t in keyValuePair3.Value)
				{
					T cacheElement4 = target.GetCacheElement(t.StringId);
					list.Add(cacheElement4);
				}
			}
			target._closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>();
			foreach (KeyValuePair<int, NavigationCacheElement<T1>> keyValuePair4 in source._closestSettlementsToFaceIndices)
			{
				NavigationCacheElement<T> cacheElement5 = target.GetCacheElement(target.GetCacheElement(keyValuePair4.Value.StringId), keyValuePair4.Value.IsPortUsed);
				target._closestSettlementsToFaceIndices.Add(keyValuePair4.Key, cacheElement5);
			}
		}

		// Token: 0x06002099 RID: 8345 RVA: 0x0008FA88 File Offset: 0x0008DC88
		public MBReadOnlyList<T> GetNeighbors(T settlement)
		{
			MBReadOnlyList<T> result;
			if (!this._fortificationNeighbors.TryGetValue(settlement, out result))
			{
				result = new MBReadOnlyList<T>();
			}
			return result;
		}

		// Token: 0x0600209A RID: 8346 RVA: 0x0008FAAC File Offset: 0x0008DCAC
		public T GetClosestSettlementToFaceIndex(int faceId, out bool isAtSea)
		{
			NavigationCacheElement<T> navigationCacheElement;
			if (this._closestSettlementsToFaceIndices.TryGetValue(faceId, out navigationCacheElement))
			{
				isAtSea = navigationCacheElement.IsPortUsed;
				return navigationCacheElement.Settlement;
			}
			isAtSea = false;
			return default(T);
		}

		// Token: 0x0600209B RID: 8347 RVA: 0x0008FAE4 File Offset: 0x0008DCE4
		public void GenerateCacheData()
		{
			this.GenerateClosestSettlementToFaceCache();
			this.GenerateSettlementToSettlementDistanceCache();
			this.GenerateNeighborSettlementsCache();
		}

		// Token: 0x0600209C RID: 8348 RVA: 0x0008FAF8 File Offset: 0x0008DCF8
		protected float GetSettlementToSettlementDistanceWithLandRatio(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, out float landRatio)
		{
			bool flag;
			NavigationCacheElement<T>.Sort(ref settlement1, ref settlement2, out flag);
			Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>> dictionary;
			if (!this._settlementToSettlementDistanceWithLandRatio.TryGetValue(settlement1, out dictionary))
			{
				dictionary = new Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>();
				this._settlementToSettlementDistanceWithLandRatio.Add(settlement1, dictionary);
			}
			ValueTuple<float, float> valueTuple;
			if (!dictionary.TryGetValue(settlement2, out valueTuple))
			{
				float realDistanceAndLandRatioBetweenSettlements = this.GetRealDistanceAndLandRatioBetweenSettlements(settlement1, settlement2, out landRatio);
				this.SetSettlementToSettlementDistanceWithLandRatio(settlement1, settlement2, realDistanceAndLandRatioBetweenSettlements, landRatio);
				valueTuple = new ValueTuple<float, float>(realDistanceAndLandRatioBetweenSettlements, landRatio);
			}
			landRatio = valueTuple.Item2;
			return valueTuple.Item1;
		}

		// Token: 0x0600209D RID: 8349 RVA: 0x0008FB6C File Offset: 0x0008DD6C
		protected void SetSettlementToSettlementDistanceWithLandRatio(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, float distance, float landRatio)
		{
			bool flag;
			NavigationCacheElement<T>.Sort(ref settlement1, ref settlement2, out flag);
			Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>> dictionary;
			if (!this._settlementToSettlementDistanceWithLandRatio.TryGetValue(settlement1, out dictionary))
			{
				dictionary = new Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>();
				this._settlementToSettlementDistanceWithLandRatio.Add(settlement1, dictionary);
			}
			ValueTuple<float, float> valueTuple;
			if (dictionary.TryGetValue(settlement2, out valueTuple))
			{
				Debug.FailedAssert("Element already exists", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\NavigationCache.cs", "SetSettlementToSettlementDistanceWithLandRatio", 215);
			}
			dictionary.Add(settlement2, new ValueTuple<float, float>(distance, landRatio));
			if (distance < 100000000f && distance > this.MaximumDistanceBetweenTwoConnectedSettlements)
			{
				this.MaximumDistanceBetweenTwoConnectedSettlements = distance;
			}
		}

		// Token: 0x0600209E RID: 8350 RVA: 0x0008FBF4 File Offset: 0x0008DDF4
		protected void AddNeighbor(T settlement1, T settlement2)
		{
			bool flag = false;
			foreach (KeyValuePair<T, MBReadOnlyList<T>> keyValuePair in this._fortificationNeighbors)
			{
				T key = keyValuePair.Key;
				if (!key.StringId.Equals(settlement1.StringId) || !keyValuePair.Value.Contains(settlement2))
				{
					key = keyValuePair.Key;
					if (!key.StringId.Equals(settlement2.StringId) || !keyValuePair.Value.Contains(settlement1))
					{
						continue;
					}
				}
				flag = true;
				break;
			}
			if (!flag)
			{
				MBReadOnlyList<T> mbreadOnlyList;
				if (!this._fortificationNeighbors.TryGetValue(settlement1, out mbreadOnlyList))
				{
					this._fortificationNeighbors.Add(settlement1, new MBReadOnlyList<T>());
				}
				MBList<T> mblist;
				if (mbreadOnlyList != null)
				{
					mblist = new MBList<T>(mbreadOnlyList.Count + 1);
					mblist.AddRange(mbreadOnlyList);
				}
				else
				{
					mblist = new MBList<T>(1);
				}
				mblist.Add(settlement2);
				this._fortificationNeighbors[settlement1] = mblist;
				MBReadOnlyList<T> mbreadOnlyList2;
				if (!this._fortificationNeighbors.TryGetValue(settlement2, out mbreadOnlyList2))
				{
					this._fortificationNeighbors.Add(settlement2, new MBReadOnlyList<T>());
				}
				if (mbreadOnlyList2 != null)
				{
					mblist = new MBList<T>(mbreadOnlyList2.Count + 1);
					mblist.AddRange(mbreadOnlyList2);
				}
				else
				{
					mblist = new MBList<T>(1);
				}
				mblist.Add(settlement1);
				this._fortificationNeighbors[settlement2] = mblist;
			}
		}

		// Token: 0x0600209F RID: 8351 RVA: 0x0008FD7C File Offset: 0x0008DF7C
		protected void SetClosestSettlementToFaceIndex(int faceId, NavigationCacheElement<T> settlement)
		{
			this._closestSettlementsToFaceIndices.Add(faceId, settlement);
		}

		// Token: 0x060020A0 RID: 8352
		protected abstract float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<T> settlement1, NavigationCacheElement<T> settlement2, out float landRatio);

		// Token: 0x060020A1 RID: 8353
		protected abstract T GetCacheElement(string settlementId);

		// Token: 0x060020A2 RID: 8354
		protected abstract NavigationCacheElement<T> GetCacheElement(T settlement, bool isPortUsed);

		// Token: 0x060020A3 RID: 8355 RVA: 0x0008FD8C File Offset: 0x0008DF8C
		protected float GetLandRatioOfPath(NavigationPath path, Vec2 startPosition)
		{
			float num = 0f;
			float num2 = 0f;
			List<Vec2> list = new List<Vec2>(path.PathPoints);
			list.Insert(0, startPosition);
			for (int i = 0; i < list.Count - 1; i++)
			{
				Vec2 vec = list[i];
				Vec2 v = list[i + 1];
				if (v == Vec2.Zero)
				{
					break;
				}
				Vec2 v2 = v - vec;
				float num3 = v2.Length / 0.5f;
				v2.Normalize();
				int num4 = 0;
				while ((float)num4 < num3 - 1f)
				{
					Vec2 position = vec + v2 * (float)num4 * 0.5f;
					Vec2 vec2 = vec + v2 * (float)(num4 + 1) * 0.5f;
					bool flag;
					this.GetFaceRecordForPoint(position, out flag);
					bool flag2;
					this.GetFaceRecordForPoint(vec2, out flag2);
					float num5 = position.Distance(vec2);
					if (flag2 && flag)
					{
						num += num5;
					}
					else if (flag2 != flag)
					{
						num += num5 / 2f;
					}
					num2 += num5;
					num4++;
				}
			}
			if (list.Count != 1)
			{
				return MBMath.ClampFloat(num / num2, 0f, 1f);
			}
			bool flag3;
			this.GetFaceRecordForPoint(list[0], out flag3);
			if (flag3)
			{
				return 1f;
			}
			return 0f;
		}

		// Token: 0x060020A4 RID: 8356
		protected abstract void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1);

		// Token: 0x060020A5 RID: 8357 RVA: 0x0008FEF0 File Offset: 0x0008E0F0
		protected void GenerateClosestSettlementToFaceCache()
		{
			int navMeshFaceCount = this.GetNavMeshFaceCount();
			for (int i = 0; i < navMeshFaceCount; i++)
			{
				Debug.Print(string.Format("Face-Settlement cache creation progress % {0}     {1}", i * 100 / navMeshFaceCount, this._navigationType), 0, Debug.DebugColor.White, 17592186044416UL);
				Vec2 navMeshFaceCenterPosition = this.GetNavMeshFaceCenterPosition(i);
				PathFaceRecord faceRecordAtIndex = this.GetFaceRecordAtIndex(i);
				bool isPortUsed = false;
				T closestSettlementToPosition = this.GetClosestSettlementToPosition(navMeshFaceCenterPosition, faceRecordAtIndex, this.GetExcludedFaceIds(), this.GetAllRegisteredSettlements(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1(), float.MaxValue, out isPortUsed);
				if (!object.Equals(closestSettlementToPosition, default(T)))
				{
					this.SetClosestSettlementToFaceIndex(i, new NavigationCacheElement<T>(closestSettlementToPosition, isPortUsed));
				}
			}
		}

		// Token: 0x060020A6 RID: 8358
		protected abstract int GetNavMeshFaceCount();

		// Token: 0x060020A7 RID: 8359
		protected abstract Vec2 GetNavMeshFaceCenterPosition(int faceIndex);

		// Token: 0x060020A8 RID: 8360
		protected abstract PathFaceRecord GetFaceRecordAtIndex(int faceIndex);

		// Token: 0x060020A9 RID: 8361
		protected abstract int[] GetExcludedFaceIds();

		// Token: 0x060020AA RID: 8362
		protected abstract int GetRegionSwitchCostTo0();

		// Token: 0x060020AB RID: 8363
		protected abstract int GetRegionSwitchCostTo1();

		// Token: 0x060020AC RID: 8364 RVA: 0x0008FFB0 File Offset: 0x0008E1B0
		protected void GenerateSettlementToSettlementDistanceCache()
		{
			List<T> allRegisteredSettlements = this.GetAllRegisteredSettlements();
			for (int i = 0; i < allRegisteredSettlements.Count; i++)
			{
				Debug.Print(string.Format("Settlement to settlement cache creation index {0},    total count: {1}     {2}", i, allRegisteredSettlements.Count, this._navigationType), 0, Debug.DebugColor.White, 17592186044416UL);
				T settlement = allRegisteredSettlements[i];
				for (int j = i + 1; j < allRegisteredSettlements.Count; j++)
				{
					T settlement2 = allRegisteredSettlements[j];
					if (this._navigationType == MobileParty.NavigationType.Default)
					{
						this.AddClosestEntrancePairBase(settlement, false, settlement2, false);
					}
					else if (this._navigationType == MobileParty.NavigationType.Naval)
					{
						if (settlement.HasPort && settlement2.HasPort)
						{
							this.AddClosestEntrancePairBase(settlement, true, settlement2, true);
						}
					}
					else if (this._navigationType == MobileParty.NavigationType.All)
					{
						this.AddClosestEntrancePairBase(settlement, false, settlement2, false);
						if (settlement.HasPort && settlement2.HasPort)
						{
							this.AddClosestEntrancePairBase(settlement, true, settlement2, true);
						}
						if (settlement2.HasPort)
						{
							this.AddClosestEntrancePairBase(settlement, false, settlement2, true);
						}
						if (settlement.HasPort)
						{
							this.AddClosestEntrancePairBase(settlement, true, settlement2, false);
						}
					}
				}
			}
		}

		// Token: 0x060020AD RID: 8365 RVA: 0x00090100 File Offset: 0x0008E300
		private void AddClosestEntrancePairBase(T settlement1, bool isPort1, T settlement2, bool isPort2)
		{
			NavigationCacheElement<T> cacheElement = this.GetCacheElement(settlement1, isPort1);
			NavigationCacheElement<T> cacheElement2 = this.GetCacheElement(settlement2, isPort2);
			float num;
			float realDistanceAndLandRatioBetweenSettlements = this.GetRealDistanceAndLandRatioBetweenSettlements(cacheElement, cacheElement2, out num);
			float num2;
			float realDistanceAndLandRatioBetweenSettlements2 = this.GetRealDistanceAndLandRatioBetweenSettlements(cacheElement2, cacheElement, out num2);
			float num3 = (realDistanceAndLandRatioBetweenSettlements + realDistanceAndLandRatioBetweenSettlements2) * 0.5f;
			if (num3 > 0f)
			{
				float landRatio = 1f;
				if (this._navigationType == MobileParty.NavigationType.Naval)
				{
					landRatio = 0f;
				}
				else if (this._navigationType == MobileParty.NavigationType.All)
				{
					landRatio = num;
				}
				bool flag;
				NavigationCacheElement<T>.Sort(ref cacheElement, ref cacheElement2, out flag);
				if (flag)
				{
					landRatio = num2;
				}
				this.SetSettlementToSettlementDistanceWithLandRatio(cacheElement, cacheElement2, num3, landRatio);
			}
		}

		// Token: 0x060020AE RID: 8366 RVA: 0x0009018C File Offset: 0x0008E38C
		protected void GenerateNeighborSettlementsCache()
		{
			this._fortificationNeighbors.Clear();
			List<T> updatedSettlementsForNeighborDetection = this.GetUpdatedSettlementsForNeighborDetection(this.GetAllRegisteredSettlements());
			for (int i = 0; i < updatedSettlementsForNeighborDetection.Count - 1; i++)
			{
				Debug.Print(string.Format("Neighbor cache progress for navigation {0}, current index: {1}  - total count: {2}", this._navigationType, i, updatedSettlementsForNeighborDetection.Count), 0, Debug.DebugColor.White, 17592186044416UL);
				T settlement = updatedSettlementsForNeighborDetection[i];
				if (settlement.IsFortification)
				{
					for (int j = i + 1; j < updatedSettlementsForNeighborDetection.Count; j++)
					{
						T settlement2 = updatedSettlementsForNeighborDetection[j];
						if (settlement2.IsFortification && this.CheckBeingNeighbor(updatedSettlementsForNeighborDetection, settlement, settlement2))
						{
							this.AddNeighbor(settlement, settlement2);
						}
					}
				}
			}
		}

		// Token: 0x060020AF RID: 8367 RVA: 0x00090258 File Offset: 0x0008E458
		private void CheckNeighbourAux(List<T> settlementsToConsider, T settlement1, T settlement2, bool useGate1, bool useGate2, ref float distance, ref bool isNeighbour)
		{
			float num;
			bool flag = this.CheckBeingNeighbor(settlementsToConsider, settlement1, settlement2, useGate1, useGate2, out num);
			if (num < distance)
			{
				distance = num;
				isNeighbour = flag;
			}
		}

		// Token: 0x060020B0 RID: 8368 RVA: 0x00090284 File Offset: 0x0008E484
		protected bool CheckBeingNeighbor(List<T> settlementsToConsider, T settlement1, T settlement2)
		{
			float maxValue = float.MaxValue;
			bool result = false;
			if (this._navigationType == MobileParty.NavigationType.Default || this._navigationType == MobileParty.NavigationType.All)
			{
				this.CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, true, true, ref maxValue, ref result);
				this.CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, true, true, ref maxValue, ref result);
			}
			if (this._navigationType == MobileParty.NavigationType.Naval || this._navigationType == MobileParty.NavigationType.All)
			{
				bool hasPort = settlement1.HasPort;
				bool hasPort2 = settlement2.HasPort;
				if (hasPort)
				{
					this.CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, false, true, ref maxValue, ref result);
					this.CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, true, false, ref maxValue, ref result);
				}
				if (hasPort2)
				{
					this.CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, true, false, ref maxValue, ref result);
					this.CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, false, true, ref maxValue, ref result);
				}
				if (hasPort2 && hasPort)
				{
					this.CheckNeighbourAux(settlementsToConsider, settlement1, settlement2, false, false, ref maxValue, ref result);
					this.CheckNeighbourAux(settlementsToConsider, settlement2, settlement1, false, false, ref maxValue, ref result);
				}
			}
			return result;
		}

		// Token: 0x060020B1 RID: 8369
		protected abstract List<T> GetAllRegisteredSettlements();

		// Token: 0x060020B2 RID: 8370 RVA: 0x0009035C File Offset: 0x0008E55C
		protected List<T> GetUpdatedSettlementsForNeighborDetection(List<T> settlements)
		{
			if (this._navigationType == MobileParty.NavigationType.Naval)
			{
				return (from x in settlements
					where x.IsFortification && x.HasPort
					select x).ToList<T>();
			}
			return (from x in settlements
				where x.IsFortification
				select x).ToList<T>();
		}

		// Token: 0x060020B3 RID: 8371
		protected abstract bool CheckBeingNeighbor(List<T> settlementsToConsider, T settlement1, T settlement2, bool useGate1, bool useGate2, out float foundDistance);

		// Token: 0x060020B4 RID: 8372
		protected abstract float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, T currentSettlementToLook, out bool isPort);

		// Token: 0x060020B5 RID: 8373 RVA: 0x000903C8 File Offset: 0x0008E5C8
		protected T GetClosestSettlementToPosition(Vec2 checkPosition, PathFaceRecord currentFaceRecord, int[] excludedFaceIds, List<T> settlementRecords, int regionSwitchCostTo0, int regionSwitchCostTo1, float minPathScoreEverFound, out bool isPort)
		{
			isPort = false;
			T result = default(T);
			foreach (T t in this.GetClosestSettlementsToPositionInCache(checkPosition, settlementRecords))
			{
				bool flag;
				float realPathDistanceFromPositionToSettlement = this.GetRealPathDistanceFromPositionToSettlement(checkPosition, currentFaceRecord, minPathScoreEverFound * 2f, t, out flag);
				if (realPathDistanceFromPositionToSettlement < minPathScoreEverFound)
				{
					minPathScoreEverFound = realPathDistanceFromPositionToSettlement;
					result = t;
					isPort = flag;
				}
			}
			return result;
		}

		// Token: 0x060020B6 RID: 8374
		protected abstract IEnumerable<T> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<T> settlements);

		// Token: 0x060020B7 RID: 8375
		public abstract void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc);

		// Token: 0x060020B8 RID: 8376 RVA: 0x00090444 File Offset: 0x0008E644
		public bool GetSettlementsDistanceCacheFileForCapability(string moduleId, out string filePath)
		{
			string text = ModuleHelper.GetModuleFullPath(moduleId) + "ModuleData/DistanceCaches";
			string str = this._navigationType.ToString();
			filePath = text + "/settlements_distance_cache_" + str + ".bin";
			bool flag = File.Exists(filePath);
			if (flag)
			{
				Debug.Print(string.Format("Found distance cache at: {0}, {1}, {2}", moduleId, text, this._navigationType), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return flag;
		}

		// Token: 0x060020B9 RID: 8377 RVA: 0x000904BC File Offset: 0x0008E6BC
		public void Serialize(string path)
		{
			BinaryWriter binaryWriter = new BinaryWriter(File.Open(path, FileMode.Create));
			uint value;
			uint value2;
			this.GetSceneXmlCrcValues(out value, out value2);
			binaryWriter.Write(value);
			binaryWriter.Write(value2);
			binaryWriter.Write(this._settlementToSettlementDistanceWithLandRatio.Count);
			foreach (KeyValuePair<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>> keyValuePair in this._settlementToSettlementDistanceWithLandRatio)
			{
				binaryWriter.Write(keyValuePair.Key.StringId);
				binaryWriter.Write(keyValuePair.Key.IsPortUsed);
				binaryWriter.Write(keyValuePair.Value.Count);
				foreach (KeyValuePair<NavigationCacheElement<T>, ValueTuple<float, float>> keyValuePair2 in keyValuePair.Value)
				{
					binaryWriter.Write(keyValuePair2.Key.StringId);
					binaryWriter.Write(keyValuePair2.Key.IsPortUsed);
					binaryWriter.Write(keyValuePair2.Value.Item1);
					if (this._navigationType == MobileParty.NavigationType.All)
					{
						binaryWriter.Write(keyValuePair2.Value.Item2);
					}
				}
			}
			binaryWriter.Write(this._fortificationNeighbors.SumQ((KeyValuePair<T, MBReadOnlyList<T>> x) => x.Value.Count));
			foreach (KeyValuePair<T, MBReadOnlyList<T>> keyValuePair3 in this._fortificationNeighbors)
			{
				T key = keyValuePair3.Key;
				string stringId = key.StringId;
				foreach (T t in keyValuePair3.Value)
				{
					binaryWriter.Write(stringId);
					binaryWriter.Write(t.StringId);
				}
			}
			binaryWriter.Write(this._closestSettlementsToFaceIndices.Count);
			foreach (KeyValuePair<int, NavigationCacheElement<T>> keyValuePair4 in this._closestSettlementsToFaceIndices)
			{
				binaryWriter.Write(keyValuePair4.Key);
				binaryWriter.Write(keyValuePair4.Value.StringId);
				binaryWriter.Write(keyValuePair4.Value.IsPortUsed);
			}
			binaryWriter.Close();
		}

		// Token: 0x060020BA RID: 8378 RVA: 0x0009077C File Offset: 0x0008E97C
		public void Deserialize(string path)
		{
			Debug.Print("Reading SettlementsDistanceCacheFilePath: " + path, 0, Debug.DebugColor.White, 17592186044416UL);
			BinaryReader binaryReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
			binaryReader.ReadUInt32();
			binaryReader.ReadUInt32();
			Campaign.Current.MapSceneWrapper.GetSceneXmlCrc();
			Campaign.Current.MapSceneWrapper.GetSceneNavigationMeshCrc();
			int num = binaryReader.ReadInt32();
			this._settlementToSettlementDistanceWithLandRatio = new Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>>(num);
			for (int i = 0; i < num; i++)
			{
				T cacheElement = this.GetCacheElement(binaryReader.ReadString());
				bool isPortUsed = binaryReader.ReadBoolean();
				NavigationCacheElement<T> cacheElement2 = this.GetCacheElement(cacheElement, isPortUsed);
				int num2 = binaryReader.ReadInt32();
				this._settlementToSettlementDistanceWithLandRatio.Add(cacheElement2, new Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>(num2));
				for (int j = 0; j < num2; j++)
				{
					T cacheElement3 = this.GetCacheElement(binaryReader.ReadString());
					bool isPortUsed2 = binaryReader.ReadBoolean();
					NavigationCacheElement<T> cacheElement4 = this.GetCacheElement(cacheElement3, isPortUsed2);
					bool flag;
					NavigationCacheElement<T>.Sort(ref cacheElement2, ref cacheElement4, out flag);
					float distance = binaryReader.ReadSingle();
					float landRatio = ((this._navigationType == MobileParty.NavigationType.Naval) ? 0f : 1f);
					if (this._navigationType == MobileParty.NavigationType.All)
					{
						landRatio = binaryReader.ReadSingle();
					}
					this.SetSettlementToSettlementDistanceWithLandRatio(cacheElement2, cacheElement4, distance, landRatio);
				}
			}
			int num3 = binaryReader.ReadInt32();
			this._fortificationNeighbors = new Dictionary<T, MBReadOnlyList<T>>(num3);
			for (int k = 0; k < num3; k++)
			{
				T cacheElement5 = this.GetCacheElement(binaryReader.ReadString());
				T cacheElement6 = this.GetCacheElement(binaryReader.ReadString());
				this.AddNeighbor(cacheElement5, cacheElement6);
			}
			int num4 = binaryReader.ReadInt32();
			this._closestSettlementsToFaceIndices = new Dictionary<int, NavigationCacheElement<T>>(num4);
			for (int l = 0; l < num4; l++)
			{
				int faceId = binaryReader.ReadInt32();
				T cacheElement7 = this.GetCacheElement(binaryReader.ReadString());
				bool isPortUsed3 = binaryReader.ReadBoolean();
				NavigationCacheElement<T> cacheElement8 = this.GetCacheElement(cacheElement7, isPortUsed3);
				this.SetClosestSettlementToFaceIndex(faceId, cacheElement8);
			}
			binaryReader.Close();
		}

		// Token: 0x0400098C RID: 2444
		private Dictionary<NavigationCacheElement<T>, Dictionary<NavigationCacheElement<T>, ValueTuple<float, float>>> _settlementToSettlementDistanceWithLandRatio;

		// Token: 0x0400098D RID: 2445
		private Dictionary<T, MBReadOnlyList<T>> _fortificationNeighbors;

		// Token: 0x0400098E RID: 2446
		private Dictionary<int, NavigationCacheElement<T>> _closestSettlementsToFaceIndices;

		// Token: 0x0400098F RID: 2447
		protected const float AgentRadius = 0.3f;

		// Token: 0x04000990 RID: 2448
		protected const float ExtraCostMultiplierForNeighborDetection = 2f;
	}
}
