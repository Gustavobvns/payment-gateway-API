using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using payment_gateway_API.src.Auth.Configuration;
using payment_gateway_API.src.Auth.CurrentUser;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Auth.Login;
using payment_gateway_API.src.Auth.Register;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Auth.Users;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Infrastructure;
using payment_gateway_API.src.Features.Ledger;
using payment_gateway_API.src.Features.Payments;
using payment_gateway_API.src.Features.Transfers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenGenerator>();
builder.Services.AddScoped<AuthResponseFactory>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IChangePasswordService, ChangePasswordService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// O binding tipado impede que emissão e validação usem configurações diferentes.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options => options.IsValid(), "A configuração Jwt é inválida.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("A configuração Jwt não foi encontrada.");
jwtOptions.EnsureValid();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(settings => settings.SwaggerEndpoint("/openapi/v1.json", "Payment Gateway API v1"));
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Cada slice registra suas próprias rotas; Program.cs fica apenas como composição da aplicação.
app.MapRegisterEndpoints();
app.MapLoginEndpoints();
app.MapUserEndpoints();
app.MapTransferEndpoints();
app.MapPaymentEndpoints();
app.MapLedgerEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
