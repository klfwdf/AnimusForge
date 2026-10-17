using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
internal static class StreamingLiveCase
{
    internal static async Task Run(Assembly af)
    {
        string session=Environment.GetEnvironmentVariable("AF_TTS_LIVE_SESSION");if(string.IsNullOrEmpty(session))return;
        string key=null;
        using(var input=new FileStream(session,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
        using(var reader=new StreamReader(input))
        {
            string line;while((line=reader.ReadLine())!=null)
            {
                using var doc=JsonDocument.Parse(line);
                if(!doc.RootElement.TryGetProperty("payload",out var p)||!p.TryGetProperty("role",out var role)||role.GetString()!="user")continue;
                string text=string.Join(" ",p.GetProperty("content").EnumerateArray().Select(c=>c.TryGetProperty("text",out var t)?t.GetString():""));
                if(!text.Contains("shenlanqaq.com"))continue;var match=Regex.Match(text,"sk-[A-Za-z0-9_-]{20,}");if(match.Success){key=match.Value;break;}
            }
        }
        if(key==null)throw new Exception("Referenced credential unavailable");
        using var observer=new StreamRequestObserver { InnerHandler=new HttpClientHandler{AllowAutoRedirect=false} };
        using var client=new HttpClient(observer){Timeout=Timeout.InfiniteTimeSpan};
        var type=af.GetType("AnimusForge.Refactor.Adapters.OpenAiCompatibleTtsGateway",true);
        object request=Activator.CreateInstance(af.GetType("AnimusForge.Refactor.Contracts.OpenAiTtsSynthesisRequest",true),"https://www.shenlanqaq.com/v1/audio/speech","gpt-sovits-v4","zh_default","旅行者，欢迎来到卡拉迪亚。城门已经为你打开，商队正在集市等待。我们会一起穿过山谷，守护村庄，并让每一位同伴平安归来。",1f,90);
        var clock=System.Diagnostics.Stopwatch.StartNew();var times=new List<double>();using var pcm=new MemoryStream();int rate=0;
        Action<byte[],int,long?> publish=(bytes,r,s)=>{times.Add(clock.Elapsed.TotalSeconds);rate=r;pcm.Write(bytes,0,bytes.Length);};
        Task send=(Task)type.GetMethod("StreamAsync").Invoke(Activator.CreateInstance(type,client),new object[]{request,key,publish,CancellationToken.None});await send;
        object result=send.GetType().GetProperty("Result").GetValue(send);
        if(!(bool)result.GetType().GetProperty("Success").GetValue(result))throw new Exception("Live streamed gateway failed: "+result.GetType().GetProperty("ErrorCode").GetValue(result));
        if(observer.StreamRequested!=true)throw new Exception("Actual upstream request did not send stream=true");
        double eof=clock.Elapsed.TotalSeconds;
        if(times.Count<2||times[0]>=eof||!pcm.ToArray().Any(b=>b!=0))throw new Exception("Live stream did not publish audible segments before completion");
        string output=Environment.GetEnvironmentVariable("AF_TTS_LIVE_OUTPUT");
        var engine=af.GetType("AnimusForge.TtsEngine",true);const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Static;
        engine.GetMethod("SavePcmAsWav",hidden).Invoke(null,new object[]{pcm.ToArray(),rate,Path.Combine(output,"streamed-live.wav")});
        var report=new{success=true,requestedStream=observer.StreamRequested,responseMediaType=observer.MediaType,transferChunked=observer.Chunked,completedUtc=DateTimeOffset.UtcNow.ToString("O"),firstPcmSeconds=times[0],eofSeconds=eof,downloadHeadStartSeconds=eof-times[0],segments=times.Count,sampleRate=rate,audioSeconds=pcm.Length/(rate*2.0),callbackSeconds=times};
        File.WriteAllText(Path.Combine(output,"streamed-live.json"),JsonSerializer.Serialize(report));Console.WriteLine("PASS live production streaming gateway "+JsonSerializer.Serialize(report));
    }
    private sealed class StreamRequestObserver : DelegatingHandler
    {
        internal bool? StreamRequested;
        internal string MediaType;
        internal bool Chunked;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            using var body=JsonDocument.Parse(await request.Content.ReadAsStringAsync());
            StreamRequested=body.RootElement.TryGetProperty("stream",out var flag)?flag.GetBoolean():(bool?)null;
            var response=await base.SendAsync(request,cancel);
            MediaType=response.Content?.Headers.ContentType?.ToString();Chunked=response.Headers.TransferEncodingChunked==true;
            return response;
        }
    }
}
