using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System.Collections.Generic;
using UniRx;

namespace KK_Plugins
{
    public class ClothingUnlockerController : CharaCustomFunctionController
    {
        private Dictionary<int, bool> ClothingUnlocked;

#if KK
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#elif EC
        public int CurrentCoordinateIndex => 0;
#endif

#if KK
        protected override void Start()
        {
            CurrentCoordinate.Subscribe(value => { OnCoordinateChanged(); });

            base.Start();
        }
#endif

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            var data = new PluginData();
            data.data.Add(nameof(ClothingUnlocked), MessagePackSerializer.Serialize(ClothingUnlocked));
            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            ClothingUnlocked = new Dictionary<int, bool>();

            var data = GetExtendedData();
            if (data != null)
                if (data.data.TryGetValue(nameof(ClothingUnlocked), out var loadedClothingUnlocked))
                    ClothingUnlocked = MessagePackSerializer.Deserialize<Dictionary<int, bool>>((byte[])loadedClothingUnlocked);

            base.OnReload(currentGameMode, maintainState);
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            var data = new PluginData();
            data.data.Add(nameof(ClothingUnlocked) + "Coordinate", GetClothingUnlocked());
            SetCoordinateExtendedData(coordinate, data);
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            SetClothingUnlocked(false);

            var loadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (loadFlags == null || loadFlags.Clothes)
            {
                var data = GetCoordinateExtendedData(coordinate);
                if (data != null)
                    if (data.data.TryGetValue(nameof(ClothingUnlocked) + "Coordinate", out var loadedClothingUnlocked))
                        SetClothingUnlocked((bool)loadedClothingUnlocked);
            }

            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

#if KK
        private void OnCoordinateChanged()
        {
            if (MakerAPI.InsideAndLoaded)
                ClothingUnlocker.ClothingUnlockToggle.SetValue(GetClothingUnlocked());
        }
#endif

        /// <summary>
        /// Get whether clothing is unlocked for the current outfit slot
        /// </summary>
        /// <returns></returns>
        public bool GetClothingUnlocked() => GetClothingUnlocked(CurrentCoordinateIndex);
        /// <summary>
        /// Get whether clothing is unlocked for the specified outfit slot
        /// </summary>
        public bool GetClothingUnlocked(int slot)
        {
            //In H scenes and talk scenes this triggers before OnReload so we have to get it early sometimes
            if (ClothingUnlocked == null)
            {
                ClothingUnlocked = new Dictionary<int, bool>();
                var data = GetExtendedData();
                if (data != null)
                    if (data.data.TryGetValue(nameof(ClothingUnlocked), out var loadedClothingUnlocked))
                        ClothingUnlocked = MessagePackSerializer.Deserialize<Dictionary<int, bool>>((byte[])loadedClothingUnlocked);
            }

            if (ClothingUnlocked.TryGetValue(slot, out bool value))
                return value;
            return false;
        }

        /// <summary>
        /// Set whether clothing is unlocked for the current outfit slot
        /// </summary>
        public void SetClothingUnlocked(bool value) => SetClothingUnlocked(CurrentCoordinateIndex, value);
        /// <summary>
        /// Set whether clothing is unlocked for the specified outfit slot
        /// </summary>
        public void SetClothingUnlocked(int slot, bool value) => ClothingUnlocked[slot] = value;
    }
}
