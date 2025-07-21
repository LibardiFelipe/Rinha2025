using Dapper;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Repositories;
using Npgsql;

namespace MinimalArchitecture.Template.Infrastructure.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly string _connString;

        public PaymentRepository(string connString)
        {
            _connString = connString;
        }

        public async Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(
            IEnumerable<PaymentReceivedEvent> events)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            const string sql = @"
                COPY payments (
                    correlation_id,
                    amount,
                    processed_by,
                    requested_at_utc)
                FROM STDIN (FORMAT BINARY)";

            await using var writer = await conn.BeginBinaryImportAsync(sql);
            foreach (var payment in events)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(payment.CorrelationId);
                await writer.WriteAsync(payment.Amount);
                await writer.WriteAsync(payment.ProcessedBy);
                await writer.WriteAsync(payment.RequestedAt);
            }

            await writer.CompleteAsync();
            return events;
        }

        public async ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT processed_by AS ProcessedBy, COUNT(*) AS TotalRequests, SUM(amount) AS TotalAmount
                FROM payments
                WHERE (@from IS NULL OR requested_at_utc >= @from)
                  AND (@to IS NULL OR requested_at_utc <= @to)
                GROUP BY processed_by;
            ";

            return await conn.QueryAsync<SummaryRowReadModel>(sql, new { from, to });
        }

        public async ValueTask PurgeAsync()
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            const string sql = "TRUNCATE TABLE payments;";
            await conn.ExecuteAsync(sql);
        }
    }
}
