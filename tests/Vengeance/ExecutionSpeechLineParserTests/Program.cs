using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using RichExecutions.Scene;

internal static class Program
{
    private static int _failures;

    private static void Main()
    {
        try { Run(); }
        catch (Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                Console.Error.WriteLine(current.GetType().FullName + ": " + current.Message);
            Environment.Exit(1);
        }
    }

    private static void Run()
    {
        HoldsASplitLineUntilTheNewlineArrives();
        KeepsTheUnfinishedTailAcrossChunks();
        DropsUnknownSpeakersAndBlankLines();
        StopsAfterEightLines();
        FlushEmitsOnlyTheFinalTail();
        BoundsASingleLine();
        SplitsOneReplyIntoThreePhases();
        ParsesTheActualEnglishPromptProtocol();
        if (_failures > 0)
        {
            Console.Error.WriteLine(_failures + " assertion(s) failed.");
            Environment.Exit(1);
        }

        Console.WriteLine("execution speech line parser: ok");
    }

    private static void HoldsASplitLineUntilTheNewlineArrives()
    {
        var parser = new ExecutionSpeechLineParser();
        Expect(0, parser.Append("刽子手: 罪名").Count);
        var finished = parser.Append("成立。\n");
        Expect(1, finished.Count);
        Expect(ExecutionSpeechLineRole.Executioner, finished[0].Role);
        Expect("罪名成立。", finished[0].Text);
    }

    private static void KeepsTheUnfinishedTailAcrossChunks()
    {
        var parser = new ExecutionSpeechLineParser();
        var first = parser.Append("死刑犯: 我听见了。\n围观: 他");
        Expect(1, first.Count);
        Expect(ExecutionSpeechLineRole.Victim, first[0].Role);
        var second = parser.Append("不该这样。\n");
        Expect(1, second.Count);
        Expect(ExecutionSpeechLineRole.Crowd, second[0].Role);
        Expect("他不该这样。", second[0].Text);
    }

    private static void DropsUnknownSpeakersAndBlankLines()
    {
        var parser = new ExecutionSpeechLineParser();
        var lines = parser.Append("旁白: 无声\n\nexecutioner: The charge stands.\n玩家: 我来行刑\n");
        Expect(1, lines.Count);
        Expect(ExecutionSpeechLineRole.Executioner, lines[0].Role);
        Expect("The charge stands.", lines[0].Text);
    }

    private static void StopsAfterEightLines()
    {
        var parser = new ExecutionSpeechLineParser();
        var text = string.Join("", Enumerable.Range(1, 30).Select(index => "死刑犯: 第" + index + "句\n"));
        var lines = parser.Append(text);
        Expect(ExecutionSpeechLineParser.MaximumLines, lines.Count);
        Expect(0, parser.Append("死刑犯: 多余\n").Count);
    }

    private static void FlushEmitsOnlyTheFinalTail()
    {
        var parser = new ExecutionSpeechLineParser();
        parser.Append("刽子手: 判决已下。\n死刑犯: 还有一句话");
        var tail = parser.Flush();
        Expect(1, tail.Count);
        Expect("还有一句话", tail[0].Text);
        Expect(0, parser.Flush().Count);
    }

    private static void BoundsASingleLine()
    {
        var parser = new ExecutionSpeechLineParser();
        var lines = parser.Append("围观: " + new string('言', 300) + "\n");
        Expect(1, lines.Count);
        Expect(ExecutionSpeechLineParser.MaximumLineCharacters, lines[0].Text.Length);
    }

    private static void SplitsOneReplyIntoThreePhases()
    {
        var parser = new ExecutionSpeechLineParser();
        var lines = parser.Append("[开场]\n刽子手: 判决已下。\n[行刑中]\n死刑犯: 动手吧。\n围观: 别看。\n[结束后]\n围观: 结束了。\n");
        Expect(4, lines.Count);
        Expect(ExecutionSpeechPhase.Opening, lines[0].Phase);
        Expect(ExecutionSpeechPhase.During, lines[1].Phase);
        Expect(ExecutionSpeechPhase.During, lines[2].Phase);
        Expect(ExecutionSpeechLineRole.Crowd, lines[2].Role);
        Expect(ExecutionSpeechPhase.Aftermath, lines[3].Phase);
    }

    private static void ParsesTheActualEnglishPromptProtocol()
    {
        foreach (string resource in new[] { "Test.ExecutionPrompt.en.json", "Test.RulePrompt.en.json" })
        {
            string prompt;
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream))
                prompt = (string)JObject.Parse(reader.ReadToEnd())["ExecutionCeremonySystemPrompt"];
            foreach (string marker in new[] { "[Opening]", "[during]", "[Aftermath]", "[last_statement]" })
                Expect(true, prompt.Contains(marker));
            Expect(false, prompt.Contains("[During Execution]"));
            Expect(false, prompt.Contains("[Final Statement]"));
            var parser = new ExecutionSpeechLineParser();
            var lines = parser.Append("[Opening]\nExecutioner: Sentence pronounced.\n[last_statement]\nCondemned: Literal {NAME} $1.\nCrowd: We heard it.\n[during]\nCondemned: Now.\n[Aftermath]\nCondemned: Must be rejected.\nCrowd: It is over.\n");
            Expect(5, lines.Count);
            Expect(ExecutionSpeechPhase.Opening, lines[1].Phase);
            Expect(true, lines[1].IsLastStatement);
            Expect("Literal {NAME} $1.", lines[1].Text);
            Expect(false, lines[2].IsLastStatement);
            Expect(ExecutionSpeechPhase.During, lines[3].Phase);
            Expect(false, lines[3].IsLastStatement);
            Expect(ExecutionSpeechPhase.Aftermath, lines[4].Phase);
            Expect(ExecutionSpeechLineRole.Crowd, lines[4].Role);
        }
    }

    private static void Expect<T>(T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        _failures++;
        Console.Error.WriteLine("expected <" + expected + "> but was <" + actual + ">");
    }
}
