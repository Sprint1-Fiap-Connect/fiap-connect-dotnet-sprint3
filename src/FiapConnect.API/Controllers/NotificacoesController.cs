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

        return Ok(new
        {
            itens = paginada,
            pagina,
            tamanhoPagina,
            total = todas.Count
        });
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