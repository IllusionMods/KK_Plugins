#if KK || KKS
using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        private const string LiquidFaceProperty = "liquidface";
        private const string LiquidFrontTopProperty = "liquidftop";
        private const string LiquidFrontBottomProperty = "liquidfbot";
        private const string LiquidBackTopProperty = "liquidbtop";
        private const string LiquidBackBottomProperty = "liquidbbot";

        private static readonly ChaFileDefine.SiruParts[] BodySiruParts =
        {
            ChaFileDefine.SiruParts.SiruFrontUp,
            ChaFileDefine.SiruParts.SiruFrontDown,
            ChaFileDefine.SiruParts.SiruBackUp,
            ChaFileDefine.SiruParts.SiruBackDown
        };

        private static readonly int[] SiruClothingSlots = { 0, 1, 2, 3, 5 };

        private readonly Dictionary<ChaFileDefine.SiruParts, byte> _pendingSiruWrites =
            new Dictionary<ChaFileDefine.SiruParts, byte>();

        internal void QueueSiruWrite(ChaFileDefine.SiruParts part, byte value)
        {
            if (GetSiruPropertyName(part) == null)
                return;

            _pendingSiruWrites[part] = value;

            // Studio only queues the vanilla Siru level here. Apply the explicitly
            // requested channel immediately so copied layers react with the UI.
            // UpdateSiru will apply it again after preserving unaffected channels.
            bool face = part == ChaFileDefine.SiruParts.SiruKao;
            if (ChaControl.hiPoly
                && (face ? ChaControl.customMatFace != null : ChaControl.customMatBody != null))
                ApplySiruWrite(part, value);
        }

        internal SiruUpdateSnapshot BeginSiruUpdate()
        {
            if (_pendingSiruWrites.Count == 0 || !ChaControl.hiPoly)
                return null;

            var writesReadyToApply = new Dictionary<ChaFileDefine.SiruParts, byte>();
            foreach (KeyValuePair<ChaFileDefine.SiruParts, byte> pendingWrite in _pendingSiruWrites)
            {
                bool face = pendingWrite.Key == ChaFileDefine.SiruParts.SiruKao;
                if (face ? ChaControl.customMatFace != null : ChaControl.customMatBody != null)
                    writesReadyToApply.Add(pendingWrite.Key, pendingWrite.Value);
            }

            if (writesReadyToApply.Count == 0)
                return null;

            var snapshot = new SiruUpdateSnapshot(writesReadyToApply);
            bool bodyWillUpdate = false;
            for (int i = 0; i < BodySiruParts.Length; i++)
            {
                if (snapshot.PendingWrites.ContainsKey(BodySiruParts[i]))
                {
                    bodyWillUpdate = true;
                    break;
                }
            }

            if (!bodyWillUpdate)
                return snapshot;

            // Vanilla rewrites all four body channels when any one of them changes.
            // Preserve the per-material values of channels that SetSiruFlags did not request.
            for (int i = 0; i < BodySiruParts.Length; i++)
            {
                ChaFileDefine.SiruParts part = BodySiruParts[i];
                if (snapshot.PendingWrites.ContainsKey(part))
                    continue;

                string propertyName = GetSiruPropertyName(part);
                List<SiruMaterialTarget> targets = GetSiruMaterialTargets(part);
                for (int j = 0; j < targets.Count; j++)
                {
                    Material material = targets[j].ResolveMaterial();
                    if (material != null)
                        snapshot.PreservedValues.Add(new PreservedSiruValue(targets[j], propertyName, material.GetFloat("_" + propertyName)));
                }
            }

            return snapshot;
        }

        internal void CompleteSiruUpdate(SiruUpdateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            try
            {
                for (int i = 0; i < snapshot.PreservedValues.Count; i++)
                    snapshot.PreservedValues[i].Restore();

                foreach (KeyValuePair<ChaFileDefine.SiruParts, byte> pendingWrite in snapshot.PendingWrites)
                    ApplySiruWrite(pendingWrite.Key, pendingWrite.Value);
            }
            finally
            {
                foreach (KeyValuePair<ChaFileDefine.SiruParts, byte> appliedWrite in snapshot.PendingWrites)
                {
                    if (_pendingSiruWrites.TryGetValue(appliedWrite.Key, out byte pendingValue)
                        && pendingValue == appliedWrite.Value)
                        _pendingSiruWrites.Remove(appliedWrite.Key);
                }
            }
        }

        internal void CancelSiruUpdate()
        {
            _pendingSiruWrites.Clear();
        }

        private void ApplySiruWrite(ChaFileDefine.SiruParts part, byte value)
        {
            string propertyName = GetSiruPropertyName(part);
            if (propertyName == null)
                return;

            var affectedMaterialNames = new HashSet<string>();
            List<SiruMaterialTarget> targets = GetSiruMaterialTargets(part);
            for (int i = 0; i < targets.Count; i++)
            {
                Material material = targets[i].ResolveMaterial();
                if (material == null)
                    continue;

                material.SetFloat("_" + propertyName, value);
                affectedMaterialNames.Add(material.NameFormatted());
            }

            RemovePersistedSiruOverrides(part, propertyName, affectedMaterialNames);
        }

        private void RemovePersistedSiruOverrides(ChaFileDefine.SiruParts part, string propertyName, HashSet<string> affectedMaterialNames)
        {
            if (affectedMaterialNames.Count == 0)
                return;

            bool face = part == ChaFileDefine.SiruParts.SiruKao;
            MaterialFloatPropertyList.RemoveAll(property =>
                property.Property == propertyName
                && affectedMaterialNames.Contains(property.MaterialName)
                && (face
                    ? property.ObjectType == ObjectType.Character
                    : property.ObjectType == ObjectType.Character
                      || property.ObjectType == ObjectType.Clothing
                         && property.CoordinateIndex == CurrentCoordinateIndex
                         && IsSiruClothingSlot(property.Slot)));
        }

        private List<SiruMaterialTarget> GetSiruMaterialTargets(ChaFileDefine.SiruParts part)
        {
            string propertyName = GetSiruPropertyName(part);
            var targets = new List<SiruMaterialTarget>();
            if (propertyName == null)
                return targets;

            var seenMaterials = new HashSet<Material>();
            var seenRenderers = new HashSet<Renderer>();
            bool face = part == ChaFileDefine.SiruParts.SiruKao;
            Renderer primaryRenderer = face ? ChaControl.rendFace : ChaControl.rendBody;
            Material primaryMaterial = face ? ChaControl.customMatFace : ChaControl.customMatBody;
            HashSet<string> characterMaterialNames = GetMaterialFamilyNames(primaryRenderer, primaryMaterial, propertyName);

            // Face and body shaders expose the same liquid properties. Use the materials
            // on the vanilla target renderer as the family boundary when finding copies.
            AddRendererTargets(
                primaryRenderer,
                propertyName,
                seenMaterials,
                seenRenderers,
                targets);

            foreach (Renderer renderer in GetRendererList(ChaControl.gameObject))
                AddRendererTargets(renderer, propertyName, seenMaterials, seenRenderers, targets, characterMaterialNames);

            AddDirectMaterialTarget(
                primaryMaterial,
                propertyName,
                seenMaterials,
                targets);

            if (!face)
            {
                for (int i = 0; i < SiruClothingSlots.Length; i++)
                {
                    int slot = SiruClothingSlots[i];
                    GameObject clothes = FindGameObject(ObjectType.Clothing, slot);
                    foreach (Renderer renderer in GetRendererList(clothes))
                        AddRendererTargets(renderer, propertyName, seenMaterials, seenRenderers, targets);
                }
            }

            return targets;
        }

        private static HashSet<string> GetMaterialFamilyNames(Renderer renderer, Material directMaterial, string propertyName)
        {
            var materialNames = new HashSet<string>();
            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material != null && material.HasProperty("_" + propertyName))
                        materialNames.Add(material.NameFormatted());
                }
            }

            if (directMaterial != null && directMaterial.HasProperty("_" + propertyName))
                materialNames.Add(directMaterial.NameFormatted());

            return materialNames;
        }

        private static void AddRendererTargets(
            Renderer renderer,
            string propertyName,
            HashSet<Material> seenMaterials,
            HashSet<Renderer> seenRenderers,
            List<SiruMaterialTarget> targets,
            HashSet<string> allowedMaterialNames = null)
        {
            if (renderer == null || !seenRenderers.Add(renderer))
                return;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null
                    || !material.HasProperty("_" + propertyName)
                    || allowedMaterialNames != null && !MaterialNameMatchesFamily(material.NameFormatted(), allowedMaterialNames)
                    || !seenMaterials.Add(material))
                    continue;

                targets.Add(new SiruMaterialTarget(renderer, i));
            }
        }

        private static bool MaterialNameMatchesFamily(string materialName, HashSet<string> familyNames)
        {
            foreach (string familyName in familyNames)
                if (materialName == familyName || materialName.StartsWith(familyName + MaterialCopyPostfix))
                    return true;

            return false;
        }

        private static void AddDirectMaterialTarget(
            Material material,
            string propertyName,
            HashSet<Material> seenMaterials,
            List<SiruMaterialTarget> targets)
        {
            if (material == null || !material.HasProperty("_" + propertyName) || !seenMaterials.Add(material))
                return;

            targets.Add(new SiruMaterialTarget(material));
        }

        private static bool IsSiruClothingSlot(int slot)
        {
            for (int i = 0; i < SiruClothingSlots.Length; i++)
                if (SiruClothingSlots[i] == slot)
                    return true;

            return false;
        }

        private static string GetSiruPropertyName(ChaFileDefine.SiruParts part)
        {
            switch (part)
            {
                case ChaFileDefine.SiruParts.SiruKao:
                    return LiquidFaceProperty;
                case ChaFileDefine.SiruParts.SiruFrontUp:
                    return LiquidFrontTopProperty;
                case ChaFileDefine.SiruParts.SiruFrontDown:
                    return LiquidFrontBottomProperty;
                case ChaFileDefine.SiruParts.SiruBackUp:
                    return LiquidBackTopProperty;
                case ChaFileDefine.SiruParts.SiruBackDown:
                    return LiquidBackBottomProperty;
                default:
                    return null;
            }
        }

        internal sealed class SiruUpdateSnapshot
        {
            internal readonly Dictionary<ChaFileDefine.SiruParts, byte> PendingWrites;
            internal readonly List<PreservedSiruValue> PreservedValues = new List<PreservedSiruValue>();

            internal SiruUpdateSnapshot(Dictionary<ChaFileDefine.SiruParts, byte> pendingWrites)
            {
                PendingWrites = new Dictionary<ChaFileDefine.SiruParts, byte>(pendingWrites);
            }
        }

        internal sealed class SiruMaterialTarget
        {
            private readonly Renderer _renderer;
            private readonly int _materialIndex;
            private readonly Material _material;

            internal SiruMaterialTarget(Renderer renderer, int materialIndex)
            {
                _renderer = renderer;
                _materialIndex = materialIndex;
            }

            internal SiruMaterialTarget(Material material)
            {
                _material = material;
                _materialIndex = -1;
            }

            internal Material ResolveMaterial()
            {
                if (_renderer != null)
                {
                    Material[] materials = _renderer.sharedMaterials;
                    if (_materialIndex >= 0 && _materialIndex < materials.Length)
                        return materials[_materialIndex];
                }

                return _material;
            }
        }

        internal sealed class PreservedSiruValue
        {
            private readonly SiruMaterialTarget _target;
            private readonly string _propertyName;
            private readonly float _value;

            internal PreservedSiruValue(SiruMaterialTarget target, string propertyName, float value)
            {
                _target = target;
                _propertyName = propertyName;
                _value = value;
            }

            internal void Restore()
            {
                Material material = _target.ResolveMaterial();
                if (material != null && material.HasProperty("_" + _propertyName))
                    material.SetFloat("_" + _propertyName, _value);
            }
        }
    }
}
#endif
