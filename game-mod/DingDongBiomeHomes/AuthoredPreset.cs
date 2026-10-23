using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
public sealed record AuthoredPreset(string Id, string ResourcePath, int Width, int Height, int? GroundOffset, int HousingCapacity, string Author, string Source, string SourceRevision, string License, PresetRole Roles, AuthoredPlacementMode PlacementMode, bool PlacementReviewed, bool ContainsNbt);
