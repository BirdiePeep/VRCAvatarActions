#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace VRCAvatarActions
{
	[CreateAssetMenu(fileName = "Simple Gestures", menuName = "VRCAvatarActions/Other Actions/Simple Gestures")]
	public class SimpleGestureActions : NonMenuActions
	{
		public GestureAction defaultAction;

        [System.Serializable]
        public class GestureAction : Action
        {
            //Gesture
            [System.Serializable]
            public struct GestureTable
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
                    switch (type)
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

                public bool IsModified()
                {
                    return neutral || fist || openHand || fingerPoint || victory || rockNRoll || handGun || thumbsUp;
                }
            }
            public GestureTable gestureTable = new GestureTable();
        }
        public List<GestureAction> actions = new List<GestureAction>();

        public override void GetActions(List<Action> output)
        {
            foreach (var action in actions)
                output.Add(action);
        }
        public override Action AddAction()
        {
            var action = new GestureAction();
            actions.Add(action);
            return action;
        }
        public override void RemoveAction(Action action)
        {
            actions.Remove(action as GestureAction);
        }
        public override void InsertAction(int index, Action action)
        {
            actions.Insert(index, action as GestureAction);
        }

        public enum GestureSide
        {
            Left,
            Right,
        }
        public GestureSide side;

        public SimpleGestureActions()
		{
			var action = new GestureAction();
			action.name = "Default";
			action.gestureTable.neutral = true;
			defaultAction = action;
		}

        public override void Build(MenuActions.MenuAction parentAction)
        {
            //Layer name
            var layerName = this.name;
            if (parentAction != null)
                layerName = $"{parentAction.name}_{layerName}_SubActions";

            //Build
            BuildNormal(AnimationLayer.FX, layerName, this.actions, parentAction);
        }
        void BuildNormal(AnimationLayer layerType, string layerName, List<GestureAction> sourceActions, MenuActions.MenuAction parentAction)
        {
            //Find all that affect this layer
            var layerActions = new List<GestureAction>();
            foreach (var action in sourceActions)
            {
                if (!action.ShouldBuild())
                    continue;
                if (!action.AffectsLayer(layerType))
                    continue;
                layerActions.Add(action);
            }
            if (layerActions.Count == 0)
                return;

            //Build
            BuildGestureLayer(layerType, layerName, layerActions, parentAction);
        }
        void BuildGestureLayer(AnimationLayer layerType, string layerName, List<GestureAction> actions, MenuActions.MenuAction parentAction)
        {
            var controller = GetController(layerType);

            //Add parameter
            string paramName = this.side == GestureSide.Left ? "GestureLeft" : "GestureRight";
            AddParameter(controller, paramName, AnimatorControllerParameterType.Int, 0);

            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(-1, 2);

            //Default state
            var defaultAction = this.defaultAction;
            var defaultState = layer.stateMachine.AddState("Default Gesture", StatePosition(0, 0));
            var unusedGestures = new List<GestureEnum>();
            unusedGestures.Add(GestureEnum.Neutral);
            unusedGestures.Add(GestureEnum.Fist);
            unusedGestures.Add(GestureEnum.OpenHand);
            unusedGestures.Add(GestureEnum.FingerPoint);
            unusedGestures.Add(GestureEnum.Victory);
            unusedGestures.Add(GestureEnum.RockNRoll);
            unusedGestures.Add(GestureEnum.HandGun);
            unusedGestures.Add(GestureEnum.ThumbsUp);

                //Animation Layer Weight
                var layerWeight = defaultState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = GetLayerIndex(controller, layer);
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

            //Build states
            int actionIter = 0;
            foreach (var action in this.actions)
            {
                //Check if valid
                if(!action.gestureTable.IsModified())
                {
                    EditorUtility.DisplayDialog("Build Warning", $"Simple Gesture {action.name} has no selected conditions.", "Okay");
                    continue;
                }

                //Build
                var state = layer.stateMachine.AddState(action.name, StatePosition(0, actionIter + 1));
                state.motion = action.GetAnimation(layerType, true);
                actionIter += 1;

                //Conditions
                AddGestureCondition(GestureEnum.Neutral);
                AddGestureCondition(GestureEnum.Fist);
                AddGestureCondition(GestureEnum.OpenHand);
                AddGestureCondition(GestureEnum.FingerPoint);
                AddGestureCondition(GestureEnum.Victory);
                AddGestureCondition(GestureEnum.RockNRoll);
                AddGestureCondition(GestureEnum.HandGun);
                AddGestureCondition(GestureEnum.ThumbsUp);
                void AddGestureCondition(BaseActions.GestureEnum gesture)
                {
                    if (action.gestureTable.GetValue(gesture))
                    {
                        //Transition
                        var transition = layer.stateMachine.AddAnyStateTransition(state);
                        transition.hasExitTime = false;
                        transition.exitTime = 0;
                        transition.duration = action.fadeIn;
                        transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, paramName);

                        //Parent
                        if(parentAction != null)
                            transition.AddCondition(AnimatorConditionMode.Equals, parentAction.controlValue, parentAction.parameter);

                        //Cleanup
                        unusedGestures.Remove(gesture);
                    }
                }
            }

            //Default transitions
            foreach (var gesture in unusedGestures)
            {
                //Transition
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                transition.hasExitTime = false;
                transition.exitTime = 0;
                transition.duration = defaultAction.fadeIn;
                transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, paramName);
            }

            //Parent
            if (parentAction != null)
            {
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                transition.hasExitTime = false;
                transition.exitTime = 0;
                transition.duration = defaultAction.fadeIn;
                transition.AddCondition(AnimatorConditionMode.NotEqual, parentAction.controlValue, parentAction.parameter);
            }
        }
    }

    [CustomEditor(typeof(SimpleGestureActions))]
    public class SimpleGestureActionsEditor : BaseActionsEditor
    {
        SimpleGestureActions gestureScript;

        public override void Inspector_Header()
        {
            gestureScript = target as SimpleGestureActions;

            //Default Action
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                //Side
                gestureScript.side = (SimpleGestureActions.GestureSide)EditorGUILayout.EnumPopup("Side", gestureScript.side);

                //Default
                var action = gestureScript.defaultAction;
                EditorGUILayout.LabelField("Default Gesture");
                EditorGUI.indentLevel += 1;
                action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
                action.fxLayerAnimations.enter = DrawAnimationReference("FX Layer", action.fxLayerAnimations.enter, "Gestures_FX_");
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndVertical();
        }
        public override void Inspector_Action_Header(BaseActions.Action action)
        {
            var gestureAction = (SimpleGestureActions.GestureAction)action;

            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);

            //Gesture
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.LabelField("Gesture Type");
                //DrawGestureToggle("Neutral", AvatarActionSet.Action.GestureEnum.Neutral);
                DrawGestureToggle("Fist", BaseActions.GestureEnum.Fist);
                DrawGestureToggle("Open Hand", BaseActions.GestureEnum.OpenHand);
                DrawGestureToggle("Finger Point", BaseActions.GestureEnum.FingerPoint);
                DrawGestureToggle("Victory", BaseActions.GestureEnum.Victory);
                DrawGestureToggle("Rock N Roll", BaseActions.GestureEnum.RockNRoll);
                DrawGestureToggle("Hand Gun", BaseActions.GestureEnum.HandGun);
                DrawGestureToggle("Thumbs Up", BaseActions.GestureEnum.ThumbsUp);

                void DrawGestureToggle(string name, BaseActions.GestureEnum type)
                {
                    var value = gestureAction.gestureTable.GetValue(type);
                    EditorGUI.BeginDisabledGroup(!value && !CheckGestureTypeUsed(type));
                    gestureAction.gestureTable.SetValue(type, EditorGUILayout.Toggle(name, value));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Warning
            if(!gestureAction.gestureTable.IsModified())
            {
                EditorGUILayout.HelpBox("No conditions currently selected.", MessageType.Warning);
            }

            //Animation
            action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
            action.fxLayerAnimations.enter = DrawAnimationReference("FX Layer", action.fxLayerAnimations.enter, "Gestures_FX");

            //Default
            //DrawInspector_Action(action, false);
        }
        bool CheckGestureTypeUsed(BaseActions.GestureEnum type)
        {
            foreach (var action in gestureScript.actions)
            {
                if (action.gestureTable.GetValue(type))
                    return false;
            }
            return true;
        }
    }
}
#endif