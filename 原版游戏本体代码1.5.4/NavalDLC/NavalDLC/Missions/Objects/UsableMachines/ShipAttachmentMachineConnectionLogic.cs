using TaleWorlds.Engine;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipAttachmentMachineConnectionLogic : ScriptComponentBehavior
{
	private MissionShip _ownerShip;

	private void FillAttachmentMachinesList()
	{
		_ownerShip = base.GameEntity.Root.GetFirstScriptOfType<MissionShip>();
	}

	protected override void OnInit()
	{
		base.OnInit();
		FillAttachmentMachinesList();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment == null || attachmentMachine.CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling || !attachmentMachine.CurrentAttachment.ShouldLookForBetterConnections())
			{
				continue;
			}
			ShipAttachmentPointMachine attachmentTarget = attachmentMachine.CurrentAttachment.AttachmentTarget;
			MissionShip ownerShip = attachmentTarget.OwnerShip;
			float num = ShipAttachmentMachine.ComputePotentialAttachmentValue(attachmentMachine, attachmentTarget, checkInteractionDistance: false, checkConnectionBlock: false, allowWiderAngleBetweenConnections: true);
			if (!(num > 0f))
			{
				continue;
			}
			float num2 = num * 1.2f;
			ShipAttachmentPointMachine shipAttachmentPointMachine = null;
			foreach (ShipAttachmentPointMachine attachmentPointMachine in ownerShip.AttachmentPointMachines)
			{
				if (attachmentTarget != attachmentPointMachine && attachmentPointMachine.CurrentAttachment == null && attachmentPointMachine.LinkedAttachmentMachine?.CurrentAttachment == null)
				{
					float num3 = ShipAttachmentMachine.ComputePotentialAttachmentValue(attachmentMachine, attachmentTarget, checkInteractionDistance: true, checkConnectionBlock: true, allowWiderAngleBetweenConnections: false);
					if (num3 > num2)
					{
						num2 = num3;
						shipAttachmentPointMachine = attachmentPointMachine;
					}
				}
			}
			if (shipAttachmentPointMachine != null)
			{
				attachmentMachine.CurrentAttachment.Destroy();
				attachmentMachine.ConnectWithAttachmentPointMachine(shipAttachmentPointMachine);
			}
		}
	}
}
