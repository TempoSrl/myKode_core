using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using mdl_utils;

namespace mdl {
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
        public static byte[] PackDataSet(DataSet D, bool zip=true) {
            int hh = MetaProfiler.StartTimer("PackDataSet");
            var MS = new MemoryStream();
            if (!zip) {
                D.WriteXml(MS, XmlWriteMode.WriteSchema);
            }
            else {
				using var CS = new GZipStream(MS, CompressionLevel.Optimal);
				D.WriteXml(CS, XmlWriteMode.WriteSchema);
				CS.Close();
			}

            byte[] A = MS.ToArray();
            MetaProfiler.StopTimer(hh);
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
            int hh = MetaProfiler.StartTimer("UnpackDataSet");
			using var MS = new MemoryStream(A);
			var D = new DataSet("dummy");
			if (!zip) {
				D.ReadXml(MS, XmlReadMode.ReadSchema);
			}
			else {
				using var CS = new GZipStream(MS, CompressionMode.Decompress);
				try {
					D.ReadXml(CS, XmlReadMode.ReadSchema);
				}
				catch {
                    //ErrorLogger.Logger.markException(E, "UnpackDataSet");
                    MetaProfiler.StopTimer(hh);
					return null;
				}
			}

			D.AcceptChanges();
            MetaProfiler.StopTimer(hh);
			return D;
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
        public static byte[] Zip(byte[] data) {
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
        public static byte[] Unzip(byte[] data) {
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

         /// <summary>
        /// Merge rows of a source table into an empy table
        /// </summary>
        /// <param name="emptyTable"></param>
        /// <param name="sourceTable"></param>
	    public static void MergeIntoEmptyDataTable(DataTable emptyTable, DataTable sourceTable) {
	        var handle1 = MetaProfiler.StartTimer($"MergeIntoEmptyDataTable * {sourceTable.TableName}");
	        
	            if (emptyTable.DataSet!=null){                        
	                emptyTable.BeginLoadData();
	                emptyTable.DataSet.Merge(sourceTable,false,MissingSchemaAction.Ignore);
	                emptyTable.EndLoadData();
	            }
	            else {
	                var temp = new DataSet {EnforceConstraints = false};
	                temp.Tables.Add(emptyTable);
	                emptyTable.BeginLoadData();
	                temp.Merge(sourceTable, true, MissingSchemaAction.Ignore);
	                emptyTable.EndLoadData();
	                temp.Tables.Remove(emptyTable);
					temp.Dispose();
	            }
            MetaProfiler.StopTimer(handle1);
	    }

         /// <summary>
        /// Insert a datarow in a table preserving it's state
        /// </summary>
        /// <param name="t"></param>
        /// <param name="r"></param>
        public static void SafeImportRow(DataTable t, DataRow r) {
            var newR = t.NewRow();
            int nCol = t.Columns.Count;
            switch (r.RowState) {
                case DataRowState.Unchanged:
                    for (int i = nCol - 1; i >= 0; i--) {
                        newR[i] = r[i];
                    }
                    t.Rows.Add(newR);
                    newR.AcceptChanges();
                    return;
                case DataRowState.Added:
                    for (int i = nCol - 1; i >= 0; i--) {
                        newR[i] = r[i];
                    }
                    t.Rows.Add(newR);
                    return;
                case DataRowState.Modified:
                    for (var i = nCol - 1; i >= 0; i--) {
                        newR[i] = r[i, DataRowVersion.Original];
                    }

                    t.Rows.Add(newR);
                    newR.AcceptChanges();
                    for (int i = nCol - 1; i >= 0; i--) {
                        newR[i] = r[i];
                    }
                    return;
                case DataRowState.Deleted:
                    for (var i = nCol - 1; i >= 0; i--) {
                        newR[i] = r[i, DataRowVersion.Original];
                    }
                    t.Rows.Add(newR);
                    newR.AcceptChanges();
                    newR.Delete();
                    return;
            }
        }


         
         /// <summary>
         /// Create a concatanation of all columns  value of a r
         /// </summary>
         /// <param name="r"></param>
         /// <param name="columns"></param>
         /// <returns></returns>
         private static string hashColumns(DataRow r,string []columns) {
	         var keys = (from string field in columns select r[field].ToString());
	         return String.Join("§",keys);
         }

         
       
         /// <summary>
         /// Copy All fields of source row into dest row
         /// </summary>
         /// <param name="source"></param>
         /// <param name="dest"></param>
         static void copyRow(DataRow source, DataRow dest) {
	         DataTable destTable = dest.Table;
	         DataTable sourceTable = source.Table;
	         foreach (DataColumn dc in destTable.Columns) {
		         if (!sourceTable.Columns.Contains(dc.ColumnName)) continue;
		         if (!dc.IsReal()) continue; //if (IsTemporary(DC))continue;
		         //if (QueryCreator.IsPrimaryKey(destTable, dc.ColumnName)) continue;
		         if (!String.IsNullOrEmpty(dc.Expression)) continue;
		         var ro = dc.ReadOnly;
		         if (ro) dc.ReadOnly = false;
		         try {
			         dest[dc.ColumnName] = source[dc.ColumnName];
		         }
		         catch (Exception e) {
			         ErrorLogger.Logger.markException(e,"copyRow");
		         }
		         if (ro) dc.ReadOnly = true;
	         }
         }

        /// <summary>
        /// Merge source into destTable searching one row at a time using Dictionary on keyColumns
        /// </summary>
        /// <param name="destTable"></param>
        /// <param name="sourceTable"></param>
        private static void mergeIntoTableWithDictionary(DataTable destTable, DataTable sourceTable) {
	        var handle2 = MetaProfiler.StartTimer("MergeIntoDataTableWithDictionary * " + sourceTable.TableName);
	        var destRows = new Dictionary<string, DataRow>();
            string []keys = (from DataColumn c in destTable.PrimaryKey select c.ColumnName).ToArray();
	        foreach (DataRow r in destTable.Rows) {
	            destRows[hashColumns(r, keys)] = r;
	        }

	        foreach (DataRow dr in sourceTable.Rows) {
	            string hashSource = hashColumns(dr, keys);
                if(destRows.TryGetValue(hashSource, out var destRow)) {
                    destRow.BeginEdit();
                    copyRow(dr, destRow);
                    destRow.EndEdit();
                }
                else {
                    destRow = destTable.NewRow();
                    copyRow(dr, destRow);
                    destTable.Rows.Add(destRow);
                    destRows[hashSource] = destRow;
                }
                destRow.AcceptChanges();
	        }

            MetaProfiler.StopTimer(handle2);
	    }



	    /// <summary>
		/// Merge ToMerge rows into OutTable. Tables should have a primary key
		///  set in order to use this function.
		/// </summary>
		/// <param name="outTable"></param>
		/// <param name="toMerge"></param>
		public static void MergeDataTable(DataTable outTable, DataTable toMerge) {	
			var handle= MetaProfiler.StartTimer("MergeDataTable");
            if ((outTable.TableName != toMerge.TableName) &&
                (toMerge.TableName == "Table")) {
                toMerge.TableName = outTable.TableName;
                toMerge.Namespace = outTable.Namespace;
            }
           
			if (toMerge.TableName== outTable.TableName && toMerge.Namespace==outTable.Namespace && outTable.Rows.Count==0) {
			    DataSetUtils.MergeIntoEmptyDataTable(outTable, toMerge);
			}
			else {
				var index = outTable?.DataSet.getIndexManager()?.getPrimaryKeyIndex(outTable);
				if (index != null) {
					mergeIntoTableWithIndex(outTable, toMerge,index);
				}
				else {
					if (toMerge.Rows.Count > 100 || outTable.Rows.Count > 100) {
						mergeIntoTableWithDictionary(outTable, toMerge);
					}
					else {
						mergeIntoTableRowByRow(outTable, toMerge);

					}
				}
			}

            MetaProfiler.StopTimer(handle);
		}

		/// <summary>
		/// Merge source into destTable searching one row at a time using Select(filterKey)
		/// </summary>
		/// <param name="destTable"></param>
		/// <param name="sourceTable"></param>
		private static void mergeIntoTableRowByRow(DataTable destTable, DataTable sourceTable) {
			var handle2 = MetaProfiler.StartTimer("MergeIntoDataTableRowByRow * " + sourceTable.TableName);			
			foreach (DataRow dr in sourceTable.Rows) {
				//OutTable.ImportRow(DR);
				//OutTable.LoadDataRow(DR.ItemArray, true);
				var filter = QueryCreator.FilterColumnPairs(
					dr,
					destTable.PrimaryKey,
					destTable.PrimaryKey,
					DataRowVersion.Default,
					forPosting: false);
				DataRow myDr;
				var found = destTable.Select(filter.toADO());
				if (!(filter is null) && (!filter.isTrue()) && (found.Length > 0)) {
					myDr = found[0];
					myDr.BeginEdit();
					copyRow(dr, myDr);
					myDr.EndEdit();
				}
				else {
					myDr = destTable.NewRow();
					copyRow(dr, myDr);
					destTable.Rows.Add(myDr);
				}

				myDr.AcceptChanges();
			}
            MetaProfiler.StopTimer(handle2);
		}


		/// <summary>
		/// Merge source into destTable searching one row at a time using Dictionary on keyColumns
		/// </summary>
		/// <param name="destTable"></param>
		/// <param name="sourceTable"></param>
		private static void mergeIntoTableWithIndex(DataTable destTable, DataTable sourceTable, IMetaIndex index) {
		    var handle2 = MetaProfiler.StartTimer("MergeIntoDataTableWithIndex * " + sourceTable.TableName);
		    
		    foreach (DataRow dr in sourceTable.Rows) {
			    string hashSource = index.hash.get(dr);
			    var destRow = index.getRow(hashSource);
			    if(destRow!=null) {
				    destRow.BeginEdit();
				    copyRow(dr, destRow);
				    destRow.EndEdit();
			    }
			    else {
				    destRow = destTable.NewRow();
				    copyRow(dr, destRow);
				    destTable.Rows.Add(destRow);
			    }
			    destRow.AcceptChanges();
		    }

            MetaProfiler.StopTimer(handle2);
	    }
    }
}
