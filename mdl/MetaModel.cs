using System;
using System.Collections.Generic;
using System.Data;
using static mdl_utils.MetaProfiler;
using System.Linq;
using q  = mdl.MetaExpression;

namespace mdl {

    public enum TableAction {
        beginLoad, endLoad,startClear, endClear
    }

    /// <summary>
    /// Delegates for custom field-calculations
    /// </summary>
    public delegate void CalcFieldsDelegate(DataRow R, string list_type);

    /// <summary>
    /// Interface for a model manager
    /// </summary>
    public interface IMetaModel {

        void Clear(DataTable T);

        void ClearActions(DataTable T, TableAction actionType, Action<DataTable> a);
        void SetAction(DataTable T,  TableAction actionType, Action<DataTable> a, bool clear=false);
        void InvokeActions(DataTable T, TableAction actionType );


        /// <summary>
        /// Mark a table for skipping security controls
        /// </summary>
        /// <param name="T"></param>
        /// <param name="value"></param>
        void SetSkipSecurity(DataTable T, bool value);

        /// <summary>
        /// Check if a table ha been marked as SkipSecurity
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        bool IsSkipSecurity(DataTable T);

        /// <summary>
        /// Unlink R from parent-child relation with primary table. I.E., R stops being a child of main row. 
        /// If R becomes unchanged, it is removed from DataSet
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        DataRow UnlinkDataRow(DataTable primaryTable, DataRow r);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        string GetNotEntityChildFilter(DataTable t);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="filter"></param>
        void SetNotEntityChildFilter(DataTable t, string filter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="child"></param>
        void MarkTableAsNotEntityChild(DataTable primary, DataTable child);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="child"></param>
        /// <param name="relName"></param>
        void MarkTableAsNotEntityChild(DataTable primaryTable, DataTable child, string relName);

        /// <summary>
        /// Set the table as NotEntitychild. So the table isn't cleared during freshform and refills
        /// </summary>
        /// <param name="T"></param>
        /// <param name="ParentRelName"></param>
        void AddNotEntityChild(DataTable T, string ParentRelName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="child"></param>
        void AddNotEntityChild(DataTable primaryTable, DataTable child);


        /// <summary>
        /// Establish if R is a Entity-SubEntity relation
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool IsSubEntityRelation(DataRelation R);
        bool IsSubEntity(DataTable child, DataTable parent);


        /// <summary>
        ///  Set the extra parameter for a table
        /// </summary>
        /// <param name="t"></param>
        /// <param name="o"></param>
        void SetExtraParams(DataTable t, object o);

        /// <summary>
        /// Get the extra parameter from a table
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        object GetExtraParams(DataTable t);

        /// <summary>
        /// Remove a table from being a  NotEntitychild
        /// </summary>
        /// <param name="T"></param>        
        void UnMarkTableAsNotEntityChild(DataTable T);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ds"></param>
        void AllowAllClear(DataSet ds);

        /// <summary>
        /// Check if an entity has changes
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="primary"></param>
        /// <param name="sourceRow"></param>
        /// <param name="isSubentity"></param>
        /// <returns></returns>
        bool HasChanges(DataTable primary, DataRow sourceRow, bool isSubentity);

      
        /// <summary>
        /// Check if a table is not an entity child
        /// </summary>
        /// <param name="childTable"></param>
        /// <returns></returns>
        bool IsNotEntityChild(DataTable childTable);

        /// <summary>
        /// Check whether a Table as a cached one
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        bool IsCached(DataTable T);

       


        /// <summary>
        /// Establish a Table as a not cached one
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        void UnCacheTable(DataTable T);

        /// <summary>
        /// Establish a filtered cached table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="addBlankRow"></param>
        void CacheTable(DataTable T, object filter=null, string sort=null, bool addBlankRow=false);

        /// <summary>
        /// Establish that if a blank row will be added to the table when it is emptied
        /// </summary>
        /// <param name="T"></param>
        void MarkToAddBlankRow(DataTable T);

        /// <summary>
        /// Adds an empty (all fields blank) row to a table if the table has been previously marked
        ///  with MarkToAddBlankRow and the table empty
        /// </summary>
        /// <param name="T"></param>
        void CheckBlankRow(DataTable T);

        /// <summary>
        /// Sets a filter that will be applied  every times that a table will be read from db
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        void SetStaticFilter(DataTable T, object filter);

        /// <summary>
        /// blocks further reads by the framework of a table 
        /// </summary>
        /// <param name="T"></param>
        void LockRead(DataTable T);

        /// <summary>
        /// Set a table as "read". Has no effect if table isn't a child table
        /// </summary>
        /// <param name="T"></param>
        void TableHasBeenRead(DataTable T);

        /// <summary>
        /// blocks empty actions by the framework of a table 
        /// </summary>
        /// <param name="T"></param>
        void DenyClear(DataTable T);

        /// <summary>
        /// allow empty actions by the framework of a table 
        /// </summary>
        /// <param name="T"></param>
        void AllowClear(DataTable T);


        /// <summary>
        /// Checks if a table can be cleared by the framework
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        bool CanClear(DataTable T);

        /// <summary>
        /// Check if a blank row will be added to the table when it is emptied
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        bool MarkedToAddBlankRow(DataTable T);


        /// <summary>
        /// Checks if a table can be read during dataset operations
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        bool CanRead(DataTable T);

        /// <summary>
        /// Gets evaluated fields, taking the ExtendedProperties QueryCreator.IsTempColumn
        ///   and considering it in a "childtable.childfield" format
        /// Also calls CalcFieldsDelegate of the table for every rows (when needed)
        /// </summary>
        /// <param name="T"></param>
        void GetTemporaryValues(DataTable T, ISecurity conn);

        /// <summary>
        /// Gets evaluated fields, taking the ExtendedProperties QueryCreator.IsTempColumn
        ///   and considering it in a "childtable.childfield" format
        /// Also calls CalcFieldsDelegate of the table for every rows (when needed)
        /// Also invokes CalculateTable(r.Table)
        /// </summary>
        /// <param name="T"></param>
        void GetTemporaryValues(DataRow r, ISecurity conn);

        /// <summary>
        /// Gets calculated fields from related table (Calculated fields are those 
        ///		provided with an expression). 
        /// </summary>
        /// <param name="R"></param>
        void CalcTemporaryValues(DataRow R, ISecurity conn);

        /// <summary>
        /// Evaluates custom fields for every row of a Table. Calls the delegate linked to the table,
        ///  corresponding to the MetaData.CalculateFields() virtual method (if it has been defined).
        /// </summary>
        /// <param name="T"></param>
        /// <remarks>No action is taken on deleted rows. Unchanged rows remain unchanged anyway</remarks>
        void CalculateTable(DataTable T);

        /// <summary>
        /// Evaluates custom fields for a single row. Calls the delegate linked to the table,
        ///  corresponding to the MetaData.CalculateFields() virtual method (if it has been defined).
        /// </summary>
        /// <param name="R"></param>
        /// <remarks>No action is taken on deleted rows. Unchanged rows remain unchanged anyway</remarks>
        void CalculateRow(DataRow R);

        void ComputeRowsAs(DataTable T, string ListingType, CalcFieldsDelegate Calc);

        void CopyDataRow(DataTable DestTable, System.Data.DataRow ToCopy);

    }

  
    /// <summary>
    /// Manages conventions over the model
    /// </summary>
    public class MetaModel : IMetaModel {
	    private const string IsTempTable  = "IsTemporaryTable";

	    static Dictionary<TableAction, string> actionNames = new Dictionary<TableAction, string>(){
            { TableAction.beginLoad , "mdl_beginLoad" },
            { TableAction.endLoad , "mdl_endLoad" },
            { TableAction.startClear , "mdl_startClear" },
            { TableAction.endClear , "mdl_endClear" }
         


        };

        /// <summary>
        /// Clears a DataTable setting the rowindex of the linked grid to 0
        /// </summary>
        /// <param name="T"></param>
        public virtual void Clear(DataTable T) {
            if (T.Rows.Count == 0) return;
            var metaclear = StartTimer($"MyClear * {T.TableName}");
            InvokeActions(T, TableAction.startClear);
            InvokeActions(T, TableAction.beginLoad);
            
            T.BeginLoadData();
            T.Clear();

            T.EndLoadData();
            InvokeActions(T, TableAction.endLoad);
            InvokeActions(T, TableAction.endClear);

            StopTimer(metaclear);
        }

        public virtual void ClearActions(DataTable T, TableAction actionType, Action<DataTable> a) {
            T.ExtendedProperties[actionNames[actionType]] = null;
        }
        public virtual void SetAction(DataTable T, TableAction actionType, Action<DataTable> a, bool clear = false) {
            TableEventManager actions = clear? null: T.ExtendedProperties[actionNames[actionType]] as TableEventManager;
            if (actions == null) {
                actions = new TableEventManager();
                T.ExtendedProperties["mdl_EndLoad"] = actions;
            }
            actions.AddAction(a);
        }
        public virtual void InvokeActions(DataTable T, TableAction actionType) {
            var actions = T.ExtendedProperties[actionNames[actionType]] as TableEventManager;
            actions?.InvokeAction(T);
        }



        /// <summary>
        /// Tells if a table should be cleared and read again during a refresh.
        /// Cached tables are not read again during refresh if they have been already been read
        /// </summary>
        /// <param name="T"></param>
        /// <returns>true if table should be read</returns>
        public bool CanRead(DataTable T){
            if (T.ExtendedProperties["cached"]==null) return true;
            if (T.ExtendedProperties["cached"].ToString()=="0")return true;
            return false;
        }

        public bool IsSubEntityRelation(DataRelation R) {
	        if (R.ExtendedProperties["isSubentity"] != null) return (bool) R.ExtendedProperties["isSubentity"];
	        var Parent = R.ParentTable;
	        var Child = R.ChildTable;
			HashSet<string> parentKeys = new HashSet<string>();
            foreach (var K in Parent.PrimaryKey) {
                parentKeys.Add(K.ColumnName);
            }
            HashSet<string> childKeys = new HashSet<string>();
            foreach (var K in Child.PrimaryKey) {
                childKeys.Add(K.ColumnName);
            }

            foreach (var cc in R.ParentColumns.Zip(R.ChildColumns)) {
                var isParentKey = parentKeys.Contains(cc.First.ColumnName);
                var isChildKey = childKeys.Contains(cc.Second.ColumnName);
                if (isParentKey && isChildKey) {
                    parentKeys.Remove(cc.First.ColumnName);
                } 
                else { 		        
			        R.ExtendedProperties["isSubentity"] = false;
			        return false;
		        }
	        }

	        if (parentKeys.Count>0) {
		        R.ExtendedProperties["isSubentity"] = false;
		        return false;
	        }

	        R.ExtendedProperties["isSubentity"] = true;			
	        return true;
		}

        /// <summary>
		/// Tells whether a Child Table is a Sub-Entity of Parent Table.
		/// This is true if:
		/// Exists some relation R that links primary key of Parent to a subset of the 
		///  primary key of Child
		/// </summary>
		/// <param name="Child"></param>
		/// <param name="Parent"></param>
		/// <returns></returns>
		public bool IsSubEntity(DataTable Child, DataTable Parent){
            return Parent.ChildRelations.Enum().Any( 
                r => r.ChildTable.TableName == Parent.TableName && IsSubEntityRelation(r) );
		}

        /// <summary>
        /// Mark a table for skipping security controls
        /// </summary>
        /// <param name="T"></param>
        /// <param name="value"></param>
        public void SetSkipSecurity(DataTable T,bool value) {
            T.ExtendedProperties["SkipSecurity"] = value ? true : (object) null;
        }
        
        /// <summary>
        /// Check if a table ha been marked as SkipSecurity
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public bool IsSkipSecurity(DataTable T) {
            return T.ExtendedProperties["SkipSecurity"] != null;
        }

        /// <summary>
        /// Must be called for combobox-related tables
        /// </summary>
        /// <param name="T"></param>
        public void MarkToAddBlankRow(DataTable T){
            T.ExtendedProperties["AddBlankRow"]=true;
        }

        /// <summary>
        /// Check if a table was marjed to add a blank row 
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public bool MarkedToAddBlankRow(DataTable T){
            return T.ExtendedProperties["AddBlankRow"]!=null;
        }

        /// <summary>
        /// Apply a filter on a table during any further read
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        public  void SetStaticFilter(DataTable T, object filter){
            T.ExtendedProperties["filter"]=filter;
        }

        
        

        internal class TableEventManager {
	        delegate void tableHandler(DataTable t);
	        event tableHandler actions;

	        public void AddAction(Action<DataTable> a) {
		        actions += new tableHandler(a);
	        }

	        public void InvokeAction(DataTable t) {
                actions?.Invoke(t);
	        }

        }

     


        /// <summary>
        /// Deny table clear when DO_GET() is called. If this is not called, a
        ///   table that is not cached, entity or subentity will be cleared during DO_GET
        /// </summary>
        /// <param name="T"></param>
        public  void DenyClear(DataTable T){
            T.ExtendedProperties["DenyClear"]="y";
        }

        /// <summary>
        /// Re-Allow table clear when DO_GET() is called. Undoes the effect of a DenyClear
        /// </summary>
        /// <param name="T"></param>
        public  void AllowClear(DataTable T){
            T.ExtendedProperties["DenyClear"]=null;
        }

        /// <summary>
        /// Tells if Table will be cleared during next DO_GET()
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public  bool CanClear(DataTable T){
            if (T.ExtendedProperties["DenyClear"]==null) return true;
            return false;
        }

        /// <summary>
        /// Tells GetData to read T once for all
        /// </summary>
        /// <param name="T"></param>
        public void CacheTable(DataTable T){
            T.ExtendedProperties["cached"]="0";
        }

        /// <summary>
        /// Tells GetData to read T once for all
        /// </summary>
        /// <param name="T"></param>
        public void UnCacheTable(DataTable T){
            T.ExtendedProperties["cached"]=null;
        }

        
        /// <summary>
        /// Table T will never be read. It is marked like a cached table that has already been read.
        /// </summary>
        /// <param name="T"></param>
        public void LockRead(DataTable T){
            T.ExtendedProperties["cached"]="1";
        }

        /// <summary>
        /// Set a table as "read". Has no effect if table isn't a chaed table
        /// </summary>
        /// <param name="T"></param>
        public void TableHasBeenRead(DataTable T){
            if (T.ExtendedProperties["cached"]==null) return;
            if (T.ExtendedProperties["cached"].ToString()=="0")LockRead(T);
        }

        /// <summary>
        /// Returns true if table is cached (the table may or may not 
        ///  have been read) 
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public bool IsCached(DataTable T){
            if (T.ExtendedProperties["cached"]==null) return false;
            return true;
        }

        //private DataTable primaryTable;
        //private DataSet ds { get { return primaryTable?.DataSet; } }

        /// <summary>
        /// class instance of cqueryhelper
        /// </summary>
        protected QueryHelper q = MetaFactory.factory.getSingleton<CQueryHelper>();

        static QueryHelper qhc  = MetaFactory.factory.getSingleton<CQueryHelper>();

        /// <inheritdoc />
        public virtual void SetExtraParams(DataTable t, object o) {
            t.ExtendedProperties["ExtraParameters"] = o;
        }

        /// <inheritdoc />
        public virtual object GetExtraParams(DataTable t) {
            return t.ExtendedProperties["ExtraParameters"];
        }


        /// <summary>
        /// Returns true if there are unsaved changes
        /// </summary>
        /// <returns></returns>
        public bool HasChanges( DataTable mainTable, DataRow sourceRow, bool isSubentity) {
            var handle = StartTimer("HasUnsavedChanges()");
            try {
	            DataSet ds = mainTable.DataSet;
	            mainTable.DataSet.RemoveFalseUpdates();
                //return ds.HasChanges();

                //Per una subentità (detail form) confronta i dati con quelli dell'origine

                if (!isSubentity) return ds.HasChanges();
                if (mainTable.Rows.Count == 0) return false;
                var myRow = mainTable.Rows[0];
                if (xVerifyChangeChilds(sourceRow.Table,  myRow)) return true;
                return xVerifyChangeChilds( mainTable, sourceRow);
            }
            finally {
                StopTimer(handle);
            }
        }


        /// <summary>
        /// Vede se ci sono differenze nella riga rSource confrontandola con quella in tDest, insieme alla child
        /// </summary>
        /// <param name="TDest"></param>
        /// <param name="RSource"></param>
        /// <returns></returns>
        public static bool xVerifyChangeChilds(DataTable tDest, DataRow rSource) {
            DataTable tSource = rSource.Table;
            //if (RSource.RowState != DataRowState.Unchanged) return true;
            if (xVerifyRowChange(tDest, rSource)) return true;
            DataSet dest = tDest.DataSet;
            DataSet rif = rSource.Table.DataSet;
            foreach (DataRelation Rel in tSource.ChildRelations) {
                if (!dest.Tables.Contains(Rel.ChildTable.TableName)) continue;
                if (!GetData.CheckChildRel(Rel)) continue; //not a subentityrel
                foreach (DataRow Child in rSource.getChildRows(Rel)) {
                    if (xVerifyChangeChilds(dest.Tables[Child.Table.TableName], Child)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Restituisce true se ci sono differenze nella riga RSource considerata cercandola per chiave in TDest
        /// </summary>
        /// <param name="TDest"></param>
        /// <param name="RSource"></param>
        /// <returns></returns>
        static bool xVerifyRowChange(DataTable TDest, DataRow RSource) {
            if (RSource.RowState == DataRowState.Deleted) return false;
            //string source_unaliased = DataAccess.GetTableForReading(RSource.Table);

            //DataTable TDest= Dest.Tables[source_unaliased];
            var TSource = RSource.Table;
            DataRow[] found = TDest.Select(qhc.CmpKey(RSource));
            if (found.Length == 0) return true;
            foreach (DataColumn C in TSource.Columns) {
                if (C.IsTemporary()) continue;
                if (!TDest.Columns.Contains(C.ColumnName)) continue;
                if (TDest.Columns[C.ColumnName].IsTemporary()) continue;
                if (found[0][C.ColumnName].Equals(RSource[C.ColumnName])) continue;
                return true; //Has changes
            }
            return false;
        }

        /// <summary>
        /// Unlink R from parent-child relation with primary table. I.E., R becomes a not-child of main row. 
        /// If R becomes unchanged, it is removed from DataSet
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        public DataRow UnlinkDataRow(DataTable primaryTable, DataRow R) {
            if (R == null) return null;
            var SourceTable = R.Table;
            //Unlink R from parent-child relation with primary table.
            DataRelation Rfound = null;
            foreach (DataRelation Rel in primaryTable.ChildRelations) {
                if (Rel.ChildTable == SourceTable) {
                    Rfound = Rel;
                    foreach (DataColumn C in Rfound.ChildColumns) {
                        if (QueryCreator.IsPrimaryKey(SourceTable, C.ColumnName)) continue;
                        R[C.ColumnName] = DBNull.Value;
                    }
                }
            }
            if (Rfound == null) {
                ErrorLogger.Logger.MarkEvent($"Can't unlink. DataTable {SourceTable.TableName} is not child of {primaryTable.TableName}.");
                return null;
            }
            if (R.IsFalseUpdate()) {  //toglie la riga se inutile
                R.Delete();
                R.AcceptChanges();
            }
            return R;
        }


        
        /// <summary>
        /// Set the table as NotEntitychild. So the table isn't cleared during freshform and refills
        /// </summary>
        /// <param name="T"></param>
        /// <param name="parentRelName"></param>
        public void AddNotEntityChild(DataTable T, string parentRelName) {
            T.SetDenyClear();
            addNotEntityChildFilter(T, parentRelName);
        }


        /// <summary>
        /// Set the table as NotEntitychild. So the table isn't cleared during freshform and refills
        /// </summary>
        /// <param name="T"></param>
        /// <param name="child"></param>
        public void AddNotEntityChild(DataTable T, DataTable child) {
            child.SetDenyClear();
            AddNotEntityChildFilter(T, child);
        }

        /// <summary>
        /// Sets all "NotSubEntityChild" tables as "CanClear". Called when form is cleared or data
        ///  is posted
        /// </summary>
        public void AllowAllClear(DataSet ds) {
            foreach (DataTable T in ds.Tables) {
                if (IsNotEntityChild(T)) {
                    T.setAllowClear();
                    clearNotEntityChild(T); ;
                }
            }
        }

        /// <summary>
        /// Establish that a table has to be considered as a child even though it is not a pure subentity
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="child"></param>
        /// <param name="relName"></param>
        public void MarkTableAsNotEntityChild(DataTable primaryTable, DataTable child, string relName) {
            //Bisogna fare denyclear altrimenti la tabella non è preservata
            child.SetDenyClear();
            if (relName == null) {
                AddNotEntityChild(primaryTable, child);
            }
            else {
                AddNotEntityChild(child, relName);
            }
        }

        /// <summary>
        /// Establish that a table has to be considered as a child even though it is not a pure subentity.
        /// It is necessary that a relation exists that links at least one child key column to a parent table column
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="child"></param>
        public void MarkTableAsNotEntityChild(DataTable primaryTable, DataTable child) {
            //Bisogna fare denyclear altrimenti la tabella non è preservata
            child.SetDenyClear();
            AddNotEntityChild(primaryTable, child);
        }

        /// <summary>
        /// Remove a table from being a  NotEntitychild
        /// </summary>
        /// <param name="T"></param>
        public void UnMarkTableAsNotEntityChild(DataTable T) {
            T.setAllowClear();
            clearNotEntityChild(T);
        }



        /// <summary>
        /// removes the filter for a NotEntityChild filter
        /// </summary>
        /// <param name="childTable"></param>
        void clearNotEntityChild(DataTable childTable) {
            SetNotEntityChildFilter(childTable, (string) null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="childTable"></param>
        /// <returns></returns>
        public bool IsNotEntityChild(DataTable childTable) {
            return GetNotEntityChildFilter(childTable) != null;
        }

        /// <summary>
        /// Get the filter for a NotEntityChild filter
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public string GetNotEntityChildFilter(DataTable t) {
            return t.ExtendedProperties["NotEntityChild"] as string;
        }

        /// <summary>
        /// Set the filter for a NotEntityChild filter
        /// </summary>
        /// <param name="t"></param>
        /// <param name="filter"></param>
        public void SetNotEntityChildFilter(DataTable t, string filter) {
            t.ExtendedProperties["NotEntityChild"] =filter;
        }

        /// <summary>
        /// Adds a filter that discriminates rows of child table. Filter finds all rows having
        ///  null values in key fields related to the parent table. So those rows are not child
        ///  following the given relation
        /// </summary>
        /// <param name="child"></param>
        /// <param name="relName">Name of the relation that links parent and child table</param>
        public virtual void addNotEntityChildFilter(DataTable child, string relName) {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (IsNotEntityChild(child)) return; //don't set if it has already been set

            if (relName == null) throw new ArgumentNullException(nameof(relName));

            if (!child.DataSet.Relations.Contains(relName)) return;
            var rel = child.DataSet.Relations[relName];
            string filter = null;
            foreach (var c in rel.ChildColumns) {
                if (QueryCreator.IsPrimaryKey(child, c.ColumnName)) continue;
                filter = q.AppAnd(filter, q.IsNull(c.ColumnName));
            }
            SetNotEntityChildFilter(child,filter);
            
        }

        /// <summary>
        /// Adds a filter that discriminates rows of child table. Filter finds all rows having
        ///  null values in key fields related to the parent table. This function takes the first
        ///  relation parent-child it finds. Rows identified by the filter relation are NOT child
        ///  of primarytable. 
        /// </summary>
        /// <param name="primaryTable"></param>
        /// <param name="child"></param>
        public virtual void AddNotEntityChildFilter(DataTable primaryTable, DataTable child) {
            if (IsNotEntityChild(child)) return; //don't set if it has already been set
            var r = QueryCreator.GetParentChildRel(primaryTable, child);
            if (r == null) return;
            string filter = null;
            foreach (DataColumn c in r.ChildColumns) {
                if (QueryCreator.IsPrimaryKey(child, c.ColumnName)) continue;
                filter = q.AppAnd(filter, q.IsNull(c.ColumnName));
            }
            SetNotEntityChildFilter(child, filter);
        }

       
      

		


		
		
		/// <summary>
		/// If a table is cached, is marked to be read again in next
		///  ReadCached. If the table is not cached, has no effect
		/// </summary>
		/// <param name="T">Table to cache again</param>
		public void ReCache(DataTable T){
			if (!IsCached(T)) return;
			CacheTable(T);
		}


		/// <summary>
		/// Set Table T to be read once for all when ReadCached will be called next time
		/// </summary>
		/// <param name="T"></param>
		/// <param name="filter"></param>
		/// <param name="sort"></param>
		/// <param name="addBlankRow">when true, a blank row is added as first row of T</param>
		public void CacheTable(DataTable T,object filter, string sort, bool addBlankRow){
			T.ExtendedProperties["cached"]="0";
			if (addBlankRow) MarkToAddBlankRow(T);
			if (sort!=null) T.ExtendedProperties["sort_by"] = sort;
			if (filter!=null) SetStaticFilter(T,filter);
		}

        
		#region Calculated Fields Management
        
		

		/// <summary>
		/// Tells MetaData Engine to call CalculateFields(R,ListingType) whenever:
		///  - a row is loaded from DataBase
		///  - a row is changed in a sub-entity form and modification accepted with mainsave
		/// </summary>
		/// <param name="T">DataTable to be custom calculated</param>
		/// <param name="ListingType">Listing type to use for delegate calling</param>
		/// <param name="Calc">Delegate function to call</param>
		public void ComputeRowsAs(DataTable T, string ListingType, CalcFieldsDelegate Calc ){
			T.ExtendedProperties["CalculatedListing"] = ListingType;
			T.ExtendedProperties["CalculatedFunction"] = Calc;
		}


		/// <summary>
		/// Evaluates custom fields for every row of a Table. Calls the delegate linked to the table,
		///  corresponding to the MetaData.CalculateFields() virtual method (if it has been defined).
		/// </summary>
		/// <param name="T"></param>
		/// <remarks>No action is taken on deleted rows. Unchanged rows remain unchanged anyway</remarks>
		public void CalculateTable(DataTable T){            
			if (T==null) return;
			//MarkEvent("Calculate Start on "+T.TableName);
			if (T.ExtendedProperties["CalculatedListing"]==null) return;
			string ListType = T.ExtendedProperties["CalculatedListing"].ToString();
			if (T.ExtendedProperties["CalculatedFunction"]==null) return;
			int handle = StartTimer("CalculateTable * " + T.TableName);
			CalcFieldsDelegate Calc = (CalcFieldsDelegate) T.ExtendedProperties["CalculatedFunction"];
			InvokeActions(T, TableAction.beginLoad);
			if (T.HasChanges()) {
				foreach (DataRow R in T.Rows) {
					if (R.RowState == DataRowState.Deleted) continue;
					bool toMark = (R.RowState == DataRowState.Unchanged);
					Calc(R, ListType);
					if (toMark) R.AcceptChanges();
				}
			}
			else {
				foreach (DataRow R in T.Rows) {
					if (R.RowState == DataRowState.Deleted) continue;
					Calc(R, ListType);
				}
				T.AcceptChanges();

			}
			
			InvokeActions(T, TableAction.endLoad);
			StopTimer(handle);
			//MarkEvent("Calculate Stop");
		}

		/// <summary>
		/// Evaluates custom fields for a single row. Calls the delegate linked to the table,
		///  corresponding to the MetaData.CalculateFields() virtual method (if it has been defined).
		/// </summary>
		/// <param name="R"></param>
		/// <remarks>No action is taken on deleted rows. Unchanged rows remain unchanged anyway</remarks>
		public  void CalculateRow(DataRow R){            
			if (R==null) return;
			if (R.RowState== DataRowState.Deleted) return;
			DataTable T=R.Table;
			if (T.ExtendedProperties["CalculatedListing"]==null) return;
			string ListType = T.ExtendedProperties["CalculatedListing"].ToString();
			CalcFieldsDelegate Calc = (CalcFieldsDelegate) T.ExtendedProperties["CalculatedFunction"];
			if (Calc==null)return;
			bool toMark= (R.RowState == DataRowState.Unchanged);
			Calc(R, ListType);
			if (toMark) R.AcceptChanges();
		}
		#endregion

		
        
		/// <summary>
		/// Gets evaluated fields, taking the ExtendedProperties QueryCreator.IsTempColumn
		///   and considering it in a "childtable.childfield" format
		/// Also calls CalcFieldsDelegate of the table for every rows (when needed)
		/// </summary>
		/// <param name="T"></param>
		public void GetTemporaryValues(DataTable T, ISecurity env) {
		    //if (destroyed) return;
		    DataSet DS = T.DataSet;
		    bool lateAcceptChanges = !T.HasChanges();
		    int handle = StartTimer("GetTemporaryValues * " + T.TableName);
		    var iManager = T.DataSet?.getIndexManager();
		    InvokeActions(T,TableAction.beginLoad);
			foreach (DataColumn c in T.Columns){                
                if (c.IsReal())continue;
                if (c.ExtendedProperties["mdl_foundInGetViewChildTable"] != null) {
	                c.ExtendedProperties["mdl_foundInGetViewChildTable"] = null;
	                continue;
                };
				object tagObj = c.GetExpression();                    
				if (tagObj==null) {				   
				        var fn  = c.GetMetaExpression();
                        if (fn is null)continue;
                        int handle3 = StartTimer("GetTemporaryValues GetMetaExpression * " + T.TableName);
				        foreach (DataRow r in T.Rows) {
				            if (r.RowState == DataRowState.Deleted) continue;
				            var toMark = (r.RowState == DataRowState.Unchanged);
				            r[c] = fn.apply(r, env) ?? DBNull.Value;
				            if (toMark&& ! lateAcceptChanges) r.AcceptChanges();
				        }	
				        StopTimer(handle3);
				    continue;
				}
				var tag = tagObj.ToString().Trim();
                if(!checkColumnProperty(tag, DS, out var table, out var column))
                    continue;
                if (table==""||column=="") continue;
				if (DS.Tables[table] == null) continue;
				DataTable sourceTable = DS.Tables[table];
				if (!sourceTable.Columns.Contains(column)) continue;
				var sourceCol = sourceTable.Columns[column];
				var parentRel = parentRelation(T, table);
				if (parentRel == null) continue;
				var parentHasher = iManager?.getParentHasher(parentRel);
				int handle2 = StartTimer("GetTemporaryValues GetRelatedRow * " + T.TableName);
				if (parentHasher == null || parentHasher.noIndex) {
					foreach(DataRow r in T.Rows){
						if (r.RowState== DataRowState.Deleted) continue;
						var toMark= (r.RowState == DataRowState.Unchanged);
						var Related = r.GetParentRow(parentRel);	//R.iGetParentRows(Rel);
						if (Related == null) continue;
						r[c] = Related[sourceCol] ; //GetRelatedRow(r, table, column,parentRel) ?? DBNull.Value;
						if (toMark&& ! lateAcceptChanges) r.AcceptChanges();
					}
				}
				else {
					Dictionary<string, object> hashed= new Dictionary<string, object>();
					foreach(DataRow r in T.Rows){
						if (r.RowState== DataRowState.Deleted) continue;
						var toMark= (r.RowState == DataRowState.Unchanged);
						string hash = parentHasher.hash.get(r);
						if (!hashed.TryGetValue(hash, out object o)) {
							var Related = parentHasher.getRow(r);	//R.iGetParentRows(Rel);
							if (Related == null) continue;
							o  = Related[sourceCol] ; //GetRelatedRow(r, table, column,parentRel) ?? DBNull.Value;
							hashed[hash] = o;
						}

						r[c] = o;
						if (toMark && ! lateAcceptChanges) r.AcceptChanges();
						
					}
				}
				StopTimer(handle2);
				
			}
			if (lateAcceptChanges)T.AcceptChanges();
			InvokeActions(T,TableAction.endLoad);
			CalculateTable(T);
			StopTimer(handle);
		}

		DataRelation parentRelation(DataTable T, string parentTableName) {
			DataSet DS = T.DataSet;
			DataTable RelatedTable = DS.Tables[parentTableName];
			return (from DataRelation rel in T.ParentRelations where rel.ParentTable.Equals(RelatedTable) select rel).FirstOrDefault();
		}

		/// <summary>
		/// Gets calculated fields from related table (Calculated fields are those 
		///		provided with an expression). 
		/// </summary>
		/// <param name="R"></param>
		public void CalcTemporaryValues(DataRow R, ISecurity conn) {
			DataSet DS = R.Table.DataSet;
			bool toMark= (R.RowState == DataRowState.Unchanged);
			R.BeginEdit();
			foreach (DataColumn C in R.Table.Columns){         
			    if (!C.IsTemporary())continue;
                object TagObj = C.GetExpression(); 
				if (TagObj==null) {
				    var fn  = C.GetMetaExpression();
				    if (fn is null)continue;
        	        R[C] = fn.apply(R,conn) ?? DBNull.Value;
				    continue;
				}
				string Tag = TagObj.ToString().Trim();
                if(!checkColumnProperty(Tag, DS, out var Table, out var Column)) continue;
                if (Column=="") continue;
				R[C] = GetRelatedRow(R, Table, Column);
			}
			R.EndEdit();
			if ((toMark) && (R.RowState != DataRowState.Unchanged))	R.AcceptChanges();

		}

		/// <summary>
		/// Evaluate a field of a row R taking the value from a related row of
		///   a specified Table - Column
		/// </summary>
		/// <param name="R">DataRow to fill</param>
		/// <param name="relatedTableName">Table from which value has to be taken</param>
		/// <param name="relatedColumn">Column from which value has to be taken</param>
		/// <param name="parentRelations"></param>
		/// <returns></returns>
		object getRelatedRow(DataRow R, string relatedTableName, string relatedColumn, DataRelation parentRelation){
			DataTable T = R.Table;
			DataTable RelatedTable = T.DataSet.Tables[relatedTableName];            
			DataRow Related=null;
			var iManager = R.Table.DataSet?.getIndexManager();
			//return iManager?.getParentRows(rChild, rel) ?? rChild.GetParentRows(rel);
			
			if (parentRelation.ParentTable.Equals(RelatedTable)){
				Related = iManager?.getParentRow(R, parentRelation) ?? R.GetParentRow(parentRelation);	//R.iGetParentRows(Rel);
					
			}
			
			return Related?[relatedColumn]??DBNull.Value;
		}

        
		/// <summary>
		/// Checks that a Column Property is in the format parenttable.parentcolumn
		/// </summary>
		/// <param name="Tag">Extended Property of a DataColumn</param>
		/// <param name="table">table part if successfull</param>
		/// <param name="column">column part if successfull</param>
		/// <returns>true if the property is in a correct format</returns>
		bool checkColumnProperty(string Tag, DataSet ds, out string table,  out string column){
			table=null;
			column=null;
			if (Tag==null) return false;
			if (Tag=="")return false;
			Tag = Tag.Trim();
			int pos=Tag.IndexOf('.');
			if (pos==-1)return false;
			table = verifyTableExistence(ds,(Tag.Split(
				new char[] {'.'}, 2)[0]).Trim());
			if (table==null) return false;
			//column = VerifyColumnExistence(Tag.Substring(pos+1), table);
			column=Tag.Substring(pos+1);
			if (column.ToString()=="") column=null;
			if (column==null) return false;
			return true;
		}

        
		/// <summary>
		/// Checks for DataTable existence in a DataSet
		/// </summary>
		/// <param name="TableName"></param>
		/// <returns>tablename if table exists, null otherwise</returns>
		string verifyTableExistence(DataSet ds,string TableName){
			if (ds.Tables[TableName]==null) return null;
			return TableName;
		}
		/// <summary>
		/// Evaluate a field of a row R taking the value from a related row of
		///   a specified Table - Column
		/// </summary>
		/// <param name="R">DataRow to fill</param>
		/// <param name="relatedTableName">Table from which value has to be taken</param>
		/// <param name="relatedColumn">Column from which value has to be taken</param>
		/// <returns></returns>
		object GetRelatedRow(DataRow R, string relatedTableName, string relatedColumn){
			DataTable T = R.Table;
			DataSet DS = T.DataSet;
			DataTable RelatedTable = DS.Tables[relatedTableName];            
			DataRow[] Related=null;
			var iManager = R.Table.DataSet?.getIndexManager();
			//return iManager?.getParentRows(rChild, rel) ?? rChild.GetParentRows(rel);

			foreach (DataRelation Rel in T.ChildRelations){
				if (Rel.ChildTable.Equals(RelatedTable)){
					Related = iManager?.getChildRows(R, Rel) ?? R.GetChildRows(Rel);	// R.iGetChildRows(Rel);
					if (Related.Length==0) continue;
					break;
				}
			}
			if (Related==null){
				foreach (DataRelation Rel in T.ParentRelations){
					if (Rel.ParentTable.Equals(RelatedTable)){
						Related = iManager?.getParentRows(R, Rel) ?? R.GetParentRows(Rel);	//R.iGetParentRows(Rel);
						if (Related.Length==0) continue;
						break;
					}
				}
			}


			if (Related==null) {
				//                MetaFactory.factory.getSingleton<IMessageShower>().Show("The field "+ChildColumn+" of table"+ChildTable +
				//                      " could not be looked-up in table "+ T.TableName);
				return DBNull.Value;
			}
			if (Related.Length==0) return DBNull.Value;
			if (RelatedTable.Columns[relatedColumn]==null) {
				//                MetaFactory.factory.getSingleton<IMessageShower>().Show("The field "+ChildColumn+" was not contained in table "+ChildTable);
				return DBNull.Value;
			}
			return Related[0][relatedColumn];
		}

        /// <summary>
		/// Temporary columns are those starting with ! or those having an expression 
		/// Gets evaluated fields, taking the ExtendedProperties QueryCreator.IsTempColumn
		///   and considering it in a "childtable.childfield" format
		/// Also calls CalcFieldsDelegate of the table for every rows (when needed)
		/// Also invokes CalculateTable(r.Table)
		/// </summary>
		/// <param name="r"></param>
		public void GetTemporaryValues(DataRow r, ISecurity env) {
		    //if (destroyed) return;
		    DataTable T = r.Table;
		    DataSet DS = T.DataSet;
		    if (r.RowState == DataRowState.Deleted) return;
		    bool toMark = r.RowState==DataRowState.Unchanged;
		    int handle = StartTimer("GetTemporaryValues DataRow * " + T.TableName);
		    var iManager = T.DataSet?.getIndexManager();
			foreach (DataColumn c in T.Columns){                
                if (c.IsReal())continue;
                if (c.ExtendedProperties["mdl_foundInGetViewChildTable"] != null) {
	                c.ExtendedProperties["mdl_foundInGetViewChildTable"] = null;
	                continue;
                };
				object tagObj = c.GetExpression();                    
				if (tagObj==null) {				   
				        var fn  = c.GetMetaExpression();
                        if (fn is null)continue;
			            r[c] = fn.apply(r, env) ?? DBNull.Value;
				    continue;
				}
				var tag = tagObj.ToString().Trim();
				//check if expression is a string "parentTable.FieldName"
                if(!checkColumnProperty(tag, DS, out var table, out var column))continue;
                if (table==""||column=="") continue;
				if (DS.Tables[table] == null) continue;
				DataTable sourceTable = DS.Tables[table];
				if (!sourceTable.Columns.Contains(column)) continue;
				var sourceCol = sourceTable.Columns[column];
				var parentRel = parentRelation(T, table);
				if (parentRel == null) continue;
				var parentHasher = iManager?.getParentHasher(parentRel);
				if (parentHasher == null || parentHasher.noIndex) {
						var Related = r.GetParentRow(parentRel);	//R.iGetParentRows(Rel);
						if (Related == null) continue;
						r[c] = Related[sourceCol] ; //GetRelatedRow(r, table, column,parentRel) ?? DBNull.Value;
				}
				else {
					Dictionary<string, object> hashed= new Dictionary<string, object>();
						string hash = parentHasher.hash.get(r);
						if (!hashed.TryGetValue(hash, out object o)) {
							var Related = parentHasher.getRow(r);	//R.iGetParentRows(Rel);
							if (Related == null) continue;
							o  = Related[sourceCol] ; //GetRelatedRow(r, table, column,parentRel) ?? DBNull.Value;
							hashed[hash] = o;
						}
						r[c] = o;
				}
				
			}
			if (toMark)T.AcceptChanges();
			CalculateTable(T);
			StopTimer(handle);
		}

        /// <summary>
        /// Adds an empty (all fields blank) row to a table if the table has been marked
        ///  with MarkToAddBlankRow and it is empty
        /// </summary>
        /// <param name="T"></param>
        public virtual void CheckBlankRow(DataTable T) {
	        if (T.Rows.Count > 0) return;
	        if (T.ExtendedProperties["AddBlankRow"]==null) return;
	        int handle = StartTimer("Add_Blank_Row * " + T.TableName);
	        DataRow BlankRow = T.NewRow();
	        QueryCreator.ClearRow(BlankRow);
	        InvokeActions(T, TableAction.beginLoad);
	        T.Rows.Add(BlankRow);
	        InvokeActions(T, TableAction.endLoad);
	        BlankRow.AcceptChanges();
	        StopTimer(handle);
        }

        /// <summary>
        /// Tells MDE that a table is temporary and should 
        ///  not be used for calling stored procedure, messages, logs, or updates.
        /// Temporary tables are never read or written to db by the library
        /// </summary>
        /// <param name="T">Table to mark</param>
        /// <param name="createblankrow">true if a row has to be added to table</param>
        public static void MarkAsTemporaryTable(DataTable T){
	        T.ExtendedProperties[IsTempTable]="y";
        }

        /// <summary>
        /// Returns true if a DataTable has been marked as Temporary
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static bool IsTemporaryTable(DataTable T){
	        if (T.ExtendedProperties[IsTempTable]==null) return false;
	        return true;
        }

        /// <summary>
        /// Undo "MarkAsTemporaryTable"
        /// </summary>
        /// <param name="T"></param>
        public static void MarkAsRealTable(DataTable T){
	        T.ExtendedProperties[IsTempTable]=null;
        }

        /// <summary>
        /// Takes values for the Source Row from linked Form Data. The goal is to propagate to
        ///  the parent form the changes made (in LinkedForm) in this form
        /// </summary>
        /// <remarks>
        ///  Necessary condition is that FormDataSet does contain only one row of the same
        ///  table as SourceRow. This function can be redefined to implement additional operations
        ///  to do in SourceRow.Table when changes to SourceRow are accepted. (ex GetSourceChanges)
        ///  </remarks>
        ///  <returns>true when operation successfull</returns>
        public bool SaveChanges( DataRow sourceRow, DataRow destRow) {
            var changes = xVerifyChangeChilds(destRow.Table, sourceRow);

            if (!changes)
                changes = xVerifyChangeChilds(sourceRow.Table, destRow);

            if (!changes) {
                CalculateRow(destRow);
                if (destRow.IsFalseUpdate()) destRow.AcceptChanges();
                return true;
            }

            //Here should be done a backup of SourceRow before changing it, in order to
            // undo modification when needed.
            try {
                if (destRow.RowState == DataRowState.Added) {
                    var oldselector = RowChange.GetHashSelectors(sourceRow.Table, destRow);
                    var newselector = RowChange.GetHashSelectors(sourceRow.Table, sourceRow);
                    if (oldselector != newselector) {
                        RowChange.CalcTemporaryID(sourceRow, destRow.Table);
                    }

                    var existentFound = destRow.Table.Select(q.CmpKey(sourceRow));
                    if (existentFound.Length > 0) {
                        if (existentFound[0] != destRow) {
                            ErrorLogger.Logger.MarkEvent(sourceRow.Table.TableName + "SaveChanges() key conflict");
                            return false;
                        }
                    }
                }
                xCopy(sourceRow.Table.DataSet, destRow.Table.DataSet, sourceRow, destRow);
            }
            catch (Exception e) {
                ErrorLogger.Logger.markException(e, sourceRow.Table.TableName + ".SaveChanges()");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copia un DataRow da un DS ad un altro.
        /// Ipotesi abbastanza fondamentale è che RSource e RDest abbiano la stessa chiave, o perlomeno
        ///  che RSource non generi conflitti in Dest
        /// </summary>
        /// <param name="source">DataSet origine della copia</param>
        /// <param name="dest">Dataset di destinazione</param>
        /// <param name="rSource">Riga da copiare</param>                                                                         
        /// <param name="rDest">Riga di destinazione</param>
        private DataRow xCopy(DataSet source, DataSet dest, DataRow rSource, DataRow rDest) {
            var destIsInsert = (rDest.RowState == DataRowState.Added);
            xRemoveChilds(source, rDest);
            return xCopyChilds(dest, rDest.Table, source, rSource, destIsInsert);  //era xMoveChilds ma non si vede il motivo di rimuovere le righe dall'origine
        }

        /// <summary>
        /// Removes a Row with all his subentity childs. 
        /// Only considers tables of D inters. Rif
        /// </summary>
        /// <param name="rif">Referring DataSet. Tables not existing in this DataSet are not recursively scanned</param>
        /// <param name="rDest">DataRow to be removed with all subentities</param>
        /// <returns></returns>
        void xRemoveChilds(DataSet rif, DataRow rDest) {
            DataTable T = rDest.Table;
            foreach (DataRelation rel in T.ChildRelations) {
                if (!rif.Tables.Contains(rel.ChildTable.TableName))
                    continue;
                if (!GetData.CheckChildRel(rel))
                    continue; //not a subentityrel
                var childs = rDest.GetChildRows(rel);
                foreach (var child in childs) {
                    xRemoveChilds(rif, child);
                }
            }

            rDest.Delete();
            if (rDest.RowState != DataRowState.Detached)
                rDest.AcceptChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="TDest"></param>
        /// <param name="rif"></param>
        /// <param name="rSource">Belongs to Rif</param>
        /// <param name="forceAddState"></param>
        DataRow xCopyChilds(DataSet dest, DataTable TDest, DataSet rif, DataRow rSource, bool forceAddState) {
            var T = rSource.Table;
            var resultRow = copyDataRow(TDest, rSource, forceAddState);

            foreach (DataRelation rel in T.ChildRelations) {
                if (!dest.Tables.Contains(rel.ChildTable.TableName))
                    continue;
                if (!GetData.CheckChildRel(rel))
                    continue; //not a subentityrel
                var childTable = rif.Tables[rel.ChildTable.TableName];
                dest.Tables[childTable.TableName].copyAutoIncrementPropertiesFrom(childTable);                

                for (int i = 0; i < childTable.Rows.Count; i++) {
                    var child = childTable.Rows[i];
                    xCopyChilds(dest, dest.Tables[childTable.TableName], rif, child, false);
                }
            }

            return resultRow;
        }

        public void CopyDataRow(DataTable DestTable, System.Data.DataRow ToCopy) {
            System.Data.DataRow Dest = DestTable.NewRow();
            DataRowVersion ToConsider = DataRowVersion.Current;
            if (ToCopy.RowState == DataRowState.Deleted)
                ToConsider = DataRowVersion.Original;
            if (ToCopy.RowState == DataRowState.Modified)
                ToConsider = DataRowVersion.Original;
            if (ToCopy.RowState != DataRowState.Added) {
                foreach (DataColumn C in DestTable.Columns) {
                    if (ToCopy.Table.Columns.Contains(C.ColumnName)) {
                        Dest[C.ColumnName] = ToCopy[C.ColumnName, ToConsider];
                    }
                }
                DestTable.Rows.Add(Dest);
                Dest.AcceptChanges();
            }
            if (ToCopy.RowState == DataRowState.Deleted) {
                Dest.Delete();
                return;
            }
            foreach (DataColumn C in DestTable.Columns) {
                if (ToCopy.Table.Columns.Contains(C.ColumnName)) {
                    if (C.ReadOnly)
                        continue;
                    Dest[C.ColumnName] = ToCopy[C.ColumnName];
                }
            }
            if ((ToCopy.RowState == DataRowState.Modified || ToCopy.RowState == DataRowState.Unchanged)) {
                CalculateRow(Dest);
                Dest.RemoveFalseUpdates();
                return;
            }


            DestTable.Rows.Add(Dest);
            CalculateRow(Dest);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destTable"></param>
        /// <param name="toCopy"></param>
        /// <param name="forceAddState"></param>
        /// <returns></returns>
        DataRow copyDataRow(DataTable destTable, DataRow toCopy, bool forceAddState) {
            var dest = destTable.NewRow();
            DataRowVersion toConsider = DataRowVersion.Current;
            if (toCopy.RowState == DataRowState.Deleted)
                toConsider = DataRowVersion.Original;
            if (toCopy.RowState == DataRowState.Modified)
                toConsider = DataRowVersion.Original;
            if (toCopy.RowState != DataRowState.Added && !forceAddState) {
                foreach (DataColumn c in destTable.Columns) {
                    if (destTable.Columns[c.ColumnName].ReadOnly)
                        continue;
                    if (toCopy.Table.Columns.Contains(c.ColumnName)) {
                        dest[c.ColumnName] = toCopy[c.ColumnName, toConsider];
                    }
                }

                destTable.Rows.Add(dest);
                dest.AcceptChanges();
            }

            if (toCopy.RowState == DataRowState.Deleted) {
                dest.Delete();
                return dest;
            }

            foreach (DataColumn c in destTable.Columns) {
                if (destTable.Columns[c.ColumnName].ReadOnly)
                    continue;
                if (toCopy.Table.Columns.Contains(c.ColumnName)) {
                    dest[c.ColumnName] = toCopy[c.ColumnName];
                }
            }

            if ((toCopy.RowState == DataRowState.Modified || toCopy.RowState == DataRowState.Unchanged)
                && !forceAddState) {
                CalculateRow(dest);
                if (dest.IsFalseUpdate())
                    dest.AcceptChanges();
                return dest;
            }

            //Vede se nella tab. di dest. c'è una riga cancellata che matcha
            var filter = q.CmpKey(toCopy);
            var deletedFound = destTable.Select(filter, null, DataViewRowState.Deleted);
            if (deletedFound.Length == 1) {
                dest.BeginEdit();
                destTable.Columns._forEach(c => {
                    if (c.ReadOnly)
                        return;
                    dest[c.ColumnName] = deletedFound[0][c.ColumnName, DataRowVersion.Original];
                });


                //RowChange.CalcTemporaryID(SourceRow);
                dest.EndEdit();

                //Elimina la riga cancellata dal DataSet
                deletedFound[0].AcceptChanges();

                //Considera la riga sorgente non più cancellata	ma invariata
                destTable.Rows.Add(dest);
                dest.AcceptChanges();

                foreach (DataColumn CC in destTable.Columns) {
                    if (destTable.Columns[CC.ColumnName].ReadOnly)
                        continue;
                    if (toCopy.Table.Columns.Contains(CC.ColumnName)) {
                        dest[CC.ColumnName] = toCopy[CC.ColumnName, DataRowVersion.Current];
                    }
                }

                CalculateRow(dest);
                if (dest.IsFalseUpdate())
                    dest.AcceptChanges();
                return dest;
            }

            destTable.Rows.Add(dest);
            CalculateRow(dest);
            return dest;

        }



    }
}
