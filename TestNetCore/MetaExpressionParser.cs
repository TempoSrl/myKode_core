using System;

using mdl;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using q = mdl.MetaExpression;
using parser = mdl.MetaExpressionParser;
using NUnit.Framework;


namespace TestNetCore {
    /// <summary>
    /// Summary description for MetaExpressionParser
    /// </summary>
    [TestFixture]
    public class Test_Token {
        [SetUp]
        public void testInit() {
        }
        [TearDown]
        public void testEnd() {
        }



        [Test]
        public void OpenParTest() {
            string s = "(";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.openPar);
            Assert.AreEqual(t.value.ToString(),"(");
            Assert.AreEqual(1,pos);


            s = "  (";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.openPar);
            Assert.AreEqual(t.value.ToString(),"(");
            Assert.AreEqual(s.Length,pos);
        }

        [Test]
        public void CloseParTest() {
            string s = ")";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.closedPar);
            Assert.AreEqual(t.value.ToString(),")");
            Assert.AreEqual(s.Length,pos);
        }


        [Test]
        public void NumberTest() {
            string s = "12.23";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(t.value,12.23);


            s = "12";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(t.value,12.0);

            s = "12.123456";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(t.value,12.123456);
        }

        [Test]
        public void StringTest() {
            string s = "'abcde 1'2";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            string value = "'abcde 1'";
            Assert.AreEqual("'"+t.value+"'",value);
            Assert.AreEqual(value.Length,pos);


            s = "'abcde '' 1' 12 12";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            value = "'abcde ' 1'";
            string inString = "'abcde '' 1'";
            Assert.AreEqual("'"+t.value+"'",value);
            Assert.AreEqual(inString.Length,pos);

            s = "'abcde '' ''n asjkaj 292 1'";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(t.kind, tokenKind.constant);
            value = "'abcde ' 'n asjkaj 292 1'";
            inString = "'abcde '' ''n asjkaj 292 1'";
            Assert.AreEqual("'"+t.value+"'",value);
            Assert.AreEqual(inString.Length,pos);
        }

        [Test]
        public void FieldNameTest() {
            string s = "NOT1";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName, t.kind);
            Assert.AreEqual(s.Length, pos);
            Assert.AreEqual(s, t.content);


            s = "NOT132 12";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName, t.kind);
            string value = "NOT132";
            string inString = "NOT132";
            Assert.AreEqual(t.content,value);
            Assert.AreEqual(inString.Length,pos);

            s = "NO1T13A2 12 A";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName, t.kind);
            value = "NO1T13A2";
            inString = "NO1T13A2";
            Assert.AreEqual(t.content,value);
            Assert.AreEqual(inString.Length,pos);
        }

        [Test]
        public void OperatorsTest() {
            string s = "NOT";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(s.ToLower(),t.content);


            s = "IS NULL";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(s.ToLower(),t.content);


            s = "IS NOT NULL";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            Assert.AreEqual(s.Length,pos);
            Assert.AreEqual(s.ToLower(),t.content);

            s = "IS NOT NULL2";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName,t.kind);
            string value = "IS";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);

            s = "IS NULL XX";
            pos = 0;
            t = Token.getToken(s, ref pos);
            value = "IS NULL";
            Assert.AreEqual(tokenKind.operatore,t.kind);
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value.ToLower(),t.content);

            s = "IS   NULL XX";
            pos = 0;
            t = Token.getToken(s, ref pos);
            value = "IS NULL";
            string inString = "IS   NULL";
            Assert.AreEqual(tokenKind.operatore,t.kind);
            Assert.AreEqual(inString.Length,pos);
            Assert.AreEqual(value.ToLower(),t.content);

            s = "ISNULL XX";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            value = "ISNULL";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value.ToLower(),t.content);


            s = "ISNULLO XX";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName,t.kind);
            value = "ISNULLO";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);

            s = "IS NULLL XX";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.fieldName,t.kind);
            value = "IS";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);


            s = "<=";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            value = "<=";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);



            s = "<>";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            value = "<>";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);

            s = "=";
            pos = 0;
            t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore,t.kind);
            value = "=";
            Assert.AreEqual(value.Length,pos);
            Assert.AreEqual(value,t.content);
        }

        [Test]
        public void IsNullFnTest() {
            string s = "isnull(1,2)";
            int pos = 0;
            Token t = Token.getToken(s, ref pos);
            Assert.AreEqual(tokenKind.operatore, t.kind);
            string value = "isnull";
            Assert.AreEqual(value.Length, pos);
            Assert.AreEqual(value, t.content);
        }


    }



    [TestFixture]
    public class Test_MetaExpressionParser {
        [SetUp]
        public void testInit() {
        }

        [TearDown]
        public void testEnd() {
        }



        [Test]
        public void ConstCheck() {
            string s = "12";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.isConstant());
            Assert.AreEqual(expr.apply(), 12);
        }

        [Test]
        public void FieldCheck() {
            string s = "a";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual(expr.Name,"field");
            Assert.AreEqual(expr.FieldName,"a");
        }

        [Test]
        public void FieldIsNullCheck() {
            string s = "a is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual(expr.Name,"isNull");
            Assert.IsNull(expr.FieldName);
            Assert.AreEqual(expr.toString(),"a is null");
        }

        [Test]
        public void FieldIsNotNullCheck() {
            string s = "a  is not  null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual(expr.Name,"isNotNull");
            Assert.IsNull(expr.FieldName);
            Assert.AreEqual("a is not null",expr.toString());
        }

        [Test]
        public void FieldIsNotNullInParCheck() {
            string s = "(a  is not  null)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual(expr.Name,"doPar");
            Assert.IsNull(expr.FieldName);
            Assert.AreEqual("(a is not null)",expr.toString());
        }

        [Test]
        public void composed2AndCheck() {
            string s = "(a  is not  null) and (b is null)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
            Assert.AreEqual("(a is not null) and (b is null)", expr.toString());
        }

        [Test]
        public void composedAndOrCheck() {
            string s = "a  is not  null and b is null and c=1 or z='a'";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a is not null and b is null and c==1 or z=='a'", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck2() {
            string s = "a  is not  null and b is null and c=1 or z='a' and q is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.IsFalse(expr.isConstant());
            string ss = expr.toString();
            Assert.AreEqual("a is not null and b is null and c==1 or z=='a' and q is null", expr.toString());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck3() {
            string s = "(a  is not  null and b is null and c=1 or z='a') and q is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("(a is not null and b is null and c==1 or z=='a') and q is null", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
        }


        [Test]
        public void composedAndOrCheck4() {
            string s = "(a  is not  null or b is null) and q is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("(a is not null or b is null) and q is null", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck5() {
            string s = "q is null and (a  is not  null or b is null) ";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("q is null and (a is not null or b is null)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck6() {
            string s = "q is null and a  is not  null or b is null or c is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("q is null and a is not null or b is null or c is null", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck7() {
            string s = "q is null or a  is not  null and b is null and c is null";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("q is null or a is not null and b is null and c is null", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void composedAndOrCheck8() {
            string s = "q is null or a  is not  null and (b is null and c is null)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("q is null or a is not null and (b is null and c is null)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void InCheck() {
            string s = "(a in (2,3,4,5))";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("(a in (2,3,4,5))", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("doPar",expr.Name);
            Assert.IsNull(expr.FieldName);
        }
        public void InCheck2() {
            string s = "a in (2,3,4,5)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a in (2,3,4,5))", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("fieldIn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }
        [Test]
        public void NotInCheck() {
            string s = "a not in (2,3,4,5)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a not in (2,3,4,5)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("fieldNotIn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void NotInCheck2() {
            string s = "a not in (2,3,4,5) or c=1";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a not in (2,3,4,5) or c==1", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void isNullFn() {
            string s = "isnull(a,1)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("isnull(a,1)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("isNullFn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void isNullFn2() {
            string s = "isnull(isnull(a,1),2)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("isnull(isnull(a,1),2)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("isNullFn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void isNullFn2_1() {
            string s = " a or isnull(a,1)";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a or isnull(a,1)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("or",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void isNullFn3() {
            string s = "isnull(a,isnull(b,c))";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("isnull(a,isnull(b,c))", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("isNullFn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void isNullFn4() {
            string s = "isnull(q,isnull(a,isnull(c,3)))";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("isnull(q,isnull(a,isnull(c,3)))", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("isNullFn",expr.Name);
            Assert.IsNull(expr.FieldName);
        }


        [Test]
        public void sys_var() {
            string s = "<%sys[alfa]%>";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("context.sys[alfa]", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("context.sys",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void usr_var() {
            string s = "<%usr[beta]%>";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("context.usr[beta]", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("context.usr",expr.Name);
            Assert.IsNull(expr.FieldName);
        }


        [Test]
        public void env_var() {
            string s = "<%variable_Name%>";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("context(variable_Name)", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("context",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void env_var_aggregate() {
            string s = "<%variable_Name%>=3 and <%usr[beta]%>='S'";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("context(variable_Name)==3 and context.usr[beta]=='S'", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void a_plus_b() {
            string s = "a+b";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a + b", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("add",expr.Name);
            Assert.IsNull(expr.FieldName);
            dynamic x = new ExpandoObject();
            x.a = 1;
            x.b = 2;
            Assert.AreEqual(3, expr.apply(x));

            dynamic y = new ExpandoObject();
            y.a = 1;
            y.b = " is one";
            Assert.AreEqual("1 is one", expr.apply(y));
        }

        [Test]
        public void a_like_b() {
            string s = "a like b";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a LIKE b", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("like",expr.Name);
            Assert.IsNull(expr.FieldName);
        }

        [Test]
        public void a_like_bexpr() {
            string s = "a like b+'%'";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a LIKE b + '%'", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("like",expr.Name);
            Assert.IsNull(expr.FieldName);

            dynamic x = new ExpandoObject();
            x.a = "babbo natale";
            x.b = "babbo";
            Assert.AreEqual(true, expr.apply(x));
            x.a = "befana";
            Assert.AreEqual(false, expr.apply(x));

        }

        [Test]
        public void between_1_2() {
            string s = "a between 1 and 2";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("a>=1 and a<=2", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("and",expr.Name);
            Assert.IsNull(expr.FieldName);
            

        }

         [Test]
        public void expressionWithPar() {
            string s = "(1+a)=2";
            q expr = parser.From(s);
            Assert.IsNotNull(expr);
            Assert.AreEqual("(1 + a)==2", expr.toString());
            Assert.IsFalse(expr.isConstant());
            Assert.AreEqual("eq",expr.Name);
            Assert.IsNull(expr.FieldName);
            

        }

    }

    
}
