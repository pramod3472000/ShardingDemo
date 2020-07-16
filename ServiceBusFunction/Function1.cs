using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ServiceBusFunction
{
    public  class ServiceBusFunctionCall
    {
        IMyService MyClass;
        public ServiceBusFunctionCall(IMyService myClass)
        {
            MyClass = myClass;
        }
        [FunctionName("ServiceBusFunctionCall")]
        public async  void Run([ServiceBusTrigger("myqueue-items", Connection = "quequeconnection")]string myQueueItem, ILogger log)
        {

            var test = Environment.GetEnvironmentVariable("developer");

            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            var key = MyClass.GetKey();
            var apiKey = "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";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("azure_d4d00ef00d99e44d948d6345ab7ff209@azure.com", "Pramod");
            var subject = "Sending with Twilio SendGrid is Fun";
            var to = new EmailAddress("pramod3472000@gmail.com", "Example User");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response =await  client.SendEmailAsync(msg).ConfigureAwait(false);

        }
    }
}
