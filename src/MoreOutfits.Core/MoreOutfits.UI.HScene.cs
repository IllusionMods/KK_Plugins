using System.IO;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.MoreOutfits.Plugin;

namespace KK_Plugins.MoreOutfits
{
    internal static class HSceneUI
    {
        internal static bool HSceneUIInitialized = false;
        private static Sprite ListTemplate;

        internal static void InitializeHSceneUI()
        {
            if (HSceneUIInitialized)
                return;
            HSceneUIInitialized = true;

            LoadListTemplate();

            var hSceneProc = Object.FindObjectOfType<HSceneProc>();

            if (hSceneProc.flags.lstHeroine.Count == 1) //Single girl H
            {
                foreach (var button in hSceneProc.sprite.categoryDress.lstButton)
                {
                    if (button != null && button.gameObject.name == "ChangeCoordinatType")
                    {
                        var parent = button.transform.Find("CoordinatGroup/CoordinatCategory");
                        SetUpList(hSceneProc, parent, 0);
                    }
                }
            }
            else //Multi girl H
            {
                for (int i = 0; i < hSceneProc.sprite.lstMultipleFemaleDressButton.Count; i++)
                {
                    var parent = hSceneProc.sprite.lstMultipleFemaleDressButton[i].coordinate.transform;
                    SetUpList(hSceneProc, parent, i);
                }
            }
        }

        private static void SetUpList(HSceneProc hSceneProc, Transform parent, int femaleIndex)
        {
            var chaControl = hSceneProc.flags.lstHeroine[femaleIndex].chaCtrl;

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

            var copyTarget = GameObject.Find("Canvas").transform.Find("clothesFileWindow/Window/WinRect/ListArea/Scroll View/Scrollbar Vertical").gameObject;
            var newScrollbar = Object.Instantiate(copyTarget, go.transform);
            scroll.verticalScrollbar = newScrollbar.GetComponent<Scrollbar>();
            newScrollbar.transform.SetRect(1f, 0f, 1f, 1f, 0f, 0f, 18f);
            var newScrollbarPos = newScrollbar.transform.localPosition;
            newScrollbar.transform.localPosition = new Vector3(135f, newScrollbarPos.y, newScrollbarPos.z);

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
                newButton.onClick.RemoveAllListeners();
                newButton.onClick.AddListener(() => hSceneProc.sprite.OnClickCoordinateChange(femaleIndex, coordinateIndex));

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
