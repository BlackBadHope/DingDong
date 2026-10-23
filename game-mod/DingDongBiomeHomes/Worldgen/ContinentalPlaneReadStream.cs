using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using Ionic.Crc;

namespace DingDongBiomeHomes.Worldgen;

// TileIO consumes the plane sequentially. A fixed buffer replaces the former
// raw-sized MemoryStream plus ToArray allocation. Validate even if TileIO stops
// exactly after its final record (without asking gzip for EOF).
internal sealed class ContinentalPlaneReadStream : Stream
{
	private readonly GZipStream gzip;
	private readonly CRC32 checksum = new();
	private readonly byte[] buffer = new byte[81_920];
	private readonly uint expectedCrc, expectedSize;
	private readonly long cap;
	private readonly int headerLength;
	private int position, count;
	private long decoded;
	private bool verified, failed, disposed;

	internal ContinentalPlaneReadStream(byte[] source, int offset, int length, int headerLength, long cap)
	{
		if (length < 18 || source[offset] != 0x1f || source[offset + 1] != 0x8b || source[offset + 2] != 8)
			throw new InvalidDataException("DingDong gzip plane header is invalid.");
		expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset + length - 8, 4));
		expectedSize = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset + length - 4, 4));
		this.headerLength = headerLength;
		this.cap = cap;
		gzip = new GZipStream(new MemoryStream(source, offset, length, writable: false), CompressionMode.Decompress);
	}

	private bool Fill()
	{
		if (verified) return false;
		try
		{
			position = 0;
			count = gzip.Read(buffer, 0, buffer.Length);
			decoded += count;
			if (decoded > cap) throw new InvalidDataException("DingDong save-plane exceeds the decode cap.");
			if (count > 0)
			{
				checksum.SlurpBlock(buffer, 0, count);
				return true;
			}
			if (decoded <= 0 || decoded != expectedSize || unchecked((uint)checksum.Crc32Result) != expectedCrc)
				throw new InvalidDataException("DingDong save-plane gzip length/CRC mismatch.");
			verified = true;
			if (decoded != headerLength)
				DingDongBiomeHomes.Instance?.Logger.Warn($"DINGDONG_SAVE_PLANE_LENGTH_DRIFT|header={headerLength}|actual={decoded}|accepted=bounded-gzip");
			return false;
		}
		catch { failed = true; throw; }
	}

	public override int Read(Span<byte> target)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		if (target.IsEmpty) return 0;
		if (position == count && !Fill()) return 0;
		int length = Math.Min(target.Length, count - position);
		buffer.AsSpan(position, length).CopyTo(target);
		position += length;
		return length;
	}

	public override int Read(byte[] target, int offset, int length) => Read(target.AsSpan(offset, length));
	public override int ReadByte()
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		return position == count && !Fill() ? -1 : buffer[position++];
	}

	protected override void Dispose(bool disposing)
	{
		if (disposed) return;
		try
		{
			if (disposing && !failed) while (Fill()) { }
		}
		finally
		{
			disposed = true;
			if (disposing) gzip.Dispose();
			base.Dispose(disposing);
		}
	}

	public override bool CanRead => !disposed;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
	public override void Flush() { }
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] source, int offset, int length) => throw new NotSupportedException();
}
