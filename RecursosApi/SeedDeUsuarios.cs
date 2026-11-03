using Microsoft.AspNetCore.Identity;

// WEB 9 — NOVO: cria as roles e dois usuários de teste.
// As senhas vêm do User Secrets (fora do repositório). Sem senha configurada, o usuário não é criado.
public static class SeedDeUsuarios
{
    public static async Task CriarAsync(IServiceProvider servicos)
    {
        using var escopo = servicos.CreateScope();   // UserManager e RoleManager são Scoped
        var roles = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var config = escopo.ServiceProvider.GetRequiredService<IConfiguration>();
        var log = escopo.ServiceProvider.GetRequiredService<ILogger<Program>>();

        foreach (var nome in new[] { "Usuario", "Administrador" })
            if (!await roles.RoleExistsAsync(nome))
                await roles.CreateAsync(new IdentityRole(nome));

        await CriarUsuarioAsync(usuarios, log, "ana",
            config["SeedSenhas:Ana"], "Usuario");
        await CriarUsuarioAsync(usuarios, log, "admin",
            config["SeedSenhas:Admin"], "Administrador");
    }

    private static async Task CriarUsuarioAsync(UserManager<IdentityUser> usuarios, ILogger log,
        string nome, string? senha, string role)
    {
        if (await usuarios.FindByNameAsync(nome) is not null) return;   // já existe

        if (string.IsNullOrWhiteSpace(senha))
        {
            log.LogWarning("Usuário {Nome} não criado: configure a senha no User Secrets.",
                nome);
            return;
        }

        var usuario = new IdentityUser(nome);
        var resultado = await usuarios.CreateAsync(usuario, senha);   // grava o HASH da senha
        if (!resultado.Succeeded)
            throw new InvalidOperationException($"Não foi possível criar {nome}: " +
                string.Join(" ", resultado.Errors.Select(e => e.Description)));

        await usuarios.AddToRoleAsync(usuario, role);
    }
}
