using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Recruitment.Contracts;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HashController : ControllerBase
    {
        private readonly ILogger<HashController> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public HashController(ILogger<HashController> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            // lien - we could get fancy with this and use an ioc container
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello World";
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Credentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                HashValue hashValue = await CalcHash(credentials);
                return Ok(hashValue);
            } 
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task<HashValue> CalcHash(Credentials credentials)
        {
            // note: this belongs in a business logic library or service library
            string functionUrl = _config["functionUrl"];
            string functionCode = _config["functionCode"];
            string url = $"{functionUrl}?code={functionCode}";
            HashValue hashValue = null;
            using (HttpClient httpClient = _httpClientFactory.CreateClient("recruitmentClient"))
            {
                var json = JsonConvert.SerializeObject(credentials);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, data);
                string responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("post failed login: " + credentials.login);
                }
                hashValue = JsonConvert.DeserializeObject<HashValue>(responseString);
            }
            return hashValue;
        }
    }
}

