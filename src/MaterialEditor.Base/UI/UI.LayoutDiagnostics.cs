using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal static class RowLayoutDiagnostics
    {
        internal static string Describe(GameObject rowRoot, string context)
        {
            var message = new StringBuilder();
            message.AppendLine("[ME layout] " + context);

            foreach (var group in rowRoot.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                var panel = (RectTransform)group.transform;
                message.AppendLine(
                    string.Format(
                        "{0}: width={1:F3}, controlWidth={2}, expandWidth={3}, spacing={4:F3}",
                        panel.name,
                        panel.rect.width,
                        group.childControlWidth,
                        group.childForceExpandWidth,
                        group.spacing));

                foreach (Transform child in panel)
                {
                    var rect = child as RectTransform;
                    if (rect == null)
                        continue;

                    message.AppendLine(
                        string.Format(
                            "  {0}: x={1:F3}, width={2:F3}, active={3}",
                            child.name,
                            rect.anchoredPosition.x,
                            rect.rect.width,
                            child.gameObject.activeSelf));

                    foreach (var component in child.GetComponents<Component>())
                    {
                        var element = component as ILayoutElement;
                        if (element == null)
                            continue;

                        var column = component as RowColumnLayoutOverride;
                        message.AppendLine(
                            string.Format(
                                "    {0}: priority={1}, min={2:F3}, preferred={3:F3}, flexible={4:F3}{5}",
                                component.GetType().FullName,
                                element.layoutPriority,
                                element.minWidth,
                                element.preferredWidth,
                                element.flexibleWidth,
                                column != null ? ", role=" + column.Role : string.Empty));
                    }
                }
            }

            return message.ToString();
        }
    }

    internal static class RowLayoutRuntimeAssertions
    {
        private const float Tolerance = 0.5f;

        internal static void Validate(RowView row)
        {
            var wasActive = row.gameObject.activeSelf;
            row.SetVisible(true);

            try
            {
                var colorPanel = FindRect(row.transform, "ColorPanel");
                var offsetPanel = FindRect(row.transform, "OffsetScalePanel");
                var floatPanel = FindRect(row.transform, "FloatPanel");
                var rInput = FindInput(row.transform, "ColorRInput");
                var gInput = FindInput(row.transform, "ColorGInput");
                var bInput = FindInput(row.transform, "ColorBInput");
                var aInput = FindInput(row.transform, "ColorAInput");

                var originalR = rInput.text;
                var originalG = gInput.text;
                ValidateNumericEditing(rInput);
                rInput.text = "0";
                gInput.text = "0.123456789";

                ForceLayout(colorPanel);
                ForceLayout(offsetPanel);
                ForceLayout(floatPanel);

                AssertClose(
                    "RGBA declared width",
                    MaterialEditorLayout.ColorInputWidth,
                    ((RectTransform)rInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA short/long widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)gInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA R/B widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)bInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA R/A widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)aInput.transform).rect.width,
                    row);

                AssertClose(
                    "Color/offset editor alignment",
                    WorldLeft(FindRect(row.transform, "ColorRText")),
                    WorldLeft(FindRect(row.transform, "OffsetXText")),
                    row);
                AssertClose(
                    "Color/float editor alignment",
                    WorldLeft(FindRect(row.transform, "ColorRText")),
                    WorldLeft(FindRect(row.transform, "FloatSlider")),
                    row);

                rInput.text = originalR;
                gInput.text = originalG;
                ForceLayout(colorPanel);
            }
            catch (Exception exception)
            {
                if (MaterialEditorPluginBase.Logger != null)
                    MaterialEditorPluginBase.Logger.LogError(
                        "[ME layout assertion] Validation failed: " + exception);
            }
            finally
            {
                row.SetVisible(wasActive);
            }
        }

        internal static void ValidateClones(RowView first, RowView second)
        {
            var firstWasActive = first.gameObject.activeSelf;
            var secondWasActive = second.gameObject.activeSelf;
            first.SetVisible(true);
            second.SetVisible(true);

            try
            {
                var firstPanel = FindRect(first.transform, "ColorPanel");
                var secondPanel = FindRect(second.transform, "ColorPanel");
                ForceLayout(firstPanel);
                ForceLayout(secondPanel);
                AssertClose(
                    "RGBA cloned row widths",
                    FindRect(first.transform, "ColorRInput").rect.width,
                    FindRect(second.transform, "ColorRInput").rect.width,
                    first);
            }
            catch (Exception exception)
            {
                if (MaterialEditorPluginBase.Logger != null)
                    MaterialEditorPluginBase.Logger.LogError(
                        "[ME layout assertion] Clone validation failed: " + exception);
            }
            finally
            {
                first.SetVisible(firstWasActive);
                second.SetVisible(secondWasActive);
            }
        }

        private static void AssertClose(
            string name,
            float expected,
            float actual,
            RowView row)
        {
            if (Mathf.Abs(expected - actual) <= Tolerance)
                return;

            if (MaterialEditorPluginBase.Logger == null)
                return;

            MaterialEditorPluginBase.Logger.LogError(
                string.Format(
                    "[ME layout assertion] {0}: expected {1:F3}, actual {2:F3}\n{3}",
                    name,
                    expected,
                    actual,
                    RowLayoutDiagnostics.Describe(row.gameObject, name)));
        }

        private static void ForceLayout(RectTransform panel)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        private static void ValidateNumericEditing(InputField input)
        {
            var numeric = input.GetComponent<NumericInputView>();
            if (numeric == null)
                throw new InvalidOperationException("Missing NumericInputView on " + input.name);

            numeric.SetValue(0.9255123f);
            numeric.OnSelect(null);
            numeric.CommitValue(0.5f);
            if (input.text != "0.5")
                throw new InvalidOperationException(
                    "Numeric input did not restore compact text after editing: " + input.text);
        }

        private static float WorldLeft(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].x;
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<RectTransform>();
        }

        private static InputField FindInput(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<InputField>();
        }
    }
}
