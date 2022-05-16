
	using System;
	using System.Data;
	using System.IO;
	using System.Net;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Collections;
	using System.Collections.Generic;
	using System.Dynamic;
	using mdl;
	using q = mdl.MetaExpression;
	
namespace mdl_aux {
	public class HashCreator_flag :IHashCreator {
		public string[] k = { "flag" };
		public string[] keys { get { return k; } }
		public string get(DataRow r, DataRowVersion v = DataRowVersion.Default) {
			if (r.RowState == DataRowState.Deleted)
				v = DataRowVersion.Original;
			return r["flag", v].ToString();
		}
		public string get(DataRow r, string field, object proposedValue, DataRowVersion v = DataRowVersion.Default) {
			if (r.RowState == DataRowState.Deleted)
				v = DataRowVersion.Original;
			return proposedValue.ToString();
		}
		public string getFromObject(object o) {
			return q.getField("flag", o).ToString();
		}
		public string getFromDictionary(Dictionary<string, object> o) {
			return (o["flag"] ?? "").ToString();
		}
		public string getChild(DataRow rParent, DataRelation rel, DataRowVersion ver = DataRowVersion.Default) {
			return rParent[rel.ParentColumns[0].ColumnName, ver].ToString();
		}
		public string getParent(DataRow rChild, DataRelation rel, DataRowVersion ver = DataRowVersion.Default) {
			return rChild[rel.ChildColumns[0].ColumnName, ver].ToString();
		}
	}
} 
 
