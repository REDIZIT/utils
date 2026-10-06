using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class AsyncBinaryReader
	{
		private readonly Stream stream;
		private readonly CancellationToken token;
		
		public AsyncBinaryReader(Stream stream, CancellationToken token)
		{
			this.stream = stream;
			this.token = token;
		}

		public async Task<byte> ReadByte() => (await ReadBytes(1))[0];
		public async Task<int> ReadInt() => BitConverter.ToInt32(await ReadBytes(4), 0);
		
		public async Task<byte[]> ReadBytes(int bytesCount)
		{
			byte[] bytes = new byte[bytesCount];
			await stream.ReadExactlySafe(bytes, bytesCount, token);
			return bytes;
		}
	}
}