using System;
using NUnit.Framework;
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
using Microsoft.VisualStudio;
using msUnit = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace test {
   [TestFixture]
    public class MetaExpressionTest {
        [SetUp]
        public static void testInit() {

        }

        [TearDown]
        public static void testEnd() {

        }

        [Test]
        public void add_test() {
            q f = q.field("x");

            q m = q.add(3, 5);
            Assert.AreEqual(m.apply(10), 8, "add of integer constant is ok");

            m = q.add(3, "5");
            Assert.AreEqual(m.apply(10), "35", "add of integer and constant is ok, result is a string");

            m = q.add(f, 3);
            Assert.AreEqual(5, m.apply(new { x = 2 }), "add of function with constant is ok");

            q h = q.field("y");
            m = q.add(f, h);
            Assert.AreEqual(5, m.apply(new { x = 2, y = 3 }), "add of function with function is ok");

            m = q.add(null,f);
            Assert.AreEqual(null,m.apply(new { x = 2 }), "add function with null constant is ok");

            m = q.add(3.1, 5);
            Assert.AreEqual(m.apply(10), 8.1, "add of decimal constant is ok");

            m = q.add(3.1, null);
            Assert.AreEqual(m.apply(10), null, "add with null constant is ok");

            m = q.add(3.1, DBNull.Value);
            Assert.AreEqual(m.apply(10), DBNull.Value, "add with DBNull constant is ok");

            m = q.add(3.1, DBNull.Value, null);
            Assert.AreEqual(m.apply(10), DBNull.Value, "add with DBNull and null constant is ok");

        }

        [Test]
        public void sub_test() {
            q f = q.field("x");

            q m = q.sub(5, 3);
            Assert.AreEqual(2,m.apply(10), "sub of integer constant is ok (op1>op2)");

            m = q.sub(5, 3.1);
            Assert.AreEqual(1.9, m.apply(10), "sub of decimal constant is ok (op2>op1)");

            m = q.sub(3, 5);
            Assert.AreEqual(-2, m.apply(10), "sub of integer constant is ok (op1>op2)");

            m = q.sub(5.1, 3.1);
            Assert.AreEqual(2d, (double)m.apply(10),0.00001,"sub of all numbers decimal is ok");

            m = q.sub(3.1, null);
            Assert.AreEqual(null, m.apply(10), "sub with null constant is ok");

            m = q.sub(DBNull.Value,3.1);
            Assert.AreEqual(DBNull.Value, m.apply(10), "sub with DBNull constant is ok");

            m = q.sub(f, 2);
            Assert.AreEqual(0, m.apply(new { x = 2 }, "sub function with constant is ok"));

            q g = q.field("y");
            m = q.sub(f, g);
            Assert.AreEqual(2, m.apply(new { x = 4, y = 2 }), "sub function with function is ok");

        }

        [Test]
        public void mul_test() {
            q f = q.field("x");

            q m = q.mul(5, 3);
            Assert.AreEqual(15, m.apply(10), "mul of integer constant is ok");

            m = q.mul(4, 3.5);
            Assert.AreEqual(14, m.apply(10), "mul of double constant is ok");

            m = q.mul(4, null);
            Assert.AreEqual(null, m.apply(10), "mul with null constant is ok");

            m = q.mul(4, DBNull.Value);
            Assert.AreEqual(DBNull.Value, m.apply(10), "mul with DBNull constant is ok");

            m = q.mul(f, 3);
            Assert.AreEqual(9, m.apply(new { x = 3 }), "mul of integer constant with function is ok");

            m = q.mul(f, 0);
            Assert.AreEqual(0, m.apply(new { x = 3 }), "mul of 0 with function is ok");

            m = q.mul(f, 2,3);
            Assert.AreEqual(18, m.apply(new { x = 3 }), "mul with function and multiple integer constant is ok");

            q g = q.field("y");
            m = q.mul(f, g);
            Assert.AreEqual(14, m.apply(new { x = 3.5, y = 4 }), "mul of function with function is ok");
        }

        [Test]
        public void div_test() {
            q f = q.field("x");
            q m = q.div(10, 2);
            Assert.AreEqual(5, m.apply(10), "div of integer constant is ok");

            m = q.div(10, 2.5);
            Assert.AreEqual(4d, m.apply(10), "div of decimal constant is ok");

            m = q.div(2.0, 10);
            Assert.AreEqual(0.2, m.apply(10), "div of decimal constant is ok");

            m = q.div(2, 10);
            Assert.AreEqual(0, m.apply(10), "div of int with constant ( op2>op1 ) is ok");

            Assert.Throws<DivideByZeroException>(() => q.div(10, 0),"div by zero exception");

            q h = q.field("y");
            q g = q.div(h, f);
            Assert.AreEqual(0.5, g.apply(new { x = 10,y=5d }), "div of constant with function is ok");

            f = q.mul(f, q.constant(2));
            g = q.div(f, 5);
            Assert.AreEqual(4,g.apply(new { x = 10 }),"div of constant with function is ok");

            f = q.field("x");
            g = q.div(f, 5.2);
            Assert.AreEqual(1.92, (double)g.apply(new { x = 10 }),0.005, "div of decimal with function is ok");

            m = q.div(0, 10);
            Assert.AreEqual(0, m.apply(10), "div of decimal constant is ok");

            m = q.div(3.1, null);
            Assert.AreEqual(null, m.apply(10), "div with null constant is ok");

            m = q.div(DBNull.Value, 3.1);
            Assert.AreEqual(DBNull.Value, m.apply(10), "div with DBNull constant is ok");

        }

       [Test]
       public void cascadeSetTable_test() {
           q f = q.field("x");
           f.cascadeSetTable("tab1");
           q m = q.div(f, 2);
           string sql = m.toSql(new CQueryHelper());
           Assert.AreEqual("tab1.x / 2", sql);
           Assert.AreEqual("(Field(tab1.x)/2)", m.toString());
       }

       [Test]
        public void or_test() {
            q m = q.or(true, false, false);
            Assert.AreEqual(true, m.apply(10), "or of constant is ok");

            m = q.or(false, false);
            Assert.AreEqual(m.apply(10), false, "or of falses is false");

            m = q.or(true, null);
            Assert.AreEqual(true, m.apply(10), "or with null constant is null");

            m = q.or(false, null);
            Assert.AreEqual(null, m.apply(10), "or with null constant is null");


            m = q.or(true, DBNull.Value);
            Assert.AreEqual(true, m.apply(10), "or with DBNull constant is ok");

            m = q.or(false, DBNull.Value);
            Assert.AreEqual(DBNull.Value, m.apply(10), "or with DBNull constant is ok");

            m = q.or(true, null);
            Assert.AreEqual(true, m.apply(10), "or with null constant is ok");

            m = q.or(false, null);
            Assert.AreEqual(null, m.apply(10), "or with null constant is ok");


            m = q.or(false, DBNull.Value, false, null);
            Assert.AreEqual(null, m.apply(10), "or with DBNull and null constant is ok");

            m = q.or(DBNull.Value, null, true);
            Assert.AreEqual(true, m.apply(10), "or with DBNull and null constant is ok");

        }

        [Test]
        public void and_test() {
            q m = q.and(true, false, false);
            Assert.AreEqual(false, m.apply(10), "and of constant is ok");

            m = q.and(true, true);
            Assert.AreEqual(m.apply(10), true, "and of true is true");

            m = q.and(true, null);
            Assert.AreEqual(null, m.apply(10), "and with null constant is null");

            m = q.and(false, null);
            Assert.AreEqual(false, m.apply(10), "and with null constant is null");


            m = q.and(false, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "and with DBNull constant is ok");

            m = q.and(true, DBNull.Value);
            Assert.AreEqual(DBNull.Value, m.apply(10), "and with DBNull constant is ok");

            m = q.and(true, null);
            Assert.AreEqual(null, m.apply(10), "and with null constant is ok");

            m = q.and(false, null);
            Assert.AreEqual(false, m.apply(10), "and with null constant is ok");


            m = q.and(false, DBNull.Value, false, null);
            Assert.AreEqual(false, m.apply(10), "and with DBNull and null constant is ok");

            m = q.and(DBNull.Value, null, true);
            Assert.AreEqual(null, m.apply(10), "and with DBNull and null constant is ok");

        }

        [Test]
        public void not_test() {
            q m = q.not(true);
            Assert.AreEqual(false, m.apply(10), "not true is false");

            m = q.not(false);
            Assert.AreEqual(true, m.apply(10), "not false is true");

            m = q.not(null);
            Assert.AreEqual(null, m.apply(10), "not null is null");

            m = q.not(DBNull.Value);
            Assert.AreEqual(DBNull.Value, m.apply(10), "not DBNull is DBNull");

        }

        [Test]
        public void eq_test() {
            q m = q.eq(1, 2);
            Assert.AreEqual(false, m.apply(10), "1 is no 2");


            m = q.eq(2, 2);
            Assert.AreEqual(true, m.apply(10), "2 is 2");

             m = q.eq(2, "2");
            Assert.AreEqual(true, m.apply(10), "2 is \"2\"");

            m = q.eq(2, 2.0);//this is true cause types are upgraded
            Assert.AreEqual(true, m.apply(10), "2 is  2.0");

            m = q.eq(1.0 + 1, 2.0);
            Assert.AreEqual(true, m.apply(10), "1+1.0 is  2.0");

            m = q.eq(q.constant("2"), "2");
            Assert.AreEqual(true, m.apply(10), "'2' is  '2'");

            UInt32 i16 = 1;
            Int32 i32 = 1;
            var m1 = q.constant(i16);
            var m2 = q.constant(i32);
            m = q.eq(m1, m2); //true cause types are upgraded
            Assert.AreEqual(true, m.apply(10), "(int32) 2 = (int16)2");

            m = q.eq(2, null);
            Assert.AreEqual(null, m.apply(10), "2 = null is null");

            m = q.eq(null, null);
            Assert.AreEqual(null, m.apply(10), "null = null is null");

            m = q.eq(2, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "2 = DBNull is false");

            m = q.eq(DBNull.Value, DBNull.Value);
            Assert.AreEqual(true, m.apply(10), "DBNull = DBNull is true");

            m = q.eq(q.constant("a"), "A");
            Assert.AreEqual(true, m.apply(10), "a is \"A\"");


        }

        [Test]
        public void ne_test() {
            q m = q.ne(1, 2);
            Assert.AreEqual(true, m.apply(10), "1 is no 2");

            m = q.ne(2, 2);
            Assert.AreEqual(false, m.apply(10), "2 is 2");

            m = q.ne(2, 2.0);//false cause types are upgraded
            Assert.AreEqual(false, m.apply(10), "2 is not 2.0");

            m = q.ne(1.0 + 1, 2.0);
            Assert.AreEqual(false, m.apply(10), "1+1.0 is  2.0");

            m = q.ne(q.constant("2"), "2");
            Assert.AreEqual(false, m.apply(10), "'2' is  '2'");


            m = q.ne(2, null);
            Assert.AreEqual(null, m.apply(10), "2 <> null is null");

            m = q.ne(null, null);
            Assert.AreEqual(null, m.apply(10), "null <> null is null");

            m = q.ne(2, DBNull.Value);
            Assert.AreEqual(true, m.apply(10), "2 <> DBNull is true");

            m = q.ne(DBNull.Value, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "DBNull <> DBNull is true");

       
        }

        [Test]
        public void le_test() {
            q m = q.le(1, 2);
            Assert.AreEqual(true, m.apply(10), "1 <= 2");

            m = q.le(3, 2);
            Assert.AreEqual(false, m.apply(10), "3 is not <= 2");

            m = q.le(2, 2.0);
            Assert.AreEqual(true, m.apply(10), "2 is <= 2.0");

            m = q.le(1.0 + 1, 2.0);
            Assert.AreEqual(true, m.apply(10), "1+1.0 is <= 2.0");

            m = q.le(q.constant("2"), "2");
            Assert.AreEqual(true, m.apply(10), "'2' is <= '2'");


            m = q.le(2, null);
            Assert.AreEqual(null, m.apply(10), "2 <= null is null");

            m = q.le(null, null);
            Assert.AreEqual(null, m.apply(10), "null <= null is null");

            m = q.le(2, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "2 <= DBNull is false");

            m = q.le(DBNull.Value, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "DBNull <= DBNull is false");

        }

        [Test]
        public void lt_test() {
            q m = q.lt(1, 2);
            Assert.AreEqual(true, m.apply(10), "1 < 2");

            m = q.lt(3, 2);
            Assert.AreEqual(false, m.apply(10), "3 is not < 2");

            m = q.lt(1, 2.0);
            Assert.AreEqual(true, m.apply(10), "1 is  < 2.0");

            m = q.lt(2, 2.0);
            Assert.AreEqual(false, m.apply(10), "2 is not < 2.0");

            m = q.lt(1.0 + 1, 2.0);
            Assert.AreEqual(false, m.apply(10), "1+1.0 is not < 2.0");

            m = q.lt(q.constant("3"), "2");
            Assert.AreEqual(false, m.apply(10), "'3' is not < '2'");

            m = q.lt(q.constant("1"), "2");
            Assert.AreEqual(true, m.apply(10), "'1' is  < '2'");


            m = q.lt(2, null);
            Assert.AreEqual(null, m.apply(10), "2 < null is null");

            m = q.lt(null, null);
            Assert.AreEqual(null, m.apply(10), "null < null is null");

            m = q.lt(2, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "2 < DBNull is false");

            m = q.lt(DBNull.Value, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "DBNull < DBNull is false");

        }

        [Test]
        public void ge_test() {
            q m = q.ge(1, 2);
            Assert.AreEqual(false, m.apply(10), "1 is not >= 2");

            m = q.ge(3.1213, 2.2121);
            Assert.AreEqual(true, m.apply(10), "3 is  >= 2");

            m = q.ge(2, 2.0);
            Assert.AreEqual(true, m.apply(10), "2 is >= 2.0");

            m = q.ge(1.032 + 1, 2.032);
            Assert.AreEqual(true, m.apply(10), "1+1.0 is >= 2.0");

            m = q.ge(q.constant("2"), "2");
            Assert.AreEqual(true, m.apply(10), "'2' is >= '2'");


            m = q.ge(2, null);
            Assert.AreEqual(null, m.apply(10), "2 >= null is null");

            m = q.ge(null, null);
            Assert.AreEqual(null, m.apply(10), "null >= null is null");

            m = q.ge(2, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "2 >= DBNull is false");

            m = q.ge(DBNull.Value, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "DBNull >= DBNull is false");

        }

        [Test]
        public void gt_test() {
            q m = q.gt(1, 2);
            Assert.AreEqual(false, m.apply(10), "1 is not > 2");

            m = q.gt(3, 2);
            Assert.AreEqual(true, m.apply(10), "3 is  > 2");

            m = q.gt(2, 2.0);
            Assert.AreEqual(false, m.apply(10), "2 is not > 2.0");

            m = q.gt(1.0 + 1, 2.0);
            Assert.AreEqual(false, m.apply(10), "1+1.0 is not > 2.0");

            m = q.gt(q.constant("3"), "2");
            Assert.AreEqual(true, m.apply(10), "'3' is > '2'");


            m = q.gt(2, null);
            Assert.AreEqual(null, m.apply(10), "2 > null is null");

            m = q.gt(null, null);
            Assert.AreEqual(null, m.apply(10), "null > null is null");

            m = q.gt(2, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "2 > DBNull is false");

            m = q.gt(DBNull.Value, DBNull.Value);
            Assert.AreEqual(false, m.apply(10), "DBNull > DBNull is false");

        }

        [Test]
        public void cmpWithVars_test() {
            dynamic x = new ExpandoObject();
            x.a = 1;

            q m1 = q.eq("a", 1);
            q m2 = q.eq("a", 2);

            Assert.AreEqual(true, m1.apply(x), "x.a = 1");
            Assert.AreEqual(false, m2.apply(x), "x.a <> 1");

            x.a = 2;
            Assert.AreEqual(false, m1.apply(x), "x.a <> 1");
            Assert.AreEqual(true, m2.apply(x), "x.a = 1");

            normalClass y = new normalClass {
                c = 1
            };

            m1 = q.eq("c", 1);
            m2 = q.eq("c", 2);

            Assert.AreEqual(true, m1.apply(y), "y.c = 1");
            Assert.AreEqual(false, m2.apply(y), "y.c <> 1");

            y.c = 2;
            Assert.AreEqual(false, m1.apply(y), "y.c <> 1");
            Assert.AreEqual(true, m2.apply(y), "y.c = 1");


            DataTable t = new DataTable("x");
            t.Columns.Add("c", typeof(Int32));
            DataRow r = t.NewRow();
            r["c"] = 1;

            Assert.AreEqual(true, m1.apply(r), "r.c = 1");
            Assert.AreEqual(false, m2.apply(r), "r.c <> 1");

            r["c"] = 2;
            Assert.AreEqual(false, m1.apply(r), "r.c <> 1");
            Assert.AreEqual(true, m2.apply(r), "r.c = 1");

            var xx = new { c = 1 };
            Assert.AreEqual(true, m1.apply(xx), "xx.c = 1");
            Assert.AreEqual(false, m2.apply(xx), "xx.c <> 1");

        }

        [Test]
        public void cmpAs_test() {
            dynamic x = new ExpandoObject();
            x.a = 1;

            dynamic x2 = new ExpandoObject();
            x2.a = 2;


            dynamic y = new ExpandoObject();
            y.b = 1;

            q m1 = q.cmpAs(x, "a", "b");
            q m2 = q.cmpAs(x2, "a", "b");

            Assert.IsFalse(m2.isConstant(), "m2 is not a constant");
            Assert.AreEqual(false, m1.apply(x), "x.b <> 1");     //false because null <> 1 is false
            Assert.AreEqual(true, m1.apply(y), "y.b = 1");
            Assert.AreEqual(false, m2.apply(y), "y.b <> 2");


            x.b = 1;
            Assert.AreEqual(true, m1.apply(x), "x.b = 1");

            x.a = 2;
            Assert.AreEqual(true, m1.apply(x), "x.b = 1");//should still compare with 1
            Assert.AreEqual(true, m1.apply(y), "y.b = 1");
            Assert.AreEqual(false, m2.apply(x), "x.b <> 2");//should still compare with 2
            Assert.AreEqual(false, m2.apply(y), "y.b <> 2");//should still compare with 2


            y.b = 2;
            Assert.AreEqual(true, m1.apply(x), "x.b = 1");     //should still compare with 1
            Assert.AreEqual(false, m1.apply(y), "y.b <> 1");     //should still compare with 1
            Assert.AreEqual(false, m2.apply(x), "x.b <> 2");     //should still compare with 2
            Assert.AreEqual(true, m2.apply(y), "y.b = 2");     //should still compare with 2

            x.b = 2;
            Assert.AreEqual(false, m1.apply(x), "x.b <> 1"); //should still compare with 1
            Assert.AreEqual(false, m1.apply(y), "y.b <> 1");  //should still compare with 1
            Assert.AreEqual(true, m2.apply(x), "x.b = 2"); //should still compare with 2
            Assert.AreEqual(true, m2.apply(y), "y.b = 2");  //should still compare with 2


        }

        [Test]
        public void isNull_test() {

            // Viene testato il metodo q.isNull per elementi del dataTable
            // -------------------------------------------------------------------------------
            // Arrange 
            var m1 = q.isNull("a");
            var m2 = q.isNull("c");
            
            Assert.IsFalse(m1.isFalse());
            Assert.IsFalse(m1.isTrue());

            // Act
            DataTable t = new DataTable("x");
            t.Columns.Add("c", typeof(Int32));
            
            DataRow r = t.NewRow();
            r["c"] = 1;

            // Assert
            Assert.AreEqual(null, m1.apply(r), "r[\"a\"].a is null");
            Assert.AreEqual(false, m2.apply(r), "r[\"c\"].c is not DBNull");

            r["c"] = DBNull.Value;
            Assert.AreEqual(true, m2.apply(r), "r[\"c\"].c now is  DBNull");
            // -------------------------------------------------------------------------------

            // Viene testato il metodo q.isNull per gli oggetti dinamici
            // -------------------------------------------------------------------------------
            dynamic expObjVar = new ExpandoObject();
            expObjVar.a = 1;

            // Assert 
            Assert.AreEqual(true, m2.apply(expObjVar), "expObjVar.c is DBNull");   //c is assumed DBNUll
            Assert.AreEqual(false, m1.apply(expObjVar), "expObjVar.a is not DBNull");
            // -------------------------------------------------------------------------------

            // Viene testato il metodo q.isNull per le costanti
            // -------------------------------------------------------------------------------
            var a = q.constant("2");
            var z = q.isNull(a);

            var c = q.nullExpression();
            var y = q.isNull(c);

            Assert.AreEqual(false, z.apply(a), "a is not DBNull");
            Assert.AreEqual(true, y.apply(c), "c is DBNull");
            Assert.AreEqual(null, q.isNull(null).apply(null), "null is not DBNull");
            // -------------------------------------------------------------------------------

            // Viene testato il metodo q.isNull per le  "normal class"
            // NB normalClass ha solo la definizione della proprieta c definita come int
            // -------------------------------------------------------------------------------
            normalClass nrmClass = new normalClass {
                c = 1
            };

            Assert.AreEqual(true, m1.apply(nrmClass), "nrmClass.a is DBNull if not declared");
            Assert.AreEqual(false, m2.apply(nrmClass), "nrmClass.c is not DBNull");

            // -------------------------------------------------------------------------------


            // Viene testato il metodo q.isNull per le classi anonime
            var anonClass = new { c = "1" };
            Assert.AreEqual(true, m1.apply(anonClass), "anonClass.a is DBNull");            
            Assert.AreEqual(false, m2.apply(nrmClass), "anonClass.c is not DBNull");
        }

        [Test]
        public void isConditionalNull_test() {

            // Viene testato il metodo q.isNull per elementi del dataTable
            // -------------------------------------------------------------------------------
            // Arrange 
            var r1 = new test.normalClassNullable() { c = 1 };
            var r2 = new test.normalClassNullable();
            var m1 = q.isNull("a");
            var m2 = q.isNull("c");

            Assert.IsFalse(m1.isFalse());
            Assert.IsFalse(m1.isTrue());

            // Assert
            Assert.AreEqual(true, m1.apply(r1), "r1[\"a\"].a is DBNull"); //true because a is assumed DBNull
            Assert.AreEqual(false, m2.apply(r1), "r1[\"c\"].c is not DBNull");

            Assert.AreEqual(true, m2.apply(r2), "r2[\"a\"].a is DBNull");

        }

        [Test]
        public void isNotNull_test() {

            var m1 = q.isNotNull("a");
            var m2 = q.isNotNull("c");

            Assert.IsFalse(m1.isFalse());
            Assert.IsFalse(m1.isTrue());

            //viene testato il metodo isNotNull per un DataTable 

            DataTable t = new DataTable("x");
            t.Columns.Add("a", typeof(Int32));
            DataRow r = t.NewRow();
            r["a"] = 1;

            Assert.AreEqual(true, m1.apply(r), "x.a is not null");
            Assert.AreEqual(null, m2.apply(r), "x.c is null");

            //viene testato il metodo isNotNull per un dynamic object
            dynamic x1 = new ExpandoObject();
            x1.a = 1;

            Assert.AreEqual(true, m1.apply(x1), "x1.a is not null");
            Assert.AreEqual(false, m2.apply(x1), "x1.c is null");

            //viene testato il metodo isNotNull per una costante

            var a3 = q.constant("2");
            var z = q.isNotNull(a3);
            Assert.AreEqual(true, z.apply(a3), "const a3 is not null");

            var a4 = q.constant(null);
            var z1 = q.isNotNull(a4);
            Assert.AreEqual(null, z1.apply(null), "const a4 is NULL");

            var a5 = q.constant(DBNull.Value);
            var z2 = q.isNotNull(a5);
            Assert.AreEqual(false, z2.apply(null), "const a5 IS DBNull");


            //viene testato il metodo isNotNull per un oggetto della classe normalclass
            var n = new normalClass {
                c = 1
            };

            Assert.AreEqual(true, m2.apply(n), "n.c is not null");
            Assert.AreEqual(false, m1.apply(n), "n.a is null");

            //test tipi anonimi
            var o = new { c = "2" };

            Assert.AreEqual(true, m2.apply(o), "o.c is not null");
            Assert.AreEqual(false, m1.apply(o), "o.a is null");
        }

        [Test]
        public void shortCircuit_test() {
            dynamic x1 = new ExpandoObject();
            x1.a = 1;
            x1.b = 2;
            MetaExpression op1 = q.eq("a", 3);
            MetaExpression op2 = q.eq(x1.b, 2);
            MetaExpression op3 = op1 | op2;
            Assert.AreEqual(true, op3.isTrue(), "true constant are detected");

            MetaExpression op4 = q.eq("a", 1);
            MetaExpression op5 = q.eq(x1.b, 1);
            MetaExpression op6 = op4 & op5;
            Assert.AreEqual(true, op6.isFalse(), "false constant are detected");
        }

        [Test]
        public void compileConstant() {

            var c1 = q.constant(123456.123456M);
            Compiler c = new Compiler();
            Assert.AreEqual("123456.123456M",c1.getCCode(c,"o",null) , "decimal constant well compiled");

            var c2 = q.constant(123456.123456D);
            Assert.AreEqual("123456.123456D", c2.getCCode(c, "o", null),  "Double constant well compiled");

            var c3 = q.constant(12345);
            Assert.AreEqual("12345", c3.getCCode(c, "o", null),  "int constant well compiled");

            var c4 = q.constant("abcde");
            Assert.AreEqual("\"abcde\"", c4.getCCode(c, "o", null),  "string constant well compiled");

            var c5 = q.constant("abc\\de");
            Assert.AreEqual("\"abc\\\\de\"", c5.getCCode(c, "o", null),  "string constant well compiled");

            var c6 = q.constant("abc\\d\"e");
            Assert.AreEqual("\"abc\\\\d\\\"e\"", c6.getCCode(c, "o", null), "string constant well compiled");



        }

        [Test]
        public void collectionTest() {
            //Create list of normalClass object
            List<normalClass> coll = new List<normalClass>();
            for (int i = 0; i < 10; i++) {
                normalClass obj = new normalClass {
                    c = i
                };
                coll.Add(obj);
            }

            //Create empty list
            List<normalClass> collEmpty = new List<normalClass>();

            //Create null list
            List<normalClass> collNull = null;

            //Create empty array
            normalClass[] coll2Empty = new normalClass[] { };

            //Create null array
            normalClass[] coll2Null = null;

            //Create array of normalClass object
            normalClass[] coll2 = new normalClass[10];
            for (int i = 0; i < coll2.Length; i++) {
                var obj = new normalClass {
                    c = i
                };
                coll2[i] = obj;
            }

            ////Create list with dynamic object
            //List<dynamic> coll3 = new List<dynamic>();
            //for (int i = 0; i < 10; i++) {
            //    dynamic obj = new ExpandoObject();
            //    obj.a = i;
            //    obj.b = (char)(65 - i);
            //    coll3.Add(obj);
            //}

            #region _Every

            bool result = coll._Every(x => x.c > 5);
            Assert.AreEqual(false, result, "not all elements are greatest then 5. Test _Every with list");

            result = coll._Every(x => x.c < 20);
            Assert.AreEqual(true, result, "all elements are lesser then 20. Test _Every with list");

            result = coll2._Every(x => x.c > 5);
            Assert.AreEqual(false, result, "not all elements are greatest then 5. Test _Every with array");

            result = coll2._Every(x => x.c < 20);
            Assert.AreEqual(true, result, "all elements are lesser then 20. Test _Every with array");

            //Usando una logica negata, e' corretto che una collezione vuota ritorni true in quanto
            //tutte le condizioni sono state soddisfatte.
            result = collEmpty._Every(x => x.c > 5);
            Assert.AreEqual(true, result, ". Test _Every with empty list");

            result = collNull._Every(x => x.c > 5);
            Assert.AreEqual(false, result, ". Test _Every with null list");

            result = coll2Empty._Every(x => x.c > 5);
            Assert.AreEqual(true, result, ". Test _Every with empty array");

            result = coll2Null._Every(x => x.c > 5);
            Assert.AreEqual(false, result, ". Test _Every with null array");


            #endregion

            #region _Filter

            q lt1 = q.lt("c", 1);
            var actColl = coll._Filter(lt1);
            var resColl = coll.Where(x => x.c < 1);
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with list");

            actColl = coll2._Filter(lt1);
            resColl = coll2.Where(x => x.c < 1);
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with array");

            actColl = collEmpty._Filter(lt1);
            resColl = collEmpty.Where(x => x.c < 1);
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with empty list");

            actColl = coll2Empty._Filter(lt1);
            resColl = coll2Empty.Where(x => x.c < 1);
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with empty array");

            //Nel caso in cui l'array o la lista sia null viene usata un yield break
            //quindi avremo come risultato un IEnumerable vuoto.
            actColl = collNull._Filter(lt1);
            resColl = Enumerable.Empty<normalClass>();
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with null list");

            actColl = coll2Null._Filter(lt1);
            resColl = Enumerable.Empty<normalClass>();
            Assert.AreEqual(resColl, actColl, "test _Filter between 2 collection. Test with null array");

            #endregion

            #region _Find

            q gt10 = q.gt("c", 10); //funzione che dato un oggetto r restituisce true se r["c"]>10
            q gt5 = q.gt("c", 5);

         
            normalClass singleAct = coll._Find(gt10);
            normalClass singleRes = coll.Find(x => x.c > 10);
            Assert.AreEqual(singleRes, singleAct, "test _Find between 2 collection. Test with list");

            singleAct = coll2._Find(gt5);
            //Uso First con espressione perche' non e' possibile utilizzare il metodo Find su un array
            singleRes = coll2.First(x => x.c > 5);
            Assert.AreEqual(singleRes, singleAct, "test _Find between 2 collection. Test with array");

            singleAct = collEmpty._Find(gt5);
            singleRes = collEmpty.Find(x => x.c > 5);
            Assert.AreEqual(singleRes, singleAct, "test _Find between 2 collection. Test with empty list");

            singleAct = coll2Empty._Find(gt5);
            singleRes = coll2Empty.FirstOrDefault(x => x.c > 5);
            Assert.AreEqual(singleRes, singleAct, "test _Find between 2 collection. Test with empty array");

            singleAct = collNull._Find(gt5);
            //singleRes = collNull.Find(x => x.c > 5);
            //Riga commentata perche' Find su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, singleAct, "test _Find between 2 collection. Test with null list");

            singleAct = coll2Null._Find(gt5);
            //singleRes = coll2Null.FirstOrDefault(x => x.c > 5);
            //Riga commentata perche' Find su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, singleAct, "test _Find between 2 collection. Test with null array");

            #endregion

            #region _First

            //A volte ho la necessita' di usare FirstOrDefault perche' il metodo First non restituisce il valore null,
            //mentre il metodo _Find gestisce questo caso.

            singleAct = coll._First();
            singleRes = coll.First();
            Assert.AreEqual(singleRes, singleAct, "test _First between 2 collection. Test with list");

            singleAct = coll2._First();
            singleRes = coll2.First();
            Assert.AreEqual(singleRes, singleAct, "test _First between 2 collection. Test with array");

            singleAct = collEmpty._First();
            singleRes = collEmpty.FirstOrDefault();
            Assert.AreEqual(singleRes, singleAct, "test _First between 2 collection. Test with empty list");

            singleAct = coll2Empty._First();
            singleRes = coll2Empty.FirstOrDefault();
            Assert.AreEqual(singleRes, singleAct, "test _First between 2 collection. Test with empty array");

            singleAct = collNull._First();
            //singleRes = collNull.FirstOrDefault();
            //Riga commentata perche' First/FirstOrDefault su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, singleAct, "test _First between 2 collection. Test with null list");

            singleAct = coll2Null._First();
            //singleRes = coll2Null.FirstOrDefault();
            //Riga commentata perche' First/FirstOrDefault su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, singleAct, "test _First collection. Test with null array");

            #endregion

            #region _forEach

            var tempColl = coll;
            coll._forEach(x => x.c += 1);
            tempColl.ForEach(x => x.c += 1);
            Assert.AreEqual(tempColl, coll, "test _forEach between 2 collection. Test with list");

            var tempColl2 = coll2;
            coll2._forEach(x => x.c += 1);
            //Uso il foreach in questo modo perchè .ForEach non è implementato da linq per un array
            foreach (var element in tempColl2) {
                element.c += 1;
            }

            Assert.AreEqual(tempColl2, coll2, "test _forEach between 2 collection. Test with array");

            tempColl = collEmpty;
            collEmpty._forEach(x => x.c += 1);
            tempColl.ForEach(x => x.c += 1);
            Assert.AreEqual(tempColl, collEmpty, "test _forEach between 2 collection. Test with empty list");

            tempColl2 = coll2Empty;
            coll2Empty._forEach(x => x.c += 1);
            //Uso il foreach in questo modo perchè .ForEach non è implementato da linq per un array
            foreach (var element in tempColl2) {
                element.c += 1;
            }

            Assert.AreEqual(tempColl2, coll2Empty, "test _forEach between 2 collection. Test with empty list");

            collNull._forEach(x => x.c += 1);
            //tempColl.ForEach(x => x.c += 1);
            //Riga commentata perche' .ForEach su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, collNull, "test _forEach between 2 collection. Test with null list");

            coll2Null._forEach(x => x.c += 1);
            //tempColl2.ForEach(x => x.c += 1);
            //Riga commentata perche' .ForEach su un oggetto null genera un'eccezione.
            Assert.AreEqual(null, coll2Null, "test _forEach between 2 collection. Test with null array");

            #endregion

            #region _HasRows

            result = coll._HasRows();
            Assert.AreEqual(true, result, "collection not empty. Test _HasRows with list");

            result = coll2._HasRows();
            Assert.AreEqual(true, result, "collection not empty. Test _HasRows with array");

            result = collEmpty._HasRows();
            Assert.AreEqual(false, result, "collection empty. Test _HasRows with list");

            result = coll2Empty._HasRows();
            Assert.AreEqual(false, result, "collection empty. Test _HasRows with array");

            //Vedere come gestire il caso di hasRows quando è null...
            Assert.DoesNotThrow(() => collNull._HasRows(), "collection null. Test _HasRows with list");

            Assert.DoesNotThrow(() => coll2Null._HasRows(),"collection null. Test _HasRows with array");

            #endregion

            #region _IfExists

            coll._IfExists(q.gt("c", 1), (normalClass x) => x.c += 1, () => coll[0].c -= 1);
            Assert.AreEqual(3, coll[0].c,
                "first element that satisfies the condition has the field c increased by 1, else coll[0].c -=1. Test _IfExists with List");

            coll._IfExists(q.gt("c", 20), (normalClass x) => x.c += 1, () => coll[0].c -= 1);
            Assert.AreEqual(2, coll[0].c,
                "first element that satisfies the condition has the field c increased by 1, else coll[0].c -=1. Test _IfExists with List");

            coll2._IfExists(q.gt("c", 1), (normalClass x) => x.c += 1, () => coll2[0].c -= 1);
            Assert.AreEqual(3, coll2[0].c,
                "first element that satisfies the condition has the field c increased by 1, else coll[0].c -=1. Test _IfExists with array");

            coll2._IfExists(q.gt("c", 20), (normalClass x) => x.c += 1, () => coll2[0].c -= 1);
            Assert.AreEqual(2, coll2[0].c,
                "first element that satisfies the condition has the field c increased by 1, else coll[0].c -=1. Test _IfExists with array");

            collEmpty._IfExists(q.gt("c", 20), (normalClass x) => x.c += 1, () => coll[0].c -= 1);
            Assert.AreEqual(1, coll[0].c, "empty list can never satisfy the condition. Test _IfExists with List");

            coll2Empty._IfExists(q.gt("c", 1), (normalClass x) => x.c += 1, () => coll2[0].c -= 1);
            Assert.AreEqual(1, coll2[0].c, "empty array can never satisfy the condition. Test _IfExists with array");

            collNull._IfExists(q.gt("c", 1), (normalClass x) => x.c += 1, () => coll[0].c -= 1);
            Assert.AreEqual(0, coll[0].c, "null list can never satisfy the condition. Test _IfExists with list");

            coll2Null._IfExists(q.gt("c", 1), (normalClass x) => x.c += 1, () => coll2[0].c -= 1);
            Assert.AreEqual(0, coll2[0].c, "null array can never satisfy the condition. Test _IfExists with array");

            #endregion

            #region _IfNotExists

            coll._IfNotExists(q.gt("c", 1), () => coll[0].c += 1);
            Assert.AreEqual(0, coll[0].c, "if no element satisfies the condition. Test _IfNotExists with List");

            coll._IfNotExists(q.gt("c", 20), () => coll[0].c += 1);
            Assert.AreEqual(1, coll[0].c, "if no element satisfies the condition. Test _IfNotExists with List");

            coll2._IfNotExists(q.gt("c", 1), () => coll2[0].c += 1);
            Assert.AreEqual(0, coll2[0].c, "if no element satisfies the condition. Test _IfNotExists with array");

            coll2._IfNotExists(q.gt("c", 20), () => coll2[0].c += 1);
            Assert.AreEqual(1, coll2[0].c, "if no element satisfies the condition. Test _IfNotExists with array");

            collEmpty._IfNotExists(q.gt("c", 20), () => coll[0].c += 1);
            Assert.AreEqual(2, coll[0].c,
                "empty array always satisfy the condition. Test _IfNotExists with empty List");

            coll2Empty._IfNotExists(q.gt("c", 20), () => coll2[0].c += 1);
            Assert.AreEqual(2, coll2[0].c,
                "empty array always satisfy the condition. Test _IfNotExists with empty array");

            collNull._IfNotExists(q.gt("c", 20), () => coll[0].c += 1);
            Assert.AreEqual(3, coll[0].c, "null array always satisfy the condition. Test _IfNotExists with null List");

            coll2Null._IfNotExists(q.gt("c", 20), () => coll2[0].c += 1);
            Assert.AreEqual(3, coll2[0].c,
                "null array always satisfy the condition. Test _IfNotExists with null array");


            #endregion

        }

        [Test]
        public void mcmp_test() {

            var o = new {a = 1, aa = false, b = "2", c = 2.3, d = 20m, e =DBNull.Value};
            var fMcmp1 = q.mCmp(new {a = 1, c = 2.3, e=(object)DBNull.Value});
            var fMcmp2 = q.mCmp(new {a = 1, c = 2.4, e=(object)DBNull.Value});
            
            Assert.AreEqual(true, fMcmp1.apply(o));
            Assert.AreEqual(false, fMcmp2.apply(o));

           
        }

        [Test]
        public void compileExpressions() {
	        string check = null;
	        var expr = check.toMetaExpression();
            Assert.AreEqual(null,expr, "null compiles to null");

            check = "";
             expr = check.toMetaExpression();
            Assert.AreEqual(null,expr, "empty string compiles to null");

            check = "(1=1)";
            expr = check.toMetaExpression();
            Assert.AreEqual(null,expr, "Truthy compiles to null");

            check = "3*4*3+2*3+5";
            object res = check.toMetaExpression().apply(null);
            Assert.AreEqual(47,res, "Operator precedence - mul and add");

            check = "24/2/3";
            res = check.toMetaExpression().apply(null);
            Assert.AreEqual(4,res, "Operator precedence - mul and add");        

            check = "a is not null";
            res = check.toMetaExpression().toString();
            Assert.AreEqual("a is not null",res, "Operator precedence - mul and add"); 
            
            check="a is not null and b=5 and c>2 and d='a'";
            res = check.toMetaExpression().toString();
            Assert.AreEqual("a is not null and b==5 and c>2 and d=='a'",res, "Operator precedence - mul and add"); 
        }
    }

    
}
