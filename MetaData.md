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
Tuttavia ci si può sempre dimenticare e se si ha a che fare con molte tabelle, diventa un'odissea, peggio ancora se ad un certo punto si volesse aggiungere un campo ad una tabella esistente e gli si volesse attribuire un default. Il metodo consigliato è quindi quello di scrivere queste istruzioni nel metadato di ogni tabella, ossia la classe derivata da MetaData specifica per quella tabella. Fatto questo, sarà possibile ad esempio eseguire un ciclo del tipo:

	foreach(DataTable t in DS.Tables) {
		Dispatcher.get(t.TableName).SetDefaults(t);
	}

oppure, usando una extension del framework:

	DS._forEach(t=>Dispatcher.get(t.TableName).SetDefaults(t));


Questo una volta per tutte quando si crea il DataSet.



