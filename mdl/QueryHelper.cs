using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Reflection;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {
    /// <summary>
    /// Implementation of QueryHelper for Sql Server database
    /// </summary>
    public class SqlServerQueryHelper : QueryHelper {

        public override string ReturnValueIfNoRowAffected(int value) {
            return $"if (@@ROWCOUNT=0) BEGIN select {value}; RETURN; END;";
        }

        public override string Max(string expr) {
            return $"max({expr})";
        }

        public override string Min(string expr) {
            return $"min({expr})";
        }
        public override string Sustring(string expr, int start, int len) {
            return $"substring({expr},{start},{len})"; //for oracle  substr
        }
        public override string ConvertToVarchar(string expr, int len) {
            return $"convert($'varchar({len})',{expr})";
        }
        public override string ConvertToInt(string expr) {
            return $"convert(int,{expr})";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="O"></param>
        /// <param name="SQL">if true, SQL compatible representation are used</param>
        /// <returns></returns>

        /// <summary>
        /// Gets the string (unquoted) representing an object (ex unquotedstrvalue).The only difference between
        ///  quoted and unquoted is about how they operate on strings.
        /// </summary>
        /// <param name="O">Object to display in the output string</param>
        /// <param name="T">Base Type of O</param>
        /// <param name="SQL">if true, result can be used for building SQL commands</param>
        /// <returns>String representation of O</returns>
        public override string unquoted(Object O){            
            if (O==null) return "null";
            if (O== DBNull.Value) return "null";
            var typename= O.GetType().Name;
            switch(typename){
                case "String": return O.ToString();
                case "Char": return O.ToString();
                case "Double": {
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;                    
					var s1 = ((Double)O).ToString("f10");
                    if(group != dec) s1= s1.Replace(group,"");

					var pos = s1.IndexOf(dec,0);
					if (pos<0) return s1;
                    s1 = s1.Replace(dec, ".");					
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}
				case "Single": {                    
					var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Single)O).ToString("f10");
                    if(group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
					if (pos<0) return s1;
					s1 =  s1.Replace(dec,".");
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}

				case "Decimal":{                   
					var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Decimal)O).ToString("f10");
                    if(group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
					if (pos<0) return s1;
                    s1 = s1.Replace(dec, ".");
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}
                case "DateTime":{                   
                    var TT = (DateTime) O; //Convert.ToDateTime(s);   
                    if (TT.Date.Equals(TT)) {
                        return "{d '"+TT.Year.ToString()+"-"+
                               TT.Month.ToString().PadLeft(2,'0')+"-"+
                               TT.Day.ToString().PadLeft(2,'0')+
                               "'}";
                    }
					return "{ts '"+TT.Year.ToString()+"-"+
						TT.Month.ToString().PadLeft(2,'0')+"-"+
						TT.Day.ToString().PadLeft(2,'0')+" "+
						TT.Hour.ToString().PadLeft(2,'0')+":"+
						TT.Minute.ToString().PadLeft(2,'0')+":"+
						TT.Second.ToString().PadLeft(2,'0')+"."+
						TT.Millisecond.ToString().PadLeft(3,'0')+
						"'}";

                }
                case "Int16": return O.ToString();
                case "Int32": return O.ToString();
				case "Int64": return O.ToString();
                case "UInt16": return O.ToString();
                case "UInt32": return O.ToString();
                case "UInt64": return O.ToString();
                case "Byte[]": 
					//BinaryFormatter BF = new BinaryFormatter();
					var buf = (Byte[])O;
					return mdl_utils.Quoting.ByteArrayToString(buf);
				case "Byte": 
					return O.ToString();
				case "Boolean":
					if ((bool)O == true) return "true";
					return "false";

                default: 
					ErrorLogger.Logger.MarkEvent("Could not find type "+typename);
					return O.ToString();
            }          
        }

        /// <summary>
		/// Gets the quoted representation of an object
		/// </summary>
		/// <param name="O"></param>
		/// <param name="SQL">if true, SQL compatible strings are used</param>
		/// <returns></returns>
		private static string stringQuote(string s){            
			if (s==null) return "null";
			return "'"+s.Replace("'","''")+"'";
		}

		


        /// <summary>
        /// Returns the string representation of a constant object, quoting strings
        /// This must be used to incorporate constants into sql expressions (ex. quote)
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string quote(object O) {
            if (O == null) return "null";
            if (O == DBNull.Value) return "null";
                        
            if (O.GetType() != typeof(string)) return unquoted(O);
            string val = O.ToString();
            if (!val.StartsWith("&£$")) return stringQuote(val);
            return val.Substring(3);
        }
        /// <summary>
        /// Put S between parentesis
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public override string DoPar(string S) {
            return QueryCreator.putInPar(S);
        }

        /// <summary>
        /// Gets the logical AND of two string expressions
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public override string AppAnd(string q1, string q2) {
            return QueryHelper.AppendAND(q1, q2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="O"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public override string IsNullFn(object O, object def) {
            return $"isnull({quote(O)},{quote(def)})";
        }

        /// <summary>
        /// Safe miminum value for dates
        /// </summary>
        /// <returns></returns>
        public override DateTime SafeMinDate() {
            return new DateTime(1900, 1, 1);
        }


        /// <summary>
        /// Gets the logical AND of a list of string expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string AppAnd(params string[] q) {
            if (q.Length == 1) {
                return q[0];
                //throw new Exception("AppAnd con un parametro solo");
            }
            return mergeWithOperator("AND",q);
        }

        /// <summary>
        /// Gets the logical AND of a list of string expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseAnd(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseAnd con un parametro solo");
            }
            return mergeWithOperator("&",q);
        }

        /// <summary>
        /// Gets the logical AND of a list of string expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseOr(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseOr con un parametro solo");
            }
            return  mergeWithOperator("|",q);;
        }

        /// <summary>
        /// Gets the logical AND of a list of string expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseXor(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseXor con un parametro solo");
            }
            return  mergeWithOperator("^",q);
        }

        /// <summary>
        /// Creates  a compare of  a specified fields of row, using the right version for the row 
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string CmpMulti(DataRow R, params string[] field) {
            DataRowVersion V = DataRowVersion.Default;
            if (R.RowState == DataRowState.Deleted) V = DataRowVersion.Original;
            List <string> operandi= new List<string>();            
            foreach (string f in field) operandi.Add( CmpEq(f, R[f, V]));
            return QueryHelper.mergeWithOperator("AND",operandi.ToArray());
            
        }

        /// <summary>
        /// Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string CmpMulti(DataRow R, DataColumn[] fields, DataRowVersion ver = DataRowVersion.Default) {
	        if ( R.RowState == DataRowState.Deleted) ver = DataRowVersion.Original;
	        var operandi= new List<string>();            
	        foreach (DataColumn c in fields) operandi.Add( CmpEq(c.ColumnName, R[c.ColumnName, ver]));
	        return mergeWithOperator("AND",operandi.ToArray());
        }

        /// <summary>
        /// Alias for CmpMulti
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string MCmp(DataRow R, params string[] field) {
            return CmpMulti(R, field);
        }

        /// <summary>
        /// Alias for CmpMulti
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public override string MCmp(object sample) {
            var operandi= new List<string>();
            string[] ff;
            if (sample is Dictionary<string, object> dict) {
                ff = dict.Keys.ToArray();
            }
            else {
                ff = sample.GetType().GetMembers().Where(f => f.MemberType == MemberTypes.Property).Pick("Name").Cast<string>().ToArray();
            }


            return mergeWithOperator("AND", ff.Map(f => CmpEq(f, MetaExpression.getField(f, sample))));

            //foreach (string f in ff) operandi.Add( CmpEq(f, MetaExpression.getField(f,sample)));
            //return mergeWithOperator("AND",operandi.ToArray());
        }


        /// <summary>
        /// Gets the logical OR of a list of string expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string AppOr(params string[] q) {
            if (q.Length == 1) {
                return q[0];
                //throw new Exception("AppOr con un parametro solo");
            }
            return mergeWithOperator("OR",q);
        }

        /// <summary>
        /// Gets the logical OR of a list of string expressions
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public override string AppOr(string q1, string q2) {
            return QueryHelper.AppendOR(q1, q2);
        }

        /// <summary>
        /// Compare a field with the specified object, or creating an IS NULL if the specified object is null
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpEq(string field, object O) {
            if ((O == null) || (O == DBNull.Value)) {
                return "(" + field + " IS NULL)";
            }
            return "(" + field + "=" + quote(O) + ")";
        }

        /// <summary>
        /// Get the compare of field with the unquoted given string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpEq(string field, string O) {
            return $"({field}={O})";
        }

        /// <summary>
        /// Check if a field is different from the specified object, or creating an IS NOT NULL if the specified object is null
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpNe(string field, object O) {
            if ((O == null) || (O == DBNull.Value)) {
                return "(" + field + " IS NOT NULL)";
            }
            return "(" + field + "<>" + quote(O) + ")";
        }

        /// <summary>
        /// Get the compare not equal of field with the unquoted given string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpNe(string field, string O) {
            return $"({field}<>{O})";
        }

        /// <summary>
        /// Get the compare Greater of field with the given object
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpGt(string field, object O) {

            return "(" + field + ">" + quote(O) + ")";
        }


        /// <summary>
        /// Get the compare Greater of field with the unquoted given string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpGt(string field, string O) {
            return $"({field}>{O})";
        }


        /// <summary>
        /// Get the compare Greater Equal of field with the given object
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpGe(string field, object O) {

            return "(" + field + ">=" + quote(O) + ")";
        }


        /// <summary>
        /// Get the compare Greater Equal of field with the given unquoted string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpGe(string field, string O) {
            return $"({field}>={O})";
        }


        /// <summary>
        /// Get the compare Less than of field with the given object
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpLt(string field, object O) {

            return "(" + field + "<" + quote(O) + ")";
        }

        /// <summary>
        /// Get the compare Less than of field with the given unquoted string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpLt(string field, string O) {
            return $"({field}<{O})";
        }


        /// <summary>
        /// Get the compare Less Equal than of field with the given object
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpLe(string field, object O) {

            return "(" + field + "<=" + quote(O) + ")";
        }

        /// <summary>
        /// Get the compare Less Equal than of field with the unquoted string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpLe(string field, string O) {
            return $"({field}<={O})";
        }


        /// <summary>
        /// get the is null condition on a field name
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string IsNull(string field) {
            return "(" + field + " IS NULL)";
        }


        /// <summary>
        ///  get the (field is null or field = O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrEq(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpEq(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &gt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrGt(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpGt(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &gt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrGe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpGe(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &lt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrLt(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpLt(field, O)));
        }
        /// <summary>
        ///  get the (field is null or field &lt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrLe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpLe(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &lt;&gt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrNe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpNe(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &lt;= O) where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrEq(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpEq(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &gt; O)  where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrGt(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpGt(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &gt;= O)  where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrGe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpGe(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &lt; O)  where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrLt(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpLt(field, O)));
        }


        /// <summary>
        ///  get the (field is null or field &lt;= O)  where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrLe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpLe(field, O)));
        }

        /// <summary>
        ///  get the (field is null or field &lt;&gt; O)  where O is used as is 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrNe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpNe(field, O)));
        }

        /// <summary>
        /// Get an expression that checks if a specified bit is set on a field
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit"></param>
        /// <returns></returns>
        public override string BitSet(string field, int nbit) {
            UInt32 mask = (UInt32)1 << nbit;
            return "((" + field + "&" + mask + ")<>0)";

        }


        /// <summary>
        /// Get an expression that checks if a specified bit is NOT set on a field
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit"></param>
        /// <returns></returns>
        public override string BitClear(string field, int nbit) {
            UInt32 mask = (UInt32)1 << nbit;
            return "((" + field + "&" + mask + ")=0)";
        }


        /// <summary>
        /// evaluates a list of distinct values converted into string with quotes and separated by a comma
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
        public override string DistinctVal(object[] OO) {
            var SS = new StringBuilder();
            var HH = new Hashtable();
            foreach (object O in OO) {
                string quoted = quote(O);
                if (HH[quoted] != null) continue;
                HH[quoted] = 1;
                SS.Append("," + quoted);
            }
            var outstring = SS.ToString();
            if (outstring != "") outstring = outstring.Substring(1);
            return outstring;


        }
        /// <summary>
        /// evaluates a list of distinct values converted into string with quotes and separated by a comma
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
        public override string UnquotedDistinctVal(string[] OO) {
            var SS = new StringBuilder();
            var HH = new Hashtable();
            foreach (string O in OO) {
                if (HH[O] != null) continue;
                HH[O] = 1;
                SS.Append("," + O);
            }
            var outstring = SS.ToString();
            if (outstring != "") outstring = outstring.Substring(1);
            return outstring;


        }

        /// <summary>
        /// evaluates a list of distinct values converted into string with quotes and separated by a comma
        ///  values are taken from a field of a row collection
        /// </summary>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string DistinctVal(DataRow[] ROWS, string column) {

            string outstring = "";
            if (ROWS.Length <= 100) {
                foreach (DataRow R in ROWS) {
                    string quoted = quote(R[column]);
                    if (outstring.IndexOf(quoted) > 0) continue;
                    outstring += "," + quoted;
                }
            }
            else {
                StringBuilder SS = new StringBuilder();
                Hashtable HH = new Hashtable();
                foreach (DataRow R in ROWS) {
                    string quoted = quote(R[column]);
                    if (HH[quoted] != null) continue;
                    HH[quoted] = 1;
                    SS.Append("," + quoted);
                }
                outstring = SS.ToString();
            }
            if (outstring != "") outstring = outstring.Substring(1);
            return outstring;
        }


        /// <summary>
        /// creates a string: (field in (list of ROWS[field]))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public override string FieldIn(string field, DataRow[] ROWS) {
            return FieldIn(field, ROWS, field);
        }

        /// <summary>
        /// creates a string: (field in (list of objects))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O">collection of object to compare with</param>
        /// <returns></returns>
        public override string FieldIn(string field, object[] O) {
            if (O.Length == 1) {
                return  CmpEq(field, O[0]) ;
            }
            string list = DistinctVal(O);
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }


        /// <summary>
        /// creates a string: (field in (list of objects))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O">collection of object to compare with</param>
        /// <returns></returns>
        public override string UnquotedFieldIn(string field, string[] O) {
            if (O.Length == 1) {
                return UnquotedCmpEq(field, O[0]);
            }
            string list = UnquotedDistinctVal(O);
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }


        /// <summary>
        /// Creates a string (field in LIST) if list is not null else (field is null and field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public override string FieldInList(string field, string list) {
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }

        /// <summary>
        /// creates a string (field in (distinct values from ROWS[column]))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string FieldIn(string field, DataRow[] ROWS, string column) {
            if (ROWS.Length == 1) {
                return CmpEq(field, ROWS[0][column]) ;
            }
            string list = DistinctVal(ROWS, column);
            return FieldInList(field, list);
        }

        /// <summary>
        ///  creates a string: (field not in (list of objects))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, object[] O) {
            if (O.Length == 1) {
                return CmpNe(field, O[0]) ;
            }
            string list = DistinctVal(O);
            if (list == "") return "";
            return "(" + field + " NOT IN (" + list + "))";
        }

        /// <summary>
        ///  creates a string: (field not in (list of objects))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedFieldNotIn(string field, string[] O) {
            if (O.Length == 1) {
                return UnquotedCmpNe(field, O[0]) ;
            }
            string list = UnquotedDistinctVal(O);
            if (list == "") return "";
            return "(" + field + " NOT IN (" + list + "))";
        }

        /// <summary>
        /// creates a string (field not in (objects from ROWS[field]))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, DataRow[] ROWS) {
            return FieldNotIn(field, ROWS, field);
        }

        /// <summary>
        ///  creates a string (field not in (list))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public override string FieldNotInList(string field, string list) {
            if (list == "") return "";
            return "(" + field + " NOT IN (" + list + "))";
        }

        /// <summary>
        ///  creates a string (field not in (objects from ROWS[column]))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, DataRow[] ROWS, string column) {
            if (ROWS.Length == 1) {
                return CmpNe(field, ROWS[0][column]);
            }
            string list = DistinctVal(ROWS, column);
            return FieldNotInList(field, list);
        }

        /// <summary>
        /// creates a string: not ( expression )
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override string Not(string expression) {
            return DoPar("NOT " + DoPar(expression));
        }

    

        /// <summary>
        /// creates a string: not ( expression )
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override string BitwiseNot(string expression) {
            return DoPar("~" + DoPar(expression));
        }

        /// <summary>
        /// creates a string: (field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string IsNotNull(string field) {
            return "(" + field + " IS NOT NULL)";
        }

        /// <summary>
        /// Compare current values of key fields of a row
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public override string CmpKey(DataRow R) {
            string res = "";
            if (R == null) return res;
            if (R.Table.PrimaryKey == null) return res;
            if (R.RowState == DataRowState.Deleted) return CmpPrevKey(R);
            foreach (DataColumn C in R.Table.PrimaryKey) {
                res = AppAnd(res, CmpEq(C.ColumnName, R[C.ColumnName]));
            }
            return res;
        }

        /// <summary>
        /// Compare original values of key fields of a row
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public override string CmpPrevKey(DataRow R) {
            string res = "";
            if (R == null) return res;
            if (R.Table.PrimaryKey == null) return res;
            if (R.RowState == DataRowState.Added) return CmpKey(R);
            var DV = DataRowVersion.Original;
            if (R.RowState == DataRowState.Added) DV = DataRowVersion.Original;
            foreach (var C in R.Table.PrimaryKey) {
                res = AppAnd(res, CmpEq(C.ColumnName, R[C.ColumnName, DV]));
            }
            return res;
        }


        /// <summary>
        /// gives ( (field &amp; mask) == val )
        /// </summary>
        /// <param name="field"></param>
        /// <param name="mask"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string CmpMask(string field, UInt64 mask, UInt64 val) {
            if (mask == 0) {
                return CmpEq(field, val);
            }
            val &= mask;
            return "((" + field + " & " + quote(mask) + ")=" + quote(val) + ")";
        }

        /// <summary>
        /// Gives (field between min and max)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public override string Between(string field, object min, object max) {
            //La between pura da problemi con la compilazione delle query se min o max sono date
            //return "(" + field + " BETWEEN " + quote(min) + " AND " + quote(max) + ")";
            return "((" + field + " >= " + quote(min) + ") AND (" + field + "<=" + quote(max) + "))";
        }

        /// <summary>
        /// gives (field like 'val')
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string Like(string field, string val) {
            return "(" + field + " LIKE " + quote(val) + ")";
        }

        /// <summary>
        /// gives (field like 'val')
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string UnquotedLike(string field, string val) {
            return "(" + field + " LIKE " + val + ")";
        }

        /// <summary>
        /// Gives  a codified version of the string field so that it will be used as a string name
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string Field(string field) {
            return "&£$" + field;
        }

        /// <summary>
        /// Gives quoted values of array object, comma separated
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string List(params object[] O) {
            string res = "";
            foreach (object OO in O) {
                if (res != "") res += ",";
                res += quote(OO);
            }
            return res;
        }

    }

    /// <summary>
    /// Helper class to create DataTable queries
    /// </summary>
    public class CQueryHelper : QueryHelper {

        public override string Max(string expr) {
            return $"max({expr})";
		}

        public override string Min(string expr) {
            return $"min({expr})";
        }
        public override string Sustring(string expr, int start, int len) {
            return $"substring({expr},{start},{len})"; //for oracle  substr
        }
        public override string ConvertToVarchar(string expr, int len) {
            return $"convert({expr},'System.String')";
        }
        public override string ConvertToInt(string expr) {
            return $"convert({expr},'System.Int32')";
        }


        /// <summary>
        /// Gets the string (unquoted) representing an object (ex unquotedstrvalue)
        /// </summary>
        /// <param name="O">Object to display in the output string</param>
        /// <param name="T">Base Type of O</param>
        /// <param name="SQL">if true, result can be used for building SQL commands</param>
        /// <returns>String representation of O</returns>
        public override string unquoted(Object O){            
            if (O==null) return "null";
            if (O== DBNull.Value) return "null";
            var typename= O.GetType().Name;
            switch(typename){
                case "String": return O.ToString();
                case "Char": return O.ToString();
                case "Double": {
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;                    
					var s1 = ((Double)O).ToString("f10");
                    if(group != dec) s1= s1.Replace(group,"");

					var pos = s1.IndexOf(dec,0);
					if (pos<0) return s1;
                    s1 = s1.Replace(dec, ".");					
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}
				case "Single": {
                   var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Single)O).ToString("f10");
                    if(group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
					if (pos<0) return s1;
					s1 =  s1.Replace(dec,".");
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}

				case "Decimal":{                    
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
					var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Decimal)O).ToString("f10");
                    if(group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
					if (pos<0) return s1;
                    s1 = s1.Replace(dec, ".");
					var last = s1.Length-1;
					while (s1[last]=='0') last--;
					if (last==pos) 
						s1=s1.Substring(0,pos); //toglie anche il punto
					else 
						s1=s1.Substring(0,last+1); //toglie gli 0 finali
					return s1;
				}
                case "DateTime":{                   
                     return "#"+((DateTime)O).ToString("yyyy-MM-ddTHH:mm:ss.fffffff"
                            , System.Globalization.DateTimeFormatInfo.InvariantInfo) + "#";

                }
                case "Int16": return O.ToString();
                case "Int32": return O.ToString();
				case "Int64": return O.ToString();
                case "UInt16": return O.ToString();
                case "UInt32": return O.ToString();
                case "UInt64": return O.ToString();
                case "Byte[]": 
					//BinaryFormatter BF = new BinaryFormatter();
					var buf = (Byte[])O;
					return mdl_utils.Quoting.ByteArrayToString(buf);
				case "Byte": 
					return O.ToString();
				case "Boolean":
					if ((bool)O == true) return "true";
					return "false";

                default: 
					ErrorLogger.Logger.MarkEvent("Could not find type "+typename);
					return O.ToString();
            }          
        }
         /// <summary>
		/// Gets the quoted representation of an object
		/// </summary>
		/// <param name="O"></param>
		/// <param name="SQL">if true, SQL compatible strings are used</param>
		/// <returns></returns>
		private static string stringQuote(string s){            
			if (s==null) return "null";
			return "'"+s.Replace("'","''")+"'";
		}

        /// <summary>
        /// Quotes an object in order to use it in a query expression string
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string quote(object O) {
            if (O == null) return "null";
            if (O == DBNull.Value) return "null";
            if (O is Boolean) {
                if (true.Equals(O)) return ("(1=1)");
                return ("(1=0)");
            }
                       
            if (O.GetType() != typeof(string)) return unquoted(O);
            string val = O.ToString();
            if (!val.StartsWith("&£$")) return stringQuote(val);
            return val.Substring(3);
        }

        /// <summary>
        /// return isnull(O,def)
        /// </summary>
        /// <param name="O"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public override string IsNullFn(object O, object def) {
            return $"ISNULL({quote(O)},{quote(def)})";
        }

        /// <summary>
        /// Returns 1/1/1
        /// </summary>
        /// <returns></returns>
        public override DateTime SafeMinDate() {
            return new DateTime(1, 1, 1);
        }
       
        /// <summary>
        ///return expression bwtween parenthesis
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override string DoPar(string expression) {
           return QueryCreator.putInPar(expression);
        }

        /// <summary>
        /// returns q1 &amp; q2
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public override string AppAnd(string q1, string q2) {
            return QueryHelper.AppendAND(q1, q2);
        }

        /// <summary>
        /// returns logical and of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string AppAnd(params string[] q) {
            if (q.Length == 1) {
                return q[0];
                //throw new Exception("AppAnd con un parametro solo");
            }

            return mergeWithOperator("AND",q);
            //string res = "";
            //foreach (string qq in q) {
            //    res = GetData.MergeFilters(res, qq);
            //}
            //return res;
        }

        /// <summary>
        /// returns logical and of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseAnd(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseAnd con un parametro solo");
            }
            string res = "";
            foreach (string qq in q) {
                res = mergeWithOperator("&",res, qq);
            }
            return res;
        }

        /// <summary>
        /// returns logical and of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseOr(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseOr con un parametro solo");
            }
            string res = "";
            foreach (string qq in q) {
                res = mergeWithOperator("|",res, qq);
            }
            return res;
        }

         /// <summary>
        /// returns logical and of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string BitwiseXor(params string[] q) {
            if (q.Length == 1) {
                throw new Exception("BitwiseOr con un parametro solo");
            }
            string res = "";
            foreach (string qq in q) {
                res = mergeWithOperator("^",res, qq);
            }
            return res;
        }


        /// <summary>
        /// creates a string: not ( expression )
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override string BitwiseNot(string expression) {
            return DoPar("~" + DoPar(expression));
        }

        /// <summary>
        /// Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string MCmp(DataRow R, params string[] field) {
            return CmpMulti(R, field);
        }

       
        /// <summary>
        /// Alias for CmpMulti
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public override string MCmp(object sample) {
            var operandi= new List<string>();
            string[] ff;
            if (sample is Dictionary<string, object> dict) {
                ff = dict.Keys.ToArray();
            }
            else {
                ff = sample.GetType().GetMembers().Where(f => f.MemberType == MemberTypes.Property).Pick("Name").Cast<string>().ToArray();
            }

            return mergeWithOperator("AND", ff.Map(f => CmpEq(f, MetaExpression.getField(f, sample))));

            //foreach (string f in ff) operandi.Add( CmpEq(f, MetaExpression.getField(f,sample)));
            //return mergeWithOperator("AND",operandi.ToArray());
        }

        /// <summary>
        /// Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string CmpMulti(DataRow R, params string[] fields) {
            var V = DataRowVersion.Default;
            if (R.RowState == DataRowState.Deleted) V = DataRowVersion.Original;
            var operandi= new List<string>();            
            foreach (string f in fields) operandi.Add( CmpEq(f, R[f, V]));
            return mergeWithOperator("AND",operandi.ToArray());
        }


        /// <summary>
        /// Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string CmpMulti(DataRow R, DataColumn[] fields, DataRowVersion ver = DataRowVersion.Default) {
	        if ( R.RowState == DataRowState.Deleted) ver = DataRowVersion.Original;
	        var operandi= new List<string>();            
	        foreach (DataColumn c in fields) operandi.Add( CmpEq(c.ColumnName, R[c.ColumnName, ver]));
	        return mergeWithOperator("AND",operandi.ToArray());
        }

        /// <summary>
        /// returns logical OR of two expressions
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public override string AppOr(string q1, string q2) {
            return QueryHelper.AppendOR(q1, q2);
        }

        /// <summary>
        ///  returns logical OR of a list of expressions
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public override string AppOr(params string[] q) {
            if (q.Length == 1) {
                return q[0];
                //throw new Exception("AppOr con un parametro solo");
            }
           return mergeWithOperator("OR",q);
        }

        /// <summary>
        /// Compares field with unquoted expression O
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpEq(string field, string O) {
            if (O.ToUpper() == "NULL") return $"({field} IS NULL)";
            return $"({field}={O})";
        }

        /// <summary>
        /// Compare field with O
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpEq(string field, object O) {
            if ((O == null) || (O == DBNull.Value)) {
                return "(" + field + " IS NULL)";
            }
            return "(" + field + "=" + quote(O) + ")";
        }

        /// <summary>
        /// returns (field &lt;&gt; value) if value is not null, else (field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpNe(string field, string O) {
            if (O.ToUpper() == "NULL") return $"({field} IS NOT NULL)";
            return $"({field}<>{O})";
        }
        /// <summary>
        /// Returns  (field is not null) if O is null, else (field &lt;&gt; quote(O))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpNe(string field, object O) {
            if ((O == null) || (O == DBNull.Value)) {
                return "(" + field + " IS NOT NULL)";
            }
            return "(" + field + "<>" + quote(O) + ")";
        }



        /// <summary>
        /// Returns (field &gt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpGt(string field, string O) {
            return $"({field}>{O})";
        }

        /// <summary>
        /// Returns (field &gt; quote(O))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpGt(string field, object O) {

            return "(" + field + ">" + quote(O) + ")";
        }

        /// <summary>
        /// Returns (field &gt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpGe(string field, string O) {
            return $"({field}>={O})";
        }

        /// <summary>
        /// Returns (field &gt;= quote(O))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpGe(string field, object O) {

            return "(" + field + ">=" + quote(O) + ")";
        }

        /// <summary>
        /// Returns (field &lt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpLt(string field, string O) {
            return $"({field}<{O})";
        }

        /// <summary>
        /// Returns (field &lt; quote(O))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpLt(string field, object O) {

            return "(" + field + "<" + quote(O) + ")";
        }

        /// <summary>
        /// Returns (field &lt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedCmpLe(string field, string O) {
            return $"({field}<={O})";
        }

        /// <summary>
        /// Returns (field &lt;= quote(O))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string CmpLe(string field, object O) {

            return "(" + field + "<=" + quote(O) + ")";
        }
        /// <summary>
        /// returns (field is null)
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string IsNull(string field) {
            return "(" + field + " IS NULL)";
        }

        /// <summary>
        /// returns (feld is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string IsNotNull(string field) {
            return "(" + field + " IS NOT NULL)";
        }


        /// <summary>
        /// Returns (field is null or field= quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrEq(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpEq(field, O)));
        }
        /// <summary>
        /// Returns (field is null or field &gt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrGt(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpGt(field, O)));
        }
        /// <summary>
        /// Returns (field is null or field &gt;= quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrGe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpGe(field, O)));
        }
        /// <summary>
        /// Returns (field is null or field &lt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrLt(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpLt(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field &lt;= quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrLe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpLe(field, O)));
        }


        /// <summary>
        /// Returns (field is null or field &lt;&gt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string NullOrNe(string field, object O) {
            return DoPar(AppOr(IsNull(field), CmpNe(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrEq(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpEq(field, O)));
        }
        /// <summary>
        /// Returns (field is null or field &gt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrGt(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpGt(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field &gt;= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrGe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpGe(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field &lt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrLt(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpLt(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field &lt;= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrLe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpLe(field, O)));
        }

        /// <summary>
        /// Returns (field is null or field &lt;&gt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedNullOrNe(string field, string O) {
            return DoPar(AppOr(IsNull(field), UnquotedCmpNe(field, O)));
        }


        /// <summary>
        /// Check if Nth bit of field is set
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit">note: first bit is numbered 0</param>
        /// <returns></returns>
        public override string BitSet(string field, int nbit) {
            if (nbit == 0) {
                return "((" + field + " % 2)=1)";
            }
            int SUP = 1 << (nbit + 1);  //Es. 1--> 4
            int INF = 1 << nbit;      //Es. 1-->2           .....> ((field % 4)>=2)
            return "((" + field + " % " + SUP + ")>=" + INF + ")";

        }

        /// <summary>
        /// Check if Nth bit of field is not set
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit">note: first bit is numbered 0</param>
        /// <returns></returns>
        public override string BitClear(string field, int nbit) {
            if (nbit == 0) {
                return "((" + field + " % 2)=0)";
            }
            int SUP = 1 << (nbit + 1);  //Es. 1--> 4
            int INF = 1 << nbit;      //Es. 1-->2           .....> ((field % 4)<2)
            return "((" + field + " % " + SUP + ")<" + INF + ")";
        }


        /// <summary>
        /// returns a list of distinct quoted values of ROWS[column], comma separated
        /// </summary>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string DistinctVal(DataRow[] ROWS, string column) {
            string outstring = "";
            foreach (DataRow R in ROWS) {
                if (R[column] == DBNull.Value) continue;
                string quoted = quote(R[column]);
                if (outstring.IndexOf(quoted) > 0) continue;
                if (outstring != "") outstring += ",";
                outstring += quoted;
            }
            return outstring;
        }

        /// <summary>
        /// returns a list of distinct quoted values of input array, comma separated
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
        public override string DistinctVal(object[] OO) {
            string outstring = "";
            foreach (object O in OO) {
                if (O == DBNull.Value) continue;
                string quoted = quote(O);
                if (outstring.IndexOf(quoted) > 0) continue;
                if (outstring != "") outstring += ",";
                outstring += quoted;
            }
            return outstring;
        }
          /// <summary>
        /// returns a list of distinct quoted values of input array, comma separated
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
        public override string UnquotedDistinctVal(string[] OO) {
            string outstring = "";
            foreach (string O in OO) {
                if (O == null || O=="") continue;
                if (outstring.IndexOf(O) > 0) continue;
                if (outstring != "") outstring += ",";
                outstring += O;
            }
            return outstring;
        }

        /// <summary>
        /// returns (field in (distinct values of O)) if O is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string FieldIn(string field, object[] O) {
            if (O.Length == 1) {
                return  CmpEq(field, O[0]) ;
            }
            string list = DistinctVal(O);
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }

         /// <summary>
        /// returns (field in (distinct values of O)) if O is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedFieldIn(string field, string[] O) {
            if (O.Length == 1) {
                return UnquotedCmpEq(field, O[0]);
            }
            string list = UnquotedDistinctVal(O);
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }

        /// <summary>
        /// returns (field in (distinct values of ROWS[field])) if ROWS is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public override string FieldIn(string field, DataRow[] ROWS) {
            return FieldIn(field, ROWS, field);
        }

        /// <summary>
        /// returns (field in (list))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public override string FieldInList(string field, string list) {
            if (list == "") return "(" + field + " IS NULL AND " + field + " IS NOT NULL)";
            return "(" + field + " IN (" + list + "))";
        }

        /// <summary>
        /// returns (field in (distinct values of ROWS[column])) if ROWS is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string FieldIn(string field, DataRow[] ROWS, string column) {
            if (ROWS.Length==1)return CmpEq(field,ROWS[0][column]);
            string list = DistinctVal(ROWS, column);
            return FieldInList(field, list);
        }


        /// <summary>
        /// returns (field NOT in (distinct values of O)) if O is not empty  else empty string 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, object[] O) {
             if (O.Length == 1) {
                return CmpNe(field, O[0]) ;
            }
            string list = DistinctVal(O);
            if (list == "") return "(NULL IS NULL)";
            return "(" + field + " NOT IN (" + list + "))";
        }

         /// <summary>
        /// returns (field NOT in (distinct values of O)) if O is not empty  else empty string 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string UnquotedFieldNotIn(string field, string[] O) {
             if (O.Length == 1) {
                return UnquotedCmpNe(field, O[0]);
            }
            string list = UnquotedDistinctVal(O);
            if (list == "") return "(NULL IS NULL)";
            return "(" + field + " NOT IN (" + list + "))";
        }

        /// <summary>
        /// returns (field NOT in (distinct values of ROWS[field])) if ROWS is not empty  else empty string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, DataRow[] ROWS) {
            return FieldNotIn(field, ROWS, field);
        }

        /// <summary>
        /// returns (field NOT in (list))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public override string FieldNotInList(string field, string list) {
            if (list == "") return "";
            return "(" + field + " NOT IN (" + list + "))";
        }

        /// <summary>
        /// returns (field NOT in (distinct values of ROWS[column])) if ROWS is not empty  else empty string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override string FieldNotIn(string field, DataRow[] ROWS, string column) {
            if (ROWS.Length==1)return CmpNe(field,ROWS[0][column]);
            string list = DistinctVal(ROWS, column);
            return FieldNotInList(field, list);
        }

        /// <summary>
        /// returns NOT(expression)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override string Not(string expression) {
            return DoPar("NOT " + DoPar(expression));
        }




        /// <summary>
        /// Compares key fields of R
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public override string CmpKey(DataRow R) {
            string res = "";
            if (R == null) return res;
            if (R.Table.PrimaryKey == null) return res;
            if (R.RowState == DataRowState.Deleted) return CmpPrevKey(R);
            foreach (DataColumn C in R.Table.PrimaryKey) {
                res = AppAnd(res, CmpEq(C.ColumnName, R[C.ColumnName]));
            }
            return res;
        }

        /// <summary>
        /// Compare original values of R key fields
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public override string CmpPrevKey(DataRow R) {
            string res = "";
            if (R == null) return res;
            if (R.Table.PrimaryKey == null) return res;
            if (R.RowState == DataRowState.Added) return CmpKey(R);
            DataRowVersion DV = DataRowVersion.Original;
            if (R.RowState == DataRowState.Added) DV = DataRowVersion.Original;
            foreach (DataColumn C in R.Table.PrimaryKey) {
                res = AppAnd(res, CmpEq(C.ColumnName, R[C.ColumnName, DV]));
            }
            return res;
        }

        /// <summary>
        /// Compares field with a mask (field &amp;mask == val)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="mask"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string CmpMask(string field, UInt64 mask, UInt64 val) {
            if (mask == 0) {
                return CmpEq(field, val);
            }

            string res = "";
            int i = 0;
            while (mask != 0) {
                if ((mask & 1) != 0) {
                    if ((val & 1) != 0) {
                        res = AppAnd(res, BitSet(field, i));
                    }
                    else {
                        res = AppAnd(res, BitClear(field, i));
                    }
                }
                mask >>= 1;
                val >>= 1;
                i++;
            }
            return res;
        }

        /// <summary>
        /// returns (field between min and max)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public override string Between(string field, object min, object max) {
            return "(" + field + " >=" + quote(min) + " AND " + field + "<=" + quote(max) + ")";
            //   return "(" + field + " BETWEEN " + quote(min) + " AND " + quote(max) + ")";
        }

        /// <summary>
        /// Returns (field like quote(val))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string Like(string field, string val) {
            return "(" + field + " LIKE " + quote(val) + ")";
        }

        /// <summary>
        /// Compares field with unquoted expression O
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string UnquotedLike(string field, string val) {
            return $"({field} LIKE {val})";
        }



        /// <summary>
        /// Returns a codified string that will become the original string when quoted
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string Field(string field) {
            return "&£$" + field;
        }


        /// <summary>
        /// Returns a comma separated list of quoted objects
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public override string List(params object[] O) {
            string res = "";
            foreach (object OO in O) {
                if (res != "") res += ",";
                res += quote(OO);
            }
            return res;
        }

		public override string ReturnValueIfNoRowAffected(int value) => throw new NotImplementedException();
	}

    /// <summary>
    /// Abstract class for query making
    /// </summary>
    public abstract class QueryHelper {
        /// <summary>
        /// Quotes an object in order to use it in a query expression string
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string quote(object O);

        /// <summary>
        /// Returns a string representation of an object, without quoting constants 
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string unquoted(object O);

        /// <summary>
        /// return expression bwtween parenthesis
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public abstract string DoPar(string S);

        /// <summary>
        ///  returns q1 &amp; q2
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public abstract string AppAnd(string q1, string q2);

        /// <summary>
        ///  returns q1 &amp; q2
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public abstract string BitwiseAnd(params string[] q);

        /// <summary>
        ///  returns q1 &#124; q2
        /// </summary>
         /// <param name="q"></param>
        /// <returns></returns>
        public abstract string BitwiseOr(params string[] q);

          /// <summary>
        ///  returns q1 &#94; q2
        /// </summary>
         /// <param name="q"></param>
        /// <returns></returns>
        public abstract string BitwiseXor(params string[] q);

        /// <summary>
        ///  returns (bit) not q
        /// </summary>
         /// <param name="q"></param>
        /// <returns></returns>
        public abstract string BitwiseNot(string q);

        /// <summary>
        /// returns logical AND of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public abstract string AppAnd(params string[] q);

        /// <summary>
        /// returns logical OR of two expressions
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public abstract string AppOr(string q1, string q2);

        /// <summary>
        /// returns logical OR of an expression list
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public abstract string AppOr(params string[] q);


        /// <summary>
        /// returns (field &lt;&gt; quote(value)) if value is not null, else (field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpNe(string field, object O);


        /// <summary>
        /// returns (field = quote(value)) if value is not null, else (field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpEq(string field, object O);

        /// <summary>
        /// returns (field &lt; quote(value)) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpLt(string field, object O);

        /// <summary>
        /// returns (field &lt;= quote(value)) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpLe(string field, object O);

        /// <summary>
        ///  returns (field &gt; quote(value)) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpGt(string field, object O);

        /// <summary>
        /// returns (field &gt;= quote(value)) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string CmpGe(string field, object O);

        /// <summary>
        /// returns (field &lt;&gt; value) if value is not null, else (field is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpNe(string field, string O);

        /// <summary>
        /// returns (field = value) if value is not null, else (field is null)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpEq(string field, string O);


        /// <summary>
        /// Returns (field &lt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpLt(string field, string O);
        /// <summary>
        ///  Returns (field &lt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpLe(string field, string O);

        /// <summary>
        /// Returns (field &gt; O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpGt(string field, string O);

        /// <summary>
        /// Returns (field &gt;= O)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedCmpGe(string field, string O);

        /// <summary>
        /// returns not (expression)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract string Not(string expression);

        /// <summary>
        /// returns (field is null)
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public abstract string IsNull(string field);


        /// <summary>
        /// Returns (field is null or field &gt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrEq(string field, object O);


        /// <summary>
        ///  Returns (field is null or field &gt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrGt(string field, object O);


        /// <summary>
        ///  Returns (field is null or field &gt;= quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrGe(string field, object O);

        /// <summary>
        /// Returns (field is null or field &lt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrLt(string field, object O);

        /// <summary>
        /// Returns (field is null or field &lt;= quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrLe(string field, object O);

        /// <summary>
        /// Returns (field is null or field &lt;&gt; quoted(value))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string NullOrNe(string field, object O);


        /// <summary>
        /// Returns (field is null or field= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrEq(string field, string O);


        /// <summary>
        ///  Returns (field is null or field &gt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrGt(string field, string O);

        /// <summary>
        ///  Returns (field is null or field &gt;= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrGe(string field, string O);

        /// <summary>
        /// Returns (field is null or field &lt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrLt(string field, string O);

        /// <summary>
        /// Returns (field is null or field &lt;= value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrLe(string field, string O);

        /// <summary>
        ///  Returns (field is null or field &lt;&gt; value)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedNullOrNe(string field, string O);

        /// <summary>
        ///  Check if Nth bit of field is set
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit"></param>
        /// <returns></returns>
        public abstract string BitSet(string field, int nbit);

        /// <summary>
        ///  Check if Nth bit of field is NOT set
        /// </summary>
        /// <param name="field"></param>
        /// <param name="nbit"></param>
        /// <returns></returns>
        public abstract string BitClear(string field, int nbit);

        /// <summary>
        /// returns a list of distinct quoted values of ROWS[column], comma separated
        /// </summary>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public abstract string DistinctVal(DataRow[] ROWS, string column);

        /// <summary>
        /// returns a list of distinct quoted values of input array, comma separated
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
        public abstract string DistinctVal(object[] OO);


        /// <summary>
        /// returns a list of distinct values of input array, comma separated
        /// </summary>
        /// <param name="OO"></param>
        /// <returns></returns>
         public abstract string UnquotedDistinctVal(string[] OO);

        /// <summary>
        ///  returns (field in (list))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public abstract string FieldInList(string field, string list);


        /// <summary>
        /// returns (field in (distinct values of O)) if O is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string FieldIn(string field, object[] O);

         /// <summary>
        /// returns (field in (distinct values of O)) if O is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedFieldIn(string field, string[] O);


        /// <summary>
        /// returns (field in (distinct values of ROWS[column])) if ROWS is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public abstract string FieldIn(string field, DataRow[] ROWS, string column);

        /// <summary>
        /// returns (field in (distinct values of ROWS[field])) if ROWS is not empty  else (false) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public abstract string FieldIn(string field, DataRow[] ROWS);

        /// <summary>
        /// returns (field NOT in (list)) 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public abstract string FieldNotInList(string field, string list);

        /// <summary>
        /// returns (field NOT in (distinct values of O)) if O is not empty  else empty string 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string FieldNotIn(string field, object[] O);

         /// <summary>
        /// returns (field NOT in (distinct values of O)) if O is not empty  else empty string 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string UnquotedFieldNotIn(string field, string[] O);

        /// <summary>
        /// returns (field NOT in (distinct values of ROWS[column])) if ROWS is not empty  else empty string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public abstract string FieldNotIn(string field, DataRow[] ROWS, string column);

        /// <summary>
        /// returns (field NOT in (distinct values of ROWS[field])) if ROWS is not empty  else empty string
        /// </summary>
        /// <param name="field"></param>
        /// <param name="ROWS"></param>
        /// <returns></returns>
        public abstract string FieldNotIn(string field, DataRow[] ROWS);

        /// <summary>
        /// Compares key fields of R
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public abstract string CmpKey(DataRow R);


        /// <summary>
        /// Compare original values of R key fields
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public abstract string CmpPrevKey(DataRow R);


        /// <summary>
        /// returns (feld is not null)
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public abstract string IsNotNull(string field);
        /// <summary>
        /// Compares field with a mask (field &amp;mask == val)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="mask"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract string CmpMask(string field, UInt64 mask, UInt64 val);

        /// <summary>
        ///  Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public abstract string CmpMulti(DataRow R, params string[] fields);

        public abstract string CmpMulti(DataRow R, DataColumn[] fields, DataRowVersion ver); 

        /// <summary>
        /// Compares n fields of a row  (field[0]= R[field[0] &amp; field[0]= R[field[0] &amp;.. )
        /// </summary>
        /// <param name="R"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public abstract string MCmp(DataRow R, params string[] fields);

        /// <summary>
        /// Compares all fields of sample
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public abstract string MCmp(object sample);


        /// <summary>
        /// returns (field between min and max)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public abstract string Between(string field, object min, object max);

        /// <summary>
        /// Returns (field like quote(val))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract string Like(string field, string val);

        /// <summary>
        /// Returns (field like val)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract string UnquotedLike(string field, string val);


        /// <summary>
        /// Returns a codified string that will become the original string when quoted
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public abstract string Field(string field);

        /// <summary>
        ///  Returns a comma separated list of quoted objects
        /// </summary>
        /// <param name="O"></param>
        /// <returns></returns>
        public abstract string List(params object[] O);

        /// <summary>
        /// return isnull(O,def)
        /// </summary>
        /// <param name="O"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public abstract string IsNullFn(object O, object def);

        /// <summary>
        /// Returns a very early date, generally 1/1/1
        /// </summary>
        /// <returns></returns>
        public abstract DateTime SafeMinDate();

        public abstract string Max(string expr);
        public abstract string Min(string expr);
        public abstract string Sustring(string expr,int start, int stop);
        public abstract string ConvertToVarchar(string expr, int len);
        public abstract string ConvertToInt(string expr);

        public abstract string ReturnValueIfNoRowAffected(int value);

        /// <summary>
		/// Merges two filters (AND) without throwing exception if some or 
		///		both are null
		/// </summary>
		/// <param name="Filter1"></param>
		/// <param name="Filter2"></param>
		/// <returns></returns>
		public static string AppendAND(string Filter1, string Filter2) {
            if ((Filter1 == "") || (Filter1 == null))
                return Filter2;
            if ((Filter2 == "") || (Filter2 == null))
                return Filter1;
            return QueryCreator.putInPar(Filter1) + "AND" + QueryCreator.putInPar(Filter2);

        }
        /// <summary>
        /// Merge two condition A and B as  (A) OR (B)
        /// </summary>
        /// <param name="Filter1"></param>
        /// <param name="Filter2"></param>
        /// <returns></returns>
        public static string AppendOR(string Filter1, string Filter2) {
            if ((Filter1 == "") || (Filter1 == null))
                return Filter2;
            if ((Filter2 == "") || (Filter2 == null))
                return Filter1;
            return QueryCreator.putInPar(Filter1) + "OR" + QueryCreator.putInPar(Filter2);
        }


        /// <summary>
        /// Merges two filters (AND) without throwing exception if some or 
        ///		both are null
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public static string mergeWithOperator(string op, params string []filter ){
	        if (filter==null || filter.Length==0)			return "";
	        if (filter.Length==1)return filter[0];
	        string res="";
	        foreach(string operando in filter) {
		        if ((operando == null) || (operando == "")) continue;
		        if ((operando.Trim()=="")) continue;
		        if (res!="")res+=op;
		        res+=QueryCreator.putInPar(operando);
	        }
	        return "("+res+")";

        }

    }
}
