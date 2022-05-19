# GetData

GetData è la classe che si occupa di leggere i dati di interi DataSet, ed è il metodo da preferire a questo scopo. Si parte da un DataSet in cui si identifica una tabella principale, che rappresenta l'oggetto "fulcro" del set di dati considerato. Tipicamente è l'oggetto principale dell'editing di una maschera. Poi ci sono le tabelle child che definiamo "subentità" ove la chiave della tabella child contenga interamente la chiave della tabella parent.
E' una condizione indispensabile e indica a MDL che quella tabella rappresenta un dettaglio della principale. Le subentità possono a loro volta avere proprie subentità. 
**E' necessario che esista nel DataSet una relazione che colleghi la tabella principale alle subentità.**



## Inizializzazione
GetData si inizializza con il metodo

	string InitClass(DataSet DS, IDataAccess Conn, string PrimaryTable);


che richiede un DataSet, un DataAccess ed il nome della tabella principale. GetData nel suo funzionamento (metodo Get) analizza la struttura del DataSet percorrendone le relazioni e stabilisce un ordine in cui effettuare la lettura delle tabelle, cercando di raggruppare più letture possibile per minimizzare i roundtrips. In ogni roundtrip infatti saranno effettuate letture di più tabelle possibile a partire da quelle che si sono già lette.
Sono lette tutte le tabelle **child** della tabella principale, e poi le child delle child e così via, e anche le tabelle **parent** di tutte le tabelle coinvolte, percorrendo le DataRelation del DataSet. Ogni DataRelation è percorsa una sola volta ed ogni tabella è letta una sola volta (onde evitare che si creino dei cicli), pertanto se si intende leggere le righe di una tabella percorrendo relazioni diverse, sarà necessario utilizzare un alias di quella tabella.


Discorso a parte è fatto per le tabelle marcate come "cached", con l'estensione

	CacheTable(this DataTable T, object filter = null, string sort = null, bool addBlankRow = false)

che stabilisce che la tabella sarà letta alla prima occasione utile, con un certo filtro *filter* ed eventualmente aggiungendo come prima riga nella tabella una riga "vuota" (utile in alcuni casi per le tendine)


**Nota bene**
Attenzione, è bene capire che GetData legge le righe dal database osservando la struttura del DataSet. Disegnando il DataSet si stabilisce implicitamente quali tabelle e quali righe saranno lette. Se ci sono tabelle non collegate alla tabella principale né indirettamente né direttamente, **non saranno lette** a meno che non siano marcate come **cached**, nel qual caso sarà effettuata una lettura incondizionata e **UNICA** (solo una volta) con il filtro eventualmente indicato.
Ulteriore ottimizzazione è effettuata per le tabelle di cui è effettuata una lettura senza filtri. Queste vengono marcate automaticamente da GetData in modo tale che non verrano lette nuovamente nello stesso DataSet.

Operativamente per leggere un DataSet si opera in due step: 

1) si leggono una o più righe nella tabella principale, queste saranno il set di partenza da cui sarà effettuata la scansione "a spirale" sul DataSet e sul database.
2) si invoca la lettura di tutto il dataset a partire dalle righe lette nella fase 1


## Step 1 - lettura sulla tabella principale

Per effettuare il passo 1) ci sono varie opzioni:

	 async Task  GetPrimaryTable(MetaExpression filter)

Questo metodo svuota la tabella principale, legge le tabelle *cached* e riempie la tabella principale con le righe del database che sodisfano la condizione *filter* stabilita.

	async Task SearchByKey(DataRow R);

Questo metodo è simile al precedente, con la sola differenza che non svuota la tabella principale e dunque è possibile invocarlo più volte per riempire il DataSet in modo incrementale. 


### ClearTables e ReadCached
Quando si è finito di operare con i dati di un Dataset e si vuole iniziare con un altro set di righe della tabella principale, senza perdere le righe delle tabelle cached, si può richiamare il metodo

	void ClearTables()

di GetData, che svuoterà tutte le tabelle escluse quelle cached e quelle lette senza filtri. 
All'opposto, se si vogliono leggere tutte le tabelle marcate come Cached, si può richiamare il metodo

	async Task ReadCached()

che leggerà tutte le tabelle marcate come cached escluse quelle che sono state già lette in precedenza nello stesso DataSet. Ovviamente chiamarla più volte non sortirà alcun effetto dopo la prima volta. Le tabelle sono lette, al solito, in modo ottimizzato, in particolare con un unico roundtrip, infatti è ReadCached utilizzi a tale scopo il metodo MultipleSelect del [DataAccess](DataAccess.md).




### Passo 1 caso particolare per i dataset di dettaglio

	async Task StartFrom(DataRow Start);

Questo metodo serve a copiare una riga di una tabella principale con tutte le sue subentità dirette e indirette da un DataSet di una maschera "parent" ad un DataSet di un DataSet "di dettaglio" in modo da poterne effettuare l'editing in un DataSet separato con una maschera distinta. E' lo step 1 da adottare per le maschere di dettaglio, che di norma condividono parte delle righe editate con la maschera principale da cui sono aperte. Questo metodo quindi NON legge dati da db, copia solo dei dati da un dataset ad  un altro.

Al termine dell'editing, si chiamerà poi il metodo  SaveChanges( DataRow sourceRow, DataRow destRow) della classe MetaModel, che riporterà nel DataSet parent le modifiche effettuate nel DataSet child (solo relativamente alle entità e subentità). Seguendo questo schema è possibile anche effettuare modifiche su dettagli di secondo o terzo livello, purché in ogni DataSet intermedio ci siano tutte le entità e subentità oggetto della modifica, altrimenti non potrebbero essere retropropagate al DataSet principale.

E' da notare che quando si riempie un DataSet con StartFrom, le tabelle che poi vanno lette sono solo le tabelle secondarie (le parent e le cached), poiché le entità e le subentità sono state già "importate" dal DataSet contenente la riga Start.


## Step 2 - Lettura di tutto il resto del DataSet

	async Task<bool> Get(bool onlyperipherals=false, DataRow OneRow=null)

Legge tutte le tabelle (esclusa la tabella principale) a partire dalle righe contenute nella tabella principale, come descritto in precedenza. Se il opzionale parametro onlyperipherals è true, non sono lette le entità e subentità ma solo le tabelle parent delle stesse.
Se il parametro opzionale OneRow è specificato, deve essere una riga della tabella principale, e saranno lette solo le righe a partire da quella specifica riga indicata e non da tutte quelle contenute nella tabella principale.




## Tabelle ALIAS in lettura

Come abbiamo anticipato, durante la scansione del DataSet (metodo Get) ogni relazione è percorsa al più una volta. Cosa succede quindi se una tabella è parent di due tabelle presenti nel DataSet, magari due tabelle di dettaglio diverse? solo una delle due sarà percorsa, mentre le righe derivanti dall'altra relazione rimarranno inesplorate, non lette. Come operare in questo caso? E' possibile inserire nel DataSet uno o più "alias" in lettura di quella tabella o vista del database. 
Gli alias possono avere un TableName a piacere, però è necessario, prima di inizializzare il DataSet, marcarli con l'estensione

	setTableForReading(this DataTable,  string tablename)

del DataTable, ad esempio:
	DataSet ds;
	..
	ds.Tables["bilancioentrata"].setTableForReading("bilancio");
	ds.Tables["bilanciospesa"].setTableForReading("bilancio");


in questo caso ho supposto di avere due alias della tabella bilancio, bilancioentrata e bilanciospesa. Le righe saranno raccolte seguendo le relazioni esistenti, e lette dalla tabella bilancio, però saranno memorizzate nei due DataTable bilancioentrata e bilanciospesa, in base alla relazione seguita.



## Campi calcolati (uso avanzato)

E' possibile aggiungere ai DataColumn di un DataTable delle colonne che non esistono effettivamente sul database, e che saranno calcolati in qualche modo dopo la lettura. Questi campi sono ignorati durante le operazioni di accesso fisico al database, e sono calcolati subito dopo, in automatico, a seguito di qualsiasi operazione di lettura fisica si effettui.

In particolare, distinguiamo tre categorie di DataColumn:
1) DataColumn con una [MetaExpression](MetaExpression.md) associata
2) DataColumn con una espressione stringa di tipo "tabella.campo" associata "table1.field1"
3) DataColumn che hanno la proprietà Expression valorizzata o il cui ColumnName inizia con ! (punto esclamativo)



In tutti questi casi, saranno **saltati** in fase di accesso al db, inoltre per ogni riga R:

1) se hanno una [MetaExpression](MetaExpression.md) associata, sono calcolati con l'invocazione del metodo apply della MetaExpression su R
2) se hanno una stringa del tipo "parentTable.field" è cercata una relazione parent-child che lega la tabella parentTable alla tabella cui appartiene R. E' quindi presa dalla tabella parentTable la P riga che risulta essere parent di R. Se non è trovata, è preso il valore DbNull, altrimenti è preso il valore del campo field di P


Nota: 
Per associare un'espressione (stringa o MetaExpression) ad un DataColumn C è sufficiente invocare

	C.SetExpression(expr)



## Tabelle ricavate da VISTE (uso avanzato)

E possibile, per ottenere il riempimento di più tabelle contemporaneamente con un'unica operazione di lettura, ricavare il contenuto delle righe di una o più tabelle parent a partire dai valori presenti nella DataTable della vista.
E' un uso molto avanzato e che serve ad ottimizzare l'accesso a tabelle parent quando una tabella di dettagli risulta contenere molte righe (ad esempio più di mille). Non è tuttavia indispensabile.

Per attivare questo meccanismo occore distinguere tre principali elementi:

1) La tabella (T) che rappresenta la tabella principale della vista
2) La vista (V) da cui saranno letti i dati, che si intende contenere tutti i campi di T più altri campi di tabelle parent P1... Pn, e anche i campi chiave di tali tabelle. 
3) Le tabelle parent P1..Pn ove qualsiasi campo di Pi deve essere elencato anche in V

In queste condizioni, quando sarà invocato il metodo Get non saranno lette le tabelle T e P1..Pn ma sarà effettuata solo una lettura da V, e poi automaticamente riempite le righe delle altre tabelle, con un risparmio di almeno N*m+1 letture distinte ove m è il numero di righe lette da V (che coincide con quelle che ci saranno in T).
Dovranno essere inoltre presenti nel DataaSet le relazioni parent-child tra P1..Pn e la tabella T.

La tabella V non deve essere relazionata.

Per impostare questo meccanismo, nelle ipotesi fatte, sarà necessario effettuare queste impostazioni:

	T.ExtendedProperties["ViewTable"] = V;
	P1.ExtendedProperties["ViewTable"] = V;
	..
	..
	Pn.ExtendedProperties["ViewTable"] = V;
	V.ExtendedProperties["RealTable"] = T;

e questo serve a descrive le relazioni tra i DataTable. Occorre a questo punto indicare a GetData la corrispondenza delle colonne per poter effettuare il riempimento. Questo si effettua con delle istruzioni del tipo:


	V.Columns["fieldName1"].ExtendedProperties["ViewSource"]= "T.fieldName1"
	V.Columns["fieldName2"].ExtendedProperties["ViewSource"]= "T.fieldName2"
	..
	V.Columns["fieldNameN1"].ExtendedProperties["ViewSource"]= "P1.fieldNameN1"
	V.Columns["fieldNameN2"].ExtendedProperties["ViewSource"]= "P1.fieldNameN2"
	...
	V.Columns["fieldNameN10"].ExtendedProperties["ViewSource"]= "P2.fieldNameN1"
	...

In sostanza per ogni colonna di V bisogna specificare in quale colonna di T, P1.. PN mettere tale valore. I valori delle chiavi  di P1..PN sono presi implicitamente dalla riga in T


	