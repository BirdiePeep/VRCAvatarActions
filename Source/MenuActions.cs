#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCAvatarActions
{
    [CreateAssetMenu(fileName = "Menu", menuName = "VRCAvatarActions/Menu Actions/Menu")]
    public class MenuActions : BaseActions
    {
        [System.Serializable]
        public class MenuAction : Action
        {
            public enum MenuType
            {
                //Action
                Toggle = 4453,
                Button = 3754,
                Slider = 8579,
                SubMenu = 3641,
                PreExisting = 5697,
            }
            public MenuType menuType = MenuType.Toggle;

            public Texture2D icon;
            public string parameter;
            public MenuActions subMenu;
            public List<NonMenuActions> subActions = new List<NonMenuActions>();
            public bool isOffState = false;

            //Meta
            public int controlValue = 0;
            public bool foldoutSubActions;

            public bool IsNormalAction()
            {
                return menuType == MenuType.Button || menuType == MenuType.Toggle;
            }
            public bool NeedsControlLayer()
            {
                return menuType == MenuType.Button || menuType == MenuType.Toggle || menuType == MenuType.Slider;
            }
            public override bool ShouldBuild()
            {
                switch(menuType)
                {
                    case MenuType.Button:
                    case MenuType.Toggle:
                    case MenuType.Slider:
                        if (string.IsNullOrEmpty(parameter))
                            return false;
                        break;
                    case MenuType.SubMenu:
                        if (subMenu == null)
                            return false;
                        break;
                }
                return base.ShouldBuild();
            }
            public override void CopyTo(Action clone)
            {
                base.CopyTo(clone);

                var menuClone = clone as MenuAction;
                if(menuClone != null)
                {
                    menuClone.icon = icon;
                    menuClone.parameter = parameter;
                    menuClone.menuType = menuType;
                    menuClone.subMenu = subMenu;
                    menuClone.foldoutSubActions = foldoutSubActions;

                    menuClone.subActions.Clear();
                    foreach (var item in subActions)
                        menuClone.subActions.Add(item);
                }
            }

            //Build
            public override string GetLayerGroup()
            {
                return parameter;
            }
            public override void AddCondition(AnimatorStateTransition transition, bool equals)
            {
                if (string.IsNullOrEmpty(this.parameter))
                    return;

                //Is parameter bool?
                AnimatorConditionMode mode;
                var param = FindExpressionParameter(this.parameter);
                if(param.valueType == ExpressionParameters.ValueType.Bool)
                    mode = equals ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
                else if(param.valueType == ExpressionParameters.ValueType.Int)
                    mode = equals ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;
                else
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", "Parameter value type is not as expected.", "Okay");
                    return;
                }

                //Set
                transition.AddCondition(mode, this.controlValue, this.parameter);
            }
        }
        public List<MenuAction> actions = new List<MenuAction>();

        public MenuAction FindMenuAction(string name)
        {
            foreach(var action in actions)
            {
                if (action.name == name)
                    return action;
            }
            foreach(var action in actions)
            {
                if(action.menuType == MenuAction.MenuType.SubMenu && action.subMenu != null)
                {
                    var result = action.subMenu.FindMenuAction(name);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }
        public override void GetActions(List<Action> output)
        {
            foreach (var action in actions)
                output.Add(action);
        }
        public override Action AddAction()
        {
            var result = new MenuAction();
            actions.Add(result);
            return result;
        }
        public override void InsertAction(int index, Action action)
        {
            actions.Insert(index, action as MenuAction);
        }
        public override void RemoveAction(Action action)
        {
            actions.Remove(action as MenuAction);
        }

        public virtual void Build()
        {
            //Collect all menu actions
            var validActions = new List<MenuAction>();
            CollectValidMenuActions(validActions);

            //Expression Parameters
            BuildExpressionParameters(validActions);
            if (BuildFailed)
                return;

            //Expressions Menu
            BuildActionValues(validActions);
            BuildExpressionsMenu(this);

            //Build normal
            BuildNormalLayers(validActions, AnimationLayer.Action);
            BuildNormalLayers(validActions, AnimationLayer.FX);

            //Build sliders
            BuildSliderLayers(validActions, AnimationLayer.Action);
            BuildSliderLayers(validActions, AnimationLayer.FX);

            //Sub Actions
            BuildSubActionLayers(validActions, AnimationLayer.Action);
            BuildSubActionLayers(validActions, AnimationLayer.FX);
        }
        void CollectValidMenuActions(List<MenuAction> output)
        {
            //Add our actions
            int selfAdded = 0;
            foreach(var action in this.actions)
            {
                //Enabled
                if (!action.ShouldBuild())
                    continue;

                //Parameter
                bool needsParameter = action.NeedsControlLayer();
                if (needsParameter && string.IsNullOrEmpty(action.parameter))
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' doesn't specify a parameter.", "Okay");
                    return;
                }

                //Check type
                if (action.menuType == MenuAction.MenuType.SubMenu)
                {
                    //Sub-Menus
                    if (action.subMenu != null)
                        action.subMenu.CollectValidMenuActions(output);
                }
                else if(action.menuType == MenuAction.MenuType.PreExisting)
                {
                    //Do Nothing
                }
                else
                {
                    //Add
                    output.Add(action);
                }

                //Increment
                selfAdded += 1;
            }

            //Validate
            if (selfAdded > ExpressionsMenu.MAX_CONTROLS)
            {
                BuildFailed = true;
                EditorUtility.DisplayDialog("Build Failed", $"{this.name} has too many actions defined.", "Okay");
            }
        }

        static void BuildExpressionParameters(List<MenuAction> sourceActions)
        {
            //Find all unique menu parameters
            AllParameters.Clear();
            foreach (var action in sourceActions)
            {
                var param = GenerateParameter(action);
                if (param != null && IsNewParameter(param))
                    AllParameters.Add(param);
            }
            bool IsNewParameter(ExpressionParameters.Parameter param)
            {
                foreach (var item in AllParameters)
                {
                    if (string.IsNullOrEmpty(item.name))
                        continue;
                    if (item.name == param.name)
                    {
                        if (item.valueType == param.valueType)
                            return false;
                        else
                        {
                            BuildFailed = true;
                            EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCExpressionParameters. Parameter named '{item.name}' is used twice but with different types.", "Okay");
                            return false;
                        }
                    }
                }
                return true;
            }

            //Add
            foreach(var param in AllParameters)
                DefineExpressionParameter(param);
        }
        static void BuildActionValues(List<MenuAction> sourceActions)
        {
            var parametersObject = AvatarDescriptor.expressionParameters;
            ParameterToMenuActions.Clear();
            foreach (var parameter in BuildParameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.name))
                    continue;

                //Find all actions
                List<MenuAction> actions = new List<MenuAction>();
                int actionCount = 1;
                bool defaultUsed = false;
                foreach (var action in sourceActions)
                {
                    if (action.parameter == parameter.name)
                    {
                        actions.Add(action);
                        action.controlValue = actionCount;
                        actionCount += 1;

                        //Default value
                        if (action.isOffState)
                        {
                            if(!defaultUsed)
                            {
                                defaultUsed = true;
                                action.controlValue = 0;
                            }
                            else
                            {
                                BuildFailed = true;
                                EditorUtility.DisplayDialog("Build Failed", $"Two menu actions are marked as 'Is Default State' for parameter '{parameter.name}'.  Only one can be marked as default at a time.", "Okay");
                                return;
                            }
                        }
                    }
                }
                ParameterToMenuActions.Add(parameter.name, actions);

                //Modify to bool
                if (actions.Count == 1 && (actions[0].menuType == MenuAction.MenuType.Toggle || actions[0].menuType == MenuAction.MenuType.Button))
                {
                    parameter.valueType = ExpressionParameters.ValueType.Bool;

                    //Save
                    EditorUtility.SetDirty(parametersObject);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        static void BuildExpressionsMenu(MenuActions rootMenu)
        {
            List<MenuActions> menuList = new List<MenuActions>();

            //Create root menu if needed
            if (AvatarDescriptor.expressionsMenu == null)
            {
                AvatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<ExpressionsMenu>();
                AvatarDescriptor.expressionsMenu.name = "ExpressionsMenu_Root";
                SaveAsset(AvatarDescriptor.expressionsMenu, rootMenu, "Generated");
            }

            //Expressions
            CreateMenu(rootMenu, AvatarDescriptor.expressionsMenu);
            void CreateMenu(MenuActions ourMenu, ExpressionsMenu expressionsMenu)
            {
                //Clear old controls
                List<ExpressionsMenu.Control> oldControls = new List<ExpressionsMenu.Control>();
                oldControls.AddRange(expressionsMenu.controls);
                expressionsMenu.controls.Clear();

                //Create controls from actions
                foreach (var action in ourMenu.actions)
                {
                    if (!action.ShouldBuild())
                        continue;

                    if (action.menuType == MenuAction.MenuType.Button)
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
                    else if (action.menuType == MenuAction.MenuType.Toggle)
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
                    else if (action.menuType == MenuAction.MenuType.Slider)
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
                    else if (action.menuType == MenuAction.MenuType.SubMenu)
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
                        if (expressionsSubMenu == null)
                        {
                            expressionsSubMenu = ScriptableObject.CreateInstance<ExpressionsMenu>();
                            expressionsSubMenu.name = "ExpressionsMenu_" + action.name;
                            SaveAsset(expressionsSubMenu, rootMenu, "Generated");
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
                    else if (action.menuType == MenuAction.MenuType.PreExisting)
                    {
                        //Recover old control
                        foreach (var controlIter in oldControls)
                        {
                            if (controlIter.name == action.name)
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

        //Normal
        static void BuildNormalLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            var controller = GetController(layerType);

            //Find matching actions
            var layerActions = new List<MenuAction>();
            foreach (var parameter in AllParameters)
            {
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.parameter != parameter.name)
                        continue;
                    if (!action.NeedsControlLayer())
                        continue;
                    if (action.menuType == MenuAction.MenuType.Slider)
                        continue;
                    if (!action.GetAnimation(layerType, true))
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Check of off state
                MenuAction offAction = null;
                foreach(var action in layerActions)
                {
                    if(action.controlValue == 0)
                    {
                        offAction = action;
                        break;
                    }
                }

                //Parameter
                AddParameter(controller, parameter.name, parameter.valueType == ExpressionParameters.ValueType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int, 0);

                //Build
                bool turnOffState = offAction == null;
                if (layerType == AnimationLayer.Action)
                    BuildActionLayer(controller, layerActions, parameter.name, null, turnOffState);
                else
                    BuildNormalLayer(controller, layerActions, parameter.name, layerType, null, turnOffState);
            }
        }
        static void BuildSliderLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            //For each parameter create a new layer
            foreach (var parameter in AllParameters)
            {
                BuildSliderLayer(sourceActions, layerType, parameter.name);
            }
        }
        public static void BuildSliderLayer(List<MenuAction> sourceActions, AnimationLayer layerType, string parameter)
        {
            var controller = GetController(layerType);

            //Find all slider actions
            var layerActions = new List<MenuAction>();
            foreach (var actionIter in sourceActions)
            {
                if (actionIter.menuType == MenuActions.MenuAction.MenuType.Slider && actionIter.parameter == parameter && actionIter.GetAnimationRaw(layerType) != null)
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
                state.motion = action.GetAnimationRaw(layerType);
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
        static void BuildSubActionLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            var controller = GetController(layerType);

            //Find matching actions
            var layerActions = new List<MenuAction>();
            foreach (var parameter in AllParameters)
            {
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.parameter != parameter.name)
                        continue;
                    if (action.subActions.Count == 0)
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Parameter
                AddParameter(controller, parameter.name, parameter.valueType == ExpressionParameters.ValueType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int, 0);

                //Sub-Actions
                foreach(var parentAction in layerActions)
                {
                    foreach(var subActions in parentAction.subActions)
                    {
                        subActions.Build(parentAction);
                    }
                }
            }
        }

        //Other
        static ExpressionParameters.Parameter GenerateParameter(MenuAction action)
        {
            if (string.IsNullOrEmpty(action.parameter))
                return null;
            var parameter = new ExpressionParameters.Parameter();
            parameter.name = action.parameter;
            switch (action.menuType)
            {
                case MenuAction.MenuType.Button:
                case MenuAction.MenuType.Toggle:
                    parameter.valueType = ExpressionParameters.ValueType.Int;
                    break;
                case MenuAction.MenuType.Slider:
                    parameter.valueType = ExpressionParameters.ValueType.Float;
                    break;
            }
            return parameter;
        }
    }

    [CustomEditor(typeof(MenuActions))]
    public class MenuActionsEditor : BaseActionsEditor
    {
        MenuActions menuScript;

        public override void Inspector_Header()
        {
            EditorGUILayout.HelpBox("Menu Actions - Actions that are displayed in the avatar's action menu.", MessageType.Info);
        }
        public override void Inspector_Body()
        {
            menuScript = target as MenuActions;

            int actionCount = 0;
            foreach(var action in menuScript.actions)
            {
                if (action.ShouldBuild())
                    actionCount += 1;
            }
            if(actionCount > ExpressionsMenu.MAX_CONTROLS)
            {
                EditorGUILayout.HelpBox($"Too many actions are defined, disable or delete until there are only {ExpressionsMenu.MAX_CONTROLS}", MessageType.Error);
            }

            base.Inspector_Body();
        }
        public override void Inspector_Action_Header(BaseActions.Action action)
        {
            //Base
            base.Inspector_Action_Header(action);

            //Type
            var menuAction = (MenuActions.MenuAction)action;
            menuAction.menuType = (MenuActions.MenuAction.MenuType)EditorGUILayout.EnumPopup("Type", menuAction.menuType);

            //Icon
            if (menuAction.menuType != MenuActions.MenuAction.MenuType.PreExisting)
                menuAction.icon = (Texture2D)EditorGUILayout.ObjectField("Icon", menuAction.icon, typeof(Texture2D), false);
        }
        public override void Inspector_Action_Body(BaseActions.Action action, bool showParam = true)
        {
            //Details
            var menuAction = (MenuActions.MenuAction)action;
            switch (menuAction.menuType)
            {
                case MenuActions.MenuAction.MenuType.Button:
                case MenuActions.MenuAction.MenuType.Toggle:
                    Inspector_Control(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.Slider:
                    DrawInspector_Slider(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.SubMenu:
                    DrawInspector_SubMenu(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.PreExisting:
                    EditorGUILayout.HelpBox("Pre-Existing will preserve custom expression controls with the same name.", MessageType.Info);
                    break;
            }
        }
        public void Inspector_Control(MenuActions.MenuAction action)
        {
            //Parameter
            action.parameter = DrawParameterDropDown(action.parameter, "Parameter");

            if(action.menuType == MenuActions.MenuAction.MenuType.Toggle)
            {
                string tooltip = "This action will be used when no other toggle with the same parameter is turned on.\n\nOnly one action can be marked as the off state for a parameter name.";
                action.isOffState = EditorGUILayout.Toggle(new GUIContent("Is Off State", tooltip), action.isOffState);
            }

            //Default
            base.Inspector_Action_Body(action);

            //Sub Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            action.foldoutSubActions = EditorGUILayout.Foldout(action.foldoutSubActions, Title("Sub Actions", action.subActions.Count > 0));
            if (action.foldoutSubActions)
            {
                //Add
                if (GUILayout.Button("Add"))
                {
                    action.subActions.Add(null);
                }

                //Sub-Actions
                for (int i = 0; i < action.subActions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    action.subActions[i] = (NonMenuActions)EditorGUILayout.ObjectField(action.subActions[i], typeof(NonMenuActions), false);
                    if (GUILayout.Button("X", GUILayout.Width(32)))
                    {
                        action.subActions.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
        public void DrawInspector_SubMenu(MenuActions.MenuAction action)
        {
            EditorGUILayout.BeginHorizontal();
            action.subMenu = (MenuActions)EditorGUILayout.ObjectField("Sub Menu", action.subMenu, typeof(MenuActions), false);
            EditorGUI.BeginDisabledGroup(action.subMenu != null);
            if (GUILayout.Button("New", GUILayout.Width(64f)))
            {
                //Create
                var subMenu = ScriptableObject.CreateInstance<MenuActions>();
                subMenu.name = $"Menu {action.name}";
                BaseActions.SaveAsset(subMenu, script, null, true);

                //Set
                action.subMenu = subMenu;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        public void DrawInspector_Slider(MenuActions.MenuAction action)
        {
            //Parameter
            action.parameter = DrawParameterDropDown(action.parameter, "Parameter");

            //Animations
            EditorGUI.BeginDisabledGroup(true); //Disable for now
            action.actionLayerAnimations.enter = DrawAnimationReference("Action Layer", action.actionLayerAnimations.enter, $"{action.name}_Action_Slider");
            EditorGUI.EndDisabledGroup();
            action.fxLayerAnimations.enter = DrawAnimationReference("FX Layer", action.fxLayerAnimations.enter, $"{action.name}_FX_Slider");
        }
    }
}
#endif