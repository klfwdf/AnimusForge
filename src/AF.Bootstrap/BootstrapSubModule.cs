using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Bootstrap
{
    public sealed class BootstrapSubModule : MBSubModuleBase
    {
        private BootstrapRuntime _runtime;
        private bool _implementationPreloaded;
        private bool _loadCompleted;
        private Exception _loadFailure;

        public BootstrapSubModule()
        {
            _runtime = new BootstrapRuntime();
            try
            {
                // Bannerlord constructs every declared submodule before it calls any
                // OnSubModuleLoad callback.  Preload the selected implementation here so
                // Gauntlet's first WidgetInfo.Refresh can discover AnimusForge's custom
                // Widget types.  The implementation lifecycle is still forwarded only
                // from the matching Bootstrap lifecycle callback below.
                _runtime.LoadImplementation();
                _implementationPreloaded = true;
                BootstrapLog.Info(
                    "AnimusForge implementation preloaded during Bootstrap construction for engine type discovery.");
            }
            catch (Exception exception)
            {
                _loadFailure = exception;
                _runtime.ReportFatal("Bootstrap construction / implementation preload", exception);
                _runtime.Dispose();
                throw new InvalidOperationException(
                    "AnimusForge Bootstrap failed closed while preloading the selected implementation. " +
                    "See the Bootstrap UTF-8 log for details.",
                    exception);
            }
        }

        protected override void OnSubModuleLoad()
        {
            if (_loadCompleted)
            {
                BootstrapLog.Warning("Ignoring a duplicate Bootstrap OnSubModuleLoad call.");
                return;
            }

            if (_loadFailure != null)
            {
                throw new InvalidOperationException("AnimusForge Bootstrap previously failed and will not retry in this process.", _loadFailure);
            }

            try
            {
                if (!_implementationPreloaded || _runtime == null)
                {
                    throw new InvalidOperationException(
                        "AnimusForge implementation was not preloaded during Bootstrap construction.");
                }

                _runtime.InvokeLifecycle(nameof(OnSubModuleLoad));
                _loadCompleted = true;
                BootstrapLog.Info("AnimusForge implementation OnSubModuleLoad completed.");
            }
            catch (Exception exception)
            {
                _loadFailure = exception;
                _runtime.ReportFatal(nameof(OnSubModuleLoad), exception);
                _runtime.Dispose();
                throw new InvalidOperationException(
                    "AnimusForge Bootstrap failed closed. See the Bootstrap UTF-8 log for details.", exception);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                if (_loadCompleted)
                {
                    InvokeVoid(nameof(OnSubModuleUnloaded));
                    BootstrapLog.Info("AnimusForge implementation OnSubModuleUnloaded completed.");
                }
            }
            finally
            {
                _runtime?.Dispose();
                _implementationPreloaded = false;
                _loadCompleted = false;
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InvokeVoid(nameof(OnBeforeInitialModuleScreenSetAsRoot));
        }

        protected override void RegisterSubModuleTypes()
        {
            InvokeVoid(nameof(RegisterSubModuleTypes));
        }

        protected override void OnNewModuleLoad()
        {
            InvokeVoid(nameof(OnNewModuleLoad));
        }

        public override void OnConfigChanged()
        {
            InvokeVoid(nameof(OnConfigChanged));
        }

        protected override void OnBeforeGameStart(MBGameManager mbGameManager, List<string> disabledModules)
        {
            InvokeVoid(nameof(OnBeforeGameStart), mbGameManager, disabledModules);
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            InvokeVoid(nameof(OnGameStart), game, gameStarterObject);
        }

        protected override void OnApplicationTick(float dt)
        {
            InvokeVoid(nameof(OnApplicationTick), dt);
        }

        protected override void AfterAsyncTickTick(float dt)
        {
            InvokeVoid(nameof(AfterAsyncTickTick), dt);
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            InvokeVoid(nameof(InitializeGameStarter), game, starterObject);
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            InvokeVoid(nameof(OnGameLoaded), game, initializerObject);
        }

        public override void OnAfterGameLoaded(Game game)
        {
            InvokeVoid(nameof(OnAfterGameLoaded), game);
        }

        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            InvokeVoid(nameof(OnNewGameCreated), game, initializerObject);
        }

        public override void BeginGameStart(Game game)
        {
            InvokeVoid(nameof(BeginGameStart), game);
        }

        public override void OnCampaignStart(Game game, object starterObject)
        {
            InvokeVoid(nameof(OnCampaignStart), game, starterObject);
        }

        public override void RegisterSubModuleObjects(bool isSavedCampaign)
        {
            InvokeVoid(nameof(RegisterSubModuleObjects), isSavedCampaign);
        }

        public override void AfterRegisterSubModuleObjects(bool isSavedCampaign)
        {
            InvokeVoid(nameof(AfterRegisterSubModuleObjects), isSavedCampaign);
        }

        public override void OnMultiplayerGameStart(Game game, object starterObject)
        {
            InvokeVoid(nameof(OnMultiplayerGameStart), game, starterObject);
        }

        public override void OnGameInitializationFinished(Game game)
        {
            InvokeVoid(nameof(OnGameInitializationFinished), game);
        }

        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            InvokeVoid(nameof(OnAfterGameInitializationFinished), game, starterObject);
        }

        public override bool DoLoading(Game game)
        {
            object result = Invoke(nameof(DoLoading), game);
            return result is bool value && value;
        }

        public override void OnGameEnd(Game game)
        {
            InvokeVoid(nameof(OnGameEnd), game);
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            InvokeVoid(nameof(OnMissionBehaviorInitialize), mission);
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            InvokeVoid(nameof(OnBeforeMissionBehaviorInitialize), mission);
        }

        public override void OnInitialState()
        {
            InvokeVoid(nameof(OnInitialState));
        }

        protected override void OnNetworkTick(float dt)
        {
            InvokeVoid(nameof(OnNetworkTick), dt);
        }

        public override void OnSubModuleActivated()
        {
            InvokeVoid(nameof(OnSubModuleActivated));
        }

        public override void OnSubModuleDeactivated()
        {
            InvokeVoid(nameof(OnSubModuleDeactivated));
        }

        public override void InitializeSubModuleGameObjects(Game game)
        {
            InvokeVoid(nameof(InitializeSubModuleGameObjects), game);
        }

        private void InvokeVoid(string methodName, params object[] arguments)
        {
            Invoke(methodName, arguments);
        }

        private object Invoke(string methodName, params object[] arguments)
        {
            if (!_loadCompleted || _runtime == null)
            {
                throw new InvalidOperationException(
                    $"AnimusForge lifecycle '{methodName}' cannot run because Bootstrap did not complete loading.",
                    _loadFailure);
            }

            return _runtime.InvokeLifecycle(methodName, arguments);
        }
    }
}
