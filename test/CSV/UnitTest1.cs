using Xunit;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using SimpleDB;
using System.Text;

    

namespace UnitTests
{
    public class UnitTest1
    {
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5175/")
        };

        [Fact]
        private static async Task StoreAndRead()
        {

            const string testFile = "test.csv";
            File.WriteAllText(testFile, "Author,Message,Timestamp\n");


            var response = await client.GetAsync("cheeps");
            response.EnsureSuccessStatusCode();
            var cheeps = await response.Content.ReadFromJsonAsync<List<Cheep>>();

            Console.WriteLine(response);
            Assert.Equal(response.StatusCode, HttpStatusCode.OK);
            Assert.True(cheeps.Count > 0);
            Assert.IsType<List<Cheep>>(cheeps);
            var json = "{\"Author\":\"ropf\",\"Message\":\"Hello, World!\",\"Timestamp\":1684229348}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response2 = await client.PostAsync("cheep", content);
            response2.EnsureSuccessStatusCode();
            Assert.Equal(response2.StatusCode, HttpStatusCode.Created);
        }
    }
}