using System;
using System.Data;
using System.Diagnostics;
using System.IO;

using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using q = mdl.MetaExpression;

#pragma warning disable IDE1006 // Naming Styles

namespace mdl
{
	/// <summary>
	/// Help class to build SQL statements
	/// </summary>
	public class QueryCreator
	{
        /// <summary>
        /// Extended property that means that the column does not really belong to 
        ///   a real table. For example, expression-like column
        /// </summary>
       
	    

		//		public QueryCreator()
		//		{
		//			//
		//			// TODO: Add constructor logic here
		//			//
		//
		//		}

		

      




        /// <summary>
        /// Check if s starts and ends with a  ( and a ) and contains a pair values of open and close parenthesis
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool isABlock(string s) {
            if (!s.StartsWith("("))return false;
            if (!s.EndsWith(")"))return false;
            return StringParser.closeBlock(s,1,'(',')')==s.Length;
        }

        /// <summary>
        /// Return an expression wrapped in parenthesis
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string putInPar(string expression) {
            if (expression == null) return expression;
            if (expression == "") return expression;
            if (isABlock(expression)) {
                return expression;
            }
            return "(" + expression + ")";
        }

		

        /// <summary>
        /// Checks that primary key of Temp is the same of Source
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Temp"></param>
        public static void CheckKeyEqual(DataTable Source,  DataTable Temp){
			if (Source.PrimaryKey==null) return;
			if (Source.PrimaryKey.Length<1) return;
			try {
				Temp.PrimaryKey=null;
				var NewKey = new DataColumn[Source.PrimaryKey.Length];
				for (var i=0;i< Source.PrimaryKey.Length; i++){
					NewKey[i]= Temp.Columns[Source.PrimaryKey[i].ColumnName];
				}
				Temp.PrimaryKey=NewKey;
			}
			catch{
			}
		}



	  
        /// <summary>
        /// Write a string to the debugger output
        /// </summary>
        /// <param name="e"></param>
		public static void MarkEvent(string e){
          ErrorLogger.Logger.MarkEvent(e);
        }

	    /// <summary>
	    /// Write a string to the debugger output
	    /// </summary>
	    /// <param name="e"></param>
	    public static void WarnEvent(string e){
	        //myLastError= QueryCreator.GetPrintable(e);
	        var msg = "$$"+DateTime.Now.ToString("HH:mm:ss.fff") + ":"+e;
	        Trace.WriteLine(msg);
	        Trace.Flush();
	    }

       
	    /// <summary>
	    /// Sets a field to DBNull (or -1(int)  or 0-like values when DBNull is not allowed)
	    /// </summary>
	    /// <param name="C"></param>
	    public static object clearValue(DataColumn C) {
	        if (C.AllowDBNull) {
	            return  DBNull.Value;
	        }
	        var typename = C.DataType.Name;
	        switch (typename) {
	            case "String":
	                return "";
	            case "Char":
	                return "";
	            case "Double": {
	                return 0d;
	            }
	            case "Single": {
	                return 0f;
	            }
	            case "Decimal": {
	                return 0d;
	            }
	            case "DateTime": {
	                return mdl.HelpUi.EmptyDate();
	            }
	            case "Int16":
	                return 0;
	            case "Int32":
	                return 0;
	            case "Byte":
	                return 0;	                
	            default:
	               return "";
	        }

	    }

        /// <summary>
        /// Sets a field to DBNull (or -1(int)  or 0-like values when DBNull is not allowed)
        /// </summary>
        /// <param name="R"></param>
        /// <param name="C"></param>
        public static void ClearField(DataRow R, DataColumn C) {
            R[C] = clearValue(C);
        }


        /// <summary>
        /// Sets to "zero" or "" all columns of a row that does not allow nulls
        /// </summary>
        /// <param name="R"></param>
        public static void ClearRow(DataRow R){
            foreach(DataColumn C in R.Table.Columns){
                if (C.AllowDBNull) continue;
                if ((R[C]!=null)&&(R[C]!=DBNull.Value)) continue;
                R[C] = clearValue(C);
            }
        }


       
		
		


       

        /// <summary>
        /// Gets the filter to be used in insert operation for a table
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
		public static string GetInsertFilter(DataTable T){
			return T.ExtendedProperties["myFilterForInsert"] as string;
		}

        /// <summary>
        /// Sets the filter to be used in insert operation for a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="S"></param>
		public static void SetInsertFilter(DataTable T,string S){
			T.ExtendedProperties["myFilterForInsert"]=S;
		}

        /// <summary>
        /// Gets the filter to be used in search operation for a table
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static string GetSearchFilter(DataTable T) {
            return T.ExtendedProperties["myFilterForSearch"] as string;
        }

        /// <summary>
        /// Sets the filter to be used in search operation for a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="S"></param>
        public static void SetSearchFilter(DataTable T, string S) {
            T.ExtendedProperties["myFilterForSearch"] = S;
        }

        /// <summary>
        /// skip this table when insertcopy command is invoked
        /// </summary>
        /// <param name="t"></param>
        /// <param name="skip"></param>
	    public static void SetSkipInsertCopy(DataTable t, bool skip) {
            t.ExtendedProperties["skipInsertCopy"] = skip;
        }
        /// <summary>
        /// check if  this table is to skip when insertcopy command is invoked
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsSkipInsertCopy(DataTable t) {
            if (t.ExtendedProperties["skipInsertCopy"] == null) return false;
            return (bool) t.ExtendedProperties["skipInsertCopy"];
        }


      
		

		/// <summary>
		/// Gets the common child of two tables
		/// </summary>
		/// <param name="Parent1"></param>
		/// <param name="Parent2"></param>
		/// <returns>Common child Table</returns>
		public static DataTable GetMiddleTable(DataTable Parent1, DataTable Parent2){
			foreach(DataRelation R1 in Parent1.DataSet.Relations){
				if (R1.ParentTable!=Parent1) continue;
				var Middle= R1.ChildTable;
				foreach(DataRelation R2 in Parent1.DataSet.Relations){
					if((R2.ParentTable==Parent2)&&(R2.ChildTable==Middle)) return Middle;
				}
			}
			return null;
		}



        /// <summary>
        /// get the general condition in order to activate a datarelation during getdata phase
        /// </summary>
        /// <param name="Rel"></param>
        /// <returns></returns>
		public static string GetRelationActivationFilter(DataRelation Rel){
			if (Rel.ExtendedProperties["activationfilter"]==null) return null;
			return Rel.ExtendedProperties["activationfilter"].ToString();
		}

        /// <summary>
        /// set the general condition in order to activate a datarelation during getdata phase
        /// </summary>
        /// <param name="Rel"></param>
        /// <param name="filter"></param>
		public static void SetRelationActivationFilter(DataRelation Rel,string filter){
			Rel.ExtendedProperties["activationfilter"]=filter;
		}

        ///// <summary>
        ///// get the condition on parent in order to activate a datarelation during getdata phase
        ///// </summary>
        ///// <param name="Rel"></param>
        ///// <returns></returns>
		//public static string GetParentRelationActivationFilter(DataRelation Rel){
		//	if (Rel.ExtendedProperties["parentactivationfilter"]==null) return null;
		//	return Rel.ExtendedProperties["parentactivationfilter"].ToString();
		//}

  //      /// <summary>
  //      ///  set the condition on parent in order to activate a datarelation during getdata phase
  //      /// </summary>
  //      /// <param name="Rel"></param>
  //      /// <param name="filter"></param>
		//private static void SetParentRelationActivationFilter(DataRelation Rel,string filter){
		//	Rel.ExtendedProperties["parentactivationfilter"]=filter;
		//}


        /// <summary>
        /// Set filter to fill the table when attaced to a combobox and main row is in insert mode
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
		public static void SetFilterForInsert(DataTable T,string filter){
			T.ExtendedProperties["FilterForInsert"]=filter;
		}
        /// <summary>
        /// Get filter to fill the table when attaced to a combobox and main row is in insert mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
		public static string  GetFilterForInsert(DataTable T){
			if (T==null) return null;
			return T.ExtendedProperties["FilterForInsert"] as string;
		}

        /// <summary>
        /// Set filter to fill the table when attaced to a combobox and main row is in search mode
        /// </summary>
        /// <param name="T"></param>
        /// <param name="filter"></param>
        public static void SetFilterForSearch(DataTable T, string filter) {
            T.ExtendedProperties["FilterForSearch"] = filter;
        }

        /// <summary>
        ///  Get filter to fill the table when attaced to a combobox and main row is in search mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public static string GetFilterForSearch(DataTable T) {
            if (T == null) return null;
            return T.ExtendedProperties["FilterForSearch"] as string;
        }


       

	
		


		/// <summary>
		/// Build a string that represents the Object O of type T. This string
		///  is built so that it can be used in a SQL instruction for assigning in
		///  VALUES lists.
		/// </summary>
		/// <param name="O">Object to display in the output string</param>
		/// <param name="T">Base Type of O</param>
		/// <returns>String representation of O</returns>
		public static string CrystalValue(Object O, System.Type T){            
			var typename= T.Name;
			switch(typename){
				case "String": 
					if (O==null) return "\"\"";
					if (O== DBNull.Value) return "\"\"";
					return "'"+O.ToString().Replace("\"","''")+"\"";
				case "Char": return "'"+O.ToString().Replace("'","''")+"'";
				case "Double": {                    
					var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
					var s1 = ((Double)O).ToString("n").Replace(group,"");
					return s1.Replace(dec,".");
				}
				case "Single": {                    
					var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
					var s1 = ((Single)O).ToString("n").Replace(group,"");
					return s1.Replace(dec,".");
				}

				case "Decimal":{
					var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
					var s1 = ((Decimal)O).ToString("n").Replace(group,"");
					return s1.Replace(dec,".");
				}
				case "DateTime":{
					var TT = (DateTime) O; //Convert.ToDateTime(s);                    
					return "DateTime("+

						TT.Month.ToString()+ "/"+ TT.Day.ToString() + "/" +
						TT.Year.ToString()+" "+TT.Hour.ToString()+":"+
						TT.Minute.ToString()+":"+TT.Second.ToString()+"."+
						TT.Millisecond.ToString().PadLeft(3,'0');
				}
				case "Int16": return O.ToString();
				case "Int32": return O.ToString();
				default: return O.ToString();
			}          
		}

		
		

      


        /// <summary>
        ///  Returns a SQL condition that tests a field name to be LIKE an 
        ///  object O of type T, if the string representation of O contains
        ///  a % character, or EQUAL if it does not happen.
        /// </summary>
        /// <param name="fieldname">Name of the field that appears in the result</param>
        /// <param name="O">Value to compare with the field</param>
        /// <param name="T">Base type of O</param>
        /// <param name="SQL">if true, a SQL string is returned</param>
        /// <returns>"(fieldname='...')" or "(fieldname LIKE '...')"</returns>
        /// <remarks>Does not manages NULL values of O</remarks>
        public static string comparelikefields(string fieldname, 
            object O, System.Type T, bool SQL){
            var s=mdl_utils.Quoting.quote(O, T, SQL);
            if (s.IndexOf("%")>=0) 
                return "(" + fieldname + " LIKE " +s+ ")";
            else
                return "("+fieldname+" = "+s+")";
        }
        
		static string ColName(DataColumn C, bool forposting){
			if (!forposting) return C.ColumnName;
			return C.PostingColumnName();
		}

        /// <summary>
        /// Creates a string like: (field1='..') and (field2='..') ...
        ///   for all real (not expression or temporary) fields of a datarow
        ///   if forposting=true uses posting-columnnames for columnsnames
        /// </summary>
        /// <param name="R">Row to consider for the values to compare</param>
        /// <param name="ver">Version of Data in the row to consider</param>
        /// <param name="forposting">if posting table/column names must be used</param>
        /// <param name="SQL">if true, SQL representation for values are used</param>
        /// <returns>condition string on all values</returns>
        static public string CompareAllFields (DataRow R, DataRowVersion ver, bool forposting, QueryHelper QH){
            var T = R.Table;
            var outstring = "";
            var first=true;
            foreach (DataColumn C in T.Columns){
				if (C.IsTemporary()) continue;
                var colname = ColName(C, forposting);
				if (colname==null) continue;
				if ((C.ExtendedProperties["sqltype"]==null)
						||(C.ExtendedProperties["sqltype"].ToString()!="text")){
					if (first)
						first=false;
					else
						outstring += " AND ";
					outstring += QH.CmpEq(colname, R[C,ver]);               
				}

            }
            return outstring;
        }



		
        
        /// <summary>
        /// Creates a string of type (field1='..') and (field2='..') ... for all
        ///   the keyfields of the Primary Key of a datarow
        /// </summary>
        /// <param name="r">Row to use for getting values to compare</param>
        /// <param name="ver">Version of the DataRow to use</param>
        /// <param name="sql">if true, SQL compatible string values are used</param>
        /// <returns></returns>
        static public MetaExpression FilterKey (DataRow r, DataRowVersion ver, bool forPosting=false){
            var T = r.Table;
            return FilterColumnPairs(r, T.PrimaryKey, T.PrimaryKey, ver, forPosting);
        }


        /// <summary>
        /// Creates a string of type (field1='..') and (field2='..') ... for all
        ///   the fields specified by a DataColumn Collection
        /// </summary>
        /// <param name="ValueRow">Row to use for getting values to compare</param>
        /// <param name="ValueCol">RowColumns of ParentRow from which values to be
        ///     compare have to be taken</param>
        /// <param name="FilterCol">RowColumn of ChildRows for which the Column NAMES have
        ///   to be taken</param>
        /// <param name="ver">Version of ValueRow to consider</param>
        /// <param name="SQL">if true, SQL representation of values are used</param>
        /// <returns>SQL comparison string on all fields</returns>
        static public MetaExpression FilterColumnPairs(DataRow ValueRow, 
            DataColumn[] ValueCol, 
            DataColumn[] FilterCol, 
            DataRowVersion ver, 
            bool forPosting=false){

            return q.and(
                    FilterCol.Zip(ValueCol,
					(col, value) => {
                        var fieldName = forPosting? col.PostingColumnName(): col.ColumnName;
                        return q.eq(fieldName, ValueRow[col.ColumnName, ver]);

                    }
                ));
        }


        /// <summary>
        /// Gets a multicompare that connects a parentrow to his child   with a specified set of parent/child columns
        /// </summary>
        /// <param name="ParentRow"></param>
        /// <param name="ParentCol"></param>
        /// <param name="ChildCol"></param>
        /// <param name="ver"></param>
        /// <param name="SQL"></param>
        /// <returns></returns>
        static public MultiCompare ParentChildFilter(DataRow ParentRow,
            DataColumn[] ParentCol,
            DataColumn[] ChildCol,
            DataRowVersion ver, bool SQL) {
            var val = new object[ParentCol.Length];
            var fields = new string[ParentCol.Length];
            for (var i = 0; i < ParentCol.Length; i++) {
                var Parent = ParentCol[i];
                var Child = ChildCol[i];
              
                var fieldname = Child.ColumnName;
                if (SQL) fieldname = Child.PostingColumnName();
                val[i] = ParentRow[Parent.ColumnName, ver];
                fields[i] = fieldname;
            }
            return new MultiCompare(fields, val);

        }

        

      



        /// <summary>
        /// Checks if any of specified columns of a specified version of R
        ///  contains null or DBNull values or empty strings
        /// </summary>
        /// <param name="R"></param>
        /// <param name="Cols"></param>
        /// <param name="ver"></param>
        /// <returns>true if some value is null</returns>
        static public bool ContainsNulls(DataRow R, DataColumn[] Cols, 
             DataRowVersion ver){
            foreach (var C in Cols){
                if (R[C,ver]==null) return true;
                if (R[C,ver]==DBNull.Value) return true;
				if (R[C,ver].ToString()=="") return true;
            }
            return false;
        }
        

       
        /// <summary>
        /// Get the list of real (not temporary or expression) columns NAMES of a table T
        ///  formatting it like "fieldname1, fieldname2,...."
        /// </summary>
        /// <param name="T">Table to scan for columns</param>
        /// <returns>table real column list</returns>
        static public string RealColumnNameList(DataTable T){
            var outstring = "";
            var first=true;
            foreach (DataColumn C in T.Columns){
                if (C.IsTemporary()) continue;
                if (first)
                    first=false;
                else
                    outstring += ",";
                outstring += C.ColumnName;
            }
            return outstring;
        }

		
		/// <summary>
		/// Get the list of real (not temporary or expression) columns NAMES of a table T
		///  formatting it like "fieldname1, fieldname2,...."
		/// Columns are sorted on their list column position values
		/// </summary>
		/// <param name="T">Table to scan for columns</param>
		/// <returns>table real column list</returns>
		static public string SortedColumnNameList(DataTable T){
			
			var outstring = "";
			var first=true;
			var L= new DataColumn[T.Columns.Count];
			var N=0;
			foreach(DataColumn C in T.Columns){
				if (C.ExtendedProperties["ListColPos"]==null)continue;
				var currpos= Convert.ToInt32(C.ExtendedProperties["ListColPos"]);
				if (currpos<0) continue;
				//cerca la posizione i dove mettere la colonna (ord.crescente)
				var i=0;
				while (i<N){
					var ThisCol=L[i];
					var thispos= Convert.ToInt32(ThisCol.ExtendedProperties["ListColPos"]);
					if (thispos>currpos) break;
					i++;
					continue;
				}
				//shifta tutti gli elementi da i+1 in poi in avanti
				if (i<N){
					for (var j=N;j>i;j--) L[j]=L[j-1];
				}
				L[i]=C;
				N++;
			}

			foreach(DataColumn C in T.Columns){
				if (C.ExtendedProperties["ListColPos"]!=null){
					var currpos= Convert.ToInt32(C.ExtendedProperties["ListColPos"]);
					if (currpos>=0) continue;
				}
				L[N]=C;
				N++;
			}

			foreach (var C in L){
				if (C.IsTemporary()) continue;
				if (first)
					first=false;
				else
					outstring += ",";
				outstring += C.ColumnName;
			}
			return outstring;
		}





       

		/// <summary>
		/// Tells wheter a field belongs to primary key
		/// </summary>
		/// <param name="T"></param>
		/// <param name="field"></param>
		/// <returns>true if field belongs to primary key of T</returns>
        public static bool IsPrimaryKey(DataTable T, string field){
            foreach(var C in T.PrimaryKey){
                if (C.ColumnName==field) return true;
            }
            return false;
        }


		/// <summary>
		/// Gets the relation that links a Parent Table with a Child Table
		/// </summary>
		/// <param name="Parent"></param>
		/// <param name="Child"></param>
		/// <returns>DataRelation or null if the relation does not exists</returns>
        public static DataRelation GetParentChildRel(DataTable Parent, DataTable Child){
			if (Parent==null) return null;
			if (Child==null) return null;
            foreach (DataRelation R in Parent.ChildRelations){
                if (R.ChildTable.TableName == Child.TableName) return R;
            }
            return null;
        }
	}
}
