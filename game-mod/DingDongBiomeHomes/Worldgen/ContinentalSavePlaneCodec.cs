using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSavePlaneCodec
{
	private static readonly byte[] Magic = Encoding.ASCII.GetBytes("DINGDDZ1");
	private static readonly byte[] EndMagic = Encoding.ASCII.GetBytes("DINGEND1");
	private const int HeaderBytes = 12;
	private const int CopyBufferBytes = 81_920;

	internal static MemoryStream Begin(int rawLength)
	{
		int capacity = Math.Clamp(rawLength / 512, 4096, 4 * 1024 * 1024);
		var output = new MemoryStream(capacity);
		Span<byte> header = stackalloc byte[HeaderBytes];
		Magic.CopyTo(header);
		BinaryPrimitives.WriteInt32LittleEndian(header[Magic.Length..], rawLength);
		output.Write(header);
		return output;
	}

	internal static BinaryWriter CreateEncodedWriter(Stream output) => new(
		new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true),
		Encoding.UTF8, leaveOpen: false);

	internal static BinaryWriter CreatePlainWriter(Stream output) =>
		new(output, Encoding.UTF8, leaveOpen: true);

	internal static void Seal(MemoryStream output) => output.Write(EndMagic);

	internal static void Validate()
	{
		var raw = new byte[16 * 1024];
		for (int i = 0; i < raw.Length; i += 257) raw[i] = (byte)i;
		byte[] encoded;
		using (MemoryStream output = Begin(raw.Length))
		{
			using (BinaryWriter writer = CreateEncodedWriter(output)) writer.Write(raw);
			Seal(output);
			encoded = output.ToArray();
		}
		if (!Decode(encoded).AsSpan().SequenceEqual(raw))
			throw new InvalidDataException("DingDong save-plane self-test mismatch.");
		byte[] legacy = { 1, 2, 3 };
		if (!ReferenceEquals(legacy, Decode(legacy)))
			throw new InvalidDataException("DingDong legacy save-plane fallback changed.");
		encoded[^1] ^= 0x5A;
		if (HasEndMagic(encoded))
			throw new InvalidDataException(
				"DingDong damaged save-plane was accepted.");
	}

	internal static byte[] Decode(byte[] source)
	{
		if (!IsEncoded(source)) return source;
		using Stream input = OpenRead(source);
		using var output = new MemoryStream();
		input.CopyTo(output, CopyBufferBytes);
		return output.ToArray();
	}

	internal static Stream OpenRead(byte[] source)
	{
		if (!IsEncoded(source)) return new MemoryStream(source, writable: false);
		int rawLength = BinaryPrimitives.ReadInt32LittleEndian(
			source.AsSpan(Magic.Length, sizeof(int)));
		if (rawLength <= 0 || rawLength > ContinentalSaveBudget.RawPlaneCapBytes)
			throw new InvalidDataException(
				$"Invalid DingDong save-plane length: {rawLength}.");
		if (!HasEndMagic(source))
			throw new InvalidDataException("DingDong save-plane trailer is missing.");
		return new ContinentalPlaneReadStream(source, HeaderBytes,
			source.Length - HeaderBytes - EndMagic.Length, rawLength,
			ContinentalSaveBudget.RawPlaneCapBytes);
	}

	private static bool IsEncoded(byte[] source)
	{
		if (source == null || source.Length < HeaderBytes) return false;
		for (int i = 0; i < Magic.Length; i++)
			if (source[i] != Magic[i]) return false;
		return true;
	}

	private static bool HasEndMagic(byte[] source)
	{
		int start = source.Length - EndMagic.Length;
		if (start < HeaderBytes) return false;
		for (int i = 0; i < EndMagic.Length; i++)
			if (source[start + i] != EndMagic[i]) return false;
		return true;
	}
}
