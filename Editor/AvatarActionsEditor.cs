using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using System;
using System.Reflection;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(AvatarActions))]
    public class AvatarActionsEditor : Editor
    {
        protected AvatarActions script;
        protected AvatarDescriptor avatarDescriptor;
        protected AvatarActions.Action selectedAction;
        static string[] ParameterNames =
        {
            "[None]",
            "Stage1",
            "Stage2",
            "Stage3",
            "Stage4",
            "Stage5",
            "Stage6",
            "Stage7",
            "Stage8",
            "Stage9",
            "Stage10",
            "Stage11",
            "Stage12",
            "Stage13",
            "Stage14",
            "Stage15",
            "Stage16",
        };
        protected static List<string> popupCache = new List<string>();

        GUIStyle boxUnselected;
        GUIStyle boxSelected;
        GUIStyle boxDisabled;
        void InitStyles()
        {
            //if(boxUnselected == null)
            boxUnselected = new GUIStyle(GUI.skin.box);

            //if(boxSelected == null)
            {
                boxSelected = new GUIStyle(GUI.skin.box);
                boxSelected.normal.background = MakeTex(2, 2, new Color(0.0f, 0.5f, 1f, 0.5f));
            }

            //if(boxDisabled == null)
            {
                boxDisabled = new GUIStyle(GUI.skin.box);
                boxDisabled.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.25f));
            }
        }

        public void OnEnable()
        {
            var editor = target as AvatarActions;
        }
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                script = target as AvatarActions;
                InitStyles();

                //Avatar Descriptor
                SelectAvatarDescriptor();
                if (avatarDescriptor == null)
                {
                    EditorGUILayout.HelpBox("No active avatar descriptor found in scene.", MessageType.Error);
                }
                Divider();

                EditorGUI.BeginDisabledGroup(avatarDescriptor == null);
                {
                    //Header
                    Inspector_Header();

                    //Controls
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Add
                        //EditorGUI.BeginDisabledGroup(script.actions.Count >= 8);
                        if (GUILayout.Button("Add"))
                        {
                            var action = new AvatarActions.Action();
                            action.name = "New Action";
                            script.actions.Add(action);
                        }
                        //EditorGUI.EndDisabledGroup();

                        //Delete
                        EditorGUI.BeginDisabledGroup(selectedAction == null);
                        if (GUILayout.Button("Delete"))
                        {
                            if (EditorUtility.DisplayDialog("Delete Action?", String.Format("Delete the action '{0}'?", selectedAction.name), "Delete", "Cancel"))
                            {
                                script.actions.Remove(selectedAction);
                                SelectAction(null);
                            }
                        }
                        EditorGUI.EndDisabledGroup();

                        //Move Up
                        if (GUILayout.Button("Move Up"))
                        {
                            var index = script.actions.IndexOf(selectedAction);
                            script.actions.RemoveAt(index);
                            script.actions.Insert(Mathf.Max(0, index - 1), selectedAction);
                        }

                        //Move Down
                        if (GUILayout.Button("Move Down"))
                        {
                            var index = script.actions.IndexOf(selectedAction);
                            script.actions.RemoveAt(index);
                            script.actions.Insert(Mathf.Min(script.actions.Count, index + 1), selectedAction);
                        }

                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();

                    Divider();

                    //Draw actions
                    indentLevel = 0;
                    DrawActions(script.actions);

                    Divider();

                    //Action Info
                    if (selectedAction != null)
                    {
                        EditorGUI.BeginDisabledGroup(!selectedAction.enabled);
                        Inspector_Action(selectedAction);
                        EditorGUI.EndDisabledGroup();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }

        public virtual void Inspector_Header()
        {
            script.isRootMenu = EditorGUILayout.Toggle("Root Menu", script.isRootMenu);
            if (script.isRootMenu)
            {
                //Gesture Sets
                script.gesturesL = (AvatarGestures)EditorGUILayout.ObjectField("Gestures L", script.gesturesL, typeof(AvatarGestures), false);
                script.gesturesR = (AvatarGestures)EditorGUILayout.ObjectField("Gestures R", script.gesturesR, typeof(AvatarGestures), false);

                //Build
                if (GUILayout.Button("Build Avatar Data"))
                {
                    AvatarActions.BuildAnimationControllers(avatarDescriptor, script);
                }

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUI.indentLevel += 1;
                script.foldoutMeta = EditorGUILayout.Foldout(script.foldoutMeta, "Built Data");
                if (script.foldoutMeta)
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
            Divider();
        }
        public virtual void Inspector_Action(AvatarActions.Action action)
        {
            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);
            action.icon = (Texture2D)EditorGUILayout.ObjectField("Icon", action.icon, typeof(Texture2D), false);

            //Type
            action.type = (AvatarActions.Action.ActionType)EditorGUILayout.EnumPopup("Type", action.type);
            switch (action.type)
            {
                case AvatarActions.Action.ActionType.Button:
                case AvatarActions.Action.ActionType.Toggle:
                    DrawInspector_Action(action);
                    break;
                case AvatarActions.Action.ActionType.Slider:
                    DrawInspector_Slider(action);
                    break;
                case AvatarActions.Action.ActionType.SubMenu:
                    DrawInspector_SubMenu(action);
                    break;
                case AvatarActions.Action.ActionType.PreExisting:
                    EditorGUILayout.HelpBox("Pre-Existing will preserve custom expression controls with the same name.", MessageType.Info);
                    break;
            }
        }
        public virtual bool CanDeleteAction(AvatarActions.Action action)
        {
            return true;
        }

        public void DrawInspector_Action(AvatarActions.Action action, bool showParam = true)
        {
            //Transitions
            action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
            action.fadeOut = EditorGUILayout.FloatField("Fade Out", action.fadeOut);

            //Parameter
            if (showParam)
            {
                action.parameter = DrawParameterDropDown(action.parameter, "Parameter");
                if (avatarDescriptor.expressionParameters == null)
                    EditorGUILayout.HelpBox("No VRCExpressionsParameter object attached to the VRCAvatarDescriptor", MessageType.Warning);
            }

            //Toggle Objects
            if (action.type == AvatarActions.Action.ActionType.Button || action.type == AvatarActions.Action.ActionType.Toggle)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginVertical(GUI.skin.box);
                action.foldoutObjects = EditorGUILayout.Foldout(action.foldoutObjects, "Toggle Objects");
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
                            if(property.objRef == null)
                                property.objRef = FindPropertyObject(avatarDescriptor.gameObject, property.path);

                            property.objRef = (GameObject)EditorGUILayout.ObjectField("", property.objRef, typeof(GameObject), true, null);
                            if(EditorGUI.EndChangeCheck())
                            {
                                if(property.objRef != null)
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
                        if(propertyHasUpdated)
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
                action.foldoutAnimations = EditorGUILayout.Foldout(action.foldoutAnimations, "Animations");
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
            action.foldoutIkOverrides = EditorGUILayout.Foldout(action.foldoutIkOverrides, "IK Overrides");
            if (action.foldoutIkOverrides)
            {
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
        }
        public void DrawInspector_SubMenu(AvatarActions.Action action)
        {
            EditorGUILayout.BeginHorizontal();
            action.subMenu = (AvatarActions)EditorGUILayout.ObjectField("Sub Menu", action.subMenu, typeof(AvatarActions), false);
            EditorGUI.BeginDisabledGroup(action.subMenu != null);
            if (GUILayout.Button("New", GUILayout.Width(64f)))
            {
                //Create
                var subMenu = ScriptableObject.CreateInstance<AvatarActions>();
                subMenu.name = "Ani_" + action.name;
                AvatarActions.SaveAsset(subMenu, script, true);

                //Set
                action.subMenu = subMenu;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        public void DrawInspector_Slider(AvatarActions.Action action)
        {
            //Animations
            EditorGUI.BeginDisabledGroup(true); //Disable for now
            action.actionLayerAnimations.enter = DrawAnimationReference("Action Layer", action.actionLayerAnimations.enter, $"{action.name}_Action_Slider");
            EditorGUI.EndDisabledGroup();
            action.fxLayerAnimations.enter = DrawAnimationReference("FX Layer", action.fxLayerAnimations.enter, $"{action.name}_FX_Slider");

            //Parameter
            action.parameter = DrawParameterDropDown(action.parameter, "Parameter");
        }

        int indentLevel = 0;
        void DrawActions(List<AvatarActions.Action> actions)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                bool isGroup = action.type == AvatarActions.Action.ActionType.SubMenu;
                bool isSelected = selectedAction == action;

                //Draw header
                var headerRect = EditorGUILayout.BeginHorizontal(isSelected ? boxSelected : boxUnselected);
                {
                    EditorGUILayout.LabelField(action.name);
                    GUILayout.FlexibleSpace();
                    action.enabled = EditorGUILayout.Toggle(action.enabled, GUILayout.Width(32));
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

        void SelectAvatarDescriptor()
        {
            var descriptors = GameObject.FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptors.Length > 0)
            {
                //Compile list of names
                string[] names = new string[descriptors.Length];
                for (int i = 0; i < descriptors.Length; i++)
                    names[i] = descriptors[i].gameObject.name;

                //Select
                var currentIndex = System.Array.IndexOf(descriptors, avatarDescriptor);
                var nextIndex = EditorGUILayout.Popup("Active Avatar", currentIndex, names);
                if (nextIndex < 0)
                    nextIndex = 0;
                if (nextIndex != currentIndex)
                    SelectAvatarDescriptor(descriptors[nextIndex]);
            }
            else
                SelectAvatarDescriptor(null);
        }
        void SelectAvatarDescriptor(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc)
        {
            if (desc == avatarDescriptor)
                return;

            avatarDescriptor = desc;
            if (avatarDescriptor != null)
            {
                //Init stage parameters
                for (int i = 0; i < 16; i++)
                    InitStage(i);
                void InitStage(int i)
                {
                    var param = desc.GetExpressionParameter(i);
                    string name = "[None]";
                    if (param != null && !string.IsNullOrEmpty(param.name))
                        name = string.Format("{0}, {1}", param.name, param.valueType.ToString(), i + 1);
                    ParameterNames[i + 1] = name;
                }
            }
            else
            {
                //Clear
                for (int i = 0; i < 16; i++)
                    ParameterNames[i + 1] = "[None]";
            }
        }
        string DrawParameterDropDown(string parameter, string label)
        {
            bool parameterFound = false;
            EditorGUILayout.BeginHorizontal();
            {
                if (avatarDescriptor != null)
                {
                    //Dropdown
                    int currentIndex;
                    if (string.IsNullOrEmpty(parameter))
                    {
                        currentIndex = -1;
                        parameterFound = true;
                    }
                    else
                    {
                        currentIndex = -2;
                        for (int i = 0; i < GetExpressionParametersCount(); i++)
                        {
                            var item = avatarDescriptor.GetExpressionParameter(i);
                            if (item.name == parameter)
                            {
                                parameterFound = true;
                                currentIndex = i;
                                break;
                            }
                        }
                    }

                    //Dropdown
                    EditorGUI.BeginDisabledGroup(avatarDescriptor.expressionParameters == null);
                    {
                        EditorGUI.BeginChangeCheck();
                        currentIndex = EditorGUILayout.Popup(label, currentIndex + 1, ParameterNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (currentIndex == 0)
                                parameter = "";
                            else
                                parameter = GetExpressionParameter(currentIndex - 1).name;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(0, new string[0]);
                    EditorGUI.EndDisabledGroup();
                }

                //Text field
                parameter = EditorGUILayout.TextField(parameter, GUILayout.MaxWidth(200));
            }
            EditorGUILayout.EndHorizontal();

            if (!parameterFound)
            {
                EditorGUILayout.HelpBox("Parameter not found on the active avatar descriptor.", MessageType.Warning);
            }

            return parameter;
        }
        int GetExpressionParametersCount()
        {
            if (avatarDescriptor != null && avatarDescriptor.expressionParameters != null && avatarDescriptor.expressionParameters.parameters != null)
                return avatarDescriptor.expressionParameters.parameters.Length;
            return 0;
        }
        ExpressionParameters.Parameter GetExpressionParameter(int i)
        {
            if (avatarDescriptor != null)
                return avatarDescriptor.GetExpressionParameter(i);
            return null;
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
        void SetListSize<TYPE>(List<TYPE> list, int size)
        {
            int difference = list.Count - size;
            if (difference > 0)
            {
                list.RemoveRange(size, difference);
            }
            else if (difference < 0)
            {
                for (int i = 0; i < -difference; i++)
                {
                    list.Add(default(TYPE));
                }
            }
        }
        void Divider()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        bool SelectRect(Rect rect)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                return rect.Contains(Event.current.mousePosition);
            }
            return false;
        }
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        bool FoldoutButton(bool show, bool state)
        {
            if (show)
            {
                if (GUILayout.Button(state ? "v" : ">", GUILayout.Width(32f)))
                {
                    state = !state;
                }
            }
            else
            {
                GUILayout.Button("", GUIStyle.none, GUILayout.Width(32f));
            }
            return state;
        }
        UnityEngine.AnimationClip DrawAnimationReference(string name, UnityEngine.AnimationClip clip, string newAssetName)
        {
            EditorGUILayout.BeginHorizontal();
            {
                clip = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField(name, clip, typeof(UnityEngine.AnimationClip), false);
                EditorGUI.BeginDisabledGroup(clip != null);
                {
                    if (GUILayout.Button("New", GUILayout.Width(48)))
                    {
                        //Create animation    
                        clip = new AnimationClip();
                        clip.name = newAssetName;
                        AvatarActions.SaveAsset(clip, this.script as AvatarActions, true);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(clip == null);
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(48)))
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
