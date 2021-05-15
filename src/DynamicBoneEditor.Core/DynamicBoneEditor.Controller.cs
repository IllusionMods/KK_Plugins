using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif

namespace KK_Plugins.DynamicBoneEditor
{
    public class CharaController : CharaCustomFunctionController
    {
        private List<DynamicBoneData> AccessoryDynamicBoneData = new List<DynamicBoneData>();
        private static readonly HashSet<DynamicBone> DBsToUpdate = new HashSet<DynamicBone>();

#if KK
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#else
        public int CurrentCoordinateIndex => 0;
#endif

        private void LateUpdate()
        {
            if (DBsToUpdate.Count != 0)
            {
                foreach (DynamicBone dynamicBone in DBsToUpdate)
                {
                    if (dynamicBone == null)
                        continue;
                    dynamicBone.GetType().GetMethod("SetupParticles", AccessTools.all).Invoke(dynamicBone, null);
                    dynamicBone.GetType().GetMethod("InitTransforms", AccessTools.all).Invoke(dynamicBone, null);
                }
                DBsToUpdate.Clear();
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (AccessoryDynamicBoneData.Count == 0)
            {
                SetExtendedData(null);
            }
            else
            {
                var data = new PluginData();
                data.data.Add(nameof(AccessoryDynamicBoneData), MessagePackSerializer.Serialize(AccessoryDynamicBoneData));
                SetExtendedData(data);
            }
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            var loadFlags = MakerAPI.GetCharacterLoadFlags();
            if (loadFlags == null || loadFlags.Clothes)
            {
                AccessoryDynamicBoneData.Clear();
                var data = GetExtendedData();
                if (data?.data != null)
                {
                    if (data.data.TryGetValue(nameof(AccessoryDynamicBoneData), out var loadedAccessoryDynamicBoneData) && loadedAccessoryDynamicBoneData != null)
                    {
                        AccessoryDynamicBoneData = MessagePackSerializer.Deserialize<List<DynamicBoneData>>((byte[])loadedAccessoryDynamicBoneData);
                    }
                }
            }
            StartCoroutine(ApplyData());

            base.OnReload(currentGameMode);
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            var coordinateAccessoryDynamicBoneData = AccessoryDynamicBoneData.Where(x => x.CoordinateIndex == CurrentCoordinateIndex).ToList();

            if (coordinateAccessoryDynamicBoneData.Count == 0)
            {
                SetCoordinateExtendedData(coordinate, null);
            }
            else
            {
                var data = new PluginData();
                if (coordinateAccessoryDynamicBoneData.Count > 0)
                {
                    data.data.Add(nameof(AccessoryDynamicBoneData), MessagePackSerializer.Serialize(coordinateAccessoryDynamicBoneData));
                }
                else
                {
                    data.data.Add(nameof(AccessoryDynamicBoneData), null);
                }
                SetCoordinateExtendedData(coordinate, data);
            }
            base.OnCoordinateBeingSaved(coordinate);
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            var loadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (loadFlags == null || loadFlags.Accessories)
            {
                AccessoryDynamicBoneData.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex);

                var data = GetCoordinateExtendedData(coordinate);
                if (data?.data != null)
                {
                    if (data.data.TryGetValue(nameof(AccessoryDynamicBoneData), out var loadedAccessoryDynamicBoneData) && loadedAccessoryDynamicBoneData != null)
                    {
                        var loadedAccessoryDynamicBoneDataList = MessagePackSerializer.Deserialize<List<DynamicBoneData>>((byte[])loadedAccessoryDynamicBoneData);
                        foreach (var dbData in loadedAccessoryDynamicBoneDataList)
                        {
                            dbData.CoordinateIndex = CurrentCoordinateIndex;
                            AccessoryDynamicBoneData.Add(dbData);
                        }
                    }
                }
            }
            StartCoroutine(ApplyData());

            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        internal void AccessoryKindChangeEvent(object sender, AccessorySlotEventArgs e)
        {
            AccessoryDynamicBoneData.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SlotIndex);

            if (MakerAPI.InsideAndLoaded && UI.Visible)
                UI.ShowUI(0);
        }

        internal void AccessoryTransferredEvent(object sender, AccessoryTransferEventArgs e)
        {
            AccessoryDynamicBoneData.RemoveAll(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.DestinationSlotIndex);

            List<DynamicBoneData> newAccessoryDynamicBoneData = new List<DynamicBoneData>();

            foreach (var dbData in AccessoryDynamicBoneData.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == e.SourceSlotIndex))
            {
                var newDBData = new DynamicBoneData(dbData.CoordinateIndex, e.DestinationSlotIndex, dbData.BoneName);
                dbData.CopyTo(newDBData);
                newAccessoryDynamicBoneData.Add(newDBData);
            }
            AccessoryDynamicBoneData.AddRange(newAccessoryDynamicBoneData);

            if (MakerAPI.InsideAndLoaded)
                UI.Visible = false;

            StartCoroutine(ApplyData());
        }

#if KK
        internal void AccessoriesCopiedEvent(object sender, AccessoryCopyEventArgs e)
        {
            foreach (int slot in e.CopiedSlotIndexes)
            {
                AccessoryDynamicBoneData.RemoveAll(x => x.CoordinateIndex == (int)e.CopyDestination && x.Slot == slot);

                List<DynamicBoneData> newAccessoryDynamicBoneData = new List<DynamicBoneData>();

                foreach (var dbData in AccessoryDynamicBoneData.Where(x => x.CoordinateIndex == (int)e.CopySource && x.Slot == slot))
                {
                    var newDBData = new DynamicBoneData((int)e.CopyDestination, slot, dbData.BoneName);
                    dbData.CopyTo(newDBData);
                    newAccessoryDynamicBoneData.Add(newDBData);
                }
                AccessoryDynamicBoneData.AddRange(newAccessoryDynamicBoneData);

                if (MakerAPI.InsideAndLoaded)
                    if ((int)e.CopyDestination == CurrentCoordinateIndex)
                        UI.Visible = false;
            }
        }
#endif

        private IEnumerator ApplyData()
        {
            yield return null;
#if !EC
            if (KKAPI.Studio.StudioAPI.InsideStudio)
            {
                yield return null;
                yield return null;
            }
#endif
            while (ChaControl == null || ChaControl.GetHead() == null)
                yield return null;

            UI.ToggleButtonVisibility();

            foreach (var dbData in AccessoryDynamicBoneData)
            {
                if (dbData.CoordinateIndex == CurrentCoordinateIndex)
                {
                    var accessory = ChaControl.GetAccessoryObject(dbData.Slot);
                    if (accessory != null)
                    {
                        var dynamicBones = accessory.GetComponentsInChildren<DynamicBone>();
                        foreach (var dynamicBone in dynamicBones)
                        {
                            if (dynamicBone.m_Root != null && dynamicBone.m_Root.name == dbData.BoneName)
                            {
                                if (dbData.FreezeAxis != null)
                                {
                                    dynamicBone.m_FreezeAxis = (DynamicBone.FreezeAxis)dbData.FreezeAxis;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Weight != null)
                                {
                                    dynamicBone.SetWeight((float)dbData.Weight);
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Damping != null)
                                {
                                    dynamicBone.m_Damping = (float)dbData.Damping;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Elasticity != null)
                                {
                                    dynamicBone.m_Elasticity = (float)dbData.Elasticity;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Stiffness != null)
                                {
                                    dynamicBone.m_Stiffness = (float)dbData.Stiffness;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Inertia != null)
                                {
                                    dynamicBone.m_Inert = (float)dbData.Inertia;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                                if (dbData.Radius != null)
                                {
                                    dynamicBone.m_Radius = (float)dbData.Radius;
                                    DBsToUpdate.Add(dynamicBone);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void CoordinateChangeEvent()
        {
            StartCoroutine(ApplyData());

            if (MakerAPI.InsideAndLoaded)
                UI.Visible = false;
        }

        private DynamicBoneData GetDBData(int slot, string boneName)
        {
            return AccessoryDynamicBoneData.FirstOrDefault(x => x.CoordinateIndex == CurrentCoordinateIndex && x.Slot == slot && x.BoneName == boneName);
        }

        public DynamicBone.FreezeAxis? GetFreezeAxis(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.FreezeAxis;
        }

        public void SetFreezeAxis(int slot, DynamicBone dynamicBone, DynamicBone.FreezeAxis value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetFreezeAxisOriginal(dynamicBone) == value)
            {
                dbData.FreezeAxis = null;
            }
            else
            {
                dbData.FreezeAxis = value;
            }
            dynamicBone.m_FreezeAxis = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public DynamicBone.FreezeAxis GetFreezeAxisOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_FreezeAxis;
            return dbData.GetFreezeAxisOriginal(dynamicBone);
        }

        public float? GetWeight(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Weight;
        }

        public void SetWeight(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetWeightOriginal(dynamicBone) == value)
            {
                dbData.Weight = null;
            }
            else
            {
                dbData.Weight = value;
            }
            dynamicBone.SetWeight(value);
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetWeightOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.GetWeight();
            return dbData.GetWeightOriginal(dynamicBone);
        }

        public float? GetDamping(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Damping;
        }

        public void SetDamping(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetDampingOriginal(dynamicBone) == value)
            {
                dbData.Damping = null;
            }
            else
            {
                dbData.Damping = value;
            }
            dynamicBone.m_Damping = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetDampingOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_Damping;
            return dbData.GetDampingOriginal(dynamicBone);
        }

        public float? GetElasticity(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Elasticity;
        }

        public void SetElasticity(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetElasticityOriginal(dynamicBone) == value)
            {
                dbData.Elasticity = null;
            }
            else
            {
                dbData.Elasticity = value;
            }
            dynamicBone.m_Elasticity = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetElasticityOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_Elasticity;
            return dbData.GetElasticityOriginal(dynamicBone);
        }

        public float? GetStiffness(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Stiffness;
        }

        public void SetStiffness(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetStiffnessOriginal(dynamicBone) == value)
            {
                dbData.Stiffness = null;
            }
            else
            {
                dbData.Stiffness = value;
            }
            dynamicBone.m_Stiffness = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetStiffnessOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_Stiffness;
            return dbData.GetStiffnessOriginal(dynamicBone);
        }

        public float? GetInertia(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Inertia;
        }

        public void SetInertia(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetInertiaOriginal(dynamicBone) == value)
            {
                dbData.Inertia = null;
            }
            else
            {
                dbData.Inertia = value;
            }
            dynamicBone.m_Inert = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetInertiaOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_Inert;
            return dbData.GetInertiaOriginal(dynamicBone);
        }

        public float? GetRadius(int slot, DynamicBone dynamicBone)
        {
            return GetDBData(slot, dynamicBone.m_Root.name)?.Radius;
        }

        public void SetRadius(int slot, DynamicBone dynamicBone, float value)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
            {
                dbData = new DynamicBoneData(CurrentCoordinateIndex, slot, dynamicBone.m_Root.name);
                AccessoryDynamicBoneData.Add(dbData);
            }
            if (dbData.GetRadiusOriginal(dynamicBone) == value)
            {
                dbData.Radius = null;
            }
            else
            {
                dbData.Radius = value;
            }
            dynamicBone.m_Radius = value;
            DBsToUpdate.Add(dynamicBone);
        }

        public float GetRadiusOriginal(int slot, DynamicBone dynamicBone)
        {
            var dbData = GetDBData(slot, dynamicBone.m_Root.name);
            if (dbData == null)
                return dynamicBone.m_Radius;
            return dbData.GetRadiusOriginal(dynamicBone);
        }
    }
}
