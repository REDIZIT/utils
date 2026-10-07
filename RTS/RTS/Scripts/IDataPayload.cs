using System;
using System.IO;

namespace RTS
{
	public interface IDataPayload : IDisposable
	{
		long Length { get; }
		Stream OpenRead();
	}
}