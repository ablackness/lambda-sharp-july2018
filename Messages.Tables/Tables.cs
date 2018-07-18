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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Messages.Tables {
    public static class Chunker {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunkSize) {
            if (chunkSize <= 0)  {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
 
            while (list.Any()) {
                yield return list.Take(chunkSize);
                list = list.Skip(chunkSize);
            }
        }
    }
    
    public class Message {

        //--- Properties ---
        public string MessageId { get; set; }
        public string Source { get; set; }
        public string Text { get; set; }
    }

    public class MessageTable {

        //--- Constants ---
        private const string MESSAGE_ID = "MessageId";
        private const string SOURCE = "Source";
        private const string TEXT = "Text";

        //--- Fields ---
        private readonly string _tableName;
        private readonly IAmazonDynamoDB _dynamoClient;

        //--- Constructors ---
        public MessageTable(string tableName, IAmazonDynamoDB dynamoClient = null) {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _dynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
        }

        //--- Methods ---
        public Task InsertMessageAsync(Message message) {
            var values = new Dictionary<string, AttributeValue> {
                [MESSAGE_ID] = new AttributeValue { S = Guid.NewGuid().ToString() },
                [SOURCE] = new AttributeValue { S = message.Source },
                [TEXT] = new AttributeValue { S = message.Text }
            };
            return _dynamoClient.PutItemAsync(_tableName, values);
        }

        public async Task<IEnumerable<Message>> ListMessagesAsync() {
            var response = await _dynamoClient.ScanAsync(new ScanRequest {
                TableName = _tableName,
            });
            return response.Items.Select(item => new Message {
                MessageId = item[MESSAGE_ID].S,
                Source = item[SOURCE].S,
                Text = item[TEXT].S
            }).ToArray();
        }

        public Task DeleteMessageAsync(string messgageId) {
            return _dynamoClient.DeleteItemAsync(_tableName, new Dictionary<string, AttributeValue> {
                [MESSAGE_ID] = new AttributeValue { S = messgageId }
            });
        }
         
        public async Task BatchDeleteMessagesAsync(IEnumerable<string> messageIds) {
            foreach (var chunk in messageIds.Chunk(10)) { 
                await BatchDeleteAsync(chunk);
            }
        }
        
        private Task BatchDeleteAsync(IEnumerable<string> messageIds) {
            var requests = messageIds.Select(x =>
                new WriteRequest(
                    new DeleteRequest(
                        new Dictionary<string, AttributeValue> {
                            { MESSAGE_ID, new AttributeValue(x) }
                        }
                    )
                )
            ).ToList();
            var request = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { _tableName, requests } });
            return _dynamoClient.BatchWriteItemAsync(request);
        }
        
        public async Task BatchInsertMessagesAsync(IEnumerable<Message> messages) {
            foreach (var chunk in messages.Chunk(25)) { 
                await BatchInsertAsync(chunk);
            }
        }
        
        private Task BatchInsertAsync(IEnumerable<Message> messages) {
            var requests = messages.Select(x =>
                new WriteRequest(
                    new PutRequest(
                        new Dictionary<string, AttributeValue> {
                            [MESSAGE_ID] = new AttributeValue { S = Guid.NewGuid().ToString() },
                            [SOURCE] = new AttributeValue { S = x.Source },
                            [TEXT] = new AttributeValue { S = x.Text }
                        }
                    )
                )
            ).ToList();
            var request = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { _tableName, requests } });
            return _dynamoClient.BatchWriteItemAsync(request);
        }
    }
}
