using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDTO registerDto)
        {
            if(await UserExists(registerDto.UserName)) return BadRequest("Username is taken."); //checking if username is available 

            using var hmac = new HMACSHA512(); //declaration of encoding object to encode password

            var user = new AppUser //declatration of a new user
            {
                UserName = registerDto.UserName.ToLower(), //all username are in lowercase to prevent repeated usernames
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)), //here is encoding magic - GETBYTES doesn't take nulls
                PasswordSalt = hmac.Key //giving the password the KEY to later decrypt it during logging
            };

            _context.Users.Add(user); //adding new user to DB Context

            await _context.SaveChangesAsync(); //THIS METHOD ADDS USER TO THE DATABASE!

            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users.
            SingleOrDefaultAsync(x => x.UserName == loginDTO.UserName); //taking username out of the DB

            if(user == null) return Unauthorized("Invalid username"); //validation - if user is not in DB throw Unauthorized - "Invalid username"

            using var hmac = new HMACSHA512(user.PasswordSalt); //Decrypting with a given key from REGISTER METHOD

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for (int i =0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password!");
            }

            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}