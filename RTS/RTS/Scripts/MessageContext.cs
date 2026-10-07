namespace RTS
{
	public class MessageContext
	{
		public readonly ClientSession session;
		public readonly object message;
		public readonly int requestID;

		public MessageContext(ClientSession session, object message, int requestId)
		{
			this.session = session;
			this.message = message;
			requestID = requestId;
		}

		public void Send(object m)
		{
			session.Send(m, requestID);
		}
	}
}