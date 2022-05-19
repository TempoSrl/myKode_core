# MetaTable

Così come i DataSet genrati da MSDataSetGenerator hanno come tabelle oggetti che derivano istanze di TypedTableBase\<R\> ove R deriva da DataRow, i DataTable generati da [HDSGene](HDSGene.rm), hanno per tabelle oggetti istanze di MetaTableBase\<R\>, ove: 
- MetaTableBase deriva da TypedTableBase
- R deriva da MetaRow, che a sua volta deriva da DataRow


metodi e proprietà aggiunti in MetaTableBase:;

	MetaTableRef<R> _as(string alias) // crea un riferimento per effettuare un join
	int Count;	                      // il numero di righe nel DataTable, incluse quelle cancellate
	List<R> all();                    // restituisce tutte le righe del DataTable, incluse quelle cancellate
	List<R> allCurrent()              // tutte le righe del DataTable escluse quelle cancellate


	DataColumn defineColumn(string ColumnName, Type ColumnType, bool allowDBNull = true, bool ReadOnly = false)

aggiunge un DataColumn al DataTable


	void defineKey(params string[] fields)

definisce la chiave primaria del DataTable


	
	R First(MetaExpression filter, ISecurity env = null, string sort = null)

Ottiene la prima riga che sodisfa un certo criterio, null se non viene trovata
	
	string ColumnNameList()

ottiene la stringa che elenca tutti i campi delle tabella esclusi quelli calcolati 

	R[] Sort(string sort, DataViewRowState rv = DataViewRowState.CurrentRows)

Ottiene un elenco ordinato delle righe avente un certo stato

	R[] Filter(MetaExpression filter, ISecurity env = null, string sort = null, bool all = false) 

Ottiene un elenco filtrato ed ordinato di righe che sodisfano un certo criterio. Se all=true sono restituite anche le righe nello stato deleted 


	Task<R[]> getDetachedRowsFromDb(IDataAccess Conn, MetaExpression filter, int timeout = -1)

Legge delle righe dal database ma senza inserirle nel DataTable

	Task<R[]> detachedSqlRunFromDb(IDataAccess Conn, string sql, int timeout = -1)

Legge delle righe attraverso l'esecuzione di codice sql seguendo come destinazione una tabella avente lo stesso schema

	Task<R[]> get(IDataAccess Conn, MetaExpression filter, int timeout = -1)

Estrae delle righe dal DataTable oppure se non ne trova le legge dal db e le inserisce nel DataTable

	Task<R[]> getFromDb(IDataAccess conn, MetaExpression filter, int timeout = -1)

Legge delle righe dal db e le inserisce nel DataTable senza controllare preliminarmente che vi siano già. Ove vi siano va in violazione di chiave
	
	Task<R[]> sqlRunFromDb(IDataAccess conn, string sql, int timeout = -1)

Legge delle righe nel DataTable attraverso l'esecuzione di un comando sql

	Task<R[]> mergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1)

Legge delle righe da db e le unisce a quelle esistenti nel DataTable, sostituendo quelle esistenti ove  abbiano la stessa chiave

	Task<R[]> safeMergeFromDb( IDataAccess conn, MetaExpression filter, int timeout = -1)
Legge delle righe dal db saltando quelle già esistenti  (quindi non sostituisce le esistenti)

	Task<R[]> sqlSafeMergeFromDb( IDataAccess conn, string sql, int timeout = -1)
Legge delle righe dal db saltando quelle esistenti, eseguendo una query sql

	string TableForReading;
Tabella fisica da cui leggere i dati (vedasi ALIAS in [GetData](GetData.md))

	string TableForPosting;

Tabella fisica in cui scivere i dati 

Questi sono senz'altro i metodi e proprietà principali, per una informazione completa leggere il file [MetaTable](mdl\MetaTable.cs)


# IndexManager, MetaIndex
Ai MetaTable è possibile associare degli indici in memoria, utili se si intende operare con grossi dataset per delle operazioni di ricerche in memoria.
E' possibile definire indici univoci e non univoci, con le classi MetaTableUniqueIndex e MetaTableNotUniqueIndex.
La creazione degli indici è molto semplice e richiede l'esecuzione di una semplice istruzione. L'indice sarà associato al DataTable a cui è essociato, ed è possibile definire un numero indefinito di indici su ogni DataTable.


Il primo passo per creare un indice è ottenere l'IndexManager del DataSet. L'IndexManager è la classe che si occupa di gestire tutti gli indici relativi alle tabelle di un DataSet.
Per creare un IndexManager basta richiarne il costruttore e passargli il DataSet da gestire:

		var IDM = new IndexManager(D);

oppure
		var IDM = D.getCreateIndexManager();



Per creare un indice univoco su una tabella si esegue semplicemente l'instanziazione di un MetaTableUniqueIndex con il suo costruttore, es:
		
		var idx = IDM.checkCreateIndex(table,  new string []{"idasset", "idpiece"},true)

Analogamente se si intende creare un indice non univoco:

		var idx = IDM.checkCreateIndex(table,  new string []{"codeinv"},false)



checkCreateIndex crea l'indice solo se già non ne esiste uno sugli stessi campi. Se si è certi che questo sia vero, su può usare createIndex, con gli stessi parametri.


Per creare un indice sulla chiave primaria, basta richiamare:

		var idx = IDM.createPrimaryKeyIndex(table)



Gli indici sono automaticamente utilizzati nelle ricerche in memoria quando si utilizza una MetaExpression di tipo Mcmp dove i campi coincidano con quelli dell'indice, o con le estensioni f_Eq, f_EqObj, getChildRows, getParentRows, combinazioni di AND di MetaExpression.eq. In sostanza sono adottati in automatico ove possibile, nell'applicazione delle ricerche nei DataTable con le MetaExpression


Quando si crea un indice su una tabella, viene creata un dictionary che associa ad ogni combinazione dei campi dell'indice una (per gli Unique) o più righe (per i NotUnique) della tabella. Ogni volta che si aggiorna il DataTable è automaticamente aggiornato il Dictionary, e questo potrebbe rallentare un poco le modifiche, tuttavia gli accessi avvengono tramite Dictionary quindi con tempi logaritmici. L'opportunità di creare o meno degli indici è da valutare di volta in volta.


# Estensioni su DataSet e affini

La classe DataSetHelper definisce numerose estensioni su DataTable, DataColumns, RowCollection e altre per agevolare l'integrazione con MDL e per aggiungervi alcuni costrutti functional-like altrimeni non disponibili poiché purtroppo molti oggetti ADO.NET non supportano l'interfaccia IEnumerable e pertanto non sono gestibili con il LINQ.

Alcune di queste estensioni aggiungono ai semplici DataTable alcuni metodi già visti nei MetaTable.

Esaminiamone alcuni

		MakeChildOf(this DataRow child, DataRow parent,
				DataTable parentTable=null
			)

Rende la riga child figlia di parent valorizzando i campi relazionati tra le due


	 DataRow[] getChildRows(this DataRow rParent, DataRelation rel);
	 DataRow[] getParentRows(this DataRow rChild, DataRelation rel);
	 DataRow[] getParentRows(this DataRow rChild, string relationName);
	 DataRow[] getChildRows(this DataRow rParent, string relationName);

Sono simili a quelle esposte dal normale DataRow (GetChildRows e GetParentRows) ma utilizzano gli indici ove presenti.



	async Task<DataRow[]> _mergeFromDb(this DataTable T, IDataAccess conn, MetaExpression filter, int timeout = -1)
	 async Task<DataRow[]> _sqlMergeFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1)

Uniscono al DataTable delle righe prese dal database tramite una select o l'esecuzione di un comando sql.
	
	async Task<DataRow[]> _safeMergeFromDb(this DataTable T, IDataAccess conn, MetaExpression filter, int timeout = -1);
	async Task< DataRow[]> _sqlSafeMergeFromDb(this DataTable T, IDataAccess conn, string sql, int timeout = -1);

Come le precedenti due, ma non sovrascrivono le righe eventualmente esistenti nel DataSet


	IIndexManager getCreateIndexManager(this DataSet d) 

è una scorciatoia per creare un IndexManager sul DataSet

	bool HasChanges(this DataTable t) 

verifica se t abbia delle righe modificate, non considerando le false modifiche.

	bool HasChanges(this DataRow R)

analogamente alla precedente, su un singolo DataRow



## Join di enumerables

	IEnumerable<Tuple<r1, r2>> Join<r1, r2>(this IEnumerable<r1> t1, IEnumerable<r2> t2, joinCondition2<r1, r2> joinFun)

Questa estensione consente di effettuare il join di due enumerables e restituisce le tuple degli elementi dei due elementi in join che hanno sodisfatto la condizione di join. La condizione di join è una semplice funzione booleana che ha due elementi in input (uno per ogni enumerable considerato) e stabilisce se la coppia va considerata o meno.


	IEnumerable<Tuple<r1, r2>> LeftJoin<r1, r2>(this IEnumerable<r1> t1, IEnumerable<r2> t2, joinCondition2<r1, r2> joinFun)

Come la precedente, ma restituisce gli elementi di t1 ove non vengano trovati corrispondenti in t2 che sodisfino la condizione.

I metodi precedenti sono disponibili anche con più di due enumerabili, nel qual caso la left join prende sempre i primi n-1 elementi anche se non c'è un match con l'ultimo enumerabile.

	void _forEach<r>(this IEnumerable<r> collection, Action<r> operation)

fornisce il metodo forEach su un generico enumerable

	 void _forEach(this DataSet d, Action<DataTable> operation)

fornisce un metodo forEach che itera su tutte le tabelle di un DataSet

	IEnumerable<DataRelation> Enum(this DataRelationCollection rels)
	IEnumerable<DataColumn> Enum(this DataColumnCollection cols)

Rendono enumerabili le collezioni di DataRelation e DataColumn, per agevolare la programmazione funzionale.

	 TR __do<TR>(this TR item, Action<TR> operation) 

E' un'estensione che prende l'oggetto in a cui si applica, invoca una funzione su esso e restituisce l'oggetto stesso. Serve per agevolare la scrittura di sequenze di operazioni nella programmazione funzionale.

	S[] Map<R, S>(this IEnumerable<R> collection, Func<R, S> mapFunc)

Fornisce la primitiva map per gli enumerabili
		
		delegate void operateOnRowIndex<r>(r R, int i);
		_forEach<TR>(this IEnumerable<TR> collection, operateOnRowIndex<TR> operation)

fornisce la primitiva 	forEach per gli enumerabili, passando alla funzione invocata anche l'indice dell'elemento oltre che l'elemento stesso


		IEnumerable<object>  Pick<TR>(this IEnumerable<TR> collection, string field)

Da un'enumerabile estrae i valori di un campo ottenendo un nuovo enumerabile.



		IEnumerable<TR> _Filter<TR>(this IEnumerable<TR> collection, Predicate<TR> filter) 

Da un'enumerabile estrae solo gli elementi che sodisfano un certo criterio, restituendo un nuovo enumerabile.

		IEnumerable<TR> _Reject<TR>(this IEnumerable<TR> collection, Predicate<TR> filter)

Esattamente l'opposto del precedente, estrae  gli elementi che non sodisfano un certo criterio

		TR _Find<TR>(this IEnumerable<TR> collection, Predicate<TR> filter) where TR:class

Estrae il primo elemento che sodisfa un criterio, o null se non lo trova

		IEnumerable<TR> _Tail<TR>(this IEnumerable<TR> collection) where TR : class

Estrae tutti gli elementi di un IEnumerable tranne il primo

		 IEnumerable<TR> _Initial<TR>(this IEnumerable<TR> collection) where TR : class

Estrae tutti gli elementi di un IEnumerable tranne l'ultimo

		
		bool _Every<TR>(this IEnumerable<TR> collection, Predicate<TR> condition) where TR:class

Stabilisce se tutti gli elementi di un IEnumerable sodisfano un criterio


		bool _Some<TR>(this IEnumerable<TR> collection, Predicate<TR> condition) where TR : class

Stabilisce se almeno un elemento di un IEnumerable sodisfa un criterio

		delegate TR accumulate<TR, TS>(TR result, TS value);
		TR _Reduce<TR,TS>(this IEnumerable<TS> collection, accumulate<TR,TS> accumulator, TR startValue )
		TR _Reduce<TR>(this IEnumerable<TR> collection, accumulate<TR, TR> accumulator)

Fornisce la primitiva reduce sugli enumerabili. Nel secondo caso il primo valore passato alla funzione accumulatore sarà il valore di default del tipo TR



		void _IfExists<r>(this IEnumerable<r> collection, Predicate<r> condition, 
                        Action<r> _then=null, Action _else=null) where r:class
		void _IfNotExists<r>(this IEnumerable<r> collection, Predicate<r> condition,
                   Action _then) where r : class
		
Se esiste/se non esiste un elemento della collezione che sodisfa un predicato, esegue un'azione specificata

		
		 object[] _Select(this object[] source,
             params object[] expr         
            )

Dato un array di oggetti, calcola le espressioni passate, che possono contenere eventualmente degli operatori di raggruppamento, nel qual caso sarà effettuata una group by sulle espressioni non raggruppate.

		object[] _SelectGroupBy(this object[] source,
             MetaExpression[] expressions,
            MetaExpression[] groupBy = null
            )

Dato un array di oggetti, calcola un insieme di espressioni raggruppando su un insieme di altre espressioni

