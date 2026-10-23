using System;

namespace DingDongBiomeHomes.Worldgen;

// A rejected estimate is not an allocation failure. tML's first-chance
// OutOfMemoryException handler terminates the client, even if it is caught.
internal sealed class ContinentalMemoryAdmissionException : Exception
{
	internal ContinentalMemoryAdmissionException(string message) : base(message) { }
}
