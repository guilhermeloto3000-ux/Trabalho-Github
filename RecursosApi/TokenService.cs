using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// WEB 9 — NOVO: monta e assina o JWT que o login devolve.
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GerarToken(IdentityUser usuario, IList<string> roles)
    {
        // Claims = as informações que vão DENTRO do token (qualquer um consegue ler).
        var claims = new List<Claim>
        {
            new("sub", usuario.Id),              // id do usuário
            new("name", usuario.UserName!),      // nome de login
        };
        foreach (var role in roles)
            claims.Add(new("role", role));       // uma claim "role" para cada role

        var descricao = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Emissor"],            // claim iss: quem emitiu
            Audience = _config["Jwt:Publico"],          // claim aud: para quem
            Subject = new ClaimsIdentity(claims),       // as claims acima
            Expires = DateTime.UtcNow.AddMinutes(
                _config.GetValue("Jwt:MinutosDeValidade", 30)),          // claim exp
            SigningCredentials = new SigningCredentials(
                CriarChave(_config), SecurityAlgorithms.HmacSha256)      // assinatura
        };

        return new JsonWebTokenHandler().CreateToken(descricao);   // "xxxxx.yyyyy.zzzzz"
    }

    // A MESMA chave assina (aqui) e confere (no AddJwtBearer do Program.cs).
    public static SymmetricSecurityKey CriarChave(IConfiguration config)
    {
        var chave = config["Jwt:Chave"];
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new InvalidOperationException(
                "Configure Jwt:Chave (pelo menos 32 caracteres) com: " +
                "dotnet user-secrets set \"Jwt:Chave\" \"...\"");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
    }
}
