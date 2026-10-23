using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
internal static class DamageTagValidation
{
	internal static void RequireNamespaced(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value) || value.IndexOf(':') <= 0 || value.EndsWith(':') || value.Any(char.IsWhiteSpace))
		{
			throw new ArgumentException($"'{value}' is not a namespaced damage key.", parameterName);
		}
	}
}
