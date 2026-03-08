using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x0200003C RID: 60
	internal class ClientRestSessionTask
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000045FE File Offset: 0x000027FE
		// (set) Token: 0x06000150 RID: 336 RVA: 0x00004606 File Offset: 0x00002806
		public RestRequestMessage RestRequestMessage { get; private set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000151 RID: 337 RVA: 0x0000460F File Offset: 0x0000280F
		// (set) Token: 0x06000152 RID: 338 RVA: 0x00004617 File Offset: 0x00002817
		public bool Finished { get; private set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000153 RID: 339 RVA: 0x00004620 File Offset: 0x00002820
		// (set) Token: 0x06000154 RID: 340 RVA: 0x00004628 File Offset: 0x00002828
		public bool Successful { get; private set; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000155 RID: 341 RVA: 0x00004631 File Offset: 0x00002831
		// (set) Token: 0x06000156 RID: 342 RVA: 0x00004639 File Offset: 0x00002839
		public IHttpRequestTask Request { get; private set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000157 RID: 343 RVA: 0x00004642 File Offset: 0x00002842
		// (set) Token: 0x06000158 RID: 344 RVA: 0x0000464A File Offset: 0x0000284A
		public RestResponse RestResponse { get; private set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000159 RID: 345 RVA: 0x00004653 File Offset: 0x00002853
		public bool IsCompletelyFinished
		{
			get
			{
				return !this._willTryAgain && this._resultExamined && this.Request.State == HttpRequestTaskState.Finished;
			}
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00004678 File Offset: 0x00002878
		public ClientRestSessionTask(RestRequestMessage restRequestMessage)
		{
			this.RestRequestMessage = restRequestMessage;
			this._taskCompletionSource = new TaskCompletionSource<bool>();
			this._sw = new Stopwatch();
			this._messageName = this.RestRequestMessage.TypeName;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0000470D File Offset: 0x0000290D
		public void SetRequestData(byte[] userCertificate, string address, IHttpDriver networkClient)
		{
			this.RestRequestMessage.UserCertificate = userCertificate;
			this._requestAddress = address;
			this._postData = this.RestRequestMessage.SerializeAsJson();
			this._networkClient = networkClient;
			this.CreateAndSetRequest();
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00004740 File Offset: 0x00002940
		private void DetermineNextTry()
		{
			if (this._sw.ElapsedMilliseconds >= (long)ClientRestSessionTask.RequestRetryTimeout)
			{
				this._willTryAgain = false;
				Debug.Print("Retrying http post request, iteration count: " + this._currentIterationCount, 0, Debug.DebugColor.White, 17592186044416UL);
				this.CreateAndSetRequest();
			}
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00004794 File Offset: 0x00002994
		private static string GetCode(WebException webException)
		{
			if (webException.Response != null && webException.Response is HttpWebResponse)
			{
				return ((HttpWebResponse)webException.Response).StatusCode.ToString();
			}
			return "NoCode";
		}

		// Token: 0x0600015E RID: 350 RVA: 0x000047DC File Offset: 0x000029DC
		private void ExamineResult()
		{
			if (!this.Request.Successful)
			{
				bool flag = false;
				if (this.Request.Exception != null && this.RetryableExceptions.Any((Type e) => e == this.Request.Exception.GetType()))
				{
					object[] array = new object[6];
					array[0] = "Http Post Request with message(";
					array[1] = this.RestRequestMessage;
					array[2] = ")  failed. Retrying: (";
					int num = 3;
					Exception exception = this.Request.Exception;
					array[num] = ((exception != null) ? exception.GetType() : null);
					array[4] = ") ";
					array[5] = this.Request.Exception;
					Debug.Print(string.Concat(array), 0, Debug.DebugColor.White, 17592186044416UL);
					flag = true;
				}
				else
				{
					object[] array2 = new object[6];
					array2[0] = "Http Post Request with message(";
					array2[1] = this.RestRequestMessage;
					array2[2] = ")  failed. Exception: (";
					int num2 = 3;
					Exception exception2 = this.Request.Exception;
					array2[num2] = ((exception2 != null) ? exception2.GetType() : null);
					array2[4] = ") ";
					array2[5] = this.Request.Exception;
					Debug.Print(string.Concat(array2), 0, Debug.DebugColor.White, 17592186044416UL);
				}
				if (this.Request != null && this.Request.Exception != null)
				{
					this.PrintExceptions(this.Request.Exception);
				}
				if (flag)
				{
					if (this._currentIterationCount < this._maxIterationCount)
					{
						this._sw.Restart();
						this._willTryAgain = true;
						this._currentIterationCount++;
						Debug.Print(string.Concat(new object[] { "Http post request(", this.RestRequestMessage, ")  will try again, iteration count: ", this._currentIterationCount }), 0, Debug.DebugColor.White, 17592186044416UL);
					}
					else
					{
						this._willTryAgain = false;
						Debug.Print("Passed max retry count for http post request(" + this.RestRequestMessage + ") ", 0, Debug.DebugColor.White, 17592186044416UL);
					}
				}
				else
				{
					Debug.Print("Http post request(" + this.RestRequestMessage + ")  will not try again due to exception type!", 0, Debug.DebugColor.White, 17592186044416UL);
					this._willTryAgain = false;
				}
			}
			else if (this._currentIterationCount > 0)
			{
				Debug.Print(string.Concat(new object[] { "Http post request(", this.RestRequestMessage, ") is successful with iteration count: ", this._currentIterationCount }), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			this._resultExamined = true;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00004A48 File Offset: 0x00002C48
		public void Tick()
		{
			switch (this.Request.State)
			{
			case HttpRequestTaskState.NotStarted:
				this.Request.Start();
				return;
			case HttpRequestTaskState.Working:
				break;
			case HttpRequestTaskState.Finished:
				if (!this._resultExamined)
				{
					this.ExamineResult();
					return;
				}
				this.DetermineNextTry();
				break;
			default:
				return;
			}
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00004A98 File Offset: 0x00002C98
		public async Task WaitUntilFinished()
		{
			Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + this._messageName, 0, Debug.DebugColor.White, 17592186044416UL);
			await this._taskCompletionSource.Task;
			Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + this._messageName + " done", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00004AE0 File Offset: 0x00002CE0
		public void SetFinishedAsSuccessful(RestResponse restResponse)
		{
			Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + this._messageName, 0, Debug.DebugColor.White, 17592186044416UL);
			this.RestResponse = restResponse;
			this.Successful = true;
			this.Finished = true;
			this._taskCompletionSource.SetResult(true);
			Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + this._messageName + " done", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00004B55 File Offset: 0x00002D55
		public void SetFinishedAsFailed()
		{
			this.SetFinishedAsFailed(null);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00004B60 File Offset: 0x00002D60
		public void SetFinishedAsFailed(RestResponse restResponse)
		{
			Debug.Print("ClientRestSessionTask::SetFinishedAsFailed::" + this._messageName, 0, Debug.DebugColor.White, 17592186044416UL);
			this.RestResponse = restResponse;
			this.Successful = false;
			this.Finished = true;
			this._taskCompletionSource.SetResult(true);
			Debug.Print("ClientRestSessionTask::SetFinishedAsFailed:: " + this._messageName + " done", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00004BD8 File Offset: 0x00002DD8
		private void CreateAndSetRequest()
		{
			string text = this._requestAddress + "/Data/ProcessMessage";
			NameValueCollection nameValueCollection = new NameValueCollection();
			nameValueCollection.Add("url", text);
			nameValueCollection.Add("body", this._postData);
			nameValueCollection.Add("verb", "POST");
			this.Request = this._networkClient.CreateHttpPostRequestTask(text, this._postData, true);
			this._resultExamined = false;
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00004C48 File Offset: 0x00002E48
		private void PrintExceptions(Exception e)
		{
			if (e != null)
			{
				Exception ex = e;
				int num = 0;
				while (ex != null)
				{
					Debug.Print(string.Concat(new object[] { "Exception #", num, ": ", ex.Message, " ||| StackTrace: ", ex.InnerException }), 0, Debug.DebugColor.White, 17592186044416UL);
					ex = ex.InnerException;
					num++;
				}
			}
		}

		// Token: 0x0400006C RID: 108
		private static readonly int RequestRetryTimeout = 1000;

		// Token: 0x0400006D RID: 109
		private readonly Type[] RetryableExceptions = new Type[]
		{
			typeof(HttpRequestException),
			typeof(TaskCanceledException),
			typeof(IOException),
			typeof(SocketException),
			typeof(InvalidOperationException)
		};

		// Token: 0x04000072 RID: 114
		public bool _willTryAgain;

		// Token: 0x04000074 RID: 116
		private string _requestAddress;

		// Token: 0x04000075 RID: 117
		private string _postData;

		// Token: 0x04000076 RID: 118
		private string _messageName;

		// Token: 0x04000077 RID: 119
		private int _maxIterationCount = 5;

		// Token: 0x04000078 RID: 120
		private int _currentIterationCount;

		// Token: 0x04000079 RID: 121
		private Stopwatch _sw;

		// Token: 0x0400007A RID: 122
		private TaskCompletionSource<bool> _taskCompletionSource;

		// Token: 0x0400007B RID: 123
		private IHttpDriver _networkClient;

		// Token: 0x0400007C RID: 124
		private bool _resultExamined;
	}
}
