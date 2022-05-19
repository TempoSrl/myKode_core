# MetaExpression

Le MetaExpression rappresentano un fattore importante per rendere la propria applicazione indipendente dal database e consentono di comporre espressioni anche molto complesse che possono essere usate sia come filtro per interrogare il database, sia come filtro per le righe di DataTable, e sia come *Predicate* Linq per filtrare collezioni generiche.

Supponiamo di voler filtrare le righe di una tabella aventi i campi a,b,c (o le righe di un array di oggetti di una classe aventi delle property chiamate a,b,c), e di voler filtrare con la seguente espressione:

    ( a > 3 ) && ( b == 1 ) &&  ( c == 'Q')  [1]

Notiamo come il filtro è dato dall'and di tre sotto espressioni, di cui due confronti numerici ed un confronto tra stringhe. Il risultato dell'espressione sarà in questo caso un valore booleano.

La MetaExpression è una classe astratta che rappresenta una generica espressione sui valori di un oggetto, e che a sua volta restituisce un valore, che potrà essere di qualsiasi tipo (booleano, numerico, stringa...). Questa espressione è calcolata quando è invocato il metodo *apply(obj,env=null)* della classe, che accetta come primo parametro l'oggetto su cui calcolare l'espressione. Questo è di solito la riga di una tabella, e può essere praticamente di qualsiasi tipo, dal DataRow al plain object così come un dictionary.

**In tutti gli esempi che seguono si assumerà di aver incluso le MetaExpression con l'istruzione:**

    using q= mdl.MetaExpression;



Una generica espressione può essere data da: 

1) una costante (numerica, stringa, null, booleana). In questo caso al momento della creazione dell'espressione si specificherà direttamente il valore. Nell'esempio [1], 3, 1 e 'Q' sono costanti. La classe MetaExpressionConstant è usata per rappresentare costanti, ma di solito non vi è bisogno di invocarla, poiché nelle altre operazioni  gli operandi costanti vengono automaticamente convertiti in MetaExpressionConstant nei costruttori delle relative MetaExpression. Ne vedremo ora un esempio.
2) il valore di un campo di un oggetto, che sarà valutato al momento dell'esecuzione dell'espressione. In questo caso al momento della costruzione dell'espressione si specificherà il nome del campo. Nell'esempio [1] questo è il caso per i campi a,b,c appunto. MetaExpressionField è usata per rappresentare questa funzione, ed è abbreviata con  q.field
3) un operatore e più operandi, ognuno dei quali potrà essere di tipo (1), (2) o (3). Sempre nell'esempio [1], abbiamo due operatori di uguaglianza, un operatore di maggioranza ed un operatore di and logico tra i risultati dei tre confronti

L'esempio di sopra, scritto come MetaExpression diviene, nella forma più estesa:

    var filter = q.and( q.gt(q.field("a"),q.constant(3)), q.eq(q.field("b"),q.constant(1)) , q.eq(q.field("c"),q.constant("Q"))

Sicuramente in questa forma può sembrare molto macchinosa di scrivere, tuttavia è importante capire che le forme semplificate che ora vedremo sono esattamente equivalenti. Innanzitutto, tutti gli operatori di confronto sono in grado di accettare anche delle costanti nel secondo parametro, quindi non è necessario richiamare la funzione q.constant:

    var filter = q.and( q.gt(q.field("a"),3), q.eq(q.field("b"),1) , q.eq(q.field("c"),"Q")


q.field è usata per il caso 2) ossia serve a prendere il campo di nome pari al suo parametro, tuttavia tutti gli operatori di confronto assumono che se come primo parametro c'è una stringa, si intende che si riferisca al nome di un campo, quindi di solito non è necessario usare q.field ma basta mettere il nome del campo, ottenendo:

    var filter = q.and( q.gt("a",3), q.eq("b",1) , q.eq("c","Q") )

Ma è possibile comporre la MetaExpresione in modo ancora più efficace poiché tutti gli operatori aritmetici e di confronto sono stati sottoposti ad override per le MetaExpression, quindi scriveremo semplicemente:

    q.gt("a",3) && q.eq("b",1) && q.eq("c","Q")     [2]

Un altro modo per esprimere la stessa MetaExpression, usando l'override degli operatori di uguaglianza, è:

    (q.field("a") >= q.constant(3)) && (q.field("b") == q.constant(3)) && (q.field("c") == q.constant("Q"))

In cui è usata anche la MetaExpression constant che rappresenta un valore costante. Tuttavia, la [2] è probabilmente più leggibile.



Per ogni tipo di MetaExpression specializzata, la classe MetaExpression espone un metodo statico che ne richiama il costruttore. Abbiamo già citato q.field e q.constant:

**MetaExpressionConst**, di cui q.constant è lo shortcut, ha come costruttore MetaExpressionConst(obj) e rappresenta un valore costante nell'espressione

**MetaExpressionField**, di cui q.field è lo shortcut, ha come costruttore MetaExpressionField(string fieldName, string tableName = null), e serve ad estrarre il "campo"  fieldName dall'oggetto della valutazione.
Questo potrà essere un campo di un DataRow, o un valore di un Dictionary<string,*> avente come chiave fieldName, o la proprietà fieldName di qualsiasi altro tipo di oggetto.
MetaExpressionField al momento della sua costruzione quindi richiede il nome del campo, mentre al momento della valutazione richiede l'oggetto da cui estrarre quel campo. Il secondo parametro del costruttore è opzionale e per un uso "più avanzato" e serve quando in un join tra più tabelle figurano campi di più oggetti (tabelle), per identificare da quale tabella prendere il campo. Vedremo il suo uso quando esamineremo i join.


**MetaExpressionSys** e **MetaExpressionUsr** servono per introdurre nell'espressione delle "variabili" che non dipendono dall'oggetto della valutazione ma dall'**ambiente** o anche **environment**. In particolare si fa riferimento a due spazi di nomi: Sys e Usr. 
Sys è inteso come uno spazio di nomi per accedere variabili "di sistema" e che l'utente non può cambiare o customizzare a piacimento. Ad esempio può essere un Dictionary<string,object> o una Hashtable
Usr invece è inteso come uno spazio di nomi per accedere a variabili customizzabili dall'utente nell'interazione con l'applicazione. E' una suddivisione puramente arbitraria e vale solo come suggerimento. Sta allo sviluppatore farne l'uso che ritiene più opportuno.

MetaExpressionSys e MetaExpressionUsr nel costruttore accettano un parametro di tipo stringa, che è il nome del campo che dovrà essere estratto dal rispettivo spazio dei nomi.
Nella valutazione di una MetaExpression (con il metodo apply che ora vedremo) è possibile indicare un environment, ed è questo oggetto, che dovrà avere dentro di sé le proprietà sys ed env, da cui le MetaExpression MetaExpressionSys e MetaExpressionUsr estraggono i propri valori.
Il fatto di poter indicare nell'espressione delle variabili di ambiente consente di avere espressioni costruite una sola volta e che possano essere applicate ai vari utenti semplicemente utilizzando nel calcolo un environment diverso.
Veniamo quindi al metodo principale esposto da tutte le MetaExpression.

## Valutazione
Il principale scopo di una espressione è calcolarne il valore. Una MetaExpression però non rappresenta una espressione costante, ma un'espressione da valutare su un certo oggetto, ad esempio un DataRow (ma può essere anche un qualsiasi plain object, Dictionary, ExpandoObject su cui abbia senso estrarne il valore dei campi). 

    object apply(object o = null, dynamic env = null)

    bool getBoolean(object o, dynamic env = null)
 

Il metodo apply(o, env) valuta una MetaExpression su un oggetto **o** usando l'environment **env**. 


Si consideri ad esempio il seguente test

	using q = mdl.MetaExpression;
    ...

    dynamic x = new ExpandoObject();
    x.a = 1;

    q m1 = q.eq("a", 1);
    q m2 = q.eq("a", 2);

    Assert.IsTrue(m1.apply(x), "x.a == 1");
    Assert.IsFalse(m2.apply(x), "x.a <> 1");

    x.a = 2;
    Assert.IsFalse(m1.apply(x), "x.a <> 1");
    Assert.IsTrue(m2.apply(x), "x.a == 1");


Come si vede il risultato della valutazione cambia a seconda dei valori dell'oggetto su cui l'espressione è valutata, in questo caso un ExpandoObject. Vediamo lo stesso esempio con i DataRow, usando gli stessi filtri m1 ed m2:


    DataTable t = new DataTable("x");
    t.Columns.Add("a", typeof(Int32));
    DataRow r = t.NewRow();
    r["a"] = 1;

    Assert.AreEqual(true, m1.apply(r), "x.a is not null");
    Assert.AreEqual(null, m2.apply(r), "x.c is null");


La funzione getBoolean(o, env) è simile ma restituisce un booleano anziché un generico oggetto dal calcolo dell'espressione.

## Elenco classi MetaExpression e relativi shortcut


| MetaExpression           | shortcut                                      | significato                                                                                                                              |
|--------------------------|-----------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| MetaExpressionAdd        | add(params object[] par)                      | somma di numeri                                                                                                                          |
| MetaExpressionMinus      | minus(par1,par2)                              | sottrazione                                                                                                                              |
| MetaExpressionMul        | mul(params object[] par)                      | moltiplicazione                                                                                                                          |
| MetaExpressionDiv        | div(par1, par2)                               | sottrazione                                                                                                                              |
| MetaExpressionModulus    | modulus(par1,par2)                            | modulo della divisione intera                                                                                                            |
| MetaExpressionSum        | sum(fieldName)                                | Somma di colonne, è una grouping function                                                                                                |
| MetaExpressionOr         | or(params object[] par)                       | Operazioni booleane                                                                                                                      |
| MetaExpressionAnd        | and(params object[] par)                      |                                                                                                                                          |
| MetaExpressionNot        | not(par)                                      |                                                                                                                                          |
| MetaExpressionBitwiseAnd | bitAnd(params object[] par)                   | Operazioni su bit                                                                                                                        |
| MetaExpressionBitwiseOr  | bitOr(params object[] par)                    |                                                                                                                                          |
| MetaExpressionBitwiseXor | bitXor(params object[] par)                   |                                                                                                                                          |
| MetaExpressionBitwiseNot | bitNot(par)                                   |                                                                                                                                          |
| MetaExpressionYear       | year(par)                                     | parte "anno" di una data                                                                                                                 |
| MetaExpressionEq         | eq(par1,par2)                                 | uguaglianza di due espressioni, ==. Negli operatori di confronto il primo parametro,  ove sia una stringa, è trattato come un nome campo |
|                          | eqf(par1,par2)                                | (par1 equal field(par2))come eq, ma il secondo parametro è considerato un nome campo                                                     |
|                          | eqObj(fieldName,obj)                          | (fieldName equal obj[fieldName])                                                                                                         |
|                          | neObj(fieldName,obj)                          | (fieldName not equal obj[fieldName])                                                                                                     |
| MetaExpressionNe         | ne(par1,par2)                                 | operatore di disuguaglianza !=                                                                                                           |
| MetaExpressionLe         | le(par1,par2)                                 | par1 <= par2                                                                                                                             |
| MetaExpressionGe         | ge(par1,par2)                                 | >=                                                                                                                                       |
| MetaExpressionLt         | lt(par1,par2)                                 | <                                                                                                                                        |
| MetaExpressionGt         | gt(par1,par2)                                 | >                                                                                                                                        |
|                          | nullOrEq(par1,par2)                           | isNull(par1) or par1 == par2                                                                                                             |
|                          | nullOrNe(par1,par2)                           | isNull(par1) or par1 != par2                                                                                                             |
|                          | nullOrGe(par1, par2)                          | isNull(par1) or par1 >= par2                                                                                                             |
|                          | nullOrLt(par1, par2)                          | isnull(par1) or par1 < par2                                                                                                              |
|                          | nullOrGt(par1,par2)                           | isnull(par1) or par1 > par2                                                                                                              |
|                          | bitSet(par,nbit)                              | true if nbit(th) of par1 is set                                                                                                          |
|                          | bitClear(par,nbit)                            | true if nbit(th) of par1 is not set                                                                                                      |
| MetaExpressionMCmp       | mCmp(object o, params string[] fields)        | (o[field1]=sample[field1) and (o[field2]=sample[field2]) and...                                                                          |
|                          | mCmp(object o)                                | come sopra, per tutti i campi dell'oggetto o                                                                                             |
|                          | mGetChilds(DataRow rParent, DataRelation rel) | Costruisce il filtro per trovare le righe child di rParent in base alla relazione rel                                                    |
|                          | mGetParents(DataRow rChild, DataRelation rel) | Costruisce il filtro per trovare le righe parents di rChild in base alla relazione rel                                                   |
|                          | mCmp(object o, params DataColumn[] fields)    | come mCmp, ma accetta una lista di DataColumn per ottenere l'elenco delle colonne                                                        |
|                          | keyCmp(DataRow o)                             | come mCmp, ma usa i campi chiave del DataRow                                                                                             |
|                          | cmpAs(object sample, source, dest)            | o[destColumn] == sample[sourceColumn]                                                                                                    |
| MetaExpressionWithEnv    | withEnv(ISecurity env)                        | Una MetaExpression equivalente a quella in oggetto, che verrà valutata (apply) con l'environment env                                     |



## Conversione in espressione SQL


    string toSql(QueryHelper q, dynamic env = null)

Il metodo toSql converte la MetaExpression in una stringa che possa essere inclusa in una istruzione SQL. 
Poiché a seconda del dialetto SQL del database utilizzato, questa potrebbe non andare bene per tutti, è utilizzata, in questa fase, una classe [QueryHelper](QueryHelper.md) che è diversa a seconda del tipo di database.
E' l'interfaccia IDbDriver che espone una proprietà di tipo QueryHelper. Senza entrare nei dettagli, esiste una classe che espone l'interfaccia IDbDriver per ogni tipo di database (SqlServer, MySql, ...) e quindi a seconda del database utilizzato, si otterrà una stringa potenzialmente diversa.
Questo spiega però perché c'è la necessità di questo parametro nel metodo toSql, infatti la MetaExpression è indipendente dal Database utilizzato. Ad ogni modo, il programmatore di solito non avrà bisogno di chiamare toSql, poiché a questo provvedono direttamente le altre classi di livello più alto (GetData, PostData, DataAccess). 
La necessità potrebbe subentrare solo se si intende inserire un filtro in una istruzione sql scritta a mano e si volesse evitare di preoccuparsi di quotare correttamnte le costanti presenti nel filtro.



## Conversione in forma "friendly"
Il metodo toString() serve ad ottenere una versione developer-like per visualizzare un'espressione. E' utile specialmente in fase di testing o di debug.


    string toString()




## Compilazione dell'espressione

Per usi particolarmenti intensivi di una MetaExpression, è possibile ottimizzarla e **compilarla** a runtime. L'ottimizzazione fa si che l'accesso alle colonne dei DataRow non avvenga per nome ma per numero di colonna, la compilazione invece fa esattamente quello che dice il termine, ottiene il codice c# corrispondente alla MetaExpression, lo usa per compilare a runtime una funzione che esegua quel codice e nelle invocazioni successive della funzione apply() utilizzerà tale funzione compilata anziché valutarla a runtime argomento per argomento.

I metodi della MetaExpression coinvolti sono:

 
    MetaExpression optimize(DataTable t, string alias=null);
	MetaExpression Compile<T>();


Il parametro T di Compile serve a specificare il tipo di parametro che la funzione compilata accetterà in input, al quale potrà essere applicata l'espressione una volta compilata. Se si intende creare una MetaExpression per filtrare dei DataRow, ad esempio, sarà appunto un DataRow.
Una qualsiasi MetaExpression è quindi facilmente ottimizzabile, ad esempio con: 

    var queryFilter = q.ge("taxable", 10.0M).optimize(t).Compile<DataRow>();

Nel quale caso filter sarà applicabile a DataRow nel seguito, ossia ammetterà un DataRow come primo parametro nel metodo apply visto prima:

    DataTable T;
    ...
    var rows = T.filter(queryFilter);

In questo caso rows conterrà le righe che sodisfano il filtro queryFilter. filter è una extension del DataTable che viene aggiunta da MDL.

## Semplificazione automatica 
C'è un set molto vasto di MetaExpression, una per ogni operatore algebrico, booleano o stringa esistente e per ognuna esiste anche un metodo statico di MetaExpression che ne richiama il costruttore, ad esempio per MetaExpressionAdd esiste MetaExpression.add :

    /// <summary>
    /// Addition of two or more metaexpressions
    /// </summary>
    /// <param name="par"></param>
    /// <returns></returns>
    public static MetaExpression add(params object[] par) {
        return tryEval(new MetaExpressionAdd(par));
    }

nel corpo del metodo statico add troviamo una funzione, tryEval, che pervade la creazione delle MetaExpression, e che ne ottimizza l'esecuzione:

    /// <summary>
    /// Tries to evaluate the expression with an undefined object as parameter
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static MetaExpression tryEval(MetaExpression m) {
        if (m.isConstant()) return m;
        object res = m.apply(null);
        if (res != null) {
            return new MetaExpressionConst(res);
        }
        return m;
    }

La tryEval in sintesi prova a calcolare una espressione passando come argomento per l'oggetto da calcolare il valore null. Questo indica l'assenza del dato nel calcolo. Se il risultato è null o una costante, l'espressione è semplificata nel suo risultato. Altrimenti è restituita l'espressione originale.

Quando serve? supponiamo di avere  una espressione del tipo, codificata come MetaExpression:

    ( espressione con calcoli complicati ) \* 0

Siccome la moltiplicazione restituisce zero se uno dei due operandi è zero, il risultato sarà zero. Allora tryEval darà la costante zero come risultato e l'espressione con calcoli complicati non sarà mai eseguita. Similmente se in una sequenza di operazioni 

    (espressione logica 1) && (espressione FALSE) && (espressione logica 2) && ..

oppure

    (espressione logica 1) || (espressione TRUE) || (espressione logica 2) || ..

saranno semplificate come espressioni costanti rispettivamente false o true. Questo vale sia a livello di risultato complessivo e sia a livello di sottoespressioni nelle espressioni composite. Il vantaggio è ancora più marcato quando questa espressione è poi usata come filtro per le righe di un database oppure in memoria su insiemi molto grandi di righe.

Per esempi sull'uso della MetaExpression si vedano gli [unit test](TestNetCore\MetaExpressionTest.cs)


