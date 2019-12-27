using MessagePack;
using System;

namespace KK_Plugins
{
    public partial class Pushup
    {
        [Serializable]
        [MessagePackObject]
        public class ClothData : BodyData
        {
            [Key("Firmness")]
            public float Firmness { get; set; } = ConfigFirmnessDefault.Value;
            [Key("Lift")]
            public float Lift { get; set; } = ConfigLiftDefault.Value;
            [Key("PushTogether")]
            public float PushTogether { get; set; } = ConfigPushTogetherDefault.Value;
            [Key("Squeeze")]
            public float Squeeze { get; set; } = ConfigSqueezeDefault.Value;
            [Key("CenterNipples")]
            public float CenterNipples { get; set; } = ConfigNippleCenteringDefault.Value;

            [Key("EnablePushUp")]
            public bool EnablePushUp { get; set; } = ConfigEnablePushup.Value;
            [Key("FlattenNipples")]
            public bool FlattenNipples { get; set; } = ConfigFlattenNipplesDefault.Value;

            [Key("UseAdvanced")]
            public bool UseAdvanced { get; set; } = false;

            public ClothData() { }
            public ClothData(ChaFileBody baseBody) : base(baseBody) { }
            public ClothData(BodyData bodyData)
            {
                Size = bodyData.Size;
                VerticalPosition = bodyData.VerticalPosition;
                HorizontalAngle = bodyData.HorizontalAngle;
                HorizontalPosition = bodyData.HorizontalPosition;
                VerticalAngle = bodyData.VerticalAngle;
                Depth = bodyData.Depth;
                Roundness = bodyData.Roundness;
                Softness = bodyData.Softness;
                Weight = bodyData.Weight;
                AreolaDepth = bodyData.AreolaDepth;
                NippleWidth = bodyData.NippleWidth;
                NippleDepth = bodyData.NippleDepth;
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
