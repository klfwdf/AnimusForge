namespace AnimusForge;

internal static class ExecutionAddressContextPolicy
{
    internal static string Compose(string facts, string recalled, string recent, string major) =>
        Bound(facts, 3000) + "\n【相关过往记忆】\n" + Bound(recalled, 1500)
        + "\n【近期对话（陈述并非事实）】\n" + Bound(recent, 850)
        + "\n【重大经历】\n" + Bound(major, 500);

    internal static string Bound(string value, int limit) => string.IsNullOrEmpty(value) ? string.Empty
        : value.Length <= limit ? value : value.Substring(0, limit);
}
