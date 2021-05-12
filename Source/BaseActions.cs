#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;
using VRC.SDK3.Avatars.Components;
using System;
using System.Linq;

namespace VRCAvatarActions
{
    public abstract class BaseActions : ScriptableObject
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
        public enum VisemeEnum
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
            Custom = 0,
            GestureLeft = 1380,
            GestureRight = 5952,
            GestureLeftWeight = 4777,
            GestureRightWeight = 1712,
            Viseme = 9866,
            AFK = 4804,
            VRMode = 1291,
            TrackingType = 9993,
            MuteSelf = 1566,
            AngularY = 5775,
            VelocityX = 6015,
            VelocityY = 1208,
            VelocityZ = 7440,
            Upright = 4434,
            Grounded = 5912,
            Seated = 1173,
            InStation = 1247,
            IsLocal = 7648,
        }
        public enum ParameterType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            Trigger = 3
        }
        public enum AnimationLayer
        {
            Action,
            FX,
        }
        public enum OnOffEnum
        {
            On = 1,
            Off = 0
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
                public AnimationClip enter;
                public AnimationClip exit;
            }
            public Animations actionLayerAnimations = new Animations();
            public Animations fxLayerAnimations = new Animations();
            public float fadeIn = 0;
            public float hold = 0;
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
                    if (objectProperties.Count > 0)
                        return true;
                    if (parameterDrivers.Count > 0)
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

            //Object Properties
            public List<ObjectProperty> objectProperties = new List<ObjectProperty>();

            //Drive Parameters
            [System.Serializable]
            public class ParameterDriver
            {
                public enum Type
                {
                    RawValue = 4461,
                    MenuToggle = 3632,
                    MenuRandom = 9065,
                }

                public ParameterDriver() { }
                public ParameterDriver(ParameterDriver source)
                {
                    this.type = source.type;
                    this.name = source.name;
                    this.value = source.value;
                }

                public Type type = Type.RawValue;
                public string name;
                public float value = 1f;

                public VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType changeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
                public float valueMin = 0;
                public float valueMax = 1;
                public float chance = 0.5f;
                public bool isZeroValid = true;
            }
            public List<ParameterDriver> parameterDrivers = new List<ParameterDriver>();

            //Triggers
            [System.Serializable]
            public class Trigger
            {
                public Trigger()
                {
                }
                public Trigger(Trigger source)
                {
                    this.type = source.type;
                    this.foldout = source.foldout;
                    foreach (var item in source.conditions)
                        this.conditions.Add(new Condition(item));
                }
                public enum Type
                {
                    Enter = 0,
                    Exit = 1,
                }
                public Type type;
                public List<Condition> conditions = new List<Condition>();
                public bool foldout;
            }

            [System.Serializable]
            public class Condition
            {
                public Condition()
                {
                }
                public Condition(Condition source)
                {
                    this.type = source.type;
                    this.parameter = string.Copy(source.parameter);
                    this.logic = source.logic;
                    this.value = source.value;
                    this.shared = source.shared;
                }
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

                public BodyOverride(BodyOverride source)
                {
                    this.head = source.head;
                    this.leftHand = source.leftHand;
                    this.rightHand = source.rightHand;
                    this.hip = source.hip;
                    this.leftFoot = source.leftFoot;
                    this.rightFoot = source.rightFoot;
                    this.leftFingers = source.leftFingers;
                    this.rightFingers = source.rightFingers;
                    this.eyes = source.eyes;
                    this.mouth = source.mouth;
                }
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
            public void AddTransitions(UnityEditor.Animations.AnimatorController controller, UnityEditor.Animations.AnimatorState lastState, UnityEditor.Animations.AnimatorState state, float transitionTime, BaseActions.Action.Trigger.Type triggerType, MenuActions.MenuAction parentAction)
            {
                //Find valid triggers
                List<BaseActions.Action.Trigger> triggers = new List<Action.Trigger>();
                foreach (var trigger in this.triggers)
                {
                    if (trigger.type == triggerType)
                        triggers.Add(trigger);
                }

                bool controlEquals = (triggerType != Action.Trigger.Type.Exit);

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
                        this.AddCondition(transition, controlEquals);

                        //Conditions
                        AddTriggerConditions(controller, transition, trigger.conditions);

                        //Parent Conditions - Enter
                        if (triggerType == Action.Trigger.Type.Enter && parentAction != null)
                            parentAction.AddCondition(transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                }
                else
                {
                    if (triggerType == Trigger.Type.Enter)
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        this.AddCondition(transition, controlEquals);

                        //Parent Conditions
                        if (parentAction != null)
                            parentAction.AddCondition(transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                    else if (triggerType == Trigger.Type.Exit && this.HasExit())
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        this.AddCondition(transition, controlEquals);

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
                    parentAction.AddCondition(transition, controlEquals);
                }

                void Finalize(AnimatorStateTransition transition)
                {
                    if (transition.conditions.Length == 0)
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }
            }
            public virtual void AddCondition(AnimatorStateTransition transition, bool equals)
            {
                //Nothing
            }

            //Metadata
            public bool foldoutMain = false;
            public bool foldoutTriggers = false;
            public bool foldoutIkOverrides = false;
            public bool foldoutToggleObjects = false;
            public bool foldoutMaterialSwaps = false;
            public bool foldoutAnimations = false;
            public bool foldoutParameterDrivers = false;

            public UnityEngine.AnimationClip GetAnimationRaw(AnimationLayer layer, bool enter = true)
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
                    //Find/Generate
                    if (GeneratesLayer(layer))
                        return FindOrGenerate(this.name + "_Generated", group.enter);
                    else
                        return group.enter;
                }
                AnimationClip GetExit()
                {
                    //Fallback to enter
                    if (group.exit == null)
                        return GetEnter();

                    //Find/Generate
                    if (GeneratesLayer(layer))
                        return FindOrGenerate(this.name + "_Generated_Exit", group.exit);
                    else
                        return group.exit;
                }
            }
            protected AnimationClip FindOrGenerate(string clipName, AnimationClip parentClip)
            {
                //Find/Generate
                AnimationClip generated = null;
                if (GeneratedClips.TryGetValue(clipName, out generated))
                    return generated;
                else
                {
                    //Generate
                    generated = BuildGeneratedAnimation(clipName, parentClip);
                    if (generated != null)
                    {
                        GeneratedClips.Add(clipName, generated);
                        return generated;
                    }
                    else
                        return parentClip;
                }
            }
            protected AnimationClip BuildGeneratedAnimation(string clipName, AnimationClip source)
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

                //Properties
                foreach (var item in this.objectProperties)
                {
                    //Is anything defined?
                    if (string.IsNullOrEmpty(item.path))
                        continue;

                    //Find object
                    item.objRef = BaseActionsEditor.FindPropertyObject(AvatarDescriptor.gameObject, item.path);
                    if (item.objRef == null)
                        continue;

                    switch (item.type)
                    {
                        case ObjectProperty.Type.ObjectToggle: AddObjectToggle(animation, item, item.objRef); break;
                        case ObjectProperty.Type.MaterialSwap: AddMaterialSwap(animation, item, item.objRef); break;
                        case ObjectProperty.Type.BlendShape: (new ObjectProperty.BlendShape(item)).AddKeyframes(animation); break;
                        case ObjectProperty.Type.PlayAudio: (new ObjectProperty.PlayAudio(item)).AddKeyframes(animation); break;
                    }
                }

                //Save
                animation.name = clipName;
                SaveAsset(animation, ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                //Return
                return animation;
            }

            public virtual void CopyTo(Action clone)
            {
                //Generic
                clone.name = string.Copy(this.name);
                clone.enabled = this.enabled;

                //Animation
                clone.actionLayerAnimations.enter = this.actionLayerAnimations.enter;
                clone.actionLayerAnimations.exit = this.actionLayerAnimations.exit;
                clone.fxLayerAnimations.enter = this.fxLayerAnimations.enter;
                clone.fxLayerAnimations.exit = this.fxLayerAnimations.exit;
                clone.fadeIn = this.fadeIn;
                clone.hold = this.hold;
                clone.fadeOut = this.fadeOut;

                //Object Properties
                clone.objectProperties.Clear();
                foreach (var prop in this.objectProperties)
                    clone.objectProperties.Add(new ObjectProperty(prop));

                //Parameter drivers
                clone.parameterDrivers.Clear();
                foreach (var driver in this.parameterDrivers)
                    clone.parameterDrivers.Add(new ParameterDriver(driver));

                //Triggers
                clone.triggers.Clear();
                foreach (var trigger in this.triggers)
                    clone.triggers.Add(new Action.Trigger(trigger));

                //Body overrides
                clone.bodyOverride = new BodyOverride(this.bodyOverride);

                //Meta
                clone.foldoutMain = this.foldoutMain;
                clone.foldoutTriggers = this.foldoutTriggers;
                clone.foldoutIkOverrides = this.foldoutIkOverrides;
                clone.foldoutToggleObjects = this.foldoutToggleObjects;
                clone.foldoutMaterialSwaps = this.foldoutMaterialSwaps;
                clone.foldoutAnimations = this.foldoutAnimations;
            }
        }

        static void AddObjectToggle(AnimationClip animation, ObjectProperty property, GameObject obj)
        {
            //Create curve
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1f));
            animation.SetCurve(property.path, typeof(GameObject), "m_IsActive", curve);

            //Disable the object
            obj.SetActive(false);
        }
        static void AddMaterialSwap(AnimationClip animation, ObjectProperty property, GameObject obj)
        {
            //For each material
            for (int i = 0; i < property.objects.Length; i++)
            {
                var material = property.objects[i];
                if (material == null)
                    continue;

                //Create curve
                var keyframes = new ObjectReferenceKeyframe[1];
                var keyframe = new ObjectReferenceKeyframe();
                keyframe.time = 0;
                keyframe.value = material;
                keyframes[0] = keyframe;
                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(property.path, typeof(Renderer), $"m_Materials.Array.data[{i}]");
                AnimationUtility.SetObjectReferenceCurve(animation, binding, keyframes);
            }
        }

        public abstract void GetActions(List<Action> output);
        public abstract Action AddAction();
        public abstract void RemoveAction(Action action);
        public abstract void InsertAction(int index, Action action);

        public virtual bool CanUseLayer(BaseActions.AnimationLayer layer)
        {
            return true;
        }
        public virtual bool ActionsHaveExit()
        {
            return true;
        }

        protected static AvatarDescriptor AvatarDescriptor = null;
        protected static AvatarActions ActionsDescriptor = null;
        protected static List<ExpressionParameters.Parameter> AllParameters = new List<ExpressionParameters.Parameter>();
        protected static AnimatorController ActionController;
        protected static AnimatorController FxController;
        protected static AnimatorController GetController(AnimationLayer layer)
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
        protected static Dictionary<string, AnimationClip> GeneratedClips = new Dictionary<string, AnimationClip>();
        public static Dictionary<string, List<MenuActions.MenuAction>> ParameterToMenuActions = new Dictionary<string, List<MenuActions.MenuAction>>();

        public static void BuildAvatarData(AvatarDescriptor desc, AvatarActions actionsDesc)
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
            ActionController = GetController(AvatarDescriptor.AnimLayerType.Action, "AnimationController_Action");
            FxController = GetController(AvatarDescriptor.AnimLayerType.FX, "AnimationController_FX");

            AnimatorController GetController(AvatarDescriptor.AnimLayerType animLayerType, string name)
            {
                //Find desc layer
                bool foundLayer = false;
                AvatarDescriptor.CustomAnimLayer descLayer = new AvatarDescriptor.CustomAnimLayer();
                int descLayerIndex = 0;
                foreach(var layer in AvatarDescriptor.baseAnimationLayers)
                {
                    if (layer.type == animLayerType)
                    {
                        descLayer = layer;
                        foundLayer = true;
                        break;
                    }
                    descLayerIndex++;
                }
                if(!foundLayer)
                {
                    EditorUtility.DisplayDialog("Build Error", $"Unable to find {animLayerType} layer on VRCAvatarDescriptor.  Please use the 'Reset To Default' button on the avatar descriptor under Playable Layers.", "Okay");
                    BuildFailed = true;
                    return null;
                }

                //Find/Create Layer
                var controller = descLayer.animatorController as UnityEditor.Animations.AnimatorController;
                if (controller == null || descLayer.isDefault)
                {
                    //Dir Path
                    var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor.ReturnAnyScriptableObject());
                    dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                    System.IO.Directory.CreateDirectory(dirPath);

                    //Create
                    var path = $"{dirPath}{name}.controller";
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

                    //Add base layer
                    controller.AddLayer("Base Layer");

                    //Save
                    descLayer.animatorController = controller;
                    descLayer.isDefault = false;
                    AvatarDescriptor.baseAnimationLayers[descLayerIndex] = descLayer;
                    EditorUtility.SetDirty(AvatarDescriptor);
                }

                //Cleanup Layers
                {
                    //Clean layers
                    for(int i=0; i< controller.layers.Length; i++)
                    {
                        if(controller.layers[i].name == "Base Layer")
                            continue;
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
            GeneratedClips.Clear();
            /*{
                var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor.ReturnAnyScriptableObject());
                dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                var files = System.IO.Directory.GetFiles(dirPath);
                foreach (var file in files)
                {
                    if (file.Contains("_Generated"))
                        System.IO.File.Delete(file);
                }
            }*/

            //Parameters
            InitExpressionParameters();
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

            //Save
            EditorUtility.SetDirty(AvatarDescriptor);
            EditorUtility.SetDirty(AvatarDescriptor.expressionsMenu);

            //Save Parameters
            {
                AvatarDescriptor.expressionParameters.parameters = BuildParameters.ToArray();

                //Parameter defaults
                foreach (var paramDefault in ActionsDescriptor.parameterDefaults)
                {
                    var param = AvatarDescriptor.expressionParameters.FindParameter(paramDefault.name);
                    if (param != null)
                        param.defaultValue = paramDefault.value;
                }

                //Check parameter count
                var parametersObject = AvatarDescriptor.expressionParameters;
                if (parametersObject.CalcTotalCost() > ExpressionParameters.MAX_PARAMETER_COST)
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCExpressionParameters. Too many parameters defined.", "Okay");
                    return;
                }

                EditorUtility.SetDirty(AvatarDescriptor.expressionParameters);
            }

            //Save prefab
            AssetDatabase.SaveAssets();
        }

        //Parameters
        static protected List<ExpressionParameters.Parameter> BuildParameters = new List<ExpressionParameters.Parameter>();
        static void InitExpressionParameters()
        {
            //Check if parameter object exists
            var parametersObject = AvatarDescriptor.expressionParameters;
            if (AvatarDescriptor.expressionParameters == null || !AvatarDescriptor.customExpressions)
            {
                parametersObject = ScriptableObject.CreateInstance<ExpressionParameters>();
                parametersObject.name = "ExpressionParameters";
                SaveAsset(parametersObject, ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                AvatarDescriptor.customExpressions = true;
                AvatarDescriptor.expressionParameters = parametersObject;
            }

            //Clear parameters
            BuildParameters.Clear();
            if(parametersObject.parameters != null)
            {
                foreach (var param in parametersObject.parameters)
                {
                    if (param != null && ActionsDescriptor.ignoreParameters.Contains(param.name))
                        BuildParameters.Add(param);
                }
            }
        }
        protected static void DefineExpressionParameter(ExpressionParameters.Parameter parameter)
        {
            //Check if already exists
            foreach(var param in BuildParameters)
            {
                if (param.name == parameter.name)
                    return;
            }

            //Add
            BuildParameters.Add(parameter);
        }
        protected static ExpressionParameters.Parameter FindExpressionParameter(string name)
        {
            foreach (var param in BuildParameters)
            {
                if (param.name == name)
                    return param;
            }
            return null;
        }

        //Normal
        protected static void BuildActionLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<Action> actions, string layerName, MenuActions.MenuAction parentAction, bool turnOffState = true)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(7, 0);

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
            if (turnOffState)
            {
                //Animation Layer Weight
                var layerWeight = waitingState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action;
            }
            else
                waitingState.writeDefaultValues = false;

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
                    state.motion = action.GetAnimation(AnimationLayer.Action, true);

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = action.fadeIn;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Store
                    lastState = state;
                }

                //Hold
                if (action.hold > 0)
                {
                    //Hold
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(3, actionIter));
                        state.motion = action.GetAnimation(AnimationLayer.Action, true);

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = true;
                        transition.exitTime = action.hold;
                        transition.duration = 0;

                        //Store
                        lastState = state;
                    }
                }

                //Disable state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(4, actionIter));
                    state.motion = action.GetAnimation(AnimationLayer.Action, false);

                    //Transition
                    action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                    playable.goalWeight = 0.0f;
                    playable.blendDuration = action.fadeOut;

                    //Store
                    lastState = state;
                }

                //Fadeout state
                if(action.fadeOut > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(5, actionIter));
                    state.motion = action.GetAnimation(AnimationLayer.Action, false);

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
                if(action.bodyOverride.HasAny())
                {
                    var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(6, actionIter));

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Tracking
                    SetupTracking(action, state, TrackingType.Tracking);

                    //Store
                    lastState = state;
                }

                //Exit transition
                {
                    var transition = lastState.AddExitTransition();
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }

                //Iterate
                actionIter += 1;
            }
        }
        protected static void BuildNormalLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<Action> actions, string layerName, AnimationLayer layerType, MenuActions.MenuAction parentAction, bool turnOffState = true)
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
            var layerIndex = GetLayerIndex(controller, layer);

            //Waiting
            AnimatorState waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            if (turnOffState)
            {
                //Animation Layer Weight
                var layerWeight = waitingState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
            }
            else
                waitingState.writeDefaultValues = false;

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

                    //Parameter Drivers
                    BuildParameterDrivers(action, state);

                    //Store
                    lastState = state;
                }

                //Hold
                if (action.hold > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(2, actionIter));
                    state.motion = action.GetAnimation(layerType, true);

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = true;
                    transition.exitTime = action.hold;
                    transition.duration = 0;

                    //Store
                    lastState = state;
                }

                //Exit
                if (action.HasExit() || parentAction != null)
                {
                    //Disable
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(3, actionIter));
                        state.motion = action.GetAnimation(layerType, false);

                        //Transition
                        action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                        //Store
                        lastState = state;
                    }

                    //Fadeout
                    if(action.fadeOut > 0)
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(4, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = action.fadeOut;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Store
                        lastState = state;
                    }

                    //Cleanup
                    if (action.bodyOverride.HasAny())
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(5, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Tracking
                        SetupTracking(action, state, TrackingType.Tracking);

                        //Store
                        lastState = state;
                    }

                    //Transition Exit
                    {
                        var transition = lastState.AddExitTransition();
                        transition.hasExitTime = false;
                        transition.exitTime = 0f;
                        transition.duration = 0f;
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
            var layerActions = new List<BaseActions.Action>();
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
        protected static void AddTriggerConditions(UnityEditor.Animations.AnimatorController controller, AnimatorStateTransition transition, IEnumerable<BaseActions.Action.Condition> conditions)
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
                    case ParameterEnum.Viseme:
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
                        //Find the parameter
                        bool found = false;
                        foreach(var param in controller.parameters)
                        {
                            if(param.name == condition.parameter)
                            {
                                paramType = param.type;
                                found = true;
                                break;
                            }
                        }

                        if(!found)
                        {
                            Debug.LogError($"AddTriggerConditions, unable to find parameter named:{condition.parameter}");
                            BuildFailed = true;
                            return;
                        }
                        break;
                    }
                    default:
                    {
                        Debug.LogError("AddTriggerConditions, unknown parameter type for trigger condition.");
                        BuildFailed = true;
                        return;
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
        protected static void BuildParameterDrivers(BaseActions.Action action, AnimatorState state)
        {
            if (action.parameterDrivers.Count == 0)
                return;

            var driverBehaviour = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driverBehaviour.localOnly = true;
            foreach(var driver in action.parameterDrivers)
            {
                if (string.IsNullOrEmpty(driver.name))
                    continue;

                //Build param
                var param = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
                if (driver.type == Action.ParameterDriver.Type.RawValue)
                {
                    param.name = driver.name;
                    param.value = driver.value;
                    param.type = driver.changeType;
                    param.valueMin = driver.valueMin;
                    param.valueMax = driver.valueMax;
                    param.chance = driver.chance;
                }
                else if(driver.type == Action.ParameterDriver.Type.MenuToggle)
                {
                    //Search for menu action
                    var drivenAction = ActionsDescriptor.menuActions.FindMenuAction(driver.name);
                    if(drivenAction == null || drivenAction.menuType != MenuActions.MenuAction.MenuType.Toggle)
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find menu toggle named '{driver.name}' for a parameter driver.  Build Failed.", "Okay");
                        return;
                    }
                    param.name = drivenAction.parameter;
                    param.value = driver.value == 0 ? 0 : drivenAction.controlValue;
                }
                else if(driver.type == Action.ParameterDriver.Type.MenuRandom)
                {
                    //Find max values    
                    List<MenuActions.MenuAction> list;
                    if(ParameterToMenuActions.TryGetValue(driver.name, out list))
                    {
                        param.name = driver.name;
                        param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random;
                        param.value = 0;
                        param.valueMin = driver.isZeroValid ? 1 : 0;
                        param.valueMax = list.Count;
                        param.chance = 0.5f;
                    }
                    else
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find any menu actions driven by parameter '{driver.name} for a parameter driver'.  Build Failed.", "Okay");
                        return;
                    }
                }
                driverBehaviour.parameters.Add(param);
            }
        }

        //Helpers
        protected static void SetupTracking(BaseActions.Action action, AnimatorState state, TrackingType trackingType)
        {
            if (!action.bodyOverride.HasAny())
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
            if(string.IsNullOrEmpty(dirPath))
            {
                BuildFailed = true;
                EditorUtility.DisplayDialog("Build Error", "Unable to save asset, unknown asset path.", "Okay");
                return false;
            }
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

    [CustomEditor(typeof(BaseActions))]
    public class BaseActionsEditor : EditorBase
    {
        protected BaseActions script;
        protected BaseActions.Action selectedAction;

        public void OnEnable()
        {
            var editor = target as BaseActions;
        }
        public override void Inspector_Body()
        {
            script = target as BaseActions;

            //Controls
            EditorGUILayout.BeginHorizontal();
            {
                //Add
                if (GUILayout.Button("Add"))
                {
                    var action = script.AddAction();
                    action.name = "New Action";
                }

                //Selected
                EditorGUI.BeginDisabledGroup(selectedAction == null);
                {
                    //Move Up
                    if (GUILayout.Button("Move Up"))
                    {
                        var temp = new List<BaseActions.Action>();
                        script.GetActions(temp);

                        var index = temp.IndexOf(selectedAction);
                        script.RemoveAction(selectedAction);
                        script.InsertAction(Mathf.Max(0, index - 1), selectedAction);
                    }

                    //Move Down
                    if (GUILayout.Button("Move Down"))
                    {
                        var temp = new List<BaseActions.Action>();
                        script.GetActions(temp);

                        var index = temp.IndexOf(selectedAction);
                        script.RemoveAction(selectedAction);
                        script.InsertAction(Mathf.Min(temp.Count - 1, index + 1), selectedAction);
                    }

                    //Move Down
                    if (GUILayout.Button("Duplicate"))
                    {
                        var action = script.AddAction();
                        selectedAction.CopyTo(action);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            //Draw actions
            var actions = new List<BaseActions.Action>();
            script.GetActions(actions);
            DrawActions(actions);

            Divider();

            //Action Info
            if (selectedAction != null)
            {
                EditorGUI.BeginDisabledGroup(!selectedAction.enabled);
                Inspector_Action_Header(selectedAction);
                Inspector_Action_Body(selectedAction);
                EditorGUI.EndDisabledGroup();
            }
        }
        public virtual void Inspector_Action_Header(BaseActions.Action action)
        {
            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);
        }
        public virtual void Inspector_Action_Body(BaseActions.Action action, bool showParam = true)
        {
            //Transitions
            action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
            if(script.ActionsHaveExit())
            {
                action.hold = EditorGUILayout.FloatField("Hold", action.hold);
                action.fadeOut = EditorGUILayout.FloatField("Fade Out", action.fadeOut);
            }

            //Properties
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUI.indentLevel += 1;
                {
                    action.foldoutToggleObjects = EditorGUILayout.Foldout(action.foldoutToggleObjects, Title("Object Properties", action.objectProperties.Count > 0));
                    if (action.foldoutToggleObjects)
                    {
                        /*var clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", null, typeof(AnimationClip), false);
                        if(clip != null)
                        {
                            var bindings = AnimationUtility.GetCurveBindings(clip);
                            Debug.Log("Bindings");
                            foreach(var binding in bindings)
                            {
                                Debug.Log("Name:" + binding.propertyName);
                                Debug.Log("Path:" + binding.path);
                            }
                        }*/

                        //Add
                        EditorGUILayout.BeginHorizontal();
                        {
                            //Add
                            if (GUILayout.Button("Add"))
                                action.objectProperties.Add(new ObjectProperty());
                        }
                        EditorGUILayout.EndHorizontal();

                        //Properties
                        for (int i = 0; i < action.objectProperties.Count; i++)
                        {
                            var property = action.objectProperties[i];
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            {
                                //Header
                                EditorGUILayout.BeginHorizontal();
                                {
                                    //Object Ref
                                    if(ObjectPropertyReferece(property))
                                    {
                                        //Clear prop data
                                        property.objects = null;
                                        property.values = null;
                                    }

                                    //Type
                                    property.type = (ObjectProperty.Type)EditorGUILayout.EnumPopup(property.type);

                                    //Delete
                                    if (GUILayout.Button("X", GUILayout.Width(32)))
                                    {
                                        action.objectProperties.RemoveAt(i);
                                        i--;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //Body
                                if (property.objRef != null)
                                {
                                    switch (property.type)
                                    {
                                        case ObjectProperty.Type.MaterialSwap: MaterialSwapProperty(property); break;
                                        case ObjectProperty.Type.BlendShape: BlendShapeProperty(new ObjectProperty.BlendShape(property)); break;
                                        case ObjectProperty.Type.PlayAudio: PlayAudioProperty(new ObjectProperty.PlayAudio(property)); break;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }


                    }
                }
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.EndVertical();
            }

            bool ObjectPropertyReferece(ObjectProperty property)
            {
                //Object Ref
                EditorGUI.BeginChangeCheck();
                if (property.objRef == null)
                    property.objRef = FindPropertyObject(avatarDescriptor.gameObject, property.path);
                property.objRef = (GameObject)EditorGUILayout.ObjectField("", property.objRef, typeof(GameObject), true, null);
                if (EditorGUI.EndChangeCheck())
                {
                    if (property.objRef != null)
                    {
                        //Get path
                        property.path = FindPropertyPath(property.objRef);
                        if (property.path == null)
                        {
                            property.Clear();
                            EditorUtility.DisplayDialog("Error", "Unable to determine the object's path", "Okay");
                        }
                        else
                            return true;
                    }
                    else
                        property.Clear();
                }
                return false;
            }
            void MaterialSwapProperty(ObjectProperty property)
            {
                //Get object materials
                var mesh = property.objRef.GetComponent<Renderer>();
                if (mesh == null)
                {
                    EditorGUILayout.HelpBox("GameObject doesn't have a Renderer component.", MessageType.Error);
                    return;
                }

                //Materials
                var materials = mesh.sharedMaterials;
                if (materials != null)
                {
                    //Create/Resize
                    if (property.objects == null || property.objects.Length != materials.Length)
                        property.objects = new UnityEngine.Object[materials.Length];

                    //Materials
                    for (int materialIter = 0; materialIter < materials.Length; materialIter++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Material", GUILayout.MaxWidth(100));
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(materials[materialIter], typeof(Material), false);
                            EditorGUI.EndDisabledGroup();
                            property.objects[materialIter] = EditorGUILayout.ObjectField(property.objects[materialIter], typeof(Material), false) as Material;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            void BlendShapeProperty(ObjectProperty.BlendShape property)
            {
                //Get mesh
                Mesh mesh = null;
                var skinnedRenderer = property.objRef.GetComponent<SkinnedMeshRenderer>();
                if (skinnedRenderer == null)
                {
                    EditorGUILayout.HelpBox("GameObject doesn't have a MeshFilter or SkinnedMeshRenderer component.", MessageType.Error);
                    return;
                }
                mesh = skinnedRenderer.sharedMesh;

                //Setup data
                property.Setup();

                var popup = new string[mesh.blendShapeCount];
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    popup[i] = mesh.GetBlendShapeName(i);

                //Editor
                EditorGUILayout.BeginHorizontal();
                {
                    //Property
                    property.index = EditorGUILayout.Popup(property.index, popup);

                    //Value
                    EditorGUI.BeginChangeCheck();
                    property.weight = EditorGUILayout.Slider(property.weight, 0f, 100f);
                    if(EditorGUI.EndChangeCheck())
                    {
                        //I'd like to preview the change, but preserving the value
                        //TODO
                        //skinnedRenderer.SetBlendShapeWeight((int)property.values[0], property.values[1]);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            void PlayAudioProperty(ObjectProperty.PlayAudio property)
            {
                //Setup
                property.Setup();

                //Editor
                //EditorGUILayout.BeginHorizontal();
                {
                    //Property
                    EditorGUILayout.BeginHorizontal();
                    {
                        property.audioClip = (AudioClip)EditorGUILayout.ObjectField("Clip", property.audioClip, typeof(AudioClip), false);
                        EditorGUILayout.TextField(property.audioClip != null ? $"{property.audioClip.length:N2}" : "", GUILayout.Width(64));
                    }
                    EditorGUILayout.EndHorizontal();
                    property.volume = EditorGUILayout.Slider("Volume", property.volume, 0f, 1f);
                    property.spatial = EditorGUILayout.Toggle("Spatial", property.spatial);
                    EditorGUI.BeginDisabledGroup(!property.spatial);
                    {
                        property.near = EditorGUILayout.FloatField("Near", property.near);
                        property.far = EditorGUILayout.FloatField("Far", property.far);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                //EditorGUILayout.EndHorizontal();
            }

            //Animations
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                action.foldoutAnimations = EditorGUILayout.Foldout(action.foldoutAnimations, Title("Animations", action.HasAnimations()));
                if (action.foldoutAnimations)
                {
                    //Action layer
                    if (script.CanUseLayer(BaseActions.AnimationLayer.Action))
                    {
                        EditorGUILayout.LabelField("Action Layer");
                        EditorGUI.indentLevel += 1;
                        action.actionLayerAnimations.enter = DrawAnimationReference("Enter", action.actionLayerAnimations.enter, $"{action.name}_Action_Enter");
                        if (script.ActionsHaveExit())
                            action.actionLayerAnimations.exit = DrawAnimationReference("Exit", action.actionLayerAnimations.exit, $"{action.name}_Action_Exit");
                        EditorGUILayout.HelpBox("Use for transfoming the humanoid skeleton.  You will need to use IK Overrides to disable IK control of body parts.", MessageType.Info);
                        EditorGUI.indentLevel -= 1;
                    }

                    //FX Layer
                    if(script.CanUseLayer(BaseActions.AnimationLayer.FX))
                    {
                        EditorGUILayout.LabelField("FX Layer");
                        EditorGUI.indentLevel += 1;
                        action.fxLayerAnimations.enter = DrawAnimationReference("Enter", action.fxLayerAnimations.enter, $"{action.name}_FX_Enter");
                        if (script.ActionsHaveExit())
                            action.fxLayerAnimations.exit = DrawAnimationReference("Exit", action.fxLayerAnimations.exit, $"{action.name}_FX_Exit");
                        EditorGUILayout.HelpBox("Use for most everything else, including bones not part of the humanoid skeleton.", MessageType.Info);
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Parameter Drivers
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                action.foldoutParameterDrivers = EditorGUILayout.Foldout(action.foldoutParameterDrivers, Title("Parameter Drivers", action.parameterDrivers.Count > 0));
                if (action.foldoutParameterDrivers)
                {
                    //Add
                    if (GUILayout.Button("Add"))
                    {
                        action.parameterDrivers.Add(new BaseActions.Action.ParameterDriver());
                    }

                    //Drivers
                    for (int i = 0; i < action.parameterDrivers.Count; i++)
                    {
                        var parameter = action.parameterDrivers[i];
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.BeginHorizontal();
                        {
                            parameter.type = (BaseActions.Action.ParameterDriver.Type)EditorGUILayout.EnumPopup("Type", parameter.type);
                            if(GUILayout.Button("X", GUILayout.Width(32)))
                            {
                                action.parameterDrivers.RemoveAt(i);
                                i--;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        if (parameter.type == BaseActions.Action.ParameterDriver.Type.RawValue)
                        {
                            parameter.name = DrawParameterDropDown(parameter.name, "Parameter");
                            EditorGUILayout.BeginHorizontal();
                            {
                                //EditorGUILayout.LabelField("Value");
                                parameter.changeType = (VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType)EditorGUILayout.EnumPopup("Value", parameter.changeType);
                                if (parameter.changeType == VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random)
                                {
                                    parameter.valueMin = EditorGUILayout.FloatField(parameter.valueMin, GUILayout.MaxWidth(98));
                                    parameter.valueMax = EditorGUILayout.FloatField(parameter.valueMax, GUILayout.MaxWidth(98));
                                }
                                else
                                    parameter.value = EditorGUILayout.FloatField(parameter.value, GUILayout.MaxWidth(200));
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else if(parameter.type == BaseActions.Action.ParameterDriver.Type.MenuToggle)
                        {
                            parameter.name = EditorGUILayout.TextField("Menu Action", parameter.name);
                            var value = parameter.value == 0 ? BaseActions.OnOffEnum.Off : BaseActions.OnOffEnum.On;
                            parameter.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup("Value", value));
                        }
                        else if(parameter.type == BaseActions.Action.ParameterDriver.Type.MenuRandom)
                        {
                            parameter.name = DrawParameterDropDown(parameter.name, "Parameter");
                            parameter.isZeroValid = EditorGUILayout.Toggle("Is Zero Valid", parameter.isZeroValid);
                        }
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.HelpBox("Raw Value - Set a parameter to a specific value.", MessageType.Info);
                    EditorGUILayout.HelpBox("Menu Toggle - Toggles a menu action by name.", MessageType.Info);
                    EditorGUILayout.HelpBox("Menu Random - Enables a random menu action driven by this parameter.", MessageType.Info);
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Body Overrides
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            action.foldoutIkOverrides = EditorGUILayout.Foldout(action.foldoutIkOverrides, Title("IK Overrides", action.bodyOverride.HasAny()));
            if (action.foldoutIkOverrides)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle On"))
                {
                    action.bodyOverride.SetAll(true);
                }
                if (GUILayout.Button("Toggle Off"))
                {
                    action.bodyOverride.SetAll(false);
                }
                EditorGUILayout.EndHorizontal();

                action.bodyOverride.head = EditorGUILayout.Toggle("Head", action.bodyOverride.head);
                action.bodyOverride.leftHand = EditorGUILayout.Toggle("Left Hand", action.bodyOverride.leftHand);
                action.bodyOverride.rightHand = EditorGUILayout.Toggle("Right Hand", action.bodyOverride.rightHand);
                action.bodyOverride.hip = EditorGUILayout.Toggle("Hips", action.bodyOverride.hip);
                action.bodyOverride.leftFoot = EditorGUILayout.Toggle("Left Foot", action.bodyOverride.leftFoot);
                action.bodyOverride.rightFoot = EditorGUILayout.Toggle("Right Foot", action.bodyOverride.rightFoot);
                action.bodyOverride.leftFingers = EditorGUILayout.Toggle("Left Fingers", action.bodyOverride.leftFingers);
                action.bodyOverride.rightFingers = EditorGUILayout.Toggle("Right Fingers", action.bodyOverride.rightFingers);
                action.bodyOverride.eyes = EditorGUILayout.Toggle("Eyes", action.bodyOverride.eyes);
                action.bodyOverride.mouth = EditorGUILayout.Toggle("Mouth", action.bodyOverride.mouth);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel -= 1;

            //Triggers
            DrawInspector_Triggers(action);
        }

        public void DrawInspector_Triggers(BaseActions.Action action)
        {
            //Enter Triggers
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            action.foldoutTriggers = EditorGUILayout.Foldout(action.foldoutTriggers, Title("Triggers", action.triggers.Count > 0));
            if (action.foldoutTriggers)
            {
                //Header
                if (GUILayout.Button("Add Trigger"))
                    action.triggers.Add(new BaseActions.Action.Trigger());

                //Triggers
                for (int triggerIter = 0; triggerIter < action.triggers.Count; triggerIter++)
                {
                    //Foldout
                    var trigger = action.triggers[triggerIter];
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        trigger.foldout = EditorGUILayout.Foldout(trigger.foldout, "Trigger");
                        if(GUILayout.Button("Up", GUILayout.Width(48)))
                        {
                            if(triggerIter > 0)
                            {
                                action.triggers.RemoveAt(triggerIter);
                                action.triggers.Insert(triggerIter - 1, trigger);
                            }
                        }
                        if (GUILayout.Button("Down", GUILayout.Width(48)))
                        {
                            if (triggerIter < action.triggers.Count-1)
                            {
                                action.triggers.RemoveAt(triggerIter);
                                action.triggers.Insert(triggerIter + 1, trigger);
                            }
                        }
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            action.triggers.RemoveAt(triggerIter);
                            triggerIter -= 1;
                            continue;
                        }
                        EditorGUILayout.EndHorizontal();
                        if (trigger.foldout)
                        {
                            //Type
                            trigger.type = (BaseActions.Action.Trigger.Type)EditorGUILayout.EnumPopup("Type", trigger.type);

                            //Conditions
                            if (GUILayout.Button("Add Condition"))
                                trigger.conditions.Add(new BaseActions.Action.Condition());

                            //Each Conditions
                            for (int conditionIter = 0; conditionIter < trigger.conditions.Count; conditionIter++)
                            {
                                var condition = trigger.conditions[conditionIter];
                                if (!DrawInspector_Condition(condition))
                                {
                                    trigger.conditions.RemoveAt(conditionIter);
                                    conditionIter -= 1;
                                }
                            }

                            if (trigger.conditions.Count == 0)
                            {
                                EditorGUILayout.HelpBox("Triggers without any conditions default to true.", MessageType.Warning);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                } //End loop
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
        public bool DrawInspector_Condition(BaseActions.Action.Condition trigger)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                //Type
                trigger.type = (BaseActions.ParameterEnum)EditorGUILayout.EnumPopup(trigger.type);

                //Parameter
                if (trigger.type == BaseActions.ParameterEnum.Custom)
                    trigger.parameter = EditorGUILayout.TextField(trigger.parameter);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(trigger.GetParameter());
                    EditorGUI.EndDisabledGroup();
                }

                //Logic
                switch (trigger.type)
                {
                    case BaseActions.ParameterEnum.Custom:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup(trigger.logic);
                        break;
                    case BaseActions.ParameterEnum.Upright:
                    case BaseActions.ParameterEnum.AngularY:
                    case BaseActions.ParameterEnum.VelocityX:
                    case BaseActions.ParameterEnum.VelocityY:
                    case BaseActions.ParameterEnum.VelocityZ:
                    case BaseActions.ParameterEnum.GestureRightWeight:
                    case BaseActions.ParameterEnum.GestureLeftWeight:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((BaseActions.Action.Condition.LogicCompare)trigger.logic);
                        break;
                    default:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((BaseActions.Action.Condition.LogicEquals)trigger.logic);
                        break;
                }

                //Value
                switch (trigger.type)
                {
                    case BaseActions.ParameterEnum.Custom:
                    case BaseActions.ParameterEnum.Upright:
                    case BaseActions.ParameterEnum.AngularY:
                    case BaseActions.ParameterEnum.VelocityX:
                    case BaseActions.ParameterEnum.VelocityY:
                    case BaseActions.ParameterEnum.VelocityZ:
                    case BaseActions.ParameterEnum.GestureRightWeight:
                    case BaseActions.ParameterEnum.GestureLeftWeight:
                        trigger.value = EditorGUILayout.FloatField(trigger.value);
                        break;
                    case BaseActions.ParameterEnum.GestureLeft:
                    case BaseActions.ParameterEnum.GestureRight:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.GestureEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.Viseme:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.VisemeEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.TrackingType:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.TrackingTypeEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.AFK:
                    case BaseActions.ParameterEnum.MuteSelf:
                    case BaseActions.ParameterEnum.InStation:
                    case BaseActions.ParameterEnum.IsLocal:
                    case BaseActions.ParameterEnum.Grounded:
                    case BaseActions.ParameterEnum.Seated:
                        EditorGUI.BeginDisabledGroup(true);
                        trigger.value = 1;
                        EditorGUILayout.TextField("True");
                        EditorGUI.EndDisabledGroup();
                        break;
                    case BaseActions.ParameterEnum.VRMode:
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.IntField(1);
                        EditorGUI.EndDisabledGroup();
                        break;
                }

                if (GUILayout.Button("X", GUILayout.Width(32)))
                {
                    return false;
                }
            }
            EditorGUILayout.EndHorizontal();
            return true;
        }

        void DrawActions(List<BaseActions.Action> actions)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                bool isSelected = selectedAction == action;

                //Draw header
                var headerRect = EditorGUILayout.BeginHorizontal(isSelected ? boxSelected : boxUnselected);
                {
                    EditorGUILayout.LabelField(action.name);
                    GUILayout.FlexibleSpace();
                    action.enabled = EditorGUILayout.Toggle(action.enabled, GUILayout.Width(32));

                    if (GUILayout.Button("X", GUILayout.Width(32)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Action?", $"Delete the action '{action.name}'?", "Delete", "Cancel"))
                        {
                            script.RemoveAction(action);
                            if (isSelected)
                                SelectAction(null);
                            i -= 1;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                //Selection
                if (Event.current.type == EventType.MouseDown)
                {
                    if (headerRect.Contains(Event.current.mousePosition))
                    {
                        SelectAction(action);
                        Event.current.Use();
                    }
                }
            }
        }
        void SelectAction(BaseActions.Action action)
        {
            if (selectedAction != action)
            {
                selectedAction = action;
                Repaint();
                GUI.FocusControl(null);
            }
        }

        #region HelperMethods
        public static string Title(string name, bool isModified)
        {
            return name + (isModified ? "*" : "");
        }
        protected UnityEngine.AnimationClip DrawAnimationReference(string name, UnityEngine.AnimationClip clip, string newAssetName)
        {
            EditorGUILayout.BeginHorizontal();
            {
                clip = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField(name, clip, typeof(UnityEngine.AnimationClip), false);
                EditorGUI.BeginDisabledGroup(clip != null);
                {
                    if (GUILayout.Button("New", GUILayout.Width(SmallButtonSize)))
                    {
                        //Create animation    
                        clip = new AnimationClip();
                        clip.name = newAssetName;
                        BaseActions.SaveAsset(clip, this.script as BaseActions, "", true);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(clip == null);
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(SmallButtonSize)))
                    {
                        EditAnimation(clip);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            //Return
            return clip;
        }
        void EditAnimation(UnityEngine.AnimationClip clip)
        {
            //Add clip source
            var clipSource = avatarDescriptor.gameObject.GetComponent<ClipSource>();
            if (clipSource == null)
                clipSource = avatarDescriptor.gameObject.AddComponent<ClipSource>();

            clipSource.clips.Clear();
            clipSource.clips.Add(clip);

            //Select the root object
            Selection.activeObject = avatarDescriptor.gameObject;

            //Open the animation window
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }
        public static GameObject FindPropertyObject(GameObject root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            return root.transform.Find(path)?.gameObject;
        }
        string FindPropertyPath(GameObject obj)
        {
            string path = obj.name;
            while (true)
            {
                obj = obj.transform.parent?.gameObject;
                if (obj == null)
                    return null;
                if (obj.GetComponent<VRCAvatarDescriptor>() != null) //Stop at the avatar descriptor
                    break;
                path = $"{obj.name}/{path}";
            }
            return path;
        }
        #endregion
    }
}
#endif