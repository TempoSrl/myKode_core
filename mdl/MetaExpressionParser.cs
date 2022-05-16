using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using q  = mdl.MetaExpression;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {


    /// <summary>
    /// Kind of token: constant, operator, fieldname and so on
    /// </summary>
    public enum tokenKind {
        /// <summary>
        /// unknown token
        /// </summary>
        notFound,
        /// <summary>
        /// numeric or string, for now we ignore date constants
        /// </summary>
        constant,   
        /// <summary>
        /// Field name
        /// </summary>
        fieldName,
        /// <summary>
        /// Operator
        /// </summary>
        operatore,
        /// <summary>
        /// Open parenthesis (
        /// </summary>
        openPar,  

        /// <summary>
        /// Closed parenthesis )
        /// </summary>
        closedPar, 
        /// <summary>
        /// End of string character
        /// </summary>
        endOfString, 
        /// <summary>
        /// Comma caracter
        /// </summary>
        comma,
        /// <summary>
        /// &lt;%sys[varname]%&gt;
        /// </summary>
        openEnvironment, 
        /// <summary>
        /// &lt;%usr[varname]%&gt;
        /// </summary>
        closeEnvironment     
    }

    class environmentExpression : BuildingExpression {
        private string  value;
        public environmentExpression(BuildingExpression parent, string value):base(parent) {
            this.value = value;            
        }

        public override int evaluationOrder() {
            return -1;
        }

        public override MetaExpression build() {
            if (value.StartsWith("sys[")) {
                string sysVar = value.Substring(4, value.Length - 5);
                return MetaExpression.sys(sysVar);
            }
            if (value.StartsWith("usr[")) {
                string usrVar = value.Substring(4, value.Length - 5);
                return MetaExpression.usr(usrVar);
            }
            return null;            
        }
    }
   

     class constantExpression : BuildingExpression {
        private object constValue;
        public constantExpression(BuildingExpression parent, object value):base(parent) {
            constValue = value;            
        }

         public override int evaluationOrder() {
             return -1;
         }

         public override MetaExpression build() {
            return q.constant(constValue);
        }
    }
    //class fnCallExpression : BuildingExpression {
    //    private string fName;
    //    public fnCallExpression(BuildingExpression parent, string fieldName):base(parent) {
    //        fName = fieldName;            
    //    }

    //    public override int evaluationOrder() {
    //        return -1;
    //    }

    //    public override MetaExpression build() {
    //        return q.field(fName);
    //    }
    //}

    class fieldExpression : BuildingExpression {
        private string fName;
        public fieldExpression(BuildingExpression parent, string fieldName):base(parent) {
            fName = fieldName;            
        }

        public override int evaluationOrder() {
            return -1;
        }

        public override MetaExpression build() {
            return q.field(fName);
        }
    }

    /// <summary>
    /// Helper class to build MetaExpressions
    /// </summary>
    public class BuildingExpression {
        /// <summary>
        /// Converts this to a MetaExpression
        /// </summary>
        /// <returns></returns>
        public virtual q build() {
            var metaOperands = (from o in operands select o.build()).ToArray();
            if (op == null) {
                if (metaOperands.Length == 0) return null;
                if (metaOperands.Length == 1) return metaOperands[0];
                return new MetaExpressionList(metaOperands);//unico modo di veicolare un elenco di parametri ad una chiamata a funzione o ad un operatore in  o not in
            }

            switch (op.name) {
                case "&":
                    return  MetaExpression.bitAnd(metaOperands);
                case "|":
                    return MetaExpression.bitOr(metaOperands);
                case "~":
                    return MetaExpression.bitNot(metaOperands);
                case "^":
                    return MetaExpression.bitXor(metaOperands);
                case "list":
                    return new MetaExpressionList(metaOperands);
                case "like":
                    return new MetaExpressionLike(metaOperands[0],metaOperands[1]);
                case "between":
                    return MetaExpression.between(metaOperands[0].FieldName,metaOperands[1],metaOperands[2]);
                case "par":
                    return new MetaExpressionDoPar(metaOperands[0]);
                case "and":
                    return MetaExpression.and(metaOperands);
                case "or":
                    return MetaExpression.or(metaOperands);
                case "not":
                    return MetaExpression.not(metaOperands[0]);
                case "not in":
                    //first operand is a field identifier, second operand is a list
                    string fieldNameNotIn = metaOperands[0].FieldName;
                    var opListNotIn = metaOperands[1] as MetaExpressionList;
                    return MetaExpression.fieldNotIn(fieldNameNotIn, opListNotIn);
                case "in":
                    //first operand is a field identifier, second operand is a list
                    string fieldNameIn = metaOperands[0].FieldName;
                    var opListIn = metaOperands[1] as MetaExpressionList;
                    return MetaExpression.fieldIn(fieldNameIn, opListIn.Parameters);
                case "is null":
                    return MetaExpression.isNull(metaOperands[0]);
                case "is not null":
                    return MetaExpression.isNotNull(metaOperands[0]);
                case "isnull":
                    var isNullList = metaOperands[0] as MetaExpressionList;
                    return MetaExpression.isNullFn(isNullList.Parameters[0],isNullList.Parameters[1]);
                case "%":
                    return MetaExpression.modulus(metaOperands[0],metaOperands[1]);
                case "-":
                    return MetaExpression.sub(metaOperands[0],metaOperands[1]);
                case "/":
                    return MetaExpression.div(metaOperands[0],metaOperands[1]);
                case ">=":
                    return MetaExpression.ge(metaOperands[0],metaOperands[1]);
                case "<=":
                    return MetaExpression.le(metaOperands[0],metaOperands[1]);
                case ">":
                    return MetaExpression.gt(metaOperands[0],metaOperands[1]);
                case "<":
                    return MetaExpression.lt(metaOperands[0],metaOperands[1]);
                case "=":
                    return MetaExpression.eq(metaOperands[0],metaOperands[1]);
                case "<>":
                    return MetaExpression.ne(metaOperands[0],metaOperands[1]);
                case "*":
                    return MetaExpression.mul(metaOperands);
                case "+":
                    return MetaExpression.add(metaOperands);
                default:
                    return null;
            }
        }

        /// <summary>
        /// List of expression to which the operator has to be applied
        /// </summary>
        public List<BuildingExpression> operands= new List<BuildingExpression>();

       
        /// <summary>
        /// Creates an empty expression
        /// </summary>
        /// <param name="parent"></param>
        public BuildingExpression(BuildingExpression parent) {
            parentExpression = parent;
        }

        private OperatorDescriptor op;

        /// <summary>
        /// Expression containing this one
        /// </summary>
        private BuildingExpression parentExpression; 

        ///// <summary>
        ///// This expression 
        ///// </summary>
        ///private q currentExpression; 

        /// <summary>
        /// Gets the evaluation order, lower values means it has to be executed first
        /// </summary>
        /// <returns></returns>
        public virtual int evaluationOrder() {
            return  op.evaluationOrder;
        }
        //private bool lastWasOperator;
        //private bool lastWasOperand;

        //void setLastAsOperand() {
        //    lastWasOperator = false;
        //    lastWasOperand = true;
        //}

        //void setLastAsOperator() {
        //    lastWasOperator = true;
        //    lastWasOperand = false;
        //}

        void replaceOperand(BuildingExpression oldOperand, BuildingExpression newOperand) {
            for (int i = 0; i < operands.Count; i++) {
                if (operands[i] == oldOperand) {
                    operands[i] = newOperand;
                    break;                    
                }
            }
        }

        /// <summary>
        /// creates a new expression as a child of this one, so that the new expression is an operand of the current one.
        /// Useful if operator is we have  a+b+c+d   and then comes *, or we have a &lt; b   and then comes +
        ///  i.e. if evaluation order of new operator is less than the current one
        /// The new expression has last operand as first operand, so the result is (a+b+c+(d*... 
        /// Returns the new created child expression, in this example d*
        /// </summary>
        /// <returns></returns>
        BuildingExpression createChildExpression() {
            var child = new BuildingExpression(parentExpression);
            if (operands.Count == 0) {
                addOperand(child);
            }
            else {
                int lastOperandIndex = operands.Count-1;
                var lastOperand = operands[lastOperandIndex];
                child.addOperand(lastOperand);
                operands[lastOperandIndex] = child;
                child.parentExpression = this;
            }
            return child;
        }

        /// <summary>
        /// Creates a new expression as a parent of this one. The new expression takes the place of the old expression in the parent expression
        /// Returns the new created parent expression
        /// </summary>
        /// <returns></returns>
        BuildingExpression createParentExpression() {
            var originalParent = parentExpression;
            var newExpr= new BuildingExpression(originalParent);
            newExpr.addOperand(this);  //"this" becomes an operand of newExpr
            if (originalParent == null) {
                // A    >>          P ( A )      >>> returns P(A)
                //There is no current parent
                return newExpr;
            }
            // B (,... A )   >>     B (,.. P ( A ) )   >> returns P(A)
            originalParent.replaceOperand(this,newExpr);
            return newExpr;
        }
        static internal  OperatorDescriptor listDescriptor = new OperatorDescriptor() {name="list",evaluationOrder=1000,nary=true};

        /// <summary>
        /// Creates an operation in parentheses into a parent expression
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        public static BuildingExpression createParentesizedExpression(BuildingExpression child) {
            var newExpr= new BuildingExpression(null);
            newExpr.addOperator(new OperatorDescriptor() {name="par",evaluationOrder = 0});
            newExpr.addOperand(child);
            return newExpr;
        }

        /// <summary>
        /// Creates an operation into a parent expression
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static BuildingExpression createList(BuildingExpression parent) {
            var newExpr= new BuildingExpression(parent);
            newExpr.addOperator(listDescriptor);
            return newExpr;
        }


        /// <summary>
        /// Gets the root expression
        /// </summary>
        /// <returns></returns>
        public BuildingExpression nextParentList() {
            var curr = this;
            while (true) {
                if (curr.op == null) {
                    return curr;
                }
                if (curr.op.name == "list") return curr;
                if (curr.parentExpression == null) {
                    //trasforma questa espressione in una lista di cui la vecchia espressione è il primo elemento
                    var newExpr= new BuildingExpression(null);
                    newExpr.addOperand(curr);
                    newExpr.addOperator(listDescriptor);
                    return newExpr;
                }
                curr = curr.parentExpression;
            }
        }

        /// <summary>
        /// Gets the root expression
        /// </summary>
        /// <returns></returns>
        public BuildingExpression outerExpression() {
            var curr = this;
            while (curr.parentExpression != null) curr = curr.parentExpression;
            return curr;
        }

        BuildingExpression appendExpression(OperatorDescriptor op) {
            if (op.unary_prefixed && operands.Count > 0) {
                BuildingExpression newExpression = new BuildingExpression(this);
                operands.Add(newExpression);
                return newExpression;
            }
            int opEvaluationOrder = op.evaluationOrder;
            if (evaluationOrder() < opEvaluationOrder) {
                //l'ordine di valutazione corrente è minore di quello nuovo: siamo nel caso a*b*c/d +
                // quindi va creata una nuova espressione in cui la precedente sia il primo operando
                var par = findSuitableParentForOperator(opEvaluationOrder);
                if (par.op != null && par.op.name == op.name) {
                    if (op.binary && par.operands.Count == 1) return par;
                    if (op.nary) return par;
                }
                return par.createParentExpression();
            }
            if (this.op.binary && this.operands.Count==2 && evaluationOrder() == op.evaluationOrder) {
                //siamo nel caso a/b/c, va eseguito prima a/b e poi /c
                return this.createParentExpression();
            }
            //siamo nel caso a+b *  
            // in questo caso creiamo una espressione in cui quello che segue prende come primo operando l'ultimo che c'era
            return createChildExpression();
        }
        /// <summary>
        /// Searches the first parent having an evaluationOrder lower than the given operator
        /// </summary>
        /// <param name="opEvaluationOrder"></param>
        /// <returns></returns>
        BuildingExpression findSuitableParentForOperator(int opEvaluationOrder) {        
            //must search an expression with evaluationOrder lower than the given operator
            // for example A + B < 
            // here < must be executed AFTER + so < becomes the root of the expression, >>    < ( A+B,...
            // while in  A < B *  
            // another replacement should be applied

            var expr = this;
            while (expr.parentExpression != null && expr.evaluationOrder() < opEvaluationOrder) {
                expr = expr.parentExpression;
            }

            if (expr.evaluationOrder() > opEvaluationOrder) {
                return expr.operands[expr.operands.Count - 1];
            }
            return expr;
        }

        /// <summary>
        /// Adds an operator to current expression, setting it to opToAdd
        /// </summary>
        /// <param name="opToAdd"></param>
        /// <returns></returns>
        public virtual BuildingExpression addOperator(OperatorDescriptor opToAdd) {
            if (op == null) {
                op = opToAdd; //opera sull'espressione corrente
                //if (!opToAdd.unary_postfixed) setLastAsOperator();
                return this;
            }



            if (op.nary && op.name == opToAdd.name) {
                //setLastAsOperator();
                return this;
            }

            if (op.name == "between" && opToAdd.name == "and" && operands.Count == 2) {
                //setLastAsOperator();
                return this;
            }

            var newExpr = appendExpression(opToAdd);
            newExpr.op = opToAdd;
            //newExpr.setLastAsOperator();
            return newExpr;
        }

        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual BuildingExpression addToken(Token t) {
            var opToAdd = t.getDescriptor();
            if (opToAdd == null) return null;
            if (!canAppendOperator(opToAdd)) return null;

            int nOps = operands.Count;
            if (opToAdd.unary_prefixed) {
                if (op == null) {
                    //assume l'operatore dato come operatore corrente
                    op = opToAdd;
                    setLastAsOperator();
                    return this;
                }
                //Allora lastWasOperator = true, quindi questo è l'inizio di una nuova espressione che inizia con opToAdd (che precederà un operando)
                var child = new BuildingExpression(this) {
                    op = opToAdd
                };
                child.setLastAsOperator();
                addOperand(child);
                return child;
            }

            if (op.unary_postfixed) {
                if (isComplete()) {
                    return addOperator(opToAdd);                    
                }
            }


            //primo operatore di operazione binaria o n-aria
            if (op.binary) {
                if (nOps==1 && op==null) {
                    op = opToAdd;
                    setLastAsOperator();
                    return this;
                }

                if (op!=null) {
                    //quel che deve fare dipende dalla precedenza degli operatori
                    // op è binaria quindi vuole un operando come predecessore
                    // quello che precede dobbiamo considerarlo come operando per op, bisogna vedere però di quanto dobbiamo risalire in questo 
                    //  processo
                    var newExpr = addOperator(opToAdd);
                    return newExpr;
                }
                
            }

            if (op.nary) {
                if (nOps==1 && op==null) {
                    op = opToAdd;
                    setLastAsOperator();
                    return this;
                }
                if (nOps>=1 && op!=null) {
                    if (op.name == opToAdd.name) {
                        setLastAsOperator();
                        return this;
                    }
                    //l'operatore è diverso, crea la nuova espressione
                    return  addOperator(opToAdd);
                }

            }

            return null;
        }
        */

        
   
        /// <summary>
        /// Adds an operand to current expression
        /// </summary>
        /// <param name="expr"></param>
        public virtual void addOperand(BuildingExpression expr) {
            //if (!canAddOperand())return;
            operands.Add(expr);
            expr.parentExpression = this;
            //setLastAsOperand();                                             
        }
        

        /*

        /// <summary>
        /// Check if this expression can be evaluated
        /// </summary>
        /// <returns></returns>
        public virtual bool isComplete() {
            if (op == null) return operands.Count==1; //è un operando
            if (op.unary_prefixed) return operands.Count==1 && lastWasOperand;
            if (op.unary_postfixed) return operands.Count==1 && lastWasOperator;
            if (op.binary) return operands.Count==2;
            if (op.nary) return operands.Count>=2 && lastWasOperand;
            return false;
        }
        */

        /*
        /// <summary>
        /// Stabilisce se si può accodare un operando o operatore all'espressione corrente (senza crearne una nuova)
        /// </summary>
        /// <param name="_operator"></param>
        /// <returns></returns>
        public virtual bool canMergeOperator(OperatorDescriptor _operator) {
            if (lastWasOperator) return false; //non può aggiungere due operatori di seguito, in nessun caso

            int nOps = operands.Count;
            if (_operator.unary_prefixed) return (op == null && nOps == 0);
            if (_operator.unary_postfixed) return (op == null && nOps == 1);


            //primo operatore di operazione binaria o n-aria
            if (_operator.binary) {
                return (nOps==1 && op==null);
            } 
            if (_operator.nary) {
                if (nOps == 0) return false; //manca il primo operando
                if (op == null) return true; //c'è solo un operando, mancava l'operatore

                //secondo o n-mo operatore di operazione n-aria (pari operatore)
                return (lastWasOperand && op.name == _operator.name);                
            } 

            return false;
        }
        */


        /*
        /// <summary>
        /// Stabilisce se si può creare una nuova espressione aggiungendo un operatore alla precedente
        /// </summary>
        /// <param name="_operator"></param>
        /// <returns></returns>
        public virtual bool canAppendOperator(OperatorDescriptor _operator) {
            if (lastWasOperator) return false; //non può aggiungere due operatori di seguito, in nessun caso

            int nOps = operands.Count;
            if (_operator.unary_prefixed) return (op == null || lastWasOperator); //un operatore prefisso deve precedere l'espressione oppure seguire un operatore (precedendo l'operando)
            if (_operator.unary_postfixed) return isComplete(); //A qualsiasi espressione "finita" posso concatenare un operatore postfisso 


            //primo operatore di operazione binaria o n-aria
            if (_operator.binary) {
                if ( lastWasOperand) return true;
                if (op==null && nOps==0) return false;
                //if (_operator.binary && op==null && nOps>0) return true;// non può accadere, sarebbe lastWasOperand true
                // allora op!=null 
                return op.unary_postfixed;           //comunque possiamo considerare quello che precedeva un operando
                        
            }

            if (_operator.nary) {
                if (nOps == 1 && op == null) return true;

                //secondo o n-mo operatore di operazione n-aria
                if (lastWasOperand) return true;

                if (lastWasOperator && op.unary_postfixed)
                return false;
            }

            return false;
        }
        */

        ///// <summary>
        ///// Verifica se si può aggiungere un operando
        ///// </summary>
        ///// <returns></returns>
        //public virtual bool canAddOperand() {
        //    int nOps = operands.Count;
        //    if (op != null) {
        //        if (op.unary_postfixed) return false; //operando già presente per l'operazione unaria
        //        if (op.unary_prefixed) return lastWasOperator; 
        //        if (op.binary) return (nOps==1 && lastWasOperator);
        //        if (op.nary) return lastWasOperator;
        //        return false;
        //    }
        //    //op==null
        //    return nOps == 0;
        //}

     



    }


    /// <summary>
    /// Information on an operator
    /// </summary>
    public class OperatorDescriptor {
        /// <summary>
        /// Operation is unary prefixed
        /// </summary>
        public bool unary_prefixed;

        /// <summary>
        /// Operation is unary postfixed
        /// </summary>
        public bool unary_postfixed;

        /// <summary>
        /// Operation is binary
        /// </summary>
        public bool binary;

        /// <summary>
        /// Operation is n-ary
        /// </summary>
        public bool nary;

        /// <summary>
        /// Evaluation order of operator
        /// </summary>
        public int evaluationOrder;

        /// <summary>
        /// Name of operation
        /// </summary>
        public string name;

        /// <summary>
        /// Operator precededs a list
        /// </summary>
        public bool precedesList;
    }

    /// <summary>
    /// Represent a basic piece of an expression
    /// </summary>
    public  class Token {
        /// <summary>
        /// token kind
        /// </summary>
        public tokenKind kind;

        /// <summary>
        /// name of operator or variable name
        /// </summary>
        public string content; //for operators and variable names

        /// <summary>
        /// value of constant
        /// </summary>
        public object value; 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="kind"></param>
        public Token(tokenKind kind) {
            this.kind = kind;
        }

        /// <summary>
        /// lower evaluationOrder indicates that operation must be executed BEFORE
        /// </summary>
        private static Dictionary<string,OperatorDescriptor> AlfaOperators= new  Dictionary<string,OperatorDescriptor> {
            { "and",new OperatorDescriptor {nary=true,evaluationOrder=100,name="and"} },
            { "between",new OperatorDescriptor {nary=true,evaluationOrder=70,name="between"} },
            { "or",new OperatorDescriptor {nary=true,evaluationOrder=120,name="or"} },
            { "not",new OperatorDescriptor {unary_prefixed=true,evaluationOrder=110,name="not"} },
            { "not in",new OperatorDescriptor {binary= true,evaluationOrder=70,name="not in",precedesList=true} },
            { "in",new OperatorDescriptor {binary= true,evaluationOrder=70,name="in",precedesList=true} },
            { "like",new OperatorDescriptor {binary = true,evaluationOrder=70,name="like"} },
            { "is null",new OperatorDescriptor {unary_postfixed= true,evaluationOrder=70,name="is null"} },
            { "is not null",new OperatorDescriptor {unary_postfixed= true,evaluationOrder=70,name="is not null"} },
        };

        private static Dictionary<string,OperatorDescriptor> functions= new  Dictionary<string,OperatorDescriptor> {
            { "isnull",new OperatorDescriptor {unary_prefixed= true,evaluationOrder=1,name="isnull",precedesList=true}},
        };

        private static Dictionary<string,OperatorDescriptor> Operators= new  Dictionary<string,OperatorDescriptor> {
            { "%",new OperatorDescriptor {nary=true,evaluationOrder=25,name="%"}},
            { "+",new OperatorDescriptor {nary=true,evaluationOrder=20,name="+"}},
            { "-",new OperatorDescriptor {binary= true,evaluationOrder=20,name="-"}},
            { "/",new OperatorDescriptor {binary=true,evaluationOrder=10,name="/"}},
            { "*",new OperatorDescriptor {nary=true,evaluationOrder=10,name="*"}},
            { "&",new OperatorDescriptor {nary=true,evaluationOrder=10,name="&"}},
            { "^",new OperatorDescriptor {nary=true,evaluationOrder=10,name="^"}},
            { "~",new OperatorDescriptor {unary_prefixed=true,evaluationOrder=5,name="~"}}, 
            { "|",new OperatorDescriptor {nary=true,evaluationOrder=10,name="|"}}, 
            { "<",new OperatorDescriptor {binary=true,evaluationOrder=50,name="<"}},
            { "<%",new OperatorDescriptor {unary_prefixed = true,evaluationOrder=0,name="openEnvironment"}},
            { ">",new OperatorDescriptor {binary=true,evaluationOrder=50,name=">"}},
            { "%>",new OperatorDescriptor {unary_postfixed = true,evaluationOrder=0,name="closeEnvironment"}},
            { "=",new OperatorDescriptor {binary=true,evaluationOrder=50,name="="}},
            { "<=",new OperatorDescriptor {binary=true,evaluationOrder=50,name="<="}},
            { ">=",new OperatorDescriptor {binary=true,evaluationOrder=50,name=">="}},
            { "<>",new OperatorDescriptor {binary=true,evaluationOrder=50,name="<>"}}
        };

        private static Token EndOfString = new Token(tokenKind.endOfString);
        private static Token NoToken = new Token(tokenKind.notFound);
        private static Token OpenPar = new Token(tokenKind.openPar) { value="("};
        private static Token ClosePar = new Token(tokenKind.closedPar){ value=")"};
        //private static Token OpenEnvironment = new Token(tokenKind.openEnvironment) { value="<%"};
        //private static Token CloseEnvironment = new Token(tokenKind.closeEnvironment){ value="%>"};

        


        private static bool anyKeyStartsWith(string prefix,Dictionary<string,OperatorDescriptor>collection) {
            foreach (string v in collection.Keys) {
                if (v.StartsWith(prefix)) return true;
            }

            return false;
        }


        //private static bool isOperand(char c) {
        //    if (char.IsLetterOrDigit(c)) return true;
        //    if (c == '_') return true;
        //    return false;
        //}


        private static bool isOperator(char c) {
            string ops = "+-/*=<>&|";
            return ops.IndexOf(c)>=0;
        }

        private static bool isAlfaNum(char c) {
            if (char.IsLetterOrDigit(c)) return true;
            if (c == '_') return true;
            return false;
        }

        //private static string getAlfaNumSequence(string s, ref int currPos) {
        //    string res = "";
        //    while (s.Length < currPos && isAlfaNum(s[currPos])) {
        //        res += s[currPos];
        //        currPos++;
        //    }
        //    return res;
        //}

        private static bool isAlfa(char c) {
            if (char.IsLetter(c)) return true;
            if (c == '_') return true;
            return false;
        }
        private static bool isNumeric(char c) {
            if (char.IsDigit(c)) return true;
            if (c == '.') return true;
            return false;
        }

        /// <summary>
        /// Rimuove tutti i spazi consecutivi tranne che nelle stringhe
        /// </summary>
        /// <param name="sqlcmd"></param>
        /// <returns></returns>
        public static string normalize(string sqlcmd){
            if (sqlcmd==null) return "";
            bool prevwasidentifier=false;
            bool spacetoadd=false;
            string res="";
            int index=0;
            //sqlcmd = StripComments(sqlcmd);
            int len = sqlcmd.Length;
            while (index< len){
                char c = sqlcmd[index];

                if ((c!=' ')&&(c!='\n')&&(c!='\r')&&(c!='\t')){
                    if (isAlfaNum(c)) {
                        if (spacetoadd) res+=" ";
                        spacetoadd=false;

                        prevwasidentifier=true;
                    }
                    else {
                        prevwasidentifier=false;
                        spacetoadd=false;

                    }
                    res+=c;

                    if (c=='\''){
                        //skips  the string constant 
                        index++;
                        //skips the string
                        while (index<len){
                            if (sqlcmd[index]!='\'') {
                                res+= sqlcmd[index];
                                index++;
                                continue;
                            }
                            //it could be an end-string character
                            if (((index+1)<len)&&(sqlcmd[index+1]=='\'')){
                                //it isn't
                                res+= sqlcmd[index];
                                index++;
                                res+= sqlcmd[index];
                                index++;
                                continue;
                            }
                            res+= sqlcmd[index];
                            break;
                        }
                    }

                }
                else {//Converte tutti gli spazi precedenti in uno spazio
                    if (prevwasidentifier) spacetoadd =true;
                    prevwasidentifier=false;
                }
                index++;
            }
            return res;
        }

        private static bool isSpace(char c) {
            return (c == ' ') || (c == '\n') || (c == '\r') || (c == '\t');
        }

        /// <summary>
        /// Salta tutti gli spazi a partire dalla posizione corrente
        /// </summary>
        /// <param name="s"></param>
        /// <param name="currPos"></param>
        public static void skipSpaces(string s, ref int currPos) {
            while (currPos < s.Length) {
                var c = s[currPos];
                if (!isSpace(c)) return;
                currPos++;
            }

        }

        /// <summary>
        /// Restituisce le proprietà dell'operatore corrente
        /// </summary>
        /// <returns></returns>
        public OperatorDescriptor getDescriptor() {
            return Token.getDescriptor(content);
        }

        internal static OperatorDescriptor getDescriptor(string name) {
            if (Operators.ContainsKey(name)) return Operators[name];
            if (AlfaOperators.ContainsKey(name)) return AlfaOperators[name];
            if (functions.ContainsKey(name))return functions[name];
            return null;
        }
        private static string getAlfaSequence(string s, ref int currPos) {
            var currValue = "";
            bool checkAlfaNum = false;
            while (currPos < s.Length && 
                    ( ( checkAlfaNum==false && isAlfa(s[currPos]))||
                      ( checkAlfaNum==true && isAlfaNum(s[currPos]))
                   )
                ) {
                currValue += s[currPos];
                checkAlfaNum = true;
                currPos++;
            }
            return currValue;
        }
        
        private static Token getOperator(string s, ref int currPos) {
            var found = getTokenOfClass(s, ref currPos, Operators,isOperator);
            if (found=="<%")return new Token(tokenKind.openEnvironment) {content= found};
            if (found=="%>")return new Token(tokenKind.closeEnvironment) {content= found};
            return found == null ? NoToken : new Token(tokenKind.operatore) {content= found};
        }

        /// <summary>
        /// Gets an alfa operator or a field name. Note that an alfa operator may contain spaces, while an identifier does not.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="currPos"></param>
        /// <returns></returns>
        private static Token getAlfaToken(string s, ref int currPos) {
            if (!isAlfa(s[currPos])) return NoToken;
            int myPos = currPos;
            string alfaSeq = getAlfaSequence(s, ref myPos);
            if (AlfaOperators.ContainsKey(alfaSeq.ToLower())) {
                //restart all again
                var opFound = getTokenOfClass(s, ref currPos, AlfaOperators,isAlfaNum);
                return new Token(tokenKind.operatore) {content = opFound};
            }

            if (functions.ContainsKey(alfaSeq.ToLower())) {      
                currPos = myPos;
                return new Token(tokenKind.operatore) {content = alfaSeq.ToLower()};
            }
            if (anyKeyStartsWith(alfaSeq.ToLower(), AlfaOperators)) {
                int startPos = currPos;
                var opFound = getTokenOfClass(s, ref startPos, AlfaOperators,isAlfaNum);
                if (opFound != null) {
                    currPos = startPos;
                    return new Token(tokenKind.operatore) {content = opFound};
                }
            }
            currPos = myPos;
            return new Token(tokenKind.fieldName) {content = alfaSeq};

        }

        delegate bool testCharFun(char c);

        private static string getTokenOfClass(string s, ref int currPos, Dictionary<string,OperatorDescriptor> classElements, testCharFun testFun) {
            int myPos = currPos;
            skipSpaces(s,ref myPos);
            if (myPos >= s.Length) return null;
            int foundNextPos = currPos;
            string foundValue = null;

            string currValue = s[myPos].ToString();
            while (anyKeyStartsWith(currValue.ToLower(), classElements)) {
                //si ferma sul primo carattere che rende currValue non associabile ad un operatore
                myPos++;
                if (classElements.ContainsKey(currValue.ToLower())) {
                    if (myPos == s.Length || !testFun(s[myPos])) {
                        //solo se la stringa è finita o non seguono altri alfanumerici è possibile effettuare il confronto
                        foundNextPos = myPos;
                        foundValue = currValue.ToLower();
                    }
                }
                if (myPos >= s.Length) break;
                currValue += char.ToLower(s[myPos]);//aggiunge massimo uno spazio tra pezzi distinti di un token
                if (isSpace(s[myPos])) {
                    skipSpaces(s,ref myPos);
                    if (!isSpace(s[myPos])) myPos--;//torna indietro altrimenti si perde un carattere
                }
            }

            if (foundValue != null) {
                currPos = foundNextPos;
                return foundValue;
            }
            

            return null;
        }
        private static  CultureInfo decCulture = new CultureInfo("en-US");
        private static Token getConstantNumeric(string s, ref int currPos) {
            if (currPos >= s.Length) return NoToken;
            if (!isNumeric(s[currPos])) return NoToken;
            string curr = "";
            int myPos = currPos;
            while (myPos < s.Length && isNumeric(s[myPos])) {
                curr += s[myPos];
                myPos++;
            }

            bool isDec = decimal.TryParse(curr, NumberStyles.Number, decCulture, out var res);
            if (!isDec) return NoToken;
            currPos = myPos;
            return new Token(tokenKind.constant) {value=res};

        }


        private static Token getConstantString(string s, ref int currPos) {
            if (currPos >= s.Length) return NoToken;
            if (s[currPos] != '\'') return NoToken;
            int len = s.Length;
            int index = currPos+1;
            var res = "";
            //skips the string
            while (index<len){
                if (s[index]!='\'') {
                    res+= s[index];
                    index++;
                    continue;
                }
                //it could be an end-string character
                if (((index+1)<len)&&(s[index+1]=='\'')){
                    //it isn't
                    res+= s[index]; //prende l'apice e lo mette nel risultato, saltando l'apice successivo
                    index++;
                    //res+= s[index]; Questo secondo apice non fa veramente parte della stringa equivalente
                    index++;
                    continue;
                }
                //res+= s[index]; //l'apice finale NON fa parte della stringa equivalente
                currPos = index+1;
                return new Token(tokenKind.constant) {value=res};
            }

            currPos = index;
            return new Token(tokenKind.constant) {value=res}; //ignoriamo l'assenza dell'apice finale...
        }

        ///// <summary>
        ///// reads a field name from a string
        ///// </summary>
        ///// <param name="s"></param>
        ///// <param name="currPos"></param>
        ///// <returns></returns>
        //private static Token getFieldName(string s, ref int currPos) {
        //    int myPos = currPos;
        //    var t = getAlfaToken(s, ref myPos);
        //    if (t.kind == tokenKind.fieldName) {
        //        currPos = myPos;
        //        return t;
        //    }

        //    return NoToken;
        //}

        /// <summary>
        /// Read a token from a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="currPos"></param>
        /// <returns></returns>
        public static Token getToken(string s, ref int currPos) {
            skipSpaces(s,ref currPos);
            if (currPos >= s.Length) return EndOfString;
            char c = s[currPos];
            if (isAlfa(c)) return getAlfaToken(s, ref currPos);
            if (c == '(') {
                currPos++;
                return OpenPar;
            }

            if (c == ')') {
                currPos++;
                return ClosePar;
            }
            if (c == '\'') return getConstantString(s,ref currPos);
            if (c == ',') {
                currPos++;
                return new Token(tokenKind.comma);
            }

            if (anyKeyStartsWith(c.ToString(), Operators)) return getOperator(s, ref currPos);
            if (isNumeric(c)) return getConstantNumeric(s, ref currPos);
            return NoToken;
        }



    }

    /// <summary>
    /// Helper class to get a MetaExpression from a string
    /// </summary>
    public static class MetaExpressionParser {

        private static BuildingExpression getExpression(string s, ref int currPos,bool wantsList) {
            Token.skipSpaces(s,ref currPos);
            if (currPos >= s.Length) return null;
            BuildingExpression expr=wantsList ? BuildingExpression.createList(null): new BuildingExpression(null);
            

            Token t = Token.getToken(s, ref currPos);
            //string allPar = "";
            bool internalWantsList = false;
            while (t.kind != tokenKind.endOfString && t.kind!=tokenKind.notFound) {
                switch (t.kind) {
                    case tokenKind.openPar:
                        //allPar += "(";
                        var internalExpr = getExpression(s, ref currPos, internalWantsList);
                        //if (!internalWantsList) internalExpr= BuildingExpression.createParentesizedExpression(internalExpr);
                        expr.addOperand(internalExpr);//la parentesi chiusa è letta nella funzione richiamata
                        internalWantsList = false;
                        break;
                    case tokenKind.closedPar:
                        //allPar += ")";
                        return wantsList ? expr?.nextParentList() :  BuildingExpression.createParentesizedExpression(expr.outerExpression());
                    case tokenKind.comma:
                        expr = expr.nextParentList();
                        break;
                    case tokenKind.constant:
                        var k = new constantExpression(expr, t.value);
                        if (expr != null) {
                            expr.addOperand(k);
                        }
                        else {//this can never happen
                            expr = k;
                        }
                        break;
                    case tokenKind.fieldName:
                        var field = new fieldExpression(expr, t.content);
                        if (expr != null) { 
                            expr.addOperand(field);
                        }
                        else { //this can never happen
                            expr = field;
                        }
                        break;
                    case tokenKind.operatore:
                        //allPar += t.content;
                        var opDescr = Token.getDescriptor(t.content);
                        if (opDescr.precedesList) internalWantsList = true;
                        expr = expr.addOperator(opDescr);
                        break;
                    case tokenKind.openEnvironment:
                        int closePosition = s.IndexOf("%>", currPos);
                        if (closePosition < 0) return null;
                        string envName = s.Substring(currPos, closePosition-currPos);
                        currPos = closePosition + 2;
                        expr.addOperand(new environmentExpression(expr,envName));
                        break;                   
                }

                t = Token.getToken(s, ref currPos);
            }            

            return expr?.outerExpression();
        }
      
        /// <summary>
        /// Transform a string into a MetaExpression
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static q From(string s) {
            if (string.IsNullOrEmpty(s)) return null;
            s = Token.normalize(s);
            int currPos = 0;
            var res= getExpression(s, ref currPos,false)?.build();
            if (res?.isTrue()??true) return null;
            return res;
        }

        /// <summary>
        /// Compiles a string into a MetaExpression
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static q toMetaExpression(this string s) {
            return From(s);
        }
    }
}
