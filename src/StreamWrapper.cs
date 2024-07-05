using System.Text;

namespace Myitian.HttpRcon;

public class StreamWrapper(Stream stream)
{
    public Stream BaseStream => stream;

    public void WriteNull()
    {
        stream.WriteByte(0);
    }
    public void ReadNull()
    {
        stream.ReadByte();
    }
    public int ReadByte()
    {
        return stream.ReadByte();
    }

    public void WriteInt(int value)
    {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 24));
    }

    public int ReadInt()
    {
        int a = ReadByte();
        int b = ReadByte();
        int c = ReadByte();
        int d = ReadByte();
        if (d < 0)
        {
            throw new EndOfStreamException();
        }
        return (d << 24) | ((c << 16) & 0xFF) | ((b << 8) & 0xFF) | (a & 0xFF);
    }

    public void WriteString(ReadOnlySpan<char> value, Encoding encoding)
    {
        Encoder encoder = encoding.GetEncoder();
        Span<byte> writeBuffer = stackalloc byte[2048];
        bool completed = false;
        while (!completed)
        {
            encoder.Convert(value, writeBuffer, false, out int charsUsed, out int bytesUsed, out completed);
            value = value[charsUsed..];
            stream.Write(writeBuffer[..bytesUsed]);
        }
        WriteNull();
    }

    public string ReadString(Encoding encoding, int length)
    {
        Decoder decoder = encoding.GetDecoder();
        StringBuilder sb = new();
        Span<byte> readBuffer = stackalloc byte[2048];
        Span<char> charBuffer = stackalloc char[2048];
        Span<char> charBuffer_t = charBuffer;
        int dataRemaining = length;
        int bufferRemaining = 0;
        int pos = 0;
        int processed = 0;
        bool hasMore = true;
        while (processed < length)
        {
            if (hasMore)
            {
                readBuffer.Slice(pos, bufferRemaining).CopyTo(readBuffer);
                pos = 0;
                int maxRead = readBuffer.Length - bufferRemaining;
                if (maxRead > dataRemaining)
                    maxRead = dataRemaining;
                int read = stream.Read(readBuffer.Slice(bufferRemaining, maxRead));
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                bufferRemaining += read;
                dataRemaining -= read;
                if (dataRemaining == 0)
                {
                    hasMore = false;
                }
            }
            Span<byte> readBuffer_t = readBuffer.Slice(pos, bufferRemaining);
            decoder.Convert(readBuffer_t, charBuffer, !hasMore, out int bytesUsed, out int charsUsed, out _);
            sb.Append(charBuffer[..charsUsed]);
            pos += bytesUsed;
            bufferRemaining -= bytesUsed;
            processed += bytesUsed;
        }
        ReadNull();
        return sb.ToString();
    }
}