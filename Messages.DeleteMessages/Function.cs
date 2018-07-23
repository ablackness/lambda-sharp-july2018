using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Messages.Tables;
using MindTouch.LambdaSharp;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Messages.DeleteMessages {

    public class Function : ALambdaApiGatewayFunction {

        private MessageTable _table;
        //private IAmazonS3 _s3Client;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            var tableName = config.ReadText("MessageTable");
            _table = new MessageTable(tableName);
            //_s3Client = new AmazonS3Client();
            return Task.CompletedTask;
        }
            
            
        public override async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var messages = await _table.ListMessagesAsync();
            List<string> ids = new List<string>();
            foreach (var msg in messages)
            {
                LogInfo(msg.MessageId);
                ids.Add(msg.MessageId);
            }
                
            await _table.BatchDeleteMessagesAsync(ids);
            
            return new APIGatewayProxyResponse {
                StatusCode = 200
            };
        }
    }
}