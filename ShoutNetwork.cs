using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public static class ShoutNetwork
{
	private const int HardcodedMaxTokens = 5000;

	private sealed class PlayerReferenceStreamFilter
	{
		private string _pending = "";

		public string Push(string text)
		{
			string text2 = (_pending ?? "") + (text ?? "");
			_pending = "";
			if (string.IsNullOrEmpty(text2))
			{
				return "";
			}
			if (text2.EndsWith("玩", StringComparison.Ordinal))
			{
				_pending = "玩";
				text2 = text2.Substring(0, text2.Length - 1);
			}
			return ApplyPlayerDynamicNameToMainText(text2);
		}

		public string Flush()
		{
			string text = _pending ?? "";
			_pending = "";
			return ApplyPlayerDynamicNameToMainText(text);
		}
	}

	private static string BuildTokenStatsOutputContent(string finalContent, string reasoningContent = null)
	{
		return ApplyPlayerDynamicNameToMainText(finalContent ?? "").Trim();
	}

	private static string BuildApiErrorDetail(string responseBody)
	{
		if (string.IsNullOrWhiteSpace(responseBody))
		{
			return "";
		}
		string text = responseBody.Replace("\r", " ").Replace("\n", " ").Trim();
		if (text.Length > 320)
		{
			text = text.Substring(0, 320) + "...";
		}
		return " " + text;
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

	private static bool TryApplyDeepSeekThinkingControls(JObject payload, string apiUrl, string modelName, out string thinkingMode)
	{
		thinkingMode = "plain";
		if (payload == null)
		{
			return false;
		}
		if (!ContainsAnyIgnoreCase(apiUrl, "deepseek") && !ContainsAnyIgnoreCase(modelName, "deepseek"))
		{
			return false;
		}
		payload["thinking"] = new JObject
		{
			["type"] = "disabled"
		};
		payload.Remove("reasoning_effort");
		thinkingMode = "deepseek_disabled";
		return true;
	}

	private static bool LooksLikeThinkingControlError(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		bool flag = ContainsAnyIgnoreCase(text, "thinking", "reasoning_effort");
		bool flag2 = ContainsAnyIgnoreCase(text, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported", "extra inputs are not permitted");
		return flag && flag2;
	}

	private static bool TryResolvePrimaryModelByDropdownState(DuelSettings settings, out string modelName, out string selectedOption, out bool manualSelected)
	{
		modelName = "";
		selectedOption = "";
		manualSelected = true;
		if (settings == null)
		{
			return false;
		}
		selectedOption = (settings.GetMainSelectedModelOption() ?? "").Trim();
		manualSelected = string.Equals(selectedOption, "*手动填写*", StringComparison.Ordinal);
		if (manualSelected)
		{
			modelName = (settings.ModelName ?? "").Trim();
		}
		else
		{
			modelName = selectedOption;
		}
		if (string.IsNullOrWhiteSpace(modelName))
		{
			modelName = (settings.GetEffectiveMainModelName() ?? "").Trim();
		}
		return !string.IsNullOrWhiteSpace(modelName);
	}

	private static JObject BuildPrimaryChatPayload(List<object> messages, string apiUrl, string modelName, bool stream, out string thinkingMode)
	{
		JObject jObject = new JObject
		{
			["model"] = modelName ?? "",
			["max_tokens"] = HardcodedMaxTokens
		};
		if (stream)
		{
			jObject["stream"] = true;
		}
		JArray jArray = new JArray();
		foreach (object message in messages ?? new List<object>())
		{
			dynamic val = message;
			jArray.Add(new JObject
			{
				["role"] = val.role,
				["content"] = val.content
			});
		}
		jObject["messages"] = jArray;
		TryApplyDeepSeekThinkingControls(jObject, apiUrl, modelName, out thinkingMode);
		return jObject;
	}

	private static List<object> ApplyPlayerDisplayNameToOutgoingMessages(List<object> messages)
	{
		try
		{
			if (messages == null || messages.Count == 0)
			{
				return messages ?? new List<object>();
			}
			List<object> list = new List<object>(messages.Count);
			foreach (object message in messages)
			{
				if (TryReadMessage(message, out var role, out var content))
				{
					list.Add(new
					{
						role = role,
						content = ApplyPlayerDynamicNameToMainText(content)
					});
				}
				else
				{
					list.Add(message);
				}
			}
			return list;
		}
		catch
		{
			return messages ?? new List<object>();
		}
	}

	private static bool TryReadMessage(object message, out string role, out string content)
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

	private static string ApplyPlayerDynamicNameToMainText(string text)
	{
		try
		{
			string text2 = text ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			string text3 = ResolvePlayerDynamicNameForOutgoingText();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "玩家";
			}
			const string text4 = "__AFEF_PLAYER_FACT__";
			const string text5 = "__PLAYER_MARRIAGE_SECTION__";
			if (!string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				text2 = text2.Replace("[AFEF" + text3 + "行为补充]", text4);
				text2 = text2.Replace("【" + text3 + "家族可婚配未婚成员（事实清单）】", text5);
			}
			text2 = text2.Replace("[AFEF玩家行为补充]", text4);
			text2 = text2.Replace("【玩家家族可婚配未婚成员（事实清单）】", text5);
			text2 = NormalizeLegacyDuelStakeText(text2, "玩家");
			if (!string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				text2 = text2.Replace("玩家", text3);
			}
			text2 = NormalizeLegacyDuelStakeText(text2, text3);
			text2 = text2.Replace(text4, "[AFEF玩家行为补充]");
			text2 = text2.Replace(text5, "【玩家家族可婚配未婚成员（事实清单）】");
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	private static string NormalizeLegacyDuelStakeText(string text, string playerName)
	{
		try
		{
			string text2 = text ?? "";
			string text3 = (playerName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
			{
				return text2;
			}
			string pattern = Regex.Escape(text3);
			text2 = Regex.Replace(text2, "你已经将\\s*(\\d+)\\s*第纳尔交给\\s*" + pattern + "\\s*（决斗赌注）[。.]?", "你在决斗中输给了 " + text3 + "，并已按赌注将 $1 第纳尔交给 " + text3 + "。");
			text2 = Regex.Replace(text2, "你已经将\\s*(\\d+)\\s*个\\s*([^（\\r\\n]+?)\\s*交给\\s*" + pattern + "\\s*（决斗赌注）[。.]?", "你在决斗中输给了 " + text3 + "，并已按赌注将 $1 个 $2 交给 " + text3 + "。");
			text2 = Regex.Replace(text2, "你从\\s*([^（\\r\\n]+?)\\s*收到了\\s*(\\d+)\\s*第纳尔（决斗赌注）[。.]?", "你在决斗中击败了 $1，并从 $1 收到了 $2 第纳尔（决斗赌注）。");
			text2 = Regex.Replace(text2, "你从\\s*([^（\\r\\n]+?)\\s*收到了\\s*(\\d+)\\s*个\\s*([^（\\r\\n]+?)\\s*（决斗赌注）[。.]?", "你在决斗中击败了 $1，并从 $1 收到了 $2 个 $3（决斗赌注）。");
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	private static string ResolvePlayerDynamicNameForOutgoingText()
	{
		try
		{
			string text = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		try
		{
			return (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	public static async Task<string> CallApiWithMessages(List<object> messages, int maxTokens, bool recordTokenStats = true)
	{
		messages = ApplyPlayerDisplayNameToOutgoingMessages(messages);
		Stopwatch sw = Stopwatch.StartNew();
		int msgCount = messages?.Count ?? 0;
		int inputTokens = Logger.EstimateTokensFromMessages(messages);
		Logger.Obs("Network", "request_start", new Dictionary<string, object>
		{
			["mode"] = "non_stream",
			["maxTokens"] = HardcodedMaxTokens,
			["messages"] = msgCount
		});
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_api_key"
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				return "（错误：未配置 API Key）";
			}
			if (!TryResolvePrimaryModelByDropdownState(settings, out var effectiveModelName, out var selectedOption, out var manualSelected))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_model_name",
					["selectedOption"] = selectedOption ?? "",
					["manualSelected"] = manualSelected
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				return "（错误：未配置模型名称）";
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			JObject payload = BuildPrimaryChatPayload(messages, effectiveApiUrl, effectiveModelName, stream: false, out var thinkingMode);
			string jsonBody = payload.ToString(Formatting.None);
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request);
				string str = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.BadRequest && thinkingMode != "plain" && LooksLikeThinkingControlError(str))
				{
					Logger.Log("ShoutNetwork", "[PrimaryChat] DeepSeek thinking payload rejected; retrying without thinking controls.");
					response.Dispose();
					JObject payload2 = BuildPrimaryChatPayload(messages, effectiveApiUrl, effectiveModelName, stream: false, out var _);
					payload2.Remove("thinking");
					payload2.Remove("reasoning_effort");
					string jsonBody2 = payload2.ToString(Formatting.None);
					using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
					httpRequestMessage.Content = (HttpContent)new StringContent(jsonBody2, Encoding.UTF8, "application/json");
					response = await DuelSettings.GlobalClient.SendAsync(httpRequestMessage);
					str = await response.Content.ReadAsStringAsync();
					thinkingMode = "deepseek_retry_plain";
				}
				if (response.IsSuccessStatusCode)
				{
					try
					{
						JObject responseJson = JObject.Parse(str);
						string content = ((string)responseJson.SelectToken("choices[0].message.content")) ?? ((string)responseJson.SelectToken("content")) ?? ((string)responseJson.SelectToken("text"));
						string reasoning = "";
						if (string.IsNullOrWhiteSpace(content))
						{
							content = "（没说话）";
						}
						content = ApplyPlayerDynamicNameToMainText(content);
						sw.Stop();
						Logger.Obs("Network", "request_complete", new Dictionary<string, object>
						{
							["mode"] = "non_stream",
							["ok"] = true,
							["status"] = (int)response.StatusCode,
							["thinkingMode"] = thinkingMode,
							["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
							["resultLen"] = content.Length
						});
						Logger.Metric("network.non_stream", ok: true, sw.Elapsed.TotalMilliseconds);
						if (recordTokenStats)
						{
							string outputContent = BuildTokenStatsOutputContent(content, reasoning);
							Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent), messages, outputContent, "non_stream");
						}
						return content.Trim();
					}
					catch
					{
						sw.Stop();
						Logger.Obs("Network", "parse_error", new Dictionary<string, object>
						{
							["mode"] = "non_stream",
							["status"] = (int)response.StatusCode,
							["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
						});
						Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
						return "（API响应格式错误）";
					}
				}
				sw.Stop();
				Logger.Obs("Network", "request_complete", new Dictionary<string, object>
				{
					["mode"] = "non_stream",
					["ok"] = false,
					["status"] = (int)response.StatusCode,
					["thinkingMode"] = thinkingMode,
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
				});
				Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
				return $"（API请求失败: {response.StatusCode}{BuildApiErrorDetail(str)}）";
			}
			finally
			{
				((IDisposable)request)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			sw.Stop();
			Logger.Obs("Network", "request_error", new Dictionary<string, object>
			{
				["mode"] = "non_stream",
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("network.non_stream", ok: false, sw.Elapsed.TotalMilliseconds);
			return "（程序错误: " + ex.Message + "）";
		}
	}

	public static async Task CallApiWithMessagesStream(List<object> messages, int maxTokens, Action<string> onChunk, Action<string> onComplete, Action<string> onError, CancellationToken cancellationToken = default(CancellationToken))
	{
		messages = ApplyPlayerDisplayNameToOutgoingMessages(messages);
		PlayerReferenceStreamFilter outputFilter = new PlayerReferenceStreamFilter();
		StringBuilder fullText = new StringBuilder();
		StringBuilder fullReasoning = new StringBuilder();
		Stopwatch sw = Stopwatch.StartNew();
		double firstChunkMs = -1.0;
		int chunkCount = 0;
		int msgCount = messages?.Count ?? 0;
		int inputTokens = Logger.EstimateTokensFromMessages(messages);
		Logger.Obs("Network", "request_start", new Dictionary<string, object>
		{
			["mode"] = "stream",
			["maxTokens"] = HardcodedMaxTokens,
			["messages"] = msgCount
		});
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_api_key"
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				onError?.Invoke("（错误：未配置 API Key）");
				return;
			}
			if (!TryResolvePrimaryModelByDropdownState(settings, out var effectiveModelName, out var selectedOption, out var manualSelected))
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = "missing_model_name",
					["selectedOption"] = selectedOption ?? "",
					["manualSelected"] = manualSelected
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				onError?.Invoke("（错误：未配置模型名称）");
				return;
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			JObject payload = new JObject
			{
				["model"] = effectiveModelName,
				["max_tokens"] = HardcodedMaxTokens,
				["stream"] = true
			};
			JArray messagesArray = new JArray();
			foreach (object msg in messages)
			{
				dynamic dict = msg;
				messagesArray.Add(new JObject
				{
					["role"] = dict.role,
					["content"] = dict.content
				});
			}
			payload["messages"] = messagesArray;
			string jsonBody = payload.ToString(Formatting.None);
			bool streamSucceeded = false;
			Exception lastStreamException = null;
			for (int attempt = 1; attempt <= 2; attempt++)
			{
				if (streamSucceeded)
				{
					break;
				}
				try
				{
					HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					try
					{
						request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
						request.Headers.ConnectionClose = true;
						request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
						HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request, (HttpCompletionOption)1, cancellationToken);
						if (!response.IsSuccessStatusCode)
						{
							string errBody = await response.Content.ReadAsStringAsync();
							sw.Stop();
							Logger.Obs("Network", "request_complete", new Dictionary<string, object>
							{
								["mode"] = "stream",
								["ok"] = false,
								["status"] = (int)response.StatusCode,
								["attempt"] = attempt,
								["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
							});
							Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
							onError?.Invoke($"（API请求失败: {response.StatusCode}{BuildApiErrorDetail(errBody)}）");
							return;
						}
						using Stream stream = await response.Content.ReadAsStreamAsync();
						using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
						while (true)
						{
							string text;
							string line = (text = await reader.ReadLineAsync());
							if (text == null || cancellationToken.IsCancellationRequested)
							{
								break;
							}
							line = line.Trim();
							if (string.IsNullOrEmpty(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
							string data = line.Substring(5).Trim();
							if (data == "[DONE]")
							{
								break;
							}
							try
							{
								JObject chunk = JObject.Parse(data);
								string reasoningDelta = (string)chunk.SelectToken("choices[0].delta.reasoning_content");
								if (reasoningDelta == null)
								{
									reasoningDelta = (string)chunk.SelectToken("delta.reasoning_content");
								}
								if (reasoningDelta == null)
								{
									reasoningDelta = (string)chunk.SelectToken("reasoning_content");
								}
								if (!string.IsNullOrEmpty(reasoningDelta))
								{
									fullReasoning.Append(reasoningDelta);
								}
								string delta = (string)chunk.SelectToken("choices[0].delta.content");
								if (delta == null)
								{
									delta = (string)chunk.SelectToken("delta.content");
								}
								if (delta == null)
								{
									delta = (string)chunk.SelectToken("content");
								}
								if (delta == null)
								{
									delta = (string)chunk.SelectToken("text");
								}
								if (!string.IsNullOrEmpty(delta))
								{
									string text2 = outputFilter.Push(delta);
									if (chunkCount == 0)
									{
										firstChunkMs = sw.Elapsed.TotalMilliseconds;
										Logger.Obs("Network", "first_chunk", new Dictionary<string, object>
										{
											["mode"] = "stream",
											["firstChunkMs"] = Math.Round(firstChunkMs, 2)
										});
									}
									chunkCount++;
									if (!string.IsNullOrEmpty(text2))
									{
										fullText.Append(text2);
										try
										{
											onChunk?.Invoke(text2);
										}
										catch
										{
										}
									}
								}
							}
							catch
							{
							}
						}
					}
					finally
					{
						((IDisposable)request)?.Dispose();
					}
					string text3 = outputFilter.Flush();
					if (!string.IsNullOrEmpty(text3))
					{
						fullText.Append(text3);
						try
						{
							onChunk?.Invoke(text3);
						}
						catch
						{
						}
					}
					streamSucceeded = true;
				}
				catch (Exception ex)
				{
					lastStreamException = ex;
					if (attempt < 2 && fullText.Length == 0)
					{
						await Task.Delay(500, cancellationToken);
					}
				}
			}
			if (!streamSucceeded)
			{
				string fallback = await CallApiWithMessages(messages, maxTokens, recordTokenStats: false);
				if (!string.IsNullOrWhiteSpace(fallback) && !fallback.StartsWith("（错误") && !fallback.StartsWith("（程序错误") && !fallback.StartsWith("（API请求失败"))
				{
					sw.Stop();
					Logger.Obs("Network", "request_complete", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["ok"] = true,
						["fallback"] = true,
						["chunkCount"] = chunkCount,
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["resultLen"] = fallback.Length
					});
					Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
					Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(fallback), messages, BuildTokenStatsOutputContent(fallback), "stream_fallback");
					onComplete?.Invoke(fallback.Trim());
					return;
				}
				if (fullText.Length > 0)
				{
					string text4 = outputFilter.Flush();
					if (!string.IsNullOrEmpty(text4))
					{
						fullText.Append(text4);
					}
					sw.Stop();
					Logger.Obs("Network", "request_complete", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["ok"] = true,
						["fallback"] = false,
						["partial"] = true,
						["chunkCount"] = chunkCount,
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["resultLen"] = fullText.Length
					});
					Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
					string outputContent3 = BuildTokenStatsOutputContent(fullText.ToString(), fullReasoning.ToString());
					Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent3), messages, outputContent3, "stream_partial");
					onComplete?.Invoke(ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim());
					return;
				}
				if (lastStreamException != null)
				{
					sw.Stop();
					Logger.Obs("Network", "request_error", new Dictionary<string, object>
					{
						["mode"] = "stream",
						["fallback"] = true,
						["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["message"] = lastStreamException.Message,
						["type"] = lastStreamException.GetType().Name
					});
					Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
					onError?.Invoke("（程序错误: " + lastStreamException.Message + "）");
					return;
				}
			}
			string finalText = fullText.ToString().Trim();
			finalText = ApplyPlayerDynamicNameToMainText(finalText);
			if (string.IsNullOrWhiteSpace(finalText))
			{
				finalText = "（没说话）";
			}
			sw.Stop();
			Logger.Obs("Network", "request_complete", new Dictionary<string, object>
			{
				["mode"] = "stream",
				["ok"] = true,
				["fallback"] = false,
				["chunkCount"] = chunkCount,
				["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["resultLen"] = finalText.Length
			});
			Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
			string outputContent2 = BuildTokenStatsOutputContent(finalText, fullReasoning.ToString());
			Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent2), messages, outputContent2, "stream");
			onComplete?.Invoke(finalText);
		}
		catch (OperationCanceledException)
		{
			sw.Stop();
			Logger.Obs("Network", "request_cancelled", new Dictionary<string, object>
			{
				["mode"] = "stream",
				["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
				["partialLen"] = fullText.Length
			});
			Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
			string outputContent4 = BuildTokenStatsOutputContent(fullText.ToString(), fullReasoning.ToString());
			Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent4), messages, outputContent4, "stream_cancelled");
			onComplete?.Invoke(ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim());
		}
		catch (Exception ex3)
		{
			string partial = ApplyPlayerDynamicNameToMainText(fullText.ToString()).Trim();
			if (!string.IsNullOrEmpty(partial))
			{
				sw.Stop();
				Logger.Obs("Network", "request_complete", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["ok"] = true,
					["partial"] = true,
					["chunkCount"] = chunkCount,
					["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["resultLen"] = partial.Length
				});
				Logger.Metric("network.stream", ok: true, sw.Elapsed.TotalMilliseconds);
				string outputContent5 = BuildTokenStatsOutputContent(partial, fullReasoning.ToString());
				Logger.RecordTokenStats(inputTokens, Logger.EstimateTokens(outputContent5), messages, outputContent5, "stream_exception_partial");
				onComplete?.Invoke(partial);
			}
			else
			{
				sw.Stop();
				Logger.Obs("Network", "request_error", new Dictionary<string, object>
				{
					["mode"] = "stream",
					["latencyMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
					["message"] = ex3.Message,
					["type"] = ex3.GetType().Name
				});
				Logger.Metric("network.stream", ok: false, sw.Elapsed.TotalMilliseconds);
				onError?.Invoke("（程序错误: " + ex3.Message + "）");
			}
		}
	}
}

