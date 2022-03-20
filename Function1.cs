using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WordTest.Input_Model;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Amazon.Util.Internal.PlatformServices;
using Azure.Storage.Blobs;

namespace WordTest
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            StringBuilder output = new StringBuilder();

            Rootobject input = JsonConvert.DeserializeObject<Rootobject>(requestBody);  // parsing json data

            if (input.Words == null || input.Words.Length == 0)  // checking for empty List of data
            {
                return new OkObjectResult("No data found");
            }

            var result = CountOccurancesIndexWise(input.Words.ToList());  // function passing list of words and getting result


            // Generating Header for CSV 
            output.AppendLine($"letter,{string.Join(",", Enumerable.Range(1, input.Words[0].Length))}");
           
            //Generating Body for CSV 
            foreach (var item in result)
            {
                output.AppendLine($"{item.Key},{string.Join(',', item.Value)}");
            }

            System.IO.File.WriteAllText("c:\\Temp\\OutputExample.csv", output.ToString());

            //await CreateBlob("BlobContainerName", "BlobPath",  "FileName" + ".csv", output.ToString(), "connectionString");

            string responseMessage = "Successfull";
            return new OkObjectResult(responseMessage);
        }


        public async static Task CreateBlob(string blobContanierName, string blobPath, string blobName, string data, string storageAccountConnectionString)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
                BlobContainerClient blobcontainer = blobServiceClient.GetBlobContainerClient(blobContanierName);
                BlobClient blobClient = blobcontainer.GetBlobClient(blobPath + "/" + blobName);
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

                memoryStream.Position = 0;
                await blobClient.UploadAsync(memoryStream, true);
                // return s1;
            }
            catch (Exception ex)
            {
                throw new Exception("There is an error uploading  blob to  Blob storage.", ex.InnerException);
            }
        }


        public static Dictionary<char, List<int>> CountOccurancesIndexWise(List<string> Words)
        {
            var dictionary = new Dictionary<char, List<int>>();

            var wordLength = Words[0].Length;

            string distinctLetters = "";

            foreach (var word in Words)
            {
                var distinctCharactersOfWord = word.Distinct();
                foreach (var character in distinctCharactersOfWord)
                {
                    if (!distinctLetters.ToUpperInvariant().Contains(char.ToUpperInvariant(character)))
                        distinctLetters += character;
                }
            }

            foreach (var letter in distinctLetters)
            {
                var characterOccurances = new List<int>();

                int counter = 0;

                for (int index = 0; index < wordLength; index++)
                {
                    foreach (var word in Words)
                    {
                        if (char.ToUpperInvariant(word.ElementAt(index)) == char.ToUpperInvariant(letter))
                            counter++;
                    }

                    characterOccurances.Add(counter);
                    counter = 0;

                }

                dictionary.Add(letter, characterOccurances);
            }

            return dictionary;
        }
    }
}
