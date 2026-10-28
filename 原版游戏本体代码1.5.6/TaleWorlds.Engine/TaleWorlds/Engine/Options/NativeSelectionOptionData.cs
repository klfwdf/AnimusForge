using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.Options;

public class NativeSelectionOptionData : NativeOptionData, ISelectionOptionData, IOptionData
{
	private readonly int _selectableOptionsLimit;

	private readonly IEnumerable<SelectionData> _selectableOptionNames;

	public NativeSelectionOptionData(NativeOptions.NativeOptionsType type)
		: base(type)
	{
		_selectableOptionsLimit = GetOptionsLimit(type);
		_selectableOptionNames = GetOptionNames(type);
	}

	public int GetSelectableOptionsLimit()
	{
		return _selectableOptionsLimit;
	}

	public IEnumerable<SelectionData> GetSelectableOptionNames()
	{
		return GetOptionNames(Type);
	}

	public static int GetOptionsLimit(NativeOptions.NativeOptionsType optionType)
	{
		return optionType switch
		{
			NativeOptions.NativeOptionsType.ScreenResolution => NativeOptions.GetResolutionCount() + 1, 
			NativeOptions.NativeOptionsType.RefreshRate => NativeOptions.GetRefreshRateCount(), 
			NativeOptions.NativeOptionsType.SoundDevice => NativeOptions.GetSoundDeviceCount(), 
			NativeOptions.NativeOptionsType.SelectedMonitor => NativeOptions.GetMonitorDeviceCount(), 
			NativeOptions.NativeOptionsType.SelectedAdapter => NativeOptions.GetVideoDeviceCount(), 
			NativeOptions.NativeOptionsType.TextureBudget => 4, 
			NativeOptions.NativeOptionsType.TextureQuality => 3, 
			NativeOptions.NativeOptionsType.VSync => 3, 
			NativeOptions.NativeOptionsType.MaxSimultaneousSoundEventCount => 4, 
			NativeOptions.NativeOptionsType.SoundPreset => 4, 
			NativeOptions.NativeOptionsType.Antialiasing => 6, 
			NativeOptions.NativeOptionsType.DLSS => NativeOptions.GetDLSSOptionCount(), 
			NativeOptions.NativeOptionsType.Occlusion => 2, 
			NativeOptions.NativeOptionsType.OverAll => 6, 
			NativeOptions.NativeOptionsType.DisplayMode => 3, 
			NativeOptions.NativeOptionsType.TextureFiltering => 6, 
			NativeOptions.NativeOptionsType.CharacterDetail => 5, 
			NativeOptions.NativeOptionsType.ParticleQuality => 3, 
			NativeOptions.NativeOptionsType.ParticleDetail => 3, 
			NativeOptions.NativeOptionsType.FoliageQuality => 5, 
			NativeOptions.NativeOptionsType.DecalQuality => 5, 
			NativeOptions.NativeOptionsType.WaterQuality => 3, 
			NativeOptions.NativeOptionsType.SSRQuality => 2, 
			NativeOptions.NativeOptionsType.ShadowmapResolution => 4, 
			NativeOptions.NativeOptionsType.ShadowmapType => 3, 
			NativeOptions.NativeOptionsType.ShadowmapFiltering => 2, 
			NativeOptions.NativeOptionsType.ShaderQuality => 3, 
			NativeOptions.NativeOptionsType.NumberOfRagDolls => 6, 
			NativeOptions.NativeOptionsType.AnimationSamplingQuality => 2, 
			NativeOptions.NativeOptionsType.LightingQuality => 3, 
			NativeOptions.NativeOptionsType.EnvironmentDetail => 5, 
			NativeOptions.NativeOptionsType.TerrainQuality => 3, 
			NativeOptions.NativeOptionsType.PhysicsTickRate => 2, 
			_ => 0, 
		};
	}

	private static IEnumerable<SelectionData> GetOptionNames(NativeOptions.NativeOptionsType type)
	{
		switch (type)
		{
		case NativeOptions.NativeOptionsType.SoundDevice:
		{
			for (int i = 0; i < NativeOptions.GetSoundDeviceCount(); i++)
			{
				string soundDeviceName = NativeOptions.GetSoundDeviceName(i);
				if (soundDeviceName != "")
				{
					yield return new SelectionData(isLocalizationId: false, soundDeviceName);
				}
			}
			break;
		}
		case NativeOptions.NativeOptionsType.SelectedMonitor:
		{
			for (int i = 0; i < NativeOptions.GetMonitorDeviceCount(); i++)
			{
				yield return new SelectionData(isLocalizationId: false, NativeOptions.GetMonitorDeviceName(i));
			}
			break;
		}
		case NativeOptions.NativeOptionsType.SelectedAdapter:
		{
			for (int i = 0; i < NativeOptions.GetVideoDeviceCount(); i++)
			{
				yield return new SelectionData(isLocalizationId: false, NativeOptions.GetVideoDeviceName(i));
			}
			break;
		}
		case NativeOptions.NativeOptionsType.ScreenResolution:
		{
			for (int j = 0; j < NativeOptions.GetResolutionCount(); j++)
			{
				Vec2 resolutionAtIndex = NativeOptions.GetResolutionAtIndex(j);
				yield return new SelectionData(isLocalizationId: false, $"{resolutionAtIndex.x}x{resolutionAtIndex.y} ({GetAspectRatioOfResolution((int)resolutionAtIndex.x, (int)resolutionAtIndex.y)})");
			}
			int width = 0;
			int height = 0;
			int i = 0;
			int height2 = 0;
			NativeOptions.GetDesktopResolution(ref width, ref height);
			NativeOptions.GetResolution(ref i, ref height2);
			if (NativeOptions.GetDLSSTechnique() != 4 || width >= 3840)
			{
				yield return new SelectionData(isLocalizationId: true, "str_options_type_ScreenResolution_Desktop");
			}
			if (NativeOptions.GetDLSSTechnique() != 4 || i >= 3840)
			{
				yield return new SelectionData(isLocalizationId: true, "str_options_type_ScreenResolution_Custom");
			}
			break;
		}
		case NativeOptions.NativeOptionsType.RefreshRate:
		{
			for (int i = 0; i < NativeOptions.GetRefreshRateCount(); i++)
			{
				int refreshRateAtIndex = NativeOptions.GetRefreshRateAtIndex(i);
				yield return new SelectionData(isLocalizationId: false, refreshRateAtIndex + " Hz");
			}
			break;
		}
		default:
		{
			int i = GetOptionsLimit(type);
			string typeName = type.ToString();
			for (int j = 0; j < i; j++)
			{
				yield return new SelectionData(isLocalizationId: true, "str_options_type_" + typeName + "_" + j);
			}
			break;
		}
		}
	}

	private static string GetAspectRatioOfResolution(int width, int height)
	{
		return $"{width / MathF.GreatestCommonDivisor(width, height)}:{height / MathF.GreatestCommonDivisor(width, height)}";
	}
}
