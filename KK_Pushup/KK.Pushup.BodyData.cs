using MessagePack;
using System;

namespace KK_Plugins
{
    public partial class Pushup
    {
        [Serializable]
        [MessagePackObject]
        public class BodyData
        {
            [Key("Size")]
            public float Size { get; set; }
            [Key("VerticalPosition")]
            public float VerticalPosition { get; set; }
            [Key("HorizontalAngle")]
            public float HorizontalAngle { get; set; }
            [Key("HorizontalPosition")]
            public float HorizontalPosition { get; set; }
            [Key("VerticalAngle")]
            public float VerticalAngle { get; set; }
            [Key("Depth")]
            public float Depth { get; set; }
            [Key("Roundness")]
            public float Roundness { get; set; }

            [Key("Softness")]
            public float Softness { get; set; }
            [Key("Weight")]
            public float Weight { get; set; }

            [Key("AreolaDepth")]
            public float AreolaDepth { get; set; }
            [Key("NippleWidth")]
            public float NippleWidth { get; set; }
            [Key("NippleDepth")]
            public float NippleDepth { get; set; }

            public BodyData() { }
            public BodyData(ChaFileBody baseBody)
            {
                Softness = baseBody.bustSoftness;
                Weight = baseBody.bustWeight;
                Size = baseBody.shapeValueBody[PushupConstants.IndexSize];
                VerticalPosition = baseBody.shapeValueBody[PushupConstants.IndexVerticalPosition];
                HorizontalAngle = baseBody.shapeValueBody[PushupConstants.IndexHorizontalAngle];
                HorizontalPosition = baseBody.shapeValueBody[PushupConstants.IndexHorizontalPosition];
                VerticalAngle = baseBody.shapeValueBody[PushupConstants.IndexVerticalAngle];
                Depth = baseBody.shapeValueBody[PushupConstants.IndexDepth];
                Roundness = baseBody.shapeValueBody[PushupConstants.IndexRoundness];
                AreolaDepth = baseBody.shapeValueBody[PushupConstants.IndexAreolaDepth];
                NippleWidth = baseBody.shapeValueBody[PushupConstants.IndexNippleWidth];
                NippleDepth = baseBody.shapeValueBody[PushupConstants.IndexNippleDepth];
            }

            public void CopyTo(BodyData data)
            {
                data.Size = Size;
                data.VerticalPosition = VerticalPosition;
                data.HorizontalAngle = HorizontalAngle;
                data.HorizontalPosition = HorizontalPosition;
                data.VerticalAngle = VerticalAngle;
                data.Depth = Depth;
                data.Roundness = Roundness;
                data.Softness = Softness;
                data.Weight = Weight;
                data.AreolaDepth = AreolaDepth;
                data.NippleWidth = NippleWidth;
                data.NippleDepth = NippleDepth;
            }
        }
    }
}
