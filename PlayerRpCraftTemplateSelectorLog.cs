using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace AnimusForge;

internal static class PlayerRpCraftTemplateSelectorLog
{
	internal const string FileName = "PlayerRpCraft_TemplateSelector.txt";

	private const int MaxLogSizeMegabytes = 16;

	// FeatureDiagnosticLogFile limits one entry to 64 KiB. Keeping each payload
	// chunk below 12,000 UTF-16 characters preserves even four-byte UTF-8 text
	// without truncating the actual request or response.
	private const int PayloadChunkCharacterLimit = 12000;

	private static readonly FeatureDiagnosticLogFile LogFile =
		new FeatureDiagnosticLogFile(
			FileName,
			"PlayerRpCraft_TemplateSelector",
			"PlayerRpTemplateSelector",
			() => true,
			() => false,
			() => MaxLogSizeMegabytes);

	private static long _exchangeSequence;

	internal static string CreateExchangeId()
	{
		long sequence = Interlocked.Increment(ref _exchangeSequence);
		return DateTime.UtcNow.ToString(
				"yyyyMMddTHHmmssfff",
				CultureInfo.InvariantCulture)
			+ "Z-"
			+ sequence.ToString("x", CultureInfo.InvariantCulture);
	}

	internal static void WriteRequest(
		string exchangeId,
		int attempt,
		string retryReason,
		string modelName,
		string controlMode,
		string requestedName,
		int investedDenars,
		bool isEquipment,
		string requestBody,
		string apiKeyForRedaction)
	{
		try
		{
			WritePayloadChunks(
				"REQUEST",
				BuildAttemptMetadata(
					exchangeId,
					attempt,
					retryReason,
					modelName,
					controlMode,
					requestedName,
					investedDenars,
					isEquipment),
				RedactApiKey(requestBody, apiKeyForRedaction));
		}
		catch
		{
		}
	}

	internal static void WriteResponse(
		string exchangeId,
		int attempt,
		string retryReason,
		bool succeeded,
		int statusCode,
		string reasonPhrase,
		string responseBody,
		string apiKeyForRedaction)
	{
		try
		{
			string metadata =
				"exchange=" + NormalizeMetadata(exchangeId, 96)
				+ " attempt=" + Math.Max(1, attempt).ToString(
					CultureInfo.InvariantCulture)
				+ " retry_reason=" + NormalizeMetadata(retryReason, 64)
				+ " http_success=" + (succeeded ? "true" : "false")
				+ " status=" + statusCode.ToString(
					CultureInfo.InvariantCulture)
				+ " reason=" + QuoteMetadata(reasonPhrase, 256);
			WritePayloadChunks(
				"RESPONSE",
				metadata,
				RedactApiKey(responseBody, apiKeyForRedaction));
		}
		catch
		{
		}
	}

	internal static void WriteRequestException(
		string exchangeId,
		int attempt,
		string retryReason,
		Exception exception,
		string apiKeyForRedaction)
	{
		try
		{
			string exceptionType =
				exception?.GetType().FullName ?? "UnknownException";
			string exceptionMessage =
				RedactApiKey(exception?.Message ?? "", apiKeyForRedaction);
			LogFile.Write(
				"HTTP_EXCEPTION",
				"exchange=" + NormalizeMetadata(exchangeId, 96)
					+ " attempt=" + Math.Max(1, attempt).ToString(
						CultureInfo.InvariantCulture)
					+ " retry_reason=" + NormalizeMetadata(retryReason, 64)
					+ " exception=" + NormalizeMetadata(exceptionType, 192)
					+ " message=" + QuoteMetadata(exceptionMessage, 4096),
				verbose: false);
		}
		catch
		{
		}
	}

	internal static void WriteParseResult(
		string exchangeId,
		int attempts,
		string assistantContent,
		bool succeeded,
		string parseError,
		PlayerRpCraftTemplateCandidate selected,
		string apiKeyForRedaction)
	{
		try
		{
			string metadata =
				"exchange=" + NormalizeMetadata(exchangeId, 96)
				+ " attempts=" + Math.Max(0, attempts).ToString(
					CultureInfo.InvariantCulture);
			WritePayloadChunks(
				"ASSISTANT_CONTENT",
				metadata,
				RedactApiKey(assistantContent, apiKeyForRedaction));
			LogFile.Write(
				"PARSE_RESULT",
				metadata
					+ " success=" + (succeeded ? "true" : "false")
					+ " candidate_rank="
					+ (selected?.Rank ?? 0).ToString(
						CultureInfo.InvariantCulture)
					+ " template="
					+ QuoteMetadata(selected?.TemplateStringId, 256)
					+ " error="
					+ QuoteMetadata(parseError, 2048),
				verbose: false);
		}
		catch
		{
		}
	}

	internal static void WriteTerminalResult(
		string exchangeId,
		int attempts,
		string outcome,
		string detail,
		string apiKeyForRedaction)
	{
		try
		{
			LogFile.Write(
				"RESULT",
				"exchange=" + NormalizeMetadata(exchangeId, 96)
					+ " attempts=" + Math.Max(0, attempts).ToString(
						CultureInfo.InvariantCulture)
					+ " outcome=" + NormalizeMetadata(outcome, 96)
					+ " detail="
					+ QuoteMetadata(
						RedactApiKey(detail, apiKeyForRedaction),
						4096),
				verbose: false);
		}
		catch
		{
		}
	}

	private static string BuildAttemptMetadata(
		string exchangeId,
		int attempt,
		string retryReason,
		string modelName,
		string controlMode,
		string requestedName,
		int investedDenars,
		bool isEquipment)
	{
		return "exchange=" + NormalizeMetadata(exchangeId, 96)
			+ " attempt=" + Math.Max(1, attempt).ToString(
				CultureInfo.InvariantCulture)
			+ " retry_reason=" + NormalizeMetadata(retryReason, 64)
			+ " model=" + QuoteMetadata(modelName, 256)
			+ " control_mode=" + NormalizeMetadata(controlMode, 96)
			+ " item=" + QuoteMetadata(requestedName, 256)
			+ " invested=" + Math.Max(0, investedDenars).ToString(
				CultureInfo.InvariantCulture)
			+ " craft_mode=" + (isEquipment ? "equipment" : "misc");
	}

	private static void WritePayloadChunks(
		string source,
		string metadata,
		string payload)
	{
		string safePayload = payload ?? "";
		if (safePayload.Length == 0)
		{
			LogFile.Write(
				source,
				metadata + " chunk=1/1" + Environment.NewLine
					+ "<empty>",
				verbose: false);
			return;
		}

		int chunkCount = CountPayloadChunks(safePayload);
		int offset = 0;
		int chunkIndex = 0;
		while (offset < safePayload.Length)
		{
			int chunkLength = GetPayloadChunkLength(safePayload, offset);
			chunkIndex++;
			LogFile.Write(
				source,
				metadata
					+ " chunk="
					+ chunkIndex.ToString(CultureInfo.InvariantCulture)
					+ "/"
					+ chunkCount.ToString(CultureInfo.InvariantCulture)
					+ Environment.NewLine
					+ safePayload.Substring(offset, chunkLength),
				verbose: false);
			offset += chunkLength;
		}
	}

	private static int CountPayloadChunks(string payload)
	{
		int count = 0;
		int offset = 0;
		while (offset < payload.Length)
		{
			offset += GetPayloadChunkLength(payload, offset);
			count++;
		}
		return Math.Max(1, count);
	}

	private static int GetPayloadChunkLength(string payload, int offset)
	{
		int remaining = Math.Max(0, payload.Length - offset);
		int length = Math.Min(PayloadChunkCharacterLimit, remaining);
		int end = offset + length;
		if (length > 0
			&& end < payload.Length
			&& char.IsHighSurrogate(payload[end - 1])
			&& char.IsLowSurrogate(payload[end]))
		{
			length--;
		}
		return Math.Max(1, length);
	}

	private static string RedactApiKey(string value, string apiKey)
	{
		string text = value ?? "";
		string secret = (apiKey ?? "").Trim();
		return secret.Length == 0
			? text
			: text.Replace(secret, "[REDACTED_API_KEY]");
	}

	private static string QuoteMetadata(string value, int maxLength)
	{
		string normalized = NormalizeMetadata(value, maxLength)
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"");
		return "\"" + normalized + "\"";
	}

	private static string NormalizeMetadata(string value, int maxLength)
	{
		string normalized = (value ?? "")
			.Replace('\r', ' ')
			.Replace('\n', ' ')
			.Replace('\t', ' ')
			.Trim();
		int safeMaxLength = Math.Max(1, maxLength);
		return normalized.Length <= safeMaxLength
			? normalized
			: normalized.Substring(0, safeMaxLength);
	}
}
