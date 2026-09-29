using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;

namespace Projeto1_IF.Controllers;

[Authorize(Roles = "Medico,Nutricionista")]
public class TbProfissionaisController : Controller
{
    private readonly Projeto1_IFContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TbProfissionaisController(Projeto1_IFContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var meuCadastro = await _context.TbProfissionals
            .FirstOrDefaultAsync(p => p.IdUser == userId);

        if (meuCadastro == null)
        {
            return NotFound("Cadastro de profissional não encontrado para este usuário.");
        }

        return RedirectToAction(nameof(Details), new { idprofissional = meuCadastro.IdProfissional });
    }

    public async Task<IActionResult> Details(int? idprofissional)
    {
        var userId = _userManager.GetUserId(User);
        var meuCadastro = await _context.TbProfissionals
            .FirstOrDefaultAsync(p => p.IdUser == userId);

        var tbprofissional = await _context.TbProfissionals
            .Include(p => p.IdCidadeNavigation)
            .Include(p => p.IdContratoNavigation)
            .FirstOrDefaultAsync(m => m.IdProfissional == idprofissional && m.IdUser == userId);
        if (tbprofissional == null)
        {
            return NotFound();
        }

        return View(tbprofissional);
    }

    public async Task<IActionResult> Edit(int? idprofissional)
    {
        var userId = _userManager.GetUserId(User);

        var tbprofissional = await _context.TbProfissionals
            .FirstOrDefaultAsync(p => p.IdProfissional == idprofissional && p.IdUser == userId);

        if (tbprofissional == null)
        {
            return NotFound();
        }

        return View(tbprofissional);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int idprofissional)
    {
        var userId = _userManager.GetUserId(User);

        var profissionalNoBanco = await _context.TbProfissionals
            .FirstOrDefaultAsync(p => p.IdProfissional == idprofissional && p.IdUser == userId);

        if (profissionalNoBanco == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(profissionalNoBanco, "",
            p => p.Nome, p => p.CrmCrn, p => p.Especialidade,
            p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
            p => p.Ddd1, p => p.Ddd2, p => p.Telefone1, p => p.Telefone2))
        {
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { idprofissional });
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível salvar: " + ex.InnerException?.Message);
            }
        }

        return View(profissionalNoBanco);
    }

    private bool TbProfissionalExists(int idprofissional)
    {
        return _context.TbProfissionals.Any(e => e.IdProfissional == idprofissional);
    }
}