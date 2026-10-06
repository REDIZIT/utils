using System.Collections.Generic;
using System.Threading.Tasks;

namespace RTS
{
	public class RequestResponseTable
	{
		private Dictionary<int, TaskCompletionSource<ITrackableMessage>> taskByRequestID = new();

		public Task<ITrackableMessage> RegisterRequest(ITrackableMessage message)
		{
			TaskCompletionSource<ITrackableMessage> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			taskByRequestID[message.RequestID] = tcs;
			return tcs.Task;
		}

		public bool TryFireResponse(ITrackableMessage message)
		{
			if (taskByRequestID.TryGetValue(message.RequestID, out TaskCompletionSource<ITrackableMessage> tcs))
			{
				tcs.TrySetResult(message);
				return true;
			}

			return false;
		}
	}
}