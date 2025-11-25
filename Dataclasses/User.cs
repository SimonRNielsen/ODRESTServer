namespace ODRESTServer.Dataclasses
{

    /// <summary>
    /// Data storage class for saving relevant info pertinent for a specific user
    /// </summary>
    public class User
    {
        
        public required string Name { get; set; }


        public required byte[] PasswordHashWithSalt { get; set; }


        public required byte[] Salt { get; set; }


        public required string Email { get; set; }


        public required DateTime JoinTime { get; set; }

    }

    /// <summary>
    /// Data transfer object with data needed for logging in
    /// </summary>
    public class LoginDTO
    {


        public required string Email { get; set; }


        public required string Password { get; set; }


        public required string EncryptReturnKey { get; set; }


    }

    /// <summary>
    /// Data transfer object with data pertinent for creating a new user
    /// </summary>
    public class CreateUserDTO
    {


        public required string Name { get; set; }


        public required string Email { get; set; }


        public required string Password { get; set; }

    }

    /// <summary>
    /// Data transfer object that's used for sending data to requestee
    /// </summary>
    public class UserReturnDTO
    {


        public required string Name { get; set; }


        public required string Email { get; set; }

    }
}
