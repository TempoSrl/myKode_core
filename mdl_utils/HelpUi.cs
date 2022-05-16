using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mdl_utils.tagUtils;

namespace mdl_utils {
    //

    //
    // Summary:
    //     Specifies how an object or text in a control is horizontally aligned relative
    //     to an element of the control.
    public enum HorizontalAlignment {
        //
        // Summary:
        //     The object or text is aligned on the left of the control element.
        Left = 0,
        //
        // Summary:
        //     The object or text is aligned on the right of the control element.
        Right = 1,
        //
        // Summary:
        //     The object or text is aligned in the center of the control element.
        Center = 2
    }

    public static class HelpUi {

       

        /// <summary>
        /// Internal representation of an "Empty Date". It is used to store 
        ///  "blank" dates where DateTime field does not allow null
        /// </summary>
        /// <returns></returns>
        public static DateTime EmptyDate() {
            return new DateTime(1000, 1, 1);
        }


        public static bool isStandardNumericFormatStyle(string fmt) {
            switch (fmt) {
                case "n":
                    return true;
                case "c":
                    return true;
                case "d":
                    return true;
                case "e":
                    return true;
                case "f":
                    return true;
                case "g":
                    return true;
                case "x":
                    return true;
                case "p":
                    return true;
                default:
                    return false;
            }
        }

        public static NumberStyles getNumberStyles(string fmt) {
            switch (fmt) {
                case "n":
                    return NumberStyles.Number;
                case "c":
                    return NumberStyles.Currency;
                case "d":
                    return NumberStyles.Integer;
                case "e":
                    return NumberStyles.Float;
                case "f":
                    return NumberStyles.Float;
                case "g":
                    return NumberStyles.Any;
                case "x":
                    return NumberStyles.HexNumber;
                case "p":
                    return NumberStyles.Number;
            }
            return NumberStyles.Any;
        }


        public static bool isStandardDateFormatStyle(string fmt) {
            switch (fmt) {
                case "d":
                    return true; //short date format.
                case "D":
                    return true; //long date format
                case "t":
                    return true; //time using the 24-hour format
                case "T":
                    return true; //long time format
                case "f":
                    return true; // long date and short time 
                case "F":
                    return true; //  long date and long time 
                case "g":
                    return true; //  short date and short time
                case "G":
                    return true; //4/3/93 05:34 PM.
                case "m":
                    return true; // month and the day of a date
                case "M":
                    return true; // month and the day of a date
                case "r":
                    return true; // date and time as Greenwich Mean Time (GMT)
                case "R":
                    return true; // date and time as Greenwich Mean Time (GMT)
                case "s":
                    return true; // date and time as a sortable index.
                case "u":
                    return true; // date and time as a GMT sortable index
                case "U":
                    return true; // date and time with the long date and long time as GMT.
                case "y":
                    return true; // year and month.
                case "Y":
                    return true; // year and month.
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if format only displays time
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public static bool IsOnlyTimeStyle(string fmt) {
            switch (fmt) {
                case "d":
                    return false; //short date format.
                case "D":
                    return false; //long date format
                case "t":
                    return true; //time using the 24-hour format
                case "T":
                    return true; //long time format
                case "f":
                    return false; // long date and short time 
                case "F":
                    return false; //  long date and long time 
                case "g":
                    return false; //  short date and short time
                case "G":
                    return false; //4/3/93 05:34 PM.
                case "m":
                    return false; // month and the day of a date
                case "M":
                    return false; // month and the day of a date
                case "r":
                    return false; // date and time as Greenwich Mean Time (GMT)
                case "R":
                    return false; // date and time as Greenwich Mean Time (GMT)
                case "s":
                    return false; // date and time as a sortable index.
                case "u":
                    return false; // date and time as a GMT sortable index
                case "U":
                    return false; // date and time with the long date and long time as GMT.
                case "y":
                    return false; // year and month.
                case "Y":
                    return false; // year and month.
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets an object given a string representation of it, and a format "tag"
        /// </summary>
        /// <param name="T"></param>
        /// <param name="S"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static object GetObjectFromString(System.Type T, string S, string tag) {
            if (S == null) S = "";
            string FieldType = GetFieldLower(tag, 2);
            if (FieldType == "") FieldType = null;
            string tname = T.Name;
            try {
                switch (tname) {
                    case "String":
                        return S;
                    case "Char": {
                        Char C = Convert.ToChar(S);
                        return C;
                    }
                    case "Double": {
                        Double D1 = 0;
                        if (S == "") return DBNull.Value;
                        if (FieldType == null) {
                            D1 = double.Parse(S, System.Globalization.NumberStyles.Number, NumberFormatInfo.CurrentInfo);
                            //Convert.ToDouble(S);                
                            return D1;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            double StdDbl = double.Parse(S, getNumberStyles(FieldType), NumberFormatInfo.CurrentInfo);
                            return StdDbl;
                        }
                        if (FieldType == "fixed") {
                            string sdec = GetFieldLower(tag, 3);
                            string prefix = GetFieldLower(tag, 4);
                            if (prefix == null) prefix = "";
                            string suffix = GetFieldLower(tag, 5);
                            if (suffix == null) suffix = "";
                            if (prefix != "") S = S.Replace(prefix, "");
                            if (suffix != "") S = S.Replace(suffix, "");
                            int dec = Convert.ToInt32(sdec);
                            var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                            myFormat.NumberDecimalDigits = dec;
                            string sscale = GetFieldLower(tag, 6);
                            if (sscale == "") sscale = null;
                            D1 = double.Parse(S, NumberStyles.Number, myFormat);
                            var NN = new NumberFormatInfo();


                            double scale = 0;
                            if (sscale != null) scale = Convert.ToDouble(sscale);
                            if (scale != 0) D1 /= scale;
                            return D1;
                        }
                        return null;
                    }
                    case "Single": {
                        float D4;
                        if (S == "") return DBNull.Value;
                        if (FieldType == null) {
                            D4 = float.Parse(S, NumberStyles.Number, NumberFormatInfo.CurrentInfo);
                            //Convert.ToSingle(S);                
                            return D4;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            var StdSngl = Single.Parse(S, getNumberStyles(FieldType), NumberFormatInfo.CurrentInfo);
                            return StdSngl;
                        }
                        if (FieldType == "fixed") {
                            string sdec = GetFieldLower(tag, 3);
                            string prefix = GetFieldLower(tag, 4);
                            if (prefix == null) prefix = "";
                            string suffix = GetFieldLower(tag, 5);
                            if (suffix == null) suffix = "";
                            if (prefix != "") S = S.Replace(prefix, "");
                            if (suffix != "") S = S.Replace(suffix, "");
                            int dec = Convert.ToInt32(sdec);
                            var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                            myFormat.NumberDecimalDigits = dec;
                            string sscale = GetFieldLower(tag, 6);
                            if (sscale == "") sscale = null;
                            D4 = Single.Parse(S, NumberStyles.Number, myFormat);
                            Single scale = 0;
                            if (sscale != null) scale = Convert.ToSingle(sscale);
                            if (scale != 0) D4 /= scale;
                            return D4;
                        }

                        return null;
                    }
                    case "Decimal": {
                        if (S == "") return DBNull.Value;
                        decimal D2 = 0;
                        if (FieldType == null) {
                            D2 = decimal.Parse(S, NumberStyles.Currency, NumberFormatInfo.CurrentInfo);
                            return D2;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            var StdDec = decimal.Parse(S, getNumberStyles(FieldType), NumberFormatInfo.CurrentInfo);
                            return StdDec;
                        }

                        if (FieldType == "fixed") {
                            string sdec = GetFieldLower(tag, 3);
                            string prefix = GetFieldLower(tag, 4);
                            if (prefix == null) prefix = "";
                            string suffix = GetFieldLower(tag, 5);
                            if (suffix == null) suffix = "";
                            if (prefix != "") S = S.Replace(prefix, "");
                            if (suffix != "") S = S.Replace(suffix, "");
                            int dec = Convert.ToInt32(sdec);
                            var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                            myFormat.NumberDecimalDigits = dec;
                            string sscale = GetFieldLower(tag, 6);
                            if (sscale == "") sscale = null;
                            D2 = decimal.Parse(S, NumberStyles.Number, myFormat);
                            Decimal scale = 0;
                            if (sscale != null) scale = Convert.ToDecimal(sscale);
                            if (scale != 0) D2 /= scale;
                            return D2;
                        }
                        return null;
                    }
                    case "DateTime":
                        if (S == "") return DBNull.Value;
                        DateTime TT;
                        if (isStandardDateFormatStyle(FieldType)) {
                            TT = DateTime.Parse(S);
                            return TT;
                        }
                        TT = Convert.ToDateTime(S);
                        return TT;

                    case "Byte":
                        if (S == "") return DBNull.Value;
                        short I11 = Convert.ToByte(S);
                        if (FieldType == null) {
                            return I11;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            I11 = Byte.Parse(S, System.Globalization.NumberStyles.Currency, NumberFormatInfo.CurrentInfo);
                            return I11;
                        }
                        return null;

                    case "Int16":
                        if (S == "") return DBNull.Value;
                        short I1 = Convert.ToInt16(S);
                        if (FieldType == null) {
                            return I1;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            I1 = short.Parse(S, NumberStyles.Currency, NumberFormatInfo.CurrentInfo);
                            return I1;
                        }
                        if (FieldType == "year") {
                            short Year = Convert.ToInt16(DateTime.Now.Year);
                            if (I1 >= Year - 100 && I1 < Year + 100) return I1;
                            short group = (I1 < 100) ? (short)100 : (short)1000;
                            short half = (I1 < 100) ? (short)50 : (short)500;
                            short aa = Convert.ToInt16( Year % group);
                            short CC = Convert.ToInt16(Year - aa);  //centenario esatto o millenario a seconda di I1
                            I1 = Convert.ToInt16(I1 + CC);
                            if (I1 > Year + half) I1 -= group;
                            if (I1 < Year - half) I1 += group;
                            return I1;
                        }
                        return null;
                    case "Int32": {
                        if (S == "") return DBNull.Value;
                        int I2 = Convert.ToInt32(S);
                        if (FieldType == null) {
                            return I2;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            I2 = Int32.Parse(S, NumberStyles.Currency, NumberFormatInfo.CurrentInfo);
                            return I2;
                        }

                        if (FieldType == "year") {
                            int Year = DateTime.Now.Year;
                            if (I2 >= Year - 100 && I2 < Year + 100) return I2;
                            int group = (I2 < 100) ? 100:1000;
                            int half = (I2 < 100) ? 50 : 500;

                            int aa = Year % group;
                            int CC = Year - aa;
                            I2 += CC;
                            if (I2 > Year + half) I2 -= group;
                            if (I2 < Year - half) I2 += group;
                            return I2;
                        }
                        return null;
                    }
                    case "Int64": {
                        if (S == "") return DBNull.Value;
                        Int64 I2 = Convert.ToInt64(S);
                        if (FieldType == null) {
                            return I2;
                        }
                        if (isStandardNumericFormatStyle(FieldType)) {
                            I2 = Int64.Parse(S, NumberStyles.Currency, NumberFormatInfo.CurrentInfo);
                            return I2;
                        }

                        if (FieldType == "year") {
                            int Year = DateTime.Now.Year;
                            if (I2 >= Year - 100 && I2 < Year + 100) return I2;
                            int group = (I2 < 100) ? 100:1000;
                            int half = (I2 < 100) ? 50 : 500;

                            int aa = Year % group;
                            int CC = Year - aa;
                            I2 += CC;
                            if (I2 > Year + half) I2 -= group;
                            if (I2 < Year - half) I2 += group;
                            return I2;
                        }
                        return null;
                    }
                    default:
                        return "'" + S + "'";

                }
            }
            catch (Exception) {
                //string msg = $"Error {QueryCreator.GetErrorString(E)} converting {S} into a {T.Name}";
                //MarkEvent(msg);
                return null;
            }
        }


        /// <summary>
        /// Returns a String representation of an Object O, given a format tag and
        ///  a DataColumn stroing eventually other format properties
        /// </summary>
        /// <param name="O"></param>
        /// <param name="tag"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static string StringValue(Object O, string tag, DataColumn C) {
            if ((O == null) || (O == DBNull.Value)) return "";
            if (C != null) tag = CompleteTag(tag, C);
            return StringValue(O, tag);
        }



        /// <summary>
        /// Gives the string representation of an object
        /// </summary>
        /// <param name="O"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string StringValue(Object O, string tag) {
            if ((O == null) || (O == DBNull.Value)) return "";
            string typename = O.GetType().Name;
            //if (typename=="String") return O.ToString();
            if (typename == "DateTime") {
                var dt = (DateTime) O;
                if (dt.Equals(EmptyDate()) || dt.Year < 1800) {
                    return "";
                }
                else {
                    var fieldType = GetFieldLower(tag, 2);
                    if (isStandardDateFormatStyle(fieldType)) {
                        if (fieldType == "g") return dt.ToShortDateString() + " " + dt.ToString("HH:mm");
                        return dt.ToString(fieldType);
                    }
                    return dt.ToShortDateString();
                }
            }
            else {
                string fieldType = GetFieldLower(tag, 2);
                if (O.GetType().Name == "Decimal") {
                    var d = (decimal) O;
                    if (fieldType == null) {
                        return d.ToString("c");
                    }
                    if (isStandardNumericFormatStyle(fieldType)) {
                        return d.ToString(fieldType);
                    }
                    if (fieldType == "fixed") {
                        string sdec = GetFieldLower(tag, 3);
                        string prefix = GetFieldLower(tag, 4) ?? "";
                        string suffix = GetFieldLower(tag, 5) ?? "";
                        var dec = Convert.ToInt32(sdec);
                        var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                        myFormat.NumberDecimalDigits = dec;
                        var sscale = GetFieldLower(tag, 6);
                        if (sscale == "") sscale = null;
                        decimal scale = 0;
                        if (sscale != null) scale = Convert.ToDecimal(sscale);
                        if (scale != 0) d *= scale;

                        string news = d.ToString("n", myFormat);
                        if (prefix != "") news = prefix + " " + news;
                        if (suffix != "") news = news + " " + suffix;
                        return news;
                    }


                }
                if (O.GetType().Name == "Double") {
                    double D2 = (double) O;
                    if (fieldType == null) {
                        return D2.ToString("n");
                    }
                    if (isStandardNumericFormatStyle(fieldType)) {
                        return D2.ToString(fieldType);
                    }
                    if (fieldType == "fixed") {
                        string sdec = GetFieldLower(tag, 3);
                        string prefix = GetFieldLower(tag, 4);
                        if (prefix == null) prefix = "";
                        string suffix = GetFieldLower(tag, 5);
                        if (suffix == null) suffix = "";
                        int dec = Convert.ToInt32(sdec);
                        var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                        myFormat.NumberDecimalDigits = dec;
                        string sscale = GetFieldLower(tag, 6);
                        if (sscale == "") sscale = null;
                        double scale = 0;
                        if (sscale != null) scale = Convert.ToDouble(sscale);
                        if (scale != 0) D2 *= scale;
                        string news = D2.ToString("n", myFormat);
                        if (prefix != "") news = prefix + " " + news;
                        if (suffix != "") news = news + " " + suffix;
                        return news;
                    }

                }
                if (O.GetType().Name == "Single") {
                    var D3 = (Single) O;
                    if (fieldType == null) {
                        return D3.ToString("n");
                    }
                    if (isStandardNumericFormatStyle(fieldType)) {
                        return D3.ToString(fieldType);
                    }

                    if (fieldType == "fixed") {
                        var sdec = GetFieldLower(tag, 3);
                        var prefix = GetFieldLower(tag, 4);
                        var sscale = GetFieldLower(tag, 6);
                        if (sscale == "") sscale = null;
                        if (prefix == null) prefix = "";
                        var suffix = GetFieldLower(tag, 5) ?? "";
                        float scale = 0;
                        if (sscale != null) scale = Convert.ToSingle(sscale);
                        if (scale != 0) D3 *= scale;
                        var dec = Convert.ToInt32(sdec);
                        var myFormat = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
                        myFormat.NumberDecimalDigits = dec;
                        string news = D3.ToString("n", myFormat);
                        if (prefix != "") news = prefix + " " + news;
                        if (suffix != "") news = news + " " + suffix;
                        return news;
                    }

                }
                return O.ToString();
            }
        }


        /// <summary>
        /// Return tag for a datacolumn, completing it with DataColumn format properties
        ///  if the tag does not contain format informations
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static string CompleteTag(string tag, DataColumn C) {
            string fmt = GetField(tag, 2);
            if ((fmt != null) && (fmt != "")) return tag;
            fmt = GetFormatForColumn(C);
            if (fmt == null) return tag;
            tag += "." + fmt;
            return tag;
        }




        /// <summary>
        /// Sets the display format for a DataColumn. This should be one of the
        ///  available Windows formats. I.e.: "c" (currency), "d" (datetime), 
        ///  "n" (numbers), and so on
        /// </summary>
        /// <param name="c"></param>
        /// <param name="fmt"></param>
        public static void SetFormatForColumn(DataColumn c, string fmt) {
            if (c == null) {
                //MarkEvent("SetFormatForColumn called with null column");
                return;
            }
            c.ExtendedProperties["format"] = fmt;
        }

        /// <summary>
        /// Returns the display format for a column, or a default format if a
        ///  format has not been specified for that column
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string GetFormatForColumn(DataColumn c) {
            if (c == null) return "g";
            if (c.ExtendedProperties["format"] != null)
                return c.ExtendedProperties["format"].ToString();
            if (c.DataType.Name == "Decimal") return "c"; //default for decimals
            if (c.DataType.Name == "Float") return "n"; //default for decimals
            if (c.DataType.Name == "Double") return "n"; //default for decimals
            if (c.DataType.Name == "DateTime") return "d"; //default for datetimes
            return "g";
        }

        /// <summary>
        /// Sets the horizontal-alignment for a column. Valid inputs are C (center), 
        ///		L (left), R (right)
        /// </summary>
        /// <param name="c"></param>
        /// <param name="clr"></param>
        public static void SetAlignForColumn(DataColumn c, string clr) {
            if (clr == "C") c.ExtendedProperties["align"] = HorizontalAlignment.Center;
            if (clr == "L") c.ExtendedProperties["align"] = HorizontalAlignment.Left;
            if (clr == "R") c.ExtendedProperties["align"] = HorizontalAlignment.Right;
        }

        /// <summary>
        /// Gets the horizontal-alignment for a column, or a default one if
        ///  an alignemnt has not been specified for that column
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static HorizontalAlignment GetAlignForColumn(DataColumn c) {
            if (c.ExtendedProperties["align"] != null)
                return (HorizontalAlignment)c.ExtendedProperties["align"];

            if (c.DataType.Name == "Decimal") return HorizontalAlignment.Right;
            if (c.DataType.Name == "DateTime") return HorizontalAlignment.Right;
            if (c.DataType.Name == "Double") return HorizontalAlignment.Right;
            if (c.DataType.Name == "Int32") return HorizontalAlignment.Right;
            if (c.DataType.Name == "Int16") return HorizontalAlignment.Right;
            return HorizontalAlignment.Left;
        }

    }
}
