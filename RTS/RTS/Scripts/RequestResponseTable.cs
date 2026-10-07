using System.Collections.Generic;
using System.Threading.Tasks;

namespace RTS
{
	public class RequestResponseTable
	{
		private Dictionary<int, TaskCompletionSource<IMessage>> taskByRequestID = new();

		private int lastUsedID;
		
		public void RegisterRequest(out Task<IMessage> task, out int requestID)
		{
			TaskCompletionSource<IMessage> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			requestID = ++lastUsedID;
			taskByRequestID[requestID] = tcs;
			task = tcs.Task;
		}

		public bool TryFireResponse(IMessage message, int requestID)
		{
			if (taskByRequestID.TryGetValue(requestID, out TaskCompletionSource<IMessage> tcs))
			{
				tcs.TrySetResult(message);
				return true;
			}

			return false;
		}
	}
}