# VRCAvatarActions
Unity script for creating avatar 3.0 content faster and easier.

## QUICK START

- Begin by right clicking in the Assets higherarchy and select "Create/VRCAvatarActions/Menu Actions/Menu Actions".
- Select the new item called "Menu Actions".
- In the inspector click "Add" to create a new action.
    - **Name** - Display name in expressions menu.
    - **Icon** - Display icon in expressions menu.
    - **Type** - Type of action.
      - Toggle - Toggle on/off.
      - Button - Press and release.
      - Slider - Radial slider, plays an animation at a normalized time based on the slider value.
      - Sub-Menu - Goes to another AvatarActions object.
      - Pre-Existing - If a menu item has the same name, it won't destroy it.
    - **Fade in** - Time to fade in.
    - **Fade out** -Time to fade out.
    - **Parameter** - Which parameter is associated with this action.
    - **Toggle Objects** - These object will be disabled by default and enabled when the control is active.
    - **Animations** - Animations to play when this control is active.
      - Enter - The base animation, it plays at the start.
      - Exit - An optional animtion to play when exiting.
    - **IK Overrides** - Informs the avatar to disable certain IK controls over the avatar while the control is active.
    - **Triggers (Advanced)** - Additional enter or exit conditions for this action.
    - **Sub-Actions (Experimental)** - Supply any number of non-menu action sets.  These actions will only activate if this control is also active.
		
## BUILDING

- Select your avatar and add a script called "Avatar Actions".
- Drag your Menu Actions object into the Menu Actions field.
- Click the "Build Avatar Data" button.

Now you are ready to upload your avatar!
