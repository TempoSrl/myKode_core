# ISecurity

ISecurity è l'interfaccia usata per rappresentare l'environment e le autorizzazioni associate ad ogni utente applicativo. Può essere organizzata per gruppi o individualmente o anche essere un singleton a seconda dell'uso che si intender farne nell'applicazione.

ISecurity espone dei metodi che vengono invocati durante l'accesso in lettura ed in scrittura sul database in ogni circostanza, senza bisogno che l'utente lo specifichi.

Il comportamento della classe DefaultSecurity che di default implementa ISecurity è di consentire tutto, pertanto se si intende implementare delle politiche di restrizione sarà necessario crearne una derivata.


I metodi da implementare a tal fine sono:

	MetaExpression postingCondition(DataTable T, string opkind_IUDSP);

Calcola la condizione per cui una certa operazione, di tipo I/U/D/S/P è consentita
I/U/D/S stanno per Insert/Update/Delete/Select, P per print ed in questo caso T.TableName si intende essere il codice associato ad una stampa ed il valore dei campi rappresentare i valori dei parametri aventi pari nome.
La condizione è di tipo MetaExpression che sarà poi applicata ai DataRow della tabella indicata.



	bool CanPost(DataRow r)

CanPost stabilisce se una riga può essere inviata al db. Il tipo di operazione implicata si deduce dal DataRowState della riga stessa.
CanPost è usata dalla classe PostData su tutte le righe oggette di scrittura sul DB, e qualora restituisca false, la transazione è annullata e viene restituito un errore che segnala che l'operazione richiesta sulla tabella è vietata dalle regole di sicurezza.
	
	 bool CanSelect(DataRow r)

CanSelect stabilisce se una riga può essere visualizzata dall'utente. E' una condizione applicata ad una riga già letta.



	MetaExpression SelectCondition(string tablename)

SelectCondition restituisce la condizione per cui possono essere lette le righe dal database. A differenza di CanSelect, è un filtro applicato a monte prima di ogni interrogazione su una certa tabella, in congiunzione a qualsiasi altro filtro impostato dall'applicazione.
SelectCondition è usata pervasivamente in DataAccess e ove restituisca un filtro, questo è posto in AND con gli altri filtri passati a tutti i metodi di accesso al DB


	bool CanPrint(DataRow r) 

Considerando r.Table.TableName il codice di una stampa, e i campi di r come i parametri della stampa, deve stabilire se all'utente è consentito o meno effettuare la stampa.

	
	bool CantUnconditionallyPost(DataTable T, string opKind) 


Deve restituire true se all'utente è vietata a priori l'operazione indicata, ossia a prescindere dal contenuto della riga. Se non esiste tale divieto restituirà false.

