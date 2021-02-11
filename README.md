# VRCAvatarActions
Unity script for creating avatar 3.0 content faster and easier.

## Quick Start - Install

- Download the package on git.  Click Code, Download as Zip.
- Extract the VRCAvatarActions folder.
- Drag the VRCAvatarActions folder into your Unity project.

## Quick Start - Create a Menu

- Begin by right clicking in the Assets higherarchy and select "Create/VRCAvatarActions/Menu Actions/Menu".
- Select the new item called "Menu".
- In the inspector click "Add" to create a new action.
	- Name - Give your action a name.
	- Parameter - Specify a parameter to control this action.  For example... if you're toggling a hat, just call it "Hat" or "Head".
	- Object Properties
		- Open the object properties and click "Add"
		- Drag in an object you want to toggle.
		
## Quick Start - Build

- Rightclick in your scene and select "Create Empty".
- Select the new game object and in the inspector click "Add Component", selecting "Avatar Actions".
- Drag your avatar object into the Avatar field.
- Drag your Menu object into the Menu Actions field.
- Click the "Build Avatar Data" button.
- It's done! Now you are ready to upload your avatar.

## Object Types
- **Menu** - A set of actions tied to the avatar's expression menu.  (Toggles, buttons, sub-menus, etc...).
- **Basic** - Actions with no default trigger conditions.  They are either always running or can have a custom set of triggers. (Idles, AFKs, etc...)
- **Gestures** - Actions tied directly to gestures.  These actions don't have exit states, as they quickly transition from one action to another.
- **Viseme** - Actions tied directly to visemes.  These actions don't have exit states, as they quickly transition from one action to another.

## Menu Actions ##
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
- **Parameter** - Which parameter used to control this action. You can share parameters between actions and in most cases this is prefered.  VRChat limits avatars to 16 total parameters.  When actions share a parameter only one can be active at a time.
- **Object Properties** - Simple actions you want to perform when this action is enabled.  (Toggle objects, swap materials, play audio, etc...)
- **Animations** - Animations to play when this control is active.
	- Enter - The base animation, it plays at the start.
	- Exit - An optional animtion to play when exiting.
- **Parameter Drivers (Advanced)** - Modify parameters or menu toggles when this control is active.
- **IK Overrides (Advanced)** - Informs the avatar to disable certain IK controls over the avatar while the control is active.
- **Triggers (Advanced)** - Additional enter or exit conditions for this action.
- **Sub-Actions (Experimental)** - Supply any number of non-menu action sets.  These actions will only activate if this control is also active.
