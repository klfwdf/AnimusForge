using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class MouseWidget : Widget
{
	private static readonly char[] _trimChars = new char[2] { ' ', ',' };

	public Widget LeftMouseButton { get; set; }

	public Widget RightMouseButton { get; set; }

	public Widget MiddleMouseButton { get; set; }

	public Widget MouseX1Button { get; set; }

	public Widget MouseX2Button { get; set; }

	public Widget MouseScrollUp { get; set; }

	public Widget MouseScrollDown { get; set; }

	public TextWidget KeyboardKeys { get; set; }

	public MouseWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		if (base.IsVisible)
		{
			UpdatePressedKeys();
		}
	}

	public void UpdatePressedKeys()
	{
		Color color = new Color(1f, 0f, 0f);
		LeftMouseButton.Color = Color.White;
		RightMouseButton.Color = Color.White;
		MiddleMouseButton.Color = Color.White;
		MouseX1Button.Color = Color.White;
		MouseX2Button.Color = Color.White;
		MouseScrollUp.IsVisible = false;
		MouseScrollDown.IsVisible = false;
		KeyboardKeys.Text = "";
		if (Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			LeftMouseButton.Color = color;
		}
		if (Input.IsKeyDown(InputKey.RightMouseButton))
		{
			RightMouseButton.Color = color;
		}
		if (Input.IsKeyDown(InputKey.MiddleMouseButton))
		{
			MiddleMouseButton.Color = color;
		}
		if (Input.IsKeyDown(InputKey.X1MouseButton))
		{
			MouseX1Button.Color = color;
		}
		if (Input.IsKeyDown(InputKey.X2MouseButton))
		{
			MouseX2Button.Color = color;
		}
		if (Input.IsKeyDown(InputKey.MouseScrollUp))
		{
			MouseScrollUp.IsVisible = true;
		}
		if (Input.IsKeyDown(InputKey.MouseScrollDown))
		{
			MouseScrollDown.IsVisible = true;
		}
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "UpdatePressedKeys");
		for (int i = 0; i < 256; i++)
		{
			if (Key.GetInputType((InputKey)i) == Key.InputType.Keyboard && Input.IsKeyDown((InputKey)i))
			{
				InputKey inputKey = (InputKey)i;
				mBStringBuilder.Append(inputKey.ToString());
				mBStringBuilder.Append(", ");
			}
		}
		KeyboardKeys.Text = mBStringBuilder.ToStringAndRelease().TrimEnd(_trimChars);
	}
}
