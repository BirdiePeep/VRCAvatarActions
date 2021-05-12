using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.Components;

#if UNITY_EDITOR
namespace VRCAvatarActions
{
    public class EditorBase : Editor
    {
        protected AvatarDescriptor avatarDescriptor;
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                InitStyles();

                //Avatar Descriptor
                SelectAvatarDescriptor();
                if (avatarDescriptor == null)
                {
                    EditorGUILayout.HelpBox("No active avatar descriptor found in scene.", MessageType.Error);
                }
                Divider();

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(avatarDescriptor == null);
                {
                    Inspector_Header();
                    Inspector_Body();
                }
                EditorGUI.EndDisabledGroup();
                if(EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(target);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
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

        public virtual void Inspector_Header()
        {
        }
        public virtual void Inspector_Body()
        {
        }

        protected const float SmallButtonSize = 48f;

        #region Parameters
        protected static string[] ParameterNames =
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

        public static string DrawParameterDropDown(string parameter, string label, VRCAvatarDescriptor avatarDescriptor)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (avatarDescriptor != null)
                {
                    //Dropdown
                    int currentIndex;
                    if (string.IsNullOrEmpty(parameter))
                    {
                        currentIndex = -1;
                    }
                    else
                    {
                        currentIndex = -2;
                        for (int i = 0; i < avatarDescriptor.GetExpressionParameterCount(); i++)
                        {
                            var item = avatarDescriptor.GetExpressionParameter(i);
                            if (item.name == parameter)
                            {
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
                                parameter = avatarDescriptor.GetExpressionParameter(currentIndex - 1).name;
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

            if (string.IsNullOrEmpty(parameter))
            {
                EditorGUILayout.HelpBox("Parameter required.", MessageType.Error);
            }

            return parameter;
        }
        protected string DrawParameterDropDown(string parameter, string label)
        {
            return DrawParameterDropDown(parameter, label, avatarDescriptor);
        }
        protected int GetExpressionParametersCount()
        {
            if (avatarDescriptor != null && avatarDescriptor.expressionParameters != null && avatarDescriptor.expressionParameters.parameters != null)
                return avatarDescriptor.expressionParameters.parameters.Length;
            return 0;
        }
        protected ExpressionParameters.Parameter GetExpressionParameter(int i)
        {
            if (avatarDescriptor != null)
                return avatarDescriptor.GetExpressionParameter(i);
            return null;
        }
        #endregion

        #region Styles
        protected GUIStyle boxUnselected;
        protected GUIStyle boxSelected;
        protected GUIStyle boxDisabled;
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
		#endregion

		#region Helper Methods
		public static void Divider()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        public static Texture2D MakeTex(int width, int height, Color col)
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
		#endregion
	}
}
#endif