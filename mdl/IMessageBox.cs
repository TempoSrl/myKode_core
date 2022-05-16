
namespace mdl {
	 public enum DialogResult {
        //
        // Summary:
        //     Nothing is returned from the dialog box. This means that the modal dialog continues
        //     running.
        None = 0,
        //
        // Summary:
        //     The dialog box return value is OK (usually sent from a button labeled OK).
        OK = 1,
        //
        // Summary:
        //     The dialog box return value is Cancel (usually sent from a button labeled Cancel).
        Cancel = 2,
        //
        // Summary:
        //     The dialog box return value is Abort (usually sent from a button labeled Abort).
        Abort = 3,
        //
        // Summary:
        //     The dialog box return value is Retry (usually sent from a button labeled Retry).
        Retry = 4,
        //
        // Summary:
        //     The dialog box return value is Ignore (usually sent from a button labeled Ignore).
        Ignore = 5,
        //
        // Summary:
        //     The dialog box return value is Yes (usually sent from a button labeled Yes).
        Yes = 6,
        //
        // Summary:
        //     The dialog box return value is No (usually sent from a button labeled No).
        No = 7
    }

      //
    // Summary:
    //     Specifies constants defining which buttons to display on a System.Windows.Forms.MessageBox.
    public enum MessageBoxButtons {
        //
        // Summary:
        //     The message box contains an OK button.
        OK = 0,
        //
        // Summary:
        //     The message box contains OK and Cancel buttons.
        OKCancel = 1,
        //
        // Summary:
        //     The message box contains Abort, Retry, and Ignore buttons.
        AbortRetryIgnore = 2,
        //
        // Summary:
        //     The message box contains Yes, No, and Cancel buttons.
        YesNoCancel = 3,
        //
        // Summary:
        //     The message box contains Yes and No buttons.
        YesNo = 4,
        //
        // Summary:
        //     The message box contains Retry and Cancel buttons.
        RetryCancel = 5
    }

      //
    // Summary:
    //     Specifies constants defining which information to display.
    public enum MessageBoxIcon {
        //
        // Summary:
        //     The message box contain no symbols.
        None = 0,
        //
        // Summary:
        //     The message box contains a symbol consisting of a white X in a circle with a
        //     red background.
        Hand = 16,
        //
        // Summary:
        //     The message box contains a symbol consisting of white X in a circle with a red
        //     background.
        Stop = 16,
        //
        // Summary:
        //     The message box contains a symbol consisting of white X in a circle with a red
        //     background.
        Error = 16,
        //
        // Summary:
        //     The message box contains a symbol consisting of a question mark in a circle.
        //     The question-mark message icon is no longer recommended because it does not clearly
        //     represent a specific type of message and because the phrasing of a message as
        //     a question could apply to any message type. In addition, users can confuse the
        //     message symbol question mark with Help information. Therefore, do not use this
        //     question mark message symbol in your message boxes. The system continues to support
        //     its inclusion only for backward compatibility.
        Question = 32,
        //
        // Summary:
        //     The message box contains a symbol consisting of an exclamation point in a triangle
        //     with a yellow background.
        Exclamation = 48,
        //
        // Summary:
        //     The message box contains a symbol consisting of an exclamation point in a triangle
        //     with a yellow background.
        Warning = 48,
        //
        // Summary:
        //     The message box contains a symbol consisting of a lowercase letter i in a circle.
        Asterisk = 64,
        //
        // Summary:
        //     The message box contains a symbol consisting of a lowercase letter i in a circle.
        Information = 64
    }

      //
    // Summary:
    //     Specifies constants defining the default button on a System.Windows.Forms.MessageBox.
    public enum MessageBoxDefaultButton {
        //
        // Summary:
        //     The first button on the message box is the default button.
        Button1 = 0,
        //
        // Summary:
        //     The second button on the message box is the default button.
        Button2 = 256,
        //
        // Summary:
        //     The third button on the message box is the default button.
        Button3 = 512
    }

      //
    // Summary:
    //     Specifies options on a System.Windows.Forms.MessageBox.
    [System.Flags]
    public enum MessageBoxOptions {
        //
        // Summary:
        //     The message box is displayed on the active desktop.
        DefaultDesktopOnly = 131072,
        //
        // Summary:
        //     The message box text is right-aligned.
        RightAlign = 524288,
        //
        // Summary:
        //     Specifies that the message box text is displayed with right to left reading order.
        RtlReading = 1048576,
        //
        // Summary:
        //     The message box is displayed on the active desktop.
        ServiceNotification = 2097152
    }

      public enum HelpNavigator {
        //
        // Summary:
        //     The Help file opens to a specified topic, if the topic exists.
        Topic = -2147483647,
        //
        // Summary:
        //     The Help file opens to the table of contents.
        TableOfContents = -2147483646,
        //
        // Summary:
        //     The Help file opens to the index.
        Index = -2147483645,
        //
        // Summary:
        //     The Help file opens to the search page.
        Find = -2147483644,
        //
        // Summary:
        //     The Help file opens to the index entry for the first letter of a specified topic.
        AssociateIndex = -2147483643,
        //
        // Summary:
        //     The Help file opens to the topic with the specified index entry, if one exists;
        //     otherwise, the index entry closest to the specified keyword is displayed.
        KeywordIndex = -2147483642,
        //
        // Summary:
        //     The Help file opens to a topic indicated by a numeric topic identifier.
        TopicId = -2147483641
    }

	public interface IMessageBoxFactory {
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, bool displayHelpButton);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options);
		DialogResult Show(string text);
		DialogResult Show(string text, string caption);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);
		DialogResult Show(object owner, string text, string caption);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, string keyword);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, string keyword);
		DialogResult Show(object owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath);
		DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options);
		DialogResult Show(object owner, string text);
	}
}