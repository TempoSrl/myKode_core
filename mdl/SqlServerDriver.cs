using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using SqlDataAdapter = AsyncDataAdapter.SqlDataAdapter;
using LM = mdl_language.LanguageManager;
using q = mdl.MetaExpression;
using static mdl_utils.MetaProfiler;
using System.Linq;

namespace mdl {

	public class SqlServerDriverDispatcher :IDbDriverDispatcher {
		public string DSN { get; set; }
		public string Server { get;  }
		public string Database { get; }
		public string UserDB { get; }
		public string PasswordDB { get; }		
        public bool SSPI { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Server">Server name or Server address</param>
        /// <param name="Database">DataBase name, eventually with port</param>
        public SqlServerDriverDispatcher(string Server, string Database) {
            this.Server = Server;
            this.Database = Database;
           
            this.PasswordDB = null;
            this.SSPI = true;            
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DSN">Simbolic name for the connection</param>
        /// <param name="Server">Server name or Server address</param>
        /// <param name="Database">DataBase name, eventually with port</param>
        /// <param name="UserDB"></param>
        /// <param name="PasswordDB"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SqlServerDriverDispatcher(string Server, string Database, string UserDB, string PasswordDB) {
			this.Server = Server;
			this.Database = Database;
			this.UserDB = UserDB;
			this.PasswordDB = PasswordDB;
            this.SSPI=false;

            if (UserDB == null) {              
               throw new ArgumentNullException("UserDB");
            }

            if (String.IsNullOrEmpty(PasswordDB)) {
                throw new ArgumentNullException("PasswordDB");
            }
                       
		}
        string getConnectionString() {
            if (SSPI) {
                return "data source=" + Server +
                ";initial catalog=" + Database +
                ";integrated Security=SSPI" +
                //";Application Name=" + appName +
                ";WorkStation ID =" + Environment.MachineName +
                ";Pooling=false" +
                ";Connection Timeout=300;";
            }
            return "data source=" + Server +
               ";initial catalog=" + Database +
               ";User ID =" + UserDB +
               ";Password=" + PasswordDB +
               //";Application Name=" + appName +
               ";WorkStation ID =" + Environment.MachineName +
               ";Pooling=false" +
               ";Connection Timeout=300;";
        }

        public virtual IDbDriver GetConnection() {            
            return new SqlServerDriver(new SqlConnection(getConnectionString()), Database);            
        }


	}



	public class SqlServerDriver :IDbDriver {
        SqlTransaction transaction;
        bool arithAborthSet = false;
        string databaseName;

        public string DummyCommand { get; }= "select getdate()";

        public IDbTransaction CurrentTransaction { get {
                return transaction; 
			}
        }
        public bool ForcelyClosed { get; set;}

        private SqlConnection conn;

        public int defaultTimeout { get; set; }=300;

        public QueryHelper QH { get; }= new SqlServerQueryHelper();

        public ConnectionState State {
			get {                
				return this.conn.State;
			}
		}
        

		public SqlServerDriver(SqlConnection conn, string databaseName) {
			this.conn = conn;
            this.databaseName = databaseName;
		}

		public async Task Close() {
			await this.conn.CloseAsync();
		}

		public async Task Open() {
			await this.conn.OpenAsync();
            if (!arithAborthSet) {
                arithAborthSet = true;
                try {
                    await this.ExecuteNonQuery("SET ARITHABORT ON");
                }
                catch {
                }

            }
        }

        public async Task ChangeDatabase(string dbName) {
            await this.conn.ChangeDatabaseAsync(dbName);
		}

        /// <summary>
        /// Get Sql Server Version
        /// </summary>
        /// <returns></returns>
        public virtual string ServerVersion() {
            if (conn == null)
                return "no connection";
            if (conn.State == ConnectionState.Open)
                return conn.ServerVersion;
            return "Closed";
        }

        public virtual void Dispose() {
            conn.Dispose();
		}
        public virtual string JoinMultipleSelectCommands(params string[] commands) {
            return String.Join(";", commands);
		}

        public virtual IDbCommand GetDbCommand(string command, int timeout = -1) {
            if (timeout == -1)
                timeout = defaultTimeout;
            return new SqlCommand(command, conn, transaction) {
                CommandTimeout = timeout
            };
        }


        /// <summary>
        /// Executes a sql command and get the result tables in a DataSet
        /// </summary>
        /// <param name="d"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public virtual async Task SqlIntoDataSet(DataSet d, string sql, int timeout = -1) {
            if (HasInvalidTransaction()) {
                throw new Exception(LM.noValidTransaction);
            }

            var sqlCommand = GetDbCommand(sql, timeout) as SqlCommand;
            int NN = StartTimer("SqlIntoDataSet*" + sql);
            SqlDataAdapter myDA = null;
            try {
                myDA = new SqlDataAdapter(sqlCommand);
                myDA.SelectCommand.Transaction = transaction;
                await myDA.FillAsync(d);
            }
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"SqlIntoDataSet running {sql}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"SqlIntoDataSet:{sql}", ex);
            }
            finally {
                await sqlCommand.DisposeAsync();
                myDA?.Dispose();
                StopTimer(NN);
            }
        }

        /// <summary>
        /// Runs a sql command that return a DataTable
        /// </summary>
        /// <param name="sql">sql command</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMsg">Error message or null when no errors</param>
        /// <returns></returns>
        public virtual async Task<DataSet> MultipleTableBySql(string sql, List<string> destTables, int timeout = -1) {
            var DD = new DataSet("x");

            var Cmd = GetDbCommand(sql, timeout) as SqlCommand;
           

            var DA = new SqlDataAdapter(Cmd);
            DA.SelectCommand.Transaction = transaction;

            var multitab="";
            for (var i = 0; i < destTables.Count; i++) {
                multitab += destTables[i] + " ";
                if (i == 0) {
                    DA.TableMappings.Add("Table", destTables[i]);
                }
                else {
                    DA.TableMappings.Add("Table" + i, destTables[i]);
                }
            }



            try {
                //int handleFill = metaprofiler.StartTimer("MULTISEL DA.Fill" + multitab);
                //DA.Fill(D);
                //metaprofiler.StopTimer(handleFill);

                ClearDataSet.RemoveConstraints(DD);
                var handleFill = StartTimer("MultipleTableBySql DA.FillAsync*" + multitab);
                await DA.FillAsync(DD);
                StopTimer(handleFill);
            }

            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"MultipleTableBySql running {sql}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"MultipleTableBySql:{sql}", ex);
            }
            finally {
                DA.Dispose();
                await Cmd.DisposeAsync();
            }


            return DD;
        }


        public virtual async Task<DbDataReader> ExecuteReaderAsync(IDbCommand command, 
                                                CommandBehavior behavior = CommandBehavior.Default, 
                                                System.Threading.CancellationToken cancellationToken = default) {
            var cmd = command as SqlCommand;
            var Read = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            return Read;
        }


        public async Task DisposeCommand(IDbCommand command) {
            if (command == null) {
                return;
            }
            if (command is IAsyncDisposable disposable) {
                await disposable.DisposeAsync();
            }
            else {
                command.Dispose();
            }
        }

        public virtual bool HasValidTransaction() {
            if (transaction== null) return false;
            return transaction.Connection!=null;
        }

        public virtual bool HasInvalidTransaction() {
            if (transaction == null) return false;
            return transaction.Connection == null;
        }

         
               

        public virtual string quote(object o) {
            return QH.quote(o);
        }

        /// <summary>
        /// Runs a sql command that return a DataTable (ex SQLRunner)
        /// </summary>
        /// <param name="sql">sql command</param>
        /// <param name="table">optional Table Model</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public virtual async Task<DataTable> TableBySql(string sql, DataTable table=null, int timeout = -1) {
            var cmd = new SqlCommand(sql, conn, transaction);
            if (timeout == -1) cmd.CommandTimeout = defaultTimeout;

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.SelectCommand.Transaction = transaction;
            var t = table ?? new DataTable();
            try {
                await dataAdapter.FillAsync(t); //MyDataAdapter.Fill(T)
            }
           
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"TableBySql running {sql}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"TableBySql:{sql}", ex);
            }
            finally {
                dataAdapter.Dispose();
            }

            return t;
        }

        public async virtual Task<IDbTransaction> BeginTransaction(IsolationLevel level) {
            transaction =  await conn.BeginTransactionAsync(level) as SqlTransaction;
            return transaction;
        }

        public async virtual Task Rollback() {
            
            try {
                if (transaction.Connection != null) {
                    //connection can be null if xact_abort is used 
                    await transaction?.RollbackAsync();
                }
            }
            finally {
                transaction?.Dispose();
                transaction = null;
            }
            
        }

        public async virtual  Task Commit() {
            try {
                await transaction.CommitAsync();
            }
            finally {
                transaction?.Dispose();
                transaction = null;
            }            
		}

        /// <summary>
        /// Returns a value executing a generic sql command  (ex ex DO_SYS_CMD)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ErrMsg">eventual error message</param>
        /// <returns></returns>
        public virtual async Task<object> ExecuteScalar(string sql, int timeout = -1) {

            var Cmd = GetDbCommand(sql, timeout) as SqlCommand;

            try {
                return await Cmd.ExecuteScalarAsync();
            }
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"ExecuteScalar running {sql}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"ExecuteScalar:{sql}", ex);
            }
            finally {
                await Cmd.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a value executing a generic sql command  (ex ex DO_SYS_CMD)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ErrMsg">eventual error message</param>
        /// <returns></returns>
        public virtual async Task<int> ExecuteNonQuery(string sql, int timeout = -1) {

            var Cmd = GetDbCommand(sql, timeout) as SqlCommand;

            try {
                return await Cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"SqlIntoDataSet running {sql}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"ExecuteNonQuery:{sql}", ex);
            }
            finally {
                await Cmd.DisposeAsync();
            }
        }






        /// <summary>
        /// Evaluate customobject and columntypes analyzing db table properties. Results are stored in DBS and should
        ///  be saved in db tables by the caller.
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="objectname"></param>
        /// <param name="forcerefresh">if false, only new tables are scanned</param>
        public virtual async Task AutoDetectTable(dbstructure DBS, string tableName) {//MUST BECOME PROTECTED
            bool IsReal = false;
            bool IsView = false;
            object nameReal = await ExecuteScalar("select name from sysobjects where "+( q.eq("xtype", "U") & q.eq("name", tableName)).toSql(QH));

            if (nameReal == DBNull.Value || nameReal == null) {
                object nameView = await ExecuteScalar("select name from sysobjects where "+ (q.eq("xtype", "V") & q.eq("name", tableName)).toSql(QH));
                if (nameView != DBNull.Value && nameView != null) {
                    IsView = true;
                }
            }
            else {
                IsReal = true;
            }

            if ((!IsReal) && (!IsView))  return;
            DataRow CurrObj;
            if (DBS.customobject.Rows.Count == 0) {
                CurrObj = DBS.customobject.NewRow();
                CurrObj["objectname"] = tableName;
                DBS.customobject.Rows.Add(CurrObj);
            }
            else {
                CurrObj = DBS.customobject.Rows[0];
            }
            CurrObj["isreal"] = IsReal ? "S" : "N";
            await EvaluateColumnTypes(DBS.columntypes, tableName);
           
            //await SaveStructure(DBS);

        }


        /// <summary>
        /// Gets the names of primary keys field of a DB table querying the DB system
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="conn"></param>
        /// <returns>null on errors</returns>
        public async Task<List<string>> GetPrimaryKey(string tableName) {
            var myKey = new List<string>();
            //"sp_pkeys @table_name = 'tabella', @table_owner = 'dbo',@table_qualifier = 'database'";
            var cmdText = $"sp_pkeys @table_name = '{tableName}',@table_qualifier = '{databaseName}'";
            var keyNames = await TableBySql(cmdText);
            if (keyNames == null) return null;

            foreach (DataRow keyName in keyNames.Rows) {
                myKey.Add(keyName["Column_Name"].ToString());
            }
            return myKey;
        }




        /// <summary>
        /// Gets from DataBase info about a table, and add/update/deletes   (ex ReadColumnTypes)
        ///   ColTypes to reflect info read
        /// </summary>
        /// <param name="colTypes"></param>
        /// <param name="tableName"></param>
        /// <returns>true when successfull</returns>
        public async Task<bool> EvaluateColumnTypes(DataTable colTypes, string tableName) {

            //Reads table structure through MShelpcolumns Database call
            var cmdText2 = $"exec sp_MShelpcolumns N'{tableName}'";
            DataTable dtColumns = await TableBySql(cmdText2);
            if (dtColumns == null)     return false;

            var tableKeys = await GetPrimaryKey(tableName);
            if (tableKeys == null) return false;

            //columns returned are:
            //col_name, col_len, col_prec, col_scale, col_basetypename, col_defname,
            // col_rulname, col_null, col_identity, col_flags, col_seed,
            // col_increment col_dridefname, text, col_iscomputed text, col_NotForRepl,
            // col_fulltext, col_AnsiPad, col_DOwner, col_DName, 
            // col_ROwner, col_RName

            //Per ogni riga della tabella, ovvero per ogni colonna presente nella tabella TableName
            foreach (DataRow myDr in dtColumns.Rows) {
                var colname = myDr["col_name"].ToString();
                DataRow[] myDrSelect = colTypes.filter(q.mCmp(new { tablename = tableName, field = colname }));
                DataRow currCol;
                var newColumn = false;
                if (myDrSelect.Length == 0) {
                    currCol = colTypes.NewRow();
                    currCol["tablename"] = tableName;
                    currCol["field"] = colname;
                    currCol["defaultvalue"] = "";
                    newColumn = true;
                }
                else {
                    currCol = myDrSelect[0];
                }

                currCol["sqltype"] = myDr["col_typename"];
                currCol["systemtype"] = GetType_Util.GetSystemType_From_SqlDbType(myDr["col_typename"].ToString());
                currCol["col_len"] = myDr["col_len"];
                if (myDr["col_null"].ToString() == "False") {
                    currCol["allownull"] = "N";
                    if (newColumn)
                        currCol["denynull"] = "S";
                }
                else {
                    currCol["allownull"] = "S";
                    if (newColumn)
                        currCol["denynull"] = "N";
                }
                currCol["col_precision"] = myDr["col_prec"];
                currCol["col_scale"] = myDr["col_scale"];
                var sqlDecl = myDr["col_typename"].ToString();
                if ((sqlDecl == "varchar") || (sqlDecl == "char") || (sqlDecl == "nvarchar") || (sqlDecl == "nchar")
                    || (sqlDecl == "binary") || (sqlDecl == "varbinary")
                    ) {
                    if ((myDr["col_len"].ToString() == "-1") || (myDr["col_len"].ToString() == "0")) {
                        sqlDecl += "(max)";
                    }
                    else {
                        sqlDecl += $"({myDr["col_len"]})";
                    }
                }
                if (sqlDecl == "decimal") {
                    sqlDecl += $"({myDr["col_prec"]},{myDr["col_scale"]})";
                }

                currCol["sqldeclaration"] = sqlDecl;
                var isKey = "N";
                foreach (string colName in tableKeys) {
                    if (myDr["col_name"].ToString() != colName)
                        continue;
                    isKey = "S";
                    break;
                }
                currCol["iskey"] = isKey;
                if (newColumn)
                    colTypes.Rows.Add(currCol);
            }

            foreach (var existingCol in colTypes.Select()) {
                //var rSelect = "(col_name='"+existingCol["field"]+"')";
                DataRow[] exists = dtColumns.filter(q.eq("col_name", existingCol["field"]));
                if (exists.Length > 0) continue;
                existingCol.Delete();
            }

            return true;
        }


        /// <summary>
        /// Builds a sql SELECT command 
        /// </summary>
        /// <param name="table">table implied</param>
        /// <param name="condition">condition for the deletion, can be a string or a MetaExpression</param>
        /// <returns></returns>
        public virtual string GetSelectCommand(string table, string columns = "*", string filter = null, string orderBy = null, string top = null) {
            if (top != null) {
                top = $" TOP {top}";
            }
            else {
                top = "";
            }
            string where = "";
            if (filter != null) {
                where = " WHERE " + filter;
            }
            string sort = "";
            if (orderBy != null) {
                sort = $" ORDER BY {orderBy}";
            }
            string cmd = $"SELECT {top} {columns} FROM {table} {where} {sort}";
            return cmd;
        }

        /// <summary>
        /// Builds a sql DELETE command 
        /// </summary>
        /// <param name="table">table implied</param>
        /// <param name="condition">condition for the deletion, can be a string or a MetaExpression</param>
        /// <returns></returns>
        public virtual string GetDeleteCommand(string table, string condition) {
            string DeleteCmd = $"DELETE FROM {table}";
            if (condition != null)
                DeleteCmd += " WHERE " + condition;
            return DeleteCmd;
        }

        /// <summary>
        /// Builds an UPDATE sql command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="condition"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to be set</param>
        /// <returns>Error msg or null if OK</returns>
        public virtual string GetUpdateCommand(string table, string filter, Dictionary<string, object> values) {
            string UpdateCmd = $"UPDATE {table} SET ";
            string outstring = "";
            bool first = true;
            foreach (var kv in values) {
                if (first)
                    first = false;
                else
                    outstring += ",";
                outstring += kv.Key + "=" + quote(kv.Value);
            }
            UpdateCmd += outstring;            
            if (filter != null)
                UpdateCmd += $" WHERE {filter}";
            return UpdateCmd;
        }

        public virtual string GetJoinSql(string table1, string table2, string columns, string whereFilter = null, string joinFilter=null) {           
            return (whereFilter == null) ?
                $"SELECT {columns} from {table1} JOIN {table2} ON  {joinFilter}" :
                $"SELECT {columns} from {table1} JOIN {table2} ON  {joinFilter} WHERE {whereFilter} ";
        }



        /// <summary>
        /// Returns a reference to the object named objectName in the schema 
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public virtual string SchemaObject(string schemaName, string objectName) {
            if (String.IsNullOrEmpty(schemaName)) return objectName;
            schemaName = $"[{schemaName}]";
            if (!objectName.StartsWith(schemaName + ".")) {
                return schemaName + "."+ objectName;
			}
            return objectName;
		}

        public virtual string GetStoredProcedureCall(string sp_name, object[] parameters) {
            string cmd = sp_name + " ";
            bool first = true;
            for (int i = 0; i < parameters.Length; i++) {
                if (!first)
                    cmd += ", ";
                first = false;
                cmd += quote(parameters[i]);
            }
            return cmd;
        }

        static string paramString(DbParameter[] par) {
            string s = "";
            int i = 0;
            foreach (var p in par) {
                if (i > 0)
                    s += ",";
                s += p.ParameterName + "=";
                if (p.Value == null || p.Value == DBNull.Value) {
                    s += "(null)";
                }
                else {
                    s += p.Value.ToString();
                }
                i++;
            }

            return "(" + s + ")";
        }

        /// <summary>
        /// Calls a stored procedure, return true if ok
        /// </summary>
        /// <param name="sp_name">name of stored procedure to call</param>
        /// <param name="ParamName">parameter names to give to the stored procedure</param>
        /// <param name="ParamType">parameter types to give to the stored procedure</param>
        /// <param name="ParamTypeLength">Length of parameters</param>
        /// <param name="ParamDirection">Type of parameters</param>
        /// <param name="ParamValues">Value for parameters</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMsg">null if ok, Error message otherwise</param>
        /// <returns></returns>
        virtual public async Task CallVoidSPParams(string sp_name, DbParameter [] parameters, int timeout=-1) {

            var MyCommand = new SqlCommand(sp_name, conn, transaction);
            if (timeout != -1)
                MyCommand.CommandTimeout = timeout;
            else
                MyCommand.CommandTimeout = defaultTimeout;

            MyCommand.CommandType = CommandType.StoredProcedure;
            foreach (var p in parameters) {
                MyCommand.Parameters.Add(p);
			}

            try { 
                await MyCommand.ExecuteNonQueryAsync();            
            }
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"callVoidSP: Error calling stored procedure {sp_name}{paramString(parameters)}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"callVoidSP: Error calling stored procedure {sp_name}{paramString(parameters)}", ex);
            }
            finally {
                MyCommand.Dispose();
            }            
        }

        virtual public async Task<DataSet> CallSPParams(string sp_name, DbParameter[] parameters, int timeout=-1) {

            SqlCommand cmd = new SqlCommand(sp_name, conn, transaction);
            if (timeout != -1) {
                cmd.CommandTimeout = timeout;
            }
            else {
                cmd.CommandTimeout = 90;
            }
            var ds = new DataSet();

            cmd.CommandType = CommandType.StoredProcedure;
            foreach (var p in parameters) {
                cmd.Parameters.Add(p);
            }
            SqlDataAdapter myDa=null;
            try {
                myDa = new SqlDataAdapter(cmd);
                if (cmd.Transaction != null)
                    myDa.SelectCommand.Transaction = cmd.Transaction;
               
                await myDa.FillAsync(ds);     
            }
            catch (SqlException ex) {
                if (ex.Class >= 20) {
                    ForcelyClosed = true;
                }
                throw new Exception($"callVoidSP: Error calling stored procedure {sp_name}{paramString(parameters)}", ex);
            }
            catch (Exception ex) {
                throw new Exception($"callVoidSP: Error calling stored procedure {sp_name}{paramString(parameters)}", ex);
            }

            finally {
                myDa.Dispose();
                await cmd.DisposeAsync();               
            }

            return ds;

        }

        /// <summary>
        /// Returns an arraylist of names of DataBase (real) tables 
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public async Task<List<string>> TableListFromDB() {
            return await objectListFromDB( "U");
        }

        /// <summary>
        /// Returns an arraylist of names of DataBase Views
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public async Task<List<string>> ViewListFromDB() {
            return await objectListFromDB( "V");
        }

        /// <summary>
        /// Gets object description from db filtering on xtype field
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="kind">xtype parameter, use U for tables, V for views</param>
        /// <returns>list of specified db object names</returns>
        async Task<List<string>> objectListFromDB(string kind) {
            var selCmd = GetSelectCommand(table: "sysobjects",
                                          columns:"name",
                                          filter: q.eq("xtype", kind).toSql(QH));
            var list = await TableBySql(selCmd);
            var outlist = new HashSet<string>();
            foreach (DataRow r in list.Rows) {
                outlist.Add(r["name"].ToString());
            }
            return outlist.ToList();
        }


    }
}
