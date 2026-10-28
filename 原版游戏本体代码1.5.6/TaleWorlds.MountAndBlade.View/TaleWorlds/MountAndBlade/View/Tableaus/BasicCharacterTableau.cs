using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class BasicCharacterTableau
{
	private static int _tableauIndex;

	private bool _isVersionCompatible;

	private const int _expectedCharacterCodeVersion = 4;

	private bool _initialized;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private float RenderScale = 1f;

	private float _cameraRatio;

	private List<string> _equipmentMeshes;

	private List<bool> _equipmentHasColors;

	private List<bool> _equipmentHasGenderVariations;

	private List<bool> _equipmentHasTableau;

	private uint _clothColor1;

	private uint _clothColor2;

	private MatrixFrame _mountSpawnPoint;

	private MatrixFrame _initialSpawnFrame;

	private Scene _tableauScene;

	private SkinMask _skinMeshesMask;

	private bool _isFemale;

	private string _skeletonName;

	private string _characterCode;

	private Equipment.UnderwearTypes _underwearType;

	private string _mountMeshName;

	private string _mountCreationKey;

	private string _mountMaterialName;

	private uint _mountManeMeshMultiplier;

	private ArmorComponent.BodyMeshTypes _bodyMeshType;

	private ArmorComponent.HairCoverTypes _hairCoverType;

	private ArmorComponent.BeardCoverTypes _beardCoverType;

	private ArmorComponent.BodyDeformTypes _bodyDeformType;

	private string _mountSkeletonName;

	private string _mountIdleAnimationName;

	private string _mountHarnessMeshName;

	private string _mountReinsMeshName;

	private string[] _maneMeshNames;

	private bool _mountHarnessHasColors;

	private bool _isFirstFrame;

	private float _faceDirtAmount;

	private float _mainCharacterRotation;

	private bool _isVisualsDirty;

	private bool _isRotatingCharacter;

	private bool _isEnabled;

	private int _race;

	private readonly GameEntity[] _currentCharacters;

	private readonly GameEntity[] _currentMounts;

	private int _currentEntityToShowIndex;

	private bool _checkWhetherEntitiesAreReady;

	private BodyProperties _bodyProperties = BodyProperties.Default;

	private Banner _banner;

	public Texture Texture { get; private set; }

	public bool IsVersionCompatible => _isVersionCompatible;

	private TableauView View => Texture?.TableauView;

	public BasicCharacterTableau()
	{
		_isFirstFrame = true;
		_isVisualsDirty = false;
		_bodyProperties = BodyProperties.Default;
		_currentCharacters = new GameEntity[2];
		_currentCharacters[0] = null;
		_currentCharacters[1] = null;
		_currentMounts = new GameEntity[2];
		_currentMounts[0] = null;
		_currentMounts[1] = null;
	}

	public void OnTick(float dt)
	{
		if (_isEnabled && _isRotatingCharacter)
		{
			UpdateCharacterRotation((int)Input.MouseMoveX);
		}
		View?.SetDoNotRenderThisFrame(value: false);
		if (_isFirstFrame)
		{
			FirstTimeInit();
			_isFirstFrame = false;
		}
		if (_isVisualsDirty)
		{
			RefreshCharacterTableau();
			_isVisualsDirty = false;
		}
		if (_checkWhetherEntitiesAreReady)
		{
			int num = (_currentEntityToShowIndex + 1) % 2;
			bool flag = true;
			if (!_currentCharacters[_currentEntityToShowIndex].CheckResources(addToQueue: true, checkFaceResources: true))
			{
				flag = false;
			}
			if (!_currentMounts[_currentEntityToShowIndex].CheckResources(addToQueue: true, checkFaceResources: true))
			{
				flag = false;
			}
			if (!flag)
			{
				_currentCharacters[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: false);
				_currentMounts[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: false);
				_currentCharacters[num].SetVisibilityExcludeParents(visible: true);
				_currentMounts[num].SetVisibilityExcludeParents(visible: true);
			}
			else
			{
				_currentCharacters[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: true);
				_currentMounts[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: true);
				_currentCharacters[num].SetVisibilityExcludeParents(visible: false);
				_currentMounts[num].SetVisibilityExcludeParents(visible: false);
				_checkWhetherEntitiesAreReady = false;
			}
		}
	}

	private void UpdateCharacterRotation(int mouseMoveX)
	{
		if (_initialized && _skeletonName != null)
		{
			_mainCharacterRotation += (float)mouseMoveX * 0.005f;
			MatrixFrame frame = _initialSpawnFrame;
			frame.rotation.RotateAboutUp(_mainCharacterRotation);
			_currentCharacters[0].SetFrame(ref frame);
			_currentCharacters[1].SetFrame(ref frame);
		}
	}

	private void SetEnabled(bool enabled)
	{
		_isEnabled = enabled;
		View?.SetEnable(_isEnabled);
	}

	public void SetTargetSize(int width, int height)
	{
		_isRotatingCharacter = false;
		if (width <= 0 || height <= 0)
		{
			_tableauSizeX = 10;
			_tableauSizeY = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			_tableauSizeX = (int)((float)width * RenderScale);
			_tableauSizeY = (int)((float)height * RenderScale);
		}
		_cameraRatio = (float)_tableauSizeX / (float)_tableauSizeY;
		View?.SetEnable(value: false);
		View?.AddClearTask(clearOnlySceneview: true);
		Texture?.Release();
		Texture = TableauView.AddTableau($"BasicCharacterTableau_{_tableauIndex++}", CharacterTableauContinuousRenderFunction, _tableauScene, _tableauSizeX, _tableauSizeY);
		View.SetScene(_tableauScene);
		View.SetSceneUsesSkybox(value: false);
		View.SetDeleteAfterRendering(value: false);
		View.SetContinuousRendering(value: true);
		View.SetDoNotRenderThisFrame(value: true);
		View.SetClearColor(0u);
		View.SetFocusedShadowmap(enable: true, ref _initialSpawnFrame.origin, 1.55f);
		View.SetRenderWithPostfx(value: true);
		SetCamera();
	}

	public void OnFinalize()
	{
		View?.SetEnable(value: false);
		View?.AddClearTask();
		Texture = null;
		_banner = null;
		if (_tableauScene != null)
		{
			_tableauScene.ManualInvalidate();
			_tableauScene = null;
		}
	}

	public void DeserializeCharacterCode(string code)
	{
		if (!(code != _characterCode))
		{
			return;
		}
		if (_initialized)
		{
			ResetProperties();
		}
		_characterCode = code;
		string[] array = code.Split(new char[1] { '|' });
		if (int.TryParse(array[0], out var result) && 4 == result)
		{
			_isVersionCompatible = true;
			int num = 0;
			try
			{
				num++;
				_skeletonName = array[num];
				num++;
				Enum.TryParse<SkinMask>(array[num], ignoreCase: false, out _skinMeshesMask);
				num++;
				bool.TryParse(array[num], out _isFemale);
				num++;
				_race = int.Parse(array[num]);
				num++;
				Enum.TryParse<Equipment.UnderwearTypes>(array[num], ignoreCase: false, out _underwearType);
				num++;
				Enum.TryParse<ArmorComponent.BodyMeshTypes>(array[num], ignoreCase: false, out _bodyMeshType);
				num++;
				Enum.TryParse<ArmorComponent.HairCoverTypes>(array[num], ignoreCase: false, out _hairCoverType);
				num++;
				Enum.TryParse<ArmorComponent.BeardCoverTypes>(array[num], ignoreCase: false, out _beardCoverType);
				num++;
				Enum.TryParse<ArmorComponent.BodyDeformTypes>(array[num], ignoreCase: false, out _bodyDeformType);
				num++;
				float.TryParse(array[num], NumberStyles.Any, CultureInfo.InvariantCulture, out _faceDirtAmount);
				num++;
				BodyProperties.FromString(array[num], out _bodyProperties);
				num++;
				uint.TryParse(array[num], out _clothColor1);
				num++;
				uint.TryParse(array[num], out _clothColor2);
				_equipmentMeshes = new List<string>();
				_equipmentHasColors = new List<bool>();
				_equipmentHasGenderVariations = new List<bool>();
				_equipmentHasTableau = new List<bool>();
				for (EquipmentIndex equipmentIndex = EquipmentIndex.NumAllWeaponSlots; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
				{
					num++;
					_equipmentMeshes.Add(array[num]);
					num++;
					bool.TryParse(array[num], out var result2);
					_equipmentHasColors.Add(result2);
					num++;
					bool.TryParse(array[num], out var result3);
					_equipmentHasGenderVariations.Add(result3);
					num++;
					bool.TryParse(array[num], out var result4);
					_equipmentHasTableau.Add(result4);
				}
				num++;
				_mountSkeletonName = array[num];
				num++;
				_mountMeshName = array[num];
				num++;
				_mountCreationKey = array[num];
				num++;
				_mountMaterialName = array[num];
				num++;
				if (array[num].Length > 0)
				{
					uint.TryParse(array[num], out _mountManeMeshMultiplier);
				}
				else
				{
					_mountManeMeshMultiplier = 0u;
				}
				num++;
				_mountIdleAnimationName = array[num];
				num++;
				_mountHarnessMeshName = array[num];
				num++;
				bool.TryParse(array[num], out _mountHarnessHasColors);
				num++;
				_mountReinsMeshName = array[num];
				num++;
				int num2 = int.Parse(array[num]);
				_maneMeshNames = new string[num2];
				for (int i = 0; i < num2; i++)
				{
					num++;
					_maneMeshNames[i] = array[num];
				}
			}
			catch (Exception ex)
			{
				ResetProperties();
				Debug.FailedAssert("Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\BasicCharacterTableau.cs", "DeserializeCharacterCode", 348);
				Debug.FailedAssert("Couldn't parse character code: " + code, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\BasicCharacterTableau.cs", "DeserializeCharacterCode", 349);
			}
		}
		_isVisualsDirty = true;
	}

	private void ResetProperties()
	{
		_skeletonName = string.Empty;
		_skinMeshesMask = SkinMask.NoneVisible;
		_isFemale = false;
		_underwearType = Equipment.UnderwearTypes.NoUnderwear;
		_bodyMeshType = ArmorComponent.BodyMeshTypes.Normal;
		_hairCoverType = ArmorComponent.HairCoverTypes.None;
		_beardCoverType = ArmorComponent.BeardCoverTypes.None;
		_bodyDeformType = ArmorComponent.BodyDeformTypes.Medium;
		_faceDirtAmount = 0f;
		_bodyProperties = BodyProperties.Default;
		_clothColor1 = 0u;
		_clothColor2 = 0u;
		_equipmentMeshes?.Clear();
		_equipmentHasColors?.Clear();
		_mountSkeletonName = string.Empty;
		_mountMeshName = string.Empty;
		_mountCreationKey = string.Empty;
		_mountMaterialName = string.Empty;
		_mountManeMeshMultiplier = uint.MaxValue;
		_mountIdleAnimationName = string.Empty;
		_mountHarnessMeshName = string.Empty;
		_mountReinsMeshName = string.Empty;
		_maneMeshNames = null;
		_mountHarnessHasColors = false;
		_race = 0;
		_isVersionCompatible = false;
		_characterCode = string.Empty;
	}

	private void FirstTimeInit()
	{
		if (_tableauScene == null)
		{
			_tableauScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
			_tableauScene.SetName("BasicCharacterTableau");
			_tableauScene.DisableStaticShadows(value: true);
			SceneInitializationData sceneInitializationData = new SceneInitializationData(initializeWithDefaults: true);
			sceneInitializationData.InitPhysicsWorld = false;
			sceneInitializationData.DoNotUseLoadingScreen = true;
			SceneInitializationData initData = sceneInitializationData;
			_tableauScene.Read("inventory_character_scene", ref initData);
			_tableauScene.SetShadow(shadowEnabled: true);
			_mountSpawnPoint = _tableauScene.FindEntityWithTag("horse_inv").GetGlobalFrame();
			_initialSpawnFrame = _tableauScene.FindEntityWithTag("agent_inv").GetGlobalFrame();
			_tableauScene.EnsurePostfxSystem();
			_tableauScene.SetDofMode(mode: false);
			_tableauScene.SetMotionBlurMode(mode: false);
			_tableauScene.SetBloom(mode: true);
			_tableauScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
			_tableauScene.RemoveEntity(_tableauScene.FindEntityWithTag("agent_inv"), 99);
			_tableauScene.RemoveEntity(_tableauScene.FindEntityWithTag("horse_inv"), 100);
			_currentCharacters[0] = GameEntity.CreateEmpty(_tableauScene, isModifiableFromEditor: false);
			_currentCharacters[1] = GameEntity.CreateEmpty(_tableauScene, isModifiableFromEditor: false);
			_currentMounts[0] = GameEntity.CreateEmpty(_tableauScene, isModifiableFromEditor: false);
			_currentMounts[1] = GameEntity.CreateEmpty(_tableauScene, isModifiableFromEditor: false);
		}
		SetEnabled(enabled: true);
		_initialized = true;
	}

	private static void ApplyBannerTextureToMesh(Mesh armorMesh, Texture bannerTexture)
	{
		if (armorMesh != null)
		{
			Material material = armorMesh.GetMaterial().CreateCopy();
			material.SetTexture(Material.MBTextureType.DiffuseMap2, bannerTexture);
			uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
			ulong shaderFlags = material.GetShaderFlags();
			material.SetShaderFlags(shaderFlags | num);
			armorMesh.SetMaterial(material);
		}
	}

	private void RefreshCharacterTableau()
	{
		if (!_initialized)
		{
			return;
		}
		_currentEntityToShowIndex = (_currentEntityToShowIndex + 1) % 2;
		GameEntity gameEntity = _currentCharacters[_currentEntityToShowIndex];
		gameEntity.ClearEntityComponents(resetAll: true, removeScripts: true, deleteChildEntities: true);
		GameEntity gameEntity2 = _currentMounts[_currentEntityToShowIndex];
		gameEntity2.ClearEntityComponents(resetAll: true, removeScripts: true, deleteChildEntities: true);
		_mainCharacterRotation = 0f;
		if (!string.IsNullOrEmpty(_skeletonName))
		{
			AnimationSystemData animationSystemData = AnimationSystemData.GetHardcodedAnimationSystemDataForHumanSkeleton();
			bool flag = _bodyProperties.Age >= 14f && _isFemale;
			gameEntity.Skeleton = MBSkeletonExtensions.CreateWithActionSet(ref animationSystemData);
			MetaMesh glovesMesh = null;
			bool flag2 = _equipmentMeshes[3].Length > 0;
			for (int i = 0; i < 5; i++)
			{
				string text = _equipmentMeshes[i];
				if (text.Length <= 0)
				{
					continue;
				}
				bool flag3 = flag && _equipmentHasGenderVariations[i];
				MetaMesh metaMesh = MetaMesh.GetCopy(flag3 ? (text + "_female") : (text + "_male"), showErrors: false, mayReturnNull: true);
				if (metaMesh == null)
				{
					string text2 = text;
					text2 = ((!flag3) ? (text2 + (flag2 ? "_slim" : "")) : (text2 + (flag2 ? "_converted_slim" : "_converted")));
					metaMesh = MetaMesh.GetCopy(text2, showErrors: false, mayReturnNull: true) ?? MetaMesh.GetCopy(text, showErrors: false, mayReturnNull: true);
				}
				if (!(metaMesh != null))
				{
					continue;
				}
				if (i == 3)
				{
					glovesMesh = metaMesh;
				}
				gameEntity.AddMultiMeshToSkeleton(metaMesh);
				if (_equipmentHasTableau[i])
				{
					for (int j = 0; j < metaMesh.MeshCount; j++)
					{
						Mesh currentMesh = metaMesh.GetMeshAtIndex(j);
						Mesh mesh = currentMesh;
						if ((object)mesh == null || mesh.HasTag("dont_use_tableau"))
						{
							continue;
						}
						Mesh mesh2 = currentMesh;
						if ((object)mesh2 != null && mesh2.HasTag("banner_replacement_mesh") && _banner != null)
						{
							BannerTextureCreationData thumbnailCreationData = new BannerTextureCreationData(_banner, delegate(Texture t)
							{
								ApplyBannerTextureToMesh(currentMesh, t);
							}, null, BannerDebugInfo.CreateManual(GetType().Name), isTableauOrNineGrid: true, isLarge: true);
							ThumbnailCacheManager.Current.CreateTexture(thumbnailCreationData);
							break;
						}
					}
				}
				else if (_equipmentHasColors[i])
				{
					for (int num = 0; num < metaMesh.MeshCount; num++)
					{
						Mesh meshAtIndex = metaMesh.GetMeshAtIndex(num);
						if (!meshAtIndex.HasTag("no_team_color"))
						{
							meshAtIndex.Color = _clothColor1;
							meshAtIndex.Color2 = _clothColor2;
							Material material = meshAtIndex.GetMaterial().CreateCopy();
							material.AddMaterialShaderFlag("use_double_colormap_with_mask_texture", showErrors: false);
							meshAtIndex.SetMaterial(material);
						}
					}
				}
				metaMesh.ManualInvalidate();
			}
			gameEntity.SetGlobalFrame(in _initialSpawnFrame);
			SkinGenerationParams skinParams = new SkinGenerationParams((int)_skinMeshesMask, _underwearType, (int)_bodyMeshType, (int)_hairCoverType, (int)_beardCoverType, (int)_bodyDeformType, prepareImmediately: true, _faceDirtAmount, flag ? 1 : 0, _race, useTranslucency: false, useTesselation: false, 0);
			MBAgentVisuals.FillEntityWithBodyMeshesWithoutAgentVisuals(gameEntity, skinParams, _bodyProperties, glovesMesh);
			gameEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_inventory_idle);
			gameEntity.SetEnforcedMaximumLodLevel(0);
			gameEntity.CheckResources(addToQueue: true, checkFaceResources: true);
			if (_mountMeshName.Length > 0)
			{
				gameEntity2.Skeleton = Skeleton.CreateFromModel(_mountSkeletonName);
				MetaMesh copy = MetaMesh.GetCopy(_mountMeshName);
				if (copy != null)
				{
					MountCreationKey mountCreationKey = MountCreationKey.FromString(_mountCreationKey);
					MountVisualCreator.SetHorseColors(copy, mountCreationKey);
					if (!string.IsNullOrEmpty(_mountMaterialName))
					{
						Material fromResource = Material.GetFromResource(_mountMaterialName);
						copy.SetMaterialToSubMeshesWithTag(fromResource, "horse_body");
						copy.SetFactorColorToSubMeshesWithTag(_mountManeMeshMultiplier, "horse_tail");
					}
					gameEntity2.AddMultiMeshToSkeleton(copy);
					copy.ManualInvalidate();
					if (_mountHarnessMeshName.Length > 0)
					{
						MetaMesh copy2 = MetaMesh.GetCopy(_mountHarnessMeshName, showErrors: false, mayReturnNull: true);
						if (copy2 != null)
						{
							if (_mountReinsMeshName.Length > 0)
							{
								MetaMesh copy3 = MetaMesh.GetCopy(_mountReinsMeshName, showErrors: false, mayReturnNull: true);
								if (copy3 != null)
								{
									gameEntity2.AddMultiMeshToSkeleton(copy3);
									copy3.ManualInvalidate();
								}
							}
							gameEntity2.AddMultiMeshToSkeleton(copy2);
							if (_mountHarnessHasColors)
							{
								for (int num2 = 0; num2 < copy2.MeshCount; num2++)
								{
									Mesh meshAtIndex2 = copy2.GetMeshAtIndex(num2);
									if (!meshAtIndex2.HasTag("no_team_color"))
									{
										meshAtIndex2.Color = _clothColor1;
										meshAtIndex2.Color2 = _clothColor2;
										Material material2 = meshAtIndex2.GetMaterial().CreateCopy();
										material2.AddMaterialShaderFlag("use_double_colormap_with_mask_texture", showErrors: false);
										meshAtIndex2.SetMaterial(material2);
									}
								}
							}
							copy2.ManualInvalidate();
						}
					}
				}
				string[] maneMeshNames = _maneMeshNames;
				for (int num3 = 0; num3 < maneMeshNames.Length; num3++)
				{
					MetaMesh copy4 = MetaMesh.GetCopy(maneMeshNames[num3], showErrors: false, mayReturnNull: true);
					if (_mountManeMeshMultiplier != uint.MaxValue)
					{
						copy4.SetFactor1Linear(_mountManeMeshMultiplier);
					}
					gameEntity2.AddMultiMeshToSkeleton(copy4);
					copy4.ManualInvalidate();
				}
				gameEntity2.SetGlobalFrame(in _mountSpawnPoint);
				gameEntity2.Skeleton.SetAnimationAtChannel(_mountIdleAnimationName, 0, 1f, 0f);
				gameEntity2.SetEnforcedMaximumLodLevel(0);
				gameEntity2.CheckResources(addToQueue: true, checkFaceResources: true);
			}
		}
		_currentCharacters[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: false);
		_currentMounts[_currentEntityToShowIndex].SetVisibilityExcludeParents(visible: false);
		int num4 = (_currentEntityToShowIndex + 1) % 2;
		_currentCharacters[num4].SetVisibilityExcludeParents(visible: true);
		_currentMounts[num4].SetVisibilityExcludeParents(visible: true);
		_checkWhetherEntitiesAreReady = true;
	}

	internal void SetCamera()
	{
		Camera camera = Camera.CreateCamera();
		camera.Frame = _tableauScene.FindEntityWithTag("camera_instance").GetGlobalFrame();
		camera.SetFovVertical(System.MathF.PI / 4f, _cameraRatio, 0.2f, 200f);
		View.SetCamera(camera);
		camera.ManualInvalidate();
	}

	public void RotateCharacter(bool value)
	{
		_isRotatingCharacter = value;
	}

	public void SetBannerCode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			_banner = null;
		}
		else
		{
			_banner = new Banner(value);
		}
	}

	internal void CharacterTableauContinuousRenderFunction(Texture sender, EventArgs e)
	{
		Scene obj = (Scene)sender.UserData;
		TableauView tableauView = sender.TableauView;
		if (obj == null)
		{
			tableauView.SetContinuousRendering(value: false);
			tableauView.SetDeleteAfterRendering(value: true);
		}
	}
}
