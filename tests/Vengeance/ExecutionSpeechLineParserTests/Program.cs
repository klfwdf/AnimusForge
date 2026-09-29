using System;
using System.Linq;
using RichExecutions.Scene;

internal static class Program
{
    private static int _failures;

    private static void Main()
    {
        HoldsASplitLineUntilTheNewlineArrives();
        KeepsTheUnfinishedTailAcrossChunks();
        DropsUnknownSpeakersAndBlankLines();
        StopsAfterEightLines();
        FlushEmitsOnlyTheFinalTail();
        BoundsASingleLine();
        SplitsOneReplyIntoThreePhases();
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

    private static void Expect<T>(T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        _failures++;
        Console.Error.WriteLine("expected <" + expected + "> but was <" + actual + ">");
    }
}
