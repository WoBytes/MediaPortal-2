#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.Database.Firebird.Settings;

namespace MediaPortal.Database.Firebird
{
  /// <summary>
  /// Database implementation for the FirebirdDotNet implementation.
  /// </summary>
  /// <remarks>
  /// Per connection, only one parallel transaction is supported.
  /// </remarks>
  public class FirebirdSQLDatabase : ISQLDatabase, IDisposable
  {
    public const string FIREBIRD_DATABASE_TYPE = "Firebird";
    public const string DATABASE_VERSION = "2.1.2";

    protected string _connectionString;

    #region Ctor & dtor

    public FirebirdSQLDatabase()
    {
      FirebirdSettings settings = ServiceScope.Get<ISettingsManager>().Load<FirebirdSettings>();
      try
      {
        if (!File.Exists(settings.DatabaseFile))
          FbConnection.CreateDatabase(settings.DatabaseFile);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Critical("Error establishing database connection", e);
        throw;
      }
      FbConnectionStringBuilder sb = new FbConnectionStringBuilder
        {
            ServerType = settings.ServerType,
            UserID = settings.UserID,
            Password = settings.Password,
            Dialect = 3,
            Database = settings.DatabaseFile,
            Pooling = false // We use our own pooling mechanism
        };
      _connectionString = sb.ConnectionString;
    }

    ~FirebirdSQLDatabase()
    {
      Dispose();
    }

    public void Dispose()
    {
      FbConnection.ClearAllPools();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Builds a restrictions string array to be used with method <see cref="FbConnection.GetSchema(string,string[])"/>.
    /// </summary>
    /// <remarks>
    /// All parameters are case sensitive. If a table wasn't explicitly created case-sensitive, its name must be given in
    /// upper-case.
    /// </remarks>
    /// <param name="table_catalog">Restricts by catalog.</param>
    /// <param name="table_schema">Restricts by table schema.</param>
    /// <param name="table_name">Restricts by table name.</param>
    /// <param name="table_type">Restricts by table type. Supported types: "VIEW", "SYSTEM TABLE", "TABLE".</param>
    /// <returns></returns>
    protected string[] BuildSchemaQueryRestrictions(string table_catalog, string table_schema, string table_name, string table_type)
    {
      return new string[] { table_catalog, table_schema, table_name, table_type };
    }

    protected FbConnection CreateConnection()
    {
      return new FbConnection(_connectionString);
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return FIREBIRD_DATABASE_TYPE; }
    }

    public string DatabaseVersion
    {
      get { return DATABASE_VERSION; }
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return FirebirdTransaction.BeginTransaction(_connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public bool TableExists(string tableName, bool caseSensitiveName)
    {
      FbConnection con = CreateConnection();
      try
      {
        if (!caseSensitiveName)
          tableName = tableName.ToUpperInvariant();
        DataTable dt = con.GetSchema("TABLES", BuildSchemaQueryRestrictions(null, null, tableName, null));
        using (DataTableReader dtr = dt.CreateDataReader())
          return dtr.Read();
      }
      finally
      {
        con.Close();
      }
    }

    public void ExecuteScript(string sqlScript, bool autoCommit)
    {
      FbConnection con = CreateConnection();
      FbScript script = new FbScript(new StringReader(sqlScript));
      script.Parse();
      FbBatchExecution batch = new FbBatchExecution(con, script);
      batch.Execute(autoCommit);
    }

    public void ExecuteBatch(IList<string> sqlStatements, bool autoCommit)
    {
      FbConnection con = CreateConnection();
      FbBatchExecution batch = new FbBatchExecution(con);
      foreach (string statement in sqlStatements)
        batch.SqlStatements.Add(statement);
      batch.Execute(autoCommit);
    }

    public void ExecuteBatch(string sqlScriptFilePath, bool autoCommit)
    {
      FbConnection con = CreateConnection();
      FbScript script = new FbScript(sqlScriptFilePath);
      script.Parse();
      FbBatchExecution batch = new FbBatchExecution(con, script);
      batch.Execute(autoCommit);
    }

    public void ExecuteBatch(TextReader reader, bool autoCommit)
    {
      FbConnection con = CreateConnection();
      FbScript script = new FbScript(reader);
      script.Parse();
      FbBatchExecution batch = new FbBatchExecution(con, script);
      batch.Execute(autoCommit);
    }

    #endregion
  }
}
