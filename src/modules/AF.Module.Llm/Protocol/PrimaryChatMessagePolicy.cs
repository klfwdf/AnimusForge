using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class PrimaryChatMessagePolicy
{
	private const string EmptyResponseRetryMarker = "[AF_EMPTY_RESPONSE_RETRY]";

	private const string EmptyResponseRetryInstruction = EmptyResponseRetryMarker + " 上一次模型响应为空白。请严格按既有角色、格式和字数要求，直接输出NPC本轮回复；禁止只输出空白、换行或无内容。";

	internal static bool HasEmptyResponseRetryMarker(List<object> messages)
	{
		try
		{
			foreach (object message in messages ?? new List<object>())
			{
				if (TryReadMessage(message, out var _, out var content) && (content ?? "").IndexOf(EmptyResponseRetryMarker, StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private const string GenericContinuationInstruction =
		"请继续完成当前请求，只输出最终结果。";

	private const string BattleSpeechContinuationInstruction =
		"请继续完成当前阵前演讲请求，只输出协议规定的最终结果，不要生成普通NPC回复。";

	internal static bool IsBattleSpeechRequest(IEnumerable<object> messages)
	{
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			if (!TryReadMessage(message, out _, out string content))
			{
				continue;
			}
			string text = content ?? string.Empty;
			if (text.IndexOf("【阵前演讲", StringComparison.Ordinal) >= 0 ||
				text.IndexOf("SPEECH_BEGIN", StringComparison.Ordinal) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static string GetLastMessageRole(
		IEnumerable<object> messages,
		out string lastContent)
	{
		string lastRole = string.Empty;
		lastContent = string.Empty;
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			if (!TryReadMessage(message, out string role, out string content))
			{
				continue;
			}
			if (string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(content))
			{
				continue;
			}
			lastRole = (role ?? string.Empty).Trim();
			lastContent = content ?? string.Empty;
		}
		return lastRole;
	}

	internal static List<object> EnsureFinalUserTurn(
		IEnumerable<object> messages,
		out string originalLastRole)
	{
		List<object> result = new List<object>();
		foreach (object message in messages ?? Enumerable.Empty<object>())
		{
			result.Add(message);
		}

		string lastContent;
		originalLastRole = GetLastMessageRole(result, out lastContent);
		if (string.Equals(originalLastRole, "user", StringComparison.OrdinalIgnoreCase) &&
			!string.IsNullOrWhiteSpace(lastContent))
		{
			return result;
		}

		result.Add(new
		{
			role = "user",
			content = IsBattleSpeechRequest(result)
				? BattleSpeechContinuationInstruction
				: GenericContinuationInstruction
		});
		return result;
	}

	internal static List<object> BuildEmptyResponseRetryMessages(List<object> messages)
	{
		List<object> list = new List<object>();
		bool flag = false;
		foreach (object message in messages ?? new List<object>())
		{
			if (!flag && TryReadMessage(message, out var role, out var content) && string.Equals((role ?? "").Trim(), "system", StringComparison.OrdinalIgnoreCase))
			{
				string text = string.IsNullOrWhiteSpace(content) ? EmptyResponseRetryInstruction : (content.TrimEnd() + "\n\n" + EmptyResponseRetryInstruction);
				list.Add(new
				{
					role = role,
					content = text
				});
				flag = true;
			}
			else
			{
				list.Add(message);
			}
		}
		if (!flag)
		{
			list.Insert(0, new
			{
				role = "system",
				content = EmptyResponseRetryInstruction
			});
		}
		return EnsureFinalUserTurn(list, out _);
	}

	private static bool ContainsAnyIgnoreCase(string text, params string[] patterns)
	{
		text = text ?? "";
		if (patterns == null || patterns.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < patterns.Length; i++)
		{
			string text2 = (patterns[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool LooksLikeThinkingControlError(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		bool flag = ContainsAnyIgnoreCase(text, "thinking", "reasoning_effort", "output_config");
		bool flag2 = ContainsAnyIgnoreCase(text, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported", "extra inputs are not permitted");
		return flag && flag2;
	}

	internal static bool TryReadMessage(object message, out string role, out string content)
	{
		role = "";
		content = "";
		if (message == null)
		{
			return false;
		}
		try
		{
			if (message is JObject jObject)
			{
				role = (string)jObject["role"] ?? "";
				content = (string)jObject["content"] ?? "";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (message is IDictionary<string, object> dictionary)
			{
				if (dictionary.TryGetValue("role", out var value) && value != null)
				{
					role = value.ToString();
				}
				if (dictionary.TryGetValue("content", out var value2) && value2 != null)
				{
					content = value2.ToString();
				}
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Type type = message.GetType();
			PropertyInfo propertyInfo = type.GetProperty("role") ?? type.GetProperty("Role");
			PropertyInfo propertyInfo2 = type.GetProperty("content") ?? type.GetProperty("Content");
			if (propertyInfo != null)
			{
				object value3 = propertyInfo.GetValue(message, null);
				if (value3 != null)
				{
					role = value3.ToString();
				}
			}
			if (propertyInfo2 != null)
			{
				object value4 = propertyInfo2.GetValue(message, null);
				if (value4 != null)
				{
					content = value4.ToString();
				}
			}
			return propertyInfo != null || propertyInfo2 != null;
		}
		catch
		{
			return false;
		}
	}
}
