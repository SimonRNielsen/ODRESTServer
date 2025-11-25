using Microsoft.AspNetCore.Mvc;
using ODRESTServer.Dataclasses;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ODRESTServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserListing : ControllerBase
    {

        private readonly ILogger<UserListing> _logger;
        private readonly string userFile = "app_data/users.json";
        private static readonly object fileLock = new object();
        private string privateKey;
        private string publicKey; 
        private static readonly Dictionary<UserResults, string> userResults = new Dictionary<UserResults, string>
        {

            { UserResults.Created, "User created" },
            { UserResults.FailedCreation, "Couldn't create user" },
            { UserResults.FailedLogin, "Login attempt failed" },
            { UserResults.InvalidPassOrMail, "Invalid password or email" }

        };


        public UserListing(ILogger<UserListing> logger)
        {

            _logger = logger;

            using (RSA rsa = RSA.Create())
            {

                privateKey = rsa.ToXmlString(true);
                publicKey = rsa.ToXmlString(false);

            }

        }

        /// <summary>
        /// Endpoint to "log in"
        /// </summary>
        /// <param name="loginAttempt">Data needed to verify user</param>
        /// <returns>Response</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO loginAttempt)
        {

            if (loginAttempt == null)
                return Conflict(userResults[UserResults.FailedLogin]);

            UserReturnDTO result;

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(userFile);
                List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

                if (users.Count == 0)
                    return Conflict(userResults[UserResults.FailedLogin]);

                var user = users.FirstOrDefault(x => x.Email == loginAttempt.Email);

                if (user == null)
                    return Unauthorized(userResults[UserResults.InvalidPassOrMail]);

                byte[] inputPlusSalt = DecryptedData(Convert.FromBase64String(loginAttempt.Password)).Concat(user.Salt).ToArray();
                using SHA256 mySHA256 = SHA256.Create();
                byte[] passPlusSaltHash = mySHA256.ComputeHash(inputPlusSalt);

                if (!passPlusSaltHash.SequenceEqual(user.PasswordHashWithSalt))
                    return Unauthorized(userResults[UserResults.InvalidPassOrMail]);

                byte[] userName;
                byte[] email;

                using (RSA rsa = RSA.Create())
                {

                    rsa.FromXmlString(loginAttempt.EncryptReturnKey);
                    userName = rsa.Encrypt(Encoding.UTF8.GetBytes(user.Name), RSAEncryptionPadding.Pkcs1);
                    email = rsa.Encrypt(Encoding.UTF8.GetBytes(user.Email), RSAEncryptionPadding.Pkcs1);

                }

                result = new UserReturnDTO
                {

                    Name = Convert.ToBase64String(userName),
                    Email = Convert.ToBase64String(email),
                    JoinTime = user.JoinTime

                };

            }

            return Ok(result);

        }

        /// <summary>
        /// Endpoint for adding/creating a new user and adding it to "users.json"
        /// </summary>
        /// <param name="newUser">User to add, contains encrypted data</param>
        /// <returns>Response</returns>
        [HttpPost("add")]
        public IActionResult CreateUser([FromBody] CreateUserDTO newUser)
        {

            if (newUser == null)
                return Conflict(userResults[UserResults.FailedCreation]);

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(userFile);
                List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

                if (users.Any(x => x.Email == newUser.Email))
                    return Conflict(userResults[UserResults.FailedCreation]);

                byte[] salt = new byte[16];
                RandomNumberGenerator.Fill(salt);

                byte[] passPlusSalt = DecryptedData(Convert.FromBase64String(newUser.Password)).Concat(salt).ToArray();
                using SHA256 mySHA256 = SHA256.Create();
                byte[] hashedPassWithSalt = mySHA256.ComputeHash(passPlusSalt);

                User createdUser = new User
                {

                    Name = newUser.Name,
                    Email = newUser.Email,
                    JoinTime = DateTime.UtcNow,
                    Salt = salt,
                    PasswordHashWithSalt = hashedPassWithSalt

                };

                users.Add(createdUser);

                var updatedUsers = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(userFile, updatedUsers);

            }

            return Ok(userResults[UserResults.Created]);

        }

        /// <summary>
        /// Method for decrypting recieved data
        /// </summary>
        /// <param name="data">Data to decrypt</param>
        /// <returns>Decrypted data</returns>
        private byte[] DecryptedData(byte[] data)
        {

            using (RSA rsa = RSA.Create())
            {

                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);

            }

        }

        /// <summary>
        /// Endpoint to get public key
        /// </summary>
        /// <returns>Public RSA-encryption key</returns>
        [HttpGet("publickey")]
        public string GetPublicKey() => publicKey;

        /// <summary>
        /// Testing tool to see contents of the users.json file
        /// </summary>
        /// <returns>All "users"</returns>
        [HttpGet("testreader")]
        public IEnumerable<User> Get()
        {

            string json = System.IO.File.ReadAllText(userFile);
            List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            return users;

        }

        /// <summary>
        /// Deletes the contents of "users.json"
        /// </summary>
        /// <returns>Response</returns>
        [HttpDelete("clear")]
        public IActionResult DeleteUsers()
        {

            lock (fileLock)
                System.IO.File.WriteAllText(userFile, "[]");

            return NoContent();

        }

    }

    public enum UserResults
    {
        
        Created,
        FailedCreation,
        FailedLogin,
        InvalidPassOrMail

    }

}
