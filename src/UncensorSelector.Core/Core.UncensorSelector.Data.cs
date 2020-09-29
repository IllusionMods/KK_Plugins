using System.Collections.Generic;
using System.Xml.Linq;

namespace KK_Plugins
{
    internal partial class UncensorSelector
    {

        public interface IUncensorData
        {
            string GUID { get; }
            string DisplayName { get; }
            bool AllowRandom { get; }
        }
        public class BodyData : IUncensorData
        {
            public string BodyGUID;
            public string DisplayName;
            public string OOBase;
            public string MMBase;
            public string Normals;
            public byte Sex;
            public bool AllowRandom = true;
            public string BodyMainTex;
            public string BodyColorMask;
            public string BodyMaterial;
            public string BodyMaterialCreate;
            public string Asset;
            public string UncensorOverlay;
            public string UncensorUnderlay;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();
            public List<string> AdditionalParts = new List<string>();

            string IUncensorData.GUID => BodyGUID;
            string IUncensorData.DisplayName => DisplayName;
            bool IUncensorData.AllowRandom => AllowRandom;

            public BodyData(XContainer bodyXMLData)
            {
                if (bodyXMLData == null)
                    return;

                BodyGUID = bodyXMLData.Element("guid")?.Value;
                DisplayName = bodyXMLData.Element("displayName")?.Value;

                if (bodyXMLData.Element("sex")?.Value.ToLower() == "female")
                    Sex = 1;
                if (bodyXMLData.Element("allowRandom")?.Value.ToLower() == "false" || bodyXMLData.Element("allowRandom")?.Value.ToLower() == "0")
                    AllowRandom = false;

                XElement oo_base = bodyXMLData.Element("oo_base");
                if (oo_base != null)
                {
                    OOBase = oo_base.Element("file")?.Value;
                    if (Asset.IsNullOrWhiteSpace())
                        Asset = oo_base.Element("asset")?.Value;
                    BodyMainTex = oo_base.Element("mainTex")?.Value;
                    BodyColorMask = oo_base.Element("colorMask")?.Value;
                    Normals = oo_base.Element("normals")?.Value;
                    UncensorOverlay = oo_base.Element("uncensorOverlay")?.Value;
                    UncensorUnderlay = oo_base.Element("uncensorUnderlay")?.Value;

                    foreach (XElement parts in oo_base.Elements("additionalPart"))
                    {
                        string part = parts.Value;
                        if (!part.IsNullOrWhiteSpace())
                            AdditionalParts.Add(part);
                    }

                    foreach (XElement colorMatch in oo_base.Elements("colorMatch"))
                    {
                        ColorMatchPart part = new ColorMatchPart(colorMatch.Element("object")?.Value,
                                                                 colorMatch.Element("material")?.Value,
                                                                 colorMatch.Element("materialCreate")?.Value,
                                                                 colorMatch.Element("mainTex")?.Value,
                                                                 colorMatch.Element("colorMask")?.Value);
                        if (part.Verify())
                            ColorMatchList.Add(part);
                    }
                }

                XElement mm_base = bodyXMLData.Element("oo_base");
                if (mm_base != null)
                {
                    MMBase = mm_base.Element("mm_base")?.Value;
                    BodyMaterial = mm_base.Element("material")?.Value;
                    BodyMaterialCreate = mm_base.Element("materialCreate")?.Value;
                }

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null/defaults for easier checks
                MMBase = MMBase.IsNullOrWhiteSpace() ? Defaults.MMBase : MMBase;
                OOBase = OOBase.IsNullOrWhiteSpace() ? Defaults.OOBase : OOBase;
                BodyGUID = BodyGUID.IsNullOrWhiteSpace() ? null : BodyGUID;
                DisplayName = DisplayName.IsNullOrWhiteSpace() ? BodyGUID : DisplayName;
                Normals = Normals.IsNullOrWhiteSpace() ? Defaults.Normals : Normals;
                BodyMainTex = BodyMainTex.IsNullOrWhiteSpace() ? Defaults.BodyMainTex : BodyMainTex;
                BodyColorMask = BodyColorMask.IsNullOrWhiteSpace() ? Sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale : BodyColorMask;
                Asset = Asset.IsNullOrWhiteSpace() ? Sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale : Asset;
                BodyMaterial = BodyMaterial.IsNullOrWhiteSpace() ? Sex == 0 ? Defaults.BodyMaterialMale : Defaults.BodyMaterialFemale : BodyMaterial;
                BodyMaterialCreate = BodyMaterialCreate.IsNullOrWhiteSpace() ? Defaults.BodyMaterialCreate : BodyMaterialCreate;
            }

            internal BodyData(byte sex, string bodyGUID, string displayName)
            {
                OOBase = Defaults.OOBase;
                MMBase = Defaults.MMBase;
                BodyGUID = bodyGUID;
                DisplayName = displayName;
                Sex = sex;
                AllowRandom = false;
                Normals = Defaults.Normals;
                BodyMainTex = Defaults.BodyMainTex;
                BodyColorMask = Sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale;
                Asset = Sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale;
                BodyMaterial = Sex == 0 ? Defaults.BodyMaterialMale : Defaults.BodyMaterialFemale;
                BodyMaterialCreate = Defaults.BodyMaterialCreate;
            }
        }

        public class PenisData : IUncensorData
        {
            public string PenisGUID;
            public string DisplayName;
            public string File;
            public string Asset;
            public bool AllowRandom = true;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();

            string IUncensorData.GUID => PenisGUID;
            string IUncensorData.DisplayName => DisplayName;
            bool IUncensorData.AllowRandom => AllowRandom;

            public PenisData(XContainer penisXMLData)
            {
                if (penisXMLData == null)
                    return;

                PenisGUID = penisXMLData.Element("guid")?.Value;
                DisplayName = penisXMLData.Element("displayName")?.Value;
                File = penisXMLData.Element("file")?.Value;
                Asset = penisXMLData.Element("asset")?.Value;

                if (penisXMLData.Element("allowRandom")?.Value.ToLower() == "false" || penisXMLData.Element("allowRandom")?.Value.ToLower() == "0")
                    AllowRandom = false;

                foreach (XElement colorMatch in penisXMLData.Elements("colorMatch"))
                {
                    ColorMatchPart part = new ColorMatchPart(colorMatch.Element("object")?.Value,
                                                             colorMatch.Element("material")?.Value,
                                                             colorMatch.Element("materialCreate")?.Value,
                                                             colorMatch.Element("mainTex")?.Value,
                                                             colorMatch.Element("colorMask")?.Value);
                    if (part.Verify())
                        ColorMatchList.Add(part);
                }

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null for easier checks
                PenisGUID = PenisGUID.IsNullOrWhiteSpace() ? null : PenisGUID;
                DisplayName = DisplayName.IsNullOrWhiteSpace() ? null : DisplayName;
                File = File.IsNullOrWhiteSpace() ? null : File;
                Asset = Asset.IsNullOrWhiteSpace() ? null : Asset;
            }

            internal PenisData(string penisGUID, string displayName)
            {
                PenisGUID = penisGUID;
                DisplayName = displayName;
                File = "chara/oo_base.unity3d";
                Asset = "p_cm_body_00";
                AllowRandom = false;
            }
        }

        public class BallsData : IUncensorData
        {
            public string BallsGUID;
            public string DisplayName;
            public string File;
            public string Asset;
            public bool AllowRandom = true;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();
            string IUncensorData.GUID => BallsGUID;
            string IUncensorData.DisplayName => DisplayName;
            bool IUncensorData.AllowRandom => AllowRandom;

            public BallsData(XContainer ballsXMLData)
            {
                if (ballsXMLData == null)
                    return;

                BallsGUID = ballsXMLData.Element("guid")?.Value;
                DisplayName = ballsXMLData.Element("displayName")?.Value;
                File = ballsXMLData.Element("file")?.Value;
                Asset = ballsXMLData.Element("asset")?.Value;

                if (ballsXMLData.Element("allowRandom")?.Value.ToLower() == "false" || ballsXMLData.Element("allowRandom")?.Value.ToLower() == "0")
                    AllowRandom = false;

                foreach (XElement colorMatch in ballsXMLData.Elements("colorMatch"))
                {
                    ColorMatchPart part = new ColorMatchPart(colorMatch.Element("object")?.Value,
                                                             colorMatch.Element("material")?.Value,
                                                             colorMatch.Element("materialCreate")?.Value,
                                                             colorMatch.Element("mainTex")?.Value,
                                                             colorMatch.Element("colorMask")?.Value);
                    if (part.Verify())
                        ColorMatchList.Add(part);
                }

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null for easier checks
                BallsGUID = BallsGUID.IsNullOrWhiteSpace() ? null : BallsGUID;
                DisplayName = DisplayName.IsNullOrWhiteSpace() ? null : DisplayName;
                File = File.IsNullOrWhiteSpace() ? null : File;
                Asset = Asset.IsNullOrWhiteSpace() ? null : Asset;
            }

            internal BallsData(string ballsGUID, string displayName)
            {
                BallsGUID = ballsGUID;
                DisplayName = displayName;
                File = "chara/oo_base.unity3d";
                Asset = "p_cm_body_00";
                AllowRandom = false;
            }
        }

        public class ColorMatchPart
        {
            public string Object;
            public string Material;
            public string MaterialCreate;
            public string MainTex;
            public string ColorMask;

            public ColorMatchPart(string obj, string mat, string matCreate, string mainTex, string colorMask)
            {
                Object = obj.IsNullOrWhiteSpace() ? null : obj;
                Material = mat.IsNullOrWhiteSpace() ? null : mat;
                MaterialCreate = matCreate.IsNullOrWhiteSpace() ? null : matCreate;
                MainTex = mainTex.IsNullOrWhiteSpace() ? null : mainTex;
                ColorMask = colorMask.IsNullOrWhiteSpace() ? null : colorMask;
            }

#if AI || HS2
            public bool Verify() => Object != null;
#else
            public bool Verify() => Object != null && Material != null && MaterialCreate != null && MainTex != null && ColorMask != null;
#endif
        }

        public class MigrationData
        {
            public string UncensorGUID;
            public string BodyGUID;
            public string PenisGUID;
            public string BallsGUID;

            public MigrationData(XContainer migrationXMLData)
            {
                if (migrationXMLData == null)
                    return;

                UncensorGUID = migrationXMLData.Element("guidUncensor")?.Value;
                BodyGUID = migrationXMLData.Element("guidBody")?.Value;
                PenisGUID = migrationXMLData.Element("guidPenis")?.Value;
                BallsGUID = migrationXMLData.Element("guidBalls")?.Value;

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null for easier checks
                UncensorGUID = UncensorGUID.IsNullOrWhiteSpace() ? null : UncensorGUID;
                BodyGUID = BodyGUID.IsNullOrWhiteSpace() ? null : BodyGUID;
                PenisGUID = PenisGUID.IsNullOrWhiteSpace() ? null : PenisGUID;
                BallsGUID = BallsGUID.IsNullOrWhiteSpace() ? null : BallsGUID;
            }
        }

        public static class Defaults
        {
            public static readonly string OOBase = "chara/oo_base.unity3d";
            public static readonly string MMBase = "chara/mm_base.unity3d";
            public static readonly string AssetMale = "p_cm_body_00";
            public static readonly string AssetFemale = "p_cf_body_00";
            public static readonly string BodyMainTex = "cf_body_00_t";
            public static readonly string BodyColorMaskMale = "cm_body_00_mc";
            public static readonly string BodyColorMaskFemale = "cf_body_00_mc";
            public static readonly string Normals = "p_cf_body_00_Nml";
            public static readonly string BodyMaterialMale = "cm_m_body";
            public static readonly string BodyMaterialFemale = "cf_m_body";
            public static readonly string BodyMaterialCreate = "cf_m_body_create";
        }
    }
}