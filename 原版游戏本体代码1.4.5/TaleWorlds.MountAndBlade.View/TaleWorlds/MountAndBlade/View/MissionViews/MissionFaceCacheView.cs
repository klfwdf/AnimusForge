using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionFaceCacheView : MissionView
{
	private struct CacheRecord
	{
		public BodyProperties BodyProperties;

		public int CacheID;

		public FaceGenerationParams FaceParamsForSimilarity;

		public ArmorComponent.HairCoverTypes HairCover;

		public ArmorComponent.BeardCoverTypes BeardCover;
	}

	private int _totalFaceBudget = 250;

	private int _uniqueCacheIndex;

	private float _currentSimilarityThreshold;

	private float _currentRandomSwitchChance;

	private KeyValuePair<float, float>[] _comprasionThresholdsWrtEmptyBudget;

	private List<CacheRecord> _alreadyAssignedFaces = new List<CacheRecord>();

	private MBFastRandom _randomGenerator;

	public MissionFaceCacheView()
	{
		_totalFaceBudget = (NativeConfig.CharacterDetail + 1) * 100;
		_randomGenerator = new MBFastRandom((uint)(Time.ApplicationTime * 73f));
		_currentSimilarityThreshold = 25f;
		_comprasionThresholdsWrtEmptyBudget = new KeyValuePair<float, float>[5];
		_comprasionThresholdsWrtEmptyBudget[0] = new KeyValuePair<float, float>(0.2f, 100f);
		_comprasionThresholdsWrtEmptyBudget[1] = new KeyValuePair<float, float>(0.4f, 200f);
		_comprasionThresholdsWrtEmptyBudget[2] = new KeyValuePair<float, float>(0.6f, 450f);
		_comprasionThresholdsWrtEmptyBudget[3] = new KeyValuePair<float, float>(0.8f, 750f);
		_comprasionThresholdsWrtEmptyBudget[4] = new KeyValuePair<float, float>(1f, 5000f);
	}

	public override void OnPreMissionTick(float dt)
	{
	}

	public override void OnBehaviorInitialize()
	{
		Mission.Current.OnComputeTroopBodyProperties += GetRandomBodyPropertyForTroop;
	}

	public override void OnMissionScreenFinalize()
	{
		Mission.Current.OnComputeTroopBodyProperties -= GetRandomBodyPropertyForTroop;
		FaceGen.FlushFaceCache();
	}

	private float ComputeSimilarityOfFace(FaceGenerationParams f0, FaceGenerationParams f1, ArmorComponent.HairCoverTypes hairCover1, ArmorComponent.HairCoverTypes hairCover2, ArmorComponent.BeardCoverTypes beardCover1, ArmorComponent.BeardCoverTypes beardCover2)
	{
		float num = 0f;
		if (hairCover1 != hairCover2)
		{
			num += 1000000f;
		}
		if (beardCover1 != beardCover2)
		{
			num += 1000000f;
		}
		if (f0.CurrentBeard != f1.CurrentBeard)
		{
			num += 10f;
		}
		if (f0.CurrentHair != f1.CurrentHair)
		{
			num += 10f;
		}
		if (f0.CurrentEyebrow != f1.CurrentEyebrow)
		{
			num += 10f;
		}
		if (f0.CurrentRace != f1.CurrentRace)
		{
			num += 10f;
		}
		if (f0.CurrentGender != f1.CurrentGender)
		{
			num += 1000f;
		}
		if (f0.CurrentFaceTexture != f1.CurrentFaceTexture)
		{
			num += 10f;
		}
		if (f0.CurrentMouthTexture != f1.CurrentMouthTexture)
		{
			num += 5f;
		}
		if (f0.CurrentFaceTattoo != f1.CurrentFaceTattoo)
		{
			num += 250f;
		}
		float num2 = 32.5f;
		for (int i = 0; i < f0.KeyWeights.Length; i++)
		{
			num += MathF.Abs(f0.KeyWeights[i] - f1.KeyWeights[i]) * num2;
		}
		return num;
	}

	private int CheckForSimilarFacesFromCache(FaceGenerationParams newFaceGen, ArmorComponent.HairCoverTypes hairCoverType, ArmorComponent.BeardCoverTypes beardCoverType)
	{
		int num = -1;
		float num2 = 1E+09f;
		for (int i = 0; i < _uniqueCacheIndex; i++)
		{
			float num3 = ComputeSimilarityOfFace(newFaceGen, _alreadyAssignedFaces[i].FaceParamsForSimilarity, hairCoverType, _alreadyAssignedFaces[i].HairCover, beardCoverType, _alreadyAssignedFaces[i].BeardCover);
			if (num3 < _currentSimilarityThreshold && _randomGenerator.NextFloat() > _currentRandomSwitchChance)
			{
				return i;
			}
			if (num2 < num3 && (_randomGenerator.NextFloat() > _currentRandomSwitchChance || num == -1))
			{
				num = i;
				num2 = num3;
			}
		}
		if (_uniqueCacheIndex == _totalFaceBudget)
		{
			return num;
		}
		return -1;
	}

	private void UpdateFaceSimilarityThreshold()
	{
		float num = (float)_uniqueCacheIndex / (float)_totalFaceBudget;
		float num2 = 0.4f;
		float num3 = 0.7f;
		float value = (num - num2) / (num3 - num2);
		value = MathF.Clamp(value, 0f, 1f);
		_currentRandomSwitchChance = 0.37f * value;
		if (num < _comprasionThresholdsWrtEmptyBudget[0].Key)
		{
			_currentSimilarityThreshold = _comprasionThresholdsWrtEmptyBudget[0].Value;
			_currentRandomSwitchChance = 0f;
			return;
		}
		for (int i = 1; i < _comprasionThresholdsWrtEmptyBudget.Count(); i++)
		{
			if (_comprasionThresholdsWrtEmptyBudget[i].Key > num)
			{
				float value2 = _comprasionThresholdsWrtEmptyBudget[i - 1].Value;
				float value3 = _comprasionThresholdsWrtEmptyBudget[i].Value;
				float key = _comprasionThresholdsWrtEmptyBudget[i - 1].Key;
				float key2 = _comprasionThresholdsWrtEmptyBudget[i].Key;
				float value4 = (num - key) / (key2 - key);
				value4 = MathF.Clamp(value4, 0f, 1f);
				_currentSimilarityThreshold = value2 + (value3 - value2) * value4;
				break;
			}
		}
	}

	private BodyProperties GetRandomBodyPropertyForTroop(AgentBuildData agentBuildData, BasicCharacterObject characterObject, Equipment equipment, int seed)
	{
		if (characterObject.IsHero)
		{
			return characterObject.GetBodyProperties(equipment, seed);
		}
		ArmorComponent.HairCoverTypes hairCoverType = equipment.HairCoverType;
		ArmorComponent.BeardCoverTypes beardCoverType = equipment.BeardCoverType;
		bool earsAreHidden = equipment.EarsAreHidden;
		bool mouthIsHidden = equipment.MouthIsHidden;
		BodyProperties bodyProperties = characterObject.GetBodyProperties(equipment, seed);
		FaceGenerationParams faceGenerationParams = default(FaceGenerationParams);
		MBBodyProperties.GetParamsFromKey(ref faceGenerationParams, bodyProperties, earsAreHidden, mouthIsHidden);
		int num = CheckForSimilarFacesFromCache(faceGenerationParams, hairCoverType, beardCoverType);
		if (num != -1)
		{
			BodyProperties result = new BodyProperties(bodyProperties.DynamicProperties, _alreadyAssignedFaces[num].BodyProperties.StaticProperties);
			agentBuildData.FaceCacheId = _alreadyAssignedFaces[num].CacheID;
			return result;
		}
		agentBuildData.FaceCacheId = _uniqueCacheIndex;
		CacheRecord item = new CacheRecord
		{
			BodyProperties = bodyProperties,
			CacheID = _uniqueCacheIndex,
			FaceParamsForSimilarity = faceGenerationParams,
			HairCover = hairCoverType,
			BeardCover = beardCoverType
		};
		_alreadyAssignedFaces.Add(item);
		_uniqueCacheIndex++;
		MBDebug.Print($"GetRandomBodyPropertyForTroop::Unique troop index: {_uniqueCacheIndex}\n");
		UpdateFaceSimilarityThreshold();
		return bodyProperties;
	}
}
