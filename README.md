# VRCAvatarActions
Unity script for creating avatar 3.0 content faster and easier.

## QUICK START

- Begin by right clicking in the Assets higherarchy and selecting "Create/VRCAvatarActions/Actions Descriptor".
- Select the new item called "Actions Descriptor".
- In the inspector click "Add" next to the Menu Actions field.
- Select the new item called "Menu Main".
- In the inspector click "Add" to create a new action.
- Define this action as you like, most of these are self explainitory.
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

Click the "Build Avatar Data" button on the Actions Descriptor object.

Now you are ready to upload your avatar!
