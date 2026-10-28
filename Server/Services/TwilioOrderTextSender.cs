using System;
using System.Threading.Tasks;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GIBS.Module.FBClients.Services
{
    public interface IFBClientsOrderTextSender
    {
        Task<string> SendAsync(string accountSid, string authToken, string from, string to, string body);
    }

    public class TwilioOrderTextSender : IFBClientsOrderTextSender
    {
        public async Task<string> SendAsync(string accountSid, string authToken, string from, string to, string body)
        {
            // A per-request client prevents credentials leaking between module instances.
            var client = new TwilioRestClient(accountSid, authToken);
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(to),
                from: new PhoneNumber(from),
                body: body,
                client: client);

            if (string.IsNullOrEmpty(message.Sid) || message.ErrorCode.HasValue
                || message.Status == MessageResource.StatusEnum.Failed
                || message.Status == MessageResource.StatusEnum.Undelivered
                || message.Status == MessageResource.StatusEnum.Canceled)
            {
                throw new InvalidOperationException("Twilio did not accept the order text. Check the Twilio console before retrying.");
            }
            return message.Sid;
        }
    }
}
