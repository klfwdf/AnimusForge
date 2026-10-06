using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AnimatedFlag : ScriptComponentBehavior
{
	private float _prevTheta;

	private float _prevSkew;

	private Vec3 _prevFlagMeshFrame;

	private float _time;

	public AnimatedFlag()
	{
		_prevFlagMeshFrame = new Vec3(0f, 0f, 0f, -1f);
		_prevTheta = 0f;
		_prevSkew = 0f;
		_time = 0f;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
		MBDebug.Print("AnimatedFlag : OnInit called.", 0, Debug.DebugColor.Yellow);
	}

	private void SmoothTheta(ref float theta, float dt)
	{
		float num = theta - _prevTheta;
		if (num > System.MathF.PI)
		{
			num -= System.MathF.PI * 2f;
			_prevTheta += System.MathF.PI * 2f;
		}
		else if (num < -System.MathF.PI)
		{
			num += System.MathF.PI * 2f;
			_prevTheta -= System.MathF.PI * 2f;
		}
		num = TaleWorlds.Library.MathF.Min(num, 150f * dt);
		theta = _prevTheta + num * 0.05f;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		if (dt == 0f)
		{
			return;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		MetaMesh metaMesh = base.GameEntity.GetMetaMesh(0);
		if (!(metaMesh == null))
		{
			Vec3 vec = globalFrame.origin - _prevFlagMeshFrame;
			vec.x /= dt;
			vec.y /= dt;
			vec.z /= dt;
			vec = new Vec3(20f, 0f, -10f) * 0.1f - vec;
			if (!(vec.LengthSquared < 1E-08f))
			{
				Vec3 vec2 = globalFrame.rotation.TransformToLocal(in vec);
				vec2.z = 0f;
				vec2.Normalize();
				float theta = TaleWorlds.Library.MathF.Atan2(vec2.y, vec2.x);
				SmoothTheta(ref theta, dt);
				Vec3 scalingVector = metaMesh.Frame.rotation.GetScaleVector();
				MatrixFrame identity = MatrixFrame.Identity;
				identity.Scale(in scalingVector);
				identity.rotation.RotateAboutUp(theta);
				_prevTheta = theta;
				float num = TaleWorlds.Library.MathF.Acos(Vec3.DotProduct(vec, globalFrame.rotation.u) / vec.Length);
				float a = num - _prevSkew;
				a = TaleWorlds.Library.MathF.Min(a, 150f * dt);
				num = (_prevSkew += a * 0.05f);
				float num2 = MBMath.ClampFloat(vec.Length, 0.001f, 10000f);
				_time += dt * num2 * 0.5f;
				metaMesh.Frame = identity;
				metaMesh.VectorUserData = new Vec3(TaleWorlds.Library.MathF.Cos(num), 1f - TaleWorlds.Library.MathF.Sin(num), 0f, _time);
				_prevFlagMeshFrame = globalFrame.origin;
			}
		}
	}

	protected internal override bool IsOnlyVisual()
	{
		return true;
	}
}
