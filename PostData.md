# PostData

## Metodi principali
La classe PostData è usata per salvare le modifiche presenti in un DataSet (di qualsiasi genere e in qualsiasi quantità) sul database. E' possibile usare questa classe in modo semplice o molto sofisticato a seconda delle esigenze, vediamone innanzitutto i metodi principali:


	Task<string> InitClass(DataSet ds, IDataAccess conn)

InitClass serve a inizializzare la classe con un DataSet e con un DataAccess. E' possibile chiamare questo metodo più volte, a patto che i DataAccess passati come argomento sia riferito allo stesso database. In questo modo sarà possibile salvare i dati di più DataSet simultaneamente con un'unica transazione.

	 Task<ProcedureMessageCollection> SaveData()

SaveData salva tutti i dati presenti nel DataSet e se il salvataggio va a buon fine aggiorna il DataSet accettando le modifiche e aggiornandolo con i campi ad autoincremento eventualmente calcolati nella transazione. Se il salvataggio non riesce, il database non è modificato ed il DataSet rimane nello stato in cui era salvo aggiornare le righe che eventualmente nel processo di salvataggio si è scoperto che erano meno aggiornate rispetto a quelle del DataBase.

Nella sua versione base, SaveData resituisce un elenco di messaggi di errore, ove ne incontri, che hanno impedito il salvataggio. E' possibile tuttavia personalizzare il calcolo di questi messaggi per gestire una business logic estremamente avanzata.


Se vi sono errori di qualsiasi genere, è effettuato un rollback della transazione. A questo punto ci sono due casi: se gli errori sono tutti ignorabili, ossia sono dei warning, è possibile presentare questi errori all'utente o stabilire di ignorarli in qualche altro modo, e quindi ripetere il procedimento, dopo aver invocato il metodo
	
	 SetIgnoredMessages(ProcedureMessageCollection msgs)

Con i messaggi che si è stabilito di ignorare. Se il salvataggio non incontrerà altri errori, andrà a buon fine, altrimenti il meccanismo ricomincia sin quando o ci sono errori non ignorabili, o si stabilisce di non ignorarli, oppure non sono trovati nuovi errori.

ProcedureMessageCollection è una collezione di ProcedureMessage, che è una classe che espone una proprietà LongMess ed una proprietà CanIgnore. La  ProcedureMessageCollection espone comunque essa stessa una proprietà CanIgnore che vale true se tutti i ProcedureMessage contenuti sono ignorabili (o non ve ne sono affatto).

Tuttavia nella sua versione base PostData espone solo i messaggi dovuti a errori di database che quindi sono sempre non ignorabili, ad esempio dovuti a violazioni di chiave o vincoli sui valori null.


## Campi ad autoincremento

Una funzione "unica" e indispensabile per i programmi di contabilità, è il calcolo automatico dei campi "ad autoincremento", ossia quei numeri o codici progressivi che devono essere assegnati ai documenti per essere correttamente catalogati e protocollati dalle istituzioni pubbliche o private.
Esaminiamo vari casi che possono presentarsi e come è possibile gestirli.
Le istruzioni che seguono vanno di solito inserite nei metodi GetNewRow delle classi [MetaData](MetaData.md)

### Semplice campo incrementale
Se il campo va semplicemente aumentato di uno ad ogni nuova riga salvata sul db,  è sufficiente usare:

        C.SetAutoIncrement()

Ove C è il DataColumn da usare come progressivo, e stiamo supponendo che sia un campo di tipo intero.


### Campo con prefisso
Supponiamo invece di avere un campo "num" ed un campo "anno" e di voler calcolare il campo num come
2020/0001, 2020/0002, ..... 2021/0001 , 2021/0002, 2021/0003 etc
dove la parte dell'anno è data dal campo anno mentre la seconda parte aumenta di 1 ogni volta, ed è il massimo per quell'anno. Inoltre facciamo un padleft con degli zeri sino a raggiungimento delle 4 cifre per il numero.

In questo caso possiamo usare:

        C.SetAutoIncrement("anno","/",4)

sul DataColumn della colonna "num", che sarà in questo caso alfanumerica.

L'interfaccia completa dell'estensione SetAutoIncrement è infatti

        public void SetAutoincrement(this DataColumn c,
                        string prefixField,
                        string middleConstant,
                        int length,
                        bool linear = false) 

Ove prefixField è il campo prefisso, nell'esempio precedente il campo "anno", poi c'è una stringa costante da inserire tra il prefisso ed il progressivo, poi la lunghezza del campo progressivo, che sarà left-padded con degli zeri in modo che il risultato sarà del tipo

                         [Row[PrefixField]] [MiddleConst] [LeftPad(newID, IDLength)]

Il campo linear ove presente fa si che il progressivo non sia calcolato a partire da uno per ogni prefisso ma sia incrementato di uno indipendentemente dal prefisso, linearmente tra tutte le righe.

### Campo con selettore
Se invece non vogliamo alcun prefisso, e vogliamo semplicemente che il campo "num" parta da uno per ogni anno, e aumenti per ogni riga inserita in quell'anno, useremo:

    C.SetAutoIncrement()
    C.SetSelector("anno");

l'interfaccia completa di SetSelector è 

    void SetSelector(this DataColumn c, string columnName, UInt64 mask =0) 

in cui è presente anche il campo mask, che ove specificato, fa si che solo i bit presenti nella maschera mask del selettore siano considerati (e non tutto il valore del campo selettore).

C'è anche la stessa estensione sul DataTable:

    void SetSelector(this DataTable T, string columnName, UInt64 mask = 0) 

 che può far comodo se tutti i campi ad autoincremento della stessa tabella hanno lo stesso selettore, infatti in questo caso il selettore si applica a tutti loro.

 Si fa notare che è possibile avere più campi selettore, nel qual caso il progressivo sarà incrementato su ogni combinazione dei selettori. Inoltre il meccanismo di filtro sui selettori si può applicare anche ai campi con prefisso, come ulteriore discrimine sul calcolo del progressivo.
 Il campo linear non agirà, ove specificato a false, sul selettore, ma solo sul prefisso. Ossia su campi selettori diversi ripartirà comunque da uno.



 ### Campo con con calcolo custom
Se vogliamo invece calcolare un campo in modo completamente custom, è possibile scrivere una funzione di calcolo, che deve uniformarsi al delegate:
    
    Task<object> CustomCalcAutoId(DataRow dr, DataColumn c, IDataAccess conn)
    
e utilizzare l'estensione setCustomAutoincrement sul DataTable implicato:

    void setCustomAutoincrement(this DataTable T, string field, RowChange.CustomCalcAutoId customFunction); 





## Personalizzazioni
Vediamo quali sono gli ulteriori metodi che si possono derivare per effettuare dei salvataggi più complessi.

### GetOptimisticClause
	
	string GetOptimisticClause(DataRow R);


Questo metodo è da modificare per far si che restituisca un filtro per l'optimistic locking. Di default la classe base confronta tutti i campi, esclusi quelli di tipo TEXT,  quando deve effettuare una UPDATE o una DELETE, che non è la migliore delle idee in generale.
Ad esempio una possibile implementazione è:

    override public string GetOptimisticClause(DataRow R) {
            if (R.Table.PrimaryKey != null) {
                if ((R.Table.Columns["lu"] != null) &&
                    (R.Table.Columns["lt"] != null) &&
                    R.Table.PrimaryKey.Length > 0) {
                    int keylen = R.Table.PrimaryKey.Length;
                    DataColumn[] Cs = new DataColumn[keylen + 2];
                    for (int i = 0; i < keylen; i++)
                        Cs[i] = R.Table.PrimaryKey[i];
                    Cs[keylen] = R.Table.Columns["lu"];
                    Cs[keylen + 1] = R.Table.Columns["lt"];
                    return qhs.CmpMulti(R, Cs, DataRowVersion.Original);
                }
            }
            return base.GetOptimisticClause(R);
    }

in cui si è supposto di usare due campi, lu ed lt, ove presenti, ai fini dell'optmistic locking


### canPost

        bool canPost(DataRow r) {
                return conn.Security.CanPost(r);
        }

il metodo canPost è richiamato per tutte le righe da scrivere sul db, subito prima della scrittura effettiva, e se ne può fare l'override per introdurre delle condizioni particolari di divieto di scrittura. Ad esempio sulla dimensione di certi campi.
Di default richiama a sua volta il metodo CanPost della classe [Security](Security.md) collegata al DataAccess.


### callChecks


	Task<ProcedureMessageCollection> callChecks(bool post, RowChangeCollection RowChanges)

Dato un set di righe da modificare, deve restituire gli errori di business logic, facendo salvi i messaggi i cui hash sono segnalati in input. ProcedureMessage aventi lo stesso LongMess sono considerati uguali.
Se post è true, i dati sono stati già inviati al db, altrimenti si tratta di un controllo preliminare.
Il metodo è infatti chiamato due volte, una volta prima di inviare i dati al db, dopo la begin transaction, per un controllo "pre", e poi dopo aver inviato i dati al db (update,insert, delete..) ma prima della commit.


### getJournal

	Task<DataJournaling> getJournal(IDataAccess Conn, RowChangeCollection RowChanges)


Ove ridefinito, viene utilizzato per ottenere una classe DataJournaling che verrà usata per salvare un log dei dati. In particolare di questa classe è richiamato il metodo DO_Journaling dopo aver inviato i dati al db, prima di  chiudere la transazione, per ottenere un ulteriore elenco di righe da inviare al database. Se avvengono errori nell'attività di journaling, la transazione è annullata.


### DoExternalUpdate


	delegate Task<(bool result, string errMsg)> DoExternalUpdateDelegate(DataSet D);
	DoExternalUpdateDelegate DoExternalUpdate;

E' possibile impostare la proprietà DoExternalUpdate in modo che venga richiamato durante la transazione, dopo aver salvato i dati di ogni DataSet, e prima di effettuare il commit, per effettuare altre operazioni di qualsiasi genere. Se restituirà degli errori, il salvataggio sarà annullato con un rollback.



### GetEmptyMessageCollection()

	ProcedureMessageCollection GetEmptyMessageCollection()

questo è il metodo che viene invocato da PostData quando deve creare una collezione vuota di messaggi, e deve quindi essere modificato attraverso l'override in una classe derivata se si vuole ottenere una lista di messaggi di errori personalizzata (con classi che derivano da ProcedureMessage)


