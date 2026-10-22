using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.Library.Http;

public static class HttpClientSend
{
	public static async Task<HttpSendResponse> SendAsync(HttpClient httpClient, HttpSendRequest request, Version httpVersion = null)
	{
		if (request == null || string.IsNullOrEmpty(request.Url))
		{
			return HttpSendResponse.Failed("invalid request");
		}
		try
		{
			using CancellationTokenSource timeout = new CancellationTokenSource((request.TimeoutMilliseconds > 0) ? request.TimeoutMilliseconds : 15000);
			using HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(request.Method ?? "POST"), request.Url);
			if (httpVersion != null)
			{
				message.Version = httpVersion;
			}
			byte[] bodyBytes = request.GetBodyBytes();
			if (bodyBytes != null)
			{
				message.Content = new ByteArrayContent(bodyBytes);
				message.Content.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType ?? "application/json")
				{
					CharSet = "utf-8"
				};
			}
			if (request.Headers != null)
			{
				foreach (KeyValuePair<string, string> header in request.Headers)
				{
					message.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}
			}
			using HttpResponseMessage response = await httpClient.SendAsync(message, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			string text = ((response.Content == null) ? "" : (await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false)));
			string text2 = text;
			return new HttpSendResponse
			{
				StatusCode = (int)response.StatusCode,
				Body = (text2 ?? "")
			};
		}
		catch (Exception ex)
		{
			return HttpSendResponse.Failed(ex.Message);
		}
	}
}
