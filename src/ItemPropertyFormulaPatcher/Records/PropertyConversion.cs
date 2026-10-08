namespace ItemPropertyFormulaPatcher.Records;

public enum StorageKind { UInt32, UInt16, Int32, Float32, ArmorRating }

public static class PropertyConversion
{
    // ARMO DNAM is a UInt32 count of hundredths. Largest safe float below its limit.
    public static readonly float MaxArmorRating = MathF.BitDecrement((float)(uint.MaxValue / 100d));

    public static double Normalize(double value, StorageKind kind)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Property results must be finite.");
        return kind switch
        {
            StorageKind.UInt32 => Math.Round(Math.Clamp(value, 0, uint.MaxValue), MidpointRounding.AwayFromZero),
            StorageKind.UInt16 => Math.Round(Math.Clamp(value, 0, ushort.MaxValue), MidpointRounding.AwayFromZero),
            StorageKind.Int32 => Math.Round(Math.Clamp(value, 0, int.MaxValue), MidpointRounding.AwayFromZero),
            StorageKind.Float32 => (float)Math.Clamp(value, 0, float.MaxValue),
            StorageKind.ArmorRating => (float)Math.Clamp(value, 0, MaxArmorRating),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
