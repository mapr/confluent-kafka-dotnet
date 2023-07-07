// Copyright 2023 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

#pragma warning disable xUnit1026

using System;
using System.Collections.Generic;
using Confluent.Kafka.Admin;
using Xunit;


namespace Confluent.Kafka.IntegrationTests
{
    public partial class Tests
    {
        /// <summary>
        ///     Test functionality of AdminClient.DescribeUserScramCredentials and AdminClient.AlterUserScramCredentials.
        /// </summary>
        [Theory, MemberData(nameof(KafkaParameters))]
        public async void AdminClient_UserScram(string bootstrapServers)
        {
            LogToFile("start AdminClient_UserScram");
            var timeout = TimeSpan.FromSeconds(30);
            using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build())
            {
                var users = new List<string>();
                users.Add("non-existing-user");
                var descResult = await adminClient.DescribeUserScramCredentialsAsync(users, new DescribeUserScramCredentialsOptions() { RequestTimeout = timeout });
                foreach (var description in descResult.UserScramCredentialsDescriptions)
                {
                    Assert.Equal(ErrorCode.ResourceNotFound,description.Error.Code);
                }

                var upsertions = new List<UserScramCredentialAlteration>();
                var upsertion = new UserScramCredentialUpsertion(){User = "non-existing-user", Salt = "Salt", Password = "Password", Mechanism = ScramMechanism.Scram_SHA_256, Iterations = 15000 };
                upsertions.Add(upsertion);
                var alterResult = await adminClient.AlterUserScramCredentialsAsync(upsertions, new AlterUserScramCredentialsOptions() { RequestTimeout = timeout });
                foreach (var alterReport in alterResult.AlterUserScramCredentialsReports)
                {
                    Assert.Equal(ErrorCode.NoError,alterReport.Error.Code);
                }

                descResult = await adminClient.DescribeUserScramCredentialsAsync(users, new DescribeUserScramCredentialsOptions() { RequestTimeout = timeout });
                foreach (var description in descResult.UserScramCredentialsDescriptions)
                {
                    Assert.Equal(ErrorCode.NoError,description.Error.Code);
                    foreach(var credentialinfo in description.ScramCredentialInfos){
                        Assert.Equal(15000,credentialinfo.Iterations);
                        Assert.Equal(ScramMechanism.Scram_SHA_256,credentialinfo.Mechanism);
                    }
                }

                var deletions = new List<UserScramCredentialAlteration>();
                var deletion = new UserScramCredentialDeletion(){User = "non-existing-user", Mechanism = ScramMechanism.Scram_SHA_256};
                deletions.Add(deletion);
                alterResult = await adminClient.AlterUserScramCredentialsAsync(deletions,new AlterUserScramCredentialsOptions() { RequestTimeout = timeout });
                foreach (var alterReport in alterResult.AlterUserScramCredentialsReports)
                {
                    Assert.Equal(ErrorCode.NoError,alterReport.Error.Code);
                }

                descResult = await adminClient.DescribeUserScramCredentialsAsync(users, new DescribeUserScramCredentialsOptions() { RequestTimeout = timeout });
                foreach (var description in descResult.UserScramCredentialsDescriptions)
                {
                    Assert.Equal(ErrorCode.ResourceNotFound,description.Error.Code);
                }


                Assert.Equal(0, Library.HandleCount);
                LogToFile("end   AdminClient_UserScram");
            }
        }
    }
}
