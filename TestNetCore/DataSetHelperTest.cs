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
using mdl_utils;
//using msUnit = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestNetCore {
   [TestFixture]
    public class DataSetHelperTest {
        public static DataAccess Conn;
        static QueryHelper QHS;
        [OneTimeSetUp]
        public static void testInit() {
            Conn = MockUtils.getAllLocalDataAccess("utente2");               
            QHS = Conn.GetQueryHelper();
        }
        [OneTimeTearDown]
        public static void testEnd() {
            Conn.Destroy().Wait();
        }

        [Test]
        public void GetMaxTest() {
            DataTable t = Conn.Select("mandatedetail", filter: QHS.CmpGe("tax", 10), top:"100").GetAwaiter().GetResult();
            decimal max = 0;
            foreach(DataRow r in t.Rows) {
                if (Convert.ToDecimal(r["tax"]) > max) max = Convert.ToDecimal(r["tax"]);
            }
            decimal max_2 = (decimal) t.GetMax<decimal>("tax");
            Assert.AreEqual(max, max_2, "max is evaluated");
        }

        [Test]
        public void GetMinTest() {
            DataTable t = Conn.Select("mandatedetail", filter:QHS.CmpGe("tax",10), top:"100").GetAwaiter().GetResult();
            decimal min = decimal.MaxValue;
            foreach (DataRow r in t.Rows) {
                if (Convert.ToDecimal(r["tax"]) < min) min = Convert.ToDecimal(r["tax"]);
            }
            decimal min_2 = (decimal)t.GetMin<decimal>("tax");
            Assert.AreEqual(min, min_2, "min is evaluated");
        }

        [Test]
        public void GetMaxPlusOneTest() {
            DataTable t = Conn.Select("mandatedetail",  filter: QHS.CmpGe("tax", 10),top: "100").GetAwaiter().GetResult();
            decimal max = 0;
            foreach (DataRow r in t.Rows) {
                if (Convert.ToDecimal(r["tax"]) > max) max = Convert.ToDecimal(r["tax"]);
            }
            decimal max_2 = (decimal)t.GetMaxPlusOne<decimal>("tax");
            Assert.AreEqual(max+1, max_2, "max+1 is evaluated");
        }


        [Test]
        public void GetSumTest() {
            DataTable t = Conn.Select("mandatedetail", filter:QHS.CmpGe("tax", 10), top:"100").GetAwaiter().GetResult();
            decimal sum = 0;
            foreach (DataRow r in t.Select(QHS.IsNotNull("tax"))) sum += (Decimal)r["tax"];
            decimal sum2 = (decimal) t.GetSum<decimal>("tax");

            Assert.AreEqual(sum, sum2, "sum evaluated");
        }

        [Test]
        public void JoinTest2() {
            var t = Conn.Select(tablename: "mandatedetail", filter:QHS.CmpGe("tax", 10), top:"100").GetAwaiter().GetResult();
            var t2 = Conn.Select("mandatekind").GetAwaiter().GetResult();
            var r = t.AsEnumerable()
                .Join(t2.AsEnumerable(),(md, mk) => md["idmankind"].Equals(mk["idmankind"]) );
            Assert.AreEqual(r.Count(), t.Rows.Count, $"All {r.Count()} rows have been joined");
            Assert.AreEqual(100, r.Count(), "Join have the requested size");
            foreach(var j in r) {
                Assert.AreEqual(j.Item1["idmankind"], j.Item2["idmankind"], "Join field are equal");
            }
           
        }

        [Test]
        public void JoinTest3() {
            DataTable t = Conn.ReadTableNoKey("mandatedetail",  filter:q.ge("tax", 10)& q.isNotNull("idivakind"),TOP:"100").GetAwaiter().GetResult();
            DataTable t2 = Conn.Select("mandatekind").GetAwaiter().GetResult();
            DataTable t3 = Conn.Select("ivakind").GetAwaiter().GetResult();
            var r = t.AsEnumerable()
                .Join(t2.AsEnumerable(),(md, mk) => md["idmankind"].Equals(mk["idmankind"]))
                .LeftJoin(t3.AsEnumerable(), (md, mk, ik) => md["idivakind"].Equals(ik["idivakind"]));
            Assert.AreEqual(r.Count(), t.Rows.Count, "All rows have been joined");
            Assert.AreEqual(100, r.Count(), "Join have the requested size");
            foreach (var j in r) {
                Assert.AreEqual(j.Item1["idmankind"], j.Item2["idmankind"], "Join field 2 are equal");
                Assert.AreEqual(j.Item1["idivakind"], j.Item3["idivakind"], "Join field 3 are equal");
            }

        }


        [Test]
        public void JoinTest4() {
	        DataTable t = Conn.Select("mandatedetail",  filter:q.ge("tax", 10)& q.isNotNull("idivakind"),top: "100").GetAwaiter().GetResult();
            DataTable t2 = Conn.Select("mandatekind").GetAwaiter().GetResult();
            DataTable t3 = Conn.Select("ivakind").GetAwaiter().GetResult();
            DataTable t4 = Conn.Select("accmotive").GetAwaiter().GetResult();
            var r = t.AsEnumerable()
                .Join(t2.AsEnumerable(), (md, mk) =>  md["idmankind"].Equals(mk["idmankind"]))
                .LeftJoin(t3.AsEnumerable(), (md, mk, ik) => md["idivakind"].Equals(ik["idivakind"]))
                 .LeftJoin(t4.AsEnumerable(), (md, mk, ik, ac) => md["idaccmotive"].Equals(ac["idaccmotive"]));
                ;
            Assert.AreEqual(r.Count(), t.Rows.Count, "All rows have been joined");
            Assert.AreEqual(100, r.Count(), "Join have the requested size");
            foreach (var j in r) {
                Assert.AreEqual(j.Item1["idmankind"], j.Item2["idmankind"], "Join field 2 are equal");
                Assert.AreEqual(j.Item1["idivakind"], j.Item3["idivakind"], "Join field 3 are equal");
                if (j.Item4 != null) {
                    Assert.AreEqual(j.Item1["idaccmotive"], j.Item4["idaccmotive"], "Join field 4 are equal");
                }
            }

        }

        [Test]
        public void ReduceTest3() {
            DataTable t = Conn.Select("mandatedetail", filter: QHS.CmpGe("tax", 10), top:"100").GetAwaiter().GetResult();
            decimal tot = 0;
            foreach(DataRow r in t.Rows) {
                tot += (decimal)r["tax"];
            }
            decimal tot2  = t.AsEnumerable()._Reduce( (prev,r)=>((decimal)r["tax"])+prev,0m);
            Assert.AreEqual(tot, tot2, "Reduce does sum");

        }


        [Test]
        public void SelectTest() {
            DataTable t = Conn.Select("mandatedetail", filter:QHS.CmpGe("tax", 10),top: "100").GetAwaiter().GetResult();
            var rows = t.Select()._Select("idmankind", "yman", "nman", "taxable");
            Assert.IsNotNull(rows, "Select return rows");
            Assert.AreEqual(rows.Count(), 100, "select returns all rows");
            Assert.IsInstanceOf(typeof(RowObject), rows.First(),  "select returns RowObject");
            foreach(var r in rows) {
                Assert.IsInstanceOf(typeof(string),MetaExpression.getField("idmankind", r));
                Assert.IsInstanceOf(typeof(Int16),MetaExpression.getField("yman", r));
                Assert.IsInstanceOf(typeof(int),MetaExpression.getField("nman", r));
                Assert.IsInstanceOf(typeof(Decimal),MetaExpression.getField("taxable", r));
            }
        }

        bool conditionCompiled(DataRow r) {
            if (r["taxable"] == DBNull.Value) return false;
            return ((((decimal)r["taxable"]) >= 10));
        }
        [Test]  //[Ignore("This is about speed")]
        public void SelectFilterTest() {
            int nrows = 100000;
            
            DataTable t = Conn.ReadTableNoKey("assetview",  filter:q.isNotNull("taxable"),TOP:nrows.ToString()).GetAwaiter().GetResult();

            Stopwatch s4 = new Stopwatch();
            s4.Start();
            List<DataRow> rows4 = new List<DataRow>();
            foreach (DataRow r in t.Select()) {
                //DataRow r = t.Rows[i];
                //if (r.RowState == DataRowState.Deleted) continue;
                if ((Boolean)CompareHelper.cmpObjGe<Decimal>(r["taxable"], 10.0M)) rows4.Add(r);
            }
            s4.Stop();

            var filter = q.ge("taxable", 10.0M).optimize(t).Compile<DataRow>();
            Stopwatch s1 = new Stopwatch();
            s1.Start();
            var rows = t.Select()._Filter(filter);
            s1.Stop();

            Stopwatch s2 = new Stopwatch();
            s2.Start();
            var rows2 = t.Select("taxable>=10");
            s2.Stop();

            Stopwatch s3 = new Stopwatch();
            s3.Start();
            List<DataRow> rows3 = new List<DataRow>();
            foreach (DataRow r in t.Select()) {
                //DataRow r = t.Rows[i];
                //if (r.RowState == DataRowState.Deleted) continue;
                if (r["taxable"] == DBNull.Value) continue;
                if (((decimal)r["taxable"]) >= 10) rows3.Add(r);
            }
            s3.Stop();

           

            int expectedMimimumThrouhput = 100000;  //100000 rows for second, 100 row for millisecond
            int expectedMaximumTime = (t.Rows.Count * 1000) / expectedMimimumThrouhput;

            Assert.AreEqual(rows.Count(),rows2.Length, "Select return rows");
            Assert.IsTrue(s1.ElapsedMilliseconds<s2.ElapsedMilliseconds, "Filter is quicker than Select");
            Assert.IsTrue(s1.ElapsedMilliseconds < expectedMaximumTime, "Filter is quick");


            Assert.IsInstanceOf(typeof(DataRow), rows.First(),  "select returns DataRows");


        }

        [Test]  // [Ignore("This is about speed")]
        public void selectFilterCompiledTest() {
            DataTable t = Conn.Select("assetview", top:"10000").GetAwaiter().GetResult();

            Stopwatch s00 = new Stopwatch();
            s00.Start();
            var rows = (from r in t.Select() where r["taxable"].Equals(10.0M) select r).ToArray();
            //var rows = t.Select("taxable = 10000000");
            s00.Stop();


            Stopwatch s0_Select = new Stopwatch();
            s0_Select.Start();
            //var rows = (from r in t.Select() where r["taxable"].Equals(10.0M) select r).ToArray();
            rows = t.Select("taxable = 10000000");
            s0_Select.Stop();

            var filter1 = q.eq("taxable", 10.0M);
            Stopwatch s1_Filter = new Stopwatch();
            s1_Filter.Start();
            rows =  t.Select()._Filter(filter1).ToArray();;
            s1_Filter.Stop();

            var filter = q.eq("taxable", 10.0M).optimize(t).Compile<DataRow>();
            
            var  rr=t.Select();
            Stopwatch s2_Compiled = new Stopwatch();
            s2_Compiled.Start();
            var rows2 = rr._Filter(filter).ToArray();
            s2_Compiled.Stop();

            var ff = (q.field("a") <= q.constant(1));            
			Stopwatch s3_code = new Stopwatch();
			s3_code.Start();
			List<DataRow> rows3 = new List<DataRow>();
			foreach (DataRow r in t.Rows) {
				//DataRow r = t.Rows[i];
				//if (r.RowState == DataRowState.Deleted) continue;
				//if (r["taxable"] == DBNull.Value) continue;
				//if (((decimal)r["taxable"]) == 10) rows3.Add(r);
				if (r["taxable"].Equals(10.0M)) rows3.Add(r);
			}
			s3_code.Stop();


			//Stopwatch s4 = new Stopwatch();
			//s4.Start();
			//List<DataRow> rows4 = new List<DataRow>();
			//foreach (DataRow r in t.Select()) {
			//	if (filter.getBooleanResult(r)) rows4.Add(r);
			//}
			//s4.Stop();

			int expectedMimimumThrouhput = 100000;  //100000 rows for second, 100 row for millisecond
            int expectedMaximumTime = (t.Rows.Count * 1000) / expectedMimimumThrouhput;

            Assert.AreEqual(rows.Count(), rows2.Length, "Select return rows");

            Assert.IsTrue(s0_Select.ElapsedMilliseconds > s00.ElapsedMilliseconds, 
	            $"Select is slower than code ({s0_Select.ElapsedMilliseconds} > {s00.ElapsedMilliseconds})");

            Assert.IsTrue(s1_Filter.ElapsedMilliseconds > s0_Select.ElapsedMilliseconds, 
	            $"Filter is slower than Select ({s1_Filter.ElapsedMilliseconds} > {s0_Select.ElapsedMilliseconds})");

            Assert.IsTrue(s2_Compiled.ElapsedMilliseconds <= s0_Select.ElapsedMilliseconds, 
	            $"Compiled Filter is faster than Select ({s2_Compiled.ElapsedMilliseconds} <= {s0_Select.ElapsedMilliseconds})");


            Assert.IsTrue(s2_Compiled.ElapsedMilliseconds <= s1_Filter.ElapsedMilliseconds, 
                    $"Compiled Filter is faster than Filter ({s2_Compiled.ElapsedMilliseconds} < {s1_Filter.ElapsedMilliseconds})");

            Assert.IsTrue(s3_code.ElapsedMilliseconds <= s2_Compiled.ElapsedMilliseconds, 
	            $"Compiled Filter is slower than code ({s3_code.ElapsedMilliseconds} < {s2_Compiled.ElapsedMilliseconds})");

            //Assert.IsTrue(s4.ElapsedMilliseconds < s2.ElapsedMilliseconds, 
	           // $"List is quicker than to array ({s4.ElapsedMilliseconds} < {s2.ElapsedMilliseconds})");

                          


        }

        [Test]
        public void ObjectSelectTest() {
            var t = Conn.SelectRowObjects("mandatedetail", filter:QHS.CmpGe("tax", 10), TOP:"100").GetAwaiter().GetResult();
            var rows = t.ToArray()._Select("idmankind", "yman", "nman", "taxable");
            Assert.IsNotNull(rows, "ObjectSelect return rows");
            Assert.AreEqual(rows.Count(), 100, "ObjectSelect returns all rows");
            Assert.IsInstanceOf(typeof(RowObject), rows.First(),  "ObjectSelect returns RowObject");
            foreach (var r in rows) {
                Assert.IsInstanceOf(typeof(string),MetaExpression.getField("idmankind", r));
                Assert.IsInstanceOf(typeof(Int16),MetaExpression.getField("yman", r));
                Assert.IsInstanceOf(typeof(int),MetaExpression.getField("nman", r));
                Assert.IsInstanceOf(typeof(Decimal),MetaExpression.getField("taxable", r));
            }
        }

        [Test]
        public void SelectGrouByTest() {
            DataTable t = Conn.Select("mandatedetail", filter: QHS.CmpGe("tax", 10), top:"100").GetAwaiter().GetResult();
            var rows = t.Select()._Select("idmankind", "yman", "nman", q.sum<decimal>("taxable")._as("totale") );
            Assert.IsNotNull(rows, "Select return rows");
            Assert.IsTrue(rows.Count()< 100, "select returns less rows");
            Assert.IsInstanceOf(typeof(RowObject), rows.First(), "select returns RowObject");
            foreach (dynamic r in rows) {
                Assert.IsInstanceOf(typeof(string),MetaExpression.getField("idmankind", r));
                Assert.IsInstanceOf(typeof(Int16),MetaExpression.getField("yman", r));
                Assert.IsInstanceOf(typeof(int),r.nman);
                Assert.IsInstanceOf(typeof(Decimal),MetaExpression.getField("totale", r));
            }
        }

        [Test] //   [Ignore("This is about speed")]
        public void TestSelectSpeed() {
            int limit = 3000;
            
            var t0 = Conn.SelectRowObjects("mandatedetail", filter:QHS.AppAnd(QHS.CmpGe("tax", 0),QHS.IsNotNull("taxable")), TOP:"10").GetAwaiter().GetResult();

            Stopwatch select1 = new Stopwatch();
            select1.Start();
            var t1 = Conn.SelectRowObjects("mandatedetail",filter:  QHS.AppAnd(QHS.CmpGe("tax", 0),QHS.IsNotNull("taxable")), TOP: $"{limit}").GetAwaiter().GetResult();
            select1.Stop();

            Stopwatch select2 = new Stopwatch();
            select2.Start();
            var t2 = Conn.Select("mandatedetail", filter:QHS.AppAnd(QHS.CmpGe("tax", 0),QHS.IsNotNull("taxable")),  top:$"{limit}").GetAwaiter().GetResult();
            select2.Stop();

            Stopwatch s1 = new Stopwatch();
            var rowsFromDataTable = t1.ToArray();
            s1.Start();
            var rows = rowsFromDataTable._Select();
            s1.Stop();
            Assert.IsNotNull(rows, "Select return rows");

            int nRows = rows.Count();
           
            Stopwatch s2 = new Stopwatch();
            var rowsFromDataTable2 = t2.Select();
            s2.Start();
            var rows2 = rowsFromDataTable._Select("idmankind");
            s2.Stop();

            Stopwatch s3 = new Stopwatch();            
            s3.Start();
            var rows3 = t2.Select();
            s3.Stop();
            //throughput = nrows / (1000*ElapsedMs)
            // throughput > 200 => nrows / (1000*ElapsedMs)>200 => nrows*200/1000 > ElapsedMs
            int expectedMimimumThrouhput = 4000;  //100000 rows for second, 100 row for millisecond
            int expectedMaximumTime = (limit*1000) / expectedMimimumThrouhput;

            Assert.IsTrue(select1.ElapsedMilliseconds < expectedMaximumTime, $"RowObject_Select ElapsedMilliseconds is less than {expectedMaximumTime}");
            Assert.IsTrue(select2.ElapsedMilliseconds < expectedMaximumTime, "RUN_SELECT ElapsedMilliseconds is less than {expectedMaximumTime}");
            Assert.AreEqual(rows.Count(), limit, "select returns all rows");
            Assert.IsInstanceOf( typeof(RowObject), rows.First(), "select returns dynamic entities");
            foreach (RowObject r in rows) {
                Assert.IsInstanceOf(typeof(string),MetaExpression.getField("idmankind", r));
                Assert.IsInstanceOf(typeof(Int16),MetaExpression.getField("yman", r));
                Assert.IsInstanceOf(typeof(int),r["nman"]);
                Assert.IsInstanceOf(typeof(Decimal),MetaExpression.getField("taxable", r));
            }
        }

        [Test]  //[Ignore("This is about speed")]
        public void SelectGrouBySpeedTest() {
            MetaProfiler.Enabled = true;
            int Limit = 100000;
            DataTable t = Conn.Select("assetview", 
                filter: QHS.AppAnd(QHS.IsNotNull("codeinv"),QHS.IsNotNull("taxable"),QHS.IsNotNull("yearstart")),
				top:Limit.ToString()
                ).GetAwaiter().GetResult();

            var tRows = t.Select();
            int NRowsBase = tRows.Count();
            Stopwatch s1 = new Stopwatch();
            s1.Start();
            var rows = tRows._Select("yearstart", "codeinv", "idinventoryagency", q.sum<decimal>("taxable")._as("totale"));
            s1.Stop();
            Assert.IsNotNull(rows, "Select return rows");
            int NRows=0;
            NRows= rows.Count();
            string s = MetaProfiler.ShowAll();
            Assert.IsTrue(NRows < Limit, "select returns less rows");
            int expectedMimimumThrouhput = 5000;  //10000 rows for second, 1 row for millisecond
            int expectedMaximumTime = (NRowsBase * 1000) / expectedMimimumThrouhput;

            Assert.IsTrue(s1.ElapsedMilliseconds < expectedMaximumTime, "Select GroupBy is quick");
            Assert.IsInstanceOf(typeof(RowObject), rows.First(),  "select returns RowObject");
            foreach (dynamic r in rows) {
                Assert.IsInstanceOf(typeof(Int32),MetaExpression.getField("yearstart", r));
                Assert.IsInstanceOf(typeof(String),MetaExpression.getField("codeinv", r) );
                Assert.IsInstanceOf(typeof(int),r.idinventoryagency);
                Assert.IsInstanceOf(typeof(Decimal),MetaExpression.getField("totale", r));
            }
        }


        [Test] 
        public void selectIndexedFilter() {
	        DataTable t = Conn.Select("assetview", top: "10000").GetAwaiter().GetResult();

	        int nSearch = 1000;


	        var filter = q.eq("idasset", 2) & q.eq("idpiece", 1);
	        Stopwatch s1 = new Stopwatch();
	        s1.Start();
	        for (int i = 1; i < nSearch; i++) {
		        var rows = t.filter(filter);
	        }
	        
	        //var rows = t.Select("taxable = 10000000");
	        s1.Stop();

	        var ds = new DataSet();
	        ds.Tables.Add(t);
	        ds.setIndexManager(new IndexManager(ds));
	        ds.getIndexManager().checkCreateIndex(t, new string []{"idasset", "idpiece"},true);

	        Stopwatch s2 = new Stopwatch();
	        s2.Start();
	        for (int i = 1; i < nSearch; i++) {
		        var rows = t.filter(filter);
	        }
	        //var rows = t.Select("taxable = 10000000");
	        s2.Stop();

	        Assert.Less(s2.ElapsedMilliseconds*100, s1.ElapsedMilliseconds,
		        $"Indexed find is 100 times faster than non indexed ({s2.ElapsedMilliseconds*100} < {s1.ElapsedMilliseconds})");
        }


    

    }

    
   

}
