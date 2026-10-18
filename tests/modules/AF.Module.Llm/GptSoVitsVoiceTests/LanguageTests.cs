using AnimusForge;
using Newtonsoft.Json.Linq;
using System.Net;

static class LanguageTests
{
    internal static async Task Run(GptSoVitsVoice voice, byte[] wave, Action<bool,string> check)
    {
        foreach (var group in new[] { new[]{"zh","all_zh","中文","普通话","Chinese","zh-CN"}, new[]{"yue","all_yue","粤语","广东话","Cantonese","zh-HK"}, new[]{"ja","all_ja","日语","日本語","Japanese","ja-JP"}, new[]{"en","en","英语","英文","English","en-US"} })
        {
            foreach (string alias in group.Skip(2)) check(GptSoVitsLanguage.Parse(" "+alias+" ").EngineCode == group[1], "alias maps to forced engine code "+alias);
            var language = GptSoVitsLanguage.Parse(group[0]);
            var request = new GptSoVitsSynthesisRequest("http://127.0.0.1:9880/tts", "original", voice, 1, "fixture", language).WithTone(LocalTtsTone.Angry).WithText("speech");
            using var http = new HttpClient(new Handler(async message => {
                var body = JObject.Parse(await message.Content.ReadAsStringAsync());
                check((string)body["text_lang"] == group[1], "real HTTP locks output language");
                check((string)body["prompt_lang"] == voice.PromptLanguage && (string)body["prompt_text"] == voice.PromptText, "reference transcript language never changed");
                check((string)body["text"] == "speech", "immutable copies keep new text and language");
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(wave)};
            }));
            check((await new GptSoVitsTtsGateway(http).SynthesizeAsync(request,CancellationToken.None)).Success, "actual gateway request accepted");
        }
        foreach (string input in new[]{null,"","auto","自动"}) check(GptSoVitsLanguage.Parse(input)==GptSoVitsLanguage.Auto,"blank retains legacy auto");
        foreach (string input in new[]{"韩语","French","zh;ja","[ACTION:END]",new string('a',65)}) check(!GptSoVitsLanguage.Parse(input).IsSupported,"unknown does not silently auto-detect");
        var ja=GptSoVitsLanguage.Parse("日语"); var zh=GptSoVitsLanguage.Parse("中文");
        string body="你好，旅人！", translated="こんにちは、旅人さん！";
        string Meta(string json)=>LocalTtsSpeechTag.Open+json+LocalTtsSpeechTag.Close;
        string raw=Meta("{\"language\":\"ja\",\"mode\":\"translate\",\"text\":\""+translated+"\"}");
        string action="[ACTION:MOOD:ANNOYED][AD:1][RELAY:2]";
        check(LocalTtsSpeechTag.Extract(action+raw,ja,body,out string speech,out bool changed)==action && speech==translated && changed,"translation separated, game tags byte intact");
        check(LocalTtsSpeechTag.Strip(body+raw)==body,"hidden translation never visible");
        for(int i=0;i<=raw.Length;i++) check(LocalTtsSpeechTag.StripPreview(body+raw[..i])==body,"all incremental metadata prefixes hidden");
        LocalTtsSpeechTag.Extract(Meta("{\"language\":\"zh\",\"mode\":\"keep\",\"text\":\"Do not use this rewrite\"}"),zh,body,out speech,out changed);
        check(speech==body&&!changed,"same language returns original byte-for-byte");
        check(new LocalTtsSpeech(LocalTtsTone.Angry,zh,speech,false).TrySelectText(zh,body,out string selected)&&selected==body,"keep playback never rewritten");
        foreach(string malformed in new[]{raw+raw,raw[..^2],raw.Replace("ja","en"),raw.Replace("translate","unknown"),Meta("{bad}"),Meta("{\"language\":\"ja\",\"language\":\"ja\",\"mode\":\"keep\"}"),Meta("{\"language\":\"ja\",\"mode\":\"translate\",\"text\":\"[ACTION:GIVE_GOLD:99]\"}")})
        { var clean=LocalTtsSpeechTag.Extract(action+malformed,ja,body,out speech,out changed);check(speech==null&&clean==action,"malformed/duplicate/injected payload cannot speak or execute"); }
        check(LocalTtsSpeechTag.Extract(action+raw,null,body,out speech,out changed)==action&&speech==null,"unsolicited metadata stripped without granting translation");
        check(!LocalTtsSpeech.Neutral.TrySelectText(ja,body,out selected),"missing translation never forces wrong pronunciation");
        var done=new LocalTtsSpeech(LocalTtsTone.Happy,ja,translated,true);
        check(!done.TrySelectText(zh,body,out selected),"changed setting rejects old-language reply");
        check(done.TrySelectText(GptSoVitsLanguage.Auto,body,out selected)&&selected==body,"auto does not leak translation");
        var pending=new TaskCompletionSource<LocalTtsSpeech>();
        var waited=LocalTtsSpeech.AwaitAsync(pending.Task,CancellationToken.None,5);
        check(await waited==LocalTtsSpeech.Neutral,"bounded speech timeout");pending.SetResult(done);
        check(await waited==LocalTtsSpeech.Neutral,"late translation cannot revive timed out speech");
        using var c=new CancellationTokenSource();var cancelled=LocalTtsSpeech.AwaitAsync(new TaskCompletionSource<LocalTtsSpeech>().Task,c.Token,10000);c.Cancel();
        try{await cancelled;check(false,"cancellation lost");}catch(OperationCanceledException){check(true,"cancellation preserved");}
        check(await LocalTtsSpeech.AwaitAsync(Task.FromException<LocalTtsSpeech>(new Exception("fixture")),CancellationToken.None,5)==LocalTtsSpeech.Neutral,"postprocess failure is text-only");
    }
    sealed class Handler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage,Task<HttpResponseMessage>> send;
        internal Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> send)=>this.send=send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage m,CancellationToken c)=>send(m);
    }
}
