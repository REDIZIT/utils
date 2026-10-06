using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public static class Extensions
	{
		public static async Task ReadExactlySafe(this Stream stream, byte[] buffer, int count, CancellationToken token)
		{
			int totalRead = 0;
			while (totalRead < count)
			{
				int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
				if (read == 0) throw new EndOfStreamException();
				totalRead += read;
			}
		}
	}
}