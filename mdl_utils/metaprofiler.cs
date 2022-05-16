using System;
using System.Data;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace mdl_utils
{

//    /// <summary>
//    /// Gives acces to windows function timeGetTime 
//    /// </summary>
//	public class GetTimer{		

//        /// <summary>
//        /// Windows timer function
//        /// </summary>
//        /// <returns></returns>
//[DllImport("winmm.dll")]
//		internal static extern UInt32 timeGetTime();

//	}

    /// <summary>
    /// Timer for making measurations
    /// </summary>
	public class crono {
        /// <summary>
        /// Timer name
        /// </summary>
		public string name;
        //long Start;
        Stopwatch S = null;
        //DateTime Start;

        /// <summary>
        /// Creates a timer  with a specified level
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="level"></param>
        public crono(crono parent, string name, int level) {
            this.name = name;
            //Start = GetTimer.timeGetTime();
            S = new Stopwatch();
            S.Start();
        }

        /// <summary>
        /// Creates a timer
        /// </summary>
        /// <param name="name"></param>
		public crono(string name) {
            this.name = name;
            //Start = GetTimer.timeGetTime();
            S = new Stopwatch();
            S.Start();
        }

        /// <summary>
        /// Stops a timer and gives duration
        /// </summary>
        /// <returns></returns>
		public long GetDuration() {
            //long Stop = GetTimer.timeGetTime();// DateTime.Now;
            S.Stop();
            //return Stop-Start;
            return S.ElapsedMilliseconds;

        }
    }

	internal class eventstat {
		int totalcalls;
		int maxdeep;
		int totalduration;
        string name;
        public Dictionary<string, int> childs = new Dictionary<string, int>();
        public eventstat() {
            totalcalls = 0;
            maxdeep = 0;
            totalduration = 0;
        }

		public eventstat(string name){
			totalcalls=0;
			maxdeep=0;
			totalduration=0;
            this.name = name;
		}
		public int  updatewith(crono C,int handle){
			int duration = Convert.ToInt32(C.GetDuration());
			totalduration+=duration;
            if (maxdeep < handle) maxdeep = handle;
			totalcalls++;
            return duration;
		}
		public int  updatewith(int duration){
			totalduration+=duration;
			totalcalls++;
			return totalduration;
		}

        public void AddChildActivity(string name, int duration) {
            if (name==null)return;
            if (childs.ContainsKey(name)) {
                childs[name] += duration;
            }
            else {
                childs.Add(name, duration);
            }
        }

        public override string ToString() {
	        if (totalduration == 0) return "";
	        var s = name.PadLeft(90) //+ "maxdeep:" + maxdeep.ToString("D5") 
	                + totalduration.ToString().PadLeft(10)
	                + totalcalls.ToString().PadLeft(7);
			if (childs.Count > 0) {
                foreach (string key in childs.Keys) {
	                if (childs[key] == 0) continue;
                    s += $"\r\n child: {key.PadLeft(82)}{childs[key].ToString().PadLeft(10)}";
                }

                s += "\r\n";
			}
            return s;
		}

        public static string GetHeader() {
            return "eventname".PadLeft(90) + "ms last".PadLeft(10)  + "calls".PadLeft(7); 
        }

        private class sortDurationHelper : IComparer {
            int IComparer.Compare(object a, object b) {
                eventstat c1 = (eventstat)a;
                eventstat c2 = (eventstat)b;
                if (c1.totalduration < c2.totalduration)
                    return 1;
                if (c1.totalduration > c2.totalduration)
                    return -1;
                else
                    return 0;
            }
        }

        public static IComparer sortDuration() {
            return (IComparer)new sortDurationHelper();
        }
	}

	/// <summary>
	/// Summary description for metaprofiler.
	/// </summary>
	public class metaprofiler
	{
        /// <summary>
        /// Current nesting level of profiling timer
        /// </summary>
		public static int level=0;

        /// <summary>
        /// Enable all timing operations
        /// </summary>
		public static bool Enabled=Debugger.IsAttached;

        /// <summary>
        /// Stack of timers
        /// </summary>
        public static Stack timers = new Stack();

        /// <summary>
        /// Total time grouped by operation name
        /// </summary>
		private static Dictionary<string,eventstat> totals = new Dictionary<string,eventstat>();

        /// <summary>
        /// Default constructor
        /// </summary>
		public metaprofiler()
		{
//			level=0;
//			timers = new ArrayList(20);
//			totals= new Hashtable();
		}

        /// <summary>
        /// Starts a timer with a given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>handler for the timer</returns>
		public static int StartTimer(string name){
			if (!Enabled) return -1;
            if (Thread.CurrentThread.Name != null) return -1;
            crono newnode;
            if (name.Length > 80)
                newnode = new crono(name.Substring(0, 80));
            else
                newnode = new crono(name);
            timers.Push(newnode);
            //QueryCreator.MarkEvent($"Opening timer {name} n. {timers.Count}  thread {Thread.CurrentThread.Name}");
            return timers.Count;
		}

        /// <summary>
        /// Stop a specified timer
        /// </summary>
        /// <param name="handle"></param>
		public static void StopTimer(int handle){
			if (!Enabled) return;		                                                   
            if (Thread.CurrentThread.Name != null) return ;
            if (handle < 0) return;
            //QueryCreator.MarkEvent($"closing timer {handle} thread {Thread.CurrentThread.Name}");
		    if (timers.Count == 0) {
                //QueryCreator.MarkEvent($"No timer available: error on cronometer {handle} len{timers.Count}, thread {Thread.CurrentThread.Name}");
                object[] arr = timers.ToArray();
                for (int i = 0; i < arr.Length; i++) {
                    //QueryCreator.MarkEvent($"Position {i}:{((crono)arr[i]).name}");
                }
		        return;
		    }
		    if (timers.Count != handle) {
                //QueryCreator.MarkEvent($"Mismatch: error on cronometer {handle} len{timers.Count}, thread {Thread.CurrentThread.Name}" );
                object []arr = timers.ToArray();
                for (int i = 0; i < arr.Length; i++) {                    
                    //QueryCreator.MarkEvent($"Position {i}:{((crono)arr[i]).name}");
                }
                //Enabled = false;
                //return;
            }
            
            var C = timers.Pop() as crono;
            if (C == null) {
                //QueryCreator.MarkEvent($"error on cronometer {handle} len{timers.Count}, thread {Thread.CurrentThread.Name}");
                Enabled = false;
                return;
            }
			string name = C.name;
			eventstat EVS;
            //QueryCreator.MarkEvent("Stopping Timer " + name.PadLeft(20) + " N. " + handle.ToString("D4"));
            if (!totals.TryGetValue(name, out EVS)) {
                EVS=new eventstat(name);
                totals[name] = EVS;
            }
			
            int duration = EVS.updatewith(C, handle);

            if (timers.Count > 0) {
                var lastC = timers.Peek() as crono;
                eventstat lastEV;
                if (!totals.TryGetValue(lastC.name, out lastEV)) {
                    lastEV=new eventstat(lastC.name);
                    totals[lastC.name] = lastEV;
                }
                lastEV.AddChildActivity(name, duration);
            }

            var parts = name.Split('*');
            if (parts.Length > 1) {
	            string subpart = parts[0];
	            eventstat subEVS;
	            if (!totals.TryGetValue(subpart, out subEVS)) {
		            subEVS=new eventstat(subpart);
		            totals[subpart] = subEVS;
	            }
	            subEVS.updatewith(duration);
            }


        }

        /// <summary>
        /// Reset all timers
        /// </summary>
		public static void Reset(){
			totals.Clear();
		}


        /// <summary>
        /// Returns a summary of all timers
        /// </summary>
        /// <returns></returns>
		public static string ShowAll(){
			var  All= new StringBuilder("Current level is "+level+"\r\n");
            All.Append( eventstat.GetHeader() + "\r\n");
            var A = new ArrayList();

            foreach (var k in totals.Keys) {
                A.Add(totals[k]);
            }
            A.Sort(eventstat.sortDuration());

            foreach (eventstat k in A) {
	            var kString = k.ToString();
	            if (kString == "") continue;
				All.AppendLine(kString);
            }
			//QueryCreator.MarkEvent(All);
			return All.ToString();


		}
	}
}
