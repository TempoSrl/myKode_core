using System;

using mdl;
using System.Data;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using Moq;
using NUnit;
using Moq.Language.Flow;
using Moq.Language;
using Moq.Linq;
using Moq.Protected;
using q = mdl.MetaExpression;
using NUnit.Framework;

namespace TestNetCore {
    [TestFixture]
    class dataAccessTest {
        public DataAccess Conn;
        public DataAccess mockConn;
        public Mock<DataAccess> mockDA;
        static QueryHelper QHS;
        [SetUp]
        public void testInit() {
            Conn = MockUtils.getAllLocalDataAccess("utente1");
            mockDA = MockUtils.MockDataAccess(Conn.Descriptor);
            mockConn = mockDA.Object;
            QHS = Conn.GetQueryHelper();
        }
        [TearDown]
        public void testEnd() {
            Conn.Destroy().Wait();
        }

        [Test]
        public void Open_Test() {
            //Variabile booleana per l'analisi del risultato
            bool result;
            //Creo una variabile nesting esterna alla classe per fare dei controlli aggiuntivi
            //e tener traccia del numero di connessioni
            int nesting = 0;

            #region 1th open / !openError / persisting
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(1, nesting, "Open connection first time");
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection first time. Connection state control");

            #endregion

            #region 2th open / !openError / persisting
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(2, nesting, "Open connection second time");
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection second time. Connection state control");
            #endregion

            #region 3th open / !openError / !persisting
            //Non passa per la seconda volta nella sezione di codice DO_SYS_CMD("SET ARITHABORT ON", true)
            Conn.Persisting = false;
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(3, nesting, "Open connection third time with not persisting");
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection third time with not persisting. Connection state control");

            Conn.Persisting = true;
            #endregion

            #region 4th open / openError / persisting 
            Conn.BrokenConnection = true;
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(3, nesting, "Open connection with openError");
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection with openError. Connection state control");
            Conn.BrokenConnection = false;
            #endregion

            #region 1th open / !openError / !persisting
            nesting = closeAll(nesting);
            Conn.Persisting = false;
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(1, nesting, "Open connection first time with not persisting");
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection first time with not persisting. Connection state control");
            Conn.Persisting = true;
            #endregion

            #region 1th open when all connection are closed / !openError / persisting
            //Sto testando assureOpen quando nesting è 0
            nesting = closeAll(nesting);
            nesting = (Conn.Open().GetAwaiter().GetResult()) ? nesting + 1 : nesting;
            Assert.AreEqual(1, nesting, "Open connection first time when i have close previusly connection");
            #endregion

            #region mainConnection != null
            //Creazione di un mockIDataAccess per ottenere una mainConnection diversa da null
            var mockIDataAccess = getMockIDataAccess(Conn.Descriptor);

            //Cambio mainConnection utilizzando il metodo della classe
            Conn.startPosting(mockIDataAccess.Object);

            //Caso in cui mainConnection è diverso da null
            result = Conn.Open().GetAwaiter().GetResult();
            Assert.IsTrue(result, "return true when mainConnection is not null");

            //Faccio ritornare mainConnection a null
            Conn.stopPosting();
            #endregion

            #region MySqlConnection == null
            Conn.Destroy().Wait();
            result = Conn.Open().GetAwaiter().GetResult();
            Assert.IsFalse(result, "Open destroyed connection");
            #endregion

        }

        [Test]
        public void Close_Test() {
            #region Chiusura di una connessione persistente mai aperta / nesting = 0
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Closed, Conn.Driver.State);
            #endregion

            #region Chiusura di una connessione persistente / nesting > 0
            Conn.Open().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State);
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State);

            #endregion

            Conn.Persisting = false;

            #region Chiusura di una connessione non persistente / nesting == 0
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Closed, Conn.Driver.State);
            #endregion

            #region Chiusura di una connessione non persistente / nesting == 1
            Conn.Open().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State);
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Closed, Conn.Driver.State);
            #endregion

            #region Chiusura di una connessione non persistente / nesting > 1
            Conn.Open().Wait();
            Conn.Open().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State);
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State);
            #endregion

            #region Chiusura di una connessione quanto mainConnection != null
            var mockIDataAccess = getMockIDataAccess(Conn.Descriptor);
            //Cambio mainConnection utilizzando il metodo della classe
            Conn.startPosting(mockIDataAccess.Object);

            //Caso in cui mainConnection è diverso da null
            Conn.Close().Wait();

            //Faccio ritornare mainConnection a null
            Conn.stopPosting();
            #endregion

        }

        [Test]
        public void GetSys_Test() {
            //Avendo delle chiavi definite, setto un comportamento per tali chiavi, altrimenti torna null
            var mockSecurity = new Mock<ISecurity>(MockBehavior.Strict);
            mockSecurity.Setup(x => x.GetSys(It.IsAny<string>())).Returns((string s) => returnDummy(s));

            Conn.Security = mockSecurity.Object;

            var temp = Conn.Security.GetSys("database");
            Assert.AreEqual("dummydatabase", temp,"Test with database key");

            temp = Conn.Security.GetSys("esercizio");
            Assert.AreEqual("dummyesercizio", temp, "Test with esercizio key");

            temp = Conn.Security.GetSys("testdummy");
            Assert.IsNull(temp);         
        }

        [Test]
        public void Destroy_Test() {
            Conn.Open().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open connection");

            #region Destroy open connection
            Conn.Destroy().Wait();
            Assert.IsNull(Conn.Driver, "Destroy open connection");
            #endregion

            #region Destroy closed connection
            testInit();
            Assert.IsNotNull(Conn.Driver, "Create new MySqlConnection");
            Conn.Open().Wait();
            Assert.AreEqual(ConnectionState.Open, Conn.Driver.State, "Open Connection");
            Conn.Persisting = false;
            Conn.Close().Wait();
            Assert.AreEqual(ConnectionState.Closed, Conn.Driver.State, "Close Connection");
            Conn.Destroy().Wait();
            Assert.IsNull(Conn.Driver, "Destroy open connection");
            #endregion

            #region Destroy destroyed connection
            testInit();
            Assert.IsNotNull(Conn.Driver, "Create new MySqlConnection");
            Conn.Destroy().Wait();
            Assert.IsNull(Conn.Driver, "Destroy new MySqlConnection");
            Conn.Destroy().Wait();
            Assert.IsNull(Conn.Driver, "Simply return because MySqlConnection is null");
            #endregion

            #region Destroy connection with s?.Connection != null
            testInit();
            Assert.DoesNotThrow(()=> {
                Conn.BeginTransaction(IsolationLevel.Unspecified).GetAwaiter().GetResult();
            });
            
            Conn.errorLogger = new Mock<IErrorLogger>(MockBehavior.Default).Object;
            Conn.Destroy().Wait();
            Assert.IsNull(Conn.Driver, "Destroy connection with current transaction");
            #endregion
        }

        [Test]
        public void rDataAccess_Run_Select_Test() {
			int count;

			//Non buono come test in quanto dipende da un DB reale. Da aggiornare successivamente           
			#region RUN_SELECT
			//Ho usato top 3000 e la condizione dentro run_select altrimenti non potevo confrontarla con il count
			//ed ho preferito usare il il parametro top altrimenti si rallenta eccessivamente il test
			var t = Conn.Select("registry", filter: q.gt("idregistryclass", 23) & q.like("title", "a%")).GetAwaiter().GetResult();
			count = Conn.Count("registry",filter: q.field("idregistryclass") > q.constant(23) & q.like("title","a%")).GetAwaiter().GetResult();
            Assert.AreEqual(t.Rows.Count, count, "DataAccess.Count with table is ok");


            t = Conn.Select("expenseview",columnlist: "idexp", filter: "curramount>1300000").GetAwaiter().GetResult();
            //greater = t._Filter(q.field("idregistryclass") > q.constant("23"));

            count = Conn.Count("expenseview", q.field("curramount") > q.constant(1300000)).GetAwaiter().GetResult();
            Assert.AreEqual(t.Rows.Count, count, "DataAccess.Count with view is ok");
            #endregion
        }

        [Test]
        public void rDataAccess_Run_Select_Test_Errored() {
	        ErrorLogger.applicationName = "MetaData Test";

			//Non buono come test in quanto dipende da un DB reale. Da aggiornare successivamente
			#region RUN_SELECT
            var t= Conn.Select("expenseview2", columnlist: "idexp", filter: "curramount > 1300000")
                  .GetAwaiter().GetResult();           
			Assert.IsNull(t, "A table is not returned");
	        //Assert.AreEqual(0, t.Columns.Count, "error in select give null result");
	        Assert.AreNotEqual(null, Conn.SecureGetLastError(), "Last error is set");

	        #endregion
        }

        [Test]
        public void rDataAccess_readValue() {
            //Non buono come test in quanto dipende da un DB reale. Da aggiornare successivamente
            object idreg = Conn.ReadValue("registry", q.eq("cf", "not existent cf !"), "title").GetAwaiter().GetResult();            
            Assert.AreEqual(null, idreg, "readValue returns null on no row found");

            object idbadge = Conn.ReadValue("registry", q.eq("idreg", 1), "badgecode").GetAwaiter().GetResult();                        
            Assert.AreEqual(DBNull.Value, idbadge, "readValue returns DBNull on row found with null value");
     
        }



        /// <summary>
        /// For testing GetSys.Return dummyString
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string returnDummy(string s) {
            string response = null;
            switch (s.ToLower()) {
            case "metadataversion":
                response = "dummyversion";
                break;
            case "computername":
                response = "dummycomputername";
                break;
            case "database":
                response = "dummydatabase";
                break;
            case "datacontabile":
                response = "dummydatacontabile";
                break;
            case "passworddb":
                response = "dummypassworddb";
                break;
            case "server":
                response = "dummyserver";
                break;
            case "computeruser":
                response = "dummycomputeruser";
                break;
            case "esercizio":
                response = "dummyesercizio";
                break;
            case "user":
                response = "dummyuser";
                break;
            case "userdb":
                response = "dummyuserdb";
                break;
            case "dns":
                response = "dummydns";
                break;
            case "password":
                response = "dummypassword";
                break;
            default:
                break;
            }

            return response;
        }

        /// <summary>
        /// Support function for testing.Close all open connection
        /// </summary>
        /// <param name="nesting"></param>
        /// <returns></returns>
        public int closeAll(int nesting) {
            while (nesting > 0) {
                Conn.Close().Wait();
                nesting--;
            }
            return nesting;
        }

        /// <summary>
        /// Mock IDataAccess for test
        /// </summary>
        /// <returns></returns>
        public Mock<IDataAccess> getMockIDataAccess(IDbDescriptor descriptor) {
            //Creazione di un mockIDataAccess per ottenere una mainConnection diversa da null
            var mockIDataAccess = new Mock<IDataAccess>();
            //Setup mock
            mockIDataAccess.Setup(x => x.Open()).Returns(async () => { return true; });
            mockIDataAccess.Setup(x => x.Descriptor).Returns(descriptor);

            return mockIDataAccess;
        }


    }


    [TestFixture]
    class mockDataAccessTest {
        public static DataAccess Conn;
        public static Mock<IDBDriver> mDbDriver;
        public static Mock<IDBDriverDispatcher> mDriverDisp;
        public static Mock<DataAccess> mDA;
        public static Mock<IDbDescriptor> mDB;

        [OneTimeSetUp]
        public static void testInit() {
            //Conn = MockUtils.getAllLocalDataAccess("utente1");
            //mockDA = MockUtils.MockDataAccess(Conn.Descriptor);
            mDbDriver = new Mock<IDBDriver>();

            mDriverDisp = new Mock<IDBDriverDispatcher>();
            mDriverDisp.Setup(x => x.GetConnection()).Returns(
                () => {
                    Console.WriteLine(mDbDriver.Object.ToString());
                    return mDbDriver.Object;
                }
            );

            mDB  = new Mock<IDbDescriptor>();
            mDB.SetupGet(x => x.Dispatcher).Returns(() => mDriverDisp.Object );

            mDA = new Mock<DataAccess>(mDB.Object);
            Conn = mDA.Object;
            //QHS = Conn.GetQueryHelper();
        }
        [OneTimeTearDown]
        public static void testEnd() {
            //Conn?.Destroy().Wait();
        }

        public void setMockSecurity(Mock<ISecurity> sec, string name = null, string response = null) {
            //Così facendo mi salvo i setup del mockSecurity
            if (name == null || response == null) return;
            sec.Setup(x => x.GetSys(name)).Returns(response);
        }

        [Test]
        public void mDataAccess_GetSys() {
            #region ISecurity init
            //Non l'ho messo nell'Init perchè non tutti i test lo richiedono
            var sec = new Mock<ISecurity>();
            mDA.SetupGet( D => D.Security).Returns(()=>sec.Object);
            //mDA.Object.Security = sec.Object;
            #endregion

            setMockSecurity(sec, "database", "unina");
            Assert.AreEqual("unina", Conn.Security.GetSys("database"), "GetSys with usual key is ok");

            setMockSecurity(sec, "ajeje");
            Assert.IsNull(Conn.Security.GetSys("ajeje"), "GetSys with unusual key is ok");

            Assert.AreNotEqual("2018", Conn.Security.GetSys("database"), "GetSys with wrong response is ok");
        }

        [Test]
        public void mDataAccess_Open() {
            mDA.Setup(x => x.Open()).Returns((async ()=>{
	            return true;
            }));
            var boolResult = Conn.Open().GetAwaiter().GetResult();
            Assert.IsTrue(boolResult, "test open connection with mock is ok");
            //Il test Close() non lo implemento in quanto torna un void e non potrei metterlo
            //in un assert e nemmeno fare il setup.
        }

        [Test]
        public void mDataAccess_RUN_SELECT() {
            //RISULTATO ATTESO
            var t1 = new DataTable("config");
            //set del mock
            mDA.Setup(x => x.Select("config", It.IsAny<object>(),
	            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
	            It.IsAny<DataTable>(), It.IsAny<int>()))
	            .Returns(async ()=> {
		            return t1;
	            });

            var a = Conn.Select("config").GetAwaiter().GetResult();
            Assert.AreEqual(t1, a, "RUN SELECT table named config");

            a = Conn.Select("config", "*", "test", "test1", "test1").GetAwaiter().GetResult();
            Assert.AreEqual(t1, a, "RUN SELECT table named config with more parameters not null");

            Assert.AreEqual(null, Conn.Select("registry").GetAwaiter().GetResult(), "RUN SELECT with inesistent table");
        }

        [Test]
        public void mDataAccess_DoDelete() {
            //Non metto alcun return in quanto se tutto è andato a buon fine fa return null
            mDA.Setup(x => x.DoDelete(It.IsAny<string>(), It.IsAny<object>()));

            var temp = Conn.DoDelete("dummyTable", "dummyCondition").GetAwaiter().GetResult();
            Assert.AreEqual(0, temp, "DoDelete with table and condition");
        }

        [Test]
        public void mDataAccess_Commit() {
            //Non metto alcun return in quanto se tutto è andato a buon fine fa return null
            mDA.Setup(x => x.Commit());

            Assert.DoesNotThrow(()=>Conn.Commit().GetAwaiter().GetResult() , "Commit mock is ok");
        }

        [Test]
        public void mDataAccess_DoInsert() {
            //Non metto alcun return in quanto se tutto è andato a buon fine fa return null
            mDA.Setup(x => x.DoInsert(It.IsAny<string>(), It.IsAny<List<string>>(),It.IsAny<List<object>>()));

            Assert.DoesNotThrow(()=> {
                 Conn.DoInsert("dummyTable", new List<string>() { "a", "b" }, new List<object>() { "1", "2" }).GetAwaiter().GetResult();
                }
                , "DoInsert with table");
        }

        [Test]
        public void mDataAccess_CreateTableByName() {
			_ = mDA.Setup(x => x.CreateTable(It.IsAny<string>(),
				  It.IsAny<string>(), It.IsAny<bool>()))
				.Returns(async (string name, string nameList, bool addExtProp) => { return new DataTable(name); });

            var t = new DataTable("dummyTable");
            var temp = Conn.CreateTable("dummyTable", "dummyNameList").GetAwaiter().GetResult();
            Assert.AreEqual(t.TableName, temp.TableName);
            Assert.AreEqual(t.GetType(), temp.GetType());

            //var t = new DataTable("dummyTable");
            //mDA.Setup(x => x.CreateTableByName(It.IsAny<string>(), It.IsAny<string>())).Returns(t);
            //var temp = Conn.CreateTableByName("dummyTable", "dummyNameList");
            //Assert.AreEqual(t, temp);
        }

        [Test]
        public void mDataAccess_count() {
            //mDA.Setup(x => x.count(It.IsAny<string>(), It.IsAny<q>()));
            //var temp = Conn.count()
        }
        [Test]
        public void connectionTest() {
            #region MyRegion

            //Da testare ancora
            //-----------------
            #endregion
        }

        //[Test]
        //public void mDataAccess_ChangeDataBase() {
        //    //Commento il test in quanto il metodo ritorna un void, quindi non c'è modo di testarlo
        //    Conn.ChangeDataBase("rettorato_ok");
        //}

        //[Test]
        //public void mDataAccess_CreateDataAccess() {
        //    //Commento il test in quanto il metodo ritorna un void, quindi non c'è modo di testarlo
        //    mDA.Setup(x => x.CreateDataAccess(It.IsAny<bool>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<int>(),
        //        It.IsAny<DateTime>()));

        //    Conn.createDataAccess(false, "dsn", "dummyServer", "dummyServer", "dummyDatabase", DateTime.Now.Year, DateTime.Now.Date);
        //}

        //[Test]
        //public void mDataAccess_createDataAccess() {
        //    //Commento il test in quanto il metodo ritorna un void, quindi non c'è modo di testarlo
        //    mDA.Setup(x => x.createDataAccess(It.IsAny<bool>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<int>(),
        //        It.IsAny<DateTime>()));
        //    mDA.Setup(x => x.createDataAccess(
        //        It.IsAny<bool>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<string>(),
        //        It.IsAny<int>(),
        //        It.IsAny<DateTime>()));

        //    Conn.createDataAccess(false, "dsn", "dummyServer", "dummyDataBase", "user", "password", DateTime.Now.Year, DateTime.Now.Date);
        //    Conn.createDataAccess(false, "dsn", "dummyServer", "dummyDataBase", "userDB", "passwordDB","user","password", DateTime.Now.Year, DateTime.Now.Date);
        //}
    }
}
