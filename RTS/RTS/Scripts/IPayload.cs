using System;
using System.IO;

namespace RTS
{
	public interface IPayload : IDisposable
	{
		long Length { get; }
		Stream OpenRead();
	}
	
	// Для коротких данных в памяти
	public class MemoryPayload : IPayload
	{
		private byte[] data;
		public long Length => data.Length;

		public MemoryPayload(byte[] data)
		{
			this.data = data;
		}

		public Stream OpenRead() => new MemoryStream(data);
		public void Dispose() { }
	}

	// Для отправки файлов прямо с диска (без загрузки в RAM)
	public class FilePayload : IPayload
	{
		private string path;
		public long Length { get; }

		public FilePayload(string path)
		{
			this.path = path;
			Length = new FileInfo(path).Length;
		}

		public Stream OpenRead() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		public void Dispose() { }
	}

	// Для приема больших файлов (автоматически создает временный файл)
	public class TempFilePayload : IPayload
	{
		public string Path { get; }
		public long Length => new FileInfo(Path).Length;

		public TempFilePayload()
		{
			Path = System.IO.Path.GetTempFileName();
		}

		public Stream OpenRead() => new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		public Stream OpenWrite() => new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        
		public void Dispose()
		{
			if (File.Exists(Path)) File.Delete(Path);
		}
	}
}