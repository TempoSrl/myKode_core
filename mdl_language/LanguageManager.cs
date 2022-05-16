using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mdl_language.Properties;


namespace mdl_language {
    static class StringCompiler {
        public static string repl(this string s, string placeHolder, string value) {
            return s.Replace("{" + placeHolder + "}", value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class LanguageManager {

        /// <summary>
        /// Nella tabella {table} non è stata trovata alcuna riga.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string noRowFoundInTable(string tableName) {
            return Resources.noRowFoundInTable.repl("table", tableName)+"\r\n";
        }

        /// <summary>
        /// Nessun oggetto è stato trovato
        /// </summary>
        public static string noObjectFound => Resources.noObjectFound;

        /// <summary>
        /// Allegati troppo grande
        /// </summary>
        public static string attachmentTooBig => Resources.attachmentTooBig;


        /// <summary>
        /// Nessuna condizione è stata usata.
        /// </summary>
        public static string noConditionUsed => Resources.noConditionUsed;

        /// <summary>
        /// Elimina
        /// </summary>
        public static string Delete => Resources.Delete;

        
        /// <summary>
        /// La condizione di ricerca impostata era: {filter}.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string conditionSetWas(string filter) {
            return Resources.conditionSetWas.repl("filter", filter)+"\r\n";
        }

        /// <summary>
        /// Nome Elenco: \'{listingType}\'.
        /// </summary>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static string listNameIs(string listType) {
            return Resources.listNameIs.repl("listingType", listType)+"\r\n";
        }

        
        /// <summary>
        /// Annulla
        /// </summary>
        public static string cancel => Resources.cancel;

        /// <summary>
        /// La voce selezionata non poteva essere scelta.
        /// </summary>
        public static string couldNotSelectRow => Resources.couldNotSelectRow;

        /// <summary>
        /// Errore
        /// </summary>
        public static string ErrorTitle => Resources.ErrorTitle;

        /// <summary>
        /// Errore nel caricamento del metadato {unaliased} è necessario riavviare il programma.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string errorLoadingMeta(string table) {
            return Resources.errorLoadingMeta.repl("unaliased", table);
        }

        /// <summary>
        /// La tabella {tableName} contiene dati non validi. Contattare il servizio di assistenza.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string invalidDataOnTable(string table) {
            return Resources.invalidDataOnTable.repl("tableName", table);
        }

        /// <summary>
        /// Cancello la riga selezionata dalla tabella {name}({tableName})
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string deleteSelectedRowFromTable(string name, string tableName) {
            return Resources.deleteSelectedRowFromTable.repl("name", name).repl("tableName", tableName);
        }

        /// <summary>
        /// A command invalidated a transaction (replace cmd)
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string cmdInvalidatedTransaction(string cmd) {
            return Resources.cmdInvalidatedTransaction.repl("cmd", cmd);
        }

        public static string errOpeningDuringSave => Resources.errOpeningDuringSave;
        
        
        public static string doSysCmdError(string cmd, string res) {
            return Resources.doSysCmdError.repl("cmd", cmd).repl("res", res);
        }
        

        /// <summary>
        /// Errore aprendo la connessione
        /// </summary>
        public static string errorOpeningConnection => Resources.errorOpeningConnection;
        /// <summary>
        /// Conferma
        /// </summary>
        public static string confirmTitle => Resources.confirmTitle;

        /// <summary>
        /// La transazione corrente non è più valida
        /// </summary>
        public static string noValidTransaction => Resources.noValidTransaction;

        /// <summary>
        /// Annullo l\'inserimento dell\'oggetto {name} nella tabella {tableName}
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string insertCancelOnTable(string name, string tableName) {
            return Resources.insertCancelOnTable.repl("name", name).repl("tableName", tableName);
        }

        
        /// <summary>
        /// Impossibile eliminare l'oggetto.
        /// </summary>
        public static string rowNotFound => Resources.rowNotFound;

        public static string readObjArrayError(string cmd) {
            return Resources.readObjArrayError.repl("query", cmd);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string deleteFromTable(string name, string tableName) {
            return Resources.deleteFromTable.repl("name", name).repl("tableName", tableName);
        }

        /// <summary>
        /// Impossibile eliminare l'oggetto.
        /// </summary>
        public static string cantDeleteObject => Resources.cantDeleteObject;

        /// <summary>
        /// Errore nell\'esecuzione del comando {command}
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string errorRunningCommand(string cmd) {
            return Resources.errorRunningCommand.repl("command", cmd);
        }

        /// <summary>
        /// Non sono riuscito a collegare la riga alla tabella {tableName} nel metadato {name}
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string couldNotLinkTable(string name, string tableName) {
            return Resources.couldNotLinkTable.repl("name", name).repl("tableName", tableName);
        }

        /// <summary>
        /// La connessione al db è stata interrotta. E' necessario disconnettersi, ripristinare la rete e riconnettersi al db.
        /// </summary>
        public static string dbConnectionInterrupted => Resources.dbConnectionInterrupted;

        /// <summary>
        /// Errore eseguendo il comando {cmd}\r\nE\' necessario chiudere la maschera.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string errorRunningCommandCloseWindow(string cmd) {
            return Resources.errorRunningCommandCloseWindow.repl("cmd", cmd);
        }

        /// <summary>
        /// (vuoto)
        /// </summary>
        public static string emptyWithinPar => Resources.EmptyWithinPar;

        /// <summary>
        /// Modificato da {user}
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string modifiedBy(string user) {
            return Resources.modifiedBy.repl("user", user);
        }

        /// <summary>
        /// Creato da {user}
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string createdBy(string user) {
            return Resources.createdBy.repl("user", user);
        }

        /// <summary>
        /// Modificato il {data}
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string modifiedAt(DateTime data) {
            return Resources.modifiedAt.repl("data", data.ToString("G"));
        }

        /// <summary>
        /// Creato il  {data}
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string createdAt(DateTime data) {
            return Resources.createdAt.repl("data", data.ToString("G"));
        }

        /// <summary>
        /// Informazioni sull'oggetto
        /// </summary>
        public static string infoAboutObject => Resources.InfoAboutObject;

        /// <summary>
        /// E' stato premuto il tasto inserisci copia. Si desidera davvero creare una copia dei dati già salvati?
        /// </summary>
        public static string insertCopyConfirm => Resources.InsertCopyConfirm;

        /// <summary>
        /// Correggi
        /// </summary>
        public static string editLable => Resources.EditLable;

        /// <summary>
        /// Cancella
        /// </summary>
        public static string deleteLable => Resources.DeleteLable;

        /// <summary>
        /// Aggiungi
        /// </summary>
        public static string addLabel => Resources.AddLabel;

        /// <summary>
        /// (Ricerca)
        /// </summary>
        public static string searchWithinPar => Resources.SearchWithinPar;

        /// <summary>
        /// Un campo chiave non può essere vuoto o duplicato.
        /// </summary>
        public static string errorEmptyKey => Resources.ErrorEmptyKey;

        /// <summary>
        /// Un determinato campo non può essere vuoto.
        /// </summary>
        public static string errorEmptyField => Resources.ErrorEmptyField;

        /// <summary>
        /// Campo troppo lungo
        /// </summary>
        public static string stringTooLong => Resources.StringTooLong;

        /// <summary>
        /// Data non valida
        /// </summary>
        public static string invalidDate => Resources.invalidDate;

        /// <summary>
        /// Elenco {name}
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string listOfName(string name) {
            return Resources.ListOfName.repl("name", name);
        }

        /// <summary>
        /// Nessun elemento trovato.
        /// </summary>
        public static string noElementFound => Resources.NoElementFound;

        /// <summary>
        /// Apri documento collegato
        /// </summary>
        public static string OpenRelatedDocument => Resources.OpenRelatedDocument;

        public static string selectLabel => Resources.SelectLabel;

        public static string insertLabel => Resources.InsertLabel;
        public static string insertCopyLabel => Resources.InsertCopyLabel;
        public static string setSearchLabel => Resources.SetSearchLabel;
        public static string doSearchLabel => Resources.DoSearchLabel;
        public static string saveLabel => Resources.SaveLabel;
        public static string deleteCancelLabel => Resources.DeleteCancelLabel;
        public static string refreshLabel => Resources.RefreshLabel;
        public static string createTicketLabel => Resources.CreateTicketLabel;
        public static string notesLabel => Resources.NotesLabel;
        public static string dataDictionary => Resources.DataDictionary;
        public static string createLastMod => Resources.CreateLastMod;
        public static string noRowFound => Resources.NoRowFound;

        /// <summary>
        /// Tabella:{searchTable}\n\rFiltro applicato:{checkfilter}
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string tableFilterApplied(string table, string filter) {
            return Resources.TableFilterApplied.repl("searchTable", table).repl("checkfilter", filter);
        }

        
        public static string confirmDeleteListType(string listType) {
            return Resources.confirmDeleteListType.repl("listType", listType);
        }
        public static string errorDeletingListType(string listType) {
            return Resources.errorDeletingListType.repl("listType", listType);
        }


        
        public static string moreThanOneRowInTable(string tableName) {
            return Resources.moreThanOneRowInTable.repl("table", tableName);
        }

        public static string tableMiddleInMemory(string middle) {
            return Resources.TableMiddleInMemory.repl("middle", middle);
        }

        public static string waitForCommandCompletion => Resources.waitForCommandCompletion;

        /// <summary>
        /// Attenzione
        /// </summary>
        public static string warningLabel => Resources.WarningLabel;
        public static string valueRequired => Resources.valueRequired;
        public static string impossibleToDeleteConditions => Resources.impossibleToDeleteConditions;
        public static string impossibleToSaveCondition => Resources.impossibleToSaveCondition;
        public static string errorSelectingRow => Resources.errorSelectingRow;

        
        
        
        public static string operatorRequired => Resources.operatorRequired;
        
        public static string childFormStillOpened => Resources.childFormStillOpened;
        public static string adviceLabel => Resources.AdviceLabel;
        public static string cantCloseWait => Resources.cantCloseWait;
        public static string stillNotOpenedForm => Resources.stillNotOpenedForm;
        public static string waitForOperationEnd => Resources.waitForOperationEnd;
        public static string windowMustBeClosed => Resources.windowMustBeClosed;
        public static string selectedRowNotPresent => Resources.selectedRowNotPresent;

        public static string errorShowingForm => Resources.errorShowingForm;
        public static string primaryKeyConflict => Resources.primaryKeyConflict;
        public static string wrongData => Resources.wrongData;
        public static string unsavedDataWarn => Resources.unsavedDataWarn;
        public static string selectFile => Resources.selectFile;
        public static string errorExportingExcel => Resources.errorExportingExcel;
        public static string badExport => Resources.badExport;
        public static string emptyExport => Resources.emptyExport;
        public static string overallTotal => Resources.overallTotal;
        public static string bugOfficeXp => Resources.bugOfficeXp;
        public static string askSaveChanges => Resources.askSaveChanges;
        public static string confirmDeleting => Resources.confirmDeleting;
        public static string dbFieldName => Resources.dbFieldName;
        public static string listColPos => Resources.listColPos;
        public static string colRequired => Resources.colRequired;
        
        
        


        static string capitalize(string s,bool leaveUnchanged) {
            if (leaveUnchanged) return s;
            if (s == null || s.Length == 0) return s;
            if (s.Length == 1) return s.ToUpperInvariant();
            return s[0].ToString().ToUpperInvariant() + s.Substring(1).ToLowerInvariant();
        }

        public static string translate(string keycode,bool capitalizeFirst=false) {
            switch (keycode) {
                case "result": return capitalize(Resources.result,capitalizeFirst);
                case "description": return capitalize(Resources.description,capitalizeFirst);
                case "selectFile": return capitalize(Resources.selectFile,capitalizeFirst);
                case "dataFolder": return capitalize(Resources.dataFolder,capitalizeFirst);
                case "total": return capitalize(Resources.total,capitalizeFirst);
                case "title": return capitalize(Resources.title,capitalizeFirst);
                case "saving": return capitalize(Resources.saving,capitalizeFirst);
                case "delete": return capitalize(Resources.Delete,capitalizeFirst);
                case "dbTableName": return capitalize(Resources.dbTableName,capitalizeFirst);
                case "list": return capitalize(Resources.list,capitalizeFirst);
                case "columnName": return capitalize(Resources.columnName,capitalizeFirst);
                case "width": return capitalize(Resources.width,capitalizeFirst);
                case "visible": return capitalize(Resources.visible,capitalizeFirst);
                case "fontName": return capitalize(Resources.fontName,capitalizeFirst);
                case "fontSize": return capitalize(Resources.fontSize,capitalizeFirst);
                case "bold": return capitalize(Resources.bold,capitalizeFirst);
                case "italic": return capitalize(Resources.italic,capitalizeFirst);
                case "underline": return capitalize(Resources.underline,capitalizeFirst);
                    
                    
                default: return keycode; 
            }
        }
        
    }


}
