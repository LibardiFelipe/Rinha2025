using Rinha2025.Domain.Events;
using Rinha2025.Domain.Repositories;
using System.Collections.Concurrent;

namespace Rinha2025.Infrastructure.Repositories
{
    public sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly ConcurrentDictionary<Guid, PaymentReceivedEvent> _payments = new();
        private readonly SortedList<DateTimeOffset, List<Guid>> _dateIndex = [];
        private readonly ReaderWriterLockSlim _indexLock = new(LockRecursionPolicy.NoRecursion);

        public Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(IEnumerable<PaymentReceivedEvent> events)
        {
            try
            {
                _indexLock.EnterWriteLock();
                foreach (var payment in events)
                {
                    if (_payments.TryAdd(payment.CorrelationId, payment))
                    {
                        if (!_dateIndex.TryGetValue(payment.RequestedAt, out var idList))
                        {
                            idList = [];
                            _dateIndex.Add(payment.RequestedAt, idList);
                        }
                        idList.Add(payment.CorrelationId);
                    }
                }
            }
            finally
            {
                _indexLock.ExitWriteLock();
            }

            return Task.FromResult(events);
        }

        public ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to)
        {
            var relevantPayments = new List<PaymentReceivedEvent>();

            try
            {
                _indexLock.EnterReadLock();
                foreach (var entry in _dateIndex)
                {
                    var timestamp = entry.Key;

                    if (from.HasValue && timestamp < from.Value) continue;
                    if (to.HasValue && timestamp > to.Value) break;

                    foreach (var id in entry.Value)
                    {
                        if (_payments.TryGetValue(id, out var payment))
                        {
                            relevantPayments.Add(payment);
                        }
                    }
                }
            }
            finally
            {
                _indexLock.ExitReadLock();
            }

            var summary = relevantPayments
                .GroupBy(p => p.ProcessedBy)
                .Select(g => new SummaryRowReadModel
                {
                    ProcessedBy = g.Key,
                    TotalRequests = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            return ValueTask.FromResult<IEnumerable<SummaryRowReadModel>>(summary);
        }

        public ValueTask PurgeAsync()
        {
            try
            {
                _indexLock.EnterWriteLock();
                _payments.Clear();
                _dateIndex.Clear();
            }
            finally
            {
                _indexLock.ExitWriteLock();
            }
            return ValueTask.CompletedTask;
        }
    }
}
