using System;
using System.Collections.Generic;
using System.Data;


namespace mdl {

    /// <summary>
    /// Generic application event
    /// </summary>
    public interface IApplicationEvent { }
    

    /// <summary>
    /// base class for managing delegates
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="event"></param>
    public delegate void ApplicationEventHandlerDelegate<in TEvent>(TEvent @event)
        where TEvent : IApplicationEvent;

    /// <summary>
    /// Event generated at the beginning of the clear of a row
    /// </summary>
    public class StartClearMainRowEvent : IApplicationEvent {
       
    }


  

    /// <summary>
    /// Event generated at the beginning of the selection of a row
    /// </summary>
    public class StopClearMainRowEvent : IApplicationEvent {

    }


    /// <summary>
    /// Event generated at the beginning of the selection of a row
    /// </summary>
    public class StartMainRowSelectionEvent : IApplicationEvent {
        /// <summary>
        /// Row being selected
        /// </summary>
        public DataRow mainRow { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="r">Selected row</param>
        public StartMainRowSelectionEvent(DataRow r) {            
            mainRow = r;
        }
    }

    /// <summary>
    /// Event generated after the completion of the selection of a row
    /// </summary>
    public class StopMainRowSelectionEvent : IApplicationEvent {
        /// <summary>
        /// Row being selected, can be null if form was emptied
        /// </summary>
        public DataRow mainRow { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="r">Selected row</param>
        public StopMainRowSelectionEvent(DataRow r) {            
            this.mainRow = r;
        }
    }

    /// <summary>
    /// Event generated at the beginning of the selection of a row
    /// </summary>
    public class StartRowSelectionEvent : IApplicationEvent {
        /// <summary>
        /// Row being selected
        /// </summary>
        public DataRow row { get; }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="r">Selected row</param>
        public StartRowSelectionEvent(DataRow r) {           
            row = r;
        }
    }

    /// <summary>
    /// Event generated after the completion of the selection of a row
    /// </summary>
    public class StopRowSelectionEvent : IApplicationEvent {
        /// <summary>
        /// Row being selected, can be null if form was emptied
        /// </summary>
        public DataRow Row { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">Selected row</param>
        public StopRowSelectionEvent(DataRow r) {          
            this.Row = r;
        }
    }

    /// <summary>
    /// Possible states of a window form
    /// </summary>
    public enum ApplicationFormState {
        /// <summary>
        /// Empty state
        /// </summary>
        Empty,
        /// <summary>
        /// Insert mode
        /// </summary>
        Insert,
        /// <summary>
        /// Edit mode
        /// </summary>
        Edit
    };

    /// <summary>
    /// Event dispatched when the form changes state
    /// </summary>
    public class ChangeFormState : IApplicationEvent {
        /// <summary>
        /// Current form state
        /// </summary>
        public  ApplicationFormState state { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fState"></param>
        public ChangeFormState(ApplicationFormState fState) {
            state = fState;
        }
    }
}
