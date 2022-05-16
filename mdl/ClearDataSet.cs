using System;
using System.Data;
using System.Collections.Generic;

namespace mdl
{
	/// <summary>
	/// Utility functions to strip DataSet from Framework-added constraints 
	///  and undesired structures.
	/// </summary>
	public class ClearDataSet
	{
	
        /// <summary>
        /// Creates a "stripped version" of a DataSet, preserving only:
        ///  Tables structure (column names, types, expressions)
        ///  Tables parent/child relations
        ///  Tables Primary Keys
        /// </summary>
        /// <param name="d">DataSet to process</param>
        /// <returns>Garbage-Stripped DataSet</returns>
        public static DataSet Clear(DataSet d){

            //duplicates structure
            var d2 = new DataSet(d.DataSetName);
            foreach (DataTable T in d.Tables){
                var t2 = new DataTable(T.TableName);
                foreach (DataColumn c in T.Columns){
                    t2.Columns.Add(c.ColumnName, c.DataType, c.Expression);
                }
                d2.Tables.Add(t2);
            }

            //copy primary keys
            foreach (DataTable T in d.Tables){
                var t2 = d2.Tables[T.TableName];
                var key = T.PrimaryKey;
                var key2 = new DataColumn[key.Length];
                for (var i=0; i<key.Length; i++){
                    key2[i] = t2.Columns[key[i].ColumnName];
                }                    
                t2.PrimaryKey = key2;                
            }

            //copy relations
            foreach (DataRelation r in d.Relations){
                var child2 = d2.Tables[r.ChildTable.TableName];
                var childcol = r.ChildColumns;
                var childcol2 = new DataColumn[childcol.Length];
                for (var i=0; i<childcol.Length; i++){
                    childcol2[i] = child2.Columns[childcol[i].ColumnName];
                }                    
                var parent2 = d2.Tables[r.ParentTable.TableName];
                var parentcol = r.ParentColumns;
                var parentcol2 = new DataColumn[parentcol.Length];
                for (var i=0; i<parentcol.Length; i++){
                    parentcol2[i] = parent2.Columns[parentcol[i].ColumnName];
                }                    
                var r2 = new DataRelation(r.RelationName, parentcol2, childcol2, false);
                d2.Relations.Add(r2);
            }
            return d2;


        }

        /// <summary>
        /// Remove specific constraints from a table
        /// </summary>
        /// <param name="t"></param>
	    public static void RemoveConstraints(DataTable t) {
	        if (t == null) return;
	        //D.EnforceConstraints=false;
	        var somethingDone = true;
	        while (somethingDone) {
	            somethingDone = false;
	            var toRemove = new List<Constraint>();
	            foreach (Constraint c in t.Constraints) {
	                if (t.Constraints.CanRemove(c)) {
	                    toRemove.Add(c);
	                    somethingDone = true;
	                }
	            }

	            foreach (var c in toRemove) {
	                c.Table.Constraints.Remove(c);
	            }

	        }
	    }

	    /// <summary>
        /// Removes all possibly-removable constraint from a DataSet.
        ///  Unique-type constraint are not removed.
        /// </summary>
        /// <param name="d"></param>
        public static void RemoveConstraints(DataSet d){
            if (d==null) return;
			//D.EnforceConstraints=false;
            var somethingDone=true;
            while (somethingDone) {
                somethingDone = false;
                var toRemove = new List<Constraint>();
                foreach (DataTable T in d.Tables) {
                    foreach (Constraint c in T.Constraints) {
                        if (T.Constraints.CanRemove(c)) {
                            toRemove.Add(c);
                            somethingDone = true;                                              
                        }
                    }
                }

                foreach (var c in toRemove) {
                    c.Table.Constraints.Remove(c);
                }

            }



        }


	}
}
