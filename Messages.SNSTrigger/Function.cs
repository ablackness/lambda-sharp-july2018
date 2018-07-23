using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda;
using Amazon.Lambda.SNSEvents;
using MindTouch.LambdaSharp;
using Newtonsoft.Json;
using Messages.Tables;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Messages.SNSTrigger {

    public class MyMessage {

        //--- Properties ---
        public string Text { get; set; }
    }

    public class Function : ALambdaFunction<SNSEvent> {

        //--- Fields ---
        private MessageTable _table;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            var tableName = config.ReadText("MessageTable");
            _table = new MessageTable(tableName);
            return Task.CompletedTask;
        }

        public override async Task<object> ProcessMessageAsync(SNSEvent message, ILambdaContext context) {
            LogInfo("Invoked!");
            LogInfo(JsonConvert.SerializeObject(message));
            LogInfo(message.Records[0].Sns.Message);

            var msg = new Message();
            msg.Source = "SNS";
            msg.Text = message.Records[0].Sns.Message;

            await _table.InsertMessageAsync(msg);

            return Task.CompletedTask;
        }
    }
}