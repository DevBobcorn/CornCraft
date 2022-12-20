// Distant Lands 2022.



using System.Collections.Generic;
#if UNITY_EDITOR 
using UnityEditor;
#endif
using UnityEngine;



namespace DistantLands.Cozy.Data
{

    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Perennial Profile", order = 361)]
    public class PerennialProfile : ClimateProfile
    {

        [Tooltip("Specifies the current ticks.")]
        public float currentTicks;
        [Tooltip("Specifies the current day.")]
        public int currentDay;
        [Tooltip("Specifies the current year.")]
        public int currentYear;
        [HideInInspector]
        public float dayAndTime;
        public bool pauseTime;
        [Tooltip("Should this profile use a series of months for a more realistic year.")]
        public bool realisticYear;
        [Tooltip("Should this profile use a longer year every 4th year.")]
        public bool useLeapYear;

        [Tooltip("Should this system reset the ticks when it loads or should it pull the current ticks from the scriptable object?")]
        public bool resetTicksOnStart = false;
        [Tooltip("The ticks that this system should start at when the scene is loaded.")]
        public float startTicks = 120;

        [Tooltip("Specifies the maximum amount of ticks per day.")]
        public float ticksPerDay = 360;
        [Tooltip("Specifies the amount of ticks that passs in a second.")]
        public float tickSpeed = 1;
        [Tooltip("Changes tick speed based on the percentage of the day.")]
        public AnimationCurve tickSpeedMultiplier;

        [System.Serializable]
        public struct TimeWeightRelation
        {
            [Range(0, 1)] public float time; [Range(0, 360)] public float sunHeight; [Range(0, 1)] public float weight;

            public TimeWeightRelation(float time, float sunHeight, float weight)
            {
                this.time = time;
                this.sunHeight = sunHeight;
                this.weight = weight;
            }
        }


        [Tooltip("Specifies the amount of ticks that passs in a second.")]
        public TimeWeightRelation sunriseWeight = new TimeWeightRelation(0.25f, 90, 0.2f);
        [Tooltip("Specifies the amount of ticks that passs in a second.")]
        public TimeWeightRelation dayWeight = new TimeWeightRelation(0.5f, 180, 0.2f);
        [Tooltip("Specifies the amount of ticks that passs in a second.")]
        public TimeWeightRelation sunsetWeight = new TimeWeightRelation(0.75f, 270, 0.2f);
        [Tooltip("Specifies the amount of ticks that passs in a second.")]
        public TimeWeightRelation nightWeight = new TimeWeightRelation(1, 360, 0.2f);

        [HideInInspector]
        public AnimationCurve sunMovementCurve;

        [HideTitle]
        public AnimationCurve displayCurve;

        [System.Serializable]
        public class Month
        {

            public string name;
            public int days;

        }

        [MonthList]
        public Month[] standardYear = new Month[12] { new Month() { days = 31, name = "January"}, new Month() { days = 28, name = "Febraury" },
        new Month() { days = 31, name = "March"}, new Month() { days = 30, name = "April"}, new Month() { days = 31, name = "May"},
        new Month() { days = 30, name = "June"}, new Month() { days = 31, name = "July"}, new Month() { days = 31, name = "August"},
        new Month() { days = 30, name = "September"}, new Month() { days = 31, name = "October"}, new Month() { days = 30, name = "Novemeber"},
        new Month() { days = 31, name = "December"}};

        [MonthList]
        public Month[] leapYear = new Month[12] { new Month() { days = 31, name = "January"}, new Month() { days = 29, name = "Febraury" },
        new Month() { days = 31, name = "March"}, new Month() { days = 30, name = "April"}, new Month() { days = 31, name = "May"},
        new Month() { days = 30, name = "June"}, new Month() { days = 31, name = "July"}, new Month() { days = 31, name = "August"},
        new Month() { days = 30, name = "September"}, new Month() { days = 31, name = "October"}, new Month() { days = 30, name = "Novemeber"},
        new Month() { days = 31, name = "December"}};

        public enum DefaultYear { January, February, March, April, May, June, July, August, September, October, November, December }
        public enum TimeDivisors { Early, Mid, Late }

        public enum TimeCurveSettings { linearDay, simpleCurve, advancedCurve }
        public TimeCurveSettings timeCurveSettings;

        public int daysPerYear = 48;

        public void GetModifiedDayPercent()
        {

            switch (timeCurveSettings)
            {

                case (TimeCurveSettings.advancedCurve):
                    sunMovementCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, nightWeight.weight, nightWeight.weight),
                new Keyframe(sunriseWeight.time, sunriseWeight.sunHeight, 0, 0, sunriseWeight.weight, sunriseWeight.weight),
                new Keyframe(dayWeight.time, dayWeight.sunHeight, 0, 0, dayWeight.weight, dayWeight.weight),
                new Keyframe(sunsetWeight.time, sunsetWeight.sunHeight, 0, 0, sunsetWeight.weight, sunsetWeight.weight),
                new Keyframe(1, sunsetWeight.sunHeight > dayWeight.sunHeight ? 360 : 0, 0, 0, nightWeight.weight, nightWeight.weight)
                    });

                    displayCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, nightWeight.weight, nightWeight.weight),
                new Keyframe(sunriseWeight.time, sunriseWeight.sunHeight, 0, 0, sunriseWeight.weight, sunriseWeight.weight),
                new Keyframe(dayWeight.time, dayWeight.sunHeight, 0, 0, dayWeight.weight, dayWeight.weight),
                new Keyframe(sunsetWeight.time, sunsetWeight.sunHeight > 180 ? 360 - sunsetWeight.sunHeight : sunsetWeight.sunHeight, 0, 0, sunsetWeight.weight, sunsetWeight.weight),
                new Keyframe(1, 0, 0, 0, nightWeight.weight, nightWeight.weight)
                    });
                    break;

                case (TimeCurveSettings.simpleCurve):
                    sunMovementCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, nightWeight.weight, nightWeight.weight),
                new Keyframe(0.25f, 90f, 0, 0, sunriseWeight.weight, sunriseWeight.weight),
                new Keyframe(0.5f, 180f, 0, 0, dayWeight.weight, dayWeight.weight),
                new Keyframe(0.75f, 270f, 0, 0, sunsetWeight.weight, sunsetWeight.weight),
                new Keyframe(1, 360, 0, 0, nightWeight.weight, nightWeight.weight)
                    });

                    displayCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, nightWeight.weight, nightWeight.weight),
                new Keyframe(0.25f, 90f, 0, 0, sunriseWeight.weight, sunriseWeight.weight),
                new Keyframe(0.5f, 180f, 0, 0, dayWeight.weight, dayWeight.weight),
                new Keyframe(0.75f, 90, 0, 0, sunsetWeight.weight, sunsetWeight.weight),
                new Keyframe(1, 0, 0, 0, nightWeight.weight, nightWeight.weight)
                    });
                    break;

                case (TimeCurveSettings.linearDay):
                    sunMovementCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, 0, 0),
                new Keyframe(0.25f, 90, 0, 0, 0, 0),
                new Keyframe(0.5f, 180, 0, 0, 0, 0),
                new Keyframe(0.75f, 270, 0, 0, 0, 0),
                new Keyframe(1, 360, 0, 0, 0, 0)
                    });

                    displayCurve = new AnimationCurve(new Keyframe[5]
                    {
                new Keyframe(0, 0, 0, 0, 0, 0),
                new Keyframe(0.25f, 90, 0, 0, 0, 0),
                new Keyframe(0.5f, 180, 0, 0, 0, 0),
                new Keyframe(0.75f, 90, 0, 0, 0, 0),
                new Keyframe(1, 0, 0, 0, 0, 0)
                    });
                    break;

            }


        }


        /// <summary>
        /// Returns the formatted time at a certain tick value.  
        /// <param name="militaryTime">Should the time be formatted in military time (24 hour day)? </param>
        /// <param name="ticks">The number of ticks  </param>
        /// </summary> 
        public string FormatTime(bool militaryTime, float ticks)
        {

            float time = ticks / ticksPerDay;

            int minutes = Mathf.RoundToInt(time * 1440);
            int hours = (minutes - minutes % 60) / 60;
            minutes -= hours * 60;

            if (militaryTime)
                return "" + hours.ToString("D2") + ":" + minutes.ToString("D2");
            else if (hours == 0)
                return "" + 12 + ":" + minutes.ToString("D2") + "AM";
            else if (hours == 12)
                return "" + 12 + ":" + minutes.ToString("D2") + "PM";
            else if (hours > 12)
                return "" + (hours - 12) + ":" + minutes.ToString("D2") + "PM";
            else
                return "" + (hours) + ":" + minutes.ToString("D2") + "AM";

        }

        public float ModifiedTickSpeed()
        {

            return tickSpeed * tickSpeedMultiplier.Evaluate(currentTicks / ticksPerDay);

        }

        public int RealisticDaysPerYear()
        {

            int i = 0;
            foreach (Month j in (useLeapYear && currentYear % 4 == 0) ? leapYear : standardYear) i += j.days;
            return i;


        }


    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PerennialProfile))]
    [CanEditMultipleObjects]
    public class E_PerennialProfile : Editor
    {

        SerializedProperty tickSpeedMultiplier;
        SerializedProperty standardYear;
        SerializedProperty leapYear;
        PerennialProfile prof;

        void OnEnable()
        {

            tickSpeedMultiplier = serializedObject.FindProperty("tickSpeedMultiplier");
            standardYear = serializedObject.FindProperty("standardYear");
            leapYear = serializedObject.FindProperty("leapYear");
            prof = (PerennialProfile)target;

        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Undo.RecordObject(prof, prof.name + " Profile Changes");

            EditorGUILayout.LabelField("Current Settings", EditorStyles.boldLabel);
            prof.currentTicks = EditorGUILayout.Slider("Current Ticks", prof.currentTicks, 0, prof.ticksPerDay);
            prof.currentDay = EditorGUILayout.IntSlider("Current Day", prof.currentDay, 0, prof.daysPerYear);
            prof.currentYear = EditorGUILayout.IntField("Current Year", prof.currentYear);
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Time Movement", EditorStyles.boldLabel);
            prof.pauseTime = EditorGUILayout.Toggle("Pause Time", prof.pauseTime);
            if (!prof.pauseTime)
            {
                prof.tickSpeed = EditorGUILayout.FloatField("Tick Speed", prof.tickSpeed);
                EditorGUILayout.PropertyField(tickSpeedMultiplier);
            }
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Length Settings", EditorStyles.boldLabel);
            prof.ticksPerDay = EditorGUILayout.FloatField("Ticks Per Day", prof.ticksPerDay);
            EditorGUILayout.Space(10);
            prof.realisticYear = EditorGUILayout.Toggle("Realistic Year", prof.realisticYear);
            if (prof.realisticYear)
            {
                prof.useLeapYear = EditorGUILayout.Toggle("Use Leap Year", prof.useLeapYear);

                EditorGUILayout.Space(10);
                EditorGUILayout.PropertyField(standardYear);
                if (prof.useLeapYear)
                    EditorGUILayout.PropertyField(leapYear);
            }
            else
            {

                prof.daysPerYear = EditorGUILayout.IntField("Days Per Year", prof.daysPerYear);

            }

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Sun Movement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeCurveSettings"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunriseWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dayWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunsetWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nightWeight"));
            prof.GetModifiedDayPercent();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayCurve"));

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prof);

        }

        public void OnStaticMeasureGUI(GUIStyle style, ref bool lengthWindow, ref bool movementWindow, ref bool curveWindow)
        {

            serializedObject.Update();


            movementWindow = EditorGUILayout.BeginFoldoutHeaderGroup(movementWindow,
                new GUIContent("    Movement Settings"), EditorUtilities.FoldoutStyle());

            if (movementWindow)
            {
                EditorGUI.indentLevel++;





                EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseTime"));
                if (!serializedObject.FindProperty("pauseTime").boolValue)
                {

                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("resetTicksOnStart"));
                    if (serializedObject.FindProperty("resetTicksOnStart").boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startTicks"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tickSpeed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tickSpeedMultiplier"));
                    EditorGUI.indentLevel--;


                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            lengthWindow = EditorGUILayout.BeginFoldoutHeaderGroup(lengthWindow,
                new GUIContent("    Length Settings"), EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (lengthWindow)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("ticksPerDay"));
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("realisticYear"));
                if (serializedObject.FindProperty("realisticYear").boolValue)
                {


                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useLeapYear"));
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("standardYear"));

                    if (serializedObject.FindProperty("useLeapYear").boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("leapYear"));

                }
                else
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("daysPerYear"));

                EditorGUI.indentLevel--;
            }

            curveWindow = EditorGUILayout.BeginFoldoutHeaderGroup(curveWindow,
                new GUIContent("    Curve Settings"), EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (curveWindow)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeCurveSettings"));
                EditorGUILayout.Space();
                prof.GetModifiedDayPercent();

                EditorGUI.indentLevel++;

                switch ((PerennialProfile.TimeCurveSettings)serializedObject.FindProperty("timeCurveSettings").enumValueIndex)
                {

                    case (PerennialProfile.TimeCurveSettings.linearDay):
                        break;
                    case (PerennialProfile.TimeCurveSettings.simpleCurve):
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sunriseWeight").FindPropertyRelative("weight"), new GUIContent("Sunrise Weight"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("dayWeight").FindPropertyRelative("weight"), new GUIContent("Day Weight"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sunsetWeight").FindPropertyRelative("weight"), new GUIContent("Sunset Weight"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("nightWeight").FindPropertyRelative("weight"), new GUIContent("Night Weight"));
                        break;
                    case (PerennialProfile.TimeCurveSettings.advancedCurve):
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sunriseWeight"), new GUIContent("Sunrise Settings"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("dayWeight"), new GUIContent("Day Settings"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sunsetWeight"), new GUIContent("Sunset Settings"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("nightWeight"), new GUIContent("Night Settings"));
                        break;
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("displayCurve"));

                EditorGUI.indentLevel--;
            }


            serializedObject.ApplyModifiedProperties();

        }

        public void OnRuntimeMeasureGUI()
        {

            serializedObject.Update();


            serializedObject.FindProperty("currentTicks").floatValue = EditorGUILayout.Slider("Current Ticks", serializedObject.FindProperty("currentTicks").floatValue, 0, serializedObject.FindProperty("ticksPerDay").floatValue);
            serializedObject.FindProperty("currentDay").intValue = EditorGUILayout.IntSlider(new GUIContent("Current Day"), serializedObject.FindProperty("currentDay").intValue, 0, serializedObject.FindProperty("daysPerYear").intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentYear"));

            serializedObject.ApplyModifiedProperties();

        }

    }
#endif
}