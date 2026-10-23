using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

/// <summary>
/// Owns only native container records newly allocated by an authored import.
/// Tile snapshots and existing-container relocation have separate ownership.
/// </summary>
internal sealed class AuthoredNativeContainerTransaction : IDisposable
{
	[ThreadStatic]
	private static AuthoredNativeContainerTransaction current;

	private readonly AuthoredNativeContainerTransaction parent;
	private readonly List<(int Index, Chest Created)> registrations = new();
	private bool committed;
	private bool disposed;

	private AuthoredNativeContainerTransaction()
	{
		parent = current;
		current = this;
	}

	internal static AuthoredNativeContainerTransaction Begin()
	{
		if (current?.committed == true)
			throw new InvalidOperationException("Cannot nest a container transaction inside a committed scope.");
		return new AuthoredNativeContainerTransaction();
	}

	/// <summary>
	/// Call immediately after successful CreateChest, never for a pre-existing record.
	/// A committed inner import remains owned by any enclosing preview/probe scope.
	/// </summary>
	internal static void Record(int index, Chest created)
	{
		if (current is null || current.disposed || current.committed)
			throw new InvalidOperationException("New authored containers require an active uncommitted transaction.");
		if (created is null || Main.chest is null || index < 0 || index >= Main.chest.Length || !ReferenceEquals(Main.chest[index], created))
			throw new InvalidOperationException("Authored container registration does not match the newly allocated native record.");
		current.registrations.Add((index, created));
	}

	internal void Commit()
	{
		RequireCurrentScope();
		committed = true;
	}

	public void Dispose()
	{
		if (disposed)
			return;
		RequireCurrentScope();
		try
		{
			if (committed)
			{
				// Only the outermost successful scope relinquishes rollback ownership.
				parent?.registrations.AddRange(registrations);
			}
			else
			{
				for (int i = registrations.Count - 1; i >= 0; i--)
				{
					(int index, Chest created) = registrations[i];
					if (Main.chest is not null && index < Main.chest.Length && ReferenceEquals(Main.chest[index], created))
						Main.chest[index] = null;
				}
			}
		}
		finally
		{
			registrations.Clear();
			disposed = true;
			current = parent;
		}
	}

	private void RequireCurrentScope()
	{
		if (disposed || !ReferenceEquals(current, this))
			throw new InvalidOperationException("Container transactions must complete in nesting order on their owning thread.");
	}
}
