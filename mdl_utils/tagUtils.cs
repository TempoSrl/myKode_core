using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mdl_utils {

    public static class tagUtils {
          /// <summary>
        /// Gets the Nth field in a list of dot - separated fields. Assumes the field
        ///  is the last of the list, so that the extracted string is allowed to 
        ///  include dots.
        /// </summary>
        /// <param name="S">input string </param>
        /// <param name="N">0 for first field</param>
        /// <returns>null if field not found</returns>
        static public string GetLastField(string S, int N) {
            if (S == null) return null;
            S = S.Trim();
            int n = 0;
            int pos = -1;
            while (n < N) {
                pos = S.IndexOf(".", pos + 1);
                n++;
                if (pos == -1) return null;
            }
            return S.Substring(pos + 1);

        }

        /// <summary>
        /// Gets the Nth field in a list of dot separated fields
        /// </summary>
        /// <param name="S">input string</param>
        /// <param name="N">0 for first field</param>
        /// <returns>null if field not found</returns>
        public static string GetField(string S, int N) {
            if (S == null) return null;
            S = S.Trim();
            try {
                string[] Field = S.Split(new char[] {'.'});
                if (Field.Length > N) return Field[N].Trim();
                else return null;
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// Gets a piece of tag, converted into lower
        /// </summary>
        /// <param name="S"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        static public string GetFieldLower(string S, int N) {
            string S2 = GetField(S, N);
            if (S2 == null) return null;
            return S2.ToLower();
        }

        /// <summary>
        /// Gets Standard tag, or search tag if Standard is not present
        /// </summary>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static string GetAnyTag(object Tag) {
            string tag = GetStandardTag(Tag);
            if (tag == null) tag = GetSearchTag(Tag);
            return tag;
        }

        /// <summary>
        /// Gets standard tag from a tag object
        /// </summary>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static string GetStandardTag(object Tag) {
            if (Tag == null) return null;
            string S = Tag.ToString().Trim();
            int pos = S.IndexOf('?');
            if (pos == -1) return BlankToNull(S);
            return BlankToNull(S.Substring(0, pos));
        }

        /// <summary>
        /// Gets Search tag from a tag object
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string GetSearchTag(object tag) {
            if (tag == null) return null;
            var s = tag.ToString().Trim();
            var pos = s.IndexOf('?');
            if (pos == -1) return BlankToNull(s);
            return BlankToNull(s.Substring(pos + 1));
        }

          /// <summary>
        /// return S, or empty string if S is null
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        static public string BlankToNull(string S) {
            if (S == null) return null;
            S = S.Trim();
            if (S == "") return null;
            return S;
        }

        /// <summary>
        /// Returns true if Tag contains  an extended ? clause
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool HasSpecificSearchTag(object tag) {
            if (tag == null) return false;
            var s = tag.ToString().Trim();
            int pos = s.IndexOf('?');
            return pos != -1;
        }

        /// <summary>
        /// Get Tag gives Table
        /// </summary>
        /// <param name="tablecolumnval1val2">tag in the format table.field[:val1:val2]</param>
        /// <returns>table</returns>
        public static string GetTableName(string tablecolumnval1val2) {
            return GetField(tablecolumnval1val2, 0);
        }

        
        /// <summary>
        /// Get Tag gives field
        /// </summary>
        /// <param name="sourcetablecolumnval1val2">Control Tag</param>
        /// <returns>ChildColumn</returns>
        /// <remarks>Tag in the format table.field[:val1:val2]  </remarks>
        public static string GetColumnName(string sourcetablecolumnval1val2) {
            sourcetablecolumnval1val2 = GetLookup(sourcetablecolumnval1val2);
            var table = GetTableName(sourcetablecolumnval1val2);
            if (table == null) return null;
            return GetField(sourcetablecolumnval1val2, 1);
        }

        /// <summary>
        /// Checks that a Tag is in the format table.field[:val1:val2]
        /// </summary>
        /// <param name="Tag">ComboBox Tag</param>
        /// <returns>true if the Tag is in a correct format</returns>
        static bool checkTag(string Tag) {
            if (Tag == null) return false;
            if (Tag == "") return false;
            Tag = Tag.Trim();
            var table = GetTableName(Tag);
            var column = GetColumnName(Tag);
            return (table != null) && (column != null);
        }

          /// <summary>
        /// Checks that a Tag is in the format master[:parenttable.parentcolumn]
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool CheckStandardTag(object tag) {
            var sTag = GetStandardTag(tag);
            return sTag != null && checkTag(sTag);
        }

        /// <summary>
        /// Return true if Tag contains a valid search tag, i.e. tablename and fieldname exists in DataSet
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool CheckSearchTag(Object tag) {
            var sTag = GetSearchTag(tag);
            return sTag != null && checkTag(sTag);
        }

          /// <summary>
        /// Get ComboBox Tag gives parenttable.parentfield
        /// </summary>
        /// <param name="sourcetablecolumnval1val2">Tag in the format table.field[:val1:val2]</param>
        /// <returns>"table.field"</returns>
        public static string GetLookup(string sourcetablecolumnval1val2) {
            if (sourcetablecolumnval1val2 == null) return null;
            var s = sourcetablecolumnval1val2.Trim();
            var pos = s.IndexOf(':');
            if (pos == -1) return s;
            var tablecolumn = s.Substring(0, pos).Trim();
            return tablecolumn.IndexOf('.') == -1 ? null : tablecolumn;
        }


           /// <summary>
        /// Get List type from a Tag 
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="index">position of the listtype in the tag</param>
        /// <returns></returns>
        public static string ListingType(object Tag, int index) {
            if (Tag == null) return "unknown";
            string tag = Tag.ToString().Trim();
            string[] fields = tag.Split(new char[] {'.'});
            string ListingName;
            if (fields.Length > index) ListingName = fields[index].Trim();
            else ListingName = "unknown";
            return ListingName;
            //			if (ListingTypes.Contains(ListingName))return ListingName;
            //			return "unknown";
        }

        /// <summary>
        /// Get Edit type from a Tag 
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="index">position of the edittype in the tag</param>
        /// <returns></returns>
        public static string EditType(object Tag, int index) {
            if (Tag == null) return "unknown";
            string tag = Tag.ToString().Trim();
            string[] fields = tag.Split(new char[] {'.'});
            string EditName = "unknown";
            if (fields.Length > index)
                EditName = fields[index].Trim();
            else {
                if (index > 1) return EditType(Tag, index - 1);
            }

            return EditName;
            //			if (EditTypes.Contains(EditName))return EditName;
            //			return "unknnown";
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
    }
}
