using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mdl;
using Moq;
using Moq.Protected;
using System.Configuration;

namespace TestNetCore {

    public class MockDataAccessHelper {
        public DataAccess Conn;
        public Mock<DataAccess> Mock;

        public MockDataAccessHelper(DataAccess conn) {
            Conn = conn;
            Mock = Conn.getMock();
        }

        public MockDataAccessHelper(string dsn="test") {
            var cfg = MockUtils.getDbParameters(dsn);
            SqlServerDriverDispatcher sd = new SqlServerDriverDispatcher(
                Server:cfg["server"], Database:cfg["database"],
                UserDB:cfg["userdb"], PasswordDB:cfg["passworddb"]);
            DbDescriptor descr=  new DbDescriptor(sd,dsn);
          Conn = new DataAccess(descr);  
        }
    }
    public static class MockUtils {

        public static DataAccess getAllLocalDataAccess(string dsn) {
            var cfg = MockUtils.getDbParameters(dsn);
            SqlServerDriverDispatcher sd = new SqlServerDriverDispatcher(
              Server: cfg["server"], Database: cfg["database"],
              UserDB: cfg["userdb"], PasswordDB: cfg["passworddb"]);
            DbDescriptor descr = new DbDescriptor(sd, dsn);
            return new AllLocal_DataAccess(descr);                                        
        }

     
        public static DataAccess getDataAccess(string dsn) {
            var cfg = MockUtils.getDbParameters(dsn);
            SqlServerDriverDispatcher sd = new SqlServerDriverDispatcher(
             Server: cfg["server"], Database: cfg["database"],
             UserDB: cfg["userdb"], PasswordDB: cfg["passworddb"]);
            DbDescriptor descr = new DbDescriptor(sd, dsn);
            return new DataAccess(descr);
        }

        /// <summary>
        /// Function utilizzata per accedere al file di configurazione della DLL 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="key"></param>
        /// <returns>Ritorna la chiave richiesta nel filie di configurazione</returns>
        static string getAppSetting(Configuration config, string key) {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            string value = element?.Value;
            if (!string.IsNullOrEmpty(value))
                return value;
            return string.Empty;
        }

        public static Dictionary<string, string> getDbParameters(string dsn) {


            string exeConfigPath =
                    typeof(MockUtils).Assembly.Location; // Recupera il percorso del file di configurazione

            Configuration config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);

            // Recupera chiave richiesta dal file config
            string tmpDsn = getAppSetting(config, dsn);

            // Verifica se la chiave esiste
            if (tmpDsn == string.Empty) {
                return null;
            }                         

            // Split della chiave su config
            string[] parametri = tmpDsn.Split(';');
            Dictionary<string, string> res = new Dictionary<string, string> {
                ["dsn"] = parametri[0],
                ["server"] = parametri[1],
                ["database"] = parametri[2],
                ["userdb"] = parametri[3],
                ["passworddb"] = parametri[4],
                ["user"] = parametri[3],
                ["password"] = parametri[4]                
            };
            return res;
        }

        public static Mock<DataAccess> MockDataAccess(
                IDbDescriptor descriptor                
                ) {
            var dMock = new Mock<DataAccess>(MockBehavior.Strict, descriptor);
            var t = new DataTable("config");
            // Invalid setup on a non-virtual (overridable in VB) member
            dMock.Setup(x => x.Select("config", null, "*", null, null, null,-1)).Returns(async ()=>t);

            //dMock.Setup(x => x.Reset());
            //dMock.Protected().Setup("createDataAccess", ItExpr.IsAny<IDbDescriptor>());

            //dMock.Setup(x => x.CreateDataAccess(true,"dsn", "dummyServer", "dummyDataBase", 
            //        DateTime.Now.Year, DateTime.Now.Date));

            return dMock;
        }

        public static Mock<DataAccess> getMock(this DataAccess conn) {
            return MockDataAccess(conn.Descriptor);
        }
    }
  
  
}
