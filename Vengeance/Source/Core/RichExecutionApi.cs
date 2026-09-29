using System;

namespace RichExecutions.Core;

public static class RichExecutionApi
{
    private static ExecutionMethodCatalog? _methods;
    private static ExecutionChargeCatalog? _charges;
    private static ExecutionSceneActCatalog? _sceneActs;
    private static ExecutionService? _service;

    public static bool IsInitialized =>
        _methods is not null && _charges is not null && _sceneActs is not null && _service is not null;

    public static ExecutionMethodCatalog Methods =>
        _methods ?? throw NotInitialized();

    public static ExecutionChargeCatalog Charges =>
        _charges ?? throw NotInitialized();

    public static ExecutionSceneActCatalog SceneActs =>
        _sceneActs ?? throw NotInitialized();

    public static ExecutionService Service =>
        _service ?? throw NotInitialized();

    internal static void InitializeDefaults()
    {
        _methods = ExecutionMethodCatalog.CreateDefault();
        _charges = ExecutionChargeCatalog.CreateDefault();
        _sceneActs = ExecutionSceneActCatalog.CreateDefault();
        _service = new ExecutionService();
        _service.AddConsequenceRule(new DefaultExecutionConsequenceRule());
    }

    private static InvalidOperationException NotInitialized() =>
        new("Vengeance has not initialized for a campaign yet.");
}
