using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed class ServerAttackInstanceIdAllocator
{
	private ulong nextId;
	private bool exhausted;
	public ServerAttackInstanceIdAllocator(ulong firstId = 1)
	{
		if (firstId == 0)
			throw new ArgumentOutOfRangeException(nameof(firstId), "AttackInstanceId zero is reserved for unclassified attacks.");
		nextId = firstId;
	}

	public ulong Issue()
	{
		if (exhausted)
			throw new InvalidOperationException("The server AttackInstanceId space is exhausted for this session.");
		ulong issued = nextId;
		if (nextId == ulong.MaxValue)
			exhausted = true;
		else
			nextId++;
		return issued;
	}
}
