using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBFaceGen
{
	[EngineMethod("get_num_editable_deform_keys", false, null, false)]
	int GetNumEditableDeformKeys(int race, bool initialGender, float age);

	[EngineMethod("get_params_from_key", false, null, false)]
	void GetParamsFromKey(ref FaceGenerationParams faceGenerationParams, ref BodyProperties bodyProperties, bool earsAreHidden, bool mouthHidden);

	[EngineMethod("get_params_max", false, null, false)]
	void GetParamsMax(int race, int curGender, float curAge, ref int hairNum, ref int beardNum, ref int faceTextureNum, ref int mouthTextureNum, ref int faceTattooNum, ref int soundNum, ref int eyebrowNum, ref float scale);

	[EngineMethod("get_zero_probabilities", false, null, false)]
	void GetZeroProbabilities(int race, int curGender, float curAge, ref float tattooZeroProbability);

	[EngineMethod("produce_numeric_key_with_params", false, null, false)]
	void ProduceNumericKeyWithParams(ref FaceGenerationParams faceGenerationParams, bool earsAreHidden, bool mouthIsHidden, ref BodyProperties bodyProperties);

	[EngineMethod("produce_numeric_key_with_default_values", false, null, false)]
	void ProduceNumericKeyWithDefaultValues(ref BodyProperties initialBodyProperties, bool earsAreHidden, bool mouthIsHidden, int race, int gender, float age);

	[EngineMethod("transform_face_keys_to_default_face", false, null, false)]
	void TransformFaceKeysToDefaultFace(ref FaceGenerationParams faceGenerationParams);

	[EngineMethod("get_random_body_properties", false, null, false)]
	void GetRandomBodyProperties(int race, int gender, ref BodyProperties bodyPropertiesMin, ref BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount, ref BodyProperties outBodyProperties);

	[EngineMethod("enforce_constraints", false, null, false)]
	bool EnforceConstraints(ref FaceGenerationParams faceGenerationParams);

	[EngineMethod("get_deform_key_data", false, null, false)]
	void GetDeformKeyData(int keyNo, ref DeformKeyData deformKeyData, int race, int gender, float age);

	[EngineMethod("get_face_gen_instances_length", false, null, false)]
	int GetFaceGenInstancesLength(int race, int gender, float age);

	[EngineMethod("get_scale", false, null, false)]
	float GetScaleFromKey(int race, int gender, ref BodyProperties initialBodyProperties);

	[EngineMethod("get_voice_records_count", false, null, false)]
	int GetVoiceRecordsCount(int race, int curGender, float age);

	[EngineMethod("get_hair_color_count", false, null, false)]
	int GetHairColorCount(int race, int curGender, float age);

	[EngineMethod("get_hair_color_gradient_points", false, null, false)]
	void GetHairColorGradientPoints(int race, int curGender, float age, Vec3[] colors);

	[EngineMethod("get_tatoo_color_count", false, null, false)]
	int GetTatooColorCount(int race, int curGender, float age);

	[EngineMethod("get_tatoo_color_gradient_points", false, null, false)]
	void GetTatooColorGradientPoints(int race, int curGender, float age, Vec3[] colors);

	[EngineMethod("get_skin_color_count", false, null, false)]
	int GetSkinColorCount(int race, int curGender, float age);

	[EngineMethod("get_maturity_type", false, null, false)]
	int GetMaturityType(float age);

	[EngineMethod("flush_face_cache", false, null, false)]
	void FlushFaceCache();

	[EngineMethod("get_voice_type_usable_for_player_data", false, null, false)]
	void GetVoiceTypeUsableForPlayerData(int race, int curGender, float age, bool[] aiArray);

	[EngineMethod("get_skin_color_gradient_points", false, null, false)]
	void GetSkinColorGradientPoints(int race, int curGender, float age, Vec3[] colors);

	[EngineMethod("get_race_ids", false, null, false)]
	string GetRaceIds();

	[EngineMethod("get_hair_indices_by_tag", false, null, false)]
	string GetHairIndicesByTag(int race, int curGender, float age, string tag);

	[EngineMethod("get_facial_indices_by_tag", false, null, false)]
	string GetFacialIndicesByTag(int race, int curGender, float age, string tag);

	[EngineMethod("get_tattoo_indices_by_tag", false, null, false)]
	string GetTattooIndicesByTag(int race, int curGender, float age, string tag);
}
