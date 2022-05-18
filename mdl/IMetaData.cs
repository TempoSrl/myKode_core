using System;
using System.Data;
using System.Threading.Tasks;

namespace mdl {

    /// <summary>
    /// Show a message to client, used to use a common interface in windows and web applications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    /// <param name="btns"></param>
    /// <returns></returns>
    public delegate bool ShowClientMsgDelegate(string message, string title, MessageBoxButtons btns);

    /// <summary>
    /// Manages logic layer of data 
    /// </summary>
    public interface IMetaData {
        #region Methods that really should apply

        string Name { get; set; }

        IMetaDataDispatcher Dispatcher { get; set; }
     

       

        

        /// <summary>
        /// Name of the table that stores the entity. If SourceRow is present, it is
        ///  the same as SourceRow.TableName
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Returns a GetData class to use with this MetaData
        /// </summary>
        /// <returns></returns>
        GetData Get_GetData();


        /// <summary>
        /// 
        /// </summary>
        bool GetDataIsPrivate { get; set; }

        /// <summary>
        /// Returns a PostData class to use with this MetaData
        /// </summary>
        /// <returns></returns>
        PostData Get_PostData();


        /// <summary>
        /// Returns the default sorting for a list type
        /// </summary>
        /// <param name="listingType"></param>
        /// <returns></returns>
        string GetSorting(string listingType);

        /// <summary>
        /// Gets the static filter associated to the form
        /// </summary>
        /// <param name="listingType"></param>
        /// <returns></returns>
        MetaExpression GetStaticFilter(string listingType);


        /// <summary>
        /// Gets the primary key fields of the main table
        /// </summary>
        /// <returns></returns>
        string[] PrimaryKey();

        /// <summary>
        /// Get the message to display when a list returns no rows
        /// </summary>
        /// <param name="listingtype"></param>
        /// <returns></returns>
        string GetNoRowFoundMessage(string listingtype);
      

        /// <summary>
        /// Must return false if the given row can be selected with "mainselect" in the
        ///  form named edit_type. Should also display to user the reason for which 
        ///  row can't be selected.
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool CanSelect(DataRow R);

        /// <summary>
        /// Must return true if a row can appear in comboboxes, or can be selected while creating an external
        ///  reference to R from another table.
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool Enabled(DataRow R);

        /// <summary>
        /// Should set the caption of DataTable Columns according to a selected Listing Type
        ///  if a Column Caption is "" or starts with a dot, it is not displayed in grids.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="ListingType"></param>
        Task DescribeColumns(DataTable T, string listingType);


        /// <summary>
        /// Sets DenyNull and Format property of Columns
        /// </summary>
        /// <param name="T"></param>
        Task DescribeColumns(DataTable T);

 
        /// <summary>
        /// If ComputeRowsAs() has been called, this is called whenever a DataRow is 
        ///  read or modified.
        /// </summary>
        /// <param name="r">DataRow to which do custom field calculation</param>
        /// <param name="listType">listing type used for calculation</param>
        void CalculateFields(DataRow r, string listingType);

        /// <summary>
        /// Tells if a given DataRow must be displayed in a given list
        /// </summary>
        /// <param name="r">DataRow To Check for a filter condition</param>
        /// <param name="listType">kind of list</param>
        /// <returns>true when Row must be displayed</returns>
        bool FilterRow(DataRow r, string listingType);

        /// <summary>
        /// Tells MetaData Engine to call CalculateFields(R,ListingType) whenever:
        ///  - a row is loaded from DataBase
        ///  - a row is changed in a sub-entity form and modification accepted with mainsave
        /// </summary>
        /// <param name="primary">DataTable to which calculate fields</param>
        /// <param name="listingType">kind of list used for calculation</param>
        void ComputeRowsAs(DataTable T, string listingType);

        /// <summary>
        /// Mark a table to be field-calculated
        /// </summary>
        /// <param name="primary"></param>
        void FilterRows(DataTable primary);

        /// <summary>
        /// Used to filter combobox when main table is in insert mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        MetaExpression GetFilterForInsert(DataTable T);

        /// <summary>
        /// Used to filter combobox when main table is in search mode
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        MetaExpression GetFilterForSearch(DataTable T);

        /// <summary>
        /// Sets default values for fields. This is necessary when those do not allow 
        ///  null values. Default value are filled into row field whenever a new row is 
        ///  created.
        /// </summary>
        /// <param name="primaryTable">Table for which default values have to be set</param>
        Task SetDefaults(DataTable primaryTable, string editType=null);

        /// <summary>
        /// It's called before accepting changes to an entity, and must state whether the entity is valid.
        /// </summary>
        /// <param name="r">DataRow to test</param>
        /// <param name="errmess">error message to display, null if no problem</param>
        /// <param name="errfield">wrong field or null if no problem</param>
        /// <returns>true when entity is valid</returns>
        bool IsValid(DataRow r, out string errmess, out string errfield);


        /// <summary>
        /// Gets a new entity row, adding it to a table T, having ParentRow as Parent
        /// </summary>
        /// <param name="parentRow">Parent Row of the new Row to create, or null if no parent is present</param>
        /// <param name="T">Table in which row has to be added</param>
        /// <returns>new row, child of ParentRow when that is given</returns>
        Task<DataRow> GetNewRow(DataTable table, DataRow parentRow=null, string editType=null, string relationName=null);


        #endregion


        /// <summary>
        /// Class for logging errors
        /// </summary>
        IErrorLogger ErrorLogger { get; set; }

       

  
        /// <summary>
        /// List type used in absence of other indications
        /// </summary>
        string DefaultListType { get; set; }

    
        /// <summary>
        /// Filter used only one time, at form activation
        /// </summary>
        string FirstSearchFilter { get; set; }

   
      

        /// <summary>
		/// Primary DataTable of the MetaData, it's a link to ds[primaryTable]
		/// </summary>
		DataTable PrimaryDataTable { get; }


        DataSet ds { get; set; }

        /// <summary>
        /// True if there are Notes or OleNotes availabe for current row
        /// </summary>
        /// <returns></returns>
        bool NotesAvailable(DataRow R);


        bool HasNotes();

        /// <summary>
        /// Gets the "notes" field related to current row
        /// </summary>
        /// <returns></returns>
        string GetNotes(DataRow R);

        /// <summary>
        /// Set the "notes" field of current row
        /// </summary>
        /// <param name="notes"></param>
        void SetNotes(DataRow R, string notes);

        /// <summary>
        /// Returns true if a "notes" field is available for the
        ///  current row
        /// </summary>
        /// <returns></returns>
        bool HasOleNotes();

        /// <summary>
        /// Gets the "notes" field related to current row
        /// </summary>
        /// <returns></returns>
        byte[] GetOleNotes(DataRow R);

        /// <summary>
        /// Set the "notes" field of current row
        /// </summary>
        /// <param name="OleNotes"></param>
        void SetOleNotes(DataRow R, Byte[] oleNotes);

        /// <summary>
        /// 
        /// </summary>
        int ListTop { get; set; }
             
        /// <summary>
        /// Destroy and unlink this MetaData from anything
        /// </summary>
        void Destroy();
          
      
    }
}