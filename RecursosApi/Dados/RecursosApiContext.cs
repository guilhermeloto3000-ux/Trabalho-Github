using Microsoft.AspNetCore.Identity;                          // WEB 9 — NOVO: IdentityUser
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;      // WEB 9 — NOVO: IdentityDbContext
using Microsoft.EntityFrameworkCore;

// WEB 9 — MUDOU: herda IdentityDbContext<IdentityUser> em vez de DbContext.
// IdentityDbContext JÁ É um DbContext, com as tabelas do Identity prontas:
// AspNetUsers (usuários), AspNetRoles (roles) e AspNetUserRoles (quem tem qual role), entre outras.
public class RecursosApiContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Recurso> Recursos => Set<Recurso>();

    public RecursosApiContext(DbContextOptions<RecursosApiContext> options)
        : base(options) { }
}
