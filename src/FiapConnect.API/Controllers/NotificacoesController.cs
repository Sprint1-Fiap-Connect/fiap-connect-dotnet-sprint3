using FiapConnect.API.Hateoas;
using FiapConnect.Application.DTOs.Notificacao;
using FiapConnect.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public class NotificacoesController : ControllerBase
{
    private readonly INotificacaoService _notificacaoService;

    public NotificacoesController(INotificacaoService notificacaoService)
    {
        _notificacaoService = notificacaoService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarNotificacaoRequest request)
    {
        var resposta = await _notificacaoService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(string id)
    {
        var resposta = await _notificacaoService.ObterPorIdAsync(id);
        return Ok(resposta);
    }

    [HttpGet]
    public async Task<IActionResult> ListarPorDestinatario(
        [FromQuery] string rmDestinatario,
        [FromQuery] bool apenasNaoLidas = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10)
    {
        var todas = (await _notificacaoService
            .ListarPorDestinatarioAsync(rmDestinatario, apenasNaoLidas)).ToList();
        var paginada = todas.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina);

        var resposta = new RespostaPaginadaDto<NotificacaoResponse>
        {
            Itens = paginada,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = todas.Count,
            TotalPaginas = (int)Math.Ceiling(todas.Count / (double)tamanhoPagina)
        };

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/notificacoes";
        var filtros = $"rmDestinatario={rmDestinatario}&apenasNaoLidas={apenasNaoLidas}";

        resposta.Links.Add(new LinkDto
        {
            Href = $"{baseUrl}?{filtros}&pagina={pagina}&tamanhoPagina={tamanhoPagina}",
            Rel = "self"
        });
        if (pagina > 1)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?{filtros}&pagina={pagina - 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "previous"
            });
        }
        if (pagina < resposta.TotalPaginas)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?{filtros}&pagina={pagina + 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "next"
            });
        }

        return Ok(resposta);
    }

    [HttpPatch("{id}/lida")]
    public async Task<IActionResult> MarcarComoLida(string id)
    {
        await _notificacaoService.MarcarComoLidaAsync(id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(string id)
    {
        await _notificacaoService.RemoverAsync(id);
        return NoContent();
    }
}