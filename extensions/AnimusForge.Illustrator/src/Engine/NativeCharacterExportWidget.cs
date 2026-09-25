using System;
using System.Reflection;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace AnimusForge.Illustrator.Engine
{
    // Constructed only for a requested portrait export. Vanilla still owns the
    // tableau, provider updates and deferred cleanup; only the screen blit is omitted.
    internal sealed class NativeCharacterExportWidget : CharacterTableauWidget
    {
        public NativeCharacterExportWidget(UIContext context) : base(context)
        {
            // Identity references use the native display idle, never the scene action.
            // Set the public property explicitly so provider creation receives it.
            IdleAction = "act_inventory_idle_start";
            IsEquipmentAnimActive = false;
            CustomAnimation = string.Empty;
            IsPlayingCustomAnimations = false;
            ShouldLoopCustomAnimation = false;
            LeftHandWieldedEquipmentIndex = -1;
            RightHandWieldedEquipmentIndex = -1;
        }

        private object _lastProvider;
        private Vec2 _lastSize;
        private long _lastUpdateFrame = -1;
        internal int ExportUpdateCount { get; private set; }

        protected override void OnUpdate(float dt)
        {
            bool providerWillTick = _isRenderRequestedPreviousFrame && IsRecursivelyVisible();
            base.OnUpdate(dt);
            if (!ReferenceEquals(_lastProvider, TextureProvider) || _lastSize.X != Size.X || _lastSize.Y != Size.Y)
            {
                _lastProvider = TextureProvider;
                _lastSize = Size;
                ExportUpdateCount = 0;
                _lastUpdateFrame = -1;
            }
            long frame = IllustratorRuntime.ApplicationFrame;
            if (providerWillTick && TextureProvider != null && Size.X > 0 && Size.Y > 0 && frame != _lastUpdateFrame)
            {
                _lastUpdateFrame = frame;
                ExportUpdateCount++;
            }
        }

        // At most twice per requested portrait, never a frame scan or camera mutation.
        internal JObject DescribeExportState()
        {
            var result = new JObject
            {
                ["applicationFrame"] = IllustratorRuntime.ApplicationFrame,
                ["providerUpdates"] = ExportUpdateCount,
                ["stance"] = StanceIndex,
                ["width"] = Size.X, ["height"] = Size.Y,
                ["configuredIdleAction"] = IdleAction,
                ["configuredCustomAnimation"] = CustomAnimation,
                ["playingCustomAnimation"] = IsPlayingCustomAnimations,
                ["equipmentAnimationEnabled"] = IsEquipmentAnimActive,
                ["gpuCompletionVerified"] = false
            };
            try
            {
                var provider = TextureProvider;
                var owner = provider?.GetType().GetField("_characterTableau", BindingFlags.Instance | BindingFlags.NonPublic);
                var tableau = owner?.GetValue(provider) as CharacterTableau;
                if (tableau == null) { result["nativeState"] = "unavailable"; return result; }
                if (ReadField(tableau, "_isFinalized") is bool finalized && finalized)
                { result["nativeState"] = "finalized"; return result; }
                foreach (string field in new[] { "_isVisualsDirty", "_agentVisualLoadingCounter", "_mountVisualLoadingCounter", "_initialLoadingCounter", "_cameraRatio", "_tableauSizeX", "_tableauSizeY", "_customAnimationName" })
                {
                    object value = ReadField(tableau, field);
                    result[field] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
                }
                if (ReadField(tableau, "_camPos") is MatrixFrame camera) result["requestedCamera"] = DescribeFrame(camera);
                if (ReadField(tableau, "_camPosGatheredFromScene") is MatrixFrame source) result["inventoryCamera"] = DescribeFrame(source);
                if (ReadField(tableau, "_continuousRenderCamera") is TaleWorlds.Engine.Camera renderCamera)
                    result["submittedCamera"] = DescribeFrame(renderCamera.Frame);
                if (ReadField(tableau, "_agentVisuals") is AgentVisuals visuals)
                {
                    var entity = visuals.GetEntity();
                    if (entity != null)
                    {
                        result["characterFrame"] = DescribeFrame(entity.GetGlobalFrame());
                        var skeleton = entity.Skeleton;
                        if (skeleton != null)
                        {
                            result["actionChannel0"] = skeleton.GetActionAtChannel(0).GetName();
                            result["actionProgress0"] = skeleton.GetAnimationParameterAtChannel(0);
                        }
                    }
                }
            }
            catch (Exception ex) { result["nativeStateError"] = ex.GetType().Name + ": " + ex.Message; }
            return result;
        }

        private static object ReadField(CharacterTableau tableau, string name) =>
            typeof(CharacterTableau).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tableau);

        private static JObject DescribeFrame(MatrixFrame frame) => new JObject
        {
            ["origin"] = DescribeVector(frame.origin),
            ["s"] = DescribeVector(frame.rotation.s),
            ["f"] = DescribeVector(frame.rotation.f),
            ["u"] = DescribeVector(frame.rotation.u)
        };

        private static JArray DescribeVector(Vec3 value) => new JArray(value.x, value.y, value.z);

        protected override void OnRender(TwoDimensionContext context, TwoDimensionDrawContext drawContext)
        {
            // TextureWidget.OnUpdate requires this flag to create and tick its
            // provider. Keep that lifecycle without calling the vanilla OnRender,
            // which submits a screen draw even for this temporary export widget.
            _isRenderRequestedPreviousFrame = true;
        }
    }
}
