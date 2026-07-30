using KK_Plugins.MaterialEditor;

internal static class SiruSyncPolicyTests
{
    internal static void Run()
    {
        PendingWriteLifecycleIsBounded();
        MaterialFamiliesAreMatchedExactly();
    }

    private static void PendingWriteLifecycleIsBounded()
    {
        var pendingWrites = new SiruPendingWriteBuffer<string>();
        pendingWrites.Set("face", 1);
        pendingWrites.Set("body", 2);
        Dictionary<string, byte> readyWrites =
            pendingWrites.CollectReadyWrites(part => part == "face");

        Equal(1, readyWrites.Count, "ready siru write count");
        Equal(1, pendingWrites.Count, "unavailable siru write discarded");

        pendingWrites.Set("face", 3);
        pendingWrites.Complete(readyWrites);
        Equal(
            1,
            pendingWrites.Count,
            "newer siru write survives completion of an older batch");

        readyWrites = pendingWrites.CollectReadyWrites(part => true);
        pendingWrites.Complete(readyWrites);
        Equal(0, pendingWrites.Count, "completed siru writes removed");

        Equal(
            true,
            SiruSyncPolicy.CanApplyPendingWrite(
                highPoly: true,
                primaryMaterialAvailable: true),
            "available siru target");
        Equal(
            false,
            SiruSyncPolicy.CanApplyPendingWrite(
                highPoly: false,
                primaryMaterialAvailable: true),
            "low-poly siru target");
        Equal(
            false,
            SiruSyncPolicy.CanApplyPendingWrite(
                highPoly: true,
                primaryMaterialAvailable: false),
            "missing siru material");
    }

    private static void MaterialFamiliesAreMatchedExactly()
    {
        var familyNames = new[] { "cf_m_body" };
        Equal(
            true,
            SiruSyncPolicy.MaterialNameMatchesFamily(
                "cf_m_body",
                familyNames,
                ".MECopy"),
            "original material family");
        Equal(
            true,
            SiruSyncPolicy.MaterialNameMatchesFamily(
                "cf_m_body.MECopy2",
                familyNames,
                ".MECopy"),
            "copied material family");
        Equal(
            false,
            SiruSyncPolicy.MaterialNameMatchesFamily(
                "cf_m_body_extra",
                familyNames,
                ".MECopy"),
            "similarly-prefixed unrelated material");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
        }
    }
}
