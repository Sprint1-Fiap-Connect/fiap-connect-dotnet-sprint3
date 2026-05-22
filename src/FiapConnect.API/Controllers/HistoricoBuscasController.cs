using FiapConnect.API.Hateoas;
using FiapConnect.Application.DTOs.HistoricoBusca;
using FiapConnect.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/historico-buscas")]
public class HistoricoBuscasController : ControllerBase
{
	private readonly IHistoricoBuscaService _historicoBuscaService;

	public HistoricoBuscasController(IHistoricoBuscaService historicoBuscaService)
	{
		_historicoBuscaService = historicoBuscaService;
	}

	[HttpPost]
	public async Task<IActionResult> Registrar([FromBody] RegistrarBuscaRequest request)
	{
		var resposta = await _historicoBuscaService.RegistrarAsync(request);
		return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> ObterPorId(string id)
	{
		var resposta = await _historicoBuscaService.ObterPorIdAsync(id);
		return Ok(resposta);
	}

	[HttpGet]
	public async Task<IActionResult> ListarPorAluno(
		[FromQuery] string rmAluno,
		[FromQuery] int pagina = 1,
		[FromQuery] int tamanhoPagina = 10)
	{
		// Service ja retorna ordenado por timestamp desc (definido no repository)
		var todas = (await _historicoBuscaService.ListarPorAlunoAsync(rmAluno)).ToList();
		var paginada = todas.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina);

		var resposta = new RespostaPaginadaDto<HistoricoBuscaResponse>
		{
			Itens = paginada,
			Pagina = pagina,
			TamanhoPagina = tamanhoPagina,
			TotalItens = todas.Count,
			TotalPaginas = (int)Math.Ceiling(todas.Count / (double)tamanhoPagina)
		};

		var baseUrl = $"{Request.Scheme}://{Request.Host}/api/historico-buscas";
		resposta.Links.Add(new LinkDto
		{
			Href = $"{baseUrl}?rmAluno={rmAluno}&pagina={pagina}&tamanhoPagina={tamanhoPagina}",
			Rel = "self"
		});
		if (pagina > 1)
		{
			resposta.Links.Add(new LinkDto
			{
				Href = $"{baseUrl}?rmAluno={rmAluno}&pagina={pagina - 1}&tamanhoPagina={tamanhoPagina}",
				Rel = "previous"
			});
		}
		if (pagina < resposta.TotalPaginas)
		{
			resposta.Links.Add(new LinkDto
			{
				Href = $"{baseUrl}?rmAluno={rmAluno}&pagina={pagina + 1}&tamanhoPagina={tamanhoPagina}",
				Rel = "next"
			});
		}

		return Ok(resposta);
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Remover(string id)
	{
		await _historicoBuscaService.RemoverAsync(id);
		return NoContent();
	}
}