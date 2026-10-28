using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Network;

public class RESTClient
{
	private string _serviceAddress;

	public RESTClient(string serviceAddress)
	{
		_serviceAddress = serviceAddress;
	}

	private ServiceException GetServiceErrorCode(Stream stream)
	{
		string text = new StreamReader(stream).ReadToEnd();
		JObject jObject = JObject.Parse(text);
		if (jObject["ExceptionMessage"] != null)
		{
			return JsonConvert.DeserializeObject<ServiceExceptionModel>(text).ToServiceException();
		}
		if (jObject["error_description"] != null)
		{
			if ((string?)jObject["error"] == "invalid_grant")
			{
				return new ServiceException(string.Empty, "InvalidUsernameOrPassword");
			}
			return new ServiceException((string?)jObject["error"], (string?)jObject["error_description"]);
		}
		return new ServiceException("unknown", string.Empty);
	}

	private HttpWebRequest CreateHttpRequest(string service, List<KeyValuePair<string, string>> headers, string contentType, string method)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new Uri(_serviceAddress + service));
		httpWebRequest.Accept = "application/json";
		httpWebRequest.ContentType = contentType;
		httpWebRequest.Method = method;
		if (headers != null)
		{
			foreach (KeyValuePair<string, string> header in headers)
			{
				httpWebRequest.Headers.Add(header.Key, header.Value);
			}
		}
		return httpWebRequest;
	}

	public async Task<TResult> Get<TResult>(string service, List<KeyValuePair<string, string>> headers)
	{
		HttpWebRequest httpWebRequest = CreateHttpRequest(service, headers, "application/json", "GET");
		try
		{
			return JsonConvert.DeserializeObject<TResult>(new StreamReader((await httpWebRequest.GetResponseAsync()).GetResponseStream()).ReadToEnd());
		}
		catch (WebException ex)
		{
			if (ex.Response != null)
			{
				throw GetServiceErrorCode(ex.Response.GetResponseStream());
			}
			throw new Exception("HTTP Get Web Request Failed", ex);
		}
		catch (Exception innerException)
		{
			throw new Exception("HTTP Get Failed", innerException);
		}
	}

	public async Task Get(string service, List<KeyValuePair<string, string>> headers)
	{
		HttpWebRequest httpWebRequest = CreateHttpRequest(service, headers, "application/json", "GET");
		try
		{
			await httpWebRequest.GetResponseAsync();
		}
		catch (WebException ex)
		{
			if (ex.Response != null)
			{
				throw GetServiceErrorCode(ex.Response.GetResponseStream());
			}
			throw new Exception("HTTP Get Web Request Failed", ex);
		}
		catch (Exception innerException)
		{
			throw new Exception("HTTP Get Failed", innerException);
		}
	}

	public async Task<TResult> Post<TResult>(string service, List<KeyValuePair<string, string>> headers, string payLoad, string contentType = "application/json")
	{
		HttpWebRequest http = CreateHttpRequest(service, headers, contentType, "POST");
		ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
		byte[] bytes = aSCIIEncoding.GetBytes(payLoad);
		try
		{
			Stream obj = await http.GetRequestStreamAsync();
			obj.Write(bytes, 0, bytes.Length);
			obj.Close();
			return JsonConvert.DeserializeObject<TResult>(new StreamReader((await http.GetResponseAsync()).GetResponseStream()).ReadToEnd());
		}
		catch (WebException ex)
		{
			if (ex.Response != null)
			{
				HttpWebResponse httpWebResponse = (HttpWebResponse)ex.Response;
				if (httpWebResponse.StatusCode == HttpStatusCode.Unauthorized || httpWebResponse.StatusCode == HttpStatusCode.NotFound)
				{
					throw ex;
				}
				throw GetServiceErrorCode(ex.Response.GetResponseStream());
			}
			throw new Exception("HTTP Post Web Request Failed", ex);
		}
		catch (Exception innerException)
		{
			throw new Exception("HTTP Post Failed", innerException);
		}
	}

	public async Task Post(string service, List<KeyValuePair<string, string>> headers, string payLoad, string contentType = "application/json")
	{
		HttpWebRequest http = CreateHttpRequest(service, headers, contentType, "POST");
		ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
		byte[] bytes = aSCIIEncoding.GetBytes(payLoad);
		try
		{
			Stream obj = await http.GetRequestStreamAsync();
			obj.Write(bytes, 0, bytes.Length);
			obj.Close();
			await http.GetResponseAsync();
		}
		catch (WebException ex)
		{
			if (ex.Response != null)
			{
				throw GetServiceErrorCode(ex.Response.GetResponseStream());
			}
			throw new Exception("HTTP Post Web Request Failed", ex);
		}
		catch (Exception innerException)
		{
			throw new Exception("HTTP Post Failed", innerException);
		}
	}
}
