using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using System;
using UnityEngineInternal;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(AvatarGestures))]
    public class AvatarGestureEditor : AvatarActionsEditor
    {
        public override void Inspector_Header()
        {
            //Default Action
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                var action = (target as AvatarGestures).defaultAction;

                EditorGUILayout.LabelField("Default Gesture");
                EditorGUI.indentLevel += 1;
                action.actionLayerAnimations.enter = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField("Action Layer", action.actionLayerAnimations.enter, typeof(UnityEngine.AnimationClip), false);
                action.fxLayerAnimations.enter = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField("Fx Layer", action.fxLayerAnimations.enter, typeof(UnityEngine.AnimationClip), false);
                EditorGUI.indentLevel -= 1;
                action.fadeIn = EditorGUILayout.FloatField("Transition In", action.fadeIn);
            }
            EditorGUILayout.EndVertical();
        }
        public override void Inspector_Action(AvatarActions.Action action)
        {
            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);

            //Gesture
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.LabelField("Gesture Type");
                //DrawGestureToggle("Neutral", AvatarActionSet.Action.GestureEnum.Neutral);
                DrawGestureToggle("Fist", AvatarActions.Action.GestureEnum.Fist);
                DrawGestureToggle("Open Hand", AvatarActions.Action.GestureEnum.OpenHand);
                DrawGestureToggle("Finger Point", AvatarActions.Action.GestureEnum.FingerPoint);
                DrawGestureToggle("Victory", AvatarActions.Action.GestureEnum.Victory);
                DrawGestureToggle("Rock N Roll", AvatarActions.Action.GestureEnum.RockNRoll);
                DrawGestureToggle("Hand Gun", AvatarActions.Action.GestureEnum.HandGun);
                DrawGestureToggle("Thumbs Up", AvatarActions.Action.GestureEnum.ThumbsUp);

                void DrawGestureToggle(string name, AvatarActions.Action.GestureEnum type)
                {
                    var value = action.gestureType.GetValue(type);
                    EditorGUI.BeginDisabledGroup(!value && !CheckGestureTypeUsed(type));
                    action.gestureType.SetValue(type, EditorGUILayout.Toggle(name, value));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Animation
            action.actionLayerAnimations.enter = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField("Action Layer", action.actionLayerAnimations.enter, typeof(UnityEngine.AnimationClip), false);
            action.fxLayerAnimations.enter = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField("Fx Layer", action.fxLayerAnimations.enter, typeof(UnityEngine.AnimationClip), false);
            action.fadeIn = EditorGUILayout.FloatField("Transition In", action.fadeIn);

            //Default
            //DrawInspector_Action(action, false);
        }
        bool CheckGestureTypeUsed(AvatarActions.Action.GestureEnum type)
        {
            foreach (var action in script.actions)
            {
                if (action.gestureType.GetValue(type))
                    return false;
            }
            return true;
        }
    }
}