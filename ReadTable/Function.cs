using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ReadTable {

    public class Function : ALambdaFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {
            using(var reader = new StreamReader(stream)) {
                LogInfo(await reader.ReadToEndAsync());
            }
            var tableName = config.ReadText("MessageTable");
            LogInfo(tableName);
            return "Ok";
        }
    }
}