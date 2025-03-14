using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KKAPI.Utilities;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.MoreOutfits.Plugin;

namespace KK_Plugins.MoreOutfits
{
    internal static class HSceneUI
    {
        internal static bool HSceneUIInitialized = false;

        internal static void InitializeHSceneUI()
        {
            if (HSceneUIInitialized)
                return;
            HSceneUIInitialized = true;

#if KK // Not needed in KKS
            LoadListTemplate();
#endif
            HFlag flags;
            HSprite sprite;

            if (!KKAPI.KoikatuAPI.IsVR())
            {
                flags = Object.FindObjectOfType<HSceneProc>().flags;
                sprite = Object.FindObjectOfType<HSceneProc>().sprite;
            }
            else
            {
                var vrHScene = Object.FindObjectOfType(System.Type.GetType("VRHScene, Assembly-CSharp"));
                flags = (HFlag)Traverse.Create(vrHScene).Field("flags").GetValue();
                sprite = (HSprite)Traverse.Create(vrHScene).Field("sprite").GetValue();
            }

            if (flags.lstHeroine.Count == 1) //Single girl H
            {
                foreach (var button in sprite.categoryDress.lstButton)
                {
#if KK
                    if (button != null && button.gameObject.name == "ChangeCoordinatType")
                    {
                        var parent = button.transform.Find("CoordinatGroup/CoordinatCategory");
                        SetUpList(flags, sprite, parent, 0);
                    }
#elif KKS
                    if (button != null && button.gameObject.transform.parent.name == "ChangeCoordinatType")
                    {
                        var parent = button.transform.parent.Find("CoordinatGroup/CoordinatCategory");
                        SetUpList(flags, sprite, parent, 0);
                    }
#endif
                }
            }
            else //Multi girl H
            {
                for (int i = 0; i < sprite.lstMultipleFemaleDressButton.Count; i++)
                {
                    var parent = sprite.lstMultipleFemaleDressButton[i].coordinate.transform;
                    SetUpList(flags, sprite, parent, i);
                }
            }
        }

        private static void SetUpList(HFlag hFlags, HSprite hSprite, Transform parent, int femaleIndex)
        {
            var chaControl = hFlags.lstHeroine[femaleIndex].chaCtrl;

            var go = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            go.transform.SetParent(parent.transform, false);
            var scroll = go.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 32f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Object.DestroyImmediate(scroll.horizontalScrollbar.gameObject);
            Object.DestroyImmediate(scroll.verticalScrollbar.gameObject);
            Object.DestroyImmediate(scroll.GetComponent<Image>());
            var viewportPos = scroll.viewport.localPosition;
            scroll.viewport.localPosition = new Vector3(0f, viewportPos.y, viewportPos.z);

            var name = "clothesFileWindow";
            if (KKAPI.KoikatuAPI.IsVR()) name = "VRClothesFileWindow";
#if KK
            var copyTarget = GameObject.Find("Canvas").transform.Find($"{name}/Window/WinRect/ListArea/Scroll View/Scrollbar Vertical").gameObject;
#elif KKS
            var copyTarget = Object.FindObjectOfType<HSprite>().transform.Find($"{name}/Window/WinRect/ListArea/Scroll View/Scrollbar Vertical").gameObject;
#endif

            var newScrollbar = Object.Instantiate(copyTarget, go.transform);
            scroll.verticalScrollbar = newScrollbar.GetComponent<Scrollbar>();
            newScrollbar.transform.SetRect(1f, 0f, 1f, 1f, 0f, 0f, 18f);
            var newScrollbarPos = newScrollbar.transform.localPosition;
#if KK
            newScrollbar.transform.localPosition = new Vector3(135f, newScrollbarPos.y, newScrollbarPos.z);
#elif KKS
            newScrollbar.transform.localPosition = new Vector3(170f, newScrollbarPos.y, newScrollbarPos.z);
#endif

            var vlg = parent.GetComponent<VerticalLayoutGroup>();
            var csf = parent.GetComponent<ContentSizeFitter>();
            vlg.enabled = false;
            csf.enabled = false;
            CopyComponent(vlg, scroll.content.gameObject).enabled = true;
            CopyComponent(csf, scroll.content.gameObject).enabled = true;

            var buttons = parent.GetComponentsInChildren<Button>().ToList();
            buttons.ForEach(x => x.transform.SetParent(scroll.content));

            for (int i = 0; i < chaControl.chaFile.coordinate.Length - OriginalCoordinateLength; i++)
            {
                int coordinateIndex = OriginalCoordinateLength + i;
                var newButton = Object.Instantiate(buttons[0], buttons[0].transform.parent);
                newButton.onClick.ActuallyRemoveAllListeners();
                newButton.onClick.AddListener(() => hSprite.OnClickCoordinateChange(femaleIndex, coordinateIndex));

#if KK
                //Set the sprite to one without text
                var buttonImage = newButton.GetComponent<Image>();
                buttonImage.sprite = ListTemplate;

                //Add a text component
                var textContainer = new GameObject("CoordinateText");
                textContainer.transform.SetParent(newButton.transform);
                var buttonText = textContainer.gameObject.GetOrAddComponent<Text>();
                buttonText.text = GetCoodinateName(chaControl, coordinateIndex);

                buttonText.transform.localScale = new Vector3(1, 1, 1);
                buttonText.rectTransform.anchorMin = new Vector2(0, 0);
                buttonText.rectTransform.anchorMax = new Vector2(0, 0);
                buttonText.rectTransform.offsetMin = new Vector2(0, 0);
                buttonText.rectTransform.offsetMax = new Vector2(112, 24);

                buttonText.font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                buttonText.fontSize = 16;
                buttonText.fontStyle = FontStyle.Bold;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
                buttonText.verticalOverflow = VerticalWrapMode.Overflow;
                buttonText.color = Color.black;
#elif KKS
                var textContainer = newButton.GetComponentInChildren<TextMeshProUGUI>();
                textContainer.text = GetCoodinateName(chaControl, coordinateIndex);
#endif
            }
        }

        private static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            var type = original.GetType();
            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst;
        }

#if KK // Not needed in KKS
        private static Sprite ListTemplate;
        internal static void LoadListTemplate()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"KK_Plugins.Resources.ListTemplate.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);

                Texture2D texture2D = new Texture2D(32, 32);
                texture2D.LoadImage(bytesInStream);
                ListTemplate = Sprite.Create(texture2D, new Rect(0f, 0f, 112, 24), new Vector2(112, 24));
            }
        }
#endif
    }

    internal static class Extensions
    {
        public static void SetRect(this Transform self, float anchorLeft = 0f, float anchorBottom = 0f, float anchorRight = 1f, float anchorTop = 1f, float offsetLeft = 0f, float offsetBottom = 0f, float offsetRight = 0f, float offsetTop = 0f)
        {
            RectTransform rt = self as RectTransform;
            rt.anchorMin = new Vector2(anchorLeft, anchorBottom);
            rt.anchorMax = new Vector2(anchorRight, anchorTop);
            rt.offsetMin = new Vector2(offsetLeft, offsetBottom);
            rt.offsetMax = new Vector2(offsetRight, offsetTop);
        }
    }
}