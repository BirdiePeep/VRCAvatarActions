using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;

namespace VRCAvatarActions
{
    public abstract class AvatarActions : ScriptableObject
    {
        //Types
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
        public enum VisimeEnum
        {
            Sil,
            PP,
            FF,
            TH,
            DD,
            KK,
            CH,
            SS,
            NN,
            RR,
            AA,
            E,
            I,
            O,
            U
        }
        public enum TrackingTypeEnum
        {
            Generic = 1,
            ThreePoint = 3,
            FourPoint = 4,
            FullBody = 6,
        }
        public enum ParameterEnum
        {
            Custom,
            GestureLeft,
            GestureRight,
            GestureLeftWeight,
            GestureRightWeight,
            Visime,
            AFK,
            VRMode,
            TrackingType,
            MuteSelf,
            AngularY,
            VelocityX,
            VelocityY,
            VelocityZ,
            Upright,
            Grounded,
            Seated,
            InStation,
            IsLocal
        }
        public enum ParameterType
        {
            Bool,
            Int,
            Float,
            Trigger
        }
        public enum AnimationLayer
        {
            Action,
            FX,
        }

        [System.Serializable]
        public class Action
        {
            //Simple Data
            public bool enabled = true;
            public string name;

            //Animations
            [System.Serializable]
            public class Animations
            {
                //Source
                public UnityEngine.AnimationClip enter;
                public UnityEngine.AnimationClip exit;

                //Generated
                public AnimationClip enterGenerated;
                public AnimationClip exitGenerated;
            }
            public Animations actionLayerAnimations = new Animations();
            public Animations fxLayerAnimations = new Animations();
            public float fadeIn = 0;
            public float fadeOut = 0;

            public bool HasAnimations()
            {
                return actionLayerAnimations.enter != null || actionLayerAnimations.exit || fxLayerAnimations.enter != null || fxLayerAnimations.exit;
            }
            public bool AffectsAnyLayer()
            {
                bool result = false;
                result |= AffectsLayer(AnimationLayer.Action);
                result |= AffectsLayer(AnimationLayer.FX);
                return result;
            }
            public bool AffectsLayer(AnimationLayer layerType)
            {
                if (GetAnimationRaw(layerType, true) != null)
                    return true;
                if (GeneratesLayer(layerType))
                    return true;
                return false;
            }
            public bool GeneratesLayer(AnimationLayer layerType)
            {
                if (layerType == AnimationLayer.FX)
                {
                    if (objProperties.Count > 0)
                        return true;
                }
                return false;
            }
            public virtual bool HasExit()
            {
                return true;
            }
            public virtual bool ShouldBuild()
            {
                if (!enabled)
                    return false;
                return true;
            }

            //Properties
            [System.Serializable]
            public class Property
            {
                //Data
                public string path;

                //Meta-data
                public GameObject objRef;

                public void Clear()
                {
                    objRef = null;
                    path = null;
                }
            }
            public List<Property> objProperties = new List<Property>();

            //Material Swaps
            [System.Serializable]
            public class MaterialSwap : Property
            {
                //Data
                public List<Material> materials = new List<Material>();
            }
            public List<MaterialSwap> materialSwaps = new List<MaterialSwap>();

            //Triggers
            [System.Serializable]
            public class Trigger
            {
                public enum Type
                {
                    Enter,
                    Exit,
                }
                public Type type;
                public List<Condition> conditions = new List<Condition>();
                public bool foldout;
            }

            [System.Serializable]
            public class Condition
            {
                public enum Logic
                {
                    Equals = 0,
                    NotEquals = 1,
                    GreaterThen = 2,
                    LessThen = 3,
                }
                public enum LogicEquals
                {
                    Equals = 0,
                    NotEquals = 1,
                }
                public enum LogicCompare
                {
                    GreaterThen = 2,
                    LessThen = 3,
                }

                public string GetParameter()
                {
                    if (type == ParameterEnum.Custom)
                        return parameter;
                    else
                        return type.ToString();
                }

                public ParameterEnum type;
                public string parameter;
                public Logic logic = Logic.Equals;
                public float value = 1;
                public bool shared = false;
            }
            public List<Trigger> triggers = new List<Trigger>();

            [System.Serializable]
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

                public void SetAll(bool value)
                {
                    head = value;
                    leftHand = value;
                    rightHand = value;
                    hip = value;
                    leftFoot = value;
                    rightFoot = value;
                    leftFingers = value;
                    rightFingers = value;
                    eyes = value;
                    mouth = value;
                }
                public bool GetAll()
                {
                    return
                        head &&
                        leftHand &&
                        rightHand &&
                        hip &&
                        leftFoot &&
                        rightFoot &&
                        leftFingers &&
                        rightFingers &&
                        eyes &&
                        mouth;
                }
                public bool HasAny()
                {
                    return head || leftHand || rightHand || hip || leftFoot || rightFoot || leftFingers || rightFingers || eyes || mouth;
                }
            }
            public BodyOverride bodyOverride = new BodyOverride();

            //Build
            public virtual string GetLayerGroup()
            {
                return null;
            }
            public void AddTransitions(UnityEditor.Animations.AnimatorController controller, UnityEditor.Animations.AnimatorState lastState, UnityEditor.Animations.AnimatorState state, float transitionTime, AvatarActions.Action.Trigger.Type triggerType, MenuActions.MenuAction parentAction)
            {
                //Find valid triggers
                List<AvatarActions.Action.Trigger> triggers = new List<Action.Trigger>();
                foreach (var trigger in this.triggers)
                {
                    if (trigger.type == triggerType)
                        triggers.Add(trigger);
                }

                AnimatorConditionMode controlTrigger = (triggerType != Action.Trigger.Type.Exit) ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;

                //Add triggers
                if (triggers.Count > 0)
                {
                    //Add each transition
                    foreach (var trigger in triggers)
                    {
                        //Check type
                        if (trigger.type != triggerType)
                            continue;

                        //Add
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        this.AddCondition(transition, controlTrigger);

                        //Conditions
                        AddTriggerConditions(controller, transition, trigger.conditions);

                        //Parent Conditions - Enter
                        if (triggerType == Action.Trigger.Type.Enter && parentAction != null)
                            parentAction.AddCondition(transition, controlTrigger);

                        //Finalize
                        Finalize(transition);
                    }
                }
                else
                {
                    if(triggerType == Trigger.Type.Enter)
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        this.AddCondition(transition, controlTrigger);

                        //Parent Conditions
                        if (parentAction != null)
                            parentAction.AddCondition(transition, controlTrigger);

                        //Finalize
                        Finalize(transition);
                    }
                    else if (triggerType == Trigger.Type.Exit && this.HasExit())
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        this.AddCondition(transition, controlTrigger);

                        //Finalize
                        Finalize(transition);
                    }
                }

                //Parent Conditions - Exit
                if (triggerType == Action.Trigger.Type.Exit && parentAction != null)
                {
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = transitionTime;
                    parentAction.AddCondition(transition, controlTrigger);
                }

                void Finalize(AnimatorStateTransition transition)
                {
                    if(transition.conditions.Length == 0)
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }
            }
            public virtual void AddCondition(AnimatorStateTransition transition, AnimatorConditionMode mode)
            {
                //Nothing
            }

            //Metadata
            public bool foldoutMain = false;
            public bool foldoutTriggers = false;
            public bool foldoutIkOverrides = false;
            public bool foldoutObjects = false;
            public bool foldoutAnimations = false;

            UnityEngine.AnimationClip GetAnimationRaw(AnimationLayer layer, bool enter=true)
            {
                //Find layer group
                Action.Animations group;
                if (layer == AnimationLayer.Action)
                    group = this.actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = this.fxLayerAnimations;
                else
                    return null;

                if (enter)
                    return group.enter;
                else
                {
                    return group.exit != null ? group.exit : group.enter;
                }
            }
            public UnityEngine.AnimationClip GetAnimation(AnimationLayer layer, bool enter = true)
            {
                //Find layer group
                Action.Animations group;
                if (layer == AnimationLayer.Action)
                    group = this.actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = this.fxLayerAnimations;
                else
                    return null;

                //Return
                return enter ? GetEnter() : GetExit();

                AnimationClip GetEnter()
                {
                    //Generate
                    if (group.enterGenerated == null && GeneratesLayer(layer))
                        group.enterGenerated = BuildGeneratedAnimation(group.enter);

                    //Reutrn
                    return group.enterGenerated != null ? group.enterGenerated : group.enter;
                }
                AnimationClip GetExit()
                {
                    //Fallback to enter
                    if (group.exit == null)
                        return GetEnter();

                    //Generate
                    if (group.exitGenerated == null && GeneratesLayer(layer))
                        group.exitGenerated = BuildGeneratedAnimation(group.exit);

                    //Return
                    return group.exitGenerated != null ? group.exitGenerated : group.exit;
                }
            }
            protected AnimationClip BuildGeneratedAnimation(AnimationClip source)
            {
                //Create new animation
                AnimationClip animation = null;
                if (source != null)
                {
                    animation = new AnimationClip();
                    EditorUtility.CopySerialized(source, animation);
                }
                else
                    animation = new AnimationClip();

                //Toggle keyframes
                foreach (var item in this.objProperties)
                {
                    //Is anything defined?
                    if (string.IsNullOrEmpty(item.path))
                        continue;

                    //Find object
                    var obj = AvatarActionsEditor.FindPropertyObject(AvatarDescriptor.gameObject, item.path);
                    if (obj == null)
                        continue;

                    //Create curve
                    var curve = new AnimationCurve();
                    curve.AddKey(new Keyframe(0f, 1f));
                    animation.SetCurve(item.path, typeof(GameObject), "m_IsActive", curve);

                    //Disable the object
                    obj.SetActive(false);
                }

                //Save
                animation.name = this.name + "_Generated";
                SaveAsset(animation, ActionsDescriptor, "Generated");

                //Return
                return animation;
            }
        }

        public enum BaseLayers
        {
            Action = 3,
            FX = 4
        }

        public abstract void GetActions(List<Action> output);
        public abstract Action AddAction();
        public abstract void RemoveAction(Action action);
        public abstract void InsertAction(int index, Action action);

        protected static AvatarDescriptor AvatarDescriptor = null;
        protected static ActionsDescriptor ActionsDescriptor = null;
        protected static List<ExpressionParameters.Parameter> AllParameters = new List<ExpressionParameters.Parameter>();
        protected static UnityEditor.Animations.AnimatorController ActionController;
        protected static UnityEditor.Animations.AnimatorController FxController;
        protected static UnityEditor.Animations.AnimatorController GetController(AnimationLayer layer)
        {
            switch(layer)
            {
                case AnimationLayer.Action:
                    return ActionController;
                case AnimationLayer.FX:
                    return FxController;
            }
            return null;
        }
        protected static bool BuildFailed = false;

        public static void BuildAvatarData(AvatarDescriptor desc, ActionsDescriptor actionsDesc)
        {
            //Store
            AvatarDescriptor = desc;
            ActionsDescriptor = actionsDesc;
            BuildFailed = false;

            //Build
            BuildSetup();
            BuildMain();
            BuildCleanup();

            //Error
            if (BuildFailed)
            {
                EditorUtility.DisplayDialog("Build Failed", "Build has failed.", "Okay");
            }
        }
        public static void BuildSetup()
        {
            //Action Controller
            AvatarDescriptor.customizeAnimationLayers = true;
            ActionController = GetController(AvatarActions.BaseLayers.Action, "AnimationController_Action");
            FxController = GetController(AvatarActions.BaseLayers.FX, "AnimationController_FX");

            AnimatorController GetController(AvatarActions.BaseLayers index, string name)
            {
                //Find/Create Layer
                var descLayer = AvatarDescriptor.baseAnimationLayers[(int)index];
                var controller = descLayer.animatorController as UnityEditor.Animations.AnimatorController;
                if (controller == null || descLayer.isDefault)
                {
                    //Dir Path
                    var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor);
                    dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                    System.IO.Directory.CreateDirectory(dirPath);

                    //Create
                    var path = $"{dirPath}{name}.controller";
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

                    //Save
                    descLayer.animatorController = controller;
                    descLayer.isDefault = false;
                    AvatarDescriptor.baseAnimationLayers[(int)index] = descLayer;
                    EditorUtility.SetDirty(AvatarDescriptor);
                    AssetDatabase.SaveAssets();
                }

                //Cleanup Layers
                {
                    //Clean layers
                    for(int i=0; i< controller.layers.Length; i++)
                    {
                        if (ActionsDescriptor.ignoreLayers.Contains(controller.layers[i].name))
                            continue;

                        //Remove
                        controller.RemoveLayer(i);
                        i--;
                    }

                    //Clean parameters
                    for(int i=0; i<controller.parameters.Length; i++)
                    {
                        if (ActionsDescriptor.ignoreParameters.Contains(controller.parameters[i].name))
                            continue;

                        //Remove
                        controller.RemoveParameter(i);
                        i--;
                    }
                }

                //Add defaults
                AddParameter(controller, "True", AnimatorControllerParameterType.Bool, 1);

                //Return
                return controller;
            }

            //Delete all generated animations
            {
                var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor);
                dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                var files = System.IO.Directory.GetFiles(dirPath);
                foreach (var file in files)
                {
                    if (file.Contains("_Generated"))
                        System.IO.File.Delete(file);
                }
            }
        }
        public static void BuildMain()
        {
            //Build menu
            if (ActionsDescriptor.menuActions != null)
            {
                ActionsDescriptor.menuActions.Build();
                if (BuildFailed)
                    return;
            }

            //Build others
            foreach (var actionSet in ActionsDescriptor.otherActions)
            {
                if(actionSet != null)
                {
                    actionSet.Build(null);
                    if (BuildFailed)
                        return;
                }
            }
        }
        public static void BuildCleanup()
        {
            var components = AvatarDescriptor.gameObject.GetComponentsInChildren<ITemporaryComponent>();
            foreach (var comp in components)
                GameObject.DestroyImmediate(comp as MonoBehaviour);
        }

        //Normal
        protected static void BuildActionLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<Action> actions, string layerName, MenuActions.MenuAction parentAction)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
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
            int actionIter = 0;
            foreach(var action in actions)
            {
                UnityEditor.Animations.AnimatorState lastState;

                //Enter state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Setup", StatePosition(1, actionIter));
                    state.motion = action.actionLayerAnimations.enter;

                    //Transition
                    action.AddTransitions(controller, waitingState, state, 0, Action.Trigger.Type.Enter, parentAction);

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
                    action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

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
                    state.motion = action.actionLayerAnimations.exit != null ? action.actionLayerAnimations.exit : action.actionLayerAnimations.enter;

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0;
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

                //Iterate
                actionIter += 1;
            }
        }
        protected static void BuildNormalLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<Action> actions, string layerName, AnimationLayer layerType, MenuActions.MenuAction parentAction)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(5, 0);

            //Animation Layer Weight
            var layerIndex = GetLayerIndex(controller, layer);

            //Waiting state
            var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));

            //Each action
            int actionIter = 0;
            foreach(var action in actions)
            {
                AnimatorState lastState = waitingState;

                //Enable
                {
                    var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                    state.motion = action.GetAnimation(layerType, true);

                    //Transition
                    action.AddTransitions(controller, lastState, state, action.fadeIn, Action.Trigger.Type.Enter, parentAction);

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

                if(action.HasExit() || parentAction != null)
                {
                    //Disable
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(2, actionIter));
                        state.motion = action.GetAnimation(layerType, false);

                        //Transition
                        action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                        //Store
                        lastState = state;
                    }

                    //Complete
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Complete", StatePosition(3, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = action.fadeOut;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Store
                        lastState = state;
                    }

                    //Cleanup
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(4, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Animation Layer Weight
                        var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                        layerWeight.goalWeight = 0;
                        layerWeight.layer = layerIndex;
                        layerWeight.blendDuration = 0;
                        layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                        //Tracking
                        SetupTracking(action, state, TrackingType.Tracking);

                        //Store
                        lastState = state;
                    }

                    //Transition Exit
                    {
                        //Exit Transition
                        var transition = lastState.AddExitTransition();
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                    }
                }

                //Iterate
                actionIter += 1;
            }
        }

        //Generated
        protected static void BuildGroupedLayers(IEnumerable<Action> sourceActions, AnimationLayer layerType, MenuActions.MenuAction parentAction, System.Func<Action, bool> onCheck, System.Action<AnimatorController, string, List<Action>> onBuild)
        {
            var controller = GetController(layerType);

            //Build layer groups
            List<string> layerGroups = new List<string>();
            foreach (var action in sourceActions)
            {
                var group = action.GetLayerGroup();
                if (!string.IsNullOrEmpty(group) && !layerGroups.Contains(group))
                    layerGroups.Add(group);
            }

            //Build grouped layers
            var layerActions = new List<AvatarActions.Action>();
            foreach (var group in layerGroups)
            {
                //Check if valid
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.GetLayerGroup() != group)
                        continue;
                    if (!onCheck(action))
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Build
                onBuild(controller, group, layerActions);
            }

            //Build unsorted layers
            foreach (var action in sourceActions)
            {
                if (!string.IsNullOrEmpty(action.GetLayerGroup()))
                    continue;
                if (!onCheck(action))
                    continue;

                layerActions.Clear();
                layerActions.Add(action);
                onBuild(controller, action.name, layerActions);
            }
        }

        //Conditions
        protected static void AddTriggerConditions(UnityEditor.Animations.AnimatorController controller, AnimatorStateTransition transition, IEnumerable<AvatarActions.Action.Condition> conditions)
        {
            foreach (var condition in conditions)
            {
                //Find parameter data
                string paramName = condition.GetParameter();
                AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Int;
                switch (condition.type)
                {
                    //Bool
                    case ParameterEnum.AFK:
                    case ParameterEnum.Seated:
                    case ParameterEnum.Grounded:
                    case ParameterEnum.MuteSelf:
                    case ParameterEnum.InStation:
                    case ParameterEnum.IsLocal:
                        paramType = AnimatorControllerParameterType.Bool;
                        break;
                    //Int
                    case ParameterEnum.Visime:
                    case ParameterEnum.GestureLeft:
                    case ParameterEnum.GestureRight:
                    case ParameterEnum.VRMode:
                    case ParameterEnum.TrackingType:
                        paramType = AnimatorControllerParameterType.Int;
                        break;
                    //Float
                    case ParameterEnum.GestureLeftWeight:
                    case ParameterEnum.GestureRightWeight:
                    case ParameterEnum.AngularY:
                    case ParameterEnum.VelocityX:
                    case ParameterEnum.VelocityY:
                    case ParameterEnum.VelocityZ:
                        paramType = AnimatorControllerParameterType.Float;
                        break;
                    //Custom
                    case ParameterEnum.Custom:
                        {
                            //Find the parameter type
                            Debug.LogError("TODO");

                            //Add

                            break;
                        }
                    default:
                        {
                            Debug.LogError("Unknown parameter type");
                            continue;
                        }
                }

                //Add parameter
                AddParameter(controller, paramName, paramType, 0);

                //Add condition
                switch (paramType)
                {
                    case AnimatorControllerParameterType.Bool:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.NotEquals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        break;
                    case AnimatorControllerParameterType.Int:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.NotEquals ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.Equals, condition.value, paramName);
                        break;
                    case AnimatorControllerParameterType.Float:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.LessThen ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, condition.value, paramName);
                        break;
                }
            }

            //Default true
            if (transition.conditions.Length == 0)
                transition.AddCondition(AnimatorConditionMode.If, 1f, "True");
        }

        //Helpers
        protected static void SetupTracking(AvatarActions.Action action, AnimatorState state, TrackingType trackingType)
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
        protected static Vector3 StatePosition(int x, int y)
        {
            return new Vector3(x * 300, y * 100, 0);
        }
        protected static int GetLayerIndex(AnimatorController controller, AnimatorControllerLayer layer)
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
        protected static UnityEditor.Animations.AnimatorControllerLayer GetControllerLayer(UnityEditor.Animations.AnimatorController controller, string name)
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
        protected static AnimatorControllerParameter AddParameter(UnityEditor.Animations.AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
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

        public static bool SaveAsset(UnityEngine.Object asset, UnityEngine.Object rootAsset, string subDir = null, bool checkIfExists = false)
        {
            //Dir Path
            var dirPath = AssetDatabase.GetAssetPath(rootAsset);
            dirPath = dirPath.Replace(Path.GetFileName(dirPath), "");
            if (!string.IsNullOrEmpty(subDir))
                dirPath += $"{subDir}/";
            System.IO.Directory.CreateDirectory(dirPath);

            //Path
            var path = $"{dirPath}{asset.name}.asset";

            //Check if existing
            var existing = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if (checkIfExists && existing != null && existing != asset)
            {
                if (!EditorUtility.DisplayDialog("Replace Asset?", $"Another asset already exists at '{path}'.\nAre you sure you want to replace it?", "Replace", "Cancel"))
                    return false;
            }

            AssetDatabase.CreateAsset(asset, path);
            return true;
        }
    }
}
