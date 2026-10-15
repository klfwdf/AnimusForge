using System;
using System.Threading;
using AnimusForge;
using AnimusForge.DialogueUI.Native;

int checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
var reveal = new StreamingDialogueTextReveal();
reveal.Update("你好世界", false, 0);
Check(reveal.TryAdvance(0, out var text) && text == "你", "first glyph starts on first tick");
Check(!reveal.TryAdvance(0, out _), "same tick cannot dump the chunk");
Check(reveal.TryAdvance(.051, out text) && text == "你好", "20 glyphs per second");
Check(reveal.TryAdvance(100, out text) && text == "你好世界", "pause catchup is bounded by available text");
reveal.Update("你好世界新的一段长句", false, 200);
Check(reveal.TryAdvance(200, out text) && text == "你好世界新", "network stall cannot bank credit for next chunk");
reveal.Update("你好世界新的一段长句", true, 200);
Check(reveal.IsActive, "completion keeps unread characters queued");
reveal.TryAdvance(200.051, out text);
Check(text == "你好世界新的", "completion does not jump to full text");
for (int i = 1; i <= 30; i++) reveal.TryAdvance(201 + i * .03, out _);
Check(reveal.IsComplete && reveal.CurrentText == "你好世界新的一段长句", "complete text drains exactly");
reveal.Reset(); reveal.Update("e\u0301🙂中文\nEnglish", false, 0);
reveal.TryAdvance(0, out text); Check(text == "e\u0301", "combining glyph stays together");
reveal.TryAdvance(.051, out text); Check(text == "e\u0301🙂", "surrogate pair stays together");
reveal.Reset(); reveal.Update("\ud83d", false, 0);
Check(!reveal.TryAdvance(0, out _), "trailing high surrogate waits for partner");
reveal.Update("🙂字", false, 1);
Check(reveal.TryAdvance(1, out text) && text == "🙂", "SSE split surrogate becomes one glyph");
reveal.Reset(); reveal.Update(new string('中', 1000), false, 0); reveal.TryAdvance(0, out _);
reveal.TryAdvance(60, out text); Check(text.Length == 3, "long frame reveals at most two new glyphs");
reveal.Update("修改", true, 60); reveal.TryAdvance(60.03, out text);
Check(!text.Contains('中'), "revised preview removes superseded prefix");
reveal.Reset();
for (int i = 1; i <= 20; i++)
{
    reveal.Update(new string('字', i), false, i * .01);
    reveal.TryAdvance(i * .01, out _);
}
Check(reveal.CurrentText.Length <= 5, "one-glyph network chunks cannot bypass 20-per-second pacing");
reveal.TryAdvance(2, out _);
Check(reveal.CurrentText.Length <= 7, "long stalls cannot accumulate a burst budget");

var vm = new FakeVm(); var owner = new object();
ConversationHelper.SetCurrentVM(vm); ConversationHelper.BeginStreaming(owner, true);
var overlay = new AnimusForgeNativeConversationOverlay();
overlay.Preview(1, "收到内容立即开始逐字显示"); ConversationHelper.Tick();
Check(vm.DialogText.Length > 0 && vm.DialogText.Length < 13, "real overlay and helper show a prefix");
string prefix = vm.DialogText;
ConversationHelper.StartTypewriterText("TTS should not clear this", 1, true);
Check(!ConversationHelper.IsTypewriterWaitingForPlayback, "streaming TTS cannot take over display");
ConversationHelper.OnRefreshPostfix();
Check(vm.DialogText == prefix, "refresh preserves visible prefix");
overlay.Complete(1, "收到内容立即开始逐字显示", "<a href=\"trusted\">收到内容立即开始逐字显示</a>");
ConversationHelper.EndStreaming(owner); ConversationHelper.Tick();
Check(!vm.DialogText.Contains("<a"), "completed rich links wait for remaining characters");
for (int i = 0; i < 50 && ConversationHelper.IsTypewriterActive; i++) { Thread.Sleep(20); ConversationHelper.Tick(); }
ConversationHelper.Tick();
Check(vm.DialogText == "<a href=\"trusted\">收到内容立即开始逐字显示</a>", "final links installed once without exposing partial tags");
Check(!ConversationHelper.IsTypewriterActive, "reveal finishes after network completion");
prefix = vm.DialogText; overlay.Preview(0, "stale"); ConversationHelper.Tick();
Check(vm.DialogText == prefix, "retired callback cannot replace current text");

var scroll = new TestScroll { FollowStreamingText = true, NextLayoutMaximum = 300 };
ConversationHelper.BeginStreaming(owner, true); overlay.Preview(1, "新字"); ConversationHelper.Tick();
scroll.Frame(); Check(scroll.VerticalScrollbar.ValueFloat == 300, "follows measured wrapped overflow");
scroll.NextLayoutMaximum = 320; scroll.Frame(); Check(scroll.VerticalScrollbar.ValueFloat == 320, "follows delayed layout height");
scroll.Frame(); scroll.VerticalScrollbar.ValueFloat = 50; scroll.Frame();
Check(scroll.VerticalScrollbar.ValueFloat == 50, "settled text releases manual scroll");
var answers = new TestScroll { NextLayoutMaximum = 500 }; answers.Frame();
Check(answers.VerticalScrollbar.ValueFloat == 0, "answer panel never opts into reply following");

ConversationHelper.BeginStreaming(owner, false);
overlay.Complete(1, "一次显示完整文字", "<a>一次显示完整文字</a>"); ConversationHelper.Tick();
Check(vm.DialogText == "<a>一次显示完整文字</a>", "stream off displays complete reply exactly as before");
ConversationHelper.StartTypewriterText("旧TTS文字", 1, true);
Check(ConversationHelper.IsTypewriterWaitingForPlayback, "stream off retains legacy TTS wait");
Check(ConversationHelper.StartTypewriterPlaybackIfWaiting(), "stream off retains audio-triggered typewriter");
scroll.VerticalScrollbar.ValueFloat = 40; scroll.NextLayoutMaximum = 900; scroll.Frame();
Check(scroll.VerticalScrollbar.ValueFloat == 40, "stream off does not move scrollbar");
ConversationHelper.BeginStreaming(owner, true); overlay.Preview(1, "<img src=bad>{danger}"); ConversationHelper.Tick();
Check(!vm.DialogText.Contains('<') && !vm.DialogText.Contains('{'), "untrusted markup remains sanitized");
ConversationHelper.UpdateDialogText("恢复原文"); ConversationHelper.Tick();
Check(vm.DialogText == "恢复原文" && !ConversationHelper.IsTypewriterActive, "restore clears queued reveal");
overlay.Preview(1, "old stream"); ConversationHelper.ClearForOwner(new object());
Check(ConversationHelper.IsStreaming, "unrelated owner cannot cancel active stream");
var replacement = new FakeVm { DialogText = "new NPC" }; ConversationHelper.SetCurrentVM(replacement); ConversationHelper.Tick();
Check(replacement.DialogText == "new NPC" && !ConversationHelper.IsStreamingTextRevealEnabled, "VM replacement clears reveal and opt-in");
ConversationHelper.Clear();
Console.WriteLine($"PASS {checks} reveal/helper/overlay/scroll checks + prefab and both submission bindings");

public sealed class FakeVm { public string DialogText { get; set; } = "original"; }
public sealed class TestScroll : AFDialogueClickThroughScrollPanel
{
    public TestScroll() : base(new TaleWorlds.GauntletUI.UIContext()) { }
    public void Frame() => OnLateUpdate(1f / 60);
}
