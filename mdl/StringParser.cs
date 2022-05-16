using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable IDE1006 // Naming Styles

namespace mdl {
    /// <summary>
    /// Helper class to parse string
    /// </summary>
   	public static class StringParser {
        
            /// <summary>
            /// Get next identifier in a string starting the search from start position
            /// </summary>
            /// <param name="S"></param>
            /// <param name="start"></param>
            /// <returns></returns>
			public static string GetNextIdentifier(string S, int start){
				int pos=start;
				while ((Char.IsLetterOrDigit(S[pos]) || (S[pos]=='_'))&& (pos<S.Length))pos++;
				return S.Substring(start,pos-start);
			}

			
            /// <summary>
            /// searches the end of a string 
            /// </summary>
            /// <param name="S"></param>
            /// <param name="start"></param>
            /// <param name="stop">stop character (not preceded by a slash)</param>
            /// <returns></returns>
			public static int closedString(string S, int start,char stop){
				int index=start;
				while (index<S.Length){
					if (S[index]=='\\'){
						index++;
						index++;
						continue;
					}
					if (S[index]==stop){
						return index+1;
					}
					index++;
				}
				return -1;
			}

			/// <summary>
			/// Restituisce la posizione della fine di un blocco, saltando i blocchi annidati 
			///	 o -1 se il blocco non si chiude
			/// </summary>
			/// <param name="S"></param>
			/// <param name="start"></param>
			/// <param name="BEGIN"></param>
			/// <param name="END"></param>
			/// <returns></returns>
			public static int closeBlock(string S, int start, char BEGIN,char END){
				int index=start;
				int level=1;
				while ((index>=0)&&(index<S.Length)){
					index= nextNonComment(S,index);
					if (index<0) return -1;
					char C=S[index];
					if (C=='"'){
						index= closedString(S,index+1,'"');
						continue;
					}
					if (C=='\''){
						index= closedString(S,index+1,'\'');
						continue;
					}
					if (C==BEGIN){
						level++;
						index++;
						continue;
					}
					if (C==END){
						level--;
						index++;
						if(level==0) return index;
						continue;
					}							
					index++;
				}
				return -1;
			}

            /// <summary>
            /// Searches for the closing of a multiline comment C or sql-style
            /// </summary>
            /// <param name="S"></param>
            /// <param name="start"></param>
            /// <returns></returns>
			public static int closedComment(string S,int start){
				if (S.IndexOf("*/",start)<0) return -1;
				return S.IndexOf("*/",start)+2;
			}


            /// <summary>
            /// Check if character at position pos is inside a comment
            /// </summary>
            /// <param name="S"></param>
            /// <param name="start">start of string to scan</param>
            /// <param name="pos">position to check</param>
            /// <returns></returns>
			public static bool IsInsideComment(string S, int start, int pos){
				int index=start;
				while ((index<S.Length)&&(index>=0)){
					char C=S[index];
					//Salta i commenti normali
					if (C=='/'){
						try {
							//vede se è un commento normale ossia /* asas */
							if (S[index+1]=='*'){
								index= closedComment(S,index+2);
								if (pos<index) return true;
								continue;
							}
							if (S[index+1]=='/'){
								int next1 = S.IndexOf("\n",index);
								int next2 = S.IndexOf("\r",index);
								if ((next1==-1)&&(next2==-1)) return true;
								if (next1==-1){
									index=next2+1;
									if (pos<index) return true;
									continue;
								}
								if (next2==-1){
									index=next1+1;
									if (pos<index) return true;
									continue;
								}
								if (next1<next2) 
									index=next1+1;
								else
									index=next2+1;
								if (pos<index) return true;
								continue;

							}
						}
						catch {
							return true;
						}
					}
					if ((C==' ')||(C=='\n')||(C=='\r')||(C=='\t')){
						index++;
						if (index>pos) return false;
						continue;
					}
					index++;
					if (index>pos) return false;
					continue;
				}
			
				return false;
			}
			/// <summary>
			/// Restituisce l'indice del prossimo non-commento e non-blank, o -1 se non ce ne sono. 
			/// </summary>
			/// <param name="S"></param>
			/// <param name="start"></param>
			/// <returns></returns>
			public static int nextNonComment(string S, int start){
				int index=start;
				while ((index<S.Length)&&(index>=0)){
					char C=S[index];

					//Salta i commenti normali
					if (C=='/'){
						try {
							//vede se è un commento normale ossia /* asas */
							if (S[index+1]=='*'){
								index= closedComment(S,index+2);
								continue;
							}
							if (S[index+1]=='/'){
								int next1 = S.IndexOf("\n",index);
								int next2 = S.IndexOf("\r",index);
								if ((next1==-1)&&(next2==-1)) return S.Length;
								if (next1==-1){
									index=next2+1;
									continue;
								}
								if (next2==-1){
									index=next1+1;
									continue;
								}
								if (next1<next2) 
									index=next1+1;
								else
									index=next2+1;
								continue;

							}
						}
						catch {
							return -1;
						}
					}
					if ((C==' ')||(C=='\n')||(C=='\r')||(C=='\t')){
						index++;
						continue;
					}
					return index;
				}
			
				return -1;
			}


	
		}
}
