using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RailwayReservationSystem.Data;
using RailwayReservationSystem.Models.Entities;
using RailwayReservationSystem.Services;
using RailwayReservationSystem.Middleware;
using Microsoft.EntityFrameworkCore;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models;
using RailwayReservationSystem.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Register Repositories and Services (3-tier)
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ITrainRepository, TrainRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. JWT Authentication Configuration
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // This handles the custom 401 response
    options.Events = new JwtBearerEvents {
        OnChallenge = context => {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new {
                message = "Authentication failed. Please ensure you use 'Bearer [token]'.",
                statusCode = 401,
                timestamp = DateTime.UtcNow
            });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context => {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new {
                message = "Access forbidden. This endpoint requires Passenger role. Please login with a Passenger account.",
                statusCode = 403,
                timestamp = DateTime.UtcNow
            });
            return context.Response.WriteAsync(result);
        }
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Railway Reservation API", 
        Version = "v1",
        Description = "API for Indian Railway network including Booking, Cancellation, and Admin operations." 
    });

    // 1. Define the Security Scheme for JWT
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
    });

    // 2. Make the Lock Icon apply to all endpoints
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Global Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandler>();

// Seed Roles and Admin User on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Ensure database is created
    await context.Database.EnsureCreatedAsync();

    // Ensure Admin and Passenger roles exist
    string[] roles = { "Admin", "Passenger" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Super Admin
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        var admin = new ApplicationUser 
        { 
            UserName = "admin", 
            Email = "admin@railway.com", 
            FullName = "System Administrator" 
        };
        // Admins log in with hardcoded credentials initially
        await userManager.CreateAsync(admin, "Admin@123"); 
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Upsert default trains so details stay current even on existing databases.
    var defaultTrains = new List<Train>
    {
        new Train
        {
            Id = "02603",
            TrainNumber = "02603",
            TrainName = "City Connect Express",
            Source = "Bengaluru City",
            Destination = "Mysuru",
            TotalSeats = 200,
            AvailableSeats = 200,
            Fare = 180.00m,
            DepartureTime = DateTime.Now.AddDays(1).Date.AddHours(8)
        },
        new Train
        {
            Id = "12321",
            TrainNumber = "12321",
            TrainName = "YNK Express",
            Source = "Yelahanka",
            Destination = "Rajankunte",
            TotalSeats = 100,
            AvailableSeats = 100,
            Fare = 30.00m,
            DepartureTime = DateTime.Now.AddDays(1).Date.AddHours(11)
        },
        new Train
        {
            Id = "12345",
            TrainNumber = "12345",
            TrainName = "Airport Link",
            Source = "Yelahanka",
            Destination = "Devanahalli",
            TotalSeats = 150,
            AvailableSeats = 150,
            Fare = 60.00m,
            DepartureTime = DateTime.Now.AddDays(1).Date.AddHours(14)
        },
        new Train
        {
            Id = "54321",
            TrainNumber = "54321",
            TrainName = "North Corridor Express",
            Source = "Yeshwantpur",
            Destination = "Chikkaballapur",
            TotalSeats = 300,
            AvailableSeats = 300,
            Fare = 140.00m,
            DepartureTime = DateTime.Now.AddDays(2).Date.AddHours(6)
        },
        new Train
        {
            Id = "98765",
            TrainNumber = "98765",
            TrainName = "Silk City Express",
            Source = "Bengaluru City",
            Destination = "Tumakuru",
            TotalSeats = 100,
            AvailableSeats = 100,
            Fare = 90.00m,
            DepartureTime = DateTime.Now.AddDays(1).Date.AddHours(16)
        }
    };

    foreach (var seedTrain in defaultTrains)
    {
        var existing = await context.Trains.FirstOrDefaultAsync(t => t.Id == seedTrain.Id);
        if (existing == null)
        {
            context.Trains.Add(seedTrain);
            continue;
        }

        existing.TrainNumber = seedTrain.TrainNumber;
        existing.TrainName = seedTrain.TrainName;
        existing.Source = seedTrain.Source;
        existing.Destination = seedTrain.Destination;
        existing.TotalSeats = seedTrain.TotalSeats;
        existing.Fare = seedTrain.Fare;
        existing.DepartureTime = seedTrain.DepartureTime;
        if (existing.AvailableSeats > existing.TotalSeats)
        {
            existing.AvailableSeats = existing.TotalSeats;
        }
    }
    await context.SaveChangesAsync();

    // Upsert default train routes with station codes
    var seedTrainIds = defaultTrains.Select(t => t.Id).ToHashSet();
    var existingSeedStations = context.TrainStations.Where(s => seedTrainIds.Contains(s.TrainId));
    context.TrainStations.RemoveRange(existingSeedStations);

    var routeStations = new List<TrainStation>
    {
        // City Connect Express (02603): Bengaluru City -> Kengeri -> Ramanagara -> Mysuru
        new TrainStation { TrainId = "02603", StationCode = "SBC", StationName = "Bengaluru City", StopOrder = 1, FareFromStart = 0m },
        new TrainStation { TrainId = "02603", StationCode = "KGI", StationName = "Kengeri", StopOrder = 2, FareFromStart = 40m },
        new TrainStation { TrainId = "02603", StationCode = "RMG", StationName = "Ramanagara", StopOrder = 3, FareFromStart = 95m },
        new TrainStation { TrainId = "02603", StationCode = "MYS", StationName = "Mysuru", StopOrder = 4, FareFromStart = 180m },

        // YNK Express (12321): Yelahanka -> Rajankunte
        new TrainStation { TrainId = "12321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 10m },
        new TrainStation { TrainId = "12321", StationCode = "RNK", StationName = "Rajankunte", StopOrder = 2, FareFromStart = 30m },

        // Airport Link (12345): Yelahanka -> Bettahalsoor -> Devanahalli
        new TrainStation { TrainId = "12345", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 0m },
        new TrainStation { TrainId = "12345", StationCode = "BTH", StationName = "Bettahalsoor", StopOrder = 2, FareFromStart = 30m },
        new TrainStation { TrainId = "12345", StationCode = "DHL", StationName = "Devanahalli", StopOrder = 3, FareFromStart = 60m },

        // North Corridor Express (54321): Yeshwantpur -> Yelahanka -> Doddaballapur -> Chikkaballapur
        new TrainStation { TrainId = "54321", StationCode = "YPR", StationName = "Yeshwantpur", StopOrder = 1, FareFromStart = 0m },
        new TrainStation { TrainId = "54321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 2, FareFromStart = 35m },
        new TrainStation { TrainId = "54321", StationCode = "DBU", StationName = "Doddaballapur", StopOrder = 3, FareFromStart = 80m },
        new TrainStation { TrainId = "54321", StationCode = "CBP", StationName = "Chikkaballapur", StopOrder = 4, FareFromStart = 140m },

        // Silk City Express (98765): Bengaluru City -> Yeshwantpur -> Nelamangala -> Tumakuru
        new TrainStation { TrainId = "98765", StationCode = "SBC", StationName = "Bengaluru City", StopOrder = 1, FareFromStart = 0m },
        new TrainStation { TrainId = "98765", StationCode = "YPR", StationName = "Yeshwantpur", StopOrder = 2, FareFromStart = 20m },
        new TrainStation { TrainId = "98765", StationCode = "NLM", StationName = "Nelamangala", StopOrder = 3, FareFromStart = 55m },
        new TrainStation { TrainId = "98765", StationCode = "TK", StationName = "Tumakuru", StopOrder = 4, FareFromStart = 90m }
    };

    context.TrainStations.AddRange(routeStations);
    await context.SaveChangesAsync();
    Console.WriteLine("✅ Default train details and route stations updated");

    // Seed Sample Bookings for Testing
    if (!context.Bookings.Any())
    {
        var bookings = new List<Booking>
        {
            new Booking
            {
                PNR = "PNRTEST001",
                TrainId = "02603",
                PassengerName = "Ahmed Khan",
                PassengerAge = 28,
                Sex = "Male",
                Address = "123 Main Street, Lahore",
                BankName = "HBL Bank",
                Class = "Business",
                SeatNumber = "B-15",
                Status = "Confirmed",
                IsCheckedIn = false,
                BookingDate = DateTime.Now
            },
            new Booking
            {
                PNR = "PNRTEST002",
                TrainId = "12345",
                PassengerName = "Fatima Ali",
                PassengerAge = 35,
                Sex = "Female",
                Address = "456 Park Avenue, Islamabad",
                BankName = "UBL Bank",
                Class = "Economy",
                SeatNumber = "E-42",
                Status = "Confirmed",
                IsCheckedIn = true,
                BookingDate = DateTime.Now.AddHours(-2)
            },
            new Booking
            {
                PNR = "PNRTEST003",
                TrainId = "54321",
                PassengerName = "Hassan Raza",
                PassengerAge = 42,
                Sex = "Male",
                Address = "789 Garden Road, Rawalpindi",
                BankName = "MCB Bank",
                Class = "Economy",
                SeatNumber = "E-88",
                Status = "Cancelled",
                IsCheckedIn = false,
                BookingDate = DateTime.Now.AddDays(-1)
            }
        };

        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync();
        
        // Add check-in record for the checked-in booking
        var checkedInBooking = context.Bookings.FirstOrDefault(b => b.PNR == "PNRTEST002" && b.IsCheckedIn);
        if (checkedInBooking != null)
        {
            var checkIn = new CheckIn
            {
                BookingId = checkedInBooking.Id,
                SeatNumber = "E-42",
                CheckInReference = "CHK" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
            };
            context.CheckIns.Add(checkIn);
        }
        
        await context.SaveChangesAsync();
        Console.WriteLine("✅ 3 sample bookings added to database for testing");
    }
}

// Enable Swagger and Auth
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication(); // Must be before Authorization
app.UseAuthorization();
app.MapControllers();
app.Run();