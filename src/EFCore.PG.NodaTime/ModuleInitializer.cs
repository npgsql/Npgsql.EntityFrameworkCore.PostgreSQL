using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void ModuleInit()
        {
            NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
        }
    }
}
