using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace mdl {
	public interface IDBDriverDispatcher {
		IDBDriver GetConnection();
	}

	public interface IDBDriver :IDisposable{
		
		int defaultTimeout { get; set; }

		ConnectionState State { get;}
		IDbTransaction CurrentTransaction { get; }

		Task Open();
		Task Close();

		QueryHelper QH { get; }

		Task ChangeDatabase(string dbName);

		string ServerVersion();
		string JoinMultipleSelectCommands(params string[] commands);


		IDbCommand GetDbCommand(string command, int timeout = -1);

		Task<DbDataReader> ExecuteReaderAsync(IDbCommand command,
											  CommandBehavior behavior = CommandBehavior.Default,
											  System.Threading.CancellationToken cancellationToken = default);
		Task DisposeCommand(IDbCommand command);

		

		Task<DataTable> TableBySql(string sql, DataTable table = null, int timeout = -1);
		Task<DataSet> MultipleTableBySql(string sql, List<string> destTables, int timeout = -1);

		/// <summary>
		/// Executes a sql command and get the result tables in a DataSet
		/// </summary>
		/// <param name="d"></param>
		/// <param name="sql"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		Task SqlIntoDataSet(DataSet d, string sql, int timeout = -1);

		/// <summary>
		/// Reads a single value
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		Task<object> ExecuteScalar(string sql,  int timeout = -1);

		/// <summary>
		/// Execute a command that eventually modifies db
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="timeout"></param>
		/// <returns>affected rows</returns>
		Task<int> ExecuteNonQuery(string sql, int timeout = -1);

		Task<IDbTransaction> BeginTransaction(IsolationLevel level);
		Task Rollback();
		Task Commit();

		/// <summary>
		/// Check if a valid transaction exists
		/// </summary>
		/// <returns></returns>
		bool HasValidTransaction();

		/// <summary>
		/// Check if a non valid transaction exists
		/// </summary>
		/// <returns></returns>
		bool HasInvalidTransaction();

		/// <summary>
		/// Dummy command used to check if connection is still up. For example select getdate()
		/// </summary>
		string DummyCommand { get; }

		Task AutoDetectTable(dbstructure DBS, string tableName);

		/// <summary>
		/// Detect a table structure using db functions
		///   ColTypes to reflect info read
		/// </summary>
		/// <param name="colTypes"></param>
		/// <param name="tableName"></param>
		/// <returns>true when successfull</returns>
		Task<bool> EvaluateColumnTypes(DataTable colTypes, string tableName);

		string SchemaObject(string schema, string objectName);

		bool ForcelyClosed { get; }

		string GetSelectCommand(string table, string columns = "*", string filter = null, string orderBy = null, string top = null);
		string GetDeleteCommand(string table, string condition);
		string GetUpdateCommand(string table, string filter, Dictionary<string, object> values);
		string GetJoinSql(string table1, string table2, string columns, string whereFilter = null, string joinFilter = null);

		string GetStoredProcedureCall(string sp_name, object[] parameters);
		Task CallVoidSPParams(string sp_name, DbParameter[] parameters, int timeout = -1);
		Task<DataSet> CallSPParams(string sp_name, DbParameter[] parameters, int timeout = -1);

		/// <summary>
		/// Returns an arraylist of names of DataBase (real) tables 
		/// </summary>
		/// <param name="conn"></param>
		/// <returns></returns>
		Task<List<string>> TableListFromDB();

		/// <summary>
		/// Returns an list of names of DataBase Views
		/// </summary>
		/// <param name="conn"></param>
		/// <returns></returns>
		Task<List<string>> ViewListFromDB();

		
	}

	


}
