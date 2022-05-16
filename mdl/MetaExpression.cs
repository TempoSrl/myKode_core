using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Dynamic;
//using Microsoft.CSharp.RuntimeBinder; decomment to support Dynamic Objects
using System.Runtime.CompilerServices;
using System.Globalization;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {

    /// <summary>
    /// Function applied to a list of DataRowExpr that returns another DataRowExpr
    /// </summary>
    /// <param name="par"></param>
    /// <returns></returns>
    public delegate MetaExpression DataRowFunction(params MetaExpression[] par);

    /// <summary>
    /// Creates a MetaExpression given a DataRow
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public delegate MetaExpression MetaExpressionGenerator(DataRow r);

    /// <summary>
    /// Informations about a MetaExpression
    /// </summary>
    [Flags]
    public enum ExpressionInfo {
        /// <summary>
        /// The expression is the True constant
        /// </summary>
        IsTrue = 1,
        /// <summary>
        /// The expression is the False constant
        /// </summary>
        IsFalse = 2,
        /// <summary>
        /// The expression is a constant
        /// </summary>
        IsConstant = 4,
        /// <summary>
        /// The expression is the DBNull constant
        /// </summary>
        IsNull = 8,
        /// <summary>
        /// The expression is null
        /// </summary>
        IsUndefined = 16,
        /// <summary>
        ///  The expression is a grouping function
        /// </summary>
        IsGrouping = 32

    }

    /// <summary>
    /// Helper class for MetaExpressions
    /// </summary>
    public static class MetaExpressionHelper {
        /// <summary>
        /// returns all key fields of this row (they depends on the table, not on the rows)
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static string[] keyFields(this DataRow r) {
            return (from c in r.Table.PrimaryKey select c.ColumnName).ToArray();
        }

        /// <summary>
        /// returns all key values for a row
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static object[] keyValues(this DataRow r) {
            return (from c in r.Table.PrimaryKey select r[c.ColumnName]).ToArray();
        }

        /// <summary>
        /// Converts an array of metaExpressions to an array of sql strings
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="q"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static string[] toSql(this MetaExpression[] fn, QueryHelper q, ISecurity env = null) {
            if (fn.Length == 0) return new string[0];
            return (from f in fn select f.toSql(q, env)).ToArray();
        }

        /// <summary>
        /// Converts an array of metaExpressions to an array of descriptivr strings
        /// </summary>
        /// <param name="fn">array of metaExpression</param>
        /// <returns></returns>
        public static string[] toString(this MetaExpression[] fn) {
            if (fn.Length == 0) return new string[0];
            return (from f in fn select f.toString()).ToArray();
        }

        /// <summary>
        /// converts an array of metaExpression into a string that is the comma separation of their friendly caption
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static string toCommaSeparated(this MetaExpression[] fn) {
            return string.Join(",", (from f  in fn select f.toString()));
        }

        /// <summary>
        /// converts an array of metaExpression in a string that is the comma separation of their sql translation
        /// </summary>
        /// <param name="fn">array of metaExpression</param>
        /// <param name="q">QueryHelper to use</param>
        /// <param name="env">environment for the evaluation</param>
        /// <returns></returns>
        public static string toCommaSeparated(this MetaExpression[] fn, QueryHelper q, ISecurity env = null) {
            return string.Join(",", (from f in fn select f.toSql(q, env)));
        }

        public static CQueryHelper qhc = MetaFactory.factory.getSingleton<CQueryHelper>();

        /// <summary>
        /// Convert MetaExpression into a Ado.net compatible expression string, suitable for DataTable querying
        /// </summary>
        /// <param name="M"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static string toADO(this MetaExpression M,dynamic env=null) {
            return M.toSql(qhc,env);

		}

    }

    /// <summary>
    /// true when s like pattern (in  a sql-like behaviour)
    /// </summary>
    public static class SqlLikeStringExtensions {

        /// <summary>
        /// true when s like pattern (in  a sql-like behaviour)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool SqlLike(this string s, string pattern) {
            return SqlLikeStringUtilities.SqlLike(pattern, s);
        }
    }

    /// <summary>
    /// Helper class  
    /// </summary>
    public static class SqlLikeStringUtilities {
        /// <summary>
        /// true when s like pattern (in  a sql-like behaviour)
        /// </summary>
        public static bool SqlLike(string pattern, string str) {
            bool isMatch = true,
                isWildCardOn = false,
                isCharWildCardOn = false,
                isCharSetOn = false,
                isNotCharSetOn = false;
            int lastWildCard = -1;
            int patternIndex = 0;
            var set = new List<char>();
            char p = '\0';


            bool endOfPattern;
            for(int i = 0; i < str.Length; i++) {
                char c = str[i];
                endOfPattern = (patternIndex >= pattern.Length);
                if(!endOfPattern) {
                    p = pattern[patternIndex];

                    if(!isWildCardOn && p == '%') {
                        lastWildCard = patternIndex;
                        isWildCardOn = true;
                        while(patternIndex < pattern.Length &&
                            pattern[patternIndex] == '%') {
                            patternIndex++;
                        }
                        if(patternIndex >= pattern.Length)
                            p = '\0';
                        else
                            p = pattern[patternIndex];
                    }
                    else if(p == '_') {
                        isCharWildCardOn = true;
                        patternIndex++;
                    }
                    else if(p == '[') {
                        if(pattern[++patternIndex] == '^') {
                            isNotCharSetOn = true;
                            patternIndex++;
                        }
                        else
                            isCharSetOn = true;

                        set.Clear();
                        if(pattern[patternIndex + 1] == '-' && pattern[patternIndex + 3] == ']') {
                            char start = char.ToUpper(pattern[patternIndex]);
                            patternIndex += 2;
                            char end = char.ToUpper(pattern[patternIndex]);
                            if(start <= end) {
                                for(char ci = start; ci <= end; ci++) {
                                    set.Add(ci);
                                }
                            }
                            patternIndex++;
                        }

                        while(patternIndex < pattern.Length &&
                            pattern[patternIndex] != ']') {
                            set.Add(pattern[patternIndex]);
                            patternIndex++;
                        }
                        patternIndex++;
                    }
                }

                if(isWildCardOn) {
                    if(char.ToUpper(c) == char.ToUpper(p)) {
                        isWildCardOn = false;
                        patternIndex++;
                    }
                }
                else if(isCharWildCardOn) {
                    isCharWildCardOn = false;
                }
                else if(isCharSetOn || isNotCharSetOn) {
                    bool charMatch = (set.Contains(char.ToUpper(c)));
                    if((isNotCharSetOn && charMatch) || (isCharSetOn && !charMatch)) {
                        if(lastWildCard >= 0)
                            patternIndex = lastWildCard;
                        else {
                            isMatch = false;
                            break;
                        }
                    }
                    isNotCharSetOn = isCharSetOn = false;
                }
                else {
                    if(char.ToUpper(c) == char.ToUpper(p)) {
                        patternIndex++;
                    }
                    else {
                        if(lastWildCard >= 0)
                            patternIndex = lastWildCard;
                        else {
                            isMatch = false;
                            break;
                        }
                    }
                }
            }
            endOfPattern = (patternIndex >= pattern.Length);

            if (isMatch && !endOfPattern) {
                bool isOnlyWildCards = true;
                for (int i = patternIndex; i < pattern.Length; i++) {
                    if (pattern[i] != '%') {
                        isOnlyWildCards = false;
                        break;
                    }
                }
                if (isOnlyWildCards) endOfPattern = true;
            }
            return isMatch && endOfPattern;
        }
    }
	/// <summary>
	/// Implemented in metaexpression like field=values & field=values & ..
	/// </summary>
    public interface IGetCompDictionary {
		/// <summary>
		/// Returns the Dictionary of field-values to compare
		/// </summary>
		/// <returns></returns>
	    Dictionary<string, object> getMcmpDictionary();
    }

    /// <summary>
    /// Base class for all MetaExpressions. It represent a function that can be applied to an object in a given context. 
    ///  MetaExpression can be combined together like any expression, and can be converted to sql query or to string when needed.
    ///  It is also implicitely convertable into a Predicate
    /// </summary>
    public abstract class MetaExpression {

        /// <summary>
        /// Mapping tra nomi delle tabelle e campi dell'oggetto in input ove sia un oggetto composito
        /// </summary>
        public Dictionary<string, string> joinContext;

        /// <summary>
        /// Compiled version of the expression (only present if compile have been invoked)
        /// </summary>
        MethodInfo compiled;

        private Type ItemType;

        /// <summary>
        /// Gets c# code to compile this expression
        /// </summary>
        /// <param name="c">compiler to use</param>
        /// <param name="varName">variable name for object to access</param>
        /// <param name="T"></param>
        /// <returns></returns>
        public virtual string getCCode(Compiler c, string varName, System.Type T) {
            return null;
        }

        /// <summary>
        /// Check if any operands is null  or undefined. If a null is found, undefined is not checked.
        /// </summary>
        /// <param name="values"></param>
        public virtual void evaluateNullOrUndefined(MetaExpression[] values) {
             if (anyIsNull(values)) {
                Info |= ExpressionInfo.IsNull;
            }
            else {
                if (anyIsUndefined(values)) Info |= ExpressionInfo.IsUndefined;
            }
        }
        /// <summary>
        /// Optimize access to DataTable when implied objects are DataRows
        /// </summary>
        /// <param name="t"></param>
        /// <param name="alias"></param>
        public virtual MetaExpression optimize(DataTable t, string alias=null) {
            Parameters?._forEach(m => m.optimize(t, alias));
            return this;
        }
        
        /// <summary>
        /// Compile this expression
        /// </summary>
        /// <typeparam name="T">object in input to the apply function</typeparam>
        public virtual MetaExpression Compile<T>() {
            this.ItemType = typeof(T);
            Compiler c = new Compiler();
            string body = getCCode(c,"o",ItemType);
            string newClassName = Compiler.GetNewFunName();
            string outBody =
                $"namespace mdl {{ public class {newClassName} {{ " +
                $" public static object apply({ItemType.Name} o, object env=null) {{" +
                " return (" + body + ");" +
                "} } } ";
            string []usingList = new[] { "System",
                         "System.Data",
                         "System.Linq",
                         //,"Microsoft.CSharp.RuntimeBinder",
            "System.Dynamic" };
            var linkedDll = new [] {
                typeof(object).Assembly.Location,
                typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location,
                "System.Core.dll",
                "System.Xml.Linq.dll",
                "System.dll",
                "System.Data.dll",
                "System.Data.Common.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Xml.dll"
                ,"mdl.dll"
                ,"Microsoft.CSharp.dll"
            };
            compiled = c.Compile("mdl."+newClassName, outBody,usingList, linkedDll);
            return this;        
        }

        internal virtual void applyContext(Dictionary<string, string> context) {
            this.joinContext = context;            
            Parameters?._forEach(m => m.applyContext(context));
        }

        internal virtual bool isGroupingFunction() {
            return Info.HasFlag(ExpressionInfo.IsGrouping) | Parameters._Some(m => m.isGroupingFunction());
        }

        /// <summary>
        /// Associates this Expression to a join context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public MetaExpression with(Dictionary<string,string> context) {
            applyContext(context);
            return this;
        }


        /// <summary>
        /// Merge a table alias association to current join context
        /// </summary>
        /// <param name="table">Name in the database or in data structure</param>
        /// <param name="_as">Name used in the expression as a reference</param>
        /// <returns></returns>
        public MetaExpression with(string table, string _as) {
            if (joinContext == null) joinContext = new Dictionary<string, string>();
            joinContext[_as] = table;
            applyContext(joinContext);
            return this;
        }



        /// <summary>
        /// Evaluates a MetaExpression on a object and returns a boolean value. Any non boolean result is considered as false
        /// </summary>
        /// <param name="o"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public bool getBoolean(object o, ISecurity env=null) {
            
            object res = compiled !=null? compiled.Invoke(null, new object[] { o, env }): apply(o, env);
            if (res == null) return false;
            if (res is Boolean boolean) return boolean;
            return false;
        }        

        /// <summary>
        /// Combines two MetaExpression with a logical AND
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator & (MetaExpression m1, MetaExpression m2) {
            return MetaExpression.and(m1, m2);
        }

        /// <summary>
        /// Combines two MetaExpression with a logical OR
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator |(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.or(m1, m2);
        }

        /// <summary>
        /// Combines two MetaExpression with a logical OR
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator > (MetaExpression m1, MetaExpression m2) {
            return MetaExpression.gt(m1, m2);
        }

        /// <summary>
        /// Combines two MetaExpression with a logical OR
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator <(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.lt(m1, m2);
        }

        /// <summary>
        /// Combines two MetaExpression with a logical OR
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator <=(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.le(m1, m2);
        }


        /// <summary>
        /// Combines two MetaExpression with a logical OR
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator >=(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.ge(m1, m2);
        }

        /// <summary>
        /// Cop
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator ==(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.eq(m1, m2);
        }

        /// <summary>
        /// Cop
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static MetaExpression operator !=(MetaExpression m1, MetaExpression m2) {
            return MetaExpression.ne(m1, m2);
        }


        /// <summary>
        /// Return the boolean complement of a MetaExpression
        /// </summary>
        /// <param name="m1"></param>
        public static MetaExpression operator !(MetaExpression m1) {
            return MetaExpression.not(m1);
        }


        /// <summary>
        /// Gives the Predicate implicitely associated to a MetaExpression
        /// </summary>
        /// <param name="m"></param>
        public static implicit operator Func<object,bool>(MetaExpression m) {
            return  (o) => m.getBoolean(o) ;
        }


        /// <summary>
        /// Gives the Predicate implicitely associated to a MetaExpression
        /// </summary>
        /// <param name="m"></param>
        public static implicit operator Predicate<object>(MetaExpression m) {
            return (o) => m.getBoolean(o);
        }

        /// <summary>
        /// Gives the Predicate implicitely associated to a MetaExpression
        /// </summary>
        /// <param name="m"></param>
        public virtual Predicate<object> applyWith(dynamic env=null) {
            return (o) => { return this.getBoolean(o,env);};            
        }


        /// <summary>
        /// Evaluates an expression on an object in a given environment
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="o"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static object calc(object expr, object o, ISecurity env = null) {
            if (expr == null) return null;
            if (DBNull.Value.Equals(expr)) return DBNull.Value;
            if (expr is MetaExpression m) return m.apply(o, env);
            Type valueType = expr.GetType();
            if (valueType.IsArray && typeof(MetaExpression).IsAssignableFrom(valueType.GetElementType())) {
                MetaExpression[] arr = (MetaExpression[])expr;
                return (from e in arr select calc(e, o, env)).ToArray();
            }
            return expr;
        }

        /// <summary>
        /// Creates a MetaExpression from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static MetaExpression fromString(string s) {
            return MetaExpressionParser.From(s);
        }
        /// <summary>
        /// Gets a MetaExpression from an object  in a parameter list (which can be a generic object or a MetaExpression)
        /// </summary>
        /// <param name="par"></param>
        /// <param name="autofield">when true, string are considered field names</param>
        /// <returns></returns>
        public static MetaExpression fromObject(object par, bool autofield = false) {
            if (DBNull.Value.Equals(par)) {
                return new MetaExpressionNull();
            }
            if (par == null) {
                return new MetaExpressionUndefined();
            }
            if(par is MetaExpression expression) {
                return tryEval(expression);
            }

            if (autofield && par is string s) {
                return new MetaExpressionField(s);
            }

            return new MetaExpressionConst(par);

        }

        /// <summary>
        /// Get an array of MetaExpression  from parameter list
        /// </summary>
        /// <param name="par"></param>
        /// <param name="autofield">if true, first element of the list is autofielded</param>
        /// <returns></returns>
        protected static MetaExpression[] getParams(object[] par, bool autofield = false) {
            return par.Select((p, index) => fromObject(p, autofield && (index == 0))).ToArray();
        }

        /// <summary>
        /// Parameters given with the constructor
        /// </summary>
        public MetaExpression[] Parameters;

        /// <summary>
        /// Information about the MetaExpression
        /// </summary>
        public ExpressionInfo Info = 0;

        /// <summary>
        /// Field Name from which this expression was eventually taken
        /// </summary>
        public string FieldName;

        /// <summary>
        /// Table to which this expression belongs if it is part of a Join
        /// </summary>
        public string TableName;

        /// <summary>
        /// Field Name to which this expression means to be assigned if queryed  
        /// </summary>
        public string Alias;

        public MetaExpression withEnv(ISecurity env) {
            return new MetaExpressionWithEnv(this, env);
		}

        /// <summary>
        /// Sets the Alias for this Expression
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public MetaExpression _as(string alias) {
            Alias = alias;
            return this;
        }

        /// <summary>
        /// Changes or set the tablename for creating the query
        /// </summary>
        /// <param name="newTable"></param>
        /// <param name="oldTable"></param>
        public void cascadeSetTable(string newTable, string oldTable = null) {
            if (Name == "field") {
                if (FieldName != null && !FieldName.Contains("(")) {
                    if (TableName == oldTable) TableName = newTable;
                }
            }
            if (Parameters==null)return;
            foreach(var exp in Parameters) exp.cascadeSetTable(newTable,oldTable);
        }


        /// <summary>
        /// Get an expression that will compare all specified fields of  rFrom with those of destination
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static MetaExpressionGenerator mcmpGen(params string[] fields) {
            return (DataRow rFrom) => MetaExpression.and(
                    fields.Select(cc =>
                 MetaExpression.eq(MetaExpression.field(cc), rFrom[cc]))
                 .ToArray());
        }

        /// <summary>
        /// Get Expression that searches the parents of a DataRow through a relation
        /// </summary>
        /// <param name="rel"></param>
        /// <returns></returns>
        public static MetaExpressionGenerator parent(DataRelation rel) {
            return (DataRow rFrom) => MetaExpression.and(rel.ParentColumns.Select((cc, i) =>
                 MetaExpression.eq(MetaExpression.field(cc.ColumnName), rFrom[rel.ChildColumns[i].ColumnName])
                                     ).ToArray());
        }

        /// <summary>
        /// Get Expression that searches the childrens of a DataRow through a relation
        /// </summary>
        /// <param name="rel"></param>
        /// <returns></returns>
        public static MetaExpressionGenerator child(DataRelation rel) {
            return (DataRow rFrom) => MetaExpression.and(rel.ChildColumns.Select((cc, i) =>
               MetaExpression.eq(MetaExpression.field(cc.ColumnName), rFrom[rel.ParentColumns[i].ColumnName])
                                     ).ToArray());
        }


        /// <summary>
        /// Simbolic name of the MetaExpression
        /// </summary>
        public string Name;

        /// <summary>
        /// Constant representing a null Expression
        /// </summary>
        public static MetaExpression NullMetaExpression = new MetaExpressionNull();

        /// <summary>
        /// Constant representing an undefined expression
        /// </summary>
        public static MetaExpression UndefinedMetaExpression = new MetaExpressionUndefined();

        /// <summary>
        /// Return true if the expression is the constant Null 
        /// </summary>
        /// <returns></returns>
        public bool isNull() {
            return (Info & ExpressionInfo.IsNull) != 0;
        }

        /// <summary>
        /// Returns true if the expression is undefined
        /// </summary>
        /// <returns></returns>
        public bool isUndefined() {
            return (Info & ExpressionInfo.IsUndefined) != 0;
        }

        /// <summary>
        /// Returns true if the expression is null or undefined
        /// </summary>
        /// <returns></returns>
        public bool isNullOrUndefined() {
            return (Info & (ExpressionInfo.IsUndefined | ExpressionInfo.IsNull)) != 0;
        }

        /// <summary>
        /// Returns true if the expression is the constant true
        /// </summary>
        /// <returns></returns>
        public bool isTrue() {
            return (Info & ExpressionInfo.IsTrue) != 0;
        }

        /// <summary>
        /// Returns true if the expression is the constant false
        /// </summary>
        /// <returns></returns>
        public bool isFalse() {
            return (Info & ExpressionInfo.IsFalse) != 0;
        }

        /// <summary>
        /// Returns true if the expression is a constant
        /// </summary>
        /// <returns></returns>
        public bool isConstant() {
            return (Info & ExpressionInfo.IsConstant) != 0;
        }

        /// <summary>
        /// Tries to evaluate the expression with an undefined object as parameter
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static MetaExpression tryEval(MetaExpression m) {
            if (m.isConstant()) return m;
            object res = m.apply(null);
            if (res != null) {
                return new MetaExpressionConst(res);
            }
            return m;
        }

        /// <summary>
        /// Evaluates the expression on an object in a given context
        /// </summary>
        /// <param name="o"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public abstract object apply(object o = null, ISecurity env = null);

        /// <summary>
        /// Get the sql representation for the expression
        /// </summary>
        /// <param name="q"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public abstract string toSql(QueryHelper q, ISecurity env = null);

        /// <summary>
        /// Friendly string representation of the expression
        /// </summary>
        /// <returns></returns>
        public abstract string toString();


        /// <summary>
        /// Get a field from  a composite object. It is equivalent to getField(getField(tableName,o),fieldName)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object getField(string tableName, string fieldName, object o) {
            if (o == null) return null;
            if (o is DataRow) {
                return getField(fieldName, o);
            }
            object main = getField(tableName, o);//o should be a collection of tables
            return getField(fieldName, main);
        }

        /// <summary>
        /// Gets a field of an object. It is similar to o[fieldName] where o  is almost anything
        /// </summary>
        /// <param name="o"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void setField(object o, string fieldName, object value) {
            try {
                if (o == null) return;
                if (o is DataRow dr) {
                    //DataRow r = (DataRow)o;
                    //if (r.RowState == DataRowState.Detached) return null;
                    //if (!r.Table.Columns.Contains(fieldName)) return DBNull.Value;
                    //DataRowVersion v = r.RowState == DataRowState.Deleted
                    //    ? DataRowVersion.Original
                    //    : DataRowVersion.Default;
                    dr[fieldName]=value;
                    return;
                }
                if (o is RowObject @object) {
                    @object[fieldName]=value;
                    return;
                }
                if(o is ExpandoObject e) {
                    IDictionary<string, object> dict = e;
                    dict[fieldName] = value;
                    return;
                }
                // decomment to support Dynamic Objects
                //if(o is DynamicObject d) { 
                //    try {
                //        var binder = Microsoft.CSharp.RuntimeBinder.Binder.SetMember(CSharpBinderFlags.None,
                //            fieldName, d.GetType(),
                //            new List<CSharpArgumentInfo> {
                //        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                //        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                //            });
                //        var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);
                //        callsite.Target(callsite, o, value);
                //        return;
                //    }
                //    catch(RuntimeBinderException) {
                //        return;
                //    }

                //}
                if (o is Dictionary<string, object>) {
                    var dict = (Dictionary<string, object>)o;
                    dict[fieldName] = value;
                    return;
                }

                var pI = o.GetType().GetProperty(fieldName);
                if (pI != null) {
                    pI.SetValue(o,value);
                    return;
                }
                var fI = o.GetType().GetField(fieldName);
                if (fI != null) {
                    fI.SetValue(o,value);
                    return;
                }               
            }
            catch {                
            }
        }

        /// <summary>
        /// Gets a field of an object. It is similar to o[fieldName] where o  is almost anything
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object getField(string fieldName, object o) {
            try {
                if (o == null) return null;
                if (o is DataRow row) {
                    //DataRow r = (DataRow)o;
                    //if (r.RowState == DataRowState.Detached) return null;
                    //if (!((DataRow)o).Table.Columns.Contains(fieldName)) return DBNull.Value;
                    //DataRowVersion v = r.RowState == DataRowState.Deleted
                    //    ? DataRowVersion.Original
                    //    : DataRowVersion.Default;
                    return row[fieldName];
                }
                if (o is RowObject @object) {
                    return @object[fieldName];
                }
                if(o is ExpandoObject e) {
                    IDictionary<string, object> dd = e;
                    if(dd.TryGetValue(fieldName, out var res)) {
                        if(res == null)
                            res = DBNull.Value;
                    }
                    else {
                        res = DBNull.Value;
                    }
                    return res;
                }
                // decomment to support Dynamic Objects
                //if(o is DynamicObject d) {
                //    var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None,
                //        fieldName, d.GetType(),
                //        new List<CSharpArgumentInfo> {
                //        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                //        });
                //    try {
                //        var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);
                //        return callsite.Target(callsite, o);
                //    }
                //    catch(RuntimeBinderException) {
                //        return DBNull.Value;
                //    }
                //}
                if (o is Dictionary<string, object> dict) {
                    if(!dict.TryGetValue(fieldName, out var res)) res = DBNull.Value;
                    return res;
                }

                PropertyInfo pI = o.GetType().GetProperty(fieldName);
                if (pI != null) return pI.GetValue(o);
                FieldInfo fI = o.GetType().GetField(fieldName);
                if (fI != null) {
                    var res =fI.GetValue(o);
                    if (res == null) return DBNull.Value;
                    return res;
                }
                return DBNull.Value;
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Value of sys environment variable
        /// </summary>
        /// <param name="envVariableName"></param>
        /// <returns></returns>
        public static MetaExpression sys(string envVariableName) {
            return new MetaExpressionSys(envVariableName);
        }

        /// <summary>
        /// Value of User environment variable
        /// </summary>
        /// <param name="envVariableName"></param>
        /// <returns></returns>
        public static MetaExpression usr(string envVariableName) {
            return new MetaExpressionUsr(envVariableName);
        }

      
        /// <summary>
        /// Constant MetaExpression
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static MetaExpression constant(object o) {
            return new MetaExpressionConst(o);
        }

        /// <summary>
        /// Expression representing the Value of a field of an object
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="tableName">optional indication of the source table name</param>
        /// <returns></returns>
        public static MetaExpression field(string fieldName, string tableName = null) {
            return new MetaExpressionField(fieldName, tableName);
        }

        /// <summary>
        /// Addition of two or more metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression add(params object[] par) {
            return tryEval(new MetaExpressionAdd(par));
        }

        /// <summary>
        /// Subtraction of two metaexpressions
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression modulus(object par1, object par2) {
            return tryEval(new MetaExpressionModulus(par1, par2));
        }


        /// <summary>
        /// Subtraction of two metaexpressions
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression sub(object par1, object par2) {
            return tryEval(new MetaExpressionSub(par1, par2));
        }

        /// <summary>
        /// Division operation between two metaexpressions
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression div(object par1, object par2) {
            return tryEval(new MetaExpressionDiv(par1, par2));
        }

        /// <summary>
        /// Multiplication of two or more metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression mul(params object[] par) {
            return tryEval(new MetaExpressionMul(par));
        }

        /// <summary>
        /// Sum of an expression. It is a grouping function.
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression sum<t>(object  par) {
            return tryEval(new MetaExpressionSum<t>(par));
        }

        /// <summary>
        /// Logical OR between N metaexpressions. Or of 0 expression is false
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression or(params object[] par) {
            var r = new MetaExpressionOr(par);
            if (r.isTrue()) return new MetaExpressionConst(true);
            return r;
        }

        /// <summary>
        /// Logical AND between N metaexpressions. And of 0 expressions is true.
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression and(params object[] par) {
            var r = new MetaExpressionAnd(par);
            if (r.isFalse()) return new MetaExpressionConst(false);
            return r;
        }

        /// <summary>
        /// Bitwise AND between N metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression bitAnd(params object[] par) {
            var r = new MetaExpressionBitwiseAnd(par);
            return r;
        }
        /// <summary>
        /// Bitwise OR between N metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression bitOr(params object[] par) {
            var r = new MetaExpressionBitwiseOr(par);
            return r;
        }

         /// <summary>
        /// Bitwise XOR between N metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression bitXor(params object[] par) {
            var r = new MetaExpressionBitwiseXor(par);
            return r;
        }

           /// <summary>
        /// Bitwise XOR between N metaexpressions
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression bitNot(object par) {
            var r = new MetaExpressionBitwiseNot(par);
            return r;
        }


        /// <summary>
        /// Logical negation of a metaexpression
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression not(object par) {
            return tryEval(new MetaExpressionNot(par));
        }

        /// <summary>
        /// Return the argument with inverted sign
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression minus(object par) {
            return tryEval(new MetaExpressionMinus(par));
        }

        /// <summary>
        /// Return the year part of the argument
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression year(object par) {
            return tryEval(new MetaExpressionYear(par));
        }

        /// <summary>
        /// Returns the expression (par1 equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression eq(object par1, object par2) {
            return tryEval(new MetaExpressionEq(par1, par2));
        }

        /// <summary>
        /// Returns the expression (par1 equal field(par2))
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression eqf(object par1, string par2) {
            return tryEval(new MetaExpressionEq(par1, MetaExpression.field(par2)));
        }

       
        /// <summary>
        /// Returns the expression (fieldName equal sample[fieldName])
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static MetaExpression eqObj(string fieldName, object sample) {
            return tryEval(new MetaExpressionEq(fieldName, getField(fieldName,sample)));
        }

        /// <summary>
        /// Returns the expression (fieldName not equal sample[fieldName])
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static MetaExpression neObj(string fieldName, object sample) {
            return tryEval(new MetaExpressionNe(fieldName, getField(fieldName, sample)));
        }

        /// <summary>
        /// Returns the expression (par1 not equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression ne(object par1, object par2) {
            return tryEval(new MetaExpressionNe(par1, par2));
        }

        /// <summary>
        /// Returns the expression (par1 less than or equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression le(object par1, object par2) {
            return tryEval(new MetaExpressionLe(par1, par2));
        }

        /// <summary>
        /// Returns the expression (par1 greater than or equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression ge(object par1, object par2) {
            return tryEval(new MetaExpressionGe(par1, par2));
        }


        /// <summary>
        /// Returns the expression (par1 less than par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression lt(object par1, object par2) {
            return tryEval(new MetaExpressionLt(par1, par2));
        }


        /// <summary>
        /// Returns the expression (par1 greater than par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression gt(object par1, object par2) {
            return tryEval(new MetaExpressionGt(par1, par2));
        }

        /// <summary>
        /// Return the expression (par1 is null or par1=par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrEq(object par1, object par2) {
            return or(isNull(par1), eq(par1, par2));
            //return new MetaExpressionNullOrEq(par1, par2);
        }

        /// <summary>
        /// Returns the expression (par1 null or not equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrNe(object par1, object par2) {
            return or(isNull(par1), ne(par1, par2));
            //return new MetaExpressionNullOrNe(par1, par2);
        }
        
        /// <summary>
        /// Returns the expression (par1 null or less equal than par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrLe(object par1, object par2) {
            return or(isNull(par1), le(par1, par2));
            //return new MetaExpressionNullOrLe(par1, par2);
        }

        /// <summary>
        /// Returns the expression (par1 null or greater or equal par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrGe(object par1, object par2) {
            return or(isNull(par1), ge(par1, par2));
            //return new MetaExpressionNullOrGe(par1, par2);
        }

        /// <summary>
        /// Returns the expression (par1 null or less than par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrLt(object par1, object par2) {
            return or(isNull(par1), lt(par1, par2));
            //return new MetaExpressionNullOrLt(par1, par2);
        }

        /// <summary>
        /// Returns the expression (par1 null or greater than par2)
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        /// <returns></returns>
        public static MetaExpression nullOrGt(object par1, object par2) {
            return or(isNull(par1), gt(par1, par2));
            //return new MetaExpressionNullOrGt(par1, par2);
        }

        /// <summary>
        /// Check if the nbit (th) of par1 is set  
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="nbit"></param>
        /// <returns>(par1 &amp; 2^nbit) &lt;&gt;0 </returns>
        public static MetaExpression bitSet(object par1, int nbit) {
            return tryEval(new MetaExpressionBitSet(par1, nbit));
        }

        /// <summary>
        /// Check if the nbit (th) of par1 is NOT set 
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="nbit"></param>
        /// <returns>(par1 &amp; 2^nbit)=0 </returns>
        public static MetaExpression bitClear(object par1, int nbit) {
            return tryEval(new MetaExpressionBitClear(par1, nbit));
        }

        /// <summary>
        /// mCmp(obj,fields) is a shortcut for (r[field1]=sample[field1) and (r[field2]=sample[field2]) and...
        /// /// </summary>
        public static MetaExpression mCmp(object o, params string[] fields) {
            return tryEval(new MetaExpressionMCmp(o, fields));
        }

        /// <summary>
        /// mCmp(obj,fields) is a shortcut for (r[field1]=sample[field1) and (r[field2]=sample[field2]) and...
        /// /// </summary>
        public static MetaExpression mCmp(object o) {
            return tryEval(new MetaExpressionMCmp(o));
        }

        /// <summary>
        /// Get filter to retrieve childs
        //// </summary>
        public static MetaExpression mGetChilds(DataRow rParent, DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {
	        var childVal= new Dictionary<string, object>();
	        for (int i = 0; i < rel.ParentColumns.Length; i++) {
		        childVal[rel.ChildColumns[i].ColumnName] = rParent[rel.ParentColumns[i].ColumnName,ver];
	        }            
	        return new MetaExpressionMCmp(childVal);
        }

        /// <summary>
        /// Get filter to retrieve parents
        //// </summary>
        public static MetaExpression mGetParents(DataRow rChild, DataRelation rel,DataRowVersion ver=DataRowVersion.Default) {
	        var parentVal= new Dictionary<string, object>();
	        for (int i = 0; i < rel.ParentColumns.Length; i++) {
		        parentVal[rel.ParentColumns[i].ColumnName] = rChild[rel.ChildColumns[i].ColumnName,ver];
	        }
	        return new MetaExpressionMCmp(parentVal);
        }

        /// <summary>
        /// mCmp(obj,fields) is a shortcut for (r[field1]=sample[field1) and (r[field2]=sample[field2]) and...
        /// /// </summary>
        public static MetaExpression mCmp(object o, params DataColumn[] fields) {
            return tryEval(new MetaExpressionMCmp(o, fields));
        }

         /// <summary>
        /// keyCmp(obj,fields) is a shortcut for (r[field1]=sample[field1) and (r[field2]=sample[field2]) and... for 
        ///   each primary key column
        /// /// </summary>
        public static MetaExpression keyCmp(DataRow o) {
            return tryEval(new MetaExpressionMCmp(o, o.Table.PrimaryKey));
        }

        /// <summary>
        /// Shortcut for r[destColumn]= sample[sourceColumn]
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="sourceColumn"></param>
        /// <param name="destColumn"></param>
        /// <returns></returns>
        public static MetaExpression cmpAs(object sample, string sourceColumn, string destColumn) {
            return tryEval(new MetaExpressionCmpAs(sample, sourceColumn, destColumn));
        }

        /// <summary>
        /// Returns a constant undefined MetaExpression
        /// </summary>
        /// <returns></returns>
        public static MetaExpression undefinedExpression() {
            return new MetaExpressionUndefined();
        }

        /// <summary>
        /// Returns a constant null MetaExpression
        /// </summary>
        /// <returns></returns>
        public static MetaExpression nullExpression() {
            return new MetaExpressionNull();
        }

        /// <summary>
        /// Returns the expression (par is null)
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression isNull(object par) {
            return tryEval(new MetaExpressionIsNull(par));
        }

        /// <summary>
        ///  Returns the expression (par is not null)
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        public static MetaExpression isNotNull(object par) {
            return tryEval(new MetaExpressionIsNotNull(par));
        }

        /// <summary>
        /// Returns par if par is not null, otherwise def. It's the sql isnull function.
        /// </summary>
        /// <param name="par"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static MetaExpression isNullFn(object par, object def) {
            return tryEval(new MetaExpressionIsNullFn(par, def));
        }

        /// <summary>
        /// Shortcut for (r[field] &amp; mask)=val
        /// </summary>
        /// <param name="field"></param>
        /// <param name="mask"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static MetaExpression cmpMask(string field, ulong mask, ulong val) {
            var expr = new MetaExpressionEq(new MetaExpressionBitwiseAnd(
                                                new MetaExpressionField(field), new MetaExpressionConst(mask)),
                                            new MetaExpressionConst(val));
            return tryEval(expr);
        }

        /// <summary>
        /// Shortcut for (field in (o1, o2,...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static MetaExpression fieldIn(string field,  object[] arr) {
            return tryEval(new MetaExpressionFieldIn(field, arr));
        }


        /// <summary>
        /// Shortcut for (field in (o1, o2,...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static MetaExpression fieldIn(string field, MetaExpressionList list) {
            return tryEval(new MetaExpressionFieldIn(field, list));
        }



        /// <summary>
        /// Shortcut for (field in (o1[sourcefield], o2[sourcefield],...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="arr"></param>
        /// <param name="sourceField"></param>
        /// <returns></returns>
        public static MetaExpression fieldIn(object field, string sourceField, object[] arr) {
            return tryEval(new MetaExpressionFieldIn(field, sourceField, arr));
        }

        /// <summary>
        /// Shortcut for NOT(field in (o1, o2,...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="valueList"></param>
        /// <returns></returns>
        public static MetaExpression fieldNotIn(string field, params object[] valueList) {
            return tryEval(new MetaExpressionFieldNotIn(field, valueList));
        }

        /// <summary>
        /// Shortcut for NOT(field in (o1, o2,...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="valueList"></param>
        /// <returns></returns>
        public static MetaExpression fieldNotIn(string field, MetaExpressionList valueList) {
            return tryEval(new MetaExpressionFieldNotIn(field, valueList));
        }

        /// <summary>
        /// Shortcut for NOT(field in (o1[sourcefield], o2[sourcefield],...))
        /// </summary>
        /// <param name="field"></param>
        /// <param name="objectList"></param>
        /// <param name="sourceField"></param>
        /// <returns></returns>
        public static MetaExpression fieldNotIn(object field,  string sourceField, object[] objectList) {
            return tryEval(new MetaExpressionFieldNotIn(field, objectList, sourceField));
        }

        /// <summary>
        /// Return the expression in parenthesis
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static MetaExpression doPar(MetaExpression m) {
            return new MetaExpressionDoPar(m);
        }

        /// <summary>
        /// Returns the expression (field between min and max)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="min">Lower limit, ignored if  null</param>
        /// <param name="max">Upper limit, ignored if null</param>
        /// <returns>(min is null or min less equal field) and (max is null or max greater equal field) </returns>
        public static MetaExpression between(string field, object min, object max) {
            return new MetaExpressionAnd(
                MetaExpression.ge(field, min), MetaExpression.le(field, max)
                );
        }


        /// <summary>
        /// Returns the expression (o1 like o2)
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public static MetaExpression like(object o1, object o2) {
            return tryEval(new MetaExpressionLike(o1, o2));
        }


        /// <summary>
        /// Check if any of the given MetaExpressions is undefined
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static bool anyIsUndefined(MetaExpression[] fn) {
            foreach (MetaExpression m in fn) {
                if (m is null) continue;
                if (m.isUndefined()) return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any of the given MetaExpressions is null
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static bool anyIsNull(MetaExpression[] fn) {
            foreach (MetaExpression m in fn) {
                if (m is null) continue;
                if (m.isNull()) return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any of the given MetaExpressions is null or undefined
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static bool anyIsNullOrUndefined(MetaExpression[] fn) {
            foreach (MetaExpression m in fn) {
                if (m is null) continue;
                if (m.isNull()) return true;
                if (m.isUndefined()) return true;
            }
            return false;
        }

        private static object[] evaluateArray(MetaExpression[] m, object o , dynamic env) {
            object []resArray = new object[m.Length];
            for (int i = 0; i < m.Length; i++) {
                resArray[i] = m[i].apply(o, env);                
            }
            return resArray;
        }
        
        /// <summary>
        /// Aligns an array to a single type, upgrading types
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static object[] upgradeTypes(params object[] arr) {
            //check for any string, if a string is found converts anything to string
            bool anyString = false;
            bool anyDateTime = false;
            bool anyDecimal = false;
            bool anyDouble = false;
            bool anyFloat = false;
            bool anyLong = false;
            bool anyUnsigned = false; //converto anything to int64, otherwise int32 is sufficient
            object []res = new object[arr.Length];
            for (int i=0;i<arr.Length;i++) {
                object x = arr[i];
                if (DBNull.Value.Equals(x)) {
                    continue;
                }
                if (x == null) {
                    continue;
                }
                if (x is string) {
                    anyString = true;
                    continue;
                }

                if (x is decimal) {
                    anyDecimal = true;
                    continue;                    
                }

                if (x is double) {
                    anyDouble = true;
                    continue;
                }

                if (x is float) {
                    anyFloat = true;
                    continue;
                }

                if (x is uint) {
                    anyUnsigned = true;
                    continue;                    
                }
                if (x is long || x is ulong) {
                    anyLong = true;
                    continue;                    
                }

                if (x is DateTime) {
                    anyDateTime = true;
                    continue;
                }
            }

            var resType = typeof(int);
            if (anyDateTime) {
                resType = typeof(DateTime);
            }
            else if (anyString) {
                resType = typeof(string);
            }
            else if (anyDecimal) {
                    resType = typeof(decimal);
                }
            else if (anyDouble) {
                resType = typeof(double);
            }
            else if (anyFloat) {
                resType = typeof(float);
            }
            else if (anyUnsigned || anyLong) {
                resType = typeof(long);
            }
            
            return (from a in arr select typeConverter(a, resType)).ToArray();

        }

        static object typeConverter(object o, Type t) {
            try {
                if (o == null || o == DBNull.Value) return o;
                if (t == typeof(DateTime)) return Convert.ToDateTime(o);
                if (t == typeof(string)) return o.ToString();
                if (t == typeof(decimal)) return Convert.ToDecimal(o);
                if (t == typeof(double)) return Convert.ToDouble(o);
                if (t == typeof(float)) return Convert.ToSingle(o);
                if (t == typeof(long)) return Convert.ToInt64(o);
                if (t == typeof(int)) return Convert.ToInt32(o);
                return o;
            }
            catch (Exception e){
                throw new Exception($"Error converting {o} of type {o.GetType()} into {t}",e);
            }
        }
    }

    

    class MetaExpressionSys : MetaExpression {
        private readonly string _envName;
        public string EnvName => _envName; //for serialization pourposes

        public MetaExpressionSys(string envVariableName) {
            _envName = envVariableName;
            Name = "context.sys";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (env == null) return null;
            return env.GetSys(_envName);
            //var s = getField("sys",env);
            //if (s == null) return null;
            //return s[_envName];
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.quote(apply(null, env));
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return $"env.sys[{_envName}]";
        }
        public override string toString() {
            return $"context.sys[{_envName}]";
        }
    }

    class MetaExpressionUsr : MetaExpression {
        private readonly string _envName;
        public string EnvName => _envName; //for serialization purpouses

        public MetaExpressionUsr(string envVariableName) {
            _envName = envVariableName;
            Name = "context.usr";

        }

        public override object apply(object o = null, ISecurity env = null) {
            if (env == null)
                return null;
            return env.GetUsr(_envName);     
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.quote(apply(null, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"env.usr[{_envName}]";
        }
        public override string toString() {
            return $"context.usr[{_envName}]";
        }
    }

    class MetaExpressionConst : MetaExpression {
        public readonly object value;

        public MetaExpressionConst(object o) {
            value = o;
            if (o is ICloneable cl) value = cl.Clone();
            Info = ExpressionInfo.IsConstant;
            Name = "const";
            if (DBNull.Value.Equals(value)) {
                Info |= ExpressionInfo.IsNull;
                Name = "null";
                return;
            }
            if (value == null) {
                Info |= ExpressionInfo.IsUndefined;
                Name = "undefined";
                return;
            }
            if (value is bool b) {
                if (b) {
                    Info |= ExpressionInfo.IsTrue;
                }
                else {
                    Info |= ExpressionInfo.IsFalse;
                }
            }
        }

        public override object apply(object o = null, ISecurity env = null) {
            return value;
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            if (value == null) return "null";
            if (DBNull.Value.Equals(value)) return "DBNull.Value";
            var nfi = new NumberFormatInfo {
                NumberDecimalSeparator = ".",
                CurrencyGroupSeparator = ""
            };
            if (value is decimal d) return d.ToString(nfi)+"M";
            if (value is double db) return db.ToString(nfi)+"D";
            if (value is float f) return f.ToString(nfi)+"F";
            if (value is long l) return l.ToString() + "L";
            if (value is ulong ul) return ul.ToString() + "UL";
            if (value is uint ui) return ui.ToString() + "U";
            if (value is string s) return $"\"{s.Replace("\\","\\\\").Replace("\"","\\\"")}\"";
            return value.ToString();
        }


        public override string toSql(QueryHelper q, ISecurity env = null) {
            if (isUndefined()) return "(undefined)";
            //if (value is String) return value.ToString();
            return q.quote(value);
        }

        public override string toString() {
            if (value == null) return "(undefined)";
            if (DBNull.Value.Equals(value)) return "(null)";
            if (isTrue())return "(1=1)";
            if (isFalse())return "(1=0)";
            if (value.GetType()==typeof(string)) return $"'{value.ToString().Replace("'","''")}'";
            return $"{value}";
        }
    }

    class MetaExpressionNull : MetaExpressionConst {
        public MetaExpressionNull() : base(DBNull.Value) {
        }

    }

    class MetaExpressionUndefined : MetaExpressionConst {
        public MetaExpressionUndefined() : base(null) {
        }
    }

    class MetaExpressionIsNull : MetaExpression {
        public MetaExpressionIsNull(object par) {
            Parameters = getParams(new[] { par }, true);
            Name = "isNull";
        }

        public override object apply(object o = null, ISecurity env = null) {
            var result = calc(Parameters[0], o, env);
            if (result == null) return null;
            return result.Equals(DBNull.Value);
        }


        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.IsNull(Parameters[0].toSql(q, env));
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(DBNull.Value.Equal({Parameters[0].getCCode(c, varName, T)}))";            
        }

        public override string toString() {
            return $"{Parameters[0].toString()} is null";
        }
    }

    class MetaExpressionIsNotNull : MetaExpression {
        public MetaExpressionIsNotNull(object par) {
            Parameters = getParams(new[] { par }, true);
            Name = "isNotNull";
        }

        public override object apply(object o = null, ISecurity env = null) {
            var result = calc(Parameters[0], o, env);
            if (result == null) return null;
            return !result.Equals(DBNull.Value);
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.IsNotNull(Parameters[0].toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(!DBNull.Value.Equal({Parameters[0].getCCode(c,varName,T)}))";
        }

        public override string toString() {
            return $"{Parameters[0].toString()} is not null";
        }
    }
    

    class MetaExpressionField : MetaExpression {
        public MetaExpressionField(string fieldName, string tableName = null) {
            string[] parts = fieldName.Split('.');
            if (parts.Length == 1) {
                FieldName = fieldName;
                TableName = tableName;
            }
            else {
                TableName = parts[0];
                FieldName = parts[1];
            }
            Name = "field";
            Alias = FieldName;
        }

        int fieldIndex = -1;
        public System.Type fieldType = null;
        public override MetaExpression optimize(DataTable t, string alias = null) {
            if (TableName != alias) return this;
            fieldIndex = t.Columns.IndexOf(FieldName);
            fieldType = t.Columns[fieldIndex].DataType;
            return this;
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (fieldIndex >= 0) {
                if (o == null) return null;
                if (o is DataRow r) {
                    return r[fieldIndex];
                }
                object main = getField(TableName, o);                
                return ((DataRow)main)[fieldIndex];
            }
            if (TableName == null || joinContext == null) {
                //int h = metaprofiler.StartTimer("getfieldSimple");
                return getField(FieldName, o);
                //metaprofiler.StopTimer(h);
                //return res;
            }
            //Se c'è un joinContext, traduce la tableName 
            if (joinContext.ContainsKey(TableName)) {
                return getField(joinContext[TableName], FieldName, o);
            }                        
            return null;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            if (TableName == null) return FieldName;
            return $"{TableName}.{FieldName}";
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            if (T.IsAssignableFrom(typeof(DataRow))) {
                if (fieldIndex<0) return $"{varName}[\"{FieldName}\"]";
                return $"{varName}[{fieldIndex}]";
            }
            if (T.IsAssignableFrom(typeof(RowObject))) {
                return $"{varName}[\"{FieldName}\"]";
            }
            if (T == typeof(Dictionary<string, object>)) {
                return $"{varName}[\"{FieldName}\"]";
            }
            return $"{varName}.{FieldName}";
        }

        public override string toString() {
            string field = (Alias == null || Alias==FieldName)  ? FieldName : $"{FieldName} as {Alias}";
            if (TableName == null) return field;
            return $"Field({TableName}.{field})";
        }

    }


    /// <inheritdoc />
    public class MetaExpressionList : MetaExpression {
        
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="par"></param>
        public MetaExpressionList(params object[] par) {
            Parameters = getParams(par);
            Name = "list";
        }

        /// <inheritdoc />
        public override object apply(object o = null, ISecurity env = null) {
            if (isUndefined()) return null;
            var resultList = new List<object>();
            foreach (MetaExpression m in Parameters) {
                object operand = calc(m, o, env);
                if (operand == null) return null;
                if (operand.Equals(DBNull.Value)) return DBNull.Value;
                resultList.Add(operand);
            }

            return resultList.ToArray();
        }

        /// <inheritdoc />
        public override string toSql(QueryHelper q, ISecurity env = null) {
            return "("+string.Join(", ", MetaExpressionHelper.toSql(Parameters, q, env))+")";
        }

        /// <inheritdoc />
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"({String.Join(",", (from m in Parameters select "("+m.getCCode(c,varName,T)+")").ToArray())})";
        }

        /// <inheritdoc />
        public override string toString() {
            return $"({Parameters.toCommaSeparated()})";
        }
    }


    class MetaExpressionAdd : MetaExpression {

        public MetaExpressionAdd(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "add";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = null;
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;
            var toSum = new List<object>();
            bool anyUndefined=false;
            foreach (MetaExpression m in Parameters) {
                object operand = calc(m, o, env);
                if (DBNull.Value.Equals(operand)) return DBNull.Value;
                if (operand==null) {
                    anyUndefined=true;
                }
                if (anyUndefined)continue;
                toSum.Add(operand);
             }
            if (anyUndefined)return null;
            foreach (dynamic r in upgradeTypes(toSum.ToArray())) {
                if (result == null) {
                    result = r;
                    continue;
                }
                result += r;
            }
            return result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return string.Join(" + ", MetaExpressionHelper.toSql(Parameters, q, env));
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return $"({String.Join("+", (from m in Parameters select "("+m.getCCode(c,varName,T)+")").ToArray())})";
        }
        public override string toString() {             
            return string.Join(" + ", (from f  in Parameters select f.toString()));            
        }

    }

    class MetaExpressionModulus : MetaExpression {


        public MetaExpressionModulus(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "modulus";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;

            dynamic arr = upgradeTypes(calc(Parameters[0], o, env), calc(Parameters[1], o, env));
            if (DBNull.Value.Equals(arr[0])) return DBNull.Value;
            if (DBNull.Value.Equals(arr[1])) return DBNull.Value;
            if (arr[0]==null||arr[1]==null) return null;
            return arr[0] % arr[1];
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return string.Join(" % ", MetaExpressionHelper.toSql(Parameters, q, env));
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(({Parameters[0].getCCode(c, varName,T)})%({Parameters[1].getCCode(c, varName,T)}))";
        }

        public override string toString() {
            return $"{Parameters[0].toString()}%{Parameters[1].toString()})";
        }


    }

    class MetaExpressionSub : MetaExpression {

        public MetaExpressionSub(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "sub";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;

            dynamic arr = upgradeTypes(calc(Parameters[0], o, env), calc(Parameters[1], o, env));
            if (DBNull.Value.Equals(arr[0])) return DBNull.Value;
            if (DBNull.Value.Equals(arr[1])) return DBNull.Value;
            if (arr[0]==null||arr[1]==null)return null;
            return arr[0] - arr[1];
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return string.Join(" - ", MetaExpressionHelper.toSql(Parameters, q, env));
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(({Parameters[0].getCCode(c, varName,T)})-({Parameters[1].getCCode(c, varName,T)}))";
        }

        public override string toString() {
            return $"{Parameters[0].toString()}-{Parameters[1].toString()})";
        }

    }

    class MetaExpressionDiv : MetaExpression {

        public MetaExpressionDiv(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "div";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;

            dynamic arr = upgradeTypes(calc(Parameters[0], o, env), calc(Parameters[1], o, env));
            if (DBNull.Value.Equals(arr[0])) return DBNull.Value;
            if (DBNull.Value.Equals(arr[1])) return DBNull.Value;
            if (arr[0]==null||arr[1]==null) return null;
            return arr[0] / arr[1];
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return string.Join(" / ", MetaExpressionHelper.toSql(Parameters, q, env));
        }

        public override string getCCode(Compiler c, string varName,Type T) {
            return "((" + Parameters[0].getCCode(c, varName,T) + ")/(" + Parameters[1].getCCode(c, varName,T) + "))";
        }

        public override string toString() {
            return $"({Parameters[0].toString()}/{Parameters[1].toString()})";
        }

    }


    class MetaExpressionMul : MetaExpression {

        public MetaExpressionMul(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            foreach (MetaExpression o in Parameters) {
                if (!o.isConstant()) continue;
                dynamic v = o.apply(null, null);
                if (v == null) continue;
                if (v.Equals(0)||DBNull.Value.Equals(v)) {
                    Info |= ExpressionInfo.IsConstant;
                    break;
                }
            }
            Name = "mul";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = null;
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;
            var toMul = new List<object>();
            bool anyUndefined=false;
            foreach (MetaExpression m in Parameters) {
                object operand = calc(m, o, env);
                if (DBNull.Value.Equals(operand)) return DBNull.Value;
                if (operand == null)  anyUndefined=true;
                if (anyUndefined)continue;
                toMul.Add(operand);
            }
            if (anyUndefined)return null;
            foreach (dynamic r in upgradeTypes(toMul.ToArray())) {
                if (result == null) {
                    result = r;
                    continue;
                }
                result *= r;
            }
            return result;
           
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return string.Join(" * ", MetaExpressionHelper.toSql(Parameters, q, env));
        }
        public override string getCCode(Compiler c, string varName,Type T) {
            return "((" + Parameters[0].getCCode(c, varName,T) + ")*(" + Parameters[1].getCCode(c, varName,T) + "))";
        }


        public override string toString() {
            return string.Join(" * ", (from f  in Parameters select f.toString()));
            //return $"mul({Parameters.toCommaSeparated()})";
        }

    }

    class MetaExpressionSum<t> : MetaExpression {
        delegate object doSum(object res, object operand);
        public MetaExpressionSum(object par) {
            Parameters = new[] { fromObject(par, true) };
            Info |= ExpressionInfo.IsGrouping;
            if (anyIsUndefined(Parameters)) Info |= ExpressionInfo.IsUndefined;
            Name = "sum";
            
            if (typeof(t) == typeof(decimal)) {
                mySum = sumDecimal;
                return;
            }
            if (typeof(t) == typeof(float)) {
                mySum = sumFloat;
                return;
            }
            if (typeof(t) == typeof(double)) {
                mySum = sumDouble;
                return;
            }
            if (typeof(t) == typeof(int)) {
                mySum = sumInt32;
                return;
            }

            if (typeof(t)== typeof(short)) {
                mySum = sumInt16;
                return;
            }
            if (typeof(t) == typeof(long)) {
                mySum = sumInt64;
                return;
            }
        }
        object sumDecimal(object res, object operand) {
            return (decimal)res + (decimal)operand;
        }
        object sumFloat(object res, object operand) {
            return (float)res + (float)operand;
        }
        object sumDouble(object res, object operand) {
            return (double)res + (double)operand;
        }
        object sumInt64(object res, object operand) {
            return (long)res + (long)operand;
        }
        object sumInt32(object res, object operand) {
            return (int)res + (int)operand;
        }
        object sumInt16(object res, object operand) {
            return (short)((short)res + (short)operand);
        }
        doSum mySum;

        /// <summary>
        /// O should be an array of objects
        /// </summary>
        /// <param name="o"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public override object apply(object o = null, ISecurity env = null) {            
            if (isUndefined()) return null;
            if(!(o is object[] arr)) return null;
            var m = Parameters[0];
            bool someFound = false;
            object result = null;
            foreach (object oo in arr) {
                object operand = calc(m, oo, env);
                if (operand == null) return null;
                if (!someFound) {
                    result = operand;
                    someFound = true;
                    continue;
                }
                if (operand==DBNull.Value) continue;
                if (DBNull.Value.Equals(result)) {
                    result = operand;
                }
                else {
                    result = mySum(result, operand);
                }
            }
            return result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            //string[] allPar = (from par in Parameters select q.IsNullFn(par.toSql(q, env), 0) as string).ToArray();
            return $"SUM({Parameters[0].toSql(q,env)})";
        }

        public override string getCCode(Compiler c, string VarName,Type T) {
            var m = Parameters[0];
            string insideVar = c.GetNewVarName();
            return $"(from {insideVar} in O select {m.getCCode(c,insideVar,typeof(t))}).Sum()";
        }


        public override string toString() {
            return $"sum({Parameters[0].toString()})";
        }

    }

    class MetaExpressionCount : MetaExpression {

        public MetaExpressionCount(object par) {
            Parameters = getParams(new[] { par }, true);
            Info |= ExpressionInfo.IsGrouping;
            if (anyIsUndefined(Parameters)) Info |= ExpressionInfo.IsUndefined;
            Name = "count";
        }

        /// <summary>
        /// O should be an array of objects
        /// </summary>
        /// <param name="o"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public override object apply(object o = null, ISecurity env = null) {
            if (isUndefined()) return null;
            if (o == null) return null;
            //MetaExpression m = Parameters[0];
            object[] arr;
            if (o is Array) {
                arr = (object[])o;
            }
            else {
                arr = new object [] {};
            }
            return arr.Length;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            //string[] allPar = (from par in Parameters select q.IsNullFn(par.toSql(q, env), 0) as string).ToArray();
            return $"COUNT({Parameters[0].toSql(q, env)})";
        }

        public override string getCCode(Compiler c, string VarName, Type T) {
            return "(o.Length)";
        }

        public override string toString() {
            return $"COUNT({Parameters[0].toString()})";
        }

    }

    class MetaExpressionIsNullFn : MetaExpression {


        public MetaExpressionIsNullFn(object par1, object def) {
            Parameters = getParams(new[] { par1, def }, true);
            Name = "isNullFn";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic par1 = Parameters[0].apply(o, env);
            if (par1 == null) return null;
            if (!DBNull.Value.Equals(par1)) return par1;
            return Parameters[1].apply(o, env);
        }



        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            if (par2.isNull()) return q.IsNull(par1.toSql(q, env));
            if (par1.isNull()) return q.IsNull(par2.toSql(q, env));
            return q.IsNullFn(par1.toSql(q, env), par2.toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(({Parameters[0].getCCode(c, varName, T)})==DBNull.Value? ({Parameters[1].getCCode(c, varName, T)}):({Parameters[0].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"isnull({Parameters[0].toString()},{Parameters[1].toString()})";
        }

    }

    class MetaExpressionNot : MetaExpression {


        public MetaExpressionNot(object par) {
            Parameters = getParams(new[] { par });
            Name = "not";
        }

        public override object apply(object o = null, ISecurity env = null) {
            var result = calc(Parameters[0], o, env);
            if (result == null) return null;
            if (DBNull.Value.Equals(result)) return DBNull.Value;
            if (result.Equals(false)) return true;
            return false;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.Not(Parameters[0].toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(!({Parameters[0].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"not({Parameters[0].toString()})";
        }


    }

    class MetaExpressionBitwiseNot : MetaExpression {


        public MetaExpressionBitwiseNot(object par) {
            Parameters = getParams(new[] { par });
            Name = "~";
        }

        public override object apply(object o = null, ISecurity env = null) {
            var result = calc(Parameters[0], o, env);
            if (result == null) return null;
            if (result.Equals(DBNull.Value)) return DBNull.Value;
            if (result.Equals(false)) return true;
             if (result.Equals(true)) return false;
            dynamic ii  = result;
            return ~ii;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.BitwiseNot(Parameters[0].toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(~({Parameters[0].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"~({Parameters[0].toString()})";
        }


    }

    class MetaExpressionYear : MetaExpression {


        public MetaExpressionYear(object par) {
            Parameters = getParams(new[] { par },true);
            Name = "year";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = calc(Parameters[0], o, env);
            if (result == null) return null;
            if (result.Equals(DBNull.Value)) return DBNull.Value;
            if (result.GetType() != typeof(DateTime)) return DBNull.Value;
            return ((DateTime) result).Year;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return "year(" + Parameters[0].toSql(q, env)+")";
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"((varName==null)?null: ({Parameters[0].getCCode(c, varName, T)}).Year)";
        }

        public override string toString() {
            return $"year({Parameters[0].toString()})";
        }


    }


    class MetaExpressionMinus : MetaExpression {


        public MetaExpressionMinus(object par) {
            Parameters = getParams(new[] { par },true);
            Name = "minus";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = calc(Parameters[0], o, env);
            if (result == null) return null;
            if (result.Equals(DBNull.Value)) return DBNull.Value;
            return -result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return "-" + Parameters[0].toSql(q, env);
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(-({Parameters[0].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"-{Parameters[0].toString()}";
        }


    }

    class MetaExpressionBitSet : MetaExpression {
        public MetaExpressionBitSet(object par, int nbit) {
            Parameters = getParams(new[] { par, nbit },true);
            evaluateNullOrUndefined(Parameters);
            Name = "bitSet";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic operand = calc(Parameters[0], o, env);
            dynamic nBit = calc(Parameters[1], o, env);
            if (operand == null) return null;
            if (operand.Equals(DBNull.Value)) return DBNull.Value;
            if (nBit == null) return null;
            if (DBNull.Value.Equals(nBit)) return DBNull.Value;

            return (operand & (1 << nBit)) != 0;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.BitSet(Parameters[0].toSql(q, env), Convert.ToInt32(Parameters[1].apply()));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(-({Parameters[0].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"BitSet({Parameters[0].toString()},{Parameters[1].toString()})";

        }
    }

    class MetaExpressionBitClear : MetaExpression  {
        public MetaExpressionBitClear(object par, int nbit) {
            Parameters = getParams(new[] { par, nbit },true);
            evaluateNullOrUndefined(Parameters);
            Name = "bitClear";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic operand = calc(Parameters[0], o, env);
            dynamic nBit = calc(Parameters[1], o, env);
            if (operand == null) return null;
            if (operand.Equals(DBNull.Value)) return DBNull.Value;
            if (nBit == null) return null;
            if (DBNull.Value.Equals(nBit)) return DBNull.Value;

            return (operand & (1 << nBit)) == 0;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.BitClear(Parameters[0].toSql(q, env), Convert.ToInt32(Parameters[1].apply()));
        }

        public override string toString() {
            return $"BitClear({Parameters[0].toString()},{Parameters[1].toString()})";

        }
    }

    class MetaExpressionOr : MetaExpression {

        public MetaExpressionOr(params object[] par) {
            Parameters = getParams(par);
            var ParamOpt = new List<MetaExpression>();
            foreach (var m in Parameters) {
                if (m.isFalse()) continue;
                if (m is null) continue;
                if (m.isTrue()) {
                    ParamOpt.Add(m);
                    Info |= ExpressionInfo.IsTrue;
                    break;
                }
                ParamOpt.Add(m);
            }
            if (ParamOpt.Count==0)Info |= ExpressionInfo.IsFalse;
            Parameters=ParamOpt.ToArray();
            Name = "or";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (isTrue()) return true;
            if (isFalse()) return false;
            bool nullFound = false;
            bool dbNullFound = false;
            foreach (MetaExpression m in Parameters) {
                var result = m.apply(o, env);
                if (result == null) {
                    nullFound = true;
                    continue;
                }
                if (result.Equals(DBNull.Value)) {
                    dbNullFound = true;
                    continue;
                }
                if (result.Equals(true)) return true;

            }
            if (nullFound) return null;
            if (dbNullFound) return DBNull.Value;
            return false;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            //if (Parameters.Length == 1) {
            //    return new MetaExpressionDoPar(Parameters[0]).toSql(q,env);
            //}
            if (isTrue())return MetaExpression.constant(true).toSql(q,env);
            if (isFalse())return MetaExpression.constant(false).toSql(q,env);
            return q.AppOr(MetaExpressionHelper.toSql(Parameters, q, env));
        }

        public override string toString() {
            return string.Join(" or ", (from f  in Parameters select f.toString()));            
        }


    }

    class MetaExpressionAnd : MetaExpression, IGetCompDictionary {

	    private Dictionary<string, object> dictMcmp = null;

	    public Dictionary<string, object> getMcmpDictionary() {
		    return dictMcmp;
	    }

	    Dictionary<string, object> mergeDictionary(Dictionary<string, object> dest, Dictionary<string, object> source) {
		    if (source == null) return null;
		    if (dest == null) return null;
		    foreach (var v in source) dest[v.Key] = source[v.Key];
		    return dest;
	    }
        // pp = fn(....) >> una metaexpr
        // q.and(l, pp, p)
        public MetaExpressionAnd(params object[] par) {
            Parameters = getParams(par);
            var ParamOpt = new List<MetaExpression>();
            dictMcmp = new Dictionary<string, object>();
            foreach (var m in Parameters) {
                if (m.isTrue())continue;
                if (m is null)continue;
                if (m.isFalse()) {
                    ParamOpt.Add(m);
                    Info |= ExpressionInfo.IsFalse;
                    break;
                }
                ParamOpt.Add(m);
                dictMcmp= mergeDictionary(dictMcmp, (m as IGetCompDictionary)?.getMcmpDictionary() );
                
            }
            if (ParamOpt.Count==0)Info |= ExpressionInfo.IsTrue;
            Parameters= ParamOpt.ToArray();
            Name = "and";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (isFalse()) return false;
            if (isTrue()) return true;
            var nullFound = false;
            var dbNullFound = false;
            foreach (var m in Parameters) {
                var result = m.apply(o, env);
                if (result == null) {
                    nullFound = true;
                    continue;
                }
                if (result.Equals(DBNull.Value)) {
                    dbNullFound = true;
                    continue;
                }
                if (result.Equals(false)) return false;
            }
            if (nullFound) return null;
            if (dbNullFound) return DBNull.Value;
            return true;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            if (isTrue())return MetaExpression.constant(true).toSql(q,env);
            if (isFalse())return MetaExpression.constant(false).toSql(q,env);
            if (Parameters.Length == 1) {
                return new MetaExpressionDoPar(Parameters[0]).toSql(q,env);
            }
            return q.AppAnd(MetaExpressionHelper.toSql(Parameters, q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return "("+
                String.Join("&",(from m in Parameters select "("+m.getCCode(c,varName,T)+")").ToArray())+
                ")";
        }
        public override string toString() {
            return string.Join(" and ", (from f  in Parameters select f.toString()));
        }
    }
	
    /// <summary>
    /// mcmp(obj,fields) is a shortcut for (r[field1]=sample[field1) and (r[field2]=sample[field2]) and...
    /// /// </summary>
    class MetaExpressionMCmp : MetaExpression, IGetCompDictionary {
        private readonly string[] _fields;
		private readonly object _sample; //for serialization purpouses

		Dictionary<string, object> _sampleFields = new Dictionary<string, object>();
		public string[] fields => _fields; //for serialization purpouses
		public object sample => _sample; //for serialization purpouses
		public MetaExpressionMCmp(object sample, params DataColumn[] fields) {
            _fields = (from f in fields select f.ColumnName).ToArray();
            _fields._forEach(f => _sampleFields[f] = getField(f, sample));
            if (sample == null) Info |= ExpressionInfo.IsNull;
			_sample = sample;
			Name = "mcmp";
        }

		/// <summary>
		/// Returns the list of field-object to compare
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> getMcmpDictionary() {
			return _sampleFields;
		}

        public MetaExpressionMCmp(object sample) {
            if (sample == null) Info |= ExpressionInfo.IsNull;
            if (sample is Dictionary<string, object> dict) {
                _fields = dict.Keys.ToArray();
            }
            else {
                _fields = sample.GetType().GetMembers().Where(f => f.MemberType == MemberTypes.Property).Pick("Name").Cast<string>().ToArray();
            }

            //TupleElementNames MyAttribute = (TupleElementNames) Attribute.GetCustomAttribute(sample.GetType(), typeof (TupleElementNames));
            _fields._forEach(f => _sampleFields[f] = getField(f, sample));
            _sample = sample;

            Name = "mcmp";
        }


        public MetaExpressionMCmp(object sample, params string[] fields) {
            if (sample == null) Info |= ExpressionInfo.IsNull;
            _fields = fields;
            _fields._forEach(f => _sampleFields[f] = getField(f, sample));
            _sample = sample;

            Name = "mcmp";
        }

        public override object apply(object o = null, ISecurity env = null) {
            foreach (string s in _fields) {
                object o1 = getField(s, o);
                object o2 = _sampleFields[s];
                if (o1 == null || o2 == null) return null;
                var oo = upgradeTypes(o1, o2);
                if (!oo[0].Equals(oo[1])) return false;
            }
            return true;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.AppAnd((from f in _fields select q.CmpEq(f, _sampleFields[f])).ToArray());
        }

        public override string toString() {
            return $"mcmp({string.Join(",", _fields)})";
        }

    }

    class MetaExpressionCmpAs : MetaExpression {
        private readonly string _dest;
        private readonly object _sourceVal;
        private readonly string _source;
        public object sourceVal => _sourceVal;//for serialization pourposes
        public string dest => _dest;    //for serialization pourposes
		public string source => _source;    //for serialization pourposes

		public MetaExpressionCmpAs(object sample, string sourceColumn, string destColumn) {
            _sourceVal = getField(sourceColumn, sample);
            _dest = destColumn;
            _source = sourceColumn;
            if (sample == null) Info |= ExpressionInfo.IsNull;
            Name = "cmpAs";
        }



        public override object apply(object o = null, ISecurity env = null) {

            object o1 = getField(_dest, o);
            object o2 = _sourceVal;
            if (o1 == null || o2 == null) return null;
            var oo = upgradeTypes(o1, o2);
            if (!oo[0].Equals(oo[1])) return false;

            return true;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.CmpEq(q.Field(_dest), _sourceVal);
        }

        public override string toString() {
            return $"cmpAs({_source},{_dest})";
        }

    }

    class MetaExpressionEq : MetaExpression, IGetCompDictionary{


        public MetaExpressionEq(params object[] par) {
            Parameters = getParams(par, true);
            Name = "eq";
        }

        /// <summary>
        /// Returns the list of field-object to compare, null if it is not a field=value
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> getMcmpDictionary() {
			if (!(Parameters[0] is MetaExpressionField eqFun))
				return null;
			if (!(Parameters[1] is MetaExpressionConst valueFun))
				return null;
			return new Dictionary<string, object>(){{eqFun.FieldName , valueFun.value}};
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return String.Compare(par1, par2,true) == 0;
            return Equals(oo[0],oo[1]);
        }

        public override string toString() {
            return $"{Parameters[0].toString()}=={Parameters[1].toString()}";
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"({Parameters[0].getCCode(c,varName,T)}.Equals({Parameters[1].getCCode(c,varName,T)}))";
        }
        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            if (par2.isNull()) return q.IsNull(par1.toSql(q, env));
            if (par1.isNull()) return q.IsNull(par2.toSql(q, env));
            return q.UnquotedCmpEq(par1.toSql(q, env), par2.toSql(q, env));
        }

    }


    class MetaExpressionNe : MetaExpression {


        public MetaExpressionNe(params object[] par) {
            Parameters = getParams(par, true);
            Name = "ne";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return String.Compare(par1, par2,true) != 0;
            return !Equals(oo[0],oo[1]);
        }

        public override string toString() {
            return $"{Parameters[0].toString()}!={Parameters[1].toString()}";
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"(!{Parameters[0].getCCode(c, varName, T)}.Equals({Parameters[1].getCCode(c, varName, T)}))";
        }
        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            if (par2.isNull()) return q.IsNotNull(par1.toSql(q, env));
            if (par1.isNull()) return q.IsNotNull(par2.toSql(q, env));
            return q.UnquotedCmpNe(par1.toSql(q, env), par2.toSql(q, env));
        }

    }

    class MetaExpressionLe : MetaExpression {


        public MetaExpressionLe(params object[] par) {
            Parameters = getParams(par, true);
            Name = "le";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            if (DBNull.Value.Equals(oo[0])) return false;
            if (DBNull.Value.Equals(oo[1])) return false;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return String.Compare(par1, par2,true) <= 0;
            return par1 <= par2;
            //return ((t)par1).CompareTo((t)par2)<=0;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            return q.UnquotedCmpLe(par1.toSql(q, env), par2.toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"CompareHelper.cmpObjLe({Parameters[0].getCCode(c, varName, T)},{Parameters[0].getCCode(c, varName, T)})";

            //return "((" + Parameters[0].getCCode(c, varName, T) + ").CompareTo(" +
            //                Parameters[1].getCCode(c, varName, T) + ")<=0)";
        }
        public override string toString() {
            return $"{Parameters[0].toString()}<={Parameters[1].toString()}";
        }
    }

    class MetaExpressionLt : MetaExpression {


        public MetaExpressionLt(params object[] par) {
            Parameters = getParams(par, true);
            Name = "lt";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            if (DBNull.Value.Equals(oo[0])) return false;
            if (DBNull.Value.Equals(oo[1])) return false;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return String.Compare(par1, par2,true) < 0;
            //return ((t)par1).CompareTo((t)par2) < 0;
            return par1 < par2;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            return q.UnquotedCmpLt(par1.toSql(q, env), par2.toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return $"CompareHelper.cmpObjLt({Parameters[0].getCCode(c, varName, T)},{Parameters[0].getCCode(c, varName, T)})";
            //return "((" + Parameters[0].getCCode(c, varName, T) + ").CompareTo(" +
            //                Parameters[1].getCCode(c, varName, T) + ")<0)";
        }
        public override string toString() {
            return $"{Parameters[0].toString()}<{Parameters[1].toString()}";
        }


    }

    class MetaExpressionGe : MetaExpression {


        public MetaExpressionGe(params object[] par) {
            Parameters = getParams(par, true);
            Name = "ge";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            if (DBNull.Value.Equals(oo[0])) return false;
            if (DBNull.Value.Equals(oo[1])) return false;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return string.Compare(par1, par2,true) >= 0;
            //return ((t)par1).CompareTo((t)par2) >= 0;
            return par1 >= par2;
        }


        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            return q.UnquotedCmpGe(par1.toSql(q, env), par2.toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            Type detected = null;
            if(Parameters[0] is MetaExpressionField p0 && p0.fieldType != null) {
                detected = p0.fieldType;
            }
            else {
                if(Parameters[1] is MetaExpressionConst p1) {
                    if(p1.value != null)
                        detected = p1.value.GetType();
                }
            }
         
            if (detected != null) {
                if (typeof(IComparable).IsAssignableFrom(detected)) {
                    return $"{$"CompareHelper.cmpObjGe<{detected.Name}>("}{Parameters[0].getCCode(c, varName, T)},{Parameters[1].getCCode(c, varName, T)})";                    
                }
                if (detected ==typeof(string)) {
                    return $"(({Parameters[0].getCCode(c, varName, T)}).CompareTo({Parameters[1].getCCode(c, varName, T)})>=0)";
                }
            }
            return $"(({Parameters[0].getCCode(c, varName, T)})>=({Parameters[1].getCCode(c, varName, T)}))";
        }
        public override string toString() {
            return $"{Parameters[0].toString()}>={Parameters[1].toString()}";
        }

    }

    class MetaExpressionGt : MetaExpression {


        public MetaExpressionGt(params object[] par) {
            Parameters = getParams(par, true);
            Name = "gt";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic oo = upgradeTypes(Parameters[0].apply(o, env), Parameters[1].apply(o, env));
            if (oo[0] == null || oo[1] == null) return null;
            if (DBNull.Value.Equals(oo[0])) return false;
            if (DBNull.Value.Equals(oo[1])) return false;
            var par1 = oo[0];
            var par2 = oo[1];
            if (par1 is string) return String.Compare(par1, par2,true) > 0;
            //return ((t)par1).CompareTo((t)par2) > 0;
            return par1 > par2;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            var par1 = Parameters[0];
            var par2 = Parameters[1];
            return q.UnquotedCmpGt(par1.toSql(q, env), par2.toSql(q, env));
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return "((" + Parameters[0].getCCode(c, varName, T) + ").CompareTo(" +
                            Parameters[1].getCCode(c, varName, T) + ")>0)";
        }
        public override string toString() {
            return $"{Parameters[0].toString()}>{Parameters[1].toString()}";
        }

    }


    class MetaExpressionBitwiseAnd : MetaExpression {


        public MetaExpressionBitwiseAnd(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "&";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = null;
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;
            bool anyUndefined=false;
            foreach (MetaExpression m in Parameters) {
                dynamic operand = calc(m, o, env);
                if (DBNull.Value.Equals(operand)) return DBNull.Value;
                if (operand == null) anyUndefined=true;
                if (anyUndefined)continue;
                if (result == null) {
                    result = operand ;
                    continue;
                }
                result &= operand;
            }
            if (anyUndefined)return null;
            return result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.BitwiseAnd(MetaExpressionHelper.toSql(Parameters, q, env));            
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            return "(" +
                String.Join("&", (from m in Parameters select "(" + m.getCCode(c, varName, T) + ")").ToArray()) +
                ")";
        }
        public override string toString() {
            return $"&({Parameters.toCommaSeparated()})";
        }
    }

    class MetaExpressionBitwiseOr : MetaExpression {


        public MetaExpressionBitwiseOr(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);
            Name = "|";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = null;
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;
             bool anyUndefined=false;
            foreach (MetaExpression m in Parameters) {
                dynamic operand = calc(m, o, env);
                if (DBNull.Value.Equals(operand)) return DBNull.Value;
                if (operand == null) anyUndefined = true;
                if (anyUndefined) continue;
                if (result == null) {
                    result = operand;
                    continue;
                }
                result |= operand;
            }
            if (anyUndefined)return null;
            return result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.BitwiseOr(MetaExpressionHelper.toSql(Parameters, q, env));         
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return "(" +
                String.Join("|", (from m in Parameters select "(" + m.getCCode(c, varName, T) + ")").ToArray()) +
                ")";
        }
        public override string toString() {
            return $"|({Parameters.toCommaSeparated()})";
        }
    }
    

    class MetaExpressionBitwiseXor : MetaExpression {


        public MetaExpressionBitwiseXor(params object[] par) {
            Parameters = getParams(par);
            evaluateNullOrUndefined(Parameters);;
            Name = "^";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic result = null;
            if (isNull()) return DBNull.Value;
            if (isUndefined()) return null;
            bool anyUndefined=false;
            foreach (MetaExpression m in Parameters) {
                dynamic operand = calc(m, o, env);
                if (DBNull.Value.Equals(operand)) return DBNull.Value;
                if (operand == null) anyUndefined=true;
                if (anyUndefined)continue;
                if (result == null) {
                    result = operand;
                    continue;
                }
                result ^= operand;
            }
            if (anyUndefined)return null;
            return result;
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
           return q.BitwiseXor(MetaExpressionHelper.toSql(Parameters, q, env));         
        }
        public override string getCCode(Compiler c, string varName, Type T) {
            return "(" +
                String.Join("^", (from m in Parameters select "(" + m.getCCode(c, varName, T) + ")").ToArray()) +
                ")";
        }
        public override string toString() {
            return $"^({Parameters.toCommaSeparated()})";
        }
    }





    class MetaExpressionFieldIn : MetaExpression {
        private readonly MetaExpression[] _arr;
        private readonly string _sourceColumn;
		private readonly object[] _parObject; //for serialization purpouses
		public object[] parObject => _parObject; //for serialization purpouses
		public string sourceColumn => _sourceColumn; //for serialization purpouses

        public MetaExpressionFieldIn(string field, MetaExpressionList listExpr) {
            Parameters = new[] { fromObject(field, true) };
            _arr = (from val in listExpr.Parameters select val).ToArray();
            _parObject = _arr;
            _sourceColumn = field;
            Name = "fieldIn";
            if (_arr.Length == 0) Info |= ExpressionInfo.IsFalse;
        }

		public MetaExpressionFieldIn(string field, object[] arr) {
            Parameters = new[] { fromObject(field, true) };
            _arr = (from element in arr select fromObject(element)).ToArray();
			_parObject = arr;
			_sourceColumn = field;
            Name = "fieldIn";
            if (arr.Length == 0) Info |= ExpressionInfo.IsFalse;
        }

        public MetaExpressionFieldIn(object field,string sourceColumn, object[] arr ) {
            Parameters = new[] { fromObject(field, true) };
			_parObject = arr;
			_arr = (from element in arr select new MetaExpressionConst(getField(sourceColumn, element))).ToArray();
            _sourceColumn = sourceColumn;
            if (arr.Length == 0) Info |= ExpressionInfo.IsFalse;
            Name = "fieldIn";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (_arr.Length == 0) return false;
            dynamic par = Parameters[0].apply(o, env);
            if (par == null) return null;
            if (DBNull.Value.Equals(par)) return DBNull.Value;
            foreach (object oo in _arr) {
                var evaluated = calc(oo, o, env);
                var pp = upgradeTypes(par, evaluated);
                if (Equals(pp[0],pp[1])) return true;
            }
            return false;

        }

        public override string toString() {
            string allVal = String.Join(",", (from s in _arr select $"{s.toString()}").ToArray());
            return $"{Parameters[0].toString()} in ({allVal})";
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            var allVal = (from s in _arr select $"{s.toSql(q,env)}").ToArray();
            return q.UnquotedFieldIn(Parameters[0].toSql(q, env), allVal);
        }
    }


    class MetaExpressionFieldNotIn : MetaExpression {
        private readonly MetaExpression[] _arr;
        private readonly string _sourceColumn;
		private readonly object[] _parObject; //for serialization purpouses
		public object[] parObject => _parObject; //for serialization purpouses
		public string sourceColumn => _sourceColumn; //for serialization purpouses

        public MetaExpressionFieldNotIn(string field, MetaExpressionList listExpr) {
            Parameters = new[] { fromObject(field, true) };
            _arr = (from val in listExpr.Parameters select val).ToArray();
            _parObject = _arr;
            _sourceColumn = field;
            Name = "fieldNotIn";
            if (_arr.Length == 0) Info |= ExpressionInfo.IsFalse;
        }

		public MetaExpressionFieldNotIn(string field, object[] arr) {
            Parameters = new[] { fromObject(field, true) };
		    _arr = (from element in arr select fromObject(element)).ToArray();
			_parObject = arr;
			_sourceColumn = field;
            Name = "fieldNotIn";
            if (arr.Length == 0) Info |= ExpressionInfo.IsTrue;
        }

        public MetaExpressionFieldNotIn(object field, object[] arr, string sourceColumn) {
            Parameters = new[] { fromObject(field, true) };
            _arr = (from element in arr select new MetaExpressionConst(getField(sourceColumn, element))).ToArray();
			_parObject = arr;
			_sourceColumn = sourceColumn;
            if (arr.Length == 0) Info |= ExpressionInfo.IsTrue;
            Name = "fieldNotIn";
        }

        public override object apply(object o = null, ISecurity env = null) {
            if (_arr.Length == 0) return true;
            dynamic par = Parameters[0].apply(o, env);
            if (par == null) return null;
            if (DBNull.Value.Equals(par)) return DBNull.Value;
            foreach (object oo in _arr) {
                var pp = upgradeTypes(par, oo);
                if (Equals(pp[0],pp[1]))  return false;
            }
            return true;

        }

        public override string toString() {
            string allVal = String.Join(",", (from s in _arr select $"{s.toString()}").ToArray());
            return $"{Parameters[0].toString()} not in ({allVal})";
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
	        var allVal = (from s in _arr select $"{s.toSql(q,env)}").ToArray();
	        return q.UnquotedFieldNotIn(Parameters[0].toSql(q, env), allVal);
        }
    }
    class MetaExpressionDoPar : MetaExpression {
        public MetaExpressionDoPar(MetaExpression m) {
            Parameters = new[] { m };
            Name = "doPar";
            Info = m.Info;
        }

        public override object apply(object o = null, ISecurity env = null) {
            return Parameters[0].apply(o, env);
        }

        public override string toString() {
             string internalExpr = Parameters[0].toString();
            if (internalExpr.StartsWith("(")&& internalExpr.EndsWith(")")) return internalExpr;
            return $"({internalExpr})";
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            string internalExpr = Parameters[0].toSql(q, env);
            if (internalExpr.StartsWith("(")&& internalExpr.EndsWith(")")) return internalExpr;
            return q.DoPar(internalExpr);
        }
    }

    class MetaExpressionLike : MetaExpression {

        bool likeFn(string s1, string s2) {
            if (s1 == null || s2 == null) return false;
            return s1.SqlLike(s2);
        }
        public MetaExpressionLike(object o1, object o2) {
            Parameters = new[] { fromObject(o1, true), fromObject(o2, false) };
            Name = "like";
        }

        public override object apply(object o = null, ISecurity env = null) {
            dynamic par1 = Parameters[0].apply(o, env);
            dynamic par2 = Parameters[1].apply(o, env);
            if (DBNull.Value.Equals(par1)) return DBNull.Value;
            if (DBNull.Value.Equals(par2)) return DBNull.Value;

            if (par1 == null) return null;
            if (par2 == null) return null;

            if (!(par1 is string)) return false;
            if (!(par2 is string)) return false;
            return likeFn(par1 as string, par2 as string);
        }

        public override string toString() {
            return $"{Parameters[0].toString()} LIKE {Parameters[1].toString()}";
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return q.UnquotedLike(Parameters[0].toSql(q, env), Parameters[1].toSql(q, env));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class MetaExpressionWithEnv :MetaExpression {
        MetaExpression M;
        ISecurity env;
        public MetaExpressionWithEnv(MetaExpression M, ISecurity env) {
            this.M = M;
            this.env = env;
            Name = "WithEnv";
        }

        public override object apply(object o = null, ISecurity env = null) {
           return M.apply(o, this.env);            
        }

        public override string toSql(QueryHelper q, ISecurity env = null) {
            return M.toSql(q,this.env);
        }

        public override string getCCode(Compiler c, string varName, Type T) {
            throw new NotImplementedException("Compilation not supported in withEnv MetaExpression");
        }

        public override string toString() {
            return M.toString();
        }

    }


}


