using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
//using System.Data.SqlClient;
using System.Threading.Tasks;
using q = mdl.MetaExpression;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {

    /// <summary>
    /// Interface that manages database transactions
    /// </summary>
    public interface ITransactionManagement {
        /// <summary>
        /// Start a "post" process, this doesnt mean to be called by applications
        /// </summary>
        /// <param name="mainConn"></param>
        void startPosting(IDataAccess mainConn);

        /// <summary>
        /// Ends a "post" process , this doesnt mean to be called by applications
        /// </summary>
        void stopPosting();

        /// <summary>
        /// Gets Current used Transaction
        /// </summary>
        /// <returns>null if no transaction is open</returns>
        IDbTransaction CurrentTransaction();

        /// <summary>
        /// Starts a new transaction 
        /// </summary>
        /// <param name="L"></param>
        /// <returns>error message, or null if OK</returns>
        Task BeginTransaction(IsolationLevel L);

        /// <summary>
        /// Commit the transaction
        /// </summary>
        /// <returns>error message, or null if OK</returns>
        Task Commit();

        /// <summary>
        /// Rollbacks transaction
        /// </summary>
        /// <returns>Error message, or null if OK</returns>
        Task Rollback();

        /// <summary>
        /// True if current transaction  is still alive, i.e. has a connection attached to it
        /// </summary>
        /// <returns></returns>
        bool HasValidTransaction();

        /// <summary>
        /// Release connection and resources
        /// </summary>
        /// <returns></returns>
        Task Destroy();

    }


    /// <summary>
    /// Interface to db access
    /// </summary>
    public interface IDataAccess:ITransactionManagement {

        IDBDriver Driver { get; set; }
        IDbDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the db name of a table
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        string GetPrefixedTable(string table);

        /// <summary>
        /// MetaModel used for this connection
        /// </summary>
        IMetaModel model { get; set; }


		/// <summary>
		/// Manages security conditions with this connection 
		/// </summary>
		ISecurity Security { get;  }

        /// <summary>
        /// Is invoked if Security to evaluate property Security if it is undefined
        /// </summary>
        /// <returns></returns>
        ISecurity CreateSecurity(string user);

  
       
        /// <summary>
        /// True if opening was not successful
        /// </summary>
        bool BrokenConnection { get; set; }

        /// <summary>
        /// Return true if Connection is using Persisting connections mode, i.e.
        ///  it is open at the beginning and closed only when instance is destroyed , no matter how
        ///   many "close" commands are executed in the application.
        ///  So every application method can invoke open/close while the decision to have a persistent or 
        ///   on-demand connection is made at an higher level, when the connection is created.
        /// </summary>
        bool Persisting { get; set; }


        /// <summary>
        /// Returns last error and resets it.
        /// </summary>
        string LastError { get; }

        /// <summary>
        /// Get last error without clearing it
        /// </summary>
        /// <returns></returns>
        string SecureGetLastError();




        #region async functions with callbacks

        /// <summary>
        /// Run a command string and get result asynchronously
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="packetSize"></param>
        /// <param name="timeout"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task ExecuteQueryTables(string sql, int packetSize, int timeout, Func<object, Task> callback);

        /// <summary>
        ///  Async version of a run selectBuilder
        /// </summary>
        /// <param name="selList"></param>
        /// <param name="packetSize"></param>
        /// <param name="callback"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task ExecuteSelectBuilder(List<SelectBuilder> selList, int packetSize,Action<SelectBuilder ,Dictionary<string, object>> callback, int timeout);



        #endregion


        /// <summary>
        /// When true, access to the table are prefixed with DBO.  It is meant to be redefined in derived classes
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        bool IsCommonSchemaTable(string tablename);

        /// <summary>
        /// When true, access to the table are prefixed with DBO.  It is meant to be redefined in derived classes
        /// </summary>
        /// <param name="procname"></param>
        /// <returns></returns>
        bool IsCommonSchemaProcedure(string procname);


        /// <summary>
        /// Use another database with this connection
        /// </summary>
        /// <param name="DBName"></param>
        Task ChangeDatabase(string DBName);

        /// <summary>
        /// Crea un duplicato di un DataAccess, con una nuova connessione allo stesso DB. 
        /// Utile se la connessione deve essere usata in un nuovo thread.
        /// </summary>
        /// <returns></returns>
        DataAccess Clone();


        /// <summary>
        /// Open the connection (or increment nesting if already open)
        /// </summary>
        /// <returns> true when successfull </returns>
        Task<bool> Open();

        /// <summary>
        /// Close the connection
        /// </summary>
        Task Close();

        #region DataBase structure management




        /// <summary>
        /// Reads table structure of a list of tables (Has only effect if UseCustomObject is true)
        /// </summary>
        /// <param name="tableName"></param>
        Task ReadStructures(params string[] tableName);



        /// <summary>
        /// Reads or evaluates all the tables/view (may require a bit)
        /// </summary>
        Task ReadStructures();

     
        /// <summary>
        /// Evaluate columntypes and customobject analizing db table properties
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="objectname"></param>
        /// <param name="forcerefresh">if false, only new tables are scanned</param>
        Task DetectStructure(string tablename);

       

        /// <summary>
        /// Forces ColumnTypes to be read again from DB for tablename
        /// </summary>
        /// <param name="tablename"></param>
        Task RefreshStructure(string tablename);

        /// <summary>
        /// Reads extended informations for a table related to a view,
        ///  in order to use it for posting. Reads data from viewcolumn.
        ///  Sets table and columnfor posting and also sets ViewExpression as tablename.columnname (for each field)
        /// </summary>
        /// <param name="T"></param>
        Task GetViewStructureExtProperties(DataTable T);

		
        #endregion

         /// <summary>
        /// If true, customobject and column types are used to describe table structure, 
        ///  when false, those are always obtained from DB
        /// </summary>
        bool UseCustomObject {get;set;}


        /// <summary>
        /// Creates an empty DataTable basing on columntypes info. Adds also primary key 
        ///  information to the table, and allownull to each field.
        ///  Columnlist must include primary table, or can be "*"
        /// </summary>
        /// <param name="tablename">name of table to create. Can be in the form DBO.tablename or department.tablename</param>
        /// <param name="columns">null means "*" which means all table columns</param>
        /// <param name="addExtProp">Add db information as extended propery of columns (column length, precision...)</param>
        /// <returns>a table with same types as DB table</returns>
        Task<DataTable> CreateTable(string tablename, string columns="*", bool addExtProp = false);

        /// <summary>
        /// Adds all extended information to table T taking them from columntypes.
        /// Every Row of columntypes is assigned to the corresponding extended 
        ///  properties of a DataColumn of T. Each Column of the Row is assigned
        ///  to an extended property with the same name of the Column
        ///  Es. R["a"] is assigned to Col.ExtendedProperty["a"]
        /// </summary>
        /// <param name="T"></param>
        Task AddExtendedProperties(DataTable T);


        #region MetaData informations management

        public void SetLastRead();
        public void SetLastWrite();

        /// <summary>
        /// Empty table structure information about a listing type of a table
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="listtype">when null, all list types of the table are forgot</param>
        Task ResetListType(string tablename, string listtype=null);


        /// <summary>
        /// Gets a DBS to describe columns of a list. returns also target-list type, that
        ///  can be different from input parameter listtype. Reads from customview,
        ///   customviewcolumn, customorderby, customviewwhere and from customredirect
        ///  Target-Table can be determined as DBS.customobject.rows[0]
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="tablename"></param>
        /// <param name="listtype"></param>
        /// <returns></returns>
        Task<(string listType, dbstructure dbs)> GetListType(string tablename, string listtype);

        /// <summary>
        /// Get information about an edit type. Reads from customedit 
        /// </summary>
        /// <param name="objectname"></param>
        /// <param name="edittype"></param>
        /// <returns>CustomEdit DataRow about an edit-type</returns>
        Task<DataRow> GetFormInfo(string objectname, string edittype);


        /// <summary>
        /// Gets the system type name of a field named fieldname
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        string GetFieldSystemTypeName(dbstructure DBS, string fieldname);

        
        /// <summary>
        /// Gets the corresponding system type of a db column named fieldname
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        Type GetFieldSystemType(dbstructure DBS, string fieldname);

        #endregion

        /// <summary>
        /// Async execute a sql statement amd returns the affected rows if any
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="timeOut"></param>
        /// <returns>number of affected rows</returns>
        Task<int> ExecuteNonQuery(string sql, int timeOut = -1);


        /// <summary>
        /// Returns a single value executing a SELECT expr FROM table WHERE condition. If no row is found, NULL is returned 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter">MetaExpression or string</param>
        /// <param name="expr"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        Task<object> ReadValue(string table, object filter=null, string expr="*", string orderBy = null);
    
      
        /// <summary>
        /// Returns a value executing a generic sql command (ex DO_SYSCMD)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ErrMsg">eventual error message</param>
        /// <returns></returns>
        Task<object> ExecuteScalar(string sql, int timeout=-1);

        /// <summary>
        /// Reads all values from a generic sql command and returns the last value read 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ErrMsg"></param>
        /// <returns></returns>
        Task<object> ExecuteScalarLastResult(string sql,  int timeout=-1);



        /// <summary>
        /// Async execute a sql statement to retrieve a table
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<DataTable> ExecuteQuery(string sql, int timeout = -1);

      

        #region build SQL statements

        string GetSelectCommand(string table, string columns = "*", object filter = null, string orderBy = null, string top = null);


        /// <summary>
        /// Return something like SELECT {columns} from {table1.TableName} JOIN {table2} ON  {joinFilter} [WHERE {whereFilter}]
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>        
        string GetJoinSql(DataTable table1, string table2, q filterTable1, q filterTable2, string[] joinColumnsTable1, string[] joinColumnsTable2 = null);

        /// <summary>
        /// Builds a sql DELETE command 
        /// </summary>
        /// <param name="table">table implied</param>
        /// <param name="condition">condition for the deletion, can be a string or a MetaExpression</param>
        /// <returns></returns>
        string GetDeleteCommand(string table, object condition);

       

        /// <summary>
        /// Builds a sql INSERT command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to insert</param>
        /// <param name="len">number of columns</param>
        /// <returns></returns>
        string GetInsertCommand(string table, List<string> columns, List<object> values);

        /// <summary>
        /// Builds a sql INSERT command
        /// </summary>
        /// <param name="R"></param>
        string GetInsertCommand(DataRow R);

        string GetUpdateCommand(DataRow R, object optimisticFilter);

        string GetDeleteCommand(DataRow R, object optimisticFilter);

        /// <summary>
        /// Builds an UPDATE sql command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter">can be a string or a MetaExpression</param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to be set</param>
        /// <param name="ncol">number of columns to update</param>
        /// <returns>Error msg or null if OK</returns>
        string GetUpdateCommand(string table, object filter, Dictionary<string, object> fieldValues);

        #endregion

        #region execute basic commands
        /// <summary>
        /// Executes an INSERT command using current tranactin
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to insert</param>
        /// <param name="len">number of columns</param>
        /// <returns>Error message or null if OK</returns>
        Task DoInsert(string table, List<string> columns, List<object> values);

       
        /// <summary>
        /// Executes an UPDATE command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="condition">where condition to apply</param>
        /// <param name="fieldValues">Values to set</param>
        /// <returns>Number of affected rows</returns>
        Task<int> DoUpdate(string table, object filter, Dictionary< string,object> fieldValues);

        /// <summary>
        /// Executes a delete command using current transaction
        /// </summary>
        /// <param name="table"></param>
        /// <param name="condition"></param>
        /// <returns>N. of rows affected</returns>
        Task<int> DoDelete(string table, object condition);

        #endregion


        /// <summary>
        /// Creates a dictionary field =&gt; value from a DataRow and a list of fields
        /// </summary>
        /// <param name=""></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Dictionary<string, object> GetDictFrom(DataRow r, params string[] fields);

         /// <summary>
        /// Creates a dictionary field =&gt; value from a DataRow and a list of fields
        /// </summary>
        /// <param name=""></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Dictionary<string, object> GetDictFrom(List<string> fields, List<object>values);

       
        /// <summary>
        /// Execute a sql cmd that returns a dataset (eventually with more than one table in it) (ex sqlRunnerDataSet)
        /// </summary>
        /// <param name="sql">sql command to run</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMess">null if ok, Error message otherwise</param>
        /// <returns></returns>
        Task<DataSet>DataSetBySql(string sql, int timeout);


        #region calling stored procedures 

        /// <summary>
        /// Calls a stored procedure with specified parameters
        /// </summary>
        /// <param name="sp_name">stored proc. name</param>
        /// <param name="parameters">Parameter list</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMess">null if ok, Error message otherwise</param>
        /// <returns></returns>
        Task<DataSet> CallSP(string sp_name, object[] parameters,  int timeout =-1);

        /// <summary>
        /// Calls a void stored procedure, return error or null if ok (ex CALLSPPARAMETER)
        /// </summary>
        /// <param name="sp_name">name of stored procedure to call</param>
        /// <param name="ParamName">parameter names to give to the stored procedure</param>
        /// <param name="ParamType">parameter types to give to the stored procedure</param>
        /// <param name="ParamTypeLength">Length of parameters</param>
        /// <param name="ParamDirection">Type of parameters</param>
        /// <param name="ParamValues">Value for parameters</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMsg">null if ok, Error message otherwise</param>
        /// <returns>true if ok</returns>
        Task CallVoidSPParams(string sp_name, DbParameter[] parameters, int timeout=-1);

        /// <summary>
        /// Calls a stored procedure and returns a DataSet. First table can be retrieved in result.Tables[0]
        /// (ex CallSPParameterDataSet)
        /// </summary>
        /// <param name="procname">name of stored procedure to call</param>
        /// <param name="ParamName">parameter names to give to the stored procedure</param>
        /// <param name="ParamType">parameter types to give to the stored procedure</param>
        /// <param name="ParamTypeLength">Length of parameters</param>
        /// <param name="ParamDirection">Type of parameters</param>
        /// <param name="ParamValues">Value for parameters</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMsg">null if ok, Error message otherwise</param>
        /// <returns></returns>
        Task<DataSet> CallSPParams(string sp_name, DbParameter[] parameters, int timeout=-1);



        #endregion


        /// <summary>
        /// Executes something like  SELECT {colTable1} from {table1.TableName} JOIN {table2} ON  {joinFilter} [WHERE {whereFilter}]
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        Task ReadTableJoined(DataTable table1, string table2,
            MetaExpression filterTable1, MetaExpression filterTable2,
            params string[] columns);




        #region get data into existing datasets

        /// <summary>
        /// Runs a sql command that return a DataTable (ex SQL_RUNNER)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMsg">Error message or null when no errors</param>
        /// <returns></returns>
        Task ExecuteQueryIntoTable(DataTable table, string sql, int timeout = -1);

        /// <summary>
        /// Reads data into a given table, skipping temporary columns (ex RUN_SELECT_INTO_TABLE)
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        Task SelectIntoTable(DataTable T, object filter=null, string top=null, string orderBy=null, int timeout=-1);

        /// <summary>
        /// Executes sql into a dataset, ex sqlRunnerintoDataSet
        /// </summary>
        /// <param name="d"></param>
        /// <param name="sql"></param>
        /// <param name="timeout"></param>
        /// <returns>error string or null if ok</returns>
        Task SqlIntoDataSet(DataSet d, string sql, int timeout);

        #endregion


        #region Read into dictionaries and rowobjects
        /// <summary>
        /// Get a list of "objects" from a table using  a specified query, every row read is encapsulated in a dictionary. The list of
        ///  such dictionaries is returned.  (ex readObjectArray)
        /// </summary>
        /// <param name="sql">sql command to run</param>
        /// <param name="timeout"></param>
        /// <param name="ErrMsg"></param>
        /// <returns></returns>
        Task<Dictionary<string, object>[]> ReadDictionaries(string sql,  int timeout=-1);

        /// <summary>
        /// Creates a dictionary from a query on a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="tablename"></param>
        /// <param name="keyField"></param>
        /// <param name="valueField"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Dictionary<T, S>> ReadDictionary<T, S>(string tablename,            
            string keyField, string valueField,
            object filter=null
        );

        

        /// <summary>
        ///  Read a set of fields from a table  and return a dictionary fieldName -&gt; value
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter">can be a string or a MetaExpression, null means read all rows</param>
        /// <param name="expr"></param>
        /// <returns></returns>
        Task<Dictionary<string, object>> ReadObject(string table, object filter=null, string expr="*");


        /// <summary>
        /// Creates a dictionary key => rowObject from a query on a table, only works with table having a single field as primary key
        ///  or if keyfield is distinct in the filtered rows 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tablename"></param>
        /// <param name="keyField"></param>
        /// <param name="fieldList"></param>
        /// <param name="filter">can be  a string or a MetaExpression</param>
        /// <returns></returns>
        Task<Dictionary<T, RowObject>> SelectRowObjectDictionary<T>(string tablename,            
            string keyField, string fieldList,
            object filter=null
        );


       

        /// <summary>
        /// Selects a set of rowobjects from db
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tables">logical table names to be assigned to dictionary keys, each corresponding to a list of RowObject</param>
        /// <returns>a Dictionary where table names are the key and List of RowObjects the values</returns>
        Task<Dictionary<string, List<RowObject>>> RowObjectsBySql(string cmd, params string[] tables);

        /// <summary>
        /// Selects a set of rowobjects from db
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist"></param>
        /// <param name="filter">can be a string or MetaExpression</param>
        /// <param name="order_by"></param>
        /// <param name="TOP"></param>
        /// <returns></returns>
        Task<List<RowObject>> SelectRowObjects(string tablename,
            string columnlist="*",
            object filter=null,
            string order_by = null,
            string TOP = null);


        #endregion




        /// <summary>
        /// Reads a DataTable from a db table. The table is created at run-time using  information
        ///  stored in columntypes
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist">list of field names separated by commas</param>
        /// <param name="order_by">list of field names separated by commas</param>
        /// <param name="filter">condition to apply, can be a string or a MetaExpression</param>
        /// <param name="TOP">how many rows to get</param>
        /// <param name="tableModel">when given, it is used as a model to create the table to read. It is not modified. </param>
        /// <returns>DataTable read</returns>
        Task<DataTable> Select(string tablename,
            object filter = null,
            string columnlist = "*",
            string order_by = null,
            string top = null,
            DataTable tableModel = null,
            int timeout = -1);



        /// <summary>
        /// Executes a List of Select, returning data in the tables specified by each select.  (ex MULTI_RUN_SELECT)
        /// </summary>
        /// <param name="SelList"></param>
        Task MultipleSelect(List<SelectBuilder> SelList);

        /// <summary>
        /// Reads a table without reading the schema. Result table has no primary key set.
        /// This is quicker than a normal select but slower than a RowObject_select
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist"></param>
        /// <param name="filter">string or MetaExpression</param>
        /// <param name="orderBy"></param>
        /// <param name="TOP"></param>
        /// <returns></returns>
        Task<DataTable> ReadTableNoKey(string tablename, object filter=null, string columnlist="*", string orderBy = null, string TOP = null);

        /// <summary>
        /// Executes a SELECT COUNT on a table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="filter">string or MetaExpression</param>
        /// <returns></returns>
        Task<int> Count(string tablename, MetaExpression filter=null);


        /// <summary>
        /// Returns the queryhelper attached to this kind of DataAccess
        /// </summary>
        /// <returns></returns>
        QueryHelper GetQueryHelper();

        /// <summary>
        /// Compile a filter substituting environment variables, that can be a string or a MetaExpression
        /// </summary>
        /// <param name="filter">string or MetaExpression</param>
        /// <returns></returns>
        string Compile(object filter, DataTable T=null);
    }
}