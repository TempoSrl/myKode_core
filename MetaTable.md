# MetaTable

Così come i DataSet genrati da MSDataSetGenerator hanno come tabelle oggetti che derivano istanze di TypedTableBase\<R\> ove R deriva da DataRow, i DataTable generati da [HDSGene](HDSGene.rm), hanno per tabelle oggetti istanze di MetaTableBase\<R\>, ove: 
- MetaTableBase deriva da TypedTableBase
- R deriva da MetaRow, che a sua volta deriva da DataRow


metodi e proprietà aggiunti in MetaTableBase:;

	MetaTableRef<R> _as(string alias) // crea un riferimento per effettuare un join
	int Count;	                      // il numero di righe nel DataTable, incluse quelle cancellate
	List<R> all();                    // restituisce tutte le righe del DataTable, incluse quelle cancellate
	List<R> allCurrent()              // tutte le righe del DataTable escluse quelle cancellate

	// aggiunge un DataColumn al DataTable
	DataColumn defineColumn(string ColumnName, Type ColumnType, bool allowDBNull = true, bool ReadOnly = false)

	//definisce la chiave primaria del DataTable
	void defineKey(params string[] fields)

	//Ottiene la prima riga che sodisfa un certo criterio, null se non viene trovata
	R First(MetaExpression filter, ISecurity env = null, string sort = null)

	//ottiene la stringa che elenca tutti i campi delle tabella esclusi quelli calcolati 
	string ColumnNameList()

	//Ottiene un elenco ordinato delle righe avente un certo stato
	R[] Sort(string sort, DataViewRowState rv = DataViewRowState.CurrentRows)

	//Ottiene un elenco filtrato ed ordinato di righe che sodisfano un certo criterio. Se all=true
	//  sono restituite anche le righe nello stato deleted 
	R[] Filter(MetaExpression filter, ISecurity env = null, string sort = null, bool all = false) 

	//Legge delle righe dal database ma senza inserirle nel DataTable
	Task<R[]> getDetachedRowsFromDb(IDataAccess Conn, MetaExpression filter, int timeout = -1)

	//Legge delle righe attraverso l'esecuzione di codice sql seguendo come destinazione una tabella avente lo stesso schema
	Task<R[]> detachedSqlRunFromDb(IDataAccess Conn, string sql, int timeout = -1)

	//Estrae delle righe dal DataTable oppure se non ne trova le legge dal db e le inserisce nel DataTable
	Task<R[]> get(IDataAccess Conn, MetaExpression filter, int timeout = -1)

	//Legge delle righe dal db e le inserisce nel DataTable senza controllare preliminarmente che vi siano già.
	//  Ove vi siano va in violazione di chiave
	Task<R[]> getFromDb(IDataAccess conn, MetaExpression filter, int timeout = -1)

	//Legge delle righe nel DataTable attraverso l'esecuzione di un comando sql
	Task<R[]> sqlRunFromDb(IDataAccess conn, string sql, int timeout = -1)


	//Legge delle righe da db e le unisce a quelle esistenti nel DataTable, sostituendo quelle esistenti ove 
	//  abbiano la stessa chiave
	Task<R[]> mergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1)


	//Legge delle righe dal db saltando quelle già esistenti  (quindi non sostituisce le esistenti)
	Task<R[]> safeMergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1)

	//Legge delle righe dal db saltando quelle esistenti, eseguendo una query sql
	Task<R[]> sqlSafeMergeFromDb( IDataAccess conn, string sql, int timeout = -1)

	//Tabella fisica da cui leggere i dati (vedasi ALIAS in [GetData](GetData.md))
	string TableForReading;

	//Tabella fisica in cui scivere i dati 
	string TableForPosting;


Questi sono senz'altro i metodi e proprietà principali, per una informazione completa leggere il file [MetaTable](mdl\MetaTable.cs)


# MetaIndex
Ai MetaTable è possibile associare degli indici in memoria, utili se si intende operare con grossi dataset per delle operazioni di ricerche in memoria.
E' possibile definire indici univoci e non univoci, con le classi MetaTableUniqueIndex e MetaTableNotUniqueIndex.
La creazione degli indici è molto semplice e richiede l'esecuzione di una semplice istruzione. L'indice sarà associato al DataTable a cui è essociato, ed è possibile definire un numero indefinito di indici su ogni DataTable.


Per creare un indice univoco su una tabella si esegue semplicemente l'instanziazione di un MetaTableUniqueIndex con il suo costruttore:
		new MetaTableUniqueIndex(DataTable t, params string[] keys) 

Analogamente se si intende creare un indice non univoco:

		new MetaTableNotUniqueIndex(DataTable t, params string[] keys) 

Gli indici sono automaticamente utilizzati nelle ricerche in memoria quando si utilizza una MetaExpression di tipo Mcmp dove i campi coincidano con quelli dell'indice, o con le estensioni f_Eq, f_EqObj, getChildRows, getParentRows, combinazioni di AND di MetaExpression.eq. In sostanza sono adottati in automatico ove possibile, nell'applicazione delle ricerche nei DataTable con le MetaExpression



