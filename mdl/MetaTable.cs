using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Dynamic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using q = mdl.MetaExpression;
using static mdl.IndexManager;
using static mdl.Metaprofiler;

using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
//using Roslyn.Utilities;

#pragma warning disable IDE1006 // Type or member is obsolete

//[assembly: ComVisible(false)]
namespace mdl {

    /// <summary>
    /// Dynamic class available for field addition
    /// </summary>
    public class DynamicEntity : DynamicObject, IComparable{
        private IDictionary<string, object> _values;

        /// <summary>
        /// Creates a dynamic entity prefilled with a set of values
        /// </summary>
        /// <param name="values"></param>
        public DynamicEntity(IDictionary<string, object> values) {
            _values = values;
        }


        /// <summary>
        /// Creates a dynamic entity prefilled with a set of values from a sample
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="fields"></param>
        public DynamicEntity(object sample, params string []fields) {
            _values = new Dictionary<string, object>();
            fields._forEach(f => _values[f] = MetaExpression.getField(f, sample));
        }

        /// <summary>
        /// Necessary to implement  IComparable interface
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj) {
            if (obj as DynamicEntity == null) return 1;
            var d = obj as DynamicEntity;
            if (this._values.Count != d._values.Count) return (this._values.Count - d._values.Count);
            foreach(string k in _values.Keys) {
                if (!d._values.ContainsKey(k)) return 1;
                int cmp = _values[k].ToString().CompareTo(d._values[k]);
                if (cmp != 0) return cmp;
            }
            return 0;
        }

        /// <summary>
        /// Necessary to implement DynamicObject interface
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (_values.ContainsKey(binder.Name)) {
                result = _values[binder.Name];
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Necessary to implement DynamicObject interface
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(
            SetMemberBinder binder, object value) {
            _values[binder.Name] = value;

            // You can always add a value to a dictionary,
            // so this method always returns true.
            return true;
        }
    }

    /// <summary>
    /// Dynamic class available for field addition
    /// </summary>
    public class RowObject : DynamicObject, IComparable {
        /// <summary>
        /// Lookup from field name to field position in the values array
        /// </summary>
        public readonly IDictionary<string, int> ordinal;

        /// <summary>
        /// Field values
        /// </summary>
        public object[] values;

        /// <summary>
        /// Get/Set field with a specified name
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object this[string fieldName]{
            get { return values[ordinal[fieldName]]; }
            set { values[ordinal[fieldName]] = value; }
        }
        /// <summary>
        /// Creates a dynamic entity prefilled with a set of values
        /// </summary>
        /// <param name="lookup">lookup between field names and array position</param>
        /// <param name="values"></param>
        public RowObject(IDictionary<string,int>lookup, object []values ) {
            this.values = values;
            ordinal = lookup;
        }

        /// <summary>
        /// Creates a dynamic entity prefilled with a set of values from a sample
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="fields"></param>
        public RowObject(object sample, params string[] fields) {
            ordinal = new Dictionary<string, int>();
            object[] values = new object[fields.Length];
            fields._forEach((f, i) => {
                ordinal[fields[i]] = i;
                values[i] = MetaExpression.getField(f, sample);
            });
        }

        /// <summary>
        /// Necessary to implement  IComparable interface
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj) {
            if (obj as RowObject == null) return 1;
            var d = obj as RowObject;
            int diff = values.Length - d.values.Length;
            if (diff != 0) return diff;
            foreach (string k in ordinal.Keys) {
                if(!d.ordinal.TryGetValue(k, out var index)) return 1;
                int cmp = values[index].ToString().CompareTo(d.values[index]);
                if (cmp != 0) return cmp;
            }
            return 0;
        }

        /// <summary>
        /// Necessary to implement DynamicObject interface
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if(!ordinal.TryGetValue(binder.Name, out var index)) {
                result = null;
                return false;
            }
            result = values[index];
            return true;
        }

        /// <summary>
        /// Necessary to implement DynamicObject interface
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if(!ordinal.TryGetValue(binder.Name, out var index)) {
                return false;
            }
            values[index] = value;
            return true;
        }
    }

    /// <summary>
    /// Helper function to compare nullable Icomparable objects
    /// </summary>
    public static class CompareHelper {
        /// <summary>
        /// Compare for equality
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjEq<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value) return (b==DBNull.Value);
            if (b== DBNull.Value) return false;
            return ((t)a).CompareTo((t)b) == 0;
        }

        /// <summary>
        /// Compare for not equality
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjNe<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value) return (b != DBNull.Value);
            if (b == DBNull.Value) return true;
            return ((t)a).CompareTo((t)b) != 0;
        }

        /// <summary>
        /// Compare for greater than
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjGt<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value || b == DBNull.Value) return DBNull.Value;
            return ((t)a).CompareTo((t)b) > 0;
        }

        /// <summary>
        /// Compare for less than
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjLt<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value || b == DBNull.Value) return DBNull.Value;
            return ((t)a).CompareTo((t)b) < 0;
        }

        /// <summary>
        /// Compare for great o  equal
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjGe<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value || b == DBNull.Value) return DBNull.Value;
            return ((t)a).CompareTo((t)b) >= 0;
        }

        /// <summary>
        /// Compare for less or equal
        /// </summary>
        /// <typeparam name="t"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object cmpObjLe<t>(object a, object b) where t : IComparable {
            if (a == null || b == null) return null;
            if (a == DBNull.Value || b == DBNull.Value) return DBNull.Value;
            return ((t)a).CompareTo((t)b) <= 0;
        }

    }

    /// <summary>
    /// Helper class to add extensions to dataset
    /// </summary>
    public static class DataSetHelper {


        /// <summary>
        /// delegate to create constructors invokations dynamically
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public delegate object ConstructorDelegate(params object[] args);

        /// <summary>
        /// Creates a dynamic entity from a dictionary
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static DynamicEntity getObj(this Dictionary<string, object> o) {
            return new DynamicEntity(o);
        }

        /// <summary>
        /// Creates an array of dynamic entities from an array of dictionaries
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static DynamicEntity[] getObj(this Dictionary<string, object>[] o) {
            return (from d in o select d.getObj()).ToArray();
        }

        /// <summary>
        /// Change the Child Row fields in order to make it child of Parent. All parent-child relations between the two tables are taken into account.
        /// </summary>
        /// <param name="parent">Parent row</param>
        /// <param name="parentTable">can be null if parentRow is not null, so it is assumed parent.Table</param>
        /// <param name="child"></param>
        /// <returns></returns>
        public static bool MakeChildOf(this DataRow child, DataRow parent,
	        DataTable parentTable=null
        ) {
	        if (parentTable == null) {
		        parentTable = parent.Table;
	        }
	        foreach(DataRelation rel in child.Table.ParentRelations) {
		        if (child.MakeChildByRelation(parent, parentTable, rel.RelationName)) {
			        return true;
		        }
	        }
	        return false;
        }

         /// <summary>
        /// Makes a "Child" DataRow related as child with a Parent Row. 
        ///     This function should be called after calling DataTable.NewRow and
        /// before calling CalcTemporaryID and DataTable.Add()
        /// </summary>
        /// <param name="parent">Parent Row (Can be null)</param>
        /// <param name="parentTable">Parent Table (to which Parent Row belongs)</param>
        /// <param name="child">Row that must become child of Parent (can't be null)</param>
        /// <param name="relname">eventually name of relation to use</param>
        /// <remarks>This function should be called after calling DataTable.NewRow and
        ///         BEFORE calling CalcTemporaryID and DataTable.Add()
        /// </remarks>
        public static bool MakeChildByRelation(this DataRow child, DataRow parent, DataTable parentTable=null,  
				string relationName=null){
            if(relationName == null) return child.MakeChildOf(parent, parentTable);
            if (parentTable == null) {
	            parentTable = parent.Table;
            }
			var rel = getChildRelation(parentTable, child.Table, relationName);
			if (rel==null) return false;
			for (var i=0; i< rel.ParentColumns.Length; i++){
				var childCol = rel.ChildColumns[i];
				if (parent!=null){
					child[childCol.ColumnName]= parent[rel.ParentColumns[i].ColumnName];					
				}
				else {
                    child[childCol] = QueryCreator.clearValue(childCol);
				}
			}
			return true;
		}

		/// <summary>
		/// Search a Relation in Child's Parent Relations that connect Child to Parent, 
		///		named relname. If it is not found, it is also searched in Parent's
		///		child relations.
		/// </summary>
		/// <param name="parent">Parent table</param>
		/// <param name="child">Child table</param>
		/// <param name="relname">Relation Name, null if it does not matter</param>
		/// <returns>a Relation from Child Parent Relations, or null if not found</returns>
		static DataRelation getChildRelation(DataTable parent, DataTable child, string relname){
			foreach (DataRelation rel in child.ParentRelations){
				if ((relname!=null)&&(rel.RelationName!=relname))continue;
				if (rel.ParentTable.TableName== parent.TableName){
					return rel;
				}
			}
			foreach (DataRelation rel2 in parent.ChildRelations){
				if ((relname!=null)&&(rel2.RelationName!=relname))continue;
				if (rel2.ChildTable.TableName== child.TableName){
					return rel2;
				}
			}

			return null;
		}

       
        /// <summary>
        /// get Child Rows of a parent row, using indexes whenever possible
        /// </summary>
        /// <param name="rParent"></param>
        /// <param name="rel"></param>
        /// <returns></returns>
        public static DataRow[] getChildRows(this DataRow rParent, DataRelation rel) {
	        var iManager = rel?.ChildTable?.DataSet?.getIndexManager();
	        return iManager?.getChildRows(rParent, rel) ?? rParent.GetChildRows(rel);
        }

        /// <summary>
        /// get Child Rows of a parent row, using indexes whenever possible
        /// </summary>
        /// <param name="rParent"></param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        public static DataRow[] getChildRows(this DataRow rParent, string relationName) {
	        var rel = rParent?.Table?.DataSet?.Relations[relationName];
	        var iManager = rel?.ChildTable?.DataSet?.getIndexManager();
	        return iManager?.getChildRows(rParent, rel) ?? rParent.GetChildRows(rel);
        }

        /// <summary>
        /// get parent Rows of a row, using indexes whenever possible
        /// </summary>
        /// <param name="rChild"></param>
        /// <param name="rel"></param>
        /// <returns></returns>
        public static DataRow[] getParentRows(this DataRow rChild, DataRelation rel) {
	        var iManager = rel?.ParentTable?.DataSet?.getIndexManager();
	        return iManager?.getParentRows(rChild, rel) ?? rChild.GetParentRows(rel);
        }

        /// <summary>
        /// get parent Rows of a row, using indexes whenever possible
        /// </summary>
        /// <param name="rChild"></param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        public static DataRow[] getParentRows(this DataRow rChild, string relationName) {
	        var rel = rChild?.Table?.DataSet?.Relations[relationName];
	        var iManager = rel?.ParentTable?.DataSet?.getIndexManager();
	        return iManager?.getParentRows(rChild, rel) ?? rChild.GetParentRows(rel);
        }

      
        public static void copyAutoIncrementPropertiesFrom(this DataTable dest, DataTable source) {
            RowChange.copyAutoIncrementProperties(dest, source);
        }

        /// <summary>
        /// Get rows from DB without affecting the table. Rows are not merged to the table.
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="conn"></param>
        /// <param name="filter">criteria to be met</param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> getDetachedRowsFromDb(this DataTable T, IDataAccess conn, object filter, int timeout = -1) {

            string sql = conn.GetSelectCommand(
                table:T.tableForReading(),
                columns:QueryCreator.RealColumnNameList(T),
                filter:filter
                );             
            return await sqlDetachedGetFromDb(T, conn, sql, timeout);
        }

        /// <summary>
        /// Get rows from DB without affecting the table. Rows are not merged to the table.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> sqlDetachedGetFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1) {
            var t = T.Clone();
            try {
                await conn.ExecuteQueryIntoTable(t, sql, timeout);
            }
			catch {
                return null;
			}            
            if (T.Rows.Count==0) return null;
            return T.Select();
        }

        /// <summary>
        ///  Get rows from DB without adding them to the table
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _getDetachedRowsFromDb(this DataTable T, IDataAccess conn, q filter, int timeout = -1) {
            if ((!(filter is null)) && filter.isFalse())return new DataRow[0];
            return await T._getDetachedRowsFromDb(conn, filter, timeout);
        }

        /// <summary>
        /// Get rows from DB and add them to the table
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _getFromDb(this DataTable T, IDataAccess conn, q filter, int timeout = -1) {
            if (!(filter is null) && filter.isFalse())return new DataRow[0];
            string sFilter = conn.Compile(filter);
            return await T._getFromDb(conn, sFilter, timeout);
        }

        /// <summary>
        ///  Get rows from DB and add them to the table
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _getFromDb(this DataTable T, IDataAccess conn, string filter, int timeout = -1) {
            var res = await T.getDetachedRowsFromDb(conn, filter:filter, timeout);
            if (res == null) return null;
            foreach (var r in res) {
                T.Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        /// <summary>
        ///  Get rows from DB and add them to the table running a sql command
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _sqlGetFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1) {
            var res = await T.sqlDetachedGetFromDb(conn, sql, timeout);
            if (res == null) return null;
            foreach (var r in res) {
                T.Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        /// <summary>
        /// Get existing rows or read from DB when no rows is found
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _get(this DataTable T, IDataAccess conn, q filter, int timeout = -1) {
            var found = T.filter(filter);
            if (found!=null && found.Length > 0) return found;
            var sFilter = filter?.toSql(conn.GetQueryHelper(), conn.Security);
            return await T._getFromDb(conn, sFilter, timeout);
        }



        /// <summary>
        /// Reads rows from db and merges them into a table. Returns all read rows 
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns>Read rows</returns>
        public static async Task<DataRow[]> _mergeFromDb(this DataTable T, IDataAccess conn, MetaExpression filter, int timeout = -1) {
            var res = await T._getDetachedRowsFromDb(conn, filter, timeout);
            if (res == null) return new DataRow[] {};
            foreach (var r in res) {
                var found = T.filter( MetaExpression.mCmp(r, T.PrimaryKey)).FirstOrDefault();
                if (found!=null)   T.Rows.Remove(found);                
                T.Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        /// <summary>
        /// Reads rows from db executing a sql command and merges them into a table. Returns all read rows 
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds, 0 means no timeout, -1 means default timeout</param>
        /// <returns>Read rows</returns>
        public static async Task<DataRow[]> _sqlMergeFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1) {
            var res = await T.sqlDetachedGetFromDb(conn, sql, timeout);
            if (res == null) return new DataRow[] {};
            foreach (var r in res) {
                DataRow found = T.filter(MetaExpression.mCmp(r, T.PrimaryKey)).FirstOrDefault();
                if (found!=null)   T.Rows.Remove(found);                
                T.Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        
        private static string hashColumns(DataRow r,string []columns) {
            var keys = (from string field in columns.ToList() select r[field].ToString());
            return string.Join("§",keys);
        }

        /// <summary>
        /// Merge a set of rows to the stable, skipping existent or overwriting them
        /// </summary>
        /// <param name="T"></param>
        /// <param name="detachedRows">rows to merge</param>
        /// <param name="overwrite">true to overwrite, false to skip existent rows</param>
        /// <returns></returns>
        public static DataRow  []_mergeRows(this DataTable T, DataRow []detachedRows,bool overwrite=false) {
            var handle2 = StartTimer("MergeIntoDataTableWithDictionary * " + T.TableName);
            var destRows = new Dictionary<string, DataRow>();
            if (detachedRows.Length == 0) return new DataRow[] {};
            var resList = new List<DataRow>();

            if (T.Rows.Count <= 100 && detachedRows.Length <= 100) {
                //merge standard
                foreach (var r in detachedRows) {
                    var found = T.filter(MetaExpression.mCmp(r, T.PrimaryKey)).FirstOrDefault();
                    if (found != null) { // Row already exists
                        if (!overwrite) continue;
                        T.Rows.Remove(found);//overwrite that one
                    }
                    T.Rows.Add(r);
                    r.AcceptChanges();
                    resList.Add(r);
                }
            }
            else {
                //merge con dictionary
                string[] keys = (from DataColumn c in T.PrimaryKey select c.ColumnName).ToArray();
                foreach (DataRow r in T.Rows) {
                    destRows[hashColumns(r, keys)] = r;
                }

                foreach (var r in detachedRows) {
                    string hashSource = hashColumns(r, keys);
                    if(destRows.TryGetValue(hashSource, out var destRow)) {
                        if(!overwrite) continue; //la salta se già c'è
                        T.Rows.Remove(destRow);
                    }
                    T.Rows.Add(r);
                    r.AcceptChanges();
                    resList.Add(r);
                }
            }

             StopTimer(handle2);
            return resList.ToArray();
        }

        /// <summary>
        /// Reads rows from db and merges them into a table, skipping existent rows. Only new merged rows are returned.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task<DataRow[]> _safeMergeFromDb(this DataTable T, IDataAccess conn, MetaExpression filter, int timeout = -1) {
            var res = await T._getDetachedRowsFromDb(conn, filter, timeout);
            if (res == null) return new DataRow[] {};
            if (res.Length == 0) return new DataRow[] {};

            return _mergeRows(T ,res, false);           
        }

        /// <summary>
        ///  Reads rows from db and merges them into a table, skipping existent rows. Only new merged rows are returned.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public static async Task< DataRow[]> _sqlSafeMergeFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1) {
            var res = await T.sqlDetachedGetFromDb(conn, sql, timeout);
            if (res == null) return new DataRow[] {};

            return _mergeRows(T ,res, false);          
        }

        /// <summary>
        /// Adds an array of DataRows to the table
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="rows"></param>
        /// <param name="withCheck">if true rows already present are removed </param>
        public static void mergeRows(this DataTable T, DataRow[] rows, bool withCheck = true) {
            if (rows == null) return;

            if (T.Rows.Count > 300 || rows.Length > 300) {
                _mergeRows(T,rows,!withCheck);
                return;
            }

            foreach (var r in rows) {
                if (withCheck) {
                    var found = T.filter(q.mCmp(r, T.PrimaryKey)).FirstOrDefault();
                    if (found!=null) T.Rows.Remove(found);
                }
                T.Rows.Add(r);
                r.AcceptChanges();
            }

        }


        /// <summary>
        /// Creates a new Row copying all fields from sample (for each field in common)
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static DataRow NewRowAs(this DataTable T, DataRow sample) {
            DataRow r = T.NewRow();
            if (sample == null) return r;
            foreach (DataColumn c in T.Columns) {
                if (sample.Table.Columns.Contains(c.ColumnName)) r[c.ColumnName] = sample[c.ColumnName];
            }
            return r;
        }

        /// <summary>
        /// get a sorted array of rows 
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="sort">string sort order</param>
        /// <param name="rv">RowState to consider</param>
        /// <returns></returns>
        public static DataRow [] _Sort(this DataTable T, string sort, DataViewRowState rv = DataViewRowState.CurrentRows) {
            return T.Select(null, sort, rv);            
        }

        /// <summary>
        /// Gets the index manager if it exists
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static IIndexManager getCreateIndexManager(this DataSet d) {
	        if (d == null) return null;
			if (d.ExtendedProperties["IIndexManager"] is IIndexManager idm) return idm;
			return new IndexManager(d);
        }

		/// <summary>
		/// Gets the index manager if it exists
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
        public static IIndexManager getIndexManager(this DataSet d) {
	        if (d == null) return null;
			var idm= d.ExtendedProperties.ContainsKey("IIndexManager") ? d.ExtendedProperties["IIndexManager"] as IIndexManager : null;
			if (idm == null) return null;
			if (idm.linkedDataSet() != d) return null;
			return idm;
		}

		/// <summary>
		/// Sets the index manager 
		/// </summary>
		/// <param name="d"></param>
		/// <param name="i"></param>
        public static void setIndexManager(this DataSet d, IIndexManager i) {
	        d.ExtendedProperties["IIndexManager"] = i;
        }

		/// <summary>
		/// Sets the index manager 
		/// </summary>
		/// <param name="d"></param>
		/// <param name="i"></param>
		public static void copyIndexFrom(this DataSet dest, DataSet source) {
			var i = new IndexManager(dest);
			i.CopyIndexFrom(source);
		}

         /// <summary>
        /// Search all rows that satisfies a criteria
        /// </summary>
        /// <param name="T">Table to operate with</param>
        /// <param name="filter">criteria to be met</param>
        /// <param name="env">Environment (a DataAccess is a good parameter)</param>
        /// <param name="sort">sorting</param>
        /// <param name="all">if all=true also deleted rows are retrived</param>
        /// <returns></returns>
        public static DataRow[] filter(this DataTable T, q filter, ISecurity env = null, string sort = null, bool all = false) {
            if (filter is null) filter = q.constant(true);           
            if (!(filter is null) && filter.isFalse())return new DataRow[0];
            if (sort != null) {
                return (from DataRow r in T._Sort(sort)
                        where MetaTable.compatibleState(r.RowState, all) && filter.getBoolean(r, env)
                        select r as DataRow)
                        .ToArray();
            }

            if (all == false ) {
	            var idm = T.DataSet.getIndexManager();
	            if (idm != null) {
		            var dict = (filter as IGetCompDictionary)?.getMcmpDictionary();
		            if (dict != null) return idm.getRows(T, dict);
	            }
            }
            return (from DataRow  r in T.Rows where MetaTable.compatibleState(r.RowState, all) && filter.getBoolean(r, env) select r).ToArray();
        }

		///// <summary>
		/////  Gets the first row of a search, or null if no row is found
		///// </summary>
		///// <param name="T">Table to operate with</param>
		///// <param name="filter">condition for the search</param>
		///// <param name="env">environment (use a DataAccess for this)</param>
		///// <param name="sort">row sorting</param>
		///// <returns></returns>
		//public static DataRow _First(this DataTable T, q filter, object env = null, string sort = null) {
  //          if (sort != null) {
  //              DataRow [] found = T._Sort(sort);
  //              foreach (DataRow r in found) {
  //                  if (r.RowState != DataRowState.Deleted && filter.getBooleanResult(r, env)) {
  //                      return r;
  //                  }
  //              }
  //          }
  //          foreach (DataRow r in T.Rows) {
  //              if (r.RowState != DataRowState.Deleted && filter.getBooleanResult(r)) {
  //                  return r;
  //              }
  //          }
  //          return null;
  //      }

        ///// <summary>
        ///// Gets the first row of a search, or null if no row is found
        ///// </summary>
        ///// <param name="T">Table to operate with</param>
        ///// <param name="filter">condition for the search</param>
        ///// <param name="sort">row sorting</param>
        ///// <param name="rv">DataViewRowState to be met</param>
        ///// <returns></returns>
        //public static DataRow _First(this DataTable T, string filter = null, string sort = null, DataViewRowState rv = DataViewRowState.CurrentRows) {
        //    DataRow[] r = T.Select(filter, sort, rv);
        //    if (r.Length == 0) return null;
        //    return r[0];
        //}



        /// <summary>
        /// Use  as  var myConstructor = CreateConstructor(typeof(MyClass), typeof(int), typeof(string));
        /// var myObject = myConstructor(10, "test message");
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ConstructorDelegate CreateConstructor(Type type, params Type[] parameters) {
            // Get the constructor info for these parameters
            var constructorInfo = type.GetConstructor(parameters);

            // define a object[] parameter
            var paramExpr = Expression.Parameter(typeof(Object[]));

            // To feed the constructor with the right parameters, we need to generate an array 
            // of parameters that will be read from the initialize object array argument.
            var constructorParameters = parameters.Select((paramType, index) =>
                // convert the object[index] to the right constructor parameter type.
                Expression.Convert(
                    // read a value from the object[index]
                    Expression.ArrayAccess(
                        paramExpr,
                        Expression.Constant(index)),
                    paramType)).ToArray();

            // just call the constructor.
            var body = Expression.New(constructorInfo, constructorParameters);

            var constructor = Expression.Lambda<ConstructorDelegate>(body, paramExpr);
            return constructor.Compile();
        }

        /// <summary>
        /// Adds a relation to the DataSet
        /// </summary>
        /// <param name="D"></param>
        /// <param name="relationName"></param>
        /// <param name="parentTable"></param>
        /// <param name="childTable"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static DataRelation defineRelation(this DataSet D, string relationName,
                string parentTable, string childTable, params string[] columns) {

            if (D.Relations.Contains(relationName)) return D.Relations[relationName];
            DataColumn[] parent = new DataColumn[columns.Length];
            DataColumn[] child = new DataColumn[columns.Length];
            for (int i = 0; i < columns.Length; i++) {
                parent[i] = D.Tables[parentTable].Columns[columns[i]];
                child[i] = D.Tables[childTable].Columns[columns[i]];
            }
            DataRelation rel = new DataRelation(relationName, parent, child);
            D.Relations.Add(rel);
            return rel;
        }

        
        #region CHECKS FOR TRUE/FALSE UPDATES
        /// <summary>
        /// returns true if row (modified) is an improperly set modified row
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public static bool IsFalseUpdate(this DataRow R){
            if (R.RowState != DataRowState.Modified) return false;
			foreach (DataColumn C in R.Table.Columns){
				if (C.IsTemporary())continue;
				if (!R[C,DataRowVersion.Original].Equals(R[C,DataRowVersion.Current])) return false;
			}
			return true;
		}

        /// <summary>
        /// Remove false update from a DataSet, i.e. calls AcceptChanges
        ///  for any DataRow set erroneously as modified
        /// </summary>
        /// <param name="DS"></param>
        public static void RemoveFalseUpdates(this DataSet DS){            
	        foreach(DataTable T in DS.Tables){
		        if (MetaModel.IsTemporaryTable(T))continue;
		        foreach (DataRow R in T.Rows) {
			        if (R.RowState != DataRowState.Modified) continue;
			        if (R.IsFalseUpdate())	R.AcceptChanges();
		        }
	        }
        }

        /// <summary>
        /// Check if table contains any row not in Unchanged state
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
	    public static bool HasChanges(this DataTable t) {
            if (t == null) return false;
            foreach (DataRow R in t.Rows) {
                if (R.RowState == DataRowState.Unchanged) continue;
                if (R.RowState != DataRowState.Modified) return true;
                if (R.IsFalseUpdate()) {
                    R.AcceptChanges();
                    continue;
                }
                return true;
            }
            return false;
        }

        public static void RemoveFalseUpdates(this DataRow R) {
	        if (!R.IsFalseUpdate()) return;
            R.AcceptChanges();
        }

        /// <summary>
		/// Check if a Row has changes, automatically removing false updates
		/// </summary>
		/// <param name="R"></param>
		/// <returns></returns>
		public static bool HasChanges(this DataRow R){
			if (R.RowState == DataRowState.Detached) return false;
			if (R.RowState == DataRowState.Unchanged) return false;
			if (R.RowState == DataRowState.Added) return true;
			if (R.RowState == DataRowState.Deleted) return true;
			if (R.IsFalseUpdate()){
				R.AcceptChanges();
				return false;
			}
			else {
				return true;
			}
		}


		#endregion

      
        /// <summary>
        /// Get all current rows of the table
        /// </summary>
        /// <returns></returns>
        public static List<R> allCurrent<R>(this TypedTableBase<R> T) where R:DataRow{
            return (from R r in T.Rows where r.RowState != DataRowState.Deleted select r).ToList();
        }

        /// <summary>
        /// Removes false updates, i.e. not real updates or changes on temporary fields
        /// </summary>
        /// <param name="T"></param>
        public static void RemoveFalseUpdates(this DataTable T) {
            foreach (DataRow r in T.Rows) {
                if (r.RowState != DataRowState.Modified) continue;
                if (r.IsFalseUpdate()) r.AcceptChanges();
            }
        }

        /// <summary>
        /// Apply a filter on a table during any further read
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        public static void SetStaticFilter(this DataTable T, object filter) => T.ExtendedProperties["filter"] = filter;



        /// <summary>
        /// Sets the maximum length allowed for a string field
        /// </summary>
        /// <param name="T"></param>
        /// <param name="fieldName"></param>
        /// <param name="len"></param>
        public static void SetMaxLen(this DataColumn C, int len) => C.ExtendedProperties["maxstringlen"] = len;


        /// <summary>
        /// Gets the maximum length allowed for a string field. This is set during form
        ///  fill dependingly of DB-structure
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public static int GetMaxLen(this DataColumn C) {
            if (C == null) return 32767;
            if (C.DataType.ToString() == "System.Decimal") {
                return 20;
            }
            if (C.DataType.ToString() == "System.Float") {
                return 20;
            }
            if (C.DataType.ToString() == "System.Double") {
                return 20;
            }
            if (C.DataType.ToString() == "System.Int32") {
                return 10;
            }
            if (C.DataType.ToString() == "System.Int16") {
                return 5;
            }

            if (C.ExtendedProperties["maxstringlen"] == null) return 2147483647;
            return (int) C.ExtendedProperties["maxstringlen"];
        }

        /// <summary>
        /// Delegate for implementing custom filter on grid displaying
        /// </summary>
        public delegate bool FilterRowsDelegate(DataRow r, string listType);

        
        /// <summary>
        /// Add a filter function to a DataTable, that will be used when 
        ///  the table will be displayed in grids
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        public static void FilterWith(this DataTable T, FilterRowsDelegate filter) {
            T.ExtendedProperties["FilterFunction"] = filter;
        }

      
        public static FilterRowsDelegate getFilterFunction(this DataTable T) {
            return T.ExtendedProperties["FilterFunction"] as FilterRowsDelegate;
        }

        /// <summary>
        /// Sets the DenyNull property of a DataColumn
        /// </summary>
        /// <param name="c"></param>
        /// <param name="deny"></param>
        public static void SetDenyNull(this DataColumn c, bool deny = true) => c.ExtendedProperties["DenyNull"] = deny;


        /// <summary>
        /// Returns true if a DataColumn is denied to store null values
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public static bool IsDenyNull(this DataColumn C) {
            if (C.ExtendedProperties["DenyNull"] == null) return false;
            return ((bool) C.ExtendedProperties["DenyNull"]);
        }


        /// <summary>
        /// Sets a DataColumn to deny zero values
        /// </summary>
        /// <param name="c"></param>
        /// <param name="fieldName"></param>
        /// <param name="deny"></param>
        public static void SetDenyZero(this DataColumn c, bool deny = true) => c.ExtendedProperties["DenyZero"] = deny;

         /// <summary>
        ///  Returns true if a DataColumn is denied to store zero values
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public static bool IsDenyZero(this DataColumn C) {
            if (C.ExtendedProperties["DenyZero"] == null) return false;
            return ((bool) C.ExtendedProperties["DenyZero"]);
        }


         
		/// <summary>
		/// Gets the expression that has been assigned to a DataColumn
		/// </summary>
		/// <param name="C"></param>
		/// <returns></returns>
        public static string GetExpression(this DataColumn C){
		    if (!(C.ExtendedProperties[IsTempColumn] is string))return null;
			var s= C.ExtendedProperties[IsTempColumn].ToString();
			if (s=="") return null;
            return s;
        }


	    /// <summary>
	    /// Gets the expression that has been assigned to a DataColumn
	    /// </summary>
	    /// <param name="C"></param>
	    /// <returns></returns>
	    public static MetaExpression GetMetaExpression(this DataColumn C){
	        return C.ExtendedProperties[IsTempColumn] as MetaExpression;
	    }



		/// <summary>
		/// Assign an expression to a given DataColumn. After this operation,
		///  the DataColumn is no longer considered "real"
		/// </summary>
		/// <param name="C"></param>
		/// <param name="S">string (table.column) or MetaExpression</param>
        public static void SetExpression(this DataColumn C, object S){
            C.ExtendedProperties[IsTempColumn]=S;
        }

		private const string IsTempColumn = "IsTemporaryColumn";

		/// <summary>
		/// Tells if a column is temporary, i.e. is not real.
		/// </summary>
		/// <param name="C"></param>
		/// <returns></returns>
        public static bool IsTemporary(this DataColumn C){
			return !C.IsReal();
        }

		
		/// <summary>
		///  Determines wheter a DataColumn is real or not. For example,
		///  columns that have been assigned expressions are not real.
		///  Also, columns whose name starts with "!" are considered not real.
		///  If a column is not real, it is never read/written to DB
		/// </summary>
		/// <param name="C"></param>
		/// <returns></returns>
        public static bool IsReal(this DataColumn C){
            if (C.ColumnName.StartsWith("!")) return false;
            if (C.ExtendedProperties[IsTempColumn]!=null) return false;
            if (C.Expression==null) return true;
            if (C.Expression=="") return true;
            return false;
        }


         /// <summary>
         /// Gets the Column name to use for posting a given field into DB
         /// </summary>
         /// <param name="C"></param>
         /// <returns>null if column is not for posting</returns>
         public static string PostingColumnName(this DataColumn C){
	         //Se non c'è ForPosting Table o PostingColumn la posting column è la stessa
	         if (C.Table.ExtendedProperties["ForPosting"]==null) return C.ColumnName;
	         if (C.ExtendedProperties["ForPosting"]==null) return C.ColumnName;
	         if (C.ExtendedProperties["ForPosting"].ToString()=="") return null;
	         //Altrimenti è la PostingColumn
	         return C.ExtendedProperties["ForPosting"].ToString();
         }

		
         /// <summary>
         /// To avoid posting of a field, it's posting col name must be "" (not null)
         /// </summary>
         /// <param name="C"></param>
         /// <param name="ColumnForPosting"></param>
         public static void SetColumnNameForPosting(this DataColumn C, string ColumnForPosting){
	         C.ExtendedProperties["ForPosting"]= ColumnForPosting;
         }

        /// <summary>
		/// Set the table from which T will be read. I.e. T is a virtual ALIAS for tablename.
		/// </summary>
		/// <param name="T">Table to set as Alias</param>
		/// <param name="tablename">Real table name</param>
		public static void setTableForReading(this DataTable T, string tablename) {
            T.ExtendedProperties["TableForReading"] = tablename;
        }
        /// <summary>
        /// Get the table from which T will be read. I.e. T is a virtual ALIAS for tablename.
        /// </summary>
        /// <param name="T">Table to set as Alias</param>
        public static string tableForReading(this DataTable T) {
            return T.ExtendedProperties["TableForReading"]?.ToString() ?? T.TableName;
        }

        /// <summary>
        /// Set the table to which T will be written. 
        /// </summary>
        /// <param name="T">Table to set as Alias</param>
        /// <param name="TableForPosting">Real table name</param>
        public static void setTableForPosting(this DataTable T, string TableForPosting) {
            T.ExtendedProperties["ForPosting"] = TableForPosting;
        }
        /// <summary>
        /// Set the table from which T will be read. I.e. T is a virtual ALIAS for tablename.
        /// </summary>
        /// <param name="T">Table to set as Alias</param>
        public static string tableForPosting(this DataTable T) {
            return T.ExtendedProperties["ForPosting"]?.ToString() ?? 
                   T.ExtendedProperties["TableForReading"]?.ToString() ?? 
                   T.TableName;
        }

        /// <summary>
        /// Keeps the last selected row of a Table in an extended properties of the Table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="r"></param>
        public static void _setLastSelected(this DataTable T,DataRow r) {
            T.ExtendedProperties["LastSelectedRow"] = r;
        }

        /// <summary>
        /// Get Last Selected Row in a specified DataTable
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static DataRow _getLastSelected(this DataTable T) {
            var r = (DataRow) T.ExtendedProperties["LastSelectedRow"];
            if (r == null) return null;
            if (r.RowState == DataRowState.Deleted) return null;
            if (r.RowState == DataRowState.Detached) return null;
            return r;
        }

        /// <summary>
        /// Establish an order in posting to database, this is generally unnecessary
        /// </summary>
        /// <param name="t"></param>
        /// <param name="skip"></param>
        public static void setSkipInsertCopy(this DataTable t, bool skip = true) {
            t.ExtendedProperties["skipInsertCopy"] = skip;
        }

        /// <summary>
        /// Mark this table to not be verified by Security conditions
        /// </summary>
        /// <param name="T"></param>
        public static void SetSkipSecurity(this DataTable T) {
            T.ExtendedProperties["SkipSecurity"] = true;
        }

        /// <summary>
        /// Set the table to never be read. It is marked like a cached table that has already been read.
        /// </summary>
        /// <param name="T"></param>
        public static void setCached(this DataTable T) {
            T.ExtendedProperties["cached"] = "1";
        }

        /// <summary>
        /// Returns true if table is cached (the table may or may not 
        ///  have been read) 
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static bool isCached(this DataTable T){
            if (T.ExtendedProperties["cached"]==null) return false;
            return true;
        }

        /// <summary>
        /// Tells if a table should be cleared and read again during a refresh.
        /// Cached tables are not read again during refresh if they have been already been read
        /// </summary>
        /// <param name="T"></param>
        /// <returns>true if table should be read</returns>
        public  static bool CanRead(DataTable T){
            if (T.ExtendedProperties["cached"]==null) return true;
            if (T.ExtendedProperties["cached"].ToString()=="0")return true;
            return false;
        }

       
       

        /// <summary>
        /// Mark a table as cached
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="addBlankRow"></param>
        public static void CacheTable(this DataTable T, object filter = null, string sort = null, bool addBlankRow = false) {
            metaModel.CacheTable(T, filter:filter, sort: sort,  addBlankRow);
        }

       

        /// <summary>
		/// Deny table clearing when a FrehForm is invoked (clear would happen inside DO_GET)
		/// </summary>
		public static void SetDenyClear(this DataTable T) {
            T.ExtendedProperties["DenyClear"] = "y";
        }

        /// <summary>
        /// Re-Allow table clear when FrehForm is called. Undoes the effect of a DenyClear
        /// </summary>
        public static void setAllowClear(this DataTable T) {
            T.ExtendedProperties["DenyClear"] = null;
        }

        /// <summary>
        /// Set sorting for a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="sort"></param>
        public static void setSorting(this DataTable T, string sort) {
            T.ExtendedProperties["sort_by"] = sort;
        }

        /// <summary>
        /// Set sorting for a table
        /// </summary>
        /// <param name="T"></param>
        public static string getSorting(this DataTable T) {
            return T?.ExtendedProperties["sort_by"]?.ToString();
        }

        public static IMetaModel metaModel = new MetaModel();

        /// <summary>
        /// Sets this table as NotEntityChild fo mainTable. So this table will not be cleared during refills.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="mainTable"></param>
        /// <param name="relationName"></param>
        public static void setNotEntityChildOf(this DataTable child, DataTable mainTable, string relationName = null) {           
            if (relationName == null) {
                metaModel.AddNotEntityChild(mainTable, child);
            }
            else {
                metaModel.AddNotEntityChild(mainTable, relationName);
            }
        }

        /// <summary>
        /// Stop this table from being a NotEntityChild
        /// </summary>
        /// <param name="child"></param>
        public static void clearEntityChild(this DataTable child) {
            child.setAllowClear();
            child.ExtendedProperties["NotEntityChild"] = null;
        }

        /// <summary>
        /// Sets the default value for a column (used when a NEW row is created for the table)
        /// </summary>
        /// <param name="T"></param>
        /// <param name="field">field name</param>
        /// <param name="o">default value wanted</param>
        public static void setDefault(this DataTable T, string field, Object o) {
            if (!T.Columns.Contains(field)) return;
            if (o == null) o = DBNull.Value;
            if (o.GetType() == T.Columns[field].DataType || o == DBNull.Value) {
                T.Columns[field].DefaultValue = o;
                return;
            }

            T.Columns[field].DefaultValue =
                mdl.HelpUi.GetObjectFromString(T.Columns[field].DataType, o.ToString(), "x.y");
        }

        /// <summary>
        /// Get maximum value of a field in a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMax<T>(this DataTable t, string field) where T : IComparable {
            return GetMax<T>(t.Select(), field);
        }

        
        /// <summary>
        /// Sets the view Expression for a DataColumn. Expression must be in form table.fieldName
        /// </summary>
        /// <param name="C"></param>
        /// <param name="expr"></param>
        public static void SetViewExpression(this DataColumn C, string expr){
	        C.ExtendedProperties["ViewExpression"] = expr;
        }


        /// <summary>
        /// Gets the view expression assigned to a DataColumn
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>
        public static string ViewExpression(this DataColumn C){
	        return (string) C.ExtendedProperties["ViewExpression"];
        }


        /// <summary>
        /// Get maximum value of a field in a Row collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMax<T>(this IEnumerable<DataRow> rows, string field) where T : IComparable {
            object max = DBNull.Value;
            foreach (DataRow r in rows) {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r[field] == DBNull.Value) continue;
                if (max == DBNull.Value) {
                    max = r[field];
                }
                else {
                    T x = (T)r[field];
                    if (x.CompareTo((T)max) > 0) max = x;
                }
            }
            return max;
        }

        

        /// <summary>
        /// Get Max+1 of a field in a table, 1 if no row was present
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMaxPlusOne<T>(this DataTable t, string field) where T : IComparable {
            return GetMaxPlusOne<T>(t.Select(), field);
        }

        /// <summary>
        /// Get Max+1 of a field in an enumeration, 1 if no row was present
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMaxPlusOne<T>(this IEnumerable<DataRow> rows, string field) where T : IComparable {
            if (typeof(T) == typeof(decimal)) {
                object o = GetMax<decimal>(rows, field);
                if (o == DBNull.Value) return (decimal)1;
                return ((decimal)o) + 1;
            }
            if (typeof(T) == typeof(int)) {
                object o = GetMax<int>(rows, field);
                if (o == DBNull.Value) return (int)1;
                return ((int)o) + 1;
            }
            if (typeof(T) == typeof(long)) {
                object o = GetMax<long>(rows, field);
                if (o == DBNull.Value) return (long)1;
                return ((long)o) + 1;
            }

            if (typeof(T) == typeof(double)) {
                object o = GetMax<double>(rows, field);
                if (o == DBNull.Value) return (double)1;
                return ((double)o) + 1;
            }

            throw new Exception($"Il tipo {typeof(T)} non è supportato dalla funzione GetMaxPlusOne()");
        }

        /// <summary>
        /// Get minimum value of a field in a row collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMin<T>(this IEnumerable<DataRow> rows, string field) where T : IComparable {
            object min = DBNull.Value;
            foreach (DataRow r in rows) {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r[field] == DBNull.Value) continue;
                if (min == DBNull.Value) {
                    min = r[field];
                }
                else {
                    T x = (T)r[field];
                    if (x.CompareTo((T)min) < 0) min = x;
                }
            }
            return min;
        }

        /// <summary>
        /// Get minimum value of a field in a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetMin<T>(this DataTable t, string field) where T : IComparable {
            return GetMin<T>(t.Select(), field);
        }

        /// <summary>
        /// Get Sum of a field in a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetSum<T>(this DataTable t, string field) where T : IComparable {
            if (typeof(T) == typeof(decimal)) {
                return (from DataRow r in t.Rows
                        where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                        select (Decimal)r[field]).Sum();
            }
            if (typeof(T) == typeof(int)) {
                return (from DataRow r in t.Rows
                        where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                        select (int)r[field]).Sum();
            }
            if (typeof(T) == typeof(long)) {
                return (from DataRow r in t.Rows
                        where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                        select (long)r[field]).Sum();
            }

            if (typeof(T) == typeof(double)) {
                return (from DataRow r in t.Rows
                        where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                        select (double)r[field]).Sum();
            }
            throw new Exception($"Il tipo {typeof(T)} non è supportato dalla funzione GetSum()");
        }

        /// <summary>
        /// Get Sum of a field in a row collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetSum<T>(this IEnumerable<DataRow> rows, string field) where T : IComparable {
            if (typeof(T) == typeof(decimal)) {
                return (from DataRow r in rows
                    where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                    select (Decimal)r[field]).Sum();
            }
            if (typeof(T) == typeof(int)) {
                return (from DataRow r in rows
                    where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                    select (int)r[field]).Sum();
            }
            if (typeof(T) == typeof(long)) {
                return (from DataRow r in rows
                    where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                    select (long)r[field]).Sum();
            }

            if (typeof(T) == typeof(double)) {
                return (from DataRow r in rows
                    where r.RowState != DataRowState.Deleted & r[field] != DBNull.Value
                    select (double)r[field]).Sum();
            }
            throw new Exception($"Il tipo {typeof(T)} non è supportato dalla funzione GetSum()");
        }

        

        /// <summary>
		/// Add a selector-column to the table. AutoIncrement columns are calculated between
		///  equal selectors-column rows
		/// </summary>
		/// <param name="T"></param>
		/// <param name="columnName"></param>
        /// <param name="mask"></param>
        public static void SetSelector(this DataTable T, string columnName, UInt64 mask = 0) {
            if (mask == 0) {
                RowChange.setSelector(T, columnName);
            }
            else {
                RowChange.setSelector(T, columnName, mask);
            }
        }

   

        /// <summary>
        /// Remove a main selector in a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="columnName"></param>
        public static void ClearSelector(this DataTable T, string columnName) {
            RowChange.clearSelector(T, columnName);
        }

        /// <summary>
        /// Set a selector for a column
        /// </summary>
        /// <param name="T"></param>
        /// <param name="sourceColumn">autoincrement column</param>
        /// <param name="columnName">selector column</param>
        /// <param name="mask"></param>
        public static void SetSelector(this DataColumn c, string columnName, UInt64 mask=0) {
            RowChange.setMySelector(c, columnName, mask);
        }

        /// <summary>
		/// Mark a column as an autoincrement, specifying how the calculated ID must be
		///  composed.
		/// </summary>
        /// <param name="T">Table implied</param>
		/// <param name="field">Column to set</param>
		/// <param name="prefix">field of rows to be put in front of ID</param>
		/// <param name="middle">middle constant part of ID</param>
		/// <param name="length">length of the variable part of the ID</param>
        /// <param name="linear">if true, Selector Fields, Middle Const and Prefix </param>
		/// <remarks>
		/// The field will be calculated like:
		/// [Row[PrefixField]] [MiddleConst] [LeftPad(newID, IDLength)]
		/// so that, called the first part [Row[PrefixField]] [MiddleConst] as PREFIX,
		/// if does not exists another row with the same PREFIX for the ID, the newID=1
		/// else newID = max(ID of same PREFIX-ed rows) + 1
		/// </remarks>
		static public void SetAutoincrement(this DataColumn c,
                string prefix,
                string middle,
                int length,
                bool linear = false) {        
            RowChange.markAsAutoincrement(c, prefix, middle, length, linear);
        }

        /// <summary>
		/// Tells whether a Column is a AutoIncrement 
		/// </summary>
		/// <param name="C"></param>
		/// <returns>true if Column is Auto Increment</returns>
        static public bool IsAutoIncrement(this DataColumn C){
			return RowChange.isAutoIncrement(C);
		}

        /// <summary>
        /// Tells whether a Column is a Custom AutoIncrement one
        /// </summary>
        /// <param name="C"></param>
        /// <returns>true if Column is Custom Auto Increment</returns>
        static public bool IsCustomAutoIncrement(this DataColumn C){
			return RowChange.isCustomAutoIncrement(C);
		}

        	/// <summary>
		/// Removes autoincrement property from a DataColumn
		/// </summary>
		/// <param name="C"></param>
		static internal void clearAutoIncrement(this DataColumn C){
			RowChange.clearAutoIncrement(C);
		}

        /// <summary>
        /// Removes autoincrement property from a DataColumn
        /// </summary>
        /// <param name="T"></param>
        /// <param name="field"></param>
        static public void clearAutoIncrement(this DataTable T, string field) {
            var c = T.Columns[field];
            RowChange.clearAutoIncrement(c);
        }

        /// <summary>
        /// Set a custom autoincrement function
        /// </summary>
        /// <param name="T"></param>
        /// <param name="field"></param>
        /// <param name="customFunction"></param>
        static public void setCustomAutoincrement(this DataTable T, string field,
            RowChange.CustomCalcAutoId customFunction) {
            DataColumn c = T.Columns[field];
            RowChange.markAsCustomAutoincrement(c, customFunction);
        }

        /// <summary>
		/// Removes Custom-autoincrement property from a DataColumn
		/// </summary>
        /// <param name="T"></param>
		/// <param name="field"></param>
		static public void clearCustomAutoIncrement(this DataTable T, string field) {
            DataColumn c = T.Columns[field];
            RowChange.clearCustomAutoIncrement(c);
        }

        /// <summary>
        /// Tells postData not to evaluate every autoincrement column with a read from db.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="isOptimized"></param>
        public static void setOptimized(this DataTable T, bool isOptimized) {
            RowChange.SetOptimized(T, isOptimized);
        }

        /// <summary>
        /// Set a minimum for temporary values when an autoincrement field is evalued
        /// </summary>
        /// <param name="T"></param>
        /// <param name="field"></param>
        /// <param name="min"></param>
        public static void setMinimumTempValue(this DataTable T, string field, int min) {
            DataColumn c = T.Columns[field];
            RowChange.SetMinimumTempValue(c, min);
        }

        /// <summary>
        /// Set a minimum for temporary values when an autoincrement field is evalued
        /// </summary>
        /// <param name="T"></param>
        /// <param name="field"></param>
        public static int getMinimumTempValue(this DataTable T, string field) {
            var c = T.Columns[field];
            if (c.ExtendedProperties["minimumTempValue"] == null) return 0;
            if (c.ExtendedProperties["minimumTempValue"].GetType() != typeof(int)) return 0;
            return Convert.ToInt32(c.ExtendedProperties["minimumTempValue"]);
        }

        /// <summary>
        /// Sets the row sorting for posting to the db
        /// </summary>
        /// <param name="T"></param>
        /// <param name="order"></param>
        public static void setPostingOrder(this DataTable T, string order) {
            PostData.SetPostingOrder(T, order);
        }

       

        /// <summary>
        /// Evaluates all temporary columns of the row
        /// </summary>
        /// <param name="r">Row to be evaluated</param>
        /// <param name="C">optional column to evaluate, otherwise all temporary column are evaluated</param>
        public static void calcTemporaryID(this DataRow r,DataColumn C=null) {
            if (C != null) {
                RowChange.calcTemporaryID(r, C);
            }
            else {
                RowChange.CalcTemporaryID(r);
            }
            
        }

        /// <summary>
        ///  Query table for  this.field = sample[field]
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_EqObj<R>(this TypedTableBase<R> tab, string field, object sample) where R : DataRow {
	        var idx = tab.DataSet?.getIndexManager()?.getIndex(tab,field);
	        if (idx != null) {
		        return Array.ConvertAll(idx.getRows(idx.hash.getFromDictionary(new Dictionary<string, object>(){{field,q.getField(field,sample)}})), item=>item as R);
	        }
            return f_EqObj(tab.allCurrent(), field, sample);
        }

        /// <summary>
        /// Query table for  this.field = sample[field]
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_EqObj(this DataTable tab, string field, object sample) {
	        var idx = tab.DataSet?.getIndexManager()?.getIndex(tab,field);
	        if (idx != null) {
		        return idx.getRows(idx.hash.getFromDictionary(new Dictionary<string, object>(){{field, q.getField(field,sample)}}));
	        }
            return f_Eq(tab.Select(), field, sample);
        }

        /// <summary>
        /// Query a row collection for  this.field = sample[field]
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field">Field to compare</param>
        /// <param name="sample">value to compare</param>
        /// <returns></returns>
        public static IEnumerable<R> f_EqObj<R>(this IEnumerable<R> rows, string field, object sample) where R : DataRow {
            var o = MetaExpression.getField(field, sample);
            var m = MetaExpression.eq(field, o);
            return (from R r in rows where m.getBoolean(r) select r);
        }


        /// <summary>
        /// Get parent a row in a parent table. Equivalent to GetParentRow()
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="row"></param>
        /// <param name="table">parent table</param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        /// <example>
        /// DataRow r;
        /// r.parent(DS.ParentTable)      gives all rows related to r in DS.ParentTable
        /// </example>
        public static S parent<S>(this DataRow row, MetaTableBase<S> table, string relationName = null) where S : MetaRow {
            DataRow first = row;
            string currTable = first.Table.TableName;
            MetaExpressionGenerator foundRel = null;
            foreach (DataRelation rel in table.ChildRelations) {
                if (rel.RelationName == relationName) {
                    foundRel = MetaExpression.parent(rel);
                    break;
                }
                if (rel.ChildTable.TableName == currTable) {
                    foundRel = MetaExpression.parent(rel);
                    break;
                }
            }
            if (foundRel == null) return null;
            var filterRelation = foundRel(row);
            return table.Filter(filterRelation).FirstOrDefault();
        }

        /// <summary>
        /// Get child rows in a given child table. Equivalent to GetChildRows()
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="row"></param>
        /// <param name="table"></param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        /// <example>
        /// DataRow r;
        /// r.childs(DS.ChildTable)      gives all child rows of r in DS.ChildTable
        /// </example>
        public static IEnumerable<S> childs<S>(this DataRow row, MetaTableBase<S> table, string relationName = null) where S : MetaRow {
            var first = row;
            var currTable = first.Table.TableName;
            MetaExpressionGenerator foundRel = null;
            foreach (DataRelation rel in table.ParentRelations) {
                if (rel.RelationName == relationName) {
                    foundRel = MetaExpression.child(rel);
                    break;
                }
                if (rel.ParentTable.TableName == currTable) {
                    foundRel = MetaExpression.child(rel);
                    break;
                }
            }
            if (foundRel == null) yield break;
            var filterRelation = foundRel(row);
            foreach (S result in table.Filter(filterRelation)) {
                yield return result;
            }
        }

        /// <summary>
        /// Get rows related in a given table
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="row"></param>
        /// <param name="table"></param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        /// <example>
        /// DataRow r;
        /// r.related(DS.ParentTable)      gives all rows related to r in DS.ParentTable
        /// </example>
        public static IEnumerable<S> related<S>(this DataRow row, MetaTableBase<S> table, string relationName = null) where S : MetaRow {
            var first = row;
            var currTable = first.Table.TableName;
            MetaExpressionGenerator foundRel = null;
            foreach (DataRelation rel in table.ChildRelations) {
                if (rel.RelationName == relationName) {
                    foundRel = MetaExpression.parent(rel);
                    break;
                }
                if (rel.ChildTable.TableName == currTable) {
                    foundRel = MetaExpression.parent(rel);
                    break;
                }
            }
            if (foundRel == null) {
                foreach (DataRelation rel in table.ParentRelations) {
                    if (rel.RelationName == relationName) {
                        foundRel = MetaExpression.child(rel);
                        break;
                    }
                    if (rel.ParentTable.TableName == currTable) {
                        foundRel = MetaExpression.child(rel);
                        break;
                    }
                }
            }
            if (foundRel == null) yield break;            
            var filterRelation = foundRel(row);
            foreach (S result in table.Filter(filterRelation)) yield return result;            
        }

        /// <summary>
        /// Get rows related in a given table 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="rows"></param>
        /// <param name="table"></param>
        /// <param name="relationName"></param>
        /// <returns></returns>
        public static IEnumerable<S> related<R, S>(this IEnumerable<R> rows,
	        MetaTableBase<S> table,
	        string relationName = null) where R : DataRow where S : MetaRow {
	        // ReSharper disable once PossibleMultipleEnumeration
	        R first = rows.First();
	        var currTable = first.Table.TableName;
	        MetaExpressionGenerator foundRel = null;
	        foreach (DataRelation rel in table.ParentRelations) {
		        if (rel.RelationName == relationName) {
			        foundRel = MetaExpression.parent(rel);
			        break;
		        }

		        if (rel.ChildTable.TableName == currTable) {
			        foundRel = MetaExpression.parent(rel);
			        break;
		        }
	        }

	        if (foundRel == null) {
		        foreach (DataRelation rel in table.ChildRelations) {
			        if (rel.RelationName == relationName) {
				        foundRel = MetaExpression.child(rel);
				        break;
			        }

			        if (rel.ParentTable.TableName == currTable) {
				        foundRel = MetaExpression.child(rel);
				        break;
			        }
		        }
	        }

	        if (foundRel == null) yield break;
	        // ReSharper disable once PossibleMultipleEnumeration
	        foreach (R curr in rows) {
		        var filterRelation = foundRel(curr);
		        foreach (S result in table.Filter(filterRelation)) yield return result;
	        }
        }

        /// <summary>
        /// Query table for field  =  object, eventually using an index when available
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<R>  f_Eq<R>(this TypedTableBase<R> tab, string field, object o) where R : DataRow {
	        var idx = tab.DataSet?.getIndexManager()?.getIndex(tab,field);
	        if (idx != null) {
		        return Array.ConvertAll(idx.getRows(idx.hash.getFromDictionary(new Dictionary<string, object>(){{field,o}})), item=>item as R);
	        }
            return f_Eq(tab.allCurrent(), field, o);
        }

        /// <summary>
        /// Query table for field  =  object, eventually using an index when available
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_Eq(this DataTable tab, string field, object o) {
	        var idx = tab.DataSet?.getIndexManager()?.getIndex(tab,field);
	        if (idx != null) {
		        return idx.getRows(idx.hash.getFromDictionary(new Dictionary<string, object>(){{field,o}}));
	        }
	        
            return f_Eq(tab.Select(), field, o);
        }

        /// <summary>
        /// Query a row collection for field  =  object
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_Eq<R>(this IEnumerable<R> rows, string field, object o) where R : DataRow {
	        var m = MetaExpression.eq(field, o);
            return (from R r in rows where m.getBoolean(r) select r);
        }

        /// <summary>
        /// Query table for this.field  !=  object
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_Ne<R>(this TypedTableBase<R> tab, string field, object o) where R : DataRow {
            return f_Ne(tab.allCurrent(), field, o);
        }

        /// <summary>
        /// Query table for this.field  !=  object
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_Ne(this DataTable tab, string field, object o) {
            return f_Ne(tab.Select(), field, o);
        }

        /// <summary>
        /// Query a row collection for field  !=  object
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_Ne<R>(this IEnumerable<R> rows, string field, object o) where R : DataRow {
            var m = MetaExpression.ne(field, o);
            return (from R r in rows where m.getBoolean(r) select r);
        }

        /// <summary>
        /// Query table for field  !=  sample[field]
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_NeObj<R>(this TypedTableBase<R> tab, string field, object sample) where R : DataRow {
            return f_NeObj(tab.allCurrent(), field, sample);
        }

        /// <summary>
        /// Query table for field  !=  sample[field]
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="field"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_NeObj(this DataTable tab, string field, object sample) {
            return f_NeObj(tab.Select(), field, sample);
        }

        /// <summary>
        /// Query row collection for field  !=  sample[field]
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="rows"></param>
        /// <param name="field"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_NeObj<R>(this IEnumerable<R> rows, string field, object sample) where R : DataRow {
            object o = MetaExpression.getField(field, sample);
            MetaExpression m = MetaExpression.ne(field, o);
            return (from R r in rows where m.getBoolean(r) select r);
        }

        /// <summary>
        /// Query table for field  is null
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="Tab"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_isNull<R>(this TypedTableBase<R> Tab, string field) where R : DataRow {
            return f_isNull(Tab.allCurrent(), field);
        }

        /// <summary>
        /// Query table for field  is null
        /// </summary>
        /// <param name="Tab"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_isNull(this DataTable Tab, string field) {
            return f_isNull(Tab.Select(), field);
        }

        /// <summary>
        /// Query row collection for field  is null
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="Rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_isNull<R>(this IEnumerable<R> Rows, string field) where R : DataRow {
            MetaExpression m = MetaExpression.isNull(field);
            return (from R r in Rows where m.getBoolean(r) select r);
        }

        /// <summary>
        /// Query table for field  is not null
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="Tab"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_isNotNull<R>(this TypedTableBase<R> Tab, string field) where R : DataRow {
            return f_isNotNull(Tab.allCurrent(), field);
        }

        /// <summary>
        /// Query table for field  is not null
        /// </summary>
        /// <param name="Tab"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> f_isNotNull(this DataTable Tab, string field) {
            return f_isNotNull(Tab.Select(), field);
        }

        /// <summary>
        /// Query row collection for field  is null
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="Rows"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<R> f_isNotNull<R>(this IEnumerable<R> Rows, string field) where R : DataRow {
            MetaExpression m = MetaExpression.isNotNull(field);
            return (from R r in Rows where m.getBoolean(r) select r);
        }

        /// <summary>
        /// Condition over 2 object for 3-table join
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <param name="R1"></param>
        /// <param name="R2"></param>
        /// <returns></returns>
        public delegate bool joinCondition2<r1, r2>(r1 R1, r2 R2);

        /// <summary>
        /// Condition over 3 objects for 3-table join
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <param name="R1"></param>
        /// <param name="R2"></param>
        /// <param name="R3"></param>
        /// <returns></returns>
        public delegate bool joinCondition3<r1, r2, r3>(r1 R1, r2 R2, r3 R3);


        /// <summary>
        /// Condition over 4 object for 4-table join
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <typeparam name="r4"></typeparam>
        /// <param name="R1"></param>
        /// <param name="R2"></param>
        /// <param name="R3"></param>
        /// <param name="R4"></param>
        /// <returns></returns>
        public delegate bool joinCondition4<r1, r2, r3,r4>(r1 R1, r2 R2, r3 R3, r4 R4);



   

        /// <summary>
        /// Evaluates an inner join between two ienumerables
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <param name="t1">first list to join</param>
        /// <param name="t2">secondi list to join</param>
        /// <param name="joinFun">condition to apply for the join</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2>> Join<r1, r2>(this IEnumerable<r1> t1, IEnumerable<r2> t2, joinCondition2<r1, r2> joinFun)
                where r1 : DataRow where r2 : DataRow {
            foreach (r1 R1 in t1) {
                foreach (r2 R2 in t2) {
                    if (joinFun(R1, R2)) yield return new Tuple<r1, r2>(R1, R2);
                }
            }
        }

      

        /// <summary>
        /// Evaluates an outer join between two ienumerables
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <param name="t1">first list to join</param>
        /// <param name="t2">secondi list to join</param>
        /// <param name="joinFun">condition to apply for the join</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2>> LeftJoin<r1, r2>(this IEnumerable<r1> t1, IEnumerable<r2> t2, joinCondition2<r1,r2> joinFun)
                    where r1 : DataRow where r2 : DataRow {
            foreach (r1 R1 in t1) {
                bool anyFound = false;
                foreach(r2 R2 in t2) {                    
	                anyFound = true;
                    if (joinFun(R1, R2)) {
                        yield return new Tuple<r1, r2>(R1, R2);
                    }
                }       
                if (!anyFound) yield return new Tuple<r1, r2>(R1, null);
            }
        }
     

        /// <summary>
        ///  Evaluates an inner join between a join and another ienumerable
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <param name="list">left part of join</param>
        /// <param name="t3">list to join</param>
        /// <param name="joinFun"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2,r3>> Join<r1, r2,r3>(this IEnumerable<Tuple<r1, r2>> list, IEnumerable<r3> t3, joinCondition3<r1,r2,r3> joinFun)
                   where r1 : DataRow where r2 : DataRow where r3: DataRow {
            foreach (var tup in list) {                
                foreach (r3 R3 in t3) {
                    if (joinFun(tup.Item1, tup.Item2, R3)) {
                        yield return new Tuple<r1, r2,r3>(tup.Item1, tup.Item2, R3);
                    }
                    
                }
            }
        }

        /// <summary>
        ///  Evaluates an outer join between a join and another ienumerable
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <param name="list">left part of join</param>
        /// <param name="t3">list to join</param>
        /// <param name="joinFun"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2, r3>> LeftJoin<r1, r2, r3>(this IEnumerable<Tuple<r1, r2>> list, IEnumerable<r3> t3, joinCondition3<r1, r2, r3> joinFun)
           where r1 : DataRow where r2 : DataRow where r3 : DataRow {
            foreach (var tup in list) {
                bool anyFound = false;
                foreach (r3 R3 in t3) {
	                anyFound = true;
                    if (joinFun(tup.Item1, tup.Item2, R3)) {
                        yield return new Tuple<r1, r2, r3>(tup.Item1, tup.Item2, R3);
                    }
                }
                if (!anyFound) yield return new Tuple<r1, r2, r3>(tup.Item1, tup.Item2, null);
            }
        }

        /// <summary>
        /// Inner Join with a 4th table
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <typeparam name="r4"></typeparam>
        /// <param name="list"></param>
        /// <param name="t4"></param>
        /// <param name="joinFun"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2, r3,r4>> Join<r1, r2, r3,r4>(this IEnumerable<Tuple<r1, r2,r3>> list, IEnumerable<r4> t4, 
                            joinCondition4<r1, r2, r3,r4> joinFun)
                   where r1 : DataRow where r2 : DataRow where r3 : DataRow where r4 : DataRow {
            foreach (var tup in list) {
                foreach (r4 R4 in t4) {
                    if (joinFun(tup.Item1, tup.Item2, tup.Item3,R4)) {
                        yield return new Tuple<r1, r2, r3,r4>(tup.Item1, tup.Item2, tup.Item3, R4);
                    }
                }
            }
        }

        /// <summary>
        /// Outer Join with a 4th table
        /// </summary>
        /// <typeparam name="r1"></typeparam>
        /// <typeparam name="r2"></typeparam>
        /// <typeparam name="r3"></typeparam>
        /// <typeparam name="r4"></typeparam>
        /// <param name="list"></param>
        /// <param name="t4"></param>
        /// <param name="joinFun"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<r1, r2, r3, r4>> LeftJoin<r1, r2, r3, r4>(this IEnumerable<Tuple<r1, r2, r3>> list, IEnumerable<r4> t4,
                            joinCondition4<r1, r2, r3, r4> joinFun)
                   where r1 : DataRow where r2 : DataRow where r3 : DataRow where r4 : DataRow {
            foreach (var tup in list) {
                bool anyFound = false;
                foreach (r4 R4 in t4) {
                    if (joinFun(tup.Item1, tup.Item2, tup.Item3, R4)) {
                        anyFound = true;
                        yield return new Tuple<r1, r2, r3, r4>(tup.Item1, tup.Item2, tup.Item3, R4);
                    }
                }
                if (!anyFound) yield return new Tuple<r1, r2, r3, r4>(tup.Item1, tup.Item2, tup.Item3, null);
            }
        }


       
        /// <summary>
        /// Return true if table has any row 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool hasRows(this DataTable t) {
            return t?.Rows.Count > 0;
        }

        

        /// <summary>
        /// Check if collection is not empty
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        public static bool _HasRows<r>(this IEnumerable<r> collection) {
            return collection?.Count() > 0;
        }

        /// <summary>
        /// Check if collection is empty
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        public static bool _IsEmpty<r>(this IEnumerable<r> collection) {
            return collection.Count() == 0;
        }

        /// <summary>
        /// Do something for each object in a collection
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        /// <param name="operation"></param>
        public static void _forEach<r>(this IEnumerable<r> collection, Action<r> operation)  {
            if (collection == null) return;
            foreach (r R in collection) {
                operation(R);
            }
        }


        /// <summary>
        /// Get names of all column given
        /// </summary>
        /// <param name="collection"></param>
        public static string[] _names(this DataColumnCollection collection)  {
            var res = new List<string>();
            foreach (DataColumn c in collection) {
                res.Add(c.ColumnName);
            }

            return res.ToArray();
        }

        public static void _forEach(this DataSet d, Action<DataTable> operation) {
            foreach (DataTable t in d.Tables) {
                operation(t);
            }
        }

        /// <summary>
        /// Do something for each DataColumn
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="operation"></param>
        public static void _forEach(this DataColumnCollection collection, Action<DataColumn> operation)  {
            if (collection == null) return;
            foreach (DataColumn c in collection) {
                operation(c);
            }
        }
        

        public static IEnumerable<DataRelation> Enum(this DataRelationCollection rels) {
            foreach(DataRelation r in rels) yield return r;
		}
        public static IEnumerable<DataColumn> Enum(this DataColumnCollection cols) {
            foreach (DataColumn c in cols)
                yield return c;
        }

        ///// <summary>
        ///// Do something for each DataColumn
        ///// </summary>
        ///// <param name="collection"></param>
        ///// <param name="condition"></param>
        //public static bool _any(this DataColumnCollection collection, Func<DataColumn,bool> condition)  {
        //    if (collection == null) return false;
        //    foreach (DataColumn c in collection) {
        //        if (condition(c))return true;
        //    }
        //    return false;
        //}


        /// <summary>
        /// Do something on an object
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="item"></param>
        /// <param name="operation"></param>
        public static TR __do<TR>(this TR item, Action<TR> operation)  {
            operation(item);
            return item;
        }

        /// <summary>
        /// Assign a property of the item to value
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="item"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public static void __setValue<TR>(this TR item, string field, object value) {
            MetaExpression.setField(item, field, value);
        }

		/// <summary>
		/// Maps an enumeration through a mapping function returning an array
		/// </summary>
		/// <typeparam name="R">type of the input enumeration</typeparam>
		/// <typeparam name="S">type of the output array</typeparam>
		/// <param name="collection"></param>
		/// <param name="mapFunc">mapping function</param>
		/// <returns></returns>
		public static S[] Map<R, S>(this IEnumerable<R> collection, Func<R, S> mapFunc) {
            return collection.Select(mapFunc).ToArray();
			//return (from R r in collection select mapFunc(r)).ToArray();
		}

		/// <summary>
		/// Create a dictionary over the collection where the key is given by the result of the keyFunc 
		/// </summary>
		/// <typeparam name="TR">type of the input enumeration</typeparam>
		/// <typeparam name="TS">type of the output array</typeparam>
		/// <param name="collection"></param>
		/// <param name="keyFunc">mapping function</param>
		/// <returns></returns>
		public static Dictionary<TS,TR> _KeyBy<TR, TS>(this IEnumerable<TR> collection, Func<TR, TS> keyFunc) {
            var dict = new Dictionary<TS, TR>();
            foreach(var r in collection) {
                dict[keyFunc(r)] = r;
            }
            return dict;
        }


        /// <summary>
        /// Action that also have index in input
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="R">element of the collection</param>
        /// <param name="i">index of element</param>
        public delegate void operateOnRowIndex<r>(r R, int i);

        /// <summary>
        /// Do something for each object in a collection, also including an index
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="operation"></param>
        public static void _forEach<TR>(this IEnumerable<TR> collection, operateOnRowIndex<TR> operation) {
            var i = 0;
            foreach (var r in collection) {
                operation(r,i);
                i++;
            }
        }

        /// <summary>
        /// Takes a specific field from a collection of object obtaining a list of objects
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<object>  Pick<TR>(this IEnumerable<TR> collection, string field) {
            return collection.Select(r => q.getField(field,r));
        }

        /// <summary>
        /// Filters a collection basing on a given predicate
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<TR> _Filter<TR>(this IEnumerable<TR> collection, Predicate<TR> filter)  {
            if (collection == null) yield break;
            foreach (var r in collection) {
                if (filter(r)) yield return r;
                //if(filter(R))result.Add(R);
            }
            //return result;
        }

        /// <summary>
        /// Exclude rows from a collection basing on a predicate. It is the opposite of _Filter
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<TR> _Reject<TR>(this IEnumerable<TR> collection, Predicate<TR> filter) {
            return collection.Where(r => !filter(r));
        }

        /// <summary>
        /// Find the first object of a collection that satisfies a predicate
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static TR _Find<TR>(this IEnumerable<TR> collection, Predicate<TR> filter) where TR:class {
            return collection?.FirstOrDefault(r => filter(r));
        }

        /// <summary>
        /// Returns all but first element of a collection
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IEnumerable<TR> _Tail<TR>(this IEnumerable<TR> collection) where TR : class {
            if (collection == null) yield break ;
            var first = true;
            foreach (var r in collection) {
                if (!first) yield return r;
                first = false;
            }            
        }

        /// <summary>
        /// Returns all but last element of a collection
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IEnumerable<TR> _Initial<TR>(this IEnumerable<TR> collection) where TR : class {
            if (collection == null) yield break ;
            TR lastElement = null;
            foreach (var r in collection) {
                if (lastElement != null) yield return lastElement;
                lastElement = r;
            }            
        }
        

        /// <summary>
        /// Return first row of a collection
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static TR _First<TR>(this IEnumerable<TR> collection) where TR : class {
            return collection?.FirstOrDefault();            
        }

        /// <summary>
        /// Returs true if a predicate is true for every element of a collection (short circuits)
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static bool _Every<TR>(this IEnumerable<TR> collection, Predicate<TR> condition) where TR : class {
            return collection != null && collection.All(s => condition(s));
        }

        /// <summary>
        /// Returs true if a predicate is true for any element of a collection (short circuits)
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static bool _Some<TR>(this IEnumerable<TR> collection, Predicate<TR> condition) where TR : class {
            return collection != null && collection.Any(r => condition(r));
        }

        /// <summary>
        /// Aggregation function
        /// </summary>
        /// <typeparam name="TR">type of the aggregation result</typeparam>
        /// <typeparam name="TS">type of the element in the collection</typeparam>
        /// <param name="result">result of previos iteration</param>
        /// <param name="value">item of current iteration</param>
        /// <returns></returns>
        public delegate TR accumulate<TR, TS>(TR result, TS value);

        /// <summary>
        /// Evaluates an aggregation function overall a collection
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <typeparam name="TS"></typeparam>
        /// <param name="collection"></param>
        /// <param name="accumulator">function to evaluates, first parameter is previous value of the aggregation, second is current element </param>
        /// <param name="startValue">Start value for the aggregation function</param>
        /// <returns></returns>
        public static TR _Reduce<TR,TS>(this IEnumerable<TS> collection, accumulate<TR,TS> accumulator, TR startValue ){
            var curr = startValue;
            foreach(var s in collection) {
                curr = accumulator(curr, s);
            }
            return curr;
        }

        /// <summary>
        /// Evaluates an aggregation function overall a collection. Starting value is the first element of the collection
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="accumulator">function to evaluates, first parameter is previous value of the aggregation, second is current element </param>
        /// <returns></returns>
        public static TR _Reduce<TR>(this IEnumerable<TR> collection, accumulate<TR, TR> accumulator) {
            var curr = default(TR);
            var first = true;
            foreach (var s in collection) {
                if (first) {
                    first = false;
                    curr = s;
                }
                else {
                    curr = accumulator(curr, s);
                }                
            }
            return curr;
        }

        /// <summary>
        /// Searches the index of the first element that satisfies a predicate
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static int _FindIndex<TR>(this TR[] collection, Predicate<TR> condition) {
            for (var i = 0; i < collection.Length; i++) {
                if (condition(collection[i])) return i;
            }
            return -1;
        }


        /// <summary>
        /// Searches the index of the last element that satisfies a predicate
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static int _FindLastIndex<r>(this r[] collection, Predicate<r> condition) {
            for (var i = collection.Length; i >=0; i--) {
                if (condition(collection[i])) return i;
            }
            return -1;
        }


        /// <summary>
        /// If a collection has any element that satisfies a predicate, the _then function is executed, otherwise the _else when it is given.
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition">Predicate for searching rows in the collection</param>
        /// <param name="_then">action to be taken when any row is found, it takes the first found row as a parameter</param>
        /// <param name="_else">action to be taken  when no row is found</param>
        public static void _IfExists<r>(this IEnumerable<r> collection, Predicate<r> condition, 
                        Action<r> _then=null, Action _else=null) where r:class{
            var found = collection._Find(condition);
            if (found != null) {
                _then?.Invoke(found);
            }
            else {
                _else?.Invoke();
            }
        }

        /// <summary>
        /// If a collection has no element that satisfies a predicate, the _then function is executed
        /// </summary>
        /// <typeparam name="r"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition">Predicate for searching rows in the collection</param>
        /// <param name="_then">action to be taken when no row is found</param>
        public static void _IfNotExists<r>(this IEnumerable<r> collection, Predicate<r> condition,
                   Action _then) where r : class {
            var found = collection._Find(condition);
            if (found == null) _then();            
        }

        /// <summary>
        /// Filters a collection of Rows basing on condition on a parent row
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <typeparam name="TS"></typeparam>
        /// <param name="rows"></param>
        /// <param name="parentTable"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static IEnumerable<TR> _whereParent<TR,TS>(this IEnumerable<TR> rows, MetaTableBase<TS> parentTable,Predicate<TS> condition)
                where TR:DataRow where TS:MetaRow {
             foreach(var r in rows) {
                var parentRow = r.parent(parentTable);
                if (parentRow == null) continue;
                if (condition(parentRow)) yield return r;
            }
        }
       
        ///// <summary>
        ///// Filters a collection with a predicate
        ///// </summary>
        ///// <typeparam name="TR"></typeparam>
        ///// <param name="source"></param>
        ///// <param name="condition"></param>
        ///// <returns></returns>
        //public static IEnumerable<TR> _where<TR>(this IEnumerable<TR> source, Predicate<object> condition) {
        //    return source.Where(r => condition(r));
        //}

        /// <summary>
        /// Select a list of expression from a source collection
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static object[] _Select(this object[] source,
             params object[] expr         
            ) {
            var expressions = expr.Map(o => MetaExpression.fromObject(o, true));
            var l = new List<MetaExpression>();
            var someGroupingFound = false;
            foreach(var e in expressions) {
                if (e.isGroupingFunction()) {
                    someGroupingFound = true;
                }
                else {
                    l.Add(e);
                }
            }

            return source._SelectGroupBy(expressions, someGroupingFound ? l.ToArray() : null);
        }

        static string namedKeyList(object o, MetaExpression[] expr) {
            string res = "";
            foreach (MetaExpression m in expr) {
                res += "," + m.apply(o);
            }
            return res;
            //return String.Join(",", (from e in expr select $":{e.apply(o)}").ToArray());
        }

        /// <summary>
        /// Evaluates a list of expression from a source collection
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expressions">List of expressions</param>
        /// <param name="groupBy">List of fields to be grouped</param>
        /// <returns></returns>
        private static object[] _SelectGroupBy(this object[] source,
             MetaExpression[] expressions,
            MetaExpression[] groupBy = null
            ) {
           
            int noName = 0;
            var result= new List<RowObject>();
            

            if (groupBy == null) {
                if (expressions == null || expressions.Length==0) {                    
                    return source;
                }
                var lookup2 = new Dictionary<string, int>();
                var len2 = expressions.Length;
                for (var i=0;i< len2; i++) {
                    var field = expressions[i].Alias;
                    if (field == null) {
                        noName += 1;
                        field = $"noName{noName}";
                    }
                    lookup2[field] = i;
                }
                foreach (var r in source) {
                    var arr = new object[len2];
                    for (var i = 0; i < len2; i++) {
                        arr[i] = expressions[i].apply(r);
                    }
                    result.Add(new RowObject(lookup2,arr));                    
                }
                return result.ToArray();
                
            }
            //int handler2 = metaprofiler.StartTimer("source.GroupBy");
            var groups = source.GroupBy(p => namedKeyList(p, groupBy));
            //metaprofiler.StopTimer(handler2);

            var lookup = new Dictionary<string, int>();
            var len = expressions.Length;
            for (var i = 0; i < len; i++) {
                var field = expressions[i].Alias;
                if (field == null) {
                    noName += 1;
                    field = $"noName{noName}";
                }
                lookup[field] = i;
            }

            foreach (var g in groups) {                
                var rowsFromGroup = g.ToArray();
                var arr = new object[len];
                for (var i = 0; i < len; i++) {
                    var m = expressions[i];
                    if (m.isGroupingFunction()) {
                        //int handler3 = metaprofiler.StartTimer("apply on "+m.Name);
                        arr[i] = m.apply(rowsFromGroup);
                        //metaprofiler.StopTimer(handler3);
                    }
                    else {
                        //int handler3 = metaprofiler.StartTimer("apply on " + m.Name);
                        arr[i] = m.apply(rowsFromGroup[0]);
                        //metaprofiler.StopTimer(handler3);
                    }
                }
                result.Add(new RowObject(lookup, arr));
            };
            return result.ToArray();

        }


    }

    /// <summary>
    /// Table reference, used for joining table. 
    /// </summary>
    /// <typeparam name="R"></typeparam>
    public class MetaTableRef <R> where R:MetaRow{
        /// <summary>
        /// Referenced table
        /// </summary>
        public MetaTableBase<R> T;
        /// <summary>
        /// Alias for the table in the join operation
        /// </summary>
        public string alias;

        /// <summary>
        /// Creates a table reference
        /// </summary>
        /// <param name="T"></param>
        /// <param name="alias"></param>
        public MetaTableRef(MetaTableBase<R> T,string alias) {
            this.T = T;
            this.alias = alias;
        }       
    }


    /// <summary>
    /// List of rows join-able to other tables
    /// </summary>
    /// <typeparam name="R"></typeparam>
    public class JoinedEnumerable<R> : IEnumerable<R> where R : MetaRow {
        internal JoinContext context = new JoinContext();


        /// <summary>
        /// Table to join
        /// </summary>
        protected IEnumerable<R> Source;

        /// <summary>
        /// Alias used in expression for relate to this collection
        /// </summary>
        public string aliasName;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">Reference to the table to join</param>
        public JoinedEnumerable(MetaTableRef<R> source) {
            this.Source = source.T.allCurrent();
            this.aliasName = source.alias;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">Reference to the table to join</param>
        /// <param name="_as">Alias for the source</param>
        public JoinedEnumerable(IEnumerable<R> source, string _as) {
            this.Source = source;
            this.aliasName = _as;
        }

        /// <summary>
        /// Create an alias for this collection
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public JoinedEnumerable<R> _as(string alias) {
            this.aliasName = alias;
            context.add("Item1", alias);
            return this;
        }

        /// <summary>
        /// Inner Join this collection to another enumeration, with a specified join condition
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="source2"></param>
        /// <param name="_as"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R, S> join<S>(IEnumerable<S> source2, string _as, MetaExpression on) where S : MetaRow {
            return new JoinedEnumerable<R, S>(this, source2, _as, on, true);
        }
        /// <summary>
        /// Inner Join this collection to another enumeration, with a specified join condition
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="source2"></param>
        /// <param name="_as"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R, S> leftJoin<S>(IEnumerable<S> source2, string _as, MetaExpression on) where S : MetaRow {
            return new JoinedEnumerable<R, S>(this, source2, _as, on, false);
        }

        /// <summary>
        /// Inner Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="source2"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R, S> join<S>(MetaTableRef<S> source2, MetaExpression on) where S : MetaRow {
            return new JoinedEnumerable<R, S>(this, source2, on, true);
        }

        /// <summary>
        /// Left Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="source2"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R, S> leftJoin<S>(MetaTableRef<S> source2, MetaExpression on) where S : MetaRow {
            return new JoinedEnumerable<R, S>(this, source2, on, false);
        }

        /// <summary>
        /// Necessary method to implement Ienumerable>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<R> GetEnumerator() {
            return Source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Source.GetEnumerator();
        }
    }

    /// <summary>
    /// List of pairs alias- fieldName used in a join operation
    /// </summary>
    public class JoinContext {
        /// <summary>
        /// Actual list of pairs
        /// </summary>
        public Dictionary<string, string> context = new Dictionary<string, string>();

        /// <summary>
        /// Add an alias-fieldName association
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="aliasName"></param>
        public void add(string fieldName, string aliasName) {
            context[aliasName] = fieldName;
        }

        /// <summary>
        /// Gets the field name related to an alias
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public string getFieldName(string alias) {
            if (context.ContainsKey(alias)) return context[alias];
            return alias;
        }

        /// <summary>
        /// Merge an external context to this
        /// </summary>
        /// <param name="c"></param>
        public void mergeContext(JoinContext c) {
            foreach (string s in c.context.Keys) {
                context[s] = c.context[s];
            }
        }

    }



    /// <summary>
    /// List of rows join-able to other tables
    /// </summary>
    /// <typeparam name="R1"></typeparam>
    /// <typeparam name="R2"></typeparam>
    public class JoinedEnumerable<R1, R2> : IEnumerable<Tuple<R1, R2>> where R1 : MetaRow where R2 : MetaRow {
        internal JoinContext context = new JoinContext();

        MetaExpression _condition;
        JoinedEnumerable<R1> _source1;
        IEnumerable<R2> _source2;
        bool _innerJoin;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <param name="_on"></param>
        /// <param name="innerJoin">when true, an inner join is made, else an outer join</param>
        public JoinedEnumerable(JoinedEnumerable<R1> source1, MetaTableRef<R2> source2,
                MetaExpression _on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2.T.allCurrent();
            this._innerJoin = innerJoin;
            this._condition = _on;

            context.mergeContext(source1.context);
            context.add("Item2", source2.alias);

        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <param name="_as">Alias for source2</param>
        /// <param name="_on"></param>
        /// <param name="innerJoin">when true, an inner join is made, else an outer join</param>
        public JoinedEnumerable(JoinedEnumerable<R1> source1, IEnumerable <R2> source2, string _as,
                MetaExpression _on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2;
            this._innerJoin = innerJoin;
            this._condition = _on;

            context.mergeContext(source1.context);
            context.add("Item2", _as);

        }

        /// <summary>
        /// Inner Join this collection to another list of rows
        /// </summary>
        /// <typeparam name="R3"></typeparam>
        /// <param name="source"></param>
        /// <param name="_as">alias for source</param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R1, R2, R3> join<R3>(IEnumerable<R3> source, string _as, MetaExpression on) where R3 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3>(this, source, _as, on, true);
        }

        /// <summary>
        /// Inner Join this collection to another list of rows
        /// </summary>
        /// <typeparam name="R3"></typeparam>
        /// <param name="source"></param>
        /// <param name="_as">alias for source</param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R1, R2, R3> leftJoin<R3>(IEnumerable<R3> source, string _as, MetaExpression on) where R3 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3>(this, source, _as, on, false);
        }

        /// <summary>
        /// Inner Join this collection to another list of rows
        /// </summary>
        /// <typeparam name="R3"></typeparam>
        /// <param name="source"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R1, R2,R3> join<R3>(MetaTableRef<R3> source, MetaExpression on) where R3 : MetaRow {
            return new JoinedEnumerable<R1,R2,R3>(this, source, on, true);
        }

        /// <summary>
        /// Outer Join this collection to another list of rows
        /// </summary>
        /// <typeparam name="R3"></typeparam>
        /// <param name="source"></param>
        /// <param name="on"></param>
        /// <returns></returns>
        public JoinedEnumerable<R1, R2, R3> leftJoin<R3>(MetaTableRef<R3> source, MetaExpression on) where R3 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3>(this, source, on, false);
        }

        /// <summary>
        /// Necessary method to implement base interfaces
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tuple<R1, R2>> GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (R1 R1 in _source1) {
                bool someFound = false;
                foreach (R2 R2 in _source2) {
                    Tuple<R1, R2> t = new Tuple<R1, R2>(R1, R2);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2>(R1, null);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (R1 R1 in _source1) {
                bool someFound = false;
                foreach (R2 R2 in _source2) {
                    Tuple<R1, R2> t = new Tuple<R1, R2>(R1, R2);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2>(R1, null);
                }
            }
        }
    }


    /// <summary>
    /// List of rows join-able to other tables
    /// </summary>
    /// <typeparam name="R1"></typeparam>
    /// <typeparam name="R2"></typeparam>
    /// <typeparam name="R3"></typeparam>
    public class JoinedEnumerable<R1, R2,R3> : IEnumerable<Tuple<R1, R2,R3>> 
        where R1 : MetaRow where R2 : MetaRow where R3:MetaRow{
        internal JoinContext context = new JoinContext();

        MetaExpression _condition;
        JoinedEnumerable<R1,R2> _source1;
        IEnumerable<R3> _source2;
        bool _innerJoin;

        /// <summary>
        /// Inner Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="R4"></typeparam>
        /// <param name="source"></param>
        /// <param name="_as">Alias for source</param>
        /// <param name="on"></param>
        /// <returns></returns>      
        public JoinedEnumerable<R1, R2, R3, R4> join<R4>(IEnumerable<R4> source, string _as, MetaExpression on) where R4 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3, R4>(this, source, _as, on, true);
        }

        /// <summary>
        /// Outer Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="R4"></typeparam>
        /// <param name="source"></param>
        /// <param name="_as">Alias for source</param>
        /// <param name="on"></param>
        /// <returns></returns>      
        public JoinedEnumerable<R1, R2, R3, R4> leftJoin<R4>(IEnumerable<R4> source, string _as, MetaExpression on) where R4 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3, R4>(this, source, _as, on, false);
        }

        /// <summary>
        /// Inner Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="R4"></typeparam>
        /// <param name="source4"></param>
        /// <param name="on"></param>
        /// <returns></returns>      
        public JoinedEnumerable<R1, R2, R3,R4> join<R4>(MetaTableRef<R4> source4, MetaExpression on) where R4 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3,R4>(this, source4, on, true);
        }

        /// <summary>
        /// Outer Join this collection to another table, with a specified join condition
        /// </summary>
        /// <typeparam name="R4"></typeparam>
        /// <param name="source4"></param>
        /// <param name="on"></param>
        /// <returns></returns>      
        public JoinedEnumerable<R1, R2, R3,R4> leftJoin<R4>(MetaTableRef<R4> source4, MetaExpression on) where R4 : MetaRow {
            return new JoinedEnumerable<R1, R2, R3, R4>(this, source4, on, false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <param name="on"></param>
        /// <param name="innerJoin"></param>
        public JoinedEnumerable(JoinedEnumerable<R1,R2> source1, MetaTableRef<R3> source2,
                MetaExpression on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2.T.allCurrent();
            this._innerJoin = innerJoin;
            this._condition = on;

            context.mergeContext(source1.context);
            context.add("Item3", source2.alias);

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <param name="_as">Alias for source2</param>
        /// <param name="on"></param>
        /// <param name="innerJoin"></param>
        public JoinedEnumerable(JoinedEnumerable<R1, R2> source1, IEnumerable<R3> source2, string _as,
                MetaExpression on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2;
            this._innerJoin = innerJoin;
            this._condition = on;

            context.mergeContext(source1.context);
            context.add("Item3", _as);

        }


        /// <summary>
        /// Necessary method to implement the interface
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tuple<R1, R2,R3>> GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (Tuple<R1, R2> r in _source1) {
                bool someFound = false;
                foreach (R3 r3 in _source2) {
                    Tuple<R1, R2,R3> t = new Tuple<R1, R2, R3>(r.Item1, r.Item2,r3);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2,R3>(r.Item1, r.Item2, null);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (Tuple<R1, R2> r in _source1) {
                bool someFound = false;
                foreach (R3 r3 in _source2) {
                    Tuple<R1, R2, R3> t = new Tuple<R1, R2, R3>(r.Item1, r.Item2, r3);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2, R3>(r.Item1, r.Item2, null);
                }
            }
        }
    }



    /// <summary>
    /// List of rows join-able to other tables
    /// </summary>
    /// <typeparam name="R1"></typeparam>
    /// <typeparam name="R2"></typeparam>
    /// <typeparam name="R3"></typeparam>
    /// <typeparam name="R4"></typeparam>
    public class JoinedEnumerable<R1, R2, R3,R4> : IEnumerable<Tuple<R1, R2, R3, R4>>
       where R1 : MetaRow where R2 : MetaRow where R3 : MetaRow where R4:MetaRow{
        internal JoinContext context = new JoinContext();

        MetaExpression _condition;
        JoinedEnumerable<R1, R2,R3> _source1;
        IEnumerable<R4> _source2;
        bool _innerJoin;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1">Left table</param>
        /// <param name="source2">Right table</param>
        /// <param name="_as">alias for right table</param>
        /// <param name="on">join condition</param>
        /// <param name="innerJoin"></param>
        public JoinedEnumerable(JoinedEnumerable<R1, R2, R3> source1, IEnumerable<R4> source2, string _as,
                MetaExpression on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2;
            this._innerJoin = innerJoin;
            this._condition = on;

            context.mergeContext(source1.context);
            context.add("Item4", _as);

        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source1">Left table</param>
        /// <param name="source2">Right table</param>
        /// <param name="on">join condition</param>
        /// <param name="innerJoin">when true, an inner join is made, else an outer join</param>
        /// <returns></returns>
        public JoinedEnumerable(JoinedEnumerable<R1, R2,R3> source1, MetaTableRef<R4> source2,
                MetaExpression on, bool innerJoin) {
            this._source1 = source1;
            this._source2 = source2.T.allCurrent();
            this._innerJoin = innerJoin;
            this._condition = on;

            context.mergeContext(source1.context);
            context.add("Item4", source2.alias);

        }

        /// <summary>
        /// Implements IEnumerator interface
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tuple<R1, R2, R3,R4>> GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (Tuple<R1, R2,R3> r in _source1) {
                bool someFound = false;
                foreach (R4 r4 in _source2) {
                    Tuple<R1, R2, R3,R4> t = new Tuple<R1, R2, R3,R4>(r.Item1, r.Item2, r.Item3,r4);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2, R3,R4>(r.Item1, r.Item2, r.Item3, null);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            _condition.applyContext(context.context);

            foreach (Tuple<R1, R2, R3> r in _source1) {
                bool someFound = false;
                foreach (R4 r4 in _source2) {
                    Tuple<R1, R2, R3, R4> t = new Tuple<R1, R2, R3, R4>(r.Item1, r.Item2, r.Item3, r4);

                    if (_condition.getBoolean(t)) {
                        someFound = true;
                        yield return t;
                    }
                }
                if (_innerJoin == false && someFound == false) {
                    yield return new Tuple<R1, R2, R3, R4>(r.Item1, r.Item2, r.Item3, null);
                }
            }
        }
    }


    /// <summary>
    /// Base type for generic DataTable containing MetaRows
    /// </summary>
    /// <typeparam name="R"></typeparam>
    [Serializable]
    public class MetaTableBase<R> : TypedTableBase<R>
        where R : MetaRow {

        /// <summary>
        /// Implements serializable
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MetaTableBase(SerializationInfo info, StreamingContext context):base(info,context) {

        }
        /// <summary>
        /// Creates a base for a join operations
        /// </summary>
        /// <param name="_as"></param>
        /// <returns></returns>
        public JoinedEnumerable<R> _joinAs(string _as) {
            return new JoinedEnumerable<R>(  new MetaTableRef<R>(this, _as));

        }

        /// <summary>
        /// Creates a reference to this table
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public MetaTableRef<R> _as(string alias) {
            return new MetaTableRef<R>(this, alias);
        }
        /// <summary>
        /// Row count (including deleted rows)
        /// </summary>
        public int Count { get { return this.Rows.Count; } }

        /// <summary>
        /// Creates a metatable with a given name
        /// </summary>
        /// <param name="tableName"></param>
        public MetaTableBase(string tableName) { TableName = tableName; }

        /// <summary>
        /// Get DataRow from collection
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public R this[int index] => (R)Rows[index];

        /// <summary>
        /// Get all rows of the table (including Deleted ones)
        /// </summary>
        /// <returns></returns>
        public List<R> all() {            
            return (from R r in Rows select r).ToList();
        }

        /// <summary>
        /// Get all current rows of the table
        /// </summary>
        /// <returns></returns>
        public List<R> allCurrent() {
            return (from R r in Rows where r.RowState!=DataRowState.Deleted select r).ToList();
        }

        static DataSetHelper.ConstructorDelegate myActivator = null;

        /// <summary>
        /// Required method to implement  TypedTableBase
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected override DataRow NewRowFromBuilder(global::System.Data.DataRowBuilder builder) {
            //var constructorTypeSignature = new Type[] { typeof(DataRowBuilder) };
            //var constructorParameters = new object[] { builder };
            //return (R) (typeof(R)).GetConstructor(constructorTypeSignature).Invoke(constructorParameters);

            if (myActivator == null) {
                myActivator = DataSetHelper.CreateConstructor(typeof(R), typeof(DataRowBuilder));
            }
            return (R)myActivator(builder);
            //return Activator.CreateInstance(typeof(R), new object[] { builder }) as DataRow;
        }

        /// <summary>
        /// Required method to implement  TypedTableBase, gives the specific  DataRow type of this table
        /// </summary>
        /// <returns></returns>
        protected override global::System.Type GetRowType() {
            return typeof(R);
        }

        /// <summary>
        /// Define all table DataColumns
        /// </summary>
        /// <param name="cols"></param>
        public virtual void addBaseColumns(params string[] cols) {
            if (cols.Length == 1 && cols[0] == "*") {
                foreach (string k in baseColumns.Keys) {
                    var c = baseColumns[k];
                    defineColumn(c.ColumnName, c.DataType, c.AllowDBNull, c.ReadOnly);
                }
                return;
            }
            foreach (string k in cols) {
                if (baseColumns.ContainsKey(k)) {
                    var C = baseColumns[k];
                    defineColumn(C.ColumnName, C.DataType, C.AllowDBNull, C.ReadOnly);
                }
            }
        }

        /// <summary>
        /// Add key to table, this has to be redefined in every derived table. This should
        /// </summary>
        public virtual void addBaseKey() {
        }

        /// <summary>
        /// Creates a DataColumn with specified informations
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="ColumnType"></param>
        /// <param name="allowDBNull"></param>
        /// <param name="ReadOnly"></param>
        /// <returns></returns>
        protected DataColumn createColumn(string ColumnName, Type ColumnType, bool allowDBNull = true, bool ReadOnly = false) {
            DataColumn C = new DataColumn(ColumnName, ColumnType);
            if (!allowDBNull) C.AllowDBNull = false;
            if (ReadOnly) C.ReadOnly = true;
            return C;
        }

        /// <summary>
        /// Defines a column. This should be called in derived MetaTable for each column required in a specific DataSet
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="ColumnType"></param>
        /// <param name="allowDBNull"></param>
        /// <param name="ReadOnly"></param>
        /// <returns></returns>
        public DataColumn defineColumn(string ColumnName, Type ColumnType, bool allowDBNull = true, bool ReadOnly = false) {
            if (Columns.Contains(ColumnName)) return Columns[ColumnName];
            DataColumn C = createColumn(ColumnName, ColumnType, allowDBNull, ReadOnly);
            Columns.Add(C);
            return C;
        }


        /// <summary>
        /// Dictionary containing all table columns. Usually defined in the metadata dataset
        /// </summary>
        public Dictionary<string, DataColumn> baseColumns = new Dictionary<string, DataColumn>();

        /// <summary>
        /// Defines the primary key of the table , used during the construction of DataSet for generic tables
        /// </summary>
        /// <param name="fields"></param>
        public void defineKey(params string[] fields) {
            if (fields == null || fields.Length == 0) {
                this.PrimaryKey = null;
                return;
            }
            this.PrimaryKey = (from string field in fields select this.Columns[field]).ToArray();
        }

        /// <summary>
        /// Gets the first row of a search, or null if no row is found
        /// </summary>
        /// <param name="filter">condition for the search</param>
        /// <param name="sort">row sorting</param>
        /// <param name="rv">DataViewRowState to be met</param>
        /// <returns></returns>
        public R First(string filter = null, string sort = null, DataViewRowState rv = DataViewRowState.CurrentRows) {
            DataRow[] r = Select(filter, sort, rv);
            if (r.Length == 0) return null;
            return (R)r[0];
        }

        /// <summary>
        ///  Gets the first row of a search, or null if no row is found
        /// </summary>
        /// <param name="filter">condition for the search</param>
        /// <param name="env">environment (use a DataAccess for this)</param>
        /// <param name="sort">row sorting</param>
        /// <returns></returns>
        public R First(MetaExpression filter, ISecurity env = null, string sort = null) {
            if (sort != null) {
                R[] found = Sort(sort);
                foreach (R r in found) {
                    if (r.RowState != DataRowState.Deleted && filter.getBoolean(r, env)) {
                        return r;
                    }
                }
            }
            foreach (R r in Rows) {
                if (r.RowState != DataRowState.Deleted && filter.getBoolean(r)) {
                    return r;
                }
            }
            return null;
        }

        /// <summary>
        /// List of table column names, comma separated
        /// </summary>
        /// <returns></returns>
        public string ColumnNameList() {
            return QueryCreator.RealColumnNameList(this);
        }
        /// <summary>
        /// get a sorted array of rows 
        /// </summary>
        /// <param name="sort">string sort order</param>
        /// <param name="rv">RowState to consider</param>
        /// <returns></returns>
        public R[] Sort(string sort, DataViewRowState rv = DataViewRowState.CurrentRows) {
            DataRow[] r = Select(null, sort, rv);
            return Array.ConvertAll(r, item => (R)item);
        }


        /// <summary>
        /// Search all rows that satisfies a criteria
        /// </summary>
        /// <param name="filter">criteria to be met</param>
        /// <param name="env">Environment (a DataAccess is a good parameter)</param>
        /// <param name="sort">sorting</param>
        /// <param name="all">if all=true also deleted rows are retrived</param>
        /// <returns></returns>
        public R[] Filter(MetaExpression filter, ISecurity env = null, string sort = null, bool all = false) {
            if (sort != null) {
                return (from R r in Sort(sort)
                        where compatibleState(r.RowState, all) && filter.getBoolean(r, env)
                        select r as R)
                        .ToArray();
            }

            if (filter is null) {
                return (from R r in Rows where compatibleState(r.RowState, all)  select r).ToArray();
            }

            if (all == false && DataSet!=null) {
	            var idm = DataSet.getIndexManager();
	            if (idm != null) {
		            var dict = (filter as IGetCompDictionary)?.getMcmpDictionary();
		            if (dict != null) return Array.ConvertAll( idm.getRows(this, dict), item=>(R)item);
	            }
            }

            return (from R r in Rows where compatibleState(r.RowState, all) && filter.getBoolean(r, env) select r).ToArray();

        }

        /// <summary>
        /// Search all rows that satisfies a criteria
        /// </summary>
        /// <param name="filter">criteria to be met</param>
        /// <param name="sort">sorting</param>
        /// <param name="rv">DataViewRowState to be met</param>
        /// <returns></returns>
        public R[] Filter(string filter = null, string sort = null, DataViewRowState rv = DataViewRowState.CurrentRows) {
            DataRow[] r = Select(filter, sort, rv);
            return Array.ConvertAll(r, item => (R)item);
        }

        /// <summary>
        ///  Get rows from DB without adding them to the table
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">Timeout in second, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public async Task<R[]> getDetachedRowsFromDb(IDataAccess Conn, MetaExpression filter, int timeout = -1) {
	        if (!(filter is null) && filter.isFalse())return new R[0];
	        string sFilter = filter?.toSql(Conn.GetQueryHelper(), Conn.Security);
	        return await getDetachedRowsFromDb(Conn, sFilter, timeout);
        }

        /// <summary>
        /// Get rows from DB without adding them to the table
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="filter">criteria to be met</param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> getDetachedRowsFromDb(IDataAccess Conn, string filter, int timeout = -1) {
            string sql = "SELECT " + ColumnNameList() + " from " + TableForReading;
            if (filter != null && filter != "") sql += " WHERE " + filter;
            return await detachedSqlRunFromDb(Conn, sql, timeout);
        }

        /// <summary>
        /// Get rows from DB executing a sql command without adding them to the table
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">Timeout in second, 0 means no timeout, -1 means default timeout</param>
        /// <returns></returns>
        public async Task<R[]> detachedSqlRunFromDb(IDataAccess Conn, string sql, int timeout = -1) {
            var t = Clone();
            try {
                await Conn.ExecuteQueryIntoTable(t, sql, timeout);
            }
            catch {
                return null;
            }
            return Array.ConvertAll(Select(), item => (R)item);
            
        }

        
        /// <summary>
        /// Get existing rows or read from DB when no rows is found
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public  async Task<R[]> get(IDataAccess Conn, MetaExpression filter, int timeout = -1) {
            if (!(filter is null) && filter.isFalse())return new R[0];
            R[] found = Filter(filter);
            if (found != null && found.Length > 0) return found;
            string sFilter = filter?.toSql(Conn.GetQueryHelper(), Conn.Security);
            return await getFromDb(Conn, sFilter, timeout);
        }

        /// <summary>
        /// Get rows from DB and merge them to the table. Use with caution, it throws exception if rows are already in table
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> getFromDb(IDataAccess conn, MetaExpression filter, int timeout = -1) {
            if (!(filter is null) && filter.isFalse())return new R[0];
            string sFilter = filter?.toSql(conn.GetQueryHelper(), conn.Security);
            return await getFromDb(conn, sFilter, timeout);
        }

        /// <summary>
        ///  Get rows from DB and add them to the table. Use with caution, it throws exception if rows are already in table
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
       public async Task<R[]> getFromDb(IDataAccess conn, string filter, int timeout = -1) {
            var res = await  getDetachedRowsFromDb(conn, filter, timeout);
            if (res == null) return null;
            foreach (var r in res) {
                Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        /// <summary>
        /// Get rows from DB executing a sql command and add them to the table.  Use with caution, it throws exception if rows are already in table
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> sqlRunFromDb(IDataAccess conn, string sql, int timeout = -1) {
            var res = await detachedSqlRunFromDb(conn, sql, timeout);
            if (res == null) return null;
            foreach (var r in res) {
                Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        /// <summary>
        /// Reads rows from db and merges them into the table, overwriting existing. Returns all read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> mergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1) {
            if (!(filter is null) && filter.isFalse())return new R[0];
            var res = await getDetachedRowsFromDb(conn, filter, timeout);
            if (res == null) return new R[] {};
            bool empty = Count == 0;
            foreach (var r in res) {
                if (!empty) {
                    DataRow found = First(MetaExpression.mCmp(r, PrimaryKey));
                    if (found!=null)   Rows.Remove(found);                                    
                }
                Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

     
        /// <summary>
        /// Reads rows from db and merges them into the table, overwriting existing. Returns all read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public virtual async Task<R []> readTableJoined(IDataAccess conn, string table2, 
            MetaExpression filterTable1, MetaExpression filterTable2, 
            params string[] columns) {
            string sql = conn.GetJoinSql(this, table2, filterTable1, filterTable2, columns);       
            return await sqlMergeFromDb(conn,sql);
        }

        /// <summary>
        /// Reads rows from db and merges them into the table, skipping existing. Returns all read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table2"></param>
        /// <param name="filterTable1"></param>
        /// <param name="filterTable2"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public virtual async Task<R []> safeReadTableJoined(IDataAccess conn, string table2, 
            MetaExpression filterTable1, MetaExpression filterTable2, 
            params string[] columns) {
            string sql = conn.GetJoinSql(this, table2, filterTable1, filterTable2, columns);       
            return await sqlSafeMergeFromDb(conn,sql);
        }


       /// <summary>
       /// 
       /// </summary>
       /// <param name="conn"></param>
       /// <param name="table2"></param>
       /// <param name="filterTable1"></param>
       /// <param name="filterTable2"></param>
       /// <param name="columns"></param>
       /// <returns></returns>
        public virtual async Task< R []> readDetachedJoin(IDataAccess conn, string table2, 
            MetaExpression filterTable1, MetaExpression filterTable2, 
            params string[] columns) {
            string sql = conn.GetJoinSql(this, table2, filterTable1, filterTable2, columns);            
            return await detachedSqlRunFromDb(conn,sql);
        }


        /// <summary>
        /// Reads rows from db and merges them into the table, overwriting existing. Returns all read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> sqlMergeFromDb( IDataAccess conn, string sql, int timeout = -1) {
            var res = await detachedSqlRunFromDb(conn, sql, timeout);
            if (res == null) return new R[] {};
            bool empty = Count == 0;
            foreach (var r in res) {
                if (!empty) {
                    DataRow found = First(MetaExpression.mCmp(r, PrimaryKey));
                    if (found!=null)   Rows.Remove(found);                
                }
                Rows.Add(r);
                r.AcceptChanges();
            }
            return res;
        }

        private static string hashColumns(R r,string []columns) {
            var keys = (from string field in columns.ToList() select r[field].ToString());
            return string.Join("§",keys);
        }

        /// <summary>
        /// Merges detached rows to the table
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="withCheck"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public  R[] mergeRows(R []rows, bool withCheck=true,bool overwrite=false) {
            var resList = new List<R>();
            if (rows == null) return new R[0];
            if (Rows.Count == 0) withCheck = false;

            if (withCheck ==false || (rows.Length <= 300 && Rows.Count <= 300)) {
                foreach (var r in rows) {
					if (withCheck) { //non va in eccezione su chiavi duplicate
						var found = this.First(MetaExpression.mCmp(r, PrimaryKey));
						if (found != null && overwrite == false) continue; //la salta se già c'è e non la restituisce
						if (found != null && overwrite) Rows.Remove(found);
					}
					if (r.RowState!=DataRowState.Detached) r.Table.Rows.Remove(r);
                    Rows.Add(r);
                    resList.Add(r);
                    r.AcceptChanges();
                }

                return resList.ToArray();
            }
            //se passa di qui allora withCheck=true

            var destRows = new Dictionary<string, DataRow>();
            var keys = (from DataColumn c in PrimaryKey select c.ColumnName).ToArray();
            foreach (R r in Rows) {
                destRows[hashColumns(r, keys)] = r;
            }
          
            foreach (var r in rows) {
                string hashSource = hashColumns(r, keys);
                if(destRows.TryGetValue(hashSource, out var destRow)) {
                    if(overwrite == false) continue; //la salta se già c'è e non la restituisce
                    Rows.Remove(destRow);
                }
                if (r.RowState!=DataRowState.Detached) r.Table.Rows.Remove(r);
                Rows.Add(r);
                r.AcceptChanges();
                resList.Add(r);
            }

            return resList.ToArray();
        }

        /// <summary>
        /// Reads rows from db and merges them into the table, skipping existing. Returns all new read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filter"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> safeMergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1) {
	        if (!(filter is null) && filter.isFalse())return new R[0];
	        var res = await getDetachedRowsFromDb(conn, filter, timeout);
	        if (res == null) return new R[] {};
	        return mergeRows(res, true);
        }

        /// <summary>
        /// Reads rows from db and merges them into the table, skipping existing. Returns all new read rows 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="timeout">timeout in seconds. 0 means no timeout, -1 means default</param>
        /// <returns></returns>
        public async Task<R[]> sqlSafeMergeFromDb( IDataAccess conn, string sql, int timeout = -1) {
            var res = await detachedSqlRunFromDb(conn, sql, timeout);
            if (res == null) return new R[] {};
            return mergeRows(res, true);
        }

        

        
    

       
        /// <summary>
        /// Creates a new typed Row 
        /// </summary>
        /// <returns></returns>
        public R newRow() {
            return (R) NewRow();
        }

        /// <summary>
        /// Creates a new Row copying all fields from sample (for each field in common)
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public R NewRowAs(DataRow sample) {
            var r = (R) NewRow();
            if (sample == null) return r;
            foreach(DataColumn c in Columns) {
                if (sample.Table.Columns.Contains(c.ColumnName)) r[c.ColumnName] = sample[c.ColumnName];
            }
            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt) {
            base.GetObjectData(info, ctxt);
            //info.AddValue("Make", this._make);
        }

        /// <summary>
        /// True if all or if r is not in detached or deleted state
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        internal static bool compatibleState(DataRowState r, bool all) {
            return all || (r == DataRowState.Added || r == DataRowState.Modified || r == DataRowState.Unchanged);
        }

        /// <summary>
        /// Static filter, always applied during read form db 
        /// </summary>
        public string StaticFilter
        {
            get
            {
                return ExtendedProperties["filter"]?.ToString();
            }
            set
            {
                ExtendedProperties["filter"] = value;
            }
        }

        /// <summary>
        /// Tabella del db da cui effettuare la lettura dei dati 
        /// </summary>
        public string TableForReading
        {
            get {
                return ExtendedProperties["TableForReading"]?.ToString() ?? TableName;
            }
            set
            {
                ExtendedProperties["TableForReading"] = value;
            }
        }

        /// <summary>
        /// Db tablename to use for writing data
        /// </summary>
        public string TableForPosting
        {
            get {
                return ExtendedProperties["TableForPosting"]?.ToString() ?? TableForReading;
            }
            set
            {
                ExtendedProperties["TableForPosting"] = value;
            }
        }

        /// <summary>
        /// Keeps the last selected row of a Table in an extended properties of the Table
        /// </summary>
        /// <param name="r"></param>
        public void setLastSelected(DataRow r) {
            ExtendedProperties["LastSelectedRow"] = r;
        }

        /// <summary>
        /// Get Last Selected Row in a specified DataTable
        /// </summary>
        /// <returns></returns>
        public R  getLastSelected() {
            var r = (R) ExtendedProperties["LastSelectedRow"];
            if (r == null) return null;
            if (r.RowState == DataRowState.Deleted) return null;
            if (r.RowState == DataRowState.Detached) return null;
            return r;
        }


        /// <summary>
        /// If true Skips this table in a deep copy
        /// </summary>
        public bool skipInsertCopy
        {
            get
            {
                if (ExtendedProperties["skipInsertCopy"] == null) return false;
                return (bool)ExtendedProperties["skipInsertCopy"];
            }
            set
            {
                ExtendedProperties["skipInsertCopy"] = value;
            }
        }

        private static IMetaModel staticModel = MetaFactory.factory.getSingleton<IMetaModel>();
        /// <summary>
		/// Tells whether this Table is a Sub-Entity of Parent Table.
		/// This is true if:
		/// Exists some relation R that links primary key of Parent to a subset of the 
		///  primary key of Child
		/// </summary>
		/// <param name="Parent"></param>
		/// <returns></returns>
		public bool IsSubEntityOf(DataTable Parent) {
	        return staticModel.IsSubEntity(child: this, parent:Parent);
        }

        /// <summary>
        /// Check if field is part of primary key
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool hasKey(string field) {
            return QueryCreator.IsPrimaryKey(this, field);
        }

        /// <summary>
        /// Ordinamento da applicare in fase di lettura da db e in visualizzazione
        /// </summary>
        public string Sorting
        {
            get
            {
                return ExtendedProperties["sort_by"] as string;
            }
            set
            {
                ExtendedProperties["sort_by"] = value;
            }
        }


    }

    public static class HashCreatorBuilder {
        static Dictionary<string,IHashCreator> existingFunctions= new Dictionary<string, IHashCreator>();

        public static void registerHashCreator(string className, IHashCreator hc) {
	        if (hc == null) return;
	        lock (existingFunctions) {
		        existingFunctions[className] = hc;

	        }
        }
        public static string getKeyHash(DataRow r) {
			if (r.Table.ExtendedProperties["hashPrimaryKey"] is IHashCreator hashCreator) return hashCreator.get(r);
			var fields = (from k in r.Table.PrimaryKey select k.ColumnName).ToArray();
	        hashCreator = getHashCreator(fields, true);
	        r.Table.ExtendedProperties["hashPrimaryKey"] = hashCreator;
	        return hashCreator.get(r);
        }
        public static HashSet<string> compilingHash = new HashSet<string>();
        
	    public static IHashCreator getHashCreator(string[] keys, bool toSort=true) {
		    string class_name = (keys.Length == 1)
			    ? "HashCreator_" + keys[0]
			    : (toSort ? "HashCreator_" + string.Join("_", (keys.OrderBy(x => x).ToArray())):
				    "HashCreator_" + string.Join("_",keys)
					);
		    class_name = class_name.Replace('!', '_');
			if (existingFunctions.TryGetValue(class_name, out var hc)) return hc;



			lock (compilingHash) {
	            if (compilingHash.Contains(class_name)) {
		            return null;
	            }
		        compilingHash.Add(class_name);
            }
		    
		    try {
			    lock (existingFunctions) {
				    hc = Compile(class_name, toSort ? (keys.OrderBy(x => x).ToArray()) : keys);
                    if (hc!=null) existingFunctions[class_name] = hc;
			    }
		    }
		    catch (Exception e) {
                ErrorLogger.Logger.MarkEvent($"Compiling {class_name} "+e.ToString());
		    }

		    
		    return hc;
	    }

	    /// <summary>
	    /// Compile an HashCreator
	    /// </summary>
	    /// <param name="newClassName"></param>
	    /// <param name="keys"></param>
	    public static IHashCreator oldCompile(string newClassName, string [] keys) {
		    Compiler c = new Compiler();
		    string classBody = $"public class {newClassName} : IHashCreator {{ \r\n" +
		                       getKeysDeclaration(keys) +
		                       getCgetDataRow(keys) +
		                       getCgetDataRowField(keys) +
		                       getCgetFromObject(keys) +
		                       getCgetFromDictionary(keys) +
		                       getCgetChild(keys) +
		                       getCgetParent(keys) +
		                       " } \r\n";
            string outBody = $"namespace metadatalibrary_aux {{\r\n {classBody} }} \r\n "; //chiude class e namespace

		    string []usingList = new[] { 
                "System",
			    "System.Data",
			    "System.Linq",
                "System.Collections.Generic",
                "mdl",
                "q = mdl.MetaExpression",
			    //,"Microsoft.CSharp.RuntimeBinder",
			    "System.Dynamic" };

		    var linkedDll = new [] {
                typeof(object).Assembly.Location,
                typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location,
                "System.Core.dll",
			    "System.Xml.Linq.dll",
			    "System.dll",
			    "System.Data.dll",
			    "System.Data.DataSetExtensions.dll",
			    "System.Xml.dll",
			    //,"metadatalibrary.dll"
			    //,"Microsoft.CSharp.dll"
		    };
		    //File.AppendAllText("codice_c.cs",classBody.ToString());
            
            //string register = $"HashCreatorBuilder.registerHashCreator(\"{newClassName}\",new {newClassName}());\r\n";
		    //File.AppendAllText("register.cs",register);
		    var ass = c.CompileAssembly(typeName:newClassName,
                                        funcBody:outBody, 
                                        referencedAssemblies: usingList, 
                                        referencedDll:linkedDll
                                        );
		    if (ass == null) {
                //ErrorLogger.Logger.logException($"Error compiling {newClassName} with Body: {outBody}");
			    return null;
		    }
		    return (IHashCreator) (ass.GetType("metadatalibrary_aux."+newClassName).GetConstructor(new Type[]{}).Invoke(new object[]{}));        
	    }

        /// <summary>
	    /// Compile an HashCreator
	    /// </summary>
	    /// <param name="newClassName"></param>
	    /// <param name="keys"></param>
	    public static IHashCreator Compile(string newClassName, string[] keys) {

            Compiler c = new Compiler();


            var linkedDll = new[] {
                typeof(object).Assembly.Location,
                //typeof(System.Collections.IList).Assembly.Location, is the same as before
                //typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location,
                "System.dll",
                "System.Data.Common.dll",
                "System.Core.dll",
                "System.Xml.Linq.dll",
                "System.Data.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Xml.dll",
                "System.Collections",
                "System.Collections.Generic"
                //typeof(MetaTable).Assembly.Location,
			    ,"mdl.dll"
			    //,"Microsoft.CSharp.dll"
		    };

          
         

          

            string classBody = $"public class {newClassName} : IHashCreator {{ \r\n" +
                               getKeysDeclaration(keys) +
                               getCgetDataRow(keys) +
                               getCgetDataRowField(keys) +
                               getCgetFromObject(keys) +
                               getCgetFromDictionary(keys) +
                               getCgetChild(keys) +
                               getCgetParent(keys) +
                               " } \r\n";
            string outBody = $"namespace mdl_aux {{\r\n {classBody} }} \r\n "; //chiude class e namespace

            string[] usingList = new[] {
                "System",
                "System.Data",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Collections",
                "System.Collections.Generic",
                "System.Dynamic",
                "mdl",
                "q = mdl.MetaExpression",
			    //,"Microsoft.CSharp.RuntimeBinder",
			    };

           
            //File.AppendAllText("codice_c.cs",classBody.ToString());

            // string register = $"HashCreatorBuilder.registerHashCreator(\"{newClassName}\",new {newClassName}());\r\n";

            //File.AppendAllText("register.cs",register);

            // see https://stackoverflow.com/questions/32769630/how-to-compile-a-c-sharp-file-with-roslyn-programmatically


            var ass = c.CompileAssembly(newClassName, outBody, usingList, linkedDll);
            if (ass == null) {
                //ErrorLogger.Logger.logException($"Error compiling {newClassName} with Body: {outBody}");
                return null;
            }
            return (IHashCreator)(ass.GetType("mdl_aux." + newClassName).GetConstructor(new Type[] { }).Invoke(new object[] { }));
        }


        public static string getKeysDeclaration(string[] keys) {
            // public string[] keys  get; private set; }= {"k1","k2",..};
            var s= new StringBuilder();
            s.Append("public string[] k   ={");
            s.Append(string.Join(",", (from string k in keys select "\"" + k + "\"")));
            s.AppendLine("};");
            s.AppendLine("public string []keys {get {return k;}}");
            return s.ToString();
	    }


	    static string getCgetDataRow(string []keys) {// get(DataRow r,DataRowVersion v=DataRowVersion.Default);
		    var s= new StringBuilder();
		    s.AppendLine("public string get(DataRow r,DataRowVersion v=DataRowVersion.Default) {");
		    s.AppendLine("  if (r.RowState == DataRowState.Deleted) v = DataRowVersion.Original;");
		    if (keys.Length == 1) {
			    s.AppendLine($"return r[\"{keys[0]}\",v].ToString();");
		    }
		    else {
			    var listaValues = string.Join(",",(from k in keys select $"r[\"{k}\",v].ToString()"));
			    s.AppendLine($"return string.Join(\"§\",{listaValues});");
		    }
		    s.AppendLine("}");
		    return s.ToString();
	    }

	    static string getCgetDataRowField(string []keys) {//get(DataRow r,string field, object proposedValue, DataRowVersion v=DataRowVersion.Default);
		    var s= new StringBuilder();
		    s.AppendLine("public string get(DataRow r,string field, object proposedValue, DataRowVersion v=DataRowVersion.Default) {");
		    s.AppendLine("  if (r.RowState == DataRowState.Deleted) v = DataRowVersion.Original;");
		    if (keys.Length == 1) {
			    s.AppendLine($"return proposedValue.ToString();");
		    }
		    else {
			    s.AppendLine($"string s = (field==\"{keys[0]}\")?proposedValue.ToString():r[\"{keys[0]}\",v].ToString();");
			    for (int i = 1; i < keys.Length;i++) {
				    s.AppendLine($"s += \"§\"+ ((field==\"{keys[i]}\")?proposedValue.ToString():r[\"{keys[i]}\",v].ToString());");
			    }

			    s.AppendLine("return s;");
		    }
		    s.AppendLine("}");
		    return s.ToString();
	    }


	    static string getCgetFromObject(string []keys) {//string getFromObject(object o);
		    var s= new StringBuilder();
		    s.AppendLine("public string getFromObject(object o) {");
		    if (keys.Length == 1) {
			    s.AppendLine($"return q.getField(\"{keys[0]}\",o).ToString();");
		    }
		    else {
			    var listaValues = string.Join(",",(from k in keys select $"q.getField(\"{k}\",o).ToString()"));
			    s.AppendLine($"return string.Join(\"§\",{listaValues});");
		    }
		    s.AppendLine("}");
		    return s.ToString();
	    }


	    static string getCgetFromDictionary(string []keys) {// string getFromDictionary(Dictionary<string,object>o);
		    // return string.Join("§", (from k in keys select o[k]));
		    var s= new StringBuilder();
		    s.AppendLine("public string getFromDictionary(Dictionary<string,object>o) {");
		    if (keys.Length == 1) {
			    s.AppendLine($"return (o[\"{keys[0]}\"]??\"\").ToString();");
		    }
		    else {
			    var listaValues = string.Join(",",(from k in keys select $"(o[\"{k}\"]??\"\").ToString()"));
			    s.AppendLine($"return string.Join(\"§\",{listaValues});");
		    }
		    s.AppendLine("}");
		    return s.ToString();
		    
		    
	    }
	    static string getCgetChild(string []keys) {// string getChild(DataRow rParent,DataRelation rel,DataRowVersion ver=DataRowVersion.Default);
		    var s= new StringBuilder();
		    s.AppendLine("public string getChild(DataRow rParent,DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {");
		    if (keys.Length == 1) {
			    s.AppendLine( $"return rParent[rel.ParentColumns[0].ColumnName,ver].ToString();");
		    }
		    else {
			    s.AppendLine(" var childVal= new Dictionary<string, object>();");
			    s.AppendLine("for (int i = 0; i < rel.ParentColumns.Length; i++) {");
			    s.AppendLine(
				    "  childVal[rel.ChildColumns[i].ColumnName] = rParent[rel.ParentColumns[i].ColumnName,ver];");
			    s.AppendLine("}");
			    var listaValues = string.Join(",",(from k in keys select $"childVal[\"{k}\"].ToString()"));
			    s.AppendLine($"return string.Join(\"§\",{listaValues});");
		    }

		    s.AppendLine("}");
		    return s.ToString();
	    }

	    static string getCgetParent(string []keys) { //string getParent(DataRow rChild,DataRelation rel,DataRowVersion ver=DataRowVersion.Default);
		    var s= new StringBuilder();
		    s.AppendLine("public string getParent(DataRow rChild,DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {");
		    if (keys.Length == 1) {
			    s.AppendLine( $"return rChild[rel.ChildColumns[0].ColumnName,ver].ToString();" );
		    }
		    else {
			    s.AppendLine(" var parentVal= new Dictionary<string, object>();");
			    s.AppendLine("for (int i = 0; i < rel.ChildColumns.Length; i++) {");
			    s.AppendLine(
				    "   parentVal[rel.ParentColumns[i].ColumnName] = rChild[rel.ChildColumns[i].ColumnName,ver];");
			    s.AppendLine("}");
			    var listaValues = string.Join(",",(from k in keys select $"parentVal[\"{k}\"].ToString()"));
			    s.AppendLine($"return string.Join(\"§\",{listaValues});");
		    }

		    s.AppendLine("}");
		    return s.ToString();
	    }
       
    }


    public interface IHashCreator {
        /// <summary>
        /// Name of fields
        /// </summary>
        string []keys { get; }

	    /// <summary>
	    /// Evaluates the hash of a row 
	    /// </summary>
	    /// <param name="r"></param>
	    /// <param name="v"></param>
	    /// <returns></returns>
	    string get(DataRow r,DataRowVersion v=DataRowVersion.Default);

	    /// <summary>
	    /// Evaluates the hash of a row 
	    /// </summary>
	    /// <param name="r"></param>
	    /// <param name="field"></param>
	    /// <param name="proposedValue"></param>
	    /// <param name="v"></param>
	    /// <returns></returns>
	    string get(DataRow r,string field, object proposedValue, DataRowVersion v=DataRowVersion.Default);

	    /// <summary>
	    /// Evaluates the hash of an object 
	    /// </summary>
	    /// <param name="o"></param>
	    /// <returns></returns>
	    string getFromObject(object o);

	    /// <summary>
	    /// Evaluates the hash of a dictionary
	    /// </summary>
	    /// <param name="o"></param>
	    /// <returns></returns>
	    string getFromDictionary(Dictionary<string,object>o);

	    /// <summary>
	    /// Gets the hash of child rows
	    /// </summary>
	    /// <param name="rParent">Parent row</param>
	    /// <param name="rel">DataRelation to consider</param>
	    /// <param name="ver">Version to consider in rParent to get values</param>
	    /// <returns></returns>
	    string getChild(DataRow rParent,DataRelation rel,DataRowVersion ver=DataRowVersion.Default);

	    /// <summary>
	    /// Gets the hash of parent row
	    /// </summary>
	    /// <param name="rChild">Parent row</param>
	    /// <param name="rel">DataRelation to consider</param>
	    /// <param name="ver">Version to consider in rChild to get values</param>
	    /// <returns></returns>
	    string getParent(DataRow rChild,DataRelation rel,DataRowVersion ver=DataRowVersion.Default);
    }

    public class HashCreator : IHashCreator {
	    public string[] keys { get; private set; }

	    public HashCreator(string[] keys,bool dontsort=false) {
		    if (dontsort) {
			    this.keys = keys.ToArray();
		    }
		    else {
			    this.keys =  keys.OrderBy(x=>x).ToArray();
		    }
		    
	    }

	    /// <summary>
	    /// Evaluates the hash of a row 
	    /// </summary>
	    /// <param name="r"></param>
	    /// <param name="v"></param>
	    /// <returns></returns>
	    public string get(DataRow r,DataRowVersion v=DataRowVersion.Default) {
		    if (r.RowState == DataRowState.Deleted) v = DataRowVersion.Original;
			//var s =  metaprofiler.StartTimer("HasCreator.get * " + r.Table.TableName );
		    var res= string.Join("§", (from k in keys select r[k, v].ToString()));
			//metaprofiler.StopTimer(s);
			return res;
	    }

        /// <summary>
        /// Evaluates the hash of a row 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="field"></param>
        /// <param name="proposedValue"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public string get(DataRow r,string field, object proposedValue, DataRowVersion v=DataRowVersion.Default) {
		    if (r.RowState == DataRowState.Deleted) v = DataRowVersion.Original;
		    //var s =  metaprofiler.StartTimer("HasCreator.get * " + r.Table.TableName );
		    var res= string.Join("§", (from k in keys select k==field? proposedValue.ToString():r[k, v].ToString()));
		    //metaprofiler.StopTimer(s);
		    return res;
	    }

	    /// <summary>
	    /// Evaluates the hash of an object 
	    /// </summary>
	    /// <param name="o"></param>
	    /// <returns></returns>
	    public string getFromObject(object o) {
		    return string.Join("§", (from k in keys select q.getField(k,o).ToString()));
	    }


	    /// <summary>
	    /// Evaluates the hash of a dictionary
	    /// </summary>
	    /// <param name="o"></param>
	    /// <returns></returns>
	    public string getFromDictionary(Dictionary<string,object>o) {
		    return string.Join("§", (from k in keys select o[k]??""));
	    }

	    /// <summary>
	    /// Gets the hash of child rows
	    /// </summary>
	    /// <param name="rParent">Parent row</param>
	    /// <param name="rel">DataRelation to consider</param>
	    /// <param name="ver">Version to consider in rParent to get values</param>
	    /// <returns></returns>
	    public string getChild(DataRow rParent,DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {
			var childVal= new Dictionary<string, object>();
			for (int i = 0; i < rel.ParentColumns.Length; i++) {
				childVal[rel.ChildColumns[i].ColumnName] = rParent[rel.ParentColumns[i].ColumnName,ver];
			}

			return string.Join("§", (from k in keys select childVal[k].ToString()));
	    }

	    /// <summary>
	    /// Gets the hash of parent row
	    /// </summary>
	    /// <param name="rChild">Parent row</param>
	    /// <param name="rel">DataRelation to consider</param>
	    /// <param name="ver">Version to consider in rChild to get values</param>
	    /// <returns></returns>
	    public string getParent(DataRow rChild,DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {
		    var parentVal= new Dictionary<string, object>();
		    for (int i = 0; i < rel.ChildColumns.Length; i++) {
			    parentVal[rel.ParentColumns[i].ColumnName] = rChild[rel.ChildColumns[i].ColumnName,ver];
		    }

		    return string.Join("§", (from k in keys select parentVal[k].ToString()));
	    }
    }
  


    public class MetaTableUniqueIndex:IMetaIndex {
	    private bool hasChanges = false;

	    private DataTable t;
	    private IHashCreator hashCreator;

	    public string tableName {
		    get { return t.TableName; }
	    }

	    public IHashCreator hash {
		    get { return hashCreator; }
	    }

	    public string owner { get;  set; }

	    public IMetaIndex clone(DataSet d) {
			return  new MetaTableUniqueIndex(d.Tables[t.TableName],hash.keys);
	    }

	    void innerOptimizeHashCreator() {
		    try {
			    if (hash != null) {
				    var creator=HashCreatorBuilder.getHashCreator(hash.keys, true);
				    if (creator != null) hashCreator = creator;
			    }
		    }
		    catch (Exception e) {
			    ErrorLogger.Logger.MarkEvent(e.ToString());
		    }
	    }

	    void optimizeHashCreator() {
		    if (Compiler.compileEnabled) {
			    new Task(() => {
				    innerOptimizeHashCreator() ;
			    }).Start();
		    }
		    else {
			    innerOptimizeHashCreator();
		    }

		    
	    }
		public Dictionary <string,DataRow> lookup = new Dictionary<string, DataRow>();
	    public MetaTableUniqueIndex(DataTable t, params string[] keys) {
		    this.t = t;
		    hashCreator = new HashCreator(keys);
		    optimizeHashCreator();

		    resume();
	    }

	   

	    /// <summary>
		/// Destroy this index
		/// </summary>
	    public void Dispose() {
		    removeSuspendedEvents();
            removeStandardEvents();
            lookup.Clear();
            hashCreator = null;
            t = null;
	    }
	  

	    private void TOnDisposed(object sender, EventArgs e) {
		    suspend();
	    }

	    private void TOnTableCleared(object sender, DataTableClearEventArgs e) {
		    lookup.Clear();
	    }
	    


	    private void TOnRowDeleting(object sender, DataRowChangeEventArgs e) {
		    lastChangingHash = hash.get(e.Row);
		    lastChangingRow = e.Row;
	    }

	    private void TOnRowDeleted(object sender, DataRowChangeEventArgs e) {
		    if (e.Row == lastChangingRow) lookup.Remove(lastChangingHash);
		    lastChangingRow = null;
	    }

	   

	    private string lastChangingHash = null;
	    private DataRow lastChangingRow = null;

	   

	    private void TOnColumnChanging(object sender, DataColumnChangeEventArgs e) {
		    if (e.Row.RowState == DataRowState.Detached) return;
		    if (!hashCreator.keys.Contains(e.Column.ColumnName)) return;
		    //var s =  metaprofiler.StartTimer("TOnColumnChanging.get(" + e.Row.Table.TableName + ")");
		    if (e.Row.RowState == DataRowState.Added) {
			    //sta cambiando il set di key, ma non scatterà il TOnRowChanged in questo caso
			    lookup.Remove(hash.get(e.Row));
			    lastChangingHash = hash.get(e.Row,e.Column.ColumnName,e.ProposedValue);
			    lookup[lastChangingHash] = e.Row;
			    //metaprofiler.StopTimer(s);
			    return;
		    }

		   
		    if (e.Row.RowState != DataRowState.Detached) {
			    if (lastChangingRow != e.Row) {
				    lastChangingHash = hash.get(e.Row);
				    lastChangingRow = e.Row;
			    }
		    } //le righe detached -> insert le aggiungiamo all'ultimo momento

            
		   

		    //metaprofiler.StopTimer(s);
	    }

	    private void TOnRowChanging(object sender, DataRowChangeEventArgs e) {
		    if (e.Row.RowState == DataRowState.Added) {
			    lastChangingRow = e.Row;
			    lastChangingHash = hash.get(e.Row);
			    return; //Action : Add,  o 
		    }
		    if (e.Row.RowState == DataRowState.Modified && e.Action==DataRowAction.Rollback) {
			    lastChangingRow = e.Row;
			    lastChangingHash = hash.get(e.Row,DataRowVersion.Current);
			    return; //Action : Add,  o 
		    }
		    //var s =  metaprofiler.StartTimer("TOnRowChanging.get * " + e.Row.Table.TableName);
		    if (lastChangingRow!=e.Row ) {  //In operazioni su righe prime dell'add è detached
			    lastChangingHash = hash.get(e.Row);
			    lastChangingRow = e.Row;
		    }
		    //metaprofiler.StopTimer(s);
	    }

	    void removeSuspendedEvents() {
		    t.RowChanged -= TOnRowChangedSuspended;
		    t.RowDeleted -= TOnRowDeletedSuspended;
		    t.TableCleared -= TOnTableClearedSuspended;
		    t.ColumnChanging -= TOnColumnChangingSuspended;
	    }

	    private void TOnRowChangedSuspended(object sender, DataRowChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnRowDeletedSuspended(object sender, DataRowChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnColumnChangingSuspended(object sender, DataColumnChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnTableClearedSuspended(object sender, DataTableClearEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }

	    private void TOnRowChanged(object sender, DataRowChangeEventArgs e) {
		    //var s =  metaprofiler.StartTimer("TOnRowChanged.get * " + e.Row.Table.TableName);
		    if (e.Row == lastChangingRow) {
			    if (e.Row.RowState == DataRowState.Detached) {//qui la riga aggiunta al datatable è già added e non più detached
				    lookup.Remove(lastChangingHash);
				    lastChangingRow = null;
				    //metaprofiler.StopTimer(s);
				    return;
			    }

			    var newHash = hash.get(e.Row);
			    if (newHash != lastChangingHash) {
				    lookup.Remove(lastChangingHash);
			    }

			    if (e.Row.RowState == DataRowState.Unchanged) {
				    lookup[newHash] = e.Row; //Siamo un merge oppure in un acceptchanges
				    lastChangingRow = null;
				    //metaprofiler.StopTimer(s);
				    return;
			    }

                if (e.Row.RowState == DataRowState.Added) {
                    lookup[newHash]= e.Row; //alla prima aggiunta potrebbe essere l'unica occasione
                    lastChangingRow = null;
                    //metaprofiler.StopTimer(s);
                    return;
                }

                if (newHash != lastChangingHash) {
				    lookup[newHash] = e.Row;
				    lastChangingRow = null;
				    //metaprofiler.StopTimer(s);
				    return;
			    }
		    }
		    lastChangingRow = null;
		    //metaprofiler.StopTimer(s);
	    }

	   


	    /// <summary>
		/// Recalc index
		/// </summary>
	    public void reindex() {
			lookup.Clear();
			foreach (DataRow r in t.Rows) lookup[hashCreator.get(r)] = r;
	    }

		/// <summary>
		/// Get rows linked to given hash
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		public DataRow[] getRows(string hash) {
			if (lookup.TryGetValue(hash, out DataRow r)) {
				return new DataRow[] {r};
			}

			return new DataRow[] { };
		}

		/// <summary>
		/// Get row linked to given hash, take first one if more than one is found
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		public DataRow getRow(string hash) {
			if (lookup.TryGetValue(hash, out DataRow r)) {
				return r;
			}

			return null;
		}

		void removeStandardEvents() {
			if (t == null) return;
			t.RowChanging -= TOnRowChanging;
			t.RowChanged -= TOnRowChanged;
			t.RowDeleting -=TOnRowDeleting;
			t.RowDeleted -= TOnRowDeleted;
			t.TableCleared -= TOnTableCleared;
			t.ColumnChanging -= TOnColumnChanging;
		}
        /// <summary>
        /// Suspend indexing
        /// </summary>
        /// <param name="keepIndexes"></param>
        public void suspend(bool keepIndexes=false) { 
	        if (!keepIndexes) lookup.Clear();
	        if (t == null) return;
	        removeStandardEvents();
	        hasChanges = false;

	        t.RowChanged += TOnRowChangedSuspended;
	        t.RowDeleted += TOnRowDeletedSuspended;
	        t.TableCleared += TOnTableClearedSuspended;
	        t.ColumnChanging += TOnColumnChangingSuspended;

	        t.Disposed -= TOnDisposed;

        }

        public void resume(bool keepIndexes=false) {
	        if (!hasChanges) {
		        removeSuspendedEvents();
	        }

	        
	        t.RowChanging += TOnRowChanging;
	        t.RowChanged += TOnRowChanged;
	        t.RowDeleting +=TOnRowDeleting;
	        t.RowDeleted += TOnRowDeleted;
	        t.TableCleared += TOnTableCleared;
	        t.Disposed += TOnDisposed;
	        t.ColumnChanging += TOnColumnChanging;

	        if (hasChanges || !keepIndexes) reindex();
        }
    }

    public class MetaTableNotUniqueIndex:IMetaIndex {
	    
	    private DataTable t;
	    private IHashCreator hashCreator;

	    public IHashCreator hash {
		    get { return hashCreator; }
	    }

	    public string tableName {
		    get { return t.TableName; }
	    }

		public string owner { get;  set; }

	    public Dictionary <string,HashSet<DataRow>> lookup = new Dictionary <string,HashSet<DataRow>>();

	    public IMetaIndex clone(DataSet d) {
		    return  new MetaTableNotUniqueIndex(d.Tables[t.TableName],hash.keys);
	    }
	    
	    void innerOptimizeHashCreator() {
		    try {
			    if (hash != null) {
				    var creator=HashCreatorBuilder.getHashCreator(hash.keys, true);
				    if (creator != null) hashCreator = creator;
			    }
		    }
		    catch (Exception e) {
			    ErrorLogger.Logger.MarkEvent(e.ToString());
		    }
	    }


	    void optimizeHashCreator() {
		    if (Compiler.compileEnabled) {
			    new Task(() => {
				    innerOptimizeHashCreator() ;
			    }).Start();
		    }
		    else {
			    innerOptimizeHashCreator();
		    }
	    }

	    public MetaTableNotUniqueIndex(DataTable t, params string[] keys) {
		    this.t = t;
		    hashCreator = new HashCreator(keys);
		    optimizeHashCreator();

		    resume();
	    }
		
	    private string lastChangingHash = null;
	    private DataRow lastChangingRow = null;

		/// <summary>
		/// Destroy this index
		/// </summary>
	    public void Dispose() {
			removeStandardEvents();
            removeStandardEvents();
            lookup.Clear();
            hashCreator = null;
            t = null;
		}

	    private void TOnDisposed(object sender, EventArgs e) {
		    lookup.Clear();
	    }

	    private void TOnColumnChanging(object sender, DataColumnChangeEventArgs e) {
		    if (e.Row.RowState == DataRowState.Detached) return;
		    if (!hashCreator.keys.Contains(e.Column.ColumnName)) return;

		    if (e.Row.RowState == DataRowState.Added) {
			    //sta cambiando il set di key, ma non scatterà il TOnRowChanged in questo caso
			    remove(hash.get(e.Row), e.Row);
			    lastChangingHash = hash.get(e.Row,e.Column.ColumnName,e.ProposedValue);
			    add(lastChangingHash, e.Row);
			    return;
		    }

		    if (e.Row.RowState != DataRowState.Detached) {
			    if (lastChangingRow != e.Row) {
				    lastChangingHash = hash.get(e.Row);
				    lastChangingRow = e.Row;
				    return;
			    }
		    }

		  

	    }

	    private void TOnTableCleared(object sender, DataTableClearEventArgs e) {
		    lookup.Clear();
	    }
	    private void TOnRowChanging(object sender, DataRowChangeEventArgs e) {
		    if (e.Row.RowState == DataRowState.Added) {
			    lastChangingHash = hash.get(e.Row);
			    lastChangingRow = e.Row;
			    return; //Action : Add,  o 
		    }
		    if (e.Row.RowState == DataRowState.Modified && e.Action==DataRowAction.Rollback) {
			    lastChangingRow = e.Row;
			    lastChangingHash = hash.get(e.Row,DataRowVersion.Current);
			    return; //Action : Add,  o 
		    }
		    if (lastChangingRow!=e.Row) {
			    lastChangingHash = hash.get(e.Row);
			    lastChangingRow = e.Row;
			    return;
		    }
	    }

	    private void TOnRowDeleting(object sender, DataRowChangeEventArgs e) {
		    lastChangingHash = hash.get(e.Row);
		    lastChangingRow = e.Row;
	    }

	    private void TOnRowDeleted(object sender, DataRowChangeEventArgs e) {
		    if (e.Row == lastChangingRow) remove(lastChangingHash,lastChangingRow);
		    lastChangingRow = null;
	    }

	    private void TOnRowChanged(object sender, DataRowChangeEventArgs e) {
		    if (e.Row == lastChangingRow) {
			    if (e.Row.RowState == DataRowState.Detached) {
				    remove(lastChangingHash, e.Row);
				    lastChangingRow = null;
				    return;
			    }
			    var newHash = hash.get(e.Row);
			    
			    if (newHash != lastChangingHash) {
				    remove(lastChangingHash, e.Row);
			    }


			    if (e.Row.RowState == DataRowState.Unchanged) {
				    add(newHash,e.Row);//Siamo un merge oppure in un acceptchanges
				    lastChangingRow = null;
				    return;
			    }
			    if (e.Row.RowState == DataRowState.Added) {
				    add(newHash, e.Row); //alla prima aggiunta potrebbe essere l'unica occasione
				    lastChangingRow = null;
				    return;
			    }

			    if (newHash != lastChangingHash) {
				    add(newHash, e.Row);
				    lastChangingRow = null;
				    return;
			    }
			    

		    }

		    lastChangingRow = null;
	    }


	    void remove(string hash, DataRow r) {
		    if (lookup.TryGetValue(hash, out HashSet<DataRow> l)) {
			    l.Remove(r);
		    }
	    }

	    void add(string hash, DataRow r) {
		    if (lookup.TryGetValue(hash, out HashSet<DataRow> l)) {
			    l.Add(r);
		    }
		    else {
				lookup[hash]= new HashSet<DataRow>(){r};
		    }
	    }

	    /// <summary>
	    /// Recalc index
	    /// </summary>
	    public void reindex() {
		    lookup.Clear();
		    foreach (DataRow r in t.Rows) {
				var h = hashCreator.get(r);
				if (!lookup.TryGetValue(h, out var l)) {
				    l= new HashSet<DataRow>();
				    lookup[h] = l;
			    } 
			    l.Add(r);
		    }
	    }

	    /// <summary>
	    /// Get rows linked to given hash
	    /// </summary>
	    /// <param name="hash"></param>
	    /// <returns></returns>
	    public DataRow[] getRows(string hash) {
		    if (lookup.TryGetValue(hash, out HashSet<DataRow> l)) {
			    return l.ToArray();
		    }

		    return new DataRow[] { };
	    }

	    /// <summary>
	    /// Get row linked to given hash, take first one if more than one is found
	    /// </summary>
	    /// <param name="hash"></param>
	    /// <returns></returns>
	    public DataRow getRow(string hash) {
		    if (lookup.TryGetValue(hash, out HashSet<DataRow> l)) {
			    if (l.Count > 0) return l.First();
			    return null;
		    }

		    return null;
	    }

        
	    void removeStandardEvents() {
		    if (t == null) return;
		    t.RowChanging -= TOnRowChanging;
		    t.RowChanged -= TOnRowChanged;
		    t.RowDeleting -=TOnRowDeleting;
		    t.RowDeleted -= TOnRowDeleted;
		    t.TableCleared -= TOnTableCleared;
		    t.ColumnChanging -= TOnColumnChanging;
	    }

	    public void suspend(bool keepIndexes=false) { 
		    if (!keepIndexes) lookup.Clear();
		    if (t == null) return;
		    removeStandardEvents();

		    hasChanges = false;

		    t.RowChanged += TOnRowChangedSuspended;
		    t.RowDeleted += TOnRowDeletedSuspended;
		    t.TableCleared += TOnTableClearedSuspended;
		    t.ColumnChanging += TOnColumnChangingSuspended;

	    }

	    void removeSuspendedEvents() {
		    t.RowChanged -= TOnRowChangedSuspended;
		    t.RowDeleted -= TOnRowDeletedSuspended;
		    t.TableCleared -= TOnTableClearedSuspended;
		    t.ColumnChanging -= TOnColumnChangingSuspended;
	    }

	    private void TOnRowChangedSuspended(object sender, DataRowChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnRowDeletedSuspended(object sender, DataRowChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnColumnChangingSuspended(object sender, DataColumnChangeEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }
	    private void TOnTableClearedSuspended(object sender, DataTableClearEventArgs e) {
		    hasChanges = true;
		    removeSuspendedEvents();
	    }

	    private bool hasChanges = false;

	    public void resume(bool keepIndexes=false) {

		    if (!hasChanges) removeSuspendedEvents();
	        
		    t.RowChanging += TOnRowChanging;
		    t.RowChanged += TOnRowChanged;
		    t.RowDeleting +=TOnRowDeleting;
		    t.RowDeleted += TOnRowDeleted;
		    t.TableCleared += TOnTableCleared;
		    t.Disposed += TOnDisposed;
		    t.ColumnChanging += TOnColumnChanging;

		    if (hasChanges || !keepIndexes) reindex();
	    }


    }



    public interface IMetaIndex: IDisposable {
	    /// <summary>
		/// Owner of this index
		/// </summary>
	    string owner { get; set; }
	    
		string tableName { get; }

	    /// <summary>
	    /// Recalc index or resume indexing
	    /// </summary>
	    void reindex();

        /// <summary>
        /// Suspend index evaluation
        /// </summary>
	    void suspend(bool keepIndexes=false);

        /// <summary>
        /// resume index evaluation
        /// </summary>
        void resume(bool keepIndexes=false);

		IHashCreator hash { get;  }
	    /// <summary>
	    /// Get rows linked to given hash
	    /// </summary>
	    /// <param name="hash"></param>
	    /// <returns></returns>
	    DataRow[] getRows(string hash);

	    /// <summary>
	    /// Get row linked to given hash, take first one if more than one is found
	    /// </summary>
	    /// <param name="hash"></param>
	    /// <returns></returns>
	    DataRow getRow(string hash);

		/// <summary>
		/// Create a copy of this index applied to another dataset
		/// </summary>
		/// <returns></returns>
	    IMetaIndex clone(DataSet d);
    }

    public interface IIndexManager : IDisposable {
	    IEnumerable<IMetaIndex> getIndexes();

	    DataSet linkedDataSet();

	    relHasher getParentHasher(DataRelation rel);

		bool autoIndex { get; set; }
		/// <summary>
		/// Check if an index exists on a table with specified columns
		/// </summary>
		/// <param name="t"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
	    bool hasIndex(DataTable t, params string[] keys);

		/// <summary>
		/// Gets an index on a table  with specified columns
		/// </summary>
		/// <param name="t"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		IMetaIndex getIndex(DataTable t, params string[] keys);

		/// <summary>
		/// Create index on primarykey
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		IMetaIndex createPrimaryKeyIndex(DataTable t);

		/// <summary>
		/// Create primary key index on all tables
		/// </summary>
		void createPrimaryKeysIndexes();

		/// <summary>
		/// Create an index if it does not already exists, and returns the newly created or existing index
		/// </summary>
		/// <param name="t"></param>
		/// <param name="keys"></param>
		/// <param name="unique"></param>
		/// <returns></returns>
		IMetaIndex checkCreateIndex(DataTable t, string[] keys, bool unique);

		/// <summary>
		/// Gets an index on a table  with specified columns
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		IMetaIndex getPrimaryKeyIndex(DataTable t);

        /// <summary>
        /// Suspend indexing of T
        /// </summary>
        /// <param name="t"></param>
        /// <param name="keepIndexes"></param>
        void suspend(bool keepIndexes, params DataTable []t);

        /// <summary>
        /// Resume indexing of t
        /// </summary>
        /// <param name="t"></param>
        void resume(bool keepIndexes,params DataTable []t);

		/// <summary>
		/// Adds an index on a table 
		/// </summary>
		/// <param name="t"></param>
		/// <param name="index"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		void addIndex(DataTable t, IMetaIndex index, string owner=null);

		/// <summary>
		/// Dispose all index of an owner
		/// </summary>
		/// <param name="owner"></param>
		void Dispose(string owner);

		DataRow[] getChildRows(DataRow r, DataRelation rel);
		DataRow[] getParentRows(DataRow r, DataRelation rel);
		DataRow getChildRow(DataRow r, DataRelation rel);
		DataRow getParentRow(DataRow r, DataRelation rel);

		DataRow []getRows(DataTable t, Dictionary<string, object> keyValues);
		DataRow getRow(DataTable t, Dictionary<string, object> keyValues);

		DataRow []getRows(DataTable t, object sample, params string []fields);
		DataRow getRow(DataTable t, object sample, params string []fields);

		
    }

    public class IndexManager : IIndexManager {
	    public bool autoIndex { get; set; }

	    public DataSet linkedDataSet() {
		    return d;
	    }
        
		/// <summary>
		/// Managed dataset
		/// </summary>
		public DataSet d { get; private set; }

		public IndexManager(DataSet d) {
			this.d = d;
			d.setIndexManager(this);
		}

		public IEnumerable<IMetaIndex> getIndexes() {
			foreach (var i in indexes.Values) {
				yield return i;
			}
		}
		

		public void CopyIndexFrom(DataSet source) {
			var other = source.getIndexManager();
			if (other == null) return;
			foreach (var i in other.getIndexes()) {
				var table = d.Tables[i.tableName];
				var _ = hash(table, i.hash.keys);
				addIndex(table,i.clone(d),owner);
			}


		}

	    private string owner = Guid.NewGuid().ToString();
	    private Dictionary<string, IMetaIndex> indexes = new Dictionary<string, IMetaIndex>();

	    string hash(DataTable t, string[] keys) {
		    return t.TableName+"§"+string.Join("§",keys.OrderBy(x=>x).ToArray());
	    }

	    /// <summary>
	    /// Dispose all index of an owner
	    /// </summary>
	    /// <param name="owner"></param>
	    public void Dispose(string owner) {
		    var keys = indexes.Keys.ToArray();
		    foreach (var k in keys) {
			    if (indexes[k].owner == owner) {
					indexes[k].Dispose();
					indexes.Remove(k);
			    }
		    }
	    }

	    public IMetaIndex checkCreateIndex(DataTable t, string[] keys, bool unique) {
		    if (t.DataSet != d) return null;
		    if (indexes.TryGetValue(hash(t,keys), out var index)) return index;
			return createIndex(t,keys,unique);
	    }

	    IMetaIndex createIndex(DataTable t, string[] keys, bool unique) {
		    if (t.DataSet != d) return null;
		    foreach (string f in keys) {
			    if (!t.Columns.Contains(f)) {
				    ErrorLogger.Logger.logException($"Campo {f} non trovato in tabella {t.TableName} creando indice unique[{unique}]");
			    }
		    }
		    if (indexes.TryGetValue(hash(t,keys), out var index)) return index;
		    IMetaIndex i;
		    if (unique) {
			    i = new MetaTableUniqueIndex(t, keys);
		    }
		    else {
			    i = new MetaTableNotUniqueIndex(t, keys);
		    }
		    addIndex(t,i, owner);
		    return i;
	    }

	    public IMetaIndex createPrimaryKeyIndex(DataTable t) {
		    if (t.PrimaryKey.Length == 0) return null;
		    var keys = (from c in t.PrimaryKey select c.ColumnName).ToArray();
		    return createIndex(t, keys, true);

	    }

	    public void createPrimaryKeysIndexes() {
		    foreach (DataTable t in d.Tables) {
			    createPrimaryKeyIndex(t);
		    }
	    }

	   

	    /// <summary>
	    /// Gets an index on a table  with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <returns></returns>
	    public IMetaIndex getPrimaryKeyIndex(DataTable t) {
		    if (t.DataSet != d) return null;
		    var keys = (from c in t.PrimaryKey select c.ColumnName).ToArray();
		    if (!indexes.TryGetValue(hash(t, keys), out var index)) return null;
		    return index;
	    }

	    /// <summary>
	    /// Check if an index exists on a table with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="keys"></param>
	    /// <returns></returns>
	    public bool hasIndex(DataTable t, params string[] keys) {
		    if (t.DataSet != d) return false;
		    return indexes.ContainsKey(hash(t,keys));
	    }

	    public void Dispose() {
			foreach(var i in indexes.Values)i.Dispose();
			indexes.Clear();
			d = null;
	    }

	    /// <summary>
	    /// Gets an index on a table  with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="keys"></param>
	    /// <returns></returns>
	    public IMetaIndex getIndex(DataTable t, params string[] keys) {
		    if (t.DataSet != d) return null;
		    if (!indexes.TryGetValue(hash(t, keys), out var index)) return null;
		    return index;
	    }

	    /// <summary>
	    /// Adds an index on a table 
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="index"></param>
	    /// <param name="owner"></param>
	    /// <returns></returns>
	    public void addIndex(DataTable t, IMetaIndex index,string owner=null) {
		    if (t.DataSet != d) throw new Exception("Bad Index Addition (wrong dataset)") ;
		    foreach (string f in index.hash.keys) {
                if (!t.Columns.Contains(f))ErrorLogger.Logger.logException(
	                $"Campo {f} non esistente in tabella {t.TableName} nella creazione di un indice");
		    }
		    index.owner = owner??this.owner;
			indexes.Add(hash(t, index.hash.keys),index);
	    }

	    public DataRow [] getRows(DataTable t, object sample, params string[] fields) {
		    var index = getIndex(t, fields);
		    if (index == null && autoIndex) index = checkCreateIndex(t, fields, false);
		    if (index == null) {
			    //return t._Filter(q.mCmp(sample, fields));
			    if (index == null) {
				    var filter = q.mCmp(sample, fields);
				    return (from DataRow  r in t.Rows where MetaTable.compatibleState(r.RowState, false) && filter.getBoolean(r) select r).ToArray();
				    //t._Filter(q.mCmp(keyValues));
			    }
		    }
		    return index.getRows(index.hash.getFromObject(sample));
	    }

	    public DataRow getRow(DataTable t, object sample, params string[] fields) {
		    var index = getIndex(t, fields);
		    if (index == null && autoIndex) index = checkCreateIndex(t, fields, true);
		    if (index == null) {
			    var filter = q.mCmp(sample, fields);
			    return (from DataRow  r in t.Rows where MetaTable.compatibleState(r.RowState, false) && filter.getBoolean(r) select r).FirstOrDefault();
		    }
		    return index.getRow(index.hash.getFromObject(sample));
	    }

	    public DataRow[] getRows(DataTable t, Dictionary<string, object> keyValues) {
		    var fields = keyValues.Keys.ToArray();
		    var index = getIndex(t, fields);
		    if (index == null && autoIndex) index = checkCreateIndex(t, fields, false);
		    if (index == null) {
			    var filter = q.mCmp(keyValues);
			    return (from DataRow  r in t.Rows where MetaTable.compatibleState(r.RowState, false) && filter.getBoolean(r) select r).ToArray();
			    //t._Filter(q.mCmp(keyValues));
		    }
		    return index.getRows(index.hash.getFromDictionary(keyValues));
	    }
	    public DataRow getRow(DataTable t, Dictionary<string, object> keyValues) {
		    var fields = keyValues.Keys.ToArray();
		    var index = getIndex(t, fields);
		    if (index == null && autoIndex) index = checkCreateIndex(t, fields, true);
		    if (index == null) {
			    var filter = q.mCmp(keyValues);
			    return (from DataRow  r in t.Rows where MetaTable.compatibleState(r.RowState, false) && filter.getBoolean(r) select r).FirstOrDefault();
		    }
		    return index.getRow(index.hash.getFromDictionary(keyValues));
	    }

	    public class relHasher {
		    public bool noIndex;
		    public IMetaIndex idx;
		    public IHashCreator hash;

		    public DataRow[] getRows(DataRow r) {
			    return idx.getRows(hash.get(r));
		    }
		    public DataRow getRow(DataRow r) {
			    return idx.getRow(hash.get(r));
		    }
	    }
        Dictionary<string, relHasher> childHasher = new Dictionary<string,relHasher>();
        Dictionary<string, relHasher> parentHasher = new Dictionary<string,relHasher>();

        relHasher createChildHasher(IMetaIndex index, DataRelation rel) {
            //deve mappare le colonne parent in base all'ordine sull'indice  del child column
            var keys = index.hash.keys;
            int nCol = keys.Length;
            var parentCols = new string[nCol];
            for (int i = 0; i < nCol; i++) {
	            for (int j = 0; j < nCol; j++) {
		            if (keys[i] == rel.ChildColumns[j].ColumnName) parentCols[i] = rel.ParentColumns[j].ColumnName;
	            }
            }
            var result= new relHasher() {
	            noIndex = false,
	            idx = index,
	            hash = new HashCreator(parentCols,true)//HashCreatorBuilder.getHashCreator(parentCols,false)
            };
            if (Compiler.compileEnabled) {
	            new Task(() => { result.hash = HashCreatorBuilder.getHashCreator(parentCols, false) ?? result.hash; })
		            .Start();
            }
            else {
	            result.hash = HashCreatorBuilder.getHashCreator(parentCols, false) ?? result.hash;
            }

            return result;
        }
        relHasher createParentHasher(IMetaIndex index, DataRelation rel) {
	        //deve mappare le colonne parent in base all'ordine sull'indice  del parent column
	        var keys = index.hash.keys;
	        int nCol = keys.Length;
	        var childCols = new string[nCol];
	        for (int i = 0; i < nCol; i++) {
		        for (int j = 0; j < nCol; j++) {
			        if (keys[i] == rel.ParentColumns[j].ColumnName) childCols[i] = rel.ChildColumns[j].ColumnName;
		        }
	        }
	        var result= new relHasher() {
		        noIndex = false,
		        idx = index,
		        hash = new HashCreator(childCols,true)
	        };
	        if (Compiler.compileEnabled) {
		        new Task(() => { result.hash = HashCreatorBuilder.getHashCreator(childCols, false) ?? result.hash; })
			        .Start();
	        }

	        return result;

        }

	    public DataRow[] getChildRows(DataRow rParent, DataRelation rel) {
			if (childHasher.TryGetValue(rel.RelationName, out var rh)) {
				if (rh.noIndex) {
					return rParent.GetChildRows(rel);
				}

				DataRowVersion v = DataRowVersion.Default;
				if (rParent.RowState == DataRowState.Deleted) v = DataRowVersion.Original;
				foreach (var parC in rel.ParentColumns) {
					if (rParent[parC, v].ToString() == "") return new DataRow[0];
				}

				return rh.getRows(rParent);
			}
			var fields =(from c in rel.ChildColumns select c.ColumnName).ToArray();
		    var index = getIndex(rel.ChildTable, fields );
		    if (index == null && true) index = checkCreateIndex(rel.ChildTable, fields, false);//crea sempre gli indici per le childRows
		    if (index == null) {
			    rh = new relHasher() {
				    noIndex = true
			    };
			    childHasher[rel.RelationName] = rh;
			    return rParent.GetChildRows(rel);
		    }

		    rh = createChildHasher(index, rel);
		    childHasher[rel.RelationName] = rh;

		    foreach (var parC in rel.ParentColumns) {
			    if (rParent[parC].ToString() == "") return new DataRow[0];
		    }

		    return rh.getRows(rParent);
	    }

        public relHasher  getParentHasher(DataRelation rel) {
	        if (parentHasher.TryGetValue(rel.RelationName, out relHasher rh)) return rh;
	        var fields =(from c in rel.ParentColumns select c.ColumnName).ToArray();
	        var index = getIndex(rel.ParentTable, fields );
	        if (index == null && autoIndex) index = checkCreateIndex(rel.ParentTable, fields, false);
	        if (index == null) {
		        rh = new relHasher() {
			        noIndex = true
		        };
		        parentHasher[rel.RelationName] = rh;
		        return rh;
	        }

	        rh = createParentHasher(index, rel);
	        parentHasher[rel.RelationName] = rh;
	        return rh;
        }

        public DataRow[] getParentRows(DataRow rChild, DataRelation rel) {
	        relHasher rh = getParentHasher(rel);
	        if (rh.noIndex) {
		        return rChild.GetParentRows(rel);
	        }
	        foreach (var parC in rel.ChildColumns) {
		        if (rChild[parC].ToString() == "") return new DataRow[0];
	        }

	        return rh.getRows(rChild);
        }


        public DataRow getChildRow(DataRow rParent, DataRelation rel) {
			if (childHasher.TryGetValue(rel.RelationName, out var rh)) {
				if (rh.noIndex) {
					return rParent.GetChildRows(rel).FirstOrDefault();
				}
				foreach (var parC in rel.ParentColumns) {
					if (rParent[parC].ToString() == "") return null;
				}
				return rh.getRow(rParent);
			}
			var fields =(from c in rel.ChildColumns select c.ColumnName).ToArray();
		    var index = getIndex(rel.ChildTable, fields );
		    if (index == null && true) index = checkCreateIndex(rel.ChildTable, fields, false);//crea sempre gli indici per le childRows
		    if (index == null) {
			    rh = new relHasher() {
				    noIndex = true
			    };
			    childHasher[rel.RelationName] = rh;
			    return rParent.GetChildRows(rel).FirstOrDefault();
		    }

		    rh = createChildHasher(index, rel);
		    childHasher[rel.RelationName] = rh;

		    foreach (var parC in rel.ParentColumns) {
			    if (rParent[parC].ToString() == "") return null;
		    }
		    return rh.getRow(rParent);
	    }

	    public DataRow getParentRow(DataRow rChild, DataRelation rel) {
			if (parentHasher.TryGetValue(rel.RelationName, out var rh)) {
				if (rh.noIndex) {
					return rChild.GetParentRows(rel).FirstOrDefault();
				}
				foreach (var parC in rel.ChildColumns) {
					if (rChild[parC].ToString() == "") return null;
				}
				return rh.getRow(rChild);
			}
			var fields =(from c in rel.ParentColumns select c.ColumnName).ToArray();
		    var index = getIndex(rel.ParentTable, fields );
		    if (index == null && autoIndex) index = checkCreateIndex(rel.ParentTable, fields, false);//crea sempre gli indici per le childRows
		    if (index == null) {
			    rh = new relHasher() {
				    noIndex = true
			    };
			    parentHasher[rel.RelationName] = rh;
			    return rChild.GetParentRows(rel).FirstOrDefault();
		    }

		    rh = createParentHasher(index, rel);
		    parentHasher[rel.RelationName] = rh;

		    foreach (var parC in rel.ChildColumns) {
			    if (rChild[parC].ToString() == "") return null;
		    }
		    return rh.getRow(rChild);
	    }

        public void suspend( bool keepIndexes, params DataTable []tt) {
	        foreach (var t in tt) {
		        foreach (var i in getIndexes()) {
			        if (i.tableName == t.TableName) i.suspend(keepIndexes);
		        }
	        }
        }
        public void resume(bool keepIndexes, params DataTable [] tt) {
	        int handle =  StartTimer("resume indexes");
	        foreach (var t in tt) {
		        foreach (var i in getIndexes()) {
			        if (i.tableName == t.TableName) i.resume(keepIndexes);
		        }
	        }
             StopTimer(handle);
        }

        
    }
	

	 public class NoIndexManager : IIndexManager {
	    public bool autoIndex { get; set; }
        public relHasher noHasher = new relHasher {noIndex=true};
	    public relHasher  getParentHasher(DataRelation rel) {
		    return noHasher;
	    }

	    public DataSet d;

	    public NoIndexManager(DataSet d) {
		    this.d = d;
	    }
	   
	    public DataSet linkedDataSet() {
		    return d;
	    }

	    public IMetaIndex checkCreateIndex(DataTable t, string[] keys, bool unique) {
		    return null;
	    }

		
	    public void suspend(bool keepIndexes, params DataTable []t) {
	    }
	    public void resume(bool keepIndexes,  params DataTable [] t) {
	    }
	    /// <summary>
	    /// Dispose all index of an owner
	    /// </summary>
	    /// <param name="owner"></param>
	    public void Dispose(string owner) {
	    }

	    public void createPrimaryKeysIndexes() {
		    return;
	    }

	    public IEnumerable<IMetaIndex> getIndexes() {
		    return new IMetaIndex[] { };
	    }

	    /// <summary>
	    /// Gets an index on a table  with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <returns></returns>
	    public IMetaIndex getPrimaryKeyIndex(DataTable t) {
		    return null;
	    }


	    /// <summary>
	    /// Check if an index exists on a table with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="keys"></param>
	    /// <returns></returns>
	    public bool hasIndex(DataTable t, string[] keys) {
		    return false;
	    }

	    public void Dispose() {
	    }

	    /// <summary>
	    /// Gets an index on a table  with specified columns
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="keys"></param>
	    /// <returns></returns>
	    public IMetaIndex getIndex(DataTable t, string[] keys) {
		    return null;
	    }

	    public IMetaIndex createPrimaryKeyIndex(DataTable t) {
		    return null;
	    }

	    /// <summary>
	    /// Adds an index on a table 
	    /// </summary>
	    /// <param name="t"></param>
	    /// <param name="index"></param>
	    /// <param name="owner"></param>
	    /// <returns></returns>
	    public void addIndex(DataTable t, IMetaIndex index,string owner=null) {
	    }

	    public DataRow [] getRows(DataTable t, object sample, params string[] fields) {
		    return t.filter(q.mCmp(sample, fields));
	    }

	    public DataRow getRow(DataTable t, object sample, params string[] fields) {
		    return t.filter(q.mCmp(sample, fields)).FirstOrDefault();
	    }

	    public DataRow[] getRows(DataTable t, Dictionary<string, object> keyValues) {
		    return t.filter(q.mCmp(keyValues));
	    }
	    public DataRow getRow(DataTable t, Dictionary<string, object> keyValues) {
		    return t.filter(q.mCmp(keyValues)).FirstOrDefault();
	    }

	    public DataRow[] getChildRows(DataRow r, DataRelation rel) {
		    return r.GetChildRows(rel);
	    }

	    public DataRow[] getParentRows(DataRow r, DataRelation rel) {
			return r.GetParentRows(rel);
	    }

	    public DataRow getChildRow(DataRow r, DataRelation rel) {
		    return r.GetChildRows(rel).FirstOrDefault();
	    }

	    public DataRow getParentRow(DataRow r, DataRelation rel) {
		    return r.GetParentRows(rel).FirstOrDefault();
	    }


    }

    /// <summary>
    /// Generic implementation for a MetaTable of MetaRows, this is a base type for all specific MetaData tables
    /// </summary>
    [Serializable]
    public class MetaTable : MetaTableBase<MetaRow> {

        /// <summary>
        /// Row collection, necessary to make it visible
        /// </summary>
        public new DataRowCollection Rows => base.Rows;

        /// <summary>
        /// Creates a MetaTable with a given name
        /// </summary>
        /// <param name="tableName"></param>
        public MetaTable(string tableName) : base(tableName) {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MetaTable() : base("Table") {
        }

        /// <summary>
        /// Needed to implement ISerializable
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MetaTable(SerializationInfo info, StreamingContext context):base(info,context) {

        }
        /// <summary>
        /// Necessary method to implement the interface TypedTableBase
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected override DataRow NewRowFromBuilder(DataRowBuilder builder) {
            return new MetaRow(builder);
        }

        /// <summary>
        ///  Necessary method to implement the interface TypedTableBase, simply return typeof(MetaRow) here
        /// </summary>
        /// <returns></returns>
        protected override global::System.Type GetRowType() {
            return typeof(MetaRow);
        }

        /// <summary>
        /// Se true, alla tabella non è applicata la sicurezza
        /// </summary>
        public bool SkipSecurity
        {
            get
            {
                if (ExtendedProperties["SkipSecurity"] != null) return true;
                return false;
            }
            set
            {
                if (value) {
                    ExtendedProperties["SkipSecurity"] = true;
                }
                else {
                    ExtendedProperties["SkipSecurity"] = null;
                }
            }
        }




        /// <summary>
        /// Filtro da applicare su questa tabella quando è chiave esterna in una riga in fase di inserimento
        /// </summary>
        public string FilterForInsert
        {
            get
            {
                return ExtendedProperties["FilterForInsert"] as string;
            }
            set
            {
                ExtendedProperties["FilterForInsert"] = value;
            }
        }

        /// <summary>
        /// Filtro  da applicare su questa tabella quando è chiave esterna in una riga in fase di ricerca
        /// </summary>
        public string FilterForSearch
        {
            get
            {
                return ExtendedProperties["FilterForSearch"] as string;
            }
            set
            {
                ExtendedProperties["FilterForSearch"] = value;
            }
        }



        /// <summary>
        /// Converts DBNull to null, other values are left unchanged
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static T? getValueOrNullStruct<T>(object o) where T : struct {
            if (o == DBNull.Value) return null;
            return (T)o;
        }

        /// <summary>
        /// Converts DBNull to null, other values are left unchanged
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static T getValueOrNullClass<T>(object o) where T : class {
            if (o == DBNull.Value) return null;
             
            return (T)o;
        }

        /// <summary>
        /// Converts null to DBNull, other values are left unchanged
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object getObject<T>(T o) where T : class {
            if (o == null) return DBNull.Value;
            return o;
        }



    }
    



    /// <summary>
    /// Base type for specific DataTables
    /// </summary>
    public class MetaRow : DataRow {
        /// <summary>
        /// Necessary constructor to make the serialization process work
        /// </summary>
        /// <param name="builder"></param>
        public MetaRow(DataRowBuilder builder) : base(builder) {
        }

      
    }
 
}


