using FiapConnect.Domain.Exceptions;

namespace FiapConnect.API.Middlewares;

// Middleware unico que captura excecoes de qualquer ponto do pipeline
// e converte para resposta HTTP padronizada
public class ExcecaoGlobalMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExcecaoGlobalMiddleware> _logger;

	public ExcecaoGlobalMiddleware(
		RequestDelegate next,
		ILogger<ExcecaoGlobalMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (RegraDeNegocioException ex)
		{
			_logger.LogWarning(ex, "Regra de negocio violada");
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
			await context.Response.WriteAsJsonAsync(new
			{
				erro = "RegraDeNegocio",
				mensagem = ex.Message
			});
		}
		catch (RecursoNaoEncontradoException ex)
		{
			_logger.LogInformation(ex, "Recurso nao encontrado");
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			await context.Response.WriteAsJsonAsync(new
			{
				erro = "RecursoNaoEncontrado",
				mensagem = ex.Message
			});
		}
		catch (UnauthorizedAccessException ex)
		{
			// Defesa em profundidade: caso algum service lance explicitamente
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await context.Response.WriteAsJsonAsync(new
			{
				erro = "NaoAutorizado",
				mensagem = ex.Message
			});
		}
		catch (Exception ex)
		{
			// Fallback: erros nao previstos nao expoem detalhes ao cliente
			_logger.LogError(ex, "Erro nao tratado");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsJsonAsync(new
			{
				erro = "ErroInterno",
				mensagem = "Ocorreu um erro inesperado"
			});
		}
	}
}