﻿using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using EvaluationSystem.Application.Interfaces;

namespace EvaluationSystem.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private IDbConnection _connection;
        private IDbTransaction _transaction;

        public UnitOfWork(IConfiguration configuration)
        {
            _connection = new SqlConnection(configuration.GetConnectionString("EvaluationSystemDBConnection"));
            _connection.Open();
        }
        public IDbConnection Connection => _connection;
        public IDbTransaction Transaction => _transaction;

        public void Begin()
        {
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {
            _transaction.Commit();
            Dispose();
        }

        public void Rollback()
        {
            _transaction.Rollback();
            Dispose();
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
