/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Messages.Tables;
using MindTouch.LambdaSharp;
using Amazon.S3.Util;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Messages.LoadMessages {

    public class Function : ALambdaFunction<S3EventNotification> {

        //--- Fields ---
        private MessageTable _table;
        private IAmazonS3 _s3Client;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            var tableName = config.ReadText("MessageTable");
            _table = new MessageTable(tableName);
            _s3Client = new AmazonS3Client();
            return Task.CompletedTask;
        }

        public override async Task<object> ProcessMessageAsync(S3EventNotification message, ILambdaContext context) {
            LogInfo(JsonConvert.SerializeObject(message));
            
            // Use S3EventNotification to get location of the file which was uploaded
            var bucket = message.Records[0].S3.Bucket.Name;
            var key = message.Records[0].S3.Object.Key;
            // Read S3 object contents
            var request = new GetObjectRequest{
                BucketName = bucket,
                Key = key
            };
            var text = "";
            using(GetObjectResponse response = await _s3Client.GetObjectAsync(request))
            using(Stream responseStream = response.ResponseStream)
            using(StreamReader reader = new StreamReader(responseStream)){
                text = await reader.ReadToEndAsync();
            }

            // Separate messages by line ending
              var splitText = text.Split('\n');

            // Use BatchInsertMessagesAsync from the Messages.Tables library to write messages to DynamoDB
            var messages = new List<Message>();
            for (int i = 0;i < splitText.Length; i++) {
                LogInfo(splitText[i]);
                var msg = new Message();
                msg.Source = "S3";
                msg.Text = splitText[i];
                messages.Add(msg);
            }
            await _table.BatchInsertMessagesAsync(messages);
            return null;
        }
    }
}
