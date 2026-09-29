using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Projeto1_IFContext") ?? throw new InvalidOperationException("Connection string 'Projeto1_IFContext' not found.");

builder.Services.AddDbContext<Projeto1_IFContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<Projeto1_IFContext>();

builder.Services.AddDbContext<Projeto1_IFContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Medico", "Nutricionista", "GerenteMedico", "GerenteNutricionista", "GerenteGeral" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var gerentes = new[]
    {
        new { Email = "gerentemedico@sistema.com", Role = "GerenteMedico" },
        new { Email = "gerentenutricionista@sistema.com", Role = "GerenteNutricionista" },
        new { Email = "gerentegeral@sistema.com", Role = "GerenteGeral" }
    };
    foreach (var g in gerentes)
    {
        var existente = await userManager.FindByEmailAsync(g.Email);
        if (existente == null)
        {
            var novoUsuario = new ApplicationUser
            {
                UserName = g.Email,
                Email = g.Email,
                EmailConfirmed = true
            };
            var resultado = await userManager.CreateAsync(novoUsuario, "SenhaForte123!");

            if (resultado.Succeeded)
            {
                await userManager.AddToRoleAsync(novoUsuario, g.Role);
            }
            else
            {
                foreach (var erro in resultado.Errors)
                    Console.WriteLine($"Erro ao criar {g.Email}: {erro.Description}");
            }
        }
    }
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
