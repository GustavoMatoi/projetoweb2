using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projeto1_IF.Controllers
{
    [Authorize(Roles = "GerenteMedico,GerenteNutricionista,GerenteGeral")]
    public class GerenciaProfissionaisController : Controller
    {
        private readonly Projeto1_IFContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GerenciaProfissionaisController(Projeto1_IFContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        private async Task<IQueryable<TbProfissional>> ProfissionaisVisiveisAsync()
        {
            var roles = new List<string>();

            if (User.IsInRole("GerenteMedico"))
                roles.Add("Medico");
            else if (User.IsInRole("GerenteNutricionista"))
                roles.Add("Nutricionista");
            else if (User.IsInRole("GerenteGeral"))
            {
                roles.Add("Medico");
                roles.Add("Nutricionista");
            }

            var idsPermitidos = new List<string>();
            foreach (var role in roles)
            {
                var usuariosDaRole = await _userManager.GetUsersInRoleAsync(role);
                idsPermitidos.AddRange(usuariosDaRole.Select(u => u.Id));
            }

            return _context.TbProfissionals.Where(p => idsPermitidos.Contains(p.IdUser));
        }

  
        private async Task CarregarDropdownsAsync(int idCidade, int idContrato)
        {
            var cidades = await _context.TbCidades.OrderBy(c => c.Nome).ToListAsync();
            var contratos = (await _context.TbContratos.OrderBy(c => c.IdContrato).ToListAsync())
                .Select(c => new { c.IdContrato, Texto = "Contrato " + c.IdContrato })
                .ToList();

            ViewBag.IdCidade = new SelectList(cidades, "IdCidade", "Nome", idCidade);
            ViewBag.IdContrato = new SelectList(contratos, "IdContrato", "Texto", idContrato);
        }

        public async Task<IActionResult> Index()
        {
            var visiveis = await ProfissionaisVisiveisAsync();

            var profissionais = await visiveis
                .Include(p => p.IdCidadeNavigation)
                .ToListAsync();

            return View(profissionais);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var visiveis = await ProfissionaisVisiveisAsync();

            var profissional = await visiveis
                .Include(p => p.IdCidadeNavigation)
                .Include(p => p.IdContratoNavigation)
                .FirstOrDefaultAsync(p => p.IdProfissional == id);


            if (profissional == null)
                return NotFound();

            return View(profissional);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var visiveis = await ProfissionaisVisiveisAsync();

            var profissional = await visiveis
                .FirstOrDefaultAsync(p => p.IdProfissional == id);

            if (profissional == null)
                return NotFound();

            await CarregarDropdownsAsync(profissional.IdCidade, profissional.IdContrato);

            return View(profissional);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Nome,CrmCrn,Especialidade,Logradouro,Numero,Bairro,Cep,Cidade,Estado,Ddd1,Ddd2,Telefone1,Telefone2,Salario,IdCidade,IdContrato")] TbProfissional dadosForm)
        {
            var visiveis = await ProfissionaisVisiveisAsync();

            var profissional = await visiveis
                .FirstOrDefaultAsync(p => p.IdProfissional == id);

            if (profissional == null)
                return NotFound();

            ModelState.Remove(nameof(TbProfissional.Cpf));
            ModelState.Remove(nameof(TbProfissional.IdUser));
            ModelState.Remove(nameof(TbProfissional.Cidade));
            ModelState.Remove(nameof(TbProfissional.Estado));
            ModelState.Remove(nameof(TbProfissional.IdCidade));
            ModelState.Remove(nameof(TbProfissional.IdContrato));
            ModelState.Remove(nameof(TbProfissional.Salario));
            if (!ModelState.IsValid)
            {
                dadosForm.IdProfissional = id;
                await CarregarDropdownsAsync(dadosForm.IdCidade, dadosForm.IdContrato);
                return View(dadosForm);
            }

            profissional.Nome = dadosForm.Nome;
            profissional.CrmCrn = dadosForm.CrmCrn;
            profissional.Especialidade = dadosForm.Especialidade;
            profissional.Logradouro = dadosForm.Logradouro;
            profissional.Numero = dadosForm.Numero;
            profissional.Bairro = dadosForm.Bairro;
            profissional.Cep = dadosForm.Cep;
            profissional.Cidade = dadosForm.Cidade;
            profissional.Estado = dadosForm.Estado;
            profissional.Ddd1 = dadosForm.Ddd1;
            profissional.Ddd2 = dadosForm.Ddd2;
            profissional.Telefone1 = dadosForm.Telefone1;
            profissional.Telefone2 = dadosForm.Telefone2;
            profissional.Salario = dadosForm.Salario;
            profissional.IdCidade = dadosForm.IdCidade;
            profissional.IdContrato = dadosForm.IdContrato;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TbProfissionals.AnyAsync(e => e.IdProfissional == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var visiveis = await ProfissionaisVisiveisAsync();

            var profissional = await visiveis
                .Include(p => p.IdCidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProfissional == id);

            if (profissional == null)
                return NotFound();

            ViewBag.TemPacientesVinculados = await _context.TbMedicoPacientes.AnyAsync(mp => mp.IdProfissional == id);

            return View(profissional);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var visiveis = await ProfissionaisVisiveisAsync();

            var profissional = await visiveis
                .FirstOrDefaultAsync(p => p.IdProfissional == id);

            if (profissional == null)
                return NotFound();

            if (await _context.TbMedicoPacientes.AnyAsync(mp => mp.IdProfissional == id))
            {
                ModelState.AddModelError(string.Empty, "Não é possível excluir: este profissional possui pacientes vinculados.");
                ViewBag.TemPacientesVinculados = true;
                return View("Delete", profissional);
            }

            _context.TbProfissionals.Remove(profissional);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}