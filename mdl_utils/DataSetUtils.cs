using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace mdl_utils {
    public static class DataSetUtils {
          /// <summary>
        /// Gets the maximum value from a column
        /// </summary>
        /// <param name="T"></param>
        /// <param name="column"></param>
        /// <returns>0 if table was empty</returns>
        public static int MaxFromColumn(DataTable T, string column) {
            if (T.Columns[column] == null) return 0;
            if (T.Rows.Count == 0) return 0;
            try {
                return Convert.ToInt32(T.Compute("MAX(" + column + ")", null).ToString());
            }
            catch {
                return 0;
            }
        }

        /// <summary>
        /// Set the primary key of Dest conformingly to table Source 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="source"></param>
        public static void CopyPrimaryKey(DataTable dest, DataTable source) {
            if (dest.PrimaryKey.Length > 0) return;
            try {

                var k = new DataColumn[source.PrimaryKey.Length];
                for (var i = 0; i < source.PrimaryKey.Length; i++)
                    k[i] = dest.Columns[source.PrimaryKey[i].ColumnName];
                dest.PrimaryKey = k;
            }
            catch  {
                //ErrorLogger.Logger.markException(e, "CopyPrimaryKey");
            }
        }

        /// <summary>
        /// Get the sum of a column
        /// </summary>
        /// <param name="T"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public static decimal SumColumn(DataTable T, string column) {
            if (T?.Columns[column] == null) return 0;
            decimal sum = 0;
            foreach (DataRow r in T.Rows) {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r[column].Equals(DBNull.Value)) continue;
                decimal x;
                try {
                    x = Convert.ToDecimal(r[column]);
                }
                catch {
                    x = 0;
                }

                sum += x;
            }

            return sum;
        }

          /// <summary>
        /// Zips/crypts an DataSet string it in a byte array
        /// </summary>
        /// <param name="D"></param>
        /// <param name="zip">if true, byte array is compressed</param>
        /// <returns></returns>
        public static byte[] PackDataSet(System.Data.DataSet D, bool zip=true) {
            int hh = metaprofiler.StartTimer("PackDataSet");
            var MS = new MemoryStream();
            if (!zip) {
                D.WriteXml(MS, XmlWriteMode.WriteSchema);
            }
            else {
	            using (var CS = new GZipStream(MS,CompressionLevel.Optimal)) {
		            D.WriteXml(CS, XmlWriteMode.WriteSchema);
		            CS.Close();
	            }
            }

            byte[] A = MS.ToArray();
            metaprofiler.StopTimer(hh);
            return A;
        }

        
        /// <summary>
        /// Unzip a DataSet stored in a byte array
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="A"></param>
        /// <returns></returns>
		public static DataSet UnpackDataSet( byte[] A) {
            //if (DataAccess.IsLocal) //Conn 
            //    return UnpackDataSet(Conn,A,false);
            //else
            return UnpackDataSet( A, true);
        }

         /// <summary>
        /// Unzip/decrypts a DataSet stored in a byte array
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="A"></param>
        /// <param name="zip">if true, byte array is first unzipped then converted to a dataset</param>
        /// <returns></returns>
        public static DataSet UnpackDataSet(byte[] A, bool zip) {
            int hh = metaprofiler.StartTimer("UnpackDataSet");
            using (var MS = new MemoryStream(A)) {
	            var D = new DataSet("dummy");
	            if (!zip) {
		            D.ReadXml(MS, XmlReadMode.ReadSchema);
	            }
	            else {
		            using (var CS = new GZipStream(MS,CompressionMode.Decompress)) {
			            try {
				            D.ReadXml(CS, XmlReadMode.ReadSchema);
			            }
			            catch {
				            //ErrorLogger.Logger.markException(E, "UnpackDataSet");
                            metaprofiler.StopTimer(hh);
                            return null;
			            }
		            }
	            }

	            D.AcceptChanges();
	            metaprofiler.StopTimer(hh);
	            return D;
            }
        }


        /// <summary>
        /// Zips/crypts an DataSet string it in a byte array
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public static byte[] PackDataSet( DataSet D) {
            //if (DataAccess.IsLocal)  //Conn 
            //    return PackDataSet(Conn, D,false);

            //else 
            return PackDataSet( D, true);
        }

        /// <summary>
        /// Compresses an array of bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] zip(byte[] data) {
            if (data.Length == 0) return data;
            var ms = new MemoryStream();
            var cs = new GZipStream(ms, CompressionMode.Compress);
            cs.Write(data, 0, data.Length);
            cs.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// Decompress an array of bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] unzip(byte[] data) {
            if (data.Length == 0) return data;
            MemoryStream ms = new MemoryStream(data);
            var cs = new GZipStream(ms, CompressionMode.Decompress);
            byte[] uncompressedData = null;
            using (var destinationStream = new MemoryStream()) {
                int bytesRead;

                // Setup a 32K buffer
                byte[] buffer = new byte[32 * 1024];

                // Read from the source stream until there is no more data, this will decompress the data
                while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0) {
                    // Compress the data by writing into the compressed stream
                    // Compressed data will be written into its InnerStream, in our case, 'destinationStream'
                    destinationStream.Write(buffer, 0, bytesRead);
                }

                /* Optional: The MemoryStream's compressed data can be copied to a byte array, you can use
                   MemoryStream.ToArray(). The method works even when the memory stream has been closed. */
                uncompressedData = destinationStream.ToArray();
            }

            cs.Close();
            return uncompressedData;
        }



    }
}
