using UnityEngine;

namespace KK_UncensorSelector
{
    static class ColorMatch
    {
        /// <summary>
        /// Do color matching for every object configured in the manifest.xml
        /// </summary>
        public static void ColorMatchMaterials(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData, KK_UncensorSelector.PenisData penisData, KK_UncensorSelector.BallsData ballsData)
        {
            ColorMatchMaterials(chaControl, uncensorData);
            ColorMatchMaterials(chaControl, penisData);
            ColorMatchMaterials(chaControl, ballsData);
        }

        private static void ColorMatchMaterials(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData)
        {
            if (uncensorData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in uncensorData.ColorMatchList)
                ColorMatchMaterial(chaControl, colorMatchPart, uncensorData.OOBase);
        }

        private static void ColorMatchMaterials(ChaControl chaControl, KK_UncensorSelector.PenisData penisData)
        {
            if (penisData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                ColorMatchMaterial(chaControl, colorMatchPart, penisData.File);
        }

        private static void ColorMatchMaterials(ChaControl chaControl, KK_UncensorSelector.BallsData ballsData)
        {
            if (ballsData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                ColorMatchMaterial(chaControl, colorMatchPart, ballsData.File);
        }

        private static void ColorMatchMaterial(ChaControl chaControl, KK_UncensorSelector.ColorMatchPart colorMatchPart, string file)
        {
            //get main tex
            Texture2D mainTexture = CommonLib.LoadAsset<Texture2D>(file, colorMatchPart.MainTex, false, string.Empty);
            if (mainTexture == null)
                return;

            //get color mask
            Texture2D colorMask = CommonLib.LoadAsset<Texture2D>(file, colorMatchPart.ColorMask, false, string.Empty);
            if (colorMask == null)
                return;

            //find the game object
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(chaControl.objBody.transform);
            GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
            if (gameObject == null)
                return;

            var customTex = new CustomTextureControl(gameObject.transform);
            customTex.Initialize(file, colorMatchPart.Material, string.Empty, file, colorMatchPart.MaterialCreate, string.Empty, 2048, 2048);

            customTex.SetMainTexture(mainTexture);
            customTex.SetColor(ChaShader._Color, chaControl.chaFile.custom.body.skinMainColor);

            customTex.SetTexture(ChaShader._ColorMask, colorMask);
            customTex.SetColor(ChaShader._Color2, chaControl.chaFile.custom.body.skinSubColor);

            //set the new texture
            var newTex = customTex.RebuildTextureAndSetMaterial();
            if (newTex == null)
                return;

            Material mat = gameObject.GetComponent<Renderer>().material;
            var mt = mat.GetTexture(ChaShader._MainTex);
            mat.SetTexture(ChaShader._MainTex, newTex);
            //Destroy the old texture to prevent memory leak
            UnityEngine.Object.Destroy(mt);
        }
        /// <summary>
        /// Set the skin line visibility for every color matching object configured in the manifest.xml
        /// </summary>
        public static void SetLineVisibility(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData, KK_UncensorSelector.PenisData penisData, KK_UncensorSelector.BallsData ballsData)
        {
            SetLineVisibility(chaControl, uncensorData);
            SetLineVisibility(chaControl, penisData);
            SetLineVisibility(chaControl, ballsData);
        }

        private static void SetLineVisibility(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData)
        {
            if (uncensorData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in uncensorData.ColorMatchList)
                SetLineVisibility(chaControl, colorMatchPart);
        }

        private static void SetLineVisibility(ChaControl chaControl, KK_UncensorSelector.PenisData penisData)
        {
            if (penisData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                SetLineVisibility(chaControl, colorMatchPart);
        }

        private static void SetLineVisibility(ChaControl chaControl, KK_UncensorSelector.BallsData ballsData)
        {
            if (ballsData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                SetLineVisibility(chaControl, colorMatchPart);
        }

        private static void SetLineVisibility(ChaControl chaControl, KK_UncensorSelector.ColorMatchPart colorMatchPart)
        {
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(chaControl.objBody.transform);
            GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
            if (gameObject != null)
                gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, chaControl.chaFile.custom.body.drawAddLine ? 1f : 0f);
        }
        /// <summary>
        /// Set the skin gloss for every color matching object configured in the manifest.xml
        /// </summary>
        public static void SetSkinGloss(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData, KK_UncensorSelector.PenisData penisData, KK_UncensorSelector.BallsData ballsData)
        {
            SetSkinGloss(chaControl, uncensorData);
            SetSkinGloss(chaControl, penisData);
            SetSkinGloss(chaControl, ballsData);
        }

        private static void SetSkinGloss(ChaControl chaControl, KK_UncensorSelector.BodyData uncensorData)
        {
            if (uncensorData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in uncensorData.ColorMatchList)
                SetSkinGloss(chaControl, colorMatchPart);
        }

        private static void SetSkinGloss(ChaControl chaControl, KK_UncensorSelector.PenisData penisData)
        {
            if (penisData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in penisData.ColorMatchList)
                SetSkinGloss(chaControl, colorMatchPart);
        }

        private static void SetSkinGloss(ChaControl chaControl, KK_UncensorSelector.BallsData ballsData)
        {
            if (ballsData == null)
                return;

            foreach (KK_UncensorSelector.ColorMatchPart colorMatchPart in ballsData.ColorMatchList)
                SetSkinGloss(chaControl, colorMatchPart);
        }

        private static void SetSkinGloss(ChaControl chaControl, KK_UncensorSelector.ColorMatchPart colorMatchPart)
        {
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(chaControl.objBody.transform);
            GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
            if (gameObject != null)
                gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(chaControl.chaFile.custom.body.skinGlossPower, 1f, chaControl.chaFile.status.skinTuyaRate));
        }

    }
}
