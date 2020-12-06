using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;

[CreateAssetMenu(fileName = "AvatarActions", menuName = "Tropical/AvatarActions")]
public class AvatarActions : ScriptableObject
{
    //Root menu
    public bool isRootMenu = false;
    public AvatarGestures gesturesL;
    public AvatarGestures gesturesR;

    //Root meta-data
    public bool foldoutMeta = false;

    [Serializable]
    public class Action
    {
        public bool enabled = true;

        public string name;
        public Texture2D icon;

        [Serializable]
        public struct Animations
        {
            public UnityEngine.AnimationClip enter;
            public UnityEngine.AnimationClip exit;
        }

        public Animations actionLayerAnimations = new Animations();
        public Animations fxLayerAnimations = new Animations();
        public List<GameObject> toggleObjects = new List<GameObject>();

        public float fadeIn = 0;
        public float fadeOut = 0;

        public enum ActionType
        {
            Button,
            Toggle,
            Slider,
            SubMenu,
            PreExisting,
        }
        public ActionType type;
        public bool IsMenuControl()
        {
            return type == ActionType.Button || type == ActionType.Toggle || type == ActionType.SubMenu || type == ActionType.PreExisting;
        }

        public string parameter;

        [Serializable]
        public struct BodyOverride
        {
            public bool head;
            public bool leftHand;
            public bool rightHand;
            public bool hip;
            public bool leftFoot;
            public bool rightFoot;
            public bool leftFingers;
            public bool rightFingers;
            public bool eyes;
            public bool mouth;
        }
        public BodyOverride bodyOverride = new BodyOverride();

        //Sub-Menu
        public AvatarActions subMenu;

        //Gesture
        public struct GestureType
        {
            public bool neutral;
            public bool fist;
            public bool openHand;
            public bool fingerPoint;
            public bool victory;
            public bool rockNRoll;
            public bool handGun;
            public bool thumbsUp;

            public bool GetValue(GestureEnum type)
            {
                switch(type)
                {
                    case GestureEnum.Neutral: return neutral;
                    case GestureEnum.Fist: return fist;
                    case GestureEnum.OpenHand: return openHand;
                    case GestureEnum.FingerPoint: return fingerPoint;
                    case GestureEnum.Victory: return victory;
                    case GestureEnum.RockNRoll: return rockNRoll;
                    case GestureEnum.HandGun: return handGun;
                    case GestureEnum.ThumbsUp: return thumbsUp;
                }
                return false;
            }
            public void SetValue(GestureEnum type, bool value)
            {
                switch (type)
                {
                    case GestureEnum.Neutral: neutral = value; break;
                    case GestureEnum.Fist: fist = value; break;
                    case GestureEnum.OpenHand: openHand = value; break;
                    case GestureEnum.FingerPoint: fingerPoint = value; break;
                    case GestureEnum.Victory: victory = value; break;
                    case GestureEnum.RockNRoll: rockNRoll = value; break;
                    case GestureEnum.HandGun: handGun = value; break;
                    case GestureEnum.ThumbsUp: thumbsUp = value; break;
                }
            }
        }
        public GestureType gestureType = new GestureType();
        public enum GestureEnum
        {
            Neutral,
            Fist,
            OpenHand,
            FingerPoint,
            Victory,
            RockNRoll,
            HandGun,
            ThumbsUp,
        }
        public AvatarGestures gestureSet;
        //public const int GestureCount = 7;
        //public GestureType gestureType;

        //Metadata
        public bool foldoutMain = false;
        public bool foldoutIkOverrides = false;
        public bool foldoutObjects = false;
        public bool foldoutAnimations = false;
        public int controlValue = 0;

        public enum AnimationLayer
        {
            Action,
            FX,
        }
        public UnityEngine.AnimationClip GetAnimation(AnimationLayer layer, bool enter = true)
        {
            if (layer == AnimationLayer.Action)
            {
                if (enter)
                    return this.actionLayerAnimations.enter;
                else
                    return this.actionLayerAnimations.exit;
            }
            else if(layer == AnimationLayer.FX)
            {
                if (enter)
                    return this.fxLayerAnimations.enter;
                else
                    return this.fxLayerAnimations.exit;
            }
            return null;
        }
    }
    public List<Action> actions = new List<Action>();

    public enum BaseLayers
    {
        Action = 3,
        FX = 4
    }

    [NonSerialized]
    static AvatarDescriptor avatarDescriptor = null;
    static AvatarActions RootMenu = null;
    static List<Action> AllActions = new List<Action>();
    static List<ExpressionParameters.Parameter> AllParameters = new List<ExpressionParameters.Parameter>();
    static bool BuildFailed = false;
    public static void BuildAnimationControllers(AvatarDescriptor desc, AvatarActions rootMenu)
    {
        //Store
        avatarDescriptor = desc;
        RootMenu = rootMenu;
        BuildFailed = false;

        //Build
        BuildMain();

        //Error
        if(BuildFailed)
        {
            EditorUtility.DisplayDialog("Build Failed", "Build has failed.", "Okay");
        }
    }
    public static void BuildMain()
    {
        //Parameters
        BuildExpressionParameters(RootMenu);
        if (BuildFailed)
            return;

        //Find all actions
        {
            AllActions.Clear();
            List<AvatarActions> menuList = new List<AvatarActions>();
            AddMenu(RootMenu);
            void AddMenu(AvatarActions menu)
            {
                foreach (var action in menu.actions)
                {
                    if (!action.enabled)
                        continue;
                    AllActions.Add(action);

                    //Sub-Menu
                    if (action.type == Action.ActionType.SubMenu && !menuList.Contains(action.subMenu))
                    {
                        menuList.Add(action.subMenu);
                        AddMenu(action.subMenu);
                    }
                }
            }
        }

        //Define action values
        BuildActionValues();

        //Action Controller
        avatarDescriptor.customizeAnimationLayers = true;
        var actionController = GetController(AvatarActions.BaseLayers.Action, "AnimationController_Action");
        var fxController = GetController(AvatarActions.BaseLayers.FX, "AnimationController_FX");

        AnimatorController GetController(AvatarActions.BaseLayers index, string name)
        {
            var layer = avatarDescriptor.baseAnimationLayers[(int)index];

            var controller = layer.animatorController as UnityEditor.Animations.AnimatorController;
            if(controller == null || layer.isDefault)
            {
                //Path
                var path = AssetDatabase.GetAssetPath(RootMenu);
                path = path.Replace(Path.GetFileName(path), name + ".controller");

                //Create
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

                //Save
                layer.animatorController = controller;
                layer.isDefault = false;
                avatarDescriptor.baseAnimationLayers[(int)index] = layer;
                EditorUtility.SetDirty(avatarDescriptor);
                AssetDatabase.SaveAssets();
            }
            return controller;
        }

        //True
        AddParameter(actionController, "True", AnimatorControllerParameterType.Bool, 1);
        AddParameter(fxController, "True", AnimatorControllerParameterType.Bool, 1);

        //Action Layer
        BuildToggles_Action(actionController);
        BuildSliders(actionController, Action.AnimationLayer.Action);
        BuildGestures(actionController, Action.AnimationLayer.Action);

        //FX Layer
        BuildToggles_FX(fxController, Action.AnimationLayer.FX);
        BuildObjectToggles(fxController);
        BuildSliders(fxController, Action.AnimationLayer.FX);
        BuildGestures(fxController, Action.AnimationLayer.FX);

        //Build expressions menu
        BuildExpressionsMenu(RootMenu);
    }

    static void BuildExpressionsMenu(AvatarActions rootMenu)
    {
        List<AvatarActions> menuList = new List<AvatarActions>();

        //Create root menu if needed
        if(avatarDescriptor.expressionsMenu == null)
        {
            avatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<ExpressionsMenu>();
            avatarDescriptor.expressionsMenu.name = "ExpressionsMenu_Root";
            SaveAsset(avatarDescriptor.expressionsMenu, rootMenu);
        }

        //Expressions
        CreateMenu(rootMenu, avatarDescriptor.expressionsMenu);
        void CreateMenu(AvatarActions ourMenu, ExpressionsMenu expressionsMenu)
        {
            //Clear old controls
            List<ExpressionsMenu.Control> oldControls = new List<ExpressionsMenu.Control>();
            oldControls.AddRange(expressionsMenu.controls);
            expressionsMenu.controls.Clear();

            //Create controls from actions
            foreach (var action in ourMenu.actions)
            {
                if (!action.enabled)
                    continue;
                if (action.type == Action.ActionType.Button)
                {
                    //Create control
                    var control = new ExpressionsMenu.Control();
                    control.name = action.name;
                    control.icon = action.icon;
                    control.type = ExpressionsMenu.Control.ControlType.Button;
                    control.parameter = new ExpressionsMenu.Control.Parameter();
                    control.parameter.name = action.parameter;
                    control.value = action.controlValue;
                    expressionsMenu.controls.Add(control);
                }
                else if (action.type == Action.ActionType.Toggle)
                {
                    //Create control
                    var control = new ExpressionsMenu.Control();
                    control.name = action.name;
                    control.icon = action.icon;
                    control.type = ExpressionsMenu.Control.ControlType.Toggle;
                    control.parameter = new ExpressionsMenu.Control.Parameter();
                    control.parameter.name = action.parameter;
                    control.value = action.controlValue;
                    expressionsMenu.controls.Add(control);
                }
                else if(action.type == Action.ActionType.Slider)
                {
                    //Create control
                    var control = new ExpressionsMenu.Control();
                    control.name = action.name;
                    control.icon = action.icon;
                    control.type = ExpressionsMenu.Control.ControlType.RadialPuppet;
                    control.subParameters = new ExpressionsMenu.Control.Parameter[1];
                    control.subParameters[0] = new ExpressionsMenu.Control.Parameter();
                    control.subParameters[0].name = action.parameter;
                    control.value = action.controlValue;
                    expressionsMenu.controls.Add(control);
                }
                else if(action.type == Action.ActionType.SubMenu)
                {
                    //Recover old sub-menu
                    ExpressionsMenu expressionsSubMenu = null;
                    foreach (var controlIter in oldControls)
                    {
                        if (controlIter.name == action.name)
                        {
                            expressionsSubMenu = controlIter.subMenu;
                            break;
                        }
                    }

                    //Create if needed
                    if(expressionsSubMenu == null)
                    {
                        expressionsSubMenu = ScriptableObject.CreateInstance<ExpressionsMenu>();
                        expressionsSubMenu.name = "ExpressionsMenu_" + action.name;
                        SaveAsset(expressionsSubMenu, rootMenu);
                    }

                    //Create control
                    var control = new ExpressionsMenu.Control();
                    control.name = action.name;
                    control.icon = action.icon;
                    control.type = ExpressionsMenu.Control.ControlType.SubMenu;
                    control.subMenu = expressionsSubMenu;
                    expressionsMenu.controls.Add(control);

                    //Populate sub-menu
                    CreateMenu(action.subMenu, expressionsSubMenu);
                }
                else if(action.type == Action.ActionType.PreExisting)
                {
                    //Recover old control
                    foreach(var controlIter in oldControls)
                    {
                        if(controlIter.name == action.name)
                        {
                            oldControls.Remove(controlIter);
                            expressionsMenu.controls.Add(controlIter);
                            break;
                        }
                    }
                }
            }

            //Save prefab
            EditorUtility.SetDirty(expressionsMenu);
        }

        //Save all assets
        AssetDatabase.SaveAssets();
    }
    static void BuildExpressionParameters(AvatarActions rootMenu)
    {
        //Check if parameter object exists
        ExpressionParameters parametersObject = avatarDescriptor.expressionParameters;
        if (avatarDescriptor.expressionParameters == null || !avatarDescriptor.customExpressions)
        {
            parametersObject = ScriptableObject.CreateInstance<ExpressionParameters>();
            parametersObject.name = "ExpressionParameters";
            SaveAsset(parametersObject, rootMenu);

            avatarDescriptor.customExpressions = true;
            avatarDescriptor.expressionParameters = parametersObject;
        }

        //Find all parameters
        AllParameters.Clear();
        SearchForParameters(rootMenu);
        void SearchForParameters(AvatarActions menu)
        {
            //Check ours
            foreach(var action in menu.actions)
            {
                var param = GenerateParameter(action);
                if(param != null && IsNewParameter(param))
                    AllParameters.Add(param);
            }

            //Check children
            foreach (var action in menu.actions)
            {
                if(action.type == Action.ActionType.SubMenu && action.subMenu != null)
                    SearchForParameters(action.subMenu);
            }
        }
        bool IsNewParameter(ExpressionParameters.Parameter param)
        {
            foreach(var item in AllParameters)
            {
                if (String.IsNullOrEmpty(item.name))
                    continue;
                if(item.name == param.name)
                {
                    if (item.valueType == param.valueType)
                        return false;
                    else
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", String.Format("Unable to build VRCExpressionParameters. Parameter named '{0}' is used twice but with different types.", item.name), "Okay");
                        return false;
                    }
                }
            }
            return true;
        }

        //Check parameter count
        if(AllParameters.Count > ExpressionParameters.MAX_PARAMETERS)
        {
            BuildFailed = true;
            EditorUtility.DisplayDialog("Build Error", String.Format("Unable to build VRCExpressionParameters. Found more then {0} parameters", ExpressionParameters.MAX_PARAMETERS), "Okay");
            return;
        }

        //Build
        parametersObject.parameters = new ExpressionParameters.Parameter[ExpressionParameters.MAX_PARAMETERS];
        for (int i = 0; i < AllParameters.Count; i++)
            parametersObject.parameters[i] = AllParameters[i];

        //Save prefab
        EditorUtility.SetDirty(parametersObject);
        AssetDatabase.SaveAssets();
    }

    static void BuildActionValues()
    {
        var parametersObject = avatarDescriptor.expressionParameters;
        foreach (var parameter in parametersObject.parameters)
        {
            if (parameter == null || String.IsNullOrEmpty(parameter.name))
                continue;

            //Find all actions
            int actionCount = 1;
            foreach (var action in AllActions)
            {
                if(action.parameter == parameter.name)
                {
                    action.controlValue = actionCount;
                    actionCount += 1;
                }
            }
        }
    }

    //Toggle - Action
    static void BuildToggles_Action(UnityEditor.Animations.AnimatorController controller)
    {
        //For each parameter create a new layer
        foreach (var parameter in AllParameters)
        {
            BuildToggleLayer_Action(controller, parameter.name);
        }
    }
    static void BuildToggleLayer_Action(UnityEditor.Animations.AnimatorController controller, string parameter)
    {
        //Find all option actions
        var layerActions = new List<AvatarActions.Action>();
        foreach (var action in AllActions)
        {
            if (action.parameter != parameter)
                continue;
            if (action.type != Action.ActionType.Button && action.type != Action.ActionType.Toggle)
                continue;
            if (action.actionLayerAnimations.enter == null)
                continue;
            layerActions.Add(action);
        }
        if (layerActions.Count == 0)
            return;

        AddParameter(controller, parameter, AnimatorControllerParameterType.Int, 0);

        //Prepare layer
        var layer = GetControllerLayer(controller, parameter);
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(6, 0);

        //Animation Layer Weight
        int layerIndex = 0;
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == layer.name)
            {
                layerIndex = i;
                break;
            }
        }

        //Waiting state
        var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));

        //Actions
        for (int actionIter = 0; actionIter < layerActions.Count; actionIter++)
        {
            var action = layerActions[actionIter];
            UnityEditor.Animations.AnimatorState lastState;

            //Enter state
            {
                var state = layer.stateMachine.AddState(action.name + "_Enter", StatePosition(1, actionIter));

                //Transition
                var transition = waitingState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.Equals, action.controlValue, action.parameter);

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action;

                //Playable Layer
                var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                playable.layer = VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.Action;
                playable.goalWeight = 1.0f;
                playable.blendDuration = action.fadeIn;

                //Tracking
                SetupTracking(action, state, TrackingType.Animation);

                //Store
                lastState = state;
            }

            //Enable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(2, actionIter));
                state.motion = action.actionLayerAnimations.enter;

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.exitTime = 0f;
                transition.duration = action.fadeIn;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Store
                lastState = state;
            }

            //Disable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(3, actionIter));
                state.motion = action.actionLayerAnimations.exit != null ? action.actionLayerAnimations.exit : action.actionLayerAnimations.enter;

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.NotEqual, action.controlValue, action.parameter);

                //Playable Layer
                var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                playable.goalWeight = 0.0f;
                playable.blendDuration = action.fadeOut;

                //Store
                lastState = state;
            }

            //Complete state
            {
                var state = layer.stateMachine.AddState(action.name + "_Complete", StatePosition(4, actionIter));

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.exitTime = 0f;
                transition.duration = action.fadeOut;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Store
                lastState = state;
            }

            //Cleanup state
            {
                var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(5, actionIter));

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.exitTime = 0f;
                transition.duration = 0f;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Exit Transition
                transition = state.AddExitTransition();
                transition.hasExitTime = false;
                transition.exitTime = 0f;
                transition.duration = 0f;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action;

                //Tracking
                SetupTracking(action, state, TrackingType.Tracking);

                //Store
                lastState = state;
            }
        }
    }

    //Toggle - FX
    static void BuildToggles_FX(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType)
    {
        //For each parameter create a new layer
        foreach (var parameter in AllParameters)
        {
            BuildToggleLayer_FX(controller, layerType, parameter.name);
        }
    }
    static void BuildToggleLayer_FX(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType, string parameter)
    {
        //Find all option actions
        var layerActions = new List<AvatarActions.Action>();
        foreach (var action in AllActions)
        {
            if (action.parameter != parameter)
                continue;
            if (action.type != Action.ActionType.Button && action.type != Action.ActionType.Toggle)
                continue;
            if (action.GetAnimation(layerType) == null)
                continue;
            layerActions.Add(action);
        }
        if (layerActions.Count == 0)
            return;

        //Add parameter
        AddParameter(controller, parameter, AnimatorControllerParameterType.Int, 0);

        //Prepare layer
        var layer = GetControllerLayer(controller, parameter);
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(3, 0);

        //Animation Layer Weight
        var layerIndex = GetLayerIndex(controller, layer);

        //Waiting state
        var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));

        //Each action
        for (int actionIter = 0; actionIter < layerActions.Count; actionIter++)
        {
            var action = layerActions[actionIter];
            AnimatorState lastState = waitingState;

            //Enable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                state.motion = action.GetAnimation(layerType, true);

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = action.fadeIn;
                transition.AddCondition(AnimatorConditionMode.Equals, action.controlValue, action.parameter);

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                //Tracking
                SetupTracking(action, state, TrackingType.Animation);

                //Store
                lastState = state;
            }

            //Disable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(2, actionIter));
                state.motion = action.GetAnimation(layerType, false);

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = action.fadeOut;
                transition.AddCondition(AnimatorConditionMode.NotEqual, action.controlValue, action.parameter);

                //Transition
                transition = state.AddExitTransition();
                transition.hasExitTime = false;
                transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = action.fadeOut;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                //Tracking
                SetupTracking(action, state, TrackingType.Tracking);

                //Store
                lastState = state;
            }
        }
    }

    static void BuildObjectToggles(UnityEditor.Animations.AnimatorController controller)
    {
        //For each parameter create a new layer
        foreach (var parameter in AllParameters)
        {
            BuildObjectToggleLayer(controller, parameter.name);
        }
    }
    static void BuildObjectToggleLayer(UnityEditor.Animations.AnimatorController controller, string parameter)
    {
        //Find all option actions
        var layerActions = new List<AvatarActions.Action>();
        foreach (var action in AllActions)
        {
            if (action.parameter != parameter)
                continue;
            if (action.type != AvatarActions.Action.ActionType.Button && action.type != AvatarActions.Action.ActionType.Toggle)
                continue;
            if (action.toggleObjects.Count == 0)
                continue;
            layerActions.Add(action);
        }
        if (layerActions.Count == 0)
            return;

        //Add parameter
        AddParameter(controller, parameter, AnimatorControllerParameterType.Int, 0);

        //Prepare layer
        var layerName = parameter + "_ObjectToggle";
        var layer = GetControllerLayer(controller, layerName);
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(3, 0);

        //Animation Layer Weight
        var layerIndex = GetLayerIndex(controller, layer);

        //Waiting state
        var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));

        //Each action
        int actionValue = 1;
        for (int actionIter = 0; actionIter < layerActions.Count; actionIter++)
        {
            var action = layerActions[actionIter];
            AnimatorState lastState = waitingState;

            //Store
            action.controlValue = actionValue;

            //Build animation
            var animation = BuildObjectToggleAnimation(action);

            //Enable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                state.motion = animation;

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = action.fadeIn;
                transition.AddCondition(AnimatorConditionMode.Equals, action.controlValue, action.parameter);

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                //Tracking
                SetupTracking(action, state, TrackingType.Animation);

                //Store
                lastState = state;
            }

            //Disable state
            {
                var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(2, actionIter));
                state.motion = animation;

                //Transition
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = action.fadeOut;
                transition.AddCondition(AnimatorConditionMode.NotEqual, actionValue, action.parameter);

                //Transition
                transition = state.AddExitTransition();
                transition.hasExitTime = false;
                transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = action.fadeOut;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                //Tracking
                SetupTracking(action, state, TrackingType.Tracking);

                //Store
                lastState = state;
            }

            //Iterate
            actionValue += 1;
        }
    }
    static UnityEngine.AnimationClip BuildObjectToggleAnimation(Action action)
    {
        var animation = new UnityEngine.AnimationClip();

        //Toggle keyframes
        foreach(var obj in action.toggleObjects)
        {
            //Create curve
            var objPath = GetObjectPath(obj);
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1f));
            animation.SetCurve(objPath, typeof(GameObject), "m_IsActive", curve);

            //Disable the object
            obj.SetActive(false);
        }

        string GetObjectPath(GameObject obj)
        {
            string path = obj.name;
            while(true)
            {
                obj = obj.transform.parent?.gameObject;
                if (obj == null)
                    return null;
                if (obj == avatarDescriptor.gameObject)
                    break;                
                path = $"{obj.name}/{path}";
            }
            return path;
        }

        //Save
        animation.name = action.name + "_ObjectToggle";
        SaveAsset(animation, RootMenu);

        //Return
        return animation;
    }
    
    //Sliders
    static void BuildSliders(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType)
    {
        //For each parameter create a new layer
        foreach (var parameter in AllParameters)
        {
            BuildSliderLayer(controller, layerType, parameter.name);
        }
    }
    static void BuildSliderLayer(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType, string parameter)
    {
        //Find all option actions
        var layerActions = new List<AvatarActions.Action>();
        foreach (var actionIter in AllActions)
        {
            if (actionIter.type == AvatarActions.Action.ActionType.Slider && actionIter.parameter == parameter && actionIter.GetAnimation(layerType) != null)
                layerActions.Add(actionIter);
        }
        if (layerActions.Count == 0)
            return;
        var action = layerActions[0];

        //Add parameter
        AddParameter(controller, parameter, AnimatorControllerParameterType.Float, 0);

        //Prepare layer
        var layer = GetControllerLayer(controller, parameter);
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(-1, 2);

        int layerIndex = GetLayerIndex(controller, layer);

        //Blend state
        {
            var state = layer.stateMachine.AddState(action.name + "_Blend", StatePosition(0, 0));
            state.motion = action.GetAnimation(layerType);
            state.timeParameter = action.parameter;
            state.timeParameterActive = true;

            //Animation Layer Weight
            var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
            layerWeight.goalWeight = 1;
            layerWeight.layer = layerIndex;
            layerWeight.blendDuration = 0;
            layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
        }
    }

    //Gesture
    static void BuildGestures(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType)
    {
        if (RootMenu.gesturesL != null)
            BuildGestureLayer(controller, layerType, RootMenu.gesturesL, "GestureLeft");
        if (RootMenu.gesturesR != null)
            BuildGestureLayer(controller, layerType, RootMenu.gesturesR, "GestureRight");
    }
    static void BuildGestureLayer(UnityEditor.Animations.AnimatorController controller, Action.AnimationLayer layerType, AvatarGestures gestureSet, string parameter)
    {
        //Prepare layer
        var layer = GetControllerLayer(controller, parameter);
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(-1, 2);

        int layerIndex = GetLayerIndex(controller, layer);

        //Default state
        var defaultAction = gestureSet.defaultAction;
        var defaultState = layer.stateMachine.AddState("Default Gesture", StatePosition(0, 0));
        var unusedGestures = new List<Action.GestureEnum>();
        unusedGestures.Add(Action.GestureEnum.Neutral);
        unusedGestures.Add(Action.GestureEnum.Fist);
        unusedGestures.Add(Action.GestureEnum.OpenHand);
        unusedGestures.Add(Action.GestureEnum.FingerPoint);
        unusedGestures.Add(Action.GestureEnum.Victory);
        unusedGestures.Add(Action.GestureEnum.RockNRoll);
        unusedGestures.Add(Action.GestureEnum.HandGun);
        unusedGestures.Add(Action.GestureEnum.ThumbsUp);

        //Build states
        int actionIter = 0;
        foreach(var action in gestureSet.actions)
        {
            //State
            {
                //Build
                var state = layer.stateMachine.AddState(action.name, StatePosition(0, actionIter + 1));
                state.motion = action.GetAnimation(layerType, true);
                actionIter += 1;

                //Transition
                var transition = layer.stateMachine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.exitTime = 0;
                transition.duration = action.fadeIn;

                //Conditions
                AddGestureCondition(Action.GestureEnum.Neutral);
                AddGestureCondition(Action.GestureEnum.Fist);
                AddGestureCondition(Action.GestureEnum.OpenHand);
                AddGestureCondition(Action.GestureEnum.FingerPoint);
                AddGestureCondition(Action.GestureEnum.Victory);
                AddGestureCondition(Action.GestureEnum.RockNRoll);
                AddGestureCondition(Action.GestureEnum.HandGun);
                AddGestureCondition(Action.GestureEnum.ThumbsUp);
                void AddGestureCondition(AvatarActions.Action.GestureEnum gesture)
                {
                    if (action.gestureType.GetValue(gesture))
                    {
                        transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, parameter);
                        unusedGestures.Remove(gesture);
                    }
                }
            }
        }

        //Default transitions
        foreach(var gesture in unusedGestures)
        {
            //Transition
            var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
            transition.hasExitTime = false;
            transition.exitTime = 0;
            transition.duration = defaultAction.fadeIn;
            transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, parameter);
        }
    }

    //Helpers
    static void SetupTracking(AvatarActions.Action action, AnimatorState state, TrackingType trackingType)
    {
        //Check for any change
        if (!action.bodyOverride.head &&
            !action.bodyOverride.leftHand &&
            !action.bodyOverride.rightHand &&
            !action.bodyOverride.hip &&
            !action.bodyOverride.leftFoot &&
            !action.bodyOverride.rightFoot &&
            !action.bodyOverride.leftFingers &&
            !action.bodyOverride.rightFingers &&
            !action.bodyOverride.eyes &&
            !action.bodyOverride.mouth)
            return;

        //Add tracking behaviour
        var tracking = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl>();
        tracking.trackingHead = action.bodyOverride.head ? trackingType : TrackingType.NoChange;
        tracking.trackingLeftHand = action.bodyOverride.leftHand ? trackingType : TrackingType.NoChange;
        tracking.trackingRightHand = action.bodyOverride.rightHand ? trackingType : TrackingType.NoChange;
        tracking.trackingHip = action.bodyOverride.hip ? trackingType : TrackingType.NoChange;
        tracking.trackingLeftFoot = action.bodyOverride.leftFoot ? trackingType : TrackingType.NoChange;
        tracking.trackingRightFoot = action.bodyOverride.rightFoot ? trackingType : TrackingType.NoChange;
        tracking.trackingLeftFingers = action.bodyOverride.leftFingers ? trackingType : TrackingType.NoChange;
        tracking.trackingRightFingers = action.bodyOverride.rightFingers ? trackingType : TrackingType.NoChange;
        tracking.trackingEyes = action.bodyOverride.eyes ? trackingType : TrackingType.NoChange;
        tracking.trackingMouth = action.bodyOverride.mouth ? trackingType : TrackingType.NoChange;
    }
    static Vector3 StatePosition(int x, int y)
    {
        return new Vector3(x * 300, y * 100, 0);
    }
    static int GetLayerIndex(AnimatorController controller, AnimatorControllerLayer layer)
    {
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == layer.name)
            {
                return i;
            }
        }
        return -1;
    }
    static ExpressionParameters.Parameter GenerateParameter(AvatarActions.Action action)
    {
        if (String.IsNullOrEmpty(action.parameter))
            return null;
        var parameter = new ExpressionParameters.Parameter();
        parameter.name = action.parameter;
        switch (action.type)
        {
            case Action.ActionType.Button:
            case Action.ActionType.Toggle:
                parameter.valueType = ExpressionParameters.ValueType.Int;
                break;
            case Action.ActionType.Slider:
                parameter.valueType = ExpressionParameters.ValueType.Float;
                break;
        }
        return parameter;
    }

    public static void SaveAsset(UnityEngine.Object asset, AvatarActions rootAsset, bool checkIfExists=false)
    {
        //Path
        var path = AssetDatabase.GetAssetPath(rootAsset);
        path = path.Replace(Path.GetFileName(path), asset.name + ".asset");

        //Check if existing
        var existing = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject));
        if(checkIfExists && existing != null && existing != asset)
        {
            if (!EditorUtility.DisplayDialog("Replace Asset?", String.Format("Another asset already exists at '{0}'.\nAre you sure you want to replace it?", path), "Replace", "Cancel"));
                return;
        }

        AssetDatabase.CreateAsset(asset, path);
    }

    #region AnimationControllerMethods
    static UnityEditor.Animations.AnimatorControllerLayer GetControllerLayer(UnityEditor.Animations.AnimatorController controller, string name)
    {
        //Check if exists
        foreach (var layer in controller.layers)
        {
            if (layer.name == name)
                return layer;
        }

        //Create
        controller.AddLayer(name);
        return controller.layers[controller.layers.Length - 1];
    }
    static AnimatorControllerParameter AddParameter(UnityEditor.Animations.AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
    {
        //Clear
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name == name)
            {
                controller.RemoveParameter(i);
                break;
            }
        }

        //Create
        var param = new AnimatorControllerParameter();
        param.name = name;
        param.type = type;
        param.defaultBool = value >= 1f;
        param.defaultInt = (int)value;
        param.defaultFloat = value;
        controller.AddParameter(param);

        return param;
    }

    #endregion
}