using Microsoft.IdentityModel.Tokens;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RailwayReservationSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAccountRepository accountRepository,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _accountRepository = accountRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> LoginAsync(LoginDto model)
        {
            var user = await _accountRepository.FindByUsernameAsync(model.Username);
            if (user == null)
            {
                return null;
            }

            var passwordOk = await _accountRepository.CheckPasswordAsync(user, model.Password);
            if (!passwordOk)
            {
                return null;
            }

            var userRoles = await _accountRepository.GetRolesAsync(user);
            var expiryHours = userRoles.Contains("Admin") ? 2 : 3;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtSecret = _configuration["JWT:Secret"]
                ?? throw new InvalidOperationException("JWT:Secret is missing from configuration.");
            var jwtIssuer = _configuration["JWT:ValidIssuer"]
                ?? throw new InvalidOperationException("JWT:ValidIssuer is missing from configuration.");
            var jwtAudience = _configuration["JWT:ValidAudience"]
                ?? throw new InvalidOperationException("JWT:ValidAudience is missing from configuration.");

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                expires: DateTime.Now.AddHours(expiryHours),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return $"Bearer {new JwtSecurityTokenHandler().WriteToken(token)}";
        }

        public async Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model)
        {
            if (await _accountRepository.FindByUsernameAsync(model.Username) != null)
            {
                return (false, "Username is already in use.");
            }

            if (await _accountRepository.FindByEmailAsync(model.Email) != null)
            {
                return (false, "Email is already registered.");
            }

            var user = new ApplicationUser
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username,
                FullName = model.FullName
            };

            var createResult = await _accountRepository.CreateUserAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                return (false, createResult.Error ?? "User creation failed.");
            }

            await _accountRepository.EnsureRoleExistsAsync("Passenger");
            await _accountRepository.AddUserToRoleAsync(user, "Passenger");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var subject = "Welcome to Railway Reservation System";
                var body = $@"Hello {user.FullName},

Your account has been created successfully.

Username: {user.UserName}
Role: Passenger

You can now login and book tickets.

Thank you for registering with Railway Reservation System.";

                try
                {
                    await _emailService.SendAsync(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "User registered but welcome email failed for {Email}.", user.Email);
                }
            }

            return (true, "User registered successfully as Passenger.");
        }
    }
}
