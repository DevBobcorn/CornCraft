using System;
using UnityEngine;
using System.Collections.Generic;
using DistantLands.Cozy.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [Serializable]
    public class CustomProperty
    {

        public enum Mode { interpolate, constant }
        public Mode mode = Mode.constant;

        [ColorUsage(true, true)]
        public Color colorVal = Color.white;
        [GradientUsage(true)]
        public Gradient gradientVal;
        public float floatVal = 1;
        public AnimationCurve curveVal = new AnimationCurve() { keys = new Keyframe[2] { new Keyframe(0, 1), new Keyframe(1, 1) } };
        public bool systemContainsProp = true;


        public void GetValue(out Color color, float time)
        {

            color = mode == Mode.constant ? colorVal : gradientVal.Evaluate(time);

        }
        public void GetValue(out float value, float time)
        {

            value = mode == Mode.constant ? floatVal : curveVal.Evaluate(time);

        }

        public Color GetColorValue(float time)
        {

            return mode == Mode.constant ? colorVal : gradientVal.Evaluate(time);

        }
        public float GetFloatValue(float time)
        {

            return mode == Mode.constant ? floatVal : curveVal.Evaluate(time);

        }
    }

    public class ProperyRelation
    {


        public CustomProperty property;


    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CozyPropertyTypeAttribute))]
    public class CustomPropertyDrawer : PropertyDrawer
    {

        bool color;
        float min;
        float max;
        CozyPropertyTypeAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (CozyPropertyTypeAttribute)attribute;
            if (_attribute.min != _attribute.max)
            {
                min = _attribute.min;
                max = _attribute.max;
            }
            color = _attribute.color;

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var unitRect = new Rect(position.x, position.y, position.width - 25, position.height);
            var dropdown = new Rect(position.x + (position.width - 20), position.y, 20, position.height);

            var mode = property.FindPropertyRelative("mode");
            var floatVal = property.FindPropertyRelative("floatVal");


            if (color)
            {
                if (mode.intValue == 0)
                    EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("gradientVal"), GUIContent.none);
                if (mode.intValue == 1)
                    EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("colorVal"), GUIContent.none);
            }
            else
            {
                if (mode.intValue == 0)
                    EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("curveVal"), GUIContent.none);
                if (mode.intValue == 1)
                    if (_attribute.min != _attribute.max)
                        EditorGUI.Slider(unitRect, floatVal, min, max, GUIContent.none);
                    else
                        EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("floatVal"), GUIContent.none);
            }
            EditorGUI.PropertyField(dropdown, property.FindPropertyRelative("mode"), GUIContent.none);


            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(MonthListAttribute))]
    public class MonthListDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {


            EditorGUI.BeginProperty(position, label, property);


            var rect1 = new Rect(position.x, position.y, 40, EditorGUIUtility.singleLineHeight);
            var rect2 = new Rect(position.x + 50, position.y, position.width / 2 - 54, EditorGUIUtility.singleLineHeight);
            var rect3 = new Rect(position.x + position.width / 2, position.y, position.width / 2 - 4, EditorGUIUtility.singleLineHeight);
            var rect4 = new Rect(position.x + position.width / 2 - 4, position.y, position.width / 2 - 4, EditorGUIUtility.singleLineHeight);

            var name = property.FindPropertyRelative("name");
            var days = property.FindPropertyRelative("days");

            EditorGUI.LabelField(rect1, "Month Name");
            EditorGUI.PropertyField(rect2, name, GUIContent.none);
            EditorGUI.PropertyField(rect3, days, new GUIContent(days.displayName));



            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineCount = 1.15f;

            return EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * 2f * (lineCount - 1);
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(ModulatedPropertyAttribute))]
    public class ModulatedPropertyDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {


            EditorGUI.BeginProperty(position, label, property);

            // Rect newPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), new GUIContent("Modulate From"));

            var indent = EditorGUI.indentLevel;
            // EditorGUI.indentLevel = 0;
            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float height = EditorGUIUtility.singleLineHeight;

            var titleRect = new Rect(position.x + 15, position.y, position.width, height);
            var unitARect = new Rect(position.x, position.y + space, (position.width / 2) - 3, height);
            var unitBRect = new Rect((position.width / 2) + 75, position.y + space, (position.width / 2), height);
            var unitCRect = new Rect(position.x + 30, position.y + space * 2, position.width - 30, height);
            var unitDRect = new Rect(position.x + 30, position.y + space * 3, position.width - 30, height);
            var unitERect = new Rect(position.x + 30, position.y + space * 4, position.width - 30, height);
            var source = property.FindPropertyRelative("modulationSource");
            var target = property.FindPropertyRelative("modulationTarget");


            property.FindPropertyRelative("expanded").boolValue = EditorGUI.Foldout(titleRect, property.FindPropertyRelative("expanded").boolValue, GetTitle(property), true);

            if (property.FindPropertyRelative("expanded").boolValue)
            {
                SerializedProperty map = null;
                if ((MaterialManagerProfile.ModulatedValue.ModulationTarget)target.enumValueIndex == Data.MaterialManagerProfile.ModulatedValue.ModulationTarget.globalValue ||
                (MaterialManagerProfile.ModulatedValue.ModulationTarget)target.enumValueIndex == Data.MaterialManagerProfile.ModulatedValue.ModulationTarget.materialValue)
                    map = property.FindPropertyRelative("mappedCurve");
                else
                    map = property.FindPropertyRelative("mappedGradient");

                var targetLayer = property.FindPropertyRelative("targetLayer");
                var targetMaterial = property.FindPropertyRelative("targetMaterial");
                var targetVariableName = property.FindPropertyRelative("targetVariableName");

                EditorGUI.PropertyField(unitARect, target, GUIContent.none);
                EditorGUI.PropertyField(unitBRect, source, GUIContent.none);
                EditorGUI.PropertyField(unitCRect, map);

                List<string> names = new List<string>();
                int selected = -1;

                if ((MaterialManagerProfile.ModulatedValue.ModulationTarget)target.enumValueIndex == MaterialManagerProfile.ModulatedValue.ModulationTarget.materialValue)
                {
                    if (property.FindPropertyRelative("targetMaterial").objectReferenceValue)
                    {
                        for (int i = 0; i < (targetMaterial.objectReferenceValue as Material).shader.GetPropertyCount(); i++)
                            if ((targetMaterial.objectReferenceValue as Material).shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Float)
                                names.Add((targetMaterial.objectReferenceValue as Material).shader.GetPropertyName(i));


                        if (names.Contains(targetVariableName.stringValue))
                            selected = names.IndexOf(targetVariableName.stringValue);
                        else
                            selected = 0;
                    }
                }
                else if ((MaterialManagerProfile.ModulatedValue.ModulationTarget)target.enumValueIndex == MaterialManagerProfile.ModulatedValue.ModulationTarget.materialColor)
                {
                    if (property.FindPropertyRelative("targetMaterial").objectReferenceValue)
                    {
                        for (int i = 0; i < (targetMaterial.objectReferenceValue as Material).shader.GetPropertyCount(); i++)
                            if ((targetMaterial.objectReferenceValue as Material).shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
                                names.Add((targetMaterial.objectReferenceValue as Material).shader.GetPropertyName(i));


                        if (names.Contains(targetVariableName.stringValue))
                            selected = names.IndexOf(targetVariableName.stringValue);
                        else
                            selected = 0;
                    }
                }

                switch ((MaterialManagerProfile.ModulatedValue.ModulationTarget)target.enumValueIndex)
                {
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalColor):
                        EditorGUI.PropertyField(unitDRect, property.FindPropertyRelative("targetVariableName"), new GUIContent("Global Color Property Name", "The name of the global shader property to set."));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalValue):
                        EditorGUI.PropertyField(unitDRect, property.FindPropertyRelative("targetVariableName"), new GUIContent("Global Value Property Name", "The name of the global shader property to set."));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialColor):
                        EditorGUI.PropertyField(unitDRect, targetMaterial);
                        if (names.Count > 0)
                            property.FindPropertyRelative("targetVariableName").stringValue = names[EditorGUI.Popup(unitERect, "Material Value Property Name", selected, names.ToArray())];
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialValue):
                        EditorGUI.PropertyField(unitDRect, targetMaterial);
                        if (names.Count > 0)
                            property.FindPropertyRelative("targetVariableName").stringValue = names[EditorGUI.Popup(unitERect, "Material Value Property Name", selected, names.ToArray())];
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.terrainLayerColor):
                        EditorGUI.PropertyField(unitDRect, targetLayer);
                        break;
                }
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        string GetTitle(SerializedProperty property)
        {

            int value = property.FindPropertyRelative("modulationTarget").intValue;

            switch ((MaterialManagerProfile.ModulatedValue.ModulationTarget)value)
            {
                case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalColor):
                    if (property.FindPropertyRelative("targetVariableName").stringValue != "")
                        return $"   {property.FindPropertyRelative("targetVariableName").stringValue}";
                    else
                        return "   Global Shader Color";
                case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalValue):
                    if (property.FindPropertyRelative("targetVariableName").stringValue != "")
                        return $"   {property.FindPropertyRelative("targetVariableName").stringValue}";
                    else
                        return "   Global Shader Value";
                case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialColor):
                    if (property.FindPropertyRelative("targetMaterial").objectReferenceValue)
                        return $"   {property.FindPropertyRelative("targetMaterial").objectReferenceValue.name}";
                    else
                        return "   Local Material Color";
                case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialValue):
                    if (property.FindPropertyRelative("targetMaterial").objectReferenceValue)
                        return $"   {property.FindPropertyRelative("targetMaterial").objectReferenceValue.name}";
                    else
                        return "   Local Material Value";
                case (MaterialManagerProfile.ModulatedValue.ModulationTarget.terrainLayerColor):
                    if (property.FindPropertyRelative("targetLayer").objectReferenceValue)
                        return $"   {property.FindPropertyRelative("targetLayer").objectReferenceValue.name}";
                    else
                        return "   Terrain Layer Color";

            }

            return "";

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineCount = 1.25f;

            if (property.FindPropertyRelative("expanded").boolValue)
                lineCount += 3.5f;

            return EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * 2f * (lineCount - 1);
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(FormatTimeAttribute))]
    public class FormattedTimeDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float div = position.width / 3;

            var hoursRect = new Rect(position.x, position.y, div - 10, position.height);
            var minutesRect = new Rect(position.x + div, position.y, div - 10, position.height);
            var meridiemRect = new Rect(position.x + div * 2, position.y, div - 10, position.height);

            var hours = property.FindPropertyRelative("hours");
            var minutes = property.FindPropertyRelative("minutes");
            var meridiem = property.FindPropertyRelative("meridiem");


            hours.intValue = Mathf.Clamp(EditorGUI.IntField(hoursRect, GUIContent.none, hours.intValue), 0, 12);
            if (hours.intValue == 0)
            {
                hours.intValue = 12;
                meridiem.intValue = meridiem.intValue == 1 ? 0 : 1;
            }
            minutes.intValue = Mathf.Clamp(EditorGUI.IntField(minutesRect, GUIContent.none, minutes.intValue), 0, 59);
            EditorGUI.PropertyField(meridiemRect, meridiem, GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }


    [UnityEditor.CustomPropertyDrawer(typeof(FXAttribute))]
    public class FXDrawer : PropertyDrawer
    {

        string title;
        FXAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (FXAttribute)attribute;
            title = _attribute.title;

            float height = EditorGUIUtility.singleLineHeight;
            var unitARect = new Rect(position.x, position.y, position.width, height);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(unitARect, property, GUIContent.none);

            position = new Rect(position.x + 30, position.y, position.width - 30, position.height);

            if (property.objectReferenceValue != null)
            {
                FXProfile profile = (FXProfile)property.objectReferenceValue;
                (Editor.CreateEditor(profile) as E_FXProfile).RenderInWindow(position);

            }


            // if (title != "")
            //     EditorGUI.PropertyField(position, property, GUIContent.none);
            // else
            //     EditorGUI.PropertyField(position, property, new GUIContent(title));


            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineCount = 1;

            if (property.objectReferenceValue != null)
            {
                FXProfile profile = (FXProfile)property.objectReferenceValue;
                lineCount += 0.5f + (Editor.CreateEditor(profile) as E_FXProfile).GetLineHeight();

            }
            return EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);
        }

    }

    [UnityEditor.CustomPropertyDrawer(typeof(HideTitleAttribute))]
    public class HideTitleDrawer : PropertyDrawer
    {

        string title;
        HideTitleAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (HideTitleAttribute)attribute;
            title = _attribute.title;
            EditorGUI.BeginProperty(position, label, property);

            if (title != "")
                EditorGUI.PropertyField(position, property, GUIContent.none);
            else
                EditorGUI.PropertyField(position, property, new GUIContent(title));


            EditorGUI.EndProperty();
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(SetHeightAttribute))]
    public class SetHeightDrawer : PropertyDrawer
    {

        int lines;
        SetHeightAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (SetHeightAttribute)attribute;
            lines = _attribute.lines;
            position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * lines);
            EditorGUI.BeginProperty(position, label, property);


            EditorGUI.PropertyField(position, property, label);


            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _attribute = (SetHeightAttribute)attribute;
            lines = _attribute.lines;

            return EditorGUIUtility.singleLineHeight * lines;
        }

    }

   
    [UnityEditor.CustomPropertyDrawer(typeof(DisplayHorizontallyAttribute))]
    public class DisplayHorizontallyDrawer : PropertyDrawer
    {

        DisplayHorizontallyAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (DisplayHorizontallyAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(position, label);


            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (property.CountInProperty() + 2);
        }

    }


    [UnityEditor.CustomPropertyDrawer(typeof(MultiAudioAttribute))]
    public class MultiAudioDrawer : PropertyDrawer
    {

        MultiAudioAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (MultiAudioAttribute)attribute;
            int preset = -1;

            EditorGUI.BeginProperty(position, label, property);

            var titleRect = new Rect(position.x, position.y, 150, position.height);
            var unitRect = new Rect(position.x + 157, position.y, position.width - 185, position.height);
            var dropdown = new Rect(position.x + (position.width - 20), position.y, 20, position.height);



            List<AnimationCurve> presets = new List<AnimationCurve>() { new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.2f, 1), new Keyframe(0.25f, 0), new Keyframe(0.75f, 0), new Keyframe(0.8f, 1), new Keyframe(1, 1)),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 0), new Keyframe(0.25f, 1), new Keyframe(0.75f, 1), new Keyframe(0.8f, 0), new Keyframe(1, 0)),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.18f, 0), new Keyframe(0.25f, 1), new Keyframe(0.35f, 0), new Keyframe(0.7f, 0), new Keyframe(0.75f, 1), new Keyframe(0.85f, 0), new Keyframe(1, 0)),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.70f, 0), new Keyframe(0.8f, 1), new Keyframe(0.85f, 0), new Keyframe(1, 0)),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.18f, 0), new Keyframe(0.22f, 1), new Keyframe(0.3f, 0), new Keyframe(1, 0))};
            List<GUIContent> presetNames = new List<GUIContent>() { new GUIContent("Plays at night"), new GUIContent("Plays during the day"),
            new GUIContent("Plays in the evening & morning"), new GUIContent("Plays in the evening"), new GUIContent("Plays in the morning")};

            EditorGUI.PropertyField(titleRect, property.FindPropertyRelative("FX"), GUIContent.none);
            EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("intensityCurve"), GUIContent.none);

            preset = EditorGUI.Popup(dropdown, GUIContent.none, -1, presetNames.ToArray());

            if (preset != -1)
                property.FindPropertyRelative("intensityCurve").animationCurveValue = presets[preset];



            EditorGUI.EndProperty();
        }
    }
    [UnityEditor.CustomPropertyDrawer(typeof(TransitionTimeAttribute))]
    public class TransitionTimeDrawer : PropertyDrawer
    {

        TransitionTimeAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (TransitionTimeAttribute)attribute;
            int preset = -1;

            EditorGUI.BeginProperty(position, label, property);

            var unitRect = new Rect(position.x, position.y, position.width - 25, position.height);
            var dropdown = new Rect(position.x + (position.width - 20), position.y, 20, position.height);



            List<AnimationCurve> presets = new List<AnimationCurve>()
            {
                new AnimationCurve (new Keyframe(0, 0, 1, 1), new Keyframe (1, 1, 1, 1)),
                new AnimationCurve (new Keyframe(0, 0, 0, 0), new Keyframe (1, 1, 2, -2)),
                new AnimationCurve (new Keyframe(0, 0, 2, 2), new Keyframe (1, 1, 0, 0)),
                new AnimationCurve (new Keyframe(0, 0, 0, 0), new Keyframe (1, 1, 3.25f, -3.25f)),
                new AnimationCurve (new Keyframe(0, 0, 3.25f, 3.25f), new Keyframe (1, 1, 0, 0)),
                new AnimationCurve (new Keyframe(0, 0, 0, 0), new Keyframe (1, 1, 0, 0)),
                new AnimationCurve (new Keyframe(0, 0, 3, 3), new Keyframe (1, 1, 3, 3))
            };

            List<GUIContent> presetNames = new List<GUIContent>()
            {
                new GUIContent("Linear"),
                new GUIContent("Exponential"),
                new GUIContent("Inverse Exponential"),
                new GUIContent("Steep Exponential"),
                new GUIContent("Steep Inverse Exponential"),
                new GUIContent("Smooth"),
                new GUIContent("Slerped"),
            };

            EditorGUI.PropertyField(unitRect, property, label);

            preset = EditorGUI.Popup(dropdown, GUIContent.none, -1, presetNames.ToArray());

            if (preset != -1)
                property.animationCurveValue = presets[preset];



            EditorGUI.EndProperty();
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(WeightedWeatherAttribute))]
    public class WeightedWeatherDrawer : PropertyDrawer
    {


        WeightedWeatherAttribute _attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            _attribute = (WeightedWeatherAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            var titleRect = new Rect(position.x, position.y, 150, position.height);
            var unitRect = new Rect(position.x + 157, position.y, position.width - 155, position.height);


            EditorGUI.PropertyField(titleRect, property.FindPropertyRelative("profile"), GUIContent.none);
            EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("weight"), GUIContent.none);





            EditorGUI.EndProperty();
        }
    }

#endif
    public class CozyPropertyTypeAttribute : PropertyAttribute
    {
        public bool color;
        public float min;
        public float max;


        public CozyPropertyTypeAttribute()
        {

            color = false;

        }

        public CozyPropertyTypeAttribute(bool isColorType)
        {

            color = isColorType;

        }
        public CozyPropertyTypeAttribute(bool isColorType, float min, float max)
        {

            color = isColorType;
            this.min = min;
            this.max = max;

        }
    }

    public class FXAttribute : PropertyAttribute
    {
        public string title;


        public FXAttribute()
        {

            title = "";

        }

        public FXAttribute(string _title)
        {

            title = _title;

        }
    }

    public class HideTitleAttribute : PropertyAttribute
    {
        public string title;
        public float lines;


        public HideTitleAttribute()
        {

            title = "";
            lines = 1;

        }
        public HideTitleAttribute(float _lines)
        {

            title = "";
            lines = _lines;

        }

        public HideTitleAttribute(string _title, float _lines)
        {

            title = _title;
            lines = _lines;

        }
    }

    public class DisplayHorizontallyAttribute : PropertyAttribute
    {

        public string key;

        public DisplayHorizontallyAttribute(string _Key)
        {

            key = _Key;

        }
    }

    public class MonthListAttribute : PropertyAttribute
    {
        public MonthListAttribute()
        {


        }
    }



    public class SetHeightAttribute : PropertyAttribute
    {
        public int lines;


        public SetHeightAttribute()
        {

            lines = 1;

        }

        public SetHeightAttribute(int _lines)
        {

            lines = _lines;

        }
    }

    public class FormatTimeAttribute : PropertyAttribute
    {

        public FormatTimeAttribute()
        {


        }

    }

    public class ModulatedPropertyAttribute : PropertyAttribute
    {
        public ModulatedPropertyAttribute()
        {

        }
    }
    public class TransitionTimeAttribute : PropertyAttribute
    {
        public TransitionTimeAttribute()
        {

        }
    }

    public class WeightedWeatherAttribute : PropertyAttribute
    {


        public WeightedWeatherAttribute()
        {


        }

    }


    public class MultiAudioAttribute : PropertyAttribute
    {

        public MultiAudioAttribute()
        {


        }

    }

}