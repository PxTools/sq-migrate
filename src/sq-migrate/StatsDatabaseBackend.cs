﻿using Microsoft.Data.SqlClient;
using PCAxis.Query;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public class StatsDatabaseBackend(string connectionsString, string owner, IDbProvider provider) : IStatsBackend
    {
        private readonly string _connectionString = connectionsString;
        private readonly string _owner = owner;
        private readonly IDbProvider _dbProvider = provider;

        public async IAsyncEnumerable<SavedQuery> GetQueries()
        {
            using (var conn = _dbProvider.CreateConnection(_connectionString))
            {
                conn.Open();
                var cmd = _dbProvider.CreateCommand($"select QueryText, QueryId, Runs, Fails, UsedDate from {_owner}.SavedQueryMeta", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {

                        string query = reader.GetString(0);
                        SavedQuery? sq;
                        try
                        {
                            sq = JsonHelper.Deserialize<SavedQuery>(query) as SavedQuery;
                            if (sq != null)
                            {
                                sq.LoadedQueryName = reader.GetInt32(1).ToString();
                                sq.Stats.RunCounter = reader.GetInt32(2);
                                sq.Stats.FailCounter = reader.GetInt32(3);
                                sq.Stats.LastExecuted = reader.IsDBNull(4)?new DateTime():reader.GetDateTime(4);
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.WriteException(ex);
                            sq = null;
                        }

                        if (sq != null)
                        {
                            yield return sq;
                        }
                    }
                }
            }
        }

        public int NumberOfQueries()
        {
            using (var conn = _dbProvider.CreateConnection(_connectionString))
            {
                
                conn.Open();

                var cmd = _dbProvider.CreateCommand($"SELECT COUNT(*) FROM {_owner}.SavedQueryMeta", conn);

                int numberOfQueries = Convert.ToInt32(cmd.ExecuteScalar());

                return numberOfQueries;
            }
        }

    }
}
