using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRCAvatarActions
{
    public abstract class NonMenuActions : AvatarActions
    {
        public abstract void Build(MenuActions.MenuAction parentAction);
    }
}