using UnityEngine;

namespace TimelineFlowControl
{
    internal static class Utils
    {
        public static string FormatTime(float time)
        {
            var minutes = (int)time / 60;
            var seconds = (int)time - 60 * minutes;
            var milliseconds = (int)(1000 * (time - minutes * 60 - seconds));
            return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }

        public static Rect GetScreenCoordinates(RectTransform uiElement)
        {
            var worldCorners = new Vector3[4];
            uiElement.GetWorldCorners(worldCorners);
            var result = new Rect(
                worldCorners[0].x,
                worldCorners[0].y,
                worldCorners[2].x - worldCorners[0].x,
                worldCorners[2].y - worldCorners[0].y);
            return result;
        }
    }
}
