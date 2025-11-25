namespace ODRESTServer.Dataclasses
{
    public class User
    {
        
        public required string Name { get; set; }


        public required byte[] PasswordHashWithSalt { get; set; }


        public required byte[] Salt { get; set; }


        public required string Email { get; set; }


        public required DateTime JoinTime { get; set; }

    }

    public class LoginDTO
    {


        public required string Email { get; set; }


        public required byte[] Password { get; set; }


        public required string EncryptReturnKey { get; set; }


    }


    public class CreateUserDTO
    {


        public required string Name { get; set; }


        public required string Email { get; set; }


        public required byte[] Password { get; set; }

    }


    public class UserReturnDTO
    {


        public required string Name { get; set; }


        public required string Email { get; set; }


        public required DateTime JoinTime { get; set; }

    }
}
