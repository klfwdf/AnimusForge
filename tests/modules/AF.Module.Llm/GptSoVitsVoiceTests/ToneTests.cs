using AnimusForge;
using Newtonsoft.Json.Linq;
using System.Net;

static class ToneTests
{
    internal static void Run(Action<bool,string> check)
    {
        string foreign = "[ACTION:MOOD:ANGRY][MOOD:HAPPY][AD:1][GUI:2][RELAY:3][END][CONTENT]";
        foreach (var name in new[] { "neutral", "happy", "angry", "sad" })
        {
            string tag = "[TTS_TONE:" + name + "]";
            string clean = LocalTtsToneTag.Extract("台词。" + tag + foreign, out var tone);
            check(clean == "台词。" + foreign && (int)tone == Array.IndexOf(new[]{"neutral","happy","angry","sad"},name), "only dedicated tag selects / foreign tags byte intact");
            for (int i=0;i<=tag.Length;i++)
                check(LocalTtsToneTag.StripStreamingPreview("台词。"+tag[..i])=="台词。", "every SSE prefix hidden");
            check(LocalTtsToneTag.StripStreamingPreview("台词。"+tag+"后续")=="台词。后续", "following visible text preserved");
        }
        foreach(string invalid in new[]{"",foreign,"我很生气。","[TTS_TONE:ANGRY]","[tts_tone:angry]","[TTS_TONE:angry","[TTS_TONE:unknown]","[TTS_TONE:sad][TTS_TONE:sad]","[TTS_TONE:happy][TTS_TONE:angry]","[TTS_TONE:bad][TTS_TONE:angry]"})
        { LocalTtsToneTag.Extract(invalid,out var tone);check(tone==LocalTtsTone.Neutral,"invalid/missing/duplicate never guesses"); }
        check(LocalTtsToneTag.Strip("前[TTS_TONE:bad[AD:4]后")=="前[AD:4]后","malformed tag never swallows next tag");
        check(LocalTtsToneTag.Strip("前[TTS_TONE:bad\n后")=="前\n后","malformed tag never swallows following line");
        check(LocalTtsToneTag.Strip("[TTS_TONE_EXTRA:angry]")=="[TTS_TONE_EXTRA:angry]","namespace boundary");
        check(LocalTtsToneTag.Strip(null)=="","null safe");
        check(LocalTtsToneTag.Strip("text[TTS_TO")=="text","truncated final metadata hidden");
        string nested="[ACTION:KEEP:[TTS_TONE:angry]]";
        check(LocalTtsToneTag.Extract(nested,out var ignored)==nested&&ignored==LocalTtsTone.Neutral,"foreign nested payload opaque");
        Parallel.For(0,128,i=> { var raw=i%2==0?"a[TTS_TONE:angry]":"b[TTS_TONE:happy]"; LocalTtsToneTag.Extract(raw,out var tone); if(tone!=(i%2==0?LocalTtsTone.Angry:LocalTtsTone.Happy)) throw new Exception("cross-request state"); });
        check(true,"concurrent parser isolation");
        string large=string.Concat(Enumerable.Repeat("[TTS_TONE:bad][ACTION:KEEP]",20000));
        check(LocalTtsToneTag.Strip(large)==string.Concat(Enumerable.Repeat("[ACTION:KEEP]",20000)),"bounded linear malformed-tag scan");
    }
    internal static async Task CheckPack(string manifest, byte[] wave, Action<bool,string> check)
    {
        var settings=new DuelSettings { TtsGptSoVitsManifestPath=Path.GetFullPath(manifest), TtsGptSoVitsMood=3 };
        DuelSettings.Current=settings;
        var catalog=GptSoVitsVoiceRuntime.GetCatalog(settings);
        check(catalog.ForGender(false).Count==5 && catalog.ForGender(true).Count==5,"five male / five female defaults preserved");
        check(catalog.ForSelection(false).Count(x=>x.IsCustom)==1,"one combined custom Sparkle");
        var paths=new HashSet<string>();
        foreach(var name in new[]{"neutral","happy","angry","sad"})
        {
            var assigned=catalog.ResolveAssignment("female-sparkle-"+name,false);
            check(assigned.Id=="female-sparkle" && assigned.IsCustom,"legacy Sparkle assignment canonicalized, still custom");
            string clean=LocalTtsToneTag.Extract("你就是个乡下来的猴子。[TTS_TONE:"+name+"]",out var tone);
            var request=GptSoVitsVoiceRuntime.CaptureRequest(clean,assigned.Id,1,settings,(int)tone);
            paths.Add(request.Voice.ReferencePath);
            using var client=new HttpClient(new Handler(async msg=>{
                var body=JObject.Parse(await msg.Content.ReadAsStringAsync());
                check((string)body["text"]=="你就是个乡下来的猴子。" && (string)body["ref_audio_path"]==request.Voice.ReferencePath && (string)body["prompt_text"]==request.Voice.PromptText,"actual capture to gateway: clean text / matching reference and transcript");
                check(body["emotion"]==null && body["tts_tone"]==null && msg.Headers.Authorization==null,"no invented GPT API fields / cloud credentials");
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(wave)};
            }));
            var result=await new GptSoVitsTtsGateway(client).SynthesizeAsync(request,CancellationToken.None);
            check(result.Success,"actual reference SHA and HTTP payload accepted by local fake endpoint");
        }
        check(paths.Count==4,"four distinct reference recordings, same voice identity");
        var normal=GptSoVitsVoiceRuntime.CaptureRequest("plain","female-sparkle",1,settings);
        check(normal.Voice.ReferencePath==catalog.Get("female-sparkle").ForMood(0).ReferencePath,"manual audition setting never becomes dialogue default");
    }
    sealed class Handler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage,Task<HttpResponseMessage>> send;
        internal Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> send)=>this.send=send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage msg,CancellationToken token)=>send(msg);
    }
}
namespace AnimusForge
{
    // Runtime game/settings boundaries only; real catalog, capture, parser and gateway above.
    internal sealed class DuelSettings
    {
        internal bool UsesManagedGptSoVits, EnableTtsSpeech = true, TtsUseGptSoVits = true;
        internal string TtsGptSoVitsOutputLanguage = "";
        internal static DuelSettings Current;
        internal static DuelSettings GetSettings()=>Current;
        internal string TtsGptSoVitsManifestPath;
        internal string TtsGptSoVitsApiUrl="http://127.0.0.1:9880/tts";
        internal int TtsGptSoVitsMood;
    }
    internal static class AnimusForgeModulePaths { internal static string GetCurrentModuleRoot()=>throw new Exception("fixture must specify catalog"); }
    internal sealed class TtsEngine { internal static readonly TtsEngine Instance=new(); internal int CancelCount; internal void CancelLocalPlayback(){CancelCount++;} }
}
