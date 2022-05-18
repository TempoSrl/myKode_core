using System;
using System.Data;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using LM = mdl_language.LanguageManager;
using q = mdl.MetaExpression;
using static mdl_utils.tagUtils;
using static mdl.HelpUi;
using System.Threading.Tasks;


namespace mdl {

    /// <summary>
    /// MetaData represents an entity of the system. An entity has a representation in
    ///  the database
    /// </summary>
    public class MetaData : IDisposable, IMetaData {

	  


        static readonly object __o = new object();
    

        /// <summary>
        /// Class for logging errors
        /// </summary>
        public IErrorLogger ErrorLogger { get; set; } = mdl.ErrorLogger.Logger;

      
        /// <summary>
        /// Riga origine della relazione, disponibile solo in afterlink
        /// </summary>
        public DataRow ComingFromRow = null;

        /// <summary>
        /// MiddleRow found in indirect navigations
        /// </summary>
        public DataRow MiddleRow = null;



/// <summary>
/// Connection to database
/// </summary>
[Obsolete("Use form.getInstance<IDataAccess>()")] public DataAccess Conn;

        private ISecurity _security;

        /// <summary>
        /// Security object linked
        /// </summary>
        public ISecurity Security {
            get { return _security ?? conn?.Security; }
            set { _security = value; }
        }


        /// <summary>                                           
        /// Data access linked to the meta
        /// </summary>
        protected internal IDataAccess conn { get; set; }
        

        /// <summary>
        /// There has been an unrecoverable error
        /// </summary>
        public bool ErroreIrrecuperabile = false;

       

        /// <summary>
        /// Helper class for db query
        /// </summary>
        public QueryHelper QHS;

        /// <summary>
        /// Helper class for dataset Query
        /// </summary>
        public CQueryHelper QHC=  MetaFactory.factory.getSingleton<CQueryHelper>();



        /// <summary>
        /// GetData class used by the metadata
        /// </summary>
        public IGetData getData { get; set; }


        /// <summary>
        /// Url used to log client errors
        /// </summary>
        public string ErrorLogUrl;

        /// <summary>
        /// Additionally filter applied on maindosearch  
        /// </summary>
        public string additional_search_condition = "";


        /// <summary>
        /// Used for Form Title
        /// </summary>
        public string Name { get; set; }
     

        /// <summary>
        /// List Type used for maindosearch button in taskbar
        /// </summary>
        public string DefaultListType { get;set; } = "default";


        /// <summary>
        /// Filter calculated by the context menu manager that has opened this form
        /// </summary>
        public string ContextFilter;

        /// <inheritdoc />
        public string contextFilter => ContextFilter; 

        /// <summary>
        /// Key for the hashtable linked to a form in his tag
        /// </summary>
        public const string MetaDataKey = "MetaData";

      
        /// <summary>
        /// List of available ListingTypes
        /// </summary>
        public ArrayList ListingTypes;

        /// <summary>
        /// List of available EditForms
        /// </summary>
        public ArrayList EditTypes;

        /// <summary>
        /// Possible Kinds of a Form
        /// </summary>
        public enum form_types {
            /// <summary>
            /// Form with a standard "save". When user clicks "Save", chanegs are
            ///  written to DB immediately
            /// </summary>
            main,

            /// <summary>
            /// Form showing a single datarow belonging to another form (of "main" kind). 
            ///  When user clicks "Save", changes are not written, but only trasnferred to
            ///  parent Form.
            /// </summary>
            detail,

            /// <summary>                                                                                      
            /// Unused
            /// </summary>
            unknown
        };

        /// <summary>
        /// When set, as the form is activated executes  maindosearch on this filter and then clear this filter
        /// </summary>
        [Obsolete("Use property firstSearchFilter")] 
        public string FirstSearchFilter { get;set;}


        /// <summary>
        /// Returns a GetData class to use with this MetaData
        /// </summary>
        /// <returns></returns>
        public virtual GetData Get_GetData() {
            return MetaFactory.create<IGetData>() as GetData;
        }

        /// <summary>
        /// Returns a PostData class to use with this MetaData
        /// </summary>
        /// <returns></returns>
        public virtual PostData Get_PostData() {
            return new PostData();
        }


		/// <summary>
		/// Name of the table that stores the entity. If SourceRow is present, it is
		///  the same as SourceRow.TableName
		/// </summary>
		public string TableName { get; }


        /// <summary>
		/// Primary DataTable of the MetaData
		/// </summary>
		public DataTable PrimaryDataTable {
            get { return ds?.Tables[TableName]; }
        }

        public DataSet ds { get; set; }


        public IMetaDataDispatcher Dispatcher { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conn">Connection to the DataBase</param>
        /// <param name="dispatcher">Entity dispatcher</param>
        /// <param name="security"></param>
        /// <param name="primaryTable">name of related primary table</param>       
        public MetaData(IDataAccess conn,
            IMetaDataDispatcher dispatcher,
            ISecurity security,
            string primaryTable) {
            this.conn = conn;
            this.Dispatcher = dispatcher as EntityDispatcher;
            
            this.Security = security;
            TableName = primaryTable;
            //			this.PrimaryAliasedName=null;
            Name = primaryTable;
            if (conn != null) {
                QHS = conn.GetQueryHelper();
            }

            Init();

        }

        /// <summary>
        /// 
        /// </summary>
        public MetaData() {
            Name = GetType().Name.Substring(5);
        }

       

        ///// <summary>
        ///// Set the DataTable for which the indexed row has to be monitored. This table is used
        /////  for giving the output row to the parent form when "MainSelect" button is clicked
        ///// </summary>
        ///// <param name="TableName"></param>
        //protected void SetTableToMonitor(string TableName) {
        //    if (myHelpForm != null) {
        //        MetaFactory.factory.getSingleton<IMessageShower>().Show("From " + Name + " SetTableToMonitor must be called before form construction!");
        //        return;
        //    }
        //    TableToMonitor = TableName;
        //}

        


        /// <summary>
        /// Sets some variables initial values
        /// </summary>
        protected void Init() {
            //formState = form_states.setsearch;  now this is in the formPresentation constructor
            //subentity = false;

            ListingTypes = new ArrayList();
            EditTypes = new ArrayList();
            //TableToMonitor = PrimaryTable;
            //form_type = form_types.main;    //Qui ancora non esiste il form Controller. Pertanto non è possibile spostare questa istruzione senza modificare il codice del form, sarebbe una modifica non retrocompatibile
            //formPresentation.DrawState = form_drawstates.building; //formState = form_states.setsearch;  now this is in the formPresentation constructor

            //formPresentation.curroperation = mainoperations.none;
        }

        #region virtual Form GetForm()

      


        /// <summary>
        /// Gets the output of Errors Debug Listeners , only last 4000 chars
        /// </summary>
        /// <returns></returns>
        public static string getOV() {
            string outputview = "";
            foreach (TraceListener tl in Trace.Listeners) {
                //Vede se ha proprietà StringBuilder Errors
                Type myType = tl.GetType();
                FieldInfo mprop = myType.GetField("Errors");
                if (mprop != null) {
                    StringBuilder ssb = (StringBuilder) mprop.GetValue(tl);
                    outputview = "Output View:\r\n" + ssb.ToString() + "\r\n";
                    break;
                }
            }

            if (outputview.Length > 4000) {
                outputview = outputview.Substring(outputview.Length - 4000);
            }

            return outputview;
        }



        public DataRow currentRow {
            get {
                if (PrimaryDataTable == null) return null;
                return PrimaryDataTable._getLastSelected();
            }
        }


        /// <summary>
        /// Sends an error message to the log service
        /// </summary>
        /// <param name="errmsg"></param>
        /// <param name="e"></param>
        public virtual void LogError(string errmsg, Exception e) {
            //mainLogError(this, Conn, errmsg, e);
            ErrorLogger?.logException(errmsg, exception: e, meta: this);

        }

        /// <summary>
        /// Sends an error message to the log service
        /// </summary>
        /// <param name="mess"></param>
        public virtual void LogError(string mess) {
            LogError(mess, null);
        }

  
        #endregion


        #region Gestione Elenchi



        
        /// <summary>
        /// Default limit to row lists
        /// </summary>
        public int ListTop { get; set; } = 1000;


        /// <summary>
        /// Returns the default sorting for a list type
        /// </summary>
        /// <param name="ListingType"></param>
        /// <returns></returns>
        public virtual string GetSorting(string ListingType) {
            return null;
        }

        /// <summary>
        /// Gets the static filter associated to the form
        /// </summary>
        /// <param name="listingType"></param>
        /// <returns></returns>
        public virtual MetaExpression GetStaticFilter(string listingType) {
            return null;
        }

      

        //bool privateLinkedForm;

        /// <summary>
        /// Get the message to display when a list returns no rows
        /// </summary>
        /// <param name="listingtype"></param>
        /// <returns></returns>
        public virtual string GetNoRowFoundMessage(string listingtype) {
            return LM.noObjectFound;
        }

     

   

        /// <summary>
        /// Must return false if the given row can be selected with "mainselect" in the
        ///  form named edit_type. Should also display to user the reason for which 
        ///  row can't be selected.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual bool CanSelect(DataRow r) {
            return Security.CanSelect(r);
        }


        /// <summary>
        /// Must return true if a row can appear in comboboxes, or can be selected while creating an external
        ///  reference to R from another table.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual bool Enabled(DataRow r) {
            return true;
        }

        /// <summary>
        /// Adds a column to a DataTable, assigning also Caption &amp; Expression
        /// </summary>
        /// <param name="T">Table where column must be added</param>
        /// <param name="colName">name of column to add</param>
        /// <param name="type">System Type of COlumn to add. Ex. typeof(int)</param>
        /// <param name="expr">Expression to use for the column. Ex. bilancio.codicebilancio</param>
        /// <param name="Caption">Caption for use in Grid Headings</param>
        protected void AddColumn(DataTable T, string colName, System.Type type, string expr, string Caption) {
            if (T.Columns[colName] != null) return;
            var dc = new DataColumn(colName, type) {Caption = Caption};
            T.Columns.Add(dc);
            dc.SetExpression( expr);
        }

    

        /// <summary>
        ///  Assign the caption (used in grids and lists) for a DataColumn
        /// </summary>
        /// <param name="T"></param>
        /// <param name="colName">Column Name</param>
        /// <param name="caption">Caption wanted for the Column, if empty or starts with ! is hidden</param>
        /// <param name="expression">Expression assigned to the column</param>
        /// <param name="listcolpos">Position of the column in the list, -1 if it has to be hidden</param>
        public static void DescribeAColumn(DataTable T, string colName, string caption,
            int listcolpos, object expression = null) {
            if (T.Columns[colName] == null) return;
            T.Columns[colName].Caption = caption;
            if (expression != null) {
                T.Columns[colName].SetExpression(expression);
            }
            T.Columns[colName].ExtendedProperties["ListColPos"] = listcolpos;
        }

        #endregion

        #region Gestione Form Detail: GetSourceChanges, SetSource


        /// <summary>
        /// Row of CURRENT DataSet mapped to the SourceRow (which belongs to the PARENT DataSet)
        /// </summary>
        public DataRow NewSourceRow { get;set;}


    

        #endregion


        /// <summary>
        /// Returns the name for the related form
        /// </summary>
        /// <returns></returns>
        public virtual string getName() {
            return Name;
        }


    

      

        /// <summary>
        /// MetaModel used by the metadata
        /// </summary>
        public IMetaModel metaModel = MetaFactory.factory.getSingleton<IMetaModel>();


        /// <summary>
        /// Filter applied in the first search
        /// </summary>
        public string firstSearchFilter {
#pragma warning disable CS0618 // Type or member is obsolete
			get { return FirstSearchFilter; }
			set { FirstSearchFilter = value; }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Default listtype 
        /// </summary>
        public string defaultListType {
            get { return DefaultListType; }
            set { DefaultListType = value; }
        }



     
        /// <summary>
        /// Enable HelpDesk form 
        /// </summary>
        public bool helpdeskEnabled = false;

        /// <summary>
        /// Open HelpDesk form
        /// </summary>
        public virtual void doHelpDesk() {

        }





        /// <summary>
        /// Should set the caption of DataTable Columns according to a selected Listing Type
        ///  if a Column Caption is "" or starts with a dot, it is not displayed in grids.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="listingType"></param>
        public virtual async Task DescribeColumns(DataTable T, string listingType) {
            await describeListType(conn, T, listingType);
        }


        /// <summary>
        /// Sets DenyNull, col_len and Format property of Columns
        /// </summary>
        /// <param name="T"></param>
        public virtual async Task DescribeColumns(DataTable T) {
            if (T == null) return;
            //int handle = metaprofiler.StartTimer("DescribeColumns");
            var dbs = await conn.Descriptor.GetStructure(T.TableName,conn);
            foreach (DataRow descCol in dbs.columntypes.Rows) {
                var colname = descCol["field"].ToString();
                if (colname == "") continue;
                var c = T.Columns[colname];
                if (c == null) continue;

                //sets format property
                string format = descCol["format"].ToString();
                if (format != "") c.ExtendedProperties["format"] = format;

                //sets denynull property
                var denynull = descCol["denynull"].ToString().ToLower();
                if (denynull == "s") c.SetDenyNull(true);

                if (
                    c.DataType == typeof(string) &&
                    (
                        (descCol["sqltype"].ToString() == "varchar") ||
                        (descCol["sqltype"].ToString() == "char") ||
                        (descCol["sqltype"].ToString() == "nchar") ||
                        (descCol["sqltype"].ToString() == "nvarchar") ||
                        (descCol["sqltype"].ToString() == "binary") ||
                        (descCol["sqltype"].ToString() == "varbinary")
                    )
                ) {
                    var len = Convert.ToInt32(descCol["col_len"]);
                    if (len > 0) c.SetMaxLen(len);                        
                }
            }

            //metaprofiler.StopTimer(handle);
        }

        /// <summary>
        /// Read listType from db
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="T"></param>
        /// <param name="listtype"></param>
        public static async Task describeListType(IDataAccess Conn, DataTable T, string listtype) {
            //int handle = metaprofiler.StartTimer("DescribeListType");
            var (_,dbs) = await Conn.GetListType( T.tableForReading(), listtype);
            var filter = $"(viewname={mdl_utils.Quoting.quote(listtype, false)})";
            var colsDesc = dbs.customviewcolumn.Select(filter, "colnumber");
            if (colsDesc.Length > 0) {
                foreach (DataColumn c in T.Columns) {
                    c.Caption = "." + c.ColumnName;
                }
            }

            foreach (var desc in colsDesc) {
                //skips empty columns and columns which does not belong to T
                var colname = desc["colname"].ToString();
                if (colname == "") continue;
                var c = T.Columns[colname];
                if (c == null) {
                    if (desc["isreal"].ToString().ToLower() == "s") continue;
                    var st = GetType_Util.GetSystemType_From_StringSystemType(desc["systemtype"].ToString());
                    c = new DataColumn(colname, st);
                    T.Columns.Add(c);
                }

                //evaluates caption 
                var caption = desc["heading"].ToString();
                if (desc["visible"].ToString().ToLower() != "1") caption = "." + caption;
                c.Caption = caption;

                //sets expression
                var expression = desc["expression"].ToString();
                if (expression != "") c.SetExpression( expression);

                //sets temporary flag
                if ((desc["isreal"].ToString().ToLower() == "n") &&
                    (expression == "")) c.SetExpression("");

                //sets format property
                var format = desc["format"].ToString();
                if (format != "") c.ExtendedProperties["format"] = format;

                //sets column position
                if ((desc.Table.Columns["listcolpos"] != null) &&
                    (desc["listcolpos"].ToString() != "")) {
                    c.ExtendedProperties["ListColPos"] = Convert.ToInt32(desc["listcolpos"]);
                }
                else {
                    c.ExtendedProperties["ListColPos"] = desc["colnumber"];
                }
            }
        }




       
        /// <summary>
        /// If ComputeRowsAs() has been called, this is called whenever a DataRow is 
        ///  read or modified.
        /// </summary>
        /// <param name="R">DataRow to which do custom field calculation</param>
        /// <param name="list_type">listing type used for calculation</param>
        public virtual void CalculateFields(DataRow R, string list_type) {
        }

        /// <summary>
        /// Tells if a given DataRow must be displayed in a given list
        /// </summary>
        /// <param name="R">DataRow To Check for a filter condition</param>
        /// <param name="listType">kind of list</param>
        /// <returns>true when Row must be displayed</returns>
        public virtual bool FilterRow(DataRow R, string listType) {
            return true;
        }

        /// <summary>
        /// Tells MetaData Engine to call CalculateFields(R,ListingType) whenever:
        ///  - a row is loaded from DataBase
        ///  - a row is changed in a sub-entity form and modification accepted with mainsave
        /// </summary>
        /// <param name="primary">DataTable to which calculate fields</param>
        /// <param name="listingType">kind of list used for calculation</param>
        public void ComputeRowsAs(DataTable primary, string listingType) {
            if (!ListingTypes.Contains(listingType)) return;
            metaModel.ComputeRowsAs(primary, listingType, CalculateFields);
        }

        /// <summary>
        /// Mark a table to be field-calculated
        /// </summary>
        /// <param name="primary"></param>
        public void FilterRows(DataTable primary) {
            primary.FilterWith(FilterRow);            
        }


        /// <summary>
        /// Used to filter combobox when main table is in insert mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public virtual MetaExpression GetFilterForInsert(DataTable T) {
            return null;
        }

        /// <summary>
        /// Used to filter combobox when main table is in search mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public virtual MetaExpression GetFilterForSearch(DataTable T) {
            return null;
        }

      

        /// <summary>
        /// When true listing types and autoincrement values are taken from db and not from metadata code
        /// </summary>
        public bool ManagedByDB = true;

        /// <summary>
        /// Sets default values for fields. This is necessary when those do not allow 
        ///  null values. Default value are filled into row field whenever a new row is 
        ///  created.
        /// </summary>
        /// <param name="primaryTable">Table for which default values have to be set</param>
        public virtual async Task SetDefaults(DataTable primaryTable, string editType=null) {
            if (ManagedByDB) {
                var dbs = await conn.Descriptor.GetStructure(primaryTable.TableName,conn);
                foreach (DataRow colDesc in dbs.columntypes.Rows) {
                    if (colDesc["defaultvalue"] == DBNull.Value) continue;
                    var fieldname = colDesc["field"].ToString();
                    var defvalstring = colDesc["defaultvalue"].ToString();
                    if (defvalstring == "") continue;
                    defvalstring = Security.Compile(defvalstring, QHC);
                    var systypename = colDesc["systemtype"].ToString();
                    var dummytag = "x.y." + colDesc["format"];
                    var systype = GetType_Util.GetSystemType_From_StringSystemType(systypename);
                    try {
                        var defval = GetObjectFromString(systype, defvalstring, dummytag);
                        if (defval != DBNull.Value) {
                            SetDefault(primaryTable, fieldname, defval);
                        }
                    }
                    catch {
                        var err =
                            $"Error setting default of {primaryTable.TableName}.{fieldname} to value {defvalstring}";
                        ErrorLogger.MarkEvent(err);
                        LogError(err);
                    }
                }
            }

            foreach (DataColumn c in primaryTable.Columns) {
                if (c.AllowDBNull) continue;
                if (c.DefaultValue != DBNull.Value) continue;
                var typename = c.DataType.Name;
                switch (typename) {
                    case "String":
                        SetDefault(primaryTable, c.ColumnName, "");
                        break;
                    case "Char":
                        SetDefault(primaryTable, c.ColumnName, "");
                        break;
                    case "Double":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    case "Decimal":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    case "DateTime":
                        SetDefault(primaryTable, c.ColumnName, EmptyDate());
                        break;
                    case "Int16":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    case "Int32":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    case "Int64":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    case "Byte":
                        SetDefault(primaryTable, c.ColumnName, 0);
                        break;
                    default:
                        SetDefault(primaryTable, c.ColumnName, "");
                        break;
                }
            }

        }


        /// <summary>
        /// Gets a new entity row, adding it to a table T, having ParentRow as Parent
        /// </summary>
        /// <param name="parentRow">Parent Row of the new Row to create, or null if no parent is present</param>
        /// <param name="T">Table in which row has to be added</param>
        /// <returns>new row, child of ParentRow when that is given</returns>
        public virtual async Task<DataRow> GetNewRow(DataTable T, DataRow parentRow=null, string editType=null, string relationName=null) {
            if (conn != null && ManagedByDB) {
                var dbs = await conn.Descriptor.GetStructure(T.TableName,conn);
                foreach (DataRow colDesc in dbs.customtablestructure.Rows) {
                    var fieldname = colDesc["colname"].ToString();
                    var col = T.Columns[fieldname];
                    if (col == null) continue;
                    if (colDesc["autoincrement"].ToString().ToUpper() == "S") {
                        string prefix = null;
                        if (colDesc["prefixfieldname"].ToString() != "") prefix = colDesc["prefixfieldname"].ToString();
                        string middleconst = null;
                        if (colDesc["middleconst"].ToString() != "")
                            middleconst = Security.Compile(colDesc["middleconst"].ToString(), QHC);
                        var length = Convert.ToInt32(colDesc["length"].ToString());
                        var linear = colDesc["linear"].ToString().ToUpper() == "S";
                        col.SetAutoincrement(prefix, middleconst, length, linear);
                    }

                    if (colDesc["selector"].ToString().ToUpper() == "S") {
                        T.SetSelector( fieldname);
                    }
                }
            }

            var r = T.NewRow();
            try {
	            if (parentRow != null) r.MakeChildByRelation(parentRow, relationName:relationName);
                RowChange.CalcTemporaryID(r);
                T.Rows.Add(r);
            }
            catch (Exception e) {
                ErrorLogger.markException(e,$"Get_New_Row({T.TableName}) on meta {TableName}");
                LogError($"GetNewRow ({T.TableName}): Error {mdl.ErrorLogger.GetErrorString(e)}");
                throw;
            }

            return r;
        }



     

        static readonly object _ispainting = new object();



        #region Link/Attivazione Form

        /// <summary>
        /// True if linked GetData class is owned by the MetaData
        /// </summary>
        public bool GetDataIsPrivate { get; set; }


        #endregion




        /// <summary>
        /// Gets a row (Output) knowing that it has been read via a certain list type.
        /// Output row is assumed to belong to primary table. Input row can belong
        ///  to anything
        /// </summary>
        /// <param name="input"></param>
        /// <param name="list"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual bool GetRowFromList(DataRow input, string list, DataRow output) {
            if (input.Table.TableName != TableName) return false;
            output.BeginEdit();
            foreach (DataColumn c in output.Table.Columns) {
                if (c.IsTemporary()) continue;
                if (input.Table.Columns.Contains(c.ColumnName))
                    output[c.ColumnName] = input[c.ColumnName];
            }

            output.EndEdit();
            return true;
        }



        /// <summary>
        /// Sets the default value (used when a NEW row is created for teh table)
        /// </summary>
        /// <param name="primaryTable">Table to which field belongs</param>
        /// <param name="field">field name</param>
        /// <param name="o">default value wanted</param>
        public static void SetDefault(DataTable primaryTable, string field, Object o) {
            if (primaryTable == null) return;
            if (!primaryTable.Columns.Contains(field)) return;
            if (o == null) o = DBNull.Value;
            if (o.GetType() == primaryTable.Columns[field].DataType || o == DBNull.Value) {
                primaryTable.Columns[field].DefaultValue = o;
                return;
            }

            primaryTable.Columns[field].DefaultValue =
                GetObjectFromString(primaryTable.Columns[field].DataType, o.ToString(), "x.y");
        }

      



       

        /// <summary>
        /// Broadcast delegate kind
        /// </summary>
        /// <param name="sender">Sender of the broadcast</param>
        /// <param name="message">Message sent</param>
        public delegate void BroadCastHandler(object sender, object message);

        /// <summary>
        /// handler used to register to broadcasts messages
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event BroadCastHandler messageBroadcaster;



        /// <summary>
        /// Sends a broadcast message from a sender. This can be intercepted  by another form registered to messageBroadcaster 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public static void sendBroadcast(object sender, object message) {
            var handler = messageBroadcaster;
            if (handler != null) {
                try {
                    handler(sender, message);
                }
                catch {
                    //ignore
                }
            }
        }

        public DataRow lastSelectedRow { get; set; }

        /// <summary>
        /// It's called before accepting changes to an entity, and must state whether the entity is valid.
        /// </summary>
        /// <param name="r">DataRow to test</param>
        /// <param name="errmess">error message to display, null if no problem</param>
        /// <param name="errfield">wrong field or null if no problem</param>
        /// <returns>true when entity is valid</returns>
        public virtual bool IsValid(DataRow r, out string errmess, out string errfield) {
            string emptyKeyMsg = LM.errorEmptyKey;
            string emptyFieldMsg = LM.errorEmptyField;
            string stringTooLong = LM.stringTooLong;
            string invalidDate = LM.invalidDate;

            //			if (form_state==form_states.setsearch) {
            //				errmess="No data to validate";
            //				errfield=null;
            //				return false;
            //			}

            //DataRow R = FormDataSet.Tables[TableName].Rows[0];
            //DataRow R = HelpForm.GetLastSelected(PrimaryDataTable);
            if (r == null) {
                errmess = "No data selected";
                errfield = null;
                return false;
            }

            foreach (DataColumn c in r.Table.PrimaryKey) {
                string colname = c.ColumnName;
                if (c.Caption != "" && c.Caption != colname) colname = c.Caption;

                if ((r[c] == null) || (r[c] == DBNull.Value)) {
                    errfield = c.ColumnName;
                    errmess = emptyKeyMsg + "(" + colname + ")";
                    return false;
                }

                if ((r[c].GetType().Name == "DateTime") &&
                    (r[c].Equals(EmptyDate()))) {
                    errfield = c.ColumnName;
                    errmess = emptyKeyMsg + " (" + colname + ")";
                    return false;
                }

                if ((r[c].GetType().Name == "String") &&
                    (r[c].ToString().TrimEnd() == "")) {
                    errfield = c.ColumnName;
                    errmess = emptyKeyMsg + " (" + colname + ")";
                    return false;
                }

                if (!c.ExtendedProperties.ContainsKey("allowZero")) {
                    if ((r[c].GetType().Name == "Int16") &&
                        (r[c].ToString() == "0")) {
                        errfield = c.ColumnName;
                        errmess = emptyKeyMsg + " (" + colname + ")";
                        return false;
                    }

                    if ((r[c].GetType().Name == "Int32") &&
                        (r[c].ToString() == "0")) {
                        errfield = c.ColumnName;
                        errmess = emptyKeyMsg + " (" + colname + ")";
                        return false;
                    }

                    if ((r[c].GetType().Name == "Int64") &&
                        (r[c].ToString() == "0")) {
                        errfield = c.ColumnName;
                        errmess = emptyKeyMsg + " (" + colname + ")";
                        return false;
                    }
                }
            }

            foreach (DataColumn C2 in r.Table.Columns) {
                string colname = C2.ColumnName;
                if (C2.Caption != "" && C2.Caption != colname) colname = C2.Caption;

                if (r[C2].GetType().Name == "String") {
                    int thislen = r[C2].ToString().Length;
                    int maxlen = C2.GetMaxLen();
                    if (maxlen > 0 && thislen > maxlen) {
                        errfield = C2.ColumnName;
                        errmess = stringTooLong + "(" + colname + ")";
                        return false;
                    }
                }

                if (C2.AllowDBNull && !C2.IsDenyNull()) continue;
                if ((r[C2] == null) || (r[C2] == DBNull.Value)) {
                    errfield = C2.ColumnName;
                    errmess = emptyFieldMsg + "(" + colname + ")";
                    return false;
                }

                if (r[C2].GetType().Name == "DateTime") {
                    if (r[C2].Equals(EmptyDate())) {
                        errfield = C2.ColumnName;
                        errmess = emptyFieldMsg + "(" + colname + ")";
                        return false;
                    }
                }
                if (r[C2].GetType().Name == "DateTime") {
                    DateTime d = (DateTime) r[C2];
                    if (d.Year < 1000) {
                        errfield = C2.ColumnName;
                        errmess = invalidDate + "(" + colname + ")";
                        return false;
                    }
                    
                }

                if ((r[C2].GetType().Name == "String") &&
                    (r[C2].ToString().TrimEnd() == "")) {
                    errfield = C2.ColumnName;
                    errmess = emptyFieldMsg + " (" + errfield + ")";
                    return false;
                }

                if (!C2.ExtendedProperties.ContainsKey("allowZero")) {
                    if ((r[C2].GetType().Name == "Int32") && C2.IsDenyZero() &&
                        (r[C2].ToString() == "0")) {
                        errfield = C2.ColumnName;
                        errmess = emptyFieldMsg + " (" + colname + ")";
                        return false;
                    }

                    if ((r[C2].GetType().Name == "Int16") && C2.IsDenyZero() &&
                        (r[C2].ToString() == "0")) {
                        errfield = C2.ColumnName;
                        errmess = emptyFieldMsg + " (" + colname + ")";
                        return false;
                    }

                    if ((r[C2].GetType().Name == "Int64") && C2.IsDenyZero() &&
                        (r[C2].ToString() == "0")) {
                        errfield = C2.ColumnName;
                        errmess = emptyFieldMsg + " (" + colname + ")";
                        return false;
                    }
                }
            }


            errmess = null;
            errfield = null;
            return true;
        }

      


        /// <summary>
        /// Returns true if RSource has a corresponding row in DataSet Destination (with all child rows)
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="rif"></param>
        /// <param name="rSource"></param>
        /// <returns></returns>
        bool xCompChilds(DataSet dest, DataSet rif, DataRow rSource) {
            var T = rSource.Table;
            if (!compDataRow(dest.Tables[rSource.Table.TableName], rSource)) return false;

            foreach (DataRelation rel in T.ChildRelations) {
                if (!dest.Tables.Contains(rel.ChildTable.TableName)) {
                    foreach (DataRow rr in rif.Tables[rel.ChildTable.TableName].Rows) {
                        if (rr.RowState != DataRowState.Unchanged) return false;
                    }

                    continue;
                }

                if (!GetData.CheckChildRel(rel)) continue; //not a subentityrel
                var childTable = rif.Tables[rel.ChildTable.TableName];
                foreach (DataRow child in childTable.Rows) {
                    if (!xCompChilds(dest, rif, child)) return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Returns true Dest table has a DataRow equal to Sample
        /// </summary>
        /// <param name="destTable"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        bool compDataRow(DataTable destTable, System.Data.DataRow sample) {
            if (sample.RowState == DataRowState.Deleted) {
                var filterdel = QHC.CmpKey(sample);
                return destTable.Select(filterdel, null, DataViewRowState.Deleted).Length > 0;
            }

            //var filter = QHC.CmpKey(sample);
            var r = destTable.filter(q.keyCmp(sample)).FirstOrDefault();//.Select(filter);
            if (r==null) return false;
            
            foreach (DataColumn cc in destTable.Columns) {
                if (cc.IsTemporary()) continue;
                if (!sample.Table.Columns.Contains(cc.ColumnName)) continue;
                if (r[cc.ColumnName].Equals(sample[cc.ColumnName])) continue;
                return false;
            }

            return true;
        }


    

        /// <summary>
        /// Should Copy a column from Source to Dest, can be inherited to skip or modify the copy 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public virtual void InsertCopyColumn(DataColumn c, DataRow source, DataRow dest) {
            dest[c.ColumnName] = source[c.ColumnName];
        }

        /// <summary>
        /// invokes InsertCopyColumn 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public void ExtCopyColumn(DataColumn c, DataRow source, DataRow dest) {
            InsertCopyColumn(c, source, dest);
        }

   

        /// <summary>
        /// Name of Notes field
        /// </summary>
        public string NotesFieldName = "notes";

        /// <summary>
        /// Name of OleNotes field
        /// </summary>
        public string OleNotesFieldName = "olenotes";



        /// <summary>
        /// Returns true if a "notes" field is available for the
        ///  current row
        /// </summary>
        /// <returns></returns>
        public virtual bool HasOleNotes() {
            if (PrimaryDataTable == null) return false;
            var colOleNotes = PrimaryDataTable.Columns[OleNotesFieldName];
            if (colOleNotes == null) return false;
            var bb = new byte[] { };
            return colOleNotes.DataType == bb.GetType();
        }

        /// <summary>
        /// Returns true if a "notes" field is available for the
        ///  current row
        /// </summary>
        /// <returns></returns>
        public virtual bool HasNotes() {
            if (PrimaryDataTable == null) return false;           
            var colNotes = PrimaryDataTable.Columns[NotesFieldName];
            if (colNotes == null) return false;
            return colNotes.DataType == typeof(string);
        }

        /// <summary>
        /// True if there are Notes or OleNotes availabe for current row
        /// </summary>
        /// <returns></returns>
        public virtual bool NotesAvailable(DataRow R) {
            if (R == null) return false;
            if (HasNotes()) {
                if (GetNotes(R) != "") return true;
            }

            if (HasOleNotes()) {
                var n = GetOleNotes(R);
                if (n.Length > 132) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the "notes" field related to current row
        /// </summary>
        /// <returns></returns>
        public virtual string GetNotes(DataRow R) {
            if (!HasNotes()) return null;
            if (R == null) return null;
            return R[NotesFieldName].ToString();
        }

        /// <summary>
        /// Set the "notes" field of current row
        /// </summary>
        /// <param name="notes"></param>
        public virtual void SetNotes(DataRow R, string notes) {
            if (!HasNotes()) return;            
            if ((notes == null) || (notes == ""))
                R[NotesFieldName] = DBNull.Value;
            else
                R[NotesFieldName] = notes;
        }


       

        /// <summary>
        /// Gets the "notes" field related to current row
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetOleNotes(DataRow R) {
            if (!HasOleNotes()) return null;           
            if (R[OleNotesFieldName] == DBNull.Value) return new byte[] { };
            return (byte[])R[OleNotesFieldName];
        }

        /// <summary>
        /// Set the "notes" field of current row
        /// </summary>
        /// <param name="oleNotes"></param>
        public virtual void SetOleNotes(DataRow R, byte[] oleNotes) {
            if (!HasOleNotes()) return;
            if (R == null) return;
            if ((R.RowState == DataRowState.Deleted) ||
                (R.RowState == DataRowState.Detached)) return;
            if ((oleNotes == null) || (oleNotes.Length <= 132)) {
                R[OleNotesFieldName] = DBNull.Value;
            }
            else {
                R[OleNotesFieldName] = oleNotes;
            }
        }


        static readonly string[] _textBoxEventsToClear = {
            "GotFocus", "LostFocus", "Enter", "Leave",
            "TextChanged", "ReadOnlyChanged", "EnableChanged"
        };

        static readonly string[] comboBoxEventsToClear = {"EnableChanged"};


       
        /// <summary>
        /// When true, this MetaData has been disposed, should not be used anymore
        /// </summary>
        public bool destroyed = false;


        /// <summary>
        /// Destroy and unlink this MetaData from anything
        /// </summary>
        public void Destroy() {
            if (destroyed) return;
            destroyed = true;

            
            conn = null;

            if (!GetDataIsPrivate) {
                getData = null;
            }

            if (getData != null) {
                getData.Destroy();
                getData = null;
            }


            if (ListingTypes != null) {
                ListingTypes.Clear();
                ListingTypes = null;
            }

            
            if (EditTypes != null) {
                EditTypes.Clear();
                EditTypes = null;
            }

          
            Dispatcher = null;
            

        
            QHC = null;
            QHS = null;
    
            _security = null;
            ErrorLogger = null;
            metaModel = null;

        }

       

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls



        /// <summary>
        /// Do dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    //// Dispose managed state (managed objects).
                    //if (LinkedForm != null) {
                    //    myHelpForm.Dispose();
                    //    myHelpForm = null;
                    //}
                    Destroy();
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MetaData() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Gets the primary key fields of the main table
        /// </summary>
        /// <returns></returns>
        public virtual string[] PrimaryKey() {
            //if (PrimaryDataTable?.PrimaryKey.Length > 0) {
            //    return (from s in PrimaryDataTable.PrimaryKey.ToArray() select s.ColumnName).ToArray();
            //}

            return null;
        }

    }



    /// <summary>
    /// Helper class for DataTables
    /// </summary>
    public static class DataSetHelperClass {
        /// <summary>
        /// Gets the first row of a DataTable if any is present
        /// </summary>
        /// <param name="t"></param>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="rv"></param>
        /// <returns></returns>
        public static DataRow First(this DataTable t, string filter = null, string sort = null,
            DataViewRowState rv = DataViewRowState.CurrentRows) {
            if (t == null) return null;
            if (filter == null && sort == null && rv == DataViewRowState.CurrentRows) {
                if (t.Rows.Count == 0) return null;
                return t.Rows[0];
            }

            return t.Select(filter, sort, rv).FirstOrDefault();
        }
    }


}

  

