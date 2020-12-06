using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarGestures", menuName = "Tropical/AvatarGestures")]
public class AvatarGestures : AvatarActions
{
	public Action defaultAction;

	public AvatarGestures()
	{
		var action = new Action();
		action.name = "Default";
		action.gestureType.neutral = true;
		defaultAction = action;
	}
}