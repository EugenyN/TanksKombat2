using System.Collections.Generic;

namespace Unity.NetCode.Generators
{
    // Registers project-specific serialization templates for the NetCode source generator (partial class injected
    // into Unity.NetCode via asmref). Entries must exactly match the [GhostField] configuration (Type, SubType,
    // Quantized, Smoothing, Composite) of the fields that intend to use them.
    public static partial class UserDefinedTemplates
    {
        static partial void RegisterTemplates(List<TypeRegistryEntry> templates, string defaultRootPath)
        {
            templates.AddRange(new[]{

                // 2D position: float3 field marked [GhostField(..., SubType=GhostFieldSubType.Translation2D)]
                // serializes only x,y as quantized int deltas (Custom.Translation2d template, z forced to 0).
                new TypeRegistryEntry
                {
                    Type = "Unity.Mathematics.float3",
                    SubType = GhostFieldSubType.Translation2D,
                    Quantized = true,
                    Smoothing = SmoothingAction.Clamp,
                    SupportCommand = false,
                    Composite = false,
                    Template = "Custom.Translation2d",
                    TemplateOverride = "",
                },
                // 2D rotation: quaternion field marked [GhostField(..., SubType=GhostFieldSubType.Rotation2D)]
                // serializes a single signed w component as quantized int delta (Custom.Rotation2d template).
                new TypeRegistryEntry
                {
                    Type = "Unity.Mathematics.quaternion",
                    SubType = GhostFieldSubType.Rotation2D,
                    Quantized = true,
                    Smoothing = SmoothingAction.Clamp,
                    SupportCommand = false,
                    Composite = false,
                    Template = "Custom.Rotation2d",
                    TemplateOverride = "",
                }
            });
        }
    }
}
