# MetaData

La classe MetaData è una classe base da cui si deriva una classe per ogni tabella del database, o perlomeno le tabelle che hanno un comportamento particolare o che si differenzia dallo standard in qualche modo.
Tipicamente avremo una classe derivata da MetaData per l'intera applicazione, che ne catturi le peculiarità comuni a tutte le tabelle, quale può essere un limite alla dimensione per tutti i campi che si chiamano in un certo modo, un default per tutti i campi che si chiamano in un altro modo, e così via.
Poi ci sarà una classe derivata da quest'ultima per ogni tabella che si differenzia da questo schema generale.
L'uso delle classi MetaData non è indispensabile, specie se non si intende creare delle righe nelle tabelle nel backend. In quel caso ci sarà probabilmente una classe analoga nel frontend, scritta in javascript.

Se invece nel backend si desiderano effettuare operazioni più complesse e creare set di dati la classe MetaData è lo schema con cui MDL suggerisce di gestire le operazioni sulle tabelle, nelle fattispecie la creazione delle righe (progressivi, default etc), la validazione e la descrizione degli elenchi.

In sostanza il MetaDato nella visione suggerita è il punto in cui si può implementare e ricercare qualsiasi comportamento o informazione specifica della tabella associata.

Il Metadato deve **tassativamente** chiamarsi Meta_[nome tabella], dove nome tabella può anche essere il nome di una vista, e una volta creato sarà reperito automaticamente in base al suo nome quando serve, ammesso che la dll associata si trovi nel percorso di esecuzione.


## Come istanziare un oggetto MetaData
Tipicamente i metadati sono istanziati attraverso la classe EntityDispatcher che li reperisce dalla cartella di esecuzione in base al nome del metadato, per questo è indispensabile rispettare la convenzione sul nome.

Per inizializzare la classe EntityDispatcher, si parte da un IDataAccess (*conn* nell'esempio) e poi si esegue:

	var entityDispatcher = new EntityDispatcher(conn);

quindi si può ottenere un qualsiasi "metadato" invocando

	var meta_Table = entityDispatcher.get("Table");

Dove Table è il nome della tabella implicata. Ove tale metadato non sia stato derivato, quindi non esista la sua dll, sarà restituito un Metadato di default, tramite l'invocazione di entityDispatcher.DefaultMetaData di cui si può fare l'override. Normalmente in un'applicazione è istanziato un Dispatcher per ogni richiesta, ma questo non è costituisce un problema per l'efficienza, poiché la cache di MetaDati che gestisce internamente è una struttura statica quindi condivisa tra tutte le istanze.

E' certamente possibile istanziare un'oggetto derivante da MetaData invocando il suo costruttore, ma questo obbliga a inserire un riferimento al progetto che lo contiene nel file che si sta editando, ed inoltre si invoca un costruttore esplicitamente, che in generale non è mai una good-pratice.


## Impostare dei valori di default 

Quando è creata una nuova riga, sono attribuiti ai suoi campi dei valori di default se questi sono stati impostati in precedenza.
A questo scopo è possibile ridefinire il metodo SetDefaults di MetaData, ad esempio così: 

		public override async Task SetDefaults(DataTable primaryTable, string editType=null){
			await base.SetSefaults(primaryTable, editType);
			primaryTable.setDefault("active", "S")
		}

In pratica nel metodo SetDefaults useremo un insieme di setDefault sul DataTable in input. Senza usare i metadati, si potrebbero scrivere queste stesse istruzioni prima di ogni creazione di righe nel DataTable oppure ogni volta che il DataSet contenente quel DataTable è creato.
Tuttavia ci si può sempre dimenticare e se si ha a che fare con molte tabelle, diventa un'odissea, peggio ancora se ad un certo punto si volesse aggiungere un campo ad una tabella esistente e gli si volesse attribuire un default. 
Il metodo consigliato è quindi quello di scrivere queste istruzioni nel metadato di ogni tabella, ossia la classe derivata da MetaData specifica per quella tabella. Fatto questo, sarà possibile ad esempio eseguire un ciclo del tipo:

	foreach(DataTable t in DS.Tables) {
		Dispatcher.get(t.TableName).SetDefaults(t);
	}

oppure, usando una extension del framework:

	DS._forEach(t=>Dispatcher.get(t.TableName).SetDefaults(t));


Questo una volta per tutte quando si crea il DataSet.
E se a quel punto si  volesse aggiungere un campo ad una tabella e attribuirgli un default, basterebbe integrare il metodo SetDefault del metadato associato a quella tabella, in modo rapido e sicuro.


## Validazione dei dati

Oltre al meccanismo della sicurezza implementato in [Security](Security.md), e della business logic che si può implementare con il metodo callChecks di [PostData](PostData.md), c'è anche un meccanismo collegato direttamente ai metadati, e che nasce soprattutto per le applicazioni rich-client, ma all'occorrenza potrebbe essere utile anche in un backend.


	 bool IsValid(DataRow r, out string errMess, out string errField)

questo metodo serve a validare una riga di una tabella prima di salvarla e può restituire, ove ci siano problemi, un messaggio in errMess ed il campo che ha determinato il problema in errField. In questa versione non è richiamata da alcun metodo, quindi è solo un suggerimento su dove è possibile inserire tali controlli.



	 


## Creazione nuove righe

Se si desiderano impostare dei campi ad autoincremento o logiche particolari nella creazione di nuove righe di una tabella, è possibile ridefinire il metodo:

	Task<DataRow> GetNewRow(DataTable T, DataRow parentRow=null, string editType=null, string relationName=null) 

ad esempio inserendovi l'invocazione dei metodi SetAutoIncrement,SetSelector, setCustomAutoincrement descritti nella classe [PostData](PostData.md)

Questo metodo in effetti è richiamato da MDL e dovrebbe essere richiamato anche nel codice dell'applicativo ogni volta che bisogna creare una nuova riga di una certa tabella, sul metadato associato a quella tabella. 
T è la tabella in cui va creata la riga, parentRow una eventuale riga parent, e se presente, fa si che i campi della riga creata rispettino la relazione parent-child tra essa e parentRow. 
Se si specifica parentRow e tra la tabella T e parentRow.table vi è più di una relazione, può essere necessario specificare in relationName il nome della relazione da seguire. 

Ad esempio, potremmo avere:

	public override Task<DataRow> GetNewRow(DataTable T, 
                                            DataRow parentRow=null, 
                                            string editType=null, 
                                            string relationName=null) {
            T.Columns["rownum"].SetSelector("yman");
            T.Columns["rownum"].SetSelector("nman");
            T.Columns["rownum"].SetSelector("idmankind");
            T.Columns["rownum"].SetAutoincrement();
            return  base.GetNewRow(ParentRow, T);
	}

In questo caso il campo rownum è un progressivo che viene calcolato a parità di yman,nman,idmankind
Se ci sono due campi ad autoincremento (e non ve ne sono altri) e hanno gli stessi selettori possiamo impostarli sulla tabella anziché sui singoli campi:

	public override Task<DataRow> GetNewRow(DataTable T, 
                                            DataRow parentRow=null, 
                                            string editType=null, 
                                            string relationName=null) {
			T.SetSelector("yman");
			T.SetSelector("nman");
			T.SetSelector("idmankind");
			T.Columns["rownum"].SetAutoincrement();
			T.Columns["idgroup"].SetAutoincrement();
            return  base.GetNewRow(ParentRow, T);
	}

In questo caso rownum e idgroup hanno la stessa modalità di calcolo. Magari uno dei due è chiave mentre l'altro potrà essere eventualmente cambiato in un secondo momento per vari scopi.


## Valori temporanei e definitivi

Quando ci sono dei campi ad autoincremento (quelli marcati con SetAutoincrement) è bene notare che il calcolo del campo sarà effettuato due volte, in due momenti ben distinti:
1) la prima volta quando è invocato il metodo GetNewRow, per far si che i campi implicati rispettino la progressività nell'ambito del contenuto corrente del DataTable. Ad esempio, supponendo di avere un semplice campo incrementale ID, e che la tabella sia vuota, questo potrebbe presumibilmente avere, quando si crea la prima riga, il valore 1.
2) quando è invocato il metodo saveData della PostData. A questo punto è effettuata una query sul database per vedere quale sia il massimo valore del campo ID per quella tabella (nelle ipotesi fatte), e viene aumentato di uno. Questo calcolo avviene durante la transazione.

Se la tabella ove è situato un campo ad autoincremento C ha delle tabelle child, e il campo C è nella relazione parent-child, PostData assume che anche il valore del campo relazionato con C va cambiato in coerenza col nuovo valore calcolato nel passo 2. E cosi via ricorsivamente nelle tabelle child sottostanti.

Il dato che viene effettivamente usato per il salvataggio dei dati è quello del passo 2, ossia il valore del passo 1 è solo temporaneo, tra l'altro nulla vieta che altri processi stiano cercando di salvare in quella tabella, o che tra l'invocazione del metodo GetNewRow e quello del salvataggio non incorra un tempo anche rilevante.
Il valore calcolato nel passo 1 quindi serve solo a non avere delle violazioni di chiavi nel DataSet stesso, ma non ha nulla a che fare con il valore che sarà effettivamente salvato, e che comparirà nel DataSet solo al termine del salvataggio.

E' bene dunque non mostrare tale dato all'utente sin quando il dato non viene effettivamente salvato per non ingenerare confusione nel vederlo poi cambiare all'atto del salvataggio.


### Problema potenziale su salvataggio righe multiple

Esaminiamo un problema che può nascere se si salvano contemporaneamente un grande numero di righe nella stessa transazione.

Osserviamo che qualora si desiderino salvare molte righe, questo meccanismo un due step (peraltro inevitabili per i motivi suddetti) possa generare un problema, che ora esaminiamo:
Supponiamo che sul database la tabella T contenga 10 righe, con chiave ID che va da 1 a 10.
Supponiamo di voler salvare 15 righe contemporaneamente, ossia di aver messo in un DataSet, nel DataTable della tabella T, 15 righe. Nel DataSet saranno pertanto numerate, se create con il semplice GetNewRow, da 1 a 15.
Quando viene salvata la prima riga, quella che ha temporaneamente il valore di chiave 1, è letto il max(ID) dal database, che è 10, e viene aumentato di uno, ottenendo 11, e viene usato questo come valore definitivo per quell'ID. Ma questo ID è già presente nel DataTable come valore temporaneo per la riga n.11, pertanto ci sarà una violazione di chiave.

Questo problema si può in definitiva presentare se si cerca di salvare contemporaneamente un numero di righe superiore a quello attualmente contenuto nel database in quella tabella. E' una situazione non frequente, ma che può capitare specie su database con pochi dati e per cui anche con poche righe è possibile che il loro numero sia superiore a quello già presente.
C'è una soluzione per evitare questo genere di situazioni, ed è usare l'estensione:

		void setMinimumTempValue(this DataTable T, string field, int min) 

ad esempio:
		T.setMinimumTempValue("ID",1000)

Lo scopo di questa istruzione, da chiamare **prima** di creare le righe, è di far si che gli id temporanei non siano inferiori al valore dato, nell'esempio 1000. Quindi in questo caso creando 15 righe saranno numerate da 1001 a 1015.

Vediamo cosa succede in questo caso se ci sono 10 righe salvate sul database. Quando viene salvata la prima, è rinumerata da 1001 a 11, poi la seconda da 1002 a 12 e cosi via e non vi è alcun conflitto.

Impostando 1000 come minimumTempValue potremmo avere problemi solo se sul db ci sono tra 1000 e 1015 righe già esistenti. Per andare sul sicuro quindi imposteremo un valore maggiore al numero di righe che ragionevolmente ci aspetteremo che quella tabella possa contenere, ad esempio 99000000.

Si rimarca che questa situazione non si potrà mai verificare se si salva una sola riga della tabella principale alla volta, che è la situazione più frequente.


## Descrizione di un elenco

E' possibile inserire nel metadato la descrizione di come debbano apparire le righe di una certa tabella in un determinato contesto di visualizzazione. Al contesto è associato un nome che in gergo definiamo listingType.

		async Task DescribeColumns(DataTable T, string listingType);


in questa troveremo tipicamente una sequenza di invocazioni del metodo statico della classe MetaData:

		void DescribeAColumn(DataTable T, string colName, string caption,
				int listcolpos, object expression = null)
	
la convenzione è la seguente: prima di visualizzare un elenco della tabella T in un contesto C, MDL invocherà il metodo DescribeColumns del metadato collegato a T passandogli la tabella T e C come listingType. Questa in base al contesto stabilirà l'intestazione delle colonne e l'ordine con le quali devono apparire.
E' possibile in questo contesto anche stabilire che una colonna non è reale ma debba essere calcolata ogni qualvolta è letta una riga dal database, mediante una formula, che potrà essere una stringa o una MetaExpression come descritto nella sezione "Campi calcolati (uso avanzato)"  alla pagina [GetData](GetData.md)

Anche in questo caso quindi abbiamo un metodo che scriviamo e che verrà chiamato tutte le volte che sarà necessario (nel presente caso, la visualizzazione di un elenco)



## Campi calcolati con funzione C#

Qualora il campo expression del punto precedente non sia ancora sufficiente, è possibile indicare a MDL di invocare un ulteriore metodo della classe MetaData,  CalculateFields(DataRow r, string listingType).
Questo metodo ha un ingresso un DataRow e si intende che opera su esso in base ad un certo listingType.
Ma per attivare l'invocazione di questo metodo, è necessario chiamare il metodo:

		ComputeRowsAs(DataTable T, string listingType)

tipicamente inserendo tale chiamata nel metodo DescribeColumns del punto precedente.
Ove si attivi questo comportamento, il metodo CalculateFields del metadato collegato sarà richiamato ogni qualvolta si leggano nuove righe nella tabella indicata.


## Filtro da applicare su un certo elenco

Se ogni qualvolta si utilizza un certo contesto di elenco (listingType) si intende filtrare le righe con un certo filtro, è possibile specificarlo derivando il metodo 

		MetaExpression GetStaticFilter(string listingType)

che dovrà restituire il filtro da applicare ad un certo listingType, o null ove non previsto.
Il filtro indicato sarà applicato "a priori" nell'estrazione dei dati dal database, ossia unito ad altri filtri eventualmente indicati.

## Ordinamento da applicare per un certo elenco

Se ogni qualvolta si utilizza un certo contesto di elenco (listingType) si intende ordinare le righe in un certo modo, è possibile specificarlo derivando il metodo 


		string GetSorting(string listingType)

che dovrà restituire l'ordinamento da applicare ad un certo listingType, o null ove non previsto.


