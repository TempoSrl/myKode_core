using System;
using System.Data;
using System.Collections;
using System.Diagnostics;
using q = mdl.MetaExpression;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace mdl
{
    
	/// <summary>
	/// Summary description for dbanalyzer.
	/// </summary>
	public class DBAnalyzer
	{
		

	



		/// <summary>
		/// Export Data from a DB table to an XML file. The XML also contains 
		/// extended informations that allow re-creating the table into another DB
		/// </summary>
		/// <param name="filename">XML filename to create</param>
		/// <param name="tablename"></param>
		/// <param name="conn"></param>
		/// <param name="filter"></param>
		/// <returns>true if OK</returns>
		public static async Task<bool> ExportTableToXML(string filename, string tablename,
				DataAccess conn, object filter) {
			DataSet ds = new DataSet();
			DataTable T = await conn.CreateTable(tablename,null);
			await DataAccess.addExtendedProperty(conn,T);
			ds.Tables.Add(T);

            //reads table
            await conn.SelectIntoTable(T,filter:filter);

			try {
				ds.WriteXml(filename, XmlWriteMode.WriteSchema);
			}
			catch(Exception e) {
				MetaFactory.factory.getSingleton<IMessageShower>().Show($"Couldn't write to file {filename} - {e}","ExportTableToXML");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Export an entire DataSet to XML file. The XML (on request) also contains 
		/// extended informations that allow re-creating the table into another DB
		/// </summary>
		/// <param name="filename">XML file name to be created</param>
		/// <param name="conn"></param>
		/// <param name="ds">DataSet to Export</param>
		/// <param name="addExtedendProperties">when true, extended information is added 
		///  to the DataSet in order to allo all tables to be re-generated in the
		///  target DB</param>
		/// <returns></returns>
		public static async Task<bool> ExportDataSetToXML(string filename, DataAccess conn, 
			DataSet ds, bool addExtedendProperties){			
			DataSet myDS= ds.Copy();
			myDS.copyIndexFrom(ds);
			if (addExtedendProperties){
				foreach(DataTable T in myDS.Tables) await DataAccess.addExtendedProperty(conn,T);
			}
			try {
				myDS.WriteXml(filename, XmlWriteMode.WriteSchema);
			}
			catch(Exception E) {
			    MetaFactory.factory.getSingleton<IMessageShower>().Show(null, $"Couldn't write to file {filename} - {E}", "ExportDataSetToXML");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Reads a DataSet from an XML file and returns it
		/// </summary>
		/// <param name="filename">XML filename to read</param>
		/// <param name="DS">returned DataSet (empty on errors)</param>
		/// <returns>true if Ok</returns>
		public static bool ImportDataSetFromXML(string filename, 
					out DataSet DS){
			DS = new DataSet();
			try {
				DS.ReadXml(filename, XmlReadMode.ReadSchema);
			}
			catch(Exception E) {
			    MetaFactory.factory.getSingleton<IMessageShower>().Show(null,
                    $"Couldn't read from file {filename} - {E}", "ImportDataSetFromXML");
				return false;
			}
			return true;
		}
	

		/// <summary>
		/// Clear a DataBase Table with an unconditioned DELETE from tablename
		/// </summary>
		/// <param name="tablename"></param>
		/// <param name="Conn"></param>
		/// <returns>true when successfull</returns>
		public static async Task<bool> ClearTable (string tablename, DataAccess Conn){
			//Cancella tutte le righe della tabella del DB 
			try {
				await Conn.DoDelete(tablename,null);
			}
			catch {
				return false;
			}
			return true;
		}


		/// <summary>
		/// Reads data from a XML table into a DataSet 
		/// </summary>
		/// <param name="filename">Esempio C:\\cartella\\nomefile.xml</param>
		/// <param name="DS"></param>
		/// <returns></returns>
		public static bool ImportTableFromXML (string filename, out DataSet DS){
			DS = new DataSet();
			try {	
				DS.ReadXml(filename,XmlReadMode.ReadSchema);
			}
			catch(Exception E) {			    
			    MetaFactory.factory.getSingleton<IMessageShower>().Show(null,
			        $"Impossibile leggere il file {filename} - {E}","ImportTableFromXML");
				return false;
			}
			return true;			
		}//Fine ImportTableFromXML


		/// <summary>
		/// Write a DataTable into a DB table with the same name. If the db table
		///  does not exist or miss some columns, it is created or columns
		///  are added to dbtable
		/// </summary>
		/// <param name="T">Table to store in the DB</param>
		/// <param name="Conn"></param>
		/// <param name="Clear">when true, table is cleared before being written</param>
		/// <param name="Replace">when true, rows with matching keys are update. When false,
		///  those are skipped</param>
		/// <param name="filter">condition to apply on data in DataTable</param>
		/// <returns>true if OK</returns>
		public static async Task<bool> WriteDataTableToDB(DataTable T, DataAccess Conn, 
			bool Clear, bool Replace, string filter){
			DataSet Existent = new DataSet();
			DataTable ExistentTable = T.Clone();
			Existent.Tables.Add(ExistentTable);
//			GetData get2= new GetData();
//			get2.InitClass(Existent, Conn, ExistentTable.TableName);

			if (!await CheckDbTableStructure(T.TableName, T, Conn)) {
				return false;
			}

            if (Clear) {
                await ClearTable(T.TableName, Conn);
            }
			
			//ArrayList MyArraySkipRows = new ArrayList();
			
			//Takes all rows from T that satisfy the filter condition
			DataRow []Filtered = T.Select(filter);
			
			//Per ogni riga della tabella T
			foreach(DataRow DR in Filtered) {


                if (!Clear) {
	                var qhs = Conn.GetQueryHelper();
                    //Controlla se è gia presente nella tabella del DB una riga con chiave uguale
                    var WhereKey = QueryCreator.FilterKey(DR, DataRowVersion.Default,forPosting: true);
                    int count = await Conn.Count(T.TableName, filter:WhereKey);


                    //if a row exists, update it
                    if (count > 0) {
                        if (!Replace) continue;
                        await Conn.SelectIntoTable(ExistentTable, filter: WhereKey);
                        DataRow Curr = ExistentTable.filter(WhereKey)[0];
                        foreach (DataColumn C in ExistentTable.Columns) {
                            Curr[C.ColumnName] = DR[C.ColumnName];
                        }
                        continue;
                    }
                }
			
				//insert new row
				DataRow newR= ExistentTable.NewRow();
				foreach (DataColumn C in ExistentTable.Columns){
					newR [C.ColumnName]= DR[C.ColumnName];
				}
				ExistentTable.Rows.Add(newR);
			}
			PostData post = new PostData();
			await post.InitClass(Existent, Conn);
			var saveResult  = await post.SaveData();
			return saveResult.Count == 0;

		}

		/// <summary>
		/// Apply the structure of DS tables to the DB (create tables, adds columns)
		/// This function does not delete columns or rows. No data is written to db.
		/// Only DB schema is (eventually) modified.
		/// </summary>
		/// <param name="DS"></param>
		/// <param name="Conn"></param>
		/// <returns></returns>
		public static async Task<bool> ApplyStructureToDB(DataSet DS, DataAccess Conn){
			if(DS == null)return false;
			foreach (DataTable T in DS.Tables){
				if (!await CheckDbTableStructure(T.TableName, T, Conn))return false;
			}
			return true;
		}

		/// <summary>
		/// Writes all data in DS into corresponding DB tables. If some DB table
		///  does not exist or misses come columns, it is created or columns are
		///  added to it.
		/// </summary>
		/// <param name="DS"></param>
		/// <param name="Conn"></param>
		/// <param name="clear"></param>
		/// <param name="Replace"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public static async Task<bool> WriteDataSetToDB(DataSet DS, DataAccess Conn, 
			bool clear, bool Replace, string filter){

			if(DS == null)return false;
			foreach (DataTable T in DS.Tables){
				bool res = await WriteDataTableToDB(T, Conn, clear, Replace, filter);
				if (!res) return false;
			}
			return true;
		}


		/// <summary>
		/// Check that tablename exists on DB and that has all necessary fields.
		/// If it does not exist, it is created. If it lacks some fields,
		///  those are added to table
		/// </summary>
		/// <param name="tablename">Name of table to be checked fo existence</param>
		/// <param name="T">Table (extended with schema info)  to check</param>
		/// <param name="Conn"></param>
		/// <returns>true when successfull</returns>
		public static async Task<bool> CheckDbTableStructure(string tablename, DataTable T, DataAccess Conn){
			if (! await TableExists(tablename, Conn))	return await CreateTableLike(tablename, T, Conn);

			//Reads table structure through MShelpcolumns Database call
//			string CmdText2 = "exec sp_MShelpcolumns N'[dbo].[" + tablename + "]'";
//			DataTable ExistingColumns = Conn.SQLRunner(CmdText2);			
			//columns returned are:
			//col_name, col_len, col_prec, col_scale, col_basetypename, col_defname,
			// col_rulname, col_null, col_identity, col_flags, col_seed,
			// col_increment col_dridefname, text, col_iscomputed text, col_NotForRepl,
			// col_fulltext, col_AnsiPad, col_DOwner, col_DName, 
			// col_ROwner, col_RName


			ArrayList ToAdd= new ArrayList();
			foreach(DataColumn C in T.Columns){
				if (await ColumnExists(tablename, C.ColumnName, Conn)) {
					//CheckColumnType(tablename, C.ColumnName,Conn, ExistingColumns);
					continue;
				}
				ToAdd.Add(C.ColumnName);
			}
			if (ToAdd.Count==0) return true;
			string [,] cols = new string[ToAdd.Count,3];
			for (int i=0; i< ToAdd.Count; i++){
				string ColToAddName= ToAdd[i].ToString();
				cols[i,0] = T.Columns[ColToAddName].ExtendedProperties["field"].ToString();
				cols[i,1] = T.Columns[ColToAddName].ExtendedProperties["sqldeclaration"].ToString();
				if (T.Columns[ColToAddName].ExtendedProperties["allownull"].ToString().ToUpper()=="S")	
					cols[i,2]= "NULL";
				else
					cols[i,2]= "NOT NULL";				
			}
			return await  AddColumns(tablename, cols, Conn);
		}

		//void CheckColumnType(string tablename, 
		//				string columnname, 
		//				DataAccess Conn,
		//				DataTable ExistingColumns){

		//}

		/// <summary>
		/// Create a DB table like a given DataTable
		/// </summary>
		/// <param name="tablename">name of table to create</param>
		/// <param name="T">DataTable with DataColumn-extended info about DB schema</param>
		/// <param name="Conn"></param>
		/// <returns>true when successfull</returns>
		public static async Task<bool> CreateTableLike(string tablename, DataTable T, DataAccess Conn){
			int colcount = T.Columns.Count;
			string [,]cols = new string[colcount, 3];
			for (int i=0; i< colcount; i++){
				cols[i,0] = T.Columns[i].ExtendedProperties["field"].ToString();
				cols[i,1] = T.Columns[i].ExtendedProperties["sqldeclaration"].ToString();
				if (T.Columns[i].ExtendedProperties["allownull"].ToString().ToUpper()=="S")	
					cols[i,2]= "NULL";
				else
					cols[i,2]= "NOT NULL";				
			}
			string [] key= new string[T.PrimaryKey.Length];
			for (int i=0; i< key.Length; i++) key[i] = T.PrimaryKey[i].ColumnName;
			return await CreateTable(tablename, cols,key, Conn);
		}

		/// <summary>
		/// Verify the existence of a Table
		/// </summary>
		/// <param name="TableName">Name of the Table</param>
		/// <param name="Conn"></param>
		/// <returns>True when Table exists </returns>
		public static async Task<bool> TableExists(string TableName, DataAccess Conn) {
			string Sql= $"select count(*) from [dbo].[sysobjects] where id = object_id(N'[{TableName}]') and OBJECTPROPERTY(id, N'IsUserTable') = 1";
			int N = Convert.ToInt32(await Conn.ExecuteScalar(Sql));
			if(N  == 0) {
				return false;
			}
			else {
				return true;
			}
		}

		/// <summary>
		/// Verify the existence of a column in a DB table. Makes use of db system tables.
		/// </summary>
		/// <param name="TableName"></param>
		/// <param name="ColumnName"></param>
		/// <param name="Conn"></param>
		/// <returns></returns>
		public static async Task<bool>  ColumnExists(string TableName, string ColumnName, DataAccess Conn) {
			string MyCmd=
                $"select count(*) from [dbo].[sysobjects] as T join [dbo].[syscolumns] as C on C.ID = T.ID where T.name='{TableName}' and C.name='{ColumnName}'";
			int N =  Convert.ToInt32(await Conn.ExecuteScalar(MyCmd));
			if(N  == 0) {
				return false;
			}
			else {
				return true;
			}
		}

		

		/// <summary>
		/// Build and Execute an SqlCommand that creates the Table 
		/// </summary>
		/// <param name="TableName">Name of the Table</param>
		/// <param name="Column">Array containing schema of the table (columnname, 
		///		type, [NOT] NULL</param>
		/// <param name="PKey">Array containg the Primary Key of the Table</param>
		/// <param name="Conn"></param>
		/// <returns></returns>
		public static async Task<bool> CreateTable(string TableName, string[,] Column, string[] PKey, DataAccess Conn) {
			int i;
			string command;
			command = "CREATE TABLE " + TableName + " (\r";
			int ncol= Column.GetLength(0);
			for(i = 0; i < ncol; i++) {
				command += Column[i,0] + " " + Column[i,1] + " " +  Column[i,2];
				if ((i+1<ncol)||(PKey.Length>0))command += ",\r";
			}
			if (PKey.Length>0){
				command += " CONSTRAINT xpk" + TableName + " PRIMARY KEY (";
				for(i = 0; i < PKey.Length; i++) {
					command+= PKey[i];
					if (i+1<PKey.Length)command += ",";
					command +="\r";
				}
				command += ")\r";
			}
			command +=  ")";

			//MetaFactory.factory.getSingleton<IMessageShower>().Show(command);
			try {
				var o = await Conn.ExecuteNonQuery(command);
				return true;
			}
			catch (Exception e){
				MetaFactory.factory.getSingleton<IMessageShower>().Show(null, e.ToString(), "Errore");
				return false;
			}
			finally {
			}			
		}

		
		/// <summary>
		/// Add columns to a DB table
		/// </summary>
		/// <param name="TableName">name of table to which add columns</param>
		/// <param name="Column">array of columns to add (fieldname, sqltype)</param>
		/// <param name="Conn"></param>
		/// <returns>true when successfull</returns>
		public static async Task<bool> AddColumns(string TableName, string[,] Column, DataAccess Conn) {
			int i;
			string command;
			command = "ALTER TABLE " + TableName + " ADD \r";
			for(i = 0; i < Column.GetLength(0); i++) {
				if (i>0) command += " , ";
				command += Column[i,0] + " " + Column[i,1] + " " +  Column[i,2] + "\r";
			}
		    MetaFactory.factory.getSingleton<IMessageShower>().Show(null,command,"Informazione");

			try {
				var o = await Conn.ExecuteScalar(command);
				return true;
			}
			catch(Exception e) {
				MetaFactory.factory.getSingleton<IMessageShower>().Show(null, e.ToString(), "Errore");
				return false;
			}
			finally {

			}
			
		}

		

		


		/// <summary>
		/// Saves a table structure to DB (customobject, columntypes..)
		/// </summary>
		/// <param name="DBS"></param>
		/// <returns></returns>
		public virtual async Task<bool> SaveStructure(dbstructure dbs, IDataAccess conn) { //MUST BECOME PROTECTED						

			dbs.RemoveFalseUpdates();
			if (!dbs.HasChanges()) {
				return true;
			}
			PostData post = new PostData();
			await post.InitClass(dbs, conn);
			ProcedureMessageCollection MC = null;
			while ((MC == null) ||
				((MC != null) && MC.CanIgnore && (MC.Count > 0))) {
				MC = await post.SaveData();
			}
			if (MC.Count == 0)
				return true;
			return false;
		}


		/// <summary>
		/// Saves all changes made to all dbstructures
		/// </summary>
		/// <returns></returns>
		public virtual async Task<bool> SaveStructures(IDataAccess conn) {
			foreach (var dbs in conn.Descriptor.GetStructures().Values) {
				var res = await SaveStructure(dbs,conn);
				if (!res) return false;
			}
			return true;
		}


		



		





	
	}
}
