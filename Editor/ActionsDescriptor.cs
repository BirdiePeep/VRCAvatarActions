using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;

namespace VRCAvatarActions
{
    [CreateAssetMenu(fileName = "Actions Descriptor", menuName = "VRCAvatarActions/Actions Descriptor")]
    public class ActionsDescriptor : ScriptableObject
    {
        //Descriptor Data
        public MenuActions menuActions;
        public List<NonMenuActions> otherActions = new List<NonMenuActions>();

        //Build Options
        public List<string> ignoreLayers = new List<string>();
        public List<string> ignoreParameters = new List<string>();

        //Meta
        public bool foldoutBuildData = false;
        public bool foldoutBuildOptions = false;
        public bool foldoutIgnoreLayers = false;
        public bool foldoutIgnoreParameters = false;
    }

    [CustomEditor(typeof(ActionsDescriptor))]
    public class ActionsDescriptorEditor : EditorBase
    {
        enum NonMenuTypes
        {
            New,
            Idle,
            SimpleGestures,
        }

        public ActionsDescriptor script;
        public override void Inspector_Body()
        {
            script = target as ActionsDescriptor;

            //Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.BeginHorizontal();
                script.menuActions = (MenuActions)EditorGUILayout.ObjectField("Menu Actions", script.menuActions, typeof(MenuActions), false);

                EditorGUI.BeginDisabledGroup(script.menuActions != null);
                if(GUILayout.Button("New", GUILayout.Width(SmallButtonSize)))
                {
                    var actions = new MenuActions();
                    actions.name = "Menu Main";
                    if(AvatarActions.SaveAsset(actions, script, checkIfExists:true))
                    {
                        script.menuActions = actions;
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Non-Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.LabelField("Other Actions");
            {
                if (GUILayout.Button("Add"))
                    script.otherActions.Add(null);
                for (int i = 0; i < script.otherActions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Reference
                        script.otherActions[i] = (NonMenuActions)EditorGUILayout.ObjectField("Actions", script.otherActions[i], typeof(NonMenuActions), false);

                        //Add
                        /*EditorGUI.BeginDisabledGroup(script.nonMenuActions[i] != null);
                        if(GUILayout.Button("New", GUILayout.Width(SmallButtonSize)))
                        {
                            

                            var actions = new IdleActions();
                            actions.name = "Idle Actions";
                            if(AvatarActions.SaveAsset(actions, script, checkIfExists: true))
                            {
                                script.nonMenuActions[i] = actions;
                            }
                        }
                        EditorGUI.EndDisabledGroup();*/

                        //Delete
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            script.otherActions.RemoveAt(i);
                            i -= 1;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            Divider();

            //Build
            if (GUILayout.Button("Build Avatar Data"))
            {
                AvatarActions.BuildAvatarData(avatarDescriptor, script);
            }

            //Build Options
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                script.foldoutBuildOptions = EditorGUILayout.Foldout(script.foldoutBuildOptions, "Built Options");
                if (script.foldoutBuildOptions)
                {
                    //Ignore Lists
                    DrawStringList(ref script.foldoutIgnoreLayers, "Ignore Layers", script.ignoreLayers);
                    DrawStringList(ref script.foldoutIgnoreParameters, "Ignore Parameters", script.ignoreParameters);

                    void DrawStringList(ref bool foldout, string title, List<string> list)
                    {
                        EditorGUI.indentLevel += 1;
                        foldout = EditorGUILayout.Foldout(foldout, AvatarActionsEditor.Title(title, list.Count > 0));
                        if (foldout)
                        {
                            //Add
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUI.indentLevel * 10);
                            if (GUILayout.Button("Add"))
                            {
                                list.Add(null);
                            }
                            GUILayout.EndHorizontal();

                            //Layers
                            for (int i = 0; i < list.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                list[i] = EditorGUILayout.TextField(list[i]);
                                if (GUILayout.Button("X", GUILayout.Width(32)))
                                {
                                    list.RemoveAt(i);
                                    i--;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Build Data
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            script.foldoutBuildData = EditorGUILayout.Foldout(script.foldoutBuildData, "Built Data");
            if (script.foldoutBuildData)
            {
                void AnimationController(AvatarActions.BaseLayers index, string name)
                {
                    var layer = avatarDescriptor.baseAnimationLayers[(int)index];
                    var controller = layer.animatorController as UnityEditor.Animations.AnimatorController;

                    EditorGUI.BeginChangeCheck();
                    controller = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField(name, controller, typeof(UnityEditor.Animations.AnimatorController), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        layer.animatorController = controller;
                        layer.isDefault = false;
                    }
                }

                EditorGUILayout.HelpBox("Objects built and linked on the avatar descriptor. Anything referenced here will be modified and possibly destroyed by the compiling process.", MessageType.Info);

                AnimationController(AvatarActions.BaseLayers.Action, "Action Controller");
                AnimationController(AvatarActions.BaseLayers.FX, "FX Controller");
                avatarDescriptor.expressionsMenu = (ExpressionsMenu)EditorGUILayout.ObjectField("Expressions Menu", avatarDescriptor.expressionsMenu, typeof(ExpressionsMenu), false);
                avatarDescriptor.expressionParameters = (ExpressionParameters)EditorGUILayout.ObjectField("Expression Parameters", avatarDescriptor.expressionParameters, typeof(ExpressionParameters), false);
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
    }
}
    
