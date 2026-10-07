namespace RTS
{
	public class TestMessage
	{
		public string message;
	}

	public class TestResponse
	{
		public string response;
	}

	public class TestHeavyRequest
	{
		public string shortMessage;
		public IPayload Archive;
	}

	public class TestHeavyResponse
	{
		public string shortResponse;
	}
}