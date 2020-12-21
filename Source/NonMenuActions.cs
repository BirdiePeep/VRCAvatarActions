using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace VRCAvatarActions
{
    public abstract class NonMenuActions : BaseActions
    {
        public abstract void Build(MenuActions.MenuAction parentAction);
    }
}
#endif