namespace KK_Plugins.MaterialEditor
{
    internal class ObjectData
    {
        public int Slot;
        public MaterialEditorCharaController.ObjectType ObjectType;

        public ObjectData(int slot)
        {
            Slot = slot;
        }

        public ObjectData(int slot, MaterialEditorCharaController.ObjectType objectType)
        {
            Slot = slot;
            ObjectType = objectType;
        }
    }
}
