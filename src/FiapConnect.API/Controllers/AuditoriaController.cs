using FiapConnect.API.Hateoas;
using FiapConnect.Application.DTOs.Auditoria;
using FiapConnect.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

// Apenas leitura. Gravacao eh interna via IAuditoriaService.RegistrarInternoAsync
[ApiController]
[Authorize]
[Route("api/auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(string id)
    {
        var resposta = await _auditoriaService.ObterPorIdAsync(id);
        return Ok(resposta);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? tabelaAfetada,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10)
    {
        // Se tabelaAfetada vier preenchida, filtra. Caso contrario, lista tudo
        var todas = string.IsNullOrWhiteSpace(tabelaAfetada)
            ? (await _auditoriaService.ListarAsync()).ToList()
            : (await _auditoriaService.ListarPorTabelaAsync(tabelaAfetada)).ToList();

        var paginada = todas.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina);

        var resposta = new RespostaPaginadaDto<AuditoriaResponse>
        {
            Itens = paginada,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = todas.Count,
            TotalPaginas = (int)Math.Ceiling(todas.Count / (double)tamanhoPagina)
        };

        // Inclui o filtro tabelaAfetada na querystring apenas se preenchido
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/auditoria";
        var filtroTabela = string.IsNullOrWhiteSpace(tabelaAfetada)
            ? string.Empty
            : $"tabelaAfetada={tabelaAfetada}&";

        resposta.Links.Add(new LinkDto
        {
            Href = $"{baseUrl}?{filtroTabela}pagina={pagina}&tamanhoPagina={tamanhoPagina}",
            Rel = "self"
        });
        if (pagina > 1)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?{filtroTabela}pagina={pagina - 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "previous"
            });
        }
        if (pagina < resposta.TotalPaginas)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?{filtroTabela}pagina={pagina + 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "next"
            });
        }

        return Ok(resposta);
    }
}