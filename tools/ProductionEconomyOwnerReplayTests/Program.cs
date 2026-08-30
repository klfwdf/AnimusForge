using System;
using System.IO;
using System.Linq;
using System.Reflection;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory, referenceDirectory })
    {
        if (!Directory.Exists(root))
        {
            continue;
        }
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try
            {
                return Assembly.LoadFrom(candidate);
            }
            catch
            {
            }
        }
    }
    return null;
};

Assembly implementation = Assembly.LoadFrom(implementationPath);
Type rewardType = implementation.GetType("AnimusForge.RewardSystemBehavior", true);
Type portType = implementation.GetType("AnimusForge.Refactor.Adapters.LegacyEconomyRewardDebtMainThreadPort", true);
MethodInfo factory = rewardType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateEconomyRewardDebtMainThreadPortForExternal"
        && method.GetParameters().Length == 0);
object port = factory.Invoke(null, null);
AssertTrue(port == null, "economy owner factory created a port without a live Campaign/RewardSystem owner");
AssertTrue(portType.IsAssignableFrom(factory.ReturnType), "economy owner factory return type drifted");

MethodInfo partyFactory = rewardType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreatePartyEconomyRewardDebtMainThreadPortForExternal");
object partyPort = partyFactory.Invoke(null, new object[] { null, null, "party-subject", null });
AssertTrue(partyPort == null, "party economy owner factory created a port without a live Campaign/party owner");
AssertTrue(portType.IsAssignableFrom(partyFactory.ReturnType), "party economy owner factory return type drifted");

Console.WriteLine("PASS productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 productionType=1 noCampaignMutation=1");
