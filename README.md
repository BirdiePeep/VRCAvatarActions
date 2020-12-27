# VRCAvatarActions
Unity script for creating avatar 3.0 content faster and easier.

## QUICK START

- Begin by right clicking in the Assets higherarchy and select "Create/VRCAvatarActions/Menu Actions/Menu".
- Select the new item called "Menu".
- In the inspector click "Add" to create a new action.
    - **Name** - Display name in expressions menu.
    - **Icon** - Display icon in expressions menu.
    - **Type** - Type of action.
      - Toggle - Toggle on/off.
      - Button - Press and release.
      - Slider - Radial menu.  Plays an animation at a position based on the slider's value.
      - Sub-Menu - Opens a new menu.
      - Pre-Existing (Advanced) - Preserves a menu item that already exists in the VRCExpressionsMenu object.
    - **Fade in** - Time to fade in.
    - **Hold** - Time to hold before checking for exit conditions.
    - **Fade out** -Time to fade out.
    - **Parameter** - Which parameter is associated with this action. **(REQUIRED)**
    - **Object Properties** - Simple actions you want to perform when this action is enabled.  Toggle objects, swap materials, play audio, etc...
    - **Animations** - Animations to play when this control is active.
      - Enter - The base animation, it plays at the start.
      - Exit - An optional animtion to play when exiting.
    - **IK Overrides (Advanced)** - Informs the avatar to disable certain IK controls over the avatar while the control is active.
    - **Triggers (Advanced)** - Additional enter or exit conditions for this action.
    - **Sub-Actions (Experimental)** - Supply any number of non-menu action sets.  These actions will only activate if this control is also active.
		
## BUILDING

- Create a new game object in your scene and add the "Avatar Actions" script.
- Drag your Menu object into the Menu Actions field.
- Click the "Build Avatar Data" button.

Now you are ready to upload your avatar!

## ScriptableObject Types
- **Menu** - A set of actions tied to the avatar's expression menu.  Create toggles, buttons, sub-menus, etc.
- **Basic** - Actions with no default trigger conditions.  They are either always running or can have a custom set of triggers.
- **Gestures** - Actions tied directly to gestures.  These actions don't have exit states, as they quickly transition from one action to another.
- **Viseme** - Actions tied directly to visemes.  These actions don't have exit states, as they quickly transition from one action to another.
