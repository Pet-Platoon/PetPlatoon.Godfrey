using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Godfrey.Extensions
{
    public static class DatabaseFacadeExtensions
    {
        public static RelationalDataReader ExecuteSqlQuery(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
        {
            var concurrencyDetector = databaseFacade.GetService<IConcurrencyDetector>();

            using (concurrencyDetector.EnterCriticalSection())
            {
                var rawSqlCommand = databaseFacade
                        .GetService<IRawSqlCommandBuilder>()
                        .Build(sql, parameters);

                return rawSqlCommand
                        .RelationalCommand
                        .ExecuteReader(databaseFacade.GetService<IRelationalConnection>(),
                                       rawSqlCommand.ParameterValues);
            }
        }

        public static async Task<RelationalDataReader> ExecuteSqlQueryAsync(this DatabaseFacade databaseFacade, string sql, CancellationToken cancellationToken = default(CancellationToken), params object[] parameters)
        {
            var concurrencyDetector = databaseFacade.GetService<IConcurrencyDetector>();

            using (await concurrencyDetector.EnterCriticalSectionAsync(cancellationToken))
            {
                var rawSqlCommand = databaseFacade
                        .GetService<IRawSqlCommandBuilder>()
                        .Build(sql, parameters);

                return await rawSqlCommand
                              .RelationalCommand
                              .ExecuteReaderAsync(databaseFacade.GetService<IRelationalConnection>(),
                                                  rawSqlCommand.ParameterValues,
                                                  cancellationToken);
            }
        }
    }
}
