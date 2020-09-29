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

            [Key("EnablePushup")]
            public bool EnablePushup { get; set; } = ConfigEnablePushup.Value;
            [Key("FlattenNipples")]
            public bool FlattenNipples { get; set; } = ConfigFlattenNipplesDefault.Value;

            [Key("UseAdvanced")]
            public bool UseAdvanced { get; set; }

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

            public void CopyTo(ClothData data)
            {
                data.Firmness = Firmness;
                data.Lift = Lift;
                data.PushTogether = PushTogether;
                data.Squeeze = Squeeze;
                data.CenterNipples = CenterNipples;
                data.EnablePushup = EnablePushup;
                data.FlattenNipples = FlattenNipples;
                data.UseAdvanced = UseAdvanced;
            }
        }
    }
}
