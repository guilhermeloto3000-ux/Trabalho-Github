using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

// WEB 9 — NOVO: o que o cliente envia no login e o que recebe de volta.
public record LoginDto(string Usuario, string Senha);
public record LoginRespostaDto(string Token, string Nome, IList<string> Roles);
public record CadastroDto(string Usuario, string Senha);                   // WEB 9 — NOVO: cadastro

// WEB 9 — NOVO: endpoints de autenticação.
public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        // POST /auth/login — confere usuário e senha; se estiverem certos, devolve um JWT.
        app.MapPost("/auth/login", async (
            LoginDto dto,
            UserManager<IdentityUser> usuarios,
            SignInManager<IdentityUser> login,
            TokenService tokens) =>
        {
            var usuario = await usuarios.FindByNameAsync(dto.Usuario);
            if (usuario is null)
                return LoginInvalido();      // mesma resposta de senha errada

            // lockoutOnFailure: true -> conta as tentativas erradas e bloqueia depois do limite
            var resultado = await login.CheckPasswordSignInAsync(
                usuario, dto.Senha, lockoutOnFailure: true);

            if (resultado.IsLockedOut)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Usuário bloqueado",
                    detail: "Muitas tentativas erradas. Tente de novo em alguns minutos.");

            if (!resultado.Succeeded)
                return LoginInvalido();

            var roles = await usuarios.GetRolesAsync(usuario);
            var token = tokens.GerarToken(usuario, roles);
            return Results.Ok(new LoginRespostaDto(token, usuario.UserName!, roles));
        })
        .WithSummary("Entra com usuário e senha e recebe um JWT")
        .Produces<LoginRespostaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // WEB 9 — NOVO: POST /auth/cadastro — cria um usuário novo, SEMPRE com a role "Usuario".
        app.MapPost("/auth/cadastro", async (CadastroDto dto, UserManager<IdentityUser> usuarios) =>
        {
            if (await usuarios.FindByNameAsync(dto.Usuario) is not null)
                return Results.Problem(statusCode: StatusCodes.Status409Conflict,
                    title: "Usuário já existe");

            var usuario = new IdentityUser(dto.Usuario);
            var resultado = await usuarios.CreateAsync(usuario, dto.Senha);   // confere as regras e grava o hash

            if (!resultado.Succeeded)                                         // senha fraca, nome inválido...
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["cadastro"] = resultado.Errors.Select(e => e.Description).ToArray()
                    },
                    title: "Não foi possível cadastrar");

            await usuarios.AddToRoleAsync(usuario, "Usuario");   // a role NÃO vem do cliente

            // 201 Created. O primeiro argumento seria o endereço do recurso criado; aqui não há um GET para ele.
            return Results.Created((string?)null, new { nome = usuario.UserName, roles = new[] { "Usuario" } });
        })
        .WithSummary("Cadastra um usuário novo (role Usuario)");

        // GET /auth/eu — mostra o que a API leu do token (só para quem está logado).
        app.MapGet("/auth/eu", (ClaimsPrincipal usuario) => Results.Ok(new
        {
            nome = usuario.Identity!.Name,                                 // claim "name"
            roles = usuario.FindAll("role").Select(c => c.Value)           // claims "role"
        }))
        .RequireAuthorization()
        .WithSummary("Mostra o usuário do token enviado");
    }

    private static IResult LoginInvalido() =>
        Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
            title: "Usuário ou senha inválidos");
}
