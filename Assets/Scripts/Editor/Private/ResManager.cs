using UnityEngine;
using UnityEditor;

namespace DMTimeArea
{
    public class ResManager
    {
        public static readonly string ASSET_PATH = "Assets/Scripts/Editor/EditorResources";

        public static GUIContent playContent = EditorGUIUtility.IconContent("Animation.Play", "|Play the Current active Shot.");
        public static GUIContent recordContent = EditorGUIUtility.IconContent("Animation.Record", "|Enable/disable keyframe recording mode.");
        public static GUIContent prevKeyContent = EditorGUIUtility.IconContent("Animation.PrevKey", "|Go to previous keyframe.");
        public static GUIContent nextKeyContent = EditorGUIUtility.IconContent("Animation.NextKey", "|Go to next keyframe.");
        public static GUIContent firstKeyContent = EditorGUIUtility.IconContent("Animation.FirstKey", "|Go to the beginning of the active Shot.");
        public static GUIContent lastKeyContent = EditorGUIUtility.IconContent("Animation.LastKey", "|Go to the end of the active Shot.");

        private static Texture _stopIcon;
        public static Texture StopIcon
        {
            get
            {
                if (_stopIcon == null)
                {
                    _stopIcon = EditorGUIUtility.Load($"{ASSET_PATH}/StopIcon.png") as Texture;
                }
                return _stopIcon;
            }
        }

        private static Texture _settingIcon;
        public static Texture SettingIcon
        {
            get
            {
                if (_settingIcon == null)
                {
                    _settingIcon = EditorGUIUtility.Load($"{ASSET_PATH}/SettingsIcon.png") as Texture;
                }
                return _settingIcon;
            }
        }

        private static Texture _cutOffGuideLine;
        private static Texture _timeHead;
        public static Texture CutOffGuideLineTexture
        {
            get
            {
                if (_cutOffGuideLine == null)
                    _cutOffGuideLine = EditorGUIUtility.Load($"{ASSET_PATH}/CutOffTimeCursor.png") as Texture;
                return _cutOffGuideLine;
            }
        }

        public static Texture TimeHeadTexture
        {
            get
            {
                if (_timeHead == null)
                    _timeHead = EditorGUIUtility.Load($"{ASSET_PATH}/Timecursor.png") as Texture;
                return _timeHead;
            }
        }
    }
}
