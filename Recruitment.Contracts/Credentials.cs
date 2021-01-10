using System.ComponentModel.DataAnnotations;

namespace Recruitment.Contracts
{
    /// <summary>
    /// Recruitment test request model
    /// </summary>
    public class Credentials
    {
        [Required]
        public string login { get; set; }

        [Required]
        public string password { get; set; }
    }

    /// <summary>
    /// Recruitment test response model
    /// </summary>
    public class HashValue
    {
        public string hash_value { get; set; }
    }

}
