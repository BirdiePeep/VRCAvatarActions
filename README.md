# VRCAvatarActions
Unity script for creating avatar 3.0 content faster and easier.

# QUICK START

- To begin, create an action set with right clicking in the Unity higherarchy window and select "Create/Tropical/AvatarActions"
- Select the new item called "AvatarActions"
- In the inspector, toggle on "Root Menu". (NOTE: Only enable "Root Menu" on the top most AvatarActions object.)
- Click "Add", then select the "New Action"
- Define this action as you like, most of these are self explainitory
    - **Name** - Display name in expressions menu
    - **Icon** - Display icon in expressions menu
    - **Type** - Type of action
      - Button - Press and release.
      - Toggle - Toggle on/off
      - Slider - Radial slider, plays an animation at a normalized time based on the slider value.
      - Sub-Menu - Goes to another AvatarActions object.
      - Pre-Existing - If a menu item has the same name, it won't destroy it.
    - **Fade in** - Time to fade in
    - **Fade out** -Time to fade out
    - **Parameter** - Which parameter is associated with this action
    - **Toggle Objects** - These object will be disabled by default and enabled when the control is active.
    - **Animations** - Animations to play when this control is active.
      - Enter - The base animation, it plays at the start
      - Exit - An optional animtion to play when exiting.
    - **IK Overrides** - Informs the avatar to disable certain IK controls over the avatar while the control is active.
		
# BUILDING

Click the "Build Avatar Data" button in the root action set.
Now you are ready to upload your avatar!
