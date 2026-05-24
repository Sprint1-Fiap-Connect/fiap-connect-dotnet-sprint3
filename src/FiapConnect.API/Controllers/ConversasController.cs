using FiapConnect.API.Hateoas;
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
        // Created com Location explicito evita problema do CreatedAtAction precisar
        // ressolver a action por reflection quando o Id pode estar em qualquer formato
        return Created($"/api/conversas/{resposta.Id}", resposta);
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

        var resposta = new RespostaPaginadaDto<ConversaResponse>
        {
            Itens = paginada,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = todas.Count,
            TotalPaginas = (int)Math.Ceiling(todas.Count / (double)tamanhoPagina)
        };

        // Monta links HATEOAS de navegacao (self, previous, next)
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/conversas";
        resposta.Links.Add(new LinkDto
        {
            Href = $"{baseUrl}?rm={rm}&pagina={pagina}&tamanhoPagina={tamanhoPagina}",
            Rel = "self"
        });
        if (pagina > 1)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?rm={rm}&pagina={pagina - 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "previous"
            });
        }
        if (pagina < resposta.TotalPaginas)
        {
            resposta.Links.Add(new LinkDto
            {
                Href = $"{baseUrl}?rm={rm}&pagina={pagina + 1}&tamanhoPagina={tamanhoPagina}",
                Rel = "next"
            });
        }

        return Ok(resposta);
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