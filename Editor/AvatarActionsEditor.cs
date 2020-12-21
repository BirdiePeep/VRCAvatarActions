using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using System;
using System.Reflection;
using Amazon.Auth.AccessControlPolicy;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(AvatarActions))]
    public class AvatarActionsEditor : EditorBase
    {
        protected AvatarActions script;
        protected AvatarActions.Action selectedAction;

        public void OnEnable()
        {
            var editor = target as AvatarActions;
        }
        public override void Inspector_Body()
        {
            script = target as AvatarActions;

            //Controls
            EditorGUILayout.BeginHorizontal();
            {
                //Add
                if (GUILayout.Button("Add"))
                {
                    var action = script.AddAction();
                    action.name = "New Action";
                }

                //Move Up
                if (GUILayout.Button("Move Up"))
                {
                    var temp = new List<AvatarActions.Action>();
                    script.GetActions(temp);

                    var index = temp.IndexOf(selectedAction);
                    script.RemoveAction(selectedAction);
                    script.InsertAction(Mathf.Max(0, index - 1), selectedAction);
                }

                //Move Down
                if (GUILayout.Button("Move Down"))
                {
                    var temp = new List<AvatarActions.Action>();
                    script.GetActions(temp);

                    var index = temp.IndexOf(selectedAction);
                    script.RemoveAction(selectedAction);
                    script.InsertAction(Mathf.Min(temp.Count-1, index + 1), selectedAction);
                }

                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            //Draw actions
            var actions = new List<AvatarActions.Action>();
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
        public virtual void Inspector_Action_Header(AvatarActions.Action action)
        {
            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);
        }
        public virtual void Inspector_Action_Body(AvatarActions.Action action, bool showParam = true)
        {
            //Transitions
            action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
            action.fadeOut = EditorGUILayout.FloatField("Fade Out", action.fadeOut);

            //Toggle Objects
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginVertical(GUI.skin.box);
                action.foldoutObjects = EditorGUILayout.Foldout(action.foldoutObjects, Title("Toggle Objects", action.objProperties.Count > 0));
                if (action.foldoutObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Add
                        if (GUILayout.Button("Add"))
                        {
                            action.objProperties.Add(new AvatarActions.Action.Property());
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    for (int i = 0; i < action.objProperties.Count; i++)
                    {
                        var property = action.objProperties[i];
                        bool propertyHasUpdated = false;
                        bool isSelected = false;

                        //Draw header
                        var headerRect = EditorGUILayout.BeginHorizontal(isSelected ? boxSelected : boxUnselected);
                        {
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
                                }
                                else
                                {
                                    property.Clear();
                                }

                                //Store
                                propertyHasUpdated = true;
                            }
                            if (GUILayout.Button("X", GUILayout.Width(32)))
                            {
                                action.objProperties.RemoveAt(i);
                                i--;
                            }
                            //action.enabled = EditorGUILayout.Toggle(action.enabled, GUILayout.Width(32));
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

                        //Finally update the array
                        if (propertyHasUpdated)
                        {
                            action.objProperties[i] = property;
                        }
                    }


                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel -= 1;
            }

            //Animations
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                action.foldoutAnimations = EditorGUILayout.Foldout(action.foldoutAnimations, Title("Animations", action.HasAnimations()));
                if (action.foldoutAnimations)
                {
                    //Action layer
                    EditorGUILayout.LabelField("Action Layer");
                    EditorGUI.indentLevel += 1;
                    action.actionLayerAnimations.enter = DrawAnimationReference("Enter", action.actionLayerAnimations.enter, $"{action.name}_Action_Enter");
                    action.actionLayerAnimations.exit = DrawAnimationReference("Exit", action.actionLayerAnimations.exit, $"{action.name}_Action_Exit");
                    EditorGUILayout.HelpBox("Use for transfoming the humanoid skeleton.  You will need to use IK Overrides to disable IK control of body parts.", MessageType.Info);
                    EditorGUI.indentLevel -= 1;

                    //FX Layer
                    EditorGUILayout.LabelField("FX Layer");
                    EditorGUI.indentLevel += 1;
                    action.fxLayerAnimations.enter = DrawAnimationReference("Enter", action.fxLayerAnimations.enter, $"{action.name}_FX_Enter");
                    action.fxLayerAnimations.exit = DrawAnimationReference("Exit", action.fxLayerAnimations.exit, $"{action.name}_FX_Exit");
                    EditorGUILayout.HelpBox("Use for most everything else, including bones not part of the humanoid skeleton.", MessageType.Info);
                    EditorGUI.indentLevel -= 1;
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Body Overrides
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            action.foldoutIkOverrides = EditorGUILayout.Foldout(action.foldoutIkOverrides, Title("IK Overrides", action.bodyOverride.HasAny()) );
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

        public void DrawInspector_Triggers(AvatarActions.Action action)
        {
            //Enter Triggers
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            action.foldoutTriggers = EditorGUILayout.Foldout(action.foldoutTriggers, Title("Triggers", action.triggers.Count > 0) );
            if (action.foldoutTriggers)
            {
                //Header
                if (GUILayout.Button("Add Trigger"))
                    action.triggers.Add(new AvatarActions.Action.Trigger());

                //Triggers
                for(int triggerIter=0; triggerIter<action.triggers.Count; triggerIter++)
                {
                    //Foldout
                    var trigger = action.triggers[triggerIter];
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        trigger.foldout = EditorGUILayout.Foldout(trigger.foldout, "Trigger");
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
                            trigger.type = (AvatarActions.Action.Trigger.Type)EditorGUILayout.EnumPopup("Type", trigger.type);

                            //Conditions
                            if (GUILayout.Button("Add Condition"))
                                trigger.conditions.Add(new AvatarActions.Action.Condition());

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

                            if(trigger.conditions.Count == 0)
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
        public bool DrawInspector_Condition(AvatarActions.Action.Condition trigger)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                //Type
                trigger.type = (AvatarActions.ParameterEnum)EditorGUILayout.EnumPopup(trigger.type);

                //Parameter
                if (trigger.type == AvatarActions.ParameterEnum.Custom)
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
                    case AvatarActions.ParameterEnum.Custom:
                        trigger.logic = (AvatarActions.Action.Condition.Logic)EditorGUILayout.EnumPopup(trigger.logic);
                        break;
                    case AvatarActions.ParameterEnum.Upright:
                    case AvatarActions.ParameterEnum.AngularY:
                    case AvatarActions.ParameterEnum.VelocityX:
                    case AvatarActions.ParameterEnum.VelocityY:
                    case AvatarActions.ParameterEnum.VelocityZ:
                    case AvatarActions.ParameterEnum.GestureRightWeight:
                    case AvatarActions.ParameterEnum.GestureLeftWeight:
                        trigger.logic = (AvatarActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((AvatarActions.Action.Condition.LogicCompare)trigger.logic);
                        break;
                    default:
                        trigger.logic = (AvatarActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((AvatarActions.Action.Condition.LogicEquals)trigger.logic);
                        break;
                }

                //Value
                switch (trigger.type)
                {
                    case AvatarActions.ParameterEnum.Custom:
                    case AvatarActions.ParameterEnum.Upright:
                    case AvatarActions.ParameterEnum.AngularY:
                    case AvatarActions.ParameterEnum.VelocityX:
                    case AvatarActions.ParameterEnum.VelocityY:
                    case AvatarActions.ParameterEnum.VelocityZ:
                    case AvatarActions.ParameterEnum.GestureRightWeight:
                    case AvatarActions.ParameterEnum.GestureLeftWeight:
                        trigger.value = EditorGUILayout.FloatField(1);
                        break;
                    case AvatarActions.ParameterEnum.GestureLeft:
                    case AvatarActions.ParameterEnum.GestureRight:
                        trigger.value = Convert.ToInt32(EditorGUILayout.EnumPopup((AvatarActions.GestureEnum)(int)trigger.value));
                        break;
                    case AvatarActions.ParameterEnum.Visime:
                        trigger.value = Convert.ToInt32(EditorGUILayout.EnumPopup((AvatarActions.VisimeEnum)(int)trigger.value));
                        break;
                    case AvatarActions.ParameterEnum.TrackingType:
                        trigger.value = Convert.ToInt32(EditorGUILayout.EnumPopup((AvatarActions.TrackingTypeEnum)(int)trigger.value));
                        break;
                    case AvatarActions.ParameterEnum.AFK:
                    case AvatarActions.ParameterEnum.MuteSelf:
                    case AvatarActions.ParameterEnum.InStation:
                    case AvatarActions.ParameterEnum.IsLocal:
                    case AvatarActions.ParameterEnum.Grounded:
                    case AvatarActions.ParameterEnum.Seated:
                        EditorGUI.BeginDisabledGroup(true);
                        trigger.value = 1;
                        EditorGUILayout.TextField("True");
                        EditorGUI.EndDisabledGroup();
                        break;
                    case AvatarActions.ParameterEnum.VRMode:
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

        void DrawActions(List<AvatarActions.Action> actions)
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
                            if(isSelected)
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
        void SelectAction(AvatarActions.Action action)
        {
            if (selectedAction != action)
            {
                selectedAction = action;
                Repaint();
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
                        AvatarActions.SaveAsset(clip, this.script as AvatarActions, "Generated", true);
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
        static void PrintMethods()
        {
            var windowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            var window = EditorWindow.GetWindow(windowType);
            if (window != null)
            {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                FieldInfo animEditor = windowType.GetField("m_AnimEditor", flags);

                Type animEditorType = animEditor.FieldType;
                System.Object animEditorObject = animEditor.GetValue(window);
                FieldInfo animWindowState = animEditorType.GetField("m_State", flags);
                Type windowStateType = animWindowState.FieldType;

                Debug.Log("Methods");
                MethodInfo[] methods = windowStateType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Debug.Log("Methods : " + methods.Length);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo currentInfo = methods[i];
                    Debug.Log(currentInfo.ToString());
                }
            }
        }
        public static GameObject FindPropertyObject(GameObject root, string path)
        {
            if (String.IsNullOrEmpty(path))
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
                if (obj == avatarDescriptor.gameObject)
                    break;
                path = $"{obj.name}/{path}";
            }
            return path;
        }
        #endregion
    }
}
