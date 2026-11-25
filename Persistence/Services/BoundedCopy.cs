using System.Buffers;

namespace Persistence.Services;

internal static class BoundedCopy
{
    /// <summary>
    /// Copies at most <paramref name="maxBytes"/> bytes; false, with the copy left incomplete, when the source has more.
    /// The size an archive declares for an entry is not trusted: only the bytes actually written are counted.
    /// </summary>
    public static bool TryCopy(Stream source, Stream destination, long maxBytes, out long copiedBytes)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81_920);
        try
        {
            copiedBytes = 0;
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (copiedBytes + read > maxBytes)
                {
                    return false;
                }

                destination.Write(buffer, 0, read);
                copiedBytes += read;
            }

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
