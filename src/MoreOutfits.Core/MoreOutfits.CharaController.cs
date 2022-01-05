using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System.Collections.Generic;

namespace KK_Plugins.MoreOutfits
{
    public class MoreOutfitsController : CharaCustomFunctionController
    {
        private Dictionary<int, string> CoordinateNames = new Dictionary<int, string>();

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (CoordinateNames.Count == 0)
            {
                SetExtendedData(null);
            }
            else
            {
                var data = new PluginData();
                data.data.Add(nameof(CoordinateNames), MessagePackSerializer.Serialize(CoordinateNames));
                SetExtendedData(data);
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState)
                return;

            var loadFlags = MakerAPI.GetCharacterLoadFlags();
            if (loadFlags == null || loadFlags.Clothes)
            {
                CoordinateNames.Clear();

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.data.TryGetValue(nameof(CoordinateNames), out var loadedCoordinateNames) && loadedCoordinateNames != null)
                    {
                        CoordinateNames = MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])loadedCoordinateNames);
                    }
                }
            }
        }

        public void SetCoordinateName(int index, string name)
        {
            CoordinateNames[index] = name.Replace("#", (index + 1).ToString());
        }

        public string GetCoodinateName(int index)
        {
            if (CoordinateNames.TryGetValue(index, out string name))
                return name;
            return $"Outfit {index + 1}";
        }
    }
}
