using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using q = mdl.MetaExpression;
//#pragma warning disable IDE1006 // Naming Styles
//using System.EnterpriseServices;
using static mdl.Metaprofiler;

namespace mdl {

	/// <summary>
	/// Represents the change of a single row that has to be performed on a 
	///  Database Table
	/// </summary>
	public class RowChange {

        /// <summary>
        /// Field name indicating the user that created the row
        /// </summary>
        public virtual string CreateUserField { get; set; }="cu";

        /// <summary>
        /// Field name indicating the creation time stamp
        /// </summary>
        public virtual string CreateTimeStampField { get; set; }="ct";

        /// <summary>
        /// Field name indicating the last user that modified the row, used for optimistic locking
        /// </summary>
        public virtual string LastModifyUserField { get; set; }="lu";


        /// <summary>
        /// Field name indicating the last update time stamp, used for optimistic locking
        /// </summary>
        public virtual string LastModifyStampField { get; set; }="lt";

        /// <summary>
        /// DataRow linked to the RowChange
        /// </summary>
        public DataRow DR;       



		/// <summary>
		/// Extended Property of DataColumn that states that the column has to be
		///  calculated during Post Process when it is added to DataBase.
		/// </summary>
		/// <remarks>
		/// The field is calculated like:
		/// [Row[PrefixField]] [MiddleConst] [LeftPad(newID, IDLength)]
		/// so that, called the first part [Row[PrefixField]] [MiddleConst] as PREFIX,
		/// if does not exists another row with the same PREFIX for the ID, the newID=1
		/// else newID = max(ID of same PREFIX-ed rows) + 1
		/// </remarks>
		const string AutoIncrement = "IsAutoIncrement";
		const string CustomAutoIncrement = "CustomAutoIncrement";
		const string PrefixField = "PrefixField";
		const string MiddleConst = "MiddleConst";
		const string IDLength    = "IDLength";
		const string Selector    = "Selector";
        const string SelectorMask = "SelectorMask";
        const string MySelector = "MySelector";
        const string MySelectorMask = "MySelectorMask";
        const string LinearField = "LinearField";
        
        /// <summary>
        /// Constructor for the class
        /// </summary>
        /// <param name="dr"></param>
        public RowChange(DataRow dr) {
            this.DR = dr;
            EnforcementMessages = null; //new ProcedureMessageCollection();
        }
        
        /// <summary>
        /// Assigns this RowChange to a Collection
        /// </summary>
        /// <param name="rc"></param>
        public void SetCollection(RowChangeCollection rc){
            myCollection=rc;        
        }
        RowChangeCollection myCollection = null;
		/// <summary>
		/// Creates a new RowChange linked to a given DataRow
		/// </summary>
		/// <param name="dr"></param>
        /// <param name="parentCollection"></param>
		public RowChange(DataRow dr, RowChangeCollection parentCollection){
			this.DR=dr;
			EnforcementMessages= null; //new ProcedureMessageCollection();
            myCollection = parentCollection;		    
		}

        /// <summary>
        /// List of tables incrementally scanned in the analysis
        /// </summary>
        public List<string> HasBeenScanned = new List<string>();


		/// <summary>
		/// String representaion of the change
		/// </summary>
		/// <returns></returns>
		public override String ToString() {
			return TableName+"."+DR.ToString();
		}    

		/// <summary>
		/// gets the DataTable that owns the chenged row
		/// </summary>
		public DataTable Table {
			get {
				return DR.Table;
			}
		}

		/// <summary>
		/// Gets the name of the table to which the changed row belongs
		/// </summary>
		public string TableName {
			get {
				return DR.Table.TableName;
			}
		}            

		/// <summary>
		/// Gets the real table that will be used to write the row to the DB
		/// </summary>
		public string PostingTable {
			get {
				return DR.Table.tableForPosting();
			}
		}
    
      

		/// <summary>
		/// Short description for update, used for composing business rule 
		///  stored procedure names
		/// </summary>
		public const string short_update_descr = "u";
		/// <summary>
		/// Short description for insert, used for composing business rule 
		///  stored procedure names
		/// </summary>
		public const string short_insert_descr = "i";
		/// <summary>
		/// Short description for delete, used for composing business rule 
		///  stored procedure names
		/// </summary>
		public const string short_delete_descr = "d";
		/// <summary>
		/// Used in the composition of the key, during logging
		/// </summary>
		public const string KeyDelimiter = " | ";

		/// <summary>
		/// Gets a i/u/d description of the row status
		/// </summary>
		/// <returns></returns>
		public virtual string ShortStatus(){
			String Op="";
			switch(DR.RowState){
				case DataRowState.Added: 
					Op = short_insert_descr;//"i"
					break;
				case DataRowState.Deleted:
					Op = short_delete_descr;//"d"
					break;
				case DataRowState.Modified:
					Op= short_update_descr;//"u"
					break;
			}
			return Op;

		}

		/// <summary>
		/// Get the name of Stored procedure to call in pre-check phase
		/// </summary>
		/// <returns></returns>
		public virtual String PreProcNameToCall(){
			return $"sp_{ShortStatus()}_{PostingTable}";
		}
		/// <summary>
		/// get the name of Stored procedure to call in post-check phase
		/// </summary>
		/// <returns></returns>
		public virtual String PostProcNameToCall(){
			return $"sp_{ShortStatus()}__{PostingTable}";
		}
        
		/// <summary>Gets a filter of TableName AND dboperation (I/U/D)</summary>
		/// <returns>filter String</returns>
		public virtual MetaExpression FilterTableOp(){
			return q.eq("dbtable",TableName) & q.and("dboperation",ShortStatus());                
		}
 
		/// <summary>
		/// Gets a filter on Posting Table and DB operation 
		/// </summary>
		/// <returns></returns>
		public virtual MetaExpression FilterPostTableOp(){
            return q.eq("dbtable", PostingTable) & q.and("dboperation", ShortStatus());
		}

		/// <summary>
		/// Error messages about related stored procedures
		/// </summary>
		public ProcedureMessageCollection EnforcementMessages;

		/// <summary>
		/// Related rows on other tables
		/// </summary>
        public Dictionary<string, DataRow> Related = new Dictionary<string, DataRow>(); //ex SortedList

		/// <summary>
		/// Get a new rowchange class linked to a given DataRow
		/// </summary>
		/// <param name="R"></param>
		/// <returns></returns>
		virtual protected RowChange getNewRowChange(DataRow R){
			return new RowChange(R);
		}

        
		/// <summary>
		/// Gets the list of primary key column name separated by KeyDelimiter
		/// </summary>
		/// <returns></returns>
		public string PrimaryKey(){
			string Key = "";
			bool first = true;
			var DV = DataRowVersion.Default;
			if (DR.RowState== DataRowState.Deleted) DV = DataRowVersion.Original;
			foreach (var C in DR.Table.PrimaryKey){
				if (!first) Key += KeyDelimiter;
				Key += DR[C.ColumnName, DV];
				first=false;
			}
			return Key;
		}



		/// <summary>
		/// Copy an extended property of a datacolumn into another one.
		/// Checks for column existence in both tables.
		/// </summary>
		/// <param name="In"></param>
		/// <param name="Out"></param>
		/// <param name="colname"></param>
		/// <param name="property"></param>
		static void copyproperty(DataTable In, DataTable Out, string colname, string property){
			if (In.Columns[colname]==null) return;
			if (Out.Columns[colname]==null) return;
			if (In.Columns[colname].ExtendedProperties[property]==null) return;

			Out.Columns[colname].ExtendedProperties[property]=
				In.Columns[colname].ExtendedProperties[property];
		}

	    /// <summary>
	    /// Class for logging errors
	    /// </summary>
	    public IErrorLogger ErrorLogger { get; set; } = mdl.ErrorLogger.Logger;

        /// <summary>
        /// Get a DataRow related to the RowChange, in a given tablename
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public DataRow GetRelated(string tablename) {
            string realname = DR.Table.tableForPosting();
            if (realname == tablename) return DR;                    
            if (Related.ContainsKey(tablename)) return Related[tablename];
            SearchRelated(tablename);
            if (Related.ContainsKey(tablename)) return Related[tablename];
            return null;
        }

        public bool useIndex=true;

        /// <summary> 
        /// Search for rows related to the "Change" variation (referred to ONE row R of ONE table T)
        ///  For each table in DS, are examined the relation to T
        ///  For each table having exactly ONE row related to R, this row is added to Related.
        ///  Rows are added to a list that is indexed by the name of the row DataTable 
        /// </summary>
        /// <param name="tablename"></param>   
        /// <remarks>
        ///   This Function assumes master/child relations to be NOT circular.
        ///   This function does not consider the possibility of raising different rows 
        ///     in the same table using more than one path. It assumes that there is
        ///     only one path connecting one table to another.
        /// </remarks>
        public void SearchRelated(string tablename) {
            if (HasBeenScanned.Contains(tablename)) return;
            HasBeenScanned.Add(tablename);

            DataRow ROW = this.DR;
            DataTable T = ROW.Table;
            
            //Tryes to get rows related to ROW.
            DataRowVersion toConsider = DataRowVersion.Default;
            if (ROW.RowState == DataRowState.Deleted) toConsider = DataRowVersion.Original;

            //Scans relations where T is the parent and T2 s the CHILD
            //We want to find rows in Rel.ChildTable
            foreach (DataRelation rel in T.ChildRelations) {
                //Here ROW is PARENT TABLE and we search in Rel.ChildTable, T2 is CHILD TABLE
                DataTable childTable = rel.ChildTable;  //table to search in
                if (childTable.tableForPosting() !=tablename) continue;
                try {
                    if (rel.ParentTable.TableName != T.TableName) continue;
                    var rr = childTable.filter(q.mGetChilds(ROW, rel, toConsider));  
                    if (rr.Length != 1) continue;
                    Related[tablename] = rr[0];
                    return;
                }
                catch (Exception e) {
                    ErrorLogger.logException("Errore in SEARCH_RELATED (1)(" + tablename + ")",exception:e);
                    ErrorLogger.markException(e,"Error in RowChange.SearchRelated.");                  
                }
            }//foreach DataRelation

            //Scans relations where T is the CHILD
            //We want to find rows in Rel.ParentTable
            foreach (DataRelation rel in T.ParentRelations) {
                //Here ROW is CHILD TABLE and we search in Rel.ParentTable
                DataTable parentTable = rel.ParentTable;
                if (parentTable.tableForPosting() != tablename) continue;
                try {
                    if (rel.ChildTable.TableName != T.TableName) continue;
                    //string whereclause = QueryCreator.WHERE_REL_CLAUSE(ROW, rel.ChildColumns,rel.ParentColumns, toConsider, false);
                    //var rr = parentTable.Select(whereclause, null, DataViewRowState.CurrentRows);
                    var rr = parentTable.filter(q.mGetParents(ROW, rel, toConsider)); 
                    if (rr.Length != 1) continue;
                    Related[tablename] = rr[0];
                    return;
                }
                catch (Exception e) {
                    ErrorLogger.logException("Errore in SEARCH_RELATED (2)(" + tablename + ")",e); 
                    ErrorLogger.markException(e,"Error in RowChange.SearchRelated.");
                }
            }//foreach DataRelation

        }

	 


        #region AUTOINCREMENT FIELD GET/SET

        /// <summary>
        /// Sets selector for a specified DataColumn
        /// </summary>
        /// <param name="C"></param>
        /// <param name="ColumnName"></param>
        /// <param name="mask"></param>
        internal static void setMySelector(DataColumn C,string ColumnName, ulong mask) {
            var amask = C.ExtendedProperties[MySelectorMask] as string;
            if (!(C.ExtendedProperties[MySelector] is string sel)) {
                sel = ColumnName;
                amask = mask.ToString();
                C.ExtendedProperties[MySelector] = sel;
                C.ExtendedProperties[MySelectorMask] = amask;
                return;
            }
            if (sel == ColumnName)
                return;
            if (sel.StartsWith(ColumnName+","))
                return;
            if (sel.EndsWith("," + ColumnName))
                return;
            if (sel.Contains(","+ColumnName+",")) return;
            sel = sel + "," + ColumnName;
            amask = amask + "," + mask.ToString();

            C.ExtendedProperties[MySelector] = sel;
            C.ExtendedProperties[MySelectorMask] = amask;
        }


		/// <summary>
		/// Add a selector-column to the table. AutoIncrement columns are calculated between
		///  equal selectors-column rows
		/// </summary>
		/// <param name="T"></param>
		/// <param name="ColumnName"></param>
		internal static void setSelector(DataTable T, string ColumnName){
			DataColumn C = T.Columns[ColumnName];
			C.ExtendedProperties[Selector]="y";
            C.ExtendedProperties[SelectorMask] = null;
		}

        /// <summary>
        /// Mark a column  as a general selector for a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="ColumnName"></param>
        /// <param name="mask"></param>
        internal static void setSelector(DataTable T, string ColumnName, UInt64 mask) {
            DataColumn C = T.Columns[ColumnName];
            C.ExtendedProperties[Selector] = "y";
            C.ExtendedProperties[SelectorMask] = mask;
        }


        /// <summary>
        /// Remove a column from general selectors of a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="ColumnName"></param>
		internal static void clearSelector(DataTable T, string ColumnName){
			DataColumn C = T.Columns[ColumnName];
			C.ExtendedProperties[Selector]=null;
            C.ExtendedProperties[SelectorMask] = null;
        }

        /// <summary>
        /// Remove all columns as specific selector ofa a column
        /// </summary>
        /// <param name="T"></param>
        /// <param name="ColumnName"></param>
        internal static void clearMySelector(DataTable T, string ColumnName) {
            DataColumn C = T.Columns[ColumnName];
            C.ExtendedProperties[MySelector] = null;
            C.ExtendedProperties[MySelectorMask] = null;
        }


        /// <summary>
        /// Gets all field selector for a datacolumn
        /// </summary>
        /// <param name="Col"></param>
        /// <returns></returns>
        internal static List<DataColumn> getSelectors(DataColumn Col) {
            var res = new List<DataColumn>();
            var T = Col.Table;
            foreach (DataColumn C in T.Columns) {
                if (C.ExtendedProperties[Selector] != null) {
                    res.Add(C);
                }
            }
            if(!(Col.ExtendedProperties[MySelector] is string mysel)) return res;

            string[] fname = mysel.Split(',');            
            for (int i = 0; i < fname.Length; i++) {
                res.Add(T.Columns[fname[i]]);
            }
            return res;
        }
		
		/// <summary>
		/// Gets a condition of all selector fields of a row
		/// </summary>
        /// <param name="DR"></param>
        /// <param name="CC"></param>
        /// <param name="QH"></param>
		/// <returns></returns>
		internal static MetaExpression getSelector( DataRow DR, DataColumn CC){            
            DataTable T = DR.Table;
			var clauses = new List<MetaExpression>(); //string sel=""
            foreach (DataColumn C in T.Columns) {              
                if (C.ExtendedProperties[Selector] != null) {
                    if (C.ExtendedProperties[SelectorMask] != null) {
                        if (DR[C.ColumnName] == DBNull.Value) {
                            //sel = QH.AppAnd(sel, QH.IsNull(C.ColumnName)); 
                            clauses.Append(q.isNull(C.ColumnName));
                            continue;
                        }
                        var mask = (ulong) C.ExtendedProperties[SelectorMask] ;
                        var val = Convert.ToUInt64( DR[C.ColumnName]);
                        clauses.Append(q.cmpMask(C.ColumnName,mask,val)); //sel = QH.AppAnd(sel,QH.CmpMask(C.ColumnName,mask,val));
                    }
                    else {
                        clauses.Append(q.eq(C.ColumnName, DR[C.ColumnName]));//sel = QH.AppAnd(sel,QH.CmpEq(C.ColumnName,DR[C.ColumnName]));
                    }
                }
            }
            //Now gets the SPECIFIC selector
            if(!(CC.ExtendedProperties[MySelector] is string mysel)) return q.and(clauses);
            string []fname = mysel.Split(',');
            string[] fmask = CC.ExtendedProperties[MySelectorMask].ToString().Split(',');            
            return q.and(fname.Zip(fmask,
				                (col,mask) => (DR[col] == DBNull.Value)? 
                                                          q.isNull(col): 
                                                          q.cmpMask(col, Convert.ToUInt64(mask), Convert.ToUInt64(DR[col])
                                )));            
		}

        /// <summary>
        /// Gets an hash of all selector fields of a row (ex GetSelector)
        /// </summary>
        /// <param name="T">Table row</param>
        /// <param name="DR"></param>
        /// <returns></returns>
        public static string GetHashSelectors(DataTable T, DataRow DR) {
            var allSelectors =  new HashSet<string> ();
            foreach (DataColumn C in T.Columns) {
                if (C.ExtendedProperties[Selector] != null) { //C is a global selector
                    allSelectors.Add(C.ColumnName);                    
                }
                if(!(C.ExtendedProperties[MySelector] is string mysel)) continue;
                string[] fname = mysel.Split(',');
                string[] fmask = C.ExtendedProperties[MySelectorMask].ToString().Split(',');
                foreach(string f in fname) {
                    allSelectors.Add(f);
                }
            }
            var orderedSel = allSelectors.ToArray().OrderBy(n=>n).ToArray();
            return String.Join(",", (from o in orderedSel select $"{o}:{DR[o]}"));

        }



		/// <summary>
		/// Copy all autoincrement properties of a table into another one.
		/// </summary>
		/// <param name="In"></param>
		/// <param name="Out"></param>
		internal static void copyAutoIncrementProperties(DataTable In, DataTable Out){
			foreach(DataColumn C in Out.Columns){
				foreach (string prop in new string [] { 
														  AutoIncrement, CustomAutoIncrement, 
														   PrefixField, MiddleConst, IDLength,
														  Selector, SelectorMask, MySelector, MySelectorMask,
                    LinearField}){
					copyproperty(In, Out, C.ColumnName, prop);
				}
			}
		}


	

		/// <summary>
		/// Set a DataColumn as "AutoIncrement", specifying how the calculated ID must be
		///  composed.
		/// </summary>
		/// <param name="C">Column to set</param>
		/// <param name="prefix">field of rows to be put in front of ID</param>
		/// <param name="middle">middle constant part of ID</param>
		/// <param name="length">length of the variable part of the ID</param>
		/// <param name="linear">if true, Selector Fields, Middle Const and Prefix 
		///		fields are not taken into account while calculating the field</param>
		/// <remarks>
		/// The field will be calculated like:
		/// [Row[PrefixField]] [MiddleConst] [LeftPad(newID, IDLength)]
		/// so that, called the first part [Row[PrefixField]] [MiddleConst] as PREFIX,
		/// if does not exists another row with the same PREFIX for the ID, the newID=1
		/// else newID = max(ID of same PREFIX-ed rows) + 1
		/// </remarks>
		static internal void markAsAutoincrement(DataColumn C, 
					string prefix, 
					string middle,
					int length, 
					bool linear=false){
			if (C==null){				
				mdl.ErrorLogger.Logger.MarkEvent("Cant mark autoincrement a null Column");
				return;
			}
			C.ExtendedProperties[AutoIncrement]="s";
			C.ExtendedProperties[PrefixField]=prefix;
			C.ExtendedProperties[MiddleConst]=middle;
			C.ExtendedProperties[IDLength]=length;
			if (linear) C.ExtendedProperties[LinearField]="1";
		}

		/// <summary>
		/// Removes autoincrement property from a DataColumn
		/// </summary>
		/// <param name="C"></param>
		static internal void clearAutoIncrement(DataColumn C){
			C.ExtendedProperties[AutoIncrement]=null;
		}

		/// <summary>
		/// Tells whether a Column is a AutoIncrement 
		/// </summary>
		/// <param name="C"></param>
		/// <returns>true if Column is Auto Increment</returns>
		static internal bool isAutoIncrement(DataColumn C){
			if (C.ExtendedProperties[AutoIncrement]!=null) return true; 
			return false;
		}

        /// <summary>
        /// Tells PostData to evaluate a specified column through the specified customFunction
        /// </summary>
        /// <param name="C">Column to evaluate</param>
        /// <param name="CustomFunction">delegate to call for evaluating autoincrement column</param>
        static internal void markAsCustomAutoincrement(DataColumn C, CustomCalcAutoId CustomFunction){
			C.ExtendedProperties[CustomAutoIncrement]= CustomFunction;
		}



        /// <summary>
        /// Tells whether a Column is a Custom AutoIncrement one
        /// </summary>
        /// <param name="C"></param>
        /// <returns>true if Column is Custom Auto Increment</returns>
        static internal bool isCustomAutoIncrement(DataColumn C){
			if (C.ExtendedProperties[CustomAutoIncrement]!=null) return true;
			return false;
		}

		/// <summary>
		/// Removes Custom-autoincrement property from a DataColumn
		/// </summary>
		/// <param name="C"></param>
		static internal void clearCustomAutoIncrement(DataColumn C){
			C.ExtendedProperties[CustomAutoIncrement]=null;
		}

        #endregion


        #region AUTOINCREMENT FIELD MANAGEMENT
        /// <summary>
        /// Set to true if any custom autoincrement found. In that case, the transaction is runned row by row and not with batches
        /// </summary>
        public bool HasCustomAutoFields = false;

    

	    /// <summary>
	    /// Function called to evaluate a custom autoincrement column
	    /// </summary>
	    /// <param name="dr">DataRow evaluated</param>
	    /// <param name="c">Column to evaluate</param>
	    /// <param name="conn">Connection to database</param>
	    /// <returns></returns>
	    public delegate Task<object> CustomCalcAutoId(DataRow dr, DataColumn c, IDataAccess conn);


        /// <summary>
        /// Evaluates a value for all autoincremented key field of a row
        /// </summary>
        /// <param name="DR">DataRow to insert</param>
        /// <param name="Conn"></param>
        protected async Task calcAutoID(DataRow DR,IDataAccess Conn){
			DR.BeginEdit();
			foreach (DataColumn C in DR.Table.Columns){
				if (isAutoIncrement(C)){
					await calcAutoID(DR, C, Conn);
				}
			}            
			DR.EndEdit();
		}

        /// <summary>
        /// Tells PostData that the given table is optimized, i.e. autoincrement values have to be cached
        /// </summary>
        /// <param name="T"></param>
        /// <param name="isOptimized"></param>
        public static void SetOptimized(DataTable T, bool isOptimized) {
            if (T == null) return;
            if (!isOptimized) {
                T.ExtendedProperties["isOptimized"] = null;
                return;
            }
            if (T.ExtendedProperties["isOptimized"] != null) return;
            T.ExtendedProperties["isOptimized"] = new Dictionary<string,int>();
        }

        /// <summary>
        /// Clear all max expression cached on a table
        /// </summary>
        /// <param name="T"></param>
        public static void ClearMaxCache(DataTable T) {
            if (T == null) return;
            if (T.ExtendedProperties["isOptimized"] == null) return;
            T.ExtendedProperties["isOptimized"] = new Dictionary<string, int>();
        }

        /// <summary>
        /// Returns true if special optimization are applied in the autoincrement properties evaluation
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static bool IsOptimized(DataTable T) {
            if (T == null) return false;
            return (T.ExtendedProperties["isOptimized"] != null);
        }

        /// <summary>
        /// Sets the new maximum for a specified combination of table, expression and filter
        /// </summary>
        /// <param name="T"></param>
        /// <param name="expr">expression for the max</param>
        /// <param name="filter">filter applied</param>
        /// <param name="num">new maximum to set</param>
        protected static void setMaxExpr(DataTable T, string expr, string filter,int num) {
            if(!(T.ExtendedProperties["isOptimized"] is Dictionary<string, int> h)) return;
            h[expr + "§" + filter] = num;
        }

        /// <summary>
        /// Gets the optimized max() value for and expression in a table, with a specified filter and minimum value
        /// </summary>
        /// <param name="T">Evaluated table</param>
        /// <param name="expr">expression to evaluate</param>
        /// <param name="filter">filter to apply</param>
        /// <param name="minimum">minimum value wanted</param>
        /// <returns></returns>
        protected static int getMaxExpr(DataTable T, string expr, string filter,int minimum) {
            var h = T.ExtendedProperties["isOptimized"] as Dictionary<string, int>;
            string k=expr + "§" + filter;
            int res=minimum;
            if (h.ContainsKey(k)) {
                res= h[k];
            }
            h[k] = res + 1;
            return res;
        }
        
        /// <summary>
        /// Sets mininimum value for evaluating temporary autoincrement columns
        /// </summary>
        /// <param name="C"></param>
        /// <param name="min"></param>
        public static void SetMinimumTempValue(DataColumn C,int min){
            C.ExtendedProperties["minimumTempValue"] = min;
        }

        static string maxSubstring(DataRow R,
            string colname,
            int start,
            int len,
            string filter) {
                int minimum = 0;
                if (R.Table.Columns[colname].ExtendedProperties["minimumTempValue"] != null) {
                    minimum = Convert.ToInt32(R.Table.Columns[colname].ExtendedProperties["minimumTempValue"]);
                }
                DataTable T = R.Table;
                if (!IsOptimized(T)) {
                    int n = maxSubstringClean(T, colname, start, len, filter);
                    if (n < minimum) n = minimum;
                    return n.ToString();
                }
                string expr = colname + "," + len;
                int res = getMaxExpr(T, expr, filter,minimum);
                if (res > 0) return res.ToString();
                
         
            res =  maxSubstringClean(T, colname, start, len, filter);
            if (res < minimum) res = minimum;
            setMaxExpr(T, expr, filter, res+1);
            return res.ToString();
        }


        static int maxSubstringClean(DataTable T,
            string colname,
            int start,
            int len,
            string filter) {


            string MAX = null;
            int maxsub = 0;
            if (start == 0 && len == 0) {
                object mm = T.Compute("MAX(" + colname + ")",filter);
                if (mm != null && mm != DBNull.Value) {
                    try {
                        maxsub = Convert.ToInt32(mm);
                        MAX = maxsub.ToString();
                    }
                    catch { }
                }

            }
            else {
                var filteredRows = T.Select(filter);
                foreach (var r in filteredRows) {
                    //if (R.RowState == DataRowState.Deleted) continue;
                    //if (R.RowState == DataRowState.Detached) continue;
                    string s = r[colname].ToString();
                    if (s.Length <= start) continue;
                    int thislen = len;
                    if (thislen == 0) thislen = s.Length - start;
                    if (start + thislen > s.Length) thislen = s.Length - start;
                    string substr = s.Substring(start, thislen);
                    if (MAX == null) {
                        MAX = substr;
                        try {
                            maxsub = Convert.ToInt32(MAX);
                        }
                        catch {
                            // ignored
                        }
                    }
                    else {
                        int xx = maxsub - 1;
                        try {
                            xx = Convert.ToInt32(substr);
                        }
                        catch {
                            // ignored
                        }

                        //if (substr.CompareTo(MAX)>0) MAX=substr;
                        if (xx > maxsub) maxsub = xx;
                    }
                }
            }
            DataRow[] filteredDeletedRows = T.Select(filter, null, DataViewRowState.Deleted);
            foreach (var r in filteredDeletedRows) {
                //if (R.RowState != DataRowState.Deleted) continue;
                string s = r[colname, DataRowVersion.Original].ToString();
                if (s.Length <= start) continue;
                int thislen = len;
                if (thislen == 0) thislen = s.Length - start;
                if (start + thislen > s.Length) thislen = s.Length - start;
                string substr = s.Substring(start, thislen);
                if (MAX == null) {
                    MAX = substr;
                    try {
                        maxsub = Convert.ToInt32(MAX);
                    }
                    catch {
                        // ignored
                    }
                }
                else {
                    int xx = maxsub - 1;
                    try {
                        xx = Convert.ToInt32(substr);
                    }
                    catch {
                        // ignored
                    }

                    //if (substr.CompareTo(MAX)>0) MAX=substr;
                    if (xx > maxsub) maxsub = xx;
                }

            }

            return maxsub;
        }




		/// <summary>
		/// Evaluates a temporary value for a field of a row, basing on AutoIncrement 
		///  properties of the column, without reading from DB.
		/// </summary>
		/// <param name="R"></param>
		/// <param name="C"></param>
		internal static void calcTemporaryID( DataRow R, DataColumn C){
			string Prefix="";
            if((C.ExtendedProperties[PrefixField] != null) &&
                (C.ExtendedProperties[PrefixField] != DBNull.Value)) {
                Prefix += R[C.ExtendedProperties[PrefixField].ToString()].ToString();
            }
            if ((C.ExtendedProperties[MiddleConst]!=null)&&
				(C.ExtendedProperties[MiddleConst]!=DBNull.Value))  {
				Prefix+= C.ExtendedProperties[MiddleConst].ToString();
			}
			int idSize=7;//default
            int totPrefsize = Prefix.Length;
            

            if ((C.ExtendedProperties[IDLength]!=null) &&
				(C.ExtendedProperties[IDLength]!=DBNull.Value)){
				idSize= Convert.ToInt32(C.ExtendedProperties[IDLength].ToString());
			}

            if ((C.DataType == typeof(int)) || (C.DataType == typeof(short)) || (C.DataType == typeof(long))) {
                if (totPrefsize == 0) idSize = 0;
            }

            var Selection = getSelector(R,C).toADO();
			string newIdvalue="1";

			if (C.ExtendedProperties[LinearField]!=null){
				string MAX = maxSubstring(R, C.ColumnName,totPrefsize,idSize, Selection);

				if (MAX!=null){
					int intFOUND2 = 0;
					try {
						intFOUND2 = Convert.ToInt32(MAX);
					}
				    catch {
				        // ignored
				    }

				    intFOUND2 += 1;
					newIdvalue = intFOUND2.ToString();
				}
			}
			else {
				//string SelCmd = $"MAX(CONVERT({C.ColumnName},'System.Int32'))";
				string filter2="";
				if (Prefix!="") filter2 = $"(CONVERT({C.ColumnName},'System.String') LIKE '{Prefix}%') ";
				if (Selection!="") {
					if (filter2!="") filter2 += " AND ";
					filter2 += Selection;
				}

				object MAXv2 =  maxSubstring(R,C.ColumnName,Prefix.Length,idSize,filter2); //T.Compute(SelCmd, filter2);
				string MAX2=null;
				if ((MAXv2!=null)&&(MAXv2!=DBNull.Value)) MAX2 = MAXv2.ToString();

				if (MAX2!=null){
					string foundSubstr=MAXv2.ToString();

					int intFound=0;
					if (foundSubstr!=""){
						try {
							intFound = Convert.ToInt32(foundSubstr);
						}
					    catch {
					        // ignored
					    }
					}
					intFound += 1;
					newIdvalue = intFound.ToString();
				}
			}

            string NEWID;
            if(idSize != 0) {
                NEWID = Prefix + newIdvalue.PadLeft(idSize, '0');
            }
            else {
                NEWID = Prefix + newIdvalue;
            }

            object oo = NEWID;
			if (C.DataType== typeof(int)) oo = Convert.ToInt32(oo);

			//Applies changes to CHILD rows of R (only necessary while resolving conflicts in POST)
			if (!oo.Equals(R[C.ColumnName])) {
                //Non cerca di riassegnare i figli quando il vecchio valore era stringa vuota, altrimenti capita che 
                // quelli senza parent (ossia il root) gli vengano assegnati come figli
                //if (R[C.ColumnName].ToString()!="") 
	                cascadeChangeField(R, C, oo);
				R[C.ColumnName]=oo;
			}
			
		}




        /// <summary>
        /// Evaluates temporary values for autoincrement columns  (reading from memory)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="T">Table to consider for autoincrement properties</param>
        /// <remarks>This function should be called when a row is added to a table, 
        ///   between DataTable.NewRow() and DataTable.Rows.Add()
        ///  </remarks>
        public static void CalcTemporaryID( DataRow r, DataTable T=null){
            if (T == null) {
                T = r.Table;
            }
			r.BeginEdit();
			foreach (DataColumn c in T.Columns){
				if (c.IsAutoIncrement()){
					calcTemporaryID(r, c);
				}
			}            
			r.EndEdit();
		}

		/// <summary>
		/// Evaluates a value for a specified key field of a row (reading from db)
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="dr">DataRow to insert</param>
		/// <param name="c">Column to evaluate ID</param>
		/// <remarks>The function takes some parameter from DataColumn ExtendedProperties:
		/// PrefixField = Row Field to consider as a prefix for the ID value
		/// MiddleConst = Constant to append to PrefixField 
		/// IDLength    = Length of automatic calculated ID
		/// </remarks>
		protected async Task calcAutoID(DataRow dr, DataColumn c, IDataAccess conn){
			var prefix="";
			object newid="";
            var qhs = conn.GetQueryHelper();

			if (c.IsCustomAutoIncrement()) {
				if (c.ExtendedProperties[CustomAutoIncrement] is CustomCalcAutoId fun) {
					newid = await fun(dr, c, conn);
					HasCustomAutoFields = true;
				}
			} 
            else {
				if ((c.ExtendedProperties[PrefixField] != null) &&
					(c.ExtendedProperties[PrefixField] != DBNull.Value)) {
					prefix += dr[c.ExtendedProperties[PrefixField].ToString()].ToString();
				}
				if ((c.ExtendedProperties[MiddleConst] != null) &&
					(c.ExtendedProperties[MiddleConst] != DBNull.Value)) {
					prefix += c.ExtendedProperties[MiddleConst].ToString();
				}
				int idSize=0;
				if ((c.ExtendedProperties[IDLength] != null) &&
					(c.ExtendedProperties[IDLength] != DBNull.Value)) {
					idSize = Convert.ToInt32(c.ExtendedProperties[IDLength].ToString());
				}
				int totPrefsize=prefix.Length;

				var Selection = getSelector(dr,c); //.toSql(qhs);


				if ((c.DataType == typeof(int)) || (c.DataType == typeof(short)) || (c.DataType == typeof(long))) {
					if (totPrefsize == 0)
						idSize = 0;
				}


				string expr2;
				if ((c.DataType == typeof(int)) || (c.DataType == typeof(short)) || (c.DataType == typeof(long))) {
					expr2 = qhs.Max(c.ColumnName); //$"MAX({c.ColumnName})";
				} else
					expr2 = qhs.Max(qhs.ConvertToInt(c.ColumnName));  //$"MAX(CONVERT(int,{c.ColumnName}))";

				if ((totPrefsize > 0) || (idSize > 0)) { //C'è da fare il substring
					int idToextr= idSize;
					if (idToextr == 0) idToextr = 12;
					string colnametoconsider=c.ColumnName;
                    if (c.DataType != typeof(String)) {
                        colnametoconsider = qhs.ConvertToVarchar(c.ColumnName,300);
                        //colnametoconsider = $"CONVERT(VARCHAR(300),{c.ColumnName})";
                    }
					expr2 = qhs.Max(qhs.ConvertToInt(qhs.Sustring(colnametoconsider,totPrefsize+1, idToextr)));
                        //$"MAX(CONVERT(int,SUBSTRING({colnametoconsider},{(totPrefsize + 1)},{idToextr})))";
				}
				if (c.ExtendedProperties[LinearField] == null) {
                    if (prefix != "") {
                        Selection &= q.like(c.ColumnName,prefix+"%"); //qhs.DoPar(qhs.Like(c.ColumnName,prefix+"%"));
                        //filter2 = $"({c.ColumnName} LIKE '{prefix}%') ";
                    }					
				}
				string unaliased = dr.Table.tableForReading();

				object result2;
				if (myCollection != null)
					result2 = myCollection.getMax(dr, c, conn, unaliased, Selection.toADO(), expr2);
				else
					result2 = await conn.ReadValue(table: unaliased, filter: Selection, expr: expr2);


				string newIDVALUE;
				if ((result2 == null) || (result2 == DBNull.Value)) {
					newIDVALUE = "1";
				} else {
					var foundId = result2.ToString();
					var intFound2 = 0;
					try {
						intFound2 = Convert.ToInt32(foundId);
					} catch {
						// ignored
					}

					intFound2 += 1;
					newIDVALUE = intFound2.ToString();
				}

				myCollection?.SetMax(unaliased, Selection.toADO(), expr2, newIDVALUE);

				if (idSize != 0) {
					newid = prefix + newIDVALUE.PadLeft(idSize, '0');
				} else {
					newid = prefix + newIDVALUE;
				}


			}


			var temp = dr.Table.NewRow();
			foreach (DataColumn CC in dr.Table.Columns) temp[CC]= dr[CC];
            //if(C.AllowDBNull && NEWID == "") {
            //    Temp[C] = DBNull.Value;
            //}
            //else {
            //    Temp[C] = NEWID;
            //}
            temp[c] = newid;

            if (!IsOptimized(dr.Table)) {
	            var keyfilter = q.keyCmp(temp);
								//QueryCreator.WHERE_KEY_CLAUSE(temp, DataRowVersion.Default, false);
                var found = dr.Table.filter(keyfilter);	//.Select(keyfilter);
                foreach (var rfound in found) {
                    if (rfound == dr) continue;
                    CalcTemporaryID(rfound);
                }
            }

			object oo = newid;
			if (c.DataType== typeof(int)&& (oo!=DBNull.Value)) oo = Convert.ToInt32(oo);


			//Applies changes to CHILD rows of R
			if (!oo.Equals(dr[c])) {
				cascadeChangeField(dr, c, oo);
				dr[c]=oo;
			}
			

		}



		#endregion

		#region Recursive operations (field change / row delete) 


		/// <summary>
		/// Changes R's child rows to reflect variation of R[ColumnToChange]= newvalue
		/// </summary>
		/// <param name="R"></param>
		/// <param name="ColumnToChange"></param>
		/// <param name="newvalue"></param>
		private static void cascadeChangeField(DataRow R, DataColumn ColumnToChange, object newvalue){
			foreach(DataRelation Rel in R.Table.ChildRelations){
				//checks if Rel includes "ColumnToChange" column of R
				for (int i=0; i< Rel.ChildColumns.Length; i++){
					DataColumn C = Rel.ParentColumns[i];
					if (C.ColumnName==ColumnToChange.ColumnName){
						DataColumn ChildColumnToChange = Rel.ChildColumns[i];
						foreach(DataRow ChildRow in R.getChildRows(Rel)) {
							if (R.RowState == DataRowState.Deleted) continue;
							cascadeChangeField(ChildRow, ChildColumnToChange, newvalue);
							ChildRow[ChildColumnToChange.ColumnName]= newvalue;
						}
					}
				}
			}
		}
        

		/// <summary>
		/// Deletes DataRow R and all it's sub-entities
		/// </summary>
		/// <param name="toDelete"></param>
		public  static void ApplyCascadeDelete(List <DataRow> toDelete) {
			if (toDelete.Count == 0) return;
			var childForTables = new Dictionary<string, List<DataRow>>();
			
	        //return iManager?.getChildRows(rParent, rel)
			//

			DataTable Parent = toDelete[0].Table;
			var iManager = Parent.DataSet?.getIndexManager();
			var rowForTable = new Dictionary<string, int>();
			foreach (DataRelation Rel in Parent.ChildRelations) {
				DataTable Child = Rel.ChildTable;
				if (!rowForTable.TryGetValue(Child.TableName, out int nRow)) {
					nRow = Child.Select().Length;
					rowForTable[Child.TableName] = nRow;
				}

				if (nRow == 0) continue;
                //Cancella le figlie nella tabella Child
                var isSubRel = staticModel.IsSubEntityRelation(Rel);
				foreach (DataRow R in toDelete) {
					DataRow[] ChildRows = iManager?.getChildRows(R, Rel) ?? R.GetChildRows(Rel);
					int nChilds = ChildRows.Length;
					if (nChilds == 0) continue;
					if (isSubRel) {
						if (!childForTables.TryGetValue(Child.TableName, out var list)){
							list = new List<DataRow>();
							childForTables[Child.TableName] = list;
						}

						list.AddRange(ChildRows);
						nRow -= nChilds;
						rowForTable[Child.TableName] = nRow;
						if (nRow == 0) break;
					}
					else {
						foreach (DataRow RChild in ChildRows) {
							//if (RChild.RowState== DataRowState.Deleted) continue;
							for (int i = 0; i < Rel.ChildColumns.Length; i++) {
								DataColumn CChild = Rel.ChildColumns[i];
								DataColumn CParent = Rel.ParentColumns[i];
								if (!CChild.AllowDBNull) continue;
								if (QueryCreator.IsPrimaryKey(RChild.Table, CChild.ColumnName)) continue;
								if (!QueryCreator.IsPrimaryKey(Parent, CParent.ColumnName)) continue;
								RChild[CChild.ColumnName] = DBNull.Value;
							}
						}
					}
				}

			}

			foreach (var list in childForTables.Values) {
				ApplyCascadeDelete(list);
			}

			staticModel.InvokeActions(Parent,TableAction.beginLoad);

			//if (toDelete.Count > 2000) {
			//	int blockSize = 1000;
			//	int nBlocks = toDelete.Count / blockSize;
			//	var task = new Task[nBlocks];
			//	int last = 0;
			//	for (int i = 0; i < nBlocks; i++) {
			//		int min = last;
			//		int max = last + blockSize-1;
			//		if (i == nBlocks - 1) max = toDelete.Count - 1;
			//		task[i] =  Task.Run(() => deleteAsync(toDelete,min,max));
			//		last = max + 1;
                    
			//	}

			//	Task.WaitAll(task);
			//}
			//else {
				foreach (DataRow R in toDelete) {
					R.Delete();
				}
			//}

			staticModel.InvokeActions(Parent,TableAction.endLoad);

		}
		private static IMetaModel staticModel = MetaFactory.factory.getSingleton<IMetaModel>();

	
		

		/// <summary>
		/// Deletes DataRow R and all it's sub-entities
		/// </summary>
		/// <param name="r"></param>
		public static void ApplyCascadeDelete (DataRow r) {
			int handle = StartTimer("ApplyCascadeDelete");
			ApplyCascadeDelete(new List<DataRow>(){r});

            StopTimer(handle);
            
		}


		///// <summary>
		///// Undo a stack of deletions
		///// </summary>
		///// <param name="RollBack"></param>
		//public static void RollBackDeletes (Stack RollBack){
		//	while (RollBack.Count>0){
		//		DataRow R = (DataRow) RollBack.Pop();
		//		R.RejectChanges(); //if it was added-deleted---> ??
		//	}
		//}

		#endregion

       

       


        /// <summary>
        /// Get list of relations where table Parent is the ParentTable and Child is the ChildTable
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        public static List<DataRelation> FindChildRelation(DataTable parent,
                    DataTable child) {
            List<DataRelation> relList = new List<DataRelation>();
            foreach (DataRelation rel in child.ParentRelations) {
                //if ((relname != null) && (Rel.RelationName != relname)) continue;
                if (rel.ParentTable.TableName == parent.TableName) {
                    relList.Add(rel);
                }
            }
            //foreach (DataRelation Rel2 in Parent.ChildRelations) {
            //    //if ((relname != null) && (Rel2.RelationName != relname)) continue;
            //    if (Rel2.ChildTable.TableName == Child.TableName) {
            //        return Rel2;
            //    }
            //}

            return relList;
        }

       
        
	    /// <summary>
	    /// Evaluates autoincrement values, completes the row to be changed with createuser,createtimestamp,
	    /// lastmoduser, lastmodtimestamp fields, depending on the operation type
	    ///  and calls CalculateFields for each DataRow involved.
	    ///  This must be done INSIDE the transaction.
	    /// </summary>
	    /// <param name="user">User who is posting</param>
	    /// <param name="acc"></param>
	    /// <param name="doCalcAutoId"></param>
	    public virtual async Task PrepareForPosting(string user, IDataAccess acc, bool doCalcAutoId) {
	        //SqlDateTime Stamp = new SqlDateTime(System.DateTime.Now);
	        var stamp = DateTime.Now;

            switch (ShortStatus()) {
                case short_insert_descr:
                    if (doCalcAutoId) await calcAutoID(DR, acc);
                    if (CreateUserField != null && DR.Table.Columns[CreateUserField] != null)
                        DR[CreateUserField] = user;
                    if (CreateTimeStampField != null && DR.Table.Columns[CreateTimeStampField] != null)
                        DR[CreateTimeStampField] = stamp;

                    if (LastModifyUserField != null && DR.Table.Columns[LastModifyUserField] != null)
                        DR[LastModifyUserField] = user;
                    if (LastModifyStampField != null && DR.Table.Columns[LastModifyStampField] != null)
                        DR[LastModifyStampField] = stamp;
                    break;
                case short_update_descr:
                    if (LastModifyUserField != null && DR.Table.Columns[LastModifyUserField] != null)
                        DR[LastModifyUserField] = user;
                    if (LastModifyStampField != null && DR.Table.Columns[LastModifyStampField] != null)
                        DR[LastModifyStampField] = stamp;
                    break;
                case short_delete_descr:
                    //nothing to do!
                    break;
            }
	        try {
	            staticModel.CalculateRow(DR);
	        }
	        catch (Exception e) {
	            ErrorLogger.logException($"PrepareForPosting({DR.Table.TableName})", e);
	        }
	    }

    }

	/// <summary>
	/// Collection of RowChange
	/// </summary>
	public class RowChangeCollection : System.Collections.ArrayList {
        /// <summary>
        /// Connection to use, used in derived classes
        /// </summary>
	    public IDataAccess connectionToUse;
        /// <summary>
        /// Total number of deleted rows
        /// </summary>
	    public int nDeletes = 0;

        /// <summary>
        /// Total number of updated rows
        /// </summary>
	    public int nUpdates = 0;

        /// <summary>
        /// Total number of added rows
        /// </summary>
	    public int nAdded = 0;

        private  Dictionary<string, string> AllMax = new Dictionary<string, string>();
        internal bool is_temporaryCollection = false;

        /// <summary>
        /// Azzera  i massimi presenti nella cache di transazione
        /// </summary>
        public void EmptyCache() {
            AllMax.Clear();
        }

    

        private string getHash(string table, string filter, string expr){
            return table+"§"+filter+"§"+expr;
        }

        /// <summary>
        /// Evaluates the maximum value for an expression referring to a Column of a DataRow, 
        /// </summary>
        /// <param name="R">DataRow being calculated</param>
        /// <param name="C">DataColumn to evaluate</param>
        /// <param name="conn">Connection</param>
        /// <param name="table">table implied</param>
        /// <param name="filter">Filter for evaluating the expression</param>
        /// <param name="expr">expression to evaluate</param>
        /// <returns></returns>
        internal string getMax(DataRow R, DataColumn C, IDataAccess conn, string table, string filter, string expr){
            string k = getHash(table,filter,expr).ToUpper();
            if (AllMax.ContainsKey(k)) return AllMax[k];
            
            DataTable T = C.Table;
            //Vede tutte le parent relation in cui sia contenuta una colonna chiave di R e che sia pure in selectors.
            // se trova una parent relation del genere, in cui il parent row sia uno, e in cui la colonna parent 
            //  sia un campo ad autoincremento per il parent, ed il parent è in stato di ADDED
            //  ALLORA
            //  PUO' EVITARSI LA SELECT e restituire direttamente null
            List<DataColumn> selectors = RowChange.getSelectors(C);
            foreach (DataColumn sel in selectors) {
                if (!QueryCreator.IsPrimaryKey(T, sel.ColumnName)) continue;
                foreach (DataRelation parRel in R.Table.ParentRelations) {
                    DataRow[] parRow = R.getParentRows(parRel);
                    if (parRow.Length != 1) continue;
                    DataRow parent = parRow[0];
                    if (parent.RowState != DataRowState.Added) continue;
                    //vede il nome della colonna corrispondente al selettore sel, nel parent
                    DataColumn parentCol = null;
                    for (int i = 0; i < parRel.ChildColumns.Length; i++) {
                        if (parRel.ChildColumns[i] == sel) {
                            parentCol = parRel.ParentColumns[i];
                        }
                    }
                    if (parentCol == null) continue;
                    if (!parentCol.IsAutoIncrement()) continue;
                    return null; //THIS IS THE CASE!!!!!
                }
            }
           
                


            object res = conn.ReadValue(table, filter:filter, expr:expr);
            if (res == null || res == DBNull.Value) return null;
            AllMax[k] = res.ToString();
            return res.ToString();
        }

        /// <summary>
        /// Sets the max value for a specific combination
        /// </summary>
        /// <param name="table">table to set the max value</param>
        /// <param name="filter">filter connected (usually a bunch of selector and static filters)</param>
        /// <param name="expr">expression that the max value refers to</param>
        /// <param name="value">value to set as new maximum</param>
        public void SetMax(string table, string filter, string expr, string value){
             string k = getHash(table,filter,expr).ToUpper();
            AllMax[k]=value;
        }

		internal void add(RowChange C) {
			base.Add(C);
		    if (C.DR.RowState == DataRowState.Deleted) nDeletes++;
		    if (C.DR.RowState == DataRowState.Modified) nUpdates++;
		    if (C.DR.RowState == DataRowState.Added) nAdded++;

            if (!is_temporaryCollection) C.SetCollection(this);
		}
        
		/// <summary>
		/// Gets the RowChange in the specified Table
		/// </summary>
		/// <param name="TableName">Name of the DataTable where the related row is to be found</param>
		/// <returns>The table-related row in the collection</returns>
        public RowChange GetByName(string TableName) {
            foreach (RowChange R in this) {
                if (R.TableName == TableName) return R;
            }

            foreach (RowChange R in this) {
                if (R.Table.tableForPosting() == TableName) return R;
            }
            foreach (RowChange R in this) {
                if (R.Table.tableForReading() == TableName) return R;
            }

            //Tablename was not found. Try searching in tablename+view			
            foreach (RowChange R in this) {
                if (R.TableName == TableName + "view") return R;
            }

            return null;
        }
    }

	/// <summary>
	/// Class that manages log
	/// </summary>
	public class DataJournaling{
		/// <summary>
		/// Should return the log rows to add to db 
		/// for a given set of changes that have been made to DB
		/// </summary>
		/// <param name="Changes"></param>
		/// <returns></returns>
		virtual public DataRowCollection DO_Journaling(RowChangeCollection Changes){
			return null;
		}
	}



	/// <summary>
	/// Configurable String Parser: 
	/// It is able to find occurencies of strings with predefined delimiters,
	///  giving the string found and the string type (referring to the delimiters)
	/// </summary>
	public class MsgParser {
		String Message;
		int next_position;
		String[] StartString;
		String[] StopString;
		int NumString;
        
		/// <summary>
		/// Create a Parser able to recognize more than one Start/Stop Tag
		/// </summary>
		/// <param name="message">Message to Parse</param>
		/// <param name="start">array of Start tags</param>
		/// <param name="stop">array of (corresponding) Stop Tags</param>
		public MsgParser(String message, String[] start, String[] stop){
			this.Message= message;
			this.NumString = start.Length;
			StartString = start;
			StopString  = stop;
			next_position=0;
		}
        
		/// <summary>
		/// Create a Parser able to recognize one Start/Stop Tag
		/// </summary>
		/// <param name="Message">Message to Parse</param>
		/// <param name="Start">Start tag</param>
		/// <param name="Stop">Stop Tag</param>
		public MsgParser(String Message, String Start, String Stop){
			this.Message= Message;
			this.NumString = 1;
			StartString = new String[]  {Start};
			StopString  = new String[]  {Stop};
			Reset();
		}
                
		/// <summary>
		/// Reset the parser to the beginning of the string
		/// </summary>
		public void Reset() {
			next_position=0;

		}
        
		/// <summary>
		///   Find the next occurrence delimited by the defined tags
		///   This function DOES NOT allow nested tags.
		/// </summary>
		/// <param name="Found" type="output">String found between delimiters</param>
		/// <param name="Skipped" type="output">String found before the first delimiter</param>
		/// <param name="Kind">Index of the delimiters</param>
		/// <returns>true when an occurrence is found</returns>
		public bool GetNext(out String Found, out String Skipped, out int Kind){
			if (next_position >= Message.Length) {
				Found="";
				Skipped="";
				Kind=-1;
				return false;
			}
            
			int found_at=-1;   //not found
			int after_end_tag=-1;
			int next_at=-1;
			int found_kind=-1;
			int new_start=-1;
			int end_tag;
			for (int i=0; i<NumString; i++){
				int curr_found_at =Message.IndexOf(StartString[i],next_position);
                
				if (curr_found_at >=0){ //checks for the presence of the end tag
					new_start = curr_found_at + StartString[i].Length;
					end_tag = Message.IndexOf(StopString[i], new_start);
					if (end_tag == -1) 
						curr_found_at=-1; //aborts the element
					else
						after_end_tag= end_tag+StopString[i].Length;
				}

				if (curr_found_at >=0){

					if ((found_at==-1) || (found_at>curr_found_at)){
						found_at = curr_found_at;
						found_kind=i;
						next_at=after_end_tag;
					}
				}
			}

			if (found_at>=0){ //string was found
				int len= next_at-StopString[found_kind].Length-new_start;
				Found = Message.Substring(new_start, len);
				Skipped = Message[next_position..found_at];
				Kind = found_kind;
				next_position = next_at;
				return true;
			}
			else { //string was not found
				Kind=-1;
				Found="";
				Skipped=Message[next_position..];
				next_position= Message.Length;
				return false;
			}
		} 

		/// <summary>
		///   Find the next occurrence delimited by the defined tags
		///   This function DOES NOT allow nested tags.
		/// </summary>
		/// <param name="Found">String found between delimiters</param>
		/// <param name="Skipped">String found before the first delimiter</param>
		/// <returns>true when an occurrence is found</returns>
		public bool GetNext(out String Found, out String Skipped){
			return GetNext(out Found, out Skipped, out _);
        }

    
	}


	/// <summary>
	/// Fills the field EnforcementRule (ProcedureMessageCollection) of any RowChange in Cs
	/// </summary>
	public class MetaDataRules {
		/// <summary>
		/// Query the business logic to get a binary representation of a list
		///  of error messages to be shown to the user. 
		/// </summary>
		/// <param name="R">Change to scan for messages</param>
		/// <param name="result">Array in which every row represents the need
		///  to display a corresponding message</param>
		virtual public void DO_CALC_MESSAGES(RowChange R, bool[] result){
		}
	}


  


	/// <summary>
	/// Business Rule Error Message 
	/// </summary>
	public class ProcedureMessage {
		/// <summary>
		/// CanIgnore is true if user is allowed to ignore the message. False if error is Severe
		/// </summary>
		public bool CanIgnore;
        
        /// <summary>
        /// True if it's a post-check, false if it is a pre-check
        /// </summary>
        public bool PostMsgs;
		/// <summary>
		/// Business Rule Error Message Text
		/// </summary>
		public String LongMess;

		/// <summary>
		/// Gets a Key that makes the message unique in the RowChange.EnforceMessages List 
		/// </summary>
		/// <returns>RuleID@@@EnforcementID</returns>
		public virtual string GetKey(){
			
			return LongMess; //RuleID + "@@@"  + EnforcementNumber;
		}

	}


	/// <summary>
	/// Collection of messages to be displayed to the user
	/// </summary>
	public class ProcedureMessageCollection : System.Collections.ArrayList{
		/// <summary>
		/// CanIgnore is True if Messages are Warning and can be ignored by the user
		///		(there are no Severe Errors)
		/// </summary>
		public bool CanIgnore=true;

		/// <summary>
		/// PostMsgs is true is Messages are "Post-Messages", i.e. generated after posting all changes to DB
		/// </summary>
		public bool PostMsgs=false;

		/// <summary>
		/// Show messages to user and return true if he decided to ignore them
		/// </summary>
		/// <returns>true if no messages or messages ignored</returns>
		public virtual bool ShowMessages(){
			return true;
		}

        /// <summary>
        /// Append a db unrecoverable error  in the list of messages
        /// </summary>
        /// <param name="message"></param>
		public virtual void AddDBSystemError(string message){
            var P = new ProcedureMessage {
                CanIgnore = false,
                LongMess = $"Errore nella scrittura sul DB.{message}"
            };
            Add(P);
		}

	    /// <summary>
	    /// Append a db recoverable error  in the list of messages
	    /// </summary>
	    /// <param name="message"></param>
	    public virtual void AddWarning(string message){
            var P = new ProcedureMessage {
                CanIgnore = true,
                LongMess = message
            };
            Add(P);
	    }

		/// <summary>
		/// Gets the Message at a specified index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ProcedureMessage GetMessage(int index){
			return (ProcedureMessage)  this[index];
		}

        /// <summary>
        /// evaluates CanIgnore flag
        /// </summary>
	    protected void recalcIgnoreFlag() {
	        CanIgnore = true;
	        foreach (ProcedureMessage r in this) {
	            if (r == null) continue;
	            if (!r.CanIgnore) CanIgnore = false;
	        }
	    }

		/// <summary>
		/// Adds a message to the list, updating CanIgnore status
		/// </summary>
		/// <param name="Msg"></param>
		public virtual void Add(ProcedureMessage Msg) {
			base.Add(Msg);
            Msg.PostMsgs = this.PostMsgs;
			if (!Msg.CanIgnore) CanIgnore = false;
		}

        /// <summary>
        ///  Appends a list of messages to this
        /// </summary>
        /// <param name="otherList"></param>
        public virtual void Add(ProcedureMessageCollection otherList) {
            foreach (ProcedureMessage p in otherList) {
                base.Add(p);
                if (!p.CanIgnore) CanIgnore = false;
            }
        }

		/// <summary>
		/// Remove from this list every message in MsgToIgnore
		/// </summary>
		/// <param name="MsgToIgnore"></param>
		public void SkipMessages(HashSet<string> MsgToIgnore){
			ArrayList list = new ArrayList();
			foreach(ProcedureMessage PP in this){
				if (!PP.CanIgnore)continue;
				if (MsgToIgnore.Contains(PP.LongMess)) list.Add(PP);
			}
			foreach (ProcedureMessage PP in list){
				Remove(PP);
			}
		}

        /// <summary>
        /// Sets an Hashtables with Messages to ignore with all messages contained in this collection
        /// </summary>
        /// <param name="MsgToIgnore">Hashtable containg all messages to ignore</param>
		public void AddMessagesToIgnore(HashSet<string> MsgToIgnore){
			foreach(ProcedureMessage PP in this) {
				MsgToIgnore.Add(PP.LongMess);
			}
		}


	}

    /// <summary>
    /// Class used to nest posting of different datasets
    /// </summary>
    public interface IInnerPosting {
        /// <summary>
        /// inner PostData class
        /// </summary>
        //PostData innerPostClass { get; }
        HashSet<string> HashMessagesToIgnore();

        /// <summary>
        /// Called to initialize the class, inside the transaction
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="conn"></param>
        Task InitClass(DataSet ds, IDataAccess conn);


        /// <summary>
        /// Unisce i messaggi dati a quelli finali
        /// </summary>
        /// <param name="messages"></param>
        void MergeMessages(ProcedureMessageCollection messages);

        /// <summary>
        /// Called after data has been committed or rolled back
        /// </summary>
        /// <param name="committed"></param>
        Task AfterPost(bool committed);
        
        /// <summary>
        /// Reads all data about views (also in inner posting classes)
        /// </summary>
        Task ReselectAllViewsAndAcceptChanges();

        /// <summary>
        /// Get innerPosting class 
        /// </summary>
        /// <returns></returns>
        IInnerPosting GetInnerPosting();

        /// <summary>
        /// Set innerPosting mode with a set of already raised messages
        /// </summary>
        /// <param name="ignoredMessages"></param>
        void SetInnerPosting(HashSet<string> ignoredMessages);

        /// <summary>
        /// Post inner data to db
        /// </summary>
        /// <returns></returns>
        Task<ProcedureMessageCollection> SaveData();
    }

	//[Transaction(TransactionOption.Required)]
	/// <summary> 
	/// PostData manages updates to DB Tables
	/// Necessary pre-conditions are that:
	/// - DataBase Tables name are the same as DataSet DataTable names
	/// - Temporary (not belonging to DataBase) table are "marked" with the ExtendedProperty[IsTempTable]!=null
	/// </summary>	
	/// <remarks>
	/// If rows are added to datatable contains ID that have to be evaluated as max+1 from
	///  the database, additional information have to be put in the ID datacolumn:
	///  IsAutoIncrement = "s"    -   REQUIRED 
	///  PrefixField              -   optional 
	///  MiddleConst              -   optional 
	///  IDLength                 -   optional
	///  see RowChange for additional info
	/// </remarks>
	public class PostData {   //: ServicedComponent {


      



        /// <summary>
        /// Returns an empty list of error messages
        /// </summary>
        /// <returns></returns>
		public virtual ProcedureMessageCollection GetEmptyMessageCollection(){
			return new ProcedureMessageCollection();
		}



        /// <summary>
        /// String Constant used to pass null parameter to stored procedures
        /// </summary>
        public const string NullParameter = "null";

        private ProcedureMessageCollection resultList=null;

		/// <summary>
		/// refresh dataset rows when update fails
		/// </summary>
		/// 
		public bool refresh_dataset;
        

        /// <summary>
        /// Automatically discards non-blocking errors while saving
        /// </summary>
        public bool AutoIgnore = false;

		// <summary>
		// true if Object is valid
		// </summary>
		//        public bool WellObject;

		

		string lasterror;

		/// <summary>
		/// Get last Error message and automatically clears it
		/// </summary>
		public string GetErrorMsg {
			get {
				string res = lasterror;
				lasterror="";
				return res;
			}
		}

        /// <summary>
        /// Manage the posting of a singleDataSet
        /// </summary>
        protected class SingleDatasetPost {
            /// <summary>
            /// MetaModel used by the metadata
            /// </summary>
            //public IMetaModel model = MetaFactory.factory.getSingleton<IMetaModel>();

	        IDataAccess privateConn;
            /// <summary>
            /// Dataset posted from this class
            /// </summary>
	        public DataSet DS;
	        string user;
            /// <summary>
            /// List of rows changed in DS
            /// </summary>
            public RowChangeCollection RowChanges;

            /// <summary>
            /// Last error occurred in this posting class
            /// </summary>
            public string LastError { get; set; }



            #region Row Changes Sorting And Classifying

            /// <summary>
            ///  Evaluates the list of the changes to apply to the DataBase, in the order they
            ///  should be "reasonably" done:
            ///  All operation on None
            ///  Deletes on  Child 
            ///  Deletes on Both
            ///  Deletes on Parent
            ///  Insert, Update on Parent
            ///  Insert, Update on Both (in the evaluated list - reversed order )
            ///  Insert, Update on Child
            ///  Excluding all temporary table!
            /// </summary>
            /// <param name="Original">DataBase to be scanned for changes</param>
            /// <returns>List of changes to be done, in a reasonably good order</returns>
            RowChangeCollection changeList(DataSet Original) {
                var ParentFirst = new ArrayList(3);
                var ChildFirst = new ArrayList(3);
                var Result = new RowChangeCollection {
                    connectionToUse = privateConn
                };
                sortTables(Original, ParentFirst, ChildFirst);
                addTablesOps(Result, ChildFirst, DataRowState.Deleted, false);
                addTablesOps(Result, ParentFirst, DataRowState.Added, false);
                addTablesOps(Result, ParentFirst, DataRowState.Modified, false);
                return Result;
            }

            void sortTables(DataSet D, ArrayList ParentFirst, ArrayList ChildFirst) {
                bool added = true;
                var Added = new Hashtable();
                while (added) {
                    added = false;
                    foreach (DataTable T in D.Tables) {
                        if (Added[T.TableName] != null) continue;
                        if (MetaModel.IsTemporaryTable(T)) continue;
                        if (checkIsNotChild(T, Added)) {
                            ParentFirst.Add(T);
                            Added[T.TableName] = "1";
                            added = true;
                            continue;
                        }
                    }
                }
                Added = new Hashtable();
                added = true;
                while (added) {
                    added = false;
                    foreach (DataTable T in D.Tables) {
                        if (Added[T.TableName] != null) continue;
                        if (MetaModel.IsTemporaryTable(T)) continue;
                        if (checkIsNotParent(T, Added)) {
                            ChildFirst.Add(T);
                            Added[T.TableName] = "1";
                            added = true;
                            continue;
                        }
                    }
                }
            }

            bool checkIsNotChild(DataTable T, Hashtable ParentToIgnore) {
                if (T.ParentRelations.Count == 0) return true;
                foreach (DataRelation Rel in T.ParentRelations) {
                    DataTable ParentTable = Rel.ParentTable;
                    if (ParentTable.TableName == T.TableName) continue;
                    if (ParentToIgnore[ParentTable.TableName] != null) continue;
                    if (MetaModel.IsTemporaryTable(ParentTable)) continue;
                    foreach (DataRow RParent in ParentTable.Rows) {
                        if (RParent.RowState != DataRowState.Unchanged) return false;
                    }
                }
                return true;
            }

            bool checkIsNotParent(DataTable T, Hashtable ChildToIgnore) {
                if (T.ChildRelations.Count == 0) return true;
                foreach (DataRelation Rel in T.ChildRelations) {
                    var ChildTable = Rel.ChildTable;
                    if (ChildToIgnore[ChildTable.TableName] != null) continue;
                    if (ChildTable.TableName == T.TableName) continue;
                    if (MetaModel.IsTemporaryTable(ChildTable)) continue;
                    if (ChildTable.Rows.Count > 0) return false;
                }
                return true;
            }


            /// <summary> 
            ///  Adds all Rows (of every Tables referred by "Tables")with a specified State  
            ///  to Result list.
            /// </summary>
            /// <param name="Result" type="output">Updated list of all specified rows</param>
            /// <param name="Tables">Name list of the DataTables to scan</param>
            /// <param name="State">Row state to consider</param>
            /// <param name="reverse">true if Tables is to be scanned in reverse order</param>
            void addTablesOps(RowChangeCollection Result, ArrayList Tables, DataRowState State, bool reverse) {
                if (reverse) {
                    for (int i = Tables.Count - 1; i >= 0; i--) {
                        var T = (DataTable)Tables[i];
                        if (MetaModel.IsTemporaryTable(T)) continue;
                        foreach (DataRow R in T.Rows) {
                            if (R.RowState == State) Result.add(mainPost.GetNewRowChange(R));
                        }
                    }
                }
                else {

                    for (int i = 0; i < Tables.Count; i++) {
                        var T = (DataTable)Tables[i];
                        if (MetaModel.IsTemporaryTable(T)) continue;
                        if (State == DataRowState.Deleted) {
                            foreach (DataRow R in T.Rows) {
                                if (R.RowState == State) Result.add(mainPost.GetNewRowChange(R));
                            }

                        }
                        else {
                            foreach (var R in T.Select(null, GetPostingOrder(T))) {
                                if (R.RowState == State) Result.add(mainPost.GetNewRowChange(R));
                            }
                        }
                    }
                }
            }





            #endregion

            private PostData mainPost;

        

            /// <summary>
            /// Posting class that saves a single DataSet with a specified DataAccess
            /// </summary>
            /// <param name="DS"></param>
            /// <param name="Conn"></param>
            /// <param name="p">main Posting class</param>
            public SingleDatasetPost(DataSet DS, IDataAccess Conn) {
	            this.DS = DS;
	            privateConn = Conn;       //every   singleDatasetPost has his connection
                user = Conn.Security.User;
            }

            public async Task Init(PostData p) {
                mainPost = p;
                ClearDataSet.RemoveConstraints(DS);
                DS.RemoveFalseUpdates();
                RowChanges = changeList(DS);
                if (RowChanges.nDeletes > 10000) {
                    ErrorLogger.Logger.MarkEvent($"Deleting {RowChanges.nDeletes} rows ");
                }
                if (!DS.HasChanges())
                    return;
                this.Rules = await mainPost.getRules(RowChanges);
            }
            /// <summary>
            /// Rules that must be applied for the current set of changes
            /// </summary>
            public MetaDataRules Rules;

         

            /// <summary>
            /// Send a message of startPosting to a privateConn if different from postConn.
            /// So next phisical operation will be executed on postConn instead of this connection
            /// </summary>
            /// <param name="postConn"></param>
            public void StartPosting(IDataAccess postConn) {
                if (postConn != privateConn) {                    
                    privateConn.startPosting(postConn);
                }                
            }

            /// <summary>
            /// Send a message of stopPosting to a privateConn if different from postConn
            /// </summary>
            /// <param name="postConn"></param>
            public void StopPosting(IDataAccess postConn) {
                if (postConn != privateConn) {
                    privateConn.stopPosting();
                }
            }

            /// <summary>
            /// Evalueates autoincrement values, completes the row to be changed with createuser,createtimestamp,
            /// lastmoduser, lastmodtimestamp fields, depending on the operation type
            ///  and eventually calls CalculateFields for each DataRow involved.
            ///  This must be done OUTSIDE the transaction.
            /// </summary>
            public async Task  PrepareForPosting() {
	            foreach (RowChange RToPreSet in RowChanges) {
	                //Adjust lastmoduser etc. in order to be able of properly calling checks                
	                await RToPreSet.PrepareForPosting(user, privateConn, false);
	            }
	        }

            /// <summary>
            /// Collection of PRE- checks
            /// </summary>
            public ProcedureMessageCollection precheck_msg = null;


            #region CALL PRE/POST CHECKS

          

            private DataJournaling Journal;

            /// <summary>
            /// Gets the Journaling class connected for the posting operation
            /// </summary>
            public async Task GetJournal() {
                Journal = await mainPost.getJournal(privateConn, RowChanges);
            }


            /// <summary>
            /// Save all change log (journal) to database
            /// </summary>
            /// <returns></returns>
            public async Task<bool> DoJournal() {
                DataRowCollection RCs = Journal.DO_Journaling(RowChanges);
                if (RCs == null) return true;
                foreach (DataRow R in RCs) {
                    if (await dbSaveRow(R) != 1) return false;
                }
                return true;
            }
            

            /// <summary>
            /// As above, but returns the error collection. Also sets precheck_msg
            /// </summary>
            public async Task<ProcedureMessageCollection> GetPreChecks(HashSet<string> IgnoredMessages) {
                ProcedureMessageCollection result;
                try {
                    await privateConn.Open();

                    //Call all necessary stored procedures for checking changement
                    result = await mainPost.callChecks(false, RowChanges);
                    result.PostMsgs = false;
                    result.SkipMessages(IgnoredMessages);
                }
                catch (Exception e) {
                    result = mainPost.GetEmptyMessageCollection();
                    result.AddDBSystemError(ErrorLogger.GetErrorString(e));
                    Trace.Write("Error :" + ErrorLogger.GetErrorString(e) + "\r", "PostData.GetPreChecks\r");
                    LastError = ErrorLogger.GetErrorString(e);
                    result.CanIgnore = false;
                    result.PostMsgs = false;
                }
                finally {
                    await privateConn.Close();
                }
                precheck_msg = result;
                return result;
            }


            /// <summary>
            /// Query the Business logic to establish whether the operation 
            ///  violates any non ignorable Post-Checks. If it happens, returns false
            /// </summary>
            /// <returns>true if the changes on the DataSet are possible</returns>              
            /// <remarks>Related rows must have been already filled</remarks>
            public async Task<ProcedureMessageCollection> GetPostChecks(HashSet<string> IgnoredMessages) {

                //Evaluates every error message & attach them to RowChanges elements
                ProcedureMessageCollection Res=null;
                try {
                    //Call all necessary stored procedures for checking changement
                    Res = await mainPost.callChecks(true, RowChanges);
                    Res.SkipMessages(IgnoredMessages);
                    Res.PostMsgs = true;
                }
                catch (Exception e) {
                    if (Res==null) Res = mainPost.GetEmptyMessageCollection();
                    Res.AddDBSystemError(ErrorLogger.GetErrorString(e));
                    Trace.Write("Error :" + ErrorLogger.GetErrorString(e) + "\r", "PostData.DO_POSTCHECK\r");
                    LastError = ErrorLogger.GetErrorString(e);
                    Res.CanIgnore = false;
                    Res.PostMsgs = true;
                }

                return Res;
            }


            #endregion


            #region DO PHYSICAL OPERATION


            /// <summary>
            /// Write all changed rows to db, returns true if succeeds
            /// </summary>
            /// <returns></returns>
            internal async Task<bool> writeToDatabase() {
                var nn = StartTimer("writeToDatabase()");
                RowChanges.EmptyCache();
                var sb = new StringBuilder();
                var batchedRows = new List<RowChange>();
                var rowindex = 0;
                foreach (RowChange r in RowChanges) {
                    //post the change                
                    await r.PrepareForPosting(user, privateConn, true); //calls calcAutoID and eventually set R.HasCustomAutoFields to true
                    if (!privateConn.model.IsSkipSecurity(r.Table)) {
                        if (!privateConn.Security.CanPost(r.DR)) {
                            LastError =
                                $"L\'operazione richiesta sulla tabella {r.TableName} è vietata dalle regole di sicurezza.";
                            StopTimer(nn);
                            return false;
                        }
                    }

                    string cmd = getPhysicalPostCommand(r.DR);
                    sb.AppendLine(cmd+";");
                    sb.AppendLine($"if (@@ROWCOUNT=0) BEGIN select {rowindex}; RETURN; END;");
                    batchedRows.Add(r);

                    if (sb.Length > 40000 || r.HasCustomAutoFields) {
                        var res = await executeBatch(sb, batchedRows);
                        if (!res) return false;
                        sb = new StringBuilder();
                        batchedRows = new List<RowChange>();
                        rowindex = 0;
                    }
                    else {
                        rowindex++;
                    }

                }
                bool result;
                if (rowindex > 0) {
                    result = await executeBatch( sb, batchedRows);
                }
                else {
                    result = true;
                }
                StopTimer(nn);
                return result;
            }

            /// <summary>
            /// Executes a batch of sql commands
            /// </summary>
            /// <param name="batch"></param>
            /// <param name="rows"></param>
            /// <returns>true if successfull, otherwise lasterror is set</returns>
            async Task<bool> executeBatch( StringBuilder batch, List<RowChange> rows) {
                batch.Append("SELECT -1");
                //DataTable T = Conn.SQLRunner(Batch.ToString(), 60, out errmess);
                object res;
                try {
                    res = await privateConn.ExecuteScalarLastResult(batch.ToString());
                }
                catch (Exception ex) {
                    ErrorLogger.Logger.markException(ex,$"Errore su db:{ex.Message}");
                    LastError = ex.Message;
                    return false;
                }

                //Get Bad Row
                int n = Convert.ToInt32(res);
                if (n == -1) {
                    privateConn.SetLastWrite();
                    return true;
                }
                if (n < 0 || n >= rows.Count) {
                    LastError = $"Errore interno eseguendo:{batch}";
                    ErrorLogger.Logger.MarkEvent(LastError);
                    return false;
                }

                RowChange R = rows[n];
                if (R.DR.RowState == DataRowState.Added) {
                    LastError = $"Error running command:{privateConn.GetInsertCommand(R.DR)}";
                    ErrorLogger.Logger.MarkEvent(LastError);
                    return false;
                }
                if (R.DR.RowState == DataRowState.Deleted) {
                    LastError = $"Error running command:{privateConn.GetDeleteCommand(R.DR, mainPost.GetOptimisticClause(R.DR))}";
                    ErrorLogger.Logger.MarkEvent(LastError);
                    return false;
                }
                if (R.DR.RowState == DataRowState.Modified) {
                    string err = $"Error running command:{privateConn.GetUpdateCommand(R.DR, mainPost.GetOptimisticClause(R.DR))}";
                    ErrorLogger.Logger.MarkEvent(err);
                    R.DR.RejectChanges();
                    await reselect(R.DR, DataRowVersion.Default);
                    LastError = err;
                    return false;
                }
                return false;

            }

           

            string getPhysicalPostCommand(DataRow R) {
				return R.RowState switch {
					DataRowState.Added => this.privateConn.GetInsertCommand(R),
					DataRowState.Modified => this.privateConn.GetUpdateCommand(R, mainPost.GetOptimisticClause(R)),
					DataRowState.Deleted => this.privateConn.GetDeleteCommand(R,mainPost.GetOptimisticClause(R)),
					_ => "",
				};
			}

            async Task<int> dbSaveRow(DataRow R) {

				return R.RowState switch {
					DataRowState.Added => await dbInsert(R),
					DataRowState.Modified => await dbUpdate(R),
					DataRowState.Deleted => await dbDelete(R),
					_ => 0,
				};
			}


           

            async Task<int> dbDelete( DataRow r) {
                var T = r.Table;
                var tablename = T.tableForPosting();
                var condition = mainPost.GetOptimisticClause(r);
                try {
                    return await privateConn.DoDelete(tablename, condition);
                }
                catch (Exception e){
                    LastError = e.ToString();
                    r.RejectChanges();
                    await reselect(r, DataRowVersion.Default);
                    return 0;
                }
               
            }


           

            async Task<int> dbInsert(DataRow R) {
                DataTable T = R.Table;
                string tablename =T.tableForPosting();
                List<string> names = new List<string>();
                List<object> values = new List<object>();

                foreach (DataColumn C in T.Columns) {
                    if (C.IsTemporary()) continue;
                    if (R[C, DataRowVersion.Default] == DBNull.Value) continue; //non inserisce valori null
                    string postcolname = C.PostingColumnName(); // C.ColumnName;
                    if (postcolname == null) continue;
                    names.Add( postcolname);
                    values.Add(R[C, DataRowVersion.Default]);
                }
                try {
                    await privateConn.DoInsert(tablename, names, values);
                    return 1;
                }
                catch (Exception e){
                    LastError = e.ToString();
                    return 0;
				}
            }

           


            async Task<int> dbUpdate(DataRow R) {
                DataTable T = R.Table;
                string tablename = T.tableForPosting();
                int npar = 0;

                List<string> names = new List<string>();
                List<object> values = new List<object>();

                foreach (DataColumn C in T.Columns) {
                    if (C.IsTemporary()) continue;
                    if (R[C, DataRowVersion.Original].Equals(R[C, DataRowVersion.Current])) continue;
                    string postcolname = C.PostingColumnName(); // C.ColumnName;
                    if (postcolname == null) continue;
                    names.Add( postcolname);
                    values.Add(R[C, DataRowVersion.Current]);
                  
                    npar++;
                }
                if (npar == 0) return 1;
                try {
                    var res = await privateConn.DoUpdate(tablename, filter: mainPost.GetOptimisticClause(R),
                                            fieldValues: privateConn.GetDictFrom(names, values));
                    if (res!= 0)  return 1;
                    return 0;
                }
                catch (Exception e){
                    LastError = e.Message;
                    R.RejectChanges();
                    await reselect(R, DataRowVersion.Default);
                    return 0;
                }
            }


            /// <summary>
            /// Re-fetch a row from DB by primary key
            /// </summary>
            /// <param name="R"></param>
            /// <param name="ver"></param>
            async Task reselect(DataRow R, DataRowVersion ver) {
                await privateConn.SelectIntoTable(R.Table, filter:QueryCreator.FilterKey(R, ver,  forPosting:false));
            }

            /// <summary>
            /// Reads from DB all views that contains data from other tables than 
            ///  primary table of the view 
            /// </summary>
            public async Task ReselectAllViews() {
                foreach (DataTable T in DS.Tables) {
                    if (T.TableName == T.tableForPosting()) continue;
                    bool HasExtraColumns = false;
                    foreach (DataColumn C in T.Columns) {
                        if (C.PostingColumnName() == null) {
                            HasExtraColumns = true;
                            break;
                        }
                    }
                    if (!HasExtraColumns) continue;
                    foreach (DataRow R in T.Rows) {
                        if ((R.RowState == DataRowState.Added) ||
                            (R.RowState == DataRowState.Modified)) {
                            R.AcceptChanges();
                            await reselect(R, DataRowVersion.Default);
                        }
                    }
                }
            }


            #endregion
        }

        /// <summary>
        /// DataSet beng posted
        /// </summary>
	    public DataSet DS;


        /// <summary>
        /// Return a new RowChange linked to a Row
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        virtual public RowChange GetNewRowChange(DataRow R) {
            return new RowChange(R);
        }


        /// <summary>
        /// Set the row order for posting a table to db
        /// </summary>
        /// <param name="T"></param>
        /// <param name="order"></param>
        public static void SetPostingOrder(DataTable T, string order) {
            T.ExtendedProperties["postingorder"] = order;
        }

        /// <summary>
        /// Gets the order for posting rows in the db
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static string GetPostingOrder(DataTable T) {
            return T.ExtendedProperties["postingorder"] as string;
        }

        /// <summary>
        /// Gives a string for using as a condition to test for assuring that no
        ///  changes have been made to the row to update/delete since it was read. 
        ///  Uses Posting Table/Columns names
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        virtual public string GetOptimisticClause(DataRow R) {
            return QueryCreator.CompareAllFields(R, DataRowVersion.Original, true, qhs);
        }

        /// <summary>
        /// Se le regole in input non sono ignorabili restituisce il set in input. Altrimenti calcola un nuovo set di regole PRE.
        /// </summary>
        /// <param name="resultList"></param>
        /// <param name="ignoredMessages">Messages to ignore</param>
        /// <returns></returns>
	    async Task<ProcedureMessageCollection> getPreChecks(ProcedureMessageCollection resultList, HashSet<string> ignoredMessages) {
            if ((resultList != null) && (!resultList.CanIgnore)) return resultList;

	        resultList = GetEmptyMessageCollection();
	        foreach (SingleDatasetPost p in allPost) {
	            var curr = await p.GetPreChecks(ignoredMessages);
	            resultList.Add(curr);
	        }
	        return resultList;

	    }
        /// <summary>
        /// Should return true if it is allowed to Post a DataRow to DB
        /// As Default returns Conn.CanPost(R)
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        protected virtual bool canPost(DataRow r) {
            return conn.Security.CanPost(r);
        }

        /// <summary>
        /// Constructor. After building a PostData, it's necessary to call
        ///  InitClass.
        /// </summary>
        public PostData() {
			//            WellObject = false;
			refresh_dataset=true;
            AutoIgnore = false;
			lasterror="";
		}

		/// <summary>
		/// Gets the set of business rule for a given set of changes
		/// </summary>
		/// <param name="Cs"></param>
		/// <returns></returns>
		virtual protected async Task<MetaDataRules> getRules(RowChangeCollection Cs){
			return new MetaDataRules();
		}


        protected IDataAccess conn;
		protected QueryHelper qhs;

        /// <summary>
        /// List of PostData classes that concurr in the transaction
        /// </summary>
	    protected List<SingleDatasetPost> allPost = new List<SingleDatasetPost>();


	    /// <summary>
	    /// Initialize PostData. Must be called before DO_POST
	    /// </summary>
	    /// <param name="ds">DataSet to handle</param>
	    /// <param name="conn">Connection to the DataBase</param>
	    /// <remarks>This function must be called AFTER the changes have
	    ///  been applied to DS.</remarks>
	    /// <returns>error string if errors, null otherwise</returns>   
	    public virtual async Task<string> InitClass(DataSet ds, IDataAccess conn) {
	        if (this.conn == null) {
	            this.conn = conn;
	        }

	        this.qhs = conn.GetQueryHelper();

	        if (DS == null) {
	            DS = ds;
	        }
            var single = new SingleDatasetPost(ds, conn);
            await single.Init(this);

            allPost.Add(single);
	        return null;
        }

		/// <summary>
		/// Calls business logic and return error messages
		/// </summary>
		/// <param name="post">if true, it is a "AFTER-POST" check</param>
		/// <param name="RC">Collection of changes posted to the DB</param>
		/// <returns>Collection of Error/warings</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		protected virtual async Task<ProcedureMessageCollection> callChecks(bool post, RowChangeCollection RC){
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			return new ProcedureMessageCollection();
		}







		/// <summary>
		/// Gets a new DataJournaling object 
		/// </summary>
		/// <param name="Conn"></param>
		/// <param name="Cs"></param>
		/// <returns></returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		protected virtual async Task<DataJournaling> getJournal(IDataAccess Conn, RowChangeCollection Cs) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			return new DataJournaling();
	    }

        /// <summary>
        /// Checks if there is any rowchange in all dataset
        /// </summary>
        /// <returns></returns>
        bool someChange() {
	        foreach (SingleDatasetPost p in allPost) {
	            if (p.RowChanges.Count > 0) return true;
	        }
	        return false;
	    }


        /// <summary>
        /// Completes the row to be changed with createuser,createtimestamp,
        /// lastmoduser, lastmodtimestamp fields, depending on the operation type
        ///  and calls CalculateFields for each DataRow involved.
        ///  This CAN be done OUTSIDE the transaction.
        /// </summary>
        async Task prepareForPosting() {
            foreach (var p in allPost) {
                await p.PrepareForPosting();
            }
        }

	    void emptyCache() {
            foreach (var p in allPost) {
                p.RowChanges.EmptyCache();
            }
        }

	    void clear_precheck_msg() {
            foreach (var p in allPost) {
                p.precheck_msg = null;
            }
        }

        async Task<ProcedureMessageCollection> getPostChecks() {
	        var handle = StartTimer("do_all_postcheck");
            var res = GetEmptyMessageCollection();
            foreach (var p in allPost) {
                var thisRes = await p.GetPostChecks(ignoredMessages);
                res.Add(thisRes);
            }
            StopTimer(handle);
            return res;
        }

        


        async Task getAllJournal() {
            foreach (var p in allPost) {
                await p.GetJournal();
            }
        }

	    async Task<bool> doAllPhisicalPostBatch() {
		    var handle = StartTimer("doAllPhisicalPostBatch");

            foreach (var p in allPost) {
                bool thisRes = await p.writeToDatabase();
                if (!thisRes) { //Mette in lasterror l'errore verificatosi nel singleDatasetPost
                    if (lasterror == null) lasterror = "";
                    lasterror += p.LastError+"\r\n";
                    StopTimer(handle);
                    return false;                    
                }                
            }
            StopTimer(handle);
            return true;            
        }

	    async Task<bool> doAllJournaling() {
            foreach (var p in allPost) {
                if (!await p.DoJournal()) return false;
            }
            return true;
            
        }

	    async Task<(bool result, string errMsg)> all_externalUpdate() {
            foreach (var p in allPost) {
	            var (res, errMsg) = await myDoExternalUpdate(p.DS);
	            lasterror = errMsg;
                if (!res) return (false,errMsg);
            }
            return (true,null);
	    }

        /// <summary>
        /// Reads all data about views (also in inner posting classes)
        /// </summary>
	    public async Task ReselectAllViewsAndAcceptChanges() {
	        var handle = StartTimer("reselectAllViewsAndAcceptChanges");
	        foreach (var p in allPost) {
	            await p.ReselectAllViews();
	            var h = StartTimer("reselectAllViewsAndAcceptChanges - AcceptChanges");
	            p.DS.AcceptChanges();
                StopTimer(h);
	        }

	        var inner = GetInnerPosting(DS);
	        if (inner != null) {
		        await inner.ReselectAllViewsAndAcceptChanges();
	        }
            StopTimer(handle);
	    }

	    void startPosting() {
	        foreach (var p in allPost) {
	            p.StartPosting(conn);
	        }
	    }

	    void stopPosting() {
                foreach (var p in allPost) {
                    p.StopPosting(conn);
                }
            }
        #region DO POST 

        /// <summary>
        /// Instruct to successively ignore a set of messages
        /// </summary>
        /// <param name="msgs"></param>
	    public void AddMessagesToIgnore(HashSet<string> msgs) {
	        foreach (string s in msgs) {
	            ignoredMessages.Add(s);
	        }
	    }

        HashSet<string> ignoredMessages = new HashSet<string>();

        /// <summary>
        /// Fills the list of ignored messages with the collection specified
        /// </summary>
        /// <param name="msgs"></param>
        public void SetIgnoredMessages(ProcedureMessageCollection msgs) {
            if (resultList == null) {
                resultList = GetEmptyMessageCollection();
            }
            else {
                resultList.Clear();
            }

            resultList.AddRange(msgs);
        }

	    int totalDeletes(out string msg) {
	        int nDel = 0;
	        msg = "";
	        foreach (var p in allPost) {
	            nDel += p.RowChanges.nDeletes;
	        }

	        if (nDel > 10000) {
	            foreach (var p in allPost) {
	                
	                foreach (DataTable t in p.DS.Tables) {
	                    int currNdel = 0;
	                    foreach (DataRow r in t.Rows) {
	                        if (r.RowState == DataRowState.Deleted) currNdel++;
	                    }

	                    if (currNdel > 10) {
	                        msg += $"Table {t.TableName}: {currNdel} deletion;\n\r";
	                    }
	                }
	            }
	        }

	        return nDel;
	    }
	   
        /// <summary>
        /// Call afterPost on all inner posting classes
        /// </summary>
        /// <param name="committed"></param>
	    async Task recursiveCallAfterPost(bool committed) {
	        var inner = GetInnerPosting(DS);
	        while (inner!=null) {
	            await inner.AfterPost(committed);
	            inner = inner.GetInnerPosting();	          
	        }
	        
	    }
       

        /// <summary>
        /// Marks a dataset so that when it will be posted it will use an innerPosting class
        /// </summary>
        /// <param name="d"></param>
        /// <param name="post"></param>
	    public static void SetInnerPosting(DataSet d, IInnerPosting post) {
	        d.ExtendedProperties["MDL_innerDoPostService"] = post;
	    }

        /// <summary>
        /// Gets the inner posting class that will be used when a dataset will be posted
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
	    public static IInnerPosting GetInnerPosting(DataSet d) {
	        return  d.ExtendedProperties["MDL_innerDoPostService"] as IInnerPosting;
	    }


        /// <summary>
        /// Do the post as a service, same as doPost except for output data kind
        /// </summary>
        /// <returns></returns>
        private async Task<ProcedureMessageCollection> internalSaveData() {
            if (!someChange()) return GetEmptyMessageCollection();

            var innerPostingClass = GetInnerPosting(DS);
            string IgnoredError = conn.LastError;
            if (!string.IsNullOrEmpty(IgnoredError)) {
                ErrorLogger.Logger.MarkEvent($"Error: {IgnoredError} has been IGNORED before starting POST!");
            }

            //Calc rows related to changes & attach them to RowChanges elements
            //This is a necessary step to call DO_PRE_CHECK & DO_POST_CHECK
            //DO_CALC_RELATED();
            //If resultList already exists, add them to Ignored Messages
            resultList?.AddMessagesToIgnore(ignoredMessages);

            resultList = GetEmptyMessageCollection();
            await getAllJournal();

            await prepareForPosting();
          
            var result=true; //se false ci sono errori bloccanti
            try {
                clear_precheck_msg();

                if (!innerPosting) {
                    try {
                        await conn.Open(); //if inner, connection is already opened
                        await conn.BeginTransaction(IsolationLevel.ReadCommitted);
                    }
                    catch (Exception e) {
                        resultList.AddDBSystemError($"Errore creando la transazione.\n{e.Message}");
                        return resultList;
                    }
                    finally {
                        await conn.Close();
					}              
                }

                int nDeletes = totalDeletes(out var logDel);

                //Ricalcola le regole PRE e ignora eventuali regole già ignorate, ma se le regole sono non ignorabili non fa nulla
                resultList = await getPreChecks(resultList, ignoredMessages);
                //ResultList.SkipMessages(IgnoredMessages);//lo fa già silentDoAllPrecheck                

                if (!conn.HasValidTransaction()) {
                    var err = conn.LastError ?? "Nessun dettaglio disponibile";
                    resultList.AddDBSystemError($"Errore durante l\'interrogazione della logica business. (Precheck).\nDettaglio: {err}");
                }
                if (resultList.CanIgnore && AutoIgnore) {
                    resultList.Clear();
                }

                if (resultList.Count > 0 && resultList.CanIgnore == false) {
                    result=false; //non procede con regola check bloccanti
                }

                //DO PHYSICAL CHANGES (auto-filling lastmoduser/timestamps fields)
                if (result) {
                    emptyCache();
                    foreach(DataTable t in DS.Tables)RowChange.ClearMaxCache(t);
                    result = await doAllPhisicalPostBatch();
                    if (!result) resultList.AddDBSystemError($"Errore nella scrittura sul database:{lasterror}");
                }                

                if (result) {
                    result = await doAllJournaling();
                    if (!result) resultList.AddDBSystemError("Si sono verificati errori durante la scrittura nel log");
                }

                if (result) {  //DB WRITE SUCCEEDED
                               //EVALUATE POST - CONDITION
                    ProcedureMessageCollection postResultList = await getPostChecks();
                    resultList.Add(postResultList);
                    resultList.SkipMessages(ignoredMessages);

                    if (!conn.HasValidTransaction()) {
                        var err = conn.LastError ?? "Nessun dettaglio disponibile";
                        resultList.AddDBSystemError("Errore durante l'interrogazione della logica business. (Postcheck).\nDettaglio: " + err);
                    }

                    if (resultList.CanIgnore && AutoIgnore) {
                        resultList.Clear();
                    }                

                    if (resultList.Count > 0) result = false;

                    if (result) {
                        (result,lasterror) = await all_externalUpdate();
                        if (!result) {
                            resultList.AddDBSystemError("Errore nella scrittura su DB. Le routine di aggiornamento hanno fallito.");
                        }
                    }

                    if (result && innerPostingClass != null) {
                        //Effettua il post interno
                        await innerPostingClass.InitClass(DS,conn);
                        innerPostingClass.SetInnerPosting(ignoredMessages);

                        var innerRules = await innerPostingClass.SaveData();
                        innerRules.SkipMessages(ignoredMessages);

                        if (innerRules.CanIgnore && AutoIgnore) {
                            innerRules.Clear();
                        }
                        if (innerRules.Count > 0) {
                            resultList.Add(innerRules);
                            result = false;
                        }                       
                    }

                    if (result) {
                        if (!innerPosting) {
                            try {
                                await conn.Commit();
                                if (nDeletes > 10000) {
                                    ErrorLogger.Logger.logException(logDel);
                                }
                            }
                            catch (Exception ex) {
                                resultList.AddDBSystemError("Errore nella commit:" + ex.Message);
                                lasterror = ex.Message;
                                result = false;
                            }
                        }                        
                    }                    
                }
            }
            catch (Exception E) {
                if (resultList == null) resultList = GetEmptyMessageCollection();
                resultList.AddDBSystemError(ErrorLogger.GetErrorString(E));
                resultList.CanIgnore = false;
                resultList.PostMsgs = true;
                Trace.Write("Error:" + ErrorLogger.GetErrorString(E) + "\r", "PostData.DO_POST_SERVICE\r");
                lasterror = E.Message;
                result = false;
            }

            if ((result==false) || resultList.Count > 0) {
                //rolls back transaction
                string msg2= innerPosting? null:await getException(conn.Rollback)();
                if (msg2 != null) {
                    //result = false; unused
                    lasterror += msg2;
                    resultList.AddDBSystemError("Errore nella scrittura su DB. Rollback fallito.");
                }
                if (!innerPosting) await getException(conn.Close)();
                resultList.PostMsgs = true; //necessario se DO_POST_CHECK non è stata chiamata
                if (!innerPosting) await recursiveCallAfterPost(false);
                return resultList;
            }

            if (!innerPosting) {
	            await conn.Close();
                await ReselectAllViewsAndAcceptChanges();
            }

            if (innerPostingClass != null && !innerPosting) {
                await recursiveCallAfterPost(true);
            }
            return resultList; //è una lista vuota se tutto è andato bene

        }

	    delegate Task TaskFun();
        delegate Task<string> StringFun();

        StringFun getException(TaskFun f) {
            return async () => {
                try {
                    await f();
                    return null;
                }
                catch (Exception e) {
                    return e.Message;
                }
            };
            
		}
        

        /// <summary>
        /// True if this posting is placed insider another posting
        /// </summary>
	    public bool innerPosting = false;
        /// <summary>
        /// Do ALL the necessary operation about posting data to DataBase. 
        /// If fails, rolles back all (eventually) changes made.
        /// Management of the transaction commit / rollback is done HERE!
        /// </summary>
        /// <returns>An empty Message Collection if success, Null if severe errors</returns>
        /// <remarks>If it succeeds, DataSet.AcceptChanges should be called</remarks>
        public virtual async Task<ProcedureMessageCollection> SaveData(){
            startPosting();

            ProcedureMessageCollection res = await internalSaveData();
            stopPosting();
            return res;
        }


		#endregion

        /// <summary>
        /// Updates a remote service with data involved in this transaction
        /// </summary>
        /// <param name="D"></param>
        /// <param name="ErrMsg">Error msg or null if all ok</param>
        /// <returns>true if operation has been correctly performed</returns>
        public delegate Task<(bool result, string errMsg)> DoExternalUpdateDelegate(DataSet D);

        /// <summary>
        /// Delegate to be runned when DataSet has been posted on db, before committing the transaction.
        /// If it throws an exception the transaction is rolledback and the Exception message showed to the user
        /// </summary>
        public DoExternalUpdateDelegate DoExternalUpdate;

        async Task<(bool result, string errMsg)> myDoExternalUpdate(DataSet d) {
            if (DoExternalUpdate == null) return (true, null);
            try {
                return await DoExternalUpdate(d);
            }
            catch (Exception E) {
                return  (false,ErrorLogger.GetErrorString(E));;
            }
        }


	}     

    /// <summary>
    /// Default inner posting class, implements InnerPosting interface
    /// </summary>
    public class Base_InnerPoster : IInnerPosting {
        /// <summary>
        /// 
        /// </summary>
        public PostData p;


        //private IDataAccess conn;
        HashSet<string> msgsToIgnore= new HashSet<string>();

        /// <summary>
        /// inner PostData class
        /// </summary>
        //PostData innerPostClass { get; }
        public  HashSet<string> HashMessagesToIgnore() {
            return msgsToIgnore;
        }


        /// <summary>
        /// Called to initialize the class, inside the transaction
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="conn"></param>
        public virtual async Task InitClass(DataSet ds, IDataAccess conn) {
            //this.conn = conn;
            msgsToIgnore.Clear();
        }


        /// <summary>
        /// Unisce i messaggi dati a quelli finali
        /// </summary>
        /// <param name="messages"></param>
        public void MergeMessages(ProcedureMessageCollection messages) {
            messages.SkipMessages(msgsToIgnore);

        }

		/// <summary>
		/// Called after data has been committed or rolled back
		/// </summary>
		/// <param name="committed"></param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task AfterPost(bool committed) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		}

        /// <summary>
        /// Reads all data in dataset views 
        /// </summary>
        public virtual async Task ReselectAllViewsAndAcceptChanges() {
            await p.ReselectAllViewsAndAcceptChanges();
        }

        /// <summary>
        /// Get inner posting class that will be used during posting 
        /// </summary>
        /// <returns></returns>
        public virtual IInnerPosting GetInnerPosting() {
            if (p == null) return null;
            return PostData.GetInnerPosting(p.DS);
        }

        /// <summary>
        /// Set inner posting messages that have already been raisen
        /// </summary>
        /// <param name="ignoredMessages"></param>
        public virtual void SetInnerPosting(HashSet<string> ignoredMessages) {
            foreach (var s in ignoredMessages) {
                msgsToIgnore.Add(s);
            }
        }

        /// <summary>
        /// Proxy to inner DO_POST_SERVICE
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ProcedureMessageCollection> SaveData() {
            //effettua tutte le operazioni che avrebbe fatto
            // Il beforePost è già stato invocato correttamente
            var msg = await p.SaveData();
            MergeMessages(msg);
            return msg;
        }
    }
}
