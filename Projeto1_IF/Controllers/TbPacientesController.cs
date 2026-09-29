using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Data;
using Projeto1_IF.Models;

namespace Projeto1_IF.Controllers;

[Authorize(Roles = "Medico,Nutricionista")]
public class TbPacientesController : Controller
{
    private readonly Projeto1_IFContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private const string CamposPaciente =
        "Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade," +
        "TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante";

    public TbPacientesController(Projeto1_IFContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<int?> ObterIdProfissionalAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _context.TbProfissionals
            .Where(p => p.IdUser == userId)
            .Select(p => (int?)p.IdProfissional)
            .FirstOrDefaultAsync();
    }

    private IQueryable<TbPaciente> PacientesDoProfissional(int idProfissional)
    {
        return _context.TbPacientes
            .Where(p => p.TbMedicoPacientes.Any(mp => mp.IdProfissional == idProfissional));
    }

    private async Task CarregarCidadesAsync(int? cidadeSelecionada = null)
    {
        var cidades = await _context.TbCidades.OrderBy(c => c.Nome).ToListAsync();
        ViewData["IdCidade"] = new SelectList(cidades, "IdCidade", "Nome", cidadeSelecionada);

        ViewData["Sexo"] = new SelectList(OpcoesPaciente.Sexos, "Key", "Value");
        ViewData["Etnia"] = new SelectList(OpcoesPaciente.Etnias, "Key", "Value");
    }

    // GET: TbPacientes
    public async Task<IActionResult> Index()
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null) return NotFound("Cadastro de profissional não encontrado.");

        var pacientes = await PacientesDoProfissional(idProf.Value)
            .Include(p => p.IdCidadeNavigation)
            .OrderBy(p => p.Nome)
            .ToListAsync();

        return View(pacientes);
    }

    // GET: TbPacientes/Details?idpaciente=5
    public async Task<IActionResult> Details(int? idpaciente)
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null || idpaciente == null) return NotFound();

        var paciente = await PacientesDoProfissional(idProf.Value)
            .Include(p => p.IdCidadeNavigation)
            .FirstOrDefaultAsync(p => p.IdPaciente == idpaciente);

        if (paciente == null) return NotFound();
        return View(paciente);
    }

    // GET: TbPacientes/Create
    public async Task<IActionResult> Create()
    {
        await CarregarCidadesAsync();
        return View();
    }

    // POST: TbPacientes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposPaciente)] TbPaciente paciente)
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null) return NotFound("Cadastro de profissional não encontrado.");

        if (ModelState.IsValid)
        {
            paciente.TbMedicoPacientes.Add(new TbMedicoPaciente { IdProfissional = idProf.Value });

            try
            {
                _context.Add(paciente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível cadastrar o paciente. Verifique os dados e tente novamente.");
            }
        }

        await CarregarCidadesAsync(paciente.IdCidade);
        return View(paciente);
    }

    // GET: TbPacientes/Edit?idpaciente=5
    public async Task<IActionResult> Edit(int? idpaciente)
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null || idpaciente == null) return NotFound();

        var paciente = await PacientesDoProfissional(idProf.Value)
            .FirstOrDefaultAsync(p => p.IdPaciente == idpaciente);

        if (paciente == null) return NotFound();

        await CarregarCidadesAsync(paciente.IdCidade);
        return View(paciente);
    }

    // POST: TbPacientes/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int idpaciente,
        [Bind("IdPaciente," + CamposPaciente)] TbPaciente formData)
    {
        if (idpaciente != formData.IdPaciente) return NotFound();

        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null) return NotFound();

        var pacienteNoBanco = await PacientesDoProfissional(idProf.Value)
            .FirstOrDefaultAsync(p => p.IdPaciente == idpaciente);

        if (pacienteNoBanco == null) return NotFound();

        if (ModelState.IsValid)
        {
            pacienteNoBanco.Nome = formData.Nome;
            pacienteNoBanco.Rg = formData.Rg;
            pacienteNoBanco.Cpf = formData.Cpf;
            pacienteNoBanco.DataNascimento = formData.DataNascimento;
            pacienteNoBanco.NomeResponsavel = formData.NomeResponsavel;
            pacienteNoBanco.Sexo = formData.Sexo;
            pacienteNoBanco.Etnia = formData.Etnia;
            pacienteNoBanco.Endereco = formData.Endereco;
            pacienteNoBanco.Bairro = formData.Bairro;
            pacienteNoBanco.IdCidade = formData.IdCidade;
            pacienteNoBanco.TelResidencial = formData.TelResidencial;
            pacienteNoBanco.TelComercial = formData.TelComercial;
            pacienteNoBanco.TelCelular = formData.TelCelular;
            pacienteNoBanco.Profissao = formData.Profissao;
            pacienteNoBanco.FlgAtleta = formData.FlgAtleta;
            pacienteNoBanco.FlgGestante = formData.FlgGestante;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { idpaciente });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TbPacientes.AnyAsync(p => p.IdPaciente == idpaciente))
                    return NotFound();
                ModelState.AddModelError(string.Empty, "O registro foi alterado por outra pessoa. Recarregue a página.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível salvar as alterações. Tente novamente.");
            }
        }

        await CarregarCidadesAsync(formData.IdCidade);
        return View(formData);
    }

    // GET: TbPacientes/Delete?idpaciente=5
    public async Task<IActionResult> Delete(int? idpaciente)
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null || idpaciente == null) return NotFound();

        var paciente = await PacientesDoProfissional(idProf.Value)
            .Include(p => p.IdCidadeNavigation)
            .FirstOrDefaultAsync(p => p.IdPaciente == idpaciente);

        if (paciente == null) return NotFound();
        return View(paciente);
    }

    // POST: TbPacientes/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int idpaciente)
    {
        var idProf = await ObterIdProfissionalAsync();
        if (idProf == null) return NotFound();

        var paciente = await PacientesDoProfissional(idProf.Value)
            .Include(p => p.IdCidadeNavigation)
            .FirstOrDefaultAsync(p => p.IdPaciente == idpaciente);

        if (paciente == null) return NotFound();

        try
        {
            var vinculos = await _context.TbMedicoPacientes
                .Where(mp => mp.IdPaciente == idpaciente)
                .ToListAsync();

            _context.TbMedicoPacientes.RemoveRange(vinculos.Where(mp => mp.IdProfissional == idProf.Value));

            if (vinculos.All(mp => mp.IdProfissional == idProf.Value))
            {
                _context.TbPacientes.Remove(paciente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty,
                "Não foi possível excluir: o paciente possui registros associados (exames, antropometria etc.).");
            return View(paciente);
        }
    }
}