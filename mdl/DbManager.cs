using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using q = mdl.MetaExpression;
using System.Data;
using System.Linq;
using System.Data.Common;
using static mdl_utils.MetaProfiler;

namespace mdl {
    public interface IDbManager {
        void createDescriptor(string dbCode, IDbDriverDispatcher driverDispatcher);
        IDbDescriptor getDescriptor(string dbCode);
    }

    public interface IDbDescriptor {
        string dbCode { get; }
        /// <summary>
        /// If true, customobject and column types are used to describe table structure, 
        ///  when false, those are always obtained from DB at runtime  when they are first used 
        /// </summary>
        bool UseCustomObject { get; set;}

        /// <summary>
        ///  True if all listtype and db properties are contained in system tables so customtablestructure is used
        /// </summary>
		bool ManagedByDB { get; set; }

        /// <summary>
        /// Evaluates table structure again
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        Task<dbstructure> DetectStructure(string tableName, IDataAccess conn);

        /// <summary>
        /// Gets DB structure related to table objectname. The dbstructure returned
        ///  is the same used for framework operations (it is not a copy of it)
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<dbstructure> GetStructure(string tableName, IDataAccess conn);

        /// <summary>
		/// Gets all dbstructure stored
		/// </summary>
		/// <returns></returns>
        Dictionary<string, dbstructure> GetStructures();

        IDbDriverDispatcher Dispatcher { get; }

        /// <summary>
        /// Reads table structure of a list of tables (Has only effect if UseCustomObject is true)
        /// </summary>
        /// <param name="tableName"></param>
        Task ReadStructures(IDataAccess conn, params string[] tableNames);       

        /// <summary>
        /// When false table is not cached in the initialization for a given table
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="tablename"></param>
        /// <returns></returns>
        bool IsToRead(dbstructure DBS, string tablename);

        ///// <summary>
        ///// Clear table informations about a table
        ///// This has not been tested like AutoDetectTable
        ///// </summary>
        ///// <param name="tableName"></param>
        ///// <returns></returns>
        //Task<dbstructure> CalculateTableStructure(string tableName);
        void Reset(string tableName = null);
    }

    



    public class DbManager :IDbManager {
        public static DbManager instance = new DbManager(); 
        Dictionary<string, IDbDescriptor> descriptors = new Dictionary<string, IDbDescriptor>();
        
        public virtual void createDescriptor(string dbCode, IDbDriverDispatcher dispatcher) {
            descriptors[dbCode] = new DbDescriptor(dispatcher, dbCode);
        }

        public virtual IDbDescriptor getDescriptor(string dbCode) {
            return descriptors[dbCode];
        }

    }


    public class DbDescriptor :IDbDescriptor {
        /// <summary>
        /// the dbstructure dataset  - one dataset for each objectname
        /// </summary>
        private Dictionary<string, dbstructure> structures = new Dictionary<string, dbstructure>();

        /// <summary>
        /// Gets all dbstructure stored
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, dbstructure> GetStructures() {
            return structures;
		}

        private readonly object structureLock = new object();
        public string dbCode { get; }

        /// <summary>
        /// If true, customobject and column types are used to describe table structure, 
        ///  when false, those are always obtained from DB
        /// </summary>
        public bool UseCustomObject { get;  set; } = true;

        /// <summary>
        ///  True if all listtype and db properties are contained in system tables
        /// </summary>
        public bool ManagedByDB { get;  set; }  = false;


        public IDbDriverDispatcher Dispatcher { get; set; }

        public DbDescriptor(IDbDriverDispatcher driverDispatcher, string dbCode) {
            this.Dispatcher = driverDispatcher;
            this.dbCode = dbCode;
            Reset();
		}


        public virtual async Task<dbstructure> DetectStructure(string tableName, IDataAccess conn) {
            bool toAdd=false;
            if (!structures.TryGetValue(tableName, out var DS)) {
                DS = new dbstructure();
                ClearDataSet.RemoveConstraints(DS);
                toAdd = true;
            };

            await conn.Driver.AutoDetectTable(DS, tableName); //evaluates customobject and column types
            DS.ExtendedProperties["mdl_changed"] = true;

            if (toAdd) {
                lock (structureLock) {
                    structures[tableName] = DS;
                }
            }
            return DS;
        }


        /// <summary>
        /// Gets DB structure related to table tableName. The dbstructure returned
        ///  is the same used for framework operations (it is not a copy of it)
        /// Evaluates data from db facilities if no data was stored (invoking AutoDetectTable)
        /// Sets DS.ExtendedProperties["mdl_changed"] if it has been autodetected
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual async Task<dbstructure> GetStructure(string tableName, IDataAccess conn) {            
            if (structures.TryGetValue(tableName, out var DS)) {
                return DS;
            };
               

            int handle = StartTimer($"GetStructure*{tableName}");

            DS = new dbstructure();
            ClearDataSet.RemoveConstraints(DS);

            if (UseCustomObject) {                
                await conn.SelectIntoTable(DS.customobject, filter: q.eq("objectname", tableName));
                await conn.SelectIntoTable(DS.columntypes, filter: q.eq("tablename", tableName));
                DS.ExtendedProperties["mdl_changed"] = false;
                //return CalculateTableStructure(objectname);
            }

            //If no customobject, evaluate it 
            if (DS.customobject.Rows.Count == 0) {
                await conn.Driver.AutoDetectTable(DS, tableName); //evaluates customobject and column types
                DS.ExtendedProperties["mdl_changed"] = true;
            }

            if (IsToRead(DS, "customtablestructure"))
                await conn.SelectIntoTable(DS.customtablestructure, filter: q.eq("objectname", tableName));
            lock (structureLock) {
                structures[tableName] = DS;
            }
            StopTimer(handle);
            return DS;
        }

        /// <summary>
        /// When false table is not cached in the initialization for a given table
        /// </summary>
        /// <param name="DBS"></param>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public virtual bool IsToRead(dbstructure DBS, string tablename) { //MUST BECOME PROTECTED
            if (!UseCustomObject) return false;
            if ((!ManagedByDB) || (DBS.Tables[tablename] == null))
                return false;
            return true;
        }

        /// <summary>
        /// Clear table informations about a table
        /// <param name="tablename">name of table to remove from cache, null if cache has to be emptied</param>
        /// </summary>
        public virtual void Reset(string tableName=null) {
            lock (structureLock) {
                if (tableName != null) {
                    if (structures.ContainsKey(tableName)) {
                        structures.Remove(tableName);
                    }
                }
                else {
                    structures.Clear();
                    foreach (string tablename in new string[] { "customobject", "columntypes" }) {
                        var DS = new dbstructure();
                        structures[tablename] = DS;

                        //riempie customobject
                        DataRow R1 = DS.customobject.NewRow();
                        R1["objectname"] = tablename;
                        R1["isreal"] = "S";
                        DS.customobject.Rows.Add(R1);
                        R1.AcceptChanges();

                        //riempie columntypes
                        if (tablename == "customobject")
                            creaDatiSistema.Settacustomobject(DS);
                        if (tablename == "columntypes")
                            creaDatiSistema.Settacolumntypes(DS);
                    }
                }
            }
           
            //QueryCreator.MarkEvent(OUT);

        }

        /// <summary>
        /// Reads table structure of a list of tables from DB (Has only effect if UseCustomObject is true)
        /// </summary>
        /// <param name="tableName"></param>
        public async Task ReadStructures(IDataAccess conn, params string[] tableNames) {
            if (!UseCustomObject) return;
            string[] toread = (from t in tableNames where !structures.ContainsKey(t) select t).ToArray();
            if (toread.Length == 0) return;
            var qh = conn.GetQueryHelper();
            var driver = conn.Driver;

			List<string> listCmd = new List<string> {
				conn.GetSelectCommand("customobject", "objectname, description, isreal, realtable, lastmodtimestamp, lastmoduser",
														   qh.FieldIn("objectname", toread)),
				conn.GetSelectCommand("columntypes",
					"tablename,field,iskey,sqltype,col_len,col_precision,col_scale,systemtype,sqldeclaration,allownull,defaultvalue,format,denynull," +
					"lastmodtimestamp,lastmoduser,createuser,createtimestamp",
														   qh.FieldIn("tablename", toread))
			};

			if (ManagedByDB) {
                listCmd.Add(conn.GetSelectCommand("customtablestructure","*", qh.FieldIn("objectname", toread)));
            }

            var ST = new Dictionary<string, dbstructure>();
            foreach (string tName in toread) {
                var di = new dbstructure();
                ClearDataSet.RemoveConstraints(di);
                ST[tName] = di;
            }

            DbDataReader rdr = null;
            IDbCommand cmd=null;
            try {
                cmd = driver.GetDbCommand( driver.JoinMultipleSelectCommands(listCmd.ToArray()));
                rdr = await conn.Driver.ExecuteReaderAsync(cmd);
                int nSet = 0;
                int countField = rdr.FieldCount;

                while (nSet < listCmd.Count) {
                    if (!rdr.HasRows) {
                        nSet++;
                        await rdr.NextResultAsync();
                        countField = rdr.FieldCount;
                        continue;
                    }
                    while (await rdr.ReadAsync()) {
                        switch (nSet) {
                            case 0:
                                //gets customobject
                                if (!ST.ContainsKey(rdr["objectname"].ToString())) {
                                    ErrorLogger.Logger.MarkEvent("manca tabella " + rdr["objectname"].ToString());
                                    break;
                                }
                                var d = ST[rdr["objectname"].ToString()];
                                var rObj = d.customobject.NewRow();
                                for (int i = countField - 1; i >= 0; i--)
                                    rObj[i] = rdr[i];
                                d.customobject.Rows.Add(rObj);
                                break;
                            case 1:
                                //gets columntypes
                                var d1 = ST[rdr["tablename"].ToString()];
                                var rCol = d1.columntypes.NewRow();
                                for (int i = countField - 1; i >= 0; i--)
                                    rCol[i] = rdr[i];
                                d1.columntypes.Rows.Add(rCol);
                                break;
                            case 2:
                                var d2 = ST[rdr["objectname"].ToString()];
                                var rTabStr = d2.customtablestructure.NewRow();
                                for (int i = countField - 1; i >= 0; i--)
                                    rTabStr[i] = rdr[i];
                                d2.customtablestructure.Rows.Add(rTabStr);
                                break;
                        }
                    }

                    await rdr.NextResultAsync();
                    countField = rdr.FieldCount;
                    nSet++;
                }

            }
            catch (Exception e) {
                ErrorLogger.Logger.markException(e, "ReadStructures");
            }
            finally {
                await driver.DisposeCommand(cmd);
                rdr?.Dispose();
            }
            foreach (string tName in toread) {
                ST[tName].AcceptChanges();
                lock (structureLock) {
                    structures[tName] = ST[tName];
                }
                
            }

        }

     

    }

}
