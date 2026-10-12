using System;
using System.Reflection;
using AnimusForge;
using TaleWorlds.Library;

static class Program
{
    private static int _checks;
    private static readonly MethodInfo Route = typeof(NonBlockingErrorReport).GetMethod(
        "CanRouteAcknowledgementInquiry", BindingFlags.NonPublic | BindingFlags.Static);

    private static void Check(bool value, string name)
    {
        _checks++;
        if (!value) throw new InvalidOperationException(name);
    }

    private static bool Routes(string title, string detail, bool negative = false, bool timeout = false)
    {
        var inquiry = new InquiryData
        {
            TitleText = title,
            Text = detail,
            IsAffirmativeOptionShown = true,
            IsNegativeOptionShown = negative,
            TimeoutAction = timeout ? () => { } : null
        };
        return (bool)Route.Invoke(null, new object[] { inquiry });
    }

    public static void Main()
    {
        Check(!Routes("Operation completed", "No error was reported."), "English success title stays modal");
        Check(!Routes("Request succeeded", "A prior attempt failed."), "English succeeded title stays modal");
        Check(!Routes("Completed successfully", "An old failure was recorded."), "English successful title stays modal");
        Check(!Routes("操作完成", "之前发生错误。"), "Chinese success title stays modal");
        Check(Routes("Operation failed", "The request timed out."), "English failure routes to message feed");
        Check(Routes("操作失败", "请求超时。"), "Chinese failure routes to message feed");
        Check(Routes("Task failed but completed", "Check diagnostics."), "Explicit failure title still routes");
        Check(!Routes("Operation failed", "Retry?", negative: true), "Retry/abort decision stays modal");
        Check(!Routes("Operation failed", "Wait?", timeout: true), "Timed inquiry stays modal");

        NonBlockingErrorReport.Show("Operation failed", "raw diagnostic 42", true, "localized detail 42");
        Check(Logger.Last.Contains("raw diagnostic 42") && !Logger.Last.Contains("localized detail 42"), "Log keeps raw diagnostic");
        Check(AiErrorAnalysisInquiry.LastDetail == "raw diagnostic 42", "Analysis keeps raw diagnostic");
        Check(InformationManager.LastMessage.Contains("localized detail 42") && !InformationManager.LastMessage.Contains("raw diagnostic 42"), "Visible message uses projection");
        Console.WriteLine($"PASS NonBlockingErrorReport routing and diagnostic boundary: {_checks} checks");
    }
}
