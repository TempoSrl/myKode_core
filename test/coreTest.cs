using System;
using mdl;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using q = mdl.MetaExpression;
//using msUnit = Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using NUnit.Framework;

namespace test {

   class sampleBaseClass {
        protected int X;
        protected int Y;
        public sampleBaseClass(int x) {
            X = fn(x);
            Y = 100;
        }
        internal virtual int fn(int y) {
            return y;
        }
        internal int getX() {
            return X;
        }
        internal int getY() {
            return Y;
        }
    }

    class sampleDerivedClass:sampleBaseClass {
        public sampleDerivedClass(int x):base(x) {
            Y = 200;
        }
        internal override int fn(int y) {
            return y+10;
        }

        private int secretFun() {
            return 1;
        }
       
    }

    class normalClass {
        public int c;
    }
    class normalClassNullable {
        public int ?c;
    }

    class staticClass {
        public static string X;
        static staticClass() {
            X = "ciao";
        }
    }



    [TestFixture]
    public class SemaphoreTest {
        Semaphore queue = new Semaphore(0, 10);
        static List<int>  data = new List<int>();
        static List<int>  dataOutput = new List<int>();
        Semaphore queueStop = new Semaphore(0, 1);
        
        void produce(int X) {
            lock(data){
                data.Add(X);
            }
            Console.WriteLine("Producing "+X);
            queue.Release();
         
        }
        void consume() {
            queue.WaitOne();
            int dataToProcess;
            lock(data){
                dataToProcess = data[0];
                data.RemoveAt(0);
                dataOutput.Add(dataToProcess);
                Console.WriteLine("Consuming "+dataToProcess);
                if (dataToProcess == 50) {
                    queueStop.Release();
                }
            }
        }


        [Test]
        public void checkSemaforo1_1() {
            var t1 = Task.Run(() => {
                while (true) {
                    consume();

                } });
            var t2 = Task.Run(() => {
                foreach (int i in new[] {10, 20, 30, 40, 50}) {
                    produce(i);
                }
            });
            queueStop.WaitOne();
            Assert.AreEqual(5, dataOutput.Count);
        }
    }

    [TestFixture]
    public class DotNetTest {          
        //call virtual method in contructor should call derived fun
        [Test]
        public void callVirtualMethod() {
            sampleBaseClass b = new sampleBaseClass(1);
            Assert.AreEqual(1, b.getX(), "base function called");
            Assert.AreEqual(100, b.getY(), "normal constructor called");

            sampleDerivedClass c = new sampleDerivedClass(1);
            Assert.AreEqual(11, c.getX(), "derived function called");
            Assert.AreEqual(200, c.getY(), "derived constructor called after base");
        }

        [Test]
        public void callPrivateMethod() {
            sampleDerivedClass c = new sampleDerivedClass(1);
            //var cPublic = new msUnit.PrivateObject(c);
            MethodInfo method = typeof(sampleDerivedClass).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "secretFun" && x.IsPrivate)
                .First();
            Assert.AreEqual(1, method.Invoke(c,new object[] { }), "derived function called");
            //Assert.AreEqual(1, cPublic.Invoke("secretFun"), "derived function called");
           
        }

        //test auto implemented properties
        public int age { get; set; } = 42;

        //test different access levels for autoimplemented properties
        public int myAge { get; private set; } = 42;

        //test default parameter
        public int method1(int x, int y, int z, int q = 10) {
            return x + y - z * q;
        }

        //test named parameters
        [Test]
        public void callerMethod1() {
            int result = method1(z: -2, x: 3, y: 4);
            Assert.AreEqual(21, result, "test call with named params");
        }

        //test variable arguments
        public int method2(params int[] operands) {
            int sum = 0;
            foreach (int x in operands) sum += x;
            return sum;
        }

        //test variable arguments call
        [Test]
        public void callerMethod2() {
            int result = method2(3, 4, 5, 6);
            Assert.AreEqual(18, result, "test call with variable arguments");
        }

        private static string varCoalesce;
        //test coalescing
        public string methodCoalesce(int x) {
            return varCoalesce ??= $"assigned to {x}";
        }

        //check coalesce calling
        [Test]
        public void callerMethodCoalesce() {
            string res = methodCoalesce(3);
            Assert.AreEqual("assigned to 3", res, "test call coalesce");
        }

        //check  static constructor has been executed
        [Test]
        public void checkStaticConstructor() {
            Assert.AreEqual(staticClass.X, "ciao", "static class initialized");
        }

        public string firstName { get; } = "A";
        public string lastName { get; } = "B";
        public string fullName => $"{firstName} {lastName}";

        [Test]
        public void checkFunctionProperty() {
            Assert.AreEqual(fullName, "A B", "function property is ok");
        }


        [Test]
        public void checkAnonymousType() {
            var o = new { a = 1, b = "2" };
            Assert.AreEqual(o.b, "2", "anonymous field set");
            Assert.AreEqual(o.a, 1, "anonymous field set");
            var p = new { o.a };
            Assert.AreEqual(p.a, 1, "anonymous field set with implicit name");
        }

        [Test]
        public void checkNullableType() {
            int? x1 = null;
            Assert.IsNull(x1, "x1 is null");
            Assert.IsFalse(x1.HasValue, "x1 has no value");
            int x2 = x1 ?? 2;
            Assert.AreEqual(2, x2, "x2 correctly assigned");
            x1 = x2 + 1;
            Assert.AreEqual(3, x1, "x1 now has been assigned");
            Assert.IsTrue(x1.HasValue, "x1 has  value");
        }

        [Flags]
        public enum DaysOfWeek {
            None = 0,
            Monday = 1,
            Tuesday = 2,
            Wednesday = 4,
            Thursday = 8,
            Friday = 16,
            Saturday = 32,
            Sunday = 64,
            Weekend = Saturday | Sunday,
            Workday = 0x1F,
            Allweek = Weekend | Workday
        }

        [Test]
        public void checkFlags() {
            int x = (int)DaysOfWeek.Monday;
            Assert.AreEqual(1, x, "Monday value ok");
            x = (int)DaysOfWeek.Tuesday;
            Assert.AreEqual(2, x, "Tuesday value ok");
            x = (int)DaysOfWeek.Wednesday;
            Assert.AreEqual(4, x, "Wednesday value ok");
            x = (int)DaysOfWeek.Thursday;
            Assert.AreEqual(8, x, "Thursday value ok");
            x = (int)DaysOfWeek.Friday;
            Assert.AreEqual(16, x, "Friday value ok");
            x = (int)DaysOfWeek.Saturday;
            Assert.AreEqual(32, x, "Saturday value ok");
            x = (int)DaysOfWeek.Sunday;
            Assert.AreEqual(64, x, "Sunday value ok");

            Assert.AreEqual((int)(DaysOfWeek.Sunday | DaysOfWeek.Friday), 0x50, "2 days ok");
            Assert.AreEqual((DaysOfWeek.Sunday | DaysOfWeek.Friday).ToString(), "Friday, Sunday", "2 days ok");
            Assert.AreEqual(DaysOfWeek.Monday.ToString(), "Monday");

            DayOfWeek xx;
            bool res = DaysOfWeek.TryParse("Sunday", out xx);
            Assert.IsTrue(res, "Sunday parsed");
            Assert.AreEqual(xx, DayOfWeek.Sunday, "Sunday value parsed ok");
        }

        [Test]
        public void checkHashCode() {
            object o = new object();
            object p = new object();
            int x = o.GetHashCode();
            int y = p.GetHashCode();
            Assert.AreNotEqual(x, 0, "o has  hashcode");
            Assert.AreNotEqual(x, y, "two objects have different hashcode");
        }

        public Tuple<int, int> divide(int dividend, int divisor) {
            int result = dividend / divisor;
            int reminder = dividend % divisor;
            return Tuple.Create(result, reminder);
        }

        [Test]
        public void checkTuple() {
            var resDiv = divide(23, 5);
            Assert.AreEqual(4, resDiv.Item1, "Result is correct");
            Assert.AreEqual(3, resDiv.Item2, "Reminder is correct");
        }





        [Test]
        public void checkgetField() {
            DataTable t = new DataTable("x");
            t.Columns.Add("c", typeof(Int32));
            DataRow r = t.NewRow();
            r["c"] = 1;
            Assert.AreEqual(1, q.getField("c", r), "GetField(fieldName,DataRow) works");
            Assert.AreEqual(null, q.getField("d", r), "GetField(invalidfield,DataRow) works");

            dynamic e = new ExpandoObject();
            e.c = 2;
            Assert.AreEqual(2, q.getField("c", e), "GetField(fieldName,ExpandoObject) works");
            Assert.AreEqual(DBNull.Value, q.getField("d", e), "GetField(invalidfield,ExpandoObject) works");

            var values = new Dictionary<string, object>();
            values.Add("c", 3);
            dynamic post = new DynamicEntity(values);
            post.f = 5;

            //Assert.AreEqual(3, q.getField("c", post), "GetField(fieldName,DynamicObject) works");
            //Assert.AreEqual(5, q.getField("f", post), "GetField(fieldName,DynamicObject) works");
            Assert.AreEqual(DBNull.Value, q.getField("d", post), "GetField(invalidfield,DynamicObject) works");

            normalClass nClass = new normalClass();
            nClass.c = 4;
            Assert.AreEqual(4, q.getField("c", nClass), "GetField(fieldName,object) works");
            Assert.AreEqual(DBNull.Value, q.getField("d", nClass), "GetField(invalidfield,object) works");
        }

        [Test]
        public void DataColumnCaseInsensitive() {
            var t = new DataTable();
            t.CaseSensitive=true;
            t.Columns.Add("c",typeof(int));
            var r= t.NewRow();
            r["C"]=1;
            Assert.AreEqual(1,r["c"]);
        }

    }


    [TestFixture]
    public class CoreTest {
        public static DataAccess Conn;
        

        [OneTimeSetUp]
        public static void testInit() {
            Conn = MockUtils.getAllLocalDataAccess("utente2");

        }

        [OneTimeTearDown]
        public static void testEnd() {
            Conn.Destroy();
        }

        [Test]
        public void CreateTableByName() {
            DataTable t =  Conn.CreateTable("expensephase").GetAwaiter().GetResult();
            Assert.IsTrue(t.Columns["idexp"].ExtendedProperties.Count > 0, "CreateTableByName assigns ext.prop.");
        }

        [Test]
        public void CreateTableTwoRows() {
            DataTable t = Conn.CreateTable("expensephase").GetAwaiter().GetResult();
            MetaData.SetDefault(t,"ct",DateTime.Now);
            MetaData.SetDefault(t,"lt",DateTime.Now);
            MetaData.SetDefault(t,"cu","nino");
            MetaData.SetDefault(t,"lu","nino");
            MetaData.SetDefault(t,"description","nino");
            
            DataRow r1 = t.NewRow();
            r1["nphase"] = 1;
            t.Rows.Add(r1);

            DataRow r2 = t.NewRow();
            r2["nphase"] = 2;
            t.Rows.Add(r2);

            Assert.IsTrue(t.Rows.Count==2,"Simple add row to table");
        }
        [Test]
        public void CreateTableTwoEqualRows() {
            DataTable t = Conn.CreateTable("expensephase").GetAwaiter().GetResult();
            t.Constraints.Clear();
            MetaData.SetDefault(t,"ct",DateTime.Now);
            MetaData.SetDefault(t,"lt",DateTime.Now);
            MetaData.SetDefault(t,"cu","nino");
            MetaData.SetDefault(t,"lu","nino");
            MetaData.SetDefault(t,"description","nino");
            
            DataRow r1 = t.NewRow();
            r1["nphase"] = 1;
            t.Rows.Add(r1);

            DataRow r2 = t.NewRow();
            r2["nphase"] = 1;
            t.Rows.Add(r2);

            Assert.IsTrue(t.Rows.Count==2,"Simple add double row to table");
        }

        [Test]
        public void CreateTableTwoEqualRowsConstraintsClear() {
            DataTable t = Conn.CreateTable("expensephase").GetAwaiter().GetResult();
            t.Constraints.Clear();
            MetaData.SetDefault(t,"ct",DateTime.Now);
            MetaData.SetDefault(t,"lt",DateTime.Now);
            MetaData.SetDefault(t,"cu","nino");
            MetaData.SetDefault(t,"lu","nino");
            MetaData.SetDefault(t,"description","nino");
            
            DataRow r1 = t.NewRow();
            r1["nphase"] = 1;
            t.Rows.Add(r1);

            DataRow r2 = t.NewRow();
            r2["nphase"] = 1;
            t.Rows.Add(r2);

            Assert.IsTrue(t.Rows.Count==2,"Simple add double row to table");
        }

        [Test]
        public void CreateTableTwoEqualRowsClearDataSet() {
            DataTable t = Conn.CreateTable("expensephase").GetAwaiter().GetResult();
           ClearDataSet.RemoveConstraints(t);
            MetaData.SetDefault(t,"ct",DateTime.Now);
            MetaData.SetDefault(t,"lt",DateTime.Now);
            MetaData.SetDefault(t,"cu","nino");
            MetaData.SetDefault(t,"lu","nino");
            MetaData.SetDefault(t,"description","nino");
            
            DataRow r1 = t.NewRow();
            r1["nphase"] = 1;
            t.Rows.Add(r1);


            try {
                DataRow r2 = t.NewRow();
                r2["nphase"] = 1;
                t.Rows.Add(r2);
                Assert.Fail("No Exception thrown");
            }
            catch {
                Assert.IsTrue(true);
            }
            
                
            
        }

        [Test]
        public void clone() {
            DataTable t = Conn.CreateTable("expense", "*", true).GetAwaiter().GetResult();
            DataTable ty = Conn.CreateTable("expenseyear", "*", true).GetAwaiter().GetResult();
            DataSet dd = new DataSet("dd");
            dd.Tables.Add(t);
            dd.Tables.Add(ty);
            dd.defineRelation("rel", "expense", "expenseyear", "idexp");
            DataTable t2 = t.Clone();
            Assert.IsTrue(t2.Columns["idexp"].ExtendedProperties.Count > 0, "Clone assigns ext.prop.");
            Assert.IsNull(t2.DataSet, "Clone does not copy dataset");
        }


        [Test]
        public void testCloneSpeed() {
            DataTable t = Conn.CreateTable("expense").GetAwaiter().GetResult();
            DataTable ty = Conn.CreateTable("expenseyear").GetAwaiter().GetResult();
            DataSet dd = new DataSet("dd");
            dd.Tables.Add(t);
            dd.Tables.Add(ty);
            dd.defineRelation("rel", "expense", "expenseyear", "idexp");
            int nItineration = 1000;
            Stopwatch S1 = new Stopwatch();
            S1.Start();
            for (int i = 0; i < nItineration; i++) {
                DataTable t2 = t.Clone();
            }
            S1.Stop();
            Stopwatch S2 = new Stopwatch();
            S2.Start();
            for (int i = 0; i < nItineration; i++) {
                DataTable t3 = DataAccess.singleTableClone(t, false);
            }
            S2.Stop();
            Console.WriteLine($"Clone duration:{S1.ElapsedMilliseconds}");
            Console.WriteLine($"SingleTableClone(,false) duration:{S2.ElapsedMilliseconds}");
            Assert.IsTrue(S1.ElapsedMilliseconds > S2.ElapsedMilliseconds, "SingleTableClone(,false)  is quicker");

            Stopwatch S3 = new Stopwatch();
            S3.Start();
            for (int i = 0; i < nItineration; i++) {
                DataTable t3 = DataAccess.singleTableClone(t, true);
            }
            S3.Stop();
            Console.WriteLine($"SingleTableClone(,true) duration:{S3.ElapsedMilliseconds}");
            Assert.IsTrue(S1.ElapsedMilliseconds > S2.ElapsedMilliseconds, "SingleTableClone(,true)  is quicker");


        }
        
        //[I212112gnore("This is about speed")]


        [Test]
        public void runSelectSpeed1000_by_10() {
            int nIteration = 1000;
            string top = "10";
            Stopwatch SRUN_SELECT = new Stopwatch();
            SRUN_SELECT.Start();
            int rCount = 0;
            for (int i = 0; i < nIteration; i++) {
                DataTable t = Conn.Select("entry", top:top).GetAwaiter().GetResult();
                rCount += t.Rows.Count;
            }
            SRUN_SELECT.Stop();
          

            Stopwatch SreadFromTable = new Stopwatch();
            SreadFromTable.Start();
            for (int i = 0; i < nIteration; i++) {
                var t4 = Conn.ReadTableNoKey("entry", TOP:top);
            }
            SreadFromTable.Stop();

            Stopwatch SRowObject_Select = new Stopwatch();
            SRowObject_Select.Start();
            for (int i = 0; i < nIteration; i++) {
                var t5 = Conn.RowObjectSelect("entry",TOP:top);
            }
            SRowObject_Select.Stop();

            Console.WriteLine($"Rows read:{rCount}");
            Console.WriteLine($"runSelect   duration:{SRUN_SELECT.ElapsedMilliseconds}");
            Console.WriteLine($"readFromTable    duration:{SreadFromTable.ElapsedMilliseconds}");
            Console.WriteLine($"RowObject_Select    duration:{SRowObject_Select.ElapsedMilliseconds}");
            Assert.IsTrue(SreadFromTable.ElapsedMilliseconds < SRowObject_Select.ElapsedMilliseconds, "RowObject_Select  is quicker");

        }

        //[Igno1212re("This is about speed")]
        [Test]
        public void runSelectSpeed10_by_10000() {
           


            int nIteration = 10;
            string top = "10000";
            Stopwatch SRUN_SELECT = new Stopwatch();
            SRUN_SELECT.Start();
            int rCount = 0;
            for (int i = 0; i < nIteration; i++) {
                DataTable t = Conn.Select("entry", top: top).GetAwaiter().GetResult();
                rCount += t.Rows.Count;
            }
            SRUN_SELECT.Stop();
        

            Stopwatch SreadFromTable = new Stopwatch();
            SreadFromTable.Start();
            for (int i = 0; i < nIteration; i++) {
                var t4 = Conn.ReadTableNoKey("entry", filter: null, TOP: top);
            }
            SreadFromTable.Stop();

            Stopwatch SRowObject_Select = new Stopwatch();
            SRowObject_Select.Start();
            for (int i = 0; i < nIteration; i++) {
                var t5 = Conn.RowObjectSelect("entry", TOP: top);
            }
            SRowObject_Select.Stop();

            Console.WriteLine($"Rows read:{rCount}");
            Console.WriteLine($"runSelect   duration:{SRUN_SELECT.ElapsedMilliseconds}");
            Console.WriteLine($"readFromTable    duration:{SreadFromTable.ElapsedMilliseconds}");
            Console.WriteLine($"RowObject_Select    duration:{SRowObject_Select.ElapsedMilliseconds}");

            Assert.IsTrue(SreadFromTable.ElapsedMilliseconds < SreadFromTable.ElapsedMilliseconds,
                $"readFromTable  is  quicker  ({SreadFromTable.ElapsedMilliseconds} < {SreadFromTable.ElapsedMilliseconds}) ");
            Assert.IsTrue(SreadFromTable.ElapsedMilliseconds > SRowObject_Select.ElapsedMilliseconds, 
                $"RowObject_Select  is quicker  ({SRowObject_Select.ElapsedMilliseconds} < {SreadFromTable.ElapsedMilliseconds}) ");

        }






        object getYEntry(DataRow r) {
            return r["yentry"];
        }

        //[Test]
        //public void accessToDetachedRow() {
        //    var t =Conn.CreateTableByName("entry", "*");
        //    var r = t.NewRow();
        //    r["yentry"] = Conn.GetEsercizio();
        //    Assert.Throws<Exception>(()=>getYEntry(r));
        //}
    }

   
   

}
