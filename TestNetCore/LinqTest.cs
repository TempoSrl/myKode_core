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
//using msUnit = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestNetCore {
   
    [TestFixture]
    public class LinqTest {
        public static DataAccess Conn;
        static QueryHelper QHS;
        [OneTimeSetUp]
        public static void testInit() {
            Conn = MockUtils.getAllLocalDataAccess("utente2");
            QHS = Conn.GetQueryHelper();
        }
        [OneTimeTearDown]
        public static void testEnd() {
            Conn.Destroy();
        }

        [Test]
        public void whereGreater100() {
            DataTable t = Conn.Select("expenseview", top: "1000").GetAwaiter().GetResult();

            var greater100 = from r in t.Select()
                             where (Decimal)r["curramount"] > 100
                             select r;
            foreach (var R in greater100) {
                Assert.IsTrue(((Decimal)R["curramount"]) > 100);
            }          
        }
        [Test]
        public void whereGreater100WithExtensions() {
            DataTable t = Conn.Select("expenseview", top: "1000").GetAwaiter().GetResult();

            var greater100Values = t.Select()
                             .Where(r => (Decimal)r["curramount"] > 100)
                             .Select(r => r["curramount"]);
            foreach (Decimal v in greater100Values) {
                Assert.IsTrue(v > 100);
            }
            var greater100Rows = t.Select()
                             .Where(r => (Decimal)r["curramount"] > 100)
                             .Select(r => r);
            foreach (DataRow R in greater100Rows) {
                Assert.IsTrue(((Decimal)R["curramount"]) > 100);
            }

        }

        [Test]
        public void whereWithIndex() {
            DataTable t = Conn.Select("expenseview", top: "100").GetAwaiter().GetResult();

            var greaterOdd = t.Select()
                             .Where((r, index) => index % 2 != 0)
                             .Select(r => r);
            int count = greaterOdd.ToArray().Length;
            Assert.AreEqual(50, count, "Skipped half rows by index");

        }

        [Test]
        public void AsEnumerableWithDeletedRows() {
            DataTable t = Conn.Select("expenseview", top:"100").GetAwaiter().GetResult();
            for (int i = 0; i < 10; i++) t.Rows[i].Delete();
            int count = t.AsEnumerable().Count();
            Assert.AreEqual(100, count, "AsEnumerable does not skip deleted rows");
            count = t.Select().Count();
            Assert.AreEqual(90, count, "Select does skip deleted rows");
        }


        [Test]
        public void filterOfType() {
            object[] arr = { 1, 2, 3, "ciao", "hello", true, "citroen" };
            var queryString = arr.OfType<string>()
                .Where(s => s.StartsWith("c"));
            var result = queryString.ToArray();
            int count = result.Length;
            Assert.AreEqual(2, count, "got 2 string");
            Assert.AreEqual("ciao", result[0]);
            Assert.AreEqual("citroen", result[1]);

        }

        [Test]
        public void selectMany() {
            CQueryHelper QHC = new CQueryHelper();

            DataTable mandateKind = Conn.Select("mandatekind" ).GetAwaiter().GetResult();
            DataTable mandate = Conn.Select("mandate", top: "1000").GetAwaiter().GetResult();
            var mandateAndKind = from r in mandate.Select()
                                 from s in mandateKind.Select(QHC.CmpEq("idmankind", r["idmankind"]))
                                 orderby r["idmankind"] ascending, r["yman"] ascending, r["nman"] ascending
                                 select new { rMan = r, rManKind = s["description"] };

            var result = mandateAndKind.ToArray();
            Assert.IsTrue(result.Length > 0, "Some row was taken from join");
            Assert.AreEqual(1000, result.Length, "Same rows as first select");
            foreach (var r in result) {
                string title = mandateKind.Select(QHC.CmpEq("idmankind", r.rMan["idmankind"]))[0]["description"].ToString();
                Assert.AreEqual(title, r.rManKind, "description is correct");
            }

            var mandateAndKindJoin = from r in mandate.Select()
                                     from s in mandateKind.Select()
                                     where r["idmankind"].Equals(s["idmankind"])
                                     orderby r["idmankind"] ascending, r["yman"] ascending, r["nman"] ascending
                                     select new { rMan = r, rManKind = s["description"] };

            var resultJoin = mandateAndKindJoin.ToArray();
            Assert.IsTrue(resultJoin.Length > 0, "Some row was taken from join");
            Assert.AreEqual(1000, resultJoin.Length, "Same rows as first select");
            foreach (var r in resultJoin) {
                string title = mandateKind.Select(QHC.CmpEq("idmankind", r.rMan["idmankind"]))[0]["description"].ToString();
                Assert.AreEqual(title, r.rManKind, "description is correct");
            }
        }

        [Test]
        public void selectJoin() {
            CQueryHelper QHC = new CQueryHelper();

            DataTable mandateKind = Conn.Select("mandatekind").GetAwaiter().GetResult();
            DataTable mandate = Conn.Select("mandate", top: "1000").GetAwaiter().GetResult();

            var mandateJoin = from r in mandate.Select()
                              join s in mandateKind.Select() on r["idmankind"] equals s["idmankind"]
                              orderby r["idmankind"] ascending, r["yman"] ascending, r["nman"] ascending
                              select new { rMan = r, rManKind = s["description"] };

            var resultJoin = mandateJoin.ToArray();
            Assert.IsTrue(resultJoin.Length > 0, "Some row was taken from join");
            Assert.AreEqual(1000, resultJoin.Length, "Same rows as first select");
            foreach (var r in resultJoin) {
                string title = mandateKind.Select(QHC.CmpEq("idmankind", r.rMan["idmankind"]))[0]["description"].ToString();
                Assert.AreEqual(title, r.rManKind, "description is correct");
            }
        }

        //[Test]
        //public void ExpressionConstant() {
        //    var x = Expression.Constant(3);
        //    Assert.AreEqual("3", x.ToString(), "Integer constant not quoted");
        //    ConstantExpression y = Expression.Constant(3.1m);
        //    Assert.AreEqual("3.1", ,  "Decimal constant not quoted");
        //    var z = Expression.Constant("abc");
        //    Assert.AreEqual( "\"abc\"", z.ToString(), "string constant quoted");

        //}
    }
}
