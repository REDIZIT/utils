namespace RTS
{
	public enum MessageType : byte
	{
		Invalid,
		BucketCreate,
		BucketPart,
		BucketReceived,
		BucketDrop
	}
}