using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace LineEndings2022
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    public class OptionPageGrid : DialogPage
    {
        public enum MarkType : byte
        {
            ArrowLeft = 0x21,
            ArrowDown = 0x24,
            ArrowLeftDash = 0x33,
            ArrowDownDash = 0x36,
            ArrowCornerDownLeft = 0x38,
            ReturnLeft = 0x40,
            NewLineLeft = 0x42,
            ArrowDown2 = 0x69,
        }
        private MarkType lfMark = MarkType.ArrowDown;

        [Category("Line Ending Mark")]
        [DisplayName("LF")]
        [Description("line ending LF mark (require re-open text)")]
        //[Editor(typeof(MarkTextEditor), typeof(UITypeEditor))]
        public MarkType LfMark
        {
            get { return lfMark; }
            set { lfMark = value; }
        }
        private MarkType crlfMark = MarkType.ArrowCornerDownLeft;

        [Category("Line Ending Mark")]
        [DisplayName("CRLF")]
        [Description("line ending CRLF mark (require re-open text)")]
        //[Editor(typeof(MarkTextEditor), typeof(UITypeEditor))]
        public MarkType CrLfMark
        {
            get { return crlfMark; }
            set { crlfMark = value; }
        }

        public Action OnApplyMarks;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            OnApplyMarks?.Invoke();
        }
    }
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(LineEndings2022Package.PackageGuidString)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Show Line Endings", "Line Endings", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class LineEndings2022Package : AsyncPackage
    {
        /// <summary>
        /// LineEndings2022Package GUID string.
        /// </summary>
        public const string PackageGuidString = "09058149-5766-4bb0-83c7-0b26796b20f0";

        private static LineEndings2022Package instance;
        public static LineEndings2022Package Instance => instance; 

        #region Package Members

        public OptionPageGrid.MarkType LfMark
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.LfMark;
            }
        }
        public OptionPageGrid.MarkType CrLfMark
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.CrLfMark;
            }
        }
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            instance = this;
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion
    }
}
