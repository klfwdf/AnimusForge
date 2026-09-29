using System;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;
using NativeTexture = TaleWorlds.Engine.Texture;
using TwoDimensionEngineTexture = TaleWorlds.Engine.GauntletUI.EngineTexture;
using GuiTexture = TaleWorlds.TwoDimension.Texture;
using EngineScene = TaleWorlds.Engine.Scene;

namespace RichExecutions.Customization;

public sealed class ExecutionSiteThumbnailWidget : TextureWidget
{
    private string _resourceKey = string.Empty;
    private int _resourceType;

    public ExecutionSiteThumbnailWidget(UIContext context) : base(context)
    {
        TextureProviderName = nameof(ExecutionSitePrefabTableauTextureProvider);
    }

    [Editor(false)]
    public string ResourceKey
    {
        get => _resourceKey;
        set
        {
            value ??= string.Empty;
            if (_resourceKey == value) return;
            _resourceKey = value;
            SetTextureProviderProperty(nameof(ExecutionSitePrefabTableauTextureProvider.ResourceKey), value);
            OnPropertyChanged(value, nameof(ResourceKey));
        }
    }

    [Editor(false)]
    public int ResourceType
    {
        get => _resourceType;
        set
        {
            if (_resourceType == value) return;
            _resourceType = value;
            SetTextureProviderProperty(nameof(ExecutionSitePrefabTableauTextureProvider.ResourceType), value);
            OnPropertyChanged(value, nameof(ResourceType));
        }
    }
}

public sealed class ExecutionSitePrefabTableauTextureProvider : TextureProvider
{
    private EngineScene? _scene;
    private GameEntity? _entity;
    private Camera? _camera;
    private NativeTexture? _engineTexture;
    private GuiTexture? _guiTexture;
    private string _resourceKey = string.Empty;
    private int _resourceType;
    private int _width = 128;
    private int _height = 128;
    private bool _dirty = true;

    public string ResourceKey
    {
        get => _resourceKey;
        set
        {
            value ??= string.Empty;
            if (_resourceKey == value) return;
            _resourceKey = value;
            _dirty = true;
        }
    }

    public int ResourceType
    {
        get => _resourceType;
        set
        {
            if (_resourceType == value) return;
            _resourceType = value;
            _dirty = true;
        }
    }

    public override void SetTargetSize(int width, int height)
    {
        base.SetTargetSize(width, height);
        width = Math.Max(16, width);
        height = Math.Max(16, height);
        if (_width == width && _height == height && !_dirty) return;
        _width = width;
        _height = height;
        _dirty = true;
        EnsureTableau();
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        if (_dirty) EnsureTableau();
        RefreshGuiTexture();
    }

    public override void Clear(bool clearNextFrame)
    {
        ReleaseTableau(clearNextFrame);
        base.Clear(clearNextFrame);
    }

    protected override GuiTexture? OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
    {
        if (_dirty) EnsureTableau();
        RefreshGuiTexture();
        return _guiTexture;
    }

    private void EnsureTableau()
    {
        _dirty = false;
        ReleaseTableau(clearNextFrame: false);
        if (string.IsNullOrWhiteSpace(_resourceKey)) return;
        try
        {
            _scene = EngineScene.CreateNewScene(true, false, DecalAtlasGroup.All, "mono_renderscene");
            _scene.SetName("RichExecutionsPrefabThumbnail");
            var initialization = default(SceneInitializationData);
            initialization.InitPhysicsWorld = false;
            initialization.DoNotUseLoadingScreen = true;
            _scene.Read("crafting_menu_outdoor", ref initialization, string.Empty);
            _scene.DisableStaticShadows(true);
            _scene.SetShadow(true);
            _entity = CreateResourceEntity(_scene, _resourceKey, (ExecutionSiteResourceType)_resourceType);
            if (_entity is null)
            {
                ReleaseTableau(clearNextFrame: false);
                return;
            }

            FitEntityToScene(_scene, _entity);
            _camera = CreateCamera(_scene, _entity);
            _engineTexture = TableauView.AddTableau(
                $"RichExecutionsPrefab_{Math.Abs(_resourceKey.GetHashCode())}_{_width}x{_height}",
                new RenderTargetComponent.TextureUpdateEventHandler(Render),
                _scene,
                _width,
                _height);
            _engineTexture.TableauView.SetSceneUsesContour(false);
            _engineTexture.TableauView.SetContinuousRendering(false);
            _engineTexture.TableauView.SetDeleteAfterRendering(false);
            _engineTexture.TableauView.SetDoNotRenderThisFrame(false);
            RefreshGuiTexture();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Thumbnail generation failed for '{_resourceKey}': {exception.Message}");
            ReleaseTableau(clearNextFrame: false);
        }
    }

    private static GameEntity? CreateResourceEntity(
        EngineScene scene,
        string key,
        ExecutionSiteResourceType type)
    {
        if (type == ExecutionSiteResourceType.Prefab)
        {
            if (!GameEntity.PrefabExists(key)) return null;
            return BannerlordApiCompatibility.InstantiatePrefab(
                scene,
                key,
                createPhysics: false,
                MatrixFrame.Identity,
                callScriptCallbacks: false);
        }

        var mesh = MetaMesh.GetCopy(key, showErrors: false, mayReturnNull: true);
        if (mesh is null || !mesh.IsValid) return null;
        var entity = GameEntity.CreateEmpty(scene, false, false, false);
        entity?.AddMultiMesh(mesh);
        return entity;
    }

    private static void FitEntityToScene(EngineScene scene, GameEntity entity)
    {
        var anchor = scene.FindEntityWithTag("weapon_point");
        var target = anchor?.GetGlobalFrame().origin ?? Vec3.Zero;
        if (anchor is not null) scene.RemoveEntity(anchor, 0);
        var bounds = entity.GetGlobalBoundingBox();
        var span = bounds.max - bounds.min;
        var longest = MathF.Max(0.01f, MathF.Max(span.x, MathF.Max(span.y, span.z)));
        var scale = MathF.Min(2.4f / longest, 3f);
        var frame = MatrixFrame.Identity;
        frame.rotation.ApplyScaleLocal(scale);
        entity.SetGlobalFrame(frame);
        bounds = entity.GetGlobalBoundingBox();
        var center = (bounds.min + bounds.max) * 0.5f;
        frame.origin = target - center;
        entity.SetGlobalFrame(frame);
        entity.SetVisibilityExcludeParents(true);
    }

    private static Camera CreateCamera(EngineScene scene, GameEntity entity)
    {
        var camera = Camera.CreateCamera();
        var cameraPoint = scene.FindEntityWithTag("camera_point");
        if (cameraPoint is not null)
        {
            var dof = Vec3.Zero;
            cameraPoint.GetCameraParamsFromCameraScript(camera, ref dof);
            camera.Frame = cameraPoint.GetGlobalFrame();
            return camera;
        }

        var bounds = entity.GetGlobalBoundingBox();
        var center = (bounds.min + bounds.max) * 0.5f;
        var origin = center + new Vec3(3.2f, -3.2f, 2.4f);
        var forward = center - origin;
        var frame = new MatrixFrame(Mat3.CreateMat3WithForward(in forward), origin);
        camera.Frame = frame;
        return camera;
    }

    private void Render(NativeTexture sender, EventArgs args)
    {
        var scene = sender.UserData as EngineScene;
        var view = sender.TableauView;
        if (scene is null || _camera is null)
        {
            view.SetContinuousRendering(false);
            view.SetDeleteAfterRendering(true);
            return;
        }

        scene.EnsurePostfxSystem();
        scene.SetDofMode(false);
        scene.SetMotionBlurMode(false);
        scene.SetBloom(true);
        _camera.SetFovVertical(MathF.PI / 4f, (float)_width / _height, 0.05f, 200f);
        ((SceneView)view).SetCamera(_camera);
        ((SceneView)view).SetScene(scene);
        ((SceneView)view).SetSceneUsesSkybox(false);
        ((SceneView)view).SetRenderWithPostfx(true);
        view.SetDeleteAfterRendering(false);
        view.SetContinuousRendering(false);
        view.SetDoNotRenderThisFrame(false);
        ((View)view).SetClearColor(0x10101000u);
    }

    private void RefreshGuiTexture()
    {
        if (!ReferenceEquals(_engineTexture, _engineTexture)) return;
        if (_engineTexture is null)
        {
            _guiTexture = null;
            return;
        }

        if (_guiTexture is null)
        {
            _guiTexture = new GuiTexture(new TwoDimensionEngineTexture(_engineTexture));
        }
    }

    private void ReleaseTableau(bool clearNextFrame)
    {
        try
        {
            if (_engineTexture?.TableauView is not null)
            {
                _engineTexture.TableauView.SetEnable(false);
                _engineTexture.TableauView.ClearAll(clearNextFrame, false);
            }
        }
        catch { }
        try { _engineTexture?.Release(); } catch { }
        try { _camera?.ReleaseCameraEntity(); } catch { }
        try { _entity?.Remove(0); } catch { }
        try { _scene?.ClearAll(); } catch { }
        _guiTexture = null;
        _engineTexture = null;
        _camera = null;
        _entity = null;
        _scene = null;
    }
}