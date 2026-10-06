using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SoundPlayer : ScriptComponentBehavior
{
	private bool Playing;

	private int SoundCode = -1;

	private SoundEvent SoundEvent;

	public bool AutoLoop;

	public bool AutoStart;

	public string SoundName;

	private void ValidateSoundEvent()
	{
		if ((SoundEvent == null || !SoundEvent.IsValid) && SoundName.Length > 0)
		{
			if (SoundCode == -1)
			{
				SoundCode = SoundManager.GetEventGlobalIndex(SoundName);
			}
			SoundEvent = SoundEvent.CreateEvent(SoundCode, base.GameEntity.Scene);
		}
	}

	public void UpdatePlaying()
	{
		Playing = SoundEvent != null && SoundEvent.IsValid && SoundEvent.IsPlaying();
	}

	public void PlaySound()
	{
		if (!Playing && SoundEvent != null && SoundEvent.IsValid)
		{
			SoundEvent.SetPosition(base.GameEntity.GlobalPosition);
			SoundEvent.Play();
			Playing = true;
		}
	}

	public void ResumeSound()
	{
		if (!Playing && SoundEvent != null && SoundEvent.IsValid && SoundEvent.IsPaused())
		{
			SoundEvent.Resume();
			Playing = true;
		}
	}

	public void PauseSound()
	{
		if (Playing && SoundEvent != null && SoundEvent.IsValid)
		{
			SoundEvent.Pause();
			Playing = false;
		}
	}

	public void StopSound()
	{
		if (Playing && SoundEvent != null && SoundEvent.IsValid)
		{
			SoundEvent.Stop();
			Playing = false;
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		MBDebug.Print("SoundPlayer : OnInit called.", 0, Debug.DebugColor.Yellow);
		ValidateSoundEvent();
		if (AutoStart)
		{
			PlaySound();
		}
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		UpdatePlaying();
		if (!Playing && AutoLoop)
		{
			ValidateSoundEvent();
			PlaySound();
		}
	}

	protected internal override bool MovesEntity()
	{
		return false;
	}
}
