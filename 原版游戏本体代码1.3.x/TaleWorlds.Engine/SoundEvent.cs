using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200008D RID: 141
	public class SoundEvent
	{
		// Token: 0x06000C8F RID: 3215 RVA: 0x0000DEA6 File Offset: 0x0000C0A6
		public int GetSoundId()
		{
			return this._soundId;
		}

		// Token: 0x06000C90 RID: 3216 RVA: 0x0000DEAE File Offset: 0x0000C0AE
		private SoundEvent(int soundId)
		{
			this._soundId = soundId;
		}

		// Token: 0x06000C91 RID: 3217 RVA: 0x0000DEC0 File Offset: 0x0000C0C0
		public static SoundEvent CreateEventFromString(string eventId, Scene scene)
		{
			UIntPtr scene2 = ((scene == null) ? UIntPtr.Zero : scene.Pointer);
			return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromString(eventId, scene2));
		}

		// Token: 0x06000C92 RID: 3218 RVA: 0x0000DEF5 File Offset: 0x0000C0F5
		public void SetEventMinMaxDistance(Vec3 newRadius)
		{
			EngineApplicationInterface.ISoundEvent.SetEventMinMaxDistance(this._soundId, newRadius);
		}

		// Token: 0x06000C93 RID: 3219 RVA: 0x0000DF08 File Offset: 0x0000C108
		public static int GetEventIdFromString(string name)
		{
			return EngineApplicationInterface.ISoundEvent.GetEventIdFromString(name);
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x0000DF15 File Offset: 0x0000C115
		public static bool PlaySound2D(int soundCodeId)
		{
			return EngineApplicationInterface.ISoundEvent.PlaySound2D(soundCodeId);
		}

		// Token: 0x06000C95 RID: 3221 RVA: 0x0000DF22 File Offset: 0x0000C122
		public static bool PlaySound2D(string soundName)
		{
			return SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString(soundName));
		}

		// Token: 0x06000C96 RID: 3222 RVA: 0x0000DF2F File Offset: 0x0000C12F
		public static int GetTotalEventCount()
		{
			return EngineApplicationInterface.ISoundEvent.GetTotalEventCount();
		}

		// Token: 0x06000C97 RID: 3223 RVA: 0x0000DF3B File Offset: 0x0000C13B
		public static SoundEvent CreateEvent(int soundCodeId, Scene scene)
		{
			return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEvent(soundCodeId, scene.Pointer));
		}

		// Token: 0x06000C98 RID: 3224 RVA: 0x0000DF53 File Offset: 0x0000C153
		public bool IsNullSoundEvent()
		{
			return this == SoundEvent.NullSoundEvent;
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x06000C99 RID: 3225 RVA: 0x0000DF5D File Offset: 0x0000C15D
		public bool IsValid
		{
			get
			{
				return this._soundId != -1 && EngineApplicationInterface.ISoundEvent.IsValid(this._soundId);
			}
		}

		// Token: 0x06000C9A RID: 3226 RVA: 0x0000DF7A File Offset: 0x0000C17A
		public bool Play()
		{
			return EngineApplicationInterface.ISoundEvent.StartEvent(this._soundId);
		}

		// Token: 0x06000C9B RID: 3227 RVA: 0x0000DF8C File Offset: 0x0000C18C
		public void Pause()
		{
			EngineApplicationInterface.ISoundEvent.PauseEvent(this._soundId);
		}

		// Token: 0x06000C9C RID: 3228 RVA: 0x0000DF9E File Offset: 0x0000C19E
		public void Resume()
		{
			EngineApplicationInterface.ISoundEvent.ResumeEvent(this._soundId);
		}

		// Token: 0x06000C9D RID: 3229 RVA: 0x0000DFB0 File Offset: 0x0000C1B0
		public void PlayExtraEvent(string eventName)
		{
			EngineApplicationInterface.ISoundEvent.PlayExtraEvent(this._soundId, eventName);
		}

		// Token: 0x06000C9E RID: 3230 RVA: 0x0000DFC3 File Offset: 0x0000C1C3
		public void SetSwitch(string switchGroupName, string newSwitchStateName)
		{
			EngineApplicationInterface.ISoundEvent.SetSwitch(this._soundId, switchGroupName, newSwitchStateName);
		}

		// Token: 0x06000C9F RID: 3231 RVA: 0x0000DFD7 File Offset: 0x0000C1D7
		public void TriggerCue()
		{
			EngineApplicationInterface.ISoundEvent.TriggerCue(this._soundId);
		}

		// Token: 0x06000CA0 RID: 3232 RVA: 0x0000DFE9 File Offset: 0x0000C1E9
		public bool PlayInPosition(Vec3 position)
		{
			return EngineApplicationInterface.ISoundEvent.StartEventInPosition(this._soundId, ref position);
		}

		// Token: 0x06000CA1 RID: 3233 RVA: 0x0000DFFD File Offset: 0x0000C1FD
		public void Stop()
		{
			if (!this.IsValid)
			{
				return;
			}
			EngineApplicationInterface.ISoundEvent.StopEvent(this._soundId);
			this._soundId = -1;
		}

		// Token: 0x06000CA2 RID: 3234 RVA: 0x0000E01F File Offset: 0x0000C21F
		public void SetParameter(string parameterName, float value)
		{
			EngineApplicationInterface.ISoundEvent.SetEventParameterFromString(this._soundId, parameterName, value);
		}

		// Token: 0x06000CA3 RID: 3235 RVA: 0x0000E033 File Offset: 0x0000C233
		public void SetParameter(int parameterIndex, float value)
		{
			EngineApplicationInterface.ISoundEvent.SetEventParameterAtIndex(this._soundId, parameterIndex, value);
		}

		// Token: 0x06000CA4 RID: 3236 RVA: 0x0000E047 File Offset: 0x0000C247
		public Vec3 GetEventMinMaxDistance()
		{
			return EngineApplicationInterface.ISoundEvent.GetEventMinMaxDistance(this._soundId);
		}

		// Token: 0x06000CA5 RID: 3237 RVA: 0x0000E059 File Offset: 0x0000C259
		public void SetPosition(Vec3 vec)
		{
			if (!this.IsValid)
			{
				return;
			}
			EngineApplicationInterface.ISoundEvent.SetEventPosition(this._soundId, ref vec);
		}

		// Token: 0x06000CA6 RID: 3238 RVA: 0x0000E076 File Offset: 0x0000C276
		public void SetVelocity(Vec3 vec)
		{
			if (!this.IsValid)
			{
				return;
			}
			EngineApplicationInterface.ISoundEvent.SetEventVelocity(this._soundId, ref vec);
		}

		// Token: 0x06000CA7 RID: 3239 RVA: 0x0000E094 File Offset: 0x0000C294
		public void Release()
		{
			MBDebug.Print("Release Sound Event " + this._soundId, 0, Debug.DebugColor.Red, 17592186044416UL);
			if (this.IsValid)
			{
				if (this.IsPlaying())
				{
					this.Stop();
				}
				EngineApplicationInterface.ISoundEvent.ReleaseEvent(this._soundId);
			}
		}

		// Token: 0x06000CA8 RID: 3240 RVA: 0x0000E0EC File Offset: 0x0000C2EC
		public bool IsPlaying()
		{
			return EngineApplicationInterface.ISoundEvent.IsPlaying(this._soundId);
		}

		// Token: 0x06000CA9 RID: 3241 RVA: 0x0000E0FE File Offset: 0x0000C2FE
		public bool IsPaused()
		{
			return EngineApplicationInterface.ISoundEvent.IsPaused(this._soundId);
		}

		// Token: 0x06000CAA RID: 3242 RVA: 0x0000E110 File Offset: 0x0000C310
		public static SoundEvent CreateEventFromSoundBuffer(string eventId, byte[] soundData, Scene scene, bool is3d, bool isBlocking)
		{
			return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromSoundBuffer(eventId, soundData, (scene != null) ? scene.Pointer : UIntPtr.Zero, is3d, isBlocking));
		}

		// Token: 0x06000CAB RID: 3243 RVA: 0x0000E13C File Offset: 0x0000C33C
		public static SoundEvent CreateEventFromExternalFile(string programmerEventName, string soundFilePath, Scene scene, bool is3d, bool isBlocking)
		{
			return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromExternalFile(programmerEventName, soundFilePath, (scene != null) ? scene.Pointer : UIntPtr.Zero, is3d, isBlocking));
		}

		// Token: 0x040001C3 RID: 451
		private const int NullSoundId = -1;

		// Token: 0x040001C4 RID: 452
		private static readonly SoundEvent NullSoundEvent = new SoundEvent(-1);

		// Token: 0x040001C5 RID: 453
		private int _soundId;
	}
}
