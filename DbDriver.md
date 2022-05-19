# DbDriver

Le classi che implementano il driver fisico per l'accesso al database devono implementare l'interfaccia IDbDriver

L'interfaccia IDbDriver fornisce tutte le primitive per operare fisicamente sul db e in più valorizza la proprietà QH di tipo QueryHelper, che serve a trasformare le MetaExpression in stringhe. Presumibilmente anche di questa va creata una classe derivata quando si implementa una nuova classe DbDriver.

Una classe che implementa l'interfaccia IDbDriver deve tener conto delle transazioni, ossia tener conto della transazione corrente e agganciarla ad ogni operazione richiesta.

La proprietà defaultTimeout deve essere usata ogni volta che il parametro timeout dei vari comandi sia -1.

