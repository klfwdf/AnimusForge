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
	internal static string PromptContract => PolicyGenerationPromptTexts.Get("VotePersonalityContract", "- votePersonality:object must contain exactly five number fields: mercy, honor, generosity, valor, and calculating. Each must be in [-1,1]; all may be 0.Assess only the means actually used in the policy body, not public feedback, keywords in the policy name, the issuer's personality, or who is expected to support it.A positive value means the means align with the corresponding positive trait, and a negative value means alignment with its opposite. Use 0 when there is no direct evidence.mercy: protecting life, forgiveness, and reducing suffering of innocents are positive; explicit cruel punishment or harm to innocents is negative. Prohibiting atrocities is positive; do not assign a negative value merely because atrocities are mentioned.honor: keeping agreements, fulfilling explicit promises, and fair procedure are positive; breaking agreements, deception, and reversing promises are negative.generosity: voluntary relief, sharing resources, and caring for subordinates are positive; explicit selfish appropriation or harsh refusal of owed support is negative. Ordinary tax changes alone do not imply stinginess.valor: explicitly accepting danger or facing battle is positive; evading responsibility out of fear is negative. Ordinary civil administration and prudent truces do not automatically lose points.calculating: explicit contingency plans, weighing risks, restraint, and long-term planning are positive; explicit impulsive risk-taking or ignoring known risks is negative. Cruelty does not automatically imply prudence.Numeric gains and losses in stability, loyalty, taxation, and similar metrics are handled by independent interest scoring; do not infer personality solely from these increases or decreases.Reducing loyalty does not automatically imply cruelty and should not increase every negative personality score.");

	private static readonly string[] Fields = { "mercy", "honor", "generosity", "valor", "calculating" };

	internal static PolicyVotePersonalityProfile ReadRequired(JToken token)
	{
		if (!(token is JObject value) || value.Count != Fields.Length
			|| Fields.Any(field => value[field] == null))
		{
			throw new JsonException("votePersonality must contain exactly five personality scores.");
		}
		foreach (string field in Fields)
		{
			JToken number = value[field];
			if ((number.Type != JTokenType.Float && number.Type != JTokenType.Integer)
				|| !IsValid(number.Value<double>()))
			{
				throw new JsonException("votePersonality score must be a finite number in [-1, 1]: " + field);
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
