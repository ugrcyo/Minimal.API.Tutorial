using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minimal.API.Tutorial.Context;
using Minimal.API.Tutorial.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MinimalDb>(options => options.UseInMemoryDatabase("items"));

#region JWT
var securityScheme = new OpenApiSecurityScheme()
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};
var securityReq = new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
};

var contact = new OpenApiContact()
{
    Name = "UGRCYO",
    Email = "hello@ugrcyo.com",
    Url = new Uri("http://www.ugrcyo.com")
};

var license = new OpenApiLicense()
{
    Name = "Free License",
    Url = new Uri("http://www.ugrcyo.com")
};

var info = new OpenApiInfo()
{
    Version = "v1",
    Title = "Minimal API - JWT Authentication with Swagger demo",
    Description = "Implementing JWT Authentication in Minimal API",
    TermsOfService = new Uri("http://www.example.com"),
    Contact = contact,
    License = license
};
#endregion

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1",info);
    x.AddSecurityDefinition("Bearer", securityScheme);
    x.AddSecurityRequirement(securityReq);
});

builder.Services.AddAuthentication(x=> 
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwtConfig =>
    {
        jwtConfig.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle




var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/todo", () => new { Item = "Minimal API", Complete = "true" });

app.MapGet("/getItems", async (MinimalDb db) => await db.Items.ToListAsync());

app.MapPost("/addItem", async (MinimalDb db, MinimalItem item) =>
{
    await db.Items.AddAsync(item);
    await db.SaveChangesAsync();
    return Results.Created($"/item/{item.Id}", item);
});

app.MapGet("/getItem/{id}", [Authorize] async (MinimalDb db, int id) => await db.Items.FindAsync(id));

app.MapPut("/updateItem/{id}", async (MinimalDb db, MinimalItem updateItem, int id) =>
{
    var item = await db.Items.FindAsync(id);

    if (item is null) return Results.NotFound();

    db.Attach(item);
    item.Item = updateItem.Item ?? item.Item;
    item.IsComplete = updateItem.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();

});

app.MapDelete("/deleteItem/{id}", async (MinimalDb db, int id)
    =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.Items.Remove(item);
    await db.SaveChangesAsync();

    return Results.Ok();

});

app.MapPost("/security/getToken", [AllowAnonymous] (User user) =>
{

    if (user.UserName == "admin@ugrcyo.com" && user.Password == "ugrcyo")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", "1"),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(6),
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);

        var jwtToken = jwtTokenHandler.WriteToken(token);

        return Results.Ok(jwtToken);
    }
    else
    {
        return Results.Unauthorized();
    }
});

app.Run();

record User(string UserName, string Password);
