using System;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    // Inspection lights belong only to the frozen snapshot. They are not a
    // reconstruction of the mission's lighting, and never inspect source lights.
    internal static class PanoramaObservationLighting
    {
        internal const int LightCount = 3;

        internal static JObject AddToSnapshot(PanoramaSceneSnapshot snapshot, MatrixFrame[] frames)
        {
            IllustratorRuntime.AssertMainThread();
            if (snapshot == null || snapshot.IsDisposed || snapshot.Scene == null || snapshot.Scene.Pointer == UIntPtr.Zero)
                throw new ArgumentException("观察补光需要有效的独立场景副本。", nameof(snapshot));
            if (frames == null || frames.Length == 0)
                throw new ArgumentException("观察补光缺少离屏镜头位置。", nameof(frames));

            Vec3 origin = frames[0].origin;
            Vec3 forward = new Vec3(-frames[0].rotation.u.x, -frames[0].rotation.u.y, 0f);
            float length = (float)Math.Sqrt(forward.x * forward.x + forward.y * forward.y);
            if (!Finite(origin) || !Finite(snapshot.CaptureCenter) || !Finite(forward) ||
                float.IsInfinity(length) || length < 0.0001f)
                throw new InvalidOperationException("观察补光的离屏镜头坐标无效。");
            forward *= 1f / length;
            Vec3 right = new Vec3(forward.y, -forward.x, 0f);
            Vec3 raisedOrigin = origin + new Vec3(0f, 0f, 0.75f);
            // One shared, stationary rig covers both views. Off-axis lamps reveal
            // surfaces facing away from the camera without per-face native writes.
            var positions = new[]
            {
                raisedOrigin,
                raisedOrigin + forward * 4f + right * 2f,
                raisedOrigin - forward * 4f - right * 2f
            };
            var intensities = new[] { 8f, 6f, 6f };
            var lights = new JArray();
            for (int i = 0; i < LightCount; i++)
            {
                float distance = (float)Math.Sqrt(positions[i].DistanceSquared(snapshot.CaptureCenter));
                float radius = ScreenCaptureHelper.PanoramaCaptureRadius + distance + 5f;
                if (float.IsNaN(radius) || float.IsInfinity(radius))
                    throw new InvalidOperationException("观察补光的覆盖范围无效。");
                string name = "afi_l" + i;
                AddPointLight(snapshot.Scene, name, positions[i], radius, intensities[i]);
                lights.Add(new JObject
                {
                    ["name"] = name,
                    ["position"] = new JArray(positions[i].x, positions[i].y, positions[i].z),
                    ["radiusMeters"] = radius,
                    ["intensity"] = intensities[i]
                });
            }
            // Detached values only: callers can persist this on their background
            // diagnostic path without reading a native object off the game thread.
            return new JObject
            {
                ["purpose"] = "neutral_geometry_inspection_not_scene_lighting",
                ["count"] = LightCount,
                ["color"] = new JArray(1f, 1f, 1f),
                ["shadows"] = false,
                ["volumetric"] = false,
                ["sourceLightsCopied"] = false,
                ["lights"] = lights
            };
        }

        private static void AddPointLight(Scene scene, string name, Vec3 position, float radius, float intensity)
        {
            GameEntity carrier = null;
            Light light = null;
            bool attached = false;
            try
            {
                // This is a new empty carrier in the owned scene, not CopyFrom.
                // No physics, scripts, simulation or visible mesh is introduced.
                carrier = GameEntity.CreateEmpty(scene, isModifiableFromEditor: false,
                    createPhysics: false, callScriptCallbacks: false);
                if (carrier == null || carrier.Pointer == UIntPtr.Zero || carrier.Scene?.Pointer != scene.Pointer)
                    throw new InvalidOperationException("无法在独立场景创建观察灯。");
                if (name.Length > 16) throw new InvalidOperationException("原生观察灯名称超过安全长度。");
                carrier.Name = name;
                carrier.EntityFlags |= EntityFlags.DoNotTick | EntityFlags.DontTickChildren;
                // Dynamic rendering registration does not animate/tick this frozen
                // entity; it ensures a newly created light is not treated as baked.
                carrier.SetMobility(GameEntity.Mobility.Dynamic);
                MatrixFrame frame = MatrixFrame.Identity;
                frame.origin = position;
                carrier.SetFrame(ref frame);

                light = Light.CreatePointLight(radius);
                if (light == null || !light.IsValid)
                    throw new InvalidOperationException("引擎未创建有效的观察灯。");
                light.LightColor = new Vec3(1f, 1f, 1f);
                light.Intensity = intensity;
                light.ShadowEnabled = false;
                light.SetShadowType(Light.ShadowType.NoShadow);
                light.SetVolumetricProperties(false, 0f);
                light.SetLightFlicker(0f, 1f);
                if (!carrier.AddLight(light))
                    throw new InvalidOperationException("无法将观察灯挂载到独立场景。");
                attached = true;
                if (light.GetEntity().Pointer != carrier.Pointer)
                    throw new InvalidOperationException("观察灯未归属于独立场景实体。");
                light.SetVisibility(true);
                carrier.UpdateVisibilityMask();
            }
            finally
            {
                try
                {
                    if (light != null && light.IsValid)
                    {
                        try
                        {
                            // AddLight gives the scene entity ownership. Do not call
                            // Light.Dispose/Release on an attached component: it must
                            // live until the existing snapshot retirement clears it.
                            if (!attached) light.Dispose();
                        }
                        finally
                        {
                            // Light has its own finalizer in addition to NativeObject.
                            // ManualInvalidate alone would leave its later Release
                            // callback targeting a component retired with the scene.
                            GC.SuppressFinalize(light);
                            light.ManualInvalidate();
                        }
                    }
                }
                finally
                {
                    // A created carrier is scene-owned even if setup fails. The
                    // renderer's existing failure path retires that whole snapshot.
                    if (carrier != null && carrier.Pointer != UIntPtr.Zero) carrier.ManualInvalidate();
                }
            }
        }

        private static bool Finite(Vec3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
