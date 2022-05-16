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


## Personalizzazioni
Vediamo quali sono gli ulteriori metodi che si possono derivare per effettuare dei salvataggi più complessi.



### DoExternalUpdate


	delegate Task<(bool result, string errMsg)> DoExternalUpdateDelegate(DataSet D);
	DoExternalUpdateDelegate DoExternalUpdate;

E' possibile impostare DoExternalUpdate in modo che venga richiamato durante la transazione, dopo aver salvato i dati di ogni DataSet, e prima di effettuare il commit, per effettuare altre operazioni di qualsiasi genere. Se restituirà degli errori, il salvataggio sarà annullato con un rollback.



### GetEmptyMessageCollection()

	ProcedureMessageCollection GetEmptyMessageCollection()

questo è il metodo che viene invocato da PostData quando deve creare una collezione vuota di messaggi, e deve quindi essere modificato se si vuole ottenere una lista di messaggi di errori personalizzata (con classi che derivano da ProcedureMessage)




E' necessario parlare estesamente dei campi ad autoincremento con selettori, prefissi e tutto quanto
