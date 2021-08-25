using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        private readonly IMapper _mapper;

        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDTO registerDto)
        {
            if(await UserExists(registerDto.Username)) return BadRequest("Username is taken."); //checking if Username is available 

            var user = _mapper.Map<AppUser>(registerDto);

            using var hmac = new HMACSHA512(); //declaration of encoding object to encode password

          
                user.Username = registerDto.Username.ToLower(); //all Username are in lowercase to prevent repeated Usernames
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)); //here is encoding magic - GETBYTES doesn't take nulls
                user.PasswordSalt = hmac.Key; //giving the password the KEY to later decrypt it during logging
       

            _context.Users.Add(user); //adding new user to DB Context

            await _context.SaveChangesAsync(); //THIS LINE ACTUALLY ADDS USER TO THE DATABASE!

            return new UserDto
            {
                Username = user.Username,
                Token = _tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users.
            Include(p => p.Photos).
            SingleOrDefaultAsync(x => x.Username == loginDTO.Username); //taking Username out of the DB

            if(user == null) return Unauthorized("Invalid Username"); //validation - if user is not in DB throw Unauthorized - "Invalid Username"

            using var hmac = new HMACSHA512(user.PasswordSalt); //Decrypting with a given key from REGISTER METHOD

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for (int i =0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password!");
            }

            return new UserDto
            {
                Username = user.Username,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        

        private async Task<bool> UserExists(string Username)
        {
            return await _context.Users.AnyAsync(x => x.Username == Username.ToLower());
        }
    }
}