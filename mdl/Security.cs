using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static mdl.Metaprofiler;
#pragma warning disable IDE1006 // Naming Styles


namespace mdl {

    /// <summary>
    /// Manages security conditions and environment variables
    /// </summary>
    public interface ISecurity: ICloneable {
        
        /// <summary>
        /// User id, also used for optimistic locking
        /// </summary>
        string User { get;set;}

         /// <summary>
         /// Gets the condition on a specific operation on a table
         /// </summary>
         /// <param name="T"></param>
         /// <param name="opkind_IUDSP"></param>
         /// <returns></returns>
         MetaExpression postingCondition(DataTable T, string opkind_IUDSP);

              /// <summary>
        /// Check if R is allowed to be written to db
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool CanPost(DataRow R);

        /// <summary>
        /// Delete all rows from T that are not allowed to be selected
        /// </summary>
        /// <param name="T"></param>
        void DeleteAllUnselectable(DataTable T);

        /// <summary>
        /// Check if a row can be selected
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool CanSelect(DataRow R);

        /// <summary>
        /// Gets the security condition for selecting rows in a table
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        MetaExpression SelectCondition(string tablename);


        
        /// <summary>
        /// Check if R can be "printed". 
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        bool CanPrint(DataRow R);

        /// <summary>
        /// Check if there is a total deny of writing/deleting/inserting on a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="OpKind"></param>
        /// <returns></returns>
        bool CantUnconditionallyPost(DataTable T, string OpKind);

        /// <summary>
        /// Substitute every &lt;%sys[varname]%&gt; and &lt;%usr[varname]%&gt; with actual values
        ///  taken from environment variables
        /// </summary>
        /// <param name="S"></param>
        /// <param name="SQL">When true, SQL representations are used to display values</param>
        /// <returns></returns>
        string Compile(string S, QueryHelper qh, bool quoted=true);

        /// <summary>
        /// Subtitutes  every sequence:  openbr sys_name closebr with the unquoted value of sys[sys_name] 
        /// </summary>
        /// <param name="S"></param>
        /// <param name="SQL"></param>
        /// <param name="openbr"></param>
        /// <param name="closebr"></param>
        /// <returns></returns>
        string CompileWeb(string S, bool SQL, string openbr, string closebr, QueryHelper qh);


        /// <summary>
        /// Get user environment variable 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object GetUsr(string key);

        /// <summary>
        /// Get system environment variable
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object GetSys(string key);

        /// <summary>
        /// Enumerates system variable names
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> EnumSysKeys();

        /// <summary>
        /// Enumerates user variable names
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> EnumUsrKeys();

        /// <summary>
        /// NON USARE !
        /// </summary>
        /// <param name="key"></param>
        /// <param name="O"></param>
        void SetUsr(string key, object O);
		
        /// <summary>
        /// Set system environment variable
        /// </summary>
        /// <param name="key"></param>
        /// <param name="o"></param>
        void SetSys(string key, object o);


        /// <summary>
        /// Returns true if current user has sysadmin membership
        /// </summary>
        /// <returns></returns>
        bool IsSystemAdmin();
       

        /// <summary>
        /// Clears entire environment
        /// </summary>
        void Clear();

       

    }

    /// <summary>
    /// Base security class
    /// </summary>
    public class DefaultSecurity :ISecurity {
        /// <summary>
        /// User id
        /// </summary>
        public string User { get; set; }


        /// <summary>
        /// Session user variables
        /// </summary>
        private readonly Dictionary<string, object> _usr =new Dictionary<string, object>(); //MUST BECOME internal protected

        /// <summary>
        /// Session system variables
        /// </summary>
        private readonly Dictionary<string,object> _sys = new Dictionary<string, object>(); //MUST BECOME internal protected

        /// <summary>
        /// Model used 
        /// </summary>
        protected static IMetaModel model = MetaFactory.factory.getSingleton<IMetaModel>();

        
     
        /// <summary>
        /// Basic security manager constructor 
        /// </summary>
        /// <param name="conn"></param>
        public DefaultSecurity(string user) {
            this.User = user;
        }

        public virtual object Clone() {
            var isec = new DefaultSecurity(User);
            foreach(var k in EnumSysKeys()) isec.SetUsr(k,_usr[k]);
            foreach (var k in EnumUsrKeys()) isec.SetUsr(k, _usr[k]);

            return isec;
        }

        /// <summary>
        /// Returns true if current user has sysadmin membership
        /// </summary>
        /// <returns></returns>
        public virtual bool IsSystemAdmin() {
            //var (o, _) = Conn.ExecuteSql("select IS_SRVROLEMEMBER ('sysadmin') AS issysadmin").GetAwaiter().GetResult();
            //return o != null && o.ToString() == "1";
            return false;
        }

        /// <summary>
        /// Empty variables
        /// </summary>
        public void Clear() {
            _usr.Clear();
            _sys.Clear();
        }

        /// <summary>
        /// Crypts a string with 3-des
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal byte[] cryptKey(string key) {
			if (key == null) return null;
			//while ((pwd.Length % 8)!=0) pwd+=" ";
			//char[] a= pwd.ToCharArray();
			//byte []A = new byte[a.Length];
			//for (int i=0; i<a.Length; i++) A[i]= Convert.ToByte(a[i]);
			var a = Encoding.Default.GetBytes(key);

			var ms = new MemoryStream(1000);
			var tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
			using (var cryptoS = new CryptoStream(ms,
				tripleDESCryptoServiceProvider.CreateEncryptor(
					new byte[] { 75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190 },
					new byte[] { 61, 13, 99, 42, 149, 123, 145, 48, 83, 20, 238, 57, 128, 38, 12, 4 }
				), CryptoStreamMode.Write)) {
				cryptoS.Write(a, 0, a.Length);
				cryptoS.FlushFinalBlock();
			}
			var b = ms.ToArray();
			tripleDESCryptoServiceProvider.Dispose();
			return b;
		}

		/// <summary>
		/// Descripts a byte array with 3-des
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		internal static string decryptKey(byte[] b) {
            if (b == null) return null;
			var tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
			var mtdes = tripleDESCryptoServiceProvider.CreateDecryptor(
	            new byte[] {75, 12, 0, 215, 93, 89, 45, 11, 171, 96, 4, 64, 13, 158, 36, 190},
	            new byte[] {61, 13, 99, 42, 149, 123, 145, 48, 83, 20, 238, 57, 128, 38, 12, 4}
            );
            var ms = new MemoryStream();
            var cryptoS = new CryptoStream(ms,mtdes, CryptoStreamMode.Write);
            cryptoS.Write(b, 0, b.Length);
            cryptoS.FlushFinalBlock();
            var key = Encoding.Default.GetString(ms.ToArray()).TrimEnd();
            cryptoS.Dispose();
			//mtdes.Dispose();
			tripleDESCryptoServiceProvider.Dispose();
            return key;
        }

        /// <summary>
        /// Get system environment variable
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetSys(string key) {            
            return _sys[key];
        }

        readonly object _lockSysKeys = new object();
        readonly object _lockUsrKeys = new object();

        /// <summary>
        /// Enumerates system variables
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> EnumSysKeys() {
            lock (_lockSysKeys) {
                foreach (var o in _sys.Keys) {
                    yield return o;
                    //var key = o.ToString();
                    //k[i] = key;
                    //i++;
                }
            }
        }

        /// <summary>
        /// Enumerates user variables
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> EnumUsrKeys() {
            lock (_lockUsrKeys) {
                foreach (var o in _usr.Keys) {
                    yield return o;
                }
            }
        }

        /// <summary>
        /// NON USARE !
        /// </summary>
        /// <param name="key"></param>
        /// <param name="o"></param>
        public void SetUsr(string key, object o) {  //deve diventare internal protected
            lock (_lockUsrKeys) {
                _usr[key] = o;
            }
        }


		/// <summary>
		/// Get user environment variable 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public object GetUsr(string key) {
            return _usr[key];
        }

        /// <summary>
        /// Set system environment variable
        /// </summary>
        /// <param name="key"></param>
        /// <param name="o"></param>
        public virtual void SetSys(string key, object o) {  //deve diventare internal
            lock (_lockSysKeys) {
                //if (key.StartsWith("password") & o is string) {
                //        _sys[key] = cryptKey(o.ToString());
                //        return;
                //    }
                _sys[key] = o;
            }
                
        }
        



		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <param name="opkind_IUDSP"></param>
		/// <returns></returns>
		public virtual MetaExpression postingCondition(DataTable t,string opkind_IUDSP) {
           return null;
        }
      

        
        /// <summary>
        /// Check if R is allowed to be written to db
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual bool CanPost(DataRow r) {
            if (model.IsSkipSecurity(r.Table)) {
                return true;
            }
            return true;
        }

        /// <summary>
        /// Delete all rows from T that are not allowed to be selected
        /// </summary>
        /// <param name="T"></param>
        public virtual void DeleteAllUnselectable(DataTable T) {
            if (model.IsSkipSecurity(T)) return;
            foreach (var r in T.Select()) {
                if (CanSelect(r)) continue;
                r.Delete();
                r.AcceptChanges();
            }
        }


        /// <summary>
        /// 
        /// Check if a row can be selected
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual bool CanSelect(DataRow r) {
            return true;
        }

        ///TODO: sql parameter must be removed, conditions must be metaexpressions
        /// <summary>
        /// Gets the security condition for selecting rows in a table
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual MetaExpression SelectCondition(string tablename) {
            return null;
        }

       

        /// <summary>
        /// Check if R can be "printed". 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual bool CanPrint(DataRow r) {
            return true;
        }

        

        /// <summary>
        /// Check if there is a total deny of writing/deleting/inserting on a table
        /// </summary>
        /// <param name="T"></param>
        /// <param name="opKind"></param>
        /// <returns></returns>
        public virtual bool CantUnconditionallyPost(DataTable T, string opKind) {
            return false;
        }

       

        private delegate string quoterFun(object o);

        /// <summary>
        /// Compile a string substituting keys with quoted values
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public virtual string Compile(string S, QueryHelper qh, bool quoted=true) {
            string newS = S;
            if ((S == null) || (S == "")) return newS;
            int handle = StartTimer("compile");
            bool applied = true;
            quoterFun quoter = quoted? (quoterFun) qh.quote : (quoterFun) qh.unquoted;
            while (applied) {
                applied = false;
                if (newS.IndexOf("<%sys[", StringComparison.Ordinal) >= 0) {

                    
                    foreach (var o in EnumSysKeys()) {
                        string oldvalue = "<%sys[" + o + "]%>";
                        if (newS.IndexOf(oldvalue, StringComparison.Ordinal) >= 0) {
                            string newvalue = quoter(_sys[o]);
                            newS = newS.Replace(oldvalue, newvalue);                            
                            //if (o.ToString() == "user") newvalue = newvalue.Replace("'", "''");
                            //if (o.ToString() == "idcustomuser") newvalue = newvalue.Replace("'", "''");
                            applied = true;
                        }
                    }
                }
                if (newS.IndexOf("<%usr[", StringComparison.Ordinal) >= 0) {
                    foreach (var o in EnumUsrKeys()) {
                        string oldvalue = "<%usr[" + o + "]%>";
                        if (newS.IndexOf(oldvalue, StringComparison.Ordinal) >= 0) {
                            string newvalue = quoter(_usr[o]);
                            newS = newS.Replace(oldvalue, newvalue);
                            applied = true;
                        }
                    }
                }
            }
            if (newS.IndexOf("<%usr") >= 0 || newS.IndexOf("<%sys") >= 0) {
                //ErrorLogger.Logger.markEvent("Trovata variabile di sicurezza non valorizzata nella stringa " + newS);
                return "(1=2)";
            }
            StopTimer(handle);
            return newS;
        }



        /// <summary>
        /// Subtitutes  every sequence:  openbr sys_name closebr with the unquoted value of sys[sys_name] 
        /// </summary>
        /// <param name="S"></param>
        /// <param name="SQL"></param>
        /// <param name="openbr"></param>
        /// <param name="closebr"></param>
        /// <returns></returns>
        public string CompileWeb(string S, bool SQL, string openbr, string closebr, QueryHelper qh) {
            string newS = S;
            if ((S == null) || (S == "")) return newS;
            bool applied = true;
            while (applied) {
                applied = false;
                foreach (var o in EnumSysKeys()) {
                    string oldvalue = openbr + o.ToString() + closebr;
                    if (newS.IndexOf(oldvalue) >= 0) {
                        string newvalue = qh.unquoted(_sys[o]);
                        newS = newS.Replace(oldvalue, newvalue);
                        applied = true;
                    }
                }
            }
            return newS;
        }


    }
}
