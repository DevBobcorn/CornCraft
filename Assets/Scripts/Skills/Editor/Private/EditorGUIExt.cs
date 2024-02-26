using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace DMTimeArea
{
    public class EditorGUIExt
    {
        private class Styles
        {
            public GUIStyle selectionRect = "SelectionRect";
        }

        private class MinMaxSliderState
        {
            public float dragStartPos = 0f;
            public float dragStartValue = 0f;
            public float dragStartSize = 0f;
            public float dragStartValuesPerPixel = 0f;
            public float dragStartLimit = 0f;
            public float dragEndLimit = 0f;
            public int whereWeDrag = -1;
        }
        private enum DragSelectionState
        {
            None,
            DragSelecting,
            Dragging
        }
        private static EditorGUIExt.Styles ms_Styles = new EditorGUIExt.Styles();
        private static int repeatButtonHash = "repeatButton".GetHashCode();
        private static float nextScrollStepTime = 0f;
        private static int firstScrollWait = 250;
        private static int scrollWait = 30;
        private static int scrollControlID;
        private static MinMaxSliderState s_MinMaxSliderState;
        private static int kFirstScrollWait = 250;
        private static int kScrollWait = 30;
        private static DateTime s_NextScrollStepTime = DateTime.Now;

        internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();
        private static bool adding = false;
        private static bool[] initSelections;
        private static int initIndex = 0;
        private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
        {
            int controlID = GUIUtility.GetControlID(repeatButtonHash, focusType, position);
            EventType typeForControl = Event.current.GetTypeForControl(controlID);
            bool result;
            if (typeForControl != EventType.MouseDown)
            {
                if (typeForControl != EventType.MouseUp)
                {
                    if (typeForControl != EventType.Repaint)
                    {
                        result = false;
                    }
                    else
                    {
                        style.Draw(position, content, controlID);
                        result = (controlID == GUIUtility.hotControl && position.Contains(Event.current.mousePosition));
                    }
                }
                else
                {
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        result = position.Contains(Event.current.mousePosition);
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            else
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                result = false;
            }
            return result;
        }
        private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool result = false;
            if (EditorGUIExt.DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool flag = EditorGUIExt.scrollControlID != scrollerID;
                EditorGUIExt.scrollControlID = scrollerID;
                if (flag)
                {
                    result = true;
                    EditorGUIExt.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)EditorGUIExt.firstScrollWait;
                }
                else
                {
                    if (Time.realtimeSinceStartup >= EditorGUIExt.nextScrollStepTime)
                    {
                        result = true;
                        EditorGUIExt.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)EditorGUIExt.scrollWait;
                    }
                }
                if (Event.current.type == EventType.Repaint)
                {
                    HandleUtility.Repaint();
                }
            }
            return result;
        }
        public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            float num;
            if (horiz)
            {
                num = size * 10f / position.width;
            }
            else
            {
                num = size * 10f / position.height;
            }
            Rect position2;
            Rect rect;
            Rect rect2;
            if (horiz)
            {
                position2 = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
                rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                position2 = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
                rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }
            float num2 = Mathf.Min(visualStart, value);
            float num3 = Mathf.Max(visualEnd, value + size);
            EditorGUIExt.MinMaxSlider(position2, ref value, ref size, num2, num3, num2, num3, slider, thumb, horiz);
            bool flag = false;
            if (Event.current.type == EventType.MouseUp)
            {
                flag = true;
            }
            if (EditorGUIExt.ScrollerRepeatButton(id, rect, leftButton))
            {
                value -= num * ((visualStart >= visualEnd) ? -1f : 1f);
            }
            if (EditorGUIExt.ScrollerRepeatButton(id, rect2, rightButton))
            {
                value += num * ((visualStart >= visualEnd) ? -1f : 1f);
            }
            if (flag && Event.current.type == EventType.Used)
            {
                EditorGUIExt.scrollControlID = 0;
            }
            if (startLimit < endLimit)
            {
                value = Mathf.Clamp(value, startLimit, endLimit - size);
            }
            else
            {
                value = Mathf.Clamp(value, endLimit, startLimit - size);
            }
        }
        public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
        {
            EditorGUIExt.DoMinMaxSlider(position, GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
        }
        internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
        {
            Event current = Event.current;
            bool flag = size == 0f;
            float num = Mathf.Min(visualStart, visualEnd);
            float num2 = Mathf.Max(visualStart, visualEnd);
            float num3 = Mathf.Min(startLimit, endLimit);
            float num4 = Mathf.Max(startLimit, endLimit);
            EditorGUIExt.MinMaxSliderState minMaxSliderState = EditorGUIExt.s_MinMaxSliderState;
            if (GUIUtility.hotControl == id && minMaxSliderState != null)
            {
                num = minMaxSliderState.dragStartLimit;
                num3 = minMaxSliderState.dragStartLimit;
                num2 = minMaxSliderState.dragEndLimit;
                num4 = minMaxSliderState.dragEndLimit;
            }
            float num5 = 0f;
            float num6 = Mathf.Clamp(value, num, num2);
            float num7 = Mathf.Clamp(value + size, num, num2) - num6;
            float num8 = (float)((visualStart <= visualEnd) ? 1 : -1);
            if (slider != null && thumb != null)
            {
                float num10;
                Rect position2;
                Rect rect;
                Rect rect2;
                float num11;
                if (horiz)
                {
                    float num9 = (thumb.fixedWidth == 0f) ? ((float)thumb.padding.horizontal) : thumb.fixedWidth;
                    num10 = (position.width - (float)slider.padding.horizontal - num9) / (num2 - num);
                    position2 = new Rect((num6 - num) * num10 + position.x + (float)slider.padding.left, position.y + (float)slider.padding.top, num7 * num10 + num9, position.height - (float)slider.padding.vertical);
                    rect = new Rect(position2.x, position2.y, (float)thumb.padding.left, position2.height);
                    rect2 = new Rect(position2.xMax - (float)thumb.padding.right, position2.y, (float)thumb.padding.right, position2.height);
                    num11 = current.mousePosition.x - position.x;
                }
                else
                {
                    float num12 = (thumb.fixedHeight == 0f) ? ((float)thumb.padding.vertical) : thumb.fixedHeight;
                    num10 = (position.height - (float)slider.padding.vertical - num12) / (num2 - num);
                    position2 = new Rect(position.x + (float)slider.padding.left, (num6 - num) * num10 + position.y + (float)slider.padding.top, position.width - (float)slider.padding.horizontal, num7 * num10 + num12);
                    rect = new Rect(position2.x, position2.y, position2.width, (float)thumb.padding.top);
                    rect2 = new Rect(position2.x, position2.yMax - (float)thumb.padding.bottom, position2.width, (float)thumb.padding.bottom);
                    num11 = current.mousePosition.y - position.y;
                }
                switch (current.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (position.Contains(current.mousePosition) && num - num2 != 0f)
                        {
                            if (minMaxSliderState == null)
                            {
                                minMaxSliderState = (EditorGUIExt.s_MinMaxSliderState = new EditorGUIExt.MinMaxSliderState());
                            }
                            minMaxSliderState.dragStartLimit = startLimit;
                            minMaxSliderState.dragEndLimit = endLimit;
                            if (position2.Contains(current.mousePosition))
                            {
                                minMaxSliderState.dragStartPos = num11;
                                minMaxSliderState.dragStartValue = value;
                                minMaxSliderState.dragStartSize = size;
                                minMaxSliderState.dragStartValuesPerPixel = num10;
                                if (rect.Contains(current.mousePosition))
                                {
                                    minMaxSliderState.whereWeDrag = 1;
                                }
                                else
                                {
                                    if (rect2.Contains(current.mousePosition))
                                    {
                                        minMaxSliderState.whereWeDrag = 2;
                                    }
                                    else
                                    {
                                        minMaxSliderState.whereWeDrag = 0;
                                    }
                                }
                                GUIUtility.hotControl = id;
                                current.Use();
                            }
                            else
                            {
                                if (slider != GUIStyle.none)
                                {
                                    if (size != 0f && flag)
                                    {
                                        if (horiz)
                                        {
                                            if (num11 > position2.xMax - position.x)
                                            {
                                                value += size * num8 * 0.9f;
                                            }
                                            else
                                            {
                                                value -= size * num8 * 0.9f;
                                            }
                                        }
                                        else
                                        {
                                            if (num11 > position2.yMax - position.y)
                                            {
                                                value += size * num8 * 0.9f;
                                            }
                                            else
                                            {
                                                value -= size * num8 * 0.9f;
                                            }
                                        }
                                        minMaxSliderState.whereWeDrag = 0;
                                        GUI.changed = true;
                                        EditorGUIExt.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)EditorGUIExt.kFirstScrollWait);
                                        float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
                                        float num14 = (!horiz) ? position2.y : position2.x;
                                        minMaxSliderState.whereWeDrag = ((num13 <= num14) ? 3 : 4);
                                    }
                                    else
                                    {
                                        if (horiz)
                                        {
                                            value = (num11 - position2.width * 0.5f) / num10 + num - size * 0.5f;
                                        }
                                        else
                                        {
                                            value = (num11 - position2.height * 0.5f) / num10 + num - size * 0.5f;
                                        }
                                        minMaxSliderState.dragStartPos = num11;
                                        minMaxSliderState.dragStartValue = value;
                                        minMaxSliderState.dragStartSize = size;
                                        minMaxSliderState.dragStartValuesPerPixel = num10;
                                        minMaxSliderState.whereWeDrag = 0;
                                        GUI.changed = true;
                                    }
                                    GUIUtility.hotControl = id;
                                    value = Mathf.Clamp(value, num3, num4 - size);
                                    current.Use();
                                }
                            }
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            current.Use();
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                        {
                            float num15 = (num11 - minMaxSliderState.dragStartPos) / minMaxSliderState.dragStartValuesPerPixel;
                            int whereWeDrag = minMaxSliderState.whereWeDrag;
                            if (whereWeDrag != 0)
                            {
                                if (whereWeDrag != 1)
                                {
                                    if (whereWeDrag == 2)
                                    {
                                        size = minMaxSliderState.dragStartSize + num15;
                                        if (value + size > num4)
                                        {
                                            size = num4 - value;
                                        }
                                        if (size < num5)
                                        {
                                            size = num5;
                                        }
                                    }
                                }
                                else
                                {
                                    value = minMaxSliderState.dragStartValue + num15;
                                    size = minMaxSliderState.dragStartSize - num15;
                                    if (value < num3)
                                    {
                                        size -= num3 - value;
                                        value = num3;
                                    }
                                    if (size < num5)
                                    {
                                        value -= num5 - size;
                                        size = num5;
                                    }
                                }
                            }
                            else
                            {
                                value = Mathf.Clamp(minMaxSliderState.dragStartValue + num15, num3, num4 - size);
                            }
                            GUI.changed = true;
                            current.Use();
                        }
                        break;
                    case EventType.Repaint:
                        slider.Draw(position, GUIContent.none, id);
                        thumb.Draw(position2, GUIContent.none, id);
                        if (GUIUtility.hotControl == id && position.Contains(current.mousePosition) && num - num2 != 0f)
                        {
                            if (position2.Contains(current.mousePosition))
                            {
                                if (minMaxSliderState != null && (minMaxSliderState.whereWeDrag == 3 || minMaxSliderState.whereWeDrag == 4))
                                {
                                    GUIUtility.hotControl = 0;
                                }
                            }
                            else
                            {
                                if (!(DateTime.Now < EditorGUIExt.s_NextScrollStepTime))
                                {
                                    float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
                                    float num14 = (!horiz) ? position2.y : position2.x;
                                    int num16 = (num13 <= num14) ? 3 : 4;
                                    if (num16 == minMaxSliderState.whereWeDrag)
                                    {
                                        if (size != 0f && flag)
                                        {
                                            if (horiz)
                                            {
                                                if (num11 > position2.xMax - position.x)
                                                {
                                                    value += size * num8 * 0.9f;
                                                }
                                                else
                                                {
                                                    value -= size * num8 * 0.9f;
                                                }
                                            }
                                            else
                                            {
                                                if (num11 > position2.yMax - position.y)
                                                {
                                                    value += size * num8 * 0.9f;
                                                }
                                                else
                                                {
                                                    value -= size * num8 * 0.9f;
                                                }
                                            }
                                            minMaxSliderState.whereWeDrag = -1;
                                            GUI.changed = true;
                                        }
                                        value = Mathf.Clamp(value, num3, num4 - size);
                                        EditorGUIExt.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)EditorGUIExt.kScrollWait);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        public static bool DragSelection(Rect[] positions, ref bool[] selections, GUIStyle style)
        {
            int controlID = GUIUtility.GetControlID(34553287, FocusType.Keyboard);
            Event current = Event.current;
            int num = -1;
            for (int i = positions.Length - 1; i >= 0; i--)
            {
                if (positions[i].Contains(current.mousePosition))
                {
                    num = i;
                    break;
                }
            }
            bool result;
            switch (current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && num >= 0)
                    {
                        GUIUtility.keyboardControl = 0;
                        bool flag = false;
                        if (selections[num])
                        {
                            int num2 = 0;
                            bool[] array = selections;
                            for (int j = 0; j < array.Length; j++)
                            {
                                bool flag2 = array[j];
                                if (flag2)
                                {
                                    num2++;
                                    if (num2 > 1)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (num2 == 1)
                            {
                                flag = true;
                            }
                        }
                        if (!current.shift && !EditorGUI.actionKey)
                        {
                            for (int k = 0; k < positions.Length; k++)
                            {
                                selections[k] = false;
                            }
                        }
                        EditorGUIExt.initIndex = num;
                        EditorGUIExt.initSelections = (bool[])selections.Clone();
                        EditorGUIExt.adding = true;
                        if ((current.shift || EditorGUI.actionKey) && selections[num])
                        {
                            EditorGUIExt.adding = false;
                        }
                        selections[num] = (!flag && EditorGUIExt.adding);
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        result = true;
                        return result;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (current.button == 0)
                        {
                            if (num < 0)
                            {
                                Rect rect = new Rect(positions[0].x, positions[0].y - 200f, positions[0].width, 200f);
                                if (rect.Contains(current.mousePosition))
                                {
                                    num = 0;
                                }
                                rect.y = positions[positions.Length - 1].yMax;
                                if (rect.Contains(current.mousePosition))
                                {
                                    num = selections.Length - 1;
                                }
                            }
                            if (num < 0)
                            {
                                result = false;
                                return result;
                            }
                            int num3 = Mathf.Min(EditorGUIExt.initIndex, num);
                            int num4 = Mathf.Max(EditorGUIExt.initIndex, num);
                            for (int l = 0; l < selections.Length; l++)
                            {
                                if (l >= num3 && l <= num4)
                                {
                                    selections[l] = EditorGUIExt.adding;
                                }
                                else
                                {
                                    selections[l] = EditorGUIExt.initSelections[l];
                                }
                            }
                            current.Use();
                            result = true;
                            return result;
                        }
                    }
                    break;
                case EventType.Repaint:
                    for (int m = 0; m < positions.Length; m++)
                    {
                        style.Draw(positions[m], GUIContent.none, controlID, selections[m]);
                    }
                    break;
            }
            result = false;
            return result;
        }
        private static bool Any(bool[] selections)
        {
            bool result;
            for (int i = 0; i < selections.Length; i++)
            {
                if (selections[i])
                {
                    result = true;
                    return result;
                }
            }
            result = false;
            return result;
        }
        private static int GetIndexUnderMouse(Rect[] hitPositions, bool[] readOnly)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            int result;
            for (int i = hitPositions.Length - 1; i >= 0; i--)
            {
                if ((readOnly == null || !readOnly[i]) && hitPositions[i].Contains(mousePosition))
                {
                    result = i;
                    return result;
                }
            }
            result = -1;
            return result;
        }
        internal static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect result = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (result.width < 0f)
            {
                result.x += result.width;
                result.width = -result.width;
            }
            if (result.height < 0f)
            {
                result.y += result.height;
                result.height = -result.height;
            }
            return result;
        }
    }
}

