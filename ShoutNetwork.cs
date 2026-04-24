using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

public static class ShoutNetwork
{
	private const int HardcodedMaxTokens = 5000;

	private static string BuildTokenStatsOutputContent(string finalContent, string reasoningContent = null)
	{
		string text = (finalContent ?? "").Trim();
		string text2 = (reasoningContent ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[REASONING]");
		stringBuilder.AppendLine(text2);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[FINAL]");
		stringBuilder.AppendLine(string.IsNullOrWhiteSpace(text) ? "（空）" : text);
		return stringBuilder.ToString().TrimEnd();
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

	public static async Task<string> CallApiWithMessages(List<object> messages, int maxTokens, bool recordTokenStats = true)
	{
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
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			JObject payload = new JObject
			{
				["model"] = settings.ModelName,
				["max_tokens"] = HardcodedMaxTokens
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
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request);
				string str = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					try
					{
						JObject responseJson = JObject.Parse(str);
						string content = ((string)responseJson.SelectToken("choices[0].message.content")) ?? ((string)responseJson.SelectToken("content")) ?? ((string)responseJson.SelectToken("text"));
						string reasoning = ((string)responseJson.SelectToken("choices[0].message.reasoning_content")) ?? ((string)responseJson.SelectToken("reasoning_content")) ?? "";
						if (string.IsNullOrWhiteSpace(content))
						{
							content = "（没说话）";
						}
						sw.Stop();
						Logger.Obs("Network", "request_complete", new Dictionary<string, object>
						{
							["mode"] = "non_stream",
							["ok"] = true,
							["status"] = (int)response.StatusCode,
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
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			JObject payload = new JObject
			{
				["model"] = settings.ModelName,
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
									fullText.Append(delta);
									try
									{
										onChunk?.Invoke(delta);
									}
									catch
									{
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
					onComplete?.Invoke(fullText.ToString().Trim());
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
			onComplete?.Invoke(fullText.ToString().Trim());
		}
		catch (Exception ex3)
		{
			string partial = fullText.ToString().Trim();
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

