using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using q = mdl.MetaExpression;
using LM = mdl_language.LanguageManager;
using mdl_utils;
using System.Data.Common;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {




    /// <summary>
    /// Information about connection
    /// </summary>
    /// <remarks>
    /// This class exists for historical reasons. 
    /// Actually it only stores some data about the work session. It is an
    ///  envelope for future addition to user-session information.
    /// </remarks>
    public class DataAccess :MarshalByRefObject, IDisposable, IDataAccess {
        //		long reading;
        //		long compiling;
        //		long preparing;

        /// <summary>
        /// Unused
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService() {
            return null;
        }

        private ISecurity _security;

        /// <summary>
        /// Actual name of the connected user, used for optimistic locking when writing timestamps
        /// </summary>
        string User { get; set; } = "user";


        /// <summary>
        /// 
        /// </summary>
        public virtual ISecurity Security {
            get {
                if (_security == null) {
                    _security = CreateSecurity(User);
                }
                return _security;
            }
            set { _security = value; }
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public virtual void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed = false;


        /// <summary>
        ///  Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                Destroy().Wait();

            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }


        /// <summary>
        /// Query helper for DB query
        /// </summary>
        protected QueryHelper QHS;

        /// <summary>
        /// Query helper for dataset query
        /// </summary>
        protected CQueryHelper QHC = MetaFactory.factory.getSingleton<CQueryHelper>();


        private IDbDriver privateConnection = null;


        IDataAccess mainConnection = null;

        /// <summary>
        /// Sql connection used for physical connection. Should not be generally used from application classes
        /// </summary>
        public virtual IDbDriver Driver {
            get {
                if (mainConnection != null)
                    return mainConnection.Driver;
                return privateConnection;
            }
            set {
                privateConnection = value;
            }
        }


        //public IDBDriver driver { get; private set; }



        /// <summary>
        /// Start a "post" process, this doesnt mean to be called by applications
        /// </summary>
        /// <param name="mainConn"></param>
	    public virtual void startPosting(IDataAccess mainConn) {
            if (mainConn == this) return;
            if (mainConn.Descriptor != this.descriptor) throw new Exception("Can't create a transaction on two different databases");
            this.mainConnection = mainConn;
        }

        /// <summary>
        /// Ends a "post" process , this doesnt mean to be called by applications
        /// </summary>
        public void stopPosting() {
            this.mainConnection = null;
        }







        /// <summary>
        /// Closes the connection without throwing exceptions
        /// </summary>
        protected async Task sureClosing() {
            if (Driver == null) return;
            try {
                if (Driver.State == ConnectionState.Open) await Driver.Close();
                //nesting=0;
            } 
            catch (Exception E) {
                MarkException("SureClosing: Error Disconnecting from DB", E);
            }
        }


        //new byte[]{75,12,0,215,   93,89,45,11,   171,96,4,64,  13,158,36,190};
        //private static byte [] GetArr11(){
        //    byte []arr=	new byte[]{75,12,0,215+23,   93,89-19,45,11,   171,96+68,4,64,  13+8,158,36,190};
        //    arr[3]-= 23;
        //    arr[5]+=19;
        //    arr[9]-=68;
        //    arr[12]-=8;
        //    return arr;
        //}


        /// <summary>
        /// When true (default), connection is Opened at first and Closed at end of program
        /// When false, connection is Opened/Closed at every db access
        /// </summary>
        bool _myPersisting = true;


        /// <summary>
        /// Return true if Connection is using Persisting connections mode, i.e.
        ///  it is Open at the beginning aand Closed at the end
        /// </summary>
        public virtual bool Persisting {
            get {
                return _myPersisting;
            }
            set {
                if (_myPersisting == value) return;
                if (Driver == null) return;
                if (_myPersisting) {
                    //Was Persisting, must become not-Persisting
                    if (nesting == 0) {
                        sureClosing().Wait(); // .GetAwaiter().GetResult();
                    }
                } else {
                    //Was not Persisting, must become persisting
                    if (nesting == 0)
                        Driver.Open().Wait();
                }
                _myPersisting = value;
            }
        }


        string myLastError;

        /// <summary>
        /// Returns last error and resets it.
        /// </summary>
        public virtual string LastError {
            get {
                if (mainConnection != null) return mainConnection.LastError;
                string S = myLastError;
                myLastError = "";
                return S;
            }
            

        }


        /// <summary>
        /// Get last error without clearing it
        /// </summary>
        /// <returns></returns>
		public virtual string SecureGetLastError() {
            if (mainConnection != null) return mainConnection.SecureGetLastError();
            return myLastError;
        }


       

        int nesting;

        /// <summary>
        /// True if SSPI is used, False if SQL Security is used
        /// </summary>
        public bool SSPI;



        /// <summary>
        /// True if Opening problems encountered  
        /// </summary>
        public bool BrokenConnection { get; set; }=false;

       
        /// <summary>
        /// If true, customobject and column types are used to describe table structure, 
        ///  when false, those are always obtained from DB
        /// </summary>
        public virtual bool UseCustomObject { get; set; } = true;

        
        private IDbDescriptor descriptor;

        public virtual IDbDescriptor Descriptor { get { return descriptor;} }
        private IDbDriverDispatcher dispatcher { get { return Descriptor?.Dispatcher; } }

        #region Constructors (with or without SSPI)

        /// <summary>
        /// Constructor for WEB 
        /// </summary>
        /// <param name="descriptor">database descriptor</param>
        public DataAccess(
                IDbDescriptor descriptor                
            ) {
			this.descriptor = descriptor ?? throw new ArgumentNullException("descriptor must be specified");

            try {
                Driver = dispatcher.GetConnection();
            }
            catch (Exception E) {
                myLastError = ErrorLogger.GetErrorString(E);
                BrokenConnection = true;
                return;
            }
            
            //QHC = MetaFactory.factory.getSingleton<CQueryHelper>();
            QHS = Driver.QH;   
         
        }

        /// <summary>
        /// Called when a Security class is needed
        /// </summary>
        /// <returns></returns>
        public virtual ISecurity CreateSecurity(string user) {
            return new DefaultSecurity(user);
        }


        public virtual IMetaModel model { get; set; }= MetaFactory.factory.getSingleton<IMetaModel>();

      

        #endregion

        /// <summary>
        /// When true, access to the table are prefixed with DBO.  By the default, they are.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public virtual bool IsCommonSchemaTable(string tablename) {
            return true;
        }

        /// <summary>
        /// When true, access to the table are prefixed with DBO. 
        /// </summary>
        /// <param name="procname"></param>
        /// <returns></returns>
		public virtual bool IsCommonSchemaProcedure(string procname) {
            return true;
        }



        /// <summary>
        /// Use another database with this connection
        /// </summary>
        /// <param name="DBName"></param>
        public virtual async Task  ChangeDatabase(string dbName) {
            await Driver.Open();
            await Driver.ChangeDatabase(dbName);
            await Driver.Close();
        }


        /// <summary>
        /// Updates last read access stamp to db 
        /// </summary>
		public virtual void SetLastRead() {
            Security.SetSys("DataAccessLastRead", DateTime.Now);
        }

        /// <summary>
        /// Updates last write access stamp to db 
        /// </summary>
        public virtual void SetLastWrite() {
	        Security.SetSys("DataAccessLastWrite", DateTime.Now);
        }

        /// <summary>
        /// Crea un duplicato di un DataAccess, con una nuova connessione allo stesso DB. 
        /// Utile se la connessione deve essere usata in un nuovo thread.
        /// </summary>
        /// <returns></returns>
        public virtual DataAccess Clone() {
			DataAccess C = new DataAccess(descriptor) {
				Security = Security
			};
			return C;
        }


        /// <summary>
        /// Release resources, should not be called during a coordinated transaction
        /// </summary>
        public async Task Destroy() {
            if (privateConnection == null) return;

            if (privateConnection.HasValidTransaction()) {
                await Driver.Rollback();
                LogError("Rollback invoked in destroy", null);
            }
           
            if (privateConnection.State == ConnectionState.Open) {
                nesting = 0;
                Persisting = false;
                await sureClosing();
            }

            privateConnection.Dispose();
            privateConnection = null;
        }
       

        #region Open/Close connection

        /// <summary>
        /// Open the connection (or increment nesting if already Open)
        /// </summary>
        /// <returns> true when successfull </returns>
        public virtual async Task<bool> Open() {
            if (mainConnection != null) return await mainConnection.Open();

            if (BrokenConnection) return false;
            if (Driver == null) return false;

            if (Driver.State == ConnectionState.Open && Persisting) {
                if (await assureOpen()) {
                    nesting++;
                    BrokenConnection = false;
                    return true;
                }
                BrokenConnection = true;
                return false;
            }

            //not Persisting
            if ((nesting == 0) || (Driver.State == ConnectionState.Broken) ||
                (Driver.State == ConnectionState.Closed)) { //Open only if is not Open
                try {
                    if (Driver.State == ConnectionState.Broken) {
                        await Driver.Close();
                        //PreparedCommands = new Hashtable();
                        await Driver.Open();
                    }

                    if (Driver.State == ConnectionState.Closed) {
                        //PreparedCommands = new Hashtable();
                        await Driver.Open();
                    }

                } catch (Exception E) {
                    //myLastError= E.Message;
                    MarkException("Open: Error connecting to DB", E);
                    BrokenConnection = true;
                    return false;
                }
            }
            nesting++;
            BrokenConnection = false;
            
            return true;
        }


        /// <summary>
        /// Close the connection
        /// </summary>
        public virtual async Task Close() {
            if (mainConnection != null) {
                await mainConnection.Close();
                return;
            }
            if (BrokenConnection) {
                return;
			}
            if (Persisting) {
                if (nesting > 0) nesting--;
                //never Closes
                return;
            }
            //not Persisting

            if (nesting == 0) return;   //should not happen

            if (nesting == 1) {
                nesting = 0;
                await sureClosing();
                return;
            }
            nesting--;
            return;
        }

        DateTime nextCheck = DateTime.Now;


        async Task<bool> assureOpen() {
            if (NTRANS > 0) return true;
            if (nesting > 0) return true;
            if (DateTime.Now < nextCheck) return true;
            if (await checkStillOpen()) {
                nextCheck = DateTime.Now.AddMinutes(3);
                return true;
            }
            return await tryToOpen();
        }

        async Task<bool> checkStillOpen() {

            if (Driver.State == ConnectionState.Closed) return false;
            if (Driver.State == ConnectionState.Broken) return false;

            //SqlCommand Cmd = new SqlCommand("select getdate()", _sqlConnection, null) {
            //    CommandTimeout = 100
            //};
            DbDataReader Read = null;
            IDbCommand cmd=null;
            try {
                //Read = await Cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
                cmd = Driver.GetDbCommand(Driver.DummyCommand, timeout: 100);
                Read = await Driver.ExecuteReaderAsync(
                    command: cmd,
                    behavior:CommandBehavior.SingleRow);

                object Result = null;
                if (Read.HasRows) {
                    await Read.ReadAsync();
                    Result = Read[0];
                    Read.Close();
                }
            } catch (Exception E) {
                if (Read != null && !Read.IsClosed) Read.Close();
                MarkException("checkStillOpen: connessione assente", E);
                return false;
            }
			finally {
                if (Read != null) Read.Close();
                await Driver.DisposeCommand(cmd);
			}
            return true;
        }

        async Task<bool> tryToOpen() {
            try {
                if (Driver.State == ConnectionState.Broken
                    ) {
                    await Driver.Close();
                    //PreparedCommands = new Hashtable();
                    await Driver.Open();
                }

                if (Driver.State == ConnectionState.Closed) {
                    //PreparedCommands = new Hashtable();
                    await Driver.Open();
                }
                return true;

            } catch (Exception E) {
                //myLastError= E.Message;
                MarkException("tryToOpen: Error connecting to DB", E);
                BrokenConnection = true;
                return false;
            }
        }
        #endregion

        ///// <summary>
        ///// Reads all data from MetaData-System Tables into a new DBstructure
        ///// </summary>
        ///// <param name="filter">string or MetaExpression</param>
        ///// <returns></returns>
        //public virtual async Task<dbstructure> GetEntireStructure(object filter) { //MUST BECOME PROTECTED

        //    var DS = new dbstructure();
        //    foreach (DataTable T in DS.Tables) {
        //        await AddExtendedProperties(T);
        //        await SelectIntoTable(T, filter: filter);
        //    }

        //    return DS;
        //}




        /// <summary>
        /// Reads or evaluates all the tables/view (may require a bit)
        /// </summary>
        public virtual async Task ReadStructures() {
            var Tables = await Driver.TableListFromDB();
            foreach (string tablename in Tables) {
                //Application.DoEvents();
                dbstructure DBS = await Descriptor.GetStructure(tablename, this);
                if (DBS.customobject.Rows.Count == 0) {
                    DataRow newobj = DBS.customobject.NewRow();
                    newobj["objectname"] = tablename;
                    newobj["isreal"] = "S";
                    DBS.customobject.Rows.Add(newobj);
                }
                await Driver.EvaluateColumnTypes(DBS.columntypes, tablename);
            }
            var Views = await Driver.ViewListFromDB();
            foreach (string tablename in Views) {
                //Application.DoEvents();
                dbstructure DBS = await Descriptor.GetStructure(tablename, this);
                if (DBS.customobject.Rows.Count == 0) {
                    DataRow newobj = DBS.customobject.NewRow();
                    newobj["objectname"] = tablename;
                    newobj["isreal"] = "N";
                    DBS.customobject.Rows.Add(newobj);
                }
                await Driver.EvaluateColumnTypes(DBS.columntypes, tablename);
            }
        }


        /// <summary>
        /// Evaluate columntypes and customobject analizing db table properties
        /// </summary>
        /// <param name="tablename"></param>
        public virtual async Task DetectStructure(string tableName) {
            await Descriptor.DetectStructure(tableName, this);
        }


        /// <summary>
        /// Reads table structure of a list of tables (Has only effect if UseCustomObject is true)
        /// </summary>
        /// <param name="tableName"></param>
        public virtual async Task ReadStructures(params string[] tableNames) {

            if (tableNames.Length == 0) { 
                return; 
            }
            int handle = MetaProfiler.StartTimer("ReadStructures()");

            if (!await Open()) {
                MetaProfiler.StopTimer(handle);
                return;
            }

            try {
                await descriptor.ReadStructures(this, tableNames);
            }
            catch (Exception e) {
                MarkException("ReadStructures", e);
            }
            await Close();

            MetaProfiler.StopTimer(handle);

        }

        /// <summary>
		/// Read a bunch of table structures, all those present in the DataSet
		/// </summary>
		/// <param name="D"></param>
		/// <param name="primarytable"></param>
		public virtual async Task ReadStructures(DataSet d) {

            var tabNames = (from DataTable t in d.Tables select t.TableName).ToArray();
          
            //foreach (DataTable T in D.Tables) {
            //    tabNames.Add(T.TableName);
            //    //if (model.IsCached(T) ||model.IsSubEntity(child: T, parent: D.Tables[primarytable])) {
            //    //    tabNames.Add(T.TableName);
            //    //}
            //}            
            await ReadStructures(tabNames);
        }

        /// <summary>
        /// Only copy columns without any costraints, key or other ilarious things
        /// </summary>
        /// <param name="T"></param>
        /// <param name="copykey">if true primary key is copied</param>
        /// <returns></returns>
        public static DataTable SimplifiedTableClone(DataTable T, bool copykey = false) {
            DataTable T2 = new DataTable(T.TableName) {
                Namespace = T.Namespace
            };
            for (int i = 0; i < T.Columns.Count; i++) {
                DataColumn C = T.Columns[i];
                DataColumn C2 = new DataColumn(C.ColumnName, C.DataType, C.Expression) {
                    AllowDBNull = true
                };
                T2.Columns.Add(C2);

            }
            if (copykey) {
                DataColumn[] key = T.PrimaryKey;
                DataColumn[] key2 = new DataColumn[key.Length];
                for (int i = 0; i < key.Length; i++) {
                    key2[i] = T2.Columns[key[i].ColumnName];
                }
                T2.PrimaryKey = key2;
            }
            return T2;
        }

        /// <summary>
        /// Return something like SELECT {colTable1} from {table1.TableName} JOIN {table2} ON  {joinFilter} [WHERE {whereFilter}]
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public virtual string GetJoinSql(DataTable table1, string table2, q filterTable1, q filterTable2, string[] joinColumnsTable1, string[] joinColumnsTable2=null) {
            string colTable1 = string.Join(",", (from c in table1.Columns._names() where table1.Columns[c].IsReal() select table1.TableName+"."+c  ).ToArray());
            MetaExpression exprJoin = null;
            if(joinColumnsTable2 == null) {
                joinColumnsTable2 = joinColumnsTable1;
            };
            var clauseJoin = q.and(from cc in joinColumnsTable1.Zip(joinColumnsTable2) 
                                    select q.eq(q.field(cc.First,table1.TableName), q.field(cc.Second, table2) ));


            
            string joinFilter = exprJoin.toSql(QHS, this.Security);
            q filter = null;
            if (filterTable1 is null) {
                filterTable1.cascadeSetTable(table1.TableName);
                filter = filterTable1;
            }

            if (!(filterTable2 is  null)) {
                filterTable2.cascadeSetTable(table2);
                if (filter is null) {
                    filter = filterTable2;
                } else {
                    filter &= filterTable2;
                }
            }

            string whereFilter = Compile(filter);
            return Driver.GetJoinSql(table1.TableName, table2, colTable1, whereFilter, joinFilter);
        }











        /// <summary>
        /// Creates a new table basing on columntypes info. Adds also primary key 
        ///  information to the table, and allownull to each field.
        ///  Columnlist must include primary table, or can be "*"
        /// </summary>
        /// <param name="tablename">name of table to create. Can be in the form DBO.tablename or department.tablename</param>
        /// <param name="columns"></param>
        /// <param name="addExtProp">Add db information as extended propery of columns (column length, precision...)</param>
        /// <returns>a table with same types as DB table</returns>
        public virtual async Task<DataTable> CreateTable(string tablename, string columns = "*", bool addExtProp = false) {
            if (tablename.Contains(".")) {
                int N = tablename.LastIndexOf('.');
                tablename = tablename[(N + 1)..];
            }

            if (columns == null) {
                columns = "*";
            } else {
                columns = columns.Trim();
            }

            if (tablename == "customobject" || tablename == "columntypes" || tablename == "customtablestructure") {
                var DBSS = new dbstructure();
                var TT = singleTableClone(DBSS.Tables[tablename], addExtProp);
                if (addExtProp) await AddExtendedProperties(TT);
                return TT;
            }

            //int handle = metaprofiler.StartTimer("Inside CreateTableByName("+tablename+")");
            int handle = MetaProfiler.StartTimer("Inside CreateTableByName()");

            var T = new DataTable(tablename);
            var DBS = await descriptor.GetStructure(tablename, this);
            if (DBS.columntypes.Rows.Count == 0) {
                myLastError = $"No column found in columntypes for table {tablename}";
                MetaProfiler.StopTimer(handle);
                return T;
            }

            if (columns == "*") {
                foreach (var Col in DBS.columntypes.Select(null, "iskey desc, field asc")) {
                    var C = new DataColumn(Col["field"].ToString()) {
                        AllowDBNull = Col["allownull"].ToString() != "N",
                        DataType = GetType_Util.GetSystemType_From_SqlDbType(Col["sqltype"].ToString())
                    };
                    if (Col["sqldeclaration"].ToString() == "text") {
                        C.ExtendedProperties["sqldeclaration"] = "text";
                    }
                    T.Columns.Add(C);
                }
            }
            else {
                string[] ColNames = columns.Split(new char[] { ',' });
                foreach (string ColName in ColNames) {
                    var Cols = DBS.columntypes.Select(QHC.CmpEq("field", ColName.Trim()));
                    if (Cols.Length == 0)
                        continue;
                    var Col = Cols[0];
                    var C = new DataColumn(Col["field"].ToString()) {
                        AllowDBNull = Col["allownull"].ToString() != "N",
                        DataType = GetType_Util.GetSystemType_From_SqlDbType(Col["sqltype"].ToString())
                    };
                    if (Col["sqldeclaration"].ToString() == "text") {
                        C.ExtendedProperties["sqldeclaration"] = "text";
                    }
                    T.Columns.Add(C);
                }
            } 

            //Add primary key to table
            DataRow[] keycols = DBS.columntypes.Select(QHC.CmpEq("iskey","S"));
            DataColumn[] Key = new DataColumn[keycols.Length];
            for (int i = 0; i < keycols.Length; i++) {
                Key[i] = T.Columns[keycols[i]["field"].ToString()];
            }
            if (Key.Length > 0) T.PrimaryKey = Key;
            await GetViewStructureExtProperties(T);
            MetaProfiler.StopTimer(handle);

            if (addExtProp) await AddExtendedProperties(T);

            return T;
        }

        /// <summary>
        /// Reads extended informations for a table related to a view,
        ///  in order to use it for posting. Reads data from viewcolumn.
        ///  Sets table and columnfor posting and also 
        ///  sets ViewExpression as tablename.columnname (for each field)
        /// </summary>
        /// <param name="T"></param>
        public virtual async Task GetViewStructureExtProperties(DataTable T) {
            if (T.tableForPosting() != T.TableName) return;
            int handle = MetaProfiler.StartTimer("GetViewStructureExtProperties(" + T.TableName + ")");
            dbstructure DBS = await descriptor.GetStructure(T.TableName,this);
            if (DBS.customobject.Rows.Count == 0) {
                MetaProfiler.StopTimer(handle);
                return;
            }
            DataRow CurrObj = DBS.customobject.Rows[0];
            if (CurrObj["isreal"].ToString().ToLower() != "n") {
                MetaProfiler.StopTimer(handle);
                return;
            }
            string primarytable = CurrObj["realtable"].ToString();
            if (primarytable == "") {
                MetaProfiler.StopTimer(handle);
                return;
            }
            T.setTableForPosting(primarytable);

            Hashtable Read = (Hashtable)DBS.viewcolumn.ExtendedProperties["AlreadyRead"];
            if (Read == null) {
                Read = new Hashtable();
                DBS.viewcolumn.ExtendedProperties["AlreadyRead"] = Read;
            }

            if (Read["1"] == null) {
                await SelectIntoTable(DBS.viewcolumn, filter: QHS.CmpEq("objectname", T.TableName));
                Read["1"] = "1";
            }

            //as default, no column is to post
            foreach (DataColumn C in T.Columns) {
                C.SetColumnNameForPosting("");
            }
            foreach (DataRow curCol in DBS.viewcolumn.Rows) {
                DataColumn Col = T.Columns[curCol["colname"].ToString()];
                if (Col == null)
                    continue;
                string postingcol;
                string viewexpr;
                if (curCol["realtable"].ToString() == primarytable) {
                    postingcol = curCol["realcolumn"].ToString();
                    viewexpr = curCol["realcolumn"].ToString();
                }
                else {
                    postingcol = ""; //correctly, instead of null which would mean realcolumn
                    viewexpr = curCol["realtable"].ToString() + "." + curCol["realcolumn"].ToString();
                }
                Col.SetColumnNameForPosting(postingcol);
                Col.SetViewExpression(viewexpr);
            }
            MetaProfiler.StopTimer(handle);

        }


        /// <summary>
        /// Adds all extended information to table T reading it from columntypes.
        /// Every Row of columntypes is assigned to the corresponding extended 
        ///  properties of a DataColumn of T. Each Column of the Row is assigned
        ///  to an extended property with the same name of the Column
        ///  Es. R["a"] is assigned to Col.ExtendedProperty["a"]
        /// </summary>
        /// <param name="T"></param>
        public virtual async Task AddExtendedProperties(DataTable T) {
            int handle = MetaProfiler.StartTimer($"AddExtendedProperty*{T.TableName}");

            var dbs = await descriptor.GetStructure(T.tableForReading(),this);
            foreach (var col in dbs.columntypes.Select()) {
                string field = col["field"].ToString();
                if (!T.Columns.Contains(field)) continue;
                var c = T.Columns[col["field"].ToString()];
                foreach (DataColumn columnProperty in dbs.columntypes.Columns) {
                    c.ExtendedProperties[columnProperty.ColumnName] =
                        col[columnProperty].ToString();
                }
            }
            MetaProfiler.StopTimer(handle);

        }



        /// <summary>
        /// Adds extended properties on the columns of a table
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="T"></param>
        public static async Task addExtendedProperty(IDataAccess Conn, DataTable T) {
            int handle = MetaProfiler.StartTimer("AddExtendedProperty*"+T.TableName);
            var COLTYPES = await Conn.Select(tablename:"columntypes", columnlist: "*",  filter: q.eq("tablename",T.TableName));

            foreach (DataRow Col in COLTYPES.Select()) {
                var C = T.Columns[Col["field"].ToString()];
                if (C == null) continue;
                foreach (DataColumn ColumnProperty in COLTYPES.Columns) {
                    C.ExtendedProperties[ColumnProperty.ColumnName] =
                        Col[ColumnProperty].ToString();
                }
            }
            MetaProfiler.StopTimer(handle);

        }


        /// <summary>
        /// Returns the primary table of a given DBstructure. It is the objectname of
        ///  the only row contained in DBS.customobject
        /// </summary>
        /// <param name="DBS"></param>
        /// <returns></returns>
        public static string PrimaryTableOf(dbstructure DBS) {
            return DBS.customobject.Rows[0]["objectname"].ToString();
        }



        /// <summary>
        /// Empty table structure information about a listing type of a table
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="listtype"></param>
        public virtual async Task ResetListType(string tablename, string listtype) {
            dbstructure DBS = await descriptor.GetStructure(tablename,this);
            Hashtable Read = (Hashtable)DBS.customview.ExtendedProperties["AlreadyRead"];
            if (Read == null) return;
            Read[listtype] = null;

        }

        /// <summary>
        /// Empty table structure information about any listing type of a table
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="listtype"></param>
        public virtual async Task ResetAllListType(string tablename, string listtype) {
            dbstructure DBS = await descriptor.GetStructure(tablename,this);
            DBS.customview.ExtendedProperties["AlreadyRead"] = null;
        }

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
        public virtual async Task<(string listType, dbstructure dbs)> GetListType(string tablename, string listtype) {
            //int handle = metaprofiler.StartTimer("GetListType("+tablename+")");
            int handle = MetaProfiler.StartTimer("GetListType*"+tablename);
            var DBS = await descriptor.GetStructure(tablename,this);
            if (listtype == null)  return (listtype,DBS);
            string DBfilter = QHS.MCmp(new {objectname = tablename, viewname = listtype});
            string CFilter = QHC.MCmp(new {objectname = tablename, viewname = listtype});
            //"(objectname=" + QueryCreator.quotedstrvalue(tablename, true) +")AND(viewname=" + QueryCreator.quotedstrvalue(listtype, true) + ")";

            Hashtable Read = (Hashtable)DBS.customview.ExtendedProperties["AlreadyRead"];
            if (Read == null) {
                Read = new Hashtable();
                DBS.customview.ExtendedProperties["AlreadyRead"] = Read;
            }
            if (Read[listtype] != null) {
                MetaProfiler.StopTimer(handle);
                return (listtype,DBS);
            }

            if (DBS.customview.Select(CFilter).Length == 0) {
                if (descriptor.IsToRead(DBS, "customview")) await SelectIntoTable(DBS.customview, filter: DBfilter);
                if (descriptor.IsToRead(DBS, "customredirect")) await SelectIntoTable(DBS.customredirect, filter: DBfilter);
            }
            DataRow[] found = DBS.customredirect.Select(CFilter);
            if (found.Length > 0) {
                var R = found[0];
                string viewtable = R["objecttarget"].ToString();
                var (_, dbs) = await GetListType( viewtable, R["viewtarget"].ToString());
                MetaProfiler.StopTimer(handle);
                return (R["viewtarget"].ToString(), dbs);
            }

            foreach (DataRow R in DBS.customviewwhere.Select(CFilter)) {
                R.Delete();
                R.AcceptChanges();
            }
            foreach (DataRow R in DBS.customvieworderby.Select(CFilter)) {
                R.Delete();
                R.AcceptChanges();
            }
            foreach (DataRow R in DBS.customviewcolumn.Select(CFilter)) {
                R.Delete();
                R.AcceptChanges();
            }

            if (descriptor.IsToRead(DBS, "customviewcolumn")) await SelectIntoTable(DBS.customviewcolumn, filter: DBfilter);
            if (descriptor.IsToRead(DBS, "customvieworderby")) await SelectIntoTable(DBS.customvieworderby, filter: DBfilter);
            if (descriptor.IsToRead(DBS, "customviewwhere")) await SelectIntoTable(DBS.customviewwhere, filter: DBfilter);

            DataRow[] List = DBS.customview.Select(DBfilter);
            if (List.Length == 0)
                Read[listtype] = "1";
            if ((List.Length > 0) && (List[0]["issystem"].ToString().ToUpper() == "S")) {
                Read[listtype] = "1";
            }
            MetaProfiler.StopTimer(handle);
            return (listtype,DBS);
        }

        /// <summary>
        /// Get information about an edit type. Reads from customedit 
        /// </summary>
        /// <param name="objectname"></param>
        /// <param name="edittype"></param>
        /// <returns>CustomEdit DataRow about an edit-type</returns>
        public virtual async Task<DataRow> GetFormInfo(string objectname, string edittype) {
            dbstructure DBS = await descriptor.GetStructure(objectname,this);
            string filter = QHS.MCmp(new { objectname, edittype }); //"(objectname='" + objectname + "')AND(edittype='" + edittype + "')";
            string dtfilter = QHC.MCmp(new {edittype}); //"(edittype='" + edittype + "')"};  {edittype}  is same as {edittype=edittype}
            DataRow[] found = DBS.customedit.Select(dtfilter);
            if (found.Length > 0) return found[0];
            if (descriptor.IsToRead(DBS, "customedit")) await SelectIntoTable(DBS.customedit, filter: filter);
            found = DBS.customedit.Select(dtfilter);
            if (found.Length == 0) return null;
            return found[0];
        }

        /// <summary>
        /// Gets the system type name of a field named fieldname
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        public virtual string GetFieldSystemTypeName(dbstructure DBS, string fieldname) {//MUST BECOME PROTECTED
            DataRow[] found = DBS.columntypes.Select(QHC.CmpEq("field",fieldname));
            if (found.Length == 0) return null;
            return found[0]["systemtype"].ToString();
        }

        /// <summary>
        /// Gets the corresponding system type of a db column named fieldname
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        public virtual Type GetFieldSystemType(dbstructure DBS, string fieldname) {//MUST BECOME PROTECTED
            string name = GetFieldSystemTypeName(DBS, fieldname);
            if (name == null) return null;
            return GetType_Util.GetSystemType_From_StringSystemType(name);
        }


        /// <summary>
        /// Marks an Exception and set Last Error
        /// </summary>
        /// <param name="main">Main description</param>
        /// <param name="E"></param>
        public virtual string MarkException(string main, Exception E) {
            myLastError = errorLogger.formatException(E);
            errorLogger.markException(E, main);
            return myLastError;
        }


        /// <summary>
        /// Class for logging errors
        /// </summary>
        public IErrorLogger errorLogger { get; set; } = ErrorLogger.Logger;

        #region Transaction Management

        int NTRANS = 0;
        bool DoppiaRollBack = false;


        /// <summary>
        /// Gets Current used Transaction
        /// </summary>
        /// <returns>null if no transaction is Open</returns>
        public virtual IDbTransaction CurrentTransaction() {
            return Driver.CurrentTransaction;
        }

        /// <summary>
        /// Starts a new transaction 
        /// </summary>
        /// <param name="L"></param>
        /// <returns>error message, or null if OK</returns>
        public virtual async Task BeginTransaction(IsolationLevel L) {
            if (mainConnection != null) {
                await mainConnection.BeginTransaction(L);
                return;
            }
            //if (sys["Transaction"]!=null){
            //    return "Impossibile accedere alla connessione. C'è già un altra transazione in corso.";
            //}
            if (NTRANS > 0) {
                //NTRANS=NTRANS+1;
                throw new Exception("ERRORE BEGIN TRANSACTION DI TRANSAZIONE ANNIDATA"); //va ad aggiungersi alla transaz. corrente
            }
            
            await ExecuteScalar("set XACT_ABORT ON");
            await Driver.BeginTransaction(L);
            DoppiaRollBack = false;
            NTRANS = 1;
                        

        }
       
        /// <summary>
        /// Commit the transaction
        /// </summary>
        /// <returns>error message, or null if OK</returns>
        public virtual async Task Commit() {
            if (mainConnection != null) {
                await mainConnection.Commit();
                return;
            }

            var Tran = CurrentTransaction();
            if (Tran.Connection == null) {
                throw new Exception( LM.noValidTransaction);
            }

            if (NTRANS == 0) {
                string err = "Commit executed without a corresponding begin transaction";
                errorLogger.logException(err, dataAccess: this);
                throw new Exception(err);
            }
            if (DoppiaRollBack) {
                string err = "Errore, due transazioni annidate, della prima è stato fatto già il rollback";
                errorLogger.logException(err, dataAccess: this);
                await Rollback();
                throw new Exception(err);
            }

            await Driver.Commit();      
            NTRANS = 0;
        }

        /// <summary>
        /// Rollbacks transaction
        /// </summary>
        /// <returns>Error message, or null if OK</returns>
        public virtual async Task Rollback() {
            if (mainConnection != null) {
                await mainConnection.Rollback();
                return;
            }
            if (NTRANS == 0) {
                DoppiaRollBack = true;
                throw new Exception("rollback invoked without a begin transaction");
            }
            if (Driver.CurrentTransaction==null) {
                throw new Exception("rollback invoked without a valid transaction");
			}
           await Driver.Rollback();                
           NTRANS -= 1;

        }

        #endregion

        /// <summary>
        /// True if current transaction  is still alive, i.e. has a connection attached to it
        /// </summary>
        /// <returns></returns>
        public virtual bool HasValidTransaction() {
            return Driver.HasValidTransaction();
        }

        /// <summary>
        /// True if current transaction  is still alive, i.e. has a connection attached to it
        /// </summary>
        /// <returns></returns>
        public virtual bool HasInvalidTransaction() {
            return Driver.HasInvalidTransaction();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public virtual string GetPrefixedTable(string table) {
            if (IsCommonSchemaTable(table)) {
                return Driver.SchemaObject("DBO", table);
            }
            return table;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public virtual string GetPrefixedStoredProcedure(string SpName) {
            if (IsCommonSchemaProcedure(SpName)) {
                return Driver.SchemaObject("DBO", SpName);
            }
            return SpName;
        }

        #region Primitives for DB-interface



        /// <summary>
        /// Read a set of fields from a table  and return a dictionary fieldName -&gt; value
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter"></param>
        /// <param name="expr">list of fields to read</param>
        /// <returns>An object dictionary</returns>
        public virtual async Task<Dictionary<string, object>> ReadObject(string table, object filter = null, string expr = "*") {
            if (!await Open()) return null;
            IDbCommand cmd;
            int NN = 0;
            var res = new Dictionary<string, object>();
            try {
                
                NN = MetaProfiler.StartTimer("ReadObject*" + table);
                var sql= Driver.GetSelectCommand(table: GetPrefixedTable(table),
                        columns: expr,
                        filter: Compile(filter));
                cmd = Driver.GetDbCommand(sql);
              
                var Read = await Driver.ExecuteReaderAsync(command:cmd, behavior:CommandBehavior.SingleRow);
                var fieldNames= new string[Read.FieldCount];
                for (int i = 0; i < Read.FieldCount; i++) {
                    fieldNames[i] = Read.GetName(i);
                }
                try {
                    if (Read.HasRows) {
                        Read.Read();
                        for (int i = 0; i < Read.FieldCount; i++) {
                            res.Add(fieldNames[i], Read[i]);
                        }
                        SetLastRead();
                    } 
                    else {
                        return null;
                    }
                }                
                finally {
                    await Driver.DisposeCommand(cmd);
                    await Read?.CloseAsync();
                }
                return res;
            } 
            finally {
                MetaProfiler.StopTimer(NN);
                if (!BrokenConnection) {
                    await Close();
                }
			}
        }




        /// <summary>
        /// Returns a single value executing a SELECT expr FROM table WHERE condition. If no row is found, NULL is returned 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter">string or MetaExpression</param>
        /// <param name="expr"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public virtual async Task<object> ReadValue(string table, object filter = null, string expr = "*", string orderBy = null) {
            string cmd ;
            if (!await Open()) {
                return null;
            }
            int NN = MetaProfiler.StartTimer("ReadValue*" + table);
            try {                
                cmd = Driver.GetSelectCommand(table: GetPrefixedTable(table), 
                    columns:expr,
                    filter: Compile(filter),
                    orderBy: orderBy);
                
                var Result = await ExecuteScalar(cmd);                
                SetLastRead();
                return Result;
            }        
            finally {               
                await Close();
                MetaProfiler.StopTimer(NN);
            }
        }


        /// <summary>
        /// Returns a value executing a generic sql command  (ex DO_SYS_CMD)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ErrMsg">eventual error message</param>
        /// <returns></returns>
        public virtual async Task <object > ExecuteScalar(string sql, int timeout = -1) {
            await assureValidOpen();

            int NN = MetaProfiler.StartTimer("ExecuteSql()"); ;
            try {
                var Result = await Driver.ExecuteScalar(sql, timeout: timeout);
                SetLastRead();
                assureValidTransaction(sql);
                return Result;
            }           
            finally {
                await Close();
                MetaProfiler.StopTimer(NN);
            }


        }

        /// <summary>
        /// Reads all value from a generic sql command and returns the last value read
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual async Task<object> ExecuteScalarLastResult(string sql,  int timeout = -1) {
	        await assureValidOpen();
            
            int NN = MetaProfiler.StartTimer("ExecuteScalarLastResult()");
            var sbRes = new StringBuilder();
            IDbCommand cmd=null;
            int nRes = 0;
            try {                
                cmd = Driver.GetDbCommand(sql,timeout);
                var Read = await Driver.ExecuteReaderAsync(cmd);
                object Result = null;

                try {
                    while (Read.HasRows) {
                        while (await Read.ReadAsync()) {
                            nRes++;
                            Result = Read[0];
                            sbRes.AppendLine($"{nRes}:{Result}");
                            SetLastRead();
                        }
                        await Read.NextResultAsync();
                    }
                }
                catch (Exception E) {
                    //if (command.Length>80000) command = command.Substring(0,79997)+"...";
                    var errMsg = MarkException($"ExecuteScalarLastResult:{LM.errorRunningCommand(sql)}", E);
                    errorLogger.logException($"ExecuteScalarLastResult:{LM.errorRunningCommand(sql)}", E, dataAccess: this);
                    throw new Exception(errMsg, E);
                }
                finally {
                    await Read.CloseAsync();
                    await Read.DisposeAsync();
                }
                assureValidTransaction(sql);                
                return Result;
            } 
            finally {
                await Driver.DisposeCommand(cmd);
                await Close();
                MetaProfiler.StopTimer(NN);

            }

        }





        /// <summary>
        /// Get a list of "objects" from a table using  a specified query, every object is encapsulated in a dictionary
        /// </summary>
        /// <param name="sql">sql command to run</param>
        /// <param name="timeout"></param>
        /// <param name="ErrMsg"></param>
        /// <returns></returns>
        public virtual async Task<Dictionary<string, object>[]> ReadDictionaries(string sql, int timeout = -1) {
            if (sql == null)
                return null;
            //command = MyCompile(command);                       
            await assureValidOpen();

            var cmd = Driver.GetDbCommand(sql, timeout);

            int NN = MetaProfiler.StartTimer("ReadDictionaries");
            var resList = new List<Dictionary<string, object>>();
            DbDataReader Read = null;
            try {
                Read = await Driver.ExecuteReaderAsync(cmd);
                string[] fieldNames = new string[Read.FieldCount];
                for (int i = 0; i < Read.FieldCount; i++) {
                    fieldNames[i] = Read.GetName(i);
                }

                if (Read.HasRows) {
                    while (await Read.ReadAsync()) {
                        var curr = new Dictionary<string, object>();
                        for (int i = 0; i < Read.FieldCount; i++) {
                            curr.Add(fieldNames[i], Read[i]);
                        }

                        resList.Add(curr);
                        SetLastRead();
                    }
                }
                assureValidTransaction(sql);

                return resList.ToArray();
            }
            finally {
                await Read.CloseAsync();
                await Read.DisposeAsync();
                await Driver.DisposeCommand(cmd);
                await Close();
                MetaProfiler.StopTimer(NN);
            }
        }



        /// <summary>
        /// Builds a sql SELECT command 
        /// </summary>
        /// <param name="table">table implied</param>
        /// <param name="condition">condition for the deletion, can be a string or a MetaExpression</param>
        /// <returns></returns>
        public virtual string GetSelectCommand(string table, string columns = "*", object filter = null, string orderBy = null, string top = null) {           
            return Driver.GetSelectCommand(table, columns, Compile(filter), orderBy, top);
            
        }


        /// <summary>
        /// Builds a sql DELETE command 
        /// </summary>
        /// <param name="table">table implied</param>
        /// <param name="condition">condition for the deletion, can be a string or a MetaExpression</param>
        /// <returns></returns>
        public virtual string GetDeleteCommand(string table, object condition) {
            return Driver.GetDeleteCommand(GetPrefixedTable(table), Compile(condition));
        }


        /// <summary>
        /// Executes a delete command using current transaction. 
        /// </summary>
        /// <remarks>Requires an open connection</remarks>
        /// <param name="table"></param>
        /// <param name="condition"></param>
        /// <returns>Number of affected record</returns>
        public virtual async Task<int> DoDelete(string table, object condition) {
            if (condition == null) {
                throw new Exception($"DoDelete without condition on table {table}");
            }

            string deleteCmd = GetDeleteCommand(table, condition);            
            assureValidTransaction();
            int count = await Driver.ExecuteNonQuery(deleteCmd);
            SetLastWrite();

            assureValidTransaction(deleteCmd);
            return count;
        }

        string CreateList(IEnumerable<object> values) {
            return String.Join(',', values.Map(v=>quote(v)));
        }

        /// <summary>
        /// Builds a sql INSERT command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to insert</param>
        /// <param name="len">number of columns</param>
        /// <returns></returns>
        public virtual string GetInsertCommand(string table, List<string> columns, List<object> values) {
            return $"INSERT INTO {GetPrefixedTable(table)}({CreateList(columns)}) VALUES ({CreateList(values)})";
        }

        public virtual string GetInsertCommand(DataRow R) {
            DataTable T = R.Table;
            string tablename = T.tableForPosting();
            List<string> names = new List<string>();
            List<object> values = new List<object>();

            foreach (DataColumn C in T.Columns) {
                if (C.IsTemporary())
                    continue;
                if (R[C, DataRowVersion.Default] == DBNull.Value)
                    continue; //non inserisce valori null
                string postcolname = C.PostingColumnName(); // C.ColumnName;
                if (postcolname == null)
                    continue;
                names.Add(postcolname);
                values.Add(R[C, DataRowVersion.Default]);
            }

            return GetInsertCommand(tablename, names, values);
        }


        public virtual string GetUpdateCommand(DataRow R, object optimisticFilter) { // mainPost.GetOptimisticClause(R)
            DataTable T = R.Table;
            string tablename = T.tableForPosting();
            int npar = 0;


            var values = new Dictionary<string, object>();
            foreach (DataColumn C in T.Columns) {
                if (C.IsTemporary())
                    continue;
                if (R[C, DataRowVersion.Original].Equals(R[C, DataRowVersion.Current]))
                    continue;
                string postcolname = C.PostingColumnName(); // C.ColumnName;
                if (postcolname == null)
                    continue;
                values[postcolname] = R[C, DataRowVersion.Current];
                npar++;
            }
            return GetUpdateCommand(tablename, optimisticFilter, values);
        }


        public virtual string GetDeleteCommand(DataRow R, object optimisticFilter) {
            var T = R.Table;
            string tablename = T.tableForPosting();
            //string condition = mainPost.GetOptimisticClause(R);
            return GetDeleteCommand(tablename, optimisticFilter);
        }



        void assureValidTransaction(string msg=null) {           
            if (HasInvalidTransaction()) {
                if (msg == null) throw new Exception(LM.noValidTransaction);
                throw new Exception(LM.cmdInvalidatedTransaction(msg));
            }
        }


        /// <summary>
        /// Assures connection is open and there is not an invalid transaction
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        async Task assureValidOpen() {
            if (!await Open()) {
                throw new Exception(LM.errorOpeningConnection);
            }
            if (HasInvalidTransaction()) {
                await Close();
                throw new Exception(LM.noValidTransaction);
            }
           
        }

        /// <summary>
        /// Executes an INSERT command using current tranactin
        /// </summary>
        /// <remarks>Requires an open connection</remarks>
        /// <param name="table"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to insert</param>
        /// <param name="len">number of columns</param>
        /// <returns>Error message or null if OK</returns>
        public virtual async Task DoInsert(string table, List<string> columns, List<object> values) {
            assureValidTransaction();
            string insertCmd = GetInsertCommand(table, columns, values);
          
            int count;
            count = await Driver.ExecuteNonQuery(insertCmd);
            if (count > 0) {
	            SetLastWrite();
            }
            assureValidTransaction(insertCmd);
        }

        
        /// <summary>
        /// Builds an UPDATE sql command
        /// </summary>
        /// <param name="table"></param>
        /// <param name="condition"></param>
        /// <param name="columns">column names</param>
        /// <param name="values">values to be set</param>
        /// <returns>Error msg or null if OK</returns>
        public virtual string GetUpdateCommand(string table, object filter, Dictionary<string,object> values) {
            if (filter is q expression) {
                if (expression.isTrue()) filter= null;
			}            
            return Driver.GetUpdateCommand(GetPrefixedTable(table), Compile(filter), values);            
        }



        /// <summary>
        /// Executes an UPDATE command
        /// </summary>
        /// <remarks>Requires an open connection</remarks>
        /// <param name="table"></param>
        /// <param name="condition">where condition to apply, can be string or MetaExpression</param>
        /// <param name="columns">Name of columns to update</param>
        /// <param name="values">Values to set</param>
        /// <param name="ncol">N. of columns</param>
        /// <returns>N. of rows affected</returns>
        public virtual async Task<int> DoUpdate(string table, object filter, Dictionary< string,object> fieldValues){
            assureValidTransaction();
            //Creates a update command            
            string updateCmd = GetUpdateCommand(table, filter,  fieldValues);
           
            int count;
            count = await Driver.ExecuteNonQuery(updateCmd);
          
            if (count > 0) {
	            SetLastWrite();
            }
            assureValidTransaction(updateCmd);
            return count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public virtual async Task SqlIntoDataSet(DataSet d, string sql, int timeout = -1) {
            await assureValidOpen();

            int NN = MetaProfiler.StartTimer("SqlIntoDataSet*" + sql);
			try {
				await Driver.SqlIntoDataSet(d,sql,timeout);
				SetLastRead();
                assureValidTransaction(sql);
			}           
            finally {
                await Close();
                MetaProfiler.StopTimer(NN);
            }
        }

        /// <summary>
        /// Execute a sql cmd that returns a dataset (eventually with more than one table in it)
        /// </summary>
        /// <param name="sql">sql command to run</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMess">null if ok, Error message otherwise</param>
        /// <returns></returns>
        virtual public async Task<DataSet> DataSetBySql(string sql, int timeout) {
            var d = new DataSet();
            await SqlIntoDataSet(d, sql, timeout);
            return d;
        }

        public virtual string quote(object o) {
            return QHS.quote(o);
        }
        /// <summary>
        /// Calls a stored procedure with specified parameters
        /// </summary>
        /// <param name="procname">stored proc. name</param>
        /// <param name="list">Parameter list</param>
        /// <param name="timeout">Timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="ErrMess">null if ok, Error message otherwise</param>
        /// <returns></returns>
        virtual public async Task<DataSet> CallSP(string sp_name, object[] parameters, int timeout = -1) {
            await assureValidOpen();
            string cmd = Driver.GetStoredProcedureCall( GetPrefixedStoredProcedure(sp_name), parameters);
                
           

            //if (sys["Transaction"]!=null) SPCall.Transaction= (SqlTransaction)	sys["Transaction"];

            int NN = MetaProfiler.StartTimer("callSP " + sp_name);
            try {
                var myDS = new DataSet();
                await Driver.SqlIntoDataSet(myDS, cmd, timeout);                
                assureValidTransaction(cmd);
                SetLastRead();
                return myDS;
            }         
            finally {
                await Close();
                MetaProfiler.StopTimer(NN);
            }

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
        virtual public async Task CallVoidSPParams(string sp_name, DbParameter[] parameters, int timeout=-1) {
            await assureValidOpen();
            int NN = MetaProfiler.StartTimer("callVoidSP*" + sp_name);

            try {
                await Driver.CallVoidSPParams(GetPrefixedStoredProcedure(sp_name), parameters,timeout);
                SetLastRead();
                assureValidTransaction(LM.cmdInvalidatedTransaction(sp_name ));
            }
            finally {
                await Close();
                MetaProfiler.StopTimer(NN);
            }

        }


        /// <summary>
        /// Calls a stored procedure and returns a DataSet. First table can be retrieved in result.Tables[0]
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
        virtual public async Task<DataSet> CallSPParams(string sp_name,DbParameter[] parameters,int timeout) {
            await assureValidOpen();

            int NN = MetaProfiler.StartTimer("callSp " + sp_name);
            
            try {
	            var ds  = await Driver.CallSPParams(GetPrefixedStoredProcedure(sp_name),parameters, timeout);	            
	            SetLastRead();
                assureValidTransaction(sp_name);
                return ds;
            }            
            finally {
	            await Close();
                MetaProfiler.StopTimer(NN);
            }




        }



        #endregion

        #region SELECT COMMANDS


        /// <summary>
        /// Returns the copy of a single DataTable. This is quicker than .Clone(), especially if copyProperties is false
        /// DataTable properties are always copyed.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="copyProperties">true if ext.properties should be copyed</param>
        /// <returns></returns>
        public static DataTable singleTableClone(DataTable T, bool copyProperties) {
            //#if DEBUG
            int handle = MetaProfiler.StartTimer("SingleTableClone");
            //#endif
            DataTable T2 = new DataTable(T.TableName) {
                Namespace = T.Namespace
            };
            foreach (DataColumn C in T.Columns) {
                DataColumn C2 = new DataColumn(C.ColumnName, C.DataType, C.Expression) {
                    AllowDBNull = C.AllowDBNull
                };
                T2.Columns.Add(C2);
            }
            DataColumn[] key = T.PrimaryKey;
            DataColumn[] key2 = new DataColumn[key.Length];
            for (int i = 0; i < key.Length; i++) {
                key2[i] = T2.Columns[key[i].ColumnName];
            }
            T2.PrimaryKey = key2;
            if (!copyProperties) {
                //#if DEBUG
                MetaProfiler.StopTimer(handle);
                //#endif
                return T2;
            }
            //DataTable properties are always copyed
            foreach (object kt in T.ExtendedProperties.Keys) T2.ExtendedProperties[kt] = T.ExtendedProperties[kt];

            foreach (DataColumn C in T.Columns) {
                DataColumn C2 = T2.Columns[C.ColumnName];
                foreach (object kc in C.ExtendedProperties.Keys)
                    C2.ExtendedProperties[kc] = C.ExtendedProperties[kc];
            }
            //#if DEBUG
            MetaProfiler.StopTimer(handle);
            //#endif
            return T2;
        }


        /// <summary>
        /// Reads data into a table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public virtual async Task ExecuteQueryIntoTable(DataTable table, object filter = null, int timeout = -1) {
            await assureValidOpen();
            string columlist = QueryCreator.RealColumnNameList(table);

            string filtersec = Compile(filter,table);

            DataTable data = await Select(tablename: table.TableName, columnlist: columlist, filter: filtersec, timeout: timeout);
            QueryCreator.CheckKeyEqual(table, data);
            data.Namespace = table.Namespace;
            DataSetUtils.MergeDataTable(table, data);
        }

        /// <summary>
        /// Reads data into a given table, skipping temporary columns. Assumes connection already open,
        ///  adds blank row if it is marked to do so.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="orderBy">sorting for db reading</param>
        /// <param name="filter">condition to apply</param>
        /// <param name="top"></param>
        /// <param name="prepare"></param>
        public virtual async Task SelectIntoTable(DataTable T,
            object filter = null,
            string orderBy = null,
            string top = null,
            int timeout = -1) {

            var handle = MetaProfiler.StartTimer($"public SelectIntoTable({T.TableName})");
            var EmptyTable = singleTableClone(T, false);
           
			if (model.IsSkipSecurity(T)) {
				model.SetSkipSecurity(EmptyTable, true);
			}

			EmptyTable.TableName = T.tableForReading();
            try {
                var NewDS = new DataSet("dummy");
                NewDS.Tables.Add(EmptyTable);
                await selectIntoEmptyTable(EmptyTable,
                                    filter:filter,
                                    top:top,
                                    orderBy:orderBy);                
                EmptyTable.AcceptChanges();
                NewDS.Tables.Remove(EmptyTable);
                EmptyTable.TableName = T.TableName;
                EmptyTable.Namespace = T.Namespace;
                DataSetUtils.MergeDataTable(T, EmptyTable);
            }
            finally {
                MetaProfiler.StopTimer(handle);
            }
        }


      

        /// <summary>
        /// Fills an empty table with the result of a sql join
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public virtual async Task ReadTableJoined(DataTable table1, string table2,
            q filterTable1, q filterTable2,
            params string[] columns) {
            string sql = GetJoinSql(table1, table2, filterTable1, filterTable2, columns);
            if (table1.Rows.Count == 0) {
                await ExecuteQueryIntoEmptyTable(table1, sql);
                table1.AcceptChanges();
            } 
            else {
                await ExecuteQueryIntoTable(table1, sql);
            }

        }

        /// <summary>
        /// Executes a sql command and returns a DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteQuery(string sql, int timeout = -1) {
            if (sql == null)
                return null;
            await assureValidOpen();

            int handle = MetaProfiler.StartTimer("executeQuery()");
            try {
                var t = await Driver.TableBySql(sql, timeout: timeout);
                assureValidTransaction(sql);
                SetLastRead();
                return t;
            }
            catch (Exception E) {
                myLastError = MarkException("executeQuery: Error running " + sql, E);
                throw new Exception(myLastError, E);
            }
            finally {
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }

        /// <summary>
        /// Reads data into a table. The table is created at run-time using information
        ///  stored in columntypes. (ex RUN_SELECT)
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist">list of field names separated by commas</param>
        /// <param name="order_by">list of field names separated by commas</param>
        /// <param name="filter">condition to apply</param>
        /// <param name="top">how many rows to get</param>
        /// <param name="tableModel">when given, it is used as a model to create the table to read. It is not modified. </param>
        /// <param name="tableModel">optional model for table to read, useful to optimize table creation </param>
        /// <returns></returns>
        public async virtual Task<DataTable> Select(string tablename,
            object filter = null,
            string columnlist = "*",
            string order_by = null,
            string top = null,
            DataTable tableModel = null,
            int timeout = -1
            ) {
            if (filter != null && filter is q expression) {
                filter = expression.toSql(QHS, this.Security);
            }
            var T = (tableModel == null) ? await CreateTable(tablename, columnlist) : singleTableClone(tableModel, false);
            var MyDS = new DataSet("dummy");
            MyDS.Tables.Add(T);
            ClearDataSet.RemoveConstraints(MyDS);            
                //			if (T.Columns.Count==0){
                //				.Show("Non sono disponibile le informazioni relative alle colonne di "+
                //					tablename + ". Questo può accadere a causa di una erronea installazione. "+
                //					"Ad esempio, non è stato eseguito AnalizzaStruttura.","Errore");
                //			}
            if (T.Columns.Count > 0) {
                await selectIntoEmptyTable(T, orderBy: order_by, filter: (string)filter, top: top, timeout: timeout);
            }            
            else {
                return null;
			}
            MyDS.Tables.Remove(T);
            return T;
        }

        /// <summary>
        /// Reads data into a table. Data are read from DB table named EmptyTable.Tablename
        /// Internally uses SqlIntoTable. (ex RUN_SELECT_INTO_EMPTY_TABLE)
        /// </summary>
        /// <param name="emptyTable"></param>
        /// <param name="columnlist"></param>
        /// <param name="orderBy"></param>
        /// <param name="filter"></param>
        /// <param name="top"></param>
        /// <param name="groupBy"></param>
        /// <param name="prepare"></param>
        /// <returns>null if ok or error</returns>
        private async Task selectIntoEmptyTable(DataTable emptyTable,
            string orderBy = null,
            object filter = null,
            string top = null,
            int timeout = -1) {
            await assureOpen();

            string tablename = emptyTable.TableName;
            int handle = MetaProfiler.StartTimer("selectIntoEmptyTable*" + tablename);
            string columnlist = QueryCreator.RealColumnNameList(emptyTable);

            filter = Compile(filter, emptyTable);
           
            var selCmd = GetSelectCommand(
                table: GetPrefixedTable(tablename),
                columns: columnlist,
                filter: filter,
                orderBy: orderBy,
                top: top
                );

            try {
                await ExecuteQueryIntoEmptyTable(emptyTable, selCmd, timeout);
                assureValidTransaction();
                SetLastRead();
            }
            catch (Exception ex) {
                myLastError = ex.ToString();
                throw;
            }
            finally {
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }



        /// <summary>
        /// Reads data into a given table, skipping temporary columns (ex SQLRUN_INTO_TABLE)
        /// </summary>
        /// <param name="T"></param>
        /// <param name="sql">sorting for db reading</param>
        /// <param name="timeout">Timeout in second, 0 means no timeout, -1 means default timeout</param>
        public virtual async Task ExecuteQueryIntoTable(DataTable T, string sql,  int timeout = -1) {
            assureValidTransaction();

            if (T.Rows.Count == 0) { //may be this could become unconditional
                await ExecuteQueryIntoEmptyTable(T, sql, timeout);
                return;
            }

            int handle = MetaProfiler.StartTimer($"public SqlIntoTable*{T.TableName}");
            var EmptyTable = singleTableClone(T, false);
            EmptyTable.TableName = T.tableForReading();
            await ExecuteQueryIntoEmptyTable(EmptyTable, sql,timeout);
            EmptyTable.AcceptChanges();
            EmptyTable.TableName = T.TableName;
            EmptyTable.Namespace = T.Namespace;

            DataSetUtils.MergeDataTable(T, EmptyTable);
            MetaProfiler.StopTimer(handle);
            return;
        }


        /// <summary>
        /// Exec  a sql statement to read rows into a table (ex SQLRUN_INTO_EMPTY_TABLE)
        /// </summary>
        /// <param name="EmptyTable"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in second, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        protected virtual async Task ExecuteQueryIntoEmptyTable(DataTable EmptyTable, string sql, int timeout = -1) {
            await assureValidOpen();

            sql = Compile(sql);

            int handle = MetaProfiler.StartTimer("SqlIntoEmptyTable*(" + sql + ")");
            var Cmd = Driver.GetDbCommand(sql, timeout: timeout);

            try {
                EmptyTable.BeginLoadData();
                await Driver.TableBySql(sql, table: EmptyTable, timeout: timeout);
                EmptyTable.EndLoadData();
                SetLastRead();
                assureValidTransaction(sql);
                return;
            }
            catch (Exception ex) {
                myLastError = ex.ToString();
                throw;
            }
            finally {
                await Driver.DisposeCommand(Cmd);
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }


        /// <summary>
        /// Creates a dictionary from a query on a table (ex readSimpleDictionary)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="tablename"></param>
        /// <param name="keyField">key field of dictionary</param>
        /// <param name="valueField">value field of dictionary</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual async Task<Dictionary<T, S>> ReadDictionary<T, S>(string tablename,
                string keyField, string valueField,
                object filter = null
                ) {
            await assureOpen();
            var result = new Dictionary<T, S>();

            int handle = MetaProfiler.StartTimer("ReadDictionary*" + tablename);
            string selCmd = GetSelectCommand(
                table: GetPrefixedTable(tablename),
                columns: $"{keyField},{valueField}",
                filter: filter);

            var Cmd = Driver.GetDbCommand(selCmd);

            DbDataReader reader = null;
            try {
                reader = await Driver.ExecuteReaderAsync(Cmd);
                if (reader.HasRows) {
                    int countField = reader.FieldCount;
                    while (await reader.ReadAsync()) {
                        result[(T)reader[0]] = (S)reader[1];
                    }
                    SetLastRead();
                }
                return result;
            }
            catch (Exception E) {
                myLastError = E.ToString();
                throw new Exception("readSimpleDictionary: Error running " + selCmd, E);
            } 
            finally {
                await Driver.DisposeCommand(Cmd);
                reader?.Dispose();
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }



        /// <summary>
        /// Creates a dictionary key => rowObject from a query on a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tablename"></param>
        /// <param name="keyField">key field of dictionary</param>
        /// <param name="fieldList">list value to read (must not include keyField)</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual async Task<Dictionary<T, RowObject>> SelectRowObjectDictionary<T>(string tablename,
                string keyField, string fieldList,
                object filter = null
                ) {
            if (!await Open()) {
                throw new Exception(LM.errorOpeningConnection);
            }
            var result = new Dictionary<T, RowObject>();
            int handle = MetaProfiler.StartTimer("SelectRowObjectDictionary*" + tablename );

            string selCmd = GetSelectCommand(
                table: GetPrefixedTable(tablename),
                filter: filter,
                columns: $" {keyField},{fieldList} "
                );
            
            var cmd = Driver.GetDbCommand(selCmd);
            
            int handleFill = MetaProfiler.StartTimer("Cmd.SelectRowObjectDictionary*" + tablename);
            DbDataReader reader = null;
            try {
                reader = await Driver.ExecuteReaderAsync(cmd);
                if (reader.HasRows) {
                    int countField = reader.FieldCount;
                    var lookup = new Dictionary<string, int>();
                    for (int i = 0; i < countField; i++) {
                        lookup[reader.GetName(i)] = i;
                    }
                    string[] fieldNames = Enumerable.Range(0, countField).Select(reader.GetName).ToArray();
                    while (await reader.ReadAsync()) {
                        object[] arr = new object[countField];
                        reader.GetValues(arr);
                        result[(T)arr[0]] = new RowObject(lookup, arr);
                    }
                    SetLastRead();                   
                }
                return result;
            }
            catch (Exception E) {
                myLastError= E.ToString();
                throw new Exception("ReadRowObjectDictionary: Error running " + selCmd, E);
            }
            finally {
                await Driver.DisposeCommand(cmd);
                await reader.DisposeAsync();
                await Close();
                MetaProfiler.StopTimer(handleFill);
            }
          
          
        }

        /// <summary>
        /// Selects multiple sets of rowobjects from db (ex SelectRowObjects)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tables">logical table names</param>
        /// <returns></returns>
        public virtual async Task<Dictionary<string, List<RowObject>>> RowObjectsBySql(string sql, params string[] tables) {
            if (!await Open()) {
                throw new Exception(LM.errorOpeningConnection);
            }

            var result = new Dictionary<string, List<RowObject>>();           
            int handle = MetaProfiler.StartTimer("SelectRowObjects()");
            sql = Compile(sql);
            var cmd = Driver.GetDbCommand(sql);
          
            DbDataReader reader = null;
            try {
                reader = await Driver.ExecuteReaderAsync(cmd);
                int nSet = 0;
                while (nSet < tables.Length) {
                    List<RowObject> currSet = new List<RowObject>();
                    result[tables[nSet]] = currSet;

                    if (!reader.HasRows) {
                        nSet++;
                        await reader.NextResultAsync();
                        continue;
                    }

                    int countField = reader.FieldCount;
                    var lookup = new Dictionary<string, int>();
                    for (int i = 0; i < countField; i++) {
                        lookup[reader.GetName(i)] = i;
                    }

                    while (await reader.ReadAsync()) {
                        object[] arr = new object[countField];
                        reader.GetValues(arr);
                        currSet.Add(new RowObject(lookup, arr));
                    }

                    await reader.NextResultAsync();
                    nSet++;
                }
                SetLastRead();
                return result;
            }
            catch (Exception E) {
                myLastError=E.ToString();
                throw new Exception("ReadRowObjectDictionary: Error running " + sql, E);
            }            
            finally {
                await Driver.DisposeCommand(cmd);
                await reader.DisposeAsync();
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }


        /// <summary>
        /// Selects a set of rowobjects from db
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist"></param>
        /// <param name="filter"></param>
        /// <param name="order_by"></param>
        /// <param name="TOP"></param>
        /// <returns></returns>
        public virtual async Task<List<RowObject>> SelectRowObjects(string tablename,
                                                                    string columnlist = "*",
                                                                    object filter = null,
                                                                    string orderBy = null,
                                                                    string TOP = null) {
            if (columnlist == null) columnlist = "*";
            filter = Compile(filter);

            if (!await Open()) {
                throw new Exception(LM.errorOpeningConnection);
            }

            var result = new List<RowObject>();
            int handle = MetaProfiler.StartTimer("RowObject_Select*" + tablename );            

            string sql = GetSelectCommand(
                table:GetPrefixedTable(tablename),
                columns:columnlist,
                top:TOP,
                filter:filter,
                orderBy: orderBy
                );
            var cmd = Driver.GetDbCommand(sql);

            DbDataReader reader = null;
            try {
                reader = await Driver.ExecuteReaderAsync(cmd);
                if (reader.HasRows) {
                    int countField = reader.FieldCount;
                    Dictionary<string, int> lookup = new Dictionary<string, int>();
                    for (int i = 0; i < countField; i++) {
                        lookup[reader.GetName(i)] = i;
                    }
                    
                    while (await reader.ReadAsync()) {
                        object[] arr = new object[countField];
                        reader.GetValues(arr);
                        result.Add(new RowObject(lookup, arr));
                    }
                    SetLastRead();
                }
                return result;
            }
            catch (Exception E) {
                myLastError= E.ToString();
                throw new Exception("SelectRowObjects: Error running " + sql, E);
            }
            finally {
                if (reader!=null) await reader.DisposeAsync();
                await Driver.DisposeCommand(cmd);
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }


        List<SelectBuilder> GroupSelect(List<SelectBuilder> L) {
            var Grouped = new List<SelectBuilder>();
            var LResult = new List<SelectBuilder>();

            //Cicla solo sugli ottimizzati
            foreach (var S in L) {
                if (!S.isOptimized()) continue;

                //se è ottimizzato lo aggiunge in modo ottimizzato oppure niente
                bool added = false;
                foreach (var G in Grouped) {
                    if (!G.isOptimized()) continue;
                    if (!G.CanAppendTo(S)) continue;
                    if (G.OptimizedAppendTo(S, QHS)) {
                        added = true;
                        break;
                    }
                }
                if (!added) {
                    Grouped.Add(S);
                }
            }

            //riprende gli ottimizzati
            foreach (var S in Grouped) {
                SelectBuilder ToGroup = null;
                foreach (var G in LResult) {
                    if (G.CanAppendTo(S)) {
                        ToGroup = G;
                        break;
                    }

                }
                if (ToGroup != null) {
                    ToGroup.AppendTo(S, QHS);
                } else {
                    LResult.Add(S);
                }
            }

            //prende i non  ottimizzati
            foreach (var S in L) {
                if (S.isOptimized()) continue;
                SelectBuilder ToGroup = null;
                foreach (var G in LResult) {
                    if (G.CanAppendTo(S)) {
                        ToGroup = G;
                        break;
                    }

                }
                if (ToGroup != null) {
                    ToGroup.AppendTo(S, QHS);
                } else {
                    LResult.Add(S);
                }
            }


            return LResult;
        }



        /// <summary>
        /// Executes a List of Select, returning data in the tables specified by each select. 
        /// </summary>
        /// <param name="SelList"></param>
        public virtual async Task  MultipleSelect(List<SelectBuilder> SelList) {
            await assureValidOpen();

            SelList = GroupSelect(SelList);

            DataSet D = null;

            List <string> cmd = new List<string>();
            List<string> destTables = new List<string>();

            string multitab = "";
            foreach (SelectBuilder Sel in SelList) {
                multitab += Sel.tablename + " ";
                string filtersec = Compile(Sel.filter, Sel.DestTable);
                //if (Sel.DestTable == null || !model.IsSkipSecurity(Sel.DestTable)) {
                //    filtersec = QHS.AppAnd(filtersec, Security.SelectCondition(Sel.tablename, true));
                //}

                string singleSelect = GetSelectCommand(
                    table:Sel.tablename,
                    columns: Sel.columnlist,
                    top: Sel.TOP,
                    filter: filtersec,
                    orderBy: Sel.order_by
                    );
                destTables.Add(Sel.tablemap);
                cmd.Add(singleSelect);

                if (Sel.DestTable != null && D == null) {
                    D = Sel.DestTable.DataSet;
                }
                model.InvokeActions(Sel.DestTable, TableAction.beginLoad);             
            }

            int handle = MetaProfiler.StartTimer("MultipleSelect " + multitab);

            string selCmd = Driver.JoinMultipleSelectCommands(cmd.ToArray());

            if (D == null) {
                D = new DataSet("temp");
                ClearDataSet.RemoveConstraints(D);
            }
            try {
                var DD = await Driver.MultipleTableBySql(selCmd, destTables);
                foreach (var sel in SelList) {
                    if (sel.DestTable == null) {
                        sel.DestTable = DD.Tables[sel.tablename];
                        DD.Tables.Remove(sel.DestTable);
                        model.InvokeActions(sel.DestTable, TableAction.endLoad);
                    }
                    else {
                        var handleMerge = MetaProfiler.StartTimer("MultipleSelect DA.Merge*" + multitab);
                        sel.DestTable.BeginLoadData();
                        if (DD.Tables[sel.DestTable.TableName] != null)
                            sel.DestTable.Merge(DD.Tables[sel.DestTable.TableName], true, MissingSchemaAction.Ignore);
                        sel.DestTable.EndLoadData();
                        model.InvokeActions(sel.DestTable, TableAction.endLoad);
                    }
                    sel.OnRead();
                }
                SetLastRead();
            }            
            catch (Exception E) {
                myLastError = E.ToString();
                throw new Exception("MultipleSelect: Error running " + selCmd, E);
            }            
            finally {                
                await Close();
                MetaProfiler.StopTimer(handle);
            }
        }


        /// <summary>
        /// Reads a table without reading the schema. Result table has no primary key set.
        /// This is quicker than a normal select but slower than a RowObject_select
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="columnlist"></param>
        /// <param name="filter"></param>
        /// <param name="order_by"></param>
        /// <param name="TOP"></param>
        /// <returns></returns>
        public virtual async Task<DataTable> ReadTableNoKey(string tablename, object filter = null, string columnlist = "*", string order_by = null, string TOP = null) {
            await assureValidOpen();

            var emptyDs = new DataSet();
            ClearDataSet.RemoveConstraints(emptyDs);

            int handle = MetaProfiler.StartTimer("readFromTable*" + tablename );
            
            string SelCmd = GetSelectCommand(
                table:GetPrefixedTable(tablename),
                top:TOP,
                columns:columnlist,
                filter: QHS.AppAnd(Compile(filter),Compile(Security.SelectCondition(tablename))),
                orderBy: order_by
                );            
            
            try {
                await Driver.SqlIntoDataSet(emptyDs, SelCmd);
                SetLastRead();
                DataTable EmptyTable = emptyDs.Tables[0];
                emptyDs.Tables.Remove(EmptyTable);
                return EmptyTable;
            }           
            finally {
                await Close();
                MetaProfiler.StopTimer(handle);
            }
            
        }





        /// <summary>
        /// Executes a SELECT COUNT on a table.
        /// Internally uses a ReadValue with a count expression
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="filter">condition to apply</param>
        /// <returns></returns>
        public virtual async Task<int> Count(string tablename, MetaExpression filter = null) {
            await assureOpen();
            object N= await ReadValue(table:tablename, filter:filter, expr:"count(*)");
            if (N == null || N == DBNull.Value) return 0;
            return Convert.ToInt32(N);

        }
        #endregion


        #region Enumerator

        /// <summary>
        /// Reads data row by row
        /// </summary>
        public sealed class DataRowReader :IDisposable, IEnumerator {
            bool disposed = false;
            IDataReader SDR = null;
            string[] cols = null;
            DataRow Curr = null;
            IDbCommand Cmd;
            DataTable T;
            IDataAccess Conn;
            void GetRow() {
                Curr = T.NewRow();
                for (int i = 0; i < cols.Length; i++) {
                    Curr[cols[i]] = SDR.GetValue(i);
                }
            }


            void init(IDataAccess Conn, string table, string columnlist, string order_by, string filter) {
                T = Conn.CreateTable(table, "*").GetAwaiter().GetResult();
                if (columnlist == null || columnlist == "*") {
                    columnlist = QueryCreator.RealColumnNameList(T);
                }
                filter = Conn.Compile(filter);

                
                //Cmd.Connection= MySqlConnection;
                //if (sys["Transaction"]!=null) Cmd.Transaction= (SqlTransaction)	sys["Transaction"];

                string SelCmd = Conn.GetSelectCommand(
                    table:Conn.GetPrefixedTable(table),
                    columns: columnlist,
                    filter:filter,
                    orderBy: order_by
                    );
                Cmd = Conn.Driver.GetDbCommand(SelCmd);
                Cmd.CommandText = SelCmd;
                Conn.Open().Wait();
                
                SDR = Conn.Driver.ExecuteReaderAsync(Cmd).GetAwaiter().GetResult();
                cols = columnlist.Split(',');

                this.Conn = Conn;
            }


            /// <summary>
            /// Creates the iterator
            /// </summary>
            /// <param name="Conn"></param>
            /// <param name="table"></param>
            /// <param name="columnlist"></param>
            /// <param name="order_by"></param>
            /// <param name="filter"></param>
            public DataRowReader(IDataAccess Conn, string table, string columnlist,
                        string order_by, string filter) {
                init(Conn, table, columnlist, order_by, filter);

            }

            /// <summary>
            ///  Creates the iterator
            /// </summary>
            /// <param name="Conn"></param>
            /// <param name="table"></param>
            /// <param name="columnlist"></param>
            /// <param name="order_by"></param>
            /// <param name="filter"></param>
            public DataRowReader(IDataAccess Conn, string table, object filter,
                    string columnlist = null, string order_by = null) {
                init(Conn, table, columnlist, order_by, Conn.Compile(filter));

            }
            object IEnumerator.Current => Curr;

            /// <summary>
            /// Necessary method to implement the iteratorinterface
            /// </summary>
            /// <returns></returns>
            public bool MoveNext() {
                if (SDR == null) return false;
                if (!SDR.Read()) return false;
                GetRow();
                return true;
            }

            /// <summary>
            /// Restars the iterator 
            /// </summary>
            public void Reset() {
                SDR?.Dispose();
                SDR = Cmd.ExecuteReader();
            }

            /// <summary>
            /// Disposes the iterator
            /// </summary>
            /// <param name="disposing"></param>
            public void Dispose(bool disposing) {
                if (!disposed) {
                    if (disposing) {
                        SDR?.Dispose();
                        Cmd?.Dispose();
                        Conn?.Close();
                    }
                    // Free your own state (unmanaged objects).
                    // Set large fields to null.
                    disposed = true;
                }
            }

            /// <summary>
            /// Disposes the iterator
            /// </summary>
            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Necessary method to implement the iteratorinterface
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator() {
                return (IEnumerator)this;
            }

            /// <summary>
            /// public destructor
            /// </summary>
            ~DataRowReader() {
                // Simply call Dispose(false).
                Dispose(false);
            }

        }


        #endregion



        /// <summary>
        /// Returns a value executing a generic sql command 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="timeOut">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public virtual async Task<int> ExecuteNonQuery(string cmd, int timeOut = -1) {
            await assureValidOpen();
                
            int result;
            try {
                result = await Driver.ExecuteNonQuery(cmd, timeOut);
                assureValidTransaction(cmd);
                return result;
            }
            catch (Exception e) {
                myLastError = e.ToString();
                MarkException("executeNonQuery: Error running " + cmd, e);
                throw new Exception("executeNonQuery: Error running " + cmd, e);
            }
            finally {
                await Close();
            }
        }


        /// <summary>
        /// Run a command string and get result asynchronously
        /// </summary>
        /// <remarks>This method </remarks>
        /// <param name="selList"></param>
        /// <param name="packetSize"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task  ExecuteSelectBuilder(List<SelectBuilder> selList, int packetSize, Action<SelectBuilder, Dictionary<string, object>> callback, int timeout = -1) {
            await assureValidOpen();

            List<string> cmd = new List<string>();
            List<string> destTables = new List<string>();

            string multitab = "";
            DataSet D = null;
            
            foreach (var Sel in selList) {

                string filtersec = Compile(Sel.filter, Sel.DestTable);
               
                string singleSelect = GetSelectCommand(
                    table:Sel.tablename,
                    columns: Sel.columnlist,
                    top: Sel.TOP,
                    filter: filtersec,
                    orderBy: Sel.order_by
                    );
                cmd.Add(singleSelect);

                multitab += Sel.tablename + " ";
                if (Sel.DestTable != null && D == null) {
                    D = Sel.DestTable.DataSet;
                }
                
            }
            ClearDataSet.RemoveConstraints(D);            
            var sqlCmd = Driver.GetDbCommand(Driver.JoinMultipleSelectCommands(cmd.ToArray()),timeout);
            DbDataReader reader = null;
            try {
                int currTable = 0;
                reader = await Driver.ExecuteReaderAsync(sqlCmd);
                do {
                    var selBuilder = selList[currTable];
                    var table = selBuilder.DestTable;
                    var colNum = new List<int>();
                    for (int i = 0; i < table.Columns.Count; i++) {
                        var C = table.Columns[i];
                        if (C.IsTemporary())
                            continue;
                        colNum.Add(i);
                    }

                    var res = new Dictionary<string, object> { ["table"] = table };
                    var localRows = new List<DataRow>();
                    if (packetSize != 0) {
                        callback(selBuilder, res); //invia lo "start" su questa tabella
                                                   //res = new Dictionary<string, object>();
                    }
                    res["rows"] = localRows;                    
                    while (await reader.ReadAsync()) {
                        var dataRow = table.NewRow();
                        for (int i = colNum.Count - 1; i >= 0; i--) {
                            dataRow[colNum[i]] = reader[i];
                        }
                        localRows.Add(dataRow);
                        if (packetSize == 0 || localRows.Count != packetSize) continue;
                        callback(selBuilder, res);  //se è arrivato al limite del pacchetto invia le righe
                        localRows = new List<DataRow>();
                        res = new Dictionary<string, object> { ["rows"] = localRows, ["table"] = table };
                    }

                    if (localRows.Count > 0) {
                        callback(selBuilder, res); //se packetsize==0 include sia "meta" che "rows" altrimenti solo "rows"
                    }
                    currTable++;
                }
                while (await reader.NextResultAsync());               
            }
            catch (Exception e) {
                myLastError = e.ToString();
                throw;
            }
            finally {
                await Driver.DisposeCommand(sqlCmd);
                await reader.DisposeAsync();
            }
            
            callback(null, new Dictionary<string, object> { ["resolve"] = 1 });//alla fine invia sempre un "resolve"
            await Close();
        }

       
        /// <summary>
        /// Run a command string and get result asynchronously
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="packetSize"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task ExecuteQueryTables(string sql, int packetSize, int timeout, Func<object, Task> callback) {
            await assureValidOpen();
            try {
                var cmd = Driver.GetDbCommand(sql, timeout);
                var reader = await Driver.ExecuteReaderAsync(cmd);

                int nSet = 0;
                do {
                    var fieldNames = new object[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++) {
                        fieldNames[i] = reader.GetName(i);
                    }

                    var res = new Dictionary<string, object> { ["meta"] = fieldNames, ["resultSet"] = nSet };
                    var localRows = new List<object[]>();
                    if (packetSize != 0) {
                        await callback(res); //invia "meta" separatamente, poi invierà le rows
                        res = new Dictionary<string, object>();
                    }

                    res["rows"] = localRows;
                    var record = (IDataRecord)reader;
                    while (await reader.ReadAsync()) {
                        var resultRecord = new object[record.FieldCount];
                        record.GetValues(resultRecord);
                        localRows.Add(resultRecord);
                        if (packetSize == 0 || localRows.Count != packetSize) continue;
                        await callback(res);  //se è arrivato al limite del pacchetto invia le righe
                        localRows = new List<object[]>();
                        res = new Dictionary<string, object> { ["rows"] = localRows };
                    }
                    if (localRows.Count > 0) {
                        await callback(res); //se packetsize==0 include sia "meta" che "rows" altrimenti solo "rows"
                    }
                    nSet++;
                } while (await reader.NextResultAsync());
                await callback(new Dictionary<string, object> { ["resolve"] = 1 });//alla fine invia sempre un "resolve"
            }
            catch (Exception e) {
                myLastError = e.ToString();
                throw;
            }
            finally {
                await Close();
            }                       
        }

        string CompileFilterObject(object filter) {
            if (filter == null)
                return null;
            if (filter == DBNull.Value)
                return null;
            if (filter is string) {
                return Security.Compile(filter as string, qh: QHS);
            }
            if (filter is q expression) {
                return expression.toSql(QHS, this.Security);
            }
                
            throw new ArgumentException("Filter must be a string or a MetaExpression");
        }

        /// <summary>
        /// creates a string filter from a generic filter (string o MetaExpression)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public string Compile(object filter, DataTable T = null) {
            string strFilter = CompileFilterObject(filter);
            if (T is null) {
                return strFilter;
			}
            if (model.IsSkipSecurity(T)) {
                return strFilter;
			}
            return QHS.AppAnd(strFilter, CompileFilterObject(Security.SelectCondition(T.tableForReading())));
        }

      

        /// <summary>
        /// Logs an error to the remote logger
        /// </summary>
        /// <param name="errmsg"></param>
        /// <param name="E"></param>
        private void LogError(string errmsg, Exception E) {
            errorLogger.logException(errmsg, security: Security, exception: E, dataAccess: this);

        }


        /// <summary>
        /// Returns the queryhelper attached to this kind of DataAccess
        /// </summary>
        /// <returns></returns>
        public virtual QueryHelper GetQueryHelper() {
            return Driver.QH;
        }


        public virtual Dictionary<string, object> GetDictFrom(DataRow r, params string[] fields) {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < fields.Length; i++) {
                d[fields[i]] = r[fields[i]];
            }
            return d;
        }

        public virtual Dictionary<string, object> GetDictFrom(List<string> fields, List<object> values) {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < fields.Count; i++) {
                d[fields[i]] = values[i];
            }
            return d;
        }

     


        /// <summary>
        /// Forces ColumnTypes to be read again from DB for tablename
        /// </summary>
        /// <param name="tablename"></param>
        public virtual async Task RefreshStructure(string tablename) {
            descriptor.Reset(tablename);
            await Descriptor.GetStructure(tablename, this);
            
        }
    }
        

    /// <summary>
    /// Compare an ordered set of field to an ordered set of values
    /// </summary>
    public class MultiCompare {
        /// <summary>
        /// Values to compare with the fields
        /// </summary>
        public object[] values;

        /// <summary>
        /// Fields to compare
        /// </summary>
        public string[] fields;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="values"></param>
        public MultiCompare(string[] fields, object[] values) {
            this.values = values;
            this.fields = fields;
        }

        /// <summary>
        /// Check if the fields of this comparator are the same of the specified one
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public bool SameFieldsAs(MultiCompare C) {
            if (C.fields.Length != this.fields.Length) return false;
            for (int i = 0; i < C.fields.Length; i++) {
                if (fields[i] != C.fields[i]) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Class for creating optimized queries
    /// </summary>
    public class OptimizedMultiCompare {
        /// <summary>
        /// List of fields to be compared
        /// </summary>
        public string[] fields;

        /// <summary>
        /// List of values to compare with  the multivalue field
        /// </summary>
        public List<object>[] values;

        /// <summary>
        /// Position of the only field that can differ in a join between two OptimizedMulticompare
        /// </summary>
        int multival_pos = -1;

        /// <summary>
        /// True when there is a field to compare with a set of values
        /// </summary>
        /// <returns></returns>
        public bool IsMultivalue() {
            return (multival_pos != -1);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="C"></param>
        public OptimizedMultiCompare(MultiCompare C) {
            this.fields = C.fields;
            this.values = new List<object>[C.values.Length];
            for (int i = 0; i < C.values.Length; i++) {
                values[i] = new List<object> { C.values[i] };
            }
        }


        /// <summary>
        /// Return true if this Compare operates on the same fields as the specified one
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public bool SameFieldsAs(OptimizedMultiCompare C) {
            if (C.fields.Length != this.fields.Length) return false;
            for (int i = 0; i < C.fields.Length; i++) {
                if (fields[i] != C.fields[i]) return false;
            }
            return true;
        }

        bool HaveValue(object O, int pos) {
            foreach (object v in values[pos]) {
                if (v.Equals(O)) return true;
            }
            return false;
        }

        /// <summary>
        /// Join this Multicompare with another one, return false  if it is not possible
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public bool JoinWith(OptimizedMultiCompare C) {
            if (!SameFieldsAs(C)) return false;
            if (C.IsMultivalue()) return false;

            int pos_diff = -1; //posizione della differenza trovata
            if (multival_pos == -1) {
                //verifica che ci sia al massimo una differenza
                for (int i = 0; i < fields.Length; i++) {
                    if (!HaveValue(C.values[i][0], i)) {
                        if (pos_diff != -1) return false; //più di una differenza trovata
                        pos_diff = i;
                        continue;
                    }
                }
            }
            else {
                //verifica che ci sia al massimo una differenza e che sia in multival_pos
                for (int i = 0; i < fields.Length; i++) {
                    if (!HaveValue(C.values[i][0], i)) {
                        if (i != multival_pos) return false; //più di una differenza trovata
                        pos_diff = i;
                        continue;
                    }
                }
            }
            if (pos_diff == -1) return true;
            values[pos_diff].Add(C.values[pos_diff][0]);
            multival_pos = pos_diff;
            return true;
        }

        /// <summary>
        /// Gets the optimized filter to obtain rows 
        /// </summary>
        /// <returns></returns>
        public MetaExpression GetFilter() {
            return q.and(
                fields.Zip(values,
                    (fName,fValue) => {
                        if (fValue.Count == 1) {
                            return q.eq(fName,fValue[0]);
						}
                        else {
                            return q.fieldIn(fName, fValue.ToArray());
						}
					}
                )
            );
           
        }

    }


    //string tablename, 
    //        string columnlist,
    //        string order_by, 
    //        string filter, 
    //        string TOP,
    //        string group_by


    /// <summary>
    /// Manage the construction of a sql - select command
    /// </summary>
    public interface ISelectBuilder {
        /// <summary>
        /// Overall filter to be used in the select command
        /// </summary>
        MetaExpression filter { get; }

        /// <summary>
        /// Adds an AfterRead delegate to be called in a specified context
        /// </summary>
        /// <param name="Fun"></param>
        /// <param name="Context"></param>
        void AddOnRead(SelectBuilder.AfterReadDelegate Fun, object Context);

        /// <summary>
        /// Method to be invoked after data have been retrived from db
        /// </summary>
        void OnRead();

        /// <summary>
        /// Specify a filter for the query
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        SelectBuilder Where(MetaExpression filter);

        /// <summary>
        /// Specify a MultiCompare as filter
        /// </summary>
        /// <param name="MC"></param>
        /// <returns></returns>
        SelectBuilder MultiCompare(MultiCompare MC);

        /// <summary>
        /// Specify the table to be read
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        SelectBuilder From(string tablename);

        /// <summary>
        /// Specify the sorting order
        /// </summary>
        /// <param name="order_by"></param>
        /// <returns></returns>
        SelectBuilder OrderBy(string order_by);

        /// <summary>
        /// Specify the TOP clause of the select command
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        SelectBuilder Top(string top);

        /// <summary>
        /// Specify the groupBy clause
        /// </summary>
        /// <param name="groupBy"></param>
        /// <returns></returns>
        SelectBuilder GroupBy(string groupBy);

        /// <summary>
        /// Specify the destination table for reading data
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        SelectBuilder IntoTable(DataTable T);

        /// <summary>
        /// Check if this select can be added to the specified one
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        bool CanAppendTo(SelectBuilder S);

        /// <summary>
        /// Check if this select is optimized (so can be joined to other)
        /// </summary>
        /// <returns></returns>
        bool isOptimized();

        /// <summary>
        /// Merge this select the specified one in an optimized way, return false if it was not possibile
        /// </summary>
        /// <param name="S"></param>
        /// <param name="QH"></param>
        /// <returns></returns>
        bool OptimizedAppendTo(SelectBuilder S, QueryHelper QH);

        /// <summary>
        /// Append this select to another one as a separate command to be executed (not-optimized)
        /// </summary>
        /// <param name="S"></param>
        /// <param name="QH"></param>
        void AppendTo(SelectBuilder S, QueryHelper QH);
    }

    /// <summary>
    /// Manage the construction of a sql - select command
    /// </summary>
    public class SelectBuilder :ISelectBuilder {
        internal string tablename = null;
        internal string columnlist = null;
        internal string order_by = null;
        private MetaExpression myfilter = null;
        internal string TOP = null;
        internal string group_by = null;

        /// <summary>
        /// Table where rows will be read into
        /// </summary>
        public DataTable DestTable;
        internal string tablemap = null;
        internal int count = 1;
        internal OptimizedMultiCompare OMC = null;

        /// <summary>
        /// Delegate kind to be called after the table is read
        /// </summary>
        /// <param name="T"></param>
        /// <param name="Context"></param>
        public delegate void AfterReadDelegate(DataTable T, object Context);

        private object Context = null;
        private AfterReadDelegate myOnRead = null;

        /// <summary>
        /// Overall filter to be used in the select command
        /// </summary>
        public MetaExpression filter {
            get {
                if (!(myfilter is null)) return myfilter;
                if (OMC != null) return GetData.MergeFilters(OMC.GetFilter(), DestTable);
                return null;
            }

        }

        /// <summary>
        /// Adds an AfterRead delegate to be called in a specified context
        /// </summary>
        /// <param name="Fun"></param>
        /// <param name="Context"></param>
        public virtual void AddOnRead(AfterReadDelegate Fun, object Context) {
            this.Context = Context;
            myOnRead += Fun;
        }

        /// <summary>
        /// Method to be invoked after data have been retrived from db
        /// </summary>
        public virtual void OnRead() {
            if (myOnRead == null) return;
            int handle = MetaProfiler.StartTimer("OnRead()");
            myOnRead(DestTable, Context);
            MetaProfiler.StopTimer(handle);
        }

        /// <summary>
        /// Constructor for reading specified columns
        /// </summary>
        /// <param name="columnlist"></param>
        public SelectBuilder(string columnlist) {
            this.columnlist = columnlist;
        }

        /// <summary>
        /// Constructor for reading all columns
        /// </summary>
        public SelectBuilder() {
            this.columnlist = "*";
        }

        /// <summary>
        /// Specify a filter for the query
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual SelectBuilder Where(MetaExpression filter) {
            this.myfilter = filter;
            return this;
        }

        /// <summary>
        /// Specify a MultiCompare as filter
        /// </summary>
        /// <param name="MC"></param>
        /// <returns></returns>
        public virtual SelectBuilder MultiCompare(MultiCompare MC) {
            this.OMC = new OptimizedMultiCompare(MC);
            return this;
        }



        /// <summary>
        /// Specify the table to be read
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public virtual SelectBuilder From(string tablename) {
            this.tablename = tablename;
            if (this.tablemap == null) {
                this.tablemap = tablename;
            }
            return this;
        }

        /// <summary>
        /// Specify the sorting order
        /// </summary>
        /// <param name="order_by"></param>
        /// <returns></returns>
        public virtual SelectBuilder OrderBy(string order_by) {
            this.order_by = order_by;
            return this;
        }

        /// <summary>
        /// Specify the TOP clause of the select command
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public virtual SelectBuilder Top(string top) {
            this.TOP = top;
            return this;
        }

        /// <summary>
        /// Specify the groupBy clause
        /// </summary>
        /// <param name="groupBy"></param>
        /// <returns></returns>
        public virtual SelectBuilder GroupBy(string groupBy) {
            this.group_by = groupBy;
            return this;
        }

        /// <summary>
        /// Specify the destination table for reading data
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public virtual SelectBuilder IntoTable(DataTable T) {
            this.DestTable = T;
            columnlist = QueryCreator.RealColumnNameList(T);
            tablename = T.tableForReading();
            tablemap = T.TableName;
            return this;
        }


        /// <summary>
        /// Check if this select can be added to the specified one
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public virtual bool CanAppendTo(SelectBuilder S) {
            if (this.tablename != S.tablename) return false;
            if (this.tablemap != S.tablemap) return false;
            if (this.group_by != null || S.group_by != null) return false;
            return true;
        }

        /// <summary>
        /// Check if this select is optimized (so can be joined to other)
        /// </summary>
        /// <returns></returns>
        public virtual bool isOptimized() {
            return (OMC != null);
        }
     

        /// <summary>
        /// Merge this select the specified one in an optimized way, return false if it was not possibile
        /// </summary>
        /// <param name="S"></param>
        /// <param name="QH"></param>
        /// <returns></returns>
        public virtual bool OptimizedAppendTo(SelectBuilder S, QueryHelper QH) {
            if (OMC == null || S.OMC == null) return false;
            bool res = OMC.JoinWith(S.OMC);
            if (!res) return false;
            myfilter = null;
            return true;
        }



        /// <summary>
        /// Append this select to another one as a separate command to be executed (not-optimized)
        /// </summary>
        /// <param name="S"></param>
        /// <param name="QH"></param>
        public virtual void AppendTo(SelectBuilder S, QueryHelper QH) {
            if (this.filter is null) return;
            if (S.filter is null) {
                this.myfilter = null;
                return;
            }
            if (this.filter.toString() == S.filter.toString()) return;
            this.myfilter = q.or(this.filter, S.filter);
            count++;
        }
    }


    /// <summary>
    /// Class used to manage system type conversions
    /// </summary>
    public class GetType_Util {

        /// <summary>
        /// Converts a system type name into a aystem type
        /// </summary>
        /// <param name="Stype">type name</param>
        /// <returns>corresponding system type</returns>
        public static Type GetSystemType_From_StringSystemType(string Stype) {
			return Stype switch {
				"System.Boolean" => typeof(bool),
				"System.Byte" => typeof(byte),
				"System.Char" => typeof(char),
				"System.DateTime" => typeof(DateTime),
				"System.DBNull" => typeof(DBNull),
				"System.Decimal" => typeof(decimal),
				"System.Double" => typeof(double),
				"System.Int16" => typeof(short),
				"System.Int32" => typeof(int),
				"System.Int64" => typeof(long),
				"System.Object" => typeof(object),
				"System.SByte" => typeof(sbyte),
				"System.Single" => typeof(float),
				"System.String" => typeof(string),
				"System.UInt16" => typeof(ushort),
				"System.UInt32" => typeof(uint),
				"System.UInt64" => typeof(ulong),
				_ => typeof(string),
			};
		}//Fine GetSystemType_From_StringSystemType

        static readonly Byte[] _BB = new byte[] { };
        /// <summary>
        /// Converts a SqlDBtype into a corresponding .net type suitable to store it.
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static System.Type GetSystemType_From_SqlDbType(string sqlDbType) {
            sqlDbType = sqlDbType.ToLower().Trim();

			return sqlDbType switch {
				"bigint" => typeof(long),
				"bit" => typeof(bool),
				"char" => typeof(string),
				"date" => typeof(DateTime),
				"datetime" => typeof(DateTime),
				"decimal" => typeof(decimal),
				"float" => typeof(double),
				"int" => typeof(int),
				"image" => _BB.GetType(),
				"binary" => _BB.GetType(),
				"varbinary" => _BB.GetType(),
				"money" => typeof(decimal),
				"nvarchar" => typeof(string),
				"real" => typeof(float),
				"smalldatetime" => typeof(DateTime),
				"smallint" => typeof(short),
				"text" => typeof(string),
				"timestamp" => _BB.GetType(),
				"tinyint" => typeof(byte),
				"uniqueidentifier" => typeof(Guid),
				"varchar" => typeof(string),
				"variant" => typeof(object),
				_ => typeof(string),
			};
		}




        /// <summary>
        /// Gets a SQL-specific data type for use in an SQL parameter in order to
        ///  store a given dbtype
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static SqlDbType GetSqlType_From_StringSqlDbType(string sqlDbType) {
            sqlDbType = sqlDbType.ToLower().Trim();
            switch (sqlDbType) {
                case "bigint": return SqlDbType.BigInt;
                case "bit": return SqlDbType.Bit;
                case "char": return SqlDbType.Char;
                case "datetime": return SqlDbType.DateTime;
                case "decimal": return SqlDbType.Decimal;
                case "float": return SqlDbType.Float;
                case "int": return SqlDbType.Int;
                case "image": return SqlDbType.Image;
                case "money": return SqlDbType.Money;
                case "binary": return SqlDbType.Binary;
                case "nvarchar": return SqlDbType.NVarChar;
                case "real": return SqlDbType.Real;
                case "smalldatetime": return SqlDbType.SmallDateTime;
                case "date": return SqlDbType.Date;
                case "smallint": return SqlDbType.SmallInt;
                case "text": return SqlDbType.Text;
                case "timestamp": return SqlDbType.Timestamp;
                case "tinyint": return SqlDbType.TinyInt;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                case "varchar": return SqlDbType.VarChar;
                case "varbinary": return SqlDbType.VarBinary;
                case "variant": return SqlDbType.Variant;
                default:
                    ErrorLogger.Logger.MarkEvent("DataAccess: Type " + sqlDbType + " was not found in switch().");
                    return SqlDbType.Text;
            }
        }

       


    }//Fine Classe




    /// <summary>
    /// Connection to database with current user = owner of schema
    /// </summary>
	public class AllLocal_DataAccess :DataAccess {

        /// <summary>
        /// Creates a connection to db with the UserDB being the schema name
        /// </summary>
        /// <param name="DSN"></param>
        /// <param name="Server"></param>
        /// <param name="Database"></param>
        /// <param name="UserDB">This must be the SCHEMA name</param>
        /// <param name="PasswordDB">Password for UserDB</param>
        /// <param name="User">Application user</param>
        /// <param name="Password">Password for application user</param>
        /// <param name="DataContabile"></param>
		public AllLocal_DataAccess(IDbDescriptor descriptor)
            : base(descriptor) {
        }

        

        /// <summary>
        /// Crea un duplicato di un DataAccess, con una nuova connessione allo stesso DB. 
        /// Utile se la connessione deve essere usata in un nuovo thread.
        /// </summary>
        /// <returns></returns>
        public override DataAccess Clone() {
			var da = new AllLocal_DataAccess(Descriptor) {
				Security = Security.Clone() as ISecurity
			};
			return da;
        }


        /// <summary>
        /// Check if tablename must be prefixed with DBO during access
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
		public override  bool IsCommonSchemaTable(string tablename) {
            return false;
        }

        /// <summary>
        /// Check if sp_name must be prefixed with DBO during access
        /// </summary>
        /// <param name="procname"></param>
        /// <returns></returns>
		public override bool IsCommonSchemaProcedure(string procname) {
            return false;
        }

      



    }






}
