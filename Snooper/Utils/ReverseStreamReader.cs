using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snooper.Utils;

/// <summary>
/// Simple reverse line reader for reading lines from the end of a file.
/// Assumes UTF-8. Also assumes the file is not currently open for writing or appending.
/// </summary>
public class ReverseStreamReader: IDisposable
{
	private readonly FileStream stream;
	private long end;

	public ReverseStreamReader(string fileName)
	{
		stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		end = stream.Length;
	}

	public string? ReadLine()
	{
		if (end == 0)
		{
			return null;
		}

		List<byte[]> buffers = new();

		while (end > 0) {
			byte[] buf = new byte[Math.Min(4096, end)];
			stream.Seek(end - buf.Length, SeekOrigin.Begin);
			FillBuffer(stream, buf);
			
			for (int i = buf.Length - 1; i >= 0; i--)
			{
				if (buf[i] == 0x0a) // LF
				{
					byte[] tail = new byte[buf.Length - i - 1];
					Array.Copy(buf, i + 1, tail, 0, tail.Length);
					buffers.Add(tail);
					end -= buf.Length - i;
					return ReverseAndConcatenateAsString(buffers);
				}
			}

			// Haven't found a newline so far - keep searching backward
			buffers.Add(buf);
			end -= buf.Length;
		}

		// We've read to the beginning and haven't found a newline
		return ReverseAndConcatenateAsString(buffers);
	}

	private static string ReverseAndConcatenateAsString(List<byte[]> buffers)
	{
		if (buffers.Count == 0)
		{
			return "";
		}

		byte[] concatenatedBuffer = buffers.Reverse<byte[]>()
				.SelectMany(x => x)
				.ToArray();

		// Trim trailing CR if present
		if (concatenatedBuffer.Length > 0 && concatenatedBuffer[^1] == 0x0d)
		{
			return Encoding.UTF8.GetString(concatenatedBuffer, 0, concatenatedBuffer.Length - 1);
		}

		return Encoding.UTF8.GetString(concatenatedBuffer);
	}

	private static byte[] FillBuffer(Stream input, byte[] buffer)
	{
		int index = 0;
		int bytesToRead = buffer.Length;

		while (index < bytesToRead)
		{
			int read = input.Read(buffer, index, bytesToRead - index);

			if (read == 0)
			{
				throw new EndOfStreamException
					(string.Format("End of stream reached with {0} byte{1} left to read.",
									bytesToRead - index,
									bytesToRead - index == 1 ? "s" : ""));
			}

			index += read;
		}

		return buffer;
	}
	
    public void Dispose()
    {
        stream.Dispose();
    }
}