using Microsoft.Extensions.Diagnostics.HealthChecks;
using payment_gateway_API.src.Data;

namespace payment_gateway_API.src.Infrastructure;

public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			return await dbContext.Database.CanConnectAsync(cancellationToken)
				? HealthCheckResult.Healthy("PostgreSQL disponível.")
				: HealthCheckResult.Unhealthy("PostgreSQL indisponível.");
		}
		catch (Exception exception)
		{
			return HealthCheckResult.Unhealthy("Não foi possível conectar ao PostgreSQL.", exception);
		}
	}
}