using System;
using System.IO;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

// MessageBuffer's reader spans the whole reusable buffer. GetData supplies the
// actual frame boundary; Mod.HandlePacket alone does not expose that length.
internal static class ContinentalSectionPacketEnvelope
{
	[ThreadStatic] private static long? packetEnd;
	private static bool installed;
	internal static void Install()
	{
		if (installed) return;
		On_MessageBuffer.GetData += ReadFrame;
		installed = true;
	}
	internal static void Uninstall()
	{
		if (!installed) return;
		On_MessageBuffer.GetData -= ReadFrame;
		installed = false;
	}
	private static void ReadFrame(On_MessageBuffer.orig_GetData orig,
		MessageBuffer self, int start, int length, out int messageType)
	{
		long? previous = packetEnd;
		packetEnd = (long)start + length;
		try { orig(self, start, length, out messageType); }
		finally { packetEnd = previous; }
	}
	internal static BinaryReader Read(BinaryReader source)
	{
		Stream stream = source.BaseStream;
		if (!stream.CanSeek) throw new IOException("Section packet reader must be seekable.");
		long end = packetEnd ?? stream.Length;
		long count = end - stream.Position;
		if (count < 0 || end > stream.Length) throw new IOException("Invalid section frame boundary.");
		if (count > 1 + ContinentalSectionStreamingNetwork.MaximumAckBatch * sizeof(uint))
		{
			stream.Position = end;
			throw new IOException("Oversized section packet.");
		}
		byte[] body = source.ReadBytes((int)count);
		if (body.Length != count) throw new EndOfStreamException();
		return new BinaryReader(new MemoryStream(body, writable: false));
	}
	internal static void Discard(BinaryReader source)
	{
		Stream stream = source.BaseStream;
		long end = packetEnd ?? stream.Length;
		if (stream.CanSeek && end >= stream.Position && end <= stream.Length) stream.Position = end;
	}
}
