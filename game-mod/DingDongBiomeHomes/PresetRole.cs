using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
[Flags]
public enum PresetRole
{
	None = 0,
	Residence = 1 << 0,
	Civic = 1 << 1,
	Workshop = 1 << 2,
	Greenhouse = 1 << 3,
	Harbor = 1 << 4,
	Underground = 1 << 5,
	Pylon = 1 << 6,
	Decoration = 1 << 7,
	Utility = 1 << 8,
	PrivateHome = 1 << 9
}
