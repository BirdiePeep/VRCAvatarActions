#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using VRCAvatarActions;
using VRC.SDK3.Avatars.ScriptableObjects;

[CreateAssetMenu(fileName = "VisemeToVolume", menuName = "VRCAvatarActions/Special Actions/VisemeToVolume")]
public class VisemeToVolume : VRCAvatarActions.NonMenuActions
{
    public string parameter;
    public AnimationClip animationFx;

    public override void GetActions(List<Action> output) { }
    public override Action AddAction() { return null; }
    public override void RemoveAction(Action action) { }
    public override void InsertAction(int index, Action action) { }

    public override void Build(MenuActions.MenuAction parent)
    {
        var controller = GetController(AnimationLayer.FX);

        //Define volume param
        {
            var param = new VRCExpressionParameters.Parameter();
            param.name = parameter;
            param.valueType = VRCExpressionParameters.ValueType.Float;
            param.defaultValue = 0;
            param.saved = false;
            DefineExpressionParameter(param);
        }

        //Define parameters on controller
        AddParameter(controller, "Viseme", AnimatorControllerParameterType.Int, 0);
        AddParameter(controller, parameter, AnimatorControllerParameterType.Float, 0);

        BuildDriverLayer();
        BuildAnimationLayer();
    }
    void BuildDriverLayer()
    {
        var controller = GetController(AnimationLayer.FX);

        var layer = GetControllerLayer(controller, "VisimeVolumeDriver");
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
        layer.stateMachine.exitPosition = StatePosition(7, 0);

        int layerIndex = GetLayerIndex(controller, layer);

        for (int i = 0; i <= 100; i++)
        {
            //State
            var state = layer.stateMachine.AddState($"Volume_{i}", StatePosition(1, i));

            //Transition
            var transition = layer.stateMachine.AddAnyStateTransition(state);
            transition.canTransitionToSelf = false;
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.hasFixedDuration = true;
            transition.AddCondition(AnimatorConditionMode.Equals, (float)i, "Viseme");

            //Playable
            var driver = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver>();
            var param = new VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver.Parameter();
            param.name = parameter;
            param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
            param.value = (float)i * 0.01f;
            driver.parameters.Add(param);
        }
    }
    void BuildAnimationLayer()
    {
        var action = new MenuActions.MenuAction();
        action.menuType = MenuActions.MenuAction.MenuType.Slider;
        action.parameter = parameter;
        action.name = "VisimeAnimation";
        action.fxLayerAnimations.enter = animationFx;

        List<MenuActions.MenuAction> list = new List<MenuActions.MenuAction>();
        list.Add(action);
        MenuActions.BuildSliderLayer(list, AnimationLayer.FX, action.parameter);
    }
}

#endif