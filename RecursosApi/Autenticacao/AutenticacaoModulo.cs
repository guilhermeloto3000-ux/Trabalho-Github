// Autenticacao/AutenticacaoModulo.cs — AULA FINAL — NOVO
// Registro do Identity, do JWT e da política de autorização (Web 9). Veio do Program.cs;
// o código é o mesmo, só trocamos builder.Configuration por config.
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public static class AutenticacaoModulo
{
    public static IServiceCollection AddAutenticacao(this IServiceCollection services, IConfiguration config)
    {
        // Identity — usuários, senhas (com hash) e roles, guardados no RecursosApiContext.
        services.AddIdentityCore<IdentityUser>(opcoes =>
            {
                opcoes.Password.RequiredLength = 8;                          // mínimo de 8 caracteres
                opcoes.Lockout.MaxFailedAccessAttempts = 5;                  // 5 senhas erradas...
                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);   // ...bloqueia por 5 min
            })
            .AddRoles<IdentityRole>()                                        // liga as roles
            .AddEntityFrameworkStores<RecursosApiContext>()                  // tabelas AspNet* no nosso banco
            .AddSignInManager();                                             // confere senha + bloqueio no login

        // Quem monta o token no login.
        services.AddScoped<TokenService>();

        // Autenticação — "quem é você?". Lê o JWT do cabeçalho Authorization.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.MapInboundClaims = false;     // mantém os nomes curtos das claims: "name", "role"
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config["Jwt:Emissor"],                 // confere quem emitiu
                    ValidAudience = config["Jwt:Publico"],               // confere para quem
                    IssuerSigningKey = TokenService.CriarChave(config),  // confere a assinatura
                    NameClaimType = "name",          // User.Identity.Name lê a claim "name"
                    RoleClaimType = "role"           // RequireRole lê as claims "role"
                };
            });

        // Autorização — "o que você pode fazer?". Uma política com nome.
        services.AddAuthorization(opcoes =>
            opcoes.AddPolicy("SomenteAdministrador", politica => politica.RequireRole("Administrador")));

        return services;
    }
}
