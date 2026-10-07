using System.Collections.Generic;
using System.Threading.Tasks;

namespace RTS
{
	public class RequestResponseTable
	{
		private Dictionary<int, TaskCompletionSource<object>> taskByRequestID = new();

		private int lastUsedID;
		
		public void RegisterRequest(out Task<object> task, out int requestID)
		{
			TaskCompletionSource<object> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			requestID = ++lastUsedID;
			taskByRequestID[requestID] = tcs;
			task = tcs.Task;
		}

		public bool TryFireResponse(object message, int requestID)
		{
			if (taskByRequestID.TryGetValue(requestID, out TaskCompletionSource<object> tcs))
			{
				tcs.TrySetResult(message);
				return true;
			}

			return false;
		}
	}
}