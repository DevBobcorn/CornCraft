// Distant Lands 2022.


using DistantLands.Cozy.Data;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace DistantLands.Cozy
{
    [System.Serializable]
    public class CozyThunderManager : CozyFXModule
    {


        public Vector2 thunderTime = new Vector2();
        public GameObject thunderPrefab = null;
        private float thunderTimer = 0;
        public bool active = false;
        public List<ThunderFX> thunderFX = new List<ThunderFX>();
        List<ThunderFX> activeThunderFX = new List<ThunderFX>();
        public ThunderFX defaultFX = null;


        public override void OnFXEnable()
        {

            thunderTimer = Random.Range(thunderTime.x, thunderTime.y);
            defaultFX = (ThunderFX)Resources.Load("Default Thunder");

        }

        public override void SetupFXParent()
        {

            if (vfx.parent == null)
                return;

            parent = new GameObject().transform;
            parent.parent = vfx.parent;
            parent.localPosition = Vector3.zero;
            parent.localScale = Vector3.one;
            parent.name = "Thunder FX";
            parent.gameObject.AddComponent<FXParent>();

        }


        public override void OnFXUpdate()
        {

            if (!isEnabled)
                return;

            if (vfx == null)
                vfx = CozyWeather.instance.GetModule<VFXModule>();

            if (parent == null)
                SetupFXParent();
            else if (parent.parent == null)
                parent.parent = vfx.parent;

            parent.transform.localPosition = Vector3.zero;

            UpdateThunderTimes();



            if (active)
            {
                thunderTimer -= Time.deltaTime;

                if (thunderTimer <= 0)
                {
                    Strike();
                }
                if (thunderTimer > thunderTime.y)
                {
                    thunderTimer = thunderTime.y;
                }

            }
        }

        public void UpdateThunderTimes()
        {

            ThunderFX j = defaultFX;
            activeThunderFX.Clear();
            float total = 0;

            foreach (ThunderFX i in thunderFX)
            {
                if (i.weight > 0)
                {
                    activeThunderFX.Add(i);
                    total += i.weight;
                }
            }

            if (activeThunderFX.Count == 0)
            {
                active = false;
                thunderTime = new Vector2(61, 61);
                return;

            }

            j.weight = Mathf.Clamp01(1 - total);

            if (j.weight > 0)
            {
                activeThunderFX.Add(j);
                total += j.weight;
            }

            thunderTime = j.timeBetweenStrikes;

            foreach (ThunderFX i in activeThunderFX)
                thunderTime = new Vector2(Mathf.Lerp(thunderTime.x, i.timeBetweenStrikes.x, i.weight),
                    Mathf.Lerp(thunderTime.y, i.timeBetweenStrikes.y, i.weight));


            if (thunderTime == new Vector2(61, 61)) { active = false; return; }

            active = true;

        }

        public void Strike()
        {

            Transform i = MonoBehaviour.Instantiate(thunderPrefab, parent).transform;
            i.eulerAngles = Vector3.up * Random.value * 360;

            thunderTimer = Random.Range(thunderTime.x, thunderTime.y);

        }

        public override void OnFXDisable()
        {

            if (parent)
                MonoBehaviour.DestroyImmediate(parent.gameObject);

        }
    }


#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CozyThunderManager))]
    public class ThunderManagerDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            Rect pos = position;

            Rect tabPos = new Rect(pos.x + 35, pos.y, pos.width - 41, pos.height);
            Rect togglePos = new Rect(5, pos.y, 30, pos.height);

            property.FindPropertyRelative("_OpenTab").boolValue = EditorGUI.BeginFoldoutHeaderGroup(tabPos, property.FindPropertyRelative("_OpenTab").boolValue, new GUIContent("    Thunder FX", "Thunder FX control the rate at which lightning strikes during your weather profiles."), EditorUtilities.FoldoutStyle());

            bool toggle = EditorGUI.Toggle(togglePos, GUIContent.none, property.FindPropertyRelative("_IsEnabled").boolValue);

            if (property.FindPropertyRelative("_IsEnabled").boolValue != toggle)
            {
                property.FindPropertyRelative("_IsEnabled").boolValue = toggle;

                if (toggle == true)
                    (property.serializedObject.targetObject as VFXModule).thunderManager.OnFXEnable();
                else
                    (property.serializedObject.targetObject as VFXModule).thunderManager.OnFXDisable();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            if (property.FindPropertyRelative("_OpenTab").boolValue)
            {
                using (new EditorGUI.DisabledScope(!property.FindPropertyRelative("_IsEnabled").boolValue))
                {

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("thunderPrefab"));
                    EditorGUI.indentLevel++;

                    if (property.FindPropertyRelative("thunderPrefab").objectReferenceValue)
                        Editor.CreateEditor((property.FindPropertyRelative("thunderPrefab").objectReferenceValue as GameObject).GetComponent<CozyThunder>()).OnInspectorGUI();

                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;

                }

            }


            EditorGUI.EndProperty();
        }

    }
#endif
}