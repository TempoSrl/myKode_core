using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using q  = mdl.MetaExpression;
using static mdl_utils.MetaProfiler;
using System.Threading.Tasks;

#pragma warning disable IDE1006 // Naming Styles



namespace mdl {
    /// <summary>
    /// Interface for GetData class
    /// </summary>
    public interface IGetData {

	    

        /// <summary>
        /// Dispose all resource
        /// </summary>
        void Destroy();

        /// <summary>
        /// Primary Table Name
        /// </summary>
        string PrimaryTable { get; }

        /// <summary>
        /// Primary Table of the DataSet. Primary Table is the first table scanned 
        ///  when data is read from db.
        /// </summary>
        DataTable PrimaryDataTable { get; }

        /// <summary>
        /// Used ISecurity
        /// </summary>
        ISecurity Security { get; set; }

        /// <summary>
        /// Used MetaFactory
        /// </summary>
        IMetaFactory factory { get; set; }
    


        /// <summary>
        /// Read all tables for which CacheTable() have been invoked, if not already been read
        /// </summary>
        Task ReadCached();

        

        /// <summary>
        /// Initialize class. Necessary before doing any other operation
        /// </summary>
        /// <param name="DS"></param>
        /// <param name="Conn"></param>
        /// <param name="PrimaryTable"></param>
        /// <returns></returns>
        string InitClass(DataSet DS, IDataAccess Conn, string PrimaryTable);

       
        /// <summary>
        /// Clears all tables except for temporary and cached (including pre-filled combobox).
        /// Also undoes the effect of denyclear on all secondary tables setting tables 
        ///  with AllowClear() (ex CLEAR_ENTITY)
        /// </summary>
        void ClearTables();

		#region firstStep
		/// <summary>
		/// Fill the primary table starting with a row equal to Start. Start Row should not
		///  belong to PrimaryTable. Infact PrimaryTable is cleared before getting values from Start fields. 
		/// Child rows of Start are recursively copied to DataSet if there are any (ex START_FROM)
		/// </summary>
		/// <param name="Start"></param>
		Task StartFrom(DataRow Start);

		/// <summary>
		/// Fill the primary table with a row searched from database. R is not required to belong to
		///  PrimaryTable, but should have the same primary key columns. (ex SEARCH_BY_KEY)
		/// </summary>
		/// <param name="R"></param>
		Task SearchByKey(DataRow R);


		/// <summary>
		/// Clears &amp; Fill the primary table with all records from a database table
		/// </summary>
		/// <param name="filter"></param>
		Task GetPrimaryTable(MetaExpression filter);

		/// <summary>
		/// Gets a DataRow from db, a row with the same key
		/// </summary>
		/// <param name="Dest">Table into which putting the row read</param>
		/// <param name="Key">DataRow with the same key as wanted row</param>
		/// <returns>null if row was not found</returns>
		Task<DataRow> GetByKey(DataTable Dest, DataRow Key);

		/// <summary>
		/// Try to get a row from an in-memory view if there is one. This function
		///  is obsolete cause now it's possible to write view table as if they
		///  were real table.
		/// </summary>
		/// <param name="Dest"></param>
		/// <param name="Key"></param>
		/// <returns></returns>
		Task<DataRow> GetFromViewByKey(DataTable Dest, DataRow Key);

      

		#endregion

        /// <summary>
        /// Gets all data of the DataSet cascated-related to the primary table.
        /// The first relations considered are child of primary, then
        ///  proper child / parent relations are called in cascade style.
        /// </summary>
        ///  <param name="onlyperipherals">if true, only peripheral (not primary or secondary) tables are refilled</param>
        ///  <param name="OneRow">The (eventually) only primary table row on which
        ///   get the entire sub-graph. Can be null if PrimaryDataTable 
        ///   already contains rows.  R is not required to belong to PrimaryDataTable.</param>
        /// <returns>always true</returns>
        Task<bool> Get(bool onlyperipherals, DataRow OneRow=null);


       
        /// <summary>
        /// Reads a DataTable with an optional set of Select 
        /// </summary>
        /// <param name="T">DataTable to Get from DataBase</param>
        /// <param name="filter"></param>
        /// <param name="clear">if true table is cleared before reading</param>
        /// <param name="sortBy">parameter to pass to "order by" clause</param>
        /// <param name="top">parameter for "top" clause of select</param>
        /// <param name="selList"></param>
        Task<SelectBuilder> GetTable(DataTable table,
			MetaExpression filter =null,  
	        bool clear=false, 
	        string sortBy=null, 
	        string top=null, 
	        List<SelectBuilder> selList=null);


     	/// <summary>
		/// Gets a DataTable related with PrimaryTable via a given Relation Name.
		/// Also gets columns implied in the relation of related table.
		/// Relation can either be a parent or a child relation
		/// </summary>
		/// <param name="relname"></param>
		/// <param name="Cs">Columns of related table, implied in the relation</param>
		/// <returns>Related table</returns>
		DataTable EntityRelatedByRel(string relname, out DataColumn[] Cs);

		/// <summary>
		/// Get all child rows in allowed child tables
		/// </summary>
		/// <param name="RR"></param>
		/// <param name="allowed"></param>
		/// <param name="SelList"></param>
		Task GetAllChildRows(DataRow[] RR, HashSet<string> allowed, List<SelectBuilder> selList=null);


        /// <summary>
        /// Get parent rows of a given Row, in a set of specified  tables.
        /// </summary>
        /// <param name="R">DataRow whose parents are wanted</param>
        /// <param name="allowed">Tables in which to search parent rows</param>
        /// <param name="selList"></param>
        /// <returns>true if any of parent DataRows was already in memory. This is not 
        ///  granted if rows are taken from a view</returns>
        Task<bool> GetParentRows(DataRow R, HashSet<string> allowed, List<SelectBuilder> selList=null);

        

    }

    /// <summary>
	/// GetData is a class to automatically get all data related to a set of rows
	///  in a primary table, given a DataSet that describes all relations between data
	///  to get. 
	/// When getting data, temporary tables are skipped, and temporary field are
	///   calculated when possible.
	/// GetData is part of the Model Layer
	/// </summary>
	public class GetData : IGetData {

	    /// <summary>
	    /// MetaModel used in this class
	    /// </summary>
	    private IMetaModel model {
		    get { return Conn?.model; }
	    }


	    bool destroyed;
        /// <summary>
        /// Dispose all resource
        /// </summary>
        public void Destroy() {
            if (destroyed) {
                return;
            }
            destroyed = true;
            this.DS = null;
            this.PrimaryDataTable = null;
            initCacheParentView();
            if (preScannedTablesRows != null) {
                preScannedTablesRows.Clear();
                preScannedTablesRows = null;
            }
            if (cachedChildSourceColumn != null) {
                cachedChildSourceColumn.Clear();
                cachedChildSourceColumn = null;
            }
            if (this.VisitedFully != null) {
                this.VisitedFully.Clear();
                VisitedFully = null;
            }            
        }


        /// <summary>
        /// DataSet on which the instance works
        /// </summary>
        private DataSet DS { get; set; }

        /// <summary>
        /// Primary Table Name
        /// </summary>
        public string PrimaryTable { get; private set; }

		/// <summary>
		/// Primary Table of the DataSet. Primary Table is the first table scanned 
		///  when data is read from db.
		/// </summary>
		public DataTable PrimaryDataTable { get; private set; }


		/// <summary>
		/// Connection to DataBase
		/// </summary>
		public IDataAccess Conn;

        /// <summary>
        /// Factory used by the instance
        /// </summary>
        public IMetaFactory factory { get; set; } = MetaFactory.factory;

	    /// <summary>
	    /// 
	    /// </summary>
	    public ISecurity Security {get;set;}


		//bool isLocalToDB;

				/// <summary>
		/// A collection of tables that have been read with a null filter. These are not read 
		///  again.
		/// </summary>
		HashSet<string> VisitedFully;

     

		/// <summary>
		/// Public constructor
		/// </summary>
		public GetData() {
		  
		}


		
		#region Cached Table Ext.Property Management
		
		


		/// <summary>
		/// Read all tables marked as "ToCache" with CacheTable() that haven't yet been read
		/// </summary>
		public async Task ReadCached(){
            int handle = StartTimer("ReadCached()");
            List<SelectBuilder> selList = new List<SelectBuilder>();
			foreach (DataTable T in DS.Tables){
				if (!model.IsCached(T))continue;
				if (!model.CanRead(T)) continue;
				await GetTable(table:T,clear:true,selList:selList);
				model.TableHasBeenRead(T);
			}
            if (selList.Count > 0) {
                await Conn.MultipleSelect(selList);
				foreach (SelectBuilder Sel in selList) {
					model.GetTemporaryValues(Sel.DestTable, Conn.Security);
				}
            }
            StopTimer(handle);
		}
		#endregion



		

		private IIndexManager idm;
	    
		/// <summary>
        /// Initialize class. Necessary before doing any other operation
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="conn"></param>
        /// <param name="primaryTable"></param>
        /// <returns></returns>
        public string InitClass(DataSet ds, IDataAccess conn,  string primaryTable) {
	        this.DS = ds;
	        this.Conn = conn;
            this.Security = conn.Security;
            this.PrimaryTable = primaryTable;
	        this.PrimaryDataTable = ds.Tables[primaryTable];
	        idm = DS.getCreateIndexManager();
	        VisitedFully = new HashSet<string>();
	        return null;
	    }


		/// <summary>
		/// Clears all tables except for temporary and cached (including pre-filled combobox).
		/// Also undoes the effect of denyclear on all secondary tables setting tables 
		///  with AllowClear()
		/// </summary>
		public void ClearTables(){   
			int metaclear = StartTimer("ClearTables");
			foreach (DataTable T in DS.Tables){
				if (MetaModel.IsTemporaryTable(T)) continue;
				if (model.IsCached(T))continue;
				if (!VisitedFully.Contains(T.TableName)) model.Clear(T); // T.Clear();
                model.AllowClear(T);
			}
			StopTimer(metaclear);
		}


        void xCopyChilds(DataSet Dest, DataSet Rif, DataRow RSource) {
            DataTable T = RSource.Table;
            string source_unaliased = RSource.Table.tableForReading();
            if (!Dest.Tables.Contains(source_unaliased)) source_unaliased = RSource.Table.TableName;
            model.CopyDataRow(Dest.Tables[source_unaliased], RSource);
            model.DenyClear(Dest.Tables[source_unaliased]);

            foreach (DataRelation Rel in T.ChildRelations) {
                if (!Dest.Tables.Contains(Rel.ChildTable.TableName)) continue;
                if (!CheckChildRel(Rel)) continue; //not a subentityrel
                DataTable ChildTable = Rif.Tables[Rel.ChildTable.TableName];
                model.DenyClear(Dest.Tables[ChildTable.TableName]);
                foreach (DataRow Child in RSource.getChildRows(Rel)) {
                    xCopyChilds(Dest, Rif, Child);
                }
            }
        }
        
        /// <summary>
        /// Check if a relation connects any field that is primarykey for both parent and child
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        internal static bool CheckChildRel(DataRelation R) {
            //Autorelation are not childrel
            if (R.ParentTable.TableName == R.ChildTable.TableName) return false;

            bool linkparentkey = false;

            for (int N = 0; N < R.ParentColumns.Length; N++) {
                DataColumn ParCol = R.ParentColumns[N];
                DataColumn ChildCol = R.ChildColumns[N];
                if (QueryCreator.IsPrimaryKey(R.ParentTable, ParCol.ColumnName) &&
                     QueryCreator.IsPrimaryKey(R.ChildTable, ChildCol.ColumnName)) linkparentkey = true;
            }
            return linkparentkey;
        }

      

		/// <summary>
		/// Fill the primary table starting with a row equal to Start. Start Row should not
		///  belong to PrimaryTable. Infact PrimaryTable is cleared before getting values from Start fields 
		/// </summary>
		/// <param name="Start"></param>
		public async Task StartFrom(DataRow Start) {
			await ReadCached();
			xCopyChilds(DS, Start.Table.DataSet, Start);
			DataRow R = PrimaryDataTable.Rows[0];
			PrimaryDataTable._setLastSelected(R);
		}


		/// <summary>
		/// Clears &amp; Fill the primary table with all records from a database table
		/// </summary>
		/// <param name="filter"></param>
		public async Task GetPrimaryTable(MetaExpression filter) {
			await ReadCached();
			//inutile poiché GetRowsByFilter efettua merge dei filtri
			//string Filter = MergeFilters(filter, PrimaryDataTable);
			model.Clear(PrimaryDataTable); // PrimaryDataTable.Clear();
			await GetRowsByFilter(filter, table: PrimaryTable);
		}

		/// <summary>
		/// Fill the primary table with a row searched from database. R is not required to belong to
		///  PrimaryTable, but should have the same primary key columns.
		/// </summary>
		/// <param name="R"></param>
		public async Task SearchByKey(DataRow R){
            int handle = StartTimer("SEARCH_BY_KEY");
			await ReadCached();
			//It's necessary to take the filter BEFORE clearing PrimaryTable, cause
			//  R could belong to PrimaryTable!
			var filter = 
				QueryCreator.FilterColumnPairs(R, 
				PrimaryDataTable.PrimaryKey,                
				PrimaryDataTable.PrimaryKey, 
				DataRowVersion.Default,
				forPosting:false);                
			await GetRowsByFilter(filter,table:PrimaryTable);
            StopTimer(handle);
		}

		/// <summary>
		/// Gets a primary table DataRow from db, given its primary key
		/// </summary>
		/// <param name="Dest">Table into which putting the row read</param>
		/// <param name="Key">DataRow with the same key as wanted row</param>
		/// <returns>null if row was not found</returns>
		public async Task<DataRow> GetByKey(DataTable Dest, DataRow R) {	
			DataRow Res = await GetFromViewByKey(Dest,R);
			if (Res==null){
				var filter = 
					QueryCreator.FilterColumnPairs(R,Dest.PrimaryKey,                
					Dest.PrimaryKey, 
					DataRowVersion.Default,
					forPosting:false);                
				await GetRowsByFilter(filter:filter,table:Dest.TableName);//reads from db
				DataRow[] found = Dest.filter(q.mCmp(R, Dest.PrimaryKey));
								//Dest.Select(filter); 
				if (found.Length==0) return null;
				Res= found[0];
			}
			model.GetTemporaryValues(Dest, Conn.Security);
			return Res;
		}

		/// <summary>
		/// Try to get a row from an in-memory view if there is one. This function
		///  is obsolete cause now it's possible to write view table as if they
		///  were real table.
		/// </summary>
		/// <param name="Dest"></param>
		/// <param name="Key"></param>
		/// <returns></returns>
		public async Task<DataRow> GetFromViewByKey(DataTable Dest, DataRow Key){
			DataTable ViewTable = (DataTable) Dest.ExtendedProperties["ViewTable"];
			if (ViewTable==null) return null;
			DataTable TargetTable = (DataTable) ViewTable.ExtendedProperties["RealTable"];
			if (TargetTable!=Dest) return null;

			var dict = new Dictionary<string, object>();
			
			//key columns of TargetTable in ViewTable
			DataColumn [] Vkey = new DataColumn[TargetTable.PrimaryKey.Length];
			//key columns of Dest Table
			DataColumn [] Ckey = TargetTable.PrimaryKey;
			for (int i=0; i<TargetTable.PrimaryKey.Length; i++){
				bool found=false;
				string colname= $"{TargetTable.TableName}.{Ckey[i].ColumnName}";
				//search the column in view corresponding to Rel.ParentCol[i]
				foreach (DataColumn CV in ViewTable.Columns){
					if (CV.ExtendedProperties["ViewSource"].ToString()==colname){
						found=true;
						Vkey[i]= CV;
						dict[CV.ColumnName] = Key[Ckey[i].ColumnName];
						break;
					}
				}
				if (!found) return null; //key columns were not found
			}

			//string viewtablefilterNOSQL = QueryCreator.WHERE_REL_CLAUSE(Key, Ckey, Vkey, DataRowVersion.Default,false);
			//DataRow [] ViewTableRows = ViewTable.Select(viewtablefilterNOSQL);
			DataRow[] ViewTableRows = ViewTable.filter(q.mCmp(dict));
			if (ViewTableRows.Length==0) {
				var viewtablefilter = QueryCreator.FilterColumnPairs(Key, Ckey, Vkey, DataRowVersion.Default, forPosting:false);
				MultiCompare MC = QueryCreator.ParentChildFilter(Key, Ckey, Vkey, DataRowVersion.Default, true);
				await GetRowsByFilter(filter:viewtablefilter,table: ViewTable.TableName, multiComp: MC);
				ViewTableRows = ViewTable.filter(q.mCmp(dict));
				if (ViewTableRows.Length==0) return null;
			}

			DataRow RR = ViewTableRows[0];

			//copy row from view to dest
			DataRow NewR = TargetTable.NewRow();
			foreach (DataColumn CC in TargetTable.Columns){
				string colname= TargetTable.TableName+"."+CC.ColumnName;
				foreach (DataColumn CV in ViewTable.Columns){
					if (CV.ExtendedProperties["ViewSource"]==null)continue;
					if (CV.ExtendedProperties["ViewSource"].ToString()==colname){
						NewR[CC]= RR[CV];
						break;
					}
				}				
			}
			TargetTable.Rows.Add(NewR);
			NewR.AcceptChanges();
			return NewR;
		}

	
		/// <summary>
		/// Merge a filter (Filter1) with the static filter of a DataTable and
		///  gives the resulting (AND) filter
		/// </summary>
		/// <param name="Filter1"></param>
		/// <param name="T"></param>
		/// <returns></returns>
		public static MetaExpression MergeFilters(MetaExpression Filter1, DataTable T){
            if (T == null) return Filter1;
			MetaExpression Filter2=null;
			if (T.ExtendedProperties["filter"]!=null) {
				Filter2 = T.ExtendedProperties["filter"] as MetaExpression;
			}
			return q.and(Filter1, Filter2);
		}


	

        public void recursivelyMarkSubEntityAsVisited(DataTable mainTable, 
	        HashSet<string> visited, HashSet<string> toVisit) {
            foreach (DataRelation Rel in mainTable.ChildRelations){
                string childtable = Rel.ChildTable.TableName;
                if ((!model.IsSubEntityRelation(Rel) &&  model.CanClear(Rel.ChildTable)) 
                    || visited.Contains(childtable)) continue; //if continue--> it will be cleared
                //Those tables will not be cleared
                visited.Add(childtable);			
                toVisit.Add(childtable);
                recursivelyMarkSubEntityAsVisited(Rel.ChildTable, visited, toVisit);
            } 
          
        }

		/// <summary>
		/// Gets all data of the DataSet cascated-related to the primary table.
		/// The first relations considered are child of primary, then
		///  proper child / parent relations are called on cascade.
		/// </summary>
		///  <param name="onlyperipherals">if true, only peripheral (not primary or secondary) tables are refilled</param>
		///  <param name="OneRow">The (eventually) only primary table row on which
		///   get the entire sub-graph. Can be null if PrimaryDataTable 
		///   already contains rows.  R is not required to belong to PrimaryDataTable.</param>
		/// <returns>always true</returns>
		public async Task<bool> Get(bool onlyperipherals=false, DataRow OneRow=null){
			int gethandle = StartTimer("Inside Get()");

            initCacheParentView();
            
            //Tables whose child and tables rows have to be retrieved
            var toVisit= new HashSet<string>();
            //Tables from which rows have NOT to be retrieved
            var visited= new HashSet<string>();

			//Set Fully-Visited and Cached tables as Visited
			foreach (DataTable T in DS.Tables){
				if ((model.IsCached(T))||(VisitedFully.Contains(T.TableName))|| MetaModel.IsTemporaryTable(T)) {
					visited.Add(T.TableName);
					//ToVisit[T.TableName] = T;
				}
			}
            string[] toPreScan = (from DataTable T in DS.Tables where !MetaModel.IsTemporaryTable(T) select T.TableName).ToArray();
            await Conn.ReadStructures(toPreScan);
			toVisit.Add(PrimaryTable);
			visited.Add(PrimaryTable);

			if (onlyperipherals){
				//Marks child tables as ToVisit+Visited
			    recursivelyMarkSubEntityAsVisited(PrimaryDataTable, visited, toVisit);


				foreach (DataTable T in DS.Tables){
					string childtable = T.TableName;
					if (!model.CanClear(T)){  //Skips DenyClear Tables
						visited.Add(childtable);
						toVisit.Add(childtable);
					}
				}
			}
			
			//Clears all other tables
			foreach (DataTable T in DS.Tables){
				if (visited.Contains(T.TableName)) continue;
				if (MetaModel.IsTemporaryTable(T)) continue;
                if(T.ExtendedProperties["RealTable"] is DataTable Main) {
                    if(visited.Contains(Main.TableName)) continue; //tratta le viste come le relative main
                }
				model.Clear(T); // T.Clear();
				T.AcceptChanges();
			}

			//Set as Visited all child tables linked by autoincrement fields
			if ((OneRow!=null) && (OneRow.RowState==DataRowState.Added)){
				foreach(DataRelation Rel in PrimaryDataTable.ChildRelations){
					string childtable = Rel.ChildTable.TableName;
					bool toskip=false;
					foreach(var C in Rel.ParentColumns){
						if (C.IsAutoIncrement()){
							toskip=true;
							break;
						}
					}
					if (toskip) visited.Add(childtable);					
				}
			}


			bool waspersisting= Conn.Persisting;
			Conn.Persisting=true;
			await Conn.Open();

            int h1 = StartTimer("ScanTables");
			await scanTables(toVisit, visited, OneRow);
            StopTimer(h1);

			if (onlyperipherals){
				//Freshes calculated fields of entity tables and Dont't-clear-tables
				foreach (DataRelation Rel in PrimaryDataTable.ChildRelations){
					string childtable = Rel.ChildTable.TableName;
                    if ((!model.IsSubEntityRelation(Rel)) && model.CanClear(Rel.ChildTable)) continue;
                    model.GetTemporaryValues(Rel.ChildTable, Conn.Security);				//
                }

				if (OneRow != null) {
					model.GetTemporaryValues(OneRow, Conn.Security);
				}
				else {
					model.GetTemporaryValues(PrimaryDataTable, Conn.Security);
				}
				
			}
			await Conn.Close();
			Conn.Persisting= waspersisting;
			StopTimer(gethandle);
			return true;  
		}
		
       
		/// <summary>
		/// Gets a DataTable with an optional set of Select. Automatically adds empty row when
		///  needed and considers static filter of the table. 
		/// </summary>
		/// <param name="T">DataTable to Get from DataBase</param>
		/// <param name="filter"></param>
		/// <param name="clear">if true table is cleared before reading</param>
		/// <param name="sortBy">parameter to pass to "order by" clause</param>
		/// <param name="top">parameter for "top" clause of select</param>
        /// <param name="selList"></param>
        async public Task<SelectBuilder> GetTable(DataTable table, 
			MetaExpression filter=null,  
			bool clear=false, 
			string sortBy=null, 
			string top=null, 
			List<SelectBuilder> selList=null) {

			if (!model.CanRead(table))return null;
			
			if (clear) {
				model.Clear(table); // T.Clear();
			}
			model.CheckBlankRow(table);
			var mergedfilter = MergeFilters(filter,table); 
			sortBy ??= table.getSorting();

            SelectBuilder mySel = null;

            if (selList == null) {
                await Conn.SelectIntoTable(table,  filter:mergedfilter, top:top); //sort_by:sortBy,
            }
            else {
                mySel = new SelectBuilder().Where(mergedfilter).Top(top).OrderBy(sortBy).IntoTable(table);
                selList.Add(mySel);
            }

            if (mergedfilter is null || mergedfilter.isTrue()) VisitedFully.Add(table.TableName);
			model.TableHasBeenRead(table);
			if (selList==null) model.GetTemporaryValues(table, Conn.Security);
            return mySel;
		}

		/// <summary>
		/// Set sorting property of a DataTable
		/// </summary>
		/// <param name="T"></param>
		/// <param name="sort"></param>
		public static void SetSorting(DataTable T, string sort){			
			T.ExtendedProperties["sort_by"]=sort;
		}
		


        /// <summary>
        /// Gets child of a selected set of rows, and gets related tables
        /// </summary>
        /// <remarks>TODO: This method will be removed </remarks>
        /// <param name="ToExpand"></param>
        public async Task expandChilds(DataRow[] ToExpand) {
            if (ToExpand.Length == 0) return;
            var T = ToExpand[0].Table;
            var toVisit = new HashSet<string>() { T.TableName};
            await GetAllChildRows(ToExpand, toVisit);

        }


        /// <summary>
        /// Gets all necessary rows from table in order to rebuild R genealogy
        /// </summary>
        /// <param name="R"></param>
        /// <param name="AddChild">when true, all child of every parent found
        ///  are retrieved
        ///  </param>
        public async Task GetParents(DataRow R, bool AddChild) {
            int handle = StartTimer("GetParents");
            try {
                DataRow[] Parents = new DataRow[20];
                Parents[0] = R;
                int found = 1;

                
                var T = R.Table;
                var allowed = new HashSet<string>(){T.TableName};

                var AutoParent = GetAutoParentRelation(T);
                if (AutoParent == null) return;
                bool res;
                //Get the strict genealogy of R (max 20 levels)
                while (found < 20) {
                    //Gets the parent of Parents[found-1];
                    DataRow Child = Parents[found - 1];
                    res = await GetParentRows(Child, allowed, null);

                    //finds parent of Child
                    DataRow[] foundparents = Child.getParentRows(AutoParent);
                    if (foundparents.Length != 1) break;
                    Parents[found] = foundparents[0];
                    found++;
                    if (res) break;
                }
                if (!AddChild) return;
                if (found == 1) return;
                //			if (res) {
                //				found--; //skip last parent, which was already in tree
                //			}
                DataRow[] list = new DataRow[found - 1];
                for (int i = 1; i < found; i++) list[i - 1] = Parents[i];
                await ExpandChilds(list); 
            }
            finally {
                StopTimer(handle);
            }
        }

        //TODO: remove
        /// <summary>
        /// Gets a relation that connects a table with its self
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        private static DataRelation GetAutoParentRelation(DataTable T) {
            foreach (DataRelation R in T.ParentRelations) {
                if (R.ParentTable.TableName == T.TableName) return R;
            }
            return null;
        }


        /// <summary>
        /// Gets child of a selected set of rows, and gets related tables
        /// </summary>
        /// <param name="ToExpand"></param>
        private async Task  ExpandChilds(DataRow[] ToExpand) {
            if(ToExpand.Length == 0)
                return;
            var T = ToExpand[0].Table;
            var ToVisit = new HashSet<string>() {T.TableName};
            await GetAllChildRows(ToExpand, allowed:ToVisit);
          
        }




		/// <summary>
		/// Get all child and parent rows of tables in "ToVisit", assuming that Tables in
		/// "Visited" table's rows have already been retrieved and so must not be 
		/// retrieved again. "Visited" can be considered as a barrier that can't be 
		/// overpassed in the scanning process.
		/// </summary>
		/// <param name="ToVisit">List of tables to scan</param>
		/// <param name="Visited">List of tables that are not to be scanned</param>
		/// <param name="OneRow">when not null, is the only primary table row for 
		///  which are taken child rows.</param>
		async Task scanTables(HashSet<string> toVisit, HashSet<string> visited, DataRow OneRow){
            //EnableCache();
            //DisableCache();

            while (toVisit.Count > 0) {
	            //tables from which retrieve rows in this step
	            var nextVisit = new HashSet<string>();
	            List<SelectBuilder> selList = new List<SelectBuilder>();

	            //Mark tables directly related to "ToVisit" tables as "Visited" +NextVisit, 
	            // so they will not be back-scanned in future iterations
	            foreach (var tableName in toVisit) {
		            var T = DS.Tables[tableName];
		            if (MetaModel.IsTemporaryTable(T)) continue;
		            //searches child tables of T & pre-set them to visited
		            foreach (DataRelation Rel in T.ChildRelations) {
			            string childtable = Rel.ChildTable.TableName;
			            if (visited.Contains(childtable)) continue;
			            if (toVisit.Contains(childtable)) continue;
			            visited.Add(childtable);
			            nextVisit.Add(childtable);
		            }

		            //searches parent tables of T & pre-set them to visited + NextVisit
		            foreach (DataRelation Rel in T.ParentRelations) {
			            string parenttable = Rel.ParentTable.TableName;
			            if (visited.Contains(parenttable)) continue;
			            if (toVisit.Contains(parenttable)) continue;
			            visited.Add(parenttable);
			            nextVisit.Add(parenttable);
		            }
	            }

	            //Only rows in NextVisit tables will be loaded in this step
	            foreach (var tableName in toVisit) {
		            var T = DS.Tables[tableName];
		            if ((OneRow == null) || (OneRow.Table.TableName != T.TableName)) {
			            foreach (DataRow R in T.Rows) {
				            if (R.RowState == DataRowState.Deleted) continue;
				            if (R.RowState == DataRowState.Detached) continue;
				            await GetParentRows(R, nextVisit, selList);
			            }

			            await GetAllChildRows(T.Select(), allowed:nextVisit, SelList: selList);
			            //GetTemporaryValues(T);
			            continue;
		            }

		            //Caso di (OneRow!=null) && (OneRow.Table == T)
		            foreach (DataRow R in T.Rows) {
			            if (R.RowState == DataRowState.Deleted) continue;
			            if (R.RowState == DataRowState.Detached) continue;

			            //If OneRow present, take childs only from OneRow in OneRow.Table
			            //this was below (*)
			            if (OneRow != R) continue;

			            await GetParentRows(R, allowed:nextVisit, selList: selList);

			            // (*) it was here
			            await GetChildRows(R, allowed: nextVisit, selList:selList);
		            }

	            }

	            if (selList.Count > 0) {
		            await Conn.MultipleSelect(selList);
	            }

	            foreach (var tableName in toVisit) {
		            var T = DS.Tables[tableName];
		            if (OneRow != null && T == OneRow.Table) {
			            model.GetTemporaryValues(OneRow, Conn.Security);
		            }
		            else {
			            model.GetTemporaryValues(T, Conn.Security);
		            }
	            }


	            OneRow = null;
	            toVisit = nextVisit;
            }

            //DisableCache();
		}




		









		async Task GetViewChildTable(DataRow R, DataRelation Rel) {
	        int chrono = StartTimer("GetViewChildTable * " + Rel.RelationName );
	        await iGetViewChildTable(R, Rel);
	        StopTimer(chrono);
	    }

		/// <summary>
		/// Gets a table reading it from a view
		/// Here ViewTable.ExtendedProperties["RealTable"]==Rel.ChildTable
		/// </summary>
		/// <param name="R"></param>
		/// <param name="Rel"></param>
		async Task iGetViewChildTable(DataRow R, DataRelation Rel){
			DataTable TargetTable = Rel.ChildTable;
			DataTable ViewTable = (DataTable) TargetTable.ExtendedProperties["ViewTable"];
			if (ViewTable == null) return;

			//search columns in view corresponding to Rel.ChildColumns
			DataColumn [] VCol = new DataColumn[Rel.ChildColumns.Length];
			DataColumn [] TCol = new DataColumn[Rel.ChildColumns.Length];
			for (int i=0; i< Rel.ChildColumns.Length; i++){
				TCol[i] = Rel.ChildColumns[i];
				string colname= TargetTable.TableName+"."+TCol[i].ColumnName;
				foreach (DataColumn CV in ViewTable.Columns){
					if (CV.ExtendedProperties["ViewSource"]==null) continue;
					if (CV.ExtendedProperties["ViewSource"].ToString()==colname){
						VCol[i]= CV;
						break;
					}
				}				
			}

			var searchfilter= QueryCreator.FilterColumnPairs(R, 
				Rel.ParentColumns, 
				VCol,
				DataRowVersion.Default,  forPosting: false);
			await GetTable(table:ViewTable, filter: searchfilter);

			//search columns in view corresponding to Rel.ChildColumns
			
			DataColumn [] VKCol = new DataColumn[TargetTable.PrimaryKey.Length];
			DataColumn [] TKCol = new DataColumn[TargetTable.PrimaryKey.Length];
			for (int i2=0; i2< TargetTable.PrimaryKey.Length; i2++){
				TKCol[i2] = TargetTable.PrimaryKey[i2];
				string colname= TargetTable.TableName+"."+TKCol[i2].ColumnName;
				foreach (DataColumn CV in ViewTable.Columns){
					if (CV.ExtendedProperties["ViewSource"]==null) {continue;}
					if (CV.ExtendedProperties["ViewSource"].ToString()==colname){
						VKCol[i2]= CV;
						break;
					}
				}				
			}
            bool emptyStartTable = TargetTable.Rows.Count == 0;
            model.InvokeActions(TargetTable,TableAction.beginLoad);

			foreach (DataRow RR in ViewTable.Rows){
				var dict = new Dictionary<string, object>();
				for (int i = 0; i < VKCol.Length; i++) {
					dict[TKCol[i].ColumnName] = RR[VKCol[i].ColumnName];
				}
				//string filterKeyChild = QueryCreator.WHERE_REL_CLAUSE(RR,VKCol, TKCol, DataRowVersion.Default,false);
				//if RV already present in TargetTable, continue
			    if (!emptyStartTable) {
			        DataRow[] found2 = TargetTable.filter(q.mCmp(dict)); //Select(filterKeyChild);
			        if (found2.Length > 0) continue;
			    }

			    List<string>skippedColumns = new List<string>();
			    DataRow newR = TargetTable.NewRow();
				foreach (DataColumn CC in TargetTable.Columns){
					string colname= TargetTable.TableName+"."+CC.ColumnName;
				    string k = Rel.RelationName + "|" + colname;
				    if (cachedChildSourceColumn.ContainsKey(k)) {
                        newR[CC] = RR[cachedChildSourceColumn[k]];
                    }
				    else {
					    if (skippedColumns.Contains(colname)) continue;
				        bool colFound = false;
				        foreach (DataColumn CV in ViewTable.Columns) {
				            if (CV.ExtendedProperties["ViewSource"] == null) {
				                continue;
				            }
				            if (CV.ExtendedProperties["ViewSource"].ToString() == colname) {
				                newR[CC] = RR[CV];
                                cachedChildSourceColumn[k] = CV;
				                colFound = true;
                                break;
				            }
				        }

				        string expr = CC.GetExpression();
				        if (expr != null) {
					        var parts = expr.Split('.');
					        if (parts.Length == 2) {
						        string exprTable = parts[0], exprCol = parts[1];
						        foreach (DataColumn CV in ViewTable.Columns) {
							        if (CV.ExtendedProperties["ViewSource"] as string !=  expr) {
								        continue;
							        }
							        newR[CC] = RR[CV];
							        cachedChildSourceColumn[k] = CV;
							        CC.ExtendedProperties["mdl_foundInGetViewChildTable"] = "S";
							        colFound = true;
							       
						        }
					        }
				        }
				        if (!colFound) {
					        skippedColumns.Add(CC.ColumnName);
				        }
				    }
				}

			    try {
			        TargetTable.Rows.Add(newR);
			    }
			    catch (Exception e) {
					var qhc=  MetaFactory.factory.getSingleton<CQueryHelper>();
                    ErrorLogger.Logger.logException(
                        $"iGetViewChildTable TargetTable.Rows.Add(newR) TargetTable={TargetTable.TableName} Relation={Rel.RelationName} DataSet={TargetTable.DataSet?.DataSetName ?? "no dataset"} "+
                        $" skippedColumns ={string.Join(",",skippedColumns.ToArray())} key={qhc.CmpKey(newR)} ", 
                        e,null,Conn);
			        continue;
			    }


			        //collego la stringa k|chiavefiglio alla riga madre nella vista
                addRowToCache(ViewTable, TargetTable.PrimaryKey,
	                QueryCreator.FilterKey(newR,DataRowVersion.Default,forPosting:false) , RR);
                //incPrescannedTable(TargetTable);
                newR.AcceptChanges();
			}
			model.InvokeActions(TargetTable,TableAction.endLoad);
		}

	    void initCacheParentView() {
	        tableCache.Clear();
	        cachedParentNoKey.Clear();
	        cachedParentVkey.Clear();
	        cachedParentCkey.Clear();
	        cachedParentSourceColumn.Clear();
	        cachedChildSourceColumn.Clear();
			preScannedTablesRows.Clear();
	    }

        DataRow getRowFromCache(DataTable t, DataColumn[] col, MetaExpression filter, out bool found) {
	        string cols = "§"+String.Join("§",(from DataColumn c in col select c.ColumnName).ToArray());
	        string tabKey = t.TableName + cols;
	        found = false;
            checkPreScannedTable(t,col);
	        if (!tableCache.ContainsKey(tabKey)) return null;
            if (!tableCache[tabKey].ContainsKey(filter.toString())) return null;
            found = true;
	        return tableCache[tabKey][filter.toString()];
	    }
       
        private Dictionary<string, Dictionary<string, DataRow>> tableCache =new Dictionary<string, Dictionary<string, DataRow>>();

	    private Dictionary<string, bool> cachedParentNoKey = new Dictionary<string, bool>();
	    private Dictionary<string, DataColumn[]> cachedParentVkey = new Dictionary<string, DataColumn[]>();
        private Dictionary<string, DataColumn[]> cachedParentCkey = new Dictionary<string, DataColumn[]>();
        private Dictionary<string, DataColumn> cachedParentSourceColumn = new Dictionary<string, DataColumn>();
       
        private Dictionary<string, DataColumn> cachedChildSourceColumn = new Dictionary<string, DataColumn>();

	    private Dictionary<string, int> preScannedTablesRows = new Dictionary<string, int>();

	    bool checkPreScannedTable(DataTable t,DataColumn[]cols) {
		    string cc = "§"+String.Join("§",(from DataColumn c in cols select c.ColumnName).ToArray());
		    string tabKey = t.TableName + cc;

	        if (!preScannedTablesRows.ContainsKey(tabKey)) {
	            preScannedTablesRows[tabKey] = 0;
	        }
	        if (preScannedTablesRows[tabKey] > 0) return false;
	        foreach (DataRow r in t.Rows) {
	            addRowToCache(t, cols,
							 QueryCreator.FilterColumnPairs(r, cols,cols, DataRowVersion.Default,  forPosting:false),
							r);
	        }
	        preScannedTablesRows[tabKey] = t.Rows.Count;
            return true;
	    }

	    void incPrescannedTable(DataTable t,DataColumn[]cols) {
		    string cc = "§"+String.Join("§",(from DataColumn c in cols select c.ColumnName).ToArray());
		    string tabKey = t.TableName + cc;
            if (!preScannedTablesRows.ContainsKey(tabKey)) {
                preScannedTablesRows[tabKey] = 0;
            }
	        preScannedTablesRows[tabKey] = preScannedTablesRows[tabKey] + 1;
	    }

        void addRowToCache(DataTable parent, DataColumn[] col, MetaExpression filter,DataRow r) {
	        string cols = "§"+String.Join("§",(from DataColumn c in col select c.ColumnName).ToArray());
	        string tabKey = parent.TableName + cols;

	        if (!tableCache.ContainsKey(tabKey)) {
                tableCache[tabKey] = new Dictionary<string, DataRow>();
	        }
            tableCache[tabKey][filter.toString()] = r;            
        }

	    bool GetParentRowsFromView(DataRow R, DataRelation Rel) {
            int chrono = StartTimer("GetParentRowsFromView * " + Rel.RelationName );
            bool res = iGetParentRowsFromView(R, Rel);
            StopTimer(chrono);
	        return res;
	    }

        /// <summary>
        /// Gets R parent (by relation Rel)row from a view. Assumes that the view table has
        ///  already been read.
        /// Here ViewTable.ExtendedProperties["RealTable"]==R.Table
        /// </summary>
        /// <param name="R"></param>
        /// <param name="Rel"></param>
        /// <returns>true if row has been read (it was in the view)</returns>
        bool iGetParentRowsFromView(DataRow R, DataRelation Rel){
		    if (cachedParentNoKey.ContainsKey(Rel.RelationName)) {
                return false;
		    }
			//Table to retrieve rows
			var TargetTable = Rel.ParentTable;
			var ViewTable = (DataTable) TargetTable.ExtendedProperties["ViewTable"];
			var MainTable = R.Table; //== ViewTable.ExtProp["RealTable"]


            DataColumn[] Ckey;
            DataColumn[] Vkey;
            if(cachedParentVkey.ContainsKey(Rel.RelationName)) {
                Vkey = cachedParentVkey[Rel.RelationName];
                Ckey = cachedParentCkey[Rel.RelationName];
            }
            else {
                //key columns of Parent Table in ViewTable
                Vkey = new DataColumn[TargetTable.PrimaryKey.Length];
                //key columns of Parent Table
                Ckey = new DataColumn[TargetTable.PrimaryKey.Length];
                for(int i = 0; i < Vkey.Length; i++) {
                    bool found = false;
                    string colname = MainTable.TableName + "." + Rel.ChildColumns[i].ColumnName;
                    Ckey[i] = Rel.ParentColumns[i];
                    //search the column in view corresponding to Rel.ParentCol[i]
                    foreach(DataColumn CV in ViewTable.Columns) {
                        if(CV.ExtendedProperties["ViewSource"] == null)
                            continue;
                        if(CV.ExtendedProperties["ViewSource"].ToString() == colname) {
                            found = true;
                            Vkey[i] = CV;
                            break;
                        }
                    }
                    if(!found) {
                        cachedParentNoKey.Add(Rel.RelationName, true);
                        return false; //relation columns were not found
                    }
                }
                cachedParentVkey[Rel.RelationName] = Vkey;
                cachedParentCkey[Rel.RelationName] = Ckey;
            }


            var viewparentfilter = QueryCreator.FilterColumnPairs(R, Rel.ChildColumns,Rel.ChildColumns, 
	            DataRowVersion.Default, forPosting:false);
            //era WHERE_REL_CLAUSE(R, Rel.ChildColumns, VKey 
            var RV = getRowFromCache(ViewTable, Rel.ChildColumns, viewparentfilter, out var foundR); //Cerca con la chiave sul campo della parent
            if (foundR && RV==null) return false;
		    if (RV == null) {
			    //string kFilter = QueryCreator.WHERE_KEY_CLAUSE(R, DataRowVersion.Default, false);
                RV = getRowFromCache(ViewTable,Rel.ChildColumns, viewparentfilter, out _);	//cerca con la chiave sul campo della vista
                if (RV == null) {
	                var ViewParentRows = ViewTable.filter(q.mCmp(R,Rel.ChildColumns));//.Select(viewparentfilter);
                    if (ViewParentRows.Length == 0) {
                        addRowToCache(ViewTable, Rel.ChildColumns, viewparentfilter, null);
                        return false;
                    }
                    RV = ViewParentRows[0];

                    addRowToCache(ViewTable, Rel.ChildColumns, viewparentfilter, RV);
                    incPrescannedTable(ViewTable,Rel.ChildColumns);
                }
               
		       
		    }


            //get search condition for child row				
            var filterparent = QueryCreator.FilterColumnPairs(RV,Vkey, Ckey, DataRowVersion.Default,  forPosting:false);
		    var childFound = getRowFromCache(TargetTable, Rel.ChildColumns, filterparent, out _);            
		    if (childFound != null) return true;
         
			
			var NewChild = TargetTable.NewRow();
			//copy key from view to new row
			for (int ii=0; ii<Vkey.Length; ii++){
                string colname2 = TargetTable.TableName + "." + Ckey[ii].ColumnName;
                string k = Rel.RelationName + "|" + colname2;
			    cachedParentSourceColumn[k] = Vkey[ii];
                //NewChild[Ckey[ii]] = RV[Vkey[ii]];
			}
			
			//copy values from view to new row
			foreach (DataColumn CCT in TargetTable.Columns){
				string colname2= TargetTable.TableName+"."+CCT.ColumnName;
			    string k = Rel.RelationName + "|" + colname2;
			    if (cachedParentSourceColumn.ContainsKey(k)) {
			        NewChild[CCT] = RV[cachedParentSourceColumn[k]];
			    }
			    else {

			        foreach (DataColumn CCV in ViewTable.Columns) {
			            if (CCV.ExtendedProperties["ViewSource"] == null) continue;
			            if (CCV.ExtendedProperties["ViewSource"].ToString() == colname2) {
			                NewChild[CCT] = RV[CCV];
                            cachedParentSourceColumn[k] = CCV;
                            break;
			            }
			        }
			    }
			}
			TargetTable.Rows.Add(NewChild);
			NewChild.AcceptChanges();
            addRowToCache(TargetTable, Ckey, filterparent, NewChild);
            incPrescannedTable(TargetTable,Rel.ChildColumns);
            return true;
		
		}

		bool DataRowInList(DataRow R, DataRow []List){
			foreach(DataRow RR in List){
				if (R.Equals(RR)) return true;
			}
			return false;
		}

        /// <summary>
        /// Get all child rows of rows in RR, only navigating in tables whose name is in Allowed.keys
        /// </summary>
        /// <param name="RR"></param>
        /// <param name="Allowed"></param>
        /// <param name="SelList"></param>
		public async Task GetAllChildRows(DataRow []RR, HashSet<string> allowed, List<SelectBuilder> SelList=null){
			if (RR.Length==0) return;
			var currTable= RR[0].Table;
            foreach (DataRelation rel in currTable.ChildRelations) {
                var allowedParents = currTable.Select(QueryCreator.GetRelationActivationFilter(rel));

                var childtable = rel.ChildTable.TableName;
                if (currTable.Rows.Count == 0) continue;
                if (!allowed.Contains(childtable)) continue;

                var viewTable = (DataTable) rel.ChildTable.ExtendedProperties["ViewTable"];//Vede se la tabella è da leggere da una vista
                if ((viewTable != null) &&
                    (viewTable.ExtendedProperties["RealTable"] == rel.ChildTable) //vede se la vista ha come tabella principale quella data
                ) {
                    foreach (var r in RR) {
                        //if (R.RowState== DataRowState.Added) continue; //NEW!
                        if (DataRowInList(r, allowedParents)) await GetViewChildTable(r, rel);//allowedParents di solito ha una riga sola
                    }
                    continue;
                }

                //if ((RR.Length==1)||(Rel.ChildColumns.Length!=1)) {
                foreach (var r in RR) {
                    if (r.RowState == DataRowState.Deleted) continue;
                    //if (R.RowState== DataRowState.Added) continue; //NEW!
                    if (!DataRowInList(r, allowedParents)) continue;
                    var childfilter = QueryCreator.FilterColumnPairs(r, rel.ParentColumns, rel.ChildColumns,DataRowVersion.Default, 
							forPosting:false);
                    var mc = QueryCreator.ParentChildFilter(r, rel.ParentColumns, rel.ChildColumns,DataRowVersion.Default, true);
                    await GetRowsByFilter(childfilter, table:childtable, multiComp:mc,  selList:SelList);
                }
            }

        }

		/// <summary>
		/// Gets R childs in a set of allowed Tables
		/// </summary>
		/// <param name="R"></param>
		/// <param name="allowed">List of tables of which childs must be searched</param>
        /// <param name="selList"></param>
		async Task GetChildRows(DataRow R, HashSet<string> allowed, List<SelectBuilder> selList=null){
            //bool HadChanges = DS.HasChanges();
			foreach (DataRelation Rel in R.Table.ChildRelations){
				DataRow []AllowedParents = R.Table.Select(QueryCreator.GetRelationActivationFilter(Rel));
				if (!DataRowInList(R,AllowedParents)) continue;
				
				string childtable= Rel.ChildTable.TableName;
				if (!allowed.Contains(childtable)) continue;

				//Retrieve child rows
				if (QueryCreator.ContainsNulls(R, Rel.ParentColumns, 
					DataRowVersion.Default)) continue;

				DataTable ViewTable = (DataTable) Rel.ChildTable.ExtendedProperties["ViewTable"];
				if ((ViewTable!=null)&&
					(ViewTable.ExtendedProperties["RealTable"]==Rel.ChildTable)
					) {
					await GetViewChildTable(R,Rel);
					continue;
				}

				var childfilter= QueryCreator.FilterColumnPairs(R, Rel.ParentColumns, Rel.ChildColumns, 
					DataRowVersion.Default, forPosting:false);
                if (childfilter.isTrue()) continue;
                MultiCompare MC = QueryCreator.ParentChildFilter(R, Rel.ParentColumns, Rel.ChildColumns,
                    DataRowVersion.Default, true);
				//inutile, poiché GetRowByFilter effettua il merge
				//childfilter = MergeFilters(childfilter, Rel.ChildTable);
				
				await GetRowsByFilter(childfilter, multiComp: MC, table:childtable, selList: selList);     
			}
            //bool HasChanges = DS.HasChanges();
            //if (HadChanges != HasChanges) {
            //    MarkEvent("Errore in GetChildRows di "+R.Table.TableName);
            //}
		}


		

        async Task GetRowsByFilter(MetaExpression filter, string table,   string TOP=null, MultiCompare multiComp=null, List<SelectBuilder> selList=null) {
            var T = DS.Tables[table];
            if (!model.CanRead(T)) return;
            var mergedfilter = GetData.MergeFilters(filter, T);

            if (selList == null) {
                await Conn.SelectIntoTable(T, filter:mergedfilter, top:TOP);
            }
            else {
                selList.Add(new SelectBuilder().IntoTable(T).Where(mergedfilter).MultiCompare(multiComp).Top(TOP).OrderBy(T.getSorting()));
            }

            //Cache(cachedcmd);
            model.TableHasBeenRead(T);

        }
         		

		/// <summary>
		/// Get parent rows of a given Row, in a set of specified  tables.
		/// </summary>
		/// <param name="r">DataRow whose parents are wanted</param>
		/// <param name="allowed">Tables in which to search parent rows</param>
        /// <param name="selList"></param>
		/// <returns>true if any of parent DataRows was already in memory. This is not 
		///  granted if rows are taken from a view</returns>
		public async Task<bool> GetParentRows(DataRow r, HashSet<string> allowed, List<SelectBuilder> selList){
			var inmemory = false;
			if (r==null) return false;
			if (r.RowState==DataRowState.Detached) return false;
			if (r.RowState==DataRowState.Deleted) return false;
			foreach (DataRelation rel in r.Table.ParentRelations){

				var parenttable= rel.ParentTable.TableName;
				if (!allowed.Contains(parenttable)) continue;
				if (QueryCreator.ContainsNulls(r, rel.ChildColumns, DataRowVersion.Default)) continue;

                //DataRow []AllowedChilds= R.Table.Select(QueryCreator.GetParentRelationActivationFilter(Rel));
                //if (!DataRowInList(R,AllowedChilds)) continue;

				var parentfilter= QueryCreator.FilterColumnPairs(r, rel.ChildColumns, rel.ParentColumns, DataRowVersion.Default, 
					forPosting:false);
                if (parentfilter.isTrue()) continue;
               


               //correggo colonne da usare nella ricerca nel parent, l'errore stava causando rallentamenti nei tree, task 15039
                var rFound = getRowFromCache(rel.ParentTable, rel.ParentColumns, parentfilter, out var parentFound);
                if (parentFound) {
					inmemory=true;
				}
				else {
					var viewTable = (DataTable) rel.ParentTable.ExtendedProperties["ViewTable"];
					if ( (viewTable!=null)&&  (viewTable.ExtendedProperties["RealTable"]==r.Table) ) {
						if (!GetParentRowsFromView(r, rel)) {
							var multiComp = QueryCreator.ParentChildFilter(r, rel.ChildColumns, rel.ParentColumns, DataRowVersion.Default, true);
							await GetRowsByFilter(filter:parentfilter,table:parenttable,multiComp: multiComp,  selList: selList);
						}					
					}
					else	   {
						var multiComp = QueryCreator.ParentChildFilter(r, rel.ChildColumns, rel.ParentColumns, DataRowVersion.Default, true);
						await GetRowsByFilter(filter:parentfilter, table:parenttable,multiComp:multiComp,selList: selList);
					}
				}
			}
			return inmemory;
		}

	
		/// <summary>
		/// Gets a DataTable related with PrimaryTable via a given Relation Name.
		/// Also gets columns implied in the relation of related table 
		/// </summary>
		/// <param name="relname"></param>
		/// <param name="Cs">Columns of related table, implied in the relation</param>
		/// <returns>Related table</returns>
		public DataTable EntityRelatedByRel(string relname, out DataColumn[] Cs){
			Cs=null;
			if (PrimaryDataTable.ParentRelations[relname]!=null){
				var ParentRel = PrimaryDataTable.ParentRelations[relname];
				Cs = ParentRel.ParentColumns;
				return ParentRel.ParentTable;
			}
			if (PrimaryDataTable.ChildRelations[relname]!=null){
				var childRel = PrimaryDataTable.ChildRelations[relname];
				Cs= childRel.ChildColumns;
				return childRel.ChildTable;
			}
			return null;
		}


		#region Gestione Custom Query 
	

		

      

        class MyParameter {
			public string name;
			public string val;
			public MyParameter(string name, string val){
				this.name=name;
				this.val=val;
			}

			public static string SearchInArray(string name, ArrayList val){
				for (int i=0; i<val.Count; i++){
					var P = (MyParameter)val[i];
					if (P.name.Equals(name))return  P.val;
				}
				return null;
			}
		}
	
		#endregion



	}



}

    


