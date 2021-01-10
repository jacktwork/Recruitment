using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Recruitment.Contracts;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Recruitment.Functions
{
    public static class CalcHash
    {
        [FunctionName("CalcHash")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("recruitment test credentials process request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Credentials cred = JsonConvert.DeserializeObject<Credentials>(requestBody);

            if (cred == null || cred.password == null || cred.password.Length == 0)
            {
                return new BadRequestResult();
            }

            try
            {
                HashValue hashValue = new HashValue();
                hashValue.hash_value = CreateMD5Hash(cred.password);
                return new OkObjectResult(hashValue);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return new BadRequestResult();
            }
        }

        public static string CreateMD5Hash(string input)
        {
            // Step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
