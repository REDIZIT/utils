using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class AsyncBinaryWriter
	{
		private readonly Stream stream;
		private readonly CancellationToken token;
		
		public AsyncBinaryWriter(Stream stream, CancellationToken token)
		{
			this.stream = stream;
			this.token = token;
		}

		public async Task Write(byte v) => await WriteBytes(new[] { v });
		public async Task Write(int v) => await WriteBytes(BitConverter.GetBytes(v));
		
		public async Task WriteBytes(byte[] bytes)
		{
			await stream.WriteAsync(bytes, token).ConfigureAwait(false);
		}
	}
}