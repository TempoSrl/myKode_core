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
    public class CQueryHelperTest {
        public static DataAccess Conn;
        static QueryHelper qhs;
        static CQueryHelper qhc;
        static DataTable tExpense;
        [OneTimeSetUp]
        public static void testInitAll() {
            Conn = MockUtils.getAllLocalDataAccess("utente2");
            qhs = Conn.GetQueryHelper();
            qhc = new CQueryHelper();
            tExpense = Conn.Select("expenseview", filter:"curramount>1000", top:"1000").GetAwaiter().GetResult();
        }
        [OneTimeTearDown]
        public static void testEndAll() {
            Conn.Destroy();
        }

        [SetUp]
        public void testInit() {
        }
        [TearDown]
        public void testEnd() {
        }


        [Test]
        public void whereGreater100_12() {
            q gt1012 = q.gt("curramount", 10.12);
            var filter = gt1012.toSql(qhc);
            var local = tExpense.Select(filter);
            Assert.AreEqual(tExpense.Rows.Count, local.Length);
        }

         [Test]
        public void whereGreater100_12m() {
            q gt1012 = q.gt("curramount", 10.12m);
            var filter = gt1012.toSql(qhc);
            var local = tExpense.Select(filter);
            Assert.AreEqual(tExpense.Rows.Count, local.Length);
        }
        [Test]
        public void whereGreaterEqualDate() {
            var lt = new DateTime(2016,11,3);
            q geFilter = q.ge("lt", lt);
            var filter = geFilter.toSql(qhc);
            var local = tExpense.Select(filter);
            var filtered = tExpense.filter(geFilter);
            Assert.AreEqual(filtered.Length, local.Length);
        }
          [Test]
        public void whereGreaterEqualDateTime() {
            var lt = new DateTime(2016,11,3,10,1,3);
            q geFilter = q.ge("lt", lt);
            var filter = geFilter.toSql(qhc);
            var local = tExpense.Select(filter);
            var filtered = tExpense.filter(geFilter);
            Assert.AreEqual(filtered.Length, local.Length);
        }
    }

    [TestFixture]
    public class SqlServerQueryHelperTest {
        public static DataAccess Conn;
        static QueryHelper qhs;
        static CQueryHelper qhc;
        static DataTable tExpense;

        [OneTimeSetUp]
        public static void testInitAll() {
            Conn = MockUtils.getAllLocalDataAccess("utente2");
            qhs = Conn.GetQueryHelper();
            qhc = new CQueryHelper();
               tExpense = Conn.Select("expenseview",filter: "curramount>1000", top:"1000").GetAwaiter().GetResult();
        }
        [OneTimeTearDown]
        public static void testEndAll() {
            Conn.Destroy();
        }

        [Test]
        public void whereGreater100_12() {
            q gt1012 = q.gt("curramount", 10.12)& q.gt("curramount",1000);
            var filter = gt1012.toSql(qhs);
            var tExpense2 = Conn.Select("expenseview",columnlist: "idexp", filter:filter, top:"1000").GetAwaiter().GetResult();
            var local = tExpense2.Select();
            Assert.AreEqual(tExpense.Rows.Count, local.Length);
        }
        [Test]
         public void whereGreater100_12m() {
            q gt1012 = q.gt("curramount", 10.12m)& q.gt("curramount",1000);
            var filter = gt1012.toSql(qhs);
            var tExpense2 = Conn.Select("expenseview", columnlist:"idexp", filter:filter, top:"1000").GetAwaiter().GetResult();
            var local = tExpense2.Select();
            Assert.AreEqual(tExpense.Rows.Count, local.Length);
        }
        [Test]
        public void whereGreaterEqualDate() {
            var lt = new DateTime(2016,11,3);
            q geFilter = q.ge("lt", lt)& q.gt("curramount",5);
            var filter = geFilter.toSql(qhs);
            var tExpense2 = Conn.Select("expenseview", columnlist:"idexp", filter:filter, top:"1000").GetAwaiter().GetResult();
            var local = tExpense2.Select();
            Assert.AreEqual(tExpense2.Rows.Count, local.Length);
        }

          [Test]
        public void whereGreaterEqualDateTime() {
            var lt = new DateTime(2019,11,3,10,1,3);
            q geFilter = q.ge("lt", lt)& q.gt("curramount",5);
            var filter = geFilter.toSql(qhs);
            var tExpense2 =  Conn.Select("expenseview", columnlist:"idexp", filter:filter, top:"100").GetAwaiter().GetResult();
            var local = tExpense2.Select();
            Assert.AreEqual(tExpense2.Rows.Count, local.Length);
        }
    }
}
