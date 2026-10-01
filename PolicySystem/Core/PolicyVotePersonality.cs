using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Optional in saved records, mandatory in new assessments. This describes the policy's
// methods, not its economic effects and not a particular voter's desired answer.
internal sealed class PolicyVotePersonalityProfile
{
	[JsonProperty("mercy")]
	public float? Mercy { get; set; }

	[JsonProperty("honor")]
	public float? Honor { get; set; }

	[JsonProperty("generosity")]
	public float? Generosity { get; set; }

	[JsonProperty("valor")]
	public float? Valor { get; set; }

	[JsonProperty("calculating")]
	public float? Calculating { get; set; }
}

internal static class PolicyVotePersonality
{
	internal const string PromptContract =
		"- votePersonality:object，必须且只能包含 mercy、honor、generosity、valor、calculating 五个 number，均在 [-1,1]，允许全部为 0。"
		+ "只按政策正文实际采用的手段评估，不按民众反馈、政策名称中的关键词、发布者性格或希望谁赞成来评分。"
		+ "正值表示该手段契合相应正向性格，负值表示契合其反面；无直接依据填 0。"
		+ "mercy：保护生命、宽恕、减少无辜者痛苦为正，明确残酷惩罚或伤害无辜者为负；禁止暴行应为正，不能因提到暴行就填负。"
		+ "honor：守约、履行明确承诺、公正程序为正，背约、欺骗、出尔反尔为负。"
		+ "generosity：主动救济、分享资源、照顾部属为正，明确自利侵吞、苛刻拒绝应有供养为负；普通税制变化本身不等于吝啬。"
		+ "valor：明确承担危险、迎战为正，因恐惧逃避责任为负；普通民政与审慎停战不自动扣分。"
		+ "calculating：有明确预案、风险权衡、克制和长期安排为正，明确冲动冒险、无视已知风险为负；残酷不自动等于审慎。"
		+ "稳定度、忠诚度、税收等数值损益由独立利益评分处理，不得仅因这些数值升降推断性格；"
		+ "降低忠诚度不自动意味着残酷，也不应给所有负面性格一并加分。";

	private static readonly string[] Fields = { "mercy", "honor", "generosity", "valor", "calculating" };

	internal static PolicyVotePersonalityProfile ReadRequired(JToken token)
	{
		if (!(token is JObject value) || value.Count != Fields.Length
			|| Fields.Any(field => value[field] == null))
		{
			throw new JsonException("votePersonality 必须包含且仅包含五项性格评分。");
		}
		foreach (string field in Fields)
		{
			JToken number = value[field];
			if ((number.Type != JTokenType.Float && number.Type != JTokenType.Integer)
				|| !IsValid(number.Value<double>()))
			{
				throw new JsonException("votePersonality 性格评分必须为 [-1,1] 的有限数值：" + field);
			}
		}
		return value.ToObject<PolicyVotePersonalityProfile>();
	}

	internal static PolicyVotePersonalityProfile CloneValidated(PolicyVotePersonalityProfile profile)
	{
		if (!IsValid(profile)) return null;
		return new PolicyVotePersonalityProfile
		{
			Mercy = profile.Mercy, Honor = profile.Honor, Generosity = profile.Generosity,
			Valor = profile.Valor, Calculating = profile.Calculating
		};
	}

	// Five bounded trait reads at the caller, no text parsing, allocation, network or world scan.
	// A +/-2 personality trait supplies +/-1 point on a fully matching axis. Combined P is
	// capped at +/-2, independently of U: destructive effects always retain their own cost.
	internal static float Compute(PolicyVotePersonalityProfile profile,
		int mercy, int honor, int generosity, int valor, int calculating)
	{
		if (!IsValid(profile)) return 0f;
		float score = Trait(mercy) * profile.Mercy.Value
			+ Trait(honor) * profile.Honor.Value
			+ Trait(generosity) * profile.Generosity.Value
			+ Trait(valor) * profile.Valor.Value
			+ Trait(calculating) * profile.Calculating.Value;
		return Math.Max(-2f, Math.Min(2f, score));
	}

	private static float Trait(int level) => Math.Max(-2, Math.Min(2, level)) / 2f;

	private static bool IsValid(PolicyVotePersonalityProfile profile) => profile != null
		&& ValidAxis(profile.Mercy) && ValidAxis(profile.Honor) && ValidAxis(profile.Generosity)
		&& ValidAxis(profile.Valor) && ValidAxis(profile.Calculating);

	private static bool ValidAxis(float? value) => value.HasValue && IsValid(value.Value);

	private static bool IsValid(double value) => !double.IsNaN(value) && !double.IsInfinity(value)
		&& value >= -1d && value <= 1d;
}
