using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mdl {

    public interface IResponder {
        bool getResponse(object ctrl, string text, string caption, MessageBoxButtons btns, out DialogResult result);
        bool getResponse(object ctrl, string text, string caption, out DialogResult result);
        void showException(object f, string msg, Exception e, string logUrl);
        void showException(object f, string msg, Exception e);
        void showError(object F,string MainMessage, string LongMessage, string logUrl);

        void showNoRowFound(object F, string mainMessage, string longMessage);

        bool skipMessagesBox { get; set; }
        bool registerErrorMessages { get; set; }
        void clearMessages();
        List<string> getMessages();
    }
    /// <summary>
    /// 
    /// </summary>
    public interface IMessageShower {
        /// <summary>
        /// Shows a message and eventually gets a result
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="text"></param>
        /// <param name="caption"></param>
        /// <param name="btns"></param>
        /// <returns></returns>
        DialogResult Show(object ctrl, string text, string caption, MessageBoxButtons btns);

        DialogResult Show(string text, string caption, MessageBoxButtons btns);

        DialogResult Show(object ctrl, string text, string caption, MessageBoxButtons btns, MessageBoxIcon icons);
        DialogResult Show(object ctrl, string text, string caption, MessageBoxButtons btns, MessageBoxIcon icons, MessageBoxDefaultButton defBtn);

        DialogResult Show(string text, string caption, MessageBoxButtons btns, MessageBoxIcon icons,MessageBoxDefaultButton defBtn);

        DialogResult Show(object ctrl, string text, string caption);

        DialogResult Show(object ctrl, string text);

        DialogResult Show(string text, string caption);
        DialogResult Show(string text, string caption,MessageBoxButtons btns, MessageBoxIcon icons);

        DialogResult Show(string text);

        void ShowException(object f, string msg, Exception e, string logUrl);
        void ShowException(object f, string msg, Exception e);
        void ShowError(object F,string MainMessage, string LongMessage, string logUrl=null);
        void ShowNoRowFound(object F, string mainMessage, string longMessage);

        /// <summary>
        /// Attach an autoresponder to the message shower
        /// </summary>
        /// <param name="responder"></param>
        void setAutoResponder(IResponder responder);

        IResponder getResponder();

      
    }

    
}
