
# MDL Core C#

***


## Cos'è

MDL (acronimo di myKode Library) è un framework nato per velocizzare e rendere molto efficiente la scrittura di applicazioni rich-client javascript. Per la parte client esiste un progetto javascript che si basa su un backend che deve esporre una specifica interfaccia per l'accesso ai dati. 
Della parte backend sono state sviluppate due versioni distinte, una node-js ed una c#, che è il presente progetto.
MDL non nasce per semplificare i programmini usa e getta, bensì progetti di grandezza media ed enterprise. Infatti per piccoli programmi non ha molto senso preoccuparsi di gestione della sicurezza avanzata, differenziare i layer, validare i dati a più livelli etc. Tuttavia anche per piccoli programmi può essere molto utile

## Peculiarità
Le classi di MDL sono ideate per leggere e scrivere dati su database relazionale utilizzando strumenti molto avanzati, quali:
- la gestione delle autorizzazioni (sulla base di condizioni specifiche row-operation-based e non solo table-operation-based)
- le regole di business (invocate durante il salvataggio fisico dei dati, all'inizio della transazione e subito prima della
  chiusura della transazione)
- la validazione dei dati lato server
- il calcolo delle colonne ad autoincremento non solo "assoluti" ma anche sulla base di campi "prefisso",  es. (tipo/anno/**numero**) in cui il numero è incrementale a parità di tipo e anno
- salvare un insieme di righe con modifiche qualsiasi (inserimenti, modifiche, cancellazioni) con una sola istruzione
- leggere un insieme di dati correlati con una sola istruzione

Ma MDL non si limita a questo, fornisce anche un insieme di strumenti che consentono di interrogare e manipolare collezioni di righe, favorendo l'uso della programmazione funzionale.
La lettura e la scrittura dal database sono db-agnostici, poiché MDL fornisce uno strato di astrazione sul particolare database utilizzato. Questo consente di scrivere applicazioni che possono girare su SQL server o Oracle o mySQL senza alcuna modifica alle funzioni di data-retrieving o di persistenza dei dati. E' anche relativamente semplice sviluppare ed integrare driver per altri tipi di database.

MDL è un framework che consente di effettuare operazioni molto sofisticate, ma al contempo, essendo le funzioni avanzate opzionali, le operazioni di base rimangono semplici. Usando MDL si ha quindi il vantaggio di poter usare lo stesso strumento sia quando è richiesto un accesso elementare ai dati (ad esempio senza una gestione della sicurezza avanzata o senza la gestione di campi ad autoincremento) sia quando è richiesto un accesso più evoluto, come una business logic stratificata e capillare, con variabili di ambiente incluse nelle condizioni di sicurezza e/o nelle regole di business, campi chiave particolarmente complicati.
Salvare una o mille righe di una o più tabelle richiede sempre una sola istruzione, cosi come leggere un dataset di una o più tabelle collegate, senza alcun limite. La complessità delle operazioni è risolta dal framework. 

La filosofia alla base sta nel **descrivere** in partenza le caratteristiche "particolari" (ove ve ne siano) delle operazioni di lettura o di salvataggio e lasciare che sia il framework ad occuparsene. Ossia in genere non si scrive codice per *fare* delle operazioni ma si inviano delle *direttive* alle classi per indicare come operare.

Da un punto di vista architetturale MDL funge da [Facade](https://en.wikipedia.org/wiki/Facade_pattern) per l'accesso evoluto al db. 

E' possibile aggiungere comportamenti custom in diversi modi: 

- invocando le funzioni dedicate di parametrizzazione, di cui alcune consentono di implementare un'IOC avendo come parametri dei delegati.
- derivando opportuni metodi delle classi del framework



## Le assunzioni principali
MDL presume che in memoria i dati vengano mantenuti in DataSet non tipizzati o in alternativa tipizzati in cui il generatore di codice è un tool fornito a corredo (HDSGene), che fornisce un codice molto efficiente e che usa le classi derivate da MetaTable invece che da DataTable. L'uso di tale tool è opzionale.

Ai DataTable e alle DataColumn sono associate delle extended properties (attraverso delle funzioni dedicate) che consentono di delegare il calcolo di campi ad autoincremento anche molto avanzati in modo automatico durante la transazione, inoltre è possibile anche gestire funzioni custom di calcolo durante il salvataggio dei dati, calcolo che sarà invocato durante la transazione.
La memorizzazione in DataSet consente di poter effettuare un salvataggio ottimizzato poiché di ogni riga è implicitamente noto lo stato attuale e anche il valore precedente di ogni colonna se il DataRow è nello stato di "modified", quindi in quel caso saranno modificati solo i campi realmente cambiati e non tutti indiscriminatamente.

### Optimistic locking
Si presuppone l'uso dell'optimistic locking, con dei campi definiti dall'utente ad indicare l'ultima data di modifica e/o l'ultimo utente che ha modificato ogni riga. E' possible anche fornire funzioni di calcolo alternative per la condizione da adottare nell'optimistic locking.

Tipicamente in fase di modifica o cancellazione di ogni riga del database, questa è effettuata a condizione che i campi di optimistic locking della versione in memoria non siano diversi da quelli presenti sul database.

Se l'update o la delete fallisce perché tale condizione non è verificata, è annullata tutta la transazione, e sono aggiornati i dati nel dataset leggendoli dal db.

## Lettura e scrittura sul db "No code"
Il salvataggio e la lettura dei dati avvengono invocando un metodo (PostData.SaveData o GetData.Get) che si occupa di tutti i dettagli, esaminando la struttura del DataSet, le tabelle che vi sono e come sono relazionate, e le modifiche in esso presenti.

Pertanto non è necessario scrivere istruzioni SQL per leggere o scrivere dati, basta disegnare il DataSet (tipicamente con i designer incluso in visual studio), mettere in relazione le tabelle in esso contenute, disegnando quella che è "la vista" della funzione che si sta scrivendo sul database.

Ogni funzione che accede al database tipicamente ha un dataset associato, ma nulla vieta che questo sia utilizzato (a livello di classe) da più funzioni. Tuttavia non è possibile utilizzare la stessa istanza di dataset in thread diversi, in sostanza un'istanza di un dataset va trattata come un set di dati locale e **non come sostituto in-memory di un database**.

## Categorizzazione delle tabelle
Il salvataggio e la lettura dei dati, come abbiamo anticipato, avvengono in base alla struttura del dataset e ad eventuali impostazioni aggiuntive. 

Il meccanismo principale però si basa sulla distinzione tra tre principali categorie di tabelle, che avviene in automatico in base alle relazioni tra le tabelle del dataset e alle chiavi delle tabelle stesse.

In fase di lettura di un DataSet, operata dalla classe GetData, si distinguono:
- tabella (o entità) principale: E' una sola ed è la tabella "principale" oggetto della modifica, quella che fa da perno a tutto il dataset. Tale tabella è indicata quando si instanzia la classe GetData, che si occupa della lettura dei Dataset. Non è detto però che la tabella principale sia effettivamente modificata, infatti è anche possibile che l'oggetto effettivo della modifica (inserimenti, cancellazioni, modifiche) siano le sue tabelle figlie, le subentità
- le subentità: sono tabelle collegate alla tabella principale con una relazione in cui sono *child* e per cui la relazione coinvolga tutta la chiave della tabella parent e la ponga in relazione con i campi chiave della tabella child  (non necessariamente tutti, ossia la chiave della child può includere altri campi chiave non relazionati).
- sono considerate subentità anche le tabelle child delle subentità e cosi via, a patto che ogni tabella child coinvolta nella catena di relazioni abbia le caratteristiche di quella descritta nel punto precedente
- tabelle parent e varie: sono tutte le altre tabelle presenti nel dataset, e possono essere del tutto prive di relazioni con altre tabelle (ad esempio tabelle di configurazione), oppure parent di entità o subentità

Quando sono letti i dati in un Dataset, le righe di tabelle entità e subentità non vengono sovrascritte, al fine di consentire di aggiornare solo le tabelle correlate durante l'interazione dell'utente con una maschera.

In fase di scrittura di un Dataset, come vedremo, la classe PostData effettua gli inserimenti/modifiche/cancellazioni seguendo un ordine dettato dal tipo di modifiche richieste e dalla struttura del DataSet, in modo automatico.

## Accesso al database db-agnostic
MDL fornisce delle classi che nascondono la necessità di coinvolgere i dettagli del dialetto SQL utilizzato nel codice applicativo, isolandolo in specifici driver. L'accesso è asincrono per tutti i database così da avere un'interfaccia uniforme. Ove il driver fisico non supporti l'accesso asincrono, l'esecuzione avverrà tuttavia in modo sincrono.
Le due classi principalmente usate per la gestione dei dati ad alto livello sono [PostData](PostData.md) e [GetData](GetData.md), rispettivamente per salvare tutti i dati presenti in un database e per riempirlo a partire da una o più righe della tabella principale.
E' presente tuttavia anche la classe *[Data Access](DataAccess.md)* in cui sono presenti metodi per accedere ad un database in modo più granulare e customizzato, tra cui:
- metodi per leggere dati ottenendo tabelle 
- metodi per riempire tabelle di dataset esistenti
- metodi per leggere dati in strutture di altro tipo, come dizionari o RowObject, una classe ottimizzata per la lettura veloce dei dati
- metodi per eseguire comandi primitivi (select/insert/update/delete) con comandi sql-dialect-agnostic
- metodi per ottenere l'sql necessario a costruire dei comandi compositi
- metodi per poter eseguire dell'sql generico (ove necessario)  per ottenere set di dati di vario genere. In questo caso è lo sviluppatore che si fa carico della corretta sintassi dell'sql e della sua conformità al dialetto sql usato dal db.
- primitive per la gestione della connessione e delle transazioni (open/close, begin/commit/rollback transaction) con un'interfaccia che consente di rendere i metodi dell'applicazione non dipendenti dal tipo di gestione che si fa della connessione stessa (ad esempio aperta e chiusa per ogni operazione o aperta all'inizio di una sessione e chiusa alla fine). E' da notare che di solito non è necessario usare direttamente queste primitive se si usa la classe [PostData](PostData.md) per salvare i dati.



## [MetaExpression](MetaExpression.md) (ME)
Per poter costruire dei filtri su cui effettuare le interrogazioni sul database, ove il riempimento del dataset tramite GetData non sia sufficiente, o per operare delle operazioni di query o di modifica su singole tabelle, è possibile costruire dei filtri attraverso un set di particolari classi derivanti dalla classe base MetaExpression (ME).
La peculiarità delle ME è che due o più ME si possono combinare con gli operatori aritmetici, booleani o di confronto per ottenere nuove ME.
Una ME è utilizzabile sia come Predicate ossia funzioni che hanno in input un DataRow (o qualsiasi altro oggetto) e restituiscono un booleano, e sia come filtri da utilizzare per selezionare righe dal database. Quindi non sarà mai necessario scrivere delle condizioni SQL manualmente a meno di filtri veramente particolari e che non includono solo i normali operatori.

Altra peculiarità delle MetaExpression è che essendo oggetti possono essere passate da una funzione all'altra, essere manipolate e combinate in vari stadi prima di essere utilizzate per filtrare righe del dataset o del database.
Possono anche essere usate per rappresentare espressioni non booleane, composte a runtime in base a condizioni dinamiche.


# Gestione della sicurezza
MDL prevede una gestione della sicurezza molto avanzata, e tutti i metodi di accesso al database



## Moduli Base

Per la descrizione dettagliata delle funzionalità sinora descritte, si veda:

- [GetData](GetData.md) per la lettura di Dataset
- [PostData](PostData.md) per la scrittura di DataSet
- [Data Access](DataAccess.md) per operare direttamente sul database, ma in modo eventualmente db-agnostic
- [MetaExpression](MetaExpression.md) per come comporre espressioni db-agnostic da usare per reperire dati dal db o effettuare modifiche


# Classe MetaData
La classe MetaData serve a centralizzare la descrizione di tutti gli aspetti di un'entità (tabella), e al contempo separare queste informazioni da tutte le altre. Queste informazioni riguardano le modalità di calcolo dei campi ad autoincremento, la validità dei dati contenuti nella riga unita a eventuali messaggi di errore da restituire, e anche le modalità con cui gli elenchi di tale tabella devono essere presentati all'utente. In sostanza è il punto in cui si può implementare e ricercare qualsiasi comportamento o informazione specifica di quella entità. Per i dettagli si veda [MetaData](MetaData.md)


# Classe MetaTable ed extensions
Per una migliore integrazione tra MDL e i DataSet, e per agevolare l'utilizzo della programmazione funzionale con i DataSet, MDL fornisce una serie di estensioni alle principali classi di System.Data. Sono anche presenti estensioni per impostare le principali proprietà usate dalle classi GetData e PostData, nonché estensioni per invocare direttamente funzioni della classe DataAccess come metodi virtuali dei DataTable.
A proposito, si veda   [MetaTable](MetaTable.md)

# Tool HDSGene, JSON e dati tipizzati
Come si è anticipato, lo strumento consigliato (ma non indispensabile) per la creazione dei DataSet è l'editor visuale di Visual Studio. Tuttavia per una maggiore integrazione con il framework MDL, è possibile personalizzare il codice c# generato quando si salva un DataSet. HDSGene è un tool da usare al posto di MSDataSetGenerator per ottenere un codice che meglio si integra con MDL.
In particolare le tabelle generate hanno MetaTable come classi base, o classi ancora più specifiche in cui il DataTable è visto come una collezione di oggetti tipizzati e che hanno nelle descrizioni dei campi dei testi customizzati in specifiche tabelle (tabledescr,coldescr,colbit,colvalue). 
Il tool HDSGene serve a diversi scopi:
- per i DataSet contenuti in un progetto generico (non una classe MetaDataXXX) genera codice più snello e in cui le tabelle derivano da MetaTable o [tableName]Table ove esista il metadato corrispondente, le quali a loro volta sono collezioni di MetaRow o [tableName]Row (vedi s)
- per i DataSet contenuti in un progetto generico, genera un file JSON nella cartella, ove vi sia la configurazione
- per i DataSet contenuti in progetti di tipo Meta_[tableName] genera il codice per una classe MetaDatatableName che rappresenta l'oggetto tipizzato di una riga di quella tabella ([tableName]Row) e per la tabella che lo conterrà [tableName]Table. Questo è usato poi per le classi
Per una descrizione dettagliata del tool si veda [HDSGene](HDSGene.md)
