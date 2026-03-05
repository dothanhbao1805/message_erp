using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using messenger.Repositories.Interfaces; // ← Thêm dòng này

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public TokenCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                await repo.CleanExpiredTokensAsync();
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}