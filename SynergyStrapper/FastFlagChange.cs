namespace SynergyStrapper
{
    public enum FastFlagChangeKind
    {
        Added,
        Removed,
        Changed
    }

    public sealed record FastFlagChange(
        string Name,
        FastFlagChangeKind Kind,
        string? Before,
        string? After
    );
}
