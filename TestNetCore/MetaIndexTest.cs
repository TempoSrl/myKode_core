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

namespace TestNetCore {
	/// <summary>
	/// Summary description for MetaIndexTest
	/// </summary>
	[TestFixture]
	public class MetaIndexTest {
		public static DataAccess conn;
		static QueryHelper QHS;

		string checkUniqueIndex(MetaTableUniqueIndex index, DataTable t) {
			var fields = index.hash.keys;
			string me = $"{t.TableName} unique index on fields " + string.Join(",", fields);
			//Check that all rows in tables are found in the index
			int nRealRow = 0;
			foreach (DataRow r in t.Select()) {
				nRealRow++;
				var rFound = index.getRow(index.hash.get(r));
				if (rFound == null)
					return $"Some row is not found in {me}";
				if (rFound != r) return $"Wrong row found in {me}";
			}

			int nIndexedRow = 0;
			foreach (var r in index.lookup.Values) {
				if (r == null) return $"Disposed row found in {me}";
				if (r.RowState == DataRowState.Detached) return $"Detached row found  in {me}";
				if (r.RowState == DataRowState.Deleted) return $"Deleted row found  in {me}";
				nIndexedRow++;
			}

			if (nIndexedRow != nRealRow) return $"Expected {nRealRow} in index but {nIndexedRow} found in {me}";
			return null;
		}

		string checkNotUniqueIndex(MetaTableNotUniqueIndex index, DataTable t) {
			var fields = index.hash.keys;
			string me = $"{t.TableName} not unique index on fields " + string.Join(",", fields);
			//Check that all rows in tables are found in the index
			int nRealRow = 0;
			foreach (DataRow r in t.Select()) {
				nRealRow++;
				var rFound = index.getRows(index.hash.get(r));
				if (rFound == null || rFound.Length == 0) {
					return $"Some row is not found in {me}";
				}

				bool reallyFound = rFound._Filter(x => x == r)._HasRows();
				if (!reallyFound) {
					return $"Some row found in {me}  but not the searched one.";
				}
			}

			int nIndexedRow = 0;
			foreach (var rList in index.lookup.Values) {
				foreach (var r in rList) {
					if (r == null) return $"Disposed row found in {me}";
					if (r.RowState == DataRowState.Detached) return $"Detached row found  in {me}";
					if (r.RowState == DataRowState.Deleted) return $"Deleted row found  in {me}";
					nIndexedRow++;
				}
			}

			if (nIndexedRow != nRealRow) return $"Expected {nRealRow} in index but {nIndexedRow} found in {me}";
			return null;
		}

		string checkDataSetIndexes(DataSet d) {
			var idm = d.getIndexManager();
			if (idm == null) return "Index not found on Dataset in form ";

			foreach (var idx in idm.getIndexes()) {
				if (idx is MetaTableUniqueIndex) {
					var result = checkUniqueIndex((MetaTableUniqueIndex) idx, d.Tables[idx.tableName]);
					if (result != null) return result;
				}

				if (idx is MetaTableNotUniqueIndex) {
					var result = checkNotUniqueIndex((MetaTableNotUniqueIndex) idx, d.Tables[idx.tableName]);
					if (result != null) return result;
				}

			}

			return null;
		}

		[OneTimeSetUp]
		public static void testInit() {
			conn = MockUtils.getAllLocalDataAccess("utente2");
			QHS = conn.GetQueryHelper();
		}

		[OneTimeTearDown]
		public static void testEnd() {
			conn.Destroy();
		}

		DataSet d;
		private DataTable t;
		private IndexManager idm;
		private IMetaIndex index;
		private IMetaIndex index2;
		private IMetaIndex index3;


		[SetUp]
		public void testStart() {
			d = new DataSet();
			t = conn.CreateTable("asset", "*").GetAwaiter().GetResult();
			d.Tables.Add(t);
			idm = new IndexManager(d);
			idm.createPrimaryKeysIndexes();
			index = idm.getIndex(t, "idasset", "idpiece");
			index2 = new MetaTableNotUniqueIndex(t, "idasset");
			idm.addIndex(t, index2);
			index3 = new MetaTableNotUniqueIndex(t, "flag");
			idm.addIndex(t, index3);
		}

		[Test]
		public void indexExists() {
			Assert.IsNotNull(index, "Index exists");
			Assert.AreEqual(2, index.hash.keys.Length, "Index has two fields");
			Assert.AreEqual(1, index2.hash.keys.Length, "Index has one fields");
			Assert.AreEqual(1, index3.hash.keys.Length, "Index has one fields");
		}


		[Test]
		public void simpleAdditions() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				Assert.AreEqual(0, index.getRows(index.hash.get(r)).Length, "detached row is not indexed");
				Assert.AreEqual(0, index2.getRows(index2.hash.get(r)).Length, "detached row is not indexed by idasset");
				Assert.AreEqual(0, index3.getRows(index3.hash.get(r)).Length, "detached row is not indexed by flag");
				t.Rows.Add(r);
				Assert.AreEqual(1, index.getRows(index.hash.get(r)).Length, "Added row is indexed");
				Assert.AreEqual(1, index2.getRows(index2.hash.get(r)).Length, "Added row is indexed by idasset");
				Assert.AreEqual(1, index3.getRows(index3.hash.get(r)).Length, "Added row is indexed by flag");

				Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleAdditions");
			}
		}

		[Test]
		public void addAndReject() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
				var hash = index.hash.get(r);
				var hash2 = index2.hash.get(r);
				var hash3 = index3.hash.get(r);

				r.RejectChanges();
				Assert.AreEqual(0, index.getRows(hash).Length, "detached row is not indexed");
				Assert.AreEqual(0, index2.getRows(hash2).Length, "detached row is not indexed by idasset");
				Assert.AreEqual(0, index3.getRows(hash3).Length, "detached row is not indexed by flag");
				Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo addAndReject");

			}
		}

		[Test]
		public void deleteAndReject() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
			}
			t.RejectChanges();
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo deleteAndReject");

		}
		[Test]
		public void deleteAndAcceptChanges() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
			}
			t.AcceptChanges();
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo deleteAndAcceptChanges");

		}


		[Test]
		public void addAndTableReject() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
				

				

			}
			t.RejectChanges();

			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo modifiedAdditions");

		}

		[Test]
		public void modifiedAdditions() {
			for (int i = 1; i < 10; i++) {
				var r = t.NewRow();
				r["idasset"] = i+2000;
				r["flag"] = i+100 ;
				r["idpiece"] = i+1000;

				r["idasset"] = i+1000;
				r["flag"] = i ;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
			

				Assert.AreEqual(0, index.getRows(index.hash.get(r)).Length, "detached row is not indexed");
				Assert.AreEqual(0, index2.getRows(index2.hash.get(r)).Length, "detached row is not indexed by idasset");
				Assert.AreEqual(0, index3.getRows(index3.hash.get(r)).Length, "detached row is not indexed by flag");

				t.Rows.Add(r);

				Assert.AreEqual(1, index.getRows(index.hash.get(r)).Length, "Added row is indexed");
				Assert.AreEqual(r, index.getRows(index.hash.get(r))[0], "Row by index is ok");

				Assert.AreEqual(1, index2.getRows(index2.hash.get(r)).Length, "Added row is indexed by idasset");
				Assert.AreEqual(r, index2.getRows(index2.hash.get(r))[0], "Row by index is ok");

				Assert.AreEqual(1, index3.getRows(index3.hash.get(r)).Length, "Added row is indexed by flag");
				Assert.AreEqual(r, index3.getRows(index3.hash.get(r))[0], "Row by index is ok");

				r.BeginEdit();
				r["idasset"] = i+10000;
				r["idpiece"] = 1+10000;
				r["flag"] = i + 200;
				r["flag"] = i + 201;
				r["flag"] = i + 200;
				r.EndEdit();
			
				Assert.AreEqual(1, index.getRows(index.hash.get(r)).Length, "Added row is indexed");
				Assert.AreEqual(r, index.getRows(index.hash.get(r))[0], "Row by index is ok");

				Assert.AreEqual(1, index2.getRows(index2.hash.get(r)).Length, "Added row is indexed by idasset");
				Assert.AreEqual(r, index2.getRows(index2.hash.get(r))[0], "Row by index is ok");

				Assert.AreEqual(1, index3.getRows(index3.hash.get(r)).Length, "Added row is indexed by flag");
				Assert.AreEqual(r, index3.getRows(index3.hash.get(r))[0], "Row by index is ok");

				Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo modifiedAdditions");
			}
		}



		[Test]
		public void simpleAdditionsAndRetrieve() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			//t.AcceptChanges();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter by idasset gives the correct row");
			}


			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve by wrong key gives no row");
			}


			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve by wrong idasset gives no row");
			}


			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleAdditionsAndRetrieve");
		}


		[Test]
		public void simpleUpdates() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
			}
			//t.AcceptChanges();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");

				filter = q.eq("idasset", i);
				r = t.filter(filter);
				Assert.AreEqual(1, r.Length, "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter by idasset gives the correct row");

				r[0]["idasset"] = i + 100;
			}



			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve modified rows returns data ");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by idasset modified rows returns data ");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(0, r.Length, "Retrieve old rows returns no data");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(0, r.Length, "Retrieve old rows by idasset returns no data");
			}


			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleUpdates");
		}

		[Test]
		public void updatesAndReject() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
			}
			t.AcceptChanges();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				r[0]["idasset"] = i + 100;
				r[0]["flag"] = i;
				r[0]["idpiece"] = 2;
			}
			foreach(DataRow r in t.Select()) {
				r.RejectChanges();
			}
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleUpdates");
		}

		[Test]
		public void updatesAndTableReject() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = i * 2;
				t.Rows.Add(r);
			}
			t.AcceptChanges();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				r[0]["idasset"] = i + 100;
				r[0]["flag"] = i;
				r[0]["idpiece"] = 2;
			}
			t.RejectChanges();
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleUpdates");
		}

		[Test]
		public void simpleDeletes() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}
			//t.AcceptChanges();

			for (int i = 10; i < 30; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				r[0].Delete();
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				if (i >= 10 && i < 30) {
					Assert.AreEqual(r.Length, 0, "Retrieve modified rows returns no data ");
				}
				else {
					Assert.AreEqual(r.Length, 1, "Retrieve unchanged rows returns data");
				}
			}


			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				if (i >= 10 && i < 30) {
					Assert.AreEqual(r.Length, 0, "Retrieve modified rows by idasset returns no data ");
				}
				else {
					Assert.AreEqual(r.Length, 1, "Retrieve unchanged rows by idasset  returns data");
				}
			}
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo simpleDeletes");
		}

		[Test]
		public void tableClear() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			t.AcceptChanges();

			t.Clear();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve unchanged rows returns no data");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve unchanged rows by idasset returns no data");
			}

			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo tableClear");
		}

		[Test]
		public void tableClearBeginLoadData() {
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			t.AcceptChanges();
			t.BeginLoadData();
			t.Clear();
			t.EndLoadData();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve unchanged rows returns no data");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve unchanged rows by id asset returns no data");
			}

			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo tableClearBeginLoadData");
		}


		[Test]
		public void beginLoadDataEndLoadDataAddRow() {
			t.BeginLoadData();
			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			t.EndLoadData();

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}
			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo beginLoadDataEndLoadDataAddRow");
		}

		[Test]
		public void beginLoadDataEndLoadDataModifyRow() {

			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			t.BeginLoadData();
			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(1, r.Length, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");

				filter = q.eq("idasset", i);
				r = t.filter(filter);
				Assert.AreEqual(1, r.Length,  "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter by idasset gives the correct row");
				r[0]["idasset"] = i + 100;
			}

			t.EndLoadData();

			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 1, "Retrieve by idasset gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}


			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve by key of old rows gives no row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i);
				var r = t.filter(filter);
				Assert.AreEqual(r.Length, 0, "Retrieve by idasset of old rows gives no row");
			}

			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo beginLoadDataEndLoadDataModifyRow");
		}

		[Test]
		public void beginEditEndEditModifyRow() {

			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(1, r.Length, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
				r[0].BeginEdit();
				r[0]["idasset"] = i + 100;
				r[0].EndEdit();
			}

			for (int i = 101; i < 200; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(1, r.Length, "Retrieve by key gives one row");
				Assert.AreEqual(r[0]["idasset"], i, "_Filter gives the correct row");
			}

			for (int i = 1; i < 100; i++) {
				q filter = q.eq("idasset", i) & q.eq("idpiece", 1);
				var r = t.filter(filter);
				Assert.AreEqual(0, r.Length, "Retrieve by key of old rows gives no row");
			}

			Assert.IsNull(checkDataSetIndexes(d), "Dataset integro dopo beginEditEndEditModifyRow");
		}


		[Test]
		public void testIndexIntegrity() {
			t.Clear();

			for (int i = 1; i < 100; i++) {
				var r = t.NewRow();
				r["idasset"] = i;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = 0;
				t.Rows.Add(r);
				Assert.IsNull(checkDataSetIndexes(d), "Dataset integro");



			}

			for (int j = 0; j < 10000; j++) {
				string lastOperation = doRandomOperationOnTableAsset(t);
				Assert.IsNull(checkDataSetIndexes(d), $"{j} Dataset integro dopo {lastOperation}");

			}

		}



		string doRandomOperationOnTableAsset(DataTable t) {
			var rnd = new Random();
			string result;
			//Random add
			if (rnd.Next(10) < 3 || t.Rows.Count < 10) {
				var maxid = (from DataRow rr in t.Select() select (int) rr["idasset"]).Max();
				var r = t.NewRow();
				r["idasset"] = maxid + 1;
				r["idpiece"] = 1;

				r["ct"] = DateTime.Now;
				r["lt"] = DateTime.Now;
				r["cu"] = "test";
				r["lu"] = "test";
				r["flag"] = rnd.Next(100);
				t.Rows.Add(r);
				return "Add random row";
			}

			var choice = rnd.Next(5);
			//Random update 
			if (choice==1) {
				var currentRows = t.Select();
				var pick = currentRows[rnd.Next(currentRows.Length - 1)];
				result = $"Random Update {pick.RowState} ";
				bool withBeginEndEdit = rnd.Next(2) == 1;
				if (withBeginEndEdit) {
					pick.BeginEdit();
					result += " with BeginEndEdit ";
				}

				if (rnd.Next(10) < 4) {
					pick["idpiece"] = (int) pick["idpiece"] + 1;
					result += $"idpiece[{pick["idpiece"]}] ";
				}

				if (rnd.Next(10) < 4) {
					pick["flag"] = rnd.Next(100);
					result += $"flag[{pick["flag"]}] ";
				}

				if (rnd.Next(10) < 4) {
					pick["lu"] = rnd.Next(100).ToString();
				}

				if (rnd.Next(10) < 4) {
					var maxid = (from DataRow rr in t.Select() select (int) rr["idasset"]).Max();
					pick["idasset"] = maxid + 1;
					result += $"idasset[{pick["idasset"]}] ";
				}

				if (withBeginEndEdit) pick.EndEdit();
				return result;
			}


			//Random delete
			if (choice==2) {
				var currentRows = t.Select();
				var pick = currentRows[rnd.Next(currentRows.Length - 1)];
				result = $"delete row id {pick["idasset"]} {pick.RowState} ";
				pick.Delete();
				return result;
			}


			//Random reject changes
			if (choice==3) {
				var pick = t.Rows[rnd.Next(t.Rows.Count)];
				result = $"RejectChanges row id {pick["idasset",pick.RowState==DataRowState.Deleted?DataRowVersion.Original:DataRowVersion.Default]} {pick.RowState} ";
				pick.RejectChanges();
				return result;
			}

			//Random accept changes
			if (choice==4) {
				var pick = t.Rows[rnd.Next(t.Rows.Count)];
				result = $"AcceptChanges row id {pick["idasset",pick.RowState==DataRowState.Deleted?DataRowVersion.Original:DataRowVersion.Default]} {pick.RowState} ";
				pick.AcceptChanges();
				return result;
			}
			return "no action";
		}
	}

}
