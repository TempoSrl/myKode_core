using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LM=mdl_language.LanguageManager;

namespace mdl {


    /// <summary>
    /// Manages metadata loading
    /// </summary>
    public interface IMetaDataDispatcher {


        /// <summary>
        /// True if an unrecoverable error has occurred
        /// </summary>
        bool unrecoverableError { get; set; }

    

        /// <summary>
        ///  Returns a standard MetaData, with all "base" functionality
        /// </summary>     
        /// <param name="metaDataName"></param>
        /// <returns></returns>
        IMetaData DefaultMetaData(string metaDataName);

        /// <summary>
        /// Returns a custom MetaData, given its name
        /// </summary>
        /// <param name="metaDataName"></param>
        /// <returns></returns>
        IMetaData Get(string metaDataName);

        /// <summary>
        /// Edit an entity (tablename) with a specified edit-type
        /// </summary>
        /// <param name="parent">Parent Form</param>
        /// <param name="metaDataName">name of primary table to edit</param>
        /// <param name="editName">logical name of form (edit-type)</param>
        /// <param name="modal">true if Form has to be opened in modal mode</param>
        /// <param name="param">Extra parameter to assign to MetaData before crating the form</param>
        /// <returns></returns>
        bool Edit(object parent, string metaDataName, string editName, bool modal, object param);
    }

    /// <summary>
    /// Class that knows how to create MetaData Objects, and editing
    ///  them using Forms identified by logical names
    /// </summary>
    public class MetaDataDispatcher :IMetaDataDispatcher {
        

        /// <summary>
        /// Data access linked to the dispatcher
        /// </summary>
#pragma warning disable 612
        public IDataAccess Conn {get;set;}
#pragma warning restore 612

        private ISecurity _security;

        /// <summary>
        /// 
        /// </summary>
        public ISecurity security {
            get { return _security ?? Conn?.Security; }
            set { _security = value; }
        }

        /// <summary>
        /// Build the dispatcher and gives it a DB connection
        /// </summary>
        /// <param name="conn"></param>
        [Obsolete]
        public MetaDataDispatcher(DataAccess conn) {
            this.Conn = conn;
        }

        /// <summary>
        /// Class for logging errors
        /// </summary>
        public IErrorLogger errorLogger { get; set; } = ErrorLogger.Logger;


        /// <summary>
        /// Builds a dispatcher connecting it to a database
        /// </summary>
        /// <param name="conn"></param>
        public MetaDataDispatcher(IDataAccess conn) {
            this.Conn = conn;
            security = conn.Security;
        }

        /// <summary>
        /// Send an Exception to remote logger
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="e"></param>
        public void logException(string msg, Exception e) {
            errorLogger.logException(msg, exception: e, dataAccess: Conn);
        }


        /// <summary>
        /// Get a system environment variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public object GetSys(string name) {
            return security.GetSys(name);
        }

        /// <summary>
        /// Set a system environment variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="O">value to set</param>
        public void SetSys(string name, object O) {
            security.SetSys(name, O);
        }

        /// <summary>
        /// Get a user environment variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public object GetUsr(string name) {

            return security.GetUsr(name);
        }

        /// <summary>
        /// Set a user environment variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="O">value to set</param>
        public void SetUsr(string name, object O) {
            security.SetUsr(name, O);
        }


        /// <summary>
        /// Check if 
        /// </summary>
        public bool unrecoverableError { get; set; }

       
        /// <summary>
        /// Returns a standard MetaData, with all "base" functionality
        /// </summary>
        /// <param name="objectname"></param>
        /// <returns></returns>
        public virtual IMetaData DefaultMetaData(string objectname) {
            return new MetaData(Conn, this, security, objectname);
        }


        /// <summary>
        /// Edit an entity (tablename) with a specified edit-type
        /// </summary>
        /// <param name="parent">Parent Form</param>
        /// <param name="metaDataName">name of primary table to edit</param>
        /// <param name="editName">logical name of form (edit-type)</param>
        /// <param name="modal">true if Form has to be opened in modal mode</param>
        /// <param name="param">Extra parameter to assign to MetaData before crating the form</param>
        /// <returns></returns>
        public bool Edit(object parent, string metaDataName, string editName, bool modal, object param) {
            return false;
        }



        /// <summary>
        /// Returns a custom MetaData, given its name
        /// </summary>
        /// <param name="metaDataName"></param>
        /// <returns></returns>
        public virtual IMetaData Get(string metaDataName) {
            return new MetaData(Conn, this, security, metaDataName);
        }

    



    }
}
