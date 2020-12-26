#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;

namespace VRCAvatarActions
{
    [CreateAssetMenu(fileName = "Basic", menuName = "VRCAvatarActions/Other Actions/Basic")]
    public class BasicActions : NonMenuActions
    {
        [System.Serializable]
        public class GenericAction : Action
        {
            public override bool HasExit()
            {
                //Check for exit transition
                foreach(var trigger in triggers)
                {
                    if (trigger.type == Trigger.Type.Exit)
                        return true;
                }
                return false;
            }
        }
        public bool anyState = false;
        public List<GenericAction> actions = new List<GenericAction>();

        public override void GetActions(List<Action> output)
        {
            foreach (var action in actions)
                output.Add(action);
        }
        public override Action AddAction()
        {
            var result = new GenericAction();
            actions.Add(result);
            return result;
        }
        public override void RemoveAction(Action action)
        {
            actions.Remove(action as GenericAction);
        }
        public override void InsertAction(int index, Action action)
        {
            actions.Insert(index, action as GenericAction);
        }


        public override void Build(MenuActions.MenuAction parentAction)
        {
            BuildLayers(actions, AnimationLayer.Action, parentAction);
            BuildLayers(actions, AnimationLayer.FX, parentAction);
        }
        void BuildLayers(IEnumerable<GenericAction> sourceActions, AnimationLayer layerType, MenuActions.MenuAction parentAction)
        {
            //Build normal
            BuildGroupedLayers(sourceActions, layerType, parentAction,
            delegate (Action action)
            {
                if (!action.AffectsLayer(layerType))
                    return false;
                return true;
            },
            delegate (AnimatorController controller, string layerName, List<Action> actions)
            {
                //Name
                if (parentAction != null)
                    layerName = $"{parentAction.name}_{layerName}_SubActions";

                //Build layer
                if (layerType == AnimationLayer.Action)
                    BuildActionLayer(controller, actions, layerName, parentAction);
                else
                    BuildNormalLayer(controller, actions, layerName, layerType, parentAction);
            });
        }
    }

    [CustomEditor(typeof(BasicActions))]
    public class GenericActionsEditor : BaseActionsEditor
    {
        public override void Inspector_Header()
        {
            EditorGUILayout.HelpBox("Basic Actions - Actions with no default triggers.", MessageType.Info);
        }
    }
}
#endif