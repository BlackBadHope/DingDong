namespace DingDongBiomeHomes.Items.AccessoryEnchanting;

// Per connection, never persisted with the player's items. One in-flight request
// keeps the ordinary client-owned inventory and the server-owned station atomic.
internal sealed class AccessoryStationSession
{
	internal const uint RequestTimeoutTicks = 900;
	private uint nextRequest;
	private uint lastProcessed;
	private uint pendingSince;
	internal bool Ready { get; private set; }
	internal uint PendingRequest { get; private set; }
	internal bool Pending => PendingRequest != 0;
	internal bool Importing { get; private set; }
	internal bool InventoryLocked => Pending && !Importing;

	internal void Reset()
	{
		Ready = false;
		Importing = false;
		PendingRequest = nextRequest = lastProcessed = pendingSince = 0;
	}

	internal bool TryBegin(bool importing, uint now, out uint request)
	{
		request = 0;
		if (Pending || importing == Ready || nextRequest == uint.MaxValue)
			return false;
		request = PendingRequest = ++nextRequest;
		Importing = importing;
		pendingSince = now;
		return true;
	}

	internal bool AcceptImport(uint request)
	{
		if (Ready || request == 0)
			return false;
		Ready = true;
		lastProcessed = request;
		return true;
	}

	internal bool AcceptRequest(uint request)
	{
		if (!Ready || request == 0 || request <= lastProcessed)
			return false;
		lastProcessed = request;
		return true;
	}

	internal bool Complete(uint request)
	{
		if (!Pending || request != PendingRequest)
			return false;
		PendingRequest = 0;
		Importing = false;
		Ready = true;
		return true;
	}

	internal bool TimedOut(uint now) => Pending && unchecked(now - pendingSince) >= RequestTimeoutTicks;
}
