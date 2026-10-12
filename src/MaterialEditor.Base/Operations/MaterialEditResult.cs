namespace MaterialEditorAPI
{
    internal enum MaterialEditStatus { Succeeded, Failed }

    /// <summary>Internal transaction result; legacy public void APIs remain unchanged.</summary>
    internal sealed class MaterialEditResult
    {
        internal readonly MaterialEditStatus Status;
        internal readonly string Stage;
        internal readonly string Diagnostic;
        internal bool Succeeded => Status == MaterialEditStatus.Succeeded;

        internal MaterialEditResult(MaterialEditStatus status, string stage = null, string diagnostic = null)
        {
            Status = status;
            Stage = stage;
            Diagnostic = diagnostic;
        }
    }
}
