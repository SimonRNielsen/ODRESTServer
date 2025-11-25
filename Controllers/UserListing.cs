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
        private readonly string userFile = "app_data/users.json", invalidPassOrMail = "Invalid password or email";
        private static readonly object fileLock = new object();
        private string privateKey;
        private string publicKey;


        public UserListing(ILogger<UserListing> logger)
        {

            _logger = logger;

            using (RSA rsa = RSA.Create())
            {

                privateKey = rsa.ToXmlString(true);
                publicKey = rsa.ToXmlString(false);

            }

        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO loginAttempt)
        {

            if (loginAttempt == null)
                return BadRequest("Login attempt was null");

            UserReturnDTO result;

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(userFile);
                List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

                if (users.Count == 0)
                    return Conflict("No users found");

                var user = users.FirstOrDefault(x => x.Email == loginAttempt.Email);

                if (user == null)
                    return Unauthorized(invalidPassOrMail);

                byte[] inputPlusSalt = DecryptedData(Convert.FromBase64String(loginAttempt.Password)).Concat(user.Salt).ToArray();
                using SHA256 mySHA256 = SHA256.Create();
                byte[] passPlusSaltHash = mySHA256.ComputeHash(inputPlusSalt);

                if (!passPlusSaltHash.SequenceEqual(user.PasswordHashWithSalt))
                    return Unauthorized(invalidPassOrMail);

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


        [HttpPost("add")]
        public IActionResult CreateUser([FromBody] CreateUserDTO newUser)
        {

            if (newUser == null)
                return BadRequest("User was null");

            lock (fileLock)
            {

                string json = System.IO.File.ReadAllText(userFile);
                List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

                if (users.Exists(x => x.Email == newUser.Email))
                    return Conflict("User already exists");

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

            return Ok("User created");

        }


        private byte[] DecryptedData(byte[] data)
        {

            using (RSA rsa = RSA.Create())
            {

                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);

            }

        }


        [HttpGet("publickey")]
        public string GetPublicKey() => publicKey;


        [HttpGet("testreader")]
        public IEnumerable<User> Get()
        {

            string json = System.IO.File.ReadAllText(userFile);
            List<User> users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            return users.ToArray();

        }

    }
}
