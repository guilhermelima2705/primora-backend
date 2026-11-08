using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Primora.Application.Importacao;
using Primora.Infrastructure.Persistence;

namespace Primora.Infrastructure.Importacao;

public sealed class EfDatasetImportador(PrimoraDbContext database, LeitorDataset reader) : IDatasetImportador
{
    public async Task<ResultadoImportacao> ImportarAsync(ReadOnlyMemory<byte> conteudo, string nomeOrigem,
        CancellationToken cancellationToken = default)
    {
        var prepared = reader.Preparar(conteudo, nomeOrigem);
        if (database.ChangeTracker.HasChanges())
            throw new InvalidOperationException("O importador exige um contexto sem alterações pendentes.");
        var outer = database.Database.CurrentTransaction;
        await using var owned = outer is null ? await database.Database.BeginTransactionAsync(cancellationToken) : null;
        var transaction = outer ?? owned!;
        var savepoint = "importacao_" + Guid.NewGuid().ToString("N");
        if (outer is not null) await transaction.CreateSavepointAsync(savepoint, cancellationToken);
        try
        {
            // Mesmo hash: serializar a consulta de idempotência e a gravação entre processos.
            var lockKey = BinaryPrimitives.ReadInt64BigEndian(Convert.FromHexString(prepared.Versao.HashSha256));
            await database.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);
            var existing = await database.VersoesDataset.AsNoTracking()
                .SingleOrDefaultAsync(version => version.HashSha256 == prepared.Versao.HashSha256, cancellationToken);
            if (existing is not null)
            {
                if (owned is not null) await owned.CommitAsync(cancellationToken);
                else await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
                return new ResultadoImportacao(existing.Id, existing.HashSha256, false, prepared.Contagens);
            }
            database.VersoesDataset.Add(prepared.Versao);
            await database.SaveChangesAsync(cancellationToken);
            database.AddRange(prepared.EntidadesDependentes);
            await database.SaveChangesAsync(cancellationToken);
            if (owned is not null) await owned.CommitAsync(cancellationToken);
            else await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
            return new ResultadoImportacao(prepared.Versao.Id, prepared.Versao.HashSha256, true, prepared.Contagens);
        }
        catch
        {
            if (owned is not null) await owned.RollbackAsync(CancellationToken.None);
            else await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            foreach (var entity in prepared.EntidadesDependentes.Reverse().Append(prepared.Versao))
                database.Entry(entity).State = EntityState.Detached;
            throw;
        }
    }
}
