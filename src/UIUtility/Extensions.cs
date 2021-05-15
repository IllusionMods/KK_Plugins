using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    internal static class Extensions
    {
        private static readonly MethodInfo ToggleSetMethod;
        private static readonly MethodInfo SliderSetMethod;
        private static readonly MethodInfo ScrollbarSetMethod;

        private static readonly FieldInfo DropdownValueField;
#if KK
        private static readonly MethodInfo DropdownRefreshMethod;  // Unity 5.2 <= only
#endif

        static Extensions()
        {
            // Find the Toggle's set method
            ToggleSetMethod = FindSetMethod(typeof(Toggle));

            // Find the Slider's set method
            SliderSetMethod = FindSetMethod(typeof(Slider));

            // Find the Scrollbar's set method
            ScrollbarSetMethod = FindSetMethod(typeof(Scrollbar));

            // Find the Dropdown's value field and its' Refresh method
            DropdownValueField = typeof(Dropdown).GetField("m_Value", AccessTools.all);
#if KK
            DropdownRefreshMethod = typeof(Dropdown).GetMethod("RefreshShownValue", AccessTools.all);  // Unity 5.2 <= only
#endif
        }

        internal static void ExecuteDelayed(this MonoBehaviour self, Action action, int waitCount = 1)
        {
            self.StartCoroutine(ExecuteDelayed_Routine(action, waitCount));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int waitCount)
        {
            for (int i = 0; i < waitCount; ++i)
                yield return null;
            action();
        }

        /// <summary>
        /// Return the index of the option matching the specified text or -1 if not found
        /// </summary>
        public static int OptionIndex(this Dropdown dropdown, string optionText)
        {
            for (int i = 0; i < dropdown.options.Count; i++)
                if (dropdown.options[i].text == optionText)
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns the text of the item at the specified index or null if option doesn't exist
        /// </summary>
        public static string OptionText(this Dropdown dropdown, int itemIndex)
        {
            if (itemIndex < dropdown.options.Count)
                return dropdown.options[itemIndex].text;
            else
                return null;
        }

        /// <summary>
        /// Set the value of a Toggle
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="sendCallback">Whether to trigger events</param>
        public static void Set(this Toggle instance, bool value, bool sendCallback = false)
        {
            ToggleSetMethod.Invoke(instance, new object[] { value, sendCallback });
        }

        /// <summary>
        /// Set the value of a Slider
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="sendCallback">Whether to trigger events</param>
        public static void Set(this Slider instance, float value, bool sendCallback = false)
        {
            SliderSetMethod.Invoke(instance, new object[] { value, sendCallback });
        }

        /// <summary>
        /// Set the value of a Scrollbar
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="sendCallback">Whether to trigger events</param>
        public static void Set(this Scrollbar instance, float value, bool sendCallback = false)
        {
            ScrollbarSetMethod.Invoke(instance, new object[] { value, sendCallback });
        }

        /// <summary>
        /// Set the value of a Dropdown
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        public static void Set(this Dropdown instance, int value)
        {
            DropdownValueField.SetValue(instance, value);
#if KK
            DropdownRefreshMethod.Invoke(instance, new object[] { }); // Unity 5.2 <= only
#else
            instance.RefreshShownValue(); // Unity 5.3 >= only
#endif
        }

        /// <summary>
        /// Set the value of an InputField
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="sendCallback">Whether to trigger events</param>
        public static void Set(this InputField instance, string value, bool sendCallback = false)
        {
            if (sendCallback)
                instance.text = value;
            else
            {
                instance.m_Text = value;
                instance.UpdateLabel();
            }
        }

        private static MethodInfo FindSetMethod(Type objectType)
        {
            var methods = objectType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            for (var i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "Set" && methods[i].GetParameters().Length == 2)
                    return methods[i];
            }

            return null;
        }
    }
}
