namespace Unity.NetCode
{
    // User-defined ghost field sub-types: integer ids referenced by [GhostField(SubType=...)] to select
    // a custom serialization template registered in UserDefinedTemplates (injected into Unity.NetCode via asmref).
    public static partial class GhostFieldSubType
    {
        public const int Rotation2D = 1;
        public const int Translation2D = 2;
    }
}