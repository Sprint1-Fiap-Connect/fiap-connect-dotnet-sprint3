using FiapConnect.Application.DTOs.Conversa;
using FiapConnect.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/conversas")]
public class ConversasController : ControllerBase
{
    private readonly IConversaService _conversaService;

    public ConversasController(IConversaService conversaService)
    {
        _conversaService = conversaService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarConversaRequest request)
    {
        var resposta = await _conversaService.CriarAsync(request);
        // Location header aponta pro GET /{id} usando o ObjectId Mongo
        return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(string id)
    {
        var resposta = await _conversaService.ObterPorIdAsync(id);
        return Ok(resposta);
    }

    [HttpGet]
    public async Task<IActionResult> ListarPorParticipante(
        [FromQuery] string rm,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10)
    {
        var todas = (await _conversaService.ListarPorParticipanteAsync(rm)).ToList();
        var paginada = todas.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina);

        return Ok(new
        {
            itens = paginada,
            pagina,
            tamanhoPagina,
            total = todas.Count
        });
    }

    [HttpPost("{id}/mensagens")]
    public async Task<IActionResult> EnviarMensagem(
        string id,
        [FromBody] EnviarMensagemRequest request)
    {
        var resposta = await _conversaService.EnviarMensagemAsync(id, request);
        return Ok(resposta);
    }

    [HttpPatch("{id}/mensagens/lidas")]
    public async Task<IActionResult> MarcarComoLidas(
        string id,
        [FromQuery] string rmLeitor)
    {
        await _conversaService.MarcarMensagensComoLidasAsync(id, rmLeitor);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(string id)
    {
        await _conversaService.RemoverAsync(id);
        return NoContent();
    }
}