using System;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
#pragma warning disable 1591
namespace mdl {
    [Serializable()]
    [DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetTypedDataSetSchema")]
    [System.Xml.Serialization.XmlRootAttribute("dbstructure")]
    [System.ComponentModel.Design.HelpKeywordAttribute("vs.data.DataSet")]
    public partial class dbstructure :DataSet {
        
        #region Table members declaration
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customobject { get { return Tables["customobject"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customview { get { return Tables["customview"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customviewcolumn { get { return Tables["customviewcolumn"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customvieworderby { get { return Tables["customvieworderby"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customviewwhere { get { return Tables["customviewwhere"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customtablestructure { get { return Tables["customtablestructure"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customredirect { get { return Tables["customredirect"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable customedit { get { return Tables["customedit"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable viewcolumn { get { return Tables["viewcolumn"]; } }
        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DataTable columntypes { get { return Tables["columntypes"]; } }
        #endregion


        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
        public new DataTableCollection Tables { get { return base.Tables; } }

        [DebuggerNonUserCodeAttribute()]
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
        public new DataRelationCollection Relations { get { return base.Relations; } }

        [DebuggerNonUserCodeAttribute()]
        public dbstructure() {
            BeginInit();
            InitClass();
            EndInit();
        }
        [DebuggerNonUserCodeAttribute()]
        protected dbstructure(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
        [DebuggerNonUserCodeAttribute()]
        private void InitClass() {
            DataSetName = "dbstructure";
            Prefix = "";
            Namespace = "http://tempuri.org/dbstructure.xsd";

            #region create DataTables
            DataTable T;
            DataColumn C;
            //////////////////// CUSTOMOBJECT /////////////////////////////////
            T = new DataTable("customobject");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("description", typeof(String)));
            T.Columns.Add(new DataColumn("isreal", typeof(String)));
            T.Columns.Add(new DataColumn("realtable", typeof(String)));
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"] };


            //////////////////// CUSTOMVIEW /////////////////////////////////
            T = new DataTable("customview");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("header", typeof(String)));
            T.Columns.Add(new DataColumn("footer", typeof(String)));
            T.Columns.Add(new DataColumn("topmargin", typeof(Double)));
            T.Columns.Add(new DataColumn("bottommargin", typeof(Double)));
            T.Columns.Add(new DataColumn("rightmargin", typeof(Double)));
            T.Columns.Add(new DataColumn("leftmargin", typeof(Double)));
            T.Columns.Add(new DataColumn("lefttoright", typeof(Int16)));
            T.Columns.Add(new DataColumn("hcenter", typeof(Int16)));
            T.Columns.Add(new DataColumn("vcenter", typeof(Int16)));
            T.Columns.Add(new DataColumn("gridlines", typeof(Int16)));
            T.Columns.Add(new DataColumn("rowheading", typeof(Int16)));
            T.Columns.Add(new DataColumn("colheading", typeof(Int16)));
            T.Columns.Add(new DataColumn("landscape", typeof(Int16)));
            T.Columns.Add(new DataColumn("scale", typeof(Int16)));
            T.Columns.Add(new DataColumn("fittopage", typeof(Int16)));
            T.Columns.Add(new DataColumn("vpages", typeof(Int16)));
            T.Columns.Add(new DataColumn("hpages", typeof(Int16)));
            T.Columns.Add(new DataColumn("isreal", typeof(String)));
            T.Columns.Add(new DataColumn("issystem", typeof(String)));
            T.Columns.Add(new DataColumn("staticfilter", typeof(String)));
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["viewname"] };


            //////////////////// CUSTOMVIEWCOLUMN /////////////////////////////////
            T = new DataTable("customviewcolumn");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("colnumber", typeof(Int16));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("heading", typeof(String)));
            T.Columns.Add(new DataColumn("colwidth", typeof(Int32)));
            T.Columns.Add(new DataColumn("visible", typeof(Int16)));
            T.Columns.Add(new DataColumn("fontname", typeof(String)));
            T.Columns.Add(new DataColumn("fontsize", typeof(Int16)));
            T.Columns.Add(new DataColumn("bold", typeof(Int16)));
            T.Columns.Add(new DataColumn("italic", typeof(Int16)));
            T.Columns.Add(new DataColumn("underline", typeof(Int16)));
            T.Columns.Add(new DataColumn("strikeout", typeof(Int16)));
            T.Columns.Add(new DataColumn("color", typeof(Int32)));
            T.Columns.Add(new DataColumn("format", typeof(String)));
            T.Columns.Add(new DataColumn("isreal", typeof(String)));
            T.Columns.Add(new DataColumn("expression", typeof(String)));
            T.Columns.Add(new DataColumn("colname", typeof(String)));
            T.Columns.Add(new DataColumn("systemtype", typeof(String)));
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            T.Columns.Add(new DataColumn("listcolpos", typeof(Int32)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["viewname"], T.Columns["colnumber"] };


            //////////////////// CUSTOMVIEWORDERBY /////////////////////////////////
            T = new DataTable("customvieworderby");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("periodnumber", typeof(Int16));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("columnname", typeof(String)));
            T.Columns.Add(new DataColumn("direction", typeof(Int32)));
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["viewname"], T.Columns["periodnumber"] };


            //////////////////// CUSTOMVIEWWHERE /////////////////////////////////
            T = new DataTable("customviewwhere");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("periodnumber", typeof(Int16));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("connector", typeof(Int32)));
            T.Columns.Add(new DataColumn("columnname", typeof(String)));
            T.Columns.Add(new DataColumn("operator", typeof(Int32)));
            T.Columns.Add(new DataColumn("value", typeof(String)));
            T.Columns.Add(new DataColumn("runtime", typeof(Int32)));
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["viewname"], T.Columns["periodnumber"] };


            //////////////////// CUSTOMTABLESTRUCTURE /////////////////////////////////
            T = new DataTable("customtablestructure");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("colname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("autoincrement", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("step", typeof(Int32)));
            T.Columns.Add(new DataColumn("prefixfieldname", typeof(String)));
            T.Columns.Add(new DataColumn("middleconst", typeof(String)));
            C = new DataColumn("length", typeof(Int32));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("linear", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("selector", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["colname"] };


            //////////////////// CUSTOMREDIRECT /////////////////////////////////
            T = new DataTable("customredirect");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("objecttarget", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("viewtarget", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["viewname"] };


            //////////////////// CUSTOMEDIT /////////////////////////////////
            T = new DataTable("customedit");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("edittype", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("dllname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("caption", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("list", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("startempty", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("tree", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("defaultlisttype", typeof(String)));
            C = new DataColumn("searchenabled", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["edittype"] };


            //////////////////// VIEWCOLUMN /////////////////////////////////
            T = new DataTable("viewcolumn");
            C = new DataColumn("objectname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("colname", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("realtable", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("realcolumn", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["objectname"], T.Columns["colname"] };


            //////////////////// COLUMNTYPES /////////////////////////////////
            T = new DataTable("columntypes");
            C = new DataColumn("tablename", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("field", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("iskey", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("sqltype", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("col_len", typeof(Int32)));
            T.Columns.Add(new DataColumn("col_precision", typeof(Int32)));
            T.Columns.Add(new DataColumn("col_scale", typeof(Int32)));
            T.Columns.Add(new DataColumn("systemtype", typeof(String)));
            C = new DataColumn("sqldeclaration", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            C = new DataColumn("allownull", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("defaultvalue", typeof(String)));
            T.Columns.Add(new DataColumn("format", typeof(String)));
            C = new DataColumn("denynull", typeof(String));
            C.AllowDBNull = false;
            T.Columns.Add(C);
            T.Columns.Add(new DataColumn("lastmodtimestamp", typeof(DateTime)));
            T.Columns.Add(new DataColumn("lastmoduser", typeof(String)));
            T.Columns.Add(new DataColumn("createuser", typeof(String)));
            T.Columns.Add(new DataColumn("createtimestamp", typeof(DateTime)));
            Tables.Add(T);
            T.PrimaryKey = new DataColumn[] { T.Columns["tablename"], T.Columns["field"] };


            #endregion


            #region DataRelation creation
            DataColumn []CPar;
            DataColumn []CChild;
            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { columntypes.Columns["tablename"] };
            Relations.Add(new DataRelation("customobjectcolumntypes", CPar, CChild, false));

            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { viewcolumn.Columns["objectname"] };
            Relations.Add(new DataRelation("customobjectviewcolumn", CPar, CChild, false));

            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { customedit.Columns["objectname"] };
            Relations.Add(new DataRelation("customobjectcustomedit", CPar, CChild, false));

            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { customredirect.Columns["objectname"] };
            Relations.Add(new DataRelation("customobjectcustomredirect", CPar, CChild, false));

            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { customtablestructure.Columns["objectname"] };
            Relations.Add(new DataRelation("customobjectcustomtablestructure", CPar, CChild, false));

            CPar = new DataColumn[2] { customview.Columns["objectname"], customview.Columns["viewname"] };
            CChild = new DataColumn[2] { customviewwhere.Columns["objectname"], customviewwhere.Columns["viewname"] };
            Relations.Add(new DataRelation("customviewcustomviewwhere", CPar, CChild, false));

            CPar = new DataColumn[2] { customview.Columns["objectname"], customview.Columns["viewname"] };
            CChild = new DataColumn[2] { customvieworderby.Columns["objectname"], customvieworderby.Columns["viewname"] };
            Relations.Add(new DataRelation("customviewcustomvieworderby", CPar, CChild, false));

            CPar = new DataColumn[2] { customview.Columns["objectname"], customview.Columns["viewname"] };
            CChild = new DataColumn[2] { customviewcolumn.Columns["objectname"], customviewcolumn.Columns["viewname"] };
            Relations.Add(new DataRelation("customviewcustomviewcolumn", CPar, CChild, false));

            CPar = new DataColumn[1] { customobject.Columns["objectname"] };
            CChild = new DataColumn[1] { customview.Columns["objectname"] };
            Relations.Add(new DataRelation("customobjectcustomview", CPar, CChild, false));

            #endregion

        }
    }
}
