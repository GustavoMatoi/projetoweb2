// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Projeto1_IF.Data;
using Projeto1_IF.Models;

namespace Projeto1_IF.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly Projeto1_IFContext _context;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        Projeto1_IFContext context)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    // Listas para preencher os dropdowns na tela
    public List<SelectListItem> Cidades { get; set; } = new();
    public List<SelectListItem> Contratos { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        // ---- Campos do profissional (TbProfissional) ----

        [Required(ErrorMessage = "Escolha se é Médico ou Nutricionista")]
        [Display(Name = "Tipo de profissional")]
        public string TipoProfissional { get; set; } = default!; // "Medico" ou "Nutricionista"

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = default!;

        [Required]
        [StringLength(15)]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = default!;

        [StringLength(20)]
        [Display(Name = "CRM/CRN")]
        public string? CrmCrn { get; set; }

        [StringLength(100)]
        [Display(Name = "Especialidade")]
        public string? Especialidade { get; set; }

        [Required]
        [Display(Name = "Cidade")]
        public int IdCidade { get; set; }

        [Required]
        [Display(Name = "Contrato")]
        public int IdContrato { get; set; }

        [StringLength(100)]
        [Display(Name = "Logradouro")]
        public string? Logradouro { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Número")]
        public string Numero { get; set; } = default!;

        [Required]
        [StringLength(100)]
        [Display(Name = "Bairro")]
        public string Bairro { get; set; } = default!;

        [Required]
        [StringLength(10)]
        [Display(Name = "CEP")]
        public string Cep { get; set; } = default!;

        [StringLength(25)]
        [Display(Name = "Telefone 1")]
        public string? Telefone1 { get; set; }

        [StringLength(25)]
        [Display(Name = "Telefone 2")]
        public string? Telefone2 { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        await CarregarDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Associa a Role escolhida (Medico ou Nutricionista)
                await _userManager.AddToRoleAsync(user, Input.TipoProfissional);

                // Cria o registro correspondente em TbProfissional
                var userId = await _userManager.GetUserIdAsync(user);

                var novoProfissional = new TbProfissional
                {
                    IdUser = userId,
                    Nome = Input.Nome,
                    Cpf = Input.Cpf,
                    CrmCrn = Input.CrmCrn,
                    Especialidade = Input.Especialidade,
                    IdCidade = Input.IdCidade,
                    IdContrato = Input.IdContrato,
                    Logradouro = Input.Logradouro,
                    Numero = Input.Numero,
                    Bairro = Input.Bairro,
                    Cep = Input.Cep,
                    Telefone1 = Input.Telefone1,
                    Telefone2 = Input.Telefone2
                };

                try
                {
                    _context.TbProfissionals.Add(novoProfissional);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar TbProfissional durante o registro.");
                    ModelState.AddModelError(string.Empty, "Não foi possível concluir seu cadastro de profissional. Tente novamente.");
                    await CarregarDropdownsAsync();
                    return Page();
                }

                var codeToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                codeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codeToken));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = codeToken, returnUrl = returnUrl },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // Se algo falhou, recarrega os dropdowns antes de mostrar o formulário de novo
        await CarregarDropdownsAsync();
        return Page();
    }

    private async Task CarregarDropdownsAsync()
    {
        Cidades = await _context.TbCidades
            .OrderBy(c => c.Nome)
            .Select(c => new SelectListItem
            {
                Value = c.IdCidade.ToString(),
                Text = c.Nome
            })
            .ToListAsync();

        var contratos = await _context.TbContratos
            .Include(c => c.IdPlanoNavigation)
            .ToListAsync();

        Contratos = contratos
            .Select(c => new SelectListItem
            {
                Value = c.IdContrato.ToString(),
                Text = $"{c.IdPlanoNavigation.Nome} " +
                       $"({(c.DataInicio.HasValue ? c.DataInicio.Value.ToString("dd/MM/yyyy") : "?")} " +
                       $"a {(c.DataFim.HasValue ? c.DataFim.Value.ToString("dd/MM/yyyy") : "sem fim")})"
            })
            .ToList();
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<ApplicationUser>)_userStore;
    }
}