using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mdl_utils {
    public class Quoting {
        /// <summary>
		/// Gets the quoted representation of an object
		/// </summary>
		/// <param name="O"></param>
		/// <param name="SQL">if true, SQL compatible strings are used</param>
		/// <returns></returns>
		public static string quote(Object O, bool SQL=false) {
            if (O == null) return "null";
            if (O is Boolean) {
                if (true.Equals(O)) return ("(1=1)");
                return ("(1=0)");
            }

            return quote(O, O.GetType(), SQL);
        }



        /// <summary>
        /// Gets the string (unquoted) representing an object
        /// </summary>
        /// <param name="O"></param>
        /// <param name="SQL">if true, SQL compatible representation are used</param>
        /// <returns></returns>
        public static string unquote(Object O, bool SQL=false) {
            if (O == null) return "null";
            if (O == DBNull.Value) return "null";
            return unquoted(O, O.GetType(), SQL);
        }


        /// <summary>
        /// Returns a string quoted, in order to be included in a sql command
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        static string quoteString(string S) {
            if (S == null) return "null";
            return "'" + S.Replace("'", "''") + "'";
        }
        /// <summary>
        /// Gets a quoted representation of O, or "null" (unquoted) if O==null/DBNull
        /// </summary>
        /// <param name="O"></param>
        /// <param name="T"></param>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public static string quote(Object O, System.Type T, bool SQL) {
            if (O == null) return "null";
            if (O == DBNull.Value) return "null";
            if (T.Name == "DateTime") {
                return unquoted(O, T, SQL);
            }
            if ((T.Name == "Byte[]") && SQL) {
                return unquoted(O, T, SQL);
            }
            return quoteString(unquoted(O, T, SQL));
        }


        /// <summary>
        /// Build a string that represents the Object O of type T.
        /// </summary>
        /// <param name="O">Object to display in the output string</param>
        /// <param name="T">Base Type of O</param>
        /// <param name="SQL">if true, result can be used for building SQL commands</param>
        /// <returns>String representation of O</returns>
        public static string unquoted(Object O, System.Type T, bool SQL) {
            if (O == null) return "null";
            if (O == DBNull.Value) return "null";
            var typename= T.Name;
            switch (typename) {
                case "String": return O.ToString();
                case "Char": return O.ToString();
                case "Double": {

                    //if(!SQL) {
                    //    return ((Double)O).ToString("R");
                    //}
                    //                    string group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
                    //                    string dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    //                    string s1 = ((Double)O).ToString("r").Replace(group,"");
                    //                    return s1.Replace(dec,".");
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
                    var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Double)O).ToString("f10");
                    if (group != dec) s1 = s1.Replace(group, "");

                    var pos = s1.IndexOf(dec,0);
                    if (pos < 0) return s1;
                    s1 = s1.Replace(dec, ".");
                    var last = s1.Length-1;
                    while (s1[last] == '0') last--;
                    if (last == pos)
                        s1 = s1.Substring(0, pos); //toglie anche il punto
                    else
                        s1 = s1.Substring(0, last + 1); //toglie gli 0 finali
                    return s1;
                }
                case "Single": {
                    //if(!SQL) {
                    //    return ((Single)O).ToString("r");
                    //}
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
                    var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Single)O).ToString("f10");
                    if (group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
                    if (pos < 0) return s1;
                    s1 = s1.Replace(dec, ".");
                    var last = s1.Length-1;
                    while (s1[last] == '0') last--;
                    if (last == pos)
                        s1 = s1.Substring(0, pos); //toglie anche il punto
                    else
                        s1 = s1.Substring(0, last + 1); //toglie gli 0 finali
                    return s1;
                }

                case "Decimal": {
                    //if (!SQL){
                    //    return ((Decimal)O).ToString("n");
                    //}
                    var group = System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
                    var dec   = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                    var s1 = ((Decimal)O).ToString("f10");
                    if (group != dec) s1 = s1.Replace(group, "");
                    var pos = s1.IndexOf(dec, 0);
                    if (pos < 0) return s1;
                    s1 = s1.Replace(dec, ".");
                    var last = s1.Length-1;
                    while (s1[last] == '0') last--;
                    if (last == pos)
                        s1 = s1.Substring(0, pos); //toglie anche il punto
                    else
                        s1 = s1.Substring(0, last + 1); //toglie gli 0 finali
                    return s1;
                }
                case "DateTime": {
                    if (!SQL) {
                        return "#" + ((DateTime)O).ToString("yyyy-MM-ddTHH:mm:ss.fffffff"
                            , System.Globalization.DateTimeFormatInfo.InvariantInfo) + "#";
                    }
                    var TT = (DateTime) O; //Convert.ToDateTime(s);   
                    if (TT.Date.Equals(TT)) {
                        return "{d '" + TT.Year.ToString() + "-" +
                               TT.Month.ToString().PadLeft(2, '0') + "-" +
                               TT.Day.ToString().PadLeft(2, '0') +
                               "'}";
                    }
                    return "{ts '" + TT.Year.ToString() + "-" +
                        TT.Month.ToString().PadLeft(2, '0') + "-" +
                        TT.Day.ToString().PadLeft(2, '0') + " " +
                        TT.Hour.ToString().PadLeft(2, '0') + ":" +
                        TT.Minute.ToString().PadLeft(2, '0') + ":" +
                        TT.Second.ToString().PadLeft(2, '0') + "." +
                        TT.Millisecond.ToString().PadLeft(3, '0') +
                        "'}";

                    //					return TT.Year.ToString()+"-"+
                    //						TT.Month.ToString().PadLeft(2,'0')+"-"+
                    //						TT.Day.ToString().PadLeft(2,'0')+" "+
                    //						TT.Hour.ToString().PadLeft(2,'0')+":"+
                    //						TT.Minute.ToString().PadLeft(2,'0')+":"+
                    //						TT.Second.ToString().PadLeft(2,'0')+"."+
                    //						TT.Millisecond.ToString().PadLeft(3,'0');


                    //                    return TT.Month.ToString()+ "/"+ TT.Day.ToString() + "/" +
                    //                        TT.Year.ToString()+" "+TT.Hour.ToString()+":"+
                    //                        TT.Minute.ToString()+":"+TT.Second.ToString()+"."+
                    //                        TT.Millisecond.ToString().PadLeft(3,'0');
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
                    return ByteArrayToString(buf);
                case "Byte":
                    return O.ToString();
                case "Boolean":
                    if ((Boolean)O == true) return "true";
                    return "false";

                default:
                    //ErrorLogger.Logger.markEvent("Could not find type " + typename);
                    return O.ToString();
            }
        }

        /// <summary>
        /// Converts a byte array into a string (something like base64)
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static string ByteArrayToString(Byte[] buf) {
            var ff= new StringBuilder();
            ff.Append("0x");
            var temp=new StringBuilder();
            for (var i = 0; i < buf.Length; i++) {
                temp.Append(ByteTohex(buf[i]));
                if (temp.Length > 1024) {
                    ff.Append(temp.ToString());
                    temp = new StringBuilder();
                }
            }
            ff.Append(temp);
            return ff.ToString();
        }


        /// <summary>
        /// Converts a string back to a byte array
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
		public static Byte[] StringToByteArray(string S) {
            var i=2;
            var len=S.Length;
            if (i >= len) return new byte[0];
            var buf = new byte[(len-i)/2];
            var index=0;
            while (i <= len - 2) {
                var xx= S.Substring(i,2);
                buf[index++] = HexToByte(xx);
                i += 2;
            }
            return buf;
        }

        static string ByteTohex(Byte B) {
            return IntToHexDigit(B >> 4) + IntToHexDigit(B & 0x0F);
        }

        static Byte HexToByte(string S) {
            return Convert.ToByte(Convert.ToByte((CharToByte(S[0]) << 4)) + CharToByte(S[1]));
        }


        static string IntToHexDigit(int x) {
            switch (x) {
                case 0: return "0";
                case 1: return "1";
                case 2: return "2";
                case 3: return "3";
                case 4: return "4";
                case 5: return "5";
                case 6: return "6";
                case 7: return "7";
                case 8: return "8";
                case 9: return "9";
                case 10: return "A";
                case 11: return "B";
                case 12: return "C";
                case 13: return "D";
                case 14: return "E";
                case 15: return "F";
            }
            return "x";
        }

        static Byte CharToByte(char C) {
            C = Char.ToUpper(C);
            switch (C) {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
            }
            return 0;
        }
    }
}
