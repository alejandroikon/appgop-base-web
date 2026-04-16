using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace GOP.Application.Tests.Common;

/// <summary>
/// Utilidades para crear DbSet mocks que soportan operaciones LINQ async
/// (FirstOrDefaultAsync, ToListAsync, CountAsync) en tests unitarios sin InMemory DB.
/// </summary>
public static class DbSetMockHelper
{
    public static DbSet<T> AsyncMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();
        var asyncProvider = new InMemoryAsyncQueryProvider<T>(data.Provider);

        ((IQueryable<T>)mockSet).Provider.Returns(asyncProvider);
        ((IQueryable<T>)mockSet).Expression.Returns(data.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(data.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(data.GetEnumerator());
        ((IAsyncEnumerable<T>)mockSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(ci => new InMemoryAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}

internal sealed class InMemoryAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        new InMemoryAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new InMemoryAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executed = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute))!
            .MakeGenericMethod(resultType)
            .Invoke(inner, [expression])!;

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executed])!;
    }
}

internal sealed class InMemoryAsyncEnumerable<T>(Expression expression)
    : EnumerableQuery<T>(expression), IAsyncEnumerable<T>, IQueryable<T>
{
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new InMemoryAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class InMemoryAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}
