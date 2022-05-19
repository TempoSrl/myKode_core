# QueryHelper

La classe QueryHelper è astratta e deve essere derivata per ogni tipo database su cui si vogliono effettuare delle interrogazioni.

Espone circa tanti metodi quanti sono le differenti operazioni disponibili come MetaExpression, ed in più il metodo quote, che serve a trasformare una costante in una string a che lo rappresenta nel dialetto SQL 